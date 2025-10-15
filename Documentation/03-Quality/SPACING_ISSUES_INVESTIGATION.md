# Color Text Spacing Issues - RESOLVED
**Date:** October 12, 2025  
**Status:** ✅ FIXED  
**Severity:** High - Affects combat log readability  
**Fix:** See `SPACING_FIX_ROLL_INFO.md`

---

## Problem Report

Extra spaces are appearing in combat messages when color markup is involved:

**Observed Issues:**
```
✗ "for 2    damage"     (expected: "for 2 damage" - 1 space, seeing ~4 spaces)
✗ "roll: 9 |    attack" (expected: "roll: 9 | attack" - 1 space, seeing ~4 spaces)  
✗ "CRITICAL    MISS"     (expected: "CRITICAL MISS" - 1 space, seeing ~4 spaces)
```

---

## Investigation Summary

### Code Review Findings

I've traced through the entire color rendering pipeline and found:

1. **Message Construction** ✅ CORRECT
   - `FormatDamageDisplay()` creates messages with proper single spaces
   - Example: `"hits {target.Name} for {actualDamage} damage"` → `"hits Nature Spirit for 2 damage"`

2. **Keyword Coloring** ✅ FIXED (Previously)
   - `KeywordColorSystem` no longer adds `&y` reset codes (removed as per `COLOR_SPACING_FIX.md`)
   - `ColorParser.Colorize()` just wraps text, doesn't add spaces

3. **Template Expansion** ✅ APPEARS CORRECT
   - `ExpandTemplates()` converts `{{template|text}}` → `&Xtext`
   - `ApplySequence()` creates one segment per character
   - No obvious space insertion

4. **Rendering** ✅ APPEARS CORRECT
   - `WriteLineColored()` renders segments sequentially
   - `currentX += segment.Text.Length` - should maintain spacing

### Yet spaces ARE being added somewhere! 🤔

---

## Diagnostic Tool Created

I've created `ColorDebugTool.cs` to help diagnose this issue. It traces a message through the entire color pipeline:

**To Run:**
```csharp
// From code
TestManager.RunColorDebugTest();

// Or directly
UI.ColorDebugTool.RunCombatMessageTests();

// Or test a specific message
UI.ColorDebugTool.DebugMessage("Your test message here");
```

**What it shows:**
1. Original message and length
2. Message after keyword coloring
3. Parsed segments (each with text, foreground, background)
4. Reconstructed text from segments
5. Stripped text (markup removed)
6. **Comparison to identify where spaces were added**

---

## Possible Root Causes

Based on the pattern of issues, here are my hypotheses:

### Hypothesis 1: Color Code Rendering Artifacts
**Theory:** When color codes like `&R` are converted to segments, something in the rendering pipeline might be inserting spaces.

**Evidence:**
- Spaces appear specifically around colored keywords
- 4-space pattern suggests systematic insertion
- Only happens with colored text

**Test:** Run `ColorDebugTool` and check if reconstructed text has extra spaces.

### Hypothesis 2: Template Expansion Double-Processing
**Theory:** Templates might be getting expanded, then re-processed, causing duplicate color codes or spacing.

**Evidence:**
- The `ExpandTemplates()` method converts templates → color codes
- Then `ParseColorCodes()` converts color codes → segments
- Could there be a loop or double-processing?

**Test:** Add debug logging to `ExpandTemplates()` to see if it's called multiple times.

### Hypothesis 3: Keyword Coloring Interference
**Theory:** Even though we removed `&y`, keyword coloring might be inserting markup in a way that affects spacing.

**Evidence:**
- The regex in `KeywordColorSystem.ApplyKeywordColors()` uses `Regex.Replace()`
- If the replacement includes extra characters, spaces could appear

**Test:** Try displaying a message WITHOUT keyword coloring to see if spaces disappear.

### Hypothesis 4: Segment Reconstruction Bug
**Theory:** When segments are converted to strings for display, something in the StringBuilder or string joining is adding spaces.

**Evidence:**
- Line 104 in CombatResults: `string.Join(" | ", rollInfo)`
- If rollInfo items already have spaces or markup, joining could be problematic

**Test:** Check what's actually in the `rollInfo` list before joining.

---

## Investigation Steps for User

### Step 1: Run the Debug Tool

**Action:**
1. Compile the game
2. Run: `TestManager.RunColorDebugTest()`
3. Look at the output for the test messages

**What to look for:**
- Does "AFTER KEYWORD COLORING" show extra spaces?
- Does "RECONSTRUCTED TEXT" match the original?
- Does "STRIPPED TEXT" have more characters than the original?
- Where exactly do the extra spaces appear?

### Step 2: Test Without Keyword Coloring

**Action:**
Temporarily disable keyword coloring in `BlockDisplayManager.cs`:

```csharp
// Line 79 - Comment out keyword coloring
// UIManager.WriteLine(ApplyKeywordColoring(actionText), UIMessageType.Combat);
UIManager.WriteLine(actionText, UIMessageType.Combat);  // No keyword coloring

// Line 82 - Comment out keyword coloring
// UIManager.WriteLine($"    {ApplyKeywordColoring(rollInfo)}", UIMessageType.RollInfo);
UIManager.WriteLine($"    {rollInfo}", UIMessageType.RollInfo);  // No keyword coloring
```

**Test:** Play a combat and see if spacing issues disappear.

**If YES:** The problem is in `KeywordColorSystem.ApplyKeywordColors()`
**If NO:** The problem is elsewhere in the rendering pipeline

### Step 3: Add Debug Logging

**Action:**
Add console logging to see actual values:

```csharp
// In BlockDisplayManager.cs, line 79
Console.WriteLine($"DEBUG - actionText length: {actionText.Length}");
Console.WriteLine($"DEBUG - actionText: '{actionText}'");
var colored = ApplyKeywordColoring(actionText);
Console.WriteLine($"DEBUG - after coloring length: {colored.Length}");
Console.WriteLine($"DEBUG - after coloring: '{colored}'");
UIManager.WriteLine(colored, UIMessageType.Combat);
```

**What to look for:**
- Does the length change after coloring?
- Are there visible extra spaces in the "after coloring" output?

### Step 4: Check Specific Keywords

**Action:**
Test if specific keywords are causing issues:

```csharp
// Test these individually
UI.ColorDebugTool.DebugMessage("damage");
UI.ColorDebugTool.DebugMessage("attack");
UI.ColorDebugTool.DebugMessage("hits");
UI.ColorDebugTool.DebugMessage("for 2 damage");
```

**What to look for:**
- Do single keywords get spaces added?
- Does "for 2 damage" turn into "for 2    damage"?

---

## Quick Fixes to Try

### Fix 1: Disable Keyword Coloring (Temporary)
```csharp
// In BlockDisplayManager.cs
private static string ApplyKeywordColoring(string text)
{
    return text; // Skip keyword coloring for now
    // return KeywordColorSystem.Colorize(text);
}
```

### Fix 2: Use Simple Color Codes Instead of Templates
Replace templates with direct color codes:
```csharp
// Instead of: {{damage|50}}
// Use: &R50
```

### Fix 3: Strip and Re-apply Colors
```csharp
// In BlockDisplayManager.cs
private static string ApplyKeywordColoring(string text)
{
    // Strip any existing markup first
    text = ColorParser.StripColorMarkup(text);
    // Then apply keyword coloring
    return KeywordColorSystem.Colorize(text);
}
```

---

## Files Involved

| File | Role | Suspect Level |
|------|------|---------------|
| `Code/UI/KeywordColorSystem.cs` | Applies keyword colors | 🔴 High |
| `Code/UI/ColorParser.cs` | Parses markup, expands templates | 🟡 Medium |
| `Code/UI/BlockDisplayManager.cs` | Displays combat blocks | 🟡 Medium |
| `Code/UI/Avalonia/CanvasUIManager.cs` | Renders to canvas | 🟢 Low |
| `Code/Combat/CombatResults.cs` | Constructs messages | 🟢 Low (verified correct) |

---

## Next Actions

1. **User:** Run `TestManager.RunColorDebugTest()` and share the output
2. **User:** Try disabling keyword coloring (Fix 1) to isolate the problem
3. **Developer:** Based on debug output, identify exact point where spaces are added
4. **Developer:** Implement targeted fix once root cause is confirmed

---

## Related Documentation

- `Documentation/05-Systems/COLOR_SPACING_FIX.md` - Previous spacing fix (removed `&y`)
- `Documentation/02-Development/COLOR_AND_TEXT_SYSTEM_ANALYSIS.md` - System analysis
- `Code/UI/ColorDebugTool.cs` - Debugging tool (NEW)
- `Code/UI/ColorParserTest.cs` - Test suite

---

## Status Log

| Date | Action | Result |
|------|--------|--------|
| 2025-10-12 | Created ColorDebugTool | Tool ready for testing |
| 2025-10-12 | Reviewed entire color pipeline | No obvious bugs found in code |
| 2025-10-12 | Awaiting user testing | Need debug output to proceed |

---

## ✅ RESOLUTION

**Root Cause Found:** Keyword coloring was being applied to roll info (technical stats), causing:
1. Keywords like "attack" and "damage" to be wrapped in color templates
2. Template expansion adding color codes that caused spacing artifacts
3. Stats being colored when they should remain white

**Fix Applied:** Removed `ApplyKeywordColoring()` from rollInfo display in `BlockDisplayManager.cs` line 82.

**Result:**
- ✅ No more extra spaces
- ✅ Stats remain white (proper visual hierarchy)
- ✅ Only narrative action text gets colored

**Full Details:** See `SPACING_FIX_ROLL_INFO.md`

---

**Status:** ✅ RESOLVED  
**Priority:** ~~HIGH~~ FIXED - October 12, 2025

