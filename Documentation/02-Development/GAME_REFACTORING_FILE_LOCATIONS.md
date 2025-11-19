# Game.cs Refactoring - File Locations & Structure

## Documentation Files Created

All refactoring documentation has been created in the `Documentation/02-Development/` directory.

### Location: `Documentation/02-Development/`

```
GAME_REFACTORING_README.md ..................... Main index and quick start guide
GAME_REFACTORING_PLAN.md ....................... Detailed planning and strategy
GAME_CS_REFACTORING_SUMMARY.md ................. Executive summary with steps
GAME_CS_REFACTORING_GUIDE.md ................... Step-by-step implementation guide
GAME_CS_ARCHITECTURE_DIAGRAM.md ................ Visual architecture diagrams
GAME_REFACTORING_FILE_LOCATIONS.md ............ This file - File structure reference
```

## Reading Order

### For Quick Understanding (15 minutes)
1. Read: `GAME_REFACTORING_README.md` (this provides overview)
2. Read: `GAME_CS_ARCHITECTURE_DIAGRAM.md` (see the changes visually)
3. Skim: `GAME_CS_REFACTORING_SUMMARY.md` (high-level plan)

### For Strategic Planning (1 hour)
1. Read: `GAME_REFACTORING_README.md` - Overview
2. Read: `GAME_REFACTORING_PLAN.md` - Detailed strategy
3. Study: `GAME_CS_ARCHITECTURE_DIAGRAM.md` - Visual understanding
4. Review: `GAME_CS_REFACTORING_SUMMARY.md` - Checklist

### For Implementation (8-12 hours)
1. Reference: `GAME_REFACTORING_README.md` - Getting started
2. Main: `GAME_CS_REFACTORING_GUIDE.md` - Phase-by-phase instructions
3. Check: `GAME_CS_REFACTORING_SUMMARY.md` - Progress tracking
4. Diagram: `GAME_CS_ARCHITECTURE_DIAGRAM.md` - Architecture reference

## Code Files to Create

### Phase 1: Create Manager Classes

**Location**: `Code/Game/`

```
GameStateManager.cs
├── Purpose: Centralize all game state management
├── Size: ~150 lines
├── Methods:
│   ├── TransitionToState(GameState)
│   ├── SetCurrentPlayer(Character)
│   ├── SetCurrentDungeon(Dungeon)
│   ├── SetCurrentRoom(Environment)
│   ├── ResetGameState()
│   └── Various property getters
└── Status: ⏳ TODO (see GAME_CS_REFACTORING_GUIDE.md Phase 1)

GameNarrativeManager.cs
├── Purpose: Manage game logging and narrative output
├── Size: ~80 lines
├── Methods:
│   ├── LogDungeonEvent(string)
│   ├── SetDungeonHeaderInfo(List<string>)
│   ├── SetRoomInfo(List<string>)
│   ├── ClearDungeonLog()
│   ├── ClearRoomInfo()
│   ├── GetFormattedLog()
│   └── ResetNarrative()
└── Status: ⏳ TODO (see GAME_CS_REFACTORING_GUIDE.md Phase 2)

GameInitializationManager.cs
├── Purpose: Handle game initialization and setup
├── Size: ~100 lines
├── Methods:
│   ├── CreateNewCharacter(string, string)
│   ├── LoadSavedCharacter()
│   ├── InitializeGameData(Character, List<Dungeon>)
│   ├── ApplyHealthMultiplier(Character)
│   └── GetDungeonGenerationConfig()
└── Status: ⏳ TODO (see GAME_CS_REFACTORING_GUIDE.md Phase 3)

GameInputHandler.cs
├── Purpose: Route and handle all user input
├── Size: ~100 lines
├── Methods:
│   ├── HandleInput(string)
│   ├── HandleMainMenuInput(string)
│   ├── HandleWeaponSelectionInput(string)
│   ├── HandleCharacterCreationInput(string)
│   ├── HandleInventoryInput(string)
│   ├── HandleCharacterInfoInput(string)
│   ├── HandleSettingsInput(string)
│   ├── HandleTestingInput(string)
│   ├── HandleGameLoopInput(string)
│   ├── HandleDungeonSelectionInput(string)
│   └── HandleDungeonCompletionInput(string)
└── Status: ⏳ TODO (see GAME_CS_REFACTORING_GUIDE.md Phase 4)
```

### Phase 2: Update Existing Files

**Location**: `Code/Game/`

```
Game.cs (REFACTOR)
├── Current Size: 1,400 lines
├── Target Size: 400-500 lines (68% reduction)
├── Changes:
│   ├── Remove all state fields → move to GameStateManager
│   ├── Remove all input handlers → move to GameInputHandler
│   ├── Remove all logging methods → move to GameNarrativeManager
│   ├── Remove initialization methods → move to GameInitializationManager
│   ├── Add manager fields
│   ├── Add state accessors (delegate to managers)
│   └── Update HandleInput to delegate to GameInputHandler
├── Removed Methods (300+):
│   ├── HandleMainMenuInput()
│   ├── HandleWeaponSelectionInput()
│   ├── HandleCharacterCreationInput()
│   ├── HandleInventoryInput()
│   ├── HandleCharacterInfoInput()
│   ├── HandleSettingsInput()
│   ├── HandleTestingInput()
│   ├── HandleGameLoopInput()
│   ├── HandleDungeonSelectionInput()
│   ├── HandleDungeonCompletionInput()
│   ├── StartNewGame()
│   ├── LoadGame()
│   ├── CreateNewCharacter()
│   └── All LogXxx() methods
└── Status: ⏳ TODO (see GAME_CS_REFACTORING_GUIDE.md Phase 5)

DungeonProgressionManager.cs (UPDATE)
├── Current Status: Exists but needs consolidation
├── Current Size: ~300 lines
├── Changes:
│   ├── Add HandleDungeonProgression()
│   ├── Add RunRoom()
│   ├── Add ProcessRoomEncounter()
│   └── Consolidate all dungeon logic
├── New Size: ~350 lines
└── Status: ⏳ TODO (see GAME_CS_REFACTORING_GUIDE.md Phase 5)
```

## Test Files to Create

**Location**: `Code/Tests/`

```
GameStateManagerTests.cs
├── Tests:
│   ├── TestStateTransition
│   ├── TestStateValidation
│   ├── TestPlayerAssignment
│   ├── TestDungeonAssignment
│   ├── TestRoomAssignment
│   └── TestStateReset
└── Status: ⏳ TODO

GameNarrativeManagerTests.cs
├── Tests:
│   ├── TestEventLogging
│   ├── TestInfoSetting
│   ├── TestLogClearing
│   ├── TestMessageFormatting
│   └── TestNarrativeReset
└── Status: ⏳ TODO

GameInitializationManagerTests.cs
├── Tests:
│   ├── TestCharacterCreation
│   ├── TestCharacterLoading
│   ├── TestGameDataInitialization
│   └── TestHealthMultiplierApplication
└── Status: ⏳ TODO

GameInputHandlerTests.cs
├── Tests:
│   ├── TestInputRouting
│   ├── TestMainMenuHandling
│   ├── TestInventoryHandling
│   ├── TestGameLoopHandling
│   └── TestInvalidInput
└── Status: ⏳ TODO

GameIntegrationTests.cs
├── Tests:
│   ├── TestCompleteGameFlow
│   ├── TestStateTransitions
│   ├── TestInputHandling
│   ├── TestNoRegressions
│   └── TestPerformance
└── Status: ⏳ TODO
```

## File Dependency Tree

```
Documentation/02-Development/
├── GAME_REFACTORING_README.md
│   ├── References → GAME_REFACTORING_PLAN.md
│   ├── References → GAME_CS_REFACTORING_SUMMARY.md
│   ├── References → GAME_CS_REFACTORING_GUIDE.md
│   └── References → GAME_CS_ARCHITECTURE_DIAGRAM.md
│
├── GAME_REFACTORING_PLAN.md
│   ├── Defines → Refactoring strategy
│   ├── Defines → New manager classes
│   ├── Defines → Migration phases
│   └── Referenced by → GAME_CS_REFACTORING_GUIDE.md
│
├── GAME_CS_REFACTORING_SUMMARY.md
│   ├── Summarizes → GAME_REFACTORING_PLAN.md
│   ├── Provides → Implementation checklist
│   ├── Provides → Risk assessment
│   └── Referenced by → GAME_CS_REFACTORING_GUIDE.md
│
├── GAME_CS_REFACTORING_GUIDE.md
│   ├── Uses → Code examples
│   ├── References → Line counts from SUMMARY
│   ├── Provides → Phase-by-phase instructions
│   └── For use during → Implementation
│
└── GAME_CS_ARCHITECTURE_DIAGRAM.md
    ├── Shows → Current architecture
    ├── Shows → Target architecture
    ├── Shows → Manager interactions
    └── For use during → Planning & presentations

Code/Game/
├── Game.cs (REFACTOR)
│   ├── Reduces from → 1,400 lines to 400-500 lines
│   ├── Uses → GameStateManager
│   ├── Uses → GameInputHandler
│   ├── Uses → GameNarrativeManager
│   ├── Uses → GameInitializationManager
│   ├── Uses → DungeonProgressionManager
│   └── Tested by → GameIntegrationTests.cs
│
├── GameStateManager.cs (NEW)
│   ├── Extracted from → Game.cs state fields
│   ├── Extracted from → Game.cs state methods
│   └── Tested by → GameStateManagerTests.cs
│
├── GameInputHandler.cs (NEW)
│   ├── Extracted from → Game.cs input methods
│   ├── Delegates to → Game.cs remaining methods
│   └── Tested by → GameInputHandlerTests.cs
│
├── GameNarrativeManager.cs (NEW)
│   ├── Extracted from → Game.cs logging methods
│   ├── Extracted from → Game.cs logging fields
│   └── Tested by → GameNarrativeManagerTests.cs
│
├── GameInitializationManager.cs (NEW)
│   ├── Extracted from → Game.cs init methods
│   ├── Wraps → GameInitializer.cs
│   └── Tested by → GameInitializationManagerTests.cs
│
└── DungeonProgressionManager.cs (UPDATE)
    ├── Consolidates → Dungeon progression logic
    ├── Extracts from → Game.cs dungeon methods
    └── Tested by → DungeonProgressionManagerTests.cs

Code/Tests/
├── GameStateManagerTests.cs (NEW)
├── GameInputHandlerTests.cs (NEW)
├── GameNarrativeManagerTests.cs (NEW)
├── GameInitializationManagerTests.cs (NEW)
└── GameIntegrationTests.cs (NEW)
```

## Implementation Checklist with File Locations

### Phase 1: Create Managers
- [ ] Create `Code/Game/GameStateManager.cs`
- [ ] Create `Code/Tests/GameStateManagerTests.cs`
- [ ] Create `Code/Game/GameNarrativeManager.cs`
- [ ] Create `Code/Tests/GameNarrativeManagerTests.cs`
- [ ] Create `Code/Game/GameInitializationManager.cs`
- [ ] Create `Code/Tests/GameInitializationManagerTests.cs`

### Phase 2: Extract Input Handling
- [ ] Create `Code/Game/GameInputHandler.cs`
- [ ] Create `Code/Tests/GameInputHandlerTests.cs`
- [ ] Update `Code/Game/Game.cs` to use GameInputHandler

### Phase 3: Extract Dungeon Progression
- [ ] Update `Code/World/DungeonProgressionManager.cs`
- [ ] Update `Code/Game/Game.cs` to delegate to DungeonProgressionManager

### Phase 4: Consolidate Game.cs
- [ ] Remove all extracted methods from `Code/Game/Game.cs`
- [ ] Reduce `Game.cs` from 1,400 to 400-500 lines
- [ ] Add manager field declarations
- [ ] Add state accessor properties
- [ ] Update constructor to initialize managers

### Phase 5: Testing & Documentation
- [ ] Create `Code/Tests/GameIntegrationTests.cs`
- [ ] Run all tests
- [ ] Update `Documentation/01-Core/ARCHITECTURE.md`
- [ ] Update `Documentation/04-Reference/CODE_PATTERNS.md`
- [ ] Verify no regressions
- [ ] Performance testing

## Quick Reference: File Organization

```
START HERE → Documentation/02-Development/GAME_REFACTORING_README.md

UNDERSTAND → Documentation/02-Development/GAME_CS_ARCHITECTURE_DIAGRAM.md

IMPLEMENT → Documentation/02-Development/GAME_CS_REFACTORING_GUIDE.md
           → Code/Game/*.cs (create new manager files)
           → Code/Tests/*Tests.cs (create test files)

TRACK → Documentation/02-Development/GAME_CS_REFACTORING_SUMMARY.md (checklist)

REFER → ARCHITECTURE.md (will be updated)
        CODE_PATTERNS.md (reference patterns used)
```

## File Size Reference

| File | Before | After | Change |
|------|--------|-------|--------|
| Game.cs | 1,400 | 450 | -950 lines (-68%) |
| GameStateManager.cs | - | 150 | +150 lines |
| GameInputHandler.cs | - | 100 | +100 lines |
| GameNarrativeManager.cs | - | 80 | +80 lines |
| GameInitializationManager.cs | - | 100 | +100 lines |
| DungeonProgressionManager.cs | 300 | 350 | +50 lines |
| **Total** | **1,400** | **1,230** | **-170 lines (-12%)** |

## Status Tracking

✅ **Documentation Complete**
- [x] GAME_REFACTORING_README.md - Ready
- [x] GAME_REFACTORING_PLAN.md - Ready
- [x] GAME_CS_REFACTORING_SUMMARY.md - Ready
- [x] GAME_CS_REFACTORING_GUIDE.md - Ready
- [x] GAME_CS_ARCHITECTURE_DIAGRAM.md - Ready
- [x] GAME_REFACTORING_FILE_LOCATIONS.md - Ready

⏳ **Implementation Pending**
- [ ] Phase 1: Create 3 base managers
- [ ] Phase 2: Create GameInputHandler
- [ ] Phase 3: Update DungeonProgressionManager
- [ ] Phase 4: Refactor Game.cs
- [ ] Phase 5: Testing and finalization

## Next Steps

1. Read `GAME_REFACTORING_README.md` to understand the initiative
2. Review `GAME_CS_ARCHITECTURE_DIAGRAM.md` to see the changes visually
3. Follow `GAME_CS_REFACTORING_GUIDE.md` step-by-step for implementation
4. Track progress using `GAME_CS_REFACTORING_SUMMARY.md` checklist
5. Refer to this file for file locations and dependencies

---

**Created**: November 19, 2025
**Status**: Planning & Documentation Phase Complete ✅
**Next**: Ready for Implementation Phase 🚀

