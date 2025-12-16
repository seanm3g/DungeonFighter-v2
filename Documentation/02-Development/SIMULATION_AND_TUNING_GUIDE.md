# DungeonFighter Combat Simulation & Tuning Framework

## Overview

A comprehensive system for simulating combat scenarios, analyzing balance, and iteratively tuning game parameters. Designed to support collaborative balance work where multiple AI agents (Claude, other bots) can test configurations, share results, and work toward common balance goals.

## Core Components

### 1. CombatSimulator
Fast, headless combat simulation without UI output. Runs individual combat scenarios with automatic result tracking.

**Key Features:**
- Simulates combat between player and enemy without UI overhead
- Tracks detailed metrics: turn count, damage dealt, health changes, phases
- Detects combat phases (100%-66%, 66%-33%, 33%-0%)
- Returns `CombatSimulationResult` with all statistics

**Usage Pattern:**
```csharp
var result = CombatSimulator.SimulateCombat(player, enemy);
// Returns: TurnsToComplete, PhaseBreakdown, DamageStats, etc.
```

### 2. BatchSimulationRunner
Runs multiple simulations with statistical aggregation.

**Key Features:**
- Runs N battles between player and M enemies, repeated X times each
- Aggregates statistics: win rate, average turns, phase distribution
- Calculates standard deviation and variance
- Tracks "good builds" (≤6 turns), "target range" (6-14 turns), "struggling" (≥14 turns)
- Returns `SimulationBatch` with comprehensive metrics

**Usage Pattern:**
```csharp
var batch = BatchSimulationRunner.RunSimulationBatch(player, enemies, 5, "Test Run");
// Returns: WinRate, AverageTurns, PhaseStats, BuildDistribution
```

### 3. SimulationAnalyzer
Interprets batch results and suggests improvements.

**Key Features:**
- Analyzes win rates, combat duration, phase distribution
- Identifies balance issues (too fast/slow, unbalanced phases)
- Generates specific tuning suggestions
- Exports to CSV for external analysis
- Rates build diversity and mechanical balance

**Output:**
- List of identified issues
- Actionable suggestions with rationale
- Recommended tuning magnitude (1.1x, 1.2x, etc.)

### 4. ScenarioConfiguration
JSON-based scenario format for sharing and versioning.

**Includes:**
- Player build (stats, level, class, gear)
- Enemy configurations (stats, specialization, count)
- Simulation parameters (target turns, repetitions, phase goals)
- Tuning configuration (current balance state)
- Metadata (creator, version, date)

**Purpose:**
- Share test scenarios between different agents/testers
- Version control balance states
- Create patches that others can load and test

### 5. MCP Tools Integration
All simulation tools exposed as MCP (Model Context Protocol) tools so Claude and other AI agents can use them.

**Key Tools:**
- `run_battle_simulation` - Run comprehensive tests
- `analyze_battle_results` - Get analysis and suggestions
- `run_parallel_battles` - Test specific stat combinations
- `suggest_tuning` - Get automated tuning recommendations
- `test_what_if` - Risk-assess potential changes
- `set_baseline` / `compare_with_baseline` - Track improvements
- `apply_tuning_suggestion` - Auto-apply recommended changes
- `save_patch` / `load_patch` - Share configurations

## Workflow: Achieving Your Goals

### Goal: ~10 Turns Average, With Variety (6-14 range)

**Phase 1: Initial Testing**
1. Set up a player character (Level 1, baseline stats)
2. Create 3-5 diverse enemies (different archetypes/specializations)
3. Run `run_battle_simulation` with 50 battles per combination
4. Get initial metrics on current balance state

**Phase 2: Analysis**
1. Call `analyze_battle_results` to see what's working/broken
2. Check phase distribution - ideally ~3.3 turns per phase
3. Identify win rate issues (should be 50-70% for interesting play)
4. Note which weapon/enemy combinations are imbalanced

**Phase 3: Tuning Iteration**
1. Call `suggest_tuning` to get AI-generated recommendations
2. Review suggestions and pick the highest-impact ones
3. Use `test_what_if` to preview impact before applying
4. Apply suggestion with `apply_tuning_suggestion`
5. Set new results as baseline with `set_baseline`
6. Run simulation again
7. Use `compare_with_baseline` to see improvement

**Phase 4: Fine-tuning Phases**
The framework tracks combat phases automatically:
- Phase 1: Initial engagement (100% → 66% enemy health)
- Phase 2: Mid-combat (66% → 33% enemy health)
- Phase 3: Endgame (33% → 0% enemy health)

Ideal phase distribution: roughly equal (~3.3 turns each if 10 turn average)

**Phase 5: Build Diversity**
Analysis categorizes wins as:
- "Good Builds" (≤6 turns) - Optimized players crushing weak enemies
- "Target Range" (6-14 turns) - Interesting, strategic play
- "Struggling" (≥14 turns) - Undergeared players barely scraping by

Target distribution: 20-30% good builds, 60-70% target range, 10-20% struggling

### Goal: Collaborative Testing with Multiple Agents

**Scenario Configuration (JSON)**
```json
{
  "version": "1.0",
  "name": "Alpha Test - Warrior vs Goblin",
  "createdBy": "claude-tuner-1",
  "playerConfig": {
    "level": 1,
    "classType": "Warrior",
    "equippedWeapon": "Iron Sword"
  },
  "enemyConfigs": [{
    "name": "Goblin",
    "level": 1,
    "enemyType": "Goblin",
    "specialization": "Balanced"
  }],
  "simulationParams": {
    "repetitionsPerEnemy": 5,
    "targetAverageTurns": 10,
    "goodBuildThreshold": 6,
    "strugglingBuildThreshold": 14
  },
  "tuningConfig": {
    "enemy_health_multiplier": 1.0,
    "player_damage_multiplier": 1.0
  }
}
```

**Workflow:**
1. **Alice** (human tuner) creates initial scenario and saves as `scenario_v1.0.json`
2. **Claude** loads scenario and runs `run_battle_simulation`
3. Claude analyzes results and commits `analysis_v1.0.md` with findings
4. Claude saves patch `balance_v1.1.json` with suggested adjustments
5. **Bob** (another AI agent or human) loads the patch and runs own tests
6. Bob compares results and suggests refinements
7. Create community repository where testers share scenarios, patches, and analysis

## Key Parameters to Tune

### Global Enemy Multipliers
- `health_multiplier` - Scale all enemy health (1.0 = baseline)
- `damage_multiplier` - Scale all enemy damage
- `armor_multiplier` - Scale all enemy armor
- `speed_multiplier` - Scale all enemy attack speed

### Player Base Stats
- `base_strength` - Affects melee damage
- `base_agility` - Affects dodge, crit chance
- `base_technique` - Affects accuracy
- `base_intelligence` - Affects magic damage
- `base_health` - Starting HP

### Per-Level Scaling
- `health_per_level` - HP gain per level
- `attributes_per_level` - Stat points per level

### Enemy Specific
- Archetypal specializations: Strength, Agility, Technique, Balanced
- Individual enemy type stats and health formulas

## Metrics to Monitor

### Win Condition
- **Win Rate**: Percentage of battles player wins (target: 50-70%)
- **Distribution**: How many are good/target/struggling

### Combat Duration
- **Average Turns**: How long fights last (target: 10)
- **Min/Max**: Variance in fight length
- **Phase Distribution**: Even split across 3 phases

### Mechanical Balance
- **Weapon Variety**: No single weapon dominates (variance < 15%)
- **Enemy Differentiation**: Different enemy types feel distinct
- **Build Viability**: Multiple stat combinations work

### Quality Score
Automatic 0-100 rating based on:
- Win rate in target range (40%): 0-30 = too low, 30-70% = ideal, 70-100% = too high
- Combat duration in range (40%): < 6 turns = too fast, 6-14 = ideal, > 14 = too slow
- Weapon balance (20%): Lower variance = better
- Enemy differentiation (20%): Higher variance = better

## Practical Example: Tuning for 10-Turn Combats

**Scenario: Fights are averaging 15 turns (too long)**

1. Run simulation and get analysis
2. Analyzer says: "Combat duration too long (avg 15 > target 10)"
3. Call `suggest_tuning` - gets suggestions like:
   - "Increase player damage by 15%" (high priority)
   - "Reduce enemy health by 10%" (high priority)
   - "Increase combat speed" (medium priority)
4. Test suggestion: `test_what_if("player_damage", 1.15, 200_battles)`
   - Returns: "Duration would drop to 12, quality score +5, low risk"
5. Apply: `apply_tuning_suggestion("increase_player_damage_15pct")`
6. Set baseline: `set_baseline()`
7. Run new simulation with tweaks
8. Compare: `compare_with_baseline()`
   - Shows: "Quality improved from 62 → 68, duration 15 → 12 turns"
9. Repeat until hitting target

## Data Sharing & Versioning

### Patch Files
Each patch contains:
- Metadata (name, author, version, date, description)
- Complete tuning configuration
- Optional test results (win rate, avg duration, etc.)
- Tags (archetype, difficulty, etc.)

### Version Naming
- `balance_v1.0.json` - Initial version
- `balance_v1.1_aggressive.json` - Variant (more aggressive enemies)
- `balance_v2.0_rework.json` - Major iteration

### Collaborative Workflow
```
MainRepository/
├── scenarios/
│   ├── baseline.json
│   ├── early_game_test.json
│   └── late_game_test.json
├── patches/
│   ├── balance_v1.0.json (Alice)
│   ├── balance_v1.1.json (Bob's tweak)
│   ├── balance_v2.0_combat_rework.json (Claude)
│   └── balance_v2.1_phase_balance.json (Alice)
├── analysis/
│   ├── v1.0_analysis.md
│   ├── v1.1_analysis.md
│   └── v2.0_analysis.md
└── CHANGELOG.md (tracking improvements over time)
```

## Integration with Claude

The MCP server exposes all simulation tools to Claude, enabling:

1. **Autonomous Testing**: Claude runs full test suites automatically
2. **Analysis & Interpretation**: Claude reads results and identifies patterns
3. **Suggestion Generation**: Claude recommends specific tuning adjustments
4. **Risk Assessment**: Claude evaluates impact before applying changes
5. **Collaborative Work**: Claude can load patches created by others and test them

Example Claude workflow:
> "Run a comprehensive battle simulation and tell me if the balance is good"
> Claude: Runs test, analyzes results, says "Win rate is 45% (below target 50%), suggesting I increase player damage by 12%"
> "Test that change for me"
> Claude: Runs what-if test, reports improved metrics, applies change
> "How much better did we get?"
> Claude: Compares baseline vs new, reports improvement metrics

## Future Enhancements

1. **Phase Difficulty Scaling**: Adjust individual phase difficulty
2. **Action-level Simulation**: Track which actions are used most
3. **Multi-player Scenarios**: Test team vs team balance
4. **AI Policy Testing**: Different bot strategies against fixed balance
5. **Tournament Mode**: Bracket-style testing for competitive balance
6. **Automated Regression Testing**: Alert if changes break other balance

---

**Start here**: Load the MCP server, run `run_battle_simulation`, then analyze results with `analyze_battle_results`. That gives you baseline metrics to work from!
