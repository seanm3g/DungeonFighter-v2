# Main Menu Centering - Quick Guide

## What Changed?
The main menu is now **center-justified** on the screen, adapting to your screen size automatically.

## How It Works

### Console Mode
- The menu automatically detects your console window width
- All menu elements (title, options, prompts) are centered
- Resizing the window will maintain centering on next display
- Color markup codes are correctly handled

### Avalonia/GUI Mode
- Menu is centered on the fixed 100-character canvas
- All clickable areas are properly aligned
- Hover effects work correctly on centered elements

## What You'll See

### Before:
```
DUNGEON FIGHTER
----------------
[1] Enter Dungeon
[2] View Inventory
[3] Character Info
...
```

### After (Centered):
```
                DUNGEON FIGHTER
                ----------------
             [1] Enter Dungeon
            [2] View Inventory
            [3] Character Info
                    ...
```

## Testing the Changes

### Console Mode
1. Run the game: `.\DF.bat` or `dotnet run`
2. Observe the centered main menu
3. Try resizing your console window
4. Navigate to the menu again to see it re-center

### Avalonia Mode  
1. Run the Avalonia version
2. Main menu will be centered on the canvas
3. Test clicking and hovering on menu items
4. All interactions should work normally

## Technical Details

**Files Modified:**
- `Code/UI/TextDisplayIntegration.cs` - Console centering
- `Code/UI/Avalonia/CanvasUICoordinator.cs` - Avalonia centering

**Key Features:**
- ✅ Dynamic screen width detection (Console)
- ✅ Color markup handling (preserves colors while centering)
- ✅ Multi-line text support
- ✅ Maintains clickable regions (Avalonia)
- ✅ No breaking changes to existing code

## Configuration
Currently, centering is always enabled. To disable it, you would need to modify the `DisplayMenu()` method in `TextDisplayIntegration.cs`.

## Troubleshooting

**Problem:** Menu appears off-center in console
**Solution:** Make sure your console is wide enough (minimum 80 characters recommended)

**Problem:** Clickable regions don't work in Avalonia
**Solution:** Restart the application - clickable regions are recalculated on startup

**Problem:** Color markup appears as text
**Solution:** Ensure `UIManager.EnableColorMarkup = true` in your settings

## Related Documentation
- Full implementation details: `MENU_CENTERING_IMPLEMENTATION.md`
- UI System documentation: `Documentation/05-Systems/`
- Color system: `Documentation/05-Systems/COLOR_SYSTEM.md`

