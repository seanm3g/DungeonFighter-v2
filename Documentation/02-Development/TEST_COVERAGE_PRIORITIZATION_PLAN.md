# Test Coverage Prioritization Plan

**Date**: Generated from analysis script execution  
**Analysis Script**: `Scripts/analyze-top-classes.ps1`  
**Current Coverage**: UI (11.5% files, 13.9% lines), Game (14.7% files, 16.3% lines), Combat (56.9% files, 94.9% lines)

## Executive Summary

Analysis of the top 10 largest classes in UI, Game, and Combat systems reveals:
- **Top 3 largest classes already have tests** (CanvasUICoordinator, CanvasRenderer, SettingsManager)
- **UI System**: All top 10 classes have tests, but coverage is still low (11.5%) - indicates many smaller classes need tests
- **Game System**: 6/10 top classes have tests, 4 need tests
- **Combat System**: 7/10 top classes have tests, 3 need tests

## Analysis Results

### Top 3 Largest Classes (All Have Tests ✅)

| System | Class | Lines | Test Status |
|--------|-------|-------|-------------|
| UI | CanvasUICoordinator.cs | 666 | ✅ Has test |
| UI | CanvasRenderer.cs | 555 | ✅ Has test |
| UI | SettingsManager.cs | 548 | ✅ Has test |

### UI System - Top 10 Analysis

| Rank | Class | Lines | Test Status | Notes |
|------|-------|-------|-------------|-------|
| 1 | CanvasUICoordinator.cs | 666 | ✅ CanvasUICoordinatorTests.cs | Core UI coordinator |
| 2 | CanvasRenderer.cs | 555 | ✅ CanvasRendererTests.cs | Rendering engine |
| 3 | SettingsManager.cs | 548 | ✅ SettingsManagerTests.cs | Settings management |
| 4 | DungeonRenderer.cs | 479 | ✅ DungeonRendererTests.cs | Dungeon visualization |
| 5 | ItemModifiersTabManager.cs | 476 | ✅ ItemModifiersTabManagerTests.cs | Item UI management |
| 6 | BlockDisplayManager.cs | 431 | ✅ BlockDisplayManagerTests.cs | Text block display |
| 7 | GameCanvasControl.cs | 430 | ✅ GameCanvasControlTests.cs | Canvas control |
| 8 | ItemsTabManager.cs | 414 | ✅ ItemsTabManagerTests.cs | Items tab UI |
| 9 | KeywordColorSystem.cs | 407 | ✅ KeywordColorSystemTests.cs | Color system |
| 10 | DisplayRenderer.cs | 399 | ✅ DisplayRendererTests.cs | Display rendering |

**UI System Status**: All top 10 classes have tests. Low coverage (11.5%) indicates many smaller classes need tests.

### Game System - Top 10 Analysis

| Rank | Class | Lines | Test Status | Notes |
|------|-------|-------|-------------|-------|
| 1 | GameStateManager.cs | 489 | ✅ GameStateManagerTests.cs | State management |
| 2 | DungeonDisplayManager.cs | 423 | ✅ DungeonDisplayManagerTests.cs | Dungeon display |
| 3 | ActionEditorHandler.cs | 399 | ✅ ActionEditorHandlerTests.cs | Action editing |
| 4 | CharacterManagementHandler.cs | 398 | ✅ CharacterManagementHandlerTests.cs | Character management |
| 5 | Game.cs (GameCoordinator) | 395 | ✅ GameCoordinatorTests.cs | Main game coordinator |
| 6 | AdjustmentExecutor.cs | 374 | ✅ AdjustmentExecutorTests.cs | Tuning adjustments |
| 7 | ClaudeAIGamePlayer.cs | 367 | ❌ **NO TEST** | AI gameplay automation |
| 8 | BattleStatisticsHandler.cs | 351 | ✅ BattleStatisticsHandlerTests.cs | Battle stats |
| 9 | MatchupAnalyzer.cs | 348 | ❌ **NO TEST** | Matchup analysis |
| 10 | GameScreenCoordinator.cs | 337 | ❌ **NO TEST** | Screen coordination |

**Game System Status**: 7/10 top classes have tests. 3 classes need tests.

### Combat System - Top 10 Analysis

| Rank | Class | Lines | Test Status | Notes |
|------|-------|-------|-------------|-------|
| 1 | BattleNarrative.cs | 490 | ✅ BattleNarrativeTests.cs | Battle narrative |
| 2 | CombatEffectsSimplified.cs | 461 | ✅ CombatEffectsSimplifiedTests.cs | Status effects |
| 3 | BattleEventAnalyzer.cs | 461 | ✅ BattleEventAnalyzerTests.cs | Event analysis |
| 4 | CombatResultsColoredText.cs | 391 | ❌ **NO TEST** | Colored text formatting |
| 5 | DamageFormatter.cs | 353 | ❌ **NO TEST** | Damage formatting |
| 6 | TurnManager.cs | 336 | ✅ TurnManagerTests.cs | Turn management |
| 7 | DamageCalculator.cs | 323 | ✅ DamageCalculatorTests.cs | Damage calculation |
| 8 | CombatManager.cs | 317 | ✅ CombatManagerTests.cs | Combat orchestration |
| 9 | BattleNarrativeFormatters.cs | 313 | ❌ **NO TEST** | Narrative formatting |
| 10 | CombatTurnHandlerSimplified.cs | 311 | ✅ CombatTurnHandlerTests.cs | Turn handling (tested via CombatTurnHandlerTests) |

**Combat System Status**: 8/10 top classes have tests. 2 classes need tests.

## Prioritized Test Plan

### Tier 1: High Priority - Missing Tests for Large Classes

These are the largest classes without tests. They should be prioritized first.

**Total Tier 1 Classes**: 6 classes (3 Game, 3 Combat)

#### Game System (3 classes)

1. **ClaudeAIGamePlayer.cs** (367 lines)
   - **Location**: `Code/Game/ClaudeAIGamePlayer.cs`
   - **Test File**: `Code/Tests/Unit/Game/ClaudeAIGamePlayerTests.cs`
   - **Key Methods to Test**:
     - AI gameplay automation logic
     - Command parsing and execution
     - Game state interaction
   - **Dependencies**: GameCoordinator, GameStateManager
   - **Complexity**: Medium (requires mocking game state)
   - **Estimated Effort**: 2-3 hours

2. **MatchupAnalyzer.cs** (348 lines)
   - **Location**: `Code/Game/MatchupAnalyzer.cs`
   - **Test File**: `Code/Tests/Unit/Game/MatchupAnalyzerTests.cs`
   - **Key Methods to Test**:
     - Matchup calculation logic
     - Statistical analysis
     - Result formatting
   - **Dependencies**: Character data, combat data
   - **Complexity**: Medium
   - **Estimated Effort**: 2-3 hours

3. **GameScreenCoordinator.cs** (337 lines)
   - **Location**: `Code/Game/GameScreenCoordinator.cs`
   - **Test File**: `Code/Tests/Unit/Game/GameScreenCoordinatorTests.cs`
   - **Key Methods to Test**:
     - Screen transition logic
     - Screen state management
     - Navigation coordination
   - **Dependencies**: GameStateManager, UI components
   - **Complexity**: Medium-High (requires UI mocking)
   - **Estimated Effort**: 3-4 hours

#### Combat System (2 classes)

4. **CombatResultsColoredText.cs** (391 lines)
   - **Location**: `Code/Combat/CombatResultsColoredText.cs`
   - **Test File**: `Code/Tests/Unit/Combat/CombatResultsColoredTextTests.cs`
   - **Key Methods to Test**:
     - Colored text generation
     - Formatting logic
     - Color code application
   - **Dependencies**: Combat results, color system
   - **Complexity**: Low-Medium
   - **Estimated Effort**: 2 hours

5. **DamageFormatter.cs** (353 lines)
   - **Location**: `Code/Combat/Formatting/DamageFormatter.cs`
   - **Test File**: `Code/Tests/Unit/Combat/DamageFormatterTests.cs`
   - **Key Methods to Test**:
     - Damage value formatting
     - Text generation
     - Color application
   - **Dependencies**: Damage data, color system
   - **Complexity**: Low-Medium
   - **Estimated Effort**: 2 hours

6. **BattleNarrativeFormatters.cs** (313 lines)
   - **Location**: `Code/Combat/BattleNarrativeFormatters.cs`
   - **Test File**: `Code/Tests/Unit/Combat/BattleNarrativeFormattersTests.cs`
   - **Key Methods to Test**:
     - Narrative text formatting
     - Event formatting
     - Text generation logic
   - **Dependencies**: Battle events, narrative data
   - **Complexity**: Low-Medium
   - **Estimated Effort**: 2 hours

7. ~~**CombatTurnHandlerSimplified.cs** (311 lines)~~ ✅ **ALREADY TESTED**
   - **Location**: `Code/Combat/CombatTurnHandlerSimplified.cs`
   - **Test File**: `Code/Tests/Unit/Combat/CombatTurnHandlerTests.cs` (covers CombatTurnHandlerSimplified)
   - **Status**: Already has comprehensive test coverage

### Tier 2: Medium Priority - Systematic Coverage

Focus on classes in the 200-300 line range that are critical to system functionality but don't have tests yet. These should be identified by:
1. Running a more comprehensive analysis of all classes in UI/Game/Combat
2. Identifying classes without tests in the 200-300 line range
3. Prioritizing by system (UI and Game have lower coverage)

**Action Items**:
- Create a script to identify all classes 200+ lines without tests
- Generate a comprehensive list for Tier 2 prioritization
- Focus on UI system first (lowest coverage at 11.5%)

### Tier 3: Comprehensive Coverage

Focus on smaller classes (<200 lines) to achieve comprehensive coverage. These are quick wins that can significantly improve coverage percentages.

**Action Items**:
- Identify all classes <200 lines without tests
- Group by system and functionality
- Create batch test creation plan

## Test Creation Guidelines

### Test File Structure

Follow the established pattern from existing tests:

```csharp
using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.[System]
{
    /// <summary>
    /// Comprehensive tests for [ClassName]
    /// </summary>
    public static class [ClassName]Tests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all [ClassName] tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== [ClassName] Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            // Test methods here

            TestBase.PrintSummary("[ClassName] Tests", _testsRun, _testsPassed, _testsFailed);
        }

        // Test methods using TestBase assertions
    }
}
```

### Integration with Test Runners

Update the appropriate system test runner to include new tests:

- **Game System**: Update `Code/Tests/Runners/GameSystemTestRunner.cs`
- **Combat System**: Update `Code/Tests/Runners/CombatSystemTestRunner.cs`

### Test Patterns

- Use `TestBase` for assertions (AssertTrue, AssertEqual, AssertNotNull, etc.)
- Use `TestDataBuilders` for creating test objects
- Follow naming convention: `Test[MethodName]` or `Test[FeatureName]`
- Include edge case testing (null handling, error cases)
- Test both positive and negative scenarios
- Group related tests using regions

## Implementation Roadmap

### Phase 1: Tier 1 Tests (Immediate - 1-2 weeks)

**Goal**: Add tests for all 6 Tier 1 classes

1. **Week 1**: Game System Tests
   - ClaudeAIGamePlayerTests.cs
   - MatchupAnalyzerTests.cs
   - GameScreenCoordinatorTests.cs

2. **Week 2**: Combat System Tests
   - CombatResultsColoredTextTests.cs
   - DamageFormatterTests.cs
   - BattleNarrativeFormattersTests.cs

**Expected Outcome**: 
- Game System coverage: 14.7% → ~18-20%
- Combat System coverage: 56.9% → ~60-65%

### Phase 2: Tier 2 Analysis (2-3 weeks)

**Goal**: Identify and test medium-sized classes (200-300 lines)

1. Create comprehensive analysis script
2. Identify all 200+ line classes without tests
3. Prioritize by system and criticality
4. Create tests in batches

**Expected Outcome**:
- UI System coverage: 11.5% → ~20-25%
- Game System coverage: ~20% → ~30-35%
- Combat System coverage: ~65% → ~75-80%

### Phase 3: Tier 3 Comprehensive Coverage (Ongoing)

**Goal**: Achieve comprehensive test coverage

1. Focus on smaller classes (<200 lines)
2. Batch creation of tests
3. Continuous improvement

**Target Coverage**:
- UI System: 30%+ file coverage
- Game System: 40%+ file coverage
- Combat System: 80%+ file coverage (maintain current high coverage)

## Metrics and Tracking

### Current Metrics

- **UI System**: 279 production files, 32 test files (11.5% file coverage, 13.9% line coverage)
- **Game System**: 143 production files, 21 test files (14.7% file coverage, 16.3% line coverage)
- **Combat System**: 65 production files, 37 test files (56.9% file coverage, 94.9% line coverage)

### Success Criteria

- **Tier 1 Complete**: All 6 large classes have comprehensive tests
- **Tier 2 Complete**: 50%+ of 200+ line classes have tests
- **Tier 3 Complete**: 30%+ overall file coverage for UI/Game systems

## Notes

- The analysis script focuses on UI, Game, and Combat systems
- Entity system (like `CharacterFileManager`) may need separate analysis
- File I/O classes will require mocking file system operations
- UI classes may need UI framework mocking (Avalonia)
- Some classes may have been refactored since the analysis (e.g., Game.cs → GameCoordinator)

## Next Steps

1. ✅ Run analysis script (completed)
2. ✅ Create prioritized test plan (this document)
3. ⏳ Begin Tier 1 test creation (Game System first)
4. ⏳ Update test runners as tests are added
5. ⏳ Create Tier 2 analysis script
6. ⏳ Track coverage improvements

## References

- Test Integration Guide: `Documentation/02-Development/TEST_INTEGRATION_GUIDE.md`
- Test Suite Summary: `Code/Tests/TEST_SUITE_SUMMARY.md`
- Testing Strategy: `Documentation/03-Quality/TESTING_STRATEGY.md`
- Analysis Script: `Scripts/analyze-top-classes.ps1`
