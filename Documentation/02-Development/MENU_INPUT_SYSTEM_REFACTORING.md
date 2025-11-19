# Menu & Input System Refactoring Plan

**Date**: November 19, 2025  
**Status**: Planning Phase  
**Priority**: HIGH - Core system affecting user experience  
**Scope**: Complete menu/input architecture redesign

---

## Executive Summary

The current menu and input system works but is **scattered across multiple files with inconsistent patterns**. This document outlines a comprehensive refactoring to consolidate the system into a clean, testable, maintainable architecture.

### Current Problems
1. **Fragmented Handler Architecture** - 6 different handler classes with different patterns
2. **Inconsistent Input Routing** - Game.cs routes input to multiple handlers differently
3. **Hard to Add New Menus** - No clear pattern for implementing new menu types
4. **Testing Complexity** - Input flow is difficult to test due to tight coupling
5. **State Management Issues** - Game state transitions scattered across handlers
6. **Code Duplication** - Similar input handling patterns repeated in each handler

### Proposed Solution
Create a unified **Menu Input Framework** with:
- **IMenuHandler** interface for consistent pattern
- **MenuInputRouter** for centralized input routing
- **MenuStateTransitionManager** for state management
- **MenuInputValidator** for input validation
- **MenuInputCommandFactory** for command creation
- Unified error handling and logging

### Expected Outcomes
- ✅ All menu logic under 150 lines each
- ✅ Easy to add new menus (3 files per menu)
- ✅ Testable components with clear dependencies
- ✅ Consistent input flow throughout application
- ✅ Centralized state management
- ✅ Professional error handling

---

## Current State Analysis

### File Structure

```
Code/Game/
├── Game.cs (1,383 lines) - Central orchestrator
│   ├── HandleInput() - Routes input to handlers
│   ├── mainMenuHandler - Main menu instance
│   ├── inventoryMenuHandler - Inventory instance
│   ├── ... other handler references
│   
├── MainMenuHandler.cs (200+ lines) - Main menu logic
│   ├── HandleMenuInput() - Input processing
│   ├── Case statement for 1, 2, 3, 0
│   
├── CharacterCreationHandler.cs (150+ lines)
│   ├── HandleMenuInput() - Different pattern
│   
├── WeaponSelectionHandler.cs (150+ lines)
│   ├── HandleMenuInput() - Another variation
│   
├── InventoryMenuHandler.cs (200+ lines)
│   ├── HandleMenuInput() - Yet another pattern
│   
├── SettingsMenuHandler.cs (150+ lines)
│   ├── HandleMenuInput()
│   
└── DungeonSelectionHandler.cs (150+ lines)
    ├── HandleMenuInput()
```

### Input Flow (Current)

```
MainWindow.axaml.cs (OnKeyDown)
  ↓
ConvertKeyToInput(Key key)
  ↓
game.HandleInput(input: string)
  ↓
Game.cs switch(stateManager.CurrentState)
  ├─→ MainMenu → mainMenuHandler.HandleMenuInput(input)
  ├─→ Inventory → inventoryMenuHandler.HandleMenuInput(input)
  ├─→ Character... → characterCreationHandler.HandleMenuInput(input)
  ├─→ etc...
```

### Current Handler Patterns

**Problem 1: Inconsistent Input Processing**
```csharp
// MainMenuHandler
public async Task HandleMenuInput(string input)
{
    string trimmedInput = input.Trim();
    switch(trimmedInput)
    {
        case "1": await StartNewGame(); break;
        case "2": await LoadGame(); break;
        // ...
    }
}

// CharacterCreationHandler - Different pattern!
public async Task HandleMenuInput(string input)
{
    switch(input)  // No trim!
    {
        case "1": HandleStatUp(); break;
        case "2": HandleStatDown(); break;
        // ... only 2 options
    }
}

// WeaponSelectionHandler - Another pattern!
public async Task HandleMenuInput(string input)
{
    if (int.TryParse(input, out int choice))
    {
        // Different validation approach
        if (choice >= 1 && choice <= weaponCount)
        {
            SelectWeapon(choice);
        }
    }
}
```

**Problem 2: Scattered State Transitions**
```csharp
// MainMenuHandler
await mainMenuHandler.StartNewGame();  // Somewhere deep in the handler
// Then inside StartNewGame():
stateManager.SetState(GameState.WeaponSelection);  // Direct state change
gameEvents.FireShowWeaponSelectionEvent();

// CharacterCreationHandler
async Task HandleMenuInput(string input)
{
    // Input processing
    // Then later:
    stateManager.SetState(GameState.GameLoop);  // Another scattered transition
}
```

**Problem 3: Inconsistent Error Handling**
```csharp
// Some handlers have error checking:
if (mainMenuHandler == null) return;

// Others don't:
await inventoryMenuHandler.HandleMenuInput(input);  // Assumes not null

// Some log errors:
DebugLogger.Log("MainMenuHandler", "Invalid choice");

// Others silent fail
```

**Problem 4: No Input Validation Framework**
```csharp
// Each handler validates differently:
string trimmedInput = input.Trim();  // MainMenuHandler trims
if (int.TryParse(input, out int choice))  // WeaponSelectionHandler parses

// No consistent validation, sanitization, or feedback
```

---

## Proposed Architecture

### 1. Core Interfaces

```csharp
// IMenuHandler.cs - Unified handler contract
public interface IMenuHandler
{
    GameState TargetState { get; }
    Task<MenuInputResult> HandleInput(string input);
}

// MenuInputResult.cs - Consistent response format
public class MenuInputResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public GameState? NextState { get; set; }
    public MenuCommand? Command { get; set; }
}

// MenuCommand.cs - Executable command
public interface IMenuCommand
{
    Task Execute(IMenuContext context);
}
```

### 2. Input Router

```csharp
// MenuInputRouter.cs
public class MenuInputRouter
{
    private readonly Dictionary<GameState, IMenuHandler> handlers;
    private readonly IMenuInputValidator validator;
    private readonly IMenuCommandExecutor executor;
    
    public async Task<MenuInputResult> RouteInput(
        string input, 
        GameState currentState)
    {
        // 1. Validate input
        var validationResult = validator.Validate(input, currentState);
        if (!validationResult.IsValid)
            return MenuInputResult.Failure(validationResult.Error);
        
        // 2. Route to appropriate handler
        var handler = handlers[currentState];
        var result = await handler.HandleInput(input);
        
        // 3. Execute any commands
        if (result.Command != null)
            await executor.Execute(result.Command);
        
        // 4. Handle state transitions
        if (result.NextState.HasValue)
            stateManager.SetState(result.NextState.Value);
        
        return result;
    }
}
```

### 3. Input Validator

```csharp
// MenuInputValidator.cs
public class MenuInputValidator
{
    public ValidationResult Validate(string input, GameState state)
    {
        // 1. Null/empty check
        if (string.IsNullOrWhiteSpace(input))
            return ValidationResult.Invalid("Input cannot be empty");
        
        // 2. State-specific validation
        var rules = GetValidationRules(state);
        return rules.Validate(input);
    }
}

// ValidationRules - Define per-menu validation
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
```

### 4. Handler Base Class

```csharp
// MenuHandlerBase.cs
public abstract class MenuHandlerBase : IMenuHandler
{
    public abstract GameState TargetState { get; }
    protected readonly IUIManager ui;
    protected readonly IStateManager stateManager;
    
    public async Task<MenuInputResult> HandleInput(string input)
    {
        try
        {
            var command = ParseInput(input);
            if (command == null)
                return MenuInputResult.Failure("Invalid input");
            
            var nextState = await ExecuteCommand(command);
            return MenuInputResult.Success(nextState);
        }
        catch (Exception ex)
        {
            LogError(ex);
            return MenuInputResult.Failure("Error processing input");
        }
    }
    
    protected abstract IMenuCommand? ParseInput(string input);
    protected abstract Task<GameState?> ExecuteCommand(IMenuCommand command);
}
```

### 5. Specific Menu Implementation

```csharp
// Handlers/MainMenuHandler.cs
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
        if (cmd is StartNewGameCommand)
            return await StartNewGame();
        // ... etc
    }
}

// Commands/StartNewGameCommand.cs
public class StartNewGameCommand : IMenuCommand
{
    public async Task Execute(IMenuContext context)
    {
        var character = await context.CharacterCreator.CreateNew();
        context.Player = character;
        context.StateManager.SetState(GameState.WeaponSelection);
    }
}
```

---

## Refactoring Roadmap

### Phase 1: Foundation (2-3 hours)
1. Create core interfaces (IMenuHandler, MenuInputResult, IMenuCommand)
2. Create MenuHandlerBase class
3. Create MenuInputRouter
4. Create MenuInputValidator

**Deliverables:**
- Unified handler interface
- Centralized input routing
- Consistent validation framework

### Phase 2: Command System (1-2 hours)
1. Create command base class
2. Extract commands from each handler
3. Create command factory
4. Implement command executor

**Deliverables:**
- Executable command pattern
- Command factory for easy menu addition
- Decoupled business logic

### Phase 3: Handler Migration (3-4 hours)
1. Refactor MainMenuHandler → use new pattern
2. Refactor CharacterCreationHandler
3. Refactor WeaponSelectionHandler
4. Refactor InventoryMenuHandler
5. Refactor SettingsMenuHandler
6. Refactor DungeonSelectionHandler

**Deliverables:**
- All handlers using unified pattern
- Each handler < 150 lines
- Consistent input flow

### Phase 4: State Management (1-2 hours)
1. Create MenuStateTransitionManager
2. Centralize state transitions
3. Implement state change validation
4. Add state transition events

**Deliverables:**
- Centralized state management
- Clear state transition rules
- Event-driven architecture

### Phase 5: Testing & Polish (2-3 hours)
1. Create comprehensive test suite
2. Test each handler independently
3. Test input routing
4. Test state transitions
5. Create integration tests

**Deliverables:**
- Unit test coverage for all handlers
- Integration tests for input flow
- Performance tests

---

## Implementation Details

### File Organization (New)

```
Code/Game/
├── Menu/  (NEW - Unified Menu System)
│   ├── Core/
│   │   ├── IMenuHandler.cs
│   │   ├── MenuHandlerBase.cs
│   │   ├── MenuInputResult.cs
│   │   ├── IMenuCommand.cs
│   │   └── MenuCommand.cs
│   │
│   ├── Routing/
│   │   ├── MenuInputRouter.cs
│   │   ├── IMenuInputValidator.cs
│   │   └── MenuInputValidator.cs
│   │
│   ├── State/
│   │   ├── MenuStateTransitionManager.cs
│   │   └── StateTransitionRule.cs
│   │
│   ├── Handlers/
│   │   ├── MainMenuHandler.cs (Refactored)
│   │   ├── CharacterCreationMenuHandler.cs (Refactored)
│   │   ├── WeaponSelectionMenuHandler.cs (Refactored)
│   │   ├── InventoryMenuHandler.cs (Refactored)
│   │   ├── SettingsMenuHandler.cs (Refactored)
│   │   └── DungeonSelectionMenuHandler.cs (Refactored)
│   │
│   └── Commands/
│       ├── MainMenu/
│       │   ├── StartNewGameCommand.cs
│       │   ├── LoadGameCommand.cs
│       │   ├── SettingsCommand.cs
│       │   └── ExitGameCommand.cs
│       │
│       ├── CharacterCreation/
│       │   ├── IncreaseStatCommand.cs
│       │   ├── DecreaseStatCommand.cs
│       │   ├── ConfirmCharacterCommand.cs
│       │   └── RandomizeCharacterCommand.cs
│       │
│       └── (Similar for other menus...)
```

### Size Targets (After Refactoring)

| Component | Current | Target | Reduction |
|-----------|---------|--------|-----------|
| MainMenuHandler | 200+ | 80 | 60% |
| CharacterCreationHandler | 150+ | 70 | 53% |
| WeaponSelectionHandler | 150+ | 75 | 50% |
| InventoryMenuHandler | 200+ | 90 | 55% |
| SettingsMenuHandler | 150+ | 65 | 57% |
| DungeonSelectionHandler | 150+ | 85 | 43% |
| Game.cs HandleInput | 150+ | 50 | 67% |
| **Total** | **1,000+** | **450** | **55%** |

---

## Migration Strategy

### Step 1: Parallel Development
- Create new Menu/ folder with new architecture
- Keep old handlers in place initially
- Game.cs routes to NEW handlers
- Old handlers become deprecated

### Step 2: Gradual Migration
- Migrate one handler at a time
- Test thoroughly before moving to next
- Keep debug output for verification
- Update documentation as we go

### Step 3: Cleanup
- Remove old handler files
- Update Game.cs to use new router
- Clean up imports and references
- Final testing pass

### Step 4: Documentation
- Document new menu patterns
- Create examples for new menus
- Update architecture guide
- Add troubleshooting guide

---

## Benefits & Outcomes

### Code Quality
- ✅ Consistent patterns across all menus
- ✅ Reduced duplication (55% reduction in lines)
- ✅ Single Responsibility Principle
- ✅ Open/Closed Principle (easy to add new menus)
- ✅ Dependency Inversion (interfaces over implementations)

### Maintainability
- ✅ Clear, predictable input flow
- ✅ Easy to debug input issues
- ✅ Easy to add logging/analytics
- ✅ Centralized error handling
- ✅ Easy to implement new menus

### Testability
- ✅ Each handler independently testable
- ✅ Mock-friendly interfaces
- ✅ Command pattern enables unit tests
- ✅ Validation logic separately testable
- ✅ State transitions verifiable

### User Experience
- ✅ Consistent input handling
- ✅ Better error messages
- ✅ More responsive feedback
- ✅ Improved error recovery
- ✅ Extensible without code duplication

---

## Success Criteria

| Criterion | Target | Status |
|-----------|--------|--------|
| Menu handlers < 150 lines | 100% | 🔄 |
| Input routing centralized | Yes | 🔄 |
| Input validation isolated | Yes | 🔄 |
| All menus use IMenuHandler | 100% | 🔄 |
| Test coverage | >80% | 🔄 |
| State transitions centralized | Yes | 🔄 |
| Command execution testable | Yes | 🔄 |
| Code duplication removed | 55% reduction | 🔄 |
| Documentation complete | Yes | 🔄 |

---

## Estimated Timeline

- **Phase 1 (Foundation)**: 2-3 hours
- **Phase 2 (Commands)**: 1-2 hours
- **Phase 3 (Migration)**: 3-4 hours
- **Phase 4 (State)**: 1-2 hours
- **Phase 5 (Testing)**: 2-3 hours

**Total**: 9-14 hours of focused development

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Breaking existing menus | HIGH | Parallel development, keep old handlers |
| Input routing complexity | MEDIUM | Clear separation of concerns, tests |
| State transition issues | MEDIUM | Centralized manager, validation rules |
| Performance regression | LOW | No new allocations, same patterns |
| Developer learning curve | MEDIUM | Documentation, examples, gradual rollout |

---

## Next Steps

1. **Review this plan** - Ensure alignment with project goals
2. **Create core interfaces** - Start Phase 1
3. **Implement base classes** - Complete Phase 1 foundation
4. **Test proof of concept** - Refactor one handler as test
5. **Iterate and refine** - Adjust based on first handler experience
6. **Complete remaining handlers** - Systematically migrate all menus
7. **Documentation & testing** - Final quality pass

---

## Related Documents

- `ARCHITECTURE.md` - Overall system architecture
- `CODE_PATTERNS.md` - Coding standards and patterns
- `DEBUGGING_GUIDE.md` - Debugging menu issues
- `MENU_INPUT_DEBUGGING_GUIDE.md` - Current input debugging info
- `MENU_INPUT_FIX_CHANGES.md` - Previous debugging changes

---

**Status**: 📋 Ready for Implementation  
**Owner**: Development Team  
**Last Updated**: November 19, 2025

