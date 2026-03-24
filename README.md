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
├── ViewModels/       ← MVVM (MainViewModel)
├── Views/            ← Avalonia XAML
└── Services/         ← BrakingCalculator, PDF, Localization, Theme

LocoCalc.Desktop/     ← Desktop entry point (Windows / Linux)
LocoCalc.Android/     ← Android entry point
```

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

**FP class rule:** consist is FP3 only when *all* locos are FP3. Cant deficiency 130 mm for FP3, 100 mm for FP2.

## Consist Positions & Brake Defaults

| Position | Default brakes | Editable |
|----------|---------------|----------|
| Front    | ON            | Yes      |
| Middle   | OFF           | Yes      |
| Rear     | ON            | **Locked — always ON** |

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

**Publish self-contained:**
```bash
dotnet publish LocoCalc.Desktop/LocoCalc.Desktop.csproj \
  -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -o ./publish
```

### Android

```bash
dotnet build LocoCalc.Android/LocoCalc.Android.csproj -c Release
```

Or build and deploy from Rider/Visual Studio with an Android device or emulator connected.

Saved consists are stored in the app's private files directory (`Context.FilesDir/Consists/`).
