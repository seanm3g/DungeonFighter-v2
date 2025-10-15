# Color Markup Spacing Fix - Summary
**Date:** October 14, 2025  
**Status:** ✅ COMPLETE

---

## Problem
Markup color characters (`&X`, `^X`, `{{template|text}}`) were being counted as spaces when calculating text length for:
- Text wrapping decisions
- Space insertion between words
- Text centering
- Truncation checks

This caused **extra spaces** to appear in combat logs and other text displays.

### Example Issue
```
Before: "hits for    7 damage"  (extra spaces)
After:  "hits for 7 damage"     (correct spacing)
```

---

## Root Cause
Code was using raw `.Length` property on strings containing markup, which includes the markup characters themselves. The correct approach is to use `ColorParser.GetDisplayLength()` which strips markup before measuring.

---

## Files Fixed

### 1. `Code/UI/Avalonia/Renderers/ColoredTextWriter.cs`
- Fixed space insertion logic in text wrapping
- Changed from `currentLine.Length` to `currentLineLength` (tracked display length)
- **Lines affected:** 201, 206, 211, 222

### 2. `Code/UI/Avalonia/GameCanvasControl.cs`
- Fixed text centering calculation
- Changed from `text.Length` to `ColorParser.GetDisplayLength(text)`
- **Lines affected:** 169

### 3. `Code/UI/TextDisplayIntegration.cs`
- Added support for background color codes (`^X`) in visible length calculation
- Already handled `&X` foreground codes
- **Lines affected:** 204

### 4. `Code/UI/Avalonia/PersistentLayoutManager.cs`
- Fixed equipment name wrapping logic
- Tracks display length separately from full text with markup
- **Lines affected:** 181-230 (entire WrapText method refactored)

### 5. `Code/UI/Avalonia/Renderers/DungeonRenderer.cs`
- Fixed room description wrapping check
- Changed from `line.Length` to `ColorParser.GetDisplayLength(line)`
- **Lines affected:** 263

### 6. `Code/UI/Avalonia/Renderers/CombatRenderer.cs`
- Fixed battle narrative truncation
- Now strips markup before truncating to avoid cutting markup codes
- **Lines affected:** 152-157

### 7. `Code/UI/Avalonia/CanvasUIManager.cs`
- Fixed display buffer message truncation
- Changed from `message.Length` to `ColorParser.GetDisplayLength(message)`
- Strips markup before truncating
- **Lines affected:** 514-518

---

## Solution Pattern

### ❌ INCORRECT (Old Way)
```csharp
if (text.Length > maxWidth)
int x = center - (text.Length / 2);
int spaceNeeded = currentLine.Length > 0 ? 1 : 0;
```

### ✅ CORRECT (New Way)
```csharp
if (ColorParser.GetDisplayLength(text) > maxWidth)
int x = center - (ColorParser.GetDisplayLength(text) / 2);
int spaceNeeded = currentLineDisplayLength > 0 ? 1 : 0;
```

### Key Principle
**Always track TWO values when working with markup:**
1. **Full text** (with markup) - for display/storage
2. **Display length** (without markup) - for calculations

---

## Testing
To verify the fix:
1. ✅ Run combat - check for extra spaces in messages
2. ✅ Check colored damage numbers spacing
3. ✅ Verify centered text alignment
4. ✅ Test wrapped text (long descriptions)
5. ✅ Check truncated narrative text

---

## Documentation
Full technical details available in:
- `Documentation/05-Systems/COLOR_MARKUP_LENGTH_FIX.md`

---

## Prevention
**Code Review Checklist:**
- [ ] Never use `.Length` on text that might contain markup
- [ ] Use `ColorParser.GetDisplayLength()` for all layout calculations
- [ ] Track display length separately when building strings
- [ ] Strip markup before truncating text with `Substring()`

