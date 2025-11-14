# Intentional Color Scheme - Quick Reference

## 🎨 Color Scheme Overview

This quick reference provides instant lookup for the intentional color scheme system implemented for items, classes, and modifications.

## 📊 Rarity Colors

| Rarity | Template | Colors | Description |
|--------|----------|--------|-------------|
| Common | `enhanced_common` | Light grey | Basic but clean |
| Uncommon | `enhanced_uncommon` | Green → White | Nature/improvement |
| Rare | `enhanced_rare` | Blue → Cyan → White | Valuable |
| Epic | `enhanced_epic` | Magenta → Cyan → White | Exceptional |
| Legendary | `enhanced_legendary` | Orange → White → Yellow | Legendary shimmer |
| Mythic | `enhanced_mythic` | Magenta → Cyan → Yellow → Blue | Prismatic |
| Transcendent | `enhanced_transcendent` | White → Magenta → Cyan → Blue | Ethereal |

## ⚔️ Item Class Colors

| Item Class | Template | Colors | Theme |
|------------|----------|--------|-------|
| Weapon | `weapon_class` | Red → Orange → Yellow | Combat/offensive |
| Head Armor | `head_armor` | Cyan → Blue → White | Mental/protective |
| Chest Armor | `chest_armor` | Blue → Cyan → White | Core protection |
| Feet Armor | `feet_armor` | Green → Cyan → White | Mobility/grounded |

## 🗡️ Weapon Type Colors

| Weapon Type | Template | Colors | Theme |
|-------------|----------|--------|-------|
| Sword | `sword_weapon` | White → Bright White | Balanced/classic |
| Dagger | `dagger_weapon` | Cyan → White | Quick/precise |
| Mace | `mace_weapon` | Orange → Red | Heavy/blunt |
| Wand | `wand_weapon` | Magenta → Cyan | Magical/mystical |

## ✨ Effect Colors

| Effect Type | Template | Colors | Theme |
|-------------|----------|--------|-------|
| Damage | `damage_effect` | Red → Orange → Yellow | Destructive |
| Speed | `speed_effect` | Cyan → White | Fast/dynamic |
| Magical | `magical_effect` | Magenta → Cyan → Blue | Mystical |
| Defensive | `defensive_effect` | Blue → Cyan → White | Protective |
| Life | `life_effect` | Green → White | Vitality |
| Death | `death_effect` | Dark Red → Red → Magenta | Corruption |
| Divine | `divine_effect` | White → Yellow → White | Holy |

## 🔧 Usage Examples

### Basic Formatting
```csharp
// Rarity only
ItemColorSystem.FormatItemName(item)

// Item class theming
ItemColorSystem.FormatItemNameWithClass(item)

// Weapon type theming
ItemColorSystem.FormatWeaponNameWithType(weapon)

// Comprehensive display
ItemColorSystem.FormatItemWithComprehensiveColors(item)
```

### Example Outputs
```
{{enhanced_rare|Steel Sword}}                    // Rarity only
{{weapon_class|Steel Sword}}                     // Class theming
{{sword_weapon|Steel Sword}}                     // Weapon type
{{damage_effect|Sharp}} {{enhanced_rare|Steel Sword}} {{defensive_effect|of Protection}} ({{sword_weapon|Sword}})  // Full display
```

## 🎯 Color Philosophy

### Visual Hierarchy
- **Rarity**: Clear progression from basic to transcendent
- **Class**: Thematic colors reinforce item purpose
- **Effects**: Intuitive color associations

### Thematic Consistency
- **Warm Colors**: Combat/offensive (weapons, damage)
- **Cool Colors**: Protection/defensive (armor, defense)
- **Magical Colors**: Mystical effects (purple, cyan, blue)

### Systematic Patterns
- **Consistent Mapping**: Same effects = same colors
- **Intelligent Fallbacks**: Unknown effects get appropriate defaults
- **Extensible Design**: Easy to add new patterns

## 🚀 Quick Implementation

### For New Items
1. Use `FormatItemName()` for basic rarity coloring
2. Use `FormatItemNameWithClass()` for class theming
3. Use `FormatItemWithComprehensiveColors()` for full display

### For New Effects
1. Add to `ModificationColorMap` in `ItemColorSystem.cs`
2. Add fallback pattern in `GetModificationColorTemplate()`
3. Add color template to `ColorTemplates.json`

### For New Item Classes
1. Add to `ItemClassColorMap` in `ItemColorSystem.cs`
2. Add color template to `ColorTemplates.json`
3. Update documentation

## 📝 Color Code Reference

| Code | Color | Description |
|------|-------|-------------|
| `r` | Dark Red | Crimson (#a64a2e) |
| `R` | Bright Red | (#ff3232) |
| `o` | Vibrant Orange | (#ff8c00) |
| `O` | Orange | (#D04200) |
| `w` | Brown | (#98875f) |
| `W` | Bright Yellow | (#ffff00) |
| `g` | Dark Green | (#009403) |
| `G` | Green | (#00c420) |
| `b` | Dark Blue | (#0048bd) |
| `B` | Blue/Azure | (#7ac5ff) |
| `c` | Dark Cyan | (#40a4b9) |
| `C` | Cyan | (#b0dce8) |
| `m` | Dark Magenta | (#b154cf) |
| `M` | Magenta | (#da5bd6) |
| `k` | Very Dark | (#0f3b3a) |
| `K` | Dark Grey | (#155352) |
| `y` | Light Grey | (#e6e6e6) |
| `Y` | White | (#ffffff) |

## 🔗 Related Files

- **`Code/UI/ItemColorSystem.cs`** - Main implementation
- **`GameData/ColorTemplates.json`** - Color template definitions
- **`Documentation/05-Systems/INTENTIONAL_COLOR_SCHEME.md`** - Full documentation

---

*This quick reference provides instant access to the intentional color scheme system for efficient development and maintenance.*
