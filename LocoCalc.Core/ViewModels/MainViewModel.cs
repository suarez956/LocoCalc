using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocoCalcAvalonia.Models;
using LocoCalcAvalonia.Services;

namespace LocoCalcAvalonia.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly LocoRepository _locoRepo;
    private readonly ConsistRepository _consistRepo;
    private readonly UicNameHistory _uicHistory;
    public LocalizationService L => LocalizationService.Instance;

    // ── Stations ──────────────────────────────────────────────────────────────
    public IReadOnlyList<Station> AllStations => StationRepository.Instance.All;

    [ObservableProperty] private Station? _startStation;
    [ObservableProperty] private Station? _endStation;

    // ── Language ─────────────────────────────────────────────────────────────
    public bool IsCzech
    {
        get => L.Language == AppLanguage.Czech;
        set { if (value) L.Language = AppLanguage.Czech; OnPropertyChanged(); OnPropertyChanged(nameof(IsEnglish)); RefreshAll(); }
    }
    public bool IsEnglish
    {
        get => L.Language == AppLanguage.English;
        set { if (value) L.Language = AppLanguage.English; OnPropertyChanged(); OnPropertyChanged(nameof(IsCzech)); RefreshAll(); }
    }

    // ── Loco catalogue ────────────────────────────────────────────────────────
    public ObservableCollection<LocomotiveDefinition> AvailableLocos { get; } = new();
    public ObservableCollection<TractionGroup>        TractionGroups { get; } = new();
    public ObservableCollection<LocoListItem>         LocoFlatList   { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddLoco))]
    private LocomotiveDefinition? _selectedLoco;

    public bool CanAddLoco      => SelectedLoco is not null;
    public bool CanGeneratePdf  => ConsistEntries.Count > 0;

    [ObservableProperty]
    private bool _pdfDarkMode = false;

    [ObservableProperty] private bool _autoOpenPdf = true;

    // Injected by platform project
    public Services.IPdfSaveService? PdfSaveService  { get; set; }
    public Services.IPdfGenerator?   PdfGenerator    { get; set; }

    /// <summary>True when both PDF services are injected.</summary>
    public bool IsPdfSupported => PdfSaveService is not null && PdfGenerator is not null;

    /// <summary>Call after injecting PdfSaveService/PdfGenerator to refresh UI binding.</summary>
    public void NotifyPdfSupportChanged() => OnPropertyChanged(nameof(IsPdfSupported));

    // ── Mobile navigation ────────────────────────────────────────────────
    public enum MobileTab { Consist, AddLoco, Etcs, Menu }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MobileTabConsist))]
    [NotifyPropertyChangedFor(nameof(MobileTabAddLoco))]
    [NotifyPropertyChangedFor(nameof(MobileTabEtcs))]
    [NotifyPropertyChangedFor(nameof(MobileTabMenu))]
    private MobileTab _activeMobileTab = MobileTab.Consist;

    public bool MobileTabConsist => ActiveMobileTab == MobileTab.Consist;
    public bool MobileTabAddLoco => ActiveMobileTab == MobileTab.AddLoco;
    public bool MobileTabEtcs    => ActiveMobileTab == MobileTab.Etcs;
    public bool MobileTabMenu    => ActiveMobileTab == MobileTab.Menu;

    [RelayCommand] private void SetMobileTabConsist() => ActiveMobileTab = MobileTab.Consist;
    [RelayCommand] private void SetMobileTabAddLoco() => ActiveMobileTab = MobileTab.AddLoco;
    [RelayCommand] private void SetMobileTabEtcs()    => ActiveMobileTab = MobileTab.Etcs;
    [RelayCommand] private void SetMobileTabMenu()    => ActiveMobileTab = MobileTab.Menu;

    // After adding a loco on mobile, jump back to consist view
    partial void OnActiveMobileTabChanged(MobileTab value) { }


    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value) => RefreshLocos();

    // ── Consist ──────────────────────────────────────────────────────────────
    public ObservableCollection<ConsistEntryViewModel> ConsistEntries { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BrakingPercentageDisplay))]
    [NotifyPropertyChangedFor(nameof(BrakingPercentageColor))]
    [NotifyPropertyChangedFor(nameof(EtcsBrakingColor))]
    [NotifyPropertyChangedFor(nameof(EtcsWarningVisible))]
    [NotifyPropertyChangedFor(nameof(EtcsWarningText))]
    private double _brakingPercentage;

    public string BrakingPercentageDisplay  => ConsistEntries.Count == 0 ? "—" : $"{BrakingPercentage:F1} %";
    public string TotalWeightDisplay        => ConsistEntries.Count == 0 ? "—" : $"{ConsistEntries.Sum(e => e.TotalWeightTonnes):F1} t";
    public string TotalLengthDisplay        => ConsistEntries.Count == 0 ? "—" : $"{ConsistEntries.Sum(e => e.LengthM):F0} m";
    public string ActiveBrakeWeightDisplay  => ConsistEntries.Count == 0 ? "—" :
        $"{ConsistEntries.Where(e => e.BrakesEnabled).Sum(e => e.ActiveBrake):F0} t";

    public string BrakingPercentageColor =>
        BrakingPercentage < 50 ? "#ef4444" : BrakingPercentage < 65 ? "#f97316" : "#22c55e";

    // ── Consist name & save ──────────────────────────────────────────────────
    [ObservableProperty] private string _consistName = string.Empty;
    private string _currentConsistId = Guid.NewGuid().ToString();

    // ── Saved consists ───────────────────────────────────────────────────────
    public ObservableCollection<Consist> SavedConsists { get; } = new();

    // ── Status / error ───────────────────────────────────────────────────────
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool   _isErrorToast  = false;

    private void ShowToast(string msg, bool isError = false)
    {
        IsErrorToast   = isError;
        StatusMessage  = msg;
    }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ErrorVisible))]
    private string _errorMessage = string.Empty;
    public bool ErrorVisible => !string.IsNullOrEmpty(ErrorMessage);

    // ── EDB dialog ────────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EdbDialogVisible))]
    [NotifyPropertyChangedFor(nameof(EdbDialogBody))]
    private string _edbDialogLocoName = string.Empty;
    public bool EdbDialogVisible => !string.IsNullOrEmpty(EdbDialogLocoName);
    public string EdbDialogBody  => L.EdbDialogBody(EdbDialogLocoName, _pendingEdbWith, _pendingEdbWithout);

    private ConsistEntry? _pendingEntry;
    private double _pendingEdbWith, _pendingEdbWithout;

    // ── Rename dialog ─────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RenameDialogVisible))]
    [NotifyPropertyChangedFor(nameof(RenameInputMatchesPrefixes))]
    [NotifyPropertyChangedFor(nameof(RenameUicMismatchVisible))]
    private ConsistEntryViewModel? _renameTarget;
    public bool RenameDialogVisible => RenameTarget is not null;

    /// <summary>Raw digits only (0–12), used to derive formatted display and history.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RenameInputFormatted))]
    [NotifyPropertyChangedFor(nameof(RenameInputMatchesPrefixes))]
    [NotifyPropertyChangedFor(nameof(RenameUicMismatchVisible))]
    [NotifyPropertyChangedFor(nameof(RenameCheckDigitInvalid))]
    [NotifyPropertyChangedFor(nameof(RenameCheckDigitWarning))]
    private string _renameInput = string.Empty;

    /// <summary>UIC-formatted version of the current raw-digit input.</summary>
    public string RenameInputFormatted =>
        UicFormatter.Format(RenameInput, RenameTarget?.UicFormat ?? "A");

    /// <summary>null = no constraint or too few digits; true = prefix match; false = mismatch warning.</summary>
    public bool? RenameInputMatchesPrefixes =>
        UicFormatter.MatchesPrefixes(RenameInput, RenameTarget?.UicPrefixes, RenameTarget?.UicPrefixOffset ?? 4);

    public bool RenameUicMismatchVisible => RenameInputMatchesPrefixes == false;

    /// <summary>True when all 12 digits are entered, check validation is enabled, and the check digit is wrong.</summary>
    public bool RenameCheckDigitInvalid =>
        RenameTarget?.UicValidateCheck == true &&
        UicFormatter.IsComplete(RenameInput) &&
        !UicFormatter.IsCheckDigitValid(RenameInput);

    /// <summary>Warning text showing the expected check digit.</summary>
    public string RenameCheckDigitWarning =>
        RenameCheckDigitInvalid
            ? L.RenameCheckDigitWarning(UicFormatter.CalculateCheckDigit(RenameInput))
            : string.Empty;

    public ObservableCollection<string> RenameHistorySuggestions { get; } = new();
    public bool RenameHistoryVisible => RenameHistorySuggestions.Count > 0;

    // ── ETCS ─────────────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EtcsMaxSpeedDisplay))]
    private int? _speedOverride;

    public int EtcsDefSpeed => ConsistEntries.Count == 0 ? 0 :
        BrakingCalculator.ConsistMaxSpeed(ConsistEntries.Select(e => e.ToModel()));

    public string EtcsMaxSpeedDisplay =>
        ConsistEntries.Count == 0 ? "—" : $"{SpeedOverride ?? EtcsDefSpeed} km/h";

    public string EtcsFpClass    => BrakingCalculator.ConsistFpClass(ConsistEntries.Select(e => e.ToModel()));
    public int    EtcsFpMm       => EtcsFpClass == "FP3" ? 130 : 100;
    public string EtcsAxleLoad   => BrakingCalculator.ConsistAxleLoad(ConsistEntries.Select(e => e.ToModel())) ?? "—";

    public bool   EtcsWarningVisible => BrakingPercentage > 0 && BrakingPercentage < 50;
    public string EtcsWarningText    => L.WarnLowBrake(BrakingPercentage);

    public string EtcsBrakingColor =>
        BrakingPercentage < 50 ? "#ef4444" : "#22c55e";

    // ── UIC History dialog ────────────────────────────────────────────────────
    [ObservableProperty] private bool _historyDialogVisible = false;

    public ObservableCollection<UicHistoryItemViewModel> UicHistoryItems { get; } = new();
    public bool HistoryIsEmpty => UicHistoryItems.Count == 0;

    // ── Constructor ───────────────────────────────────────────────────────────
    public MainViewModel(ILocoDataProvider locoProvider, string consistFolder)
    {
        _locoRepo    = new LocoRepository(locoProvider);
        _consistRepo = new ConsistRepository(consistFolder);
        _uicHistory  = new UicNameHistory(
            Path.Combine(Path.GetDirectoryName(consistFolder) ?? consistFolder, "uic_history.json"));
        L.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(BrakingPercentageDisplay));
            OnPropertyChanged(nameof(EtcsWarningText));
        };
        UicHistoryItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HistoryIsEmpty));
        LoadLocos();
        RefreshSavedConsists();
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddLocoToConsist()
    {
        if (SelectedLoco is null) return;
        ClearError();

        var fi = ConsistEntries.Count;
        var ft = ConsistEntries.Count + 1;
        var pos = BrakingCalculator.DerivePosition(fi, ft);

        var entry = new ConsistEntry
        {
            DefinitionId          = SelectedLoco.Id,
            Designation           = SelectedLoco.Designation,
            TotalWeightTonnes     = SelectedLoco.TotalWeightTonnes,
            BrakingWeightTonnes   = SelectedLoco.BrakingWeightTonnes,
            BrakingWeightTonnesR  = SelectedLoco.BrakingWeightTonnesR,
            BrakingWeightWithEDB  = SelectedLoco.BrakingWeightWithEDB,
            BrakingWeightWithEDBR = SelectedLoco.BrakingWeightWithEDBR,
            LengthM               = SelectedLoco.LengthM,
            MaxSpeed              = SelectedLoco.MaxSpeed,
            FpClass               = SelectedLoco.FpClass,
            AxleLoad              = SelectedLoco.AxleLoad,
            UicFormat            = SelectedLoco.UicFormat,
            UicPrefixes          = SelectedLoco.UicPrefixes,
            UicPrefixOffset      = SelectedLoco.UicPrefixOffset,
            UicValidateCheck     = SelectedLoco.UicValidateCheck,
            UicTypePrefix        = SelectedLoco.UicTypePrefix,
            Position             = pos,
            BrakesEnabled        = BrakingCalculator.DefaultBrakesEnabled(pos),
            EdbActive            = false,
        };

        if (SelectedLoco.HasEDB && pos == ConsistPosition.Front)
        {
            _pendingEntry      = entry;
            _pendingEdbWith    = SelectedLoco.BrakingWeightWithEDB!.Value;
            _pendingEdbWithout = SelectedLoco.BrakingWeightTonnes;
            EdbDialogLocoName  = SelectedLoco.Designation;
            return;
        }

        CommitEntry(entry);
    }

    [RelayCommand]
    private void EdbYes()
    {
        if (_pendingEntry is null) return;
        _pendingEntry.EdbActive = true;
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

    [RelayCommand]
    private void OpenRenameDialog(ConsistEntryViewModel? vm)
    {
        if (vm is null) return;
        var existing = UicFormatter.StripToDigits(vm.CustomName);
        // Pre-fill with type prefix (e.g. "91547" / "92542") when no name has been set yet
        RenameInput = string.IsNullOrEmpty(existing) ? vm.UicTypePrefix : existing;
        RenameTarget = vm;  // Set after RenameInput so SyncRenameTextBox sees the correct value
        UpdateRenameHistorySuggestions();
    }

    [RelayCommand]
    private void ConfirmRename()
    {
        if (RenameTarget is null) return;
        if (string.IsNullOrEmpty(RenameInput))
        {
            RenameTarget.CustomName = null;
        }
        else
        {
            var formatted = UicFormatter.Format(RenameInput, RenameTarget.UicFormat);
            RenameTarget.CustomName = formatted;
            if (UicFormatter.IsComplete(RenameInput))
                _uicHistory.Add(RenameTarget.DefinitionId, RenameInput);
        }
        RenameTarget = null;
        RenameInput  = string.Empty;
        RenameHistorySuggestions.Clear();
        OnPropertyChanged(nameof(RenameHistoryVisible));
    }

    [RelayCommand]
    private void CancelRename()
    {
        RenameTarget = null;
        RenameInput  = string.Empty;
        RenameHistorySuggestions.Clear();
        OnPropertyChanged(nameof(RenameHistoryVisible));
    }

    [RelayCommand]
    private void OpenHistoryDialog()
    {
        RefreshHistoryItems();
        HistoryDialogVisible = true;
    }

    [RelayCommand]
    private void CloseHistoryDialog() => HistoryDialogVisible = false;

    [RelayCommand]
    private void DeleteHistoryItem(UicHistoryItemViewModel? item)
    {
        if (item is null) return;
        _uicHistory.Remove(item.DefinitionId, item.RawDigits);
        UicHistoryItems.Remove(item);
    }

    private void RefreshHistoryItems()
    {
        UicHistoryItems.Clear();
        var defLookup = _locoRepo.GetAll().ToDictionary(l => l.Id);
        foreach (var kv in _uicHistory.GetAll())
        {
            defLookup.TryGetValue(kv.Key, out var def);
            var designation = def?.Designation ?? kv.Key;
            var format      = def?.UicFormat   ?? "A";
            foreach (var digits in kv.Value)
                UicHistoryItems.Add(new UicHistoryItemViewModel(kv.Key, designation, digits, format));
        }
    }

    private void CommitEntry(ConsistEntry entry)
    {
        SpeedOverride = null;
        var vm = new ConsistEntryViewModel(entry);
        vm.PropertyChanged += OnEntryChanged;
        ConsistEntries.Add(vm);
        ReassignPositions();
        Recalculate();
        RefreshConsistName();
        ShowToast(L.StatusAdded(entry.Designation));
        if (ActiveMobileTab == MobileTab.AddLoco)
            ActiveMobileTab = MobileTab.Consist;
    }

    [RelayCommand]
    private void RemoveEntry(ConsistEntryViewModel? vm)
    {
        if (vm is null) return;
        ClearError();
        var name = vm.Designation;
        vm.PropertyChanged -= OnEntryChanged;
        ConsistEntries.Remove(vm);
        SpeedOverride = null;
        ReassignPositions();
        Recalculate();
        RefreshConsistName();
        ShowToast(L.StatusRemoved(name), isError: true);
    }

    [RelayCommand]
    private void MoveUp(ConsistEntryViewModel? vm)
    {
        if (vm is null) return;
        var i = ConsistEntries.IndexOf(vm);
        if (i <= 0) return;
        ConsistEntries.Move(i, i - 1);
        SpeedOverride = null;
        ReassignPositions();
        Recalculate();
        RefreshConsistName();
    }

    [RelayCommand]
    private void MoveDown(ConsistEntryViewModel? vm)
    {
        if (vm is null) return;
        var i = ConsistEntries.IndexOf(vm);
        if (i < 0 || i >= ConsistEntries.Count - 1) return;
        ConsistEntries.Move(i, i + 1);
        SpeedOverride = null;
        ReassignPositions();
        Recalculate();
        RefreshConsistName();
    }

    [RelayCommand]
    private void ToggleBrakes(ConsistEntryViewModel? vm)
    {
        if (vm is null || vm.BrakesLocked) return;
        ClearError();
        vm.BrakesEnabled = !vm.BrakesEnabled;
        Recalculate();
    }

    [RelayCommand]
    private void ToggleEdb(ConsistEntryViewModel? vm)
    {
        if (vm is null || !vm.EdbButtonEnabled) return;
        vm.EdbActive = !vm.EdbActive;
        Recalculate();
    }

    [RelayCommand]
    private void ToggleRMode(ConsistEntryViewModel? vm)
    {
        if (vm is null || !vm.HasRMode) return;
        vm.RModeActive = !vm.RModeActive;
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
        ShowToast(L.StatusSaved(ConsistName));
    }

    [RelayCommand]
    private void LoadConsist(Consist? consist)
    {
        if (consist is null) return;
        ClearError();
        foreach (var vm in ConsistEntries) vm.PropertyChanged -= OnEntryChanged;
        ConsistEntries.Clear();
        var defLookup = _locoRepo.GetAll().ToDictionary(l => l.Id);
        foreach (var entry in consist.Entries)
        {
            // Always use the loco definition's UicFormat/prefixes — handles old saves that predate these fields
            if (defLookup.TryGetValue(entry.DefinitionId, out var def))
            {
                entry.UicFormat             = def.UicFormat;
                entry.UicPrefixes           = def.UicPrefixes;
                entry.UicPrefixOffset       = def.UicPrefixOffset;
                entry.UicValidateCheck      = def.UicValidateCheck;
                entry.UicTypePrefix         = def.UicTypePrefix;
                entry.AxleLoad              = def.AxleLoad;
                entry.BrakingWeightTonnesR  = def.BrakingWeightTonnesR;
                entry.BrakingWeightWithEDBR = def.BrakingWeightWithEDBR;
            }
            var vm = new ConsistEntryViewModel(entry);
            vm.PropertyChanged += OnEntryChanged;
            ConsistEntries.Add(vm);
        }
        SpeedOverride     = null;
        ConsistName       = consist.Name;
        _currentConsistId = consist.Id;
        ReassignPositions();
        Recalculate();
        ShowToast(L.StatusLoaded(consist.Name));
    }

    [RelayCommand]
    private void DeleteConsist(Consist? consist)
    {
        if (consist is null) return;
        _consistRepo.Delete(consist);
        RefreshSavedConsists();
        ShowToast(L.StatusDeleted(consist.Name), isError: true);
    }

    [RelayCommand]
    private void NewConsist()
    {
        ClearError();
        foreach (var vm in ConsistEntries) vm.PropertyChanged -= OnEntryChanged;
        ConsistEntries.Clear();
        ConsistName       = string.Empty;
        SpeedOverride     = null;
        _currentConsistId = Guid.NewGuid().ToString();
        Recalculate();
        OnPropertyChanged(nameof(CanGeneratePdf));
        ShowToast(L.StatusNew);
    }

    [RelayCommand]
    private void SetCzech()   { IsCzech   = true; }
    [RelayCommand]
    private void SetEnglish() { IsEnglish = true; }

    [RelayCommand]
    private void ClearError() => ErrorMessage = string.Empty;

    [RelayCommand]
    private async Task GeneratePdfAsync()
    {
        if (ConsistEntries.Count == 0)
        {
            ErrorMessage = L.ErrorNothingToPrint;
            return;
        }
        if (PdfSaveService is null || PdfGenerator is null)
        {
            ErrorMessage = L.Language == AppLanguage.Czech
                ? "PDF není na tomto zařízení podporováno."
                : "PDF is not supported on this device.";
            return;
        }

        var suggested = $"LocoCalc_{(ConsistName.Length > 0 ? ConsistName : "Souprava")}_{DateTime.Now:yyyyMMdd_HHmmss}{(PdfDarkMode ? "_dark" : "")}.pdf";
        var path = await PdfSaveService.PickSavePathAsync(suggested);
        if (path is null) return;  // user cancelled

        var bytes = PdfGenerator!.Generate(
            ConsistEntries.Select(e => e.ToModel()).ToList(),
            ConsistName.Length > 0 ? ConsistName : "Souprava",
            SpeedOverride ?? EtcsDefSpeed,
            L.Language == AppLanguage.Czech,
            PdfDarkMode,
            StartStation is null ? null : $"{StartStation.Id}  {StartStation.Name}",
            EndStation   is null ? null : $"{EndStation.Id}  {EndStation.Name}");

        await File.WriteAllBytesAsync(path, bytes);
        ShowToast(L.Language == AppLanguage.Czech
            ? $"PDF uloženo: {path}"
            : $"PDF saved: {path}");

        if (AutoOpenPdf)
            PdfSaveService.OpenFile(path);
    }

    [RelayCommand]
    private void ToggleGroup(TractionGroup? group)
    {
        if (group is null) return;
        group.IsExpanded = !group.IsExpanded;
    }

    [RelayCommand]
    private void ClearSpeedOverride()
    {
        SpeedOverride = null;
        NotifyEtcsChanged();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ReassignPositions()
    {
        var total = ConsistEntries.Count;
        for (int i = 0; i < total; i++)
        {
            var vm  = ConsistEntries[i];
            var pos = BrakingCalculator.DerivePosition(i, total);
            if (vm.Position != pos)
                vm.Position = pos;
        }
    }

    private void OnEntryChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ConsistEntryViewModel.BrakesEnabled)
                           or nameof(ConsistEntryViewModel.EdbActive))
            Recalculate();
    }

    private void Recalculate()
    {
        BrakingPercentage = BrakingCalculator.Calculate(ConsistEntries.Select(e => e.ToModel()));
        NotifyStatsChanged();
        NotifyEtcsChanged();
    }

    private void NotifyStatsChanged()
    {
        OnPropertyChanged(nameof(TotalWeightDisplay));
        OnPropertyChanged(nameof(TotalLengthDisplay));
        OnPropertyChanged(nameof(ActiveBrakeWeightDisplay));
        OnPropertyChanged(nameof(CanGeneratePdf));
    }

    private void NotifyEtcsChanged()
    {
        OnPropertyChanged(nameof(EtcsDefSpeed));
        OnPropertyChanged(nameof(EtcsMaxSpeedDisplay));
        OnPropertyChanged(nameof(EtcsFpClass));
        OnPropertyChanged(nameof(EtcsFpMm));
        OnPropertyChanged(nameof(EtcsAxleLoad));
        OnPropertyChanged(nameof(EtcsWarningVisible));
        OnPropertyChanged(nameof(EtcsWarningText));
        OnPropertyChanged(nameof(EtcsBrakingColor));
    }

    private void RefreshConsistName()
    {
        ConsistName = ConsistNameGenerator.Generate(
            ConsistEntries.Select(e => e.ToModel()).ToList(), L.Language);
    }

    private void RefreshSavedConsists()
    {
        SavedConsists.Clear();
        foreach (var c in _consistRepo.GetAll()) SavedConsists.Add(c);
    }

    partial void OnRenameInputChanged(string value) => UpdateRenameHistorySuggestions();

    private void UpdateRenameHistorySuggestions()
    {
        RenameHistorySuggestions.Clear();

        if (RenameTarget is null)
        {
            OnPropertyChanged(nameof(RenameHistoryVisible));
            return;
        }

        var format  = RenameTarget.UicFormat;
        var history = _uicHistory.GetFor(RenameTarget.DefinitionId);

        foreach (var digits in history)
        {
            if (string.IsNullOrEmpty(RenameInput) || digits.StartsWith(RenameInput))
                RenameHistorySuggestions.Add(UicFormatter.Format(digits, format));
        }

        OnPropertyChanged(nameof(RenameHistoryVisible));
    }

    private static readonly (string key, string label, string dot)[] TractionOrder =
    {
        ("dc",     "DC",    "#3b82f6"),
        ("ac",     "AC",    "#a855f7"),
        ("ms",     "MS",    "#f97316"),
        ("diesel", "Diesel","#22c55e"),
    };

    private void LoadLocos()
    {
        _locoRepo.Invalidate();
        AvailableLocos.Clear();
        TractionGroups.Clear();
        LocoFlatList.Clear();

        var q = SearchText?.Trim().ToLower() ?? string.Empty;
        var all = _locoRepo.GetAll()
            .Where(l => string.IsNullOrEmpty(q) || l.Designation.ToLower().Contains(q))
            .ToList();

        foreach (var l in all) AvailableLocos.Add(l);

        foreach (var (key, label, dot) in TractionOrder)
        {
            var locos = all.Where(l => l.Traction == key).ToList();
            if (locos.Count == 0) continue;
            var fullLabel = L.TractionLabel(key);

            // TractionGroups (kept for backwards compat)
            var group = new TractionGroup(key, fullLabel, dot);
            foreach (var l in locos) group.Locos.Add(l);
            TractionGroups.Add(group);

            // Flat list: header item then loco items — both reference the same TractionGroup
            LocoFlatList.Add(new LocoListItem(group));
            foreach (var l in locos) LocoFlatList.Add(new LocoListItem(group, l));
        }
    }

    private void RefreshLocos() => LoadLocos();

    private void RefreshAll()
    {
        RefreshLocos();   // rebuilds TractionGroups with new language labels
        RefreshConsistName();
        NotifyEtcsChanged();
        NotifyStatsChanged();
        OnPropertyChanged(nameof(BrakingPercentageDisplay));
    }
}
