# Problem Solutions - DungeonFighter

This document contains solutions to common problems encountered during development. Use this as a quick reference when similar issues arise.

## Recent Fixes

### Combat strip: Iron Cull damage stayed flat while CRISIS climbed (October 2026)
**Problem:** With CRISIS x5 on the status panel, Iron Cull's card still showed the amp-only swing (18) and no live convert line.

**Root cause:** Convert payout reads the action's scale columns. A pool copy whose columns name the wrong material or an empty keyword bank returned 0, so the strip added nothing even though the Iron set's CRISIS bank was stocked. The card line also omitted the stack count, so a rising bank was invisible.

**Solution:** `GetConvertDamageBonus` falls back to the material build's own material and keyword when the set is formed and that bank is stocked. The strip already adds that bonus to the flat damage number (percent of the swing when percent converts are on). The card line includes the live count (`CRISIS x5 +N`), and the swing number turns green while the convert is paying.

**Related files:** `MaterialSetController.cs`, `CombatActionStripBuilder.cs`, `DungeonRenderer.RoomAndCombat.cs`, `MaterialSetControllerTests.cs`, `ActionCardExternalBonusCollectorTests.cs`

### UI: F5 narrative video bands drift against text line spacing (October 2026)
**Problem:** With the F5 combat-log video feed on, the backdrop’s horizontal bands (brick courses / scanlines)
started aligned with text near the top of the log but drifted out of phase further down.

**Root cause:** `NarrativeVideoOverlayControl` stretch-filled the native MP4 frame across the whole band.
Periodic features in the footage have a vertical period that is not an integer multiple of character-row
height, so phase accumulates line by line (classic moiré against the monospace grid).

**Solution:** Resample each decode frame onto the character-cell grid (`NarrativeVideoCellLock`): one
center sample per cell, filled as a solid block, drawn with `BitmapInterpolationMode.None`. Each text
row maps to one discrete video strip locked to line spacing.

**Related files:** `NarrativeVideoCellLock.cs`, `NarrativeVideoOverlayControl.cs`, `NarrativeVideoCellMaskTests.cs`

### UI: DoT "no longer burning" line lost its indent (October 2026)
**Problem:** Combat log showed `Basil takes 1 burn damage` and `(Basil is no longer burning!)` flush-left on both lines;
the status-end line should sit under the damage line with the standard 5-space action-block indent.

**Root cause:** `StatusEffectProcessor` joins damage + follow-up with an explicit `\n` in one buffer entry.
`TextWrappingHelper.WrapColoredSegments` treats soft-wrap continuations as book-style (no leading spaces) and also
collapsed post-hard-break spaces into a single inter-word flag, so the follow-up indent never reached the second line.

**Solution:** After an explicit hard break, leading spaces become a `LineIndent` token and seed that logical line.
Soft-wrap width continuations still stay flush left.

**Related files:** `TextWrappingHelper.cs`, `TextWrappingHelperTests.cs`

### Combat strip: repeated actions and strip replacements looped back to the first copy (October 2026)
**Problem:** Strips with a repeated action (STAB > MISDIRECT > EXPOSE > STAB > STAB) never reached their last slots, and
MISDIRECT (replace next swing with STAB) made Marked Hunter ping-pong STAB / MISDIRECT so EXPOSE and its converts never fired.

**Root cause:** `Character.IncrementComboStep` found the slot that just ran with `FindIndex` by name, i.e. the first copy.
A `strip_replace_next` swing resolves the replacement's own strip slot, so the strip resumed from there.

**Solution:** Advance from the current step's slot when it holds that action; a strip-replaced swing records the slot it
replaced (`Character.ReplacedStripSlot`, set in `ActionExecutionFlow.Selection`) and the strip advances from it.

**Related files:** `Character.cs`, `ActionExecutionFlow.Selection.cs`, `ActionExecutionFlow.cs`, `BuildGlueTests.cs`

### Skills and audio cues went dead after starting a new game (October 2026)
**Problem:** Skill-tree effects and combat audio cues worked in the first game of a session but not after New Game.

**Root cause:** `CombatEventBus.Clear()` runs on new game; `SkillEffectRouter` and `AudioCueDispatcher` subscribe once and
kept a "subscribed" flag, so they never re-subscribed.

**Solution:** The bus raises `Cleared`; the audio dispatcher re-subscribes on it, and the skill router checks
`HasSubscriber` before trusting its flag.

**Related files:** `CombatEventBus.cs`, `SkillEffectRouter.cs`, `AudioCueDispatcher.cs`

### UI: item names lowercased affix words; travel roll line glued to its title; card labels lost trigger names (October 2026)
**Problem:** "Worn Thick Wrap" showed as "Worn thick Wrap"; the travel step's indented roll line joined the title line;
WHILE_EQUIPPED card lines read "Bludgeon +12%" instead of the trigger's name.

**Root cause:** `ItemThemeProvider.GetBaseLeadingWordTheme` passed the lowercased lookup key as display text;
`SpacingHelper` collapsed every whitespace-only segment (including "
     ") to one space;
`EquippedItemTriggerApplicator.CloneBundle` dropped `IdentityName`.

**Solution:** Theme the original word; keep whitespace segments that contain a newline; copy `IdentityName` /
`Description` in the clone.

**Related files:** `ItemThemeProvider.cs`, `SpacingHelper.cs`, `EquippedItemTriggerApplicator.cs`

### Data: tutorial granted a missing action; "MAGIC MISSLE" shown to players (October 2026)
**Problem:** The Training Ground unarmed action PUNCH HARD did not exist in the action data, and the wand basic was
spelled MAGIC MISSLE in menus and the combat log.

**Root cause:** The sheet renamed PUNCH HARD to PUNCH; the wand basic kept the sheet's typo as its data key.

**Solution:** The tutorial, `Triggers.json` (`grant_action:PUNCH`) and the class-actions mapper use PUNCH;
`ActionLoader.CanonicalizeLegacyKeys` re-keys MAGIC MISSLE as MAGIC MISSILE after loading (old saves still resolve).

**Related files:** `GameConstants.cs`, `ClassActionsUnlockConfig.cs`, `ActionLoader.cs`, `GameData/Triggers.json`

### Tests: suites depended on run order and the active balance patch (October 2026)
**Problem:** About 60 tests failed and five suites crashed in the full run but many passed alone.

**Root cause:** Tests mutated singletons without restoring them (`SimulationPacing.EnableFastMode` left MCP mode,
combat UI mute and fast mode on for every later suite), hard-coded values that are now patch data (base HP, material
converts, combo strip length 1, rarity colors), or counted data that grew (tags, mechanics, trigger identities).

**Solution:** Pin or restore global config inside each suite (`MaterialSetControllerTests`, `EnemyAttributeGrowthTests`,
`ActionCardExternalBonusCollectorTests`, `DungeonExitChoiceHandlerTests`, `DeveloperModeStateTests`); compare against
the live patch or palette instead of literals; lab tests add strip slots through the recorded panel delta
(`LabTestSlots.Give`). Full suite: 13163 run, 0 failed.

### Skills: Fortify from skills gave no defense; temp stats stacked all fight (October 2026)
**Problem:** Crest of Triumph, Second Wind, Mobile Bulwark and Field Brief priced at 100 in their own builds; Gut Instinct,
Focus Lens, Thermal Shock and Cadence Keeper priced ×1.7–2.3.

**Root cause:** (1) Skills raise `FortifyStacks` directly, but defense only reads `FortifyArmorBonus`, which only the
status handler sets (and only +1 armor per stack against K 275). (2) Gut Instinct / Focus Lens added temp STR / TEC on
every hit and `TempStatBonusTurns` was never set, so the bonus never decayed; Grease Palm / Loaded Odds likewise.
(3) Thermal Shock stunned whenever Burn and Slow were up and never consumed them. (4) Cadence Keeper granted at least
1 Focus per alternation whatever its power.

**Solution:** `combat.statusFortifyDefensePerStack` (Fortify stacks add defense in K units, like Harden); Gut Instinct /
Focus Lens set their bonus once (the Steady March pattern); Grease Palm / Loaded Odds last 2 turns; Thermal Shock
consumes Burn and Slow; Cadence Keeper uses rank × power with the fraction as a chance.

**Related files:** `StatusCombatModifiers.cs`, `SkillEffectRouter.cs`, `CombatConfig.cs`

### Combat: burn / bleed did 1 damage per stack at every level (October 2026)
**Problem:** Enemy Rend made the hero bleed for 1 damage at L40; Deep Cut and the bleed / burn procs faded to nothing
past L30.

**Root cause:** Burn and bleed intensity are flat integers and tick for their intensity.

**Solution:** `combat.dotDamagePerLevel` (overnight-v2: 0.1): tick damage × (1 + target level × k). Trigger bundles now
pass their value to burn / bleed stacks as well as poison.

**Related files:** `Actor.cs`, `ActionTriggerBundleApplicator.cs`, `CombatConfig.cs`

### Combat: speed bonuses never changed swing time (October 2026)
**Problem:** SPRINT / RUSH / MOMENTUM / MARATHON, First Formation, Metronome, War Drum and the "+10% speed" item triggers did nothing: BUILDLAB priced every Warrior speed node at exactly 100.

**Root cause:** SPEED_MOD lands in `ConsumedSpeedModPercent`, which `ActionExecutionFlow` clears when the swing resolves. The swing's duration is computed afterwards (`ActionSpeedSystem.ExecuteAction` in the turn handler), so it always read 0.

**Solution:** The flow keeps the swing's SPEED_MOD in `LastSwingSpeedModPercent` before clearing; `ActionSpeedSystem` takes it (once) when it sets the duration.

**Related files:** `ActionExecutionFlow.cs`, `ActionSpeedSystem.cs`, `CharacterEffects.cs`, `CharacterEffectsState.cs`

### Materials: a foe's hits minted the hero's ON CONNECT currencies (October 2026)
**Problem:** Crystal CONTACT, Glass GRAZE, Shadow SWING and Mithril STREAK banks grew when the *enemy* connected (Goblin JAB / TAUNT minted CONTACT).

**Root cause:** The take-hit mint call (`ActionExecutionFlow.Outcomes`) ran every material build's WHEN against the foe's hit event, so ON CONNECT / ON HIT / ON ATTACK matched it too.

**Solution:** `TryMintFromEvent(..., takeHitOnly: true)` on that path: only take-hit WHENs (Bone RAGE) mint from a foe's hit, and take-hit WHENs never mint from the hero's own swings.

**Related files:** `MaterialSetController.cs`, `ActionExecutionFlow.Outcomes.cs`

### Skills: "on dealing damage" skills never fired (October 2026)
**Problem:** Blood Price, Mana Bleed and Toxic Ledger did nothing even at 10× power.

**Root cause:** The hit event is published before damage is rolled, so `evt.Damage` is always 0 in the router's on-hit handlers. Blood Price / Mana Bleed also healed a flat 1 HP per rank, which stops mattering past the first levels.

**Solution:** Blood Price / Mana Bleed heal 0.5% of max HP per rank (× power) on a connect; Toxic Ledger adds its bonus as a damage mod on the finisher hit itself.

**Related files:** `SkillEffectRouter.cs`, `GameData/SkillTrees.json`, `skill-tree-templates.json`

### Items: Flaming / Poisonous / serrated mods did nothing (October 2026)
**Problem:** BUILD_CATALOG gap G6: the DoT mods never applied, so status-reaction skills (Kindle, Sigil Burn, Toxin Pressure, Envenomed Edge, Catalyst) had no source.

**Root cause:** `Modifications.json` names the effects BURN / POISON / BLEED; the weapon DoT path only read weaponBurn / weaponPoison / weaponBleed. Poisonous also rolls 0.

**Solution:** `CombatEffectsSimplified.NormalizeWeaponDotEffect` maps the short names; Poisonous defaults to 0.5% poison per application (poison is % of max HP per tick and never decays in a fight).

**Related files:** `CombatEffectsSimplified.cs`

### Items: trigger poison ignored its value (October 2026)
**Problem:** Every poison trigger added 1% of max HP per tick whatever its value.

**Root cause:** `ActionTriggerBundleApplicator.ApplyStatus` never received the bundle magnitude; `PoisonEffectHandler` defaults to 1%.

**Solution:** `ApplyStatus` takes the magnitude and sets `PoisonPercentToAdd` for poison. Material poison triggers are 0.25 (on connect) / 0.5 (on crit, take hit).

**Related files:** `ActionTriggerBundleApplicator.cs`, `MaterialTriggerCatalog.cs`

### Balance: one-hit damage cap flattened late-game scaling (October 2026)
**Problem:** Builds were worth ×1.3–2.4 at L30 but ×1.0–1.7 at L60, and stacked bonuses (convert banks, crits, AMP) stopped mattering.

**Root cause:** `combat.maximumDamageCap` 950 clipped every hit; by L30–60 burst hits sit on it.

**Solution:** overnight-v2 sets the cap to 100000 and re-solves the enemy health / damage curves. (overnight-v1 keeps 950.)

**Related files:** `GameData/Patches/Balance/overnight-v2.json`

### Balance / Skills: optimized heroes killed in 2–4 actions (October 2026)
**Problem:** A hero that spent skill points (smart bot) killed in 2–4 hero actions from L6 (Barbarian, Wizard) against an ~11-action target; the whole Barbarian tree was worth 5.5× at L10.

**Root cause:** (1) Skill magnitudes were written for the old stat scale: +25 STR per empty slot per rank, +8–10% per matching item per rank, hero level as STR, while +1 STR is now +2.5% damage. (2) Any skill AMP_MOD lifted an opener's combo multiplier from 1.0 to the full TEC combo amp before applying the %, so a +0.5% AMP passive was worth ~+48%. (3) The basic bot never learns skills, so balance checks never saw them.

**Solution:** Per-node `power` in `SkillTrees.json` (multiplies the per-rank amount; effect text updated); skill AMP goes to `ConsumedSkillAmpPercent`, a plain multiplier with no lift; new `SKILLVALUE` tool prices every node and the whole tree.

**Related files:** `SkillEffectRouter.cs`, `SkillEffectRouter.Preview.cs`, `SkillTreesConfig.cs`, `GameData/SkillTrees.json`, `DamageCalculator.cs`, `CharacterEffectsState.cs`, `SkillValueRunner.cs`, `BALANCE_PROCESS.md`

### UI / Inventory: trade-up preview Confirm clipped off-screen (October 2026)
**Problem:** Opening Trade Up Preview with five gear pieces showed full stat blocks for every input, so Confirm Trade Up was pushed below the center panel (no scroll, cancel disabled).

**Root cause:** `ItemSelectionRenderer.RenderTradeUpPreview` painted unlimited lines and ignored `height`; it never pinned a footer the way inventory / equip-comparison screens do.

**Solution:** Compact traded inputs to name-only rows, clear the content area, pin Confirm on the last row via `TradeUpPreviewLayout`, and clamp result stats above the footer. Also corrected StatBonuses suffix typo `of ferosity` → `of ferocity`.

**Related files:** `ItemSelectionRenderer.cs`, `TradeUpPreviewLayout`, `GameData/StatBonuses.json`, `InventoryRendererTests.cs`

### Balance / Combat: some enemies unwinnable at mid levels, all identical at L1 (October 2026)
**Problem:** Across the full roster, the worst enemy won 7% of fights at L35 (Mud Lurker) while others never threatened; at L1 every enemy swung at the same speed with the same damage.

**Root cause:** Enemy power came from raw stat totals: damage = floor((STR + primary) × scale), swing time from AGI on the shared curve. Growth rows differ per enemy, so threat diverged with level (AGI-heavy enemies swung 3.3× faster by L50), while L1 base stats all sit below the agility floor.

**Solution:** `enemySystem.identity` mode: level references set power; the growth row sets a threat-neutral shape (speed vs hit size vs HP) that strengthens with level; `Scripts/enemy-calibrate.py` solves per-enemy damage anchors for what actions add. Then WRSOLVE for the damage curve.

**Related files:** `EnemyIdentity.cs`, `EnemyConfig.cs`, `EnemyStatCalculator.cs`, `EnemyFactory.cs`, `DamageCalculator.cs`, `SpeedCalculator.cs`, `Scripts/enemy-calibrate.py`, `GameData/Patches/Balance/overnight-v1.json`, `EnemyIdentityTests.cs`, `BALANCE_PROCESS.md`

### Balance / Combat: two archetypes did all the killing (October 2026)
**Problem:** In the balance matrix, Wolf and Bear (Berserker) caused nearly every hero loss; Goblin, Rats, Wraith (Assassin) and Lich (Sage) won 100% through L50.

**Root cause:** (1) `ArchetypeManager` profiles stored `speed: 1.2` for "fast" archetypes, but `SpeedCalculator` multiplies **attack time** by it, so Assassin/Acrobat/Trickster/Duelist swung 10–20% slower. (2) Archetype `DamageMultiplier` (Berserker 1.2, Sage 1.1, …) was never applied anywhere in damage. (3) The matrix only tested 3 of 9 archetypes, so the gap was invisible in the averages.

**Solution:** Profiles store attack time (`1/1.2` etc.); `DamageCalculator` applies the archetype damage multiplier; patch overrides via `enemySystem.archetypeCombat`. Solved per-archetype damage so each costs the same HP per fight, re-solved `damageCurve`, expanded `matrixEnemies` to all archetypes.

**Related files:** `ArchetypeManager.cs`, `EnemyConfig.cs`, `SpeedCalculator.cs`, `DamageCalculator.cs`, `GameData/Patches/Balance/overnight-v1.json`, `GameData/TuningProfiles/balance-targets.json`, `ArchetypeCombatTests.cs`, `BALANCE_PROCESS.md`

### Balance / Tuning: attribute damage scale + Starting/Scaling/Outfit IA (October 2026)
**Problem:** Attribute damage was 1:1 with STR+Primary, so base stats of 3 made every early +1 feel huge; Combat Tuning knobs were scattered across many tabs (Core, Hero, Speed, Equipment, Enemy, Progression, Archetypes, Status, Rewards, Goals).

**Solution:**
1. `attrDmg = floor((STR + Primary) × Combat.AttributeDamageScale)` (default **0.05** → **1** at base 10+10); shipped player bases **10/10/10/10**
2. Combat Tuning UI replaced with **Starting | Scaling | Outfit** (+ Simulation tool); registry retagged; unimplemented difficulty presets removed from the panel
3. Damage HUD/tooltips show the scaled equation

**Related files:** `CombatConfig.cs`, `CharacterFacade.cs`, `CharacterConfig.cs`, `CombatTuningParameterRegistry*.cs`, `CombatTuningSettingsPanel.*`, `CombatTuningPanelViewModel.cs`, `GameData/Patches/Balance/7-30-26.json`, `default.json`

### Perf / UI: text distortion frame-rate drop (October 2026)
**Problem:** With F6 mouse-wind distortion on, moving the pointer over dense combat-log / panel text caused noticeable frame-rate dips.

**Root cause:** Live config had drifted to expensive trail/settle knobs (28 trail points, 16ms full-canvas refresh). Per-glyph sampling re-resolved trail facing and used `Sqrt` rejects; long lines that only clipped the wake still sampled every glyph; `char.ToString()` allocated on every distorted glyph draw.

**Solution:** Precompute trail facing in `BeginFrame`; elliptical reject via `t²`; per-glyph wake AABB early-out; intern ASCII glyph strings; hard-cap trail at 14; floor settle/pointer refresh at 20ms; restore lean trail/settle defaults in `UIConfiguration.json`.

**Related files:** `WindSwayField.cs`, `CanvasPrimitivesRenderer.cs`, `GameCanvasControl.cs`, `UIConfiguration.json`, `WindSwayFieldTests.cs`, `CanvasPrimitiveStackingTests.cs`, `PERFORMANCE_NOTES.md`, `OVERVIEW.md`, `ARCHITECTURE.md`, `TASKLIST.md`

### Bug fix / UI: dungeon select hover changed letter spacing (October 2026)
**Problem:** On the AVAILABLE DUNGEONS menu, moving the pointer onto a row made letter spacing jump wider even though F6 distortion was not being “used” intentionally.

**Root cause:** (1) Idle rows used per-glyph undulation; hover used a single yellow string (different advance under center-column zoom). (2) After recoloring the row yellow, `AddTextWithMerging` collapsed adjacent length-1 cells into one run — paint then used `Round(charWidth × zoom)` while idle neighbors stayed on `charWidth`.

**Solution:** Keep one-glyph-per-cell segments and only recolor on hover (`DungeonSelectionRenderer`). Do not adjacent-merge length-1 adds (`CanvasElementManager.ShouldKeepAsOwnCell`). Header wake path uses `DrawTextRun` when offset is zero so FormattedText cannot tighten kerning on mouse enter.

**Related files:** `DungeonSelectionRenderer.cs`, `CanvasElementManager.cs`, `CanvasPrimitivesRenderer.cs`, `DungeonSelectionHoverLayoutTests.cs`, `CanvasPrimitiveStackingTests.cs`, `OVERVIEW.md`, `ARCHITECTURE.md`, `TASKLIST.md`

### Bug fix / UI: DistortionPulse left a hole before SLAM (October 2026)
**Problem:** On the F7 Action-beat DistortionPulse (e.g. `A decisive  SLAM`), there looked like an extra space before the green action word. Earlier outcome prose (`a chaining SLAM`) stayed correctly spaced.

**Root cause:** Prose only has one separator space. The pulse exclude AABB covered only the letters of SLAM, so the flanking space glyph was pushed outward and vacated its cell while SLAM stayed rigid — reading as a double space.

**Solution:** `ActionTextEmphasisWordLocator` pads the rigid exclude rect by one cell on each side (origin stays on the word center) so separator spaces do not flee. Tests: `ActionTextEmphasisWordLocatorTests`.

**Related files:** `ActionTextEmphasisWordLocator.cs`, `ActionTextEmphasisWordLocatorTests.cs`, `ARCHITECTURE.md`, `OVERVIEW.md`, `TASKLIST.md`

### UI: new menu jitters under mouse (screen-open damper) (October 2026)
**Problem:** When a new menu/screen opened with the pointer already over text, fresh glyphs jittered under F6 wind distortion.

**Root cause:** Residual wake energy from the previous screen (or a layout rebuild that rebased mouse deltas) applied full amp on the first paint of the new text.

**Solution:** `ScreenTransitionProtocol` calls `CanvasUICoordinator.NotifyScreenOpened` → `WindSwayField.BeginOpenDamper` at the start of each menu transition. That clears residual wind/trail, reseeds the mouse baseline (next push establishes position only), and eases amp 0→1 over ~1.5s via smoothstep. Impulse and sample strength both honor the damper.

**Related files:** `WindSwayField.cs`, `GameCanvasControl.cs`, `CanvasUICoordinator.cs`, `ScreenTransitionProtocol.cs`, `WindSwayFieldTests.cs`

### UI: micro mouse moves jitter glyphs under distortion (October 2026)
**Problem:** With F6 mouse-wind distortion on, tiny pointer movements that should leave text alone were still shoving pixel-font characters around one device pixel at a time.

**Root cause:** (1) Sub-deadzone pointer samples still refreshed the push clock, so a barely-over-deadzone nudge used ~16ms dt and looked like a full-speed flick. (2) The whole travel past deadzone became impulse (no excess-only). (3) Rest-snap at 0.5px still allowed sub-pixel offsets that integer-snapped to a 1px jump.

**Solution:** Raise motion deadzone to 6px; apply impulse only for travel past the deadzone; advance push-dt only on meaningful motion (cap resume-from-idle dt); ramp wake speed over the next 10px of excess so just-over nudges cannot full-amp; rest-snap sampled offsets below 1px.

**Related files:** `WindSwayField.cs`, `WindSwayFieldTests.cs`, `OVERVIEW.md`, `ARCHITECTURE.md`, `TASKLIST.md`

### UI: VT323 kerning tightens then jumps wider near the mouse (October 2026)
**Problem:** On VT323 (and other pixel fonts), menu/body text looked tightly spaced until the mouse wake was near, then letter spacing suddenly widened — distinct from intentional sway/distort motion.

**Root cause:** Idle paint drew whole strings with `FormattedText` (face-native advances, often a fraction tighter than the snapped cell width). The wake path places glyphs at `i × Round(charWidth)`. Entering the wake AABB switched layouts, so spacing jumped even when offsets were near rest.

**Solution:** Pixel-grid fonts (`GameFonts.UsesPixelGrid`) always paint body text glyph-by-glyph on the snapped monospace grid (`DrawTextRun` / `MonospaceGlyphOriginX`), idle and wake alike. Courier New keeps the whole-string path. Headers that move as one unit are unchanged.

**Related files:** `CanvasPrimitivesRenderer.cs`, `CanvasPrimitiveStackingTests.cs`, `OVERVIEW.md`, `ARCHITECTURE.md`, `TASKLIST.md`

### UI: VT323 letters jump / look garbled near the mouse (October 2026)
**Problem:** On the default VT323 face (and other pixel fonts), menu/options text sometimes looked like letters were jumping or overlapping with pink/cyan ghosts — especially when the mouse was near dungeon-selection options.

**Root cause:** Mouse wind sway (F6, default on) applies subpixel glyph offsets + chromatic fringe. Draw origins already `Math.Round` to whole pixels, so offsets near half-pixel boundaries flip letters by a full device pixel each frame. Fractional measured `charWidth` compounded uneven advances when the paint path switched to per-glyph draws.

**Solution:** Snap measured cell width/height to whole pixels; raise wind `PixelRestEpsilon` to 1px; for pixel-grid fonts (`GameFonts.UsesPixelGrid`) snap sway/burst/CA offsets (and fringe deltas) to integers before paint; round center-column glyph advance to a whole pixel at the drawn EM size. Mouse wake still moves letters intentionally — press **F6** to disable distortion entirely.

**Related files:** `CanvasCoordinateConverter.cs`, `CanvasPrimitivesRenderer.cs`, `WindSwayField.cs`, `GameFonts.cs`, `GameFontsTests.cs`, `CanvasPrimitiveStackingTests.cs`, `WindSwayFieldTests.cs`

### UI: click-burst rebuild returned letters in random order (October 2026)
**Problem:** After a click-charge explosion, hole-click / autofill cleanup restored glyphs in a scrambled order instead of starting from the beginning of the text.

**Root cause:** `SampleExploding` chose which letter came home from a per-glyph hash `roll` in `[0,1]`, so rebuild steps hit letters randomly.

**Solution:** Paint registers affected glyph keys per zone; the first rebuild step freezes that list sorted by reading order (row, then column). Each rebuild step homes the next batch from the start of that list. Tests: `TextClickBurstFieldTests.TestRebuildReturnsLettersInReadingOrder`.

**Related files:** `TextClickBurstField.cs`, `TextClickBurstConfig.cs`, `TextClickBurstFieldTests.cs`

### AI: Victory verse showed at room-clear instead of dungeon end (October 2026)
**Problem:** With Victory Limerick enabled, the "Victory verse" appeared under the post-fight defeat line while the stay/leave dungeon prompt was still up, so every cleared room felt like a full run ending.

**Root cause:** `EnemyEncounterHandler` delivered the prefetched limerick immediately after each won fight.

**Solution:** Prefetch only on the **final content room** (`SetFinaleRoomPrefetchAllowed` from `RoomProcessor` → `ProcessEnemyEncounter`). Delivery moved to `GameScreenCoordinator.ShowDungeonCompletion` via `AppendVictoryVerseToCompletionBuffer`. Mid-run fights and early exits clear/ignore prefetch.

**Related files:** `VictoryLimerickService.cs`, `CombatTurnHandlerSimplified.cs`, `EnemyEncounterHandler.cs`, `RoomProcessor.cs`, `DungeonOrchestrator.cs`, `GameScreenCoordinator.cs`, `CanvasUICoordinator.DisplayBuffer.cs`, `VictoryLimerickServiceTests.cs`

### Combat: Naiveté removed — Leather Lucky is the only starter luck (October 2026)
**Problem:** Early-game **Naiveté** miss→advantage charges overlapped the Leather set **Lucky** standing advantage, so new heroes had two luck systems.

**Root cause:** Naiveté was the prior early-game cushion; Leather Lucky later became the intended starter set bonus.

**Solution:** Removed Naiveté config, fight charges, miss→advantage resolution, and the left-panel **NAIV** row. Leather **Lucky** remains WHILE_EQUIPPED 2d20 keep highest; STATUS EFFECTS shows plain **Lucky**.

**Related files:** `LeatherSetBonus.cs`, `ActionExecutionFlow.Selection.cs`, `CombatTriggerContext.cs`, `EarlyGameConfig.cs`, `CharacterPanelRenderer.cs`, `StatusEffectDisplayLines.cs`, `LeatherSetBonusTests.cs`, `StatusEffectDisplayLinesTests.cs`

### AI: OpenAI insufficient_quota dumps into combat log (October 2026)
**Problem:** With Victory Limerick enabled, an empty OpenAI billing quota produced a wall of `Error in OpenAiChatClient.CompleteAsync: OpenAI HTTP 429` JSON in the player-facing combat/system log before (or alongside) the FlavorText "Victory verse".

**Root cause:** `OpenAiChatClient.CompleteAsync` called `ErrorHandler.LogError`, which always writes via `UIManager.WriteSystemLine`. Quota/auth failures are expected optional-feature conditions, not gameplay errors.

**Solution:** HTTP and orchestration failures on the limerick path log with `ScrollDebugLogger.LogAlways` only; `CompleteAsync` still returns null so FlavorText fallback delivers the verse quietly. `SummarizeApiError` keeps debug lines short (`type: message`).

**Related files:** `OpenAiChatClient.cs`, `VictoryLimerickService.cs`, `OpenAiChatClientTests.cs`

### Combat: scene JSON wrong enemy action / gear + ComfyUI corner framing (October 2026)
**Problem:** Right-click combat-log export described Rats as `lunging mace slam` with `weapon_main: Bludgeon`. Establishing framing also read as a medium action shot instead of a room-dominant corner fight.

**Root cause:** `ResolveAction` fell back to `lastPlayerAction` when the enemy action failed to resolve from the pool/catalog, so hero SLAM leaked onto enemy beats. `FormatActionPhrase` / `BuildGear` also treated the enemy’s random combat-math weapon (tier-1 Bludgeon) as visible kit.

**Solution:** Only reuse `lastPlayerAction` for the hero actor; omit enemy gear from packets; never add weapon-type flavor for enemies. Camera strings + new `composition` field lock an ultra-wide empty establishing shot, then the same wide room with fighters small in the far corner (ComfyUI: generate row 0, img2img / regional later rows).

**Related files:** `CombatScenePacket.cs`, `CombatScenePacketBuilder.cs`, `CombatScenePacketBuilderTests.cs`

### UI: right panel flickers after dungeon select (October 2026)
**Problem:** After picking a dungeon, the right LOCATION / ENEMY panel visibly cleared and redrew several times before settling.

**Root cause:** `DungeonOrchestrator` still called full `RenderRoomEntry` on every auto-walk step (and waited `DefaultStepDelayMs`). That path existed for the removed bottom-right minimap. With no map to update, each step only cleared/redrew side panels via `RightPanelRenderer.RenderRightPanel`.

**Solution:** Walk with `onStep: null`. `RoomProcessor.ProcessRoom` still paints once on arrival. Documented on `DungeonTraversalNavigator` that callers must not full-repaint the HUD per step.

**Related files:** `DungeonOrchestrator.cs`, `DungeonTraversalNavigator.cs`, `DungeonTraversalStrategyTests.cs`

### UI: dungeon minimap removed (October 2026)
**Problem:** The bottom-right dungeon traversal minimap was no longer wanted in the player-facing UI.

**Solution:** Removed `DungeonMinimapRenderer` and all paint call sites (`CanvasRenderer`, `RenderCoordinator`). Dropped `IsMinimapFrozen` from traversal state. Automated dungeon traversal (grid pathing, Smart/Random walk, approach outcomes) is unchanged. Overlay-box clear semantics remain for hover tips. Do not reintroduce per-step `RenderRoomEntry` for walk pacing — that caused right-panel flicker after the overlay was gone.

**Related files:** `CanvasRenderer.cs`, `RenderCoordinator.cs`, `DungeonOrchestrator.cs`, `RoomProcessor.cs`, `DungeonTraversalState.cs`

### UI: weapon selection decorative rules overrun the gold box (October 2026)
**Problem:** On **Choose your starting weapon**, the gold title rules and grey footer separator looked broken and extended past the right edge of the gold frame.

**Root cause:** Rules were built from Unicode `─` and centered by character count. Center-column Ctrl+/- paint scales glyphs (`CenterColumnTextZoom`) while the frame `AddBox` stays in cell space, so long runs grew past the right border. Pixel fonts that fall back for box-drawing glyphs can also advance wider than one cell when drawn as one `FormattedText` block.

**Solution:** `MenuLayoutCalculator.BuildDecorativeRule` uses ASCII `-` and clamps length with `ScaleWrapWidth`. Title/options/rules/instructions center via `CalculateCenteredZoomedTextX` / `VisualCenterColumnColumns`. Weapon selection vertical packing uses `LineStride` so rules clear the box stroke. Tests in `WeaponSelectionRendererTests`.

**Related files:** `WeaponSelectionRenderer.cs`, `MenuLayoutCalculator.cs`, `WeaponSelectionRendererTests.cs`

### UI: Ctrl+/- zoom drifts main menu to the right (October 2026)
**Problem:** Raising UI size (e.g. Bytesized at 150%) left main-menu options shifted toward the right; side panels also looked like they grew with the font.

**Root cause:** Menu states suppress `ForceFullLayoutRender` / `PerformRender`. Ctrl+/- and grid-width changes only called that path, so character-grid text stayed at pre-zoom column coordinates while fewer columns fit and glyphs grew — chrome boxes kept old widths in cell space (appearing larger in pixels) and centered menus sat too far right on the narrower grid.

**Solution:** `MainWindow` zoom/font/resize rebuild calls `Game.RefreshPersistentChromeAfterStatsToggle`, which re-paints menu screens in place. `MainMenuHandler.RefreshMainMenuInPlace` clears and re-renders without a state transition. Chromeless centering math is covered by `EffectiveVisibleWidthRegressionTests.TestChromelessMenuStaysCenteredWhenColumnsChange`.

**Related files:** `MainWindow.axaml.cs`, `Game.cs`, `MainMenuHandler.cs`, `MenuLayoutCalculator.cs`, `EffectiveVisibleWidthRegressionTests.cs`

### UI: Ctrl+/- zoom grows side panels and crushes the center (October 2026)
**Problem:** Raising UI size (e.g. Courier New at 112%) made the left/right chrome dominate the window and squeezed the center narrative panel; top action cards also grew with zoom. Inverse chrome-column shrink still left side-panel **glyphs** larger than 100%.

**Root cause:** Zoom multiplied the whole character-grid glyph/cell size. Shrinking left/right column counts kept pixel footprints near design size, but side text still painted at the zoomed EM size.

**Solution:** Decouple the grid from `GameFonts.ActiveZoom` (`CanvasGridSizer` stays window-fit 100%). Left/right/strip chrome stay at design character sizes. **`CenterColumnTextZoom`** + `CanvasPrimitivesRenderer` scale only center-column glyphs; combat-log wrap/stride follow zoom; paint clips to the center column.

**Related files:** `CenterColumnTextZoom.cs`, `CanvasGridSizer.cs`, `LayoutConstants.cs`, `CanvasPrimitivesRenderer.cs`, `DisplayRenderer.cs`, `GameCanvasControl.cs`, `EffectiveVisibleWidthRegressionTests.cs`, `CenterColumnTextZoomTests.cs`

### UI: Ctrl+/- zoom pushes centered menus off-screen (October 2026)
**Problem:** Sizing the UI up (e.g. 174%) left main-menu options near the bottom of a mostly black window, looking mis-oriented; pixel fonts also looked mushy/jagged.

**Root cause:** UI zoom multiplies fill-height glyph scale, so the character grid becomes taller than the window, but paint was top-left anchored. Vertically centered content therefore slid toward the bottom of the visible crop. Fractional EM sizes also put pixel fonts off the device pixel grid.

**Solution:** `GameCanvasControl` centers the grid in the viewport via `CanvasGridSizer.CalculateContentOrigin` (letterbox when smaller; centered crop when larger). Pointer hit-testing, wind/burst sampling, and the narrative video overlay use the same content origin. `CanvasCoordinateConverter` snaps font size to whole pixels; text draw points round to integers.

**Related files:** `CanvasGridSizer.cs`, `GameCanvasControl.cs`, `CanvasCoordinateConverter.cs`, `CanvasPrimitivesRenderer.cs`, `MouseInteractionHandler.cs`, `NarrativeVideoOverlayControl.cs`, `EffectiveVisibleWidthRegressionTests.cs`

### UI: F7 orphan wrap left early stubs like "Skill" alone (October 2026)
**Problem:** Narrative paragraphs showed jagged early line breaks — e.g. `Skill` alone on a line before `and luck conspire…`, and other mid-phrase stubs — even when the column still had room for a short connector.

**Root cause:** `TextWrappingHelper` orphan prevention pulled any short word (≤4 chars) to the next line whenever the following word would not fit, including mid-line connectors like `and`/`the` when remaining space was still larger than an orphan-sized slot.

**Solution:** Only apply short-word orphan pulls in the tight EOL zone (`remainBefore <= MaxOrphanWordLength`). EOL subjects like `Wolf` still move with their predicate; mid-line connectors stay put.

**Related files:** `TextWrappingHelper.cs`, `TextWrappingHelperTests.cs`

### UI: narrative video stays on dungeon victory screen (October 2026)
**Problem:** After clearing a dungeon, the narrative video overlay kept playing over the victory summary.

**Root cause:** `HasCurrentDungeon` remains true until the player leaves `DungeonCompletion` (dungeon is cleared in the completion handler), so the overlay’s dungeon-run gate stayed open.

**Solution:** `NarrativeVideoOverlayGate.CountsAsActiveDungeonRunForOverlay` treats `DungeonCompletion` and `Death` as inactive for overlay purposes. `MainWindow` uses that helper for `SetInDungeonProvider`; `CanvasUICoordinator` notifies the overlay on state change so playback stops immediately.

**Related files:** `NarrativeVideoOverlayGate.cs`, `MainWindow.axaml.cs`, `CanvasUICoordinator.cs`, `NarrativeVideoCellMaskTests.cs`

### UI: narrative video LibVLC unknown option `--no-hw-dec` spam (October 2026)
**Problem:** Console flooded with `vlc: unknown option or missing mandatory argument '--no-hw-dec'` in a tight loop.

**Root cause:** `EnsureLibVlc` passed `--no-hw-dec`, which this LibVLC build rejects. Ctor failed, `_mediaPlayer` stayed null, and the mask timer retried every ~66ms.

**Solution:** Drop `--no-hw-dec`; keep `--avcodec-hw=none` for software decode.

**Related files:** `NarrativeVideoOverlayControl.cs`

### UI: narrative video plays outside dungeon / before narrative log (October 2026)
**Problem:** LibVLC could decode/play the overlay while F7 was on even outside a dungeon run (or before combat-log prose appeared).

**Root cause:** Overlay gating checked config + narrative mode (+ later glyphs) but not `HasCurrentDungeon`.

**Solution:** `NarrativeVideoOverlayGate` requires dungeon run when `onlyWhenInDungeon` (default true), F7 when `onlyWhenNarrativeLog`, and occupied glyphs before Play/visible. `MainWindow` supplies `HasCurrentDungeon` via `SetInDungeonProvider`.

**Related files:** `NarrativeVideoOverlayControl.cs`, `NarrativeVideoOverlayGate.cs`, `NarrativeVideoOverlayConfig.cs`, `NarrativeVideoCellMaskTests.cs`

### UI: narrative video plays before narrative log starts (October 2026)
**Problem:** With F7 narrative on by default, LibVLC began decoding/playing `videoplayback.mp4` as soon as `MainWindow` loaded (title/menus), long before any combat-log prose appeared.

**Root cause:** `ReloadConfigAndMaybeStart` / `UpdatePlaybackState` only checked config + `IsNarrativeCombatLog`, not whether the center band had text.

**Solution:** Sample combat-log glyphs first; require `NarrativeVideoCellMask.HasOccupiedGlyphs` before Play/visible. Stop when the band is empty or narrative mode turns off. (Later also gated to dungeon runs — see entry above.)

**Related files:** `NarrativeVideoOverlayControl.cs`, `NarrativeVideoCellMask.cs`, `NarrativeVideoCellMaskTests.cs`

### UI: narrative video overlay LibVLC get_buffer / no frame spam (October 2026)
**Problem:** With F7 narrative video on, the console flooded with `[h264] get_buffer() failed`, `decode_slice_header error`, `no frame!`, plus `mp4 demux: Fragment sequence discontinuity`.

**Root cause:** (1) Custom `VideoFormat` allocated `pitch × height` and left `lines` unaligned — LibVLC needs pitches/lines multiples of 32 and a buffer of `pitches × lines`. (2) `GameData/Video/videoplayback.mp4` is a DASH/fMP4 (`ftypdash`); `:input-repeat` loop aggravated fragment sequence discontinuities.

**Solution:** `NarrativeVideoFrameLayout` computes 32-aligned RV32 storage; allocate that full size; keep visible width/height unchanged. Loop via deferred `EndReached` Stop/Play (not `:input-repeat`). Soft-decode with `--avcodec-hw=none` / `--quiet`.

**Related files:** `NarrativeVideoOverlayControl.cs`, `NarrativeVideoFrameLayout.cs`, `NarrativeVideoCellMaskTests.cs`

### UI: left character panel clips overflow and scrolls (October 2026)
**Problem:** Dense HERO/STATS/GEAR/THRESHOLDS content painted through the blue bottom border with no way to reach clipped rows.

**Solution:** Clip body drawing to the inner band (`LeftPanelViewport`), scrub anything on the border rows, track content height on `StatsPanelStateManager`, and scroll with the mouse wheel when the pointer is over the left panel (`ContainsLeftPanel` → `TryScrollLeftPanel` → chrome refresh).

**Related files:** `LeftPanelViewport.cs`, `CharacterPanelRenderer.cs`, `StatsPanelStateManager.cs`, `LayoutConstants.cs`, `MouseInteractionHandler.cs`, `LeftPanelViewportTests.cs`

### UI: left-panel Crit Miss no longer paints below the blue border (October 2026)
**Problem:** With THRESHOLDS near the bottom of the left panel, `Crit:` stayed inside the frame but `Crit Miss:` (and other ladder/CHANCES rows) still appeared under the cyan bottom border.

**Root cause:** `DiceRollThresholdRowsRenderer` always called `AddText` with no viewport clip, while `PanelText` already gated on `LeftPanelViewport.IsRowVisible`. The post-paint scrub only cleared the border row plus one spill row — not enough when Crit sat on the last body row and Crit Miss landed two+ rows below the border.

**Solution:** Pass `ContentTop` / `ContentBottomExclusive` into threshold row rendering so out-of-band rows never paint; expand clear/scrub through `LeftPanelViewport.ScrubBottomExclusive` (border + full CHANCES spill).

**Related files:** `DiceRollThresholdRowsRenderer.cs`, `ThresholdSectionRenderer.cs`, `CharacterPanelRenderer.cs`, `LeftPanelViewport.cs`, `LeftPanelViewportTests.cs`

### UI: soft-gray mouse + ↕ scroll affordance in menus (October 2026)
**Problem:** Scrollable menus (inventory bag list, skill tree) did not clearly cue that the mouse wheel works, and inventory used a shouty yellow corner arrow.

**Solution:** When content overflows, draw a dark-gray animal-mouse + up/down arrow (`🐭↕`) via `MenuMouseScrollHint`. Inventory keeps directional text hints; skill tree replaces the long “Arrows / PgUp/PgDn / Wheel scroll” line with the same soft glyph.

**Related files:** `MenuMouseScrollHint.cs`, `AsciiArtAssets.cs`, `InventoryScreenRenderer.cs`, `SkillTreeRenderer.cs`, `MenuMouseScrollHintTests.cs`

### UI / Combat: F7 DistortionPulse for SLAM (October 2026)
**Problem:** Wanted a reusable way for specific actions to trigger different text animation effects when they appear in the F7 narrative log — starting with SLAM displaying as a full word while adjacent letters get a short “pushed away” distortion.

**Solution:** `ActionTextEffectCatalog` maps action names → effect kinds (`SLAM` → `DistortionPulse`). On the Action beat only, `ActionTextEmphasisTypewriter` dumps the whole word in one typewriter step; `TextEmphasisPulseField` then pushes nearby center-panel glyphs outward and auto-reforms (exclude rect keeps the action word rigid). `TextEmphasisExplosionPlayer` plays `GameData/Video/explosion.gif` once centered on the word as a letter-masked overlay (`EmphasisExplosionLetterMask` + `PushOpacityMask`, same technique as narrative video — GIF only through occupied combat-log glyphs; SkiaSharp decode, near-black → transparent). Sibling of click-burst (does not stay scattered). Config: `windSway.emphasisPulse`. Same F6 gate.

**Related files:** `ActionTextEffectCatalog.cs`, `ActionTextEmphasisTypewriter.cs`, `TextEmphasisPulseField.cs`, `TextEmphasisExplosionPlayer.cs`, `EmphasisExplosionLetterMask.cs`, `EmphasisExplosionGifDecoder.cs`, `ActionTextEmphasisWordLocator.cs`, `CombatSequenceFlavorPresenter.cs`, `GameCanvasControl.cs`, `CanvasPrimitivesRenderer.cs`, `GameData/Video/explosion.gif`, `UIConfiguration.json`

### UI / Combat: SLAM explosion letter-masked like narrative video (October 2026)
**Problem:** The SLAM DistortionPulse GIF drew as a free rectangle under/around the word, instead of only showing through letters the way the F5 narrative video feed does.

**Solution:** After body text paints, `TextEmphasisExplosionPlayer` builds a per-cell opacity mask from occupied center-panel glyphs in the explosion footprint (`EmphasisExplosionLetterMask`) and draws the GIF with `PushOpacityMask` so it only appears on those letters. Hover/overlay text still paints above.

**Related files:** `TextEmphasisExplosionPlayer.cs`, `EmphasisExplosionLetterMask.cs`, `CanvasPrimitivesRenderer.cs`

### Bug fix / UI: SLAM DistortionPulse raced the combat paint (October 2026)
**Problem:** F7 dumped the word SLAM, but surrounding glyphs never distorted and `explosion.gif` never played behind it.

**Root cause:** `FireEmphasisPulse` called `TryTriggerActionTextEmphasis` immediately after typewriter `ReplaceLast`. That path only *schedules* a Background combat paint (`TriggerRender` → external callback / `PerformRender` posted to the UI thread), so `ActionTextEmphasisWordLocator` scanned stale `TextElements` and returned false — pulse + GIF never started.

**Solution:** `ActionTextEmphasisTrigger.QueueAfterPaint` forces a buffer render, then retries locate+fire on the UI Background queue until the word is present (or attempts exhaust). `CombatSequenceFlavorPresenter.FireEmphasisPulse` → `CanvasUICoordinator.QueueActionTextEmphasisAfterPaint`.

**Related files:** `ActionTextEmphasisTrigger.cs`, `CanvasUICoordinator.cs`, `CombatSequenceFlavorPresenter.cs`, `ActionTextEmphasisTriggerTests.cs`

### UI: click-burst glyph spin flings letters off-screen (October 2026)
**Problem:** Exploded glyphs looked like they were thrown up/out of frame even with a small explode strength (e.g. 3 cells). Equipment lines showed huge holes mid-word.

**Root cause:** Avalonia matrices use row-vector composition (`p' = p * M`). Burst/header glyph rotation built `T(C) * R * T(-C)`, which rotates around world `(0,0)` instead of the glyph center — same class of bug as the wake debug oval.

**Solution:** Rotate with `T(-C) * R * T(C)` via `CanvasPrimitivesRenderer.CreateRotateAboutPointTransform`. Letters now spin in place around their small scatter offset.

**Related files:** `CanvasPrimitivesRenderer.cs`, `CanvasPrimitiveStackingTests.cs`

### UI: click-burst explosion stuck to the screen while text scrolls (October 2026)
**Problem:** After a click-charge explosion in the combat log (or left panel), scrolling moved the text away while the scattered letters stayed fixed on the viewport.

**Root cause:** Burst zone origins and glyph keys were stored in screen/content-pixel grid space with no link to the display buffer scroll offset. Manual and auto scroll repainted lines at new grid Y values without moving the mess.

**Solution:** `TextClickBurstField.SyncViewportScroll` tracks per-viewport scroll rows; on delta it shifts in-bounds origins and remaps glyph keys. `DisplayRenderer` syncs the center log each paint; `CharacterPanelRenderer` syncs the left panel. Scatter hash is relative to the zone origin so the look does not pop when both glyph and origin move together.

**Related files:** `TextClickBurstField.cs`, `GameCanvasControl.cs`, `DisplayRenderer.cs`, `CharacterPanelRenderer.cs`, `TextClickBurstFieldTests.cs`

### UI: wake debug oval orbits world origin when oriented (October 2026)
**Problem:** Turning on **Show wake radius on canvas** made the motion-oriented “vector radius” oval jump/spin away from the cursor instead of staying centered while leaning with travel.

**Root cause:** Avalonia matrices use row-vector composition (`p' = p * M`). The overlay built `Translation * Rotation`, which translates first and then rotates around world `(0,0)`.

**Solution:** Draw with `Rotation * Translation` (`GameCanvasControl.CreateWakeDebugTransform`). `TrackMousePosition` also updates motion facing so the oval orients when wind/F6 is off.

**Related files:** `GameCanvasControl.cs`, `WindSwayField.cs`, `WindSwayFieldTests.cs`

### UI: wind wake oval rotates with mouse motion (October 2026)
**Problem:** After forcing a screen-space circle, the wake no longer matched the familiar tall cell-aspect oval, and it did not lean with travel direction.

**Solution:** Restore the cell-aspect ellipse (`cells×charWidth` × `cells×charHeight`) and rotate its major axis to follow the mouse motion vector. Debug overlay uses the same radii + rotation; sampling uses elliptical normalized distance in that oriented frame.

**Related files:** `WindSwayField.cs`, `GameCanvasControl.cs`, `WindSwayFieldTests.cs`

### UI: wind sway not fully clearing when pointer sits still (October 2026)
**Problem:** After hovering/moving over menu text (chromatic fringe + glyph offset), letting the pointer sit still left residual distortion instead of a clean rest pose.

**Root cause:** Residual wind amp kept sampling under the cursor; trail stamps preserved full wind after stop; OS micro-jitter could re-feed tiny impulses; settle timer stopped without a final rest paint.

**Solution:** Ignore sub-deadzone motion; after a short idle grace, **hard-zero** wind/speed and clear the trail; snap sampled offsets below 1px to rest; raise chromatic idle epsilon so faint ghosts cannot smear pixel fonts; paint one final clean frame when the settle timer goes inactive.

**Related files:** `WindSwayField.cs`, `WindSwayChromatic.cs`, `GameCanvasControl.cs`, `WindSwayFieldTests.cs`

### UI: item hover still smears STR / GEAR (October 2026)
**Problem:** Hovering a gear/item tip left left-panel **STR** looking muddy orange and **GEAR** fringed; distortion did not return to a clean pose until the cursor left the wake.

**Root cause:** (1) Soft-decay offsets + chromatic fringe kept painting while reading a still tooltip; OS jitter near the deadzone could refresh idle. (2) Cyan proximity glow on saturated colors (Barbarian primary-red STR, gold section headers) shifted hue toward orange/yellow.

**Solution:** Clear visual offsets/CA after a short **visual rest** (~40ms) even before hard idle zero; raise motion deadzone; skip proximity glow on saturated hues and on `====` section headers (brighten-only / no highlight).

**Related files:** `WindSwayField.cs`, `InteractiveTextHighlight.cs`, `CanvasPrimitivesRenderer.cs`, `WindSwayFieldTests.cs`, `InteractiveTextHighlightTests.cs`

### UI: wind wake radius draws as a circle, not a tall ellipse (October 2026)
**Problem:** The “Show wake radius on canvas” overlay (and the sway falloff zone) looked like a vertically stretched oval.

**Root cause:** Radius used equal *character-cell* counts on X and Y (`cells × charWidth` vs `cells × charHeight`). Monospace cells are taller than wide, so that is an ellipse in pixels.

**Solution:** Briefly forced an isotropic pixel circle. Superseded by the oriented cell-aspect oval (major axis follows mouse motion).

**Related files:** `WindSwayField.cs`, `WindSwayConfig.cs`, `WindSwayFieldTests.cs`

### UI: combo next-border waits until the action block finishes (October 2026)
**Problem:** After a successful combo, the white “next” strip border jumped to the following card as soon as `ComboStep` advanced, while that swing’s combat block was still playing.

**Root cause:** Strip selection always used live `ComboStep % filled`. Execution advances `ComboStep` before the action block presents; only the gold flash was deferred to punchline.

**Solution:** `QueueForPunchline(ComboComplete)` holds the white selection on the firing panel; `PunchlineRevealFeedback.NotifyBlockFinished` (end of `DisplayActionBlock` / `DisplayActionBlockAsync`) releases it. Gold pulse still commits on punchline and can animate during the block.

**Related files:** `HeroActionStripFeedback.cs`, `PunchlineRevealFeedback.cs`, `BlockDisplayManager.cs`, `DungeonRenderer.RoomAndCombat.cs`, `HeroActionStripFeedbackTests.cs`

### UI: clipboard copy no longer flashes status text (October 2026)
**Problem:** Right-click / Ctrl+C to copy the combat/dungeon text log briefly flashed yellow status text (e.g. `Copied N lines…`) for a frame.

**Root cause:** `ClipboardHelper.CopyDisplayBufferToClipboard` called `UpdateStatus` / `Notify` on every copy attempt, which drew a canvas status message that was immediately overwritten by the next render.

**Solution:** Remove all status-text feedback from the clipboard helper. Keep only `FlashCenterPanelCopyFeedback` (center-panel tint) as the success cue.

**Related files:** `ClipboardHelper.cs`, `MainWindow.axaml.cs`

### UI: combat log text no longer paints below the center panel border (October 2026)
**Problem:** Soft-wrapped combat-log / F7 narrative lines (e.g. tempo closers) could appear entirely below the cyan center-panel bottom border.

**Root cause:** `DisplayRenderer` only checked that a message *started* inside the content viewport, then `WriteLineColoredWrapped` painted every wrapped row with no bottom clip. The clear band also stopped at the inner content bottom, so spilled glyphs on the border/outer-pad rows stuck across frames.

**Solution:** Pass an exclusive max Y into `WriteLineColoredWrapped` so wrapped rows stop at `contentY + contentHeight`. Extend the framed-log clear band through the bottom border row and outer bottom pad (help footer redraws afterward). Helpers `CountLinesFittingInViewport` / `ComputeClearEndY` lock the contract in tests.

**Related files:** `DisplayRenderer.cs`, `ColoredTextWriter.cs`, `DisplayRendererClearBandRegressionTests.cs`

### UI: text-log copy works on stay/leave dungeon prompt (October 2026)
**Problem:** Right-click / Ctrl+C copied the center text log during combat, but did nothing on the between-room stay/leave prompt.

**Root cause:** `IsCombatLogClipboardContext` only allowed combat display mode, `GameState.Combat`, and `ActionInteractionLab`. After a fight the game returns to `GameState.Dungeon` with `StandardDisplayMode`, so the stay/leave prompt failed the gate even though the same display buffer was still on screen.

**Solution:** Centralize the gate in `CombatLogCopyInput.AllowsClipboardContext` and include `GameState.Dungeon` so dungeon exploration (including exit choice) keeps the same copy behavior as combat.

**Related files:** `CombatLogCopyInput.cs`, `CanvasUICoordinator.DisplayBuffer.cs`, `CombatLogCopyInputTests.cs`, `HotkeyHelpCatalog.cs`

### UI: F7 prose hover tip punched a hole through narrative (October 2026)
**Problem:** Hovering an F7 narrative paragraph opened a centered yellow mechanical tip; full-width prose still showed through the box and left orphaned fragments on both sides.

**Root cause:** Tip width was capped (~72) and centered over the log column. `ClearTextInArea` only removes text whose origin cell is inside the clear rect, so runs that start left of the tip and extend into it kept painting through.

**Solution:** Size the combat-log tip to the prose hit band (full column width). Before drawing the opaque framed panel, mask intersecting non-overlay glyphs (`MaskTextRunOutsideRange` / `MaskNonOverlayTextInArea`) so leftover body text cannot bleed under the tip.

**Related files:** `DungeonRenderer.RoomAndCombat.cs`, `HoverTooltipDrawing.cs`, `CanvasElementManager.cs`, `CombatLogActionHoverState.cs`, `CombatLogProseHoverMap.cs`, `HoverTooltipDrawingTests.cs`, `CombatLogProseHoverTests.cs`

### UI: F7 toggle converts existing combat log (October 2026)
**Problem:** Pressing F7 only changed how *future* swings were written; already-buffered lines stayed in the previous format.

**Root cause:** Narrative and mechanical forms were not stored as a dual view on the display buffer, so there was nothing to swap.

**Solution:** Each dual-capable entry keeps an alternate view (`lineHoverInfoLines` + `lineDualSpans`). Narrative prose already stored the mechanical tip for hover; mechanical writes now bind a silent narrative paragraph via `TryBuildSilentParagraphFromPending`. F7 calls `CombatLogDualView.Swap` and rebuilds the active character's buffer.

**Related files:** `CombatLogDualView.cs`, `BufferStorage.cs`, `MainWindow.axaml.cs`, `BlockDisplayManager.cs`, `CombatSequenceFlavorPresenter.cs`, `CombatLogDualViewTests.cs`

### UI: equip comparison Mods/Stats/Actions wrap inside column (October 2026)
**Problem:** On the Avalonia equip comparison screen, long `Mods:` (and Stats/Actions) summary lines on CURRENT/NEW ITEM columns spilled past the center panel into the right inventory sidebar.

**Root cause:** `ItemComparisonRenderer.RenderItemBonuses` received `columnWidth` as `maxWidth` but rendered with `RenderSegments` (single unwrapped line). Action-change lines had the same issue.

**Solution:** Soft-wrap those colored lines with `ColoredTextWriter.WriteLineColoredWrapped` at the comparison column width, advancing Y by the wrapped row count. `BuildModsLineSegments` exposes the mods line for unit coverage.

**Related files:** `ItemComparisonRenderer.cs`, `ItemComparisonRendererTests.cs`

### UI: F7 typewriter kept identity colors while typing (October 2026)
**Problem:** Growing F7 narrative sentences looked fully white until a name/action word finished (and DistortionPulse neighbors did not shove if the mouse was still).

**Root cause:** Keyword/name coloring required a complete token (`Marcus Serpentfang`, `SLAM`, `wound`). Typewriter prefixes such as `Mar` / `SLA` / `wou` missed those matches. `CanvasPrimitivesRenderer.ShouldApplyDistortion` also ignored an active emphasis pulse unless wind or click-burst was already distorting.

**Solution:** `CombatSequenceNarrativeEmphasis` paints trailing identity prefixes; `KeywordColorSystem` colors registered-name prefixes (including the first token of a two-word name) and short last-token keyword prefixes. Typewriter `Truncate` clones colored spans. Distortion gates treat emphasis pulse like click-burst (global for non-overlay body text).

**Related files:** `CombatSequenceNarrativeEmphasis.cs`, `KeywordColorSystem.cs`, `ColoredTextRenderer.cs`, `CanvasPrimitivesRenderer.cs`, `CombatSequenceFlavorPresenterTests.cs`, `KeywordColorSystemTests.cs`, `CanvasPrimitiveStackingTests.cs`

### UI: F7 typewriter no longer jumps words mid-reveal (October 2026)
**Problem:** During character-by-character narrative reveal, a word that would not fit on the current line started typing at EOL and then jumped to the next line once more letters arrived.

**Root cause:** Truncate painted a growing unwrapped prefix; buffer wrap only saw the partial word, so it stayed on the current line until the full word no longer fit.

**Solution:** Soft-wrap the complete paragraph (`TextWrappingHelper.ApplySoftWraps` at `CenterPanelTextColumnWidth`) before typewriter Truncate. Overflow words begin on the next line from their first glyph. Soft newlines skip typewriter delay and pacing-ramp counters. `BufferStorage.FitToLineWidth` shares the same helper.

**Related files:** `CombatSequenceFlavorPresenter.cs`, `TextWrappingHelper.cs`, `BufferStorage.cs`, `TextWrappingHelperTests.cs`

### UI: sparse F7 narrative combat-log highlights (October 2026)
**Problem:** F7 narrative paragraphs were dense with cyan atmospheric words (`fortune`, `stance`, `steel`, `heavy`, `solid`, …) and red false matches on glue words like `as` (substring overlap with `slash`).

**Root cause:** `KeywordGroupManager` registered a large cyan `narrative` keyword group plus common damage verbs; `KeywordGroup.ContainsKeyword` used bidirectional substring matching.

**Solution:** Keep identity spans (names + action) and only severity/status/effect keywords. Exact whole-word match after punctuation strip. Drop atmospheric cyan narrative keywords and common verbs (`hit`/`strike`/`attack`).

**Related files:** `KeywordGroupManager.cs`, `KeywordColorSystem.cs`, `CombatSequenceNarrativeEmphasis.cs`, `CombatSequenceFlavorPresenterTests.cs`, `KeywordColorSystemTests.cs`

### UI: F7 enemy article left uncolored (October 2026)
**Problem:** Narrative lines colored the whole phrase `the Orc` / `The Orc` in enemy orange, including the definite article.

**Root cause:** Enemy tokens include `the ` + name for prose grammar, and `CombatSequenceNarrativeEmphasis` / name-color registration used that full string as the identity span.

**Solution:** `CombatSequenceFlavorTokens.StripLeadingArticle` strips the article before emphasis matching and `KeywordColorSystem` registration so only the bare name is colored.

**Related files:** `CombatSequenceFlavorTokens.cs`, `CombatSequenceNarrativeEmphasis.cs`, `CombatSequenceFlavorPresenter.cs`, `CombatSequenceFlavorPresenterTests.cs`

### UI: blank spaces between letters on dungeon/room names and status words (October 2026)
**Problem:** Header values like `Dungeon: Ancient Forest` / `Room: Puzzle Chamber` and status words like `STUN` / `stunned` rendered with a space between every letter (`A n c i e n t`, `S T U N`). Solid-color lines were fine.

**Root cause:** Undulation and multi-color templates emit one `ColoredText` segment per glyph. `TextWrappingHelper.WrapColoredSegments` tokenized each glyph as its own word and always re-inserted a space between words, inventing letter-spacing that was not in the source text.

**Solution:** Track whether source text actually had whitespace before each word token (`NeedsLeadingSpace`). Only insert a separating space when that flag is set (or after glued punctuation). Adjacent per-character glyphs stay compacted.

**Related files:** `TextWrappingHelper.cs`, `TextWrappingHelperTests.cs`

### Inventory: equipped charm missing from comparison (September 2026)
**Problem:** Choosing a bag charm while another charm was equipped showed `(empty slot)` under CURRENT ITEM. The header also read EQUIP ITEM instead of EQUIP CHARM.

**Solution:** Comparison and inventory refresh look up the worn item with `CharacterEquipment.GetSlotItem`, which includes the charm slot. The current-column hover id is `gear:charm`, and the left-panel tooltip builder renders that charm.

**Related files:** `InventoryItemComparisonHandler.cs`, `InventoryMenuHandler.cs`, `CharacterEquipment.cs`, `ItemComparisonRenderer.cs`, `LeftPanelTooltipBuilder.cs`

### UI: neutral stance on every action card (September 2026)
**Problem:** Every action card printed `result: neutral stance` when BLOCK was 100%. **SLAM** was authored as defensive (BLOCK 180).

**Solutions:**
1. `StandingBlock.FormatCardLine` returns an empty string for neutral. The strip skips that row so it does not leave a blank line. Hover Stats uses the same string, so the tip omits it too.
2. `Actions.json` **SLAM** BLOCK is **0** (aggressive).

**Related files:** `StandingBlock.cs`, `HeroDefenseHudFormatter.cs`, `DungeonRenderer.RoomAndCombat.cs`, `Actions.json`

### Progression: Puberty adds level to Strength (September 2026)
**Problem:** Puberty was a flat +15 Strength, so a level 5 hero gained the same Strength as a level 25 hero.

**Solution:** Learned Puberty (`customEffectId` `puberty`) adds the hero's level to Strength (level 5 → +5 STR), once per rank. Commission, Initiation, and Apprenticeship stay +15. The STR hover **Skill tree** line shows that same number.

**Related files:** `SkillEffectRouter.cs`, `SkillTrees.json`, `SkillEffectRankScalingTests.cs`, `StatTooltipFormatterTests.cs`
**Problem:** Echo Spell (`retrigger_next`) played itself again, and the encore roll footer showed up as `(ro...`.

**Solutions:**
1. Snapshot the next strip index when the retrigger is scheduled, before the hit advances `ComboStep`. Consuming `ComboStep + 1` after that advance wrapped a short strip back onto the action that just fired.
2. After the encore, move the sequence to the slot after the one that was retriggered (slot 1 Echo Spell + slot 2 Slam → next turn is slot 3). Nested swings do not change the strip.
3. Width-cap each line inside a multi-line combat-log entry. The prepare line, encore headline, and roll footer share one buffer message; capping the whole blob chopped the roll line.

**Related files:** `RetriggerScheduler.cs`, `ActionExecutionFlow.cs`, `ActionExecutionFlow.Outcomes.cs`, `BufferStorage.cs`

### Combat: WoW-style Defense DR (September 2026)
**Problem:** Standing BLOCK was a flat percent DR plus class Tempo/Counter/Shield/Grit, which did not match the diminishing-returns Defense model needed for testing.

**Solutions:**
1. `DR = effective / (effective + K)` with `effective = rating × action BLOCK%` (0–500; 100=1×); K from `Combat.ArmorReductionFactor` (default 100)
2. Unnamed/open stance is 100%; incoming multi-hit uses the action modifier on tick 0 only
3. Live path does not apply Grit/Shield or mint Tempo/Counter; remap Actions.json 0/25/45 → 0/100/180
4. Tests: `ClassDefenseCalculatorTests`, `ActionBlockSheetColumnsTests`, `DamageCalculatorTests`, `HeroDefenseHudFormatterTests`, `MultiHitTests`

**Related files:** `StandingBlock.cs`, `ClassDefenseCalculator.cs`, `DamageCalculator.cs`, `Actions.json`

### Combat: hit resets combo BLOCK; enemies use Defense DR (September 2026)
**Problem:** Combo BLOCK stayed until the next named action, so the left-panel Defense number did not match the live modified rating, and enemies still used flat armor subtract.

**Solution:**
1. Left-panel Defense shows `rating × standing BLOCK`
2. A successful incoming hit resets standing BLOCK to 100% (base) after the swing HUD/footer is captured
3. Enemies use the same WoW DR path and standing BLOCK; enemy panel shows Defense
4. Tests: `ClassDefenseCalculatorTests`, `DamageCalculatorTests`, `HeroDefenseHudFormatterTests`

**Related files:** `StandingBlock.cs`, `ClassDefenseCalculator.cs`, `DamageCalculator.cs`, `CharacterPanelRenderer.cs`, `RightPanelRenderer.cs`

### UI: Defense rating on panel, % DR on hover (September 2026)
**Problem:** Standing BLOCK % and live DR % were always visible under HERO/STATS, crowding the Defense rating the player should read at a glance.

**Solution:**
1. Left-panel HERO always shows `Health X/Y  Defense N`; STATS shows Defense rating only
2. Defense hover highlights live **Damage reduction N%**; BLOCK / effective / K remain in the tip
3. Tests: `HeroDefenseHudFormatterTests`, `StatTooltipFormatterTests`

**Related files:** `CharacterPanelRenderer.cs`, `StatTooltipFormatter.cs`, `HeroDefenseHudFormatter.cs`

### Bug fix: Loaded Dice (replace_next_roll) still triggered combo amp (September 2026)
**Problem:** After **LOADED DICE** set the next natural roll to **12**, the follow-up swing logged `roll: 12` but still executed a named strip action (**SLAM**) with `amp: 1.04x` despite 12 being below the combo threshold.

**Root cause:** `ActionExecutionFlow.SelectActionAndResolveRoll` called `ActionSelector.SelectActionByEntityType` (which rolls a fresh d20 for combo-vs-normal selection) **before** consuming `CombatTriggerContext`’s pending replace face. A high raw die could pick the strip action; only the displayed/resolved face was overwritten to 12. Combo amp follows `action.IsComboAction`, so the wrong pick showed amp.

**Solutions:**
1. Consume `replace_next_roll` **before** selection and pass the face as `forcedBaseRoll` into `SelectActionBasedOnRoll` / `SelectEnemyActionBasedOnRoll`
2. Forced / strip-replaced actions that skip selection still apply the same face to `BaseRoll`
3. Tests: `ActionExecutionFlowTests.TestReplaceNextRollFaceGatesComboSelection`, `ActionSelectorRollBasedTests.TestForcedBaseRollOverridesDiceForSelection`

**Related files:** `ActionExecutionFlow.Selection.cs`, `ActionSelector.cs`

### Bug fix: hero miss flavor replayed on every later swing (September 2026)
**Problem:** After a hero critical miss, the combat log kept printing random hero-miss flavor (`goes astray`, `off-target`, `poorly timed strike`) on later hits and enemy turns.

**Root cause:** `BattleNarrative` stored events in a `ConcurrentBag` and treated `ToList()[Count-1]` as the latest swing. The bag is unordered (often LIFO), so the original natural-1 event stayed "last." `GetTriggeredNarrativesIfSignificant` re-analyzed that miss each turn and minted a new random miss line. Display filtering also missed several FlavorText phrases, so leftover miss copy leaked onto hits.

**Solutions:**
1. Store events in a FIFO `ConcurrentQueue`; generate narratives once in `AddEvent`; consume them once for display
2. Overview combat log no longer attaches BattleNarrative flavor under action blocks (action + roll + status only; F7 is the prose path)
3. Tests: `BattleNarrativeTests`, `BattleEventAnalyzerTests`, `CombatLogDisplayTests`, `BlockMessageCollectorTests`

**Related files:** `BattleNarrative.cs`, `BattleEventAnalyzer.cs`, `TextDisplayIntegration.cs`, `BlockMessageCollector.cs`

### Combat: free action Block % replaces energy (September 2026)
**Problem:** Energy was only a proxy for BLOCK (cost 1–3 → leftover → fixed % table) and cluttered the action budget metaphor.

**Solutions:**
1. Remove `EnergyCost` / ENERGY / leftover ledger; actions author free **Block %** (`StandingBlock`, ACTIONS **BLOCK**)
2. Standing BLOCK until next named hero action; class DEFENSE = Tempo / Counter / Shield / Grit (no Warrior %, dodge, or armor RAGE)
3. Migrate legacy energy 1/2/3 → block 45/25/0; JSON field `"block"`
4. **ACTIONS push** renames ENERGY → BLOCK in place (or deletes leftover ENERGY); inserts BLOCK only when neither exists
5. Tests: `ActionBlockSheetColumnsTests`, `ClassDefenseCalculatorTests`, `HeroDefenseHudFormatterTests`

**Related files:** `StandingBlock.cs`, `ClassDefenseCalculator.cs`, `ActionBlockSheetColumns.cs`, `ActionSheetsPushService.cs`, `Actions.json`, `HeroDefenseHudFormatter.cs`

### Bug fix: ACTIONS push skipped ENERGY and overwrote keyword-bonus headers (September 2026)
**Problem:** Pushing ACTIONS did not add an ENERGY column, and it overwrote designer convert-scale headers at DS/DT/DU (**bonus per keyword**, **effect**, **keyword**).

**Root cause:** Ensuring ENERGY inserted the label into the in-memory header, then `WriteHeaderRowsAsync` dumped the entire header row as cell values with no Sheets `InsertDimension`. That overwrote every subsequent header (and blanked extra columns on data write) instead of adding a column. Push also preferred a stale published CSV header over the live tab.

**Solutions:**
1. Read the live ACTIONS tab header; insert new columns with `InsertDimension`; write only those header cells
2. Overlay existing row cells for unknown columns; map **bonus per keyword** / **effect** / **keyword** as DS/DT/DU aliases; do not blank non-empty convert cells with empty JSON
3. Column is now **BLOCK** (`"block"`); legacy `"energy"` still migrates on load
4. Tests: `ActionBlockSheetColumnsTests`, `SpreadsheetActionDataSheetRowSerializerTests`, `ActionSheetsPushRowMergerTests`

**Related files:** `ActionSheetsPushService.cs`, `ActionBlockSheetColumns.cs`, `ActionConvertScaleSheetColumns.cs`, `SpreadsheetActionDataSheetRowSerializer.cs`, `ActionSheetsPushRowMerger.cs`, `SpreadsheetActionJsonConverter.cs`

### Bug fix: sequence HUD painted over the Skill Tree (September 2026)
**Problem:** Opening the Skill Tree (or other hub/menu screens) still showed the combat sequence HUD (ATTACKER / ROLL / OUTCOME / …) over the tree boxes.

**Root cause:** The HUD was reserved as dungeon chrome (`GameState.Dungeon` plus Combat). Skill Tree is a menu that paints through `CoordinateLayout`, which always calls `CombatSequenceHudRenderer.Render`. Display-buffer rendering is suppressed for menus, so `IsBandReserved` stayed true from the last fight and the leftover swing overlaid the tree.

**Solutions:**
1. `ShouldReserveBand` keeps dungeon chrome (Dungeon + Combat + Action Lab) but excludes Skill Tree, inventory, hub, selection, and completion
2. `SyncReservation` clears leftover columns when leaving reserved states; called from state-change, layout, and the display-buffer paint path

**Related files:** `CombatSequenceHudState.cs`, `CanvasUICoordinator.cs`, `PersistentLayoutRenderCoordinator.cs`, `RenderCoordinator.cs`

### UI: sequence HUD shows from dungeon select (October 2026)
**Problem:** The ATTACKER/ROLL/OUTCOME status bar only appeared once combat started, so ENTERING DUNGEON / ENTERING ROOM played without the empty header band.

**Solution:** `ShouldReserveBand` includes `GameState.Dungeon` again so the band reserves as soon as the dungeon run begins (headers idle until the first swing). Skill Tree and other menus stay excluded via `SyncReservation`.

**Related files:** `CombatSequenceHudState.cs`, `CombatSequencePresenterTests.cs`

### Action Lab sequence HUD Step lock and missing Material (August 2026)
**Problem:** The Action Lab combat canvas did not show the sequence HUD (or Material set UI on lab-edited gear). Piece-by-piece stepping also could not work because `_labControlInFlight` held the tools lock for the whole `StepAsync`.

**Root cause:** `ShouldReserveBand` omitted `GameState.ActionInteractionLab`. Lab weapon/armor factories cleared mods and never set `Item.Material`. The tools in-flight gate dropped a second `[ Step ]` while HUD playback waited.

**Solutions:**
1. Reserve the HUD band in the lab; Piece mode waits on `TryAdvanceManualBeat` (advance before the in-flight lock; release the lock before awaiting a Swing/Piece turn)
2. Stamp Material on lab-built items (`ActionLabGearMaterial.Stamp`)
3. Tests: `CombatSequencePresenterTests` manual advance/cancel; `ActionInteractionLabTests` toggle + factory Material

**Related files:** `CombatSequenceHudState.cs`, `CombatSequencePresenter.cs`, `ActionLabInputCoordinator.cs`, `ActionLabWeaponFactory.cs`, `ActionLabArmorFactory.cs`

### UI: normal hit no longer flashes the combo card green (September 2026)
**Problem:** A normal hit pulsed the current combo-strip card green. The selected card should stay solid white.

**Solution:** `HeroActionStripFlashKind.Hit` clears any strip pulse instead of painting green. Miss stays red; a combo-action hit stays gold.

**Related files:** `HeroActionStripFeedback.cs`, `GameplaySettingsPanel.axaml`, `HeroActionStripFeedbackTests.cs`

### Bug fix: sequence HUD froze the canvas while attack audio still played (August 2026)
**Problem:** The first live swing left the window stuck (action on the strip, enemy HP unchanged, combat log not advancing) while hit/miss SFX still played.

**Root cause:** Encounter combat runs on a threadpool task. `CombatSequencePresenter` called `gameCanvas.Refresh()` (`InvalidateVisual`) from that thread, which deadlocks or stalls Avalonia painting. HUD text is drawn in `RenderLayout`, so a visual invalidate also never rebuilt the two-row band; HP stays on the pre-swing hold until a real layout paint.

**Solutions:**
1. Post HUD paints with `Dispatcher.UIThread.Post` (never invoke ForceRender/InvalidateVisual inline on combat, and never nest ForceRender inside a paint callback)
2. Wire the live callback to `CanvasUICoordinator.ForceRender` so ACTION/ROLL/OUTCOME and the HP drop rebuild chrome
3. Tests: `CombatSequencePresenterTests` asserts the invalidate callback does not run inline from a background thread

**Related files:** `CombatSequencePresenter.cs`, `GameInitializationHandler.cs`

### Combat log: room structure shift hidden inside the luck arrow (October 2026)
**Problem:** A dilapidated room turned a luck keep of 18 into 17, and the footer showed `2d20 luck 18/2 → 17` with no sign that the third number was a room penalty.

**Root cause:** `EnvironmentRollModifier` applies elegant (+) / dilapidated (−) hero-level shift after the two faces are recorded. The arrow prints that shifted face.

**Solution:** When the shift actually changes the die, the action block adds a follow-up line under the roll footer: `(dilapidated: -1)` or `(elegant: +N)`. Cancelled tags and clamped no-ops stay silent. Tests: `EnvironmentRollModifierTests`.

### Bug fix: luck 18 showed a normal hit instead of the named action (August 2026)
**Problem:** Combat log could show `2d20 luck 18/5 → 18` with `and hits for N damage` (unnamed) instead of `and hits with {ACTION}`.

**Root cause:** Combo-strip vs unnamed normal was chosen from the first d20, then luck rolled a second die for display only (and luck was sometimes rolled twice). A 5 that luck-kept 18 still executed the synthetic normal.

**Solutions:**
1. Action selection no longer rolls luck; the unforced second d20 is consumed once during resolution
2. After luck, reconcile the selected action to that kept face vs the combo threshold
3. Tests: `ActionExecutionFlowTests`, `ActionSelectorRollBasedTests`

**Related files:** `ActionSelector.cs`, `ActionExecutionFlow.Selection.cs`

### Bug fix: action SFX played on combat-log setup instead of reveal (August 2026)
**Problem:** Hit/miss/crit/combo/hurt sound effects played when `{Actor} Attacks {Target}...` appeared, before the line revealed `and hits` / `and misses`.

**Root cause:** `ActionEventPublisher` fired `AudioCues.Trigger` as soon as the swing resolved, which is before `DisplayActionBlockAsync` shows the setup telegraph.

**Solutions:**
1. Queue the outcome cue with `AudioCues.QueueForPunchline` at publish time
2. Commit it with strip flash on punchline reveal (`PunchlineRevealFeedback.CommitQueued`)
3. Drop the cue when the action block is not shown (`ClearQueued`)
4. Tests: `AudioCueDispatcherTests` queue-until-commit and clear-without-play

**Related files:** `AudioCues.cs`, `ActionEventPublisher.cs`, `PunchlineRevealFeedback.cs`, `CanvasUIRenderer.cs`

### Bug fix: left-panel gear hover tooltip sat far from the mouse (August 2026)
**Problem:** Hovering equipped gear or **Sets:** in the left sidebar opened the yellow tooltip in the upper-middle of the center panel, away from the cursor.

**Root cause:** Left-panel hit targets do not overlap the center inner band (1-cell gap). `GetHorizontalPositionAvoidingTarget` then kept the centered default X, and the overlay always used the top of the framed center panel for Y.

**Solutions:**
1. Dock X to the nearer inner edge when the hover target is entirely left (or right) of the center band
2. Align tooltip top with the hovered row, clamped so the box stays inside the band (`GetVerticalPositionNearTarget`)
3. Tests: `HoverTooltipDrawingTests`

**Related files:** `HoverTooltipDrawing.cs`, `DungeonRenderer.RoomAndCombat.cs`

### Bug fix: combat follow-up lines scrolled the setup block up (August 2026)
**Problem:** Near the bottom of the combat log, `{Actor} Attacks {Target}...` appeared, then roll/feed/status lines appended after the half delay and pushed the whole block up a line.

**Root cause:** Canvas two-beat display wrote the setup line first, waited, then appended follow-ups. Each new row grew the buffer and scrolled.

**Solutions:**
1. Dump setup plus one blank placeholder per follow-up in the same immediate pass (`SetupPunchlineReservation`)
2. After the wait, `ReplaceAtFromEnd` fills the headline and follow-ups in place (line count does not grow)
3. Inter-line `MessageDelayMs` still applies between filled follow-ups
4. Tests: `DisplayBufferReplaceLastTests`

**Related files:** `CanvasUIRenderer.cs`, `SetupPunchlineReservation.cs`, `BufferStorage.cs`, `CanvasUICoordinator.IUIManager.cs`

### Bug fix: title screen hitch on first window show (August 2026)
**Problem:** The main window appeared, froze for about a second with the Windows wait cursor (blue circle), then the title idle started.

**Root cause:** Opacity 0 still shows a black window on Windows. `Show()` also measured `SettingsPanel` (the same 2563-line tree `SettingsWindow` already delayed because it freezes layout). Revealing before `GameCoordinator` warmup meant that UI-thread construction froze the visible window.

**Solutions:**
1. `TitleScreenHelper.Preload()` runs in `App.OnFrameworkInitializationCompleted` before `new MainWindow()`
2. Main window starts **minimized**, opacity 0, no taskbar button
3. `SettingsPanel` / `TuningMenuPanel` are created only when those menus open
4. After `Opened`, paint the preloaded frame and await warmup **while minimized**, then restore to normal
5. Tests: `TitleScreenAnimationTests`, `TitleToMenuBootstrapTests`

**Related files:** `TitleScreenController.cs`, `GameInitializationHandler.cs`, `App.axaml.cs`, `MainWindow.axaml`

### Bug fix: Windows PC launcher failed for friends (August 2026)
**Problem:** Sharing the repo (zip or folder) and double-clicking `Dungeon Fighter(PC).bat` failed on a friend's PC: no .NET SDK, the installer wanted administrator rights, downloaded files were blocked by Mark of the Web, or they ran the `.bat` from inside the zip window.

**Root cause:** The launcher always rebuilt from source and installed the SDK into Program Files. Friends typically have no SDK, no admin, and an incomplete extract.

**Solutions:**
1. Real launcher is `Scripts/launch-windows.bat`; `DungeonFighter-PC.bat` is the preferred trampoline (no parentheses)
2. Unblock downloaded scripts/exes; refuse zip-internal paths; require `Code` + `GameData`
3. Install .NET 8 SDK to `%USERPROFILE%\.dotnet` without admin
4. If `dist\DF.exe` already exists, launch it instead of failing the build
5. Tests: `WindowsLauncherTests`

**Related files:** `Scripts/launch-windows.bat`, `Scripts/install-dotnet.ps1`, `DungeonFighter-PC.bat`, `Dungeon Fighter(PC).bat`

### Bug fix: convert actions added +5 per keyword instead of multiplying (August 2026)
**Problem:** Convert actions (BONE WRATH / IRON CULL / …) multiplied swing damage by the keyword bank (2 RAGE doubled the hit). Feed 3/5 was also folded into that multiplier.

**Root cause:** `GetConvertDamageMultiplier` treated bank (+ 3/5 feed) as an action damage multiplier.

**Solutions:**
1. Convert adds **+5 per banked keyword** (`GetConvertDamageBonus`; 2 RAGE → +10)
2. Feed 3/5 still only changes how much currency is minted
3. Card line is `{KEYWORD} +N`; strip preview adds the flat bonus
4. Tests: `MaterialSetControllerTests.TestTwoRageAddsTenConvertDamage`, `DamageCalculatorTests.TestConvertKeywordAddsFlatDamage`, `ActionCardExternalBonusCollectorTests`

**Related files:** `MaterialSetController.cs`, `DamageCalculator.cs`, `ActionCardExternalBonusCollector.cs`, `CombatActionStripBuilder.cs`

### Bug fix: material convert unlocked in tooltip but missing from action pool (August 2026)
**Problem:** Bone 2/5 hover said `Convert: BONE WRATH (unlocked)`, but **POOL (from gear)** still listed only weapon/class actions.

**Root cause:** Hover only checks equipped material count. Convert grant into the pool lived on `RebuildCharacterActions` (dungeon start / save load). Inventory `EquipmentManager` updated gear/class actions on equip and never synced material converts.

**Solutions:**
1. `MaterialSetController.SyncConvertActionsToPool` adds granted converts and removes them from pool + combo when the set drops below 2
2. `EquipmentManager.UpdateActionsAfterGearChange` calls that sync after every equip/unequip
3. Opening inventory and combat init also sync so an already-equipped 2-set heals without re-equipping
4. `RebuildCharacterActions` uses the same helper
5. Convert load falls back to raw action data if the workshop active-set tier filter would hide the row
6. Tests: `MaterialSetControllerTests.TestSyncConvertActionsToPoolOnEquipAndUnequip`, `EquipmentManagerTests.TestEquipSecondMaterialPieceAddsConvertToPool`

**Related files:** `MaterialSetController.cs`, `EquipmentManager.cs`, `CharacterSerializer.cs`

### Material keyword currency now lasts the dungeon (August 2026)
**Problem:** Material-set keyword currency (CRISIS, DRAG, …) reset at the start of every fight, so convert scale could not grow across rooms in a dungeon.

**Root cause:** `CombatStateManager.InitializeCombatEntities` called `MaterialSetController.ClearFightBanks`, which wiped the keyword bank on every combat init.

**Solutions:**
1. Combat init only resets consecutive-connect tracking (`ResetFightConnects`)
2. Keyword bank clears with other dungeon-run state in `Character.ClearDungeonRunTempEffects` (dungeon start, completion, early exit, clone-after-death)
3. Tests: `MaterialSetControllerTests.TestKeywordBankSurvivesCombatInitAndClearsOnDungeonEnd`, `CombatStateManagerTests.TestInitializeCombatEntitiesPreservesMaterialKeywordBank`

**Related files:** `MaterialSetController.cs`, `CombatStateManager.cs`, `Character.cs`, `DungeonOrchestrator.cs`

### Bug fix: strip_random crashed dungeon on 1-slot combo (August 2026)
**Problem:** After an action announced "next combo slot is randomized", the dungeon aborted with `Dice must have at least 2 sides (Parameter 'sides')`.

**Root cause:** `strip_random` / `ComboRouting.RandomAction` routes through `ComboRouter.PickRandomEnabledSlot`, which called `Dice.Roll(1, enabled.Count)`. A 1-slot strip (or one remaining non-disabled slot) passed `sides = 1`, which `Dice` rejects.

**Solutions:**
1. When zero enabled slots, return 0; when exactly one, return that slot without rolling
2. Only call `Dice.Roll` when ≥2 enabled slots
3. Tests: `StripMutationTests.TestStripRandomSingleSlotDoesNotThrow`, `TestStripRandomOneEnabledSlotDoesNotThrow`

**Related files:** `ComboRouter.cs`, `StripMutationTests.cs`

### Bug fix: Closing the window left DF.exe locked (August 2026)
**Problem:** Hitting the title-bar **X** closed the UI but `DF.exe` often stayed alive, so the next build failed with `MSB3026` (`DF.exe` locked by process `DF`).

**Root cause:** Menu Exit Game called `Environment.Exit(0)` after cleanup; the window-close path only ran `ApplicationShutdownHelper.PerformShutdown()` and relied on Avalonia lifetime. SoundFlow/native threads could keep the process alive after the main window closed.

**Solutions:**
1. Main window `Closing` and desktop `Exit` call `PerformShutdown(forceProcessExit: true)`
2. Forced exit starts a 1.5s watchdog so hung audio dispose cannot leave a zombie process
3. Ticker `Stop(waitForExit: false)` on shutdown so Closing is not blocked on `Task.Wait`
4. Menu Exit Game uses the same helper
5. `Code.csproj` kills leftover `DF.exe` before `BeforeBuild` (with a short settle delay)
6. Tests: `ApplicationShutdownHelperTests`

**Related files:** `App.axaml.cs`, `ApplicationShutdownHelper.cs`, `SettingsMenuHandler.cs`, `GameTicker.cs`, `Code.csproj`

### Class Skill Trees — Skill Points vs rank (August 2026)
**Problem:** Spending class points into skills must not lower titles, combo slot tiers, or item scaling that key off lifetime path investment.

**Solutions:**
1. Keep `BarbarianPoints` / `WarriorPoints` / `RoguePoints` / `WizardPoints` as **lifetime** Skill Points
2. Spent amount is derived from learned node costs in `SkillTrees.json`; `Available = Lifetime − Spent`
3. `TryLearnSkillNode` never calls `RemoveClassPoint`; roots auto-grant at cost 0 when a path has ≥1 lifetime point
4. Hub: `GameState.SkillTree` beside Inventory; spend on the **primary** path tree, plus **shared secondary-rail** nodes (`sharedWith` containing the primary weapon/class) which spend **owner-path** SP; learned nodes stay active if path is no longer primary
5. Default node cost is **1 SP per rank** (`TierCosts` fallback `{0,1,1,1,1}`); Action nodes are forced to cost 1 / maxRank 1; scalable Passive/Mastery sinks use maxRank up to 5 and their combat bonuses multiply by learned rank in `SkillEffectRouter`
6. Tests: `SkillTreeProgressionTests`, `SkillEffectRankScalingTests`, updated `ClassActionManagerTests`

**Related files:** `CharacterProgression.cs`, `SkillTreesConfig.cs`, `SkillTreeService.cs`, `SkillEffectRouter.cs`, `SkillTreeMenuHandler.cs`, `SkillTreeRenderer.cs`, `ClassActionManager.cs`, `GameData/SkillTrees.json`

### Bug fix: Puberty +15 STR not applied (August 2026)
**Problem:** Learning **Puberty** (R1/1, “Rite of passage: +15 Strength”) left the HUD STR unchanged (e.g. 13 instead of 28). The node was stored in `LearnedSkillRanks` with `customEffectId` `puberty`, but no runtime applied that bonus to effective attributes.

**Solutions:**
1. `SkillEffectRouter.GetSkillAttributeBonus` returns +15 per rank for Puberty (STR), Commission (AGI), Initiation (TEC), and Apprenticeship (INT)
2. `CharacterFacade` extra-attribute bonus includes that value so `GetEffectiveStrength` / HUD / damage / item gates see it
3. Suffix % reference and STR hover **Skill tree** line use the same source
4. Tests: `SkillEffectRankScalingTests`, `StatTooltipFormatterTests`

**Related files:** `SkillEffectRouter.cs`, `CharacterFacade.cs`, `CharacterCombatCalculator.cs`, `EquipmentBonusCalculator.cs`, `StatTooltipFormatter.cs`

### Hybrid skill side rail — Concept A (August 2026)
**Problem:** Hybrid titles (Spellblade, Warbrute, …) existed without a skill UI for secondary-path skills, and showing all four trees was too overwhelming.

**Solutions:**
1. Tag selected secondary-tree nodes with `sharedWith: ["Sword"]` (weapon or class key)
2. When a secondary path exists, `SkillTreeService.GetSharedRailNodes` lists those nodes beside the primary tree
3. `SkillTreeRenderer` draws a magenta **SHARED / {Duo}** rail; detail notes which path’s SP pays
4. Spend still path-tagged (Wand rail node costs Wand SP); non-shared secondary nodes stay `WrongPath` / `NotPrimaryPath`

**Related files:** `SkillTreesConfig.cs`, `SkillTreeService.cs`, `SkillTreeRenderer.cs`, `SkillTreeMenuHandler.cs`, `SkillTreesSheetConverter.cs`, `GameData/SkillTrees.json`

### Bug fix: Return to main menu after character snapshot appeared to quit (July 2026)
**Problem:** After Inventory → Snapshot for Action Lab, returning to the main menu (Game Loop → **0**) did nothing on screen, then another **0** closed the app.

**Root cause:** `GameLoopInputHandler` awaited `SaveCharacterAsync` with `ConfigureAwait(false)`, so `ShowMainMenuEvent` could run off the Avalonia UI thread. `TransitionToState(MainMenu)` still succeeded; paint failed; a second **0** was Quit.

**Solutions:**
1. Use `ConfigureAwait(true)` after save (same contract as `SettingsMenuHandler.SaveGameAsync`)
2. After the snapshot name dialog, `Activate` the owner window and `FocusCanvas`
3. Inventory snapshot capture uses a safe await path instead of bare fire-and-forget
4. Test: `GameLoopInputHandlerTests`

**Related files:** `GameLoopInputHandler.cs`, `Game.cs`, `InventoryMenuHandler.cs`

### UI: Action Lab foes list fills catalog height (July 2026)
**Problem:** The foes column in the Action Lab catalog window stopped after a fixed 10 type rows while the actions column filled down to the window bottom, leaving empty space under the enemy list.

**Solutions:**
1. `ActionLabCatalogRenderer.RenderFoeColumn` sizes type rows from remaining panel height (`panelBottom - y - 1`), matching the actions column
2. Scroll clamp / wheel use `LastEnemyCatalogVisibleRowCount` (fallback `EnemyCatalogVisibleRowCount` before first paint)

**Related files:** `ActionLabCatalogRenderer.cs`, `ActionInteractionLabSession.UiState.cs`, `ActionLabInputCoordinator.cs`

### UI: Action Lab foes + actions secondary window (July 2026)
**Problem:** Even after a two-column tools layout, foe types and the long action catalog still crowded the session tools (snapshots, dungeon, d20, Step/Sim).

**Solutions:**
1. New `ActionLabCatalogWindow` + `ActionLabCatalogRenderer` (foes left, actions right)
2. Tools window keeps only Snapshots / Dungeon / turn / d20 / footer; opens/refreshes/closes the catalog with it
3. Placement docks catalog immediately left of right-anchored tools; closing catalog alone does not exit the lab

**Related files:** `ActionLabCatalogWindow.cs`, `ActionLabCatalogRenderer.cs`, `ActionLabControlsWindow.cs`, `ActionLabControlsRenderer.cs`, `ActionLabWindowPlacement.cs`

### UI: Action Lab two-column tools layout (July 2026)
**Problem:** The Action Lab tools pop-out stacked Snapshots, Dungeon, foe types, turn/d20, and the long action catalog in one narrow column, producing a tall unreadable checklist.

**Solutions:**
1. Widen aux canvas to ~72×54 and default window ~920×960
2. Left column = setup/state (snapshots, dungeon, foe, turn, d20); right = action catalog; full-width footer for Step/Sim/Exit
3. Preserve existing click tokens and session wheel hit-box ranges so input/coordinator stays unchanged

**Related files:** `ActionLabControlsRenderer.cs`, `ActionLabControlsWindow.cs`

### Feature: Action Lab character snapshots + seeded dungeon tooling (July 2026)
**Problem:** Real characters could not be copied into the Action Lab with gear/strip for replaying dungeon layouts; dungeon generation was unseeded and lab d20 was only fixed or free-random.

**Solutions:**
1. `CharacterLabSnapshotService` writes `GameData/LabSnapshots/*.json` (character JSON + combo strip); Inventory **5** and Settings → Testing manage/load
2. `Dungeon(..., generationSeed)` + `RoomGenerator`/`EnemyGenerationManager` RNG plumbing; `ActionLabDungeonFactory` for lab Gen
3. Lab **Seed** d20 stream (`UseSeededD20` / `D20SequenceSeed`) rewound on Reset/room enter; room nav + `ActionLabDungeonSimulator` for batch clears
4. Tests: snapshot round-trip, deterministic generate, seeded d20, dungeon sim smoke

**Related files:** `CharacterLabSnapshotService.cs`, `ActionLabDungeonFactory.cs`, `ActionLabDungeonSimulator.cs`, `ActionInteractionLabSession*.cs`, `Dungeon.cs`, `RoomGenerator.cs`

### Bugfix: Multihit under-counted when target died mid-swing (July 2026)
**Problem:** Slam (or any Multihit) could log `(4 hits) for 45 damage` while a healthy target got the correct product (e.g. `(23 − 1) × 4 = 88`). Early-exit in `MultiHitProcessor` / `AttackActionExecutor` stopped remaining ticks at 0 HP, so the planned hit count and totaled damage diverged.

**Solutions:**
1. Always resolve every planned Multihit tick and sum `totalDamage`; `TakeDamage` already clamps HP at 0 (overkill)
2. Colored attack path uses the planned `multiHitCount` for the `(N hits)` label (not a truncated `actualHits`)
3. Test: `MultiHitTests.TestMultiHitFullDamageWhenTargetDiesMidSwing`

**Related files:** `MultiHitProcessor.cs`, `AttackActionExecutor.cs`, `MultiHitTests.cs`

### Bugfix: Combat roll footer attack/armor looked random (July 2026)
**Problem:** Lines like `attack 23 - 1 armor` beside a multihit total (e.g. **66** from **3 hits**) looked arbitrary — raw and armor were shown without the net or hit multiplier that relates to the damage line.

**Solutions:**
1. `AddAttackVsArmor` / `FormatAttackVsArmorPlain` use `attack: X - Y armor = Z`, and when multihit `… = Z × N`
2. Zero armor omits the DR clause (`attack: X` or `attack: X × N`) so the segment stays consistent with `roll:` / `speed:` / `amp:`
3. Tests: `DamageFormatterTests.TestAddAttackVsArmor`

**Related files:** `DamageFormatter.cs`, `RollInfoFormatter.cs`, `CombatResults.cs`, `COMBAT_LOG_SPACING_STANDARD.md`

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
- Slam also painted redundant **`ACTION (Nx)` / `MULTIHIT +N`** while damage already showed `Nx…` (and ACTION should only label next-action grants)

**Root cause:**
1. Redeemed `ConsumedMultiHitMod` was cleared only at the *start* of the next swing, so strip paint between swings included spent Multihit on every slot
2. Pending bank was relocated onto the recipient as `ACTION`/`MULTIHIT` text, hiding grants on Rapid Strike

**Solutions:**
1. Clear `Consumed*` modifier bonuses at end of `ActionExecutionFlow.Execute`; strip `BuildPanelData` peeks pending Multihit without Consumed*
2. `BuildActionStripModifierTailLines` keeps authored ACTION grant groups on the grantor always; omits pending ACTION/Multihit text on the recipient (Nx damage + cyan shimmer carry the cue)
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

**Related files:** `ActionLabEncounterSimulator`, `GameStateManager`, `SettingsMenuHandler`, `DeathScreenHandler`

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

**Related files:** `Dice`, `DeveloperSimMode`, `HealthBarDeltaDamageHint`, `CombatUiMuteScope`, `ActionExecutor`, `GameTicker`

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

**Related files:** `BlockDisplayManager`, `CharacterSaveService`, `GameStateManager`, `CombatUiMuteScope`, `GameTicker`

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
