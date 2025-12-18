# MCP Balance Simulation and Adjustment Tools

This document describes the MCP tools available for running battle simulations, analyzing results, and adjusting game balance. These tools allow Claude Code to automatically test and tune game balance.

## Overview

The balance simulation tools enable:
1. **Running Battle Simulations**: Test all weapon types against all enemy types
2. **Analyzing Results**: Get detailed analysis reports with issues and recommendations
3. **Validating Balance**: Automated validation checks against target metrics
4. **Adjusting Balance**: Modify enemy multipliers, archetypes, and weapon scaling
5. **Managing Patches**: Save and load balance configurations

## Simulation Tools

### `run_battle_simulation`

Runs comprehensive battle simulations testing all weapon types against all enemy types.

**Parameters:**
- `battlesPerCombination` (int, default: 50): Number of battles per weapon-enemy combination
- `playerLevel` (int, default: 1): Player level for testing
- `enemyLevel` (int, default: 1): Enemy level for testing

**Returns:**
- Overall win rate, total battles, wins/losses
- Per-weapon statistics (win rates, average turns, damage)
- Per-enemy statistics (win rates, average turns, damage received)
- Per-combination results (detailed stats for each weapon vs enemy matchup)

**Example:**
```
Run 100 battles per combination at level 1
```

### `run_parallel_battles`

Runs parallel battles with custom player and enemy stats. Useful for testing specific stat combinations.

**Parameters:**
- `playerDamage` (int): Player damage stat
- `playerAttackSpeed` (double): Player attack speed
- `playerArmor` (int): Player armor stat
- `playerHealth` (int): Player health stat
- `enemyDamage` (int): Enemy damage stat
- `enemyAttackSpeed` (double): Enemy attack speed
- `enemyArmor` (int): Enemy armor stat
- `enemyHealth` (int): Enemy health stat
- `numberOfBattles` (int, default: 100): Number of battles to run

**Returns:**
- Win rate, average turns, damage statistics
- Min/max turn counts

**Example:**
```
Test player with 50 damage, 1.0 attack speed, 10 armor, 100 health
vs enemy with 40 damage, 1.2 attack speed, 8 armor, 80 health
```

## Analysis Tools

### `analyze_battle_results`

Analyzes the most recent battle simulation results and generates a detailed analysis report.

**Note:** Must run `run_battle_simulation` first to generate results.

**Returns:**
- Matchup analysis (weapon vs enemy win rates with status indicators)
- Issues detected (win rates outside target range, combat duration issues)
- Recommendations for fixes
- Weapon and enemy average statistics
- Formatted text report

**Status Indicators:**
- `GOOD`: Win rate 85-98%, combat duration 8-15 turns
- `WARNING`: Win rate 80-85% or >98%, or duration slightly off
- `CRITICAL`: Win rate <80% or severe duration issues

**Example:**
```
After running simulation, analyze results to identify problematic matchups
```

### `validate_balance`

Validates balance based on the most recent battle simulation results.

**Note:** Must run `run_battle_simulation` first to generate results.

**Returns:**
- Validation status (PASS/FAIL)
- Checks passed/total
- Errors (critical issues that must be fixed)
- Warnings (issues that should be addressed)
- Formatted validation report

**Validation Checks:**
1. Target win rate range (85-98%)
2. Combat duration (8-15 turns)
3. Weapon balance variance (<10%)
4. Enemy differentiation (>3% variance)
5. Overall win rate (85-98%)
6. Critical matchups (<80% or >98%)

**Example:**
```
After running simulation, validate balance to check if targets are met
```

## Balance Adjustment Tools

### `adjust_global_enemy_multiplier`

Adjusts global enemy multipliers that affect all enemies.

**Parameters:**
- `multiplierName` (string): One of: "health", "damage", "armor", "speed"
- `value` (double): New multiplier value (e.g., 1.2 for 20% increase)

**Returns:**
- Success status
- Current multiplier values

**Example:**
```
Increase enemy health by 20%: adjust_global_enemy_multiplier("health", 1.2)
Decrease enemy damage by 10%: adjust_global_enemy_multiplier("damage", 0.9)
```

### `adjust_archetype`

Adjusts archetype stat multipliers for specific enemy archetypes.

**Parameters:**
- `archetypeName` (string): Archetype name (e.g., "Berserker", "Tank", "Assassin")
- `statName` (string): One of: "health", "strength", "agility", "technique", "intelligence", "armor"
- `value` (double): New stat value

**Returns:**
- Success status
- Updated archetype stats

**Example:**
```
Increase Berserker strength: adjust_archetype("Berserker", "strength", 1.5)
Decrease Tank health: adjust_archetype("Tank", "health", 0.8)
```

### `adjust_player_base_attribute`

Adjusts player base attributes (strength, agility, technique, intelligence).

**Parameters:**
- `attributeName` (string): "strength", "agility", "technique", or "intelligence"
- `value` (int): New base attribute value

**Returns:**
- Success status
- Current base attributes

**Example:**
```
Increase player base strength: adjust_player_base_attribute("strength", 8)
```

### `adjust_player_base_health`

Adjusts player base health.

**Parameters:**
- `value` (int): New base health value

**Returns:**
- Success status
- Current base health and health per level

**Example:**
```
Increase player base health: adjust_player_base_health(60)
```

### `adjust_player_attributes_per_level`

Adjusts how many attributes the player gains per level.

**Parameters:**
- `value` (int): New attributes per level value

**Returns:**
- Success status
- Current attributes per level

**Example:**
```
Increase player attributes per level: adjust_player_attributes_per_level(2)
```

### `adjust_player_health_per_level`

Adjusts how much health the player gains per level.

**Parameters:**
- `value` (int): New health per level value

**Returns:**
- Success status
- Current health per level

**Example:**
```
Increase player health per level: adjust_player_health_per_level(5)
```

### `adjust_enemy_baseline_stat`

Adjusts enemy baseline stats (affects all enemies).

**Parameters:**
- `statName` (string): "health", "strength", "agility", "technique", "intelligence", or "armor"
- `value` (double): New baseline stat value

**Returns:**
- Success status
- Current baseline stats

**Example:**
```
Reduce enemy baseline strength: adjust_enemy_baseline_stat("strength", 2)
Increase enemy baseline health: adjust_enemy_baseline_stat("health", 25)
```

### `adjust_enemy_scaling_per_level`

Adjusts how much enemy stats increase per level.

**Parameters:**
- `statName` (string): "health", "attributes", or "armor"
- `value` (double): New scaling value

**Returns:**
- Success status
- Current scaling values

**Example:**
```
Reduce enemy health scaling: adjust_enemy_scaling_per_level("health", 2)
Increase enemy attribute scaling: adjust_enemy_scaling_per_level("attributes", 3)
```

### `adjust_weapon_scaling`

Adjusts weapon scaling multipliers.

**Parameters:**
- `weaponType` (string): Weapon type or "global" for global multiplier
- `parameter` (string): "damage" or "damageMultiplier"
- `value` (double): New value

**Returns:**
- Success status
- Current global damage multiplier

**Example:**
```
Increase global weapon damage: adjust_weapon_scaling("global", "damage", 1.15)
```

### `apply_preset`

Applies a quick preset configuration.

**Parameters:**
- `presetName` (string): One of:
  - `"aggressive_enemies"`: +20% health, +20% damage
  - `"tanky_enemies"`: +50% health, +30% armor, -10% damage
  - `"fast_enemies"`: +30% speed, -10% health
  - `"baseline"`: Reset to default (1.0 for all multipliers)

**Returns:**
- Success status
- Current multiplier values

**Example:**
```
Reset to baseline: apply_preset("baseline")
Make enemies more aggressive: apply_preset("aggressive_enemies")
```

### `get_current_configuration`

Gets the current game configuration values.

**Returns:**
- Global enemy multipliers
- Archetype configurations
- Weapon scaling settings

**Example:**
```
Check current configuration before making adjustments
```

### `save_configuration`

Saves the current game configuration to TuningConfig.json.

**Returns:**
- Success status

**Example:**
```
After making adjustments, save configuration to persist changes
```

## Patch Management Tools

### `save_patch`

Saves the current configuration as a balance patch that can be shared or loaded later.

**Parameters:**
- `name` (string): Patch name
- `author` (string): Author name
- `description` (string): Patch description
- `version` (string, default: "1.0"): Patch version
- `tags` (string, optional): Comma-separated tags

**Returns:**
- Success status

**Example:**
```
Save current configuration as a patch:
save_patch("aggressive_balance_v2", "Claude", "Increased enemy health and damage by 20%", "2.0", "aggressive,tested")
```

### `load_patch`

Loads and applies a balance patch by patch ID or name.

**Parameters:**
- `patchId` (string): Patch ID or name

**Returns:**
- Success status

**Example:**
```
Load a previously saved patch:
load_patch("aggressive_balance_v2")
```

### `list_patches`

Lists all available balance patches.

**Returns:**
- Count of patches
- List of patches with metadata (name, author, description, version, tags, test results)

**Example:**
```
List all available patches to see what's available
```

### `get_patch_info`

Gets detailed information about a specific patch.

**Parameters:**
- `patchId` (string): Patch ID or name

**Returns:**
- Patch metadata
- Configuration snapshot (global multipliers)

**Example:**
```
Get details about a patch before loading it
```

## Automated Tuning Tools (Recommended)

For automated tuning with suggestions and quality scoring, see **MCP_AUTOMATED_TUNING_GUIDE.md**.

**Quick Start:**
1. Run simulation: `run_battle_simulation(100)`
2. Set baseline: `set_baseline()`
3. Get suggestions: `suggest_tuning()`
4. Apply suggestions: `apply_tuning_suggestion(suggestionId)`
5. Test and compare: `compare_with_baseline()`

## Typical Workflow

### 1. Baseline Testing (Manual)
```
1. Run simulation: run_battle_simulation(battlesPerCombination: 100)
2. Analyze results: analyze_battle_results()
3. Validate balance: validate_balance()
4. Review issues and recommendations
```

### 2. Automated Tuning (Recommended)
```
1. Run simulation: run_battle_simulation(100)
2. Set baseline: set_baseline()
3. Get quality score: get_balance_quality_score()
4. Get suggestions: suggest_tuning()
5. Apply highest priority: apply_tuning_suggestion(suggestionId)
6. Test: run_battle_simulation(100)
7. Compare: compare_with_baseline()
8. Iterate until quality score >90
```

### 3. Manual Balance Adjustment
```
1. Get current configuration: get_current_configuration()
2. Make adjustments:
   - adjust_global_enemy_multiplier("health", 1.2)
   - adjust_archetype("Berserker", "strength", 1.5)
3. Save configuration: save_configuration()
```

### 4. Test Adjustments
```
1. Run simulation again: run_battle_simulation(battlesPerCombination: 100)
2. Analyze results: analyze_battle_results()
3. Validate balance: validate_balance()
4. Compare with baseline: compare_with_baseline()
```

### 5. Save Successful Configuration
```
1. If balance is good, save as patch:
   save_patch("balanced_v1", "Claude", "Well-balanced configuration", "1.0", "balanced,tested")
2. Continue iterating or load patch later
```

## Target Metrics

The tools validate against these target metrics:

- **Win Rate**: 85-98% (player should win most battles, but not all)
- **Combat Duration**: 8-15 turns (fights should be engaging but not too long)
- **Weapon Balance**: <10% variance between best and worst weapon
- **Enemy Differentiation**: >3% variance (enemies should feel different)

## Tips for Claude Code

1. **Use Automated Tuning**: Start with `suggest_tuning()` for data-driven recommendations
2. **Track Quality Score**: Use `get_balance_quality_score()` to measure progress
3. **Set Baseline**: Always `set_baseline()` before making changes
4. **Compare Results**: Use `compare_with_baseline()` to verify improvements
5. **Prioritize Critical**: Apply Critical priority suggestions first
6. **One at a Time**: Apply suggestions one at a time to understand impact
7. **Test Frequently**: Run simulations after each adjustment
8. **Save Good States**: Use patches to save successful balance states
9. **Document Changes**: Use patch descriptions to document what changed and why

**For detailed automated tuning workflow, see MCP_AUTOMATED_TUNING_GUIDE.md**

## Example Conversation

**You**: "Run a battle simulation with 50 battles per combination"

**Claude**: [Uses `run_battle_simulation`, returns statistics]

**You**: "Analyze the results and tell me what issues you found"

**Claude**: [Uses `analyze_battle_results`, reviews issues and recommendations]

**You**: "The win rate is too high. Increase enemy health by 20%"

**Claude**: [Uses `adjust_global_enemy_multiplier("health", 1.2)`]

**You**: "Test the changes"

**Claude**: [Uses `run_battle_simulation` again, then `analyze_battle_results` and `validate_balance`]

**You**: "Save this configuration as a patch"

**Claude**: [Uses `save_patch` with appropriate metadata]

## Notes

- Simulations can take time depending on `battlesPerCombination` and system performance
- Results are cached after `run_battle_simulation` - you can analyze/validate without re-running
- Configuration changes are in-memory until `save_configuration()` is called
- Patches are saved to `GameData/BalancePatches/` directory
- All tools return JSON for easy parsing and analysis

