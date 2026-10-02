# Problem Solutions - DungeonFighter

This document contains solutions to common problems encountered during development. Use this as a quick reference when similar issues arise.

## Recent Fixes

### UI: Ctrl+/- zoom grows side panels and crushes the center (October 2026)
**Problem:** Raising UI size (e.g. Courier New at 112%) made the left/right chrome dominate the window and squeezed the center narrative panel; top action cards also grew with zoom.

**Root cause:** Zoom multiplied glyph/cell size while left/right stayed at fixed character widths (32/30) and the action-info strip stayed at 11 rows, so chrome took a larger share of the fewer columns that still fit.

**Solution:** `LayoutConstants.UpdateUiZoom` shrinks left/right column counts and action-info strip rows inversely with zoom so their pixel footprints stay near the 100% design size; `CENTER_PANEL_WIDTH` / height absorb the leftover. `GameCanvasControl` passes `GameFonts.ActiveZoom` on measure/layout.

**Related files:** `LayoutConstants.cs`, `GameCanvasControl.cs`, `CharacterPanelRenderer.cs`, `EffectiveVisibleWidthRegressionTests.cs`

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

### UI: click-burst glyph spin flings letters off-screen (October 2026)
**Problem:** Exploded glyphs looked like they were thrown up/out of frame even with a small explode strength (e.g. 3 cells). Equipment lines showed huge holes mid-word.

**Root cause:** Avalonia matrices use row-vector composition (`p' = p * M`). Burst/header glyph rotation built `T(C) * R * T(-C)`, which rotates around world `(0,0)` instead of the glyph center — same class of bug as the wake debug oval.

**Solution:** Rotate with `T(-C) * R * T(C)` via `CanvasPrimitivesRenderer.CreateRotateAboutPointTransform`. Letters now spin in place around their small scatter offset.

**Related files:** `CanvasPrimitivesRenderer.cs`, `CanvasPrimitiveStackingTests.cs`

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

**Solution:** Ignore sub-deadzone motion; after a short idle grace, **hard-zero** wind/speed and clear the trail; snap sampled offsets below 0.5px to rest; raise chromatic idle epsilon so faint ghosts cannot smear pixel fonts; paint one final clean frame when the settle timer goes inactive.

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

### Bug fix: luck 18 showed a normal hit instead of the named action (August 2026)
**Problem:** Combat log could show `2d20 luck 18/5 → 18` with `and hits for N damage` (unnamed) instead of `and hits with {ACTION}`.

**Root cause:** Combo-strip vs unnamed normal was chosen from the first d20, then luck/naiveté rolled a second die for display only (and luck was sometimes rolled twice). A 5 that luck-kept 18 still executed the synthetic normal.

**Solutions:**
1. Action selection no longer rolls luck; the unforced second d20 is consumed once during resolution
2. After luck (and after naiveté miss→advantage), reconcile the selected action to that kept face vs the combo threshold
3. Tests: `ActionExecutionFlowTests`, `NaiveteThresholdBonusesTests`, `ActionSelectorRollBasedTests`

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
