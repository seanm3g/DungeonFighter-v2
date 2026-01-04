# Test Coverage Gaps Analysis

This document identifies gameplay aspects that could benefit from additional test coverage based on the current test suite analysis.

**Organization**: Tasks are sorted by production file count (descending) to prioritize systems with the most code. This helps focus testing efforts on areas with the largest codebases first.

## Current Test Coverage Summary

### Well-Tested Areas ✅
- **Combat System**: Damage calculations, hit/miss, critical hits, status effects
- **Combo System**: Sequence management, execution, routing
- **Action System**: Loading, execution, properties
- **Dice Mechanics**: Roll generation, modifiers, exploding dice
- **Character Attributes**: Basic health, stats, leveling basics
- **Item Generation**: Item creation, properties, tier distribution
- **Dungeon Generation**: Basic dungeon creation and room generation
- **Color System**: Parsing, rendering, keyword matching
- **UI Components**: Message building, display formatting
- **Game Handlers**: Menu handlers, input routing, handler initialization (NEW)
- **Game Initialization**: Game initialization manager, game loader, file manager (NEW)
- **Game State Management**: State transitions, validation, persistence (ENHANCED)
- **UI Spacing System**: Text spacing rules, formatting, validation (NEW)
- **UI Chunking System**: Chunk strategy selection (NEW)
- **UI Block Display**: Delay management, message collection, entity extraction (NEW)
- **UI Services**: Message routing, message filtering (NEW)

## Areas Needing Additional Test Coverage

*Note: Tasks are sorted by production file count (descending) to prioritize systems with the most code.*

### 1. Game State Management (Medium Priority) - 143 Production Files
**System**: Game  
**Current Coverage**: GameStateManagerMultiCharacterTests exists  
**Missing Tests**:
- **State Transitions**: Test all valid state transitions
- **Invalid State Transitions**: Test handling of invalid transitions
- **State Persistence**: Test state is maintained across operations
- **Error Recovery**: Test recovery from invalid states
- **Concurrent State Changes**: Test handling of simultaneous state changes
- **State Validation**: Test state validation on transitions

**Suggested Test File**: `Code/Tests/Unit/GameStateManagementTests.cs`

### 2. Entity System Tests (High/Medium Priority) - 47 Production Files
**System**: Entity  
**Current Coverage**: Some tests exist for character attributes and basic operations

#### 2a. Equipment System (High Priority)
**Missing Tests**:
- **Equip/Unequip Operations**: Test equipping and unequipping items
  - Weapon equipping/unequipping
  - Armor equipping/unequipping (head, body, feet)
  - Slot validation
- **Stat Bonus Application**: Test stat bonuses are applied correctly when equipping
- **Stat Bonus Removal**: Test stat bonuses are removed correctly when unequipping
- **Action Pool Updates**: Test that action pool updates when equipment changes
- **Inventory Management**: Test adding/removing items from inventory
- **Equipment Conflicts**: Test handling of already-equipped slots
- **Null Equipment**: Test handling of null/empty equipment slots
- **Equipment Health Bonuses**: Test effective max health updates with equipment changes
- **Equipment Change Health Adjustment**: Test health adjustment when max health changes

**Suggested Test File**: `Code/Tests/Unit/EquipmentSystemTests.cs`

#### 2b. Level Up System (Medium Priority)
**Missing Tests**:
- **Multiple Level-Ups**: Test gaining enough XP to level up multiple times in one operation
- **Class Point Distribution**: Verify correct class points awarded per weapon type
  - Barbarian (Mace) points
  - Warrior (Sword) points
  - Rogue (Dagger) points
  - Wizard (Wand) points
- **Stat Increases**: Verify stat increases per class on level up
- **Health Restoration**: Test that health is restored to effective max (including equipment bonuses)
- **Class Balance Multipliers**: Test health multiplier application per class
- **Level Up Info**: Verify LevelUpInfo structure contains correct data
- **XP Overflow**: Test handling of XP that exceeds multiple level thresholds

**Suggested Test File**: `Code/Tests/Unit/LevelUpSystemTests.cs`

#### 2c. XP System (Medium Priority)
**Missing Tests**:
- **XP Calculation**: Test XP reward calculation based on player level
- **XP Scaling**: Verify XP scales correctly with dungeon level
- **Guaranteed Level-Up**: Test first-dungeon guaranteed level-up logic
- **XP Thresholds**: Test XP required for each level (Level^2 vs Level^2.2)
- **Multiple Level-Ups**: Test single XP gain triggering multiple level-ups
- **XP Persistence**: Test XP is preserved correctly across save/load

**Suggested Test File**: `Code/Tests/Unit/XPSystemTests.cs`

#### 2d. Save/Load System (Medium Priority)
**Missing Tests**:
- **Save File Validation**: Test save file structure and required fields
- **Load Error Handling**: Test handling of corrupted/invalid save files
- **Save File Versioning**: Test backward compatibility with older save formats
- **Multi-Character Saves**: Test multiple character save file management
- **Save File Cleanup**: Test deletion of save files on character death
- **Partial Save/Load**: Test saving/loading with incomplete data
- **Equipment Persistence**: Test equipment is saved/loaded correctly
- **Progression Persistence**: Test XP, level, and class points are saved/loaded correctly
- **Inventory Persistence**: Test inventory is saved/loaded correctly

**Suggested Test File**: `Code/Tests/Unit/SaveLoadSystemTests.cs`

### 3. World System - Room Generation (Low Priority) - 25 Production Files
**System**: World  
**Current Coverage**: Basic dungeon and room generation tests exist  
**Missing Tests**:
- **Room Type Distribution**: Test room type selection matches probabilities
- **Hostile/Non-Hostile Logic**: Test forced hostile room logic (last room guarantee)
- **Theme Consistency**: Test rooms match dungeon theme
- **Environmental Action Assignment**: Test environmental actions are assigned correctly
- **Room Level Scaling**: Test room levels scale with dungeon level
- **Boss Room Generation**: Test boss room appears in final position
- **Empty Room Handling**: Test handling when no rooms can be generated

**Suggested Test File**: `Code/Tests/Unit/RoomGenerationTests.cs`

### 4. Dungeon Rewards System (✅ Covered)
**Current Coverage**: Comprehensive test suite exists in `Code/Tests/Unit/DungeonRewardsTests.cs`  
**Tests Implemented**:
- ✅ **Loot Generation**: Test loot generation based on player/dungeon level (with guaranteed loot)
- ✅ **Loot Tier Calculation**: Test tier calculation logic and distribution
- ✅ **Loot Level Calculation**: Test adjusted loot level based on player vs dungeon level
- ✅ **XP Reward Calculation**: Test XP reward calculation and scaling
- ✅ **Guaranteed Level-Up Logic**: Test first-dungeon level-up guarantee
- ✅ **Loot Rarity Distribution**: Test rarity distribution matches tier distribution tables
- ✅ **Contextual Loot**: Test theme-based loot generation
- ✅ **Fallback Loot**: Test fallback loot generation when primary generation fails
- ✅ **Multiple Rewards**: Test handling of multiple items found during dungeon run
- ✅ **Reward Health Restoration**: Test health restoration on dungeon completion

**Test File**: `Code/Tests/Unit/DungeonRewardsTests.cs` (All tests passing as of latest fix)

### 5. Cross-System Integration & Error Handling

#### 5a. Error Handling & Edge Cases (High Priority) - Multiple Systems
**Current Coverage**: Some error handling in existing tests  
**Missing Tests**:
- **Null Reference Handling**: Test all methods handle null parameters gracefully
- **Invalid Data Handling**: Test handling of invalid JSON data
- **File I/O Errors**: Test handling of file read/write failures
- **Division by Zero**: Test calculations that could divide by zero
- **Negative Values**: Test handling of negative health, XP, stats
- **Overflow Conditions**: Test handling of integer overflow scenarios
- **Empty Collections**: Test handling of empty inventories, action lists, etc.
- **Boundary Conditions**: Test min/max values for all numeric properties

**Suggested Test File**: `Code/Tests/Unit/ErrorHandlingTests.cs`

#### 5b. Integration Scenarios (High Priority) - Multiple Systems
**Current Coverage**: Some integration tests exist  
**Missing Tests**:
- **Complete Dungeon Run**: Test full dungeon completion flow
  - Room progression
  - Combat encounters
  - Reward distribution
  - State updates
- **Full Character Progression**: Test character from level 1 to level 10+
  - Multiple level-ups
  - Equipment changes
  - Stat increases
  - Action pool evolution
- **Equipment Changes During Combat**: Test equipping/unequipping during combat
- **Multiple Level-Ups During Dungeon**: Test leveling up multiple times in one dungeon
- **Save/Load During Gameplay**: Test saving and loading mid-gameplay
- **Inventory Management Flow**: Test complete inventory management workflow

**Suggested Test File**: `Code/Tests/Integration/GameplayFlowTests.cs`

#### 5c. Performance & Scalability (Low Priority) - Multiple Systems
**Current Coverage**: PerformanceMonitorTest exists  
**Missing Tests**:
- **Large Inventory Handling**: Test performance with 100+ items
- **Many Enemies in Combat**: Test combat with 10+ enemies
- **Long Combo Chains**: Test performance with 20+ combo steps
- **Rapid State Changes**: Test performance with rapid state transitions
- **Memory Leaks**: Test for memory leaks in long-running sessions
- **Save File Size**: Test handling of large save files

**Suggested Test File**: `Code/Tests/Performance/ScalabilityTests.cs`

## Priority Recommendations

*Note: Tasks are now sorted by production file count to prioritize systems with the most code.*

### High Priority (Implement First)
1. **Game State Management Tests** (143 prod files) - Largest system, ensures stable state handling
2. **Equipment System Tests** (47 prod files - Entity) - Core gameplay mechanic, affects many systems
3. **Error Handling Tests** (Cross-system) - Prevents crashes and bugs
4. **Integration Scenarios** (Cross-system) - Ensures end-to-end functionality
5. ~~**Dungeon Rewards Tests**~~ ✅ **COMPLETE** - All tests implemented and passing

### Medium Priority (Implement Second)
1. **Level Up System Tests** (47 prod files - Entity) - Important for progression
2. **XP System Tests** (47 prod files - Entity) - Core progression mechanic
3. **Save/Load System Tests** (47 prod files - Entity) - Critical for game persistence

### Low Priority (Implement When Time Permits)
1. **Room Generation Tests** (25 prod files - World) - Already has basic coverage
2. **Performance Tests** (Cross-system) - Good to have but not critical

## Test Implementation Guidelines

When implementing new tests:

1. **Follow Existing Patterns**: Use `TestBase` for assertions and `TestDataBuilders` for test data
2. **Test Both Positive and Negative Cases**: Test success paths and error conditions
3. **Include Edge Cases**: Test boundary conditions, null values, empty collections
4. **Group Related Tests**: Use regions or separate methods for related test groups
5. **Document Test Purpose**: Add comments explaining what each test verifies
6. **Use Descriptive Names**: Test method names should clearly indicate what they test

## Example Test Structure

```csharp
public static class EquipmentSystemTests
{
    private static int _testsRun = 0;
    private static int _testsPassed = 0;
    private static int _testsFailed = 0;

    public static void RunAllTests()
    {
        Console.WriteLine("\n=== Equipment System Tests ===");
        
        TestEquipWeapon();
        TestUnequipWeapon();
        TestEquipArmor();
        TestStatBonusApplication();
        // ... more tests
        
        TestBase.PrintSummary("Equipment System Tests", 
            _testsRun, _testsPassed, _testsFailed);
    }

    private static void TestEquipWeapon()
    {
        Console.WriteLine("\n--- Testing Weapon Equipping ---");
        
        var character = TestDataBuilders.Character().Build();
        var weapon = TestDataBuilders.Weapon().Build();
        
        var previousWeapon = character.EquipItem(weapon, "weapon");
        
        TestBase.AssertEqual(weapon, character.Equipment.Weapon,
            "Weapon should be equipped",
            ref _testsRun, ref _testsPassed, ref _testsFailed);
    }
}
```

## Notes

- This analysis is based on the current codebase state as of the test coverage review
- Priorities may shift based on discovered bugs or new features
- Some areas may have implicit test coverage through integration tests
- Consider adding tests when fixing bugs to prevent regressions

