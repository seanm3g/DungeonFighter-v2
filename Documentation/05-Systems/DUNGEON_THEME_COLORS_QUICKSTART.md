# Dungeon Theme Colors - Quick Start Guide

## Overview

The Dungeon Theme Colors system automatically applies thematic colors to dungeon names throughout the UI. Each of the 24 dungeon themes has a unique color that helps players visually identify dungeon types at a glance.

## Quick Example

```csharp
using RPGGame.UI.Avalonia;

// Get color for a dungeon theme
Color forestColor = DungeonThemeColors.GetThemeColor("Forest");
// Returns: Green RGB(0, 196, 32)

// Use in UI
canvas.AddText(x, y, dungeonName, forestColor);
```

## Theme Color Reference

### Natural Themes
- **Forest** → Vibrant Green
- **Nature** → Dark Green  
- **Swamp** → Murky Green

### Fire/Heat Themes
- **Lava** → Red
- **Volcano** → Orange

### Ice/Cold Themes
- **Ice** → Cyan
- **Mountain** → Silver

### Dark Themes
- **Crypt** → Purple
- **Shadow** → Indigo
- **Void** → Dark Gray
- **Dream** → Medium Purple

### Magical Themes
- **Crystal** → Bright Cyan
- **Arcane** → Blue
- **Astral** → Magenta
- **Dimensional** → Blue Violet
- **Temporal** → Cornflower Blue

### Holy/Divine Themes
- **Temple** → Gold
- **Divine** → Light Yellow

### Weather Themes
- **Storm** → Steel Blue
- **Ocean** → Deep Blue

### Industrial Themes
- **Steampunk** → Bronze
- **Underground** → Brown

### Desert/Arid Themes
- **Desert** → Sandy Beige
- **Ruins** → Stone Gray-Brown

### Default
- **Generic** → Silver

## Common Use Cases

### 1. Dungeon Selection Screen
Automatically applied - dungeons display in their theme colors.

### 2. Dungeon Information
```csharp
var themeColor = DungeonThemeColors.GetThemeColor(dungeon.Theme);
canvas.AddText(x, y, dungeon.Name, themeColor);
```

### 3. Custom Display
```csharp
// Normal color
Color color = DungeonThemeColors.GetThemeColor("Lava");

// Dimmed (for hover effects)
Color dimmed = DungeonThemeColors.GetDimmedThemeColor("Lava");

// Brightened (for highlights)
Color bright = DungeonThemeColors.GetBrightenedThemeColor("Lava");
```

### 4. List All Themes
```csharp
var allThemes = DungeonThemeColors.GetAllThemeColors();
foreach (var theme in allThemes)
{
    Console.WriteLine($"{theme.Key}: RGB({theme.Value.R}, {theme.Value.G}, {theme.Value.B})");
}
```

## Where It's Used

The system automatically colors dungeon names in:

1. **Dungeon Selection Screen** - Each dungeon option shows in its theme color
2. **Dungeon Start Screen** - Dungeon name displays in theme color
3. **Dungeon Completion Screen** - Completed dungeon name shows in theme color

## Benefits

- **Visual Identification**: Instantly recognize dungeon types by color
- **Thematic Consistency**: Colors match environments (green = forest, blue = ice, etc.)
- **Enhanced UI**: Adds visual richness without cluttering
- **Scalability**: Easy to add new themes or adjust colors

## Implementation Details

### File Location
`Code/UI/DungeonThemeColors.cs`

### Modified Files
- `Code/UI/Avalonia/CanvasUICoordinator.cs` - Added theme color usage in dungeon screens

### Methods Available
- `GetThemeColor(string theme)` - Gets the main theme color
- `GetDimmedThemeColor(string theme)` - Gets 70% brightness version
- `GetBrightenedThemeColor(string theme)` - Gets brightened version (+50 RGB)
- `GetAllThemeColors()` - Returns all theme/color mappings

## Adding New Themes

To add a new dungeon theme color:

1. Open `Code/UI/DungeonThemeColors.cs`
2. Add to the `themeColorMap` dictionary:
```csharp
{ "NewTheme", Color.FromRgb(255, 128, 0) },  // Orange
```
3. The color will automatically be available throughout the UI

## Complete Documentation

For comprehensive documentation, see:
- `Documentation/05-Systems/COLOR_SYSTEM.md` - Full color system documentation
- `Documentation/05-Systems/COLOR_SYSTEM_IMPLEMENTATION_SUMMARY.md` - Implementation details
- `Documentation/01-Core/ARCHITECTURE.md` - System architecture

---

*Part of the DungeonFighter-v2 Color System*  
*Implemented: October 2025*

