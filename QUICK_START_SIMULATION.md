# Quick Start: Combat Simulation & Balance Tuning

Get your simulation system running in 5 minutes.

## Prerequisites
- MCP server running (already started)
- GameWrapper initialized
- Test player and enemies configured

## Step 1: Run Initial Simulation

```
Tool: run_battle_simulation
Parameters:
  - battlesPerCombination: 50
  - playerLevel: 1
  - enemyLevel: 1
```

This runs 50 battles for each weapon × enemy combination and returns:
- Overall win rate
- Average turns per battle
- Statistics by weapon and enemy type

## Step 2: Analyze Results

```
Tool: analyze_battle_results
```

This generates a detailed report including:
- Current win rate and how it compares to target (50-70%)
- Combat duration analysis (is it 6, 10, or 14+ turns?)
- Phase distribution (are the 3 combat phases roughly balanced?)
- List of issues found
- Specific suggestions for improvement

## Step 3: Review Quality Score

```
Tool: get_balance_quality_score
```

Returns a 0-100 score with breakdown:
- Win Rate Status (too low/target/too high)
- Duration Status (too short/target/too long)
- Weapon Balance (variance < 15% is good)
- Enemy Differentiation (higher variance is good)

## Step 4: Get Tuning Suggestions

```
Tool: suggest_tuning
```

Returns prioritized list of tuning adjustments:
- Each suggestion has ID, target parameter, suggested value
- Priority level (Critical/High/Medium/Low)
- Reason why this change is recommended
- Expected impact on balance

## Step 5: Test Impact Before Applying

```
Tool: test_what_if
Parameters:
  - parameter: "enemy.globalmultipliers.health"
  - value: 0.9
  - numberOfBattles: 200
```

This shows what would happen if you made a change WITHOUT actually applying it:
- Win rate change
- Duration change
- Quality score impact
- Risk assessment

## Step 6: Apply Changes

### Option A: Apply Specific Suggestion
```
Tool: apply_tuning_suggestion
Parameters:
  - suggestionId: "suggestion_id_from_step_4"
```

### Option B: Manual Adjustment
```
Tool: adjust_global_enemy_multiplier
Parameters:
  - multiplierName: "health"
  - value: 0.9
```

## Step 7: Set Baseline for Comparison

```
Tool: set_baseline
```

This saves current test results so you can compare future changes against it.

## Step 8: Run New Simulation & Compare

```
Tool: run_battle_simulation
(same parameters as step 1)

Then:

Tool: compare_with_baseline
```

This shows:
- Quality score change (better/same/worse)
- Win rate change
- Duration change
- Weapon/enemy variance changes

## Step 9: Iterate or Finalize

If balance is good:
```
Tool: save_patch
Parameters:
  - name: "balance_v1.1"
  - author: "claude-tuner"
  - description: "Adjusted enemy health for 10-turn combats"
  - version: "1.1"
  - tags: "balanced,10-turn-target"
```

If more tuning needed, go back to step 2 with new results.

---

## Common Patterns

### "Fights are too long (average 15 turns)"
1. Get suggestions (`suggest_tuning`)
2. Look for: "Increase player damage" or "Reduce enemy health"
3. Test highest-priority suggestion (`test_what_if`)
4. Apply if risk is low (`apply_tuning_suggestion`)
5. Verify (`run_battle_simulation` + `compare_with_baseline`)

### "Win rate is too low (30%)"
1. Analyze results (`analyze_battle_results`)
2. Apply suggestion: Usually increase player damage or reduce enemy damage
3. Test it (`test_what_if`)
4. Verify impact (`compare_with_baseline`)

### "Weapon types are imbalanced"
1. Check `analyze_battle_results` - look at weapon variance
2. Use `analyze_parameter_sensitivity` to find optimal weapon damage values
3. Test adjustments and verify

### "Combat phases aren't evenly distributed"
1. Review phase statistics from analysis
2. If Phase 1 is too long: reduce early enemy health or increase player damage
3. If Phase 3 is too long: increase late-game scaling
4. Test and verify phase distribution in results

---

## Handy Shortcuts

Save current config as patch:
```
Tool: save_patch
```

Load a previous patch:
```
Tool: load_patch
Parameters:
  - patchId: "balance_v1.0"
```

List all patches:
```
Tool: list_patches
```

See current config values:
```
Tool: get_current_configuration
```

---

**Typical full cycle time: 10-15 minutes per tuning iteration**
- Run simulation: 2-3 min
- Analyze + decide: 2-3 min
- Test change: 2-3 min
- Apply + verify: 2-3 min

Good luck tuning!
