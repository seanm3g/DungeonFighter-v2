# Actions Tab Settings Fix

## Problem

The Actions tab in Settings was showing an empty content area with no actions list or form visible. The Create/Delete buttons were also barely visible.

## Root Causes

1. **Format Mismatch**: `ActionEditor` was trying to deserialize Actions.json directly as `List<ActionData>`, but the file is in `SpreadsheetActionJson` format (with "action" property instead of "name"). This caused silent failures.

2. **UI Visibility Issues**: 
   - Buttons were positioned at the bottom with poor visibility
   - ListBox had no background, making it hard to see
   - No error feedback when loading failed

3. **Missing Error Handling**: Errors were silently swallowed, making debugging difficult.

## Solutions Implemented

### 1. Fixed ActionEditor to Support Both Formats

**File**: `Code/Game/Editors/ActionEditor.cs`

- Updated `LoadActions()` to use `ActionLoader.GetAllActionData()` instead of direct deserialization
- `ActionLoader` automatically detects format (SpreadsheetActionJson vs legacy ActionData) and converts appropriately
- Added proper error logging

**Changes**:
```csharp
// Before: Direct deserialization (only worked with legacy format)
actions = JsonSerializer.Deserialize<List<ActionData>>(jsonContent, ...)

// After: Use ActionLoader (handles both formats)
ActionLoader.LoadActions();
actions = ActionLoader.GetAllActionData();
```

### 2. Added GetAllActionData() Method to ActionLoader

**File**: `Code/Data/ActionLoader.cs`

- Added public method `GetAllActionData()` to expose ActionData objects for editing
- This allows ActionEditor to access the already-loaded and converted ActionData objects

### 3. Improved UI Visibility

**File**: `Code/UI/Avalonia/Settings/ActionsSettingsPanel.axaml`

- **Buttons**: 
  - Moved from bottom to top
  - Increased size (40px height)
  - Added proper styling (blue Create, red Delete)
  - Made fully visible with proper colors

- **ListBox**:
  - Added dark background (#FF2A2A2A)
  - Added border for visibility
  - Set white foreground text

### 4. Enhanced Error Handling and Feedback

**File**: `Code/UI/Avalonia/Managers/ActionsTabManager.cs`

- Added try-catch in `LoadActionsList()`
- Added status messages for:
  - Success: "Loaded X actions"
  - No actions: "No actions found. Check Actions.json file."
  - Errors: "Error loading actions: [error message]"
- Filter out actions with empty names

## Testing

To verify the fix works:

1. **Open Settings** → Navigate to Actions tab
2. **Verify Actions List**: Should show all actions from Actions.json on the left
3. **Verify Buttons**: Create and Delete buttons should be visible at the top right
4. **Select Action**: Click an action to see the edit form on the right
5. **Test Create**: Click Create to add a new action
6. **Test Delete**: Select an action and click Delete

## Expected Behavior

- **Actions List**: Shows all actions from Actions.json (including "TEST ACTION - ALL PARAMETERS")
- **Action Form**: Displays when an action is selected
- **Create Button**: Opens form to create new action
- **Delete Button**: Deletes selected action (with confirmation)
- **Status Messages**: Shows feedback for all operations

## Notes

- The Actions tab now properly handles both SpreadsheetActionJson and legacy ActionData formats
- When saving through the Settings UI, actions are saved in legacy ActionData format
- To use the optimized format (omitting empty fields), regenerate from spreadsheet using import scripts
- The test action "TEST ACTION - ALL PARAMETERS" should now be visible in the list

## Related Files

- `Code/Game/Editors/ActionEditor.cs` - Action editing logic
- `Code/Data/ActionLoader.cs` - Action loading with format detection
- `Code/UI/Avalonia/Settings/ActionsSettingsPanel.axaml` - UI layout
- `Code/UI/Avalonia/Managers/ActionsTabManager.cs` - Tab management logic
