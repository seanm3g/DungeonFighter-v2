# Opening Animation Fixes

## Issues Fixed

### 1. **Color Markup Codes Showing as Text**
**Problem:** Color markup codes like `{{G|`, `{{W|`, `{{Y|` were displaying as visible text instead of being rendered as colors.

**Solution:** Created a dedicated `RenderOpeningAnimation()` method in `CanvasUICoordinator` that properly parses and renders color markup codes using the existing `WriteLineColored()` method.

### 2. **Animation Not Centered**
**Problem:** The opening animation was left-aligned instead of centered on the screen, making it look off-balance.

**Solution:** 
- Added `GetVisibleLength()` method to calculate text length excluding markup codes
- Each line is centered based on its visible length: `startX = (screenWidth - visibleLength) / 2`
- Content is vertically centered as well: `startY = (screenHeight - contentHeight) / 2`

### 3. **Animation Loads Too Slowly**
**Problem:** The animation displayed for 10 seconds with 50ms delays between lines, making it feel sluggish.

**Solution:**
- Reduced display time from 10 seconds to 3 seconds for Avalonia UI
- Lines render instantly (no per-line delays in canvas mode)
- Animation appears immediately when the app starts

### 4. **Incorrect Width Calculation**
**Problem:** The width calculation included markup codes, causing misalignment.

**Solution:** The `GetVisibleLength()` method correctly calculates visible text length by:
- Skipping `{{template|text}}` markup (only counts text after `|`)
- Skipping `&X` color codes
- Only counting actual visible characters

## Files Modified

### `Code/UI/Avalonia/CanvasUICoordinator.cs`
**Added Methods:**
- `RenderOpeningAnimation()` - Renders the opening animation with proper color parsing and centering
- `GetVisibleLength(string text)` - Calculates visible text length excluding markup codes

**Changes:**
- Integrated ASCII art directly into the canvas renderer
- Proper color markup parsing using existing `WriteLineColored()` method
- Both horizontal and vertical centering

### `Code/UI/Avalonia/MainWindow.axaml.cs`
**Changes:**
- Modified `InitializeGame()` to call `canvasUI.RenderOpeningAnimation()` instead of `OpeningAnimation.ShowOpeningAnimation()`
- Reduced display time to 3 seconds (from 10 seconds)
- Added `Avalonia.Threading` using statement for Dispatcher

## Technical Details

### Color Markup Support
The animation now properly handles:
- `{{G|text}}` - Green text (borders, decorative elements)
- `{{W|text}}` - White text (DUNGEON ASCII art)
- `{{R|text}}` - Red text (FIGHTER ASCII art)
- `{{Y|text}}` - Yellow text (decorative swords, prompts)
- `{{C|text}}` - Cyan text (tagline)

### Centering Algorithm
1. **Visible Length Calculation:**
   ```csharp
   // Parses markup to get actual visible character count
   int visibleLength = GetVisibleLength(line);
   ```

2. **Horizontal Centering:**
   ```csharp
   int startX = Math.Max(0, (screenWidth - visibleLength) / 2);
   ```

3. **Vertical Centering:**
   ```csharp
   int startY = Math.Max(2, (screenHeight - contentHeight) / 2);
   ```

### Performance Improvements
- **Before:** 10-second display + ~2.6 seconds line animation = ~12.6 seconds total
- **After:** 3-second display + instant rendering = 3 seconds total
- **Improvement:** **76% faster** (9.6 seconds saved)

## Testing

### To Test the Fixes:
1. **Close any running instances** of the game
2. **Rebuild:** `dotnet build`
3. **Run:** `dotnet run` or `.\DF.exe`
4. **Observe:**
   - Animation should appear instantly
   - Colors should render properly (green borders, white/red text)
   - All text should be perfectly centered
   - Animation should disappear after 3 seconds

### Expected Visual Results:
✅ Green borders and decorative elements  
✅ White "DUNGEON" text (large Unicode blocks)  
✅ Red "FIGHTER" text (large Unicode blocks)  
✅ Green floral decorations (☘, ✿, ❀, ❦)  
✅ Yellow sword symbols (⚔)  
✅ Cyan tagline  
✅ Perfect horizontal centering  
✅ Perfect vertical centering  
✅ No visible markup codes  

## Notes

- Console mode still uses the original `OpeningAnimation.ShowOpeningAnimation()` which works correctly
- Avalonia UI now has its own optimized rendering path
- The `WriteLineColored()` method properly integrates with the existing color system
- All color definitions come from the `ColorParser` system

## Future Enhancements

Possible improvements:
- Add fade-in/fade-out effects
- Make display duration configurable
- Add option to skip animation with any key press
- Support for additional animation styles
- Animation for other UI transitions

