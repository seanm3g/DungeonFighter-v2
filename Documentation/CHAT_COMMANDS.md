# Chat Commands - DungeonFighter-v2

Quick reference for special chat commands that can be used during development.

## Process Management Commands

### @kill.mdc
**Purpose:** Terminate all DungeonFighter-related processes
**Usage:** Type `@kill.mdc` in chat
**Action:** Automatically runs PowerShell command to kill all dotnet, Code, and DF4 processes
**When to use:** 
- Game shows "locked" error
- Multiple instances running
- Stuck processes
- Need to force terminate application

**Command executed:**
```powershell
Get-Process | Where-Object {$_.ProcessName -like "*dotnet*" -or $_.ProcessName -like "*Code*" -or $_.ProcessName -like "*DF4*"} | Stop-Process -Force
```

### @tune.mdc
**Purpose:** Binary search balance tuning with systematic approach
**Usage:** Type `@tune.mdc` in chat
**Action:** Analyzes latest balance results and applies binary search tuning strategy
**CRITICAL BINARY SEARCH STRATEGY:**
- **Step 1**: Find the TWO BIG tunings that bracket the optimal value
  - **Upper Bound**: Value where enemies are too hard (0% win rate)
  - **Lower Bound**: Value where enemies are too easy (≥98% win rate)
- **Step 2**: Fine-tune between the established bounds
- **NEVER fine-tune until both bounds are found**
- **Priority order**: StatConversionRates → Pools → Archetypes → Individual Stats → Actions

**Tuning Factors (in priority order):**
1. **StatConversionRates** (global impact on all enemies)
2. **Attribute/Sustain Pools** (total points available)
3. **ArchetypeConfigs** (enemy group modifications)
4. **Individual Enemy Stats** (specific enemy adjustments)
5. **Action Damage Multipliers** (action-specific changes)

**Binary Search Logic:**
- **Bound Finding**: ±50% major changes to find upper/lower bounds
- **Fine Tune**: ±25% adjustments between established bounds
- **Micro Adjust**: ±10% when accuracy >80%
- **Overshoot Response**: Halve adjustment and reverse direction
- **Stopping Criteria**: Accuracy >95% or no improvement after 3 iterations

**Bound Finding Process:**
1. **Start with current value** (e.g., StatConversionRates = 1.0)
2. **If too hard (0% win rate)**: Make BIG reduction (-50%) to find lower bound
3. **If too easy (≥98% win rate)**: Make BIG increase (+50%) to find upper bound
4. **Continue until both bounds established**
5. **Then fine-tune between bounds**

**Expected Outcome:**
- Win rates moved to 85-98% target range
- Improved accuracy (enemies in target range)
- Better weapon balance across all types

## How to Use Chat Commands

1. **Type the command** exactly as shown (including the @ symbol)
2. **No additional explanation needed** - the command will be executed immediately
3. **Commands are case-sensitive** - use exact formatting
4. **Commands work in any context** - during development, debugging, or general chat

## Adding New Commands

To add new chat commands:
1. Add the command to this file
2. Document the purpose, usage, and action
3. Include the actual command/script that will be executed
4. Test the command to ensure it works correctly

---

*This file should be updated when new chat commands are added.*
