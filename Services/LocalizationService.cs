using CommunityToolkit.Mvvm.ComponentModel;

namespace LocoCalc.Services;

public enum AppLanguage { Czech, English }

/// <summary>
/// Simple two-language string table. Bind UI text to properties on this singleton.
/// </summary>
public partial class LocalizationService : ObservableObject
{
    public static readonly LocalizationService Instance = new();

    [ObservableProperty]
    private AppLanguage _language = AppLanguage.Czech;

    partial void OnLanguageChanged(AppLanguage value) => RefreshAll();

    private void RefreshAll()
    {
        // Raise change for every string property so bindings update
        OnPropertyChanged(string.Empty);
    }

    private string T(string cs, string en) =>
        Language == AppLanguage.Czech ? cs : en;

    // ── App shell ─────────────────────────────────────────────────────────────
    public string AppSubtitle         => T("Kalkulátor brzdící hmotnosti", "Braking Weight Calculator");
    public string BrakingWeightLabel  => T("Brzdící hmotnost:", "Braking Weight:");

    // ── Left panel ────────────────────────────────────────────────────────────
    public string PanelLocomotives    => T("LOKOMOTIVY", "LOCOMOTIVES");
    public string ReloadFromDisk      => T("↻  Znovu načíst ze souboru", "↻  Reload from disk");
    public string AddToConsist        => T("＋  Přidat do soupravy", "＋  Add to Consist");
    public string TotalWeightShort    => T("celkem", "total");
    public string BrakeWeightShort    => T("brzda", "brake");

    // ── Centre panel ─────────────────────────────────────────────────────────
    public string ConsistNamePlaceholder => T("Název soupravy…", "Consist name…");
    public string SaveButton          => T("💾 Uložit", "💾 Save");
    public string NewButton           => T("＋ Nová", "＋ New");

    // ── Entry row ─────────────────────────────────────────────────────────────
    public string PositionLabel       => T("POZICE", "POSITION");
    public string BrakesLabel         => T("BRZDY", "BRAKES");
    public string BrakesOn            => T("ZAP ✓", "ON ✓");
    public string BrakesOff           => T("VYP ✗", "OFF ✗");
    public string PosFront            => T("Čelo", "Front");
    public string PosMiddle           => T("Střed", "Middle");
    public string PosRear             => T("Konec", "Rear");

    // ── Strip ─────────────────────────────────────────────────────────────────
    public string ConsistOverview     => T("PŘEHLED SOUPRAVY", "CONSIST OVERVIEW");

    // ── Summary stats ─────────────────────────────────────────────────────────
    public string StatLocos           => T("LOKOMOTIVY", "LOCOS");
    public string StatTotalWeight     => T("CELK. HMOTNOST", "TOTAL WEIGHT");
    public string StatActiveBrake     => T("AKT. BRZD. HMOT.", "ACTIVE BRAKE WT");
    public string StatBrakingPct      => T("BRZDÍCÍ %", "BRAKING %");

    // ── Right panel ───────────────────────────────────────────────────────────
    public string SavedConsists       => T("ULOŽENÉ SOUPRAVY", "SAVED CONSISTS");
    public string LoadButton          => T("Načíst", "Load");
    public string LocosCount          => T("lok.", "locos");

    // ── Status messages ───────────────────────────────────────────────────────
    public string StatusAdded(string name)   => T($"Přidáno: {name}", $"Added {name}");
    public string StatusRemoved(string name) => T($"Odebráno: {name}", $"Removed {name}");
    public string StatusSaved(string name)   => T($"Uloženo „{name}"", $"Saved \"{name}\"");
    public string StatusLoaded(string name)  => T($"Načteno „{name}"", $"Loaded \"{name}\"");
    public string StatusDeleted(string name) => T($"Smazáno „{name}"", $"Deleted \"{name}\"");
    public string StatusNewConsist           => T("Nová souprava zahájena", "New consist started");

    // ── Error messages ────────────────────────────────────────────────────────
    public string ErrorLastBrakeLoco(string name) =>
        T($"Lokomotiva {name} nemůže být deaktivována — je poslední s aktivními brzdami v soupravě.",
          $"Locomotive {name} cannot be disabled — it is the last loco with active brakes in the consist.");

    // ── Language toggle ───────────────────────────────────────────────────────

    // ── EDB dialog ────────────────────────────────────────────────────────────
    public string EdbDialogTitle          => T("Elektromagnetická brzda", "Electrodynamic Brake");
    public string EdbDialogBody(string name) =>
        T($"Lokomotiva {name} je vybavena elektromagnetickou brzdou (EDB). Použít brzdovou váhu s EDB jako vedoucí vozidlo?",
          $"Locomotive {name} has an electrodynamic brake (EDB). Use the braking weight with EDB as the leading loco?");
    public string EdbYes                  => T("Ano, použít s EDB", "Yes, use with EDB");
    public string EdbNo                   => T("Ne, bez EDB", "No, without EDB");
    public string EdbLabel                => T("EDB", "EDB");

    public string LanguageToggleLabel => T("EN", "CS");
}
