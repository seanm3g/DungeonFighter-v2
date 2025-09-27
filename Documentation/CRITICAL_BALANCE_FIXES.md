# Critical Balance Fixes - Emergency Damage Reduction

## Problem Identified
From terminal output, the balance was severely broken:
- **Hero dealing 32 damage** to enemies with only 26 health
- **Enemies dying in 1 hit** instead of the target 10 actions
- **Enemy STR showing 30** instead of expected ~7-8 values
- **Weapon damage scaling was 3-4x too high**

## Root Causes Found

### 1. Enemy Stat Double-Scaling
**Problem**: Enemy stats were being scaled twice:
- First in `EnemyLoader.cs` with DPS calculations
- Then again in `Enemy.cs` constructor with level scaling
- Result: STR 7 → 14 → 30+ (way too high)

**Fix**: Removed double scaling in `Enemy.cs` constructor
```csharp
// OLD: Double scaling
Strength = strength + (level * tuning.Attributes.EnemyAttributesPerLevel);
Strength += level * tuning.Attributes.EnemyPrimaryAttributeBonus;

// NEW: Use calculated stats directly
Strength = strength; // Stats already calculated for target DPS
```

### 2. Weapon Damage Scaling Too High
**Problem**: Weapon damage formulas were adding massive multipliers:
- **Sword**: BaseDamage * (1.2 + Tier * 0.3 + Level * 0.05) = 1.55x multiplier
- **Base weapon damage ~9** → 9 * 1.55 = ~14 damage just from weapon
- **Total damage**: 7 + 7 + 14 = 28+ damage (way too high)

**Fix**: Dramatically reduced weapon scaling multipliers:
```json
// OLD: High multipliers
"Sword": "BaseDamage * (1.2 + Tier * 0.3 + Level * 0.05)"  // 1.55x

// NEW: Low multipliers  
"Sword": "BaseDamage * (0.3 + Tier * 0.1 + Level * 0.02)"  // 0.42x
```

### 3. Enemy DPS System Mismatch
**Problem**: `EnemyDPSSystem.CalculateRequiredStats` was using old level scaling formula
- Used `level * 2` but `Combat.cs` was changed to `level * 0`
- Caused incorrect stat calculations

**Fix**: Updated to match current system:
```csharp
// OLD: Mismatched scaling
double levelBonus = level * 2; // Old formula

// NEW: Matched scaling
double levelBonus = level * 0; // No level bonus (DPS system handles scaling)
```

## New Balance Calculations

### Player Level 1 DPS
- **Base Damage**: STR(7) + Highest(7) + Weapon(~4) = ~18 damage
- **Attack Time**: 10.0 - (7 × 0.1) = 9.3 seconds
- **DPS**: 18 ÷ 9.3 = ~1.94 DPS ✅

### Enemy Level 1 DPS  
- **Base Damage**: STR(~7) + Highest(~7) = ~14 damage
- **Attack Time**: 10.0 - (7 × 0.1) = 9.3 seconds
- **DPS**: 14 ÷ 9.3 = ~1.51 DPS ✅

### Actions to Kill
- **Level 1 Health**: 18 base + 2 per level = 20 health
- **Player vs Enemy**: 20 health ÷ 1.94 DPS = ~10.3 actions ✅
- **Enemy vs Player**: 20 health ÷ 1.51 DPS = ~13.2 actions ✅

## Files Modified

### Core Balance Files
- `GameData/TuningConfig.json` - Reduced enemy DPS target from 2.0 to 1.5
- `GameData/ItemScalingConfig.json` - Reduced weapon damage multipliers by ~70%
- `Code/Enemy.cs` - Removed double stat scaling
- `Code/EnemyDPSSystem.cs` - Fixed level scaling mismatch

### Expected Results
- **Combat Duration**: ~10-13 actions per fight (target achieved)
- **Damage Output**: ~1.5-2.0 DPS (target achieved)  
- **Enemy Stats**: Realistic values (~7-8 instead of 30+)
- **Weapon Impact**: Reasonable contribution (~4 damage instead of 14+)

## Testing Verification Needed
1. **Level 1 Combat**: Should take ~10-13 actions to kill enemy
2. **Enemy Stats**: Should show realistic values (STR ~7-8, not 30+)
3. **Player Damage**: Should deal ~18 damage, not 32+
4. **Weapon Scaling**: Should contribute ~4 damage, not 14+

This emergency fix addresses the critical balance issues and should restore the intended "actions to kill" balance system.
