# Combat Display Fix - October 11, 2025

## Issue
Combat output was not being displayed properly:
1. Combat text was not contained within the center block of the persistent layout
2. Colors were not being rendered - raw color markup syntax (like `{{red|Kobold}}` and `{{critical|CRITICAL}}`) was showing instead of colored text

## Root Cause
The `RenderDisplayBuffer()` method in `CanvasUICoordinator.cs` was:
1. Not using the persistent layout system during combat
2. Not parsing color markup when rendering text to the canvas
3. Rendering directly to a simple bordered area without proper structure

During combat, `UIManager.WriteLine()` calls were going to `CanvasUICoordinator.WriteLine()`, which added text to the display buffer and called `RenderDisplayBuffer()`. This method was rendering combat text outside the persistent layout and without color parsing.

## Solution

### 1. Updated `RenderDisplayBuffer()` Method
Modified `Code/UI/Avalonia/CanvasUICoordinator.cs` to:
- Check if a character is set (indicating we're in combat or active gameplay)
- If character is set, use the persistent layout system to render in the center content area
- Always parse color markup using `WriteLineColored()` instead of plain `AddText()`
- Fallback to simple rendering when no character is set (for non-combat screens)

**Changes:**
```csharp
private void RenderDisplayBuffer()
{
    Dispatcher.UIThread.Post(() =>
    {
        canvas.Clear();
        
        // If we have a character (in combat or during gameplay), use persistent layout
        if (currentCharacter != null)
        {
            layoutManager.RenderLayout(currentCharacter, (contentX, contentY, contentWidth, contentHeight) =>
            {
                // Render display buffer in the center content area with color parsing
                int y = contentY;
                foreach (var line in displayBuffer.TakeLast(maxLines))
                {
                    if (y < contentY + contentHeight - 1)
                    {
                        // Parse and render color markup
                        WriteLineColored(line, contentX + 2, y);
                        y++;
                    }
                }
            }, "COMBAT");
        }
        else
        {
            // Fallback to simple rendering when no character is set
            // ... with color parsing enabled
        }
        
        canvas.Refresh();
    }, DispatcherPriority.Background);
}
```

### 2. Updated Combat Flow in Game.cs
Modified `ProcessEnemyEncounter()` in `Code/Game/Game.cs` to:
- Clear the display buffer before combat starts
- Ensure character is set in the UI manager
- This ensures a clean slate for combat output

**Changes:**
```csharp
private async Task<bool> ProcessEnemyEncounter(Enemy enemy)
{
    // ... show enemy encounter ...
    
    // Prepare for combat - clear display buffer and ensure character is set
    if (customUIManager is CanvasUICoordinator canvasUI2)
    {
        canvasUI2.ResetForNewBattle();
        canvasUI2.SetCharacter(currentPlayer);
    }
    
    // ... run combat ...
}
```

## Benefits

### 1. Proper Layout Structure
- Combat text now appears in the center content area of the persistent layout
- Character stats, health, and equipment remain visible on the left panel during combat
- Consistent with the game's persistent layout design

### 2. Color Rendering
- Color markup (like `{{red|text}}` and `&R` syntax) is now properly parsed and rendered
- Combat messages display with appropriate colors (red for enemies, critical highlights, etc.)
- Improves visual clarity and user experience

### 3. Consistent Architecture
- Uses the same persistent layout system as other game screens
- Follows the established pattern of character panel + dynamic content area
- Easier to maintain and extend

## Testing
- ✅ Code compiles successfully (Release build)
- ✅ No linter errors introduced
- 🔄 Manual testing needed: Run game and enter combat to verify layout and colors

## Files Modified
1. `Code/UI/Avalonia/CanvasUICoordinator.cs` - Updated `RenderDisplayBuffer()` method
2. `Code/Game/Game.cs` - Updated `ProcessEnemyEncounter()` method

## Related Documentation
- `Documentation/05-Systems/PERSISTENT_LAYOUT_SYSTEM.md` - Persistent layout architecture
- `Documentation/05-Systems/COLOR_SYSTEM.md` - Color markup system
- `Documentation/05-Systems/TEXT_FADE_ANIMATION_SYSTEM.md` - Text rendering system

## Next Steps
1. Run the game and test combat to verify the fix works as expected
2. Check that colors render properly for all combat messages
3. Verify the persistent layout displays correctly during combat
4. Test with different enemy types and combat scenarios

---

*Fix completed: October 11, 2025*

