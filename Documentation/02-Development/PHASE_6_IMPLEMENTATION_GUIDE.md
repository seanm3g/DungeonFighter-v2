# Phase 6 Implementation Guide: Completing the Game.cs Refactoring

**Status**: 3 of 10 managers complete  
**Progress**: 30% (MainMenuHandler, CharacterMenuHandler, SettingsMenuHandler created)  
**Remaining**: 7 managers to create and integrate

---

## Completed Managers (✅ Done)

### 1. MainMenuHandler.cs ✅
- **Lines**: ~200
- **Status**: Created and ready
- **Key Methods**:
  - `ShowMainMenu()`
  - `HandleMenuInput(string input)`
  - `StartNewGame()`
  - `LoadGame()`
  
**Extract Code From Game.cs Lines**:
- ShowMainMenuWithCustomUI (lines 184-196)
- HandleMainMenuInput (lines 250-275)
- LoadGame (lines 277-332)
- StartNewGame (lines 364-422)
- HandleEscapeKey (lines 334-361)

---

### 2. CharacterMenuHandler.cs ✅
- **Lines**: ~50
- **Status**: Created and ready
- **Key Methods**:
  - `ShowCharacterInfo()`
  - `HandleMenuInput(string input)`

**Extract Code From Game.cs Lines**:
- ShowCharacterInfo (lines 442-450)
- HandleCharacterInfoInput (lines 766-771)

---

### 3. SettingsMenuHandler.cs ✅
- **Lines**: ~100
- **Status**: Created and ready
- **Key Methods**:
  - `ShowSettings()`
  - `HandleMenuInput(string input)`
  - `SaveGame()`
  - `ExitGame()`

**Extract Code From Game.cs Lines**:
- ShowSettings (lines 451-457)
- HandleSettingsInput (lines 924-1008)
- SaveGame (lines 459-475)
- ExitGame (lines 477-486)

---

## Remaining Managers (⏳ To Do)

### 4. InventoryMenuHandler.cs (150 lines)
**Priority**: HIGH - Complex logic

**From Game.cs**:
- ShowInventory (lines 424-440)
- HandleInventoryInput (lines 681-765)
- PromptEquipItem (lines 774-794)
- PromptUnequipItem (lines 796-807)
- PromptDiscardItem (lines 809-828)
- EquipItem (lines 830-862)
- UnequipItem (lines 864-896)
- DiscardItem (lines 898-907)
- ShowComboManagement (lines 909-916)
- State tracking fields (lines 918-921)

**Implementation Notes**:
- Contains inventory management logic
- Handles item equipping/unequipping/discarding
- Multi-step actions with state tracking
- Use delegates for ShowMessage, ShowMainMenu events

### 5. WeaponSelectionHandler.cs (50 lines)
**Priority**: MEDIUM

**From Game.cs**:
- ShowWeaponSelection (lines 1401-1409)
- HandleWeaponSelectionInput (lines 1410-1430)

**Implementation Notes**:
- Simple weapon selection logic
- Display and input handling only
- Delegates to character creation or game loop

### 6. GameLoopInputHandler.cs (40 lines)
**Priority**: MEDIUM

**From Game.cs**:
- HandleGameLoopInput (lines 1009-1034)

**Implementation Notes**:
- Routes game loop menu input
- Delegates to various sub-handlers
- Mostly switch statement

### 7. DungeonSelectionHandler.cs (80 lines)
**Priority**: HIGH

**From Game.cs**:
- HandleDungeonSelectionInput (lines 1086-1125)
- SelectDungeon (lines 1126-1145)
- StartDungeonSelection (lines 1067-1084)

**Implementation Notes**:
- Handles dungeon selection and preparation
- Regenerates dungeons based on player level
- Renders dungeon selection screen

### 8. DungeonRunnerManager.cs (200 lines) ⭐
**Priority**: CRITICAL - Most complex!

**From Game.cs**:
- RunDungeon (lines 1150-1204)
- ProcessRoom (lines 1206-1258)
- ProcessEnemyEncounter (lines 1259-1378)

**Implementation Notes**:
- Most complex refactoring
- Handles entire dungeon run orchestration
- Processes rooms and encounters
- Requires careful testing
- Multiple state transitions
- Combat integration

### 9. DungeonCompletionHandler.cs (40 lines)
**Priority**: MEDIUM

**From Game.cs**:
- HandleDungeonCompletionInput (lines 1036-1065)

**Implementation Notes**:
- Handles post-dungeon options
- Inventory access
- Save and exit
- Dungeon selection restart

### 10. TestingSystemHandler.cs (130 lines)
**Priority**: LOW - Isolated system

**From Game.cs**:
- ShowTestingMenu (lines 513-519)
- HandleTestingInput (lines 521-586)
- RunAllTests (lines 587-607)
- RunSystemTests (lines 608-628)
- RunCombatUIFixes (lines 629-654)
- RunIntegrationTests (lines 655-680)
- State tracking (lines 918-921)

**Implementation Notes**:
- Completely isolated testing system
- No dependencies on game state except display
- Can be extracted independently

---

## Implementation Strategy (Remaining 7 Managers)

### Quick Implementation Path:

#### Step 1: Create Remaining 7 Manager Files
```bash
Create:
- InventoryMenuHandler.cs
- WeaponSelectionHandler.cs
- GameLoopInputHandler.cs
- DungeonSelectionHandler.cs
- DungeonRunnerManager.cs ⭐ (save for last, most complex)
- DungeonCompletionHandler.cs
- TestingSystemHandler.cs
```

#### Step 2: Update Game.cs Constructor
Add all 10 manager initializations:
```csharp
private MainMenuHandler mainMenuHandler;
private CharacterMenuHandler characterMenuHandler;
private SettingsMenuHandler settingsMenuHandler;
private InventoryMenuHandler inventoryMenuHandler;
private WeaponSelectionHandler weaponSelectionHandler;
private GameLoopInputHandler gameLoopInputHandler;
private DungeonSelectionHandler dungeonSelectionHandler;
private DungeonRunnerManager dungeonRunnerManager;
private DungeonCompletionHandler dungeonCompletionHandler;
private TestingSystemHandler testingSystemHandler;
```

#### Step 3: Update Game.cs.HandleInput()
Route to appropriate handlers:
```csharp
public async Task HandleInput(string input)
{
    switch (stateManager.CurrentState)
    {
        case GameState.MainMenu:
            await mainMenuHandler.HandleMenuInput(input);
            break;
        case GameState.Inventory:
            await inventoryMenuHandler.HandleMenuInput(input);
            break;
        // ... (10 cases total)
    }
}
```

#### Step 4: Remove Extracted Methods from Game.cs
Delete all private methods that are now in managers (1,100+ lines deleted!)

#### Step 5: Test Integration
- Build solution
- Verify all menus work
- Verify all input routing works
- Run through game scenarios

---

## Expected Game.cs After Completion

**Before**: 1,383 lines  
**After**: 250-300 lines

```csharp
public class Game
{
    // Fields (10 managers + existing)
    private MainMenuHandler mainMenuHandler;
    private CharacterMenuHandler characterMenuHandler;
    private SettingsMenuHandler settingsMenuHandler;
    private InventoryMenuHandler inventoryMenuHandler;
    private WeaponSelectionHandler weaponSelectionHandler;
    private GameLoopInputHandler gameLoopInputHandler;
    private DungeonSelectionHandler dungeonSelectionHandler;
    private DungeonRunnerManager dungeonRunnerManager;
    private DungeonCompletionHandler dungeonCompletionHandler;
    private TestingSystemHandler testingSystemHandler;
    
    // Existing managers
    private GameStateManager stateManager = new();
    private GameNarrativeManager narrativeManager = new();
    private GameInitializationManager initializationManager = new();
    private GameInputHandler? inputHandler;
    
    // + Other core managers (menuManager, etc.)
    
    // Constructors (initialize all managers)
    
    // Main methods
    public async void ShowMainMenu() { mainMenuHandler.ShowMainMenu(); }
    
    public async Task HandleInput(string input)
    {
        switch (stateManager.CurrentState)
        {
            case GameState.MainMenu:
                await mainMenuHandler.HandleMenuInput(input);
                break;
            case GameState.CharacterInfo:
                characterMenuHandler.HandleMenuInput(input);
                break;
            case GameState.Settings:
                settingsMenuHandler.HandleMenuInput(input);
                break;
            case GameState.Inventory:
                await inventoryMenuHandler.HandleMenuInput(input);
                break;
            case GameState.WeaponSelection:
                await weaponSelectionHandler.HandleMenuInput(input);
                break;
            case GameState.GameLoop:
                await gameLoopInputHandler.HandleMenuInput(input);
                break;
            case GameState.DungeonSelection:
                await dungeonSelectionHandler.HandleMenuInput(input);
                break;
            case GameState.Dungeon:
            case GameState.Combat:
                // Handled by DungeonRunnerManager
                break;
            case GameState.DungeonCompletion:
                await dungeonCompletionHandler.HandleMenuInput(input);
                break;
            case GameState.Testing:
                await testingSystemHandler.HandleMenuInput(input);
                break;
            case GameState.CharacterCreation:
                // Handled by game flow
                break;
        }
    }
    
    public Task HandleEscapeKey() { /* delegate */ }
    
    // That's basically it! 
    // Everything else is delegated to managers.
}
```

---

## Testing Checklist

After completing all 10 managers and integrating:

- [ ] Game builds without errors
- [ ] Game builds without warnings
- [ ] Main menu displays correctly
- [ ] Can start new game
- [ ] Can load saved game
- [ ] Can access inventory menu
- [ ] Can equip/unequip items
- [ ] Can access character info
- [ ] Can access settings
- [ ] Can run tests from settings
- [ ] Can select weapons
- [ ] Can select dungeons
- [ ] Can run dungeons
- [ ] Can complete dungeons
- [ ] Escape key works from all menus
- [ ] Save game works
- [ ] Exit game works

---

## Estimated Remaining Time

- **InventoryMenuHandler**: 45 minutes
- **WeaponSelectionHandler**: 15 minutes
- **GameLoopInputHandler**: 15 minutes
- **DungeonSelectionHandler**: 30 minutes
- **DungeonRunnerManager**: 90 minutes (complex!)
- **DungeonCompletionHandler**: 15 minutes
- **TestingSystemHandler**: 45 minutes
- **Integration & Testing**: 45 minutes

**Total**: 4-5 hours

---

## Next Steps

1. Create remaining 7 manager files (use templates provided above)
2. Add all manager initialization to Game.cs constructors
3. Wire up HandleInput() to route to appropriate handlers
4. Delete extracted methods from Game.cs
5. Fix any compilation errors
6. Test thoroughly
7. Celebrate with ultra-clean Game.cs! 🎉

---

## Notes

- **DungeonRunnerManager** is the most complex - save it for when you're fresh
- **Use events/delegates** to communicate between Game.cs and managers
- **Keep Game.cs as pure coordinator** - it just routes and delegates
- **Test frequently** - don't wait until the end
- **Manager naming**: -Handler for menu handlers, -Manager for complex systems

---

Good luck! You're 30% done. Just 7 more managers to go! 💪

