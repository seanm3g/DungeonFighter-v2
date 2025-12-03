# Rendering System Refactoring Proposal

## Problem Summary

The text rendering system has multiple issues causing text to shift when scrolling:

1. **Mixed Coordinate Systems**: Using measured pixel widths (double) for positioning in a character grid system (int)
2. **Floating Point Accumulation**: Rounding errors compound across multiple segments
3. **Multiple Render Calls**: Several render operations happening simultaneously
4. **Incomplete Clearing**: Old text not fully cleared before new text rendered

## Root Cause

The canvas uses a **character grid system** (integer X, Y positions), but segment renderers use **measured pixel widths** (double) for positioning. This mismatch causes:
- Floating point rounding errors
- Inconsistent positioning on re-render
- Text shifting when scrolling

## Proposed Solution: Character-Count Positioning

### Core Change
**Use character count for X positioning, not measured widths.**

For monospace fonts, each character is exactly 1 character position wide. We should:
- Use `segment.Text.Length` for X position advancement
- Only use `MeasureTextWidth()` for wrapping calculations (when text exceeds maxWidth)
- Keep all X positions as integers throughout the pipeline

### Benefits
- ✅ No floating point errors
- ✅ Consistent positioning on every render
- ✅ Simpler code
- ✅ Better performance (no measurement needed for positioning)

### Implementation

1. **Modify Segment Renderers** to use character count:
   ```csharp
   // Instead of:
   double segmentWidth = canvas.MeasureTextWidth(segment.Text);
   currentX += segmentWidth;
   
   // Use:
   int segmentWidth = segment.Text.Length;
   currentX += segmentWidth;
   ```

2. **Keep measured width only for wrapping**:
   - Use `MeasureTextWidth()` only in `WrapColoredSegments()` to determine if text fits
   - Don't use it for positioning

3. **Simplify ColoredTextWriter**:
   - Remove double precision accumulation
   - Use integer X positions throughout

4. **Improve Clearing**:
   - Clear entire content area before render
   - Use atomic clear+render operations

## Alternative: If Measured Widths Are Needed

If we must use measured widths (for non-monospace or special characters):

1. **Cache measured widths** per segment on first render
2. **Store cached positions** in the buffer
3. **Reuse cached positions** on re-render instead of recalculating

This adds complexity but maintains precision.

## Recommendation

**Use character-count positioning** - it's simpler, faster, and eliminates the precision issues. The monospace font assumption is valid for this game.

