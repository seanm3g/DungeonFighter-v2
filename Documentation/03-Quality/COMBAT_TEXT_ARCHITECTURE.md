# Combat Text Display Architecture - High Level Analysis

**Date:** October 12, 2025  
**Purpose:** Understanding the complete text rendering pipeline to fix spacing issues

---

## Text Flow Pipeline

```
1. COMBAT GENERATION
   └─> CombatResults.FormatDamageDisplay()
       - Creates damage text: "Lorin hits Umbra for &R6&y damage"
       - Creates roll info: "(roll: 7 | attack 4 - 2 armor | speed: 8.8s)"
       └─> Returns combined string with \n separator

2. BLOCK DISPLAY MANAGER
   └─> BlockDisplayManager.DisplayActionBlock(actionText, rollInfo, effects)
       - Splits text into action line + roll info line
       - LINE 79: Applies KeywordColorSystem.Colorize(actionText)  ⚠️ PROBLEM #1
       - LINE 82: Roll info NOT colored (good)
       - LINE 88: Effects ARE colored ⚠️ PROBLEM #2
       └─> Calls UIManager.WriteLine() for each line

3. UI MANAGER
   └─> UIManager.WriteLine(message, messageType)
       - Forwards to CanvasUICoordinator.WriteLine()
       └─> No processing here, just routing

4. CANVAS UI MANAGER
   └─> CanvasUICoordinator.WriteLine(message, messageType)
       - LINE 167-171: Adds to displayBuffer
       - Calls RenderDisplayBuffer()
       └─> No text processing here

5. RENDER DISPLAY BUFFER
   └─> CanvasUICoordinator.RenderDisplayBuffer()
       - LINE 572: Calls WriteLineColoredWrapped() for each line
       └─> This is where color parsing happens

6. WRITE LINE COLORED
   └─> CanvasUICoordinator.WriteLineColored(message, x, y)
       - LINE 178: Checks if message has color markup
       - LINE 180: Calls ColorParser.Parse(message)  ⚠️ PROBLEM #3
       └─> Renders segments to canvas

7. COLOR PARSER
   └─> ColorParser.Parse(text)
       - LINE 31: Expands templates: {{red|text}} → &Rtext
       - LINE 34: Parses color codes: &Rtext → segments
       └─> Returns List<ColoredSegment>

8. CANVAS RENDERING
   └─> GameCanvasControl.AddText(x, y, text, color)
       - Final rendering to Avalonia canvas
```

---

## The Problem: Triple Color Processing

**Issue:** Text goes through keyword coloring AFTER we've already added explicit color codes!

### Example Text Flow

**Original:**
```
"Lorin hits Umbra for &R6&y damage"
```

**After KeywordColorSystem.Colorize() (LINE 79):**
```
"Lorin {{golden|hits}} Umbra for &R6&y {{damage|damage}}"
```
⚠️ The keyword "damage" gets wrapped even though it already has &y before it!

**After Template Expansion (LINE 31):**
```
"Lorin &ghits&y Umbra for &R6&y &rdamage&y"
```
⚠️ Now "damage" has TWO color resets: our &y AND the template's &y

**After Final Parsing:**
```
Segments:
- "Lorin " (white)
- "hits" (green)
- " Umbra for " (white) 
- "6" (red)
- " " (white) ← EXTRA SPACE HERE FROM COLOR RESET
- "damage" (dark red) ← WRONG COLOR
```

---

## Root Causes

### Problem #1: Keyword Coloring After Explicit Colors
**Location:** `BlockDisplayManager.cs` LINE 79
```csharp
UIManager.WriteLine(ApplyKeywordColoring(actionText), UIMessageType.Combat);
```

**Issue:** 
- We add explicit color codes in `CombatResults.cs`: `&R{damage}&y`
- Then `KeywordColorSystem` wraps keywords: `damage` → `{{damage|damage}}`
- This ignores our explicit colors and adds its own

**Why It Fails:**
- `KeywordColorSystem.Colorize()` doesn't check for existing color codes
- It treats &R6&y damage as plain text and colors "damage"
- Template expansion adds more color codes

### Problem #2: Status Effect Coloring
**Location:** `BlockDisplayManager.cs` LINE 88
```csharp
UIManager.WriteLine($"    {ApplyKeywordColoring(effect)}", UIMessageType.EffectMessage);
```

**Issue:**
- Environmental effects already have explicit colors
- Example: `"&YSTUNNED&Y for 2 turns&y"`
- Keyword coloring tries to color "STUNNED" and "turns" again
- Creates double color codes and spacing issues

### Problem #3: Color Code Spacing
**Location:** `ColorParser.cs` template expansion

**Issue:**
- When template `{{damage|damage}}` expands to `&rdamage&y`
- And we already have `&y damage`
- Result: `&y &rdamage&y` (two resets)
- The parser might add spaces or the keyword system does

---

## Solutions

### Option A: Disable Keyword Coloring for Pre-Colored Text ✅ RECOMMENDED

**Change:**
```csharp
// BlockDisplayManager.cs LINE 17-20
private static string ApplyKeywordColoring(string text)
{
    // Skip keyword coloring if text already has explicit color codes
    if (ColorParser.HasColorMarkup(text))
    {
        return text;
    }
    return KeywordColorSystem.Colorize(text);
}
```

**Pros:**
- Simple fix
- Respects explicit colors
- No spacing issues

**Cons:**
- Some keywords won't be colored if mixed with explicit codes

### Option B: Make KeywordColorSystem Smarter ✅ BETTER LONG-TERM

**Change:**
```csharp
// KeywordColorSystem.cs LINE 271-281
result = Regex.Replace(result, pattern, match =>
{
    // Check if this match is already inside color markup
    if (IsInsideColorMarkup(result, match.Index))
    {
        return match.Value;
    }
    
    // ALSO check if immediately preceded by color code
    if (match.Index >= 2 && result[match.Index - 2] == '&')
    {
        return match.Value; // Skip, already colored
    }

    return ColorParser.Colorize(match.Value, group.ColorPattern);
}, options);
```

**Pros:**
- Keyword coloring still works
- Respects explicit colors
- More intelligent

**Cons:**
- More complex
- Might still have edge cases

### Option C: Remove Keyword Coloring Entirely ⚠️ LAST RESORT

**Change:**
```csharp
// BlockDisplayManager.cs LINE 17-20
private static string ApplyKeywordColoring(string text)
{
    return text; // Disabled
}
```

**Pros:**
- No conflicts
- Complete control

**Cons:**
- Lose automatic coloring
- More manual work

---

## Recommended Fix

**1. Immediate Fix (Option A):**
Add the `HasColorMarkup()` check to `ApplyKeywordColoring()` in `BlockDisplayManager.cs`

**2. Long-term Fix (Option B):**
Improve `KeywordColorSystem` to detect and skip already-colored text

**3. Clean up explicit colors:**
- Ensure all explicit color codes use proper format: `&RX&y text` (no space before text)
- Remove unnecessary color wrapping

---

## Testing Points

After fixes, verify:

1. ✅ Combat damage: "hits Umbra for 6 damage" (6 is red, rest is white)
2. ✅ No extra spaces between number and "damage"
3. ✅ Status effects: "affected by STUN for 2 turns" (STUN colored, rest white)
4. ✅ Roll info stays white (no coloring)
5. ✅ No color bleed between words

---

## Files Involved

| File | Role | Issue |
|------|------|-------|
| `CombatResults.cs` | Formats damage text | Adds explicit colors |
| `BlockDisplayManager.cs` | Displays blocks | **Applies keyword coloring AFTER explicit colors** |
| `KeywordColorSystem.cs` | Auto-colors keywords | **Doesn't check for existing colors** |
| `ColorParser.cs` | Parses color codes | **Template expansion might add spaces** |
| `CanvasUICoordinator.cs` | Renders to canvas | Processes final text |

---

## Next Steps

1. Implement Option A (HasColorMarkup check)
2. Test combat display
3. If spacing still occurs, check ColorParser template expansion
4. Consider Option B for long-term improvement

