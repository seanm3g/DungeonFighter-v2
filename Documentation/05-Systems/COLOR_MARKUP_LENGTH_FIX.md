# Color Markup Length Calculation Fix
**Date:** October 14, 2025  
**Issue:** Markup color characters were being counted as spaces when calculating text length  
**Status:** ✅ FIXED

---

## The Problem

When generating or wrapping text with color markup, the system was using raw `.Length` property to determine:
- When to add spaces between words
- When to wrap text to the next line
- How to center text

This caused markup characters (`&X`, `^X`, `{{template|text}}`) to be counted in the length, which led to:
- **Extra spaces** being inserted where markup was present
- **Incorrect text wrapping** calculations
- **Misaligned centered text**

### Example
```csharp
// Text with markup:
"hits for &R7&y damage"

// Raw .Length: 20 characters (includes &R and &y)
// Display Length: 16 characters (visible text only)

// When checking if (currentLine.Length > 0) to add space:
// - Would be true even if only markup was present (no visible text)
// - This caused extra spaces to be added
```

---

## Root Causes

### 1. ColoredTextWriter.WrapText() - Line 201, 206, 211, 222
**Problem:**
```csharp
// BEFORE (incorrect):
int spaceNeeded = currentLine.Length > 0 ? 1 : 0;
currentLine += (currentLine.Length > 0 ? " " : "") + word;
if (currentLine.Length > 0)
```

`currentLine` contains the full text WITH markup, so `.Length` includes markup characters.

**Solution:**
```csharp
// AFTER (correct):
int spaceNeeded = currentLineLength > 0 ? 1 : 0;
currentLine += (currentLineLength > 0 ? " " : "") + word;
if (currentLineLength > 0)
```

Use `currentLineLength` which tracks the display length (visible characters only).

**Files Modified:**
- `Code/UI/Avalonia/Renderers/ColoredTextWriter.cs`

### 2. GameCanvasControl.AddText() - Line 168
**Problem:**
```csharp
// BEFORE (incorrect):
x = CenterX - (text.Length / 2);
```

When centering text, raw `.Length` was used even though text might contain markup.

**Solution:**
```csharp
// AFTER (correct):
x = CenterX - (ColorParser.GetDisplayLength(text) / 2);
```

Use `ColorParser.GetDisplayLength()` which strips markup before calculating length.

**Files Modified:**
- `Code/UI/Avalonia/GameCanvasControl.cs`

### 3. TextDisplayIntegration.GetVisibleLength() - Line 204
**Problem:**
```csharp
// BEFORE (incomplete):
if (text[i] == '&' && i < text.Length - 1)
```

Only handled `&X` foreground colors, but not `^X` background colors.

**Solution:**
```csharp
// AFTER (complete):
if ((text[i] == '&' || text[i] == '^') && i < text.Length - 1)
```

Now handles both foreground and background color codes.

**Files Modified:**
- `Code/UI/TextDisplayIntegration.cs`

### 4. PersistentLayoutManager.WrapText() - Lines 192, 208
**Problem:**
```csharp
// BEFORE (incorrect):
if (!string.IsNullOrEmpty(currentLine) && (currentLine.Length + 1 + word.Length) > maxWidth)
if (currentLine.Length > maxWidth)
```

Used raw `.Length` for wrapping calculations.

**Solution:**
```csharp
// AFTER (correct):
int currentLineDisplayLength = 0;
int wordDisplayLength = ColorParser.GetDisplayLength(word);
if (currentLineDisplayLength > 0 && (currentLineDisplayLength + 1 + wordDisplayLength) > maxWidth)
if (currentLineDisplayLength > maxWidth)
```

Track display length separately and use it for all comparisons.

**Files Modified:**
- `Code/UI/Avalonia/PersistentLayoutManager.cs`

### 5. DungeonRenderer - Line 263
**Problem:**
```csharp
// BEFORE (incorrect):
if (line.Length > width - 8)
```

Used raw `.Length` to check if line needs wrapping.

**Solution:**
```csharp
// AFTER (correct):
if (ColorParser.GetDisplayLength(line) > width - 8)
```

Use `ColorParser.GetDisplayLength()` to check visible length.

**Files Modified:**
- `Code/UI/Avalonia/Renderers/DungeonRenderer.cs`

### 6. CombatRenderer - Line 151-152
**Problem:**
```csharp
// BEFORE (incorrect):
if (narrativeLine.Length > width - 8)
    narrativeLine = narrativeLine.Substring(0, width - 11) + "...";
```

Used raw `.Length` and `Substring` which could cut markup in the middle.

**Solution:**
```csharp
// AFTER (correct):
if (ColorParser.GetDisplayLength(narrativeLine) > width - 8)
{
    string strippedLine = ColorParser.StripColorMarkup(narrativeLine);
    narrativeLine = strippedLine.Substring(0, Math.Min(strippedLine.Length, width - 11)) + "...";
}
```

Strip markup before truncating to avoid breaking markup codes.

**Files Modified:**
- `Code/UI/Avalonia/Renderers/CombatRenderer.cs`

### 7. CanvasUICoordinator.AddToDisplayBuffer() - Line 514-516
**Problem:**
```csharp
// BEFORE (incorrect):
if (message.Length > CONTENT_WIDTH)
{
    message = message.Substring(0, CONTENT_WIDTH - 3) + "...";
}
```

Used raw `.Length` and `Substring` for truncation check and operation.

**Solution:**
```csharp
// AFTER (correct):
if (ColorParser.GetDisplayLength(message) > CONTENT_WIDTH)
{
    string strippedMessage = ColorParser.StripColorMarkup(message);
    message = strippedMessage.Substring(0, Math.Min(strippedMessage.Length, CONTENT_WIDTH - 3)) + "...";
}
```

Use display length for check and strip markup before truncating.

**Files Modified:**
- `Code/UI/Avalonia/CanvasUICoordinator.cs`

---

## The Fix Pattern

**Rule:** Never use `.Length` directly on text that might contain markup.

### When to Use ColorParser.GetDisplayLength()

✅ **Use GetDisplayLength() for:**
- Checking if text needs wrapping
- Calculating text position for centering
- Determining if space is needed between words
- Comparing text lengths for layout
- Any calculation that affects visual positioning

❌ **Don't use raw .Length for:**
- Text with color markup (`&X`, `^X`, `{{template|text}}`)
- Any string that might be displayed with colors

### Correct Pattern
```csharp
// Track BOTH the full text (with markup) and the display length
string currentLine = "";           // Full text with markup
int currentLineLength = 0;         // Display length (visible only)

// When adding text:
currentLine += word;                              // Add full text
currentLineLength += ColorParser.GetDisplayLength(word);  // Add display length

// When checking conditions:
if (currentLineLength > 0)  // Use display length
{
    currentLine += " ";      // Add space to full text
    currentLineLength += 1;  // Add space to display length
}
```

---

## Impact

### Before Fix:
```
Pax Stormrider hits Bat for    7 damage
                               ^^^^
                          Extra spaces
```

### After Fix:
```
Pax Stormrider hits Bat for 7 damage
                            ^
                       Correct spacing
```

---

## Testing

To verify the fix works:
1. Run combat and check for extra spaces in damage messages
2. Look for proper spacing around colored text
3. Verify centered text with markup is properly aligned
4. Check wrapped text doesn't have spacing issues

---

## Related Files

### Core Utilities:
- `Code/UI/ColorParser.cs` - Provides `GetDisplayLength()` and `StripColorMarkup()`
- `Code/UI/ColorDefinitions.cs` - Color code definitions

### Files Fixed:
- `Code/UI/Avalonia/Renderers/ColoredTextWriter.cs` - Text wrapping
- `Code/UI/Avalonia/GameCanvasControl.cs` - Text centering
- `Code/UI/TextDisplayIntegration.cs` - Visible length calculation
- `Code/UI/Avalonia/PersistentLayoutManager.cs` - Equipment wrapping
- `Code/UI/Avalonia/Renderers/DungeonRenderer.cs` - Description wrapping check
- `Code/UI/Avalonia/Renderers/CombatRenderer.cs` - Narrative truncation
- `Code/UI/Avalonia/CanvasUICoordinator.cs` - Display buffer truncation

---

## Future Prevention

**Code Review Checklist:**
- [ ] Any use of `.Length` on user-facing text should be reviewed
- [ ] Text layout code should use `GetDisplayLength()` instead of raw `.Length`
- [ ] Text wrapping functions should track display length separately
- [ ] Centering calculations must strip markup before measuring

**Pattern to Avoid:**
```csharp
❌ if (text.Length > maxWidth)                    // Wrong
❌ int x = center - (text.Length / 2)             // Wrong
❌ int spaceNeeded = currentLine.Length > 0 ? 1 : 0  // Wrong
```

**Pattern to Use:**
```csharp
✅ if (ColorParser.GetDisplayLength(text) > maxWidth)  // Correct
✅ int x = center - (ColorParser.GetDisplayLength(text) / 2)  // Correct
✅ int spaceNeeded = displayLength > 0 ? 1 : 0     // Correct
```

