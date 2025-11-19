# Game.cs Refactoring Implementation Guide

## Overview

This guide provides a detailed implementation path for refactoring `Game.cs` from 1,400 lines into a coordinated set of specialized managers.

**Duration**: 8-12 hours of focused development
**Complexity**: High (complex interdependencies)
**Risk Level**: Medium (requires thorough testing)

## Phase 1: Create GameStateManager (2-3 hours)

### Purpose
Centralize all game state management and provide a clean API for state operations.

### Create File: `Code/Game/GameStateManager.cs`

```csharp
using System;
using System.Collections.Generic;

namespace RPGGame
{
    public class GameStateManager
    {
        // State fields
        private GameState currentState = GameState.MainMenu;
        private Character? currentPlayer;
        private List<Item> currentInventory = new();
        private List<Dungeon> availableDungeons = new();
        private Dungeon? currentDungeon;
        private Environment? currentRoom;
        
        // Properties
        public GameState CurrentState
        {
            get => currentState;
            private set => currentState = value;
        }
        
        public Character? CurrentPlayer
        {
            get => currentPlayer;
            private set => currentPlayer = value;
        }
        
        public List<Item> CurrentInventory
        {
            get => currentInventory;
            private set => currentInventory = value;
        }
        
        public List<Dungeon> AvailableDungeons
        {
            get => availableDungeons;
            private set => availableDungeons = value;
        }
        
        public Dungeon? CurrentDungeon
        {
            get => currentDungeon;
            private set => currentDungeon = value;
        }
        
        public Environment? CurrentRoom
        {
            get => currentRoom;
            private set => currentRoom = value;
        }
        
        // State management methods
        public void TransitionToState(GameState newState)
        {
            if (ValidateStateTransition(currentState, newState))
            {
                CurrentState = newState;
            }
        }
        
        public bool ValidateStateTransition(GameState from, GameState to)
        {
            // Define valid transitions based on game logic
            return true; // Simplify for now - can add validation later
        }
        
        public void SetCurrentPlayer(Character? player)
        {
            CurrentPlayer = player;
            if (player != null && player.Inventory != null)
            {
                CurrentInventory = player.Inventory;
            }
        }
        
        public void SetCurrentDungeon(Dungeon? dungeon)
        {
            CurrentDungeon = dungeon;
            CurrentRoom = null; // Reset room when entering new dungeon
        }
        
        public void SetCurrentRoom(Environment? room)
        {
            CurrentRoom = room;
        }
        
        public void ResetGameState()
        {
            CurrentState = GameState.MainMenu;
            CurrentPlayer = null;
            CurrentInventory.Clear();
            AvailableDungeons.Clear();
            CurrentDungeon = null;
            CurrentRoom = null;
        }
        
        public bool HasPlayer => CurrentPlayer != null;
        public bool HasCurrentDungeon => CurrentDungeon != null;
        public bool HasCurrentRoom => CurrentRoom != null;
    }
}
```

### Integration into Game.cs
Replace all state fields with:
```csharp
private GameStateManager stateManager = new();
```

Replace property accesses with:
```csharp
// OLD: currentPlayer = null;
// NEW:
stateManager.SetCurrentPlayer(null);

// OLD: if (currentPlayer != null)
// NEW:
if (stateManager.HasPlayer)
```

### Testing
Create `Code/Tests/GameStateManagerTests.cs`:
- Test state transitions
- Test player assignment
- Test dungeon assignment
- Test room assignment
- Test state reset

---

## Phase 2: Create GameNarrativeManager (1-2 hours)

### Purpose
Manage all game logging, narrative construction, and event tracking.

### Create File: `Code/Game/GameNarrativeManager.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace RPGGame
{
    public class GameNarrativeManager
    {
        private List<string> dungeonLog = new();
        private List<string> dungeonHeaderInfo = new();
        private List<string> currentRoomInfo = new();
        
        // Properties
        public List<string> DungeonLog => new List<string>(dungeonLog);
        public List<string> DungeonHeaderInfo => new List<string>(dungeonHeaderInfo);
        public List<string> CurrentRoomInfo => new List<string>(currentRoomInfo);
        
        // Logging methods
        public void LogDungeonEvent(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                dungeonLog.Add(message);
            }
        }
        
        public void SetDungeonHeaderInfo(List<string> info)
        {
            dungeonHeaderInfo = new List<string>(info);
        }
        
        public void SetRoomInfo(List<string> info)
        {
            currentRoomInfo = new List<string>(info);
        }
        
        public void AddRoomInfo(string info)
        {
            currentRoomInfo.Add(info);
        }
        
        public void ClearDungeonLog()
        {
            dungeonLog.Clear();
        }
        
        public void ClearRoomInfo()
        {
            currentRoomInfo.Clear();
        }
        
        public string GetFormattedLog()
        {
            var sb = new StringBuilder();
            foreach (var entry in dungeonLog)
            {
                sb.AppendLine(entry);
            }
            return sb.ToString();
        }
        
        public void ResetNarrative()
        {
            ClearDungeonLog();
            ClearRoomInfo();
            dungeonHeaderInfo.Clear();
        }
    }
}
```

### Integration into Game.cs
Replace logging fields with:
```csharp
private GameNarrativeManager narrativeManager = new();
```

### Testing
Create `Code/Tests/GameNarrativeManagerTests.cs`:
- Test event logging
- Test info setting
- Test log clearing
- Test narrative reset

---

## Phase 3: Create GameInitializationManager (1-2 hours)

### Purpose
Handle all game initialization (new game, load game, character creation).

### Create File: `Code/Game/GameInitializationManager.cs`

```csharp
using System;
using System.Threading.Tasks;

namespace RPGGame
{
    public class GameInitializationManager
    {
        private GameInitializer gameInitializer = new();
        
        public async Task<Character?> CreateNewCharacter(string weapon, string playerClass)
        {
            try
            {
                var character = gameInitializer.CreateCharacterFromWeaponChoice(weapon, playerClass);
                return character;
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError($"Error creating character: {ex.Message}");
                return null;
            }
        }
        
        public async Task<Character?> LoadSavedCharacter()
        {
            try
            {
                var savedCharacter = await Task.Run(() => Character.LoadCharacter());
                return savedCharacter;
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError($"Error loading character: {ex.Message}");
                return null;
            }
        }
        
        public void InitializeGameData(Character character, System.Collections.Generic.List<Dungeon> dungeons)
        {
            gameInitializer.InitializeExistingGame(character, dungeons);
        }
        
        public void ApplyHealthMultiplier(Character character)
        {
            var settings = GameSettings.Instance;
            if (settings.PlayerHealthMultiplier != 1.0)
            {
                character.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
            }
        }
        
        public static DungeonGenerationConfig GetDungeonGenerationConfig()
        {
            return new DungeonGenerationConfig
            {
                minRooms = 2,
                roomCountScaling = 0.5,
                hostileRoomChance = 0.8,
                bossRoomName = "Boss"
            };
        }
        
        public static System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> GetThemeSpecificRooms()
        {
            return new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>
            {
                ["Forest"] = new System.Collections.Generic.List<string> { "Grove", "Thicket", "Canopy", "Meadow", "Wilderness", "Tree Hollow", "Sacred Grove" },
                ["Lava"] = new System.Collections.Generic.List<string> { "Magma Chamber", "Fire Pit", "Molten Pool", "Volcanic Vent", "Ash Field", "Ember Cave", "Inferno Hall" },
                // ... add all other themes
            };
        }
    }
}
```

### Integration into Game.cs
Replace initialization logic with:
```csharp
private GameInitializationManager initializationManager = new();
```

### Testing
Create `Code/Tests/GameInitializationManagerTests.cs`:
- Test character creation
- Test character loading
- Test game data initialization
- Test health multiplier application

---

## Phase 4: Create GameInputHandler (2-3 hours)

### Purpose
Centralize all input handling and routing.

### Create File: `Code/Game/GameInputHandler.cs`

```csharp
using System;
using System.Threading.Tasks;
using RPGGame.UI.Avalonia;

namespace RPGGame
{
    public class GameInputHandler
    {
        private Game gameInstance;
        private IUIManager? uiManager;
        private GameStateManager stateManager;
        
        public GameInputHandler(Game game, GameStateManager stateManager, IUIManager? uiManager = null)
        {
            gameInstance = game;
            this.stateManager = stateManager;
            this.uiManager = uiManager;
        }
        
        public async Task HandleInput(string input)
        {
            switch (stateManager.CurrentState)
            {
                case GameState.MainMenu:
                    HandleMainMenuInput(input);
                    break;
                case GameState.WeaponSelection:
                    await HandleWeaponSelectionInput(input);
                    break;
                case GameState.CharacterCreation:
                    await HandleCharacterCreationInput(input);
                    break;
                case GameState.Inventory:
                    HandleInventoryInput(input);
                    break;
                case GameState.CharacterInfo:
                    HandleCharacterInfoInput(input);
                    break;
                case GameState.Settings:
                    HandleSettingsInput(input);
                    break;
                case GameState.Testing:
                    HandleTestingInput(input);
                    break;
                case GameState.GameLoop:
                    await HandleGameLoopInput(input);
                    break;
                case GameState.DungeonSelection:
                    await HandleDungeonSelectionInput(input);
                    break;
                case GameState.DungeonCompletion:
                    await HandleDungeonCompletionInput(input);
                    break;
                default:
                    stateManager.TransitionToState(GameState.MainMenu);
                    break;
            }
        }
        
        private void HandleMainMenuInput(string input)
        {
            switch (input)
            {
                case "1":
                    gameInstance.StartNewGame();
                    break;
                case "2":
                    gameInstance.LoadGame();
                    break;
                case "3":
                    stateManager.TransitionToState(GameState.Settings);
                    gameInstance.ShowSettings();
                    break;
                case "0":
                    gameInstance.ExitGame();
                    break;
            }
        }
        
        private async Task HandleWeaponSelectionInput(string input)
        {
            // Delegate to game instance
            await gameInstance.HandleWeaponSelectionInput(input);
        }
        
        private async Task HandleCharacterCreationInput(string input)
        {
            // Delegate to game instance
            await gameInstance.HandleCharacterCreationInput(input);
        }
        
        private void HandleInventoryInput(string input)
        {
            gameInstance.HandleInventoryInput(input);
        }
        
        private void HandleCharacterInfoInput(string input)
        {
            gameInstance.HandleCharacterInfoInput(input);
        }
        
        private void HandleSettingsInput(string input)
        {
            gameInstance.HandleSettingsInput(input);
        }
        
        private void HandleTestingInput(string input)
        {
            gameInstance.HandleTestingInput(input);
        }
        
        private async Task HandleGameLoopInput(string input)
        {
            await gameInstance.HandleGameLoopInput(input);
        }
        
        private async Task HandleDungeonSelectionInput(string input)
        {
            await gameInstance.HandleDungeonSelectionInput(input);
        }
        
        private async Task HandleDungeonCompletionInput(string input)
        {
            await gameInstance.HandleDungeonCompletionInput(input);
        }
    }
}
```

### Integration into Game.cs
Replace input handling with:
```csharp
private GameInputHandler inputHandler;

// In constructor:
inputHandler = new GameInputHandler(this, stateManager, customUIManager);

// In HandleInput:
public async Task HandleInput(string input)
{
    await inputHandler.HandleInput(input);
}
```

### Testing
Create `Code/Tests/GameInputHandlerTests.cs`:
- Test input routing
- Test each state's input handling
- Test invalid input handling
- Test state transitions

---

## Phase 5: Update DungeonProgressionManager (2-3 hours)

### Purpose
Consolidate dungeon progression logic into a single manager.

### What to Move from Game.cs:
- `HandleDungeonProgression()` method
- `RunRoom()` method
- `ProcessRoomEncounter()` method
- All dungeon-related logic

### Update Existing: `Code/World/DungeonProgressionManager.cs`

This manager likely already exists. We need to:
1. Add methods for handling dungeon progression
2. Extract combat orchestration
3. Handle room completion and transitions
4. Manage reward distribution

---

## Phase 6: Refactor Game.cs (1-2 hours)

### Step 1: Add Manager Fields
```csharp
public class Game
{
    private GameStateManager stateManager = new();
    private GameInputHandler inputHandler;
    private DungeonProgressionManager dungeonProgressionManager;
    private GameNarrativeManager narrativeManager = new();
    private GameInitializationManager initializationManager = new();
    
    private GameMenuManager menuManager;
    private IUIManager? customUIManager;
    private GameInitializer gameInitializer;
    private GameLoopManager? gameLoopManager;
    private DungeonManagerWithRegistry? dungeonManager;
    private CombatManager? combatManager;
}
```

### Step 2: Update Constructors
```csharp
public Game()
{
    GameTicker.Instance.Start();
    menuManager = new GameMenuManager();
    gameInitializer = new GameInitializer();
    gameLoopManager = new GameLoopManager();
    dungeonManager = new DungeonManagerWithRegistry();
    combatManager = new CombatManager();
    
    inputHandler = new GameInputHandler(this, stateManager);
    dungeonProgressionManager = new DungeonProgressionManager();
}

public Game(IUIManager uiManager)
{
    // ... similar initialization ...
    inputHandler = new GameInputHandler(this, stateManager, uiManager);
}
```

### Step 3: Remove Extracted Methods
Delete these methods from Game.cs:
- `HandleMainMenuInput()`
- `HandleWeaponSelectionInput()`
- `HandleCharacterCreationInput()`
- `HandleInventoryInput()`
- `HandleCharacterInfoInput()`
- `HandleSettingsInput()`
- `HandleTestingInput()`
- `HandleGameLoopInput()`
- `HandleDungeonSelectionInput()`
- `HandleDungeonCompletionInput()`
- `LogDungeonEvent()` and other logging methods
- `StartNewGame()` (move to initialization manager)
- `LoadGame()` (move to initialization manager)
- `CreateNewCharacter()` (move to initialization manager)

### Step 4: Update HandleInput Method
```csharp
public async Task HandleInput(string input)
{
    await inputHandler.HandleInput(input);
}
```

### Step 5: Add State Accessors
```csharp
public GameState CurrentState => stateManager.CurrentState;
public Character? CurrentPlayer => stateManager.CurrentPlayer;
public List<Item> CurrentInventory => stateManager.CurrentInventory;
public Dungeon? CurrentDungeon => stateManager.CurrentDungeon;
public Environment? CurrentRoom => stateManager.CurrentRoom;
```

### Step 6: Consolidate Display Methods
Keep only essential display methods that delegate to managers:
```csharp
public void ShowMainMenuWithCustomUI()
{
    if (customUIManager is CanvasUICoordinator canvasUI)
    {
        bool hasSavedGame = stateManager.HasPlayer;
        string? characterName = stateManager.CurrentPlayer?.Name;
        int characterLevel = stateManager.CurrentPlayer?.Level ?? 0;
        canvasUI.RenderMainMenu(hasSavedGame, characterName, characterLevel);
    }
    stateManager.TransitionToState(GameState.MainMenu);
}
```

---

## Phase 7: Testing & Verification (1-2 hours)

### Run Existing Tests
```bash
dotnet test Code/Tests/
```

### Create New Tests
1. **GameStateManagerTests** - State management
2. **GameNarrativeManagerTests** - Logging and narratives
3. **GameInitializationManagerTests** - Game setup
4. **GameInputHandlerTests** - Input routing
5. **GameIntegrationTests** - Full game flow

### Regression Testing
- [ ] Start new game
- [ ] Load saved game
- [ ] Navigate menus
- [ ] Enter combat
- [ ] Complete dungeon
- [ ] Verify no crashes or missing functionality

### Performance Testing
- [ ] Measure game startup time
- [ ] Verify no memory leaks
- [ ] Test with large dungeons
- [ ] Profile hot paths

---

## Phase 8: Documentation & Cleanup (1 hour)

### Update Documentation
1. Update `ARCHITECTURE.md` with new managers
2. Update `CODE_PATTERNS.md` with manager patterns used
3. Create individual manager documentation
4. Update `QUICK_REFERENCE.md`

### Code Cleanup
1. Remove unused imports
2. Add XML documentation comments
3. Ensure consistent naming
4. Fix any linting issues

### Final Verification
- [ ] Code compiles without warnings
- [ ] All tests pass
- [ ] Documentation is current
- [ ] No TODOs left in code
- [ ] Performance verified

---

## Success Checklist

- [ ] Game.cs reduced from 1,400 to <600 lines
- [ ] All managers created and tested
- [ ] No functionality lost
- [ ] All existing tests pass
- [ ] New unit tests for managers (>80% coverage)
- [ ] Integration tests verify full game flow
- [ ] No performance regression
- [ ] Documentation updated
- [ ] Code passes linting
- [ ] Ready for production

---

## Related Documentation

- **ARCHITECTURE.md**: Updated system architecture
- **CODE_PATTERNS.md**: Design patterns reference
- **GAME_REFACTORING_PLAN.md**: Detailed planning
- **GAME_CS_REFACTORING_SUMMARY.md**: High-level overview

