# Testing System - DungeonFighter

## Overview

The Testing System provides comprehensive analysis tools for game balance and item generation. It's accessible through the Settings menu and provides detailed statistical analysis of game mechanics.

## Accessing Tests

1. Start the game
2. Go to **Settings** (option 3 from main menu)
3. Select **Tests** (option 2 from settings menu)
4. Choose the test you want to run

## Available Tests

### Test 1: Item Generation Analysis

**Purpose**: Analyzes item generation patterns across levels 1-20 to verify balance and distribution.

**What it does**:
- Generates 100 items at each level from 1-20 (2,000 total items)
- Analyzes rarity distribution (Common, Uncommon, Rare, Epic, Legendary)
- Tracks tier distribution (Tiers 1-5)
- Monitors modification frequency and types
- Records stat bonus and action bonus distributions
- Tracks item type distribution (Weapons vs Armor)

**Output**:
- Real-time console display with summary statistics
- Detailed text file: `item_generation_test_results.txt`
- Level-by-level breakdown
- Overall distribution analysis
- Top 20 most common modifications, stat bonuses, and action bonuses

**Use Cases**:
- Verify that rarity distribution matches expected percentages
- Check if higher levels produce better items
- Analyze modification balance
- Identify any anomalies in item generation
- Balance testing and tuning

### Test 2: Tier Distribution Verification

**Purpose**: Verifies that tier distribution works correctly across different character and dungeon level combinations.

**What it does**:
- Tests tier distribution across various player/dungeon level scenarios
- Covers early game (levels 1-2), mid game (levels 20-30), high level (levels 40-60), and end game (levels 80-100)
- Tests same-level, underleveled, and overleveled scenarios
- Generates 500 items per scenario to verify tier distribution
- Compares results against expected values from TierDistribution.json
- Tests extreme scenarios (level 1 in level 100 dungeon, etc.)

**Output**:
- Real-time console display showing tier distribution for each scenario
- Calculated loot level for each scenario
- Expected vs actual tier percentages for levels 1-20
- Summary analysis of tier distribution behavior

**Use Cases**:
- Verify that underleveled characters get higher tier loot (reward for challenge)
- Ensure overleveled characters get lower tier loot (prevents farming)
- Test loot level calculation formula across all level ranges
- Validate tier distribution matches configuration expectations
- Balance testing for different difficulty scenarios

## Test Results File

The test results are saved to `item_generation_test_results.txt` in the game directory and include:

### Overall Statistics
- Total items generated
- Levels tested
- Items per level

### Distribution Analysis
- **Rarity Distribution**: Percentage breakdown of Common, Uncommon, Rare, Epic, Legendary items
- **Tier Distribution**: Percentage breakdown of Tiers 1-5
- **Item Type Distribution**: Weapons vs Armor breakdown

### Level-by-Level Breakdown
- Detailed statistics for each level (1-20)
- Rarity distribution per level
- Tier distribution per level
- Item type distribution per level

### Top Lists
- **Top 20 Modifications**: Most frequently generated modifications
- **Top 20 Stat Bonuses**: Most frequently generated stat bonuses
- **Top 20 Action Bonuses**: Most frequently generated action bonuses

## Technical Details

### TestManager Class
- **Location**: `Code/Utils/TestManager.cs`
- **Purpose**: Centralized test execution and analysis
- **Key Methods**:
  - `RunItemGenerationTest()`: Main test execution
  - `GenerateItemsForLevel()`: Generates items for a specific level
  - `DisplayTestResults()`: Console output formatting
  - `SaveTestResults()`: File output generation

### Integration
- **Menu System**: Integrated into Settings menu via `SettingsManager`
- **Menu Configuration**: Uses `MenuConfiguration.GetTestsMenuOptions()`
- **UI Integration**: Uses `TextDisplayIntegration` for consistent display

### Data Collection
- **Rarity Tracking**: Monitors item rarity distribution
- **Tier Analysis**: Tracks item tier distribution
- **Modification Analysis**: Records all modifications applied
- **Bonus Tracking**: Monitors stat and action bonuses
- **Item Type Tracking**: Distinguishes between weapons and armor

## Expected Results

### Rarity Distribution (Based on RarityTable.json)
- **Common**: ~87.7% (500/570 weight)
- **Uncommon**: ~8.8% (50/570 weight)
- **Rare**: ~2.6% (15/570 weight)
- **Epic**: ~0.5% (3/570 weight)
- **Legendary**: ~0.2% (1/570 weight)

### Tier Distribution
- Should show progression from lower tiers at low levels to higher tiers at high levels
- Tier distribution should follow the TierDistribution.json configuration

### Modifications
- Should show balanced distribution across different modification types
- Higher-tier items should have more modifications
- Reroll modifications should occasionally appear

## Troubleshooting

### Common Issues
1. **Test takes too long**: Normal for 2,000 item generation
2. **File not found errors**: Ensure game data files are present
3. **Memory issues**: Test uses significant memory for large datasets

### Performance Notes
- Test execution time: ~30-60 seconds depending on system
- Memory usage: ~50-100MB during test execution
- File size: Results file typically 50-100KB

## Future Enhancements

### Planned Tests
- **Combat Balance Test**: Analyze damage scaling and combat duration
- **Enemy Generation Test**: Test enemy scaling and balance
- **Dungeon Generation Test**: Analyze dungeon generation patterns
- **Performance Test**: Measure game performance metrics

### Test Framework Extensions
- Configurable test parameters
- Automated test scheduling
- Test result comparison
- Statistical significance testing
- Regression testing capabilities

## Related Documentation

- **`ARCHITECTURE.md`**: System architecture overview
- **`TESTING_STRATEGY.md`**: General testing approaches
- **`CODE_PATTERNS.md`**: Code patterns and conventions
- **`PROBLEM_SOLUTIONS.md`**: Solutions to common issues

---

*This testing system provides essential tools for maintaining game balance and ensuring consistent item generation across all levels.*
