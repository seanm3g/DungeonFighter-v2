# Inventory Button Fix

## Problem
Most inventory buttons (Unequip, Discard, Manage Combos) were not working correctly. Only the Equip button seemed to work.

### Root Cause
When buttons like "Unequip Item" or "Discard Item" were clicked, the game would:
1. Set a waiting flag (`waitingForItemSelection` or `waitingForSlotSelection`)
2. Show a message asking for user input
3. **But keep the same inventory screen visible with all the old buttons still clickable**

When the user clicked another button (like "Equip Item" with value "1"), the game interpreted that as item/slot selection (e.g., unequip slot 1) instead of a menu action, because the waiting flag was still active.

## Solution
Created proper state-based rendering for item and slot selection:

### Files Modified

#### 1. `Code/UI/Avalonia/Renderers/InventoryRenderer.cs`
Added two new rendering methods:

- **`RenderItemSelectionPrompt()`** - Shows a list of inventory items as clickable buttons for equip/discard actions
  - Displays each item with its stats
  - Only shows item buttons + cancel button
  - Hides the main inventory action buttons
  
- **`RenderSlotSelectionPrompt()`** - Shows equipment slots as clickable buttons for unequip action
  - Displays all 4 equipment slots (Weapon, Head, Body, Feet)
  - Shows what's currently equipped in each slot
  - Only shows slot buttons + cancel button
  - Hides the main inventory action buttons

#### 2. `Code/UI/Avalonia/CanvasUIManager.cs`
Added two new public methods to call the renderer methods:

- **`RenderItemSelectionPrompt()`** - Wraps the inventory renderer's item selection
- **`RenderSlotSelectionPrompt()`** - Wraps the inventory renderer's slot selection

#### 3. `Code/Game/Game.cs`
Updated the prompt methods to render proper selection screens:

- **`PromptEquipItem()`** - Now calls `RenderItemSelectionPrompt()` instead of just showing a message
- **`PromptUnequipItem()`** - Now calls `RenderSlotSelectionPrompt()` instead of just showing a message
- **`PromptDiscardItem()`** - Now calls `RenderItemSelectionPrompt()` instead of just showing a message

## How It Works Now

### Equipping Items
1. User clicks "Equip Item" button
2. Screen changes to show **only** inventory items as clickable buttons
3. User clicks the item they want to equip
4. Item is equipped and screen returns to normal inventory

### Unequipping Items
1. User clicks "Unequip Item" button
2. Screen changes to show **only** the 4 equipment slots as clickable buttons
3. User clicks the slot they want to unequip (e.g., "Weapon: Iron Sword")
4. Item is unequipped and screen returns to normal inventory

### Discarding Items
1. User clicks "Discard Item" button
2. Screen changes to show **only** inventory items as clickable buttons
3. User clicks the item they want to discard
4. Item is discarded and screen returns to normal inventory

### Cancel Option
All selection screens include a "[0] Cancel" button that returns to the normal inventory without making changes.

## Benefits
- **Clear User Interface** - Users see only the relevant options for each action
- **No Button Confusion** - Old buttons are hidden during selection, preventing accidental wrong actions
- **Better Visual Feedback** - Dedicated screens for each type of selection
- **Consistent Behavior** - All inventory actions now work the same way

## Testing
To test the fix:
1. Close the running game
2. Build the project: `dotnet build`
3. Run the game
4. Navigate to inventory
5. Test each button:
   - [1] Equip Item - should show item selection screen
   - [2] Unequip Item - should show slot selection screen
   - [3] Discard Item - should show item selection screen
   - [4] Manage Combo Actions - shows "Coming soon" message (placeholder)
   - [5] Continue to Dungeon - navigates to dungeon selection
   - [6] Return to Main Menu - returns to main menu
   - [0] Exit Game - exits the game

All buttons should now work correctly!

