# White Temperature System Implementation

## Overview

Implemented a configurable white temperature system that allows adjusting the intensity of warm-to-cool white gradients throughout the game via a single parameter.

## Changes Made

### 1. Configuration Parameter Added

**File:** `GameData/UIConfiguration.json`
```json
"WhiteTemperatureIntensity": 1.0
```

**File:** `Code/UI/UIConfiguration.cs`
- Added `WhiteTemperatureIntensity` property (double, default: 1.0)
- Documentation explains the gradient behavior

### 2. Color System Updated

**File:** `Code/UI/ColorLayerSystem.cs`

Added method:
- `GetTemperatureIntensity()` - Reads intensity from UIConfiguration

Updated methods:
- `GetWhite()` - Now calculates warm/cool colors based on intensity
- `InterpolateWhiteTemperature()` - Interpolates between intensity-adjusted endpoints

### 3. Main Menu Integration

**File:** `Code/UI/Avalonia/CanvasUIManager.cs`

**Before:**
```csharp
var menuConfig = new[]
{
    (1, "New Game", Color.FromRgb(255, 250, 235)),    // Hardcoded
    (2, loadGameText, Color.FromRgb(255, 255, 250)),  // Hardcoded
    (3, "Settings", Color.FromRgb(240, 245, 255)),    // Hardcoded
    (0, "Quit", Color.FromRgb(210, 225, 240))         // Hardcoded
};
```

**After:**
```csharp
var menuConfig = new[]
{
    (1, "New Game", ColorLayerSystem.GetWhite(WhiteTemperature.Warm, 1.0f).ToAvaloniaColor()),
    (2, loadGameText, ColorLayerSystem.GetWhiteByDepth(2, 4, 1.0f).ToAvaloniaColor()),
    (3, "Settings", ColorLayerSystem.GetWhiteByDepth(3, 4, 1.0f).ToAvaloniaColor()),
    (0, "Quit", ColorLayerSystem.GetWhite(WhiteTemperature.Cool, 0.92f).ToAvaloniaColor())
};
```

The main menu now respects the `WhiteTemperatureIntensity` parameter!

### 4. Documentation Created

**File:** `GameData/README_WHITE_TEMPERATURE_CONTROL.md`
- Complete guide on how to use the parameter
- Table of intensity values and their effects
- Usage examples
- Technical details about color calculation

## How the System Works

### Intensity Scale

| Intensity | Description | Effect |
|-----------|-------------|--------|
| 0.0 | No shift | Pure white (255, 255, 255) throughout |
| 0.5 | Subtle | Barely noticeable warm/cool tint |
| 1.0 | Default | Standard yellowish to bluish progression |
| 2.0 | Strong | Pronounced warm/cool effect |
| 3.0+ | Extreme | Very dramatic temperature shift |

### Color Formulas

**Warm White (early dungeon, "New Game" option):**
- Red: 255 (always maximum)
- Green: 255 - (5 × intensity)
- Blue: 255 - (35 × intensity)

At intensity=1.0: RGB(255, 250, 220) - yellowish tint

**Cool White (deep dungeon, "Quit" option):**
- Red: 255 - (35 × intensity)
- Green: 255 - (25 × intensity)
- Blue: 255 (always maximum)

At intensity=1.0: RGB(220, 230, 255) - bluish tint

### Gradient Interpolation

The system smoothly interpolates between warm and cool endpoints:
- **Main Menu**: 4 steps from warm to cool
- **Dungeons**: Based on room number / total rooms ratio
- **Progression**: 0.0 (warm) → 1.0 (cool)

## Benefits

1. **Single Parameter Control**: Adjust all white gradients with one value
2. **Consistency**: Main menu and dungeon progression use the same system
3. **Flexibility**: Range from no effect (0.0) to extreme (3.0+)
4. **Atmospheric**: Creates visual progression through dungeons
5. **Configurable**: Easy to adjust without code changes

## Usage

To adjust the intensity, simply edit `GameData/UIConfiguration.json`:

```json
{
  ...
  "WhiteTemperatureIntensity": 1.5
}
```

The change affects:
- Main menu option colors
- Dungeon depth progression
- Any white text using the ColorLayerSystem

## Testing

To test the system:
1. Set `WhiteTemperatureIntensity` to 0.0 → All whites should be pure white
2. Set to 2.0 → Very noticeable yellow/blue shift
3. Start game and check main menu gradient
4. Enter dungeon and observe text color progression

## Future Enhancements

Possible future additions:
- Per-dungeon intensity overrides
- Custom color temperature presets
- Dynamic intensity based on story events
- Temperature shift based on time of day

