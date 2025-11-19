# Game.cs Architecture Diagram

## Current Architecture (1,400 lines - Monolithic)

```
┌─────────────────────────────────────────────────────────────┐
│                     Game.cs (1,400 lines)                    │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  State Management (~200 lines)                       │   │
│  │  • currentState                                      │   │
│  │  • currentPlayer, currentInventory                   │   │
│  │  • availableDungeons, currentDungeon, currentRoom    │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Input Handling (~300 lines)                         │   │
│  │  • HandleInput()                                     │   │
│  │  • HandleMainMenuInput(), HandleInventoryInput()    │   │
│  │  • HandleGameLoopInput(), HandleDungeonInput()      │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  UI Display (~150 lines)                             │   │
│  │  • ShowMainMenu(), ShowGameLoopMenu()               │   │
│  │  • ShowSettings(), ShowCharacterInfo()              │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Dungeon Progression (~200 lines)                    │   │
│  │  • HandleDungeonProgression()                        │   │
│  │  • RunRoom(), ProcessRoomEncounter()                 │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Initialization (~150 lines)                         │   │
│  │  • StartNewGame(), LoadGame()                        │   │
│  │  • CreateNewCharacter()                              │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Logging/Narrative (~100 lines)                      │   │
│  │  • dungeonLog, dungeonHeaderInfo, currentRoomInfo    │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Utilities (~100 lines)                              │   │
│  │  • GetThemeSpecificRooms()                           │   │
│  │  • GetDungeonGenerationConfig()                      │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
└─────────────────────────────────────────────────────────────┘

Problems:
✗ Hard to test individual concerns
✗ High cognitive complexity
✗ Difficult to maintain
✗ Tight coupling
✗ Violates Single Responsibility Principle
```

---

## Refactored Architecture (400-500 lines - Modular)

```
┌─────────────────────────────────────────────────────────────────┐
│                 Game.cs (400-500 lines)                         │
│              (Coordinator/Facade Pattern)                       │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Public API & State Accessors                            │  │
│  │  • ShowMainMenu(), HandleInput(), HandleEscapeKey()      │  │
│  │  • CurrentState, CurrentPlayer, CurrentInventory         │  │
│  │  • SetUIManager()                                        │  │
│  └──────────────────────────────────────────────────────────┘  │
│                      │                                          │
│                      ├─────────────────┬─────────────┬──────┐   │
│                      ↓                 ↓             ↓      ↓   │
│  ┌─────────────────────────┐  ┌──────────────────┐ ┌─────┐ ┌─┐ │
│  │ GameStateManager        │  │ GameInputHandler │ │Dung │ │U │ │
│  │  (150 lines)            │  │  (100 lines)     │ │eon  │ │I │ │
│  │                         │  │                  │ │Mgr  │ │  │ │
│  │ • State tracking        │  │ • Input routing  │ │(300)│ │M │ │
│  │ • Player management     │  │ • Command proces │ └─────┘ │g │ │
│  │ • Inventory tracking    │  │                  │         │r │ │
│  │ • State validation      │  │ & (many input    │         │  │ │
│  │                         │  │  handler methods)│         │  │ │
│  └─────────────────────────┘  └──────────────────┘         │  │ │
│                                                             └─┘ │
│  ┌─────────────────────────┐  ┌──────────────────┐             │
│  │ GameNarrativeManager    │  │ GameInitialization           │
│  │  (80 lines)             │  │ Manager (100 lines)           │
│  │                         │  │                               │
│  │ • Event logging         │  │ • Character creation          │
│  │ • Message formatting    │  │ • Game loading                │
│  │ • Event tracking        │  │ • Data initialization         │
│  │                         │  │                               │
│  └─────────────────────────┘  └──────────────────┘            │
│                                                               │
└─────────────────────────────────────────────────────────────────┘

Benefits:
✓ Easy to test (unit test each manager)
✓ Low cognitive complexity
✓ Easy to maintain (changes isolated)
✓ Loose coupling
✓ Follows Single Responsibility Principle
✓ Follows Composition Pattern
✓ Follows Coordinator/Facade Pattern
```

---

## Manager Interaction Diagram

```
                    ┌─────────────────────┐
                    │   UILayer/Controls  │
                    │   (Avalonia Canvas) │
                    └──────────┬──────────┘
                               │
                    calls HandleInput()
                               │
                               ↓
                    ┌─────────────────────┐
                    │   Game.cs           │
                    │  (Coordinator)      │
                    └──────────┬──────────┘
                               │
                ┌──────────────┼──────────────┐
                ↓              ↓              ↓
    ┌───────────────────┐ ┌─────────────┐ ┌──────────────────┐
    │ GameInputHandler  │ │GameState    │ │GameNarrative     │
    │                   │ │Manager      │ │Manager           │
    │ • Routes input    │ │             │ │                  │
    │ • Processes cmds  │ │ • Validates │ │ • Logs events    │
    │                   │ │   state     │ │ • Formats msgs   │
    └───────────┬───────┘ │ • Tracks    │ └──────────────────┘
                │         │   player    │
    ┌───────────┼──────────┤ • Manages  │
    │           │          │   inventory│
    │           │          └────────────┘
    ↓           ↓
  Game      DungeonProgression
  State     Manager
  Changes   • Handles rooms
            • Orchestrates
              combat
            • Manages rewards

    ┌───────────────────────────────┐
    │ GameInitializationManager     │
    │ • Creates characters          │
    │ • Loads saved games           │
    │ • Initializes game data       │
    └───────────────────────────────┘
```

---

## Data Flow Diagram

```
START: User Input
  │
  ├─→ [Game.HandleInput()] → Receives input string
  │        │
  │        ├─→ [GameInputHandler.HandleInput()]
  │        │        │
  │        │        ├─→ [Read CurrentState from GameStateManager]
  │        │        │
  │        │        ├─→ [Route to appropriate handler]
  │        │        │     (HandleMainMenuInput, etc.)
  │        │        │
  │        │        └─→ [Process command and update state]
  │        │
  │        └─→ [GameStateManager.TransitionToState()]
  │        
  ├─→ [GameStateManager] ← Updates state
  │        │
  │        ├─→ ValidateTransition
  │        └─→ Updates currentState, player, dungeon, room
  │
  ├─→ [GameNarrativeManager] ← Logs events (optional)
  │        │
  │        └─→ LogDungeonEvent, SetRoomInfo
  │
  ├─→ [DungeonProgressionManager] ← Handles dungeons
  │        │
  │        └─→ ProcessRoom, RunCombat, CompleteRoom
  │
  └─→ [UILayer] ← Refreshes display with new state
       │
       └─→ Renders Game.CurrentPlayer, Game.CurrentState, etc.
```

---

## State Machine Diagram

```
                    ┌─────────────────┐
                    │   MainMenu      │
                    └────────┬────────┘
                     1│      │2      │3
                      ↓      ↓      ↓
              ┌─────────────────┬──────────────┐
              │                 │              │
        ┌─────▼─────┐    ┌──────▼──────┐   ┌─▼────────────┐
        │   New     │    │   Load      │   │  Settings    │
        │  Game     │    │   Game      │   │              │
        └─────┬─────┘    └──────┬──────┘   └──────────────┘
              │                 │
              │    ┌────────────┘
              │    │
              └───▼──────────────────┐
                                     │
                    ┌────────────────▼───────┐
                    │  Character Creation    │
                    └────────────────┬───────┘
                                     │
                                     ↓
                    ┌────────────────────────┐
                    │   Weapon Selection     │
                    └────────────────┬───────┘
                                     │
                                     ↓
                    ┌────────────────────────┐
                    │    GameLoop            │
                    │  (Main Game State)     │
                    └──────┬────────┬────────┘
                     1│    │2       │3
                      ↓    ↓       ↓
            ┌─────────────┬────────────────┐
            │             │                │
      ┌─────▼──────┐ ┌────▼────────┐  ┌───▼──────┐
      │ Inventory  │ │Character    │  │Dungeon   │
      │            │ │Info         │  │Selection │
      └────────────┘ └─────────────┘  └──────────┘
                                            │
                                    ┌───────┘
                                    ↓
                    ┌────────────────────────┐
                    │    Dungeon            │
                    │  (Room Exploration)    │
                    └────────────┬───────────┘
                                 │
                                 ↓
                    ┌────────────────────────┐
                    │     Combat            │
                    └────────────┬───────────┘
                                 │
                                 ↓
                    ┌────────────────────────┐
                    │ DungeonCompletion      │
                    │ (Rewards/Results)      │
                    └──────────┬─────────────┘
                               │
                    ┌──────────┘
                    │
                    └──→ [Escape or Continue]
                         │
                         └──→ [GameLoop or MainMenu]
```

---

## Manager Responsibilities Matrix

```
┌────────────────────────────┬────────────────────────────────────┐
│        Manager             │       Responsibilities              │
├────────────────────────────┼────────────────────────────────────┤
│ GameStateManager           │ • Track game state                 │
│ (150 lines)                │ • Track player/inventory           │
│                            │ • Track dungeons/rooms             │
│                            │ • Validate state transitions       │
│                            │ • Provide state accessors          │
├────────────────────────────┼────────────────────────────────────┤
│ GameInputHandler           │ • Route input by state             │
│ (100 lines)                │ • Handle menu input                │
│                            │ • Handle inventory input           │
│                            │ • Handle combat input              │
│                            │ • Delegate to game methods         │
├────────────────────────────┼────────────────────────────────────┤
│ GameNarrativeManager       │ • Log dungeon events               │
│ (80 lines)                 │ • Track room information           │
│                            │ • Format narrative output          │
│                            │ • Build event log                  │
├────────────────────────────┼────────────────────────────────────┤
│ GameInitializationManager  │ • Create new characters            │
│ (100 lines)                │ • Load saved games                 │
│                            │ • Initialize game data             │
│                            │ • Apply game settings              │
├────────────────────────────┼────────────────────────────────────┤
│ DungeonProgressionManager  │ • Manage dungeon progression       │
│ (300 lines)                │ • Handle room encounters           │
│                            │ • Orchestrate combat               │
│                            │ • Distribute rewards               │
│                            │ • Manage room transitions          │
├────────────────────────────┼────────────────────────────────────┤
│ Game (Coordinator)         │ • Coordinate managers              │
│ (400-500 lines)            │ • Provide public API               │
│                            │ • Handle UI delegation             │
│                            │ • State transition decisions       │
└────────────────────────────┴────────────────────────────────────┘
```

---

## Testing Architecture

```
┌─────────────────────────────────────────────────────┐
│             Unit Tests                              │
├─────────────────────────────────────────────────────┤
│                                                     │
│ ┌─────────────────────┐  ┌─────────────────────┐   │
│ │ GameStateManagerTest│  │ GameInputHandler    │   │
│ │                     │  │ Test                │   │
│ │ • Test transitions  │  │ • Test routing      │   │
│ │ • Test getters      │  │ • Test handlers     │   │
│ │ • Test validation   │  │                     │   │
│ └─────────────────────┘  └─────────────────────┘   │
│                                                     │
│ ┌─────────────────────┐  ┌─────────────────────┐   │
│ │ GameNarrative       │  │ GameInitialization  │   │
│ │ ManagerTest         │  │ ManagerTest         │   │
│ │ • Test logging      │  │ • Test creation     │   │
│ │ • Test formatting   │  │ • Test loading      │   │
│ └─────────────────────┘  └─────────────────────┘   │
│                                                     │
└─────────────────────────────────────────────────────┘
         │
         │ Compose into:
         ↓
┌─────────────────────────────────────────────────────┐
│         Integration Tests                           │
├─────────────────────────────────────────────────────┤
│                                                     │
│ ┌─────────────────────────────────────────────┐    │
│ │ GameIntegrationTest                         │    │
│ │ • Test full game flow                       │    │
│ │ • Test all state transitions                │    │
│ │ • Test no regressions                       │    │
│ │ • Test input → state → display              │    │
│ └─────────────────────────────────────────────┘    │
│                                                     │
└─────────────────────────────────────────────────────┘
```

---

## Summary

**Before Refactoring:**
- Single monolithic Game class (1,400 lines)
- Mixed concerns and responsibilities
- Difficult to test individual systems
- High cognitive load

**After Refactoring:**
- Specialized managers for each concern
- Clear separation of responsibilities
- Easy to test individual components
- Lower cognitive load
- Easier to extend and maintain
- ~68% reduction in Game.cs size

**Key Patterns Used:**
- Manager Pattern (specialized managers)
- Coordinator Pattern (Game coordinates managers)
- Composition Pattern (Game composes managers)
- Single Responsibility Principle
- Dependency Injection (passing managers to handlers)

