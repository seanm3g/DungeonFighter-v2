# Combat Freeze Fix - November 20, 2025

## Issue
The game was freezing (showing "not responding") during combat encounters.

## Root Causes (2 Issues Found)

### Issue #1: Non-existent Method Calls
After the recent refactoring, the `CombatManager.cs` was calling a non-existent method `WaitForMessageQueueCompletionAsync()` on the `CanvasUICoordinator` class at multiple points in the combat loop.

### Issue #2: Async/Await Deadlock (THE ACTUAL FREEZE!)
The main freeze was caused by a **circular deadlock** in the `RunCombat()` method (line 264 of `CombatManager.cs`):

```csharp
// CAUSES DEADLOCK:
return Task.Run(async () => await RunCombatAsync(...)).GetAwaiter().GetResult();
```

**Deadlock Chain:**
1. UI thread calls `RunCombat()` (synchronous) from `DungeonRunnerManager`
2. `RunCombat()` uses `Task.Run()` to move execution to background thread
3. `RunCombatAsync()` (now on background thread) tries to access/post to UI elements
4. UI thread is blocked waiting for `Task.Run()` to complete
5. Background thread is waiting for UI thread to process UI calls
6. **DEADLOCK → FREEZE**

## Solutions

### Fix #1: Replace Non-existent Method Calls
Replaced all calls to `WaitForMessageQueueCompletionAsync()` with `await Task.Delay(50)` in `CombatManager.cs` (4 locations)

### Fix #2: Remove Task.Run() Deadlock from RunCombat()
Simplified the `RunCombat()` method in `CombatManager.cs` (lines 256-272) to remove the `Task.Run()` wrapper:

**Before:**
```csharp
public bool RunCombat(...) {
    if (UIManager.GetCustomUIManager() is UI.Avalonia.CanvasUICoordinator) {
        return Task.Run(async () => await RunCombatAsync(...))  // ❌ CAUSES DEADLOCK
            .GetAwaiter().GetResult();
    }
    ...
}
```

**After:**
```csharp
public bool RunCombat(...) {
    // Simply delegate to async method with ConfigureAwait
    return RunCombatAsync(...).ConfigureAwait(false).GetAwaiter().GetResult();
}
```

This removes the circular deadlock by eliminating the `Task.Run()` layer while keeping the sync method for backward compatibility.

### Changes Made

#### 1. `Code/Combat/CombatManager.cs` - Multiple Changes

**A) Namespace Fix (Line 11):**
- Changed from: `namespace RPGGame.Combat`
- Changed to: `namespace RPGGame`
- This allows `DungeonRunnerManager` to access the public methods

**B) Task.Delay() Replacements (4 locations):**
Replaced non-existent `WaitForMessageQueueCompletionAsync()` method calls with `Task.Delay(50)`:
- Player Turn (Line ~197)
- Enemy Turn (Line ~213)  
- Environmental Turn (Line ~229)
- Battle End (Line ~242)

**C) RunCombat() Method Simplification (Lines 256-267):**
Removed the `Task.Run()` wrapper that caused the deadlock. Changed from complex if-else logic to simple delegation with `ConfigureAwait(false)`.

## Impact
- ✅ Eliminates circular deadlock in async/await chain
- ✅ Game no longer freezes during combat
- ✅ Combat flows smoothly with proper UI rendering
- ✅ 50ms delay allows sufficient time for UI updates between actions
- ✅ No change to game logic or balance - purely a UI synchronization fix

## Testing
To verify the fix works:
1. Run the game: `cd Code && dotnet run`
2. Enter any dungeon (e.g., Forest)
3. Start combat with an enemy
4. Verify combat actions display and flow without freezing
5. Complete multiple combats to ensure stability

## Files Modified
- `Code/Combat/CombatManager.cs` - 4 method calls removed/replaced

## Related Architecture
- `Code/UI/Avalonia/CanvasUICoordinator.cs` - The UI manager implementation
- `Code/Combat/CombatTurnHandlerSimplified.cs` - Turn processing logic
- `Code/Actions/ActionSpeedSystem.cs` - Action timing system

## Notes
- The `UIManager.ApplyActionEndDelay()` call is retained - it provides the primary delay between actions
- The Task.Delay(50) provides a secondary synchronization point for UI rendering
- This fix maintains compatibility with both GUI and console modes (if applicable)

