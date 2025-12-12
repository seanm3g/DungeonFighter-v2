# DungeonFighter Combat Simulation & Tuning System

## What You've Just Set Up

A complete framework for simulating combat, analyzing balance, and iteratively tuning game parameters. Designed specifically to:

1. **Simulate combat scenarios** without UI overhead (fast iteration)
2. **Analyze balance** across weapon types, enemy types, and player builds
3. **Suggest improvements** with AI-driven recommendations
4. **Test changes safely** before applying them permanently
5. **Share configurations** with other testers/bots in standardized formats
6. **Track improvements** through version control and patches

## Key Files Created

### Code Files (in `/Code/Simulation/`)
- **CombatSimulator.cs** - Runs individual combat simulations
- **BatchSimulationRunner.cs** - Runs large batches with stats
- **ScenarioConfiguration.cs** - JSON format for sharing scenarios
- **SimulationAnalyzer.cs** - Analyzes results and generates reports

### Documentation
- **SIMULATION_AND_TUNING_GUIDE.md** - Complete system overview
- **QUICK_START_SIMULATION.md** - 5-minute getting started guide
- **scenario_template.json** - Copy this to create custom test scenarios

### MCP Integration
All simulation tools are exposed via the MCP server (already integrated into McpTools.cs):
- 60+ tools for testing, analysis, tuning, and patch management

## Core Workflows

### Workflow 1: Quick Balance Check
```
1. run_battle_simulation(50)                    # Test all weapon/enemy combos
2. analyze_battle_results()                     # Get assessment
3. get_balance_quality_score()                  # See 0-100 score
```
Time: 5-10 minutes

### Workflow 2: Iterative Tuning
```
1. run_battle_simulation()
2. analyze_battle_results() → get suggestions
3. test_what_if(param, value) → preview impact
4. apply_tuning_suggestion() → apply change
5. set_baseline()
6. run_battle_simulation()
7. compare_with_baseline() → see improvement
8. repeat from step 2 if needed
```
Time: 10-20 minutes per iteration

### Workflow 3: Collaborative Testing
```
1. Save scenario: save_patch("name", "author", "description")
2. Share scenario_v1.0.json with team
3. Other tester loads patch: load_patch("scenario_v1.0")
4. Other tester runs tests and saves refined patch
5. Share new patch back to team
6. Track changes in version control
```

## Target Metrics Explained

### Average Turns: ~10
- **6 or fewer**: Good optimized build, rare (20-30% of wins)
- **6-14**: Target range, common (60-70% of wins)  
- **14+**: Struggling build, less common (10-20% of wins)

### Combat Phases
Combat is divided into 3 phases based on enemy health:
- **Phase 1** (100% → 66% enemy health): Opening moves
- **Phase 2** (66% → 33% enemy health): Main fight
- **Phase 3** (33% → 0% enemy health): Endgame

Ideal: Roughly equal turns in each phase (~3.3 turns if 10-turn average)

### Win Rate
- **Target: 50-70%** - Creates interesting challenge
- <50%: Too hard, players losing too often
- >70%: Too easy, players winning too reliably

### Quality Score (0-100)
Measures overall balance health:
- **90+**: Excellent
- **75-90**: Good
- **60-75**: Fair
- **40-60**: Poor
- **<40**: Critical

Scores based on:
- Win rate in target range (40% weight)
- Combat duration near 10 turns (40% weight)
- Weapon balance / variety (10% weight)
- Enemy differentiation (10% weight)

## Tuning Parameters

### Global Multipliers (affects all enemies)
- `health_multiplier` - Scale enemy HP (1.0 = baseline)
- `damage_multiplier` - Scale enemy damage
- `armor_multiplier` - Scale enemy armor
- `speed_multiplier` - Scale enemy attack speed

### Player Stats
- `base_strength` - Melee damage
- `base_agility` - Dodge/crit chance
- `base_technique` - Accuracy
- `base_intelligence` - Magic damage
- `base_health` - Starting HP
- `health_per_level` - HP gain per level
- `attributes_per_level` - Stat points per level

### Enemy Archetypes
- Strength (high damage, low speed)
- Agility (fast, moderate damage)
- Technique (accurate, medium damage)
- Balanced (all stats moderate)

## Tools at Your Fingertips

### Testing
- `run_battle_simulation` - Full comprehensive test suite
- `run_parallel_battles` - Test specific stat combinations
- `test_what_if` - Preview parameter changes

### Analysis
- `analyze_battle_results` - Detailed assessment
- `validate_balance` - Check against standards
- `analyze_parameter_sensitivity` - Find optimal values
- `get_balance_quality_score` - Overall 0-100 rating

### Tuning
- `suggest_tuning` - AI-generated recommendations
- `apply_tuning_suggestion` - Apply specific suggestion
- `adjust_global_enemy_multiplier` - Tweak multipliers
- `adjust_player_base_attribute` - Change player stats
- `adjust_enemy_baseline_stat` - Change baseline enemy stats

### Management
- `save_patch` - Save configuration as shareable patch
- `load_patch` - Load previous configuration
- `list_patches` - See all available patches
- `set_baseline` / `compare_with_baseline` - Track improvements

### Configuration
- `get_current_configuration` - See all current values
- `save_configuration` - Persist changes to disk

## Usage Examples

### Example 1: Quick Health Check
```
run_battle_simulation(50, 1, 1)           # 50 battles, level 1 player/enemy
→ Returns: 64% win rate, 10.2 avg turns

analyze_battle_results()
→ "Win rate is good. Combat duration is perfect. 
   Weapon variety could improve. Consider adjusting Sword/Dagger damage gap."
```

### Example 2: Tuning for Duration
```
run_battle_simulation() → 15 avg turns (too long!)

suggest_tuning()
→ Suggestion 1 (High): Increase player damage +15%
→ Suggestion 2 (High): Reduce enemy health -10%
→ Suggestion 3 (Medium): Increase attack speed +5%

test_what_if("player_damage", 1.15, 200)
→ "Duration would drop to 12, quality score +8, low risk"

apply_tuning_suggestion("increase_player_damage_15pct")

set_baseline()
run_battle_simulation()
compare_with_baseline()
→ "Quality improved 62→70, duration 15→12, win rate stable"
```

### Example 3: Collaborative Balance Work
```
# Alice creates initial scenario
save_patch("balance_v1.0", "alice", "Initial baseline")
# Shares balance_v1.0.json

# Bob loads and tests
load_patch("balance_v1.0")
run_battle_simulation()
→ Sees issues, makes improvements

# Bob saves refinement
save_patch("balance_v1.1", "bob", "Adjusted phase distribution")

# Alice loads Bob's work
load_patch("balance_v1.1")
compare_with_baseline()
→ "Bob's changes improved weapon balance by 8%"

# Both track improvements in version control
# Creates: balance_v1.0.json → v1.1.json → v1.2.json → v2.0.json (major rework)
```

## Next Steps

1. **Read** QUICK_START_SIMULATION.md (5 minutes)
2. **Run** your first simulation (2 minutes)
3. **Analyze** the results (2 minutes)
4. **Tune** one parameter (5 minutes)
5. **Compare** before/after (1 minute)

Total time to first successful tuning iteration: ~15 minutes

## Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│         MCP Server (Protocol Interface)              │
│  (Exposes all tools to Claude & other AI agents)    │
└────────────────┬────────────────────────────────────┘
                 │
     ┌───────────┼───────────┐
     │           │           │
┌────▼──┐ ┌─────▼──┐ ┌──────▼─────┐
│ Testing│ │Analysis │ │ Tuning &  │
│ Tools  │ │ Tools   │ │Management │
└────┬──┘ └────┬────┘ └──────┬─────┘
     │         │             │
     └────────┬┴────────────┬─┘
              │             │
        ┌─────▼─────┐   ┌───▼──────────┐
        │  Combat   │   │ Configuration│
        │ Simulator │   │   & Patches  │
        └─────┬─────┘   └───┬──────────┘
              │             │
              └────┬────────┘
                   │
            ┌──────▼──────┐
            │ Game Engine │
            │  (Existing) │
            └─────────────┘
```

## Success Criteria

You'll know it's working when:

✅ You run a simulation and get metrics back (win rate, avg turns)
✅ You get detailed analysis with specific issues identified
✅ You test a parameter change and see safe impact prediction
✅ You apply a change and see measurable improvement
✅ You can save/load patches and compare versions
✅ You can share scenarios with others and they can test them

## Common Pitfalls & Solutions

**"Simulation is too slow"**
- Reduce `battlesPerCombination` parameter (try 25 instead of 50)
- Reduce number of enemies being tested
- Run tests during off-peak times

**"Results are inconsistent between runs"**
- Normal - combat uses dice rolls, so variance is expected
- Increase number of battles for more stable averages
- Focus on trends, not single runs

**"I don't know which parameter to tune"**
- Use `suggest_tuning` - it analyzes results and prioritizes
- Check which weapon/enemy combos are most imbalanced
- Use `analyze_parameter_sensitivity` to find biggest impact

**"I'm overwhelmed by options"**
- Start with global multipliers (simplest, broadest effect)
- Then move to per-archetype adjustments
- Finally tune individual enemy types

---

**You're ready to start balancing!** Load the MCP server and begin with `run_battle_simulation` → `analyze_battle_results` → iterate.

Good luck! 🎮⚔️
