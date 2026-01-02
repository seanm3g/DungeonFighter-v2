# Test Integration Guide

## Overview

All new system test suites have been integrated into the comprehensive test infrastructure. The tests are ready to run and are automatically included when you run the full test suite.

## Integration Status

✅ **All tests are already integrated!** The new system test suites are automatically included in:
- `ComprehensiveTestRunner.RunAllTests()` - Runs all tests including new system tests
- GUI Test Panel - "Run All Tests" button includes all new tests
- Command-line execution - All tests run together

## How to Run the Tests

### Method 1: Run All Tests (Recommended)

**From Code:**
```csharp
using RPGGame.Tests.Runners;

// Run all tests (includes all new system tests)
ComprehensiveTestRunner.RunAllTests();
```

**From GUI:**
1. Launch the game
2. Open Settings panel
3. Go to Testing tab
4. Click "Run All Tests" button

**From Command Line:**
```bash
# Windows
Scripts\run-tests.bat

# PowerShell
Scripts\run-tests.ps1
```

### Method 2: Run Individual System Test Suites

**From Code:**
```csharp
using RPGGame.Tests.Runners;

// Run specific system tests
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

**From GUI:**
The GUI currently has buttons for existing test categories. To add buttons for new system tests, see "Adding GUI Test Buttons" below.

### Method 3: Run Individual Test Classes

**From Code:**
```csharp
using RPGGame.Tests.Unit.Data;
using RPGGame.Tests.Unit.Items;
// ... etc

// Run specific test classes
ActionLoaderTests.RunAllTests();
InventoryManagerTests.RunAllTests();
CharacterTests.RunAllTests();
// ... etc
```

## Test Organization

### New Test Suites

All new tests are organized by system in `Code/Tests/Unit/`:

```
Code/Tests/Unit/
├── Data/              # 9 test files
│   ├── ActionLoaderTests.cs
│   ├── JsonLoaderTests.cs
│   ├── LootGeneratorTests.cs
│   └── ...
├── Items/             # 4 test files
│   ├── InventoryManagerTests.cs
│   ├── ItemTests.cs
│   └── ...
├── Actions/           # 5 test files
├── Config/            # 5 test files
├── Entity/            # 6 test files
├── Combat/            # 5 test files
├── World/             # 4 test files
├── Game/              # 4 test files
└── UI/                # 3 test files
```

### Test Runners

System-specific test runners are in `Code/Tests/Runners/`:
- `DataSystemTestRunner.cs`
- `ItemsSystemTestRunner.cs`
- `ActionsSystemTestRunner.cs`
- `ConfigSystemTestRunner.cs`
- `EntitySystemTestRunner.cs`
- `CombatSystemTestRunner.cs`
- `WorldSystemTestRunner.cs`
- `GameSystemTestRunner.cs`
- `UISystemTestRunner.cs`

## Adding GUI Test Buttons (Optional)

If you want to add buttons for individual system test suites in the GUI:

### Step 1: Add Methods to TextBoxTestRunner

Edit `Code/UI/Avalonia/Managers/TextBoxTestRunner.cs`:

```csharp
/// <summary>
/// Runs data system tests
/// </summary>
public async Task RunDataSystemTestsAsync()
{
    await orchestrator.RunTestAsync(
        () => DataSystemTestRunner.RunAllTests(),
        "Running data system tests...",
        "Data system tests complete");
}

// Repeat for other systems...
```

### Step 2: Add Buttons to Testing Panel

Edit `Code/UI/Avalonia/Settings/TestingSettingsPanel.axaml` to add new buttons:

```xml
<Button Name="TestDataSystemButton" Content="Data System Tests" />
<Button Name="TestItemsSystemButton" Content="Items System Tests" />
<!-- etc -->
```

### Step 3: Wire Up Buttons

Edit `Code/UI/Avalonia/Managers/Settings/PanelHandlers/TestingPanelHandler.cs`:

```csharp
if (testDataSystemButton != null)
{
    testDataSystemButton.Click += async (s, e) =>
    {
        await textBoxTestRunner.RunDataSystemTestsAsync();
    };
}
```

## Verification

### Quick Verification

Run this to verify all tests are integrated:

```csharp
using RPGGame.Tests.Runners;

// This should include all new system tests in Phase 9
ComprehensiveTestRunner.RunAllTests();
```

You should see output like:
```
=== PHASE 9: SYSTEM-SPECIFIC TESTS ===

========================================
  DATA SYSTEM TEST SUITE
========================================
...
```

### Check Test Count

The test summary at the end will show the total number of tests run, including all new system tests.

## Test Execution Flow

```
ComprehensiveTestRunner.RunAllTests()
  ├── Phase 1-8: Existing tests
  └── Phase 9: System-Specific Tests (NEW)
      ├── DataSystemTestRunner.RunAllTests()
      ├── ItemsSystemTestRunner.RunAllTests()
      ├── ActionsSystemTestRunner.RunAllTests()
      ├── ConfigSystemTestRunner.RunAllTests()
      ├── EntitySystemTestRunner.RunAllTests()
      ├── CombatSystemTestRunner.RunAllTests()
      ├── WorldSystemTestRunner.RunAllTests()
      ├── GameSystemTestRunner.RunAllTests()
      └── UISystemTestRunner.RunAllTests()
```

## Troubleshooting

### Tests Not Appearing

If tests don't appear in the output:

1. **Check Build**: Ensure the project builds successfully
   ```bash
   dotnet build Code/Code.csproj
   ```

2. **Check Namespaces**: Verify using statements are correct
   ```csharp
   using RPGGame.Tests.Runners;
   ```

3. **Check Integration**: Verify `ComprehensiveTestRunner.cs` includes Phase 9

### Compilation Errors

If you get compilation errors:

1. **Missing References**: Ensure all test files have proper using statements
2. **TestDataBuilders**: Some tests use `TestDataBuilders` - ensure it's accessible
3. **Namespace Issues**: Check that test classes are in correct namespaces

### Runtime Errors

If tests crash at runtime:

1. **Data Files**: Some tests require JSON data files in `GameData/`
2. **Initialization**: Some systems need initialization before testing
3. **Dependencies**: Check that all dependencies are loaded

## Next Steps

1. **Run All Tests**: Execute `ComprehensiveTestRunner.RunAllTests()` to verify integration
2. **Review Results**: Check test output for any failures
3. **Fix Issues**: Address any failing tests
4. **Add More Tests**: Expand coverage as needed

## Additional Resources

- **Test Documentation**: `Code/Tests/TEST_SUITE_SUMMARY.md`
- **Test Organization**: `Documentation/02-Development/TEST_ORGANIZATION.md`
- **Testing Strategy**: `Documentation/03-Quality/TESTING_STRATEGY.md`
