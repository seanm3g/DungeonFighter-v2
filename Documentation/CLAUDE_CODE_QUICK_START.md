# Claude Code Multi-Agent Tuning - Quick Start

## TL;DR Commands

### For Testing & Verification
```
/test full              # Run all tests (5 min)
/test quick             # Quick metrics only (2 min)
/test regression        # Compare to baseline
```

### For Analysis & Diagnosis
```
/analyze balance        # Overall assessment
/analyze weapons        # Weapon-specific issues
/analyze enemies        # Enemy balance issues
/analyze engagement     # Fun moment analysis
```

### For Balance Tuning
```
/balance 90 true        # Tune to 90% win rate with variance
/balance 85 false       # Tune to 85% without variance maximization
```

### For Gameplay Testing
```
/playtest 1 Mace        # Play level 1 with Mace
/playtest 3             # Play level 3 with random weapon
/playtest               # Play level 1 with random weapon
```

### For Configuration Management
```
/patch save v1 "Initial balance setup"
/patch list
/patch load v1
/patch info v1
```

### For Full Automation
```
/cycle 90 5             # Full cycle: analyze → tune → test → playtest → save
/cycle 85 3             # Faster convergence to 85% win rate
```

---

## Common Workflows

### Workflow A: Quick Validation (5 min)
```
/test quick
# If good: done!
# If bad: next workflow
```

### Workflow B: Targeted Fix (10 min)
```
/analyze balance        # Identify problem
/balance 90 true        # Fix it
/test full              # Verify
```

### Workflow C: Full Overhaul (15 min)
```
/cycle 90 5             # Everything automated
/patch save final "Complete balance"
```

### Workflow D: Focused Investigation (10 min)
```
/analyze weapons        # Weapon-specific diagnostics
# or
/analyze enemies        # Enemy-specific diagnostics
# or
/analyze engagement     # Fun moment analysis
```

---

## What Each Agent Does

| Agent | Command | Time | Use When |
|-------|---------|------|----------|
| **Tester** | `/test full` | 5 min | Need comprehensive verification |
| **Tuner** | `/balance 90 true` | 5-10 min | Need to reach specific win rate |
| **Analyzer** | `/analyze focus` | 5 min | Need to understand problems |
| **Game Tester** | `/playtest level weapon` | 3 min | Need qualitative feedback |
| **Config Mgr** | `/patch save name` | 1 min | Need to save/load configs |
| **Master** | `/cycle 90 5` | 15 min | Need full automated cycle |

---

## Setup

1. **Ensure MCP server is running:**
   ```bash
   dotnet run -- MCP
   ```

2. **Verify .mcp.json exists:**
   - File: `.mcp.json`
   - Points to: `dotnet run --project Code/Code.csproj -- MCP`

3. **Use slash commands:**
   ```
   /test full      # Any of the commands above
   /balance 90     # Just type the command
   /cycle 90 5     # Claude Code will execute it
   ```

---

## Example Session

```
You: /analyze balance
Claude (Analysis Agent): [Runs diagnostics, shows problems]

You: I need to reach 90% win rate
Claude: /balance 90 true
Claude (Balance Tuner): [Iterates, shows progress toward 90%]

You: Verify it worked
Claude: /test full
Claude (Tester): [Shows all metrics pass, no regressions]

You: Play-test it
Claude: /playtest 1 Mace
Claude (Game Tester): [Plays one level, reports feedback]

You: Save it
Claude: /patch save balanced-v1 "Reached 90% win rate with variance"
Claude (Config Manager): [Saved with metadata]

You: Give me a summary
Claude: [Summarizes entire session, metrics, and recommendations]
```

---

## Key Settings

All configurable in `TuningConfig.json`:

- **Enemy Health Multiplier** (current: 1.3)
- **Enemy Damage Multiplier** (current: 0.8)
- **Weapon Damage Values** (Mace: 11, Sword: 9, Dagger: 7, Wand: 8)
- **Player Attributes** (base: 3 each, +2 per level)
- **Enemy Archetypes** (Berserker, Guardian, Assassin, Brute, Mage)

Agents can adjust all of these automatically.

---

## Cost Estimates

| Operation | Battles | Time | Approx Cost |
|-----------|---------|------|------------|
| `/test quick` | 200 | 2 min | Minimal |
| `/test full` | 900 | 5 min | Low |
| `/analyze focus` | 900 + analysis | 5 min | Low |
| `/balance 90 5` | 125-625 | 5-10 min | Low |
| `/cycle 90 5` | ~2500 + all analysis | 15 min | Medium |

All operations are single requests to Claude (no per-battle charges).

---

## Troubleshooting

**Q: Commands not showing up?**
A: Slash commands are defined in `.claude/commands/*.md` files. They should appear in autocomplete.

**Q: Agent not running?**
A: Check MCP server: `dotnet run -- MCP` must be running in another terminal.

**Q: Configuration not updating?**
A: Agents save to `TuningConfig.json`. Restart game to reload config.

**Q: Win rate won't reach target?**
A: Run `/analyze balance` to see what's preventing progress. Some targets may be impossible with current balance.

**Q: Want to undo changes?**
A: Use `/patch load old-patch-name` to revert to previous config.

---

## Next Steps

1. **Start simple:** `/test quick` to see current state
2. **Diagnose if needed:** `/analyze balance` if something seems off
3. **Fix if needed:** `/balance 90 true` to tune to target
4. **Save:** `/patch save descriptive-name "what changed"`
5. **Verify:** `/test full` to ensure everything works

---

## Full Documentation

For detailed information, see:
- `MULTI_AGENT_TUNING_GUIDE.md` - Complete reference
- `.claude/commands/*.md` - Individual command docs

---

## Remember

- Agents can be run individually or combined
- Each agent is independent and can run repeatedly
- Configuration is auto-saved, manual save to versioning system
- Fun variance in weapons is expected and desired
- Quality target: 80+ quality score, 88-92% win rate

**Start with `/test quick` if unsure. It shows you the current state with no changes.**

