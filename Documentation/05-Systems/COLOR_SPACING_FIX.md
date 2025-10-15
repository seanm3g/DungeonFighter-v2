# Color Spacing Fix - October 12, 2025

## Issue
Extra spaces were appearing around colored text in combat logs, making text like:
```
Xan Ironheart hits Rock Elemental    for 19   damage
```

instead of:
```
Xan Ironheart hits Rock Elemental for 19 damage
```

## Root Cause

### Problem 1: Unnecessary Color Reset Codes
In `Code/UI/KeywordColorSystem.cs` line 280, after every colored keyword, the system was adding `+ "&y"` to reset the color to grey:

```csharp
return ColorParser.Colorize(match.Value, group.ColorPattern) + "&y";
```

This color reset code was unnecessary because:
1. The next colored keyword would set its own color anyway
2. Template markup already handles color transitions properly
3. The extra `&y` codes were potentially causing rendering issues with spacing

### Problem 2: Invalid Color Patterns
Several keyword groups in `GameData/KeywordColorGroups.json` were using invalid color pattern names:
- `"enemy"` group used `"red"` (not a valid template or color code)
- `"class"` group used `"cyan"` (not a valid template or color code)
- `"experience"` group used `"white"` (not a valid template or color code)

When `ColorParser.Colorize()` receives an invalid pattern, it returns the text unchanged (but KeywordColorSystem still added `&y` after it), resulting in text that wasn't actually colored but had color reset codes.

## Solution

### Fix 1: Remove Color Reset Codes
**File:** `Code/UI/KeywordColorSystem.cs` (line 280)

**Before:**
```csharp
// Apply the color pattern and reset to default grey color after
return ColorParser.Colorize(match.Value, group.ColorPattern) + "&y";
```

**After:**
```csharp
// Apply the color pattern
return ColorParser.Colorize(match.Value, group.ColorPattern);
```

**Rationale:** Color reset codes are unnecessary. The color system naturally handles transitions between colored and uncolored text.

### Fix 2: Correct Invalid Color Patterns
**File:** `GameData/KeywordColorGroups.json`

**Changes:**
- `"enemy"` colorPattern: `"red"` → `"R"` (red color code)
- `"class"` colorPattern: `"cyan"` → `"C"` (cyan color code)
- `"experience"` colorPattern: `"white"` → `"Y"` (white color code)

**Valid Color Patterns:**
1. **Single-letter color codes:** `R`, `G`, `B`, `C`, `M`, `Y`, `K`, `r`, `g`, `b`, `c`, `m`, `y`, `k`, `o`, `O`, `w`, `W`
2. **Template names:** `fiery`, `icy`, `toxic`, `electric`, `arcane`, `damage`, `critical`, `heal`, etc. (see `ColorTemplates.json`)

## Testing
To verify the fix:
1. Build and run the game
2. Start combat
3. Observe combat log messages - colored words should have proper spacing
4. Enemy names (like "Rock Elemental") should be colored red
5. No extra spaces should appear around colored keywords

## Impact
- **Combat logs:** Properly spaced text with smooth color transitions
- **Enemy names:** Now correctly colored (previously not colored due to invalid pattern)
- **Keyword highlighting:** Maintains visual clarity without spacing artifacts
- **Performance:** Slightly improved by removing unnecessary color code generation

## Related Files
- `Code/UI/KeywordColorSystem.cs` - Keyword coloring logic
- `Code/UI/ColorParser.cs` - Color markup parsing
- `GameData/KeywordColorGroups.json` - Keyword group definitions
- `GameData/ColorTemplates.json` - Color template definitions

## Notes
- The color system uses Caves of Qud-style markup: `&X` for foreground, `^X` for background
- Color codes are automatically stripped when calculating text length for wrapping/positioning
- Template markup `{{template|text}}` is expanded to color code sequences before rendering
- Keywords are matched using whole-word boundaries (`\b` regex) to avoid partial matches

