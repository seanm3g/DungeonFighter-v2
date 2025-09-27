# Combat Balance Changes

## Overview
Comprehensive damage reduction to make combat last longer and be more balanced. The changes address the issue where enemies and players were dying too quickly due to excessive damage output.

## Changes Made

### 1. Health Scaling Increases
**File: `GameData/TuningConfig.json`**
- **Player Base Health**: 150 → 200 (+33% increase)
- **Health Per Level**: 3 → 5 (+67% increase)
- **Enemy Health Per Level**: 4 → 6 (+50% increase)

### 2. Attribute Scaling Reductions
**File: `GameData/TuningConfig.json`**
- **Player Base Attributes**: 15 → 8 (all stats reduced by ~47%)
- **Enemy Primary Attribute Bonus**: 3 → 2 (reduced enemy scaling)

### 3. Combat Multiplier Reductions
**File: `GameData/TuningConfig.json`**
- **Critical Hit Multiplier**: 1.5 → 1.3 (reduced crit damage)
- **Enemy Damage Multiplier**: 1.0 → 0.7 (30% reduction in enemy damage)

### 4. Action Damage Multiplier Reductions
**File: `GameData/Actions.json`**

#### High-Damage Actions Reduced:
- **CRIT**: 1.8x → 1.4x (-22% damage)
- **SHIELD BASH**: 1.6x → 1.2x (-25% damage)
- **SHARP EDGE**: 1.6x → 1.2x (-25% damage)
- **BLOOD FRENZY**: 1.6x → 1.2x (-25% damage)
- **DEAL WITH THE DEVIL**: 1.6x → 1.2x (-25% damage)
- **BERZERK**: 1.6x → 1.2x (-25% damage)
- **SWING FOR THE FENCES**: 1.6x → 1.2x (-25% damage)
- **TRUE STRIKE**: 1.6x → 1.2x (-25% damage)
- **LAST GRASP**: 1.6x → 1.2x (-25% damage)
- **SECOND WIND**: 1.6x → 1.2x (-25% damage)
- **QUICK REFLEXES**: 1.6x → 1.2x (-25% damage)
- **PRETTY BOY SWAG**: 1.6x → 1.2x (-25% damage)

#### Environmental Actions Reduced:
- **LAVA SPLASH**: 1.8x → 1.3x (-28% damage)
- **GHOSTLY WHISPER**: 1.8x → 1.3x (-28% damage)
- **SKELETON HAND**: 1.6x → 1.2x (-25% damage)
- **RISING DEAD**: 1.7x → 1.3x (-24% damage)
- **BOSS RAGE**: 1.6x → 1.2x (-25% damage)
- **EARTHQUAKE**: 1.8x → 1.3x (-28% damage)
- **CRYSTAL SHARDS**: 1.8x → 1.3x (-28% damage)
- **DIVINE JUDGMENT**: 1.6x → 1.2x (-25% damage)

### 5. Enemy Damage Scaling Reduction
**File: `Code/Combat.cs`**
- **Enemy Level Damage Bonus**: +2 per level → +1 per level (-50% scaling)

## Expected Impact

### Before Changes:
- **Base Damage Formula**: STR (15) + Highest Attribute (15) + Weapon Damage = ~30+ base damage
- **With 1.6x Action**: ~48+ damage per hit
- **Player Health**: 150 base + 3 per level
- **Result**: 3-4 hits to kill most enemies/players

### After Changes:
- **Base Damage Formula**: STR (8) + Highest Attribute (8) + Weapon Damage = ~16+ base damage
- **With 1.2x Action**: ~19+ damage per hit
- **Player Health**: 200 base + 5 per level
- **Result**: 10+ hits to kill most enemies/players

## Combat Duration Improvement
- **Previous**: Combat typically lasted 2-4 rounds
- **Expected**: Combat should now last 6-10+ rounds
- **Enemy Scaling**: Reduced enemy damage output by ~30-50%
- **Player Scaling**: Reduced player damage output by ~40-50%

## Balance Philosophy
The changes maintain the relative power differences between actions while reducing the overall damage output across the board. This should create:
- Longer, more strategic combat encounters
- More opportunity to use different actions and combos
- Better pacing for the narrative system
- More meaningful equipment and stat progression

## Testing Recommendations
1. Test combat encounters at different levels (1, 5, 10)
2. Verify that high-damage actions still feel impactful relative to basic attacks
3. Ensure environmental actions don't feel too weak
4. Check that boss encounters maintain appropriate challenge
5. Validate that the combo system still provides meaningful damage amplification
