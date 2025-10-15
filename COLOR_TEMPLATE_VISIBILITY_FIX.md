# Color Template Visibility Fix

## Issue
Color templates were using dark colors (`k` = very dark #0f3b3a, `K` = dark grey/black #155352) that were invisible or barely visible on black backgrounds.

## Solution
Updated all color templates that are displayed on black backgrounds to use brighter, more visible alternatives while maintaining their thematic appearance.

## Templates Fixed

### Dungeon/Environment Templates
| Template | Old Colors | New Colors | Change Description |
|----------|-----------|------------|-------------------|
| `crypt` | K, k, y, K | y, m, Y, y | Dark → Grey/magenta/white (ghostly) |
| `shadow` (env) | m, M, y, m | m, M, y, m | Already fixed (magenta-based) |
| `steampunk` | w, o, K, y | w, o, y, w | Removed dark grey, use brown |
| `swamp` | g, w, k, g | g, w, y, g | Removed very dark, use grey |
| `underground` | K, w, y, k | w, y, Y, w | Brown/grey earthy tones |
| `volcano` | R, O, K, R | R, O, r, R | Removed dark, use dark red |
| `ruins` | K, w, y, k | w, y, Y, y | Brown/grey aged stone |
| `mountain` | K, y, Y, y | y, Y, C, Y | Grey/white/cyan snowy peaks |
| `void` | k, K, m, K | m, M, y, m | Magenta/grey mysterious void |

### Effect/Status Templates  
| Template | Old Colors | New Colors | Change Description |
|----------|-----------|------------|-------------------|
| `demonic` | r, R, K, r, R | r, R, m, r, R | Dark grey → magenta (hellish) |
| `shadow` | K, k, y, k, K | m, M, y, m, M | Dark → magenta/purple shadows |
| `bloodied` | r, R, r, K | r, R, r, r | Removed dark, pure blood red |
| `corrupted` | m, K, r, K, m | m, r, M, r, m | Dark → red/magenta corruption |
| `poisoned` | g, G, g, k | g, G, g, g | Removed dark, green poison |
| `weakened` | K, y, k, y | y, r, y, r | Grey/red debuff effect |

### Item Modifier Templates
| Template | Old Colors | New Colors | Change Description |
|----------|-----------|------------|-------------------|
| `worn` | k | r | Very dark → dark red (damaged) |
| `dull` | K | y | Dark grey → grey (plain) |

## Templates NOT Fixed (Intentional)
| Template | Colors | Reason |
|----------|--------|--------|
| `title_dungeon` | k, K, y, Y, W | Title screen gradient effect, starts dark and brightens |
| `title_fighter` | k, K, y, Y, O | Title screen gradient effect, starts dark and brightens |

## Color Code Reference
- `k` = very dark (#0f3b3a) - **INVISIBLE on black**
- `K` = dark grey (#155352) - **BARELY VISIBLE on black**
- `y` = grey (#b1c9c3) - **VISIBLE**
- `m` = dark magenta (#b154cf) - **VISIBLE**
- `r` = dark red (#a64a2e) - **VISIBLE**
- `w` = brown (#98875f) - **VISIBLE**

## Testing
All fixed templates should now be clearly visible on black backgrounds while maintaining their thematic color schemes:

1. **Dark/Shadow Themes**: Now use magenta/purple (m, M) instead of black
2. **Underground/Ruins**: Now use brown/grey (w, y) instead of black
3. **Negative Modifiers**: Use visible dark colors (r = dark red, y = grey)
4. **Status Effects**: Maintain theme but use visible color ranges

## Validation
Run the game and verify:
- ✅ All dungeon names are clearly visible in dungeon selection
- ✅ Status effects are visible in combat
- ✅ Item modifiers (worn, dull) are visible in inventory
- ✅ Environmental effects are visible in descriptions

## Related Files
- `GameData/ColorTemplates.json` - Updated template definitions
- `Code/UI/Avalonia/CanvasUIManager.cs` - Uses templates for dungeon display
- `Code/UI/ColorParser.cs` - Processes color templates

---

**Status**: ✅ Complete  
**Impact**: Visual enhancement - all text now visible on black backgrounds  
**Testing**: Visual verification required

