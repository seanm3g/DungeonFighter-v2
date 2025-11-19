# Dungeon Selection Wrong Screen Debug Guide

## Issue
When selecting dungeon 2, the game goes to inventory screen instead of the dungeon.

## Debug Logging Added

### 1. Game.cs - Event Subscription Initialization
```csharp
DebugLogger.Log("Game", "StartDungeonEvent subscribed to DungeonRunnerManager.RunDungeon()");
DebugLogger.Log("Game", "StartDungeonEvent fired - calling DungeonRunnerManager.RunDungeon()");
```

**What this tells us:**
- Whether the event was properly subscribed during initialization
- Whether the event is actually being fired when dungeon is selected

### 2. DungeonSelectionHandler - Event Firing
```csharp
DebugLogger.Log("DungeonSelectionHandler", $"Set current dungeon: {stateManager.CurrentDungeon?.Name ?? "null"}");
DebugLogger.Log("DungeonSelectionHandler", "Dungeon generated");
DebugLogger.Log("DungeonSelectionHandler", "Firing StartDungeonEvent");
DebugLogger.Log("DungeonSelectionHandler", "ERROR: StartDungeonEvent is null!");
DebugLogger.Log("DungeonSelectionHandler", "ERROR: CurrentDungeon is null after SetCurrentDungeon!");
DebugLogger.Log("DungeonSelectionHandler", "ERROR: CurrentPlayer is null in SelectDungeon!");
```

**What this tells us:**
- Whether the dungeon is being set correctly
- Whether the dungeon is being generated
- Whether the event is null (not subscribed)
- Whether CurrentPlayer/Dungeon are null

### 3. DungeonRunnerManager - Dungeon Start
```csharp
DebugLogger.Log("DungeonRunnerManager", "RunDungeon called");
DebugLogger.Log("DungeonRunnerManager", $"CurrentPlayer: {stateManager.CurrentPlayer?.Name ?? "null"}");
DebugLogger.Log("DungeonRunnerManager", $"CurrentDungeon: {stateManager.CurrentDungeon?.Name ?? "null"}");
DebugLogger.Log("DungeonRunnerManager", $"CombatManager: {(combatManager != null ? "initialized" : "null")}");
DebugLogger.Log("DungeonRunnerManager", "ERROR: Cannot run dungeon - missing required components");
DebugLogger.Log("DungeonRunnerManager", "Transitioning to Dungeon state");
```

**What this tells us:**
- Whether RunDungeon() is actually being called
- Whether all required components are initialized
- Whether state is transitioning to Dungeon

## Testing Steps

1. **Run the game**
2. **Navigate to dungeon selection** (Main Menu → New Game → Weapon → Character Creation → Start Adventure)
3. **Select dungeon 2**
4. **Close the game or force close it**
5. **Find debug log** in `Code/DebugAnalysis/debug_analysis_YYYY-MM-DD_HH-mm-ss.txt`

## Expected Debug Output Sequence

### Success Case:
```
DEBUG [Game]: StartDungeonEvent subscribed to DungeonRunnerManager.RunDungeon()
DEBUG [Game]: HandleInput: input='2', state=DungeonSelection
DEBUG [Game]: Routing to DungeonSelectionHandler.HandleMenuInput('2')
DEBUG [DungeonSelectionHandler]: Valid dungeon selection: 2
DEBUG [DungeonSelectionHandler]: Set current dungeon: Bandit Hideout
DEBUG [DungeonSelectionHandler]: Dungeon generated
DEBUG [DungeonSelectionHandler]: Firing StartDungeonEvent
DEBUG [Game]: StartDungeonEvent fired - calling DungeonRunnerManager.RunDungeon()
DEBUG [DungeonRunnerManager]: RunDungeon called
DEBUG [DungeonRunnerManager]: CurrentPlayer: {character name}
DEBUG [DungeonRunnerManager]: CurrentDungeon: Bandit Hideout
DEBUG [DungeonRunnerManager]: CombatManager: initialized
DEBUG [DungeonRunnerManager]: Transitioning to Dungeon state
```

## Failure Cases to Look For

### Case 1: Event Not Subscribed
```
// Missing: "StartDungeonEvent subscribed..."
// Cause: dungeonRunnerManager is null during initialization
```

### Case 2: Event Not Fired
```
// Missing: "Firing StartDungeonEvent"
// Cause: StartDungeonEvent is null or CurrentDungeon is null
```

### Case 3: RunDungeon Not Called
```
// Missing: "RunDungeon called"
// Cause: Event wasn't properly subscribed
```

### Case 4: Missing Components
```
DEBUG [DungeonRunnerManager]: ERROR: Cannot run dungeon - missing required components
// Cause: CurrentPlayer, CurrentDungeon, or CombatManager is null
```

### Case 5: Wrong Input Path
```
// Shows input going to different handler
// Could mean state machine is in wrong state
```

## After Providing Debug Output

Once you share the debug log, we can pinpoint:
1. Is the event properly subscribed?
2. Is the event being fired?
3. Is RunDungeon being called?
4. Are required components initialized?
5. Is the state transitioning to Dungeon?

This will help us identify exactly why you're ending up in Inventory instead of the dungeon.

