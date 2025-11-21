# Item Color Theme System

## Overview

The Item Color Theme System provides comprehensive multi-color theming for items based on their properties. Items can now have multiple colors attached to them based on:

- **Rarity** (Common, Uncommon, Rare, Epic, Legendary, Mythic, Transcendent)
- **Tier** (1-10+, with increasing vibrancy)
- **Modifications** (each modification has its own color theme)
- **Item Type** (Weapon, Head, Chest, Feet)
- **Weapon Type** (Sword, Dagger, Mace, Wand, Staff, Axe, Bow)

## Architecture

### Core Components

1. **`ItemColorThemeSystem.cs`** - Main system for managing item color themes
   - `GetItemThemes()` - Gets all color themes for an item
   - `FormatItemNameWithThemes()` - Formats item name with multiple colors
   - `FormatFullItemDisplay()` - Formats complete item display with all themes

2. **`ItemColorThemes`** - Data class holding all color themes for an item
   - `RarityTheme` - Color theme for rarity
   - `TierTheme` - Color theme for tier
   - `ModificationThemes` - Dictionary of color themes for each modification
   - `ItemTypeTheme` - Color theme for item type
   - `WeaponTypeTheme` - Color theme for weapon type (if applicable)

3. **`ColorTemplateLibrary.cs`** - Extended to support all item-related templates
   - Rarity templates (common, uncommon, rare, epic, legendary, mythic, transcendent)
   - Item type templates (weapon_class, head_armor, chest_armor, feet_armor)
   - Weapon type templates (sword_weapon, dagger_weapon, mace_weapon, wand_weapon)
   - Modification templates (worn, sharp, swift, vampiric, etc.)

## Color Themes by Property

### Rarity Colors

| Rarity | Color Theme | Description |
|--------|-------------|-------------|
| Common | White | Basic white/grey |
| Uncommon | Green | Solid green |
| Rare | Blue | Solid blue |
| Epic | Purple | Solid purple |
| Legendary | Orange/Gold | Shimmering orange-gold |
| Mythic | Purple/Cyan/White | Prismatic purple-cyan |
| Transcendent | White/Magenta/Cyan | Ethereal white-magenta-cyan |

### Tier Colors

| Tier | Color Theme | Description |
|------|-------------|-------------|
| 1 | Grey | Basic grey |
| 2 | Green | Green |
| 3 | Blue | Blue |
| 4 | Cyan | Cyan |
| 5 | Purple | Purple |
| 6 | Orange | Orange |
| 7 | Golden | Golden shimmer |
| 8 | Legendary | Legendary shimmer |
| 9 | Mythic | Mythic prismatic |
| 10+ | Transcendent | Transcendent ethereal |

### Modification Colors

Modifications use color themes based on their rank and name:

- **Common Modifications**: Grey/white (Worn, Dull, Sturdy, Balanced)
- **Uncommon Modifications**: Subtle shimmer (Sharp, Swift, Precise, Reinforced)
- **Rare Modifications**: Brighter mixed colors (Keen, Agile, Lucky, Vampiric)
- **Epic Modifications**: Saturated colors (Brutal, Lightning, Blessed, Venomous)
- **Legendary Modifications**: Complex shimmer (Masterwork, Godlike, Enchanted)
- **Mythic/Transcendent**: Prismatic effects (Annihilation, Timewarp, Perfect, Divine, Reality Breaker, Omnipotent, Infinite, Cosmic)

### Item Type Colors

- **Weapon**: Red-Orange-Yellow gradient (combat/offensive theme)
- **Head Armor**: Cyan-Blue-White gradient (mental/protective theme)
- **Chest Armor**: Blue-Cyan-White gradient (core protection theme)
- **Feet Armor**: Green-Cyan-White gradient (mobility/grounded theme)

### Weapon Type Colors

- **Sword**: White-Gold-White (balanced/classic)
- **Dagger**: Cyan-White-Cyan (quick/precise)
- **Mace**: Orange-Red-Orange (heavy/blunt)
- **Wand**: Magenta-Cyan-Magenta (magical/mystical)
- **Staff**: Arcane (magenta-cyan pattern)
- **Axe**: Fiery (red-orange pattern)
- **Bow**: Natural (green-brown pattern)

## Usage Examples

### Basic Item Display

```csharp
// Get all themes for an item
var themes = ItemColorThemeSystem.GetItemThemes(item);

// Format item name with all color themes
var coloredName = ItemColorThemeSystem.FormatItemNameWithThemes(item, includeModifications: true);

// Format full item display
var fullDisplay = ItemColorThemeSystem.FormatFullItemDisplay(item);
```

### Displaying Modifications

```csharp
// Get modification themes
var modThemes = ItemColorThemeSystem.GetModificationThemes(item.Modifications);

// Format modifications list
var modList = ItemColorThemeSystem.FormatModificationsList(item);
```

### Integration with Existing Systems

The system integrates seamlessly with existing `ItemColorSystem`:

```csharp
// Simple display (uses new theme system)
var simpleDisplay = ItemColorSystem.FormatSimpleItemDisplay(item);

// Full display with modifications (uses new theme system)
var fullDisplay = ItemColorSystem.FormatFullItemName(item);

// Modification display (uses new theme system)
var modDisplay = ItemColorSystem.FormatModification(modification);
```

## Implementation Details

### Color Template System

The system uses the existing `ColorTemplateLibrary` which has been extended to support:

1. **Rarity Templates**: Direct color mapping for rarities
2. **Tier Templates**: Tier-based color progression
3. **Modification Templates**: Rank-based and name-based color themes
4. **Item Type Templates**: Thematic colors for item categories
5. **Weapon Type Templates**: Specific colors for weapon types

### Template Lookup Priority

When determining a modification's color:

1. First, check if there's a template matching the modification name exactly
2. If not found, use the modification's `ItemRank` to determine color
3. Fall back to white if no match is found

### Multi-Color Text Formatting

The system uses `ColoredTextBuilder` to combine multiple color themes:

- Item name uses rarity color
- Prefix modifications appear before the name with their own colors
- Suffix modifications (starting with "of ") appear after the name with their own colors
- Type, tier, and weapon type appear on separate lines with their respective colors

## Benefits

1. **Visual Clarity**: Items are immediately recognizable by their color themes
2. **Information Density**: Multiple properties are color-coded simultaneously
3. **Consistency**: All items follow the same color theme rules
4. **Extensibility**: Easy to add new color themes for new properties
5. **Backward Compatibility**: Works with existing display systems

## Future Enhancements

Potential future improvements:

1. **Dynamic Theme Loading**: Load color themes from JSON configuration
2. **User Customization**: Allow players to customize color themes
3. **Animation Support**: Add animated color transitions for high-tier items
4. **Accessibility**: Support for colorblind-friendly themes
5. **Performance**: Cache color themes for frequently displayed items

## Related Documentation

- `COLOR_SYSTEM.md` - Overall color system documentation
- `COLOR_SYSTEM_BEST_PRACTICES.md` - Best practices for using colors
- `COLOR_TEMPLATE_REFERENCE.md` - Reference for all color templates

