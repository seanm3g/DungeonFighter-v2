# Test Suite Organization

This directory contains all test files organized by type and purpose.

## Directory Structure

### `Unit/`
Contains unit tests for specific components:
- **`ActionEditorTest.cs`** - Comprehensive tests for ActionEditor (create, update, delete, validation)
- **`BattleNarrativeManagersTest.cs`** - Tests for battle narrative managers
- **`ColorConfigurationLoaderTest.cs`** - Comprehensive tests for unified color configuration loader (includes infinite recursion prevention)
- **`CombatLogSpacingTest.cs`** - Comprehensive tests for combat log spacing system
- **`GameDataGeneratorTest.cs`** - Tests for game data generation system
- **`TierDistributionTest.cs`** - Tests for item tier distribution
- **`TextDelayConfigurationTest.cs`** - Comprehensive tests for text delay configuration system
- **`TuningSystemTest.cs`** - Tests for tuning and balance system

### `Integration/`
Contains integration tests for system interactions:
- *(Reserved for future integration tests)*

### `Examples/`
Contains example/demo files showing system usage:
- **`ColorLayerExamples.cs`** - Demonstrates color layer system for brightness/saturation
- **`ColorSystemExamples.cs`** - Shows various color system capabilities
- **`KeywordColorExamples.cs`** - Examples of keyword-based coloring
- **`TextFadeAnimatorExamples.cs`** - Demonstrates text animation features

## Running Tests

### From Code - Comprehensive Test Suite (Recommended)
```csharp
using RPGGame.Tests.Runners;

// Run all tests (includes all new system tests)
ComprehensiveTestRunner.RunAllTests();

// Run specific system test suites
DataSystemTestRunner.RunAllTests();
ItemsSystemTestRunner.RunAllTests();
ActionsSystemTestRunner.RunAllTests();
ConfigSystemTestRunner.RunAllTests();
EntitySystemTestRunner.RunAllTests();
CombatSystemTestRunner.RunAllTests();
WorldSystemTestRunner.RunAllTests();
GameSystemTestRunner.RunAllTests();
UISystemTestRunner.RunAllTests();
```

### From Code - Individual Test Classes
```csharp
// Data system tests
using RPGGame.Tests.Unit.Data;
ActionLoaderTests.RunAllTests();
JsonLoaderTests.RunAllTests();

// Items system tests
using RPGGame.Tests.Unit.Items;
InventoryManagerTests.RunAllTests();
ItemTests.RunAllTests();

// And so on for other systems...
```

### From Code - Legacy Test Manager
```csharp
// Run comprehensive test suite (legacy method)
TestManager.RunAllTests();

// Run specific legacy tests
ActionEditorTest.RunAllTests();
BattleNarrativeManagersTest.RunAllTests();
ColorConfigurationLoaderTest.RunAllTests();
CombatLogSpacingTest.RunAllTests();
GameDataGeneratorTest.TestRefactoredGenerator();
TierDistributionTest.RunTierDistributionTest();
TextDelayConfigurationTest.RunAllTests();
TuningSystemTest.RunTuningSystemTests();
```

### From Examples
```csharp
// Color system demonstrations
ColorLayerExamples.DemoEventSignificance();
ColorLayerExamples.DemoDungeonDepthProgression();
ColorSystemExamples.ShowAllExamples();
KeywordColorExamples.DemonstrateKeywordColoring();
TextFadeAnimatorExamples.DemonstrateAllAnimations();
```

## Test Guidelines

### Unit Tests
- Test individual components in isolation
- Mock dependencies when necessary
- Focus on single responsibility
- Include edge cases and error conditions

### Integration Tests
- Test interaction between multiple components
- Use realistic data and scenarios
- Verify end-to-end workflows
- Check system behavior under load

### Examples
- Demonstrate practical usage
- Show best practices
- Provide visual output when applicable
- Include comments explaining behavior

## Adding New Tests

1. **Unit Tests**: Add to `Unit/` directory
   - Name: `[ComponentName]Test.cs`
   - Namespace: `RPGGame.Tests.Unit`

2. **Integration Tests**: Add to `Integration/` directory
   - Name: `[SystemName]IntegrationTest.cs`
   - Namespace: `RPGGame.Tests.Integration`

3. **Examples**: Add to `Examples/` directory
   - Name: `[FeatureName]Examples.cs`
   - Namespace: `RPGGame.Tests.Examples`

## Test Cleanup (October 2025)

### Removed Obsolete Files
The following debug files were removed as they are no longer needed:
- `debug_wand_actions.cs` - Obsolete wand action debugging
- `simple_wand_test.cs` - Obsolete simple wand test
- `test_changes_system.cs` - Empty test file
- `test_wand_actions.cs` - Obsolete wand action test
- `test_wand_fix.cs` - Obsolete wand fix test

### Migration
Tests were moved from scattered locations:
- `Code/Utils/*Test.cs` → `Code/Tests/Unit/`
- `Code/UI/*Test.cs` → `Code/Tests/Unit/`
- `Code/UI/*Examples.cs` → `Code/Tests/Examples/`

## Notes

- `TestManager.cs` remains in `Utils/` as it's a utility service used across the application
- Test files use their original namespaces to avoid breaking references
- Examples can be run independently to demonstrate features

