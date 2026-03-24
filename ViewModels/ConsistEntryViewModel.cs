using CommunityToolkit.Mvvm.ComponentModel;
using LocoCalc.Models;
using LocoCalc.Services;

namespace LocoCalc.ViewModels;

public partial class ConsistEntryViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BrakesLocked))]
    [NotifyPropertyChangedFor(nameof(PositionDisplay))]
    private ConsistPosition _position;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BrakesButtonText))]
    private bool _brakesEnabled;

    /// <summary>Whether this loco's EDB is active (only settable when it's the Front/lead loco).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveBrakingWeight))]
    [NotifyPropertyChangedFor(nameof(EdbBadgeVisible))]
    private bool _edbActive;

    public string Designation { get; }
    public double TotalWeightTonnes { get; }
    public double LengthM { get; }
    public double BrakingWeightTonnes { get; }       // without EDB
    public double? BrakingWeightWithEDB { get; }
    public string DefinitionId { get; }

    public bool HasEDB => BrakingWeightWithEDB.HasValue;
    public bool BrakesLocked => Position == ConsistPosition.Rear;

    /// <summary>Current effective braking weight based on EDB state.</summary>
    public double ActiveBrakingWeight =>
        (HasEDB && EdbActive) ? BrakingWeightWithEDB!.Value : BrakingWeightTonnes;

    /// <summary>Show EDB badge in UI when EDB is active.</summary>
    public bool EdbBadgeVisible => HasEDB && EdbActive;

    public string PositionDisplay => Position switch
    {
        ConsistPosition.Front  => LocalizationService.Instance.PosFront,
        ConsistPosition.Middle => LocalizationService.Instance.PosMiddle,
        ConsistPosition.Rear   => LocalizationService.Instance.PosRear,
        _                      => Position.ToString()
    };

    public string BrakesButtonText =>
        BrakesEnabled
            ? LocalizationService.Instance.BrakesOn
            : LocalizationService.Instance.BrakesOff;

    public ConsistEntryViewModel(ConsistEntry entry)
    {
        DefinitionId          = entry.DefinitionId;
        Designation           = entry.Designation;
        TotalWeightTonnes     = entry.TotalWeightTonnes;
        LengthM               = entry.LengthM;
        BrakingWeightTonnes   = entry.BrakingWeightTonnes;
        BrakingWeightWithEDB  = entry.BrakingWeightWithEDB;
        _position             = entry.Position;
        _brakesEnabled        = entry.BrakesEnabled;
        _edbActive            = entry.EdbActive;

        LocalizationService.Instance.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(PositionDisplay));
            OnPropertyChanged(nameof(BrakesButtonText));
        };
    }

    partial void OnPositionChanged(ConsistPosition value)
    {
        BrakesEnabled = BrakingCalculator.DefaultBrakesEnabled(value);
        // EDB only valid at front position
        if (value != ConsistPosition.Front)
            EdbActive = false;
    }

    partial void OnEdbActiveChanged(bool value)
    {
        // Recalculate braking weight notification
        OnPropertyChanged(nameof(ActiveBrakingWeight));
    }

    public ConsistEntry ToModel() => new()
    {
        DefinitionId          = DefinitionId,
        Designation           = Designation,
        TotalWeightTonnes     = TotalWeightTonnes,
        LengthM               = LengthM,
        BrakingWeightTonnes   = BrakingWeightTonnes,
        BrakingWeightWithEDB  = BrakingWeightWithEDB,
        Position              = Position,
        BrakesEnabled         = BrakesEnabled,
        EdbActive             = EdbActive,
    };

    public ConsistEntryViewModel_Snapshot ToSnapshot() =>
        new(Designation, Position);
}
