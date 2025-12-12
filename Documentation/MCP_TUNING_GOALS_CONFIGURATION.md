# Tuning Goals Configuration

This document explains where and how to configure balance tuning goals.

## Where Goals Are Specified

### Option 1: Configuration File (Recommended)

Goals are configurable via `GameData/TuningConfig.json` in the `BalanceTuningGoals` section:

```json
{
  "BalanceTuningGoals": {
    "WinRate": {
      "MinTarget": 85.0,
      "MaxTarget": 98.0,
      "OptimalMin": 88.0,
      "OptimalMax": 95.0,
      "CriticalLow": 80.0,
      "WarningLow": 85.0,
      "WarningHigh": 98.0,
      "CriticalHigh": 99.0
    },
    "CombatDuration": {
      "MinTarget": 8.0,
      "MaxTarget": 15.0,
      "OptimalMin": 9.0,
      "OptimalMax": 13.0,
      "CriticalShort": 6.0,
      "WarningShort": 8.0,
      "WarningLong": 15.0,
      "CriticalLong": 18.0
    },
    "WeaponBalance": {
      "MaxVariance": 10.0,
      "OptimalVariance": 5.0,
      "CriticalVariance": 15.0
    },
    "EnemyDifferentiation": {
      "MinVariance": 3.0,
      "OptimalVariance": 5.0,
      "CriticalVariance": 1.0
    },
    "QualityWeights": {
      "WinRateWeight": 0.40,
      "DurationWeight": 0.25,
      "WeaponBalanceWeight": 0.20,
      "EnemyDiffWeight": 0.15
    }
  }
}
```

### Option 2: Code Constants (Fallback)

If not specified in the config file, goals default to hardcoded values in `Code/Game/BalanceTuningGoals.cs`. However, the system now reads from configuration first.

## Goal Categories

### Win Rate Goals

- **MinTarget/MaxTarget**: Acceptable range (85-98%)
- **OptimalMin/OptimalMax**: Ideal range (88-95%)
- **CriticalLow/CriticalHigh**: Critical thresholds (<80% or >99%)
- **WarningLow/WarningHigh**: Warning thresholds (80-85% or 98-99%)

### Combat Duration Goals

- **MinTarget/MaxTarget**: Acceptable range (8-15 turns)
- **OptimalMin/OptimalMax**: Ideal range (9-13 turns)
- **CriticalShort/CriticalLong**: Critical thresholds (<6 or >18 turns)
- **WarningShort/WarningLong**: Warning thresholds (6-8 or 15-18 turns)

### Weapon Balance Goals

- **MaxVariance**: Maximum acceptable difference between best/worst weapon (10%)
- **OptimalVariance**: Ideal variance (5%)
- **CriticalVariance**: Critical imbalance threshold (15%)

### Enemy Differentiation Goals

- **MinVariance**: Minimum acceptable difference between easiest/hardest enemy (3%)
- **OptimalVariance**: Ideal variance (5%)
- **CriticalVariance**: Critical similarity threshold (1%)

### Quality Score Weights

These determine how much each metric contributes to the overall quality score (must sum to 1.0):

- **WinRateWeight**: 40% (most important)
- **DurationWeight**: 25%
- **WeaponBalanceWeight**: 20%
- **EnemyDiffWeight**: 15%

## Adjustable Parameters

The tuning system can now adjust:

### Player Adjustments

1. **Base Attributes**: Strength, Agility, Technique, Intelligence
2. **Base Health**: Starting health for new characters
3. **Attributes Per Level**: How many attributes gained per level
4. **Health Per Level**: How much health gained per level

### Enemy Adjustments

1. **Global Multipliers**: Health, Damage, Armor, Speed (affects all enemies)
2. **Baseline Stats**: Health, Strength, Agility, Technique, Intelligence, Armor (starting stats for all enemies)
3. **Scaling Per Level**: Health, Attributes, Armor (how stats increase per level)
4. **Archetype Multipliers**: Per-archetype stat adjustments

### Weapon Adjustments

1. **Global Weapon Scaling**: Damage multiplier for all weapons
2. **Weapon-Specific Scaling**: Individual weapon adjustments (requires Weapons.json modification)

## MCP Tools for Adjustments

### Player Adjustment Tools

- `adjust_player_base_attribute(attributeName, value)` - Adjust base attributes
- `adjust_player_base_health(value)` - Adjust base health
- `adjust_player_attributes_per_level(value)` - Adjust attributes per level
- `adjust_player_health_per_level(value)` - Adjust health per level

### Enemy Adjustment Tools

- `adjust_global_enemy_multiplier(multiplierName, value)` - Adjust global multipliers
- `adjust_enemy_baseline_stat(statName, value)` - Adjust baseline stats
- `adjust_enemy_scaling_per_level(statName, value)` - Adjust scaling per level
- `adjust_archetype(archetypeName, statName, value)` - Adjust archetype multipliers

### Automated Tuning

- `suggest_tuning()` - Get automated suggestions (includes player/enemy adjustments)
- `apply_tuning_suggestion(suggestionId)` - Apply a suggestion automatically

## Example: Customizing Goals

### Making the Game Easier

```json
{
  "BalanceTuningGoals": {
    "WinRate": {
      "MinTarget": 90.0,
      "MaxTarget": 99.0,
      "OptimalMin": 92.0,
      "OptimalMax": 97.0
    }
  }
}
```

### Making Combat Longer

```json
{
  "BalanceTuningGoals": {
    "CombatDuration": {
      "MinTarget": 10.0,
      "MaxTarget": 20.0,
      "OptimalMin": 12.0,
      "OptimalMax": 18.0
    }
  }
}
```

### Emphasizing Weapon Balance

```json
{
  "BalanceTuningGoals": {
    "QualityWeights": {
      "WinRateWeight": 0.30,
      "DurationWeight": 0.20,
      "WeaponBalanceWeight": 0.35,
      "EnemyDiffWeight": 0.15
    }
  }
}
```

## How Goals Are Used

1. **Quality Score Calculation**: Goals determine the 0-100 quality score
2. **Suggestion Generation**: Goals determine what adjustments are suggested
3. **Priority Assignment**: Goals determine suggestion priorities (Critical/High/Medium/Low)
4. **Adjustment Magnitude**: Goals determine how large adjustments should be

## Reloading Goals

Goals are loaded from `TuningConfig.json` when:
- Game starts
- Configuration is reloaded
- `save_configuration()` is called (saves current goals)

To change goals:
1. Edit `GameData/TuningConfig.json`
2. Add/update `BalanceTuningGoals` section
3. Restart game or reload configuration

## Default Values

If `BalanceTuningGoals` is not in the config file, these defaults are used:

- **Win Rate**: 85-98% (optimal: 88-95%)
- **Combat Duration**: 8-15 turns (optimal: 9-13)
- **Weapon Variance**: <10% (optimal: <5%)
- **Enemy Variance**: >3% (optimal: >5%)
- **Quality Weights**: 40% win rate, 25% duration, 20% weapon, 15% enemy

These defaults are defined in `Code/Config/SystemConfig.cs` in the `BalanceTuningGoalsConfig` class.

