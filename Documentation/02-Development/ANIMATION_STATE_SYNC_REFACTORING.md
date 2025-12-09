# Animation State Synchronization Refactoring

**Date**: Current  
**Status**: ✅ Complete  
**Issue**: Recurring flickering between dungeon selection and combat screens

---

## Problem

The dungeon selection animation continued rendering even after the game transitioned to combat/dungeon states, causing visual flickering. This was a recurring issue that indicated a design problem rather than a one-off bug.

### Root Cause

1. **No State Awareness**: The `CanvasAnimationManager` had no knowledge of game state changes
2. **Polling Instead of Events**: Animation manager used polling/checking rather than being notified of state changes
3. **Tight Coupling**: Animation system was decoupled from game state, requiring manual coordination

---

## Solution: Event-Driven Architecture

Refactored to use the **Observer Pattern** with state change events, making the system reactive rather than polling-based.

### Changes Made

#### 1. **GameStateManager** - Added State Change Events
- Added `StateChanged` event that fires when `TransitionToState()` is called
- Created `StateChangedEventArgs` to pass previous and new state information
- Allows any system to subscribe and react to state changes

```csharp
public event EventHandler<StateChangedEventArgs>? StateChanged;

public bool TransitionToState(GameState newState)
{
    if (ValidateStateTransition(currentState, newState))
    {
        var previousState = currentState;
        CurrentState = newState;
        
        // Fire state change event to notify subscribers
        StateChanged?.Invoke(this, new StateChangedEventArgs(previousState, newState));
        
        return true;
    }
    // ...
}
```

#### 2. **CanvasAnimationManager** - Event Subscription
- Subscribes to `GameStateManager.StateChanged` event
- Automatically stops animation when leaving `DungeonSelection` state
- Removed polling/checking approach
- Added proper cleanup in `Dispose()` to unsubscribe

```csharp
private void OnStateChanged(object? sender, StateChangedEventArgs e)
{
    // If we're leaving dungeon selection state, stop the animation
    if (e.PreviousState == GameState.DungeonSelection && 
        e.NewState != GameState.DungeonSelection)
    {
        StopDungeonSelectionAnimation();
    }
}
```

#### 3. **CanvasUICoordinator** - State Manager Wiring
- Added `SetStateManager()` method to wire up state manager after Game initialization
- Called automatically from `Game.SetUIManager()`

#### 4. **Game** - Automatic Wiring
- `SetUIManager()` now automatically wires state manager to UI coordinator
- Ensures animation system is connected when Game is initialized

---

## Benefits

### 1. **Prevents Recurring Issues**
- Animation automatically stops when state changes
- No manual coordination required
- Eliminates race conditions

### 2. **Better Architecture**
- **Event-Driven**: Systems react to changes rather than polling
- **Loose Coupling**: Animation manager doesn't need direct state access
- **Single Source of Truth**: GameStateManager is the authoritative state source

### 3. **Extensibility**
- Other systems can easily subscribe to state changes
- No need to modify GameStateManager for each new subscriber
- Follows Open/Closed Principle

### 4. **Maintainability**
- Clear separation of concerns
- Easy to understand flow: State changes → Event fires → Systems react
- Less code duplication

---

## Architecture Pattern

**Before (Polling)**:
```
Animation Timer → Check flag → Check state function → Render
```

**After (Event-Driven)**:
```
State Change → Event Fires → Animation Manager Reacts → Stops Animation
```

---

## Testing

The refactoring should be tested by:
1. Selecting a dungeon and verifying animation stops immediately
2. Transitioning between states and confirming no flickering
3. Verifying animation resumes correctly when returning to dungeon selection

---

## Future Improvements

This event system can be extended for:
- UI transitions (fade in/out on state changes)
- Sound system (play different music per state)
- Save system (auto-save on state transitions)
- Analytics (track state transition patterns)

---

## Files Modified

1. `Code/Game/GameStateManager.cs` - Added events
2. `Code/UI/Avalonia/Managers/CanvasAnimationManager.cs` - Event subscription
3. `Code/UI/Avalonia/CanvasUICoordinator.cs` - State manager wiring
4. `Code/Game/Game.cs` - Automatic wiring in SetUIManager
5. `Code/UI/Avalonia/Managers/ICanvasAnimationManager.cs` - Interface update

---

## Related Issues

- Previous flickering issues between dungeon selection and combat
- State synchronization problems in UI systems
- Need for better separation between game logic and UI rendering

