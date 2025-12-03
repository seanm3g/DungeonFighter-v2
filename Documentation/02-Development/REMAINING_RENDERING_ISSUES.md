# Remaining Rendering Issues & Refactoring Opportunities

**Date:** Current Analysis  
**Purpose:** Identify remaining issues after fixing character-count positioning

---

## Issues Found

### 1. **Inaccurate Scroll Offset Calculation** ⚠️ HIGH PRIORITY

**Location:** `CenterPanelDisplayManager.CalculateMaxScrollOffset()`

**Problem:**
```csharp
private int CalculateMaxScrollOffset()
{
    // This is a simplified calculation - in practice, we'd need to calculate
    // the total height of all wrapped lines. For now, return a reasonable estimate.
    return Math.Max(0, buffer.Count * 2 - 50); // Rough estimate
}
```

**Issue:** Uses a rough estimate instead of actual wrapped line count. This can cause:
- Incorrect scroll limits
- Scrolling beyond actual content
- Scroll buttons not working correctly

**Fix:** Calculate actual wrapped line count using the same logic as `DisplayRenderer.Render()`:
```csharp
private int CalculateMaxScrollOffset()
{
    var (contentX, contentY, contentWidth, contentHeight) = layoutManager.GetCenterContentArea();
    int availableWidth = contentWidth - 2;
    
    var linesToRender = buffer.GetLast(buffer.MaxLines);
    int totalHeight = 0;
    
    foreach (var segments in linesToRender)
    {
        int linesNeeded = CalculateWrappedLineCount(segments, availableWidth);
        totalHeight += linesNeeded;
    }
    
    return Math.Max(0, totalHeight - contentHeight);
}
```

**Note:** This requires access to `layoutManager` and `CalculateWrappedLineCount`, which might need refactoring.

---

### 2. **ForceRender Called Too Frequently** ⚠️ MEDIUM PRIORITY

**Location:** `CenterPanelDisplayManager.ScrollUp()` and `ScrollDown()`

**Problem:**
- Every scroll action calls `timing.ForceRender()` immediately
- This bypasses debouncing and might cause multiple renders
- Could cause visual flicker or performance issues

**Current:**
```csharp
public void ScrollUp(int lines = 3)
{
    // ...
    timing.ForceRender(new System.Action(PerformRender)); // Immediate render
}
```

**Consideration:** 
- Scrolling needs immediate feedback, so `ForceRender` might be appropriate
- But we should ensure only one render happens per scroll action
- The `DisplayTiming.ForceRender()` already cancels pending renders, so this might be OK

**Potential Fix:** Add a flag to prevent multiple simultaneous renders:
```csharp
private bool isRendering = false;

private void PerformRender()
{
    if (isRendering) return; // Prevent concurrent renders
    isRendering = true;
    try
    {
        // ... existing render logic ...
    }
    finally
    {
        isRendering = false;
    }
}
```

---

### 3. **DisplayTiming.ForceRender() Threading Issue** ⚠️ MEDIUM PRIORITY

**Location:** `DisplayTiming.ForceRender()`

**Problem:**
```csharp
public void ForceRender(System.Action renderAction)
{
    // ...
    renderAction.Invoke(); // Called directly, not on UI thread
}
```

**Issue:** If called from non-UI thread, this could cause threading issues. However, `PerformRender()` already uses `Dispatcher.UIThread.Post()`, so this might be OK.

**Current Flow:**
1. `ForceRender()` called → `renderAction.Invoke()` (direct call)
2. `renderAction` is `PerformRender`
3. `PerformRender()` uses `Dispatcher.UIThread.Post()` → Safe

**Verdict:** Probably OK, but could be clearer. Consider documenting or ensuring all callers are on UI thread.

---

### 4. **CalculateWrappedLineCount Might Be Inaccurate** ⚠️ LOW PRIORITY

**Location:** `DisplayRenderer.CalculateWrappedLineCount()`

**Current:**
```csharp
private int CalculateWrappedLineCount(List<ColoredText> segments, int maxWidth)
{
    int displayLength = ColoredTextRenderer.GetDisplayLength(segments);
    int wrappedLines = Math.Max(1, (int)Math.Ceiling((double)displayLength / maxWidth));
    return wrappedLines;
}
```

**Issue:** Uses simple division, which might not match actual wrapping logic in `ColoredTextWriter.WriteLineColoredWrapped()`. The actual wrapping might:
- Break on word boundaries
- Handle spaces differently
- Have different behavior for long words

**Fix:** Use the same wrapping logic, or extract wrapping to a shared utility:
```csharp
// Option 1: Use actual wrapping logic
private int CalculateWrappedLineCount(List<ColoredText> segments, int maxWidth)
{
    // Actually wrap the text and count lines
    var wrapped = textWriter.WrapColoredSegments(segments, maxWidth);
    return wrapped.Count;
}

// Option 2: Extract wrapping to shared utility
// Create TextWrappingUtility.CalculateWrappedLineCount()
```

---

### 5. **Multiple Render Paths** ⚠️ LOW PRIORITY (Architecture)

**Location:** `CenterPanelDisplayManager.PerformRender()`

**Issue:** Two render paths:
1. Full layout render (when `shouldClearCanvas || state.NeedsFullLayout`)
2. Center content only (otherwise)

**Consideration:** This is probably intentional for performance, but could be simplified.

**Potential Improvement:** Extract render logic to separate methods for clarity.

---

### 6. **Render State Tracking Complexity** ⚠️ LOW PRIORITY

**Location:** `RenderStateManager`

**Issue:** Tracks many state variables:
- `lastRenderedCharacter`
- `lastRenderedEnemy`
- `lastRenderedDungeonName`
- `lastRenderedRoomName`
- `lastBufferCount`
- `lastScrollOffset`
- `lastIsManualScrolling`

**Consideration:** This is necessary for determining when to render, but could be simplified with a state hash or version number.

---

## Recommended Fixes (Priority Order)

### Priority 1: Fix Scroll Offset Calculation
**Impact:** High - Fixes scroll accuracy  
**Effort:** Medium - Requires refactoring to share calculation logic  
**Risk:** Low - Improves accuracy

### Priority 2: Add Render Guard
**Impact:** Medium - Prevents concurrent renders  
**Effort:** Low - Simple flag check  
**Risk:** Low - Safety improvement

### Priority 3: Improve Wrapped Line Calculation
**Impact:** Medium - More accurate scroll limits  
**Effort:** Medium - Extract or share wrapping logic  
**Risk:** Low - Better accuracy

### Priority 4: Document Threading Model
**Impact:** Low - Code clarity  
**Effort:** Low - Add comments  
**Risk:** None

---

## Quick Wins

1. **Add render guard** - Simple, low-risk improvement
2. **Improve CalculateMaxScrollOffset** - Share calculation with DisplayRenderer
3. **Extract wrapping logic** - Create shared utility for consistency

---

## Long-term Refactoring

1. **Unify render paths** - Single render method with mode parameter
2. **Simplify state tracking** - Use state hash instead of multiple variables
3. **Extract calculation utilities** - Shared utilities for scroll/wrap calculations

