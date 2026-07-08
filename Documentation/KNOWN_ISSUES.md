# Known Issues - DungeonFighter

Track of known problems, their status, and potential solutions.

## Critical Issues

### None Currently Known
*No critical issues identified at this time.*

## High Priority Issues

### Issue: NullReferenceException in DungeonSelectionRenderer
**Status**: ✅ RESOLVED (November 20, 2025)
**Priority**: HIGH
**Description**: Application throws NullReferenceException when rendering dungeon selection screen at `DungeonSelectionRenderer.RenderDungeonSelection()` line 69.

**Root Causes**:
1. Missing null validation on `dungeons` parameter
2. Missing validation for individual dungeon objects in the list
3. `SequenceEqual` called on potentially null list

**Solution**:
1. Added null check for `dungeons` parameter - throws ArgumentNullException with clear message
2. Added validation loop to ensure no null dungeon objects in list
3. Exception thrown before attempting to use potentially null references

**Files Modified**:
- `Code/UI/Avalonia/Renderers/DungeonSelectionRenderer.cs` (lines 68-98)

**Verification**:
- Dungeon selection screen renders without error
- Proper exceptions thrown for invalid input
- Stack trace no longer occurs

### Issue: Validate enemy action sets include damage
**Status**: ✅ RESOLVED
**Priority**: HIGH
**Description**: Some enemies defined only utility/debuff actions which could lead to unwinnable (for enemy) encounters.

**Mitigation**:
- Data fix to include at least one damaging action per enemy (e.g., added `POISON BITE` to `Prism Spider`).
- Loader safeguard injects `BASIC ATTACK` if missing.
- Test `--test-enemies` validates configuration.

**Next Steps**:
- Keep validation test in CI/regression runs.

## Medium Priority Issues

### Issue: Combat reliability closeout (Phase 4)
**Status**: ✅ RESOLVED (July 2026 — Phase 4 reliability)
**Priority**: MEDIUM
**Description**: Remaining reliability tails: `ActionLabEncounterSimulator` did not call `CombatManager.Cleanup()` (static event leak risk), `GameStateManager` could retain stale legacy dungeon/room on character register, settings/death saves blocked UI on sync disk IO, and batch lab sim used `SetAsyncLabEncounterTestRoll` (all 1d20 rolls) instead of queue semantics.

**Solution**:
1. `combatManager?.Cleanup()` in `ActionLabEncounterSimulator` finally.
2. Clear `legacyDungeonFallback` / `legacyRoomFallback` on `SetCurrentPlayer` / `AddCharacter` when syncing active character.
3. `SettingsMenuHandler.SaveGameAsync` and `DeathScreenHandler` async save paths.
4. Batch sim uses `QueueAsyncForcedD20Rolls` / `ClearAsyncForcedD20Rolls`.

**Files Modified**:
- `Code/Game/ActionInteractionLab/ActionLabEncounterSimulator.cs`
- `Code/Game/GameStateManager.cs`
- `Code/Game/SettingsMenuHandler.cs`, `DeathScreenHandler.cs`

### Issue: Combat reliability static bleed (Phase 3)
**Status**: ✅ RESOLVED (July 2026 — Phase 3 reliability)
**Priority**: MEDIUM
**Description**: Remaining process-global combat state could poison live play: interactive Action Lab used `Dice.SetTestRoll`, `DeveloperSimMode.NegativeHpFloor` leaked across tuning batches, muted sims wrote `HealthBarDeltaDamageHint` entries consumed by live HP bars, `ActionExecutor` static dictionaries were not thread-safe, and Action Lab `GameTicker.Reset()` hit the global clock.

**Solution**:
1. Action Lab steps use `Dice.QueueAsyncForcedD20Rolls` / `ClearAsyncForcedD20Rolls` (AsyncLocal).
2. `DeveloperSimMode.BeginScope(continuePastZeroHp, negativeHpFloor?)` scopes floor; fundamentals runner uses scope.
3. `HealthBarDeltaDamageHint` no-ops when `CombatUiMuteScope.IsMuted`; mute enter clears pending hints.
4. `ActionExecutor` uses `ConcurrentDictionary` for last-action maps.
5. Action Lab session holds `GameTicker.BeginIsolatedEncounterGameTime` for its lifetime.

**Files Modified**:
- `Code/Game/ActionInteractionLab/ActionInteractionLabSession.cs`
- `Code/Game/Tuning/DeveloperSimMode.cs`, `FundamentalsSimulationRunner.cs`
- `Code/Combat/UI/HealthBarDeltaDamageHint.cs`, `CombatUiMuteScope.cs`
- `Code/Actions/ActionExecutor.cs`, `ActionExecutionFlow.cs`, `ActionExecutionFlow.Selection.cs`

### Issue: Combat reliability secondary nicks (Phase 2)
**Status**: ✅ RESOLVED (July 2026 — Phase 2 reliability)
**Priority**: MEDIUM
**Description**: After Phase 1 pacing/mute fixes, remaining player-facing risks were silent combat display exceptions, load timeout that did not cancel the file read, sync menu-exit save that could hang the UI, `GameStateManager` dual-writing legacy dungeon/room fields beside `CharacterContext` (bleed on character switch), and muted sims calling `GameTicker.Reset()` on the process-global clock.

**Solution**:
1. `BlockDisplayManager` logs display failures via `DebugLogger.WriteDebugAlways` (combat continues).
2. `CharacterSaveService.LoadCharacterAsync` uses a linked CTS + `CancelAfter` so the underlying read is cancelled on timeout/caller cancel; menu exit uses `SaveCharacterAsync`.
3. `GameStateManager` dungeon/room properties prefer active `CharacterContext` and clear legacy fallbacks on switch.
4. Muted `RunCombat` / `BattleExecutor` / `CombatSimulator` wrap `GameTicker.BeginIsolatedEncounterGameTime()`.

**Files Modified**:
- `Code/UI/BlockDisplayManager.cs`
- `Code/Entity/Services/CharacterSaveService.cs`, `CharacterFileManager.cs`, `ICharacterSaveService.cs`
- `Code/Game/GameStateManager.cs`, `GameLoopInputHandler.cs`
- `Code/Combat/CombatManager.cs`, `Code/Simulation/CombatSimulator.cs`, `Code/Game/BattleStatistics/BattleExecutor.cs`

### Issue: Combat log GUI pacing / fire-and-forget races (historical)
**Status**: ✅ RESOLVED (July 2026 — Phase 1 reliability)
**Priority**: MEDIUM
**Description**: Avalonia combat log dumped action lines instantly because `CombatDelayManager` skipped GUI message delays, while sync display wrappers fire-and-forgot async batch writes so environmental turns could advance before prior UI finished.

**Solution**:
1. `DelayAfterMessageAsync` applies for GUI and console when delays are enabled; `DelayAfterActionAsync` remains a GUI no-op (end-of-action wait owned by batch `delayAfterBatchMs`).
2. Environment turns use `ProcessEnvironmentTurnAsync` → `DisplayActionBlockAsync`.
3. Sync batch/renderers dump without starting orphan async work; combat must await async APIs.
4. `CombatUiMuteScope` / AsyncLocal room context prevent mute/room bleed across lab/sim.

**Files Modified**:
- `Code/Combat/CombatDelayManager.cs`, `CombatTurnHandlerSimplified.cs`, `CombatManager.cs`
- `Code/UI/Avalonia/Coordinators/BatchOperationCoordinator.cs`, BlockDisplay renderers / `BlockDelayManager.cs`
- `Code/Combat/CombatUiMuteScope.cs`, `CombatEnvironmentContext.cs`

## Low Priority Issues

### None Currently Known
*No low priority issues identified at this time.*

## Resolved Issues

### Issue: Enemies Dying in 1 Hit
**Status**: ✅ RESOLVED
**Date Resolved**: Recent
**Description**: Enemies were dying in single hits instead of the target 10 actions due to balance issues.

**Root Causes**:
1. Double scaling of enemy stats (EnemyLoader.cs + Enemy.cs constructor)
2. Weapon damage scaling too high (1.55x multiplier)
3. Enemy DPS system using old level scaling formula

**Solution**:
1. Removed double scaling in Enemy.cs constructor
2. Reduced weapon scaling to 0.42x
3. Updated EnemyDPSSystem to match current system

**Verification**: Balance analysis shows proper "actions to kill" ratio.

### Issue: Combat Balance Broken
**Status**: ✅ RESOLVED
**Date Resolved**: Recent
**Description**: Combat was severely unbalanced with heroes dealing 32+ damage to enemies with 26 health.

**Solution**:
1. Increased player base health from 150 to 200
2. Increased health per level from 3 to 5
3. Reduced player base attributes from 15 to 8
4. Reduced critical hit multiplier from 1.5 to 1.3
5. Reduced enemy damage multiplier from 1.0 to 0.7

**Verification**: DPS analysis shows balanced combat duration.

### Issue: Weapon Damage Scaling Too High
**Status**: ✅ RESOLVED
**Date Resolved**: Recent
**Description**: Weapon damage formulas were adding massive multipliers causing excessive damage.

**Solution**:
1. Reduced weapon scaling multipliers to 0.42x
2. Updated scaling formulas in TuningConfig.json
3. Verified scaling calculations in ScalingManager.cs

**Verification**: Weapon damage now scales appropriately with level.

### Issue: Action Damage Multipliers Too High
**Status**: ✅ RESOLVED
**Date Resolved**: Recent
**Description**: High-damage actions were dealing excessive damage compared to basic attacks.

**Solution**:
1. Reduced high-damage actions from 1.6x to 1.2x (-25% damage)
2. Reduced environmental actions from 1.8x to 1.3x (-28% damage)
3. Updated action damage calculations

**Verification**: Action damage now balanced with basic attacks.

### Issue: Armor Generation Amplifying Corrupted Values
**Status**: ✅ RESOLVED
**Date Resolved**: Recent
**Description**: Armor generation system was amplifying existing corrupted values instead of generating proper base values, creating massive numbers like 30,130,992 for Tier 2 armor.

**Root Causes**:
1. `GenerateArmorFromConfig` method was multiplying existing armor values by tier multipliers
2. System was using corrupted existing values as base instead of generating clean base values
3. No proper base value lookup system for different tiers and slots

**Solution**:
1. Replaced amplification logic with proper base value generation
2. Created `GetBaseArmorForTierAndSlot` method with predefined base values
3. Implemented tier-based armor progression (Tier 1: 2/4/2, Tier 2: 4/8/4, etc.)
4. Added fallback calculation for unknown tier/slot combinations

**Verification**: Armor values now generate reasonable numbers (Tier 2 head: 4 instead of 30,130,992).

**Related Files**:
- `Code/GameDataGenerator.cs` (GenerateArmorFromConfig method)
- `GameData/Armor.json` (generated armor values)

**Prevention**: Always use base value generation instead of amplifying existing values in data generation systems.

## Issue Tracking Template

### New Issue Template
```markdown
### Issue: [Brief Description]
**Status**: [OPEN/IN_PROGRESS/RESOLVED]
**Priority**: [CRITICAL/HIGH/MEDIUM/LOW]
**Date Reported**: [YYYY-MM-DD]
**Date Resolved**: [YYYY-MM-DD] (if resolved)

**Description**: 
[Detailed description of the issue]

**Steps to Reproduce**:
1. [Step 1]
2. [Step 2]
3. [Step 3]

**Expected Behavior**:
[What should happen]

**Actual Behavior**:
[What actually happens]

**Root Cause**:
[Analysis of the underlying cause]

**Solution**:
[How the issue was or will be resolved]

**Verification**:
[How to verify the fix works]

**Related Files**:
- [File1.cs]
- [File2.cs]
- [ConfigFile.json]

**Notes**:
[Additional context or information]
```

## Issue Categories

### Combat System Issues
- Damage calculations
- Action execution
- Combo system
- Turn management
- Status effects

### Character System Issues
- Stat calculations
- Equipment bonuses
- Level progression
- Save/load functionality

### Enemy System Issues
- Enemy scaling
- AI behavior
- Stat calculations
- Spawning logic

### Data Loading Issues
- JSON parsing
- File loading
- Configuration loading
- Error handling

### UI/Display Issues
- Formatting problems
- Display errors
- Menu navigation
- Text output

### Performance Issues
- Slow execution
- Memory usage
- Garbage collection
- Optimization needs

### Balance Issues
- DPS calculations
- Scaling problems
- Progression curves
- Difficulty tuning

## Issue Resolution Process

### 1. Issue Identification
- Monitor error logs
- Review user feedback
- Run automated tests
- Check balance analysis

### 2. Issue Analysis
- Reproduce the issue
- Identify root cause
- Assess impact and priority
- Plan solution approach

### 3. Issue Resolution
- Implement fix
- Test solution
- Verify no regressions
- Update documentation

### 4. Issue Verification
- Run relevant tests
- Check balance analysis
- Manual testing
- Monitor for recurrence

## Prevention Strategies

### Code Quality
- Follow established patterns
- Use comprehensive testing
- Maintain code documentation
- Regular code reviews

### Testing
- Run tests frequently
- Use automated testing
- Test edge cases
- Monitor test results

### Monitoring
- Track error logs
- Monitor performance
- Check balance analysis
- Review user feedback

### Documentation
- Keep documentation current
- Document known issues
- Update resolution procedures
- Share knowledge

## Issue Reporting Guidelines

### When to Report
- Critical functionality broken
- Performance degradation
- Balance issues
- Data corruption
- Security concerns

### What to Include
- Clear description
- Steps to reproduce
- Expected vs actual behavior
- Error messages
- System information
- Related files

### How to Report
- Use issue tracking template
- Include relevant logs
- Provide test cases
- Suggest potential solutions
- Assign appropriate priority

## Issue Status Definitions

### OPEN
- Issue identified but not yet addressed
- Requires investigation and analysis
- May need more information

### IN_PROGRESS
- Issue is being actively worked on
- Solution is being implemented
- Testing is in progress

### RESOLVED
- Issue has been fixed
- Solution has been verified
- No longer causing problems

### CLOSED
- Issue resolved and verified
- No further action needed
- Documentation updated

## Related Documentation

- **`PROBLEM_SOLUTIONS.md`**: Solutions to common problems
- **`DEBUGGING_GUIDE.md`**: Debugging techniques and tools
- **`TESTING_STRATEGY.md`**: Testing approaches and verification
- **`DEVELOPMENT_WORKFLOW.md`**: Development process and best practices
- **`QUICK_REFERENCE.md`**: Fast lookup for issue-related information

---

*This document should be updated as new issues are identified and resolved.*
