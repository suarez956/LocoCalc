# LocoCalc — Locomotive Braking Weight Calculator

A .NET 8 Avalonia desktop app for calculating braking weight percentages across a locomotive consist.

## Formula

```
Braking % = (Sum of BrakingWeight of locos with brakes ON) / (Sum of TotalWeight of ALL locos) × 100
```

## Project Structure

```
LocoCalc/
├── Data/
│   ├── Locos/          ← One JSON file per locomotive type
│   └── Consists/       ← Auto-created; saved consist files live here
├── Models/             ← Domain models (LocomotiveDefinition, ConsistEntry, Consist)
├── ViewModels/         ← MVVM ViewModels (MainViewModel, ConsistEntryViewModel)
├── Views/              ← Avalonia XAML views
├── Services/           ← LocoRepository, ConsistRepository, BrakingCalculator
└── Converters/         ← Value converters for XAML bindings
```

## Adding a New Locomotive

Create a JSON file in `Data/Locos/` (alongside the exe after build, or in the project folder):

```json
{
  "Id": "unique-id",
  "Designation": "BR 120",
  "TotalWeightTonnes": 83.0,
  "BrakingWeightTonnes": 104.0,
  "DefaultPosition": "Front"
}
```

**DefaultPosition** options:
| Value    | Auto brake state | Editable? |
|----------|-----------------|-----------|
| `Front`  | Enabled (ON)    | Yes       |
| `Middle` | Disabled (OFF)  | Yes       |
| `Rear`   | Enabled (ON)    | **Locked — always ON** |

Click **↻ Reload from disk** in the app to pick up new files without restarting.

## Building & Running

```bash
# Prerequisites: .NET 8 SDK
dotnet restore
dotnet run --project LocoCalc/LocoCalc.csproj
```

Or open `LocoCalc.sln` in Visual Studio / Rider and press F5.

## Publishing (Windows self-contained)

```bash
dotnet publish LocoCalc/LocoCalc.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o ./publish
```

The `Data/Locos/` folder will be alongside the exe — add or edit loco JSON files there.
