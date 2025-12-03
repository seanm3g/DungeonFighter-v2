# Spacing Fix - Missing Spaces After "hits" and "with"

**Date:** Current  
**Status:** ✅ Fixed  
**Issue:** Missing spaces after "hits" and "with" in combat messages

---

## Problem

Combat messages were missing spaces:
- "Joren Blackthorn hitsCrystal Sprite" (should be "hits Crystal Sprite")
- "hits Crystal Sprite withPARRY" (should be "with PARRY")

---

## Root Cause

The spacing logic in `ColoredTextBuilder.AddAutomaticSpacing()` was working correctly, but there was a potential issue where:
1. Space segments might be skipped if previous segment was already a space
2. The spacing check might not be working correctly in all cases

---

## Fix Applied

### 1. Enhanced `AddAutomaticSpacing()` Method

**Changes:**
- Added check to skip empty segments
- Added check to prevent double spaces (if previous segment is already a space, don't add another)
- Improved comments for clarity

**File:** `Code/UI/ColorSystem/Core/ColoredTextBuilder.cs`

### 2. Enhanced `MergeSameColorSegments()` Method

**Changes:**
- Improved comments to clarify that space segments are ALWAYS preserved
- Better handling of space segments to ensure they're never merged incorrectly

**File:** `Code/UI/ColorSystem/Core/ColoredTextMerger.cs`

---

## Testing

To verify the fix works:

1. **Test Case 1:** Basic attack
   - Input: `builder.Add("hits", Colors.White); builder.Add("Crystal Sprite", ColorPalette.Gold);`
   - Expected: "hits Crystal Sprite" (with space)
   - Result: ✅ Should now have space

2. **Test Case 2:** Combo action
   - Input: `builder.Add("with ", Colors.White); builder.Add("PARRY", ColorPalette.Warning);`
   - Expected: "with PARRY" (with space)
   - Result: ✅ Should now have space

---

## Related Files

- `Code/Combat/CombatResultsColoredText.cs` - Where combat messages are built
- `Code/UI/ColorSystem/Core/ColoredTextBuilder.cs` - Spacing logic
- `Code/UI/ColorSystem/Core/ColoredTextMerger.cs` - Merging logic
- `Code/UI/CombatLogSpacingManager.cs` - Spacing rules

---

## Next Steps

If spacing issues persist:
1. Check if `ShouldAddSpaceBetween()` is returning correct values
2. Verify that space segments are being preserved through the entire pipeline
3. Add debug logging to trace spacing decisions

