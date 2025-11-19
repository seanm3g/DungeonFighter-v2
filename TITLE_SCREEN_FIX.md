# Title Screen Display Fix

## Issue
The title screen displaying "DUNGEON FIGHTER" was not visible to users when the game started. The screen would briefly show the title, then immediately clear when the "Press any key to continue..." message was displayed.

## Root Cause
In `UtilityCoordinator.cs`, the `ShowPressKeyMessage()` method was calling `canvas.Clear()`, which erased the title screen that had just been rendered. This happened because:

1. `TitleScreenHelper.ShowStaticTitleScreen()` renders the title screen
2. Then calls `renderer.ShowPressKeyMessage()` 
3. Which delegates to `CanvasUICoordinator.ShowPressKeyMessage()`
4. Which delegates to `UtilityCoordinator.ShowPressKeyMessage()`
5. Which was calling `canvas.Clear()` - destroying the title display

## Solution
Modified `UtilityCoordinator.ShowPressKeyMessage()` to:
- **Remove** the `canvas.Clear()` call that was erasing the title screen
- **Add** the "Press any key to continue..." message at the bottom (Y=50) without clearing
- **Preserve** the rendered title screen on the display

### Changes Made
**File:** `Code/UI/Avalonia/Coordinators/UtilityCoordinator.cs`

```csharp
// BEFORE:
public void ShowPressKeyMessage()
{
    canvas.Clear();  // ❌ This was erasing the title!
    canvas.AddCenteredText(25, "Press any key to continue...", AsciiArtAssets.Colors.Gray);
    canvas.Refresh();
}

// AFTER:
public void ShowPressKeyMessage()
{
    // Don't clear - just add the message to the bottom of the existing display
    // This preserves the title screen that was just rendered
    canvas.AddCenteredText(50, "Press any key to continue...", AsciiArtAssets.Colors.Gray);
    canvas.Refresh();
}
```

## Build Status
✅ **Build Successful** - Project compiled with no errors (only existing warnings)

## Testing
To verify the fix:
1. Run the game: `dotnet run` (from the Code directory)
2. You should now see:
   - Black screen briefly (1 second delay)
   - **DUNGEON** text in gold color
   - **FIGHTER** text in red color
   - "Press any key to continue..." message at the bottom
   - All animation should be visible before the game initializes

## Architecture Notes
The title screen system is properly architected:
- `TitleArtAssets.cs` - Stores ASCII art for "DUNGEON" and "FIGHTER"
- `TitleAnimationConfig.cs` - Configures animation timing and colors
- `TitleAnimation.cs` - Generates animation frames
- `TitleFrameBuilder.cs` - Builds individual frames
- `TitleScreenController.cs` - Orchestrates playback
- `TitleRenderer.cs` - Implements rendering for Canvas/Console
- `MainWindow.axaml.cs` - Calls `TitleScreenHelper.ShowStaticTitleScreen()`

The fix ensures the full animation pipeline works correctly without clearing the display prematurely.

