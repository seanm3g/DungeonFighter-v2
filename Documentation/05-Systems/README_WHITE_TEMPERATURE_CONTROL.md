# White Temperature Control

## Overview

The `WhiteTemperatureIntensity` parameter in `UIConfiguration.json` controls how warm (yellowish) or cool (bluish) the white colors appear in gradients throughout the game, particularly in dungeon depth progression.

## Parameter Location

**File:** `GameData/UIConfiguration.json`

```json
{
  ...
  "WhiteTemperatureIntensity": 1.0
}
```

## How It Works

This single parameter adjusts both endpoints of the warm-to-cool white gradient:

### Intensity Values

| Value | Warm White (Start) | Cool White (End) | Effect |
|-------|-------------------|------------------|---------|
| `0.0` | RGB(255, 255, 255) | RGB(255, 255, 255) | **No temperature shift** - pure white throughout |
| `0.5` | RGB(255, 252, 237) | RGB(237, 242, 255) | **Subtle** - barely noticeable tint |
| `1.0` | RGB(255, 250, 220) | RGB(220, 230, 255) | **Default** - standard yellowish to bluish |
| `1.5` | RGB(255, 247, 202) | RGB(202, 217, 255) | **Moderate** - noticeable temperature shift |
| `2.0` | RGB(255, 245, 185) | RGB(185, 205, 255) | **Strong** - pronounced warm/cool effect |
| `3.0` | RGB(255, 240, 150) | RGB(150, 180, 255) | **Extreme** - very dramatic temperature shift |

### Gradient Behavior

The gradient interpolates smoothly between warm and cool white as you progress through dungeons:
- **Early rooms** → Warm white (yellowish) - creates a torch-lit atmosphere
- **Mid dungeon** → Neutral white - balanced lighting
- **Deep rooms** → Cool white (bluish) - creates an eerie, cold atmosphere

## Usage Examples

### Minimal Temperature Shift
For a cleaner look with subtle atmospheric changes:
```json
"WhiteTemperatureIntensity": 0.3
```

### Standard (Default)
Balanced atmospheric effect:
```json
"WhiteTemperatureIntensity": 1.0
```

### Dramatic Atmosphere
For a more pronounced atmospheric progression:
```json
"WhiteTemperatureIntensity": 2.5
```

### No Temperature Shift
Pure white throughout (disables atmospheric temperature effect):
```json
"WhiteTemperatureIntensity": 0.0
```

## Technical Details

### Color Calculation

**Warm White Formula:**
- Red: 255 (always maximum)
- Green: 255 - (5 × intensity)
- Blue: 255 - (35 × intensity)

**Cool White Formula:**
- Red: 255 - (35 × intensity)
- Green: 255 - (25 × intensity)
- Blue: 255 (always maximum)

### Where It's Applied

The temperature system affects:
- **Main Menu**: Gradient from warm to cool across menu options
  - "New Game" → Warm white
  - "Load Game" → Neutral-warm white (33% progression)
  - "Settings" → Neutral-cool white (67% progression)
  - "Quit" → Cool white (slightly dimmed)
- **Dungeon depth progression**: Text color shifts as you progress through dungeons
- **Direct white color requests**: Any code using `GetWhite()` or `GetWhiteByDepth()`
- **UI elements**: Any atmospheric white coloring throughout the game

## Tips

1. **Start with default (1.0)** and adjust incrementally by ±0.25
2. **Use 0.0** if you want consistent white throughout the game
3. **Higher values (2.0+)** create a more stylized, atmospheric experience
4. **Lower values (0.3-0.7)** provide subtle atmospheric cues without being distracting
5. The effect is most noticeable in dungeon exploration and narrative text

## Related Files

- **Configuration:** `GameData/UIConfiguration.json`
- **Implementation:** `Code/UI/ColorLayerSystem.cs`
- **Config Class:** `Code/UI/UIConfiguration.cs`

