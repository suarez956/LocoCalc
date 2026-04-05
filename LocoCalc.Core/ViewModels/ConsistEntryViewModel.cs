using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using LocoCalcAvalonia.Models;
using LocoCalcAvalonia.Services;

namespace LocoCalcAvalonia.ViewModels;

public partial class ConsistEntryViewModel : ObservableObject
{
    public string DefinitionId { get; }
    public string Designation { get; }
    public double TotalWeightTonnes { get; }
    public double BrakingWeightTonnes { get; }
    public double? BrakingWeightTonnesR { get; }
    public double? BrakingWeightWithEDB { get; }
    public double? BrakingWeightWithEDBR { get; }
    public double LengthM { get; }
    public int MaxSpeed { get; }
    public string FpClass { get; }
    public string? AxleLoad { get; }
    public string UicFormat { get; }
    public IReadOnlyList<string>? UicPrefixes { get; }
    public int UicPrefixOffset { get; }
    public bool UicValidateCheck { get; }
    public string UicTypePrefix { get; }
    public int? PomerCislo30 { get; }
    public int? PomerCislo50 { get; }
    public bool HasEDB        => BrakingWeightWithEDB.HasValue;
    public bool HasRMode      => BrakingWeightTonnesR.HasValue;
    public bool HasPomerCislo => PomerCislo30.HasValue;
    public Bitmap? LocoImage { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BrakesLocked))]
    [NotifyPropertyChangedFor(nameof(PositionDisplay))]
    [NotifyPropertyChangedFor(nameof(PositionBadgeColor))]
    private ConsistPosition _position;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BrakesButtonText))]
    [NotifyPropertyChangedFor(nameof(BrakesOnColor))]
    [NotifyPropertyChangedFor(nameof(ActiveBrakeDisplay))]
    [NotifyPropertyChangedFor(nameof(EntryBorderColor))]
    [NotifyPropertyChangedFor(nameof(EdbButtonEnabled))]
    private bool _brakesEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveBrake))]
    [NotifyPropertyChangedFor(nameof(ActiveBrakeDisplay))]
    [NotifyPropertyChangedFor(nameof(EdbButtonColor))]
    [NotifyPropertyChangedFor(nameof(EdbButtonText))]
    private bool _edbActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveBrake))]
    [NotifyPropertyChangedFor(nameof(ActiveBrakeDisplay))]
    [NotifyPropertyChangedFor(nameof(EdbButtonEnabled))]
    [NotifyPropertyChangedFor(nameof(RModeButtonText))]
    [NotifyPropertyChangedFor(nameof(RModeButtonColor))]
    private bool _rModeActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    [NotifyPropertyChangedFor(nameof(HasCustomName))]
    private string? _customName;

    public string DisplayName     => CustomName ?? Designation;
    public bool   HasCustomName   => !string.IsNullOrWhiteSpace(CustomName);
    public bool EdbButtonEnabled => BrakesEnabled &&
        (RModeActive ? BrakingWeightWithEDBR.HasValue : BrakingWeightWithEDB.HasValue);

    public bool BrakesLocked => Position == ConsistPosition.Rear || Position == ConsistPosition.Front;

    public double ActiveBrake
    {
        get
        {
            if (RModeActive && BrakingWeightTonnesR.HasValue)
                return (EdbActive && BrakingWeightWithEDBR.HasValue)
                    ? BrakingWeightWithEDBR!.Value
                    : BrakingWeightTonnesR!.Value;
            return (EdbActive && BrakingWeightWithEDB.HasValue)
                ? BrakingWeightWithEDB!.Value
                : BrakingWeightTonnes;
        }
    }

    public string RModeButtonText  => RModeActive ? "R" : "P";
    public string RModeButtonColor => RModeActive ? "#3b82f6" : "#252540";

    public string ActiveBrakeDisplay =>
        BrakesEnabled ? $"{ActiveBrake:F0} t" : "0 t";

    public string PositionDisplay
    {
        get
        {
            var L = LocalizationService.Instance;
            return Position switch
            {
                ConsistPosition.Front  => L.PosFront,
                ConsistPosition.Middle => L.PosMiddle,
                ConsistPosition.Rear   => L.PosRear,
                _                      => Position.ToString()
            };
        }
    }

    public string BrakesButtonText =>
        BrakesEnabled
            ? LocalizationService.Instance.BrakesOn
            : LocalizationService.Instance.BrakesOff;

    // Colors
    public string BrakesOnColor    => BrakesEnabled ? "#22c55e" : "#ef4444";
    public string EntryBorderColor => BrakesEnabled ? "#22c55e" : "#ef4444";
    public string PositionBadgeColor => Position switch
    {
        ConsistPosition.Front  => "#f97316",
        ConsistPosition.Rear   => "#3b82f6",
        _                      => "#252540"
    };
    public string EdbButtonColor => EdbActive ? "#f97316" : "#252540";
    public string EdbButtonText  => EdbActive ? "EDB ✓" : "EDB ✗";

    public ConsistEntryViewModel(ConsistEntry entry)
    {
        DefinitionId          = entry.DefinitionId;
        Designation           = entry.Designation;
        TotalWeightTonnes     = entry.TotalWeightTonnes;
        BrakingWeightTonnes   = entry.BrakingWeightTonnes;
        BrakingWeightTonnesR  = entry.BrakingWeightTonnesR;
        BrakingWeightWithEDB  = entry.BrakingWeightWithEDB;
        BrakingWeightWithEDBR = entry.BrakingWeightWithEDBR;
        LengthM               = entry.LengthM;
        MaxSpeed              = entry.MaxSpeed;
        FpClass               = entry.FpClass;
        AxleLoad              = entry.AxleLoad;
        UicFormat             = entry.UicFormat;
        UicPrefixes           = entry.UicPrefixes;
        UicPrefixOffset       = entry.UicPrefixOffset;
        UicValidateCheck      = entry.UicValidateCheck;
        UicTypePrefix         = entry.UicTypePrefix;
        PomerCislo30          = entry.PomerCislo30;
        PomerCislo50          = entry.PomerCislo50;
        _position             = entry.Position;
        // Front and Rear positions always have brakes enabled (locked)
        _brakesEnabled        = _position != ConsistPosition.Middle || entry.BrakesEnabled;
        _edbActive            = entry.EdbActive;
        _rModeActive          = entry.RModeActive;
        _customName           = entry.CustomName;

        LocoImage = LoadLocoImage(DefinitionId);

        LocalizationService.Instance.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(PositionDisplay));
            OnPropertyChanged(nameof(BrakesButtonText));
        };
    }

    private static Bitmap? LoadLocoImage(string id)
    {
        var filename = id.Replace('_', '.');
        var uri = new Uri($"avares://LocoCalc.Core/Assets/LocoImages/{filename}.webp");
        try
        {
            using var stream = AssetLoader.Open(uri);
            return new Bitmap(stream);
        }
        catch
        {
            try
            {
                using var fallback = AssetLoader.Open(
                    new Uri("avares://LocoCalc.Core/Assets/LocoImages/UFO.webp"));
                return new Bitmap(fallback);
            }
            catch
            {
                return null;
            }
        }
    }

    partial void OnPositionChanged(ConsistPosition value)
    {
        BrakesEnabled = BrakingCalculator.DefaultBrakesEnabled(value);
        if (value != ConsistPosition.Front)
            EdbActive = false;
    }

    partial void OnRModeActiveChanged(bool value)
    {
        // Disable EDB if the new mode has no EDB value
        if (value && !BrakingWeightWithEDBR.HasValue)
            EdbActive = false;
        else if (!value && !BrakingWeightWithEDB.HasValue)
            EdbActive = false;
    }

    public ConsistEntry ToModel() => new()
    {
        DefinitionId          = DefinitionId,
        Designation           = Designation,
        TotalWeightTonnes     = TotalWeightTonnes,
        BrakingWeightTonnes   = BrakingWeightTonnes,
        BrakingWeightTonnesR  = BrakingWeightTonnesR,
        BrakingWeightWithEDB  = BrakingWeightWithEDB,
        BrakingWeightWithEDBR = BrakingWeightWithEDBR,
        LengthM               = LengthM,
        MaxSpeed              = MaxSpeed,
        FpClass               = FpClass,
        AxleLoad              = AxleLoad,
        UicFormat             = UicFormat,
        UicPrefixes           = UicPrefixes?.ToList(),
        UicPrefixOffset       = UicPrefixOffset,
        UicValidateCheck      = UicValidateCheck,
        UicTypePrefix         = UicTypePrefix,
        Position              = Position,
        BrakesEnabled         = BrakesEnabled,
        EdbActive             = EdbActive,
        RModeActive           = RModeActive,
        CustomName            = CustomName,
        PomerCislo30          = PomerCislo30,
        PomerCislo50          = PomerCislo50,
    };
}
