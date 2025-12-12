# Balance Testing Philosophy

## Overview

This document outlines the systematic approach to testing and tuning game balance for DungeonFighter. The goal is to create a data-driven methodology that enables rapid iteration while maintaining clear targets and evaluation criteria.

## Core Principles

### 1. Data-Driven Decisions

- **Never rely on gut feelings alone**: Always test changes with actual combat simulations
- **Measure everything**: Track win rates, combat duration, damage output, and other key metrics
- **Compare before and after**: Always run baseline tests before making changes
- **Statistical significance**: Use sufficient sample sizes (100+ battles per matchup for final validation)

### 2. Incremental Changes

- **Small adjustments**: Make changes in 5-15% increments, not 50%+ swings
- **One variable at a time**: Change one parameter, test, then move to the next
- **Track changes**: Document what was changed and why
- **Iterate quickly**: Use quick tests (10-50 battles) for rapid feedback, full tests (100+ battles) for validation

### 3. Context-Aware Testing

- **Test in full combat system**: Don't test isolated mechanics; use the complete combat flow
- **Multiple matchups**: Test all weapon types against all enemy types
- **Level variations**: Test at different player/enemy levels to ensure scaling works
- **Realistic scenarios**: Use actual game conditions, not theoretical calculations

### 4. Player Experience Focus

- **Win rate targets**: Aim for 85-98% player win rate (challenging but fair)
- **Combat duration**: Target 8-15 turns per fight (engaging but not tedious)
- **Tactical variety**: Ensure different strategies are viable
- **Weapon balance**: No weapon should be clearly superior in all situations

## Testing Methodology

### Step 1: Establish Baseline

Before making any changes:

1. Run comprehensive matchup analysis (all weapons vs all enemies)
2. Record current win rates, combat duration, and damage metrics
3. Identify problem areas (too easy/hard matchups)
4. Save baseline configuration as a profile

### Step 2: Identify Issues

Analyze the baseline results to find:

- **Outliers**: Matchups with win rates outside 85-98% range
- **Weapon imbalance**: Weapons that consistently over/underperform
- **Enemy similarity**: Enemies that feel too similar
- **Scaling problems**: Issues that appear at different levels

### Step 3: Make Targeted Adjustments

Based on identified issues:

1. Select the highest priority problem
2. Make a small adjustment (5-15% change)
3. Document the change and reasoning
4. Use quick test mode (10-50 battles) for rapid feedback

### Step 4: Evaluate Changes

After each adjustment:

1. Compare new results to baseline
2. Check if the change moved in the right direction
3. Verify no new problems were introduced
4. If promising, run full test (100+ battles) for validation

### Step 5: Iterate

Continue the cycle:

1. If change helped: Fine-tune with smaller adjustments
2. If change didn't help: Try different approach or revert
3. If change made things worse: Revert and try smaller change
4. Move to next priority issue when current one is resolved

### Step 6: Save and Share

Once balance is improved:

1. Save successful configuration as a patch
2. Add metadata (description, test results)
3. Export patch for sharing
4. Document what was changed and why

## Matchup Evaluation Criteria

### Win Rate Targets

- **Optimal Range**: 85-98% player win rate
- **Too Easy**: >98% win rate (enemy needs buffs)
- **Too Hard**: <85% win rate (enemy needs nerfs)
- **Acceptable Variance**: ±3% from target is fine

### Combat Duration

- **Target Range**: 8-15 turns per fight
- **Too Short**: <8 turns (combat feels trivial)
- **Too Long**: >15 turns (combat feels tedious)
- **Variety**: Some matchups can be shorter/longer, but average should be in range

### Damage Output Balance

- **Player DPS**: Should be slightly higher than enemy DPS (for 85-98% win rate)
- **Damage per Action**: Should feel impactful but not overwhelming
- **Scaling**: Damage should scale appropriately with level

### Tactical Variety

- **Different strategies viable**: Different weapons should have different optimal matchups
- **Enemy differentiation**: Each enemy should feel distinct
- **Counterplay**: Some weapons should counter certain enemies

## Scaling Philosophy

### Health Scaling

- **Base Health**: Start with reasonable base (e.g., 30-60 at level 1)
- **Per Level**: Add 3-5 health per level
- **Enemy vs Player**: Enemies should have similar health to players at same level

### Damage Scaling

- **Base Damage**: Start with meaningful base damage
- **Per Level**: Add 1-2 damage per level
- **Multipliers**: Use multipliers for archetypes (e.g., Berserker +50% damage)

### Armor Scaling

- **Base Armor**: Start with low base (e.g., 1-3)
- **Per Level**: Add 0.1-0.2 armor per level
- **Trade-offs**: High armor enemies should have lower health/damage

### Attribute Scaling

- **Base Attributes**: Start with balanced base (e.g., 3 in each)
- **Per Level**: Add 1-2 points per level
- **Primary Attributes**: Enemies should have one primary attribute (Strength, Agility, etc.)

## Enemy Differentiation Strategy

### Archetype System

Use archetype multipliers to create distinct enemy types:

- **Berserker**: High damage, low health/armor (aggressive glass cannon)
- **Guardian**: High armor, moderate health, low damage (defensive tank)
- **Assassin**: High agility, low health/armor, moderate damage (fast striker)
- **Brute**: High health, moderate damage, low agility (slow tank)
- **Mage**: High intelligence, low health/armor, moderate damage (magical caster)

### Trade-offs

Each enemy should have clear strengths and weaknesses:

- High damage → Low health or armor
- High health → Low damage or agility
- High armor → Low damage or health
- High agility → Low health or armor

### Individual Enemy Tuning

After archetype is set, fine-tune individual enemies:

- Use overrides in `Enemies.json` for specific adjustments
- Test that each enemy feels different
- Ensure no two enemies are identical

## Testing Workflow

### Quick Test (Rapid Iteration)

For rapid feedback during tuning:

1. Run 10-50 battles per matchup
2. Focus on win rate trends (not exact percentages)
3. Use to verify direction of changes
4. Takes 1-2 minutes for full test suite

### Full Test (Validation)

For final validation:

1. Run 100+ battles per matchup
2. Get statistically significant results
3. Verify all matchups are in target range
4. Takes 5-10 minutes for full test suite

### Comparative Analysis

Always compare results:

1. Before vs After: Did the change help?
2. Current vs Baseline: How far have we come?
3. Weapon vs Weapon: Are weapons balanced?
4. Enemy vs Enemy: Are enemies differentiated?

## Balance Targets Summary

| Metric | Target Range | Too Low | Too High |
|--------|-------------|---------|----------|
| Player Win Rate | 85-98% | <85% (too hard) | >98% (too easy) |
| Combat Duration | 8-15 turns | <8 (too fast) | >15 (too slow) |
| Player DPS | 1.5-2.5 | <1.5 (too weak) | >2.5 (too strong) |
| Enemy DPS | 1.0-2.0 | <1.0 (too weak) | >2.0 (too strong) |
| Actions to Kill | 8-12 | <8 (too fast) | >12 (too slow) |

## Common Tuning Scenarios

### Scenario 1: Enemy Too Easy (>98% win rate)

**Symptoms**: Player wins almost every time

**Solutions**:
- Increase enemy health by 10-20%
- Increase enemy damage by 10-20%
- Increase enemy armor by 10-20%
- Use global multiplier to affect all enemies

### Scenario 2: Enemy Too Hard (<85% win rate)

**Symptoms**: Player loses frequently

**Solutions**:
- Decrease enemy health by 10-20%
- Decrease enemy damage by 10-20%
- Decrease enemy armor by 10-20%
- Adjust specific enemy's stats via overrides

### Scenario 3: Weapon Underperforming

**Symptoms**: One weapon has consistently lower win rates

**Solutions**:
- Increase weapon base damage
- Adjust weapon scaling formula
- Check if weapon's actions are effective
- Verify weapon's stat scaling (Strength, Agility, etc.)

### Scenario 4: Enemies Feel Too Similar

**Symptoms**: All enemies play the same way

**Solutions**:
- Adjust archetype multipliers to create more variance
- Give enemies different action sets
- Tune individual enemy overrides
- Ensure clear trade-offs (high X = low Y)

### Scenario 5: Combat Too Fast/Slow

**Symptoms**: Fights end in <8 turns or >15 turns

**Solutions**:
- Adjust health scaling (increase for longer, decrease for shorter)
- Adjust damage scaling (decrease for longer, increase for shorter)
- Balance both health and damage together

## Best Practices

1. **Always test before and after**: Never make changes without baseline comparison
2. **Document everything**: Record what changed, why, and the results
3. **Save successful configs**: Use patches to save good balance states
4. **Share and compare**: Share patches with others to get different perspectives
5. **Be patient**: Balance is iterative; don't expect perfect results immediately
6. **Focus on fun**: Balance targets are guidelines; player experience matters most
7. **Test at multiple levels**: Ensure scaling works across level ranges
8. **Consider edge cases**: Test extreme scenarios (very high/low levels)

## Tools and Resources

### Available Tools

- **Balance Tuning Console**: Real-time variable adjustment
- **Matchup Analyzer**: Comprehensive weapon vs enemy analysis
- **Balance Validator**: Automated validation checks
- **Patch System**: Save and share balance configurations

### Configuration Files

- **TuningConfig.json**: Main balance configuration
- **Enemies.json**: Individual enemy definitions
- **Weapons.json**: Weapon definitions
- **TuningProfiles/**: Local saved profiles
- **BalancePatches/**: Shared balance patches

### Documentation

- **This document**: Testing philosophy and methodology
- **BALANCE_TUNING_STRATEGY.md**: Binary search tuning approach
- **TUNING_SYSTEM_IMPLEMENTATION_SUMMARY.md**: Technical implementation details

## Conclusion

Balance testing is an iterative, data-driven process. By following this philosophy:

1. Establish clear targets
2. Test systematically
3. Make incremental changes
4. Compare results
5. Iterate until targets are met

With the right tools and methodology, you can achieve balanced, engaging combat that feels fair and fun.

