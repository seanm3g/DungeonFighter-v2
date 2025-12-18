# DungeonFighter Simulation & Tuning System - Documentation Index

## 🚀 Quick Navigation

### Just Want to Start Testing? (5 minutes)
→ Read: [`02-Development/QUICK_START_SIMULATION.md`](02-Development/QUICK_START_SIMULATION.md)
→ Do: Follow the 9 steps to run your first simulation

### Want Full Understanding? (30 minutes)
→ Read: [`02-Development/IMPLEMENTATION_SUMMARY.md`](02-Development/IMPLEMENTATION_SUMMARY.md) (this explains everything)
→ Then: [`02-Development/COMBAT_SIMULATION_README.md`](02-Development/COMBAT_SIMULATION_README.md) (detailed overview)

### Using with Claude? (10 minutes)
→ Read: [`05-Systems/MCP_INTEGRATION_GUIDE.md`](05-Systems/MCP_INTEGRATION_GUIDE.md)
→ Example: Show Claude the scenario and ask it to balance

### Deep Technical Dive? (60+ minutes)
→ Read: [`02-Development/SIMULATION_AND_TUNING_GUIDE.md`](02-Development/SIMULATION_AND_TUNING_GUIDE.md)
→ Explore: `/Code/Simulation/` source files
→ Experiment: Create custom scenarios and test them

---

## 📚 Documentation Files

| File | Purpose | Read Time | Who |
|------|---------|-----------|-----|
| **02-Development/IMPLEMENTATION_SUMMARY.md** | Complete overview of what was built and why | 10 min | Everyone - start here |
| **02-Development/QUICK_START_SIMULATION.md** | 9 simple steps to run first simulation | 5 min | People who want to start immediately |
| **02-Development/COMBAT_SIMULATION_README.md** | Deep dive into the system and how it works | 20 min | People who want to understand everything |
| **02-Development/SIMULATION_AND_TUNING_GUIDE.md** | Complete technical guide with examples | 30 min | Balance designers and power users |
| **05-Systems/MCP_INTEGRATION_GUIDE.md** | How to use with Claude and other AI agents | 10 min | People using Claude for tuning |
| **scenario_template.json** | Template for creating custom test scenarios | 5 min | When creating new test cases |

---

## 🛠️ Source Code Files

**Location**: `/Code/Simulation/`

| File | Purpose | Lines |
|------|---------|-------|
| `CombatSimulator.cs` | Single combat simulation (no UI) | 226 |
| `BatchSimulationRunner.cs` | Run multiple battles + aggregate stats | 155 |
| `SimulationAnalyzer.cs` | Analyze results, suggest improvements | 207 |
| `ScenarioConfiguration.cs` | JSON format for sharing scenarios | 167 |

**Total**: ~755 lines of new code, integrates seamlessly with existing system

---

## 🎯 Use Cases

### Use Case 1: "Are we balanced?"
**Time**: 10 minutes
```
1. run_battle_simulation(50)
2. analyze_battle_results()
3. get_balance_quality_score()
```
**Output**: Yes/No + specific issues

### Use Case 2: "Make fights take 10 turns"
**Time**: 30-45 minutes  
```
1. run_battle_simulation()
2. analyze_battle_results() → identify duration issue
3. suggest_tuning() → get recommendation
4. test_what_if() → preview change
5. apply_tuning_suggestion() → apply change
6. compare_with_baseline() → verify improvement
7. Repeat steps 2-6 until happy
```
**Output**: Balanced combat around 10-turn target

### Use Case 3: "Test Bob's balance patch"
**Time**: 5 minutes
```
1. load_patch("balance_bob_v1.2")
2. run_battle_simulation(50)
3. compare_with_baseline()
```
**Output**: Comparison: is Bob's version better? by how much?

### Use Case 4: "Have Claude balance the game"
**Time**: 1-2 hours (automated by Claude)
```
Tell Claude: "Balance the game to 10-turn combats with 50-70% win rate"
Claude does steps 1-7 from Use Case 2 automatically
```
**Output**: Fully balanced game, Claude explains reasoning

### Use Case 5: "Optimize for 3-phase combat"
**Time**: 45 minutes
```
1. Track phase distribution in results
2. Identify unbalanced phase (e.g., Phase 3 too long)
3. Adjust relevant parameters (e.g., increase final-phase player damage)
4. Test adjustment impact on phases
5. Verify new phase distribution is even
```
**Output**: Well-balanced 3-phase combat experience

---

## 📊 Key Metrics You'll Track

### Combat Duration
- Goal: ~10 turns average
- Good builds: ≤6 turns
- Target range: 6-14 turns  
- Struggling: ≥14 turns

### Combat Phases
- Phase 1 (100%→66%): ~3.3 turns
- Phase 2 (66%→33%): ~3.3 turns
- Phase 3 (33%→0%): ~3.3 turns

### Win Rate
- Goal: 50-70%
- <50%: too hard
- >70%: too easy

### Quality Score
- 0-100 scale
- 90+: Excellent
- 75-90: Good
- 60-75: Fair
- <60: Poor

---

## 🔧 Tools Available

**19+ MCP tools for testing, analysis, and tuning**

### Testing (3 tools)
- `run_battle_simulation` - Full test suite
- `run_parallel_battles` - Specific combos
- `test_what_if` - Preview changes

### Analysis (4 tools)
- `analyze_battle_results` - Assessment
- `validate_balance` - Validation
- `analyze_parameter_sensitivity` - Find optimal values
- `get_balance_quality_score` - Quality rating

### Suggestions (2 tools)
- `suggest_tuning` - AI recommendations
- `apply_tuning_suggestion` - Apply suggestion

### Configuration (3 tools)
- `get_current_configuration` - View values
- `adjust_global_enemy_multiplier` - Adjust multipliers
- `adjust_player_base_attribute` - Adjust stats

### Management (5 tools)
- `save_patch` - Save configuration
- `load_patch` - Load configuration
- `list_patches` - View all patches
- `get_patch_info` - Patch details
- `compare_with_baseline` - Compare versions

### Baseline (2 tools)
- `set_baseline` - Save for comparison
- `compare_with_baseline` - Show improvement

---

## 🚀 Getting Started (Choose Your Path)

### Path A: Quick Test (5 min)
```
1. Read: 02-Development/QUICK_START_SIMULATION.md
2. Run: run_battle_simulation
3. Check: analyze_battle_results
Done!
```

### Path B: Basic Tuning (30 min)
```
1. Read: 02-Development/IMPLEMENTATION_SUMMARY.md
2. Read: 02-Development/QUICK_START_SIMULATION.md
3. Run: Full iteration cycle (test → analyze → tune → compare)
```

### Path C: Deep Learning (2 hours)
```
1. Read: 02-Development/IMPLEMENTATION_SUMMARY.md
2. Read: 02-Development/COMBAT_SIMULATION_README.md
3. Read: 02-Development/SIMULATION_AND_TUNING_GUIDE.md
4. Explore: Source code in /Code/Simulation/
5. Create: Custom scenario from template
6. Test: Your custom scenario
```

### Path D: Collaborative AI (1 hour)
```
1. Read: 02-Development/IMPLEMENTATION_SUMMARY.md
2. Read: 05-Systems/MCP_INTEGRATION_GUIDE.md
3. Create: Initial scenario
4. Tell Claude: "Balance this scenario"
5. Let Claude work while you observe
6. Review Claude's improvements
```

---

## 📋 Pre-Session Checklist

- [ ] MCP server is running
- [ ] `/Code/Simulation/` files exist and compile
- [ ] Read at least 02-Development/QUICK_START_SIMULATION.md
- [ ] Have a scenario ready (use template or default)
- [ ] Have a goal in mind (e.g., "10-turn combats")

---

## 🎓 Learning by Example

### Example 1: Duration Too Long
**Current**: 15 turns average (too long)
**Steps**:
1. `analyze_battle_results()` → "Duration too long"
2. `suggest_tuning()` → "Increase player damage 15%"
3. `test_what_if(player_damage, 1.15)` → "Safe, improves to 12 turns"
4. `apply_tuning_suggestion()` → Apply change
5. `compare_with_baseline()` → "Quality 62→70, duration 15→12"

### Example 2: Win Rate Too Low
**Current**: 40% (too low)
**Steps**:
1. `analyze_battle_results()` → "Win rate too low"
2. `suggest_tuning()` → "Reduce enemy health 10%"
3. `test_what_if(enemy_health, 0.9)` → "Safe, improves to 55%"
4. `apply_tuning_suggestion()` → Apply change
5. `compare_with_baseline()` → "Win rate improved 40→55%"

### Example 3: Weapon Imbalance
**Current**: Sword 65% win rate, Dagger 40% (imbalanced)
**Steps**:
1. `analyze_battle_results()` → "Dagger underperforms vs Sword"
2. `analyze_parameter_sensitivity(dagger_damage, 0.9:1.2)` → "Optimal at 1.15x"
3. `test_what_if(dagger_damage, 1.15)` → "Dagger 40→52%"
4. `apply_tuning_suggestion()` → Apply change
5. `compare_with_baseline()` → "Weapon variance improved"

---

## 🤝 Collaborative Workflows

### Workflow 1: Linear Iteration
```
Alice: Create baseline_v1.0.json
Claude: Test and improve → balance_v1.1.json  
Bob: Test and improve → balance_v1.2.json
Release: balance_v1.2.json (best of all)
```

### Workflow 2: Parallel Testing
```
Alice: Tests "aggressive enemy" approach
Claude: Tests "tanky enemy" approach
Bob: Tests "fast enemy" approach
Merge: Best aspects of each approach
```

### Workflow 3: Peer Review
```
Alice: Creates patch, saves as v1.0
Bob: Tests patch, provides feedback
Claude: Analyzes feedback, makes improvements
Alice: Reviews Claude's changes, approves v2.0
```

---

## 📞 Getting Help

### Problem: "Simulation is too slow"
→ See SIMULATION_AND_TUNING_GUIDE.md → "Common Pitfalls"

### Problem: "I don't know which parameter to tune"
→ Use `suggest_tuning()` - it tells you!

### Problem: "Results are inconsistent"
→ Normal due to dice rolls. Run more battles for stability.

### Problem: "I want to test with Claude"
→ See 05-Systems/MCP_INTEGRATION_GUIDE.md → "Example Claude Interactions"

### Problem: "I'm overwhelming by options"
→ Start with 02-Development/QUICK_START_SIMULATION.md, follow the 9 steps

---

## 🎉 Success Milestones

- ✅ Run first simulation (5 min)
- ✅ Understand results (5 min)
- ✅ Make first tuning change (10 min)
- ✅ See improvement (5 min)
- ✅ Complete first balance iteration (30 min)
- ✅ Collaborate with Claude (1 hour)
- ✅ Achieve target metrics (2-4 hours)

---

## 📖 Reading Order (Recommended)

1. **This file** (2 min) - You're reading it!
2. **02-Development/IMPLEMENTATION_SUMMARY.md** (10 min) - Understand what was built
3. **02-Development/QUICK_START_SIMULATION.md** (5 min) - Get ready to test
4. **Now**: Run your first simulation!
5. **Later**: Dive into deeper guides as needed

---

**Ready to start balancing?** Pick a path above and go! 🚀
