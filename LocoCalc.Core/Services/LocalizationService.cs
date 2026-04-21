using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LocoCalc.Services;

public enum AppLanguage { Czech, English }

internal record LocalizedEntry(
    [property: JsonPropertyName("cs")] string Cs,
    [property: JsonPropertyName("en")] string En);

public partial class LocalizationService : ObservableObject
{
    public static readonly LocalizationService Instance = new();

    [ObservableProperty]
    private AppLanguage _language = AppLanguage.Czech;

    partial void OnLanguageChanged(AppLanguage value) => OnPropertyChanged(string.Empty);

    private readonly Dictionary<string, LocalizedEntry> _strings;

    private LocalizationService()
    {
        var asm = typeof(LocalizationService).Assembly;
        var name = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("strings.json", StringComparison.OrdinalIgnoreCase));

        if (name is null) { _strings = []; return; }

        using var stream = asm.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        _strings = JsonSerializer.Deserialize<Dictionary<string, LocalizedEntry>>(
            reader.ReadToEnd(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? [];
    }

    private string T(string key)
    {
        if (_strings.TryGetValue(key, out var e))
            return Language == AppLanguage.Czech ? e.Cs : e.En;
        return key;
    }

    public string AppSubtitle        => T("AppSubtitle");
    public string BrakingWeightLabel => T("BrakingWeightLabel");
    public string PanelLocos         => T("PanelLocos");
    public string AddToConsist       => T("AddToConsist");
    public string SearchPlaceholder  => T("SearchPlaceholder");
    public string ConsistNamePh      => T("ConsistNamePh");
    public string SaveButton         => T("SaveButton");
    public string NewButton          => T("NewButton");
    public string ConsistOverview    => T("ConsistOverview");
    public string StatLocos          => T("StatLocos");
    public string StatTotalWeight    => T("StatTotalWeight");
    public string StatLength         => T("StatLength");
    public string StatActiveBrake    => T("StatActiveBrake");
    public string StatBrakingPct     => T("StatBrakingPct");
    public string SavedConsists      => T("SavedConsists");
    public string EtcsData           => T("EtcsData");
    public string LoadButton         => T("LoadButton");
    public string PosFront           => T("PosFront");
    public string PosMiddle          => T("PosMiddle");
    public string PosRear            => T("PosRear");
    public string BrakesOn           => T("BrakesOn");
    public string BrakesOff          => T("BrakesOff");
    public string EmptyConsist       => T("EmptyConsist");
    public string EmptyConsistMobile => T("EmptyConsistMobile");
    public string NoSaved            => T("NoSaved");
    public string SpeedLabel         => T("SpeedLabel");
    public string SpeedOverride      => T("SpeedOverride");
    public string CrossSection       => T("CrossSection");
    public string CantDeficiency     => T("CantDeficiency");
    public string EtcsMaxSpeed       => T("EtcsMaxSpeed");
    public string BrakingPctLabel    => T("BrakingPctLabel");
    public string TrainLength        => T("TrainLength");
    public string AxleLoad           => T("AxleLoad");
    public string EtcsParameters     => T("EtcsParameters");
    public string ConsistComposition => T("ConsistComposition");
    public string TotalRow           => T("TotalRow");
    public string SpeedSection       => T("SpeedSection");
    public string TrainParameters    => T("TrainParameters");

    public string NavConsist      => T("NavConsist");
    public string NavAddLoco      => T("NavAddLoco");
    public string NavEtcs         => T("NavEtcs");
    public string NavMenu         => T("NavMenu");
    public string NavConsistEmoji => T("NavConsistEmoji");
    public string NavAddLocoEmoji => T("NavAddLocoEmoji");
    public string NavEtcsEmoji    => T("NavEtcsEmoji");
    public string NavMenuEmoji    => T("NavMenuEmoji");
    public string MenuConsistInfo => T("MenuConsistInfo");
    public string MenuSaved       => T("MenuSaved");

    public string PrintReport        => T("PrintReport");
    public string PrintReportIcon    => T("PrintReportIcon");
    public string AutoOpenPdf        => T("AutoOpenPdf");
    public string ErrorNothingToPrint => T("ErrorNothingToPrint");
    public string ConsistTotalWeight => T("ConsistTotalWeight");
    public string ConsistBrakingPct => T("ConsistBrakingPct");

    public string TrDc     => T("TrDc");
    public string TrAc     => T("TrAc");
    public string TrMs     => T("TrMs");
    public string TrDiesel => T("TrDiesel");

    public string EdbDialogTitle => T("EdbDialogTitle");
    public string EdbDialogBody(string name, double withEdb, double without) =>
        string.Format(T("EdbDialogBody"), name, withEdb, without);
    public string EdbYes => T("EdbYes");
    public string EdbNo  => T("EdbNo");

    public string RenameDialogTitle => T("RenameDialogTitle");
    public string RenameDialogBody  => T("RenameDialogBody");
    public string RenameConfirm     => T("RenameConfirm");
    public string RenameCancel      => T("RenameCancel");
    public string RenameUicMismatch => T("RenameUicMismatch");
    public string RenameCheckDigitWarning(int expected) => string.Format(T("RenameCheckDigitWarn"), expected);
    public string HistoryButton      => T("HistoryButton");
    public string HistoryDialogTitle => T("HistoryDialogTitle");
    public string HistoryEmpty       => T("HistoryEmpty");
    public string CloseButton        => T("CloseButton");

    public string StartStation   => T("StartStation");
    public string EndStation     => T("EndStation");
    public string StartStationPh => T("StartStationPh");
    public string EndStationPh   => T("EndStationPh");
    public string PdfRoute       => T("PdfRoute");

    public string TwrButton      => T("TwrButton");
    public string TwrWeightLabel => T("TwrWeightLabel");
    public string TwrWeightPh    => T("TwrWeightPh");
    public string TwrNoLocos     => T("TwrNoLocos");
    public string TwrCalculate   => T("TwrCalculate");
    public string TwrSumTitle    => T("TwrSumTitle");
    public string TwrSumValue(int sum) => string.Format(T("TwrSumValue"), sum);
    public string TwrRowFormat(int twr) => string.Format(T("TwrRowFormat"), twr);
    public string TwrTable30     => T("TwrTable30");
    public string TwrTable50     => T("TwrTable50");

    public string WarnLowBrake(double pct) => string.Format(T("WarnLowBrake"), pct);
    public string WarnLowBrakeSub          => T("WarnLowBrakeSub");

    public string StatusAdded(string name)   => string.Format(T("StatusAdded"), name);
    public string StatusRemoved(string name) => string.Format(T("StatusRemoved"), name);
    public string StatusSaved(string name)   => string.Format(T("StatusSaved"), name);
    public string StatusLoaded(string name)  => string.Format(T("StatusLoaded"), name);
    public string StatusDeleted(string name) => string.Format(T("StatusDeleted"), name);
    public string StatusNew                  => T("StatusNew");

    /// <summary>Gets a string by key with an explicit language flag (for PDF generators).</summary>
    public static string GetString(string key, bool cs)
    {
        if (Instance._strings.TryGetValue(key, out var e))
            return cs ? e.Cs : e.En;
        return key;
    }

    public string TractionLabel(string t) => t switch
    {
        "dc"     => TrDc,
        "ac"     => TrAc,
        "ms"     => TrMs,
        "diesel" => TrDiesel,
        _        => t
    };
}
