# Multi-Agent Balance Tuning System Guide

## Overview

The multi-agent balance tuning system leverages Claude Code's ability to spawn specialized agents that coordinate to improve game balance efficiently. Instead of doing all balance work yourself, you can delegate specific roles to specialized agents that work in sequence or independently.

## Available Agents

### 1. **Tester Agent** - Balance Verification Specialist
**Role:** Run and verify all tests, catch regressions

**Command:** `/test [mode]`
- `full` - Complete test suite (simulation, validation, quality score, fun moments, baseline comparison)
- `quick` - Core metrics only (200 battles, quality score)
- `regression` - Compare current results with baseline

**MCP Tools:**
- `run_battle_simulation` - 900 battles across 36 weapon-enemy combinations
- `validate_balance` - Check balance constraints
- `get_balance_quality_score` - Quantify balance health
- `analyze_fun_moments` - Track engagement
- `compare_with_baseline` - Detect regressions

**When to Use:**
- After making balance changes to verify no regressions
- To establish a baseline: `/test full` then manually save
- To verify specific metrics are stable

### 2. **Balance Tuner Agent** - Equilibrium Specialist
**Role:** Adjust game balance and optimize win rates

**Command:** `/balance [target-winrate] [maximize-variance]`
- `target-winrate` - Win rate percentage (default: 90)
- `maximize-variance` - Enable enemy variance (default: true)

**Algorithm:**
1. Run battle simulation to get current win rate
2. Get tuning suggestions from MCP
3. Compare current vs target win rate
4. If win rate too low: buff player (lower enemy health/damage)
5. If win rate too high: buff enemies (raise enemy health/damage)
6. Maximize enemy variance every 2nd iteration (if enabled)
7. Iterate until within 2% of target or max iterations reached
8. Save configuration

**MCP Tools:**
- `adjust_global_enemy_multiplier` - Bulk enemy adjustments
- `adjust_archetype` - Fine-tune specific enemy types
- `adjust_weapon_scaling` - Tweak weapon damage
- `suggest_tuning` - Get AI recommendations
- `apply_tuning_suggestion` - Auto-apply suggestions
- `save_configuration` - Persist changes

**When to Use:**
- To reach a specific win rate target (e.g., 90%)
- To enhance enemy variance for more unique matchups
- To fix imbalanced matchups quickly

### 3. **Analysis Agent** - Data Scientist
**Role:** Deep-dive metrics, identify problems, suggest improvements

**Command:** `/analyze [focus]`
- `balance` - Overall balance assessment and recommendations
- `weapons` - Weapon variance and effectiveness analysis
- `enemies` - Enemy matchups and archetype balance
- `engagement` - Fun moments and engagement metrics

**Features:**
- Runs detailed simulation with turn logs
- Identifies which weapon-enemy combinations are problematic
- Detects imbalanced archetypes
- Tests parameter sensitivity to find high-impact changes
- Provides actionable recommendations

**MCP Tools:**
- `analyze_battle_results` - Generate detailed reports
- `run_battle_simulation_with_logs` - Detailed analysis with turn logs
- `analyze_parameter_sensitivity` - Identify high-impact parameters
- `analyze_fun_moments` / `get_fun_moment_summary` - Engagement analysis
- `suggest_tuning` - Get recommendations

**When to Use:**
- When you notice balance is off but don't know why
- To understand weapon variance issues
- To identify which enemy matchups are problematic
- Before making major balance changes (get diagnostics first)

### 4. **Game Tester Agent** - QA/Gameplay Specialist
**Role:** Actually play the game, verify fun factor, spot edge cases

**Command:** `/playtest [level] [weapon]`
- `level` - Dungeon level (1-5, default: 1)
- `weapon` - Weapon type (Mace/Sword/Dagger/Wand, default: random)

**Features:**
- Starts a fresh game
- Plays through specified dungeon level
- Tests weapon feel and matchups
- Reports on difficulty and pacing
- Identifies tedious or satisfying moments
- Provides player feedback perspective

**MCP Tools:**
- `start_new_game` - Initialize fresh games
- `handle_input` - Play through dungeons
- `get_available_actions` - Understand mechanics
- `get_inventory` / `get_player_stats` - Track progression

**When to Use:**
- To verify metrics-based balance feels good in actual play
- After major balance changes (qualitative validation)
- To spot edge cases or unexpected behaviors
- When you want human-like feedback on the experience

### 5. **Config Manager Agent** - Operations/Version Control
**Role:** Patch management, configuration storage, version tracking

**Command:** `/patch [action] [args...]`

**Actions:**
```
/patch save [name] [description]
  - Create versioned snapshot of current balance
  - Example: /patch save enhanced-enemies-v1 "Increased enemy variance"

/patch list
  - List all saved balance patches

/patch load [name]
  - Apply a previously saved patch
  - Example: /patch load enhanced-enemies-v1

/patch info [name]
  - Show detailed information about a patch
```

**Features:**
- Automatic timestamp tracking
- Metadata storage (author, description, version, tags)
- Easy rollback capability
- Change history
- Backup creation before major changes

**When to Use:**
- After successful tuning runs (save the patch)
- To revert failed experiments
- To share balance configurations with team
- To maintain multiple balance variants

---

## Master Command: Full Automation

### `/cycle [target-winrate] [iterations]`

Orchestrates a complete multi-agent balance cycle that coordinates all agents sequentially:

```
PHASE 1: ANALYSIS AGENT (Diagnoses current state)
  → Runs battle simulation
  → Analyzes results
  → Computes quality score
  → Generates tuning suggestions

PHASE 2-N: BALANCE TUNER AGENT (Applies adjustments)
  → Runs simulation to get current win rate
  → Gets suggestions from analysis
  → Applies targeted adjustments
  → Saves configuration
  → [Repeats for each iteration]

PHASE 3: TESTER AGENT (Verifies changes)
  → Runs full test suite
  → Validates constraints
  → Computes quality score
  → Analyzes fun moments
  → Compares with baseline

PHASE 4: GAME TESTER AGENT (Gameplay verification)
  → Starts new game
  → Plays through dungeon
  → Reports on fun factor
  → Checks for edge cases

PHASE 5: CONFIG MANAGER AGENT (Saves successful patch)
  → Creates timestamped patch
  → Stores metadata
  → Documents changes made
  → Updates version history
```

**Parameters:**
- `target-winrate` - Win rate goal (default: 90%)
- `iterations` - Max tuning iterations (default: 5)

**Example Usage:**
```
/cycle              # Use defaults (90% win rate, 5 iterations)
/cycle 90 10        # 90% target, up to 10 iterations
/cycle 85 3         # Lower target (85%), faster convergence
/cycle 95 7         # Higher target (95%), more iterations
```

**Time Estimates:**
- 5 iterations: ~10-15 minutes
- 10 iterations: ~20-30 minutes
- Includes all testing and verification

---

## Workflow Patterns

### Pattern 1: Quick Balance Check
```
/test quick         # Get core metrics
/analyze balance    # See what's off
```
**Time:** ~2 minutes
**When:** Daily check-ins, quick validation

### Pattern 2: Targeted Fix
```
/analyze weapons    # Identify weapon problems
/balance 90 true    # Tune to target
/test regression    # Verify no regressions
```
**Time:** ~5-10 minutes
**When:** Fixing specific issues

### Pattern 3: Deep Diagnosis
```
/analyze balance    # Overall assessment
/analyze enemies    # Enemy-specific issues
/analyze engagement # Fun moment analysis
```
**Time:** ~10 minutes
**When:** Understanding why balance is off

### Pattern 4: Full Automation
```
/cycle 90 5         # Complete cycle
```
**Time:** ~10-15 minutes
**When:** Major balance overhaul needed

### Pattern 5: Manual Iteration
```
/analyze balance                    # Diagnose
/balance 90 true                    # Tune
/test full                          # Verify
/patch save v1-tuned "description"  # Save result
```
**Time:** ~15 minutes
**When:** Want control over each step

---

## Agent Communication & State

Agents share state through:

1. **McpToolState** - Shared singleton holding:
   - Current game wrapper instance
   - Last test result
   - Baseline test result
   - Variable editor reference

2. **Structured Results** - Each agent returns:
   - Formatted string output (for display)
   - Dictionary of computed metrics
   - Success/failure status
   - Actionable recommendations

3. **File-Based Configuration** - Agents read/write:
   - `TuningConfig.json` - All game balance settings
   - `Enemies.json` - Enemy definitions
   - Patch files - Versioned configurations

---

## MCP Tools Reference

### Simulation Tools
- `run_battle_simulation(battlesPerCombination, playerLevel, enemyLevel)`
- `run_parallel_battles(playerDamage, ..., numberOfBattles)`

### Analysis Tools
- `analyze_battle_results()` - Battle analysis
- `validate_balance()` - Constraint checking
- `analyze_fun_moments()` - Fun moment analysis
- `analyze_parameter_sensitivity(parameter, range, testPoints, battlesPerPoint)`

### Balance Adjustment Tools
- `adjust_global_enemy_multiplier(name, value)` - health/damage/armor/speed
- `adjust_archetype(name, stat, value)` - Archetype-specific adjustments
- `adjust_weapon_scaling(weaponType, parameter, value)` - Weapon tweaks
- `get_current_configuration()` / `save_configuration()` - Config management

### Patch Management
- `save_patch(name, author, description, version, tags)`
- `load_patch(patchId)`
- `list_patches()`
- `get_patch_info(patchId)`

---

## Best Practices

### 1. **Always Baseline Before Major Changes**
```
/test full          # Establish baseline
[make major changes]
/test regression    # Compare with baseline
```

### 2. **Use Focused Analysis**
Don't jump to tuning without understanding the problem:
```
/analyze [specific-focus]  # Diagnose first
/balance [target]          # Then fix
```

### 3. **Save Successful Configurations**
After each successful cycle or major change:
```
/patch save descriptive-name "What changed and why"
```

### 4. **Preserve Variance**
The system tries to maximize enemy variance. Maintain it:
- Don't homogenize enemy stats
- Keep archetype differences
- Preserve per-enemy overrides

### 5. **Iterative Over Monolithic**
```
# Good: Multiple small cycles
/cycle 87 3    # Reach 87%
/cycle 90 3    # Refine to 90%

# Avoid: Single large cycle
/cycle 90 10   # One massive run
```

### 6. **Test Between Major Changes**
```
/analyze balance        # Diagnose
/balance 90 true        # Make changes
/test full              # Verify
/playtest 2 Sword       # Play-test
/patch save ...         # Save
```

---

## Troubleshooting

### "Win rate is stuck at X%"
- Run `/analyze balance` to see what's preventing progress
- Check if you're at a local optimum (enemy variance may be preventing further tuning)
- Try `/balance [lower-target]` to find achievable target

### "One weapon-enemy combo has 5% win rate"
- Run `/analyze weapons` to see detailed variance
- Run `/analyze enemies` for matchup-specific issues
- The system accepts some variance as design feature

### "Changes aren't applying"
- Verify configuration was saved: `dotnet run -- CONFIG GET health`
- Check `TuningConfig.json` was updated with new values
- Restart game if testing in-game

### "Tests show high variance, metrics look good"
- This is expected with small sample sizes (25 battles/combo)
- Run `/test full` with more battles for stability
- Fun moments variance is natural (RNG-based)

### "Want to revert to previous balance"
```
/patch list              # See saved patches
/patch load old-patch-name
/test regression        # Verify it loaded
```

---

## Architecture Notes

### Why Multi-Agent?
- **Separation of Concerns**: Each agent has one job
- **Parallel Thinking**: Multiple agents analyze same data differently
- **Distributed Work**: Could run agents on different machines
- **Scalability**: Easy to add new agents (e.g., "Performance Agent")

### Agent Independence
Agents are independent and can be:
- Run individually for focused work
- Chained for sequential workflows
- Called together for parallel analysis
- Integrated into custom scripts

### Adding New Agents
To add a new specialized agent:
1. Create `NewAgent.cs` with `public static async Task<string> Analyze(...)`
2. Add MCP tool method to `McpTools.cs`
3. Add slash command to `.claude/commands/newcommand.md`
4. Update `AutomatedTuningLoop.cs` if it should be in the cycle

---

## Examples

### Example 1: Diagnose Dagger Problems
```
/analyze weapons     # Identify Dagger-specific issues
# Output shows Dagger too slow
/balance 90 true     # Tune (will consider Dagger in suggestions)
/playtest 1 Dagger   # Play-test specifically with Dagger
/test full           # Full verification
```

### Example 2: Quick Daily Check
```
/test quick          # 2-minute snapshot
# If metrics look good, done!
# If metrics look bad:
/analyze balance     # Understand why
```

### Example 3: Major Overhaul
```
/test full           # Establish baseline
/analyze balance     # Diagnose current state
/analyze enemies     # Check enemy variance
/cycle 90 10         # Full automated tuning
/patch save final "Complete balance overhaul"
```

### Example 4: Manual Experimental Tuning
```
/balance 85 true     # Reach 85% first (easier)
/test full           # Verify at 85%
/patch save mid "Intermediate state"
/balance 90 true     # Push to 90%
/test regression     # Verify no regressions
/patch save final "Final balance"
```

---

## Measuring Success

After running agents, look for:

1. **Win Rate**
   - Target: 90% (configurable)
   - Acceptable: 88-92%

2. **Weapon Variance**
   - Mace: 85-95% win rate
   - Sword: 85-95% win rate
   - Dagger: 75-95% win rate (faster/riskier)
   - Wand: 85-95% win rate
   - Variance is expected, not a problem

3. **Enemy Matchup Uniqueness**
   - Each enemy should feel different
   - No enemy combo should be trivial or impossible
   - Fun moments distributed across all weapons/enemies

4. **Quality Score**
   - 70+: Acceptable balance
   - 80+: Good balance
   - 90+: Excellent balance

5. **Gameplay Feel** (from playtesting)
   - Combat feels responsive
   - Difficulty progression makes sense
   - No weapon feels "wrong"
   - Enemy variety keeps gameplay fresh

---

## FAQ

**Q: Can I run multiple agents in parallel?**
A: Currently they run sequentially. Future enhancement could support parallel analysis phases.

**Q: How much does it cost to run a full cycle?**
A: ~900 battles + analysis + 5 iterations of smaller tests. Typically 10-15 minutes of Claude time.

**Q: Can agents make worse balance?**
A: Unlikely. Tuner Agent moves toward target win rate. Worst case: backup with `/patch load`.

**Q: Should I always use `/cycle` or run individual agents?**
A: Use `/cycle` for major overhauls. Use individual agents for targeted fixes or diagnostics.

**Q: How do I know if balance is good?**
A: Check quality score 80+, win rate 88-92%, and play-test feels satisfying.

**Q: Can I customize agent behavior?**
A: Yes, modify agent classes or use individual tools with custom parameters.

**Q: What if agents disagree (e.g., Analysis says "buff player" but Tuner says "nerf enemies")?**
A: Different analysis focuses may have different views. Run both and look at detailed reports.

