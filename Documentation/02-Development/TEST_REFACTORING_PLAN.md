# Test Refactoring Plan

## Overview
This document outlines the plan to refactor large test classes into smaller, more focused test files.

## Target Files

### 1. GameSystemTestRunner.cs (2390 lines)
**Split into:**
- `Runners/CharacterSystemTestRunner.cs`
- `Runners/CombatSystemTestRunner.cs`
- `Runners/InventorySystemTestRunner.cs`
- `Runners/DungeonSystemTestRunner.cs`
- `Runners/UISystemTestRunner.cs`
- `Runners/DataLoadingTestRunner.cs`
- `Runners/SaveLoadTestRunner.cs`
- `Runners/ActionSystemTestRunner.cs`
- `Runners/ColorSystemTestRunner.cs`
- `Runners/CombatUITestRunner.cs`
- `Runners/IntegrationTestRunner.cs`
- `Runners/AnalysisTestRunner.cs`

### 2. RollModificationTest.cs / AdvancedMechanicsTest (1305 lines)
**Split into:**
- `Unit/RollModificationTests.Phase1.cs` - Roll Modification & Conditional Triggers
- `Unit/RollModificationTests.Phase2.cs` - Advanced Status Effects
- `Unit/RollModificationTests.Phase3.cs` - Tag System & Combo Routing
- `Unit/RollModificationTests.Phase4.cs` - Outcome-Based Actions & Meta-Progression

### 3. ActionEditorTest.cs (879 lines)
**Split into:**
- `Unit/ActionEditorTests.Core.cs` - Create, Update, Delete, Get
- `Unit/ActionEditorTests.Validation.cs` - All validation tests
- `Unit/ActionEditorTests.EdgeCases.cs` - Edge cases and error handling

### 4. ComboDiceRollTests.cs (816 lines)
**Split into:**
- `Unit/ComboDiceRollTests.DiceMechanics.cs` - Dice roll mechanics
- `Unit/ComboDiceRollTests.ActionSelection.cs` - Action selection based on rolls
- `Unit/ComboDiceRollTests.ComboSequences.cs` - Combo sequence information
- `Unit/ComboDiceRollTests.ConditionalTriggers.cs` - Conditional trigger tests
- `Unit/ComboDiceRollTests.Integration.cs` - Integration tests

### 5. ColoredTextVisualTests.cs (763 lines)
**Split into:**
- `Unit/ColoredTextVisualTests.Basic.cs` - Tests 1-4 (Basic, Multi-Color, Templates, Multi-Word)
- `Unit/ColoredTextVisualTests.Advanced.cs` - Tests 5-8 (Whitespace, Long Text, Special Chars, Transitions)
- `Unit/ColoredTextVisualTests.RealWorld.cs` - Tests 9-12 (Edge Cases, Real-World, Combat Log, Roll Info)

### 6. CombatLogSpacingTest.cs (655 lines)
**Split into:**
- `Unit/CombatLogSpacingTests.Core.cs` - Core spacing logic
- `Unit/CombatLogSpacingTests.Integration.cs` - ColoredText integration
- `Unit/CombatLogSpacingTests.Combat.cs` - Combat-specific spacing tests

### 7. ColorConfigurationLoaderTest.cs (621 lines)
**Split into:**
- `Unit/ColorConfigurationLoaderTests.Core.cs` - Core functionality
- `Unit/ColorConfigurationLoaderTests.Cache.cs` - Cache behavior
- `Unit/ColorConfigurationLoaderTests.EdgeCases.cs` - Edge cases and recursion prevention

### 8. TextDelayConfigurationTest.cs (508 lines)
**Split into:**
- `Unit/TextDelayConfigurationTests.Core.cs` - Core functionality tests
- `Unit/TextDelayConfigurationTests.EdgeCases.cs` - Edge cases and thread safety

### 9. CombatSystemTests.cs (507 lines)
**Split into:**
- `Unit/CombatSystemTests.Damage.cs` - Damage calculation tests
- `Unit/CombatSystemTests.HitMiss.cs` - Hit/miss calculation tests
- `Unit/CombatSystemTests.StatusEffects.cs` - Status effect tests
- `Unit/CombatSystemTests.Flow.cs` - Combat flow tests

## Implementation Strategy

1. **Create new split files** - One file per logical grouping
2. **Update RunAllTests()** - Each split file has its own RunAllTests() that can be called independently
3. **Update parent test class** - Original test class becomes a coordinator that calls all split test classes
4. **Maintain backward compatibility** - Original test class names remain and delegate to split classes

## Benefits

- **Easier navigation** - Smaller files are easier to understand and navigate
- **Better organization** - Related tests are grouped together
- **Parallel development** - Multiple developers can work on different test files
- **Faster compilation** - Smaller files compile faster
- **Better IDE performance** - IDEs handle smaller files better

## Migration Notes

- All existing test calls will continue to work
- Test results and output format remain the same
- No changes needed to test runners or test infrastructure

