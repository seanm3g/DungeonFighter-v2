# Delete Saved Character Feature

## Overview
Added the ability to delete saved character data from the Settings menu in both the console and GUI versions of DungeonFighter.

## Implementation Details

### GUI Version (Avalonia Interface)

#### Settings Screen Enhancement
- **Location**: Settings menu (accessible from Main Menu → Settings)
- **Display**: Shows saved character information if a save file exists
  - Character name and level displayed below delete button
  - Button color: Light red (RGB 255, 100, 100)
  
#### Two-Step Confirmation Process
1. **First Click**: Button changes to "Confirm Delete?" in bright red (RGB 255, 0, 0)
2. **Second Click**: Character save file is permanently deleted
3. **Any Other Action**: Cancels the delete confirmation

#### User Experience
- Clear visual feedback with color change on confirmation
- Safe two-click process prevents accidental deletion
- Displays current character info so user knows what they're deleting
- Returns to main menu after successful deletion
- If currently playing the deleted character, clears the current player state

### Console Version
- Already implemented in `SettingsManager.cs`
- Settings menu option "2. Delete Saved Characters"
- Text-based confirmation prompt (y/N)
- Same backend functionality using `CharacterSaveManager.DeleteSaveFile()`

## Technical Implementation

### Modified Files

#### 1. `Code/UI/Avalonia/CanvasUICoordinator.cs`
**Changes:**
- Updated `RenderSettings()` method to:
  - Check for saved character existence
  - Display character info (name and level)
  - Add delete button with confirmation state
  - Adjust layout dynamically based on save file presence
- Added `deleteConfirmationPending` field
- Added `ResetDeleteConfirmation()` method
- Added `SetDeleteConfirmationPending(bool)` method

#### 2. `Code/Game/Game.cs`
**Changes:**
- Updated `HandleSettingsInput(string)` to:
  - Handle option "1" (Back to Main Menu)
  - Handle option "2" (Delete Saved Character)
  - Cancel confirmation on any other input
- Added `HandleDeleteCharacter(CanvasUICoordinator)` method:
  - Checks for save file existence
  - Implements two-step confirmation logic
  - Calls `CharacterSaveManager.DeleteSaveFile()`
  - Clears current player if they were deleted
  - Returns to main menu after deletion
- Added `deleteConfirmationPending` field

### Existing Infrastructure Used
- **`CharacterSaveManager.DeleteSaveFile()`**: Handles actual file deletion
- **`CharacterSaveManager.SaveFileExists()`**: Checks for save file
- **`CharacterSaveManager.GetSavedCharacterInfo()`**: Gets character name and level

## User Instructions

### How to Delete a Saved Character (GUI)
1. Start DungeonFighter
2. From the Main Menu, select **"3. Settings"**
3. If you have a saved character, you'll see:
   - A button labeled **"[2] Delete Saved Character"**
   - Below it: "Current Save: [Character Name] (Level [X])"
4. Click the delete button once
   - Button changes to **"Confirm Delete?"** in bright red
5. Click the button again to permanently delete
   - Character save file is deleted
   - Returns to main menu
6. To cancel, press any other key or button

### How to Delete a Saved Character (Console)
1. Start DungeonFighter
2. From the Main Menu, select **"3. Settings"**
3. Select **"2. Delete Saved Characters"**
4. Confirm with **"y"** or **"yes"**
5. Character save file is deleted

## Safety Features
- **Two-step confirmation**: Prevents accidental deletion
- **Visual feedback**: Button color changes to warn user
- **Character info display**: Shows what will be deleted
- **Cancellation option**: Any other input cancels the operation
- **Error handling**: Gracefully handles missing files or errors

## Related Files
- `Code/Entity/CharacterSaveManager.cs` - Backend save/load/delete operations
- `Code/Data/SettingsManager.cs` - Console settings menu
- `Code/UI/MenuConfiguration.cs` - Menu option configuration
- `GameData/character_save.json` - The file that gets deleted

## Future Enhancements
- Multiple save slots
- Export/import save files
- Cloud save backup
- Character restore from backup

