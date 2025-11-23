# Main Menu Centering Implementation

## Overview
This document describes the implementation of center-justified main menu display based on screen size for both Console and Avalonia UI modes.

## Changes Made

### 1. Console UI (TextDisplayIntegration.cs)
**Location:** `Code/UI/TextDisplayIntegration.cs`

**Changes:**
- Added `CenterText()` method that centers text based on console window width
- Added `CenterSingleLine()` helper method for single-line centering
- Added `GetVisibleLength()` method to calculate text length excluding color markup
- Modified `DisplayMenu()` to apply centering to:
  - Title text
  - Separator line
  - All menu options

**Features:**
- Supports multi-line text (centers each line independently)
- Handles color markup codes (both `{{template|text}}` and `&X` formats)
- Calculates visible text length excluding markup for accurate centering
- Gracefully handles invalid console width
- Uses `Console.WindowWidth` to dynamically adapt to screen size

### 2. Avalonia UI (CanvasUICoordinator.cs)
**Location:** `Code/UI/Avalonia/CanvasUICoordinator.cs`

**Changes:**
- Modified `RenderMainMenu()` method to center all menu elements
- Calculated menu option positions based on the longest option text
- Centered all clickable elements and hover zones

**Features:**
- Titles are auto-centered using `AddTitle()` (which already includes centering)
- Menu options are centered as a block based on longest option
- Instructions text is centered at the bottom
- All clickable regions updated to match centered positions
- Based on screen width of 100 characters (from `PersistentLayoutManager.SCREEN_WIDTH`)

## Implementation Details

### Console Centering Algorithm
1. Get console window width using `Console.WindowWidth`
2. Calculate visible text length (excluding markup)
3. Calculate padding: `(consoleWidth - visibleLength) / 2`
4. Prepend padding spaces to text

### Avalonia Centering Algorithm
1. Use fixed screen width (100 characters)
2. For titles: Use `AddTitle()` which auto-centers
3. For menu options:
   - Find longest option text
   - Calculate start position: `(screenWidth - maxOptionLength) / 2`
   - Position all options at the same X coordinate

### Color Markup Handling
The `GetVisibleLength()` method correctly handles:
- Template markup: `{{template|text}}` - counts only the text after `|`
- Color codes: `&X` - skips both the `&` and color character
- Regular text: counts normally

## Testing

### Console UI
To test console centering:
1. Run the game in console mode
2. Resize the console window
3. Navigate to main menu
4. Verify menu is centered regardless of window size

### Avalonia UI
To test Avalonia centering:
1. Run the game in Avalonia mode
2. Verify main menu is centered on the canvas
3. Test hover effects on centered menu items
4. Verify clickable regions are correctly positioned

## Files Modified
- `Code/UI/TextDisplayIntegration.cs` - Added centering logic for console menus
- `Code/UI/Avalonia/CanvasUICoordinator.cs` - Updated main menu rendering to center elements

## Notes
- Console centering is dynamic and adapts to window size changes
- Avalonia centering is based on fixed canvas size (100x40)
- Color markup is correctly handled to prevent off-center text
- All clickable regions are updated to match centered positions
- No breaking changes to existing functionality

## Future Enhancements
Possible improvements:
- Add centering option to configuration (allow toggle on/off)
- Support for other menu types (not just main menu)
- Vertical centering in addition to horizontal
- Support for different alignment modes (left, center, right)

