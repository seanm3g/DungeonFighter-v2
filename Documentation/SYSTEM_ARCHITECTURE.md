# Multi-Agent Balance Tuning System Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                   CLAUDE CODE WITH MCP SERVER                   │
│                  (DungeonFighter-v2 Game Instance)              │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    SLASH COMMAND DISPATCHER                      │
├─────────────────────────────────────────────────────────────────┤
│  /test   /balance   /analyze   /playtest   /patch   /cycle     │
└─────────────────────────────────────────────────────────────────┘
                              │
        ┌─────────┬──────────┬┴──────────┬──────────┬──────────┐
        ▼         ▼          ▼           ▼          ▼          ▼
    ┌────────┐┌────────┐┌────────┐┌────────┐┌────────┐┌────────┐
    │Tester  ││Tuner   ││Analyzer││GameTest││Config  ││Master  │
    │Agent   ││Agent   ││Agent   ││Agent   ││Manager ││Cycle   │
    │        ││        ││        ││        ││Agent   ││        │
    │Test    ││Balance ││Deep    ││Gameplay││Patch   ││Coord   │
    │Verify  ││Adjust  ││Analyze ││Testing ││Mgmt    ││All     │
    └────────┘└────────┘└────────┘└────────┘└────────┘└────────┘
        │         │          │           │          │          │
        └─────────┴──────────┴───────────┴──────────┴──────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   MCP TOOL LAYER (40+ tools)                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Simulation Tools       │ Analysis Tools       │ Adjustment Tools│
│  ─────────────────     │ ──────────────       │ ────────────────│
│  • Run Battle Sim      │ • Analyze Results    │ • Adjust Health │
│  • Run Parallel        │ • Validate Balance   │ • Adjust Damage │
│  • With Turn Logs      │ • Quality Score      │ • Adjust Armor  │
│  • Parameter Sens.     │ • Fun Moments        │ • Weapon Scaling│
│  • What-If Tests       │ • Fun Summary        │ • Player Stats  │
│                        │                      │ • Archetypes    │
│                        │                      │ • Presets       │
│                        │                      │ • Save Config   │
│                        │                      │                 │
│  Information Tools     │ Game Control Tools   │ Patch Tools     │
│  ──────────────────    │ ────────────────     │ ───────────     │
│  • Game State          │ • Start New Game     │ • Save Patch    │
│  • Player Stats        │ • Save Game          │ • Load Patch    │
│  • Dungeon Info        │ • Handle Input       │ • List Patches  │
│  • Inventory           │ • Get Actions        │ • Patch Info    │
│  • Combat State        │ • Get Dungeons       │                 │
│  • Recent Output       │                      │ Variable Tools  │
│                        │                      │ ───────────────│
│                        │                      │ • List Variable │
│                        │                      │ • Get Variable  │
│                        │                      │ • Set Variable  │
│                        │                      │ • Save Changes  │
│                        │                      │ • Categories    │
│                        │                      │                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    GAME STATE & CONFIGURATION                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  TuningConfig.json          GameData/                            │
│  ──────────────────────     ─────────                            │
│  • Combat Settings          • Enemies.json                       │
│  • CombatBalance            • Weapons.json                       │
│  • Roll System              • Character.json                     │
│  • Character Stats          • Archetypes                         │
│  • Attributes               • Actions                            │
│  • Enemy System                                                  │
│    - GlobalMultipliers      Patch Files/                         │
│    - BaselineStats          ────────────                         │
│    - ScalingPerLevel        • AutoTuned_*.json                   │
│    - Archetypes             • Version history                    │
│    - LevelVariance          • Metadata                           │
│  • Progression                                                   │
│  • StatusEffects            Game Wrapper                         │
│  • LootSystem               ────────────                         │
│  • ComboSystem              • GameConfiguration                  │
│  • WeaponScaling            • Battle Manager                     │
│  • EquipmentScaling         • Combat State                       │
│  • RaritySettings           • Turn Manager                       │
│  • UI Customization         • Battle Narrative                   │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

## Agent Interaction Flow

### Sequential Flow (used by /cycle)

```
User Request: /cycle 90 5
    │
    ▼
┌──────────────────────────────────┐
│  PHASE 1: ANALYSIS AGENT         │
│  ├─ Run battle simulation         │
│  ├─ Analyze results               │
│  ├─ Get quality score             │
│  └─ Generate suggestions          │
│     │ Results stored in McpToolState
│     └─→ Provides input for Tuner
    │
    ▼
┌──────────────────────────────────┐
│  PHASE 2: BALANCE TUNER (Iter 1) │
│  ├─ Analyze suggestions           │
│  ├─ Calculate adjustments         │
│  ├─ Apply changes                 │
│  └─ Save configuration            │
│     │ Results stored in McpToolState
│     └─→ Tests adjustment
    │
    ▼
┌──────────────────────────────────┐
│  PHASE 3: TESTER AGENT (Iter 1)  │
│  ├─ Run full test suite           │
│  ├─ Check for regressions         │
│  ├─ Compute quality score         │
│  └─ Report results                │
│     │ Success? Continue to next iter
│     └─→ Tuner uses results
    │
    ├─→ [Repeat iterations 2-5]
    │
    ▼
┌──────────────────────────────────┐
│  PHASE 4: GAME TESTER AGENT      │
│  ├─ Start new game                │
│  ├─ Play through dungeon          │
│  ├─ Verify fun factor             │
│  └─ Report gameplay feedback      │
    │
    ▼
┌──────────────────────────────────┐
│  PHASE 5: CONFIG MANAGER AGENT   │
│  ├─ Create patch file             │
│  ├─ Store metadata                │
│  ├─ Add to version history        │
│  └─ Confirm save                  │
    │
    ▼
    Summary: Success! 90% win rate reached
```

### Parallel Flow (potential enhancement)

```
User Request: /analyze balance + /analyze weapons + /analyze enemies
    │
    ├──────────────────────────────────────────────────┬──────────────┐
    ▼                                                  ▼              ▼
┌────────────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│  ANALYSIS AGENT        │  │  ANALYSIS AGENT  │  │  ANALYSIS AGENT  │
│  (Balance Focus)       │  │  (Weapons Focus) │  │  (Enemies Focus) │
│  ├─ Overall metrics    │  │  ├─ Weapon vars  │  │  ├─ Matchups     │
│  ├─ Win rates          │  │  ├─ Scaling      │  │  ├─ Archetypes   │
│  └─ Recommendations    │  │  └─ Sensitivity  │  │  └─ Override      │
└────────────────────────┘  └──────────────────┘  └──────────────────┘
    │                          │                      │
    └──────────────────────────┴──────────────────────┘
                               │
                               ▼
                    Consolidated Report:
                    • Balance assessment
                    • Weapon issues
                    • Enemy issues
                    • Unified recommendations
```

## State Management

### McpToolState (Shared Singleton)

```csharp
public class McpToolState
{
    public static GameWrapper? GameWrapper { get; set; }
    public static BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? LastTestResult { get; set; }
    public static BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? BaselineTestResult { get; set; }
    public static VariableEditor VariableEditor { get; private set; }
}
```

**Purpose:** Agents share test results and configuration state without re-running expensive operations

**Lifespan:**
- Created when MCP server starts
- Persists across agent invocations
- Cleared/updated by each agent operation

### Data Flow Between Agents

```
┌─────────────────┐
│  Analysis Agent │
│  ───────────    │
│  • Runs sim     │
│  • Stores in:   │
│    McpToolState │
│    .LastTest    │
│  • Returns:     │
│    Recommendations
└────────┬────────┘
         │ Recommendation
         │ includes suggested
         │ adjustments
         ▼
┌──────────────────────┐
│  Balance Tuner Agent │
│  ─────────────────   │
│  • Reads:            │
│    LastTestResult    │
│  • Applies changes   │
│  • Saves config      │
│  • Stores new result │
│    in McpToolState   │
└──────────┬───────────┘
           │ New test
           │ result
           ▼
┌────────────────────┐
│  Tester Agent      │
│  ────────────────  │
│  • Reads:          │
│    LastTestResult  │
│  • Runs full tests │
│  • Stores result   │
│  • Compares with   │
│    BaselineTest    │
└────────────────────┘
```

## Tool Layer Organization

### By Agent

```
Tester Agent Tools:
├─ run_battle_simulation
├─ validate_balance
├─ get_balance_quality_score
├─ analyze_fun_moments
├─ compare_with_baseline

Analysis Agent Tools:
├─ run_battle_simulation
├─ analyze_battle_results
├─ get_balance_quality_score
├─ analyze_parameter_sensitivity
├─ run_battle_simulation_with_logs

Tuner Agent Tools:
├─ run_battle_simulation
├─ adjust_global_enemy_multiplier
├─ adjust_archetype
├─ adjust_player_base_attribute
├─ adjust_weapon_scaling
├─ get_current_configuration
├─ save_configuration
├─ apply_preset

Game Tester Agent Tools:
├─ start_new_game
├─ handle_input
├─ get_available_actions
├─ get_game_state
├─ get_player_stats
├─ get_combat_state

Config Manager Tools:
├─ save_patch
├─ load_patch
├─ list_patches
├─ get_patch_info
├─ get_current_configuration
├─ set_variable
├─ get_variable
├─ save_variable_changes
```

### By Domain

```
Simulation & Testing:
├─ run_battle_simulation
├─ run_battle_simulation_with_logs
├─ run_parallel_battles
├─ validate_balance
├─ test_what_if

Analysis & Recommendations:
├─ analyze_battle_results
├─ get_balance_quality_score
├─ analyze_fun_moments
├─ get_fun_moment_summary
├─ analyze_parameter_sensitivity
├─ suggest_tuning
├─ compare_with_baseline

Balance Adjustment:
├─ adjust_global_enemy_multiplier
├─ adjust_archetype
├─ adjust_player_base_attribute
├─ adjust_player_base_health
├─ adjust_player_attributes_per_level
├─ adjust_enemy_baseline_stat
├─ adjust_enemy_scaling_per_level
├─ adjust_weapon_scaling
├─ apply_preset

Game Management:
├─ start_new_game
├─ save_game
├─ get_game_state
├─ get_player_stats
├─ get_inventory
├─ get_current_dungeon
├─ get_available_dungeons
├─ get_combat_state
├─ handle_input
├─ get_available_actions

Configuration Management:
├─ get_current_configuration
├─ save_configuration
├─ set_variable
├─ get_variable
├─ list_variables
├─ list_variable_categories
├─ save_variable_changes

Patch Management:
├─ save_patch
├─ load_patch
├─ list_patches
├─ get_patch_info

Automated Tuning:
├─ suggest_tuning
├─ apply_tuning_suggestion
├─ set_baseline
├─ compare_with_baseline
├─ get_balance_quality_score
```

## Extension Points

### Adding a New Agent

1. **Create Agent Class**
   ```csharp
   // Code/MCP/Tools/NewAgent.cs
   public class NewAgent
   {
       public static async Task<string> Analyze(...)
       {
           // Use existing MCP tools
           // Return formatted string
       }
   }
   ```

2. **Register MCP Tool**
   ```csharp
   // McpTools.cs
   [McpServerTool(Name = "new_agent_run")]
   public static Task<string> NewAgentRun(...)
       => NewAgent.Analyze(...);
   ```

3. **Create Slash Command**
   ```markdown
   // .claude/commands/newcommand.md
   Description of what agent does

   Usage: /newcommand [args]
   Examples
   ```

4. **Integrate to /cycle** (if needed)
   ```csharp
   // AutomatedTuningLoop.RunFullCycle()
   output.AppendLine("PHASE N: New Agent");
   var result = await RunNewAgentPhase(output);
   ```

### Adding a New MCP Tool

1. Create tool class in `Code/MCP/Tools/`
2. Add methods with `[McpServerTool]` attribute
3. Use in agent classes
4. Update documentation

## Performance Characteristics

| Operation | Battles | Time | Cost |
|-----------|---------|------|------|
| /test quick | 200 | 2 min | Low |
| /test full | 900 | 5 min | Low |
| /analyze | 900 | 5 min | Low |
| /balance 5 | 125-625 | 5-10 min | Low-Med |
| /cycle 90 5 | ~2500 | 15 min | Medium |

All are single MCP calls (no per-operation charges beyond token usage).

## Scalability

### Current Limitations
- Sequential execution (not parallel)
- Single MCP server instance
- In-memory game state (no persistence between cycles)

### Future Enhancements
- Parallel agent execution (Phase 1 analysis branches)
- Distributed agents (run on different machines)
- Streaming results (real-time progress)
- Custom agent composition (user-defined workflows)
- Agent learning (historical tuning data analysis)

## Security Considerations

- MCP server runs locally (no external API calls)
- Configuration files are local JSON (human-readable, diffable)
- Agents have full access to game state (expected)
- Patches are versioned (easy rollback)
- No credentials stored (game-only state management)

