# Dungeon Selection - Color Pattern Update

## Summary
The dungeon selection screen has been updated to display dungeon names with **themed color patterns** instead of single colors. This provides a more visually appealing and immersive experience.

## What Changed

### Before
Each dungeon name was displayed in a single solid color based on its theme:
```
[1] Ocean Depths (lvl 5)        ← Single blue color
[2] Crystal Caverns (lvl 6)     ← Single cyan color
[3] Mystical Garden (lvl 7)     ← Single green color
```

### After
Each dungeon name now uses a **color pattern template** that cycles through multiple themed colors:
```
[1] Ocean Depths (lvl 5)        ← Flowing blue→cyan→blue ocean shimmer
[2] Crystal Caverns (lvl 6)     ← Sparkling magenta→cyan→yellow crystal pattern
[3] Mystical Garden (lvl 7)     ← Natural green→yellow→green garden glow
```

## Examples by Theme

| Dungeon | Color Pattern Effect |
|---------|---------------------|
| **Ocean Depths** | Deep blue → bright blue → cyan → blue (oceanic waves) |
| **Crystal Caverns** | Magenta → cyan → yellow (crystalline refraction) |
| **Mystical Garden** | Green → yellow → green (natural growth) |
| **Lava Caves** | Red → orange → red (molten fire) |
| **Frozen Peaks** | Cyan → blue → white → cyan (icy frost) |
| **Shadow Realm** | Black → dark grey → magenta → grey (dark shadows) |
| **Ancient Library** | Magenta → cyan → blue (arcane magic) |
| **Time Distortion** | Cyan → magenta → yellow → blue (temporal flux) |

## Technical Implementation

### Modified File
- `Code/UI/Avalonia/CanvasUIManager.cs`

### Key Changes
1. Added `GetDungeonThemeTemplate()` method to map themes to color templates
2. Updated `RenderDungeonSelectionContent()` to use `ColorParser.Colorize()`
3. Changed rendering to use `WriteLineColored()` for template processing

### Code Example
```csharp
// Map theme to template (e.g., "Ocean" → "ocean")
string templateName = GetDungeonThemeTemplate(dungeon.Theme);

// Apply color template to dungeon name
string coloredName = ColorParser.Colorize(dungeon.Name, templateName);

// Render with pattern: {{ocean|Ocean Depths}}
displayText = $"&y[{i + 1}] {coloredName} &Y(lvl {dungeon.MinLevel})";
WriteLineColored(displayText, x + 4, y);
```

## How to Test

1. **Build the game** (close any running instances first)
   ```
   cd "d:\Code Projects\github projects\DungeonFighter-v2"
   dotnet build Code/Code.csproj
   ```

2. **Run the game**
   ```
   .\Code\bin\Debug\net8.0\DF.exe
   ```

3. **Navigate to dungeon selection**
   - Start or load a game
   - Select "Explore Dungeons" from the main menu
   - Observe the new color patterns on dungeon names

4. **Test hover effects**
   - Hover over dungeon options to verify they still turn yellow
   - Click to select and ensure functionality works correctly

## Compatibility

- ✅ All 24 unique dungeon themes have matching color templates
- ✅ "Generic" theme falls back to grey color (no special pattern needed)
- ✅ Hover effects maintained (entire line turns yellow when hovered)
- ✅ Click functionality unchanged
- ✅ No breaking changes to existing systems

## Color Template System

This update leverages the existing color template system defined in:
- `GameData/ColorTemplates.json` - Template definitions
- `Code/UI/ColorParser.cs` - Template parsing
- `Code/UI/ColorTemplate.cs` - Template application

The same system is used for:
- Item rarity colors
- Item modifier patterns
- Status effect displays
- Environmental descriptions

## Future Enhancements

Possible improvements:
- Animated color cycling for dynamic effects
- Special boss dungeon templates
- Room description color patterns
- Enemy name theming based on dungeon
- Transition effects when selecting dungeons

## Related Documentation

- `Documentation/05-Systems/DUNGEON_SELECTION_COLOR_UPDATE.md` - Detailed technical documentation
- `GameData/COLOR_TEMPLATE_USAGE_GUIDE.md` - Color template usage guide
- `GameData/ColorTemplates.json` - All available templates

---

**Status**: ✅ Complete and ready for testing
**Impact**: Visual enhancement only, no gameplay changes
**Testing Required**: Visual verification of color patterns in dungeon selection screen

