# Iterative Balance Tuning Strategy (5-Cycle Process)

## Overview

This guide provides a structured workflow for running 5 iterations of simulation → analysis → adjustment → testing to progressively improve game balance and fun factor.

---

## Overall Flow Diagram

```
Cycle 1        Cycle 2        Cycle 3        Cycle 4        Cycle 5
  ↓              ↓              ↓              ↓              ↓
Baseline → Analyze → Adjust → Baseline → Analyze → Adjust → ... → Final Results
            & Test            & Test            & Test
```

---

## Iteration Template (Repeat 5 Times)

Each iteration follows this 5-phase structure:

### **Phase 1: Establish Baseline**
Set a checkpoint for comparison.

```
Tools:
  1. run_battle_simulation(battlesPerCombination=100)
     - Comprehensive test: all weapons vs all enemy types
     - Takes several seconds depending on simulation size

  2. set_baseline()
     - Mark current results as baseline for this iteration
     - Used later with compare_with_baseline()
```

**Expected Output:**
- Complete battle statistics
- Win/loss rates per weapon
- Average damage, health, duration data
- Fun moment summaries per weapon

---

### **Phase 2: Analyze Results**
Extract insights about balance issues and engagement.

```
Tools (Run in sequence):
  1. analyze_battle_results()
     - Detailed analysis with identified issues
     - Shows recommendations for fixes

  2. validate_balance()
     - Check against balance validation rules
     - Returns errors and warnings

  3. analyze_fun_moments()
     - Shows which weapons create engaging gameplay
     - Per-weapon fun scores

  4. get_fun_moment_summary()
     - Breakdown of fun moment types
     - Top 10 most intense moments
     - Weapon comparison chart

  5. get_balance_quality_score()
     - Single 0-100 score for overall balance
     - Track this across iterations

  6. suggest_tuning()
     - AI-generated suggestions with priority levels
     - Specific adjustment recommendations
     - Expected impact estimates
```

**What to Look For:**
- Which weapons are overpowered/underpowered?
- Which enemy types are too hard/too easy?
- Is fun score 60+? (Target engagement level)
- Are there balance validation errors?
- Which suggestions have "High" priority?

---

### **Phase 3: Make Targeted Changes**
Apply 1-3 adjustments based on analysis.

```
Choose from these adjustment tools:

Global Changes:
  - adjust_global_enemy_multiplier(multiplierName, value)
    Examples: 'health', 'damage', 'armor', 'speed'
    E.g.: adjust_global_enemy_multiplier('health', 1.2)  # 20% increase

  - apply_preset(presetName)
    Quick presets: 'aggressive_enemies', 'tanky_enemies', 'fast_enemies', 'baseline'
    Use if suggestions align with a preset

Archetype Adjustments:
  - adjust_archetype(archetypeName, statName, value)
    Archetypes: 'Berserker', 'Tank', 'Assassin', etc.
    Stats: 'health', 'strength', 'agility', 'technique', 'intelligence', 'armor'
    E.g.: adjust_archetype('Assassin', 'agility', 1.5)

Weapon Adjustments:
  - adjust_weapon_scaling(weaponType, parameter, value)
    Weapons: 'Mace', 'Sword', 'Dagger', 'Wand', or 'global'
    Parameters: 'damage' or 'damageMultiplier'
    E.g.: adjust_weapon_scaling('Dagger', 'damage', 1.15)

Player Stat Adjustments:
  - adjust_player_base_attribute(attributeName, value)
    Attributes: 'strength', 'agility', 'technique', 'intelligence'

  - adjust_player_base_health(value)

  - adjust_player_attributes_per_level(value)

  - adjust_player_health_per_level(value)

Enemy Stat Adjustments:
  - adjust_enemy_baseline_stat(statName, value)
    Stats: 'health', 'strength', 'agility', 'technique', 'intelligence', 'armor'

  - adjust_enemy_scaling_per_level(statName, value)
    Scaling: 'health', 'attributes', or 'armor'

Variable Editor (Fine-grained control):
  - set_variable(variableName, value)
    E.g.: set_variable('EnemySystem.GlobalMultipliers.HealthMultiplier', '1.2')
```

**Best Practice:**
- Make 1-3 focused changes per iteration (avoid changing too much at once)
- Prioritize "High" priority suggestions from suggest_tuning()
- Document what you change for future reference

---

### **Phase 4: Test Changes**
Verify improvements with new simulation.

```
Tools (Run in sequence):
  1. run_battle_simulation(battlesPerCombination=100)
     - Test with new configuration
     - Compare against baseline

  2. compare_with_baseline()
     - Shows what changed since set_baseline()
     - Indicates if you improved or regressed

  3. analyze_battle_results()
     - Check if previous issues are fixed
     - Look for new problems introduced

  4. get_balance_quality_score()
     - Did score improve?
     - Track across all 5 iterations
```

**Decision Point:**
- **Better?** → Keep changes, continue to next iteration
- **Worse?** → Revert using `load_patch(patch_id)` or undo manually
- **Mixed?** → Decide if trade-offs are acceptable

---

### **Phase 5: Checkpoint & Archive**
Save progress for reference.

```
Tools:
  1. save_configuration()
     - Saves current settings to TuningConfig.json

  2. save_patch(name, author, description, version, tags)
     - Save iteration results as reusable patch

Example:
  save_patch(
    name = "Iteration_1_Balance_Pass",
    author = "Your Name",
    description = "Buffed dagger damage, adjusted enemy health scaling",
    version = "1.1",
    tags = "weapon-balance,enemy-scaling"
  )
```

**Why Checkpoints Matter:**
- Roll back if future iteration breaks something
- Compare different approaches
- Share patches with team
- Version your balance changes

---

## Complete 5-Iteration Example

### **Iteration 1: Weapon Balance**
```
Phase 1: Baseline
  → run_battle_simulation(100)
  → set_baseline()

Phase 2: Analysis
  → analyze_battle_results()
    Issue: Dagger win rate only 25%, Sword at 65%
  → get_balance_quality_score() = 42

Phase 3: Adjust
  → adjust_weapon_scaling('Dagger', 'damage', 1.25)  # +25% damage

Phase 4: Test
  → run_battle_simulation(100)
  → compare_with_baseline()
    Dagger win rate now 38%, overall score = 55

Phase 5: Checkpoint
  → save_patch("Iteration_1", "DevTeam", "Buffed Dagger damage")
```

### **Iteration 2: Enemy Health Balance**
```
Phase 1: Baseline
  → set_baseline()

Phase 2: Analysis
  → analyze_battle_results()
    Issue: All weapons killing enemies too quickly, fun score only 35
  → suggest_tuning()
    Suggestion: Increase enemy health baseline

Phase 3: Adjust
  → adjust_enemy_baseline_stat('health', 50)

Phase 4: Test
  → run_battle_simulation(100)
  → get_balance_quality_score() = 62

Phase 5: Checkpoint
  → save_patch("Iteration_2", "DevTeam", "Increased enemy health")
```

### **Iteration 3: Archetype Diversity**
```
Phase 1: Baseline
  → set_baseline()

Phase 2: Analysis
  → analyze_fun_moments()
    Issue: Assassin archetype fun score 45, others 60+
  → suggest_tuning()
    Suggestion: Improve Assassin action variety

Phase 3: Adjust
  → adjust_archetype('Assassin', 'agility', 1.3)

Phase 4: Test
  → run_battle_simulation(100)
  → analyze_fun_moments()
    Assassin fun score now 58

Phase 5: Checkpoint
  → save_patch("Iteration_3", "DevTeam", "Buffed Assassin agility")
```

### **Iteration 4: Difficulty Tuning**
```
Phase 1: Baseline
  → set_baseline()

Phase 2: Analysis
  → validate_balance()
    Warning: Boss enemies too weak
  → compare_with_baseline()
    Check trend over iterations

Phase 3: Adjust
  → adjust_global_enemy_multiplier('damage', 1.15)

Phase 4: Test
  → run_battle_simulation(100)
  → compare_with_baseline()
    Improved without breaking existing balance

Phase 5: Checkpoint
  → save_patch("Iteration_4", "DevTeam", "Increased enemy damage")
```

### **Iteration 5: Final Polish**
```
Phase 1: Baseline
  → set_baseline()

Phase 2: Analysis
  → get_balance_quality_score() = 78
  → analyze_fun_moments()
    All weapons fun score 55+
  → validate_balance()
    No critical errors

Phase 3: Adjust
  → Fine-tune any remaining issues
  → apply_preset('balanced') if available

Phase 4: Test
  → run_battle_simulation(100)
  → get_balance_quality_score() = 81

Phase 5: Checkpoint
  → save_patch("Iteration_5_Final", "DevTeam", "Final balance pass - ready for 1.0")
  → save_configuration()
```

---

## Quick Reference: Tools by Phase

| Phase | Primary Tool | Secondary Tools |
|-------|---|---|
| **Baseline** | `run_battle_simulation` | `set_baseline` |
| **Analysis** | `suggest_tuning` | `analyze_battle_results`, `validate_balance`, `get_balance_quality_score`, `analyze_fun_moments` |
| **Adjust** | `adjust_*` or `apply_preset` | `set_variable` |
| **Test** | `run_battle_simulation` | `compare_with_baseline`, `get_balance_quality_score` |
| **Checkpoint** | `save_patch` | `save_configuration` |

---

## Key Metrics to Track Across 5 Iterations

Create a simple tracking table:

```
Iteration | Quality Score | Fun Score | Issues Found | Changes Made | Notes
----------|---------------|-----------|--------------|--------------|-------
Baseline  |      42       |    35     |      5       |      -       | Initial state
1         |      55       |    48     |      3       | Dagger +25%  | Improved
2         |      62       |    52     |      2       | Enemy HP +50 | Good progress
3         |      68       |    58     |      1       | Assassin +30%| Converging
4         |      75       |    65     |      0       | Enemy DMG +15%| Balanced
5         |      81       |    72     |      0       | Final tweaks | Ready!
```

**Goals:**
- Quality Score: Increase from baseline to 70+
- Fun Score: Reach 60+ (engaging gameplay)
- Issues: Reduce to 0 by iteration 5
- No crashes or validation errors

---

## Pro Tips for Success

### 1. **Don't Change Too Much**
- Max 2-3 adjustments per iteration
- Easier to identify what worked
- Easier to revert if needed

### 2. **Prioritize High-Impact Changes**
- Use `suggest_tuning()` output (lists priority)
- Weapon balance before enemy scaling
- Engagement before difficulty

### 3. **Use Presets Strategically**
- `apply_preset('baseline')` to reset if needed
- `apply_preset('aggressive_enemies')` if you want quick difficulty jump
- Good starting points before fine-tuning

### 4. **Save Patches Frequently**
- After each successful iteration
- Before trying risky changes
- Use descriptive names: "Iteration_X_Description"

### 5. **Monitor Both Metrics**
- Balance quality score (fairness)
- Fun moment score (engagement)
- Can be at odds - don't sacrifice both

### 6. **Test Different Scales**
- Start with `battlesPerCombination=50` for quick feedback
- Use `=200` for final validation
- Use `run_parallel_battles()` for specific stat combinations

### 7. **Document Everything**
- What changed each iteration
- Why you made the change
- What the result was
- Include in patch descriptions

---

## Troubleshooting Common Issues

### **Quality Score Not Improving**
- Try preset: `apply_preset('baseline')` then one change at a time
- Check `validate_balance()` for specific errors
- May need bigger adjustments: try 1.5x or 2.0x instead of 1.1x

### **Fun Score Stuck Below 50**
- Analyze: `analyze_fun_moments()` to see which weapons are boring
- Adjust: Increase action variety or damage variance
- Try: `adjust_weapon_scaling('global', 'damage', 1.2)` to increase variance

### **Changes Have No Effect**
- Verify: `get_current_configuration()` shows your changes
- Check: Are you testing the same levels/combinations?
- May need: Larger adjustment values

### **Previous Iteration Was Better**
- Use: `load_patch('Iteration_X')` to revert
- Then: Try different adjustment approach
- Or: Combine best changes from multiple iterations

---

## Expected Timeline

- **Each iteration**: 30-60 seconds (depending on simulation size)
- **Full 5 cycles**: ~5-10 minutes total
- **Analysis between cycles**: 1-2 minutes (reading and deciding)

---

## Next Steps After 5 Iterations

1. **Archive**: Save final version as "Version_1.0"
2. **Document**: Note what worked and what didn't
3. **Test**: Have players test or play through game
4. **Iterate**: Return to this process if needed
5. **Monitor**: Track player feedback for future iterations

---

## References

- `MCP_FUN_MOMENTS_GUIDE.md` - Details on fun moment calculations
- `MCP_CLAUDE_CODE_SETUP.md` - MCP tool descriptions
- Original tool list: Run `list all MCP tools` in Claude Code

