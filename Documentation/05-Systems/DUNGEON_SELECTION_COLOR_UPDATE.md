# Dungeon Selection Color Pattern Update

## Overview
Updated the dungeon selection screen to display dungeon names with themed color patterns instead of single colors, providing a more visually appealing and thematically appropriate presentation.

## Changes Made

### File Modified
- `Code/UI/Avalonia/CanvasUIManager.cs`

### Key Modifications

#### 1. New Helper Method: `GetDungeonThemeTemplate()`
- Maps dungeon themes to their corresponding color template names
- Converts theme names to lowercase for template lookup
- Falls back to grey color code "y" for themes without templates

#### 2. Updated `RenderDungeonSelectionContent()`
- Changed from using single `DungeonThemeColors.GetThemeColor()` to color template patterns
- Dungeon names now use `ColorParser.Colorize()` with theme-based templates
- Utilizes `WriteLineColored()` to render templated text with color patterns
- Maintains yellow highlight on hover for consistent UX

## Color Template Mappings

Each dungeon theme maps to a color template with themed color patterns:

| Dungeon Theme | Template Name | Color Pattern |
|--------------|---------------|---------------|
| Ocean | ocean | Deep blue → bright blue → cyan sequence |
| Crystal | crystal | Magenta → cyan → yellow shimmer |
| Nature/Mystical Garden | nature | Green → yellow → green natural tones |
| Forest | forest | Dark green → bright green → brown |
| Lava | lava | Red → orange → red fire effect |
| Ice | ice | Cyan → blue → white frozen effect |
| Shadow | shadow | Dark grey → magenta → dark tones |
| Crypt | crypt | Black → grey → yellow decay |
| Temple | temple | White → yellow → brown sacred tones |
| Steampunk | steampunk | Brown → orange → grey mechanical |
| Swamp | swamp | Green → brown → dark murky tones |
| Astral | astral | Magenta → blue → cyan cosmic effect |
| Underground | underground | Black → brown → grey earthy tones |
| Storm | storm | Cyan → yellow → blue electric effect |
| Arcane | arcane | Magenta → cyan → blue magical energy |
| Desert | desert | Yellow → orange → brown sandy tones |
| Volcano | volcano | Red → orange → black volcanic heat |
| Ruins | ruins | Black → brown → grey ancient stone |
| Mountain | mountain | Grey → white → grey rocky peaks |
| Temporal | temporal | Cyan → magenta → blue time distortion |
| Dream | dream | Magenta → cyan → purple dreamlike |
| Void | void | Black → dark grey → magenta emptiness |
| Dimensional | dimensional | Magenta → cyan → blue reality bending |
| Divine | divine | White → yellow → magenta holy light |
| Generic | N/A | Falls back to grey color code |

## Technical Details

### Before
```csharp
// Single color for entire dungeon name
var themeColor = DungeonThemeColors.GetThemeColor(dungeon.Theme);
canvas.AddText(nameX, y, dungeon.Name, hoverColor);
```

### After
```csharp
// Themed color pattern using template
string templateName = GetDungeonThemeTemplate(dungeon.Theme);
string coloredName = ColorParser.Colorize(dungeon.Name, templateName);
displayText = $"&y[{i + 1}] {coloredName} &Y(lvl {dungeon.MinLevel})";
WriteLineColored(displayText, x + 4, y);
```

## Visual Impact

### Examples:
- **Ocean Depths** - Now displays with flowing blue-to-cyan oceanic shimmer
- **Crystal Caverns** - Sparkles with magenta-cyan-yellow crystalline colors
- **Mystical Garden** - Glows with green-yellow natural tones

### Benefits:
1. **Thematic Immersion** - Each dungeon name reflects its environment's atmosphere
2. **Visual Distinction** - Easier to identify dungeons at a glance
3. **Aesthetic Enhancement** - More engaging and polished UI presentation
4. **Consistent System** - Uses the same color template system as items and effects

## Testing

To test the changes:
1. Build and run the game
2. Navigate to the dungeon selection screen
3. Observe that dungeon names display with themed color patterns
4. Verify hover effects still work (entire line turns yellow)
5. Test with various dungeon themes to see different color patterns

## Related Files
- `GameData/ColorTemplates.json` - Contains all color template definitions
- `Code/UI/ColorParser.cs` - Handles template parsing and color code conversion
- `Code/UI/ColorTemplate.cs` - Color template library and application logic
- `GameData/Dungeons.json` - Dungeon definitions with theme properties

## Future Enhancements
- Could add animated color cycling for extra visual flair
- Could create special "boss dungeon" templates with more dramatic patterns
- Could apply similar patterns to room descriptions and environmental text

