# Weapon Selection Rendering Issue - Root Cause Analysis

## Problem
The top portion of the weapon selection menu (weapons [1] and [2]) is being hidden/cleared, showing only weapons [3] and [4] along with orphaned stats text. The top decorative line is also clipped by the box border.

## Root Cause Analysis

### Sequence of Operations:
1. `LayoutCoordinator.CoordinateLayout()` called with `clearCanvas: true`
2. Line 52: `canvas.Clear()` - Clears ALL elements
3. Line 67: `canvas.AddBorder(...)` - Adds center panel border at Y=0, height=53 (covers Y=0 to Y=52)
4. Line 108: `renderCenterContent?.Invoke(centerX, centerY, centerW, centerH)` - Renders weapon selection
   - Passes `centerY = CENTER_PANEL_Y + 1 = 1` as starting Y coordinate
   - Weapon selection calculates `boxY` relative to this coordinate

### The Problem:
- **Center Panel Border**: Added at Y=0, covers entire center panel (Y=0 to Y=52)
- **Weapon Selection Box**: Positioned relative to `centerY = 1`
  - With `minTopMargin = 4`, box starts at `boxY >= 5`
  - Text inside starts at `boxY + 2 >= 7`
- **Rendering Order**: Boxes render BEFORE text in `CanvasRenderer.Render()`
  - This is correct - text should appear on top
  - But if box borders have any visual thickness or anti-aliasing, they might cover adjacent text

### Why Weapons [1] and [2] Are Missing:
The image shows:
- Top decorative line is clipped (should be at `boxY + 2`)
- Weapons [1] and [2] names/numbers are missing
- Weapons [1] and [2] stats ARE visible (rendered on next line)
- Weapons [3] and [4] are fully visible

This suggests text at Y coordinates around 7-15 is being hidden, while text at Y=17+ is visible.

### Possible Causes:
1. **Box Border Overlap**: The weapon selection box border might be covering text positioned too close to it
2. **Center Panel Border Interference**: The center panel border (Y=0) might be creating visual artifacts
3. **Coordinate Calculation Error**: Text might be positioned at wrong Y coordinates
4. **Rendering Artifact**: Box border pen stroke might have thickness that covers adjacent pixels

## Changes Made:
1. ✅ Removed redundant `ClearTextInArea` call (was clearing text unnecessarily)
2. ✅ Increased `minTopMargin` from 2 to 4 for more clearance
3. ✅ Added comments explaining rendering order

## Next Steps to Investigate:
1. Check if center panel border is necessary for weapon selection (it has its own border)
2. Verify exact Y coordinates where text is being added vs where it should be
3. Check if box border pen stroke has thickness that covers text
4. Consider making center panel border optional for menu screens with their own borders
