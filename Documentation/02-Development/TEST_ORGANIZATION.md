# Test Organization - DungeonFighter

## Overview
This document describes the test suite organization and structure implemented in October 2025.

## Test Directory Structure

```
Code/Tests/
├── README.md           # Test suite documentation
├── Unit/              # Unit tests for specific components
│   ├── ActionSystemTests.cs
│   ├── ActionExecutionFlowTests.cs
│   ├── DiceMechanicsTests.cs
│   ├── CombatOutcomeTests.cs
│   ├── RollBonusTests.cs
│   ├── ComboExecutionTests.cs
│   ├── ColorSystemCoreTests.cs
│   ├── ColorSystemApplicationTests.cs
│   ├── ColorSystemRenderingTests.cs
│   ├── CombatLogDisplayTests.cs
│   ├── CharacterAttributesTests.cs
│   ├── StatusEffectsTests.cs
│   ├── EnvironmentalActionsTests.cs
│   ├── ConditionalTriggersTests.cs
│   ├── ColorParserTest.cs (existing)
│   ├── GameDataGeneratorTest.cs (existing)
│   ├── TierDistributionTest.cs (existing)
│   └── TuningSystemTest.cs (existing)
├── Integration/       # Integration tests
│   ├── CombatIntegrationTests.cs
│   └── SystemInteractionTests.cs
├── Runners/           # Test runners
│   └── ComprehensiveTestRunner.cs
└── Examples/          # Example/demo files
    ├── ColorLayerExamples.cs
    ├── ColorSystemExamples.cs
    ├── KeywordColorExamples.cs
    └── TextFadeAnimatorExamples.cs
```

## Test Categories

### Unit Tests (`Tests/Unit/`)
**Purpose**: Test individual components in isolation

**Files**:
- **ColorParserTest.cs** - Comprehensive test suite for ColorParser functionality
  - Tests template expansion, color code parsing, length calculations, edge cases
  - ~50+ test cases covering all parser features
  
- **GameDataGeneratorTest.cs** - Tests for game data generation system
  - Tests safe generation (no overwrite)
  - Tests force overwrite generation
  - Tests individual file generation
  - Verifies configuration status
  
- **TierDistributionTest.cs** - Tests for item tier distribution
  - Validates tier distribution percentages
  - Checks item generation patterns
  - Verifies rarity calculations
  
- **TuningSystemTest.cs** - Tests for tuning and balance system
  - Tests configuration loading/saving
  - Validates balance calculations
  - Checks scaling formulas

### Integration Tests (`Tests/Integration/`)
**Purpose**: Test interaction between multiple components

**Files**:
- **CombatIntegrationTests.cs** - End-to-end combat flow tests
  - Complete combat flow
  - Multiple action sequences
  - Combo execution in combat
  - Status effect application in combat
  - Environmental effects in combat
  - Character progression through combat
  
- **SystemInteractionTests.cs** - System interaction tests
  - Action + Dice + Combo interaction
  - Status Effects + Damage interaction
  - Equipment + Stats + Actions interaction
  - Color System + Display System interaction
  - Character + Enemy + Environment interaction

**Potential Future Tests**:
- Dungeon generation flow (Dungeon + Rooms + Enemies)
- Item generation pipeline (LootGenerator + ItemGenerator + Character)
- UI rendering pipeline (UIManager + ColorSystem + TextDisplay)

### Examples (`Tests/Examples/`)
**Purpose**: Demonstrate system usage and features

**Files**:
- **ColorLayerExamples.cs** - Demonstrates color layer system
  - Event significance levels (Trivial to Critical)
  - Dungeon depth progression (Warm → Neutral → Cool whites)
  - Practical usage examples
  
- **ColorSystemExamples.cs** - Shows color system capabilities
  - Template-based coloring
  - Direct color codes
  - Mixed markup examples
  - Performance comparisons
  
- **KeywordColorExamples.cs** - Examples of keyword-based coloring
  - Damage types (Fire, Ice, Poison)
  - Status effects (Bleed, Stun, Weaken)
  - Item rarities (Common to Legendary)
  - Context-aware coloring
  
- **TextFadeAnimatorExamples.cs** - Demonstrates text animation
  - Various fade effects
  - Timing configurations
  - Multi-line animations

## Utility Services

### TestManager (`Utils/TestManager.cs`)
**Purpose**: Central test execution and management service

**Features**:
- Coordinates test execution across systems
- Generates comprehensive test reports
- Saves test results to files
- Provides test data generation

**Usage**:
```csharp
TestManager.RunAllTests();
TestManager.RunItemGenerationTest();
```

## Test Cleanup (October 2025)

### Migration Summary
**Moved Files**: 9 test/example files organized into proper structure

**From → To**:
- `Code/Utils/GameDataGeneratorTest.cs` → `Code/Tests/Unit/`
- `Code/Utils/TuningSystemTest.cs` → `Code/Tests/Unit/`
- `Code/Utils/TierDistributionTest.cs` → `Code/Tests/Unit/`
- `Code/UI/ColorParserTest.cs` → `Code/Tests/Unit/`
- `Code/UI/ColorLayerExamples.cs` → `Code/Tests/Examples/`
- `Code/UI/ColorSystemExamples.cs` → `Code/Tests/Examples/`
- `Code/UI/KeywordColorExamples.cs` → `Code/Tests/Examples/`
- `Code/UI/TextFadeAnimatorExamples.cs` → `Code/Tests/Examples/`

### Removed Obsolete Files
The following debug/test files were removed as no longer needed:
- `debug_wand_actions.cs` - Obsolete wand action debugging
- `simple_wand_test.cs` - Obsolete simple wand test
- `test_changes_system.cs` - Empty test file
- `test_wand_actions.cs` - Obsolete wand action test
- `test_wand_fix.cs` - Obsolete wand fix test

### Benefits
- **Improved Organization**: Clear separation of tests, examples, and production code
- **Better Discoverability**: Easy to find relevant tests
- **Reduced Clutter**: Removed 5 obsolete test files
- **Consistent Structure**: All tests follow standardized organization
- **Future-Ready**: Structure supports growth and new test categories

## Running Tests

### Comprehensive Test Runner
Run all tests using the comprehensive test runner:
```csharp
// Run all tests
ComprehensiveTestRunner.RunAllTests();

// Run quick tests (fast unit tests)
ComprehensiveTestRunner.RunQuickTests();

// Run specific test categories
ComprehensiveTestRunner.RunActionSystemTests();
ComprehensiveTestRunner.RunDiceMechanicsTests();
ComprehensiveTestRunner.RunComboSystemTests();
ComprehensiveTestRunner.RunColorSystemTests();
ComprehensiveTestRunner.RunDisplaySystemTests();
ComprehensiveTestRunner.RunCharacterSystemTests();
ComprehensiveTestRunner.RunStatusEffectsTests();
ComprehensiveTestRunner.RunIntegrationTests();
```

### Command Line
From the main application, tests can be accessed through:
```csharp
// In Program.cs or Game.cs
TestManager.RunAllTests();
```

### Direct Invocation
Individual tests can be run:
```csharp
// Run specific unit tests
ActionSystemTests.RunAllTests();
DiceMechanicsTests.RunAllTests();
CombatOutcomeTests.RunAllTests();
ComboExecutionTests.RunAllTests();
ColorSystemCoreTests.RunAllTests();
CharacterAttributesTests.RunAllTests();
StatusEffectsTests.RunAllTests();

// Run existing tests
ColorParserTest.RunAllTests();
GameDataGeneratorTest.TestRefactoredGenerator();
TierDistributionTest.RunTierDistributionTest();
TuningSystemTest.RunTuningSystemTests();

// Run examples
ColorLayerExamples.DemoEventSignificance();
ColorSystemExamples.ShowAllExamples();
KeywordColorExamples.DemonstrateKeywordColoring();
TextFadeAnimatorExamples.DemonstrateAllAnimations();
```

## Test Guidelines

### Writing Unit Tests
1. **Single Responsibility**: Test one thing at a time
2. **Isolation**: Mock dependencies, avoid side effects
3. **Clear Names**: Use descriptive test method names
4. **Edge Cases**: Include boundary conditions and error cases
5. **Assertions**: Use clear, specific assertions

### Writing Integration Tests
1. **Realistic Scenarios**: Use actual game workflows
2. **End-to-End**: Test complete features from start to finish
3. **Data Setup**: Create realistic test data
4. **Cleanup**: Ensure tests clean up after themselves
5. **Independence**: Tests should not depend on execution order

### Writing Examples
1. **Self-Contained**: Examples should run independently
2. **Documented**: Include comments explaining behavior
3. **Visual**: Provide visual output when applicable
4. **Practical**: Show real-world usage patterns
5. **Progressive**: Start simple, build to complex

## Adding New Tests

### 1. Determine Test Type
- **Unit**: Tests a single component in isolation
- **Integration**: Tests multiple components together
- **Example**: Demonstrates feature usage

### 2. Create Test File
- **Location**: `Tests/Unit/`, `Tests/Integration/`, or `Tests/Examples/`
- **Naming**: `[ComponentName]Test.cs` or `[FeatureName]Examples.cs`
- **Namespace**: Original namespace (e.g., `RPGGame`, `RPGGame.UI`)

### 3. Implement Tests
```csharp
namespace RPGGame.Tests.Unit
{
    public static class MyComponentTest
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== MyComponent Test Suite ===");
            TestFeatureA();
            TestFeatureB();
            // ... more tests
        }
        
        private static void TestFeatureA()
        {
            // Arrange
            var component = new MyComponent();
            
            // Act
            var result = component.DoSomething();
            
            // Assert
            if (result == expectedValue)
                Console.WriteLine("✓ TestFeatureA passed");
            else
                Console.WriteLine("✗ TestFeatureA failed");
        }
    }
}
```

### 4. Register Test
Add to TestManager if appropriate, or document in test README.

## Best Practices

### Test Organization
- Keep tests close to what they test (same namespace)
- Group related tests together
- Use clear, descriptive names
- Document complex test scenarios

### Test Data
- Use realistic test data
- Create test data builders for complex objects
- Avoid hard-coded magic values
- Clean up test data after use

### Test Output
- Use clear, actionable messages
- Include context in failure messages
- Provide visual feedback (✓/✗)
- Save detailed results to files when appropriate

### Test Maintenance
- Update tests when features change
- Remove obsolete tests promptly
- Refactor duplicated test code
- Keep tests fast and focused

## Future Enhancements

### Planned Additions
1. **Automated Test Runner**: Command-line test execution
2. **Test Coverage Reports**: Track which code is tested
3. **Performance Benchmarks**: Measure system performance
4. **Regression Test Suite**: Catch breaking changes
5. **Continuous Integration**: Automated test execution

### Test Categories to Add
- **Performance Tests**: Measure execution time and memory
- **Load Tests**: Test system under high load
- **UI Tests**: Automated UI interaction tests
- **Data Validation Tests**: Verify JSON data integrity
- **Balance Tests**: Automated game balance analysis

## Related Documentation

- **`TESTING_STRATEGY.md`**: Overall testing approach and philosophy
- **`DEBUGGING_GUIDE.md`**: Debugging techniques and tools
- **`CODE_PATTERNS.md`**: Coding patterns including test patterns
- **`DEVELOPMENT_WORKFLOW.md`**: Development process including testing

---

*Last Updated: October 2025*
*Test structure established as part of refactoring initiative*

