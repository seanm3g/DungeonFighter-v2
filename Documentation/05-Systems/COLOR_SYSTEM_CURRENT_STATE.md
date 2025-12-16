# Color System Current State - Quick Reference

## Current Configuration Files

### ✅ Data-Driven (JSON)
1. **ColorCodes.json** - Single-letter color codes (r, R, g, G, etc.)
2. **ColorPalette.json** - ColorPalette enum mappings (Player, Enemy, Damage, etc.)
3. **ColorTemplates.json** - Color templates (fiery, icy, legendary, etc.)
4. **KeywordColorGroups.json** - Automatic keyword coloring groups

### ❌ Hard-Coded (C#)
1. **DungeonThemeColors.cs** - 24+ dungeon theme colors (RGB values)
2. **ColorPatterns.cs** - 50+ pattern-to-palette mappings

### ⚠️ Missing Color Info
1. **Enemies.json** - No color information per enemy
2. **Dungeons.json** - No color information per dungeon

## Current Color Assignment

### Entity Colors
- **Player:** `ColorPalette.Player` (Cyan - #00ffff)
- **Enemy:** `ColorPalette.Enemy` (Orange - #ffa500)
- **Boss:** `ColorPalette.Boss` (Purple - #800080)
- **NPC:** `ColorPalette.NPC` (Green - #00ff00)

**Usage:** Found 94 instances in code using `ColorPalette.Player` and `ColorPalette.Enemy`

### Combat Colors
- **Damage:** `ColorPalette.Damage` (Red - #d74200)
- **Healing:** `ColorPalette.Healing` (Green - #00c420)
- **Critical:** `ColorPalette.Critical` (Red - #d74200)
- **Miss:** `ColorPalette.Miss` (Gray - #808080)

### Dungeon Theme Colors
Currently hard-coded in `DungeonThemeColors.cs`:
- Forest: Green (#00c420)
- Lava: Red (#d74200)
- Crypt: Purple (#b154cf)
- Ice: Cyan (#77bfcf)
- ... 20+ more themes

## What Can Be Moved to JSON

### High Priority
1. **Dungeon Theme Colors** → `DungeonThemeColors.json`
   - 24+ theme-to-color mappings
   - Currently hard-coded RGB values

2. **Color Patterns** → `ColorPatterns.json`
   - 50+ pattern-to-palette mappings
   - Currently hard-coded dictionary

### Medium Priority
3. **Per-Enemy Color Overrides** → Add to `Enemies.json`
   - Allow specific enemies to have custom colors
   - Example: Fire Elemental could use "fiery" template

4. **Per-Dungeon Color Overrides** → Add to `Dungeons.json`
   - Allow specific dungeons to have custom colors
   - Example: "Ancient Forest" could use "forest" template

### Low Priority
5. **Consolidate All Color Configs** → `ColorConfiguration.json`
   - Single unified file for all color settings
   - Easier to edit but larger file

## Quick Edit Guide

### Currently Editable (JSON)
- ✅ Damage colors → `ColorPalette.json` → "Damage"
- ✅ Healing colors → `ColorPalette.json` → "Healing"
- ✅ Player color → `ColorPalette.json` → "Player"
- ✅ Enemy color → `ColorPalette.json` → "Enemy"
- ✅ Rarity colors → `ColorPalette.json` → "Common", "Rare", etc.
- ✅ Color templates → `ColorTemplates.json`
- ✅ Keyword groups → `KeywordColorGroups.json`

### Currently NOT Editable (Hard-Coded)
- ❌ Dungeon theme colors → `DungeonThemeColors.cs`
- ❌ Color pattern mappings → `ColorPatterns.cs`
- ❌ Per-enemy colors → Not supported
- ❌ Per-dungeon colors → Not supported

## Example: What You Want to Edit

### Scenario 1: Change Damage Color
**Current:** Edit `ColorPalette.json` → "Damage" → Change RGB/hex
**Status:** ✅ Already possible

### Scenario 2: Change Enemy Name Color
**Current:** Edit `ColorPalette.json` → "Enemy" → Change RGB/hex
**Status:** ✅ Already possible

### Scenario 3: Change Specific Enemy Color (e.g., "Goblin")
**Current:** Not possible - all enemies use `ColorPalette.Enemy`
**After Refactor:** Edit `Enemies.json` → "Goblin" → Add "colorOverride"
**Status:** ❌ Requires refactoring

### Scenario 4: Change Dungeon Theme Color (e.g., "Forest")
**Current:** Edit `DungeonThemeColors.cs` → Change RGB value
**After Refactor:** Edit `DungeonThemeColors.json` → "Forest" → Change RGB/hex
**Status:** ❌ Requires refactoring

### Scenario 5: Change Specific Dungeon Color (e.g., "Ancient Forest")
**Current:** Not possible - uses theme color
**After Refactor:** Edit `Dungeons.json` → "Ancient Forest" → Add "colorOverride"
**Status:** ❌ Requires refactoring

## Recommended Refactoring Order

1. **Move Dungeon Theme Colors to JSON** (High Impact, Low Risk)
2. **Move Color Patterns to JSON** (Medium Impact, Low Risk)
3. **Add Per-Entity Color Overrides** (High Impact, Medium Risk)
4. **Consolidate Files** (Optional, Lower Priority)

## See Also

- `Documentation/02-Development/COLOR_SYSTEM_REFACTORING_ANALYSIS.md` - Detailed analysis
- `GameData/README_COLOR_CONFIG.md` - Current color system documentation
- `Documentation/05-Systems/COLOR_SYSTEM.md` - Complete color system docs

