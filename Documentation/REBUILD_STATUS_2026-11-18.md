# Rebuild status — overnight run, steps 0–2 (2026-11-18)

Branch `claude/rebuild-steps-0-2-c250d7` (worktree `rebuild-steps-0-2-c250d7`), based on `805ab9cd`. Brief:
[`REBUILD_AGENT_BRIEF_2026-11-18.md`](REBUILD_AGENT_BRIEF_2026-11-18.md); decisions:
[`CORE_DESIGN_PASS_2026-11-17.md`](CORE_DESIGN_PASS_2026-11-17.md). Nothing pushed, no PR, no balance work.

**Baseline before any change:** 15102 passed, **2 failed** (both pre-existing, in the cadence `+25%`
tests filed under `EnemyLevelCaption_ShowsHeroDelta`: "Mid with +25% deals more than unbuffed mid" and
"Finisher gets its own +25%"). Every run below is compared with that.

**Which balance patch runs (important for every number below):** this worktree had no `GameData/PatchProfile.json`
(gitignored), so the first test run created one pointing at the old shipped default `7-30-26`, and the walk showed
193-HP level 1 enemies. Your main checkout runs `night-v47-6b`, which is gitignored too. I copied `night-v47-6b` and
its base chain (`night-v47-1..5`, `lowhp10-l11soft`, `lowhp10`, `slotrise-v46*`) from the main checkout into this
worktree's ignored `GameData/Patches/Balance/`, unchanged, and pointed this worktree's `PatchProfile.json` at it. No
patch file is edited or committed. *With `night-v47-6b` active, the 2 baseline failures pass, so from 1.3 on the
comparison point is 0 failures.

**Test-run side effect (pre-existing):** a full `--run-tests` rewrites `GameData/Patches/Balance/7-30-26.json`.
I restore it with `git checkout` after every run; it never goes into a commit. Worth a fix later.

## Morning summary
**Steps 0, 1 and 2 are done**: 17 sub-steps (0.2–0.4, 1.1–1.9, 2.1–2.5), one commit each plus this wrap-up, and
an MCP walk after each step on this worktree's own build. Last full run: **15305 passed, 0 failed**. Nothing is
pushed. **7 questions** for you at the bottom. Two things I could not check by eye (no screen access overnight):
the board (1.1) and the canvas side of the reveal and item card (2.2, 2.4). Please look at one fight and one dungeon
end first. Every cut system sits behind a switch (listed per sub-step); nothing old was deleted (step 12).

## Summary

| Sub-step | Status | Commit | Tests |
|---|---|---|---|
| 0.2 Dev build flag | done | `4cc9a3dd` | 15128 passed / 2 failed (baseline) |
| 0.3 System unlocks | done | `395338b1` | 15169 passed / 2 failed (baseline) |
| 0.4 Text pipeline + braces | done | `c38b786d` | 15187 passed / 2 failed (baseline) |
| Step 0 MCP walk | done | — | see "Step 0 walk" |
| 1.3 One AMP formula | done | `aa2064d1` | 15196 passed / 0 failed* |
| 1.4 INT 10 routing gate removed | done | `f3fe9d30` | 15196 passed / 0 failed |
| 1.6 Chain rules confirmed | done, 1 question | `05f94848` | ChainRulesConfig 10/10 |
| 1.5 Crit amplifies effects | done | `5d8bd70a` | 15213 passed / 0 failed |
| 1.7 Simple / detailed log | done | `1f99d974` | 15217 passed / 0 failed |
| 1.8 First 60 seconds | done | `cf810c72` | 15220 passed / 0 failed |
| 1.9 Level 1 fights ~2 swings | done | `0b27b695` | 15220 passed / 0 failed |
| 1.2 Audio split | done | `28dc087f` | 15235 passed / 0 failed |
| 1.1 The board | done, not seen on screen | `df423b8a` | 15253 passed / 0 failed |
| Step 1 MCP walk | done | — | see "Step 1 walk" |
| 2.1 Commons are plain | done, 1 question | `ed0a8603` | 15269 passed / 0 failed |
| 2.5 Charms off | done | `16aebda5` | 15273 passed / 0 failed |
| 2.3 No gear changes mid-dungeon | done | `5a18d94a` | 15277 passed / 0 failed |
| 2.4 Dungeon end: staged loot reveal | done, 2 questions | `76f45d1a` | 15293 passed / 0 failed |
| 2.2 Item card reading order | done | `a41b6987` | 15304 passed / 0 failed |
| Step 2 MCP walk + reveal verdict fix | done | (this commit) | 15305 passed / 0 failed |

## Step 0 — Foundation

### 0.2 Dev build flag — done
- New `DevBuild` (`Code/Config/DevBuild.cs`): Debug builds are dev builds, Release builds are player builds;
  `DF_DEV_BUILD=1|0` overrides either way (so a Debug build can be checked as a player sees it).
- The 11 developer states (Testing, DeveloperMenu, BattleStatistics, VariableEditor, TuningParameters,
  ActionEditor, CreateAction, EditAction, ViewAction, DeleteActionConfirmation, ActionInteractionLab) are refused
  by `GameStateManager.TransitionToState` in a player build. Entry points also check it: the settings menu's
  developer option, `DeveloperMenuHandler.ShowDeveloperMenu`, `Game.EnterActionInteractionLabCore`.
- Settings sidebar: the Developer, Balance & Tuning and Testing groups are hidden in a player build
  (`SettingsPanelCatalog.IsDevOnlyGroup`). Player Settings and About stay.
- The clone (D7.9) is dev-only: the death screen (renderer, handler, console fallback), the MCP death choices
  and the Stable's `[R] Clone` all check `DevBuild.AllowsClone`. A player build offers only "leave a tombstone".
- Tests: new `DevBuildTests` (26 checks).
- **Note:** the MCP server and `--run-tests` use Debug builds, so they still see every dev screen. That's
  intended ("they keep working for you").

### 0.3 System unlocks — done
- New `SystemUnlocks` (`Code/Entity/SystemUnlocks.cs`) on `Character.Unlocks`, saved as `systemUnlocks` (a list of
  names) in the character save. The rungs are the ladder's track 1 as a `GameSystem` enum: Board, Items, Combos,
  ActionInteractions, DungeonSelection, Rooms, Bosses, Regions, DiceModifiers, Statuses, Synergies, LootDepth,
  MaterialSets, SkillTree, Stores. Only Board is open on a new character. It also keeps free-form "seen" keys
  (`status:bleed`, `store:rage`) for the NEW cards in step 5 (D9.3), and raises `Unlocked` once per key.
- First events wired now: a dropped or rewarded item (`Character.ReceiveLoot`, used by dungeon rewards and travel
  loot) opens Items, and Synergies / LootDepth by rarity (a lucky early Rare opens its rung early, G1.5); a level-up
  or gear change re-checks strip length (2+ slots → Combos), two worn pieces of one material (→ MaterialSets) and
  an unspent skill point (→ SkillTree). `OnStatusApplied`, `OnBossEncountered`, `OnStoreMinted` exist but are not
  called yet; their steps (4, 5, 8) wire them along with the screens that read them.
- **Old saves:** a save with no `systemUnlocks` gets every rung its state shows (`InferFromCharacter`: items in the
  bag, rarities owned, strip length, worn materials; a level 2+ hero also gets the early rungs, level 5+ bosses and
  regions, level 13+ stores), so an existing hero never loses a screen.
- Nothing reads it yet, so no player-visible change (as step 0 intends). Tests: new `SystemUnlocksTests` (41 checks,
  including a save round trip and a legacy save).

### 0.4 Text pipeline and the leaking braces — done
- New `RuleText` (`Code/UI/RuleText.cs`): `Plain` (authored text → one line; `{n}` × scale, `{lo-hi}` → `lo–hi`,
  stray braces stripped, line breaks collapsed), `Line` (a parsed CrossWires rule → one line), `Lines` (a `;` rule
  source → one line per rule, `{n}` = rolled value) and `SkillNode` (authored effect × power, else its rules, else
  its payoff).
- Routed through it: skill node `DisplayEffect`, the MCP skill tree screen (this was the Phase 1 leak: it printed
  the raw `Effect`, "+{8}% damage"), base modifier text on item cards (`ItemTriggerBundleDisplay.FormatModifier`,
  `ModifierTooltipFormatter`), RULE / CROSSWIRE prefix text (`CrossWires.DescribeMod`) and the skill lines attached
  to convert actions.
- Tests: new `RuleTextTests` (14 checks), including a sweep over all 210 skill nodes and 150 rolled items' full
  tooltips (levels 1–30) asserting no `{` or `}` reaches the card.
- Not changed: the wording of `CrossWires.Describe` itself (the one renderer still owns the sentences).

### Step 0 walk (MCP, this worktree's build) — done
- The `dungeonfighter` MCP server in this session runs the **main checkout** (`.mcp.json` points at its
  `Scripts/mcp-server.ps1`), not this worktree. So I built this worktree with its own `Scripts/mcp-server.ps1
  -BuildOnly` (into `Code/bin/_mcp/`) and drove that `DF.exe MCP` over stdio with a small client
  (`get_agent_context` → recommended choice → `handle_input`), 24 steps from New Game.
- Seen: main menu → Training Ground offer → path intro → weapon choice → the inventory with a random extra item →
  hub → three dungeons → death at level 4 (Dense Brush L4, a Demonic Living Wall Crawler). Level 1 fights were
  **one swing** against 3-HP enemies (D0.4 wants about 2). The death screen offered the clone, labelled "dev build",
  as expected for a Debug build. The item compare card showed the starter Leather Armor's modifier ("CRIT HEAL also
  counts as a [status] action") with no braces; Commons losing their modifier is step 2.

## Step 1 — Rung 1: dice, one slot, the board

### 1.3 One AMP formula — done
- New switch `comboSystem.singleAmpFormula` (default **true**, no patch change). While on: AMP = INT curve × gear AMP
  and slot k deals AMP^(k−1) for heroes and enemies; INT potency (`combat.intelligenceAmpPerPoint`, 0.005 in the
  active chain), the Mythic animal set's AMP bonus, `comboSlotDamagePercentPerStep` and `comboAmplifierLinearSlots`
  are ignored. False brings the old stack back (deleted in step 12).
- Kept: gear AMP mods, cadence AMP_MOD grants and skill-tree AMP (skills are step 9).
- Tests: new `SingleAmpFormulaTests`; `BuildGlueTests.TestIntelligenceAmp` now switches the flag off to keep
  covering the legacy path (its expectation changed with D2.3).

### 1.4 INT 10 routing gate — done
- New switch `comboSystem.intelligenceRoutingGate` (default **false**). `ActionUtilities.UsesOrderedComboSequence` is
  now true for everyone, so strip routing (jump / repeat / loop cards) works at any INT; true restores the gate.
- Tests: the three legacy gate tests in `ActionSequenceTests` now run with the switch on; a new one checks an INT 1
  hero routes by default.

### 1.6 Chain rules confirmed — done (Q1 answered: a plain HIT resets the chain over the patch)
- Checked in code and in the active config: a combo roll earns a free action (`freeActionOnComboHit`), no HIT band
  mid-chain (`missBelowComboMidChain`), +1 per link from slot 2 (`comboThresholdRisePerSlot` 1, start slot 2),
  g = 0.25 (`comboChainGrowth`), and no loop (`CombatTurnHandlerSimplified` ends the chain when the step wraps to
  slot 1; only cards loop). New `ChainRulesConfigTests` pins the code defaults and the active values.
- **Mismatch:** D2.1 says a plain HIT resets the chain "as today", and the code default is `true`, but the active
  chain sets `comboSystem.resetChainOnNormalHit = false` (from `slotrise-v46a`, inherited by `night-v47-6b`). I did
  not change the patch (out of scope). In practice a turn's chain already resets on a failed bonus roll and at the
  strip end, so this only matters when a rule keeps the chain alive across turns (EXEMPT COMBO). See Q1.

### 1.5 Crit amplifies effects — done
- New switch `combat.critAmplifiesEffects` (default **true**). On a critical hit, each status the action itself
  applies lands `CritEffectApplications` times: the crit multiplier rounded (×2 today → twice), capped at 4. Stacking
  statuses get twice the stacks; refresh-only ones (Stun, Silence) just refresh. The extra applications' log lines
  fold into one: "Critical: bleed lands 2 times".
- Scope: the action's own statuses (`CombatEffectsSimplified.ApplyStatusEffects`). Not amplified yet: gear mod
  statuses, trigger bundles, heals and cadence grants. **REVIEW**: D2.5 says "and other effects"; I stopped at
  statuses because the other effect paths merge into one rule engine in step 6, where one crit hook covers them all.
- Crit miss confirmed as already built: `Actor.HasCriticalMissPenalty` / `ActionSpeedSystem` double the action
  length, so a natural 1 is a miss with 2× recovery. Tests: new `CritAmplifiesEffectsTests` (Expose 1 → 2 on a crit,
  switch off → 1, crit-miss length ×2).

### 1.7 Simple / detailed log — done
- New player setting `DetailedCombatLog` (default **off** = the simple log), with a checkbox in Settings → Gameplay
  ("Detailed combat log (full roll math)"). Simple: each swing keeps its headline (who, action, band such as
  CRITICAL, damage) and a short roll line, `(rolled 16)` or `(rolled 16 −2 = 14)`. Detailed: the old footer (attack vs
  armor, defense margin, speed, AMP).
- Not done here: naming the nudge's *cause* in the simple line ("−2 Rattle"). The roll formatter only gets the
  total bonus; the per-source breakdown arrives with the roll ruler (board, 1.1) and statuses (step 5).
- Tests: new `CombatLogLevelTests`; `CombatResultsColoredTextTests`, `CombatResultsTests` and
  `DefenseBlockCalculatorTests` now run with the detailed log on (they check the detailed footer).

### 1.8 The first 60 seconds — done
- New switches in `FirstSixtySeconds` (`Code/Config/FirstSixtySeconds.cs`): `ShowPathIntro` (off), `RandomStartingArmor`
  (off), `StraightIntoFirstDungeon` (on). The old screens stay behind them until step 12.
- Flow now: New → **Training Ground offer (kept, D1.1)** → weapon choice → the **first dungeon starts at once**
  (the lowest-level dungeon on offer; `DungeonSelectionHandler.StartFirstDungeonAsync`). No path intro, no inventory
  screen, no random extra armor. Skipping the offer and finishing the Training Ground both go through the same skip.
  `WeaponSelectionHandler.HandleMenuInput` is now async so MCP's `handle_input` runs the whole first dungeon.
- Weapon choice was already 4 class cards on the canvas (art, class name, weapon stats, starting actions). Each card
  now adds how the class plays its strip and its signature status (also in the MCP weapon labels). **REVIEW:**
  the wording is the design doc's example text (`ClassPresentationConfig.*StripStyle` / `*SignatureStatus`):
  Barbarian "Short and heavy: big hits that build Stagger" / Stagger, Warrior "Steady: Guard setups that turn into
  tempo" / Guard, Rogue "Many quick hits that stack Bleed" / Bleed, Wizard "Composition: cards that change the cards
  after them" / Burn. Guard and Stagger only become real statuses in step 5. No Fernis line (step 3).
- Not done: "name rolled (reroll allowed)". Names are already rolled automatically; there is no reroll button yet.
  See Q2.
- Tests: `GameInitializerTests` now expects an empty starting bag (its expectation changed with the decision).
- MCP walk: New → offer → skip → weapon 1 → the first dungeon ran inside the same `handle_input`, then the
  completion screen. Confirmed three times.

### 1.9 Level 1 fights about 2 swings — done (a data knob, not a solve)
- Cause: the active chain's level 1 enemy HP band is ×0.026 (from `lowhp10`), so level 1 enemies had 3–9 HP and died
  to one swing.
- New knob `enemySystem.levelOneHealthMultiplierFloor` (default **0.2**, 0 = off; code default, no patch edit): a floor
  on the level 1 multiplier only. Level 2+ keep the patch curve (×0.237 at level 2, which already ramps). Picked by
  reading hero hits in the walk (about 9–15, typically 14) against level 1 base HP (about 60–190): 0.2 gives 12–38 HP.
  I tried 0.12 first: still one swing (8–9 HP).
- MCP walk at 0.2, three fresh Barbarians, first dungeon: fights took 2, 2, 2, 3, 1, 1 hero swings. No sims run.
- Tests: new `FirstFightsTests` (switch defaults, the floor raises level 1 only, level 2 unchanged).

### 1.2 Audio split — done
- Hero hits keep their sounds (rising combo notes, the strike jingle on the last slot, crit). Enemy hits on you now
  split: a plain hit plays `Combat_HeroHurt` (as before); an enemy **combo** link plays new cues
  `Combat_EnemyChainStep1..3`, low thuds that deepen for three links and then hold (your chain climbs seven notes),
  at volume 0.6 / 0.65 / 0.7. Unbound, they fall back to `Combat_HeroHurt`.
- Assets: three new synthesized files, `GameData/Audio/SFX/enemy_thud1..3.wav` (a pitch-dropping sine thud with a
  short noise transient). **REVIEW:** placeholders; swap in real impact sounds when you have them. The bindings ship
  in code (`AudioConfig.DefaultBindingFor`) because audio patches are gitignored; your own audio patch still wins.
- Tests: new `AudioSplitTests`. `AudioCueDispatcherTests`' enemy-hit test now uses a plain hit (an enemy combo hit
  plays its thud by design). Also fixed here: `AgentGameplayTests.AdvanceToDungeonSelection` still typed the old
  path-intro and inventory inputs; it now follows the 1.8 flow (offer → weapon → first dungeon → selection).

### 1.1 The board — done in code, **not checked by eye**
The board's numbers live in a new testable model, `ComboBoardModel` (`Code/UI/ComboBoardModel.cs`), and the existing
renderers draw them. I could not look at the Avalonia window tonight (MCP is text-only and screen control needs your
approval), so the visual result is unverified. Please look at one fight first thing.
- **Odds on every card (D2.7):** the card's bottom line (where authored threshold shifts used to show alone) now reads
  `needs 15+ · 30%`: the slot's combo threshold (14, +1 per link) against d20 + the card's roll bonus, a natural 20
  always lands, Lucky (2d20 keep high) counted. Authored shifts (`C:+1` …) follow it. Shown on hero and enemy cards.
- **The hero strip escalates (D0.1):** on the board, cards the chain has already fired stay lit at full brightness
  (they used to dim), their border goes white → gold and 1 → 4 px along the strip. The next card keeps
  `ComboTension`'s glow; the finisher stays gold.
- **The strip collapses on a break:** already built. A failed roll resets the combo step to slot 1, so every lit card
  goes dark at once, and `HeroActionStripFeedback` flashes the card red. No change needed.
- **The enemy strip (D0.11, D9.6):** cards the enemy's chain already fired now stay lit, red → a little gold, 1 → 2 px
  (quieter than yours), and "its chain: N" shows under its strip while it chains.
- **The roll ruler (D2.2):** once the die lands, one row under it shows the five bands, e.g.
  `1 │ 2–5 │ 6–13 │ [14+: 16] │ 20`, with the landed band lit in its color, and `2d20` when Lucky rolled two dice.
  Mid-chain the HIT band is gone, so the ruler drops it. Nudges and the kept die already show in the die's math
  caption on the reveal beat.
- Not done: cards don't grow in *size* (the strip is a fixed grid; thickness and color carry the escalation), and
  the power multiplier per card (×1.0, ×1.3…) isn't a separate line (the swing line already includes slot AMP).
  **REVIEW** whether that reads as "bigger" enough.
- Tests: new `ComboBoardModelTests` (odds, +1 per link, escalation, ruler bands, mid-chain ruler).

### Step 1 walk (MCP, this worktree's build) — done
- A fresh Rogue, 30 steps: New → offer → skip → class cards (with strip style and signature) → the first dungeon ran
  inside the weapon choice → 8 dungeons to level 5, no death. Level 1 fights took 1–4 hero swings (mostly 2); level 5
  fights 5–9. The completion screen and loot are unchanged until step 2.
- Seen: the strip shows **1/2 slots from level 2 with only the starter action**. Nothing fills slot 2 until gear
  brings an action, and with Commons now actionless (2.1) that means the first Uncommon. See Q3.

## Step 2 — Items, the second slot, the dungeon's end

### 2.1 Commons are stats and a material only — done
- New `CommonItemRules` (`Code/Items/CommonItemRules.cs`), switch `lootSystem.itemGeneration.commonsArePlain`
  (default **true**). A rolled Common gets no base modifier, no action lines and no mechanic lines (stat and material
  pools only). Uncommon and up are unchanged.
- **Starters:** keep their action (D5.6) but lose their modifier (D0.7 says the rule change covers "every base,
  starters included"), so the first item card is plain. Every weapon still grants its type's required basic (STAB,
  SLAM…), so a Common weapon never empties the strip.
- **Save migration** (Phase 10 table): every loaded item passes `CommonItemRules.MakePlain` in `ItemTypeConverter`, so
  an old Common keeps its stats and material and loses its modifier and rolled actions; starters keep their action.
- Tests: new `CommonItemRulesTests` (migration, starters, a new hero's kit). `ItemRollerTests` updated where the
  decision changed them: Commons no longer "always an action" or "carry the base modifier", and the "Common = its
  action + one suffix" check is now "stats and a material only".

### 2.5 Charms off — done
- New switch `lootSystem.charmsEnabled` (default **false**). While off: no charm drops (`LootGenerator.TryRollCharmLoot`),
  the charm slot refuses gear ("Charms are off for now."), the inventory paper doll and the MCP slot lists drop the
  charm tile, and a loaded save's equipped charm moves to the bag (the Phase 10 migration row). `Charms.json` and the
  charm code stay for step 12.
- Not hidden yet: the inventory's slot filter still cycles through "charm" (it just finds nothing). Small, left for
  step 12's cleanup.
- Tests: new `CharmsOffTests` (no drops in 400 rolls, the slot refuses, MCP, save migration). The two charm suites
  (`CharmAndAnimalLadderTests`, `LeftPanelTooltipBuilderTests`) now run with charms switched on.
- A `StableManagerTests` play-by-play check failed once in this run's first pass and passed 3/3 on rerun: flaky
  (it depends on a room having a fight), not caused by this change.

### 2.3 No gear changes mid-dungeon; the strip locks — done
- The strip was already locked for the whole run: `ComboInputHandler`, `InventoryComboManager` and the mouse reorder
  all refuse while `GameStateManager.HasCurrentDungeon`. Confirmed, unchanged.
- New: gear locks the same way. `InventoryItemActionHandler` refuses equip (picker, compare, drag), unequip and
  auto-equip inside a run with one line: "Your gear is locked inside a dungeon. Change it after you leave." The dungeon
  stays set on the completion screen until its first input clears it, so the end-of-dungeon Equip / Keep / Salvage
  (which equips directly, not through this handler) and the inventory reached from there both work.
- Loot already arrives only at the end (dungeon rewards are awarded on completion; room searches give consumables).
- Tests: new `DungeonGearLockTests`.

### 2.4 Dungeon end: summary, staged reveal, Equip / Keep / Salvage — done
- New `Code/Game/LootReveal/LootReveal.cs`: `LootRevealBuilder` builds each item's beats and verdict;
  `LootRevealSession` walks them. Switch: `LootRevealSession.Enabled` (default on; off = the old silent bag).
- Flow: the completion summary shows first (stats, XP, level-ups; its loot list becomes "Loot: N items to reveal").
  Then each item reveals in order, Commons first and fast (all their beats at once), the best last. Beats, one per
  Continue (D4.5 draft): Rare+ get "..." first; the drop ("A RARE drops!"); what it is (name, rarity, slot, material,
  tier); **the comparison** against what you wear, one line per attribute, computed on a copy of the hero with the
  item worn ("vs Leather Boots: +4 Defense +7 AGI +3 TEC"); the next set step only ("Bronze 0 → 1 pieces worn",
  "breaks your Leather set (3 → 2): you lose Lucky"); its action (Uncommon+); its modifier; its unique rule
  (Legendary+); then the verdict: Upgrade / Sidegrade (why) / Worse, or "fills an empty slot".
- Then **Equip / Keep / Salvage** (E / K / S, or three buttons on the canvas footer). The suggested choice is
  pre-marked: Equip for an upgrade you can wear, Salvage for a worse Common, else Keep; Continue applies it. Breaking a
  set step makes it a sidegrade (Mock 3: keep Leather for now). After the last item, Continue goes to the hub as before.
- The loot was already in the bag when the screen opens, so Keep leaves it, Equip wears it (old piece to the bag),
  Salvage removes it. **REVIEW / Q4:** Salvage currently just discards.
- MCP: `get_agent_context` shows the reveal as SubScreen `LootReveal` with Continue / skip, then E / K / S choices.
- Not done: the glow and sound per rarity on the drop beat, and timed beats (each beat waits for Continue). Fernis
  reports (D9.2) are step 3. **Not checked by eye** on the canvas; the MCP path was walked (below).
- Tests: new `LootRevealTests` (order, fast Commons, staged Rare, comparison, salvage, equip, the Leather set break).
  `AgentGameplayTests.AdvanceToDungeonSelection` walks the reveal first.
- MCP walk: a Wizard, 4 dungeons: three single-Common reveals, e.g. "vs Leather Armor: −2 Defense +3 AGI +2 INT ·
  Wood 0 → 1 · breaks your Leather set (4 → 3) · Sidegrade: +3 AGI, but −2 Defense" → Keep; two upgrades → Equip.

### 2.2 The item card reading order — done (layout settled 2026-11-18)
- New `ItemCard` (`Code/UI/ItemCard.cs`), switch `ItemCard.Enabled` (default on). Every item tooltip now opens with:
  name; `Uncommon head · Iron · T2`; **the comparison** ("vs Leather Helmet: +17 Defense", one line per attribute,
  computed on a copy of the hero; a worn piece says "Gives: +8 Defense"); "breaks your Leather set (…)" when it
  does; ▸ its action; ◆ its modifier (via `RuleText`); the **next set step only** ("Iron 0/5 — wear 2 to start storing
  CRISIS"); requirements; then "Alt: details". Holding Alt (the existing `HoverTooltipDetailState` toggle) adds the old
  full breakdown under a "Details" header. MCP's `get_item_details` shows card + details.
- Nothing before its rung: the set line waits for the MaterialSets unlock unless this piece would start a set. To make
  that mean something, **starter pieces no longer count toward the MaterialSets first event** (the Leather kit is
  already a set on every new hero, so the rung fired on turn one). Comparisons are cached per item until the worn
  gear changes (hover repaints don't re-copy the hero).
- Tests: new `ItemCardTests`. `ItemTooltipFormatterTests`, `LeftPanelTooltipBuilderTests` and
  `ItemRollerTests.TestCombatImplicitShowsInTooltip` check the pre-2.2 layout and now run with the card off.
- **Not checked by eye** on the canvas (tooltip width, wrapping).

### Step 2 walk (MCP, this worktree's build) — done
- A Warrior and a Wizard, about 10 dungeons. Every dungeon end showed the summary, then "LOOT 1 of 1" with the
  comparison and verdict, then E / K / S. Examples: "vs Bronze Branch: +10 Damage −2 AGI +4 INT → Sidegrade" (kept),
  "vs Leather Helmet: +2 Defense +6 AGI +2 TEC +3 INT → Upgrade" (equipped). An Uncommon staged its beats over 6
  Continues (drop → name → comparison → set step → ▸ action → ◆ modifier → verdict).
- Fixed after the walk: an Uncommon weapon whose only cost was −5 AGI but which brought the hero's **first action for
  the empty second slot** (RELAY STRIKE) was called "Worse". A new action for an empty strip slot now makes it a
  sidegrade ("brings RELAY STRIKE for your empty strip slot, but −5 AGI"). Test added.
- `get_item_details` shows the new card: "Leather Armor · Common body · Leather · T1 · Gives: +8 Defense · Leather
  4/5 — wear 5 for the full set", then Details. **REVIEW:** Leather's own ladder is 3 pieces for Lucky; the card uses
  the generic 2 / 3 / 5 steps for every material.
- Seen but out of scope: Uncommon modifiers still name actions you don't have ("On connect (every 2) with BARRIER");
  self-contained modifiers are step 6. Heroes still die around level 3–4 in the walks (balance comes later).

## Questions for the user (answered 2026-11-18)
- **Q1 (chain reset, D2.1) — answered "isn't this the same thing?" (yes). Done:** the rule now wins over the patch.
  New static code switch `ComboSystemConfig.PlainHitAlwaysResetsChain` (default true; a static, so no patch or overlay
  can set it) makes `ComboSystemConfig.EffectiveResetChainOnNormalHit` true whatever `comboSystem.resetChainOnNormalHit`
  says; combat reads only that. No patch file was edited (`night-v47-6b` still says false; it no longer matters).
  Setting the switch false lets the patch value decide again. `ChainRulesConfigTests` checks the active config and a
  patch value of false.
- **Q2 (first 60 seconds):** the flow names a "name rolled (reroll allowed)" step. Names are rolled today but there
  is no reroll. Do you want a reroll on the class-card screen (one key, one line), or is the auto-rolled name enough?
- **Q3 (the second slot):** with actions starting at Uncommon (D5.6), slot 2 (open from level 2) stays empty until an
  Uncommon drops; every walk showed "1/2 slots" for several dungeons. The ladder has the strip lighting up across two
  cards around level 2–3. Should something guarantee a second action by then (an early Uncommon, a class action at
  level 2, the Training Ground's reward), or is waiting for the first Uncommon the intended pace? (The reveal now
  calls out "brings X for your empty strip slot".)
- **Q4 (salvage):** what does Salvage give? Tonight it discards the item. Phase 4 says "trade-up (5 → 1) stays as the
  salvage sink": should salvaged items feed trade-up instead of vanishing?
- **Q5 (reveal pacing):** beats advance on Continue. Do you want them on a timer (higher rarities slower), with
  Continue as skip? That needs the per-rarity glow and sound you described; I left both for a review pass.
- **Q6 (sets and the starter kit):** the starter Leather kit is already a 4-piece set, so "two pieces of one
  material" fired the MaterialSets rung on every new hero. I made starter pieces not count toward that first event
  (2.2), so the set line waits for found gear. Is that the intent?
- **Q7 (crit amplifies "other effects", D2.5):** I did damage and the action's own statuses (1.5). Should heals,
  cadence grants and trigger effects double on a crit too? The cleanest place is the merged rule engine in step 6.

## REVIEW items (placeholders to look at)
- Class card lines (1.8): strip style and signature status per class, from the design doc's examples.
- Enemy chain thuds (1.2): three synthesized `enemy_thud1..3.wav` placeholders.
- Crit scope (1.5, Q7) and the board's "bigger" cards (1.1: thickness and color, not size).
- The item card's set step uses the generic 2 / 3 / 5 ladder, also for Leather (whose Lucky is at 3).

## Follow-ups noticed (not in scope tonight)
- A full `--run-tests` rewrites `GameData/Patches/Balance/7-30-26.json` (and the active patch file); tests should
  not write patch files.
- `StableManagerTests`' play-by-play check is flaky (depends on a room having a fight).
- The inventory slot filter still cycles through "charm" while charms are off (harmless; step 12).
- Uncommon modifiers still name actions the hero doesn't have (step 6, self-contained modifiers).
