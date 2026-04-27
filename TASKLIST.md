# Tasklist

This file tracks the work currently in progress. Only items listed here should be modified/implemented.

## Active

- [ ] **DevEx / Scripts:** Consolidate `Scripts/` into a small set of core commands (build/run/test/clean/metrics) and move legacy Google Sheets + redundant wrappers into `Scripts/_legacy/`. Update script docs and keep a single entrypoint.
- [ ] **Combat / INT:** Replace INT “+roll bonus per N” with milestone-based threshold bonuses that grant +1 to HIT/COMBO/CRIT at specific effective-INT breakpoints (as per the tuning table). Ensure combat resolution, action selection, and HUD threshold preview all agree; add unit tests.
- [ ] **Loot / Rarity:** Remove **Transcendent** as a rarity category; **Mythic** is the highest. Update loot rarity rolls, item-gen lab fixed chances, color themes, and unit tests accordingly.
- [x] **UI / Item Generation:** Top **Auto** scroll area — **Generation Parameters** (2×4 grid + rarity preview), **lab rarity** + **live tier** strip, **Clear**/**Generate**, and **Affix bounds** in a collapsed **Expander** (inner affix table + notes in a `ScrollViewer` capped at **400** px height). Bottom **`*`** row is only **Generated list** | **Selected** (`ItemGenerationSettingsPanel.axaml`).
- [x] **UI / Item Generation:** Item Generation tab uses a **2-column** list row (**Generated list** | **Selected Item**); **Generate** and **Clear** live in the **top** control strip (supersedes earlier “Generate in Selected only” layout).
- [ ] **UI / Item Generation:** Add a **Statistics** section near “Selected Item” that shows counts for each **tier × rarity** combination in the current generated batch, plus how many generated items are **duplicates** (repeats of an identical item in the batch).
- [ ] **UI / Item Generation:** Fixed rarity chances — add **Base ratio** input so each rarity is X× rarer than the previous, and auto-derive normalized percentages (Common→Mythic) for the lab.
- [x] **UI / Item Generation:** Fixed rarity chance boxes — improve hover readability (remove hard-coded TextBox colors so hover style applies; ensure text stays high-contrast).
- [x] **UI / Item Generation:** Remove the top “Item Generation Lab” banner section in Settings → Item Generation (title + subtitle card).
- [x] **UI / Item Generation:** Change **Dungeon level** input to **Dungeon Δ** (relative to hero). Enter `+5` so hero 10 implies dungeon 15; clamp final dungeon level 1–99; update preview copy + tests.
- [x] **UI / Item Generation:** Move **Hero / dungeon** inputs into the top parameter rows so the rarity + tier preview tables can use the full panel width.
- [x] **UI / Item Generation:** Rename affix-bounds group labels to **Prefix / Suffix / Actions** (keep helper copy clarifying **Suffix** = `StatBonuses.json` suffix lines, distinct from prefix-slot modifications that can also change stats).
- [x] **UI / Item Generation:** Clarify affix table column headers (prefix slots vs stat-bonus suffix lines vs action bonus lines) so abbreviations aren’t misread as “suffix” in general.
- [x] **UI / Item Generation:** Tighten copy so “Stat bonus suffixes” reads as **StatBonuses lines** (distinct from modification/prefix-driven stat effects).
- [x] **UI / Item Generation:** Widen the “Affix bounds by rarity” grid so column labels and values don’t clip/abbreviate in the Settings → Item Generation tab.
- [x] **UI / Item Generation:** Generation Parameters panel — cleaner layout (three equal columns, fixed-width count/hero level, rarity preview callout, actions right-aligned).
- [x] **UI / Item Generation:** Tier roll preview under rarity summary (loot level from hero vs dungeon 1, `LootTierCalculator.GetTierRollPreview`; tests in `LootTierCalculatorTests`).
- [x] **Item Generation Lab:** When **Tier** is **Any**, batch generation rolls tier with `LootTierCalculator` (hero + `DungeonLevel` → loot level → `TierDistribution.json`), same as `LootGenerator` — not uniform 1–5. Tests: `ItemGenerationLabServiceTests.TestAnyTierUsesTierDistributionAtLootLevel1` / `AtLootLevel19`.
- [x] **UI / Item Generation:** Dungeon Δ input added next to hero level; tier preview and generation use hero-vs-(hero+Δ) for loot level and tier rolls.
- [x] **UI / Item Generation:** Fixed rarity chance inputs (Common→Transcendent) to control lab rarity rolls when Rarity is Any (ignores level gating). Tests: `ItemGenerationLabServiceTests.TestFixedRarityChancesOverridesAnyAndIgnoresLevelGates`.

## Completed (reference)

- [x] **UI / Dungeon selection:** Custom difficulty — typed dungeon level shows inline on the **Custom difficulty** row (`Level:` + digits + `_` cursor); removed full-screen `ShowMessage` for this flow; validation uses bottom non-blocking messages (`DungeonSelectionHandler`, `DungeonSelectionRenderer`, `CanvasUICoordinator`).
- [x] **Data / New game:** Only **four** starting weapons on the pick-one menu — removed duplicate **starter** tag from the second identical Wand **STICK** row in `Weapons.json`; `OVERVIEW.md` documents starter-tag vs tier-1 fallback.
- [x] **Loot / rarity weights:** Set `RarityTable.json` weights to pure powers of 10 (Common→Transcendent) for clean “1 in N” mental math; document resulting odds.
- [x] **Bugfix / items:** First stat suffix duplicated in display names when two `StatBonuses` names had the same length (`GetBaseItemName` only stripped the outer suffix in one pass). Fixed with iterative stripping; test `ItemGeneratorTests` equal-length suffix idempotency case.
- [x] **UI / Item Generation Lab:** Rarity **Any** rolls per item with `LootRarityProcessor` (fixes 100% Common bug). **Hero level** UI + `ItemGenerationSpec.PlayerLevel` for those rolls and Epic+ gates; `ItemGenerationLabServiceTests.TestAnyRarityRollsNonCommonDistribution`.
- [x] **Loot / probabilistic affix tuning:** Per-rarity **minimum** prefix slots, stat suffixes, and action bonuses in `TuningConfig.json` (`itemAffixByRarity`) with optional **extra chance** (0–100%) and **max** caps; `LootBonusApplier` rolls final counts each drop. Settings **Item Generation** tab grid for load/save (`ItemGenerationPanelHandler`, `ItemAffixRollRule`, `RollAxis`). Tests: `LootBonusApplierTests.TestProbabilisticAffixTuningHitsMax`, `ItemConfigTests` (extra JSON + `RollAxis`). `OVERVIEW.md`.

- [x] **Item Generation Lab / names:** Triple prefix in the lab (and any double call to `GenerateItemNameWithBonuses`) fixed: `ItemGenerator.GetBaseItemName` strips leading prefix-slot words and trailing suffix-style tokens so name assembly is idempotent; `ItemGenerationLabService` no longer re-runs name generation after `LootBonusApplier.ApplyBonuses`. Test: `ItemGeneratorTests` idempotency case.
- [x] **Settings / Item Generation Lab:** New Settings tab “Item Generation” with forced generation controls (rarity/tier/weapon type/armor slot/count) and a generated list sorted best→worst (rarity → tier → primary stats). Code: `ItemGenerationSettingsPanel`, `ItemGenerationLabService`. Tests: `ItemGenerationLabServiceTests`. `OVERVIEW.md` updated.
- [x] **Loot / item prefixes:** Three-slot prefix system (Adjective / Material / Quality) with rarity rules (Common 1 random slot, Uncommon 2 random, Rare+ all three), ordered naming, `gearPrimaryStatMultiplier` for quality, material attribute effects on equipment stats, merged `PrefixMaterialQuality.json`, Divine reroll resolves to a replacement adjective, `RarityTable.json` prefix counts cleared. Code: `ItemPrefixHelper`, `LootBonusApplier`, `Item.cs`, `EquipmentBonusCalculator`, `LootDataCache`, `ItemGenerator`, `ItemNameParser`, lab clone `PrefixCategory`. Tests: `ItemPrefixHelperTests`, `ContextualLootTest` adjective-only thematic count; `OVERVIEW.md`.

- [x] **UI / Settings — tags visibility:** Actions tab — selected-action runtime tags preview (`ActionsSettingsPanel`, `ActionsTabManager`). Items — `TagsSummary` on weapon/armor rows, **Tags** field in `ItemEditDialog`, load/save/rename via `ItemsDataCoordinator` / `ItemsTabManager` + `GameDataTagHelper.ParseCommaSeparatedTags`. Enemies — new **Enemies** category (`EnemiesSettingsPanel`, `EnemiesTabManager`, `EnemiesDataService`), save path in `SettingsSaveOrchestrator` + `EnemyLoader.LoadEnemies()` after write. Tests: `GameDataTagHelperTests`; `OVERVIEW.md`.

- [x] **Data / Sheets — selective push:** Per-tab OAuth push toggles in `SheetsPushConfig.json` (`pushActionsTab`, `pushWeaponsTab`, …); `GameDataSheetsPushService` / preflight respect flags; Balance Tuning panel checkboxes load/save with `SheetsPushConfig.Load` migration for legacy JSON (missing keys → push enabled); tests `SheetsPushConfigTests` (`ApplyMissingPushTabDefaults`); `SheetsPushConfig.template.json`; `OVERVIEW.md`.

- [x] **Data / tags:** ACTIONS sheet **`TAGS`** row-2 column ingests into `SpreadsheetActionData.Tags`; optional **`tags`** on `Weapons.json` / `Armor.json` / `Enemies.json` (sheet columns + JSON arrays); `ItemGenerator` copies catalog tags to `Item.Tags`; `GameDataTagHelper`; validators warn on empty tag entries; tests `SpreadsheetActionDataSheetRowSerializerTests.TestTagsColumn_IngestsAndConverts`, `JsonArraySheetConverterTests` (weapons/armor/enemies tags round-trips), `ItemGeneratorTests` (copy tags); `OVERVIEW.md`, `Documentation/01-Core/OVERVIEW.md`.

- [x] **Saves / death:** On player death, persist tombstone (`isDead`, `character_*_dead.json`), remove live save, exclude from load list and refuse explicit load; clear-all removes dead files; tests `SaveLoadSystemTests.TestDeadCharacterTombstone`; `OVERVIEW.md` (Saves — death tombstones).

- [x] **UI / combat log:** Damage lines render the **hits** keyword in **white** (including multi-hit `(N hits)` wording); `CombatColorStrategy.GetHitsColor`, `DamageFormatter.FormatDamageDisplayColored`; `OVERVIEW.md`.

- [x] **UI / HUD:** Left HERO panel — hero **name** uses per-glyph class color cycles once the HUD title leaves the default **Fighter** (no-points / pre–tier-1 gate) state; palettes for each solo weapon path and each hybrid pair (top two paths by points). `HeroNamePanelColoredText`, `CharacterPanelRenderer`; tests `HeroNamePanelColoredTextTests`; `OVERVIEW.md` (HUD — left panel hero name).

- [x] **Bugfix / combat:** Below INT 10, combo-path strip index no longer rolled a random slot each swing (strip showed slot 0 e.g. **CAST** while combat could resolve **STAB**); `ActionUtilities.ResolveComboStripIndex` now uses `ComboStep % count` when salt is null, matching HUD and encounter reset. `ActionSequenceTests.TestComboStripIndexRespectsIntelligenceThreshold`; `OVERVIEW.md` combo bullet.

- [x] **Bugfix / combat:** Combo AMP tiers follow **strip slot index** (first action 1.00×, second = TECH baseline, then scaling); opener/finisher flags no longer invert tiers vs sequence. `ActionUtilities.GetComboAmplificationExponent`, `TryGetComboActionSlotIndex`; tests `ActionUtilitiesTests`, `ComboExecutionTests`; `OVERVIEW.md`, tooltip copy.

- [x] **Combat / state:** Hero status effects and temp combat state wipe after each combat, after each survived room, and on dungeon completion or early exit (`CombatManager`, `RoomProcessor`, `DungeonOrchestrator`); `Actor.ClearAllTempEffects` extended for advanced stack fields; `CharacterFacade.ClearAllTempEffects` → `Character.ClearAllTempEffects`; tests `ActorClearTempEffectsTests`; `OVERVIEW.md`.

- [x] **Bugfix / UI:** Inventory — hover tooltips for bag rows (and left character panel while inventory is open): `InventoryScreenRenderer` no longer clears shared clickables after the character panel registered `lphover` targets; inventory `lphover` uses a full chrome refresh so tooltip paint does not erase the item list; `LeftPanelTooltipBuilder.AppendGear` adds an **Actions:** line from `GearActionNames.Resolve` via `GetGearActions`; tests `LeftPanelTooltipBuilderTests`; `OVERVIEW.md`.

- [x] **UI / Inventory:** Right panel **INVENTORY** list under **POOL** — actions from bag items (`InventoryActionPoolEntries`); row click equips (slot swap, old piece to bag) via `cpi:invpool:N` + `InventoryComboManager` / `InventoryItemActionHandler`; adds that action to the sequence when a matching combo pool instance exists. Combo management center panel mirrors the block. Tests: `ComboPointerInputTests`, `InventoryActionPoolEntriesTests`; layout reserve synced in `InventoryRightPanelLayout`; `OVERVIEW.md`.

- [x] **Data / New game:** Starter weapons unified with `Weapons.json` — menu + equipped item use first tier-1 row per class path; `StartingGear.json` armor-only; `StartingGearLoader`, `GameInitializer`, weapon UI/lab dialog, tests, `OVERVIEW.md`.

- [x] **UI:** Action Lab gear editor — **Tier** filter on weapon, head, body, and feet dialogs (`ActionLabWeaponEditDialog`, `ActionLabGearCatalogFilter.ItemMatchesTierFilter`, tests in `ActionInteractionLabTests.ActionLabGearCatalogFilter_Basics`); `OVERVIEW.md`

- [x] **Bugfix / combat:** Combo-strip vs normal selection used preview attack total and ACC-shifted threshold, so a low d20 (e.g. 9) could still execute a **14+** combo special. Selection now compares the modified die only to the threshold with COMBO adjustments but **without** ACC lowering the gate; INT/stat roll bonuses no longer bypass the die. `ActionSelector`, `ActionSelectorRollBasedTests`, `OVERVIEW.md` (combo bullet), `LeftPanelTooltipBuilder` (combo threshold tip).

- [x] **Bugfix / combat:** Weapon required basic was not in the pool when a weapon only specified other actions (`GearAction` / bonuses), so `WeaponRequiredComboAction` could not protect or re-inject it. `GearActionNames.Resolve` now prepends the resolved required basic when missing; test `GearActionNamesTests.TestWeaponWithGearActionOnly_IncludesRequiredBasicName`; `OVERVIEW.md` (combo bullet).

- [x] **UI:** Action Lab armor gear (head / body / feet) — **Armor class** + **Rarity** filters aligned with weapon dialog behavior (`ActionLabWeaponEditDialog`, `ActionLabGearCatalogFilter`, tests `ActionInteractionLabTests.ActionLabGearCatalogFilter_Basics`); `OVERVIEW.md`

- [x] **Combat / DoT:** Poison, burn, and bleed from weapon mods (`weaponPoison` / `weaponBurn` / `weaponBleed`), weapon `StatusEffects`, and unconditional action flags apply on **critical hit** only (`CombatEffectsSimplified`); UI strings; tests `CombatEffectsSimplifiedTests`, `CombatResultsTests`; `OVERVIEW.md`

- [x] **Progression / XP:** Dungeon-paced level bars — one tier-1 dungeon completion to L2, then ~1.5, 2, 3, 4… completions worth per level (`CharacterProgression.GetXpRequiredToAdvanceFromLevel`, `GetBaseXpForContentLevel`, `GetStandardDungeonCompletionXpForLevel`, `GetExpectedDungeonsToLevelFromLevel`); `XPRewardSystem` / `RewardManager` share completion math; tuning docs (`ProgressionConfig`, variable editor), `OVERVIEW.md`, `XPSystemTests.TestDungeonPacedXpRequirementCurve`.

- [x] **Bugfix / UI:** Combat log — no blank line between a **DoT tick on the hero** (burn/poison/bleed block) and that hero’s **next** combat action; `TextSpacingSystem` tracks `lastDoTAfflictedEntity` on `RecordBlockDisplayed(PoisonDamage, …)`, `StatusEffectProcessor` passes `entity.Name`; test `TextSpacingSystemTests.TestDoTSpacingRules`; `OVERVIEW.md` (DoT — combat log spacing).

- [x] **Progression / bugfix:** Mid-dungeon `AddXP` double-counted `Level` (progression + `LevelUpManager.LevelUp`), so one threshold could show “level 3” with only one class point. Fixed `CharacterFacade.AddXP`; multi-step `AddXPWithLevelUpInfo` now passes per-step display level. **XP curve** wired to `BaseXPToLevel2` + `XPScalingFactor` (flat default). Tests: `XPSystemTests`, `MultiSourceXPRewardTests`, `LevelUpSystemTests`, `GameplayFlowTests`; `OVERVIEW.md` (Progression — XP subsection).

- [x] **Bugfix / UI:** Right panel LOCATION — dungeon/room/enemy names use full inner panel width for ellipsis (fixes **Tomb of the forgotten** truncated at 20 chars). `RightPanelContentText`, `RightPanelRenderer`, tests `RightPanelContentTextTests`; `OVERVIEW.md`

- [x] **Bugfix / combat:** Enemies below combo threshold no longer fall back to the first combo-flagged special when the pool has no non-combo basics; they use the same unnamed synthetic normal attack as heroes. Enemy combo-threshold preview uses the resolved strip slot (matches hero logic). `ActionSelector`, tests `ActionSelectorRollBasedTests.TestEnemyComboOnlyPoolBelowThresholdIsUnnamedNormal`, `OVERVIEW.md`

- [x] **Combat / combo:** Reset full hero combo state (`ResetCombo`) at new encounter init, new dungeon room, and dungeon start; enemy reset on encounter init; `RunCombat` uses `ResetCombo` instead of only `ComboStep = 0`; test `CombatStateManagerTests.TestInitializeCombatEntitiesResetsPlayerAndEnemyCombo`; `OVERVIEW.md`

- [x] **UI / Feature:** Inventory — **right-click** a filled **action strip** card removes that combo sequence slot (same as `cpi:rm`; blocked during equip/compare/trade prompts; weapon-required basics protected). `InventoryMenuHandler.TryHandleStripRightClickRemove`, `GameCoordinator.TryHandleInventoryStripRightClickRemove`, `MouseInteractionHandler.TryHandleActionStripRightClickRemove`; tests `InventoryMenuStripRightClickTests`; `OVERVIEW.md`

- [x] **Bugfix / combat:** Low-INT chaotic combo-strip pick called `Dice.Roll(1, stripCount)`; with **one** combo action that became invalid `1d1` and aborted the dungeon at combat start. Fixed in `ActionUtilities.ResolveComboStripIndex`; regression `ActionSequenceTests.TestComboStripIndexSingleSlotLowIntDoesNotThrow`; `OVERVIEW.md` (combo bullet).

- [x] **Data:** `GameData/Enemies.json` — every enemy now has full **`baseAttributes`** (STR/AGI/TEC/INT) and explicit four-stat **`growthPerLevel`**; legacy root stat keys removed (weights were merged into `growthPerLevel` so normalized 6pt/level behavior matches prior fold); `OVERVIEW.md` already describes base + growth.

- [x] **Data / Sheets:** Remove enemy `overrides` from the model and ENEMIES sheet; **STR+AGI+TEC+INT** `growthPerLevel` normalized to **6 points/level** (`EnemyStatCalculator`, `EnemyDataPostLoad` for legacy `overrides.health`); legacy root stats fold into **growth only**; canonical columns **A–P**; tests `EnemyAttributeGrowthTests`, `JsonArraySheetConverterTests`, `TuningSystemTest`; `OVERVIEW.md`

- [x] **Data / Sheets:** ENEMIES legacy root stats (`strength`, `agility`, …) fold into `growthPerLevel` on load (historically also `overrides`); CSV import hoists them; push maps roots into canonical columns (`EnemyData.ExtensionData`, `EnemyDataLegacyRootStats`, `JsonArraySheetConverter`); tests `JsonArraySheetConverterTests`, `EnemyAttributeGrowthTests`; `OVERVIEW.md`

- [x] **Bugfix / data:** Partial `baseAttributes` / `growthPerLevel` in `Enemies.json` no longer zeroed unspecified stats (nullable `EnemyAttributeSet` + `EnemyStatCalculator` docs); regression `EnemyAttributeGrowthTests.TestPartialBaseAttributesFallsBackForOmittedStats`; `OVERVIEW.md`

- [x] **UI:** Action Lab — right-panel enemy **level** line shows **hero delta** (`Lvl 9 (-2)` when hero is 11); `ActionLabRightPanelEnemyAdjustment.FormatEnemyLevelCaptionWithHeroDelta`, `RightPanelRenderer`; test `ActionInteractionLabTests.EnemyLevelCaption_ShowsHeroDelta`; `OVERVIEW.md`

- [x] **UI / Feature:** Action Lab tools — **[ Reset ]** (was “Reset combo”): clears combat log and step/sim counters, refills HP, clears status/temp effects, zeros combo steps; keeps gear, combo strip, and current enemy (`ActionInteractionLabSession.ResetLabEncounterAsync`, `ActionLabControlsRenderer`, `ActionLabInputCoordinator`); tests `ActionInteractionLabTests`; `OVERVIEW.md`

- [x] **Combat / combo:** INT-gated combo strip — effective INT &lt; 10 randomizes which combo action is used on combo-path selection; INT ≥ `GameConstants.ComboSequenceIntelligenceThreshold` (10) keeps `ComboStep` order (`ActionUtilities.ResolveComboStripIndex`, `ActionSelector`, hero ACTION cadence slot from selected action in `ActionExecutionFlow`). **Low INT also skips `ComboRouter` on `Character.IncrementComboStep`** (linear next slot only). Tests: `ActionSequenceTests` (INT threshold + pending slot + `TestLowIntIgnoresComboRoutingOnComboStepAdvance`), `ActionInteractionLabTests.EnemyComboSelectionUsesComboStepIndex`; `OVERVIEW.md`

- [x] **Bugfix / UI:** Hero threshold panel — queued ACC/COMBO/HIT preview no longer adds stat roll bonus to threshold shifts (matches combat: roll bonus on attack total, FIFO ACC on thresholds); `ClampDiceLadderDisplayValue` floors display; `ThresholdDisplayFormattingTests`; `OVERVIEW.md`

- [x] **UI / Feature:** Action Lab — **hero level** (left panel) applies the same level delta to the **lab enemy** when the hero’s level changes; **enemy level** (right panel) stays independent (`ActionLabLeftPanelStatAdjustment`, `ApplyLabEnemyLevelDelta`); test `ActionInteractionLabTests.LeftPanelHeroLevelSyncsLabEnemy_EnemyRowIndependent`; `OVERVIEW.md`

- [x] **Combat / combo:** Per **weapon type**, one **required basic** action must stay in the player’s combo sequence (`weapon_basic` + `weaponTypes` in `Actions.json`; `WeaponRequiredComboAction`, combo restore/gear rebuild ensure; removal blocked unless a duplicate copy remains). Tests: `WeaponRequiredComboActionTests`; `EntitySystemTestRunner`; `OVERVIEW.md`

- [x] **UI / Feature:** Action Lab tools — **Actions taken** counter (`LabTotalActionTicks`, batch-sim turn sum, reset on fight history clear); `ActionLabControlsRenderer`, `ActionLabInputCoordinator`, `ActionInteractionLabSession`; tests `ActionInteractionLabTests.LabTotalActionTicks_StepUndoSimAndFightReset`; `OVERVIEW.md`

- [x] **Bugfix:** Weapon poison (and other on-hit status effects) applied **twice** when using the colored combat path — `ActionExecutionFlow` already called `ApplyStatusEffects`; `ExecuteActionWithStatusEffectsColored` called it again. UI now parses `StatusEffectMessages` only; regression test in `CombatResultsTests`; `OVERVIEW.md` DoT note

- [x] **UI:** Action Lab weapon dialog — **Weapon type** + **Rarity** filters (prefix/suffix/weapon lists); optional `StatBonus.ItemRank`; `ActionLabGearCatalogFilter` + tests; `OVERVIEW.md`

- [x] **UI:** Action Lab open — **multi-window placement**: center main window on its monitor, right-anchor lab tools in working area, left-nudge pop-out settings on same monitor; tests `ActionLabWindowPlacementTests`; `OVERVIEW.md`

- [x] **UI:** Action Lab gear editor dialog — default ~1000px height, user-resizable, grid layout so weapon/prefix/suffix lists share vertical space (`ActionLabWeaponEditDialog`); `OVERVIEW.md`

- [x] **Feature:** Action Lab gear editor — **multi-select** prefixes (modifications) and suffixes (stat bonuses) when right-clicking gear; `ActionLabWeaponEditDialog`, `ActionLabWeaponFactory` / `ActionLabArmorFactory` list overloads; tests + `OVERVIEW.md`

- [x] **UI:** Right combat panel — under Status Effects, show **Immune: Bleed, Poison** when enemy template has `isLiving` false (`Enemy.IsLiving`); tests + `OVERVIEW.md`

- [x] **Combat / data:** `weaponPoison` roll ranges — **Venomous** `MinValue`/`MaxValue` **1–2** and **Venomous Blade** **2–3** in `Modifications.json` (was 1/1 for both, so Blade only added **+1%** per hit instead of **+2–3%**); `OVERVIEW.md` DoT note

- [x] **Data / Sheets:** Item suffixes tab — `statBonusesSheetUrl` / `statBonusesSheetTabName`, `GameDataTabularSheetKind.StatBonuses`, pull/push `StatBonuses.json`; Balance Tuning gid field; `SheetsConfig.json` gid `388294050`; tests + `OVERVIEW.md`

- [x] **Combat:** Poison / burn / bleed revamp — poison as % of max HP (monotone on apply); burn intensity + pending on 5s game tick; bleed same intensity math on actor turn end; deterministic weapon mods; handlers, UI lines, `Modifications.json`, tests + `OVERVIEW.md` DoT subsection

- [x] **Data / Sheets:** Dungeons tab — `dungeonsSheetUrl` / `dungeonsSheetTabName`, `GameDataTabularSheetKind.Dungeons`, pull/push `Dungeons.json`, pipe-separated `possibleEnemies` on import; tests + `OVERVIEW.md`

- [x] **Data / Sheets:** Enemies & environments spreadsheet import support
  - [x] `JsonArraySheetConverter`: full `EnemyData`-aligned canonical headers; `enemies` on environments; pipe-separated `actions` normalization for ENEMIES CSV
  - [x] `RoomData` + `RoomEnemyData`, `RoomLoader` → `Environment` → `EnemyGenerationManager` weighted room spawn pool; `RoomDataValidator` + `ReferenceValidator` for `enemies` references
  - [x] Tests: `JsonArraySheetConverterTests` (full enemy stats round-trip, pipe actions, environments with `enemies`)
  - [x] Docs: `OVERVIEW.md` (Sheets subsection) + this task list

- [x] **UI:** Action Lab controls pop-out — show more enemy type rows, fewer catalog rows (`ActionLabControlsRenderer`).

- [x] **Feature:** Action Lab — interactive enemy level on right combat panel
  - [x] Session: `_labEnemyBaseLevel`, `_labPanelEnemyLevelDelta`, `ApplyLabEnemyLevelDelta`, `BuildLabEnemyFromPanelState`, restore + `CaptureSimulationSnapshot`
  - [x] UI: `Lvl` row + `rphover:enemy:level` click target when rendering lab combat; DMG/Spd lines aligned with direct-stat enemies
  - [x] Input: `ActionLabRightPanelEnemyAdjustment` + `MouseInteractionHandler` left/right click
  - [x] `TestCharacterFactory.CreateTestEnemy(..., level)` scaling; `Enemy.UsesDirectCombatStats` / `GetEffectiveStrength` for direct damage
  - [x] Tests: `ActionInteractionLabTests`, `ActionLabEncounterSimulatorTests`
  - [x] Docs: `OVERVIEW.md` + this task list

- [x] **Data:** Enemy + environment actions in `GameData/Actions.json` (spreadsheet format) with `enemy` / `environment` tags; loader and weapon fallbacks exclude them from the hero.

- [x] **Bugfix:** Hero base stats “MultiHit mod” applies to the next action (see `OVERVIEW.md` milestone note)

