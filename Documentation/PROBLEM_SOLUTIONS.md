# Problem Solutions - DungeonFighter

This document contains solutions to common problems encountered during development. Use this as a quick reference when similar issues arise.

## Recent Fixes

### Feature: Action Lab sequence edit resets strip to first slot (July 2026)
**Goal:** After changing actions in the Action Lab combo sequence, the strip highlight / next-step pointer should return to slot 1 instead of keeping a mid-sequence `ComboStep`.

**Solutions:**
1. `ActionInteractionLabSession.ResetLabStripPositionToFirstSlot` sets `ComboStep = 0`, syncs the catalog pick, and refreshes UI
2. Called from catalog add, `TryRemoveFromLabCombo`, and Action Lab strip drag-reorder completion
3. Test: `LabSequenceEdit_ResetsStripPositionToFirstSlot`

**Related files:** `ActionInteractionLabSession.LabSetup.cs`, `MouseInteractionHandler.cs`, `ActionInteractionLabTests.cs`

### Bugfix: ACTION bank sticky on recipient after miss (July 2026)
**Problem:** After Rapid Strike queued Multihit/`DAMAGE_MOD` onto Slam, a miss reset `ComboStep` to 0 and the strip/`2x`/cyan shimmer jumped to Rapid Strike even though the bank was still pending for Slam.

**Solutions:**
1. `PendingActionCadencePreviewSlot` records the intended recipient on bank deposit (`GetNextComboSlotForPendingBonuses`)
2. Strip preview, shimmer, MH/AMP peeks paint the bank on that sticky slot (not live `ComboStep`)
3. Combat Selection peeks/redeems the bank only when the executed action’s combo slot matches the sticky recipient
4. Tests: `TestActionCadenceBankStaysOnRecipientAfterMiss`, `TestCueBankStaysOnStickySlotAfterComboStepReset`

**Related files:** `CharacterEffectsState.cs`, `CombatActionStripBuilder.cs`, `ActionBonusBorderShimmer.cs`, `ActionExecutionFlow.Selection.cs`, `RollModificationManager.cs`

### Feature: Strip cards bake amp into damage (no amp: label) (July 2026)
**Goal:** Card swing lines no longer append `| amp: N.NNx`; Effective damage already includes TECH slot amp (and pending sheet `AMP_MOD`). Hover keeps `AMP: … = Pow(…)`. Combat-log amp footers unchanged.

**Solutions:**
1. `FormatStripSwingLine` / compact `%` line: damage | speed only
2. `GetStripSwingDisplayPercents` Effective mode uses `GetStripSwingDisplayAmp` (slot TECH + pending AMP_MOD)
3. Tests: `CombatActionStripBuilderTests` (no `amp:` on card; damage rises with amp)

**Related files:** `CombatActionStripBuilder.cs`, `CombatActionStripBuilder.Tooltips.cs`

### Feature: Action strip shows AMP + calc on action info (July 2026)
**Goal:** Slot amp was easy to miss when reading strip cards (only combat log footers showed `amp: 1.02x`), so second-slot damage looked unexplained vs the card number.

**Solutions:**
1. Strip Effective damage multiplies `Pow(TECH baseline, strip index)` plus pending sheet `AMP_MOD` (card no longer shows a separate `amp:` segment; hover still has AMP calc)
2. Hover tooltip adds `AMP: … = Pow(…)` (with sheet multiplier when pending)
3. Tests: `CombatActionStripBuilderTests` swing/tooltip amp assertions

**Related files:** `CombatActionStripBuilder.cs`, `CombatActionStripBuilder.Tooltips.cs`, `DungeonRenderer.RoomAndCombat.cs`

### Feature: Action set filters gameplay + Action Lab (July 2026)
**Goal:** Settings → Actions **Action set** should control what exists in-game and in Action Lab, not only the workshop list.

**Solutions:**
1. Persist selection as `GameSettings.ActionsActiveSetMaxTier` (`null` = all tiers)
2. `ActionSetVisibility` + filtered `ActionLoader.GetAllActionNames` / `GetAllActions` / `GetAction` / `HasAction` / `GetActiveSetActionData`
3. Gear, loot, defaults, environments, and lab catalog consume the active set; Settings editor still uses full `GetAllActionData`
4. Changing the dropdown refreshes the live hero pool and lab catalog; tests: `ActionSetVisibilityTests`

**Related files:** `ActionSetVisibility.cs`, `ActionLoader.cs`, `ActionsTabManager.cs`, `GameSettings.cs`

### Feature: Actions cadence mechanic dropdown (Action-set style) (July 2026)
**Goal:** Restore a clear Action-set-style dropdown for adding mechanics on ability/action rows in Settings → Actions.

**Solutions:**
1. Mechanic ComboBox uses short labels (`Hero ACC`, `WEAKEN`), `Pick mechanic…` placeholder, and `SettingsInputApplier` chrome
2. Timing-agnostic mechanics (`heal`, `disrupt`) remain in every cadence’s dropdown list
3. Already-authored mechanic IDs stay in the ItemsSource even when the cadence filter would hide them
4. Tests: `ActionMechanicsRegistryTests` (cadence-agnostic list + dropdown labels)

**Related files:** `ActionFormSectionBuilders.CadenceMechanics.cs`, `ActionMechanicsRegistry.cs`

### Issue: Combo-band rolls (14+) amplified raw damage by 1.5× (July 2026)
**Symptoms:**
- Hero panel Damage showed **16**, but a combo-tier swing (`roll: 16`) dealt **24** (`attack 24`) even with `amp: 1.00x`
- Players expected 14+ only to unlock combo actions / strip AMP, not multiply raw damage

**Root cause:**
`CombatBalance.RollDamageMultipliers.ComboRollDamageMultiplier` defaulted to **1.5** (and balance `default.json` / variance-compression endpoints reintroduced values > 1.0)

**Solutions:**
1. Ship default + unset repair = **1.0**; active balance patches `GameData/Patches/Balance/default.json` and `Code/Patches/Balance/default.json` set to **1.0**
2. Variance compression chaotic/regular combo-band endpoints both **1.0** so the master slider cannot reintroduce band amplify
3. Regression: `DamageCalculatorTests.TestComboBandRollDoesNotAmplifyRawDamage`

**Related files:** `CombatConfig.cs`, `RollFeelVarianceCompression.cs`, balance `default.json`, `DamageCalculator.cs` (consumer)

### Issue: ACTION bonus lines stick on strip cards after the action (July 2026)
**Symptoms:**
- After **RAPID STRIKE** banked Multihit and **SLAM** redeemed it (2 hits), both strip cards still showed **`2x`** damage
- Rapid Strike still showed authored **`ACTION (1x)` / `MULTIHIT +1`** while Multihit was pending on Slam

**Root cause:**
1. Redeemed `ConsumedMultiHitMod` was cleared only at the *start* of the next swing, so strip paint between swings included spent Multihit on every slot
2. Authored ACTION grant lines were always drawn on the grantor card, even after those bonuses had already been deposited into the pending bank

**Solutions:**
1. Clear `Consumed*` modifier bonuses at end of `ActionExecutionFlow.Execute`; strip `BuildPanelData` peeks pending Multihit without Consumed*
2. `BuildActionStripModifierTailLines` hides authored ACTION grant groups while any ACTION pending exists, and draws pending bonuses on the recipient slot instead
3. Tests: `CombatActionStripBuilderTests.TestActionCadenceGrantLinesResetWhenPendingThenRedeemed`, `MultiHitTests` post-redeem strip assertions

**Related files:** `ActionExecutionFlow.cs`, `CombatActionStripBuilder.cs`, `RollModificationManager.cs`, `DungeonRenderer.RoomAndCombat.cs`

### Feature: Action-bonus strip cards shimmer (July 2026)
**Goal:** Make combo-strip cards that currently have pending ACTION-cadence buffs visually distinct at a glance (recipient cue, not grantor cue).

**Solutions:**
1. `ActionBonusBorderShimmer` animates a cyan dual-sine border plus a traveling perimeter highlight (~¾ of the frame, with a visible gap) while such cards are on-screen
2. Cue is `SlotHasPendingBonusCue`: per-slot pending queue and/or additive bank on the current `ComboStep` (same peek basis as strip damage preview). Authored `ActionAttackBonuses` alone do not shimmer
3. Selected next-slot cards shimmer white↔cool cyan; hit/miss/combo flash from `HeroActionStripFeedback` still overrides
4. Timer keep-alive stops shortly after the strip no longer needs shimmer (no forever refresh on main menu)
5. Tests: `ActionBonusBorderShimmerTests`

**Related files:** `ActionBonusBorderShimmer.cs`, `DungeonRenderer.RoomAndCombat.cs`

### Issue: Rapid Strike Multihit applied to itself in combat log (July 2026)
**Symptoms:**
- After **RAPID STRIKE** combo hit, combat log showed `(2 hits)` and strip/readouts looked like Multihit applied on Rapid Strike
- Log also correctly showed `(Next action: +1 MH)` for Slam

**Root cause:**
ACTION cadence Multihit is deposited into the next-action bank **after** damage, then combat-log formatting and deferred accuracy scaling called `GetEffectiveMultiHitCountForModifierScaling`, which peeked that just-queued Multihit (and/or the next combo step’s pending Multihit) and attributed it to the granting swing.

**Solutions:**
1. Record `ActionExecutionResult.ResolvedMultiHitCount` at damage time (before bank deposit)
2. Combat-log formatting and deferred sheet accuracy hit-layers use that count, not a post-deposit peek
3. Tests: `MultiHitTests.TestActionCadenceMultiHitDoesNotApplyToGrantingAction` (+ assertions on dual-authorship test)

**Related files:** `ActionExecutor.cs`, `ActionExecutionFlow.Outcomes.cs`, `MultiHitTests.cs`

### Issue: Rapid Strike MULTIHIT_MOD applied twice (July 2026)
**Symptoms:**
- **RAPID STRIKE** logged `(3 hits)` and queued `+1 MH`, while **SLAM** strip showed **`3x`** damage (as if +2 Multihit)
- Intended grant is **+1 Multihit** on the next action only (Rapid Strike itself is 1 hit)

**Root cause:**
RAPID STRIKE (and similar Settings-authored next-action rows) store the same grant in **both** `multiHitMod` and `actionAttackBonusesJson` (`MULTIHIT_MOD`). On hit+combo the bank path queued from `ActionAttackBonuses` and `AddModifierBonusesFromAction` also enqueued the sheet column onto the next combo slot. Strip peek + redeem **summed** slot + bank → **+2**.

**Solutions:**
1. When ACTION `ActionAttackBonuses` already cover SPEED/DAMAGE/MULTIHIT/AMP mod types, skip those sheet columns in `AddModifierBonusesFromAction` (bank remains authoritative)
2. Test: `MultiHitTests.TestActionCadenceMultiHitNotDoubleAppliedFromSheetAndBonuses`

**Related files:** `CharacterEffectsState.cs`, `MultiHitTests.cs`

### Issue: Next-action Multihit (ACTION cadence) dropped on miss (July 2026)
**Symptoms:**
- After **RAPID STRIKE** (or similar) queued **+1 MH** on the next strip action, a **miss** cleared the pending Multihit
- Strip no longer showed the boosted hit count until a fresh grant

**Root cause:**
`ResolvePendingActionCadenceBonuses` always **consumed** the ACTION slot queue and additive bank whenever they were peeked for a roll, then only **applied** them on hit+combo — so miss / non-combo hit forfeited the pending mods.

**Solutions:**
1. Redeem (consume + apply) **only when `result.IsCombo`**; miss and non-combo hit leave pending in place
2. Strip preview includes pending `MULTIHIT_MOD` via `RollModificationManager.GetEffectiveMultiHitCountForModifierScaling` / `PeekPendingActionCadenceMultiHitMod`
3. Tests: `ActionBonusMechanicsTests` miss/non-combo keep; `MultiHitTests.TestActionCadenceMultiHitSurvivesMissUntilCombo`

**Related files:** `ActionExecutionFlow.Outcomes.cs`, `RollModificationManager.cs`, `CharacterEffectsState.cs`

### Issue: Item Generation affix slot dropdown looked broken (July 2026)
**Symptoms:**
- Changing **Affix rules for:** (Head / Chest / Legs / Feet / Weapon) did not appear to change the grid
- Users assumed the control was disconnected

**Root cause:**
1. Shipped balance `itemAffixByRarity.perItemType` tables are **identical** across all five slots, so a correct switch still shows the same mins
2. Scratch build shallow-copied `PerItemType` entry objects (risk of aliasing live tuning)
3. Slot `SelectionChanged` could no-op if `SelectedItem` was not a plain `string`, or if parent walk failed; LoadSettings could also re-enter the handler while resetting to Head

**Solutions:**
1. `ItemAffixScratchBuilder` deep-clones per-slot rows; resolves slot from SelectedItem, SelectedIndex, or AddedItems
2. Handler keeps a wired panel ref, suppresses SelectionChanged during LoadSettings, paints a **Showing table for:** label
3. UI copy notes that shipped defaults often match until you edit a slot
4. Tests: `ItemConfigTests.TestItemAffixScratchBuilderDeepClonesPerSlot`, `TestItemAffixScratchBuilderResolvesSlotKey`

**Related files:** `ItemAffixScratchBuilder.cs`, `ItemGenerationPanelHandler.cs`, `ItemGenerationSettingsPanel.axaml`

### Issue: Stun lasted many enemy turns after Room Collapse (July 2026)
**Symptoms:**
- Combat log showed `STUN for 1 turn` then ~8–10 enemy swings before the hero acted again
- Felt like stun duration ignored the hero's attack time

**Root cause:**
1. Stun skips called `AdvanceEntityTurn`, which **snaps** readiness to global game time then adds attack speed
2. Environment actions cost `15 × action.Length` seconds (Room Collapse ≈ 30s), creating huge catch-up debt for the foe
3. Snapping the stunned hero to "now" parked them at the end of that debt while the foe resolved catch-up from their old timeline
4. Duration tick used `attackSpeed / 10.0` instead of one discrete stun turn per skip

**Solutions:**
1. `StunProcessor` consumes **one stun turn** per skip (`UpdateTempEffects(DEFAULT_ACTION_LENGTH)`)
2. Skip recovery advances by the victim's **`GetTotalAttackSpeed()`** via `ActionSpeedSystem.AdvanceOwnTimeline` (same own-timeline contract as `ExecuteAction`)
3. Tests: `StunProcessorTests`, `ActionSpeedSystemTests.TestAdvanceOwnTimeline`

**Related files:** `StunProcessor.cs`, `ActionSpeedSystem.cs`

### Issue: Combat Reliability Phase 4 — Closeout (July 2026)
**Symptoms:**
- Repeated lab encounter sims could leak `OneShotKillOccurred` subscriptions
- Legacy GSM dungeon/room fallbacks could resurrect on character switch
- Settings save and death-screen saves blocked UI on sync disk writes
- Batch lab sim forced all 1d20 rolls, diverging from interactive lab

**Solutions:**
1. `ActionLabEncounterSimulator` calls `CombatManager.Cleanup()` in finally
2. `GameStateManager` clears legacy dungeon/room when active character is registered or switched
3. `SaveGameAsync` / `SaveCharacterAsync` on settings and death paths
4. Batch sim uses `QueueAsyncForcedD20Rolls` (1d20 queue only)

**Related files:** See `COMBAT_RELIABILITY_PHASE4.md`

### Issue: Combat Reliability Phase 3 — Scoped Static State (July 2026)
**Symptoms:**
- Interactive Action Lab `SetTestRoll` leaked forced d20 across parallel work
- `DeveloperSimMode.NegativeHpFloor` persisted across fundamentals tuning batches
- Muted sim DoT ticks left `HealthBarDeltaDamageHint` entries that colored live HP bar segments
- `ActionExecutor` static dictionaries could race under parallel battle stats
- Action Lab `GameTicker.Reset()` zeroed the process-global clock during bootstrap

**Solutions:**
1. Action Lab steps use `Dice.QueueAsyncForcedD20Rolls` (AsyncLocal, 1d20 only)
2. `DeveloperSimMode.BeginScope(continuePastZeroHp, negativeHpFloor?)` scopes floor per batch
3. `HealthBarDeltaDamageHint` skips writes when muted; `CombatUiMuteScope.Begin` clears pending hints
4. `ConcurrentDictionary` for `ActionExecutor` last-action maps; `IDictionary` in `ActionExecutionFlow`
5. Action Lab session holds `GameTicker.BeginIsolatedEncounterGameTime` from `Begin` to `EndSession`

**Related files:** See `COMBAT_RELIABILITY_PHASE3.md`

### Issue: Combat Reliability Phase 2 — Secondary Nicks (July 2026)
**Symptoms:**
- Combat display exceptions vanished into `Debug.WriteLine` only
- Load timeout left file reads running after the UI timed out
- Save & Exit blocked the UI on sync disk write
- Switching characters could show the previous hero's dungeon/room
- Parallel muted battles / sims zeroed live `GameTicker` via `Reset()`

**Solutions:**
1. `BlockDisplayManager.LogDisplayFailure` → `DebugLogger.WriteDebugAlways`
2. Linked CTS + `CancelAfter` on `LoadCharacterAsync`; `SaveCharacterAsync` for menu exit
3. `GameStateManager` context-first dungeon/room (no dual-write bleed)
4. Isolated encounter game time whenever combat UI is muted

**Related files:** See `COMBAT_RELIABILITY_PHASE2.md`

### Issue: Hero Armor Was a Consumable Room Pool (July 2026)
**Symptoms:**
- Equipped armor depleted as hits landed and reset at the start of each fight/room
- Combat HUD showed `Armor current/max` as if armor were a second HP bar
- Heroes and enemies used different armor maths (pool absorption vs flat DR)

**Root Cause:**
`CharacterHealthManager` treated armor as a depleting pool absorbed in `TakeDamage`, while `DamageCalculator` skipped flat reduction for heroes. Room entry, combat end, and equip paths called `RefreshRoomArmor()` to refill the pool.

**Solution:**
1. Apply the same flat armor subtraction for heroes and enemies in `DamageCalculator.ResolveTargetArmor` / `CalculateDamage`
2. Stop consuming armor in `TakeDamage`; keep `CurrentArmor` / `GetMaxArmor` as the effective (derived) value
3. Leave `RefreshRoomArmor` as a no-op for call-site compatibility; update HUD and roll footers to persistent flat DR

**Related files:** `CharacterHealthManager`, `DamageCalculator`, `CharacterPanelRenderer`, `RollInfoFormatter`, `DamageFormatter`, `CombatResults`

### Issue: Combat Log GUI Instant Dump + Fire-and-Forget Race (July 2026)
**Symptoms:**
- Avalonia combat action lines appeared all at once instead of line-by-line
- Environmental hazard turns could start overlapping the previous action's log
- Lab silent sims and battle statistics could leave combat UI muted for a live fight

**Root Causes:**
1. `CombatDelayManager.DelayAfterMessageAsync` returned immediately whenever a custom UI manager existed, while `BatchOperationCoordinator` still awaited those no-op delays between lines
2. Sync display wrappers (`WriteColoredSegmentsBatch`, `RenderMessageGroups`) started async work with `_ = ...` so the combat loop did not wait
3. Process-global `DisableCombatUIOutput` / `CombatEnvironmentContext.CurrentRoom` were shared across lab, sims, and live combat; `TurnManager` had a second divergent mute flag

**Solutions:**
1. Restore real GUI inter-line delays in `DelayAfterMessageAsync`; keep GUI `DelayAfterActionAsync` as a no-op so end-of-action pacing stays solely on batch `delayAfterBatchMs`
2. Environment turns await `DisplayActionBlockAsync`; sync batch/render paths dump without orphaning tasks
3. `CombatUiMuteScope` (AsyncLocal) + scoped room context; `TurnManager.DisableCombatUIOutput` aliases `CombatManager`
4. `RunCombat` / background dungeon accept `CancellationToken`; battle executors call `Cleanup()`

**Related files:** `CombatDelayManager`, `BatchOperationCoordinator`, `CombatTurnHandlerSimplified`, `CombatUiMuteScope`, `CombatEnvironmentContext`, `BackgroundDungeonTaskManager`, `BattleExecutor`

### Issue: Combat Freeze During Battle (November 20, 2025)
**Symptoms:**
- Game shows "not responding" during combat
- Enemy encounter screen freezes at start of combat
- No error message, just hangs

**Root Causes (2 Issues):**

**Issue #1 - Non-existent Method Calls:**
- CombatManager.cs was calling `WaitForMessageQueueCompletionAsync()` method that doesn't exist
- Called 4 times during combat loop causing runtime errors

**Issue #2 - Async/Await Deadlock (PRIMARY CAUSE):**
- DungeonRunnerManager called synchronous `RunCombat()` wrapper from UI thread
- `RunCombat()` used `Task.Run()` to move to background thread
- `RunCombatAsync()` on background thread tried to access/post to UI
- UI thread blocked waiting for `Task.Run()` to complete
- Background thread blocked waiting for UI operations
- **CIRCULAR DEADLOCK = FREEZE**

**Solutions:**

1. **CombatManager.cs (4 locations):**
   - Replaced all `await canvasUI.WaitForMessageQueueCompletionAsync()` with `await Task.Delay(50)`
   - Lines affected: Player turn (~200), Enemy turn (~220), Environment turn (~240), Battle end (~257)

2. **DungeonRunnerManager.cs (Line 215):**
   - Changed from: `combatManager.RunCombat(...)`
   - Changed to: `await combatManager.RunCombatAsync(...)`
   - Eliminates the `Task.Run()` wrapper that caused the deadlock

**Why This Works:**
- Calling `RunCombatAsync()` directly allows proper async flow without deadlock
- No circular wait between UI thread and background thread
- UI synchronization works naturally through await chains

**Testing:**
- Combat now flows without freezing
- All actions display properly
- Game responds to input during combat
- Multiple consecutive combats work correctly

## Combat Balance Issues

### Problem: Enemies dying in 1 hit instead of target 10 actions
**Symptoms:**
- Hero dealing 32+ damage to enemies with 26 health
- Combat ending too quickly
- Enemy STR showing 30+ instead of expected 7-8

**Root Causes & Solutions:**
1. **Double Scaling**: Enemy stats scaled twice (EnemyLoader.cs + Enemy.cs constructor)
   - **Fix**: Remove scaling in Enemy.cs constructor
2. **Weapon Damage Too High**: Weapon scaling formulas adding massive multipliers (1.55x)
   - **Fix**: Reduce weapon scaling to 0.42x
3. **Enemy DPS System Mismatch**: Using old level scaling formula
   - **Fix**: Update EnemyDPSSystem to match current system

**Verification:**
- Check enemy stats in combat display
- Verify weapon damage scaling in TuningConfig.json
- Run balance analysis in Tuning Console

### Problem: Combat feels too fast/slow
**Solutions:**
- Adjust `CombatSpeed` in settings (0.5 = slow, 2.0 = fast)
- Modify `EnableTextDisplayDelays` for pacing
- Tune `NarrativeBalance` (0.0 = action-by-action, 1.0 = full narrative)

## Null Reference Issues

### Problem: NullReferenceException in Dungeon Selection Renderer
**Symptoms:**
- Application crashes with `System.NullReferenceException` in `DungeonSelectionRenderer.RenderDungeonSelection()`
- Stack trace shows error at line 69
- Occurs when attempting to render dungeon selection screen

**Root Causes:**
1. Missing null validation on `dungeons` parameter passed to renderer
2. No check for null dungeon objects within the list
3. `SequenceEqual()` method called on potentially null collections

**Solution:**
1. **Add Parameter Validation**: Validate dungeons list is not null at method entry
2. **Add Element Validation**: Check each dungeon object before accessing properties
3. **Throw Appropriate Exceptions**: Use ArgumentNullException for null parameter, InvalidOperationException for null elements

```csharp
// Validate input - dungeons list must not be null
if (dungeons == null)
{
    throw new ArgumentNullException(nameof(dungeons), "Dungeon list cannot be null");
}

// Inside loop - validate dungeon object is not null
if (dungeon == null)
{
    throw new InvalidOperationException($"Dungeon at index {i} is null");
}
```

**Files Modified:**
- `Code/UI/Avalonia/Renderers/DungeonSelectionRenderer.cs` (lines 68-98)

**Prevention:**
- Always validate parameters at method entry, especially collections
- Add element validation in iteration loops
- Use defensive programming for UI rendering operations
- Test with null and invalid inputs

## Data Generation Issues

### Problem: Armor generation creating massive numbers (30,130,992)
**Symptoms:**
- Armor values in Armor.json showing extremely large numbers
- Tier 2 armor showing 30+ million instead of reasonable values like 4-8
- Data generation amplifying existing corrupted values

**Root Cause:**
- `GenerateArmorFromConfig` method multiplying existing armor values by tier multipliers
- System using corrupted existing values as base instead of generating clean base values

**Solution:**
1. **Replace Amplification Logic**: Use base value generation instead of multiplying existing values
2. **Create Base Value Lookup**: Implement `GetBaseArmorForTierAndSlot` with predefined values
3. **Define Proper Progression**: 
   - Tier 1: Head=2, Chest=4, Feet=2
   - Tier 2: Head=4, Chest=8, Feet=4
   - Tier 3: Head=6, Chest=12, Feet=6
   - etc.
4. **Add Fallback**: Simple tier-based calculation for unknown combinations

**Prevention:**
- Always use base value generation instead of amplifying existing values
- Test data generation with clean/corrupted input files
- Validate generated values are within reasonable ranges

**Files to Check:**
- `Code/GameDataGenerator.cs` (GenerateArmorFromConfig method)
- `GameData/Armor.json` (generated values)

## Data Loading Issues

### Problem: JSON files not loading properly
**Symptoms:**
- Actions not appearing in game
- Default values being used instead of JSON data
- File not found errors

**Solutions:**
1. **Check File Paths**: Verify GameData/ folder structure
2. **Validate JSON Syntax**: Use JSON validator for syntax errors
3. **Check JsonLoader.cs**: Ensure proper error handling
4. **Verify File Permissions**: Ensure read access to GameData files

**Common JSON Issues:**
- Missing commas between objects
- Trailing commas in arrays
- Unescaped quotes in strings
- Invalid number formats

### Problem: Actions not appearing in Action Pool
**Root Causes:**
1. **Equipment Not Providing Actions**: Check weapon/armor action chances
2. **Action Loading Failure**: Verify Actions.json structure
3. **Action Pool Not Updated**: Check InventoryManager action pool updates

**Solutions:**
- Verify weapon has `"actions": ["ACTION_NAME"]` in JSON
- Check armor action chance percentages
- Ensure ActionLoader.cs is loading actions correctly

### Problem: Some enemies can't deal damage (only utility/debuff actions)
**Symptoms:**
- Encounters where enemies never reduce player health
- Examples: `Prism Spider` had only `LIGHT REFRACTION` and `WEB TRAP`

**Solution:**
1. Added a data fix ensuring all enemies include at least one damaging action in `GameData/Enemies.json` (e.g., added `POISON BITE` to `Prism Spider`).
2. Added a loader-time safeguard in `EnemyLoader.CreateEnemyFromData` that injects `BASIC ATTACK` if no damaging actions are present or resolvable.
3. Added `EnemyTests.TestEnemiesHaveDamagingAction()` and a CLI hook `--test-enemies` to validate configurations.

**Verification:**
- Run `Code.exe --test-enemies` to see validation results.
- In combat, previously non-damaging enemies now attack and can win fights.

## Character Progression Issues

### Problem: Character stats not scaling properly
**Symptoms:**
- Stats not increasing on level up
- Health not restoring on level up
- Class points not being awarded

**Solutions:**
1. **Check CharacterProgression.cs**: Verify level up logic
2. **Validate Stat Formulas**: Check TuningConfig.json scaling
3. **Verify XP System**: Ensure XP is being awarded and calculated correctly

### Problem: Equipment not providing expected bonuses
**Solutions:**
- Check item tier and rarity in inventory display
- Verify stat bonus calculations in CharacterEquipment.cs
- Ensure proper equipment scaling in ScalingManager.cs

## UI/Display Issues

### Problem: Damage display showing wrong values
**Symptoms:**
- Showing raw damage instead of actual damage
- Armor calculations not visible
- Inconsistent damage formatting

**Solutions:**
- Use `FormatDamageDisplay()` method in Combat.cs
- Ensure `CalculateDamage()` is called for actual damage
- Check armor reduction calculations

### Problem: Inventory display formatting issues
**Solutions:**
- Check InventoryDisplayManager.cs formatting methods
- Verify item stat calculations
- Ensure proper indentation and spacing

## Testing Issues

### Problem: Tests failing unexpectedly
**Solutions:**
1. **Check Test Data**: Ensure test data matches current game balance
2. **Update Expected Values**: Balance changes may require test updates
3. **Verify Test Environment**: Ensure tests run in clean state

### Problem: Balance tests showing incorrect DPS
**Solutions:**
- Update DPS calculations in EnemyBalanceCalculator.cs
- Verify scaling formulas in TuningConfig.json
- Check enemy stat scaling in EnemyLoader.cs

## Performance Issues

### Problem: Game running slowly
**Solutions:**
1. **Disable Delays**: Set `EnableTextDisplayDelays = false`
2. **Reduce Narrative**: Set `NarrativeBalance = 1.0` for full narrative mode
3. **Optimize Calculations**: Check for expensive operations in combat loops

### Problem: Memory usage issues
**Solutions:**
- Check for object creation in tight loops
- Verify proper disposal of resources
- Monitor JSON loading and caching

## Configuration Issues

### Problem: Tuning changes not taking effect
**Solutions:**
1. **Reload Configuration**: Use "Reload Config" in Tuning Console
2. **Check JSON Syntax**: Validate TuningConfig.json format
3. **Restart Application**: Some changes require full restart

### Problem: Formula evaluation errors
**Solutions:**
- Check variable names match exactly (case-sensitive)
- Ensure parentheses are balanced
- Verify mathematical operators are supported
- Use FormulaEvaluator test function

## Common Error Patterns

### "Something went wrong" in combat
**Cause**: Action execution failure
**Solution**: Check action definitions in Actions.json, verify action properties

### "File not found" errors
**Cause**: Missing or moved GameData files
**Solution**: Verify file paths in GameConstants.cs, check file existence

### Null reference exceptions
**Cause**: Uninitialized objects or missing data
**Solution**: Add null checks, verify object initialization order

## Quick Fixes

### Reset to Known Good State
1. Restore TuningConfig.json from backup
2. Reload configuration in Tuning Console
3. Run balance analysis to verify

### Clear Cache Issues
1. Delete character_save.json to reset character
2. Restart application
3. Create new character to test

### Verify System Integrity
1. Run all tests in Settings → Tests
2. Check balance analysis in Tuning Console
3. Verify JSON file syntax

## Prevention Strategies

1. **Always Test Changes**: Run relevant tests after modifications
2. **Backup Configurations**: Export tuning configs before major changes
3. **Incremental Changes**: Make small changes and test frequently
4. **Document Changes**: Note what was changed and why
5. **Use Version Control**: Commit working states before major changes

## Related Documentation

- **`DEBUGGING_GUIDE.md`**: Systematic debugging approaches and tools
- **`QUICK_REFERENCE.md`**: Fast lookup for key information and commands
- **`KNOWN_ISSUES.md`**: Current status of known problems
- **`TESTING_STRATEGY.md`**: Testing approaches for verification
- **`DEVELOPMENT_WORKFLOW.md`**: Step-by-step development process

---

*This document should be updated when new problems are encountered and solved.*
