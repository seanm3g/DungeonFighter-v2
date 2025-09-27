# DPS Balance Implementation - "Actions to Kill" System

## Overview
Implemented a core balance mechanic where combat duration is measured in "actions to kill" rather than raw damage numbers. The target is approximately 2 DPS at level 1, resulting in roughly 10 actions to kill an enemy.

## Target Specifications
- **Level 1 DPS Target**: ~2.0 DPS for both heroes and enemies
- **Actions to Kill Target**: ~10 actions at level 1
- **Health at Level 1**: 18 base + 2 per level = 20 health
- **Damage at Level 1**: ~17 base damage per action

## Changes Made

### 1. Health Scaling Adjustment
**File: `GameData/TuningConfig.json`**
```json
"Character": {
  "PlayerBaseHealth": 18,        // Reduced from 200
  "HealthPerLevel": 2,           // Reduced from 5
  "EnemyHealthPerLevel": 2       // Reduced from 6
}
```

### 2. Attribute Scaling Adjustment
**File: `GameData/TuningConfig.json`**
```json
"Attributes": {
  "PlayerBaseAttributes": {
    "Strength": 7,               // Reduced from 15
    "Agility": 7,                // Reduced from 15
    "Technique": 7,              // Reduced from 15
    "Intelligence": 7            // Reduced from 15
  }
}
```

### 3. Enemy DPS System Update
**File: `GameData/TuningConfig.json`**
```json
"EnemyDPS": {
  "BaseDPSAtLevel1": 2.0,        // Increased from 0.2
  "DPSPerLevel": 0.5,            // Reduced from 1.0
  "DPSScalingFormula": "BaseDPSAtLevel1 + (Level * DPSPerLevel)"
}
```

### 4. Enemy Damage Scaling Removal
**Files: `Code/Combat.cs`, `Code/EnemyDPSCalculator.cs`**
- Removed level-based damage bonus (+1 per level)
- Enemy damage scaling now handled by DPS system instead

## DPS Calculations

### Level 1 Player DPS
- **Base Damage**: STR(7) + Highest(7) + Weapon(~3) = ~17 damage
- **Attack Time**: 10.0 - (7 × 0.1) = 9.3 seconds
- **DPS**: 17 ÷ 9.3 = ~1.83 DPS

### Level 1 Enemy DPS
- **Base Damage**: STR(~7) + Highest(~7) = ~14 damage
- **Attack Time**: 10.0 - (7 × 0.1) = 9.3 seconds  
- **DPS**: 14 ÷ 9.3 = ~1.51 DPS
- **With Archetype Multipliers**: 1.51 × 0.7-1.2 = ~1.1-1.8 DPS

### Actions to Kill Calculation
- **Level 1 Health**: 18 base + 2 per level = 20 health
- **Player vs Enemy**: 20 health ÷ 1.83 DPS = ~11 actions
- **Enemy vs Player**: 20 health ÷ 1.51 DPS = ~13 actions

## Scaling Philosophy

### Level Progression
- **Health Scaling**: +2 per level (linear)
- **Damage Scaling**: +1 attribute per level (linear)
- **DPS Scaling**: Gradual increase through attribute growth
- **Actions to Kill**: Remains relatively constant (~10-15 actions)

### Archetype Balance
Different enemy archetypes maintain the same target DPS but distribute it differently:
- **Berserker**: Fast attacks (1.4x speed, 0.71x damage)
- **Assassin**: Quick strikes (1.2x speed, 0.83x damage)  
- **Warrior**: Balanced (1.0x speed, 1.0x damage)
- **Brute**: Slow but strong (0.75x speed, 1.1x damage)
- **Juggernaut**: Very slow but powerful (0.6x speed, 1.2x damage)

## Benefits of This System

1. **Predictable Combat Duration**: Players can expect roughly 10-15 actions per fight
2. **Scalable Balance**: System works at all levels with consistent feel
3. **Strategic Depth**: More actions = more opportunities for combos and strategy
4. **Clear Progression**: Higher levels = more health and damage, but similar fight duration
5. **Archetype Variety**: Different enemy types feel distinct while maintaining balance

## Testing Recommendations

1. **Level 1 Combat**: Verify ~10-15 actions to kill
2. **Level 5 Combat**: Check that scaling feels appropriate
3. **Level 10 Combat**: Ensure system remains balanced at higher levels
4. **Archetype Testing**: Test each enemy archetype for proper DPS distribution
5. **Action Variety**: Verify that different actions feel impactful within the system

## Future Adjustments

The system is designed to be easily tunable:
- **Health**: Adjust `PlayerBaseHealth` and `HealthPerLevel`
- **Damage**: Adjust `PlayerBaseAttributes` values
- **DPS Target**: Modify `BaseDPSAtLevel1` in `EnemyDPS` section
- **Scaling**: Adjust `DPSPerLevel` for different progression curves

This creates a solid foundation where "actions to kill" becomes the primary balance metric, making combat feel consistent and strategic across all levels.
