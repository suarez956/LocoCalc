# LocoCalc — Locomotive Braking Weight Calculator

A cross-platform app (Windows / Linux / Android) for calculating braking weight percentages across a locomotive consist, with ETCS parameter output and PDF report generation.

## Formula

```
Braking % = Σ BrakingWeight (locos with brakes ON) / Σ TotalWeight (ALL locos) × 100
```

## Features

- Build a locomotive consist from a built-in database (~40 Czech loco types)
- Toggle brakes per loco; rear loco brakes are always locked ON
- EDB (electrodynamic brake) prompt for lead locos that support it
- Auto-calculated ETCS parameters: FP class, cant deficiency, max speed, train length
- Save and load named consists
- UIC number entry with per-locomotive history (last known locos)
- PDF braking report — light and dark mode
- Czech / English UI

## Project Structure

```
LocoCalc.Core/
├── Data/
│   ├── Locos/        ← Locomotive definitions (embedded, edit in source)
│   ├── strings.json  ← All UI and PDF strings (CS + EN)
│   └── theme.json    ← PDF color palette (light + dark)
├── Models/           ← Domain models
├── ViewModels/       ← MVVM (MainViewModel, UicHistoryItemViewModel, TractionGroup)
├── Views/            ← Avalonia XAML (MainView)
└── Services/         ← BrakingCalculator, Localization, Theme, UicNameHistory, PlatformInsets

LocoCalc.Desktop/
├── Views/            ← MainWindow (desktop shell)
└── Services/         ← PdfReportService (QuestPDF, desktop-only)

LocoCalc.Android/     ← Android entry point, PDF generator, save service
Directory.Build.props ← Single source of truth for AppVersion
```

## Versioning

The app version is defined in one place — `Directory.Build.props` at the solution root:

```xml
<Project>
  <PropertyGroup>
    <AppVersion>1.1.1</AppVersion>
  </PropertyGroup>
</Project>
```

Both `LocoCalc.Desktop` (`Version`, `FileVersion`) and `LocoCalc.Android` (`ApplicationDisplayVersion`) reference `$(AppVersion)`. To bump a release, edit only this file.

## Locomotive JSON Format

Loco definitions live in `LocoCalc.Core/Data/Locos/` and are **compiled into the app** as embedded resources. To add or change a loco, edit the JSON in source and rebuild.

```json
{
  "Id": "r163",
  "Designation": "Řada 163",
  "TotalWeightTonnes": 84.5,
  "BrakingWeightTonnes": 44.0,
  "BrakingWeightWithEDB": 52.0,
  "LengthM": 17,
  "MaxSpeed": 120,
  "traction": "dc",
  "fp": "FP3"
}
```

| Field                  | Required | Notes                                             |
|------------------------|----------|---------------------------------------------------|
| `Id`                   | ✓        | Unique string identifier                          |
| `Designation`          | ✓        | Display name                                      |
| `TotalWeightTonnes`    | ✓        | Service weight in tonnes                          |
| `BrakingWeightTonnes`  | ✓        | Braking weight in tonnes                          |
| `BrakingWeightWithEDB` |          | If present, EDB prompt shown for lead loco        |
| `LengthM`              | ✓        | Length in metres                                  |
| `MaxSpeed`             |          | km/h — defaults to 80                             |
| `traction`             |          | `dc`, `ac`, `ms`, `diesel` — defaults to `diesel` |
| `fp`                   |          | `FP2` or `FP3` — defaults to `FP2`                |
| `uicFormat`            |          | UIC number display format: `"A"` = `XX XX XXXX XXX-X` (default), `"B"` = `XX XX X XXX XXX-X` |
| `uicPrefixes`          |          | List of allowed digit substrings validated at `uicPrefixOffset`. Empty/omitted means no restriction. Example: `["3630","3632"]` for Řada 363 |
| `uicPrefixOffset`      |          | Zero-based index in the raw 12-digit string where `uicPrefixes` are matched. Defaults to `4` (skips 2-digit type + 2-digit country code) |

**FP class rule:** consist is FP3 only when *all* locos are FP3. Cant deficiency 130 mm for FP3, 100 mm for FP2.

## Consist Positions & Brake Defaults

| Position | Default brakes | Editable |
|----------|----------------|----------|
| Front    | ON             | **Locked — always ON** |
| Middle   | OFF            | Yes      |
| Rear     | ON             | **Locked — always ON** |

## Customising Strings and Colors

Both files are embedded resources — edit in source, then rebuild.

**`Data/strings.json`** — all UI and PDF text, Czech and English side by side:
```json
{
  "AppSubtitle": { "cs": "Kalkulátor brzdících procent", "en": "Braking Percentage Calculator" }
}
```

**`Data/theme.json`** — PDF color palette, separate light and dark sections:
```json
{
  "light": { "bgPage": "#ffffff", "orange": "#f97316", ... },
  "dark":  { "bgPage": "#0f0f1a", "orange": "#f97316", ... }
}
```

## Building

### Desktop (Windows / Linux)

```bash
# Prerequisites: .NET 10 SDK
dotnet restore
dotnet run --project LocoCalc.Desktop/LocoCalc.Desktop.csproj
```

Or open `LocoCalc.sln` in Rider / Visual Studio and run the `LocoCalc.Desktop` project.

### Android

```bash
dotnet build LocoCalc.Android/LocoCalc.Android.csproj -c Release
```

Or build and deploy from Rider/Visual Studio with an Android device or emulator connected.

Saved consists are stored in the app's private files directory (`Context.FilesDir/Consists/`).

Android enforces **portrait-only on phones**; tablets (`sw >= 600dp`) allow all orientations.

## Publishing

Use `publish.sh` to build self-contained release packages:

```bash
# Build all targets (win-x64, linux-x64, android APK)
./publish.sh

# Build specific targets
./publish.sh -r win-x64 -r linux-x64

# Custom output folder
./publish.sh -o /tmp/release

# Combine flags
./publish.sh -r android -o ~/Desktop
```

Output files:
- `LocoCalc-win-x64-<version>.zip`
- `LocoCalc-linux-x64-<version>.zip`
- `LocoCalc-android-<version>.apk`

The version suffix is read from `Directory.Build.props` automatically.
