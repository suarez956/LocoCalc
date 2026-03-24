using CommunityToolkit.Mvvm.ComponentModel;

namespace LocoCalcAvalonia.Services;

public enum AppLanguage { Czech, English }

public partial class LocalizationService : ObservableObject
{
    public static readonly LocalizationService Instance = new();

    [ObservableProperty]
    private AppLanguage _language = AppLanguage.Czech;

    partial void OnLanguageChanged(AppLanguage value) => OnPropertyChanged(string.Empty);

    private string T(string cs, string en) =>
        Language == AppLanguage.Czech ? cs : en;

    public string AppSubtitle        => T("Kalkulátor brzdících procent", "Braking Percentage Calculator");
    public string BrakingWeightLabel => T("Brzdící procenta:", "Braking percent:");
    public string PanelLocos         => T("LOKOMOTIVY", "LOCOMOTIVES");
    public string ReloadLocos        => T("↻  Znovu načíst", "↻  Reload");
    public string AddToConsist       => T("＋  Přidat do soupravy", "＋  Add to Consist");
    public string SearchPlaceholder  => T("Hledat…", "Search…");
    public string ConsistNamePh      => T("Název soupravy…", "Consist name…");
    public string SaveButton         => T("💾 Uložit", "💾 Save");
    public string NewButton          => T("＋ Nová", "＋ New");
    public string ConsistOverview    => T("PŘEHLED SOUPRAVY", "CONSIST OVERVIEW");
    public string StatLocos          => T("LOKOMOTIVY", "LOCOS");
    public string StatTotalWeight    => T("CELK. HMOT.", "TOTAL WT");
    public string StatLength         => T("DÉLKA", "LENGTH");
    public string StatActiveBrake    => T("AKT. BRZ. HMOT.", "ACTIVE BRAKE WT");
    public string StatBrakingPct     => T("BRZDÍCÍ %", "BRAKING %");
    public string SavedConsists      => T("ULOŽENÉ SOUPRAVY", "SAVED CONSISTS");
    public string EtcsData           => T("ETCS DATA", "ETCS DATA");
    public string LoadButton         => T("Načíst", "Load");
    public string PosFront           => T("Čelo", "Front");
    public string PosMiddle          => T("Střed", "Middle");
    public string PosRear            => T("Konec", "Rear");
    public string BrakesOn           => T("ZAP ✓", "ON ✓");
    public string BrakesOff          => T("VYP ✗", "OFF ✗");
    public string EmptyConsist       => T("Přidejte lokomotivy z levého panelu.", "Add locos from the left panel.");
    public string EmptyConsistMobile  => T("Přidejte lokomotivy z dolního panelu.", "Add locos from the bottom panel.");
    public string NoSaved            => T("Žádné uložené soupravy.", "No saved consists yet.");
    public string SpeedLabel         => T("Rychlost dle soupravy:", "Consist speed:");
    public string SpeedOverride      => T("Přepsat rychlost:", "Override speed:");
    public string CrossSection       => T("Průjezdný průřez", "Cross-section");
    public string CantDeficiency     => T("Nedostatek převýšení", "Cant deficiency");
    public string EtcsMaxSpeed       => T("Max. rychlost ETCS", "ETCS max speed");
    public string BrakingPctLabel    => T("Brzdící procenta", "Braking percentage");
    public string TrainLength        => T("Délka vlaku", "Train length");
    public string EtcsParameters     => T("ETCS parametry", "ETCS parameters");
    public string ConsistComposition => T("Složení soupravy", "Consist composition");
    public string TotalRow           => T("Celkem", "Total");
    public string SpeedSection       => T("RYCHLOST", "SPEED");
    // Mobile nav
    public string NavConsist      => T("Souprava", "Consist");
    public string NavAddLoco      => T("Přidat", "Add Loco");
    public string NavEtcs         => "ETCS";
    public string NavMenu         => T("Menu", "Menu");
    // Font Awesome 6 Free Solid codepoints
    public string NavConsistEmoji => "\uf238";  // fa-train
    public string NavAddLocoEmoji => "\uf067\uf238";  // fa-plus fa-train
    public string NavEtcsEmoji    => "\uf108";         // fa-desktop
    public string NavMenuEmoji    => "\uf0c9";         // fa-bars
    public string MenuConsistInfo => T("Info soupravy", "Consist Info");
    public string MenuSaved       => T("Uložené soupravy", "Saved Consists");
    public string MenuNewConsist  => T("Nová souprava", "New Consist");

    public string PrintReport        => T("Tisknout / PDF", "Print / PDF");
    public string PrintReportIcon    => "\uf02f";  // fa-print
    public string PdfDarkMode        => T("Tmavý", "Dark");
    public string PdfDarkModeIcon    => "\uf186";  // fa-moon
    public string PdfLightMode       => T("Světlý", "Light");
    public string PdfLightModeIcon   => "\uf185";  // fa-sun
    public string AutoOpenPdf         => T("Automaticky otevřít PDF", "Auto-open PDF");
    public string ErrorNothingToPrint => T("Souprava je prázdná — přidejte lokomotivy před generováním PDF.",
                                           "Consist is empty — add locomotives before generating a PDF.");
    public string LightDarkToggle    => T("\uf186 Tmavý report", "\uf186 Dark report");

    public string TrDc      => T("Stejnosměrné", "DC Electric");
    public string TrAc      => T("Střídavé", "AC Electric");
    public string TrMs      => T("Vícesystémové", "Multi-System");
    public string TrDiesel  => T("Motorové", "Diesel");

    public string EdbDialogTitle => T("Elektromagnetická brzda (EDB)", "Electrodynamic Brake (EDB)");
    public string EdbDialogBody(string name, double withEdb, double without) =>
        T($"Lokomotiva {name} má elektromagnetickou brzdu.\nJako vedoucí vozidlo použít zvýšenou brzdovou váhu?\n\nS EDB: {withEdb} t  ·  Bez EDB: {without} t",
          $"Locomotive {name} has an electrodynamic brake.\nUse the higher braking weight as lead loco?\n\nWith EDB: {withEdb} t  ·  Without EDB: {without} t");
    public string EdbYes => T("Ano — s EDB", "Yes — with EDB");
    public string EdbNo  => T("Ne — bez EDB", "No — without EDB");

    public string WarnLowBrake(double pct) =>
        T($"⚠️ Brzdící procenta {pct:F1}% jsou pod minimem 50%.\nAktivujte brzdy dalších lokomotiv nebo přidejte více HV.\n🚫 Vlak nesmí jet na traťové úseky vybavené pouze ETCS!",
          $"⚠️ Braking percentage {pct:F1}% is below the 50% minimum.\nEnable more loco brakes or add more locos.\n🚫 Train must not proceed onto ETCS-only track sections!");

    public string ErrorLastBrake(string name) =>
        T($"Lokomotiva {name} nemůže být deaktivována — je poslední s aktivními brzdami.",
          $"Locomotive {name} cannot be disabled — it is the last loco with active brakes.");

    public string StatusAdded(string name)   => T($"Přidáno: {name}", $"Added {name}");
    public string StatusRemoved(string name) => T($"Odebráno: {name}", $"Removed {name}");
    public string StatusSaved(string name)   => T($"Uloženo „{name}", $"Saved \"{name}\"");
    public string StatusLoaded(string name)  => T($"Načteno „{name}", $"Loaded \"{name}\"");
    public string StatusDeleted(string name) => T($"Smazáno „{name}", $"Deleted \"{name}\"");
    public string StatusNew                  => T("Nová souprava", "New consist");

    public string LastSuffix => T(" posl.", " lst.");
    public string CountSep   => "× ";

    public string TractionLabel(string t) => t switch
    {
        "dc"     => TrDc,
        "ac"     => TrAc,
        "ms"     => TrMs,
        "diesel" => TrDiesel,
        _        => t
    };
}
