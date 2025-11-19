# Menu & Input System - Architecture Comparison

**Visual Guide to Before/After Refactoring**

---

## Current Architecture (Before Refactoring)

### High-Level Data Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    MainWindow.axaml.cs                          │
│                     OnKeyDown Event                             │
└─────────────────┬───────────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────────┐
│              ConvertKeyToInput(Key key)                         │
│  Maps Key.D1 → "1", Key.H → "H", etc.                         │
└─────────────────┬───────────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                  game.HandleInput(string)                       │
│                                                                 │
│  if (mainMenuHandler == null || inventoryMenuHandler == null)  │
│      return;                                                    │
│                                                                 │
│  switch(stateManager.CurrentState)                             │
│  {                                                              │
│      case MainMenu:                                            │
│          await mainMenuHandler.HandleMenuInput(input);         │
│          break;                                                │
│      case Inventory:                                           │
│          await inventoryMenuHandler.HandleMenuInput(input);    │
│          break;                                                │
│      case CharacterCreation:                                   │
│          await characterCreationHandler.HandleMenuInput(input);│
│          break;                                                │
│      case WeaponSelection:                                     │
│          await weaponSelectionHandler.HandleMenuInput(input);  │
│          break;                                                │
│      case Settings:                                            │
│          await settingsMenuHandler.HandleMenuInput(input);     │
│          break;                                                │
│      case DungeonSelection:                                    │
│          await dungeonSelectionHandler.HandleMenuInput(input); │
│          break;                                                │
│  }                                                              │
└─────────────────┬───────────────────────────────────────────────┘
                  │
        ┌─────────┼─────────┬───────────┬──────────┬────────┐
        ▼         ▼         ▼           ▼          ▼        ▼
   ┌────────┐ ┌────────┐ ┌──────┐ ┌────────┐ ┌───────┐ ┌────────┐
   │MainMenu│ │Inv Menu│ │CharCr│ │Weapon │ │Setting│ │Dungeon │
   │Handler │ │Handler │ │Handler│ │Handler│ │Handler│ │Handler │
   │        │ │        │ │      │ │       │ │       │ │        │
   │ case   │ │ Input  │ │Stat+ │ │Select │ │Toggle │ │Select  │
   │ "1"→   │ │ Valid? │ │Stat- │ │Weap?  │ │Opt?   │ │Dungeon?│
   │ New    │ │No clear│ │? (No)│ │No     │ │No     │ │No      │
   │ Game   │ │pattern │ │clear │ │clear  │ │clear  │ │clear   │
   │        │ │        │ │      │ │       │ │       │ │        │
   │case    │ │state?  │ │state?│ │state? │ │state? │ │state?  │
   │ "2"→   │ │(No)    │ │ Yes  │ │ Yes   │ │ Yes   │ │ Yes    │
   │ Load   │ │        │ │Direct│ │Direct │ │Direct │ │Direct  │
   │ Game   │ │log?    │ │Set   │ │Set    │ │Set    │ │Set     │
   │        │ │Yes, but│ │State │ │State  │ │State  │ │State   │
   │case    │ │incons. │ │      │ │       │ │       │ │        │
   │ "3"→   │ │        │ │      │ │       │ │       │ │        │
   │Setting │ │        │ │      │ │       │ │       │ │        │
   │        │ │        │ │      │ │       │ │       │ │        │
   │case    │ │        │ │      │ │       │ │       │ │        │
   │ "0"→   │ │        │ │      │ │       │ │       │ │        │
   │ Exit   │ │        │ │      │ │       │ │       │ │        │
   └────────┘ └────────┘ └──────┘ └────────┘ └───────┘ └────────┘
```

### Problems with Current Design

```
┌─ PROBLEM 1: SCATTERED STATE MANAGEMENT
│  ├─ Each handler modifies state directly
│  ├─ No centralized transition rules
│  ├─ Hard to track valid transitions
│  └─ Easy to create invalid states
│
├─ PROBLEM 2: INCONSISTENT PATTERNS
│  ├─ MainMenuHandler: switch statement on input
│  ├─ CharacterCreationHandler: Simple if checks
│  ├─ WeaponSelectionHandler: Parse int + validation
│  ├─ InventoryMenuHandler: Complex logic
│  └─ Different patterns → different bugs
│
├─ PROBLEM 3: NO INPUT VALIDATION FRAMEWORK
│  ├─ Each handler validates differently
│  ├─ Trim vs no trim
│  ├─ Parse int vs string match
│  ├─ No sanitization
│  └─ Error messages vary
│
├─ PROBLEM 4: TIGHT COUPLING
│  ├─ Handlers coupled to state management
│  ├─ Handlers coupled to UI manager
│  ├─ Handlers coupled to game logic
│  ├─ Hard to test in isolation
│  └─ Hard to add new menus
│
├─ PROBLEM 5: CODE DUPLICATION
│  ├─ Input loop logic repeated 6 times
│  ├─ Error handling patterns repeated
│  ├─ State transition logic repeated
│  ├─ Validation logic repeated
│  └─ ~400 lines of duplicated code
│
└─ PROBLEM 6: HARD TO DEBUG
   ├─ Input flow split across 7+ files
   ├─ State changes scattered
   ├─ No centralized logging
   ├─ Hard to trace input path
   └─ Hard to verify correct flow
```

---

## Proposed Architecture (After Refactoring)

### High-Level Data Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    MainWindow.axaml.cs                          │
│                     OnKeyDown Event                             │
└─────────────────┬───────────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────────┐
│              ConvertKeyToInput(Key key)                         │
│  Maps Key.D1 → "1", Key.H → "H", etc.                         │
└─────────────────┬───────────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                  game.HandleInput(string)                       │
│  NEW: Simplified to 20 lines                                   │
│                                                                 │
│  var result = await menuInputRouter.RouteInput(                │
│      input, stateManager.CurrentState);                        │
│                                                                 │
│  if (!result.Success)                                          │
│      ui.ShowError(result.Message);                             │
└─────────────────┬───────────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────────┐
│           MenuInputRouter.RouteInput()                          │
│                                                                 │
│  1. Validate input                                             │
│  2. Get appropriate handler                                    │
│  3. Route to handler                                           │
│  4. Execute command                                            │
│  5. Handle state transition                                    │
│  6. Return result                                              │
└─────────────────┬───────────────────────────────────────────────┘
                  │
        ┌─────────┼─────────┐
        ▼         ▼         ▼
    ┌────────┐ ┌────────┐ ┌─────────────┐
    │Validator│ │Handler │ │Commander   │
    │         │ │        │ │Executor    │
    │Checks:  │ │Parse:  │ │            │
    │- Null? │ │- Input │ │Execute:    │
    │- Empty?│ │- Valid?│ │- Command   │
    │- Valid?│ │- Create│ │- Get Result│
    │        │ │Command │ │- Fire event│
    └────────┘ └────────┘ └─────────────┘
                  │
        ┌─────────┼─────────┬───────────┬──────────┬────────┐
        ▼         ▼         ▼           ▼          ▼        ▼
   ┌────────┐ ┌────────┐ ┌──────┐ ┌────────┐ ┌───────┐ ┌────────┐
   │MainMenu│ │Inv Menu│ │CharCr│ │Weapon │ │Setting│ │Dungeon │
   │Handler │ │Handler │ │Handler│ │Handler│ │Handler│ │Handler │
   │        │ │        │ │      │ │       │ │       │ │        │
   │Parse→ │ │Parse→  │ │Parse │ │Parse  │ │Parse  │ │Parse   │
   │Command│ │Command │ │Command│ │Command│ │Command│ │Command │
   │        │ │        │ │      │ │       │ │       │ │        │
   │Returns│ │Returns │ │Returns│ │Returns│ │Returns│ │Returns │
   │Result │ │Result  │ │Result │ │Result │ │Result │ │Result  │
   └────────┘ └────────┘ └──────┘ └────────┘ └───────┘ └────────┘
        │          │          │          │         │        │
        ▼          ▼          ▼          ▼         ▼        ▼
   ┌────────┐ ┌────────┐ ┌──────┐ ┌────────┐ ┌───────┐ ┌────────┐
   │NewGame │ │Equip   │ │Stat+ │ │Select  │ │Toggle │ │Select  │
   │Command │ │Command │ │Command│ │Weapon │ │Option │ │Dungeon │
   │        │ │        │ │      │ │Command│ │Command│ │Command │
   │Execute │ │Execute │ │Execute│ │Execute│ │Execute│ │Execute │
   │        │ │        │ │      │ │       │ │       │ │        │
   │Result: │ │Result: │ │Result│ │Result │ │Result │ │Result  │
   │Success │ │Success │ │Success│ │Success│ │Success│ │Success │
   │→WeaponS│ │→None   │ │→None │ │→CharCr│ │→None  │ │→DungRun│
   └────────┘ └────────┘ └──────┘ └────────┘ └───────┘ └────────┘
        │          │          │          │         │        │
        ▼          ▼          ▼          ▼         ▼        ▼
   ┌──────────────────────────────────────────────────────────┐
   │  MenuStateTransitionManager                             │
   │                                                           │
   │  if (result.NextState.HasValue)                          │
   │  {                                                        │
   │    transitions.Validate(current, next);                  │
   │    stateManager.SetState(next);                          │
   │    events.FireStateChangedEvent();                       │
   │  }                                                        │
   └──────────────────────────────────────────────────────────┘
```

### Benefits of New Design

```
✅ BENEFIT 1: CENTRALIZED INPUT ROUTING
   ├─ Single MenuInputRouter
   ├─ Clear input → state mapping
   ├─ Easy to trace input flow
   ├─ Simple to add logging/debugging
   └─ Easy to modify routing logic

✅ BENEFIT 2: CONSISTENT PATTERNS
   ├─ All handlers implement IMenuHandler
   ├─ All use MenuHandlerBase
   ├─ Same validation approach
   ├─ Same error handling
   └─ Easy to learn new menu

✅ BENEFIT 3: UNIFIED INPUT VALIDATION
   ├─ MenuInputValidator centralizes checks
   ├─ Per-menu validation rules
   ├─ Consistent error messages
   ├─ Reusable validation logic
   └─ Easy to modify validation

✅ BENEFIT 4: LOOSE COUPLING
   ├─ Handlers don't know about each other
   ├─ Handlers don't modify state directly
   ├─ Handlers return commands (not side effects)
   ├─ Easy to test in isolation
   └─ Easy to mock dependencies

✅ BENEFIT 5: NO CODE DUPLICATION
   ├─ All logic in specific classes
   ├─ Reusable validation
   ├─ Reusable command infrastructure
   ├─ Reusable state management
   └─ 55% code reduction

✅ BENEFIT 6: EASY TO DEBUG
   ├─ Centralized logging
   ├─ Traceable input path
   ├─ Centralized state changes
   ├─ Clear validation flow
   └─ Easy error recovery
```

---

## Side-by-Side Component Comparison

### Input Validation

**BEFORE:**
```csharp
// MainMenuHandler
public async Task HandleMenuInput(string input)
{
    string trimmedInput = input.Trim();
    switch(trimmedInput)
    {
        case "1": // ...
    }
}

// CharacterCreationHandler  
public async Task HandleMenuInput(string input)
{
    switch(input)  // No trim! Bug?
    {
        case "1": // ...
    }
}

// WeaponSelectionHandler
public async Task HandleMenuInput(string input)
{
    if (int.TryParse(input, out int choice))
    {
        if (choice >= 1 && choice <= weaponCount)
        {
            // ...
        }
    }
}
```

**AFTER:**
```csharp
// MenuInputValidator
public ValidationResult Validate(string input, GameState state)
{
    if (string.IsNullOrWhiteSpace(input))
        return ValidationResult.Invalid("Input required");
    
    var rules = GetValidationRules(state);
    return rules.Validate(input);
}

// MainMenuValidationRules
public class MainMenuValidationRules : IValidationRules
{
    public ValidationResult Validate(string input)
    {
        string clean = input.Trim();
        if (clean.Length != 1)
            return ValidationResult.Invalid("Enter single digit");
        if (!"1230".Contains(clean))
            return ValidationResult.Invalid("Invalid option");
        return ValidationResult.Valid();
    }
}

// WeaponSelectionValidationRules
public class WeaponSelectionValidationRules : IValidationRules
{
    public ValidationResult Validate(string input)
    {
        if (!int.TryParse(input.Trim(), out int choice))
            return ValidationResult.Invalid("Enter number");
        if (choice < 1 || choice > weaponCount)
            return ValidationResult.Invalid("Invalid weapon");
        return ValidationResult.Valid();
    }
}
```

---

### State Transition Management

**BEFORE:**
```csharp
// Scattered across 6+ handlers

// MainMenuHandler.StartNewGame()
stateManager.SetState(GameState.WeaponSelection);
gameEvents.FireShowWeaponSelectionEvent();

// CharacterCreationHandler.HandleMenuInput()
stateManager.SetState(GameState.GameLoop);

// WeaponSelectionHandler.SelectWeapon()
stateManager.SetState(GameState.CharacterCreation);

// ... repeated pattern everywhere
// Problem: No validation of transitions
// Problem: Hard to track all transitions
// Problem: Easy to create invalid states
```

**AFTER:**
```csharp
// MenuStateTransitionManager
public class MenuStateTransitionManager
{
    private readonly Dictionary<(GameState, GameState), bool> validTransitions;
    
    public bool CanTransition(GameState from, GameState to)
    {
        if (validTransitions.TryGetValue((from, to), out bool valid))
            return valid;
        return false;
    }
    
    public async Task TransitionTo(GameState newState)
    {
        if (!CanTransition(stateManager.CurrentState, newState))
            throw new InvalidStateTransitionException(...);
        
        DebugLogger.Log("StateTransition", 
            $"{stateManager.CurrentState} → {newState}");
        
        stateManager.SetState(newState);
        events.FireStateChangedEvent();
    }
}

// Usage in handlers:
public class StartNewGameCommand : IMenuCommand
{
    public async Task Execute(IMenuContext context)
    {
        var character = await context.CharacterCreator.CreateNew();
        context.Player = character;
        
        // Clear, auditable transition
        await context.StateTransitionManager
            .TransitionTo(GameState.WeaponSelection);
    }
}
```

---

### Handler Implementation

**BEFORE (MainMenuHandler - ~200 lines):**
```csharp
public class MainMenuHandler
{
    private IUIManager uiManager;
    private GameStateManager stateManager;
    private ICharacterRepository characterRepository;
    // ... other dependencies
    
    public async Task HandleMenuInput(string input)
    {
        string trimmedInput = input.Trim();
        
        switch(trimmedInput)
        {
            case "1":
                await StartNewGame();
                break;
            case "2":
                await LoadGame();
                break;
            case "3":
                await ShowSettings();
                break;
            case "0":
                await ExitGame();
                break;
            default:
                uiManager.DisplayMessage("Invalid choice");
                break;
        }
    }
    
    private async Task StartNewGame()
    {
        // ... complex logic
    }
    
    private async Task LoadGame()
    {
        // ... more complex logic
    }
    
    // ... many more methods
}
```

**AFTER (MainMenuHandler - ~80 lines):**
```csharp
public class MainMenuHandler : MenuHandlerBase
{
    public override GameState TargetState => GameState.MainMenu;
    
    protected override IMenuCommand? ParseInput(string input)
    {
        return input.Trim() switch
        {
            "1" => new StartNewGameCommand(),
            "2" => new LoadGameCommand(),
            "3" => new SettingsCommand(),
            "0" => new ExitGameCommand(),
            _ => null
        };
    }
    
    protected override async Task<GameState?> ExecuteCommand(IMenuCommand cmd)
    {
        return cmd switch
        {
            StartNewGameCommand => GameState.WeaponSelection,
            LoadGameCommand => GameState.GameLoop,
            SettingsCommand => null,  // No state change
            ExitGameCommand => GameState.Exit,
            _ => null
        };
    }
}

// StartNewGameCommand (new file)
public class StartNewGameCommand : IMenuCommand
{
    public async Task Execute(IMenuContext context)
    {
        var character = 
            await context.CharacterCreator.CreateNew();
        context.Player = character;
    }
}

// LoadGameCommand (new file)
public class LoadGameCommand : IMenuCommand
{
    public async Task Execute(IMenuContext context)
    {
        context.Player = await context
            .SaveManager.LoadCharacter();
    }
}

// Similar for other commands...
```

---

### Game.HandleInput Method

**BEFORE (~150 lines):**
```csharp
public async void HandleInput(string input)
{
    if (string.IsNullOrEmpty(input)) 
        return;

    if (mainMenuHandler == null || 
        inventoryMenuHandler == null) 
        return;

    DebugLogger.Log("Game", 
        $"HandleInput: input='{input}', " +
        $"state={stateManager.CurrentState}, " +
        $"mainMenuHandler={mainMenuHandler != null}");

    switch(stateManager.CurrentState)
    {
        case GameState.MainMenu:
            if (mainMenuHandler != null)
            {
                DebugLogger.Log("Game", 
                    $"Routing to MainMenuHandler..." +
                    $".HandleMenuInput('{input}')");
                await mainMenuHandler
                    .HandleMenuInput(input);
            }
            else
            {
                DebugLogger.Log("Game", 
                    "ERROR: mainMenuHandler is null!");
            }
            break;

        case GameState.Inventory:
            if (inventoryMenuHandler != null)
            {
                await inventoryMenuHandler
                    .HandleMenuInput(input);
            }
            break;

        case GameState.CharacterCreation:
            if (characterCreationHandler != null)
            {
                await characterCreationHandler
                    .HandleMenuInput(input);
            }
            break;

        case GameState.WeaponSelection:
            if (weaponSelectionHandler != null)
            {
                await weaponSelectionHandler
                    .HandleMenuInput(input);
            }
            break;

        case GameState.Settings:
            if (settingsMenuHandler != null)
            {
                await settingsMenuHandler
                    .HandleMenuInput(input);
            }
            break;

        case GameState.DungeonSelection:
            if (dungeonSelectionHandler != null)
            {
                await dungeonSelectionHandler
                    .HandleMenuInput(input);
            }
            break;

        default:
            DebugLogger.Log("Game", 
                $"No handler for state: " +
                $"{stateManager.CurrentState}");
            break;
    }
}
```

**AFTER (~50 lines):**
```csharp
public async void HandleInput(string input)
{
    if (string.IsNullOrEmpty(input)) 
        return;

    try
    {
        var result = await menuInputRouter
            .RouteInput(input, stateManager.CurrentState);

        if (!result.Success)
        {
            uiManager.ShowError(result.Message);
            return;
        }

        if (result.NextState.HasValue)
        {
            await stateTransitionManager
                .TransitionTo(result.NextState.Value);
        }

        if (result.Command != null)
        {
            await commandExecutor.Execute(result.Command);
        }

        DebugLogger.Log("Game", 
            $"Input processed: '{input}' " +
            $"in state {stateManager.CurrentState}");
    }
    catch (Exception ex)
    {
        DebugLogger.LogError("Game", 
            $"Error handling input: {ex.Message}");
        uiManager.ShowError("Error processing input");
    }
}
```

---

## Size Reduction Visualization

### Before & After Line Counts

```
                        BEFORE          AFTER           REDUCTION
MainMenuHandler         ████████████░░░░░░░ 200 lines    ████░░░░░░░░░ 80 lines    -60%
CharacterCreationH.     ████████░░░░░░░░░░░ 150 lines    ████░░░░░░░░░░ 70 lines   -53%
WeaponSelectionH.       ████████░░░░░░░░░░░ 150 lines    ████░░░░░░░░░░ 75 lines   -50%
InventoryMenuHandler    ████████████░░░░░░░ 200 lines    ████░░░░░░░░░░ 90 lines   -55%
SettingsMenuHandler     ████████░░░░░░░░░░░ 150 lines    ████░░░░░░░░░░ 65 lines   -57%
DungeonSelectionH.      ████████░░░░░░░░░░░ 150 lines    ████░░░░░░░░░░ 85 lines   -43%
Game.HandleInput        ███████░░░░░░░░░░░░ 150 lines    ██░░░░░░░░░░░░░ 50 lines   -67%

TOTAL MENU SYSTEM       ████████████████░░░ 1100 lines   ████████░░░░░░░░ 515 lines  -53%
```

---

## Quality Metrics Comparison

### Code Quality

| Metric | Before | After |
|--------|--------|-------|
| **Cyclomatic Complexity** | High (many switch cases) | Low (one per handler) |
| **Code Duplication** | 35-40% | 5-10% |
| **Testability** | Low (tight coupling) | High (interfaces) |
| **Maintainability** | Medium | High |
| **Extensibility** | Low (hard to add menus) | High (template pattern) |
| **Understandability** | Medium (many patterns) | High (one pattern) |

### Performance (Expected)

| Metric | Before | After | Impact |
|--------|--------|-------|--------|
| **Input Processing Time** | ~2-5ms | ~2-5ms | No change |
| **Memory Allocation** | ~100-200 bytes | ~100-200 bytes | No change |
| **GC Pressure** | Low | Low | No change |
| **Startup Time** | <100ms | <100ms | No change |

---

## Development Timeline

### Phase-by-Phase Progress

```
Phase 1: Foundation (2-3 hours)
████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ Create core interfaces
       ↓
Phase 2: Commands (1-2 hours)
████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ Implement command system
       ↓
Phase 3: Migration (3-4 hours)
█████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ Refactor handlers
       ↓
Phase 4: State Mgmt (1-2 hours)
██░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ Centralize state
       ↓
Phase 5: Testing (2-3 hours)
████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ Tests & Documentation
       ↓
Total: 9-14 hours of development
```

---

## Summary

### What's Changing?

| Aspect | Before | After |
|--------|--------|-------|
| **Architecture** | Scattered | Centralized |
| **Patterns** | Multiple | Unified |
| **Handler Base** | None | MenuHandlerBase |
| **Input Routing** | In switch statements | MenuInputRouter |
| **Validation** | Repeated | MenuInputValidator |
| **State Transitions** | Scattered | MenuStateTransitionManager |
| **Commands** | Inline logic | IMenuCommand objects |
| **Error Handling** | Inconsistent | Unified |
| **Testing** | Difficult | Easy |
| **Code Size** | ~1,100 lines | ~515 lines |

### Why It Matters?

✅ **Easier to Maintain** - Consistent patterns throughout  
✅ **Easier to Extend** - Add new menus without duplication  
✅ **Easier to Debug** - Centralized input flow  
✅ **Easier to Test** - Isolated components  
✅ **More Professional** - Industry-standard patterns  
✅ **Better User Experience** - Consistent error handling  

---

**Status**: ✅ Architecture Comparison Complete  
**Next Step**: Review MENU_INPUT_SYSTEM_REFACTORING.md for implementation details

