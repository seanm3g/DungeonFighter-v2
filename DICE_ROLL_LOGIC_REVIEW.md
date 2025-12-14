# Dice Roll and Action Result Logic Review

## Summary
This document reviews all dice roll calculations and action result determinations in the game to identify inconsistencies and bugs.

## Issues Found

### 1. **Inconsistent Total Roll Calculation in ActionExecutionFlow.cs**

**Location**: `Code/Actions/Execution/ActionExecutionFlow.cs:93`

**Issue**: 
```csharp
int totalRoll = result.ModifiedBaseRoll + result.RollBonus;
```

**Problem**: `result.AttackRoll` is already calculated on line 59 as `result.ModifiedBaseRoll + result.RollBonus`. The variable `totalRoll` is redundant and could lead to inconsistencies if one is updated but not the other.

**Fix**: Use `result.AttackRoll` instead of recalculating.

---

### 2. **Critical Hit Check Inconsistency**

**Location**: `Code/Actions/Execution/ActionExecutionFlow.cs:125`

**Issue**:
```csharp
bool isCriticalHit = totalRoll >= 20;
```

**Problem**: 
- Line 74 already calculates `result.IsCritical` using the threshold manager: `result.IsCritical = result.AttackRoll >= RollModificationManager.GetThresholdManager().GetCriticalHitThreshold(source);`
- Line 125 uses a hardcoded value of 20, which may not match the configured threshold
- This creates inconsistency between the two checks

**Fix**: Use `result.IsCritical` instead of recalculating with hardcoded value.

---

### 3. **Heal Action Roll Calculation Inconsistency**

**Location**: `Code/Actions/Execution/ActionExecutionFlow.cs:143`

**Issue**:
```csharp
int totalRoll = result.BaseRoll + result.RollBonus;
```

**Problem**: 
- This uses `result.BaseRoll` (unmodified) instead of `result.ModifiedBaseRoll`
- It doesn't match the pattern used for damage actions (line 93 uses `result.ModifiedBaseRoll + result.RollBonus`)
- Should use `result.AttackRoll` for consistency

**Fix**: Use `result.AttackRoll` instead.

---

### 4. **Non-Damage Action Roll Calculation Inconsistency**

**Location**: `Code/Actions/Execution/ActionExecutionFlow.cs:149`

**Issue**:
```csharp
ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction, 0, result.ModifiedBaseRoll + result.RollBonus, result.RollBonus, true, result.IsCombo, 0, 0, false, result.BaseRoll, battleNarrative);
```

**Problem**: 
- Recalculates `result.ModifiedBaseRoll + result.RollBonus` instead of using `result.AttackRoll`
- Inconsistent with other places

**Fix**: Use `result.AttackRoll` instead.

---

### 5. **Miss Event Roll Calculation Inconsistency**

**Location**: `Code/Actions/Execution/ActionExecutionFlow.cs:292`

**Issue**:
```csharp
ActionUtilities.CreateAndAddBattleEvent(source, target, result.SelectedAction, 0, result.ModifiedBaseRoll + result.RollBonus, result.RollBonus, false, false, 0, 0, false, result.BaseRoll, battleNarrative);
```

**Problem**: 
- Same issue as #4 - recalculates instead of using `result.AttackRoll`

**Fix**: Use `result.AttackRoll` instead.

---

### 6. **MultiHitProcessor Critical Hit Check Inconsistency**

**Location**: `Code/Actions/Execution/MultiHitProcessor.cs:60`

**Issue**:
```csharp
bool isCriticalHit = totalRoll >= 20;
```

**Problem**: 
- Uses hardcoded value of 20 instead of using the threshold manager
- Inconsistent with `ActionExecutionFlow` which uses `result.IsCritical`
- The `totalRoll` parameter passed to `MultiHitProcessor` is correct (`result.ModifiedBaseRoll + result.RollBonus`), but the critical check should use the threshold manager

**Fix**: Pass `isCritical` as a parameter or use threshold manager to check.

---

### 7. **AttackActionExecutor (Legacy/Unused Code?)**

**Location**: `Code/Actions/Execution/AttackActionExecutor.cs`

**Issue**: The `AttackActionExecutor.ExecuteAttackActionColored` method receives `baseRoll` and `rollBonus` as separate parameters and calculates `totalRoll = baseRoll + rollBonus`.

**Problem**: 
- This doesn't account for `ModifiedBaseRoll` from roll modifications
- However, this method doesn't appear to be called from `ActionExecutionFlow` (which uses `MultiHitProcessor` instead)
- May be legacy code or used elsewhere

**Status**: Needs verification if this method is actually used. If not used, should be removed. If used, needs to be fixed.

---

## Roll Calculation Flow

### Current Flow:
1. **Action Selection** (`ActionSelector.cs`):
   - Rolls `baseRoll = Dice.Roll(1, 20)`
   - Calculates `rollBonus = ActionUtilities.CalculateRollBonus(source, null)`
   - Calculates `totalRoll = baseRoll + rollBonus` for action selection
   - Stores `baseRoll` for later use

2. **Action Execution** (`ActionExecutionFlow.cs`):
   - Gets stored `baseRoll` from `ActionSelector.GetActionRoll(source)`
   - Applies roll modifications: `ModifiedBaseRoll = RollModificationManager.ApplyActionRollModifications(...)`
   - Calculates roll bonus: `RollBonus = ActionUtilities.CalculateRollBonus(source, result.SelectedAction)`
   - Calculates final roll: `AttackRoll = ModifiedBaseRoll + RollBonus`

3. **Hit/Miss Check**:
   - Uses `AttackRoll` to determine hit/miss (roll >= 6)

4. **Critical Check**:
   - Uses `AttackRoll` with threshold manager to determine critical (roll >= threshold, default 20)

5. **Damage Calculation**:
   - Some places use `AttackRoll`, some recalculate `ModifiedBaseRoll + RollBonus`
   - Some places use `BaseRoll + RollBonus` (incorrect - doesn't include modifications)

---

## Recommended Fixes

### Priority 1 (Critical - Affects Gameplay):
1. Fix `AttackActionExecutor` to use modified roll values
2. Fix critical hit check inconsistency
3. Fix heal action roll calculation

### Priority 2 (Important - Code Quality):
4. Remove redundant `totalRoll` calculations
5. Standardize all roll usage to `result.AttackRoll`

---

## Roll Value Usage Summary

| Location | Current Usage | Should Use | Status |
|----------|--------------|------------|--------|
| ActionExecutionFlow:93 | `ModifiedBaseRoll + RollBonus` | `AttackRoll` | ❌ Redundant |
| ActionExecutionFlow:125 | `totalRoll >= 20` | `result.IsCritical` | ❌ Inconsistent |
| ActionExecutionFlow:143 | `BaseRoll + RollBonus` | `AttackRoll` | ❌ Wrong calculation |
| ActionExecutionFlow:149 | `ModifiedBaseRoll + RollBonus` | `AttackRoll` | ❌ Redundant |
| ActionExecutionFlow:292 | `ModifiedBaseRoll + RollBonus` | `AttackRoll` | ❌ Redundant |
| AttackActionExecutor | `baseRoll + rollBonus` | `AttackRoll` (passed in) | ❌ Missing modifications |

---

## Notes

- The roll modification system (`RollModificationManager`) can modify the base roll (rerolls, exploding dice, etc.)
- These modifications are stored in `ModifiedBaseRoll`
- The final `AttackRoll = ModifiedBaseRoll + RollBonus` should be used consistently everywhere
- Using `BaseRoll + RollBonus` bypasses roll modifications, which is incorrect

