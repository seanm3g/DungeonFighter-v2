# Using the Simulation System with Claude (MCP Integration)

The simulation framework is integrated with Claude through the Model Context Protocol (MCP). This means Claude can directly run tests, analyze results, and make tuning suggestions.

## How to Use with Claude

### The MCP Server is Already Running
Location: `D:\Code Projects\github projects\DungeonFighter-v2\Code`
Port: stdio (communicates via stdin/stdout)

When you use Claude with the MCP server connection, all simulation tools are available.

### Example Claude Interactions

#### Simple Question: "Are things balanced?"
```
You:    "Is the current game balance good?"
Claude: [Runs run_battle_simulation internally]
        [Calls analyze_battle_results]
        "The current balance has an 58% win rate, which is good. 
         Combat averages 10.2 turns, exactly on target. However, 
         Dagger weapons underperform vs Swords (40% vs 65% win rate).
         I'd recommend adjusting Dagger damage multiplier by +12%."
```

#### Guided Tuning: "Make fights take 10 turns"
```
You:    "Fights are too long (15 turns). Fix it for me."
Claude: [Runs simulation]
        [Gets suggestions]
        [Tests what-if for top suggestion]
        "I tested increasing player damage by 15%. This would drop
         duration to 12 turns and improve quality score from 62→70.
         Low risk - I'll apply it."
        [Applies change]
        [Sets baseline]
        [Runs new simulation]
        "Done! Duration improved 15→12 turns, quality score 62→70.
         Need more tuning or shall I try another adjustment?"
```

#### Comparative Analysis: "How much better is this patch?"
```
You:    "Load the v1.1 patch and tell me if it's better"
Claude: [Loads patch_v1.1.json]
        [Runs simulation with new config]
        [Compares with baseline v1.0]
        "Patch v1.1 is significantly better:
         • Quality score: 58 → 72 (+14 points)
         • Win rate: 52% → 64% (+12%)
         • Weapon variance: 18% → 12% (more balanced)
         • Enemy variety: 8% → 15% (better differentiation)
         
         Primary improvement: Better combat duration (was 15 turns, now 11)"
```

#### Collaborative Testing: "Can you test Bob's configuration?"
```
You:    "Test this scenario and compare with our baseline" [share scenario_v1.2.json]
Claude: [Loads scenario configuration]
        [Extracts player/enemy/tuning config]
        [Runs comprehensive test]
        [Analyzes results]
        "Bob's configuration shows:
         • Win rate: 61% (excellent)
         • Combat duration: 9.8 turns (within target)
         • Quality score: 76 (good)
         
         Improvements over baseline:
         • Quality: +14 points
         • Enemy diversity: Much better
         
         Issues: Dagger slightly underpowered vs Wand (+8% recommended)
         
         Recommendation: Merge Bob's config with minor Dagger buff"
```

#### Sensitivity Analysis: "Find the best damage multiplier"
```
You:    "What's the optimal player damage value?"
Claude: [Runs analyze_parameter_sensitivity]
        [Tests player_damage from 0.8x to 1.2x in 10 steps]
        [Analyzes quality score at each point]
        "Optimal player damage multiplier: 1.09x
         
         Quality score by multiplier:
         0.8x:  45 (too weak)
         0.9x:  58 (below target)
         1.0x:  68 (good)
         1.09x: 78 (optimal) ←
         1.2x:  72 (slightly overpowered)
         1.3x:  55 (too powerful)
         
         Sensitivity: ±0.1x creates ±8-12 point quality swing
         Recommendation: Set to 1.09x for best balance"
```

## Architecture for Claude Integration

When Claude uses these tools:

```
Claude Process
    ↓
[Make API Call to MCP Tool]
    ↓
[Tool Executes in DungeonFighter Game Engine]
    ↓
[Results Returned as JSON]
    ↓
[Claude Interprets & Recommends]
    ↓
[Claude Reports or Takes Further Action]
```

## Multi-Agent Collaboration Workflow

This enables collaborative tuning with multiple bots:

```
Timeline:
---------
T0:  Alice (human) creates baseline_v1.0.json
     Commits to repository
     
T1:  Claude #1 loads scenario, runs tests
     Creates analysis_v1.0.md with findings
     Commits to repository
     
T2:  Claude #2 loads patch, creates variation
     Tests alternative tuning strategy
     Saves as balance_v1.1_alternative.json
     Commits to repository
     
T3:  Bob (human) compares both approaches
     Selects Claude #2's version as better
     Creates v1.2 based on hybrid approach
     
T4:  Claude #3 validates v1.2
     Tests against multiple scenarios
     Approves with notes in repository
     
T5:  Release as production: balance_v1.2.json
     Tag: "released"
```

## Available Tools for Claude

### Core Testing (3 tools)
- `run_battle_simulation` - Full test suite
- `run_parallel_battles` - Specific stat combos
- `test_what_if` - Preview changes

### Analysis (4 tools)
- `analyze_battle_results` - Detailed assessment
- `validate_balance` - Check against standards
- `analyze_parameter_sensitivity` - Find optimal values
- `get_balance_quality_score` - Overall rating

### Suggestions (2 tools)
- `suggest_tuning` - AI recommendations
- `apply_tuning_suggestion` - Apply specific suggestion

### Management (5 tools)
- `save_patch` - Save as shareable configuration
- `load_patch` - Load previous version
- `list_patches` - View all patches
- `get_patch_info` - Detailed patch metadata
- `compare_with_baseline` - See improvement

### Configuration (3 tools)
- `get_current_configuration` - View all values
- `adjust_global_enemy_multiplier` - Adjust multipliers
- `adjust_player_base_attribute` - Adjust player stats

### Baseline Tracking (2 tools)
- `set_baseline` - Save for comparison
- `compare_with_baseline` - Show improvements

**Total: 19+ core tuning tools available to Claude**

## Example Prompt for Claude

```
You're a game balance expert. I want combat to average 10 turns with good variety.
Here's the current state: [scenario_v1.0.json]

Your job:
1. Test the current balance
2. Identify what's wrong
3. Suggest specific fixes
4. Test the fixes
5. Tell me if things improved

Go!
```

Claude's process would be:
1. `run_battle_simulation()` - get current metrics
2. `analyze_battle_results()` - identify issues
3. `suggest_tuning()` - generate suggestions
4. `test_what_if()` - preview top suggestion
5. `apply_tuning_suggestion()` - make change
6. `set_baseline()` - save current state
7. `run_battle_simulation()` - test new state
8. `compare_with_baseline()` - show improvement
9. Report results and recommendations

## Sharing Configurations Across AI Agents

Format: JSON patch files with metadata

```json
{
  "patchId": "balance_v2.0_claude_rework",
  "name": "Combat Rework v2.0",
  "author": "claude-ai-tuner",
  "createdDate": "2025-12-11",
  "description": "Complete combat rebalance focusing on phase distribution",
  "version": "2.0",
  "tags": ["production", "phase-balanced", "10-turn-target"],
  "testResults": {
    "averageWinRate": 0.64,
    "battlesTested": 5000,
    "qualityScore": 78
  },
  "configuration": { /* full tuning config */ }
}
```

Other agents (or humans) can:
- Load it: `load_patch("balance_v2.0_claude_rework")`
- Test it: `run_battle_simulation()`
- Compare it: `compare_with_baseline()`
- Iterate on it: Make adjustments and save as new version

## Best Practices for AI Tuning

### Do ✅
- Save baseline before making major changes
- Test what-if before applying changes
- Use prioritized suggestions (test high-priority first)
- Compare results before/after
- Document reasoning in patch metadata
- Share patches with clear descriptions

### Don't ❌
- Make multiple large changes at once (can't tell what helped)
- Apply suggestions without testing impact
- Ignore risk assessment in what-if results
- Forget to set baseline for tracking improvements
- Mix different balance approaches in one patch

### Good Tuning Etiquette
- Keep patches in version control (v1.0, v1.1, v1.2, etc.)
- Leave comments/documentation for other agents
- Test thoroughly before sharing
- Compare new work with previous versions
- Don't make breaking changes without discussion

## Integration with Your Development

The simulation system integrates with existing game code:

```
Your Game Engine (existing)
    ↓
[CombatManager, Character, Enemy, etc.]
    ↓
CombatSimulator (new)
    ↓
Batch/Analysis/Tuning (new)
    ↓
MCP Server (new)
    ↓
Claude & Other Agents
```

Existing code is unchanged. New layer sits on top and can be disabled with:
```csharp
CombatManager.DisableCombatUIOutput = true;  // Already in place!
```

## Next: Practical Session

When you're ready to start:

1. Tell Claude: "I have an MCP server running with game simulation tools"
2. Describe what you want: "Make combats average 10 turns with good variety"
3. Provide context: Share your current scenario or initial test results
4. Claude will take it from there!

Example opening to Claude:
```
I have a DungeonFighter game with an MCP server that can run combat
simulations. I want to tune the balance so:
- Fights average ~10 turns
- Win rate is 50-70%
- Combat feels like 3 distinct phases
- Different weapons and enemies feel distinct

Can you run a test to see where we stand, then help me tune it?
```

---

**The system is ready. Claude is ready. Time to balance!** 🎮
