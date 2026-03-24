using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocoCalc.Models;
using LocoCalc.Services;

namespace LocoCalc.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly LocoRepository _locoRepo;
    private readonly ConsistRepository _consistRepo;

    public LocalizationService L => LocalizationService.Instance;

    // ── Loco catalogue ────────────────────────────────────────────────────────
    public ObservableCollection<LocomotiveDefinition> AvailableLocos { get; } = new();

    [ObservableProperty]
    private LocomotiveDefinition? _selectedLoco;

    // ── Current consist ───────────────────────────────────────────────────────
    public ObservableCollection<ConsistEntryViewModel> ConsistEntries { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BrakingPercentageDisplay))]
    private double _brakingPercentage;

    public string BrakingPercentageDisplay    => $"{BrakingPercentage:F1} %";
    public string TotalWeightDisplay          => ConsistEntries.Count == 0 ? "—" :
        $"{ConsistEntries.Sum(e => e.TotalWeightTonnes):F0} t";
    public string ActiveBrakeWeightDisplay    => ConsistEntries.Count == 0 ? "—" :
        $"{ConsistEntries.Where(e => e.BrakesEnabled).Sum(e => e.ActiveBrakingWeight):F0} t";
    public string TotalLengthDisplay          => ConsistEntries.Count == 0 ? "—" :
        $"{ConsistEntries.Sum(e => e.LengthM):F0} m";

    // ── Consist name / save ───────────────────────────────────────────────────
    [ObservableProperty]
    private string _consistName = string.Empty;
    private string _currentConsistId = Guid.NewGuid().ToString();

    // ── Saved consists list ───────────────────────────────────────────────────
    public ObservableCollection<Consist> SavedConsists { get; } = new();

    // ── Status / error ────────────────────────────────────────────────────────
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // ── EDB dialog ────────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EdbDialogVisible))]
    private string _edbDialogLocoName = string.Empty;

    public bool EdbDialogVisible => !string.IsNullOrEmpty(EdbDialogLocoName);
    public string EdbDialogBody => L.EdbDialogBody(EdbDialogLocoName);

    // Pending entry awaiting EDB decision
    private ConsistEntry? _pendingEntry;

    // ── Language ──────────────────────────────────────────────────────────────
    public bool IsCzech
    {
        get => L.Language == AppLanguage.Czech;
        set { L.Language = value ? AppLanguage.Czech : AppLanguage.English; OnPropertyChanged(nameof(IsCzech)); OnPropertyChanged(nameof(IsEnglish)); RefreshConsistName(); }
    }
    public bool IsEnglish
    {
        get => L.Language == AppLanguage.English;
        set { L.Language = value ? AppLanguage.English : AppLanguage.Czech; OnPropertyChanged(nameof(IsCzech)); OnPropertyChanged(nameof(IsEnglish)); RefreshConsistName(); }
    }

    public MainViewModel()
    {
        _locoRepo    = new LocoRepository();
        _consistRepo = new ConsistRepository();
        L.PropertyChanged += (_, _) => OnPropertyChanged(nameof(BrakingPercentageDisplay));
        RefreshLocos();
        RefreshSavedConsists();
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddLocoToConsist()
    {
        if (SelectedLoco is null) return;
        ClearError();

        // Determine position BEFORE adding so we know if it will be Front
        var futureIndex = ConsistEntries.Count;   // will be appended at this index
        var futureTotal = ConsistEntries.Count + 1;
        var pos = BrakingCalculator.DerivePosition(futureIndex, futureTotal);

        var entry = new ConsistEntry
        {
            DefinitionId          = SelectedLoco.Id,
            Designation           = SelectedLoco.Designation,
            TotalWeightTonnes     = SelectedLoco.TotalWeightTonnes,
            LengthM               = SelectedLoco.LengthM,
            BrakingWeightTonnes   = SelectedLoco.BrakingWeightTonnes,
            BrakingWeightWithEDB  = SelectedLoco.BrakingWeightWithEDB,
            Position              = pos,
            BrakesEnabled         = BrakingCalculator.DefaultBrakesEnabled(pos),
            EdbActive             = false,
        };

        // If loco has EDB AND it will be the front/lead loco, ask user
        if (SelectedLoco.HasEDB && pos == ConsistPosition.Front)
        {
            _pendingEntry = entry;
            EdbDialogLocoName = SelectedLoco.Designation;
            OnPropertyChanged(nameof(EdbDialogBody));
            return;  // wait for user response
        }

        CommitEntry(entry);
    }

    [RelayCommand]
    private void EdbYes()
    {
        if (_pendingEntry is null) return;
        _pendingEntry.EdbActive = true;
        _pendingEntry.BrakingWeightTonnes = _pendingEntry.BrakingWeightWithEDB!.Value;
        CommitEntry(_pendingEntry);
        _pendingEntry = null;
        EdbDialogLocoName = string.Empty;
    }

    [RelayCommand]
    private void EdbNo()
    {
        if (_pendingEntry is null) return;
        _pendingEntry.EdbActive = false;
        CommitEntry(_pendingEntry);
        _pendingEntry = null;
        EdbDialogLocoName = string.Empty;
    }

    private void CommitEntry(ConsistEntry entry)
    {
        var vm = new ConsistEntryViewModel(entry);
        vm.PropertyChanged += OnEntryChanged;
        ConsistEntries.Add(vm);
        ReassignPositions();
        Recalculate();
        RefreshConsistName();
        StatusMessage = L.StatusAdded(entry.Designation);
    }

    [RelayCommand]
    private void RemoveEntry(ConsistEntryViewModel? entry)
    {
        if (entry is null) return;
        ClearError();
        var name = entry.Designation;
        ConsistEntries.Remove(entry);
        ReassignPositions();
        Recalculate();
        RefreshConsistName();
        StatusMessage = L.StatusRemoved(name);
    }

    [RelayCommand]
    private void MoveUp(ConsistEntryViewModel? entry)
    {
        if (entry is null) return;
        var idx = ConsistEntries.IndexOf(entry);
        if (idx <= 0) return;
        ConsistEntries.Move(idx, idx - 1);
        ReassignPositions();
        RefreshConsistName();
    }

    [RelayCommand]
    private void MoveDown(ConsistEntryViewModel? entry)
    {
        if (entry is null) return;
        var idx = ConsistEntries.IndexOf(entry);
        if (idx < 0 || idx >= ConsistEntries.Count - 1) return;
        ConsistEntries.Move(idx, idx + 1);
        ReassignPositions();
        RefreshConsistName();
    }

    [RelayCommand]
    private void ToggleBrakes(ConsistEntryViewModel? entry)
    {
        if (entry is null) return;
        ClearError();
        if (entry.BrakesLocked) return;
        if (entry.BrakesEnabled && ConsistEntries.Count(e => e.BrakesEnabled) <= 1)
        {
            ErrorMessage = L.ErrorLastBrakeLoco(entry.Designation);
            return;
        }
        entry.BrakesEnabled = !entry.BrakesEnabled;
        Recalculate();
    }

    [RelayCommand]
    private void ToggleEdb(ConsistEntryViewModel? entry)
    {
        if (entry is null || !entry.HasEDB) return;
        entry.EdbActive = !entry.EdbActive;
        Recalculate();
    }

    [RelayCommand]
    private void SaveConsist()
    {
        ClearError();
        var consist = new Consist
        {
            Id      = _currentConsistId,
            Name    = ConsistName,
            Entries = ConsistEntries.Select(e => e.ToModel()).ToList()
        };
        _consistRepo.Save(consist);
        RefreshSavedConsists();
        StatusMessage = L.StatusSaved(ConsistName);
    }

    [RelayCommand]
    private void LoadConsist(Consist? consist)
    {
        if (consist is null) return;
        ClearError();
        ConsistEntries.Clear();
        foreach (var entry in consist.Entries)
        {
            var vm = new ConsistEntryViewModel(entry);
            vm.PropertyChanged += OnEntryChanged;
            ConsistEntries.Add(vm);
        }
        ReassignPositions();
        ConsistName       = consist.Name;
        _currentConsistId = consist.Id;
        Recalculate();
        StatusMessage = L.StatusLoaded(consist.Name);
    }

    [RelayCommand]
    private void DeleteConsist(Consist? consist)
    {
        if (consist is null) return;
        _consistRepo.Delete(consist);
        RefreshSavedConsists();
        StatusMessage = L.StatusDeleted(consist.Name);
    }

    [RelayCommand]
    private void NewConsist()
    {
        ClearError();
        ConsistEntries.Clear();
        ConsistName       = string.Empty;
        _currentConsistId = Guid.NewGuid().ToString();
        Recalculate();
        StatusMessage = L.StatusNewConsist;
    }

    [RelayCommand]
    private void RefreshLocos()
    {
        _locoRepo.Invalidate();
        AvailableLocos.Clear();
        foreach (var loco in _locoRepo.GetAll())
            AvailableLocos.Add(loco);
    }

    [RelayCommand]
    private void ClearError() => ErrorMessage = string.Empty;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ReassignPositions()
    {
        int total = ConsistEntries.Count;
        for (int i = 0; i < total; i++)
        {
            var vm  = ConsistEntries[i];
            var pos = BrakingCalculator.DerivePosition(i, total);
            if (vm.Position != pos)
                vm.Position = pos;
            // EDB only makes sense at front
            if (pos != ConsistPosition.Front && vm.EdbActive)
                vm.EdbActive = false;
        }
    }

    private void OnEntryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ConsistEntryViewModel.BrakesEnabled)
                           or nameof(ConsistEntryViewModel.EdbActive))
            Recalculate();
    }

    private void Recalculate()
    {
        BrakingPercentage = BrakingCalculator.Calculate(ConsistEntries.Select(e => e.ToModel()));
        OnPropertyChanged(nameof(TotalWeightDisplay));
        OnPropertyChanged(nameof(ActiveBrakeWeightDisplay));
        OnPropertyChanged(nameof(TotalLengthDisplay));
    }

    private void RefreshConsistName()
    {
        ConsistName = ConsistNameGenerator.Generate(
            ConsistEntries.Select(e => e.ToSnapshot()).ToList(),
            L.Language);
    }

    private void RefreshSavedConsists()
    {
        SavedConsists.Clear();
        foreach (var c in _consistRepo.GetAll())
            SavedConsists.Add(c);
    }
}
