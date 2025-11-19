# Game.cs Refactoring Plan

## Executive Summary

**Current Status**: Game.cs is 1,400 lines and acts as a monolithic orchestrator for all game operations.

**Goal**: Reduce Game.cs to ~400-500 lines by extracting core responsibilities into specialized managers using the Manager and Coordinator patterns.

**Expected Outcome**: 
- Better separation of concerns
- Easier testing and maintenance
- Clearer code organization
- Reduced cognitive complexity

## Current Architecture Analysis

### Game.cs Responsibilities (Current)

1. **State Management** (~200 lines)
   - Manages `currentState` (GameState enum)
   - Tracks `currentPlayer`, `currentInventory`, `availableDungeons`
   - Tracks `currentDungeon`, `currentRoom`, `dungeonLog`

2. **Input Handling** (~300 lines)
   - `HandleInput()` - Main input router
   - `HandleMainMenuInput()`, `HandleWeaponSelectionInput()`, etc.
   - One method per UI state

3. **UI Display/Rendering** (~150 lines)
   - `ShowMainMenu()`, `ShowGameLoopMenu()`, `ShowSettings()`
   - Delegates to UIManager or custom UI

4. **Dungeon Progression** (~200 lines)
   - `HandleDungeonProgression()`, `RunRoom()`, `ProcessRoomEncounter()`
   - Room completion, reward handling, state advancement

5. **Initialization Logic** (~150 lines)
   - `StartNewGame()`, `LoadGame()`, `CreateNewCharacter()`
   - Game configuration and setup

6. **Utility Methods** (~100 lines)
   - `GetThemeSpecificRooms()`, `GetDungeonGenerationConfig()`
   - Game constants and configuration

7. **Logging/Narrative** (~100 lines)
   - `dungeonLog`, `dungeonHeaderInfo`, `currentRoomInfo` management
   - Narrative construction and display

## Proposed Refactoring

### Manager Classes to Create

#### 1. GameStateManager
**Purpose**: Centralize all game state management
**Responsibilities**:
- Track current game state
- Provide state transition methods
- Manage player, inventory, dungeons, current room/dungeon

**Lines**: ~150
**Methods**:
- `TransitionToState(newState)`
- `SetCurrentPlayer(character)`
- `SetCurrentDungeon(dungeon)`
- `SetCurrentRoom(room)`
- `GetCurrentPlayer()`, `GetCurrentState()`, etc.

#### 2. GameInputHandler
**Purpose**: Centralize input handling logic
**Responsibilities**:
- Route input based on current state
- Delegate to appropriate input handlers
- Manage state-specific input processing

**Lines**: ~100
**Methods**:
- `HandleInput(input, currentState)`
- `HandleMainMenuInput(input)`
- `HandleInventoryInput(input)`
- `HandleGameLoopInput(input)`
- etc.

#### 3. DungeonProgressionManager
**Purpose**: Manage dungeon exploration and progression
**Responsibilities**:
- Handle room progression
- Manage dungeon encounters and combat
- Handle room completion and rewards
- Track dungeon progress

**Lines**: ~300
**Methods**:
- `StartDungeon(dungeon, character)`
- `GetNextRoom()`
- `CompleteCurrentRoom()`
- `CompleteDungeon()`
- `ProcessRoomEncounter()`

#### 4. GameNarrativeManager
**Purpose**: Manage game narrative output and logging
**Responsibilities**:
- Build dungeon log
- Track room information
- Format narrative output
- Manage message display

**Lines**: ~80
**Methods**:
- `LogDungeonEvent(message)`
- `SetRoomInfo(info)`
- `GetDungeonLog()`
- `FormatRoomCompletion(room, result)`

#### 5. GameInitializationManager
**Purpose**: Handle all game initialization
**Responsibilities**:
- New game creation
- Game loading
- Character initialization
- Dungeon generation

**Lines**: ~100
**Methods**:
- `InitializeNewGame()`
- `InitializeLoadedGame(character)`
- `CreateCharacter(weapon, class)`

### Refactored Game.cs Structure

**New Game.cs Responsibilities** (~350 lines):
- Coordinate between managers
- Handle state transitions
- Delegate to appropriate managers
- Provide public API for UI layer

```csharp
public class Game
{
    private GameStateManager stateManager;
    private GameInputHandler inputHandler;
    private DungeonProgressionManager dungeonManager;
    private GameNarrativeManager narrativeManager;
    private GameInitializationManager initializationManager;
    private IUIManager? customUIManager;
    
    // Public API
    public async Task HandleInput(string input)
    public async Task HandleEscapeKey()
    public void ShowMainMenu()
    public void SetUIManager(IUIManager uiManager)
    
    // State accessors
    public Character? GetCurrentPlayer() => stateManager.GetCurrentPlayer();
    public GameState GetCurrentState() => stateManager.GetCurrentState();
    
    // State transitions
    private void TransitionToState(GameState state)
    
    // Display methods (delegated)
    private void ShowMainMenuWithCustomUI()
    private void ShowGameLoopMenu()
    private void ShowSettings()
}
```

## Migration Strategy

### Phase 1: Create Managers (Minimal Impact)
1. Create `GameStateManager` - Extract state management
2. Create `GameNarrativeManager` - Extract logging logic
3. Create `GameInitializationManager` - Extract initialization
4. **No changes to Game.cs** - Just create new classes

### Phase 2: Extract Input Handling
1. Create `GameInputHandler`
2. Extract input methods from Game.cs
3. Update Game.cs to delegate to handler
4. **Backward compatible** - Same behavior, different organization

### Phase 3: Extract Dungeon Progression
1. Create `DungeonProgressionManager`
2. Extract dungeon-related methods
3. Update Game.cs to delegate
4. **Most complex phase** - Requires careful testing

### Phase 4: Consolidate Game.cs
1. Update Game.cs to use all managers
2. Remove delegated methods
3. Test thoroughly
4. Update ARCHITECTURE.md

## Benefits

### Code Quality
- **Single Responsibility**: Each manager handles one concern
- **Testability**: Each manager can be unit tested independently
- **Maintainability**: Changes to one system don't affect others
- **Reusability**: Managers can be used in different contexts

### Performance
- **No performance impact** - Just reorganization
- **Lazy initialization** - Managers only created when needed
- **Composition over inheritance** - Flexibility for future changes

### Development
- **Easier debugging** - Can test managers in isolation
- **Clear dependencies** - Explicit manager relationships
- **Modular testing** - Test individual managers without full game
- **Future extensibility** - Easy to add new managers

## Testing Strategy

### Unit Tests
- Test each manager independently
- Mock dependencies
- Verify state transitions
- Test input handling

### Integration Tests
- Test manager interactions
- Test full game flow
- Test state transitions
- Verify no regressions

### Coverage
- Aim for 80%+ code coverage
- Focus on critical paths
- Test edge cases
- Verify error handling

## Timeline Estimate

| Phase | Tasks | Estimate |
|-------|-------|----------|
| 1 | Create 3 managers | 2-3 hours |
| 2 | Extract input handling | 1-2 hours |
| 3 | Extract dungeon progression | 3-4 hours |
| 4 | Consolidate & test | 2-3 hours |
| **Total** | | **8-12 hours** |

## Success Criteria

- [ ] Game.cs reduced from 1,400 to <600 lines (50%+ reduction)
- [ ] All managers created with clear responsibilities
- [ ] No functionality lost or changed
- [ ] All existing tests pass
- [ ] New unit tests for managers (>80% coverage)
- [ ] ARCHITECTURE.md updated
- [ ] Documentation created for each manager
- [ ] No performance regression

## Related Documentation

- **ARCHITECTURE.md**: Overall system architecture
- **CODE_PATTERNS.md**: Design patterns and conventions
- **DEVELOPMENT_WORKFLOW.md**: Development process
- **QUICK_REFERENCE.md**: Manager reference guide

## Notes

This refactoring follows established architectural patterns:
- **Manager Pattern**: Specialized managers for specific concerns
- **Composition Pattern**: Game composes managers instead of inheriting behavior
- **Coordinator Pattern**: Game coordinates between managers
- **Single Responsibility Principle**: Each manager has one reason to change

The refactoring is low-risk because:
1. No public API changes
2. Existing functionality preserved
3. Changes are internal reorganization only
4. Can be tested incrementally

