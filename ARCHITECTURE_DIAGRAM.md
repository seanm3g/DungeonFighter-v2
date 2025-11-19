# Game State Handler Architecture

## Complete Input Flow Diagram

```
                    PLAYER INPUT (Keyboard)
                            ↓
                    MainWindow.OnKeyDown
                            ↓
                    ConvertKeyToInput
                            ↓
                    game.HandleInput(string)
                            ↓
                ┌───────────────────────────┐
                │ Game.HandleInput()        │
                │ Check Current State       │
                └───────────┬───────────────┘
                            ↓
        ┌───────────────────┼───────────────────┐
        ↓                   ↓                   ↓
    ┌───────────┐      ┌──────────────┐   ┌─────────────────┐
    │ MainMenu  │      │ WeaponSelect │   │ CharacterCreate │
    └─────┬─────┘      └──────┬───────┘   └────────┬────────┘
          │                   │                    │
          ↓                   ↓                    ↓
    ┌──────────────────┐ ┌────────────────┐ ┌──────────────────┐
    │ MainMenuHandler  │ │WeaponSelHandler│ │CharCreationHandlr│
    ├──────────────────┤ ├────────────────┤ ├──────────────────┤
    │ Input: 1,2,3,0   │ │ Input: 1-4     │ │ Input: 1, 0      │
    ├──────────────────┤ ├────────────────┤ ├──────────────────┤
    │ 1=New Game ✅   │ │ 1=Mace ✅      │ │ 1=Start ✅      │
    │ 2=Load Game     │ │ 2=Sword        │ │ 0=Back ✅       │
    │ 3=Settings      │ │ 3=Dagger       │ │                  │
    │ 0=Quit          │ │ 4=Wand         │ │ ✨ NEW HANDLER  │
    └──────────────────┘ └────────────────┘ └──────────────────┘
          │                   │                    │
          ↓                   ↓                    ↓
    StartNewGame()    InitializeWeapon()   TransitionToGameLoop()
          │                   │                    │
          ↓                   ↓                    ↓
    CreateCharacter  TransitionToCharCreate  Game Starts
          │                   │                    │
          ↓                   ↓                    ↓
    → WeaponSelect   → CharacterCreate      → GameLoop
```

## State Transition Diagram

```
                         START
                          ↓
                    ┌─────────────┐
                    │ Main Menu   │
                    └──────┬──────┘
                    Input: 1/2/3/0
                    │
        ┌───────────┼───────────┐
        │           │           │
        ↓           ↓           ↓
    New Game   Load Game   Settings/Quit
        │           │           │
        └───────────┼───────────┘
                    ↓
            Create Character
                    ↓
            ┌─────────────────────┐
            │ Weapon Selection    │ ✅ NOW WORKING
            └──────┬──────────────┘
            Input: 1/2/3/4
                    │
            Choose Weapon
                    │
                    ↓
            ┌──────────────────────────┐
            │ Character Creation       │ ✨ NEW HANDLER
            └────────┬─────────────────┘
            Input: 1/0
                    │
        ┌───────────┴───────────┐
        │                       │
        ↓                       ↓
    Start Game            Go Back
        │                       │
        ↓                       ↓
    ┌──────────┐        Weapon Select
    │ Game Loop│        (choose again)
    │ (PLAYING)│
    └──────────┘
```

## Handler Class Hierarchy

```
Interface: IMenuHandler (implied)
├── MainMenuHandler
│   ├── ShowMainMenu()
│   ├── HandleMenuInput(input)
│   └── Events:
│       ├── ShowGameLoopEvent
│       ├── ShowWeaponSelectionEvent
│       ├── ShowSettingsEvent
│       ├── ExitGameEvent
│       └── ShowMessageEvent
│
├── WeaponSelectionHandler ✅ IMPROVED
│   ├── ShowWeaponSelection()
│   ├── HandleMenuInput(input)
│   └── Events:
│       ├── ShowCharacterCreationEvent
│       └── ShowMessageEvent
│
├── CharacterCreationHandler ✨ NEW
│   ├── ShowCharacterCreation()
│   ├── HandleMenuInput(input)
│   └── Events:
│       ├── StartGameLoopEvent
│       └── ShowMessageEvent
│
└── [Other Handlers...]
    ├── GameLoopInputHandler
    ├── DungeonSelectionHandler
    ├── InventoryMenuHandler
    └── etc.
```

## Input Processing Chain

```
raw input: "1"
    ↓
Game.HandleInput("1")
    │
    ├─ Validate: if (string.IsNullOrEmpty("1")) → NO
    │
    ├─ Log: DebugLogger.Log("Game", "HandleInput: input='1'...")
    │
    ├─ Get Current State: MainMenu
    │
    ├─ Switch on State
    │   ├─ case MainMenu:
    │   │   └─ mainMenuHandler.HandleMenuInput("1")
    │   │       ├─ Trim: "1"
    │   │       ├─ Switch on trimmed
    │   │       │   ├─ case "1": StartNewGame()
    │   │       │   │   ├─ Create Character
    │   │       │   │   ├─ Fire Event
    │   │       │   │   └─ Transition to WeaponSelection
    │   │       │   └─ case default: Show error
    │   └─ ...other states...
    │
    └─ Done
```

## Event Flow Diagram

```
Game Constructor/InitializeHandlers()
    │
    ├─ mainMenuHandler.ShowWeaponSelectionEvent 
    │  └─ += weaponSelectionHandler.ShowWeaponSelection()
    │
    ├─ weaponSelectionHandler.ShowCharacterCreationEvent 
    │  └─ += characterCreationHandler.ShowCharacterCreation()
    │
    ├─ characterCreationHandler.StartGameLoopEvent 
    │  └─ += Game.ShowGameLoop()
    │
    └─ [Other event wiring...]
```

## Data Flow

```
Player Presses "1"
    │
    ├─ InputType: Keyboard
    ├─ Key: D1 (Avalonia key code)
    ├─ State: MainMenu
    │
    └─ String: "1"
        │
        ├─ MainMenuHandler.HandleMenuInput("1")
        │   ├─ Parse: weaponChoice = 1
        │   ├─ Validate: 1 >= 1 && 1 <= 4 → YES
        │   ├─ Initialize: InitializeNewCharacter(character, 1)
        │   └─ Transition: State → WeaponSelection
        │
        └─ Event Chain:
            ├─ ShowWeaponSelectionEvent fires
            ├─ weaponSelectionHandler.ShowWeaponSelection() called
            ├─ UI renders weapon selection screen
            └─ System waits for next input
```

## Component Responsibilities

```
┌────────────────────────────────────────────────────┐
│ Game.cs (Orchestrator)                             │
│ • Manages all handlers                             │
│ • Routes input based on state                      │
│ • Coordinates state transitions                    │
│ • Wires up events                                  │
└────────────────────────────────────────────────────┘
            ↓                               ↓
┌─────────────────────────┐     ┌──────────────────────────┐
│ MainMenuHandler         │     │ WeaponSelectionHandler   │
│ • Display main menu     │     │ • Display weapons (1-4)  │
│ • Handle menu input     │     │ • Validate weapon choice │
│ • Start new game/load   │     │ • Initialize weapon      │
│ • Settings/quit         │     │ • Route to char create   │
└─────────────────────────┘     └──────────────────────────┘
                                           ↓
                        ┌────────────────────────────────┐
                        │ CharacterCreationHandler ✨    │
                        │ • Display character details    │
                        │ • Handle confirmation input    │
                        │ • Start game / go back         │
                        │ • Route to game loop           │
                        └────────────────────────────────┘
```

---

## Key Features of This Architecture

✅ **Separation of Concerns** - Each handler has one responsibility
✅ **Event-Driven** - Handlers communicate via events
✅ **Testable** - Each handler can be tested independently
✅ **Scalable** - Easy to add new states/handlers
✅ **Maintainable** - Clear flow, easy to debug
✅ **Robust** - Complete error handling

