# Game.cs Refactoring Summary

## Problem Statement

**Current Situation:**
- `Game.cs` is 1,400 lines
- Handles state management, input, UI display, dungeon progression, initialization, and logging
- Difficult to test, maintain, and extend
- High cognitive complexity

**Target State:**
- `Game.cs` reduced to ~400-500 lines
- Responsibilities distributed among specialized managers
- Clear separation of concerns
- Easier to test and extend

## High-Level Refactoring Strategy

### Current Structure
```
Game (1,400 lines) handles:
├── State Management (~200 lines)
├── Input Handling (~300 lines)  
├── UI Display (~150 lines)
├── Dungeon Progression (~200 lines)
├── Initialization (~150 lines)
├── Utilities (~100 lines)
└── Logging/Narrative (~100 lines)
```

### Target Structure
```
Game (400-500 lines) coordinates:
├── GameStateManager (150 lines)
│   ├── Current state
│   ├── Player/inventory/dungeons
│   ├── State transitions
│   └── State validation
│
├── GameInputHandler (100 lines)
│   ├── Input routing
│   ├── State-specific handlers
│   └── Command processing
│
├── DungeonProgressionManager (300 lines)
│   ├── Dungeon progression
│   ├── Room management
│   ├── Combat orchestration
│   └── Reward handling
│
├── GameNarrativeManager (80 lines)
│   ├── Logging
│   ├── Message formatting
│   └── Event tracking
│
└── GameInitializationManager (100 lines)
    ├── New game setup
    ├── Character creation
    ├── Game loading
    └── Configuration
```

## Step-by-Step Refactoring Plan

### Step 1: Extract State Management
**File**: `Code/Game/GameStateManager.cs`

```csharp
public class GameStateManager
{
    private GameState currentState = GameState.MainMenu;
    private Character? currentPlayer;
    private List<Item> currentInventory = new();
    private List<Dungeon> availableDungeons = new();
    private Dungeon? currentDungeon;
    private Environment? currentRoom;
    
    // Properties
    public GameState CurrentState => currentState;
    public Character? CurrentPlayer => currentPlayer;
    public Dungeon? CurrentDungeon => currentDungeon;
    
    // Methods
    public void TransitionToState(GameState newState)
    public void SetCurrentPlayer(Character? player)
    public void SetCurrentDungeon(Dungeon? dungeon)
    public void SetCurrentRoom(Environment? room)
    public List<Dungeon> GetAvailableDungeons()
    public List<Item> GetInventory()
    public bool ValidateStateTransition(GameState from, GameState to)
}
```

**What Gets Moved**:
- All state fields from Game.cs
- State validation logic
- State transition methods

### Step 2: Extract Input Handling
**File**: `Code/Game/GameInputHandler.cs`

```csharp
public class GameInputHandler
{
    private Game gameInstance;
    private IUIManager? uiManager;
    
    public GameInputHandler(Game game, IUIManager? uiManager)
    
    // Main routing method
    public async Task HandleInput(string input, GameState currentState)
    
    // State-specific handlers
    private void HandleMainMenuInput(string input)
    private async Task HandleWeaponSelectionInput(string input)
    private async Task HandleCharacterCreationInput(string input)
    private void HandleInventoryInput(string input)
    private async Task HandleGameLoopInput(string input)
    private async Task HandleDungeonSelectionInput(string input)
    // ... etc for each state
}
```

**What Gets Moved**:
- All `HandleXxxInput()` methods from Game.cs
- Input validation logic
- Input routing/dispatching

### Step 3: Extract Narrative/Logging
**File**: `Code/Game/GameNarrativeManager.cs`

```csharp
public class GameNarrativeManager
{
    private List<string> dungeonLog = new();
    private List<string> dungeonHeaderInfo = new();
    private List<string> currentRoomInfo = new();
    
    public void LogEvent(string message)
    public void SetRoomInfo(List<string> info)
    public List<string> GetDungeonLog()
    public string FormatEncounterResult(Enemy enemy, bool playerWon)
    public void ClearLog()
}
```

**What Gets Moved**:
- All logging fields from Game.cs
- Narrative construction methods
- Message formatting logic

### Step 4: Extract Initialization
**File**: `Code/Game/GameInitializationManager.cs`

```csharp
public class GameInitializationManager
{
    private GameInitializer gameInitializer = new();
    
    public async Task<Character?> InitializeNewGame(string weapon, string playerClass)
    public async Task<Character?> InitializeLoadedGame()
    public void CreateCharacter(string weapon, string playerClass)
    public Task InitializeGameManagers(Character character, List<Dungeon> dungeons)
}
```

**What Gets Moved**:
- `StartNewGame()`, `LoadGame()`, `CreateNewCharacter()` methods
- Character initialization logic
- Configuration loading

### Step 5: Extract Dungeon Progression
**File**: Already exists as `DungeonProgressionManager`

**What Needs Consolidation**:
- Move `HandleDungeonProgression()` from Game.cs
- Move `RunRoom()` method
- Move `ProcessRoomEncounter()` method
- Consolidate dungeon-related logic

### Step 6: Refactored Game.cs
**Reduced to ~450 lines**

```csharp
public class Game
{
    private GameStateManager stateManager;
    private GameInputHandler inputHandler;
    private DungeonProgressionManager dungeonManager;
    private GameNarrativeManager narrativeManager;
    private GameInitializationManager initializationManager;
    
    private GameMenuManager menuManager;
    private IUIManager? customUIManager;
    
    public Game() { /* Initialize all managers */ }
    public Game(IUIManager uiManager) { /* ... */ }
    public Game(Character existingCharacter) { /* ... */ }
    
    // Public API (Coordinator methods)
    public async void ShowMainMenu()
    public async Task HandleInput(string input)
    public Task HandleEscapeKey()
    public void SetUIManager(IUIManager uiManager)
    
    // State accessors (delegate to stateManager)
    public Character? CurrentPlayer => stateManager.CurrentPlayer;
    public GameState CurrentState => stateManager.CurrentState;
    
    // Display methods (delegate to menuManager/customUIManager)
    private void ShowMainMenuWithCustomUI()
    private void ShowGameLoopMenu()
    private void ShowSettings()
    
    // Static utility methods (can move to GameConstants)
    public static Dictionary<string, List<string>> GetThemeSpecificRooms()
    public static DungeonGenerationConfig GetDungeonGenerationConfig()
}
```

## Line Count Summary

| Component | Before | After | Reduction |
|-----------|--------|-------|-----------|
| Game.cs | 1,400 | 450 | 68% ↓ |
| GameStateManager | - | 150 | New |
| GameInputHandler | - | 100 | New |
| GameNarrativeManager | - | 80 | New |
| GameInitializationManager | - | 100 | New |
| DungeonProgressionManager (updated) | 300* | 350 | +50 |
| **Total Organized Code** | **1,400** | **1,230** | **12% ↓** |

*Existing manager code

## Testing Strategy

### 1. Unit Tests for Each Manager
```
GameStateManagerTests
├── TestStateTransition
├── TestStateValidation
└── TestGetters

GameInputHandlerTests
├── TestMainMenuInput
├── TestGameLoopInput
└── TestInvalidInput

GameNarrativeManagerTests
├── TestLogging
├── TestMessageFormatting
└── TestLogClearing

GameInitializationManagerTests
├── TestNewGameInitialization
├── TestLoadedGameInitialization
└── TestCharacterCreation
```

### 2. Integration Tests
```
GameIntegrationTests
├── TestCompleteGameFlow
├── TestStateTransitions
├── TestInputHandling
└── TestNoRegressions
```

### 3. Regression Testing
- Run all existing tests
- Verify no behavior changes
- Test edge cases
- Test error handling

## Implementation Checklist

- [ ] **Phase 1: Create Managers**
  - [ ] Create GameStateManager
  - [ ] Create GameNarrativeManager
  - [ ] Create GameInitializationManager
  - [ ] Test each manager in isolation

- [ ] **Phase 2: Extract Input**
  - [ ] Create GameInputHandler
  - [ ] Move all input methods
  - [ ] Update Game.cs to delegate
  - [ ] Test input handling

- [ ] **Phase 3: Consolidate**
  - [ ] Refactor DungeonProgressionManager
  - [ ] Update Game.cs to use all managers
  - [ ] Remove extracted code from Game.cs
  - [ ] Test complete flow

- [ ] **Phase 4: Polish**
  - [ ] Update ARCHITECTURE.md
  - [ ] Create manager documentation
  - [ ] Update CODE_PATTERNS.md
  - [ ] Final testing and QA

## Risk Assessment

### Low Risk Areas
- State management extraction (well-defined)
- Narrative logging (isolated logic)
- Initialization (clear boundaries)

### Medium Risk Areas
- Input handling (touches all states)
- Requires thorough testing

### High Risk Areas
- Dungeon progression (complex logic)
- Requires integration testing
- Most likely to have regressions

## Success Metrics

✅ **When Complete, Verify:**
1. Game.cs is <600 lines (target 450)
2. All managers have <400 lines each
3. No functionality lost
4. All existing tests pass
5. New tests for managers (>80% coverage)
6. No performance regression
7. Documentation updated
8. Code is cleaner and easier to understand

## Related Architecture Documents

- See `ARCHITECTURE.md` for system overview
- See `CODE_PATTERNS.md` for design patterns used
- See `GAME_REFACTORING_PLAN.md` for detailed planning

