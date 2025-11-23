# Item Color System

## Overview

The Item Color System provides rich, visually appealing color coding for items based on their rarity and modifications (prefixes/suffixes). Higher rarities feature more elaborate color patterns, creating a sense of progression and excitement when finding rare items.

## Implementation Date
October 11, 2025

## Features

### 1. Rarity-Based Coloring

Items are colored according to their rarity tier, with increasingly fancy patterns for higher rarities:

| Rarity | Color Pattern | Description |
|--------|---------------|-------------|
| **Common** | White (solid) | Simple white color |
| **Uncommon** | Green (solid) | Solid green color |
| **Rare** | Blue (solid) | Solid blue color |
| **Epic** | Purple (solid) | Solid purple color |
| **Legendary** | Orange-White-Orange (sequence) | Golden shimmer effect |
| **Mythic** | Purple-Cyan-White-Cyan-Purple (sequence) | Prismatic glow effect |
| **Transcendent** | White-Purple-Cyan-Blue-Purple-White (sequence) | Ethereal radiance effect |

### 2. Prefix/Suffix Coloring

Item modifications (prefixes and suffixes) are colored based on their effect type:

| Effect Type | Color Template | Examples |
|-------------|----------------|----------|
| Damage | Fiery (Red-Orange-Yellow) | +10 Damage |
| Speed | Electric (Cyan-Yellow-White) | Attack Speed +0.2s |
| Magical | Arcane (Purple-Magenta-Cyan) | +5 Roll Bonus, Magic Find |
| Life/Death | Bloodied (Red-Dark Red) | Lifesteal, Bleed Chance |
| Divine | Holy (Gold-White-Gold) | Godlike, Auto-Success |

### 3. Stat Bonus Coloring

Stat bonuses are colored according to their type:

| Stat Type | Color Template |
|-----------|----------------|
| STR | Fiery (Red) |
| AGI | Electric (Cyan) |
| TEC | Crystalline (Purple-Blue) |
| INT | Arcane (Purple-Magenta) |
| Health | Heal (Green) |
| Armor | Natural (Green-Brown) |

## Usage Examples

### Console Display

```csharp
// Simple colored item name
string coloredName = ItemDisplayFormatter.GetColoredItemName(item);
UIManager.WriteMenuLine(coloredName);

// Full item display with prefixes/suffixes
string fullName = ItemDisplayFormatter.GetColoredFullItemName(item);
UIManager.WriteMenuLine(fullName);

// Display item with colored bonuses
ItemDisplayFormatter.FormatItemBonusesWithColor(item, UIManager.WriteMenuLine);
```

### GUI Display

The GUI automatically uses the color system when rendering inventory:

```csharp
// In CanvasUICoordinator.RenderInventoryContent
string coloredItemName = ItemDisplayFormatter.GetColoredItemName(item);
string displayLine = $"&y[{i + 1}] {coloredItemName}";
WriteLineColored(displayLine, x + 2, y);
```

## Visual Examples

### Common Item
```
Iron Sword (Damage: 10, Speed: 0.5s)
```
*Displayed in grey*

### Legendary Item
```
{{legendary|Blazing Excalibur}} (Damage: 100, Speed: 0.1s)
    Stats: {{fiery|of the Titan}} (STR +15)
    Modifiers: {{fiery|Fury}} (+25 damage), {{electric|Swiftness}} (15% faster)
```
*Main name shimmers in orange-white-orange*
*"Fury" glows with red-orange-yellow fire effect*
*"Swiftness" crackles with cyan-yellow-white electric effect*

### Mythic Item
```
{{mythic|Starfall Hammer of the Gods}} (Damage: 150, Speed: 0.2s)
    Stats: {{fiery|of Supreme Strength}} (STR +25), {{arcane|of the Archmage}} (INT +20)
    Modifiers: {{holy|Godlike}} (+10 to rolls & +1 STR), {{crystalline|Prismatic}} (reroll with +5 bonus)
```
*Main name displays with purple-cyan-white-cyan-purple prismatic glow*
*Each modifier has its own thematic color pattern*

### Transcendent Item
```
{{transcendent|Eternity's Edge}} (Damage: 200, Speed: 0.05s)
    Stats: {{fiery|of the Immortal}} (STR +40), {{heal|of Eternal Life}} (Health +100)
    Modifiers: {{holy|Divine Essence}} (auto-success), {{ethereal|Transcendence}} (+50 magic find)
```
*Main name shimmers with white-purple-cyan-blue-purple-white ethereal radiance*
*Creates a sense of otherworldly power*

## Technical Implementation

### Files Modified/Created

1. **GameData/ColorTemplates.json** - Added Mythic and Transcendent color templates
2. **Code/UI/ItemColorSystem.cs** - New class for item color formatting
3. **Code/UI/ItemDisplayFormatter.cs** - Added color formatting methods
4. **Code/UI/GameDisplayManager.cs** - Updated to use colored item display
5. **Code/UI/Avalonia/CanvasUICoordinator.cs** - Updated GUI inventory rendering
6. **Code/UI/Avalonia/AsciiArtAssets.cs** - Added Transcendent rarity color

### Color System Integration

The system integrates seamlessly with the existing Caves of Qud-inspired color system:

- Uses `{{template|text}}` syntax for color templates
- Uses `&X` syntax for single-character color codes
- Supports both console and GUI rendering
- Automatically parses color markup via `ColorParser`
- Renders colored segments via `WriteLineColored` in GUI

## Benefits

1. **Visual Hierarchy** - Players can instantly identify item quality
2. **Excitement** - Finding high-tier items feels rewarding with fancy colors
3. **Information Density** - Colors convey meaning without extra text
4. **Consistency** - Same color = same type of bonus across all items
5. **Scalability** - Easy to add new color templates for new effect types
6. **Flexibility** - Works in both console and GUI modes

## Configuration

### Adding New Color Templates

To add a new color template for item effects:

1. Add template to `GameData/ColorTemplates.json`:
```json
{
  "name": "my_effect",
  "shaderType": "sequence",
  "colors": ["R", "O", "W"],
  "description": "My custom effect"
}
```

2. Map effect to template in `ItemColorSystem.cs`:
```csharp
private static readonly Dictionary<string, string> ModificationColorMap = new()
{
    { "myEffect", "my_effect" }
};
```

### Customizing Rarity Colors

Rarity color templates can be customized in `GameData/ColorTemplates.json` without code changes.

## Future Enhancements

Potential improvements:

1. **Animated Colors** - Time-based color cycling for legendary+ items
2. **Custom Shaders** - More complex color pattern algorithms
3. **Player Preferences** - Allow players to choose color schemes
4. **Color Blind Mode** - Alternative color palettes for accessibility
5. **Context-Aware Colors** - Different colors based on equipped items
6. **Comparison Highlighting** - Special colors for better/worse comparisons

## Examples in Game

### Inventory Display

When viewing inventory, items are displayed with full color coding:

```
INVENTORY ITEMS
[1] {{uncommon|Iron Sword}} (Damage: 14, Speed: 0.5s)
[2] {{rare|Enchanted Staff}} (Damage: 20, Speed: 0.3s)
    Stats: {{arcane|of the Sage}} (INT +10)
    Modifiers: {{arcane|Magical}} (+5 roll bonus)
[3] {{legendary|Flaming Axe of the Berserker}} (Damage: 45, Speed: 0.4s)
    Stats: {{fiery|of Fury}} (STR +15), {{fiery|of Rage}} (Damage +10)
    Modifiers: {{fiery|Blazing}} (+15 damage), {{bloodied|Vampiric}} (5% lifesteal)
```

### Equipment Display

Equipped items are shown with colors in character panel:

```
GEAR
Weapon: {{legendary|Excalibur}}
Head: {{rare|Enchanted Helm}}
Body: {{epic|Dragonscale Armor}}
Feet: {{uncommon|Swift Boots}}
```

## Performance

The color system is highly optimized:

- **Color parsing** is cached and reused
- **Template expansion** happens once per render
- **No noticeable performance impact** even with many items
- **Efficient string building** using StringBuilder where appropriate

## Conclusion

The Item Color System adds significant visual polish to DungeonFighter-v2, making loot drops more exciting and item quality immediately apparent. The system is flexible, extensible, and integrates seamlessly with the existing color infrastructure.

---

*Documentation last updated: October 11, 2025*

