# Automated Tuning Guide - Making Simulations Valuable

This guide explains how to use the automated tuning system to make battle simulations actionable and valuable for balance tuning.

## Overview

The automated tuning system provides:
1. **Clear Goals**: Defined targets for win rate, combat duration, weapon balance, and enemy differentiation
2. **Quality Scoring**: Overall balance quality score (0-100) to track progress
3. **Automated Suggestions**: Specific tuning recommendations with priorities and magnitudes
4. **Iteration Tracking**: Compare before/after results to measure improvement

## Where to Configure Goals

Tuning goals are configured in `GameData/TuningConfig.json` in the `BalanceTuningGoals` section. See **MCP_TUNING_GOALS_CONFIGURATION.md** for detailed configuration options.

If not specified in the config file, default values are used (see defaults below).

## Tuning Goals

### Win Rate Targets
- **Optimal Range**: 88-95% (ideal)
- **Target Range**: 85-98% (acceptable)
- **Warning**: 80-85% or 98-99% (needs attention)
- **Critical**: <80% or >99% (must fix)

### Combat Duration Targets
- **Optimal Range**: 9-13 turns (ideal)
- **Target Range**: 8-15 turns (acceptable)
- **Warning**: 6-8 or 15-18 turns (needs attention)
- **Critical**: <6 or >18 turns (must fix)

### Weapon Balance Targets
- **Max Variance**: <10% difference between best/worst weapon
- **Optimal Variance**: <5% difference
- **Critical**: >15% difference

### Enemy Differentiation Targets
- **Min Variance**: >3% difference between easiest/hardest enemy
- **Optimal Variance**: >5% difference
- **Critical**: <1% difference (enemies too similar)

## Quality Score

The balance quality score (0-100) is calculated from:
- **40%** Win Rate (most important)
- **25%** Combat Duration
- **20%** Weapon Balance
- **15%** Enemy Differentiation

**Interpretation:**
- **90-100**: Excellent balance
- **75-89**: Good balance
- **60-74**: Fair balance (needs improvement)
- **40-59**: Poor balance (significant issues)
- **0-39**: Critical balance (major problems)

## Workflow

### 1. Run Baseline Simulation

```
1. Run simulation: run_battle_simulation(battlesPerCombination: 100)
2. Set as baseline: set_baseline()
3. Check quality score: get_balance_quality_score()
```

### 2. Get Tuning Suggestions

```
1. Get suggestions: suggest_tuning()
2. Review suggestions (sorted by priority):
   - Critical: Must fix issues (<80% or >99% win rate)
   - High: Outside optimal range
   - Medium: In warning range
   - Low: Fine-tuning
```

### 3. Apply Suggestions

```
1. Review suggestion details (reason, impact, affected matchups)
2. Apply highest priority suggestions: apply_tuning_suggestion(suggestionId)
3. Start with Critical priority, then High, then Medium
4. Apply one suggestion at a time for best results
```

### 4. Test Changes

```
1. Run simulation again: run_battle_simulation(battlesPerCombination: 100)
2. Compare with baseline: compare_with_baseline()
3. Check quality score: get_balance_quality_score()
4. Review if quality score improved
```

### 5. Iterate

```
1. If improved: Continue with next suggestions
2. If worsened: Revert changes (use undo or load previous patch)
3. If no change: Try different suggestions or adjust magnitude
4. Continue until quality score >90 or all critical issues resolved
```

## MCP Tools

### `suggest_tuning`
Analyzes simulation results and generates prioritized tuning suggestions.

**Returns:**
- Quality score
- Current metrics (win rate, duration, variances)
- List of suggestions with:
  - Priority (Critical/High/Medium/Low)
  - Category (global/archetype/weapon/enemy)
  - Target and parameter to adjust
  - Current and suggested values
  - Adjustment magnitude (%)
  - Reason and expected impact
  - Affected matchups

**Example:**
```
After running simulation, get suggestions:
suggest_tuning()
```

### `apply_tuning_suggestion`
Applies a specific tuning suggestion by ID.

**Parameters:**
- `suggestionId`: ID from suggest_tuning results

**Example:**
```
Apply a suggestion:
apply_tuning_suggestion("global_health_reduce")
```

### `get_balance_quality_score`
Gets the overall balance quality score and metric status.

**Returns:**
- Quality score (0-100)
- All metrics with targets and status
- Interpretation (Excellent/Good/Fair/Poor/Critical)

**Example:**
```
Check current balance quality:
get_balance_quality_score()
```

### `set_baseline`
Sets current simulation results as baseline for comparison.

**Example:**
```
After initial simulation:
set_baseline()
```

### `compare_with_baseline`
Compares current results with baseline.

**Returns:**
- Baseline metrics
- Current metrics
- Changes (improvements/regressions)
- Overall improvement status

**Example:**
```
After making adjustments and re-running simulation:
compare_with_baseline()
```

## Tuning Strategies

### Strategy 1: Fix Critical Issues First
1. Run simulation
2. Get suggestions
3. Apply all Critical priority suggestions
4. Test and verify improvement
5. Move to High priority

### Strategy 2: One Adjustment at a Time
1. Run simulation
2. Get suggestions
3. Apply highest priority suggestion
4. Test immediately
5. If improved, continue; if not, revert
6. Repeat

### Strategy 3: Global First, Then Specific
1. Fix global multipliers first (affects all enemies)
2. Then fix weapon balance
3. Then fix enemy-specific issues
4. Finally fine-tune

## Example Tuning Session

```
1. Run baseline: run_battle_simulation(100)
   Result: Quality Score 45.2 (Poor)

2. Set baseline: set_baseline()

3. Get suggestions: suggest_tuning()
   Found: 3 Critical, 5 High priority suggestions

4. Apply first critical: apply_tuning_suggestion("global_health_reduce")
   Reduced enemy health by 15%

5. Test: run_battle_simulation(100)
   Result: Quality Score 62.3 (Fair) - Improved!

6. Compare: compare_with_baseline()
   Quality score improved by +17.1 points

7. Continue with next suggestions...

8. Final: Quality Score 87.5 (Good)
   Save as patch: save_patch("balanced_v1", "Claude", "Improved from 45 to 87 quality score")
```

## Tips

1. **Start with Baseline**: Always establish baseline before making changes
2. **Prioritize Critical**: Fix critical issues first (<80% or >99% win rate)
3. **One at a Time**: Apply suggestions one at a time to understand impact
4. **Test Frequently**: Run simulations after each adjustment
5. **Compare Results**: Always compare with baseline to track progress
6. **Save Good States**: Save patches when quality score improves significantly
7. **Revert if Needed**: If changes worsen balance, revert and try different approach
8. **Use Appropriate Sample Size**: 
   - Quick tests: 10-50 battles (rapid iteration)
   - Validation: 100+ battles (final confirmation)

## Common Scenarios

### Scenario 1: Overall Win Rate Too Low (<85%)
**Symptoms**: Quality score low, win rate <85%

**Suggested Actions**:
1. Apply `global_health_reduce` suggestion (reduce enemy health)
2. Apply `global_damage_reduce` suggestion (reduce enemy damage)
3. Test and iterate

### Scenario 2: Overall Win Rate Too High (>98%)
**Symptoms**: Quality score low, win rate >98%

**Suggested Actions**:
1. Apply `global_health_increase` suggestion (increase enemy health)
2. Apply `global_damage_increase` suggestion (increase enemy damage)
3. Test and iterate

### Scenario 3: Weapon Imbalance
**Symptoms**: Weapon variance >10%

**Suggested Actions**:
1. Apply weapon buff suggestions for worst weapon
2. Apply weapon nerf suggestions for best weapon
3. Test and iterate

### Scenario 4: Combat Too Short (<8 turns)
**Symptoms**: Average duration <8 turns

**Suggested Actions**:
1. Apply `duration_increase_health` suggestion
2. Test and iterate

### Scenario 5: Combat Too Long (>15 turns)
**Symptoms**: Average duration >15 turns

**Suggested Actions**:
1. Apply `duration_decrease_health` suggestion
2. Test and iterate

## Success Criteria

A well-balanced configuration should have:
- **Quality Score**: >90 (Excellent)
- **Win Rate**: 88-95% (optimal range)
- **Combat Duration**: 9-13 turns (optimal range)
- **Weapon Variance**: <5% (optimal)
- **Enemy Variance**: >5% (optimal)

## Next Steps

After achieving good balance:
1. Save as patch with test results
2. Test at different levels (level 1, 5, 10)
3. Test with different player builds
4. Document what worked and what didn't
5. Share patches for feedback

