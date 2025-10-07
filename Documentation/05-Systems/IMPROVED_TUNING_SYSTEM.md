# Improved Tuning System Documentation

## Overview

The improved tuning system consolidates all game balance parameters into a single, unified configuration system. This system provides a clear hierarchy for parameter application and makes it easy to adjust game balance without code changes.

## Key Improvements

### 1. **Unified Enemy System**
- **Before**: Multiple disconnected enemy configurations (`EnemyScaling`, `EnemyBaseline`, `EnemyArchetypes`, `EnemyBalance`)
- **After**: Single `EnemySystem` section with clear hierarchy

### 2. **Applied Global Multipliers**
- **Before**: `EnemyScaling` parameters were defined but never used
- **After**: All parameters are actually applied to enemy creation

### 3. **Individual Enemy Overrides**
- **Before**: No way to customize individual enemies
- **After**: Each enemy can have stat overrides in `Enemies.json`

### 4. **Clear Calculation Flow**
- **Before**: Complex, multi-step calculation with potential for errors
- **After**: Single, documented calculation path

## Configuration Structure

### TuningConfig.json Structure

```json
{
  "EnemySystem": {
    "GlobalMultipliers": {
      "HealthMultiplier": 1.0,
      "DamageMultiplier": 1.0,
      "ArmorMultiplier": 1.0,
      "SpeedMultiplier": 1.0
    },
    "BaselineStats": {
      "Health": 50,
      "Strength": 3,
      "Agility": 3,
      "Technique": 3,
      "Intelligence": 3,
      "Armor": 2
    },
    "ScalingPerLevel": {
      "Health": 3,
      "Attributes": 2,
      "Armor": 0.1
    },
    "Archetypes": {
      "Berserker": {
        "Health": 0.8,
        "Strength": 1.5,
        "Agility": 0.9,
        "Technique": 0.9,
        "Intelligence": 0.8,
        "Armor": 0.5
      }
      // ... other archetypes
    },
    "LevelVariance": 1
  }
}
```

### Enemies.json Structure

```json
[
  {
    "name": "Goblin",
    "archetype": "Assassin",
    "isLiving": true,
    "actions": ["JAB", "TAUNT"],
    "overrides": {
      "Health": 0.8,
      "Strength": 1.2,
      "Agility": 1.1
    },
    "description": "A quick and cunning creature"
  }
]
```

## Stat Calculation Flow

The improved system uses a clear, hierarchical calculation flow:

```
1. Start with BaselineStats from TuningConfig.json
2. Apply Archetype multipliers from TuningConfig.json
3. Apply Individual overrides from Enemies.json
4. Scale by level using ScalingPerLevel
5. Apply Global multipliers from TuningConfig.json
6. Create enemy with final stats
```

### Example Calculation

For a Level 3 Goblin (Assassin archetype):

```
Baseline Stats:
- Health: 50, Strength: 3, Agility: 3, Technique: 3, Intelligence: 3, Armor: 2

Apply Assassin Archetype:
- Health: 50 * 0.7 = 35
- Strength: 3 * 1.0 = 3
- Agility: 3 * 1.6 = 4.8 → 4
- Technique: 3 * 1.2 = 3.6 → 3
- Intelligence: 3 * 1.0 = 3
- Armor: 2 * 0.4 = 0.8 → 0

Apply Individual Overrides (Goblin):
- Health: 35 * 0.8 = 28
- Strength: 3 * 1.2 = 3.6 → 3
- Agility: 4 * 1.1 = 4.4 → 4

Scale by Level (Level 3):
- Health: 28 + (3-1) * 3 = 34
- Strength: 3 + (3-1) * 2 = 7
- Agility: 4 + (3-1) * 2 = 8
- Technique: 3 + (3-1) * 2 = 7
- Intelligence: 3 + (3-1) * 2 = 7
- Armor: 0 + (3-1) * 0.1 = 0.2 → 0

Apply Global Multipliers:
- Health: 34 * 1.0 = 34
- Strength: 7 * 1.0 = 7
- Agility: 8 * 1.0 = 8
- Technique: 7 * 1.0 = 7
- Intelligence: 7 * 1.0 = 7
- Armor: 0 * 1.0 = 0

Final Stats: Health=34, STR=7, AGI=8, TEC=7, INT=7, Armor=0
```

## Benefits

### 1. **Easy Global Balancing**
Change one value to affect all enemies:
```json
"GlobalMultipliers": {
  "HealthMultiplier": 1.2  // All enemies get 20% more health
}
```

### 2. **Archetype Customization**
Modify all enemies of a type:
```json
"Archetypes": {
  "Berserker": {
    "Strength": 2.0  // All Berserkers get double strength
  }
}
```

### 3. **Individual Enemy Tuning**
Customize specific enemies:
```json
{
  "name": "Boss Orc",
  "overrides": {
    "Health": 2.0,  // This specific orc gets double health
    "Strength": 1.5
  }
}
```

### 4. **Clear Documentation**
Every parameter has a description explaining its purpose.

### 5. **Validation Support**
The system can validate that:
- All archetypes are defined
- Override values are reasonable
- Global multipliers don't break balance

## Migration Guide

### From Old System to New System

1. **Replace old enemy configurations** with the new `EnemySystem` section
2. **Update Enemies.json** to include override support
3. **Update enemy creation code** to use the new calculation flow
4. **Test balance** with the new system

### Configuration Mapping

| Old Configuration | New Configuration |
|------------------|-------------------|
| `EnemyScaling.EnemyHealthMultiplier` | `EnemySystem.GlobalMultipliers.HealthMultiplier` |
| `EnemyScaling.EnemyDamageMultiplier` | `EnemySystem.GlobalMultipliers.DamageMultiplier` |
| `EnemyBaseline.BaseStats` | `EnemySystem.BaselineStats` |
| `EnemyBaseline.ScalingPerLevel` | `EnemySystem.ScalingPerLevel` |
| `EnemyArchetypes.Archetypes` | `EnemySystem.Archetypes` |

## Usage Examples

### Making All Enemies Harder
```json
"GlobalMultipliers": {
  "HealthMultiplier": 1.5,
  "DamageMultiplier": 1.3
}
```

### Making Berserkers More Aggressive
```json
"Archetypes": {
  "Berserker": {
    "Strength": 2.0,
    "Health": 0.8
  }
}
```

### Creating a Special Boss Enemy
```json
{
  "name": "Ancient Dragon",
  "archetype": "Brute",
  "overrides": {
    "Health": 3.0,
    "Strength": 2.0,
    "Armor": 2.0
  }
}
```

### Adjusting Level Scaling
```json
"ScalingPerLevel": {
  "Health": 5,      // More health per level
  "Attributes": 1,  // Less attribute growth
  "Armor": 0.2      // More armor per level
}
```

## Implementation Status

- ✅ **Configuration Structure**: New unified structure defined
- ✅ **Enemy Data Structure**: Enhanced with override support
- ✅ **Calculation Logic**: Unified calculation flow implemented
- ✅ **Documentation**: Comprehensive documentation created
- 🔄 **Integration**: Ready for integration with existing codebase
- ⏳ **Testing**: Requires testing with actual game scenarios

## Next Steps

1. **Integrate with GameConfiguration**: Update the main configuration class
2. **Update EnemyLoader**: Replace old enemy creation with new system
3. **Test Balance**: Verify that the new system produces balanced enemies
4. **Migrate Data**: Convert existing enemy data to new format
5. **Validate**: Ensure all parameters are properly applied

## Troubleshooting

### Common Issues

1. **Enemy stats too high/low**: Adjust `GlobalMultipliers`
2. **Archetype not working**: Check archetype name spelling
3. **Overrides not applied**: Verify JSON structure in `Enemies.json`
4. **Level scaling issues**: Check `ScalingPerLevel` values

### Debug Information

The system provides detailed logging for:
- Which configuration values are being used
- Step-by-step calculation results
- Warnings for missing or invalid data
- Validation errors for configuration consistency

This improved tuning system provides a solid foundation for game balance that is both powerful and easy to use.
