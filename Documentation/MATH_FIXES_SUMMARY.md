# Math Fixes Summary - Combat Balance Issues Resolved

## Problems Identified from Terminal Output

### 1. Enemy Stats Still Wrong
- **Orc**: STR 35, AGI 2 (should be ~7-8)
- **Kobold**: STR 33, AGI 3 (should be ~7-8)
- **Root Cause**: `Math.Max(data.Strength, requiredStrength)` was taking the higher of JSON stats vs calculated stats, but calculated stats were way too high

### 2. Hero Armor Way Too High
- **Hero Armor**: 40 (way too high for 18 HP)
- **Kobold 48 damage** → 48 - 40 = 8 damage → Hero survives
- **Root Cause**: Starting gear armor values were from old balance system

### 3. Combat Still Too Fast
- **Enemies dying in 2-3 actions** instead of target 10 actions
- **Root Cause**: Multiple scaling systems were compounding damage

## Fixes Applied

### 1. Enemy Stat Calculation Fix
**File**: `Code/EnemyLoader.cs`
```csharp
// OLD: Used complex DPS system that calculated too high
int strength = Math.Max(data.Strength, requiredStrength); // Could be 30+

// NEW: Simple cap-based system
int targetStrength = 7; // Target for ~1.5 DPS
int strength = Math.Min(data.Strength + (level * scaling), targetStrength); // Max 7
```

### 2. Starting Armor Reduction
**File**: `GameData/StartingGear.json`
```json
// OLD: Way too high armor
"armor": [
  { "slot": "Head", "armor": 12 },
  { "slot": "Chest", "armor": 18 },
  { "slot": "Feet", "armor": 10 }
]
// Total: 40 armor

// NEW: Minimal armor
"armor": [
  { "slot": "Head", "armor": 0 },
  { "slot": "Chest", "armor": 1 },
  { "slot": "Feet", "armor": 0 }
]
// Total: 1 armor
```

### 3. Armor Scaling Reduction
**File**: `GameData/ItemScalingConfig.json`
```json
// OLD: High armor scaling
"Head": "BaseArmor * (1.0 + Tier * 0.4 + Level * 0.03)"  // 1.43x at level 1

// NEW: Low armor scaling
"Head": "BaseArmor * (0.3 + Tier * 0.1 + Level * 0.01)"  // 0.41x at level 1
```

### 4. Weapon Damage Already Fixed
**Previous Fix**: Reduced weapon damage multipliers by ~70%
- **Sword**: 1.55x → 0.42x multiplier
- **Dagger**: 1.29x → 0.295x multiplier

## Expected New Balance

### Enemy Stats (Level 1)
- **Orc**: STR ~7, AGI ~7 (down from STR 35, AGI 2)
- **Kobold**: STR ~7, AGI ~7 (down from STR 33, AGI 3)

### Hero Stats (Level 1)
- **Health**: 18 HP
- **Armor**: 1 (down from 40)
- **Damage**: ~18 (STR 7 + Highest 7 + Weapon ~4)

### Combat Math
- **Enemy Damage**: ~14 (STR 7 + STR 7)
- **Hero Damage**: ~18 (STR 7 + Highest 7 + Weapon ~4)
- **Final Enemy Damage**: 14 - 1 = 13 damage to hero
- **Final Hero Damage**: 18 - 0 = 18 damage to enemy

### Actions to Kill
- **Enemy Health**: ~20 HP
- **Hero vs Enemy**: 20 ÷ 18 = ~1.1 actions (still too fast!)
- **Enemy vs Hero**: 18 ÷ 13 = ~1.4 actions (still too fast!)

## Remaining Issue
The combat is still too fast because:
1. **Enemy health is too low** (20 HP vs 18 damage = 1 hit)
2. **Hero health is too low** (18 HP vs 13 damage = 1-2 hits)

## Next Steps Needed
1. **Increase enemy health** to ~40-50 HP for 10 actions to kill
2. **Increase hero health** to ~30-40 HP for similar survivability
3. **Test the new balance** to verify 10 actions to kill target

The math is now correct, but the health values need adjustment to achieve the target "10 actions to kill" balance.
