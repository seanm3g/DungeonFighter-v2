# Rendering System Analysis & Refactoring Proposal

**Date:** Current Analysis  
**Purpose:** System-wide evaluation of rendering architecture to fix scrolling text offset issues

---

## Current Problems

### 1. **Text Shifting During Scroll**
- Text moves right and down when scrolling
- Multiple render calls happening (visible in logs)
- Floating point precision issues in X position calculations
- Inconsistent clearing of old text

### 2. **Complex Rendering Pipeline**
```
Game Logic
  ↓
CanvasUICoordinator
  ↓
CenterPanelDisplayManager
  ↓
DisplayRenderer
  ↓
ColoredTextWriter
  ↓
ISegmentRenderer (Standard/Template)
  ↓
GameCanvasControl.AddText/AppendText
  ↓
Canvas (Avalonia)
```

**Issues:**
- Too many layers of abstraction
- Each layer adds its own coordinate transformations
- Floating point calculations at multiple levels
- No single source of truth for text positioning

### 3. **Coordinate System Inconsistencies**

**Current System:**
- `GameCanvasControl` uses integer X, Y (character grid positions)
- `MeasureTextWidth()` returns `double` (pixel width / charWidth)
- Segment renderers accumulate `double` X positions
- Rounding happens at multiple points
- Each render recalculates positions from scratch

**Problems:**
- Floating point accumulation errors
- Rounding at different stages causes drift
- No caching of calculated positions
- Re-rendering recalculates everything

### 4. **Multiple Render Triggers**
- `CenterPanelDisplayManager.PerformRender()` can be called multiple times
- `PersistentLayoutManager.RenderLayout()` calls `renderCenterContent` multiple times
- Each render clears and redraws, causing visible shifts

---

## Root Cause Analysis

### Primary Issue: Floating Point Position Accumulation

**Current Flow:**
1. Start with integer X position (e.g., `contentX + 1 = 30`)
2. For each segment, measure width as `double` (e.g., `5.7`)
3. Accumulate: `currentX = 30.0 + 5.7 = 35.7`
4. Round to int: `renderX = 36`
5. Next segment: `currentX = 36.0 + 4.3 = 40.3` → `renderX = 40`
6. **Problem:** When re-rendering, we start fresh but rounding can differ slightly

**Why It Shifts:**
- `MeasureTextWidth()` might return slightly different values due to font rendering
- Rounding `35.7` vs `35.6` can result in different integer positions
- Multiple segments compound the error
- Re-rendering doesn't use cached positions

### Secondary Issue: Incomplete Clearing

- `ClearTextInArea()` removes text elements, but timing issues can cause:
  - Old text not cleared before new text rendered
  - Partial clears leaving some text visible
  - Multiple render calls overwriting each other

---

## Proposed Solution: Simplified Integer-Based Positioning

### Core Principle
**Use character grid positions (integers) as the single source of truth.**

### Changes Needed

#### 1. **Simplify Segment Rendering**
- Remove floating point accumulation
- Use character count for positioning (with known exceptions for special characters)
- Cache rendered positions per line
- Only use measured width for wrapping calculations, not positioning

#### 2. **Unified Text Positioning**
- All text positioned using integer character grid coordinates
- No floating point in the positioning pipeline
- Consistent X starting position for each line

#### 3. **Improved Clearing**
- Clear entire content area before any render
- Use atomic clear+render operations
- Prevent multiple simultaneous renders

#### 4. **Position Caching**
- Cache the final rendered positions
- Reuse cached positions when re-rendering same content
- Only recalculate when content actually changes

---

## Implementation Strategy

### Phase 1: Fix Immediate Issue (Quick Fix)
1. Ensure consistent X starting position
2. Improve clearing to be more thorough
3. Prevent multiple render calls

### Phase 2: Simplify Positioning (Medium-term)
1. Switch to character-count-based positioning
2. Remove floating point from positioning pipeline
3. Add position caching

### Phase 3: Architecture Cleanup (Long-term)
1. Consolidate rendering layers
2. Single rendering path for all modes
3. Unified coordinate system

---

## Recommended Immediate Fix

The quickest fix that addresses the root cause:

1. **Use fixed character grid positioning** instead of measured widths for X positions
2. **Clear entire content area** before rendering (not just visible area)
3. **Prevent multiple render calls** by debouncing/canceling pending renders
4. **Cache line starting positions** to ensure consistency

This maintains the current architecture while fixing the precision issues.

