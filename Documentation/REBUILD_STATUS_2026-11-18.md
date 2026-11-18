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
patch file is edited or committed. \*With `night-v47-6b` active, the 2 baseline failures pass, so from 1.3 on the
comparison point is 0 failures.

**Test-run side effect (pre-existing):** a full `--run-tests` rewrites `GameData/Patches/Balance/7-30-26.json`.
I restore it with `git checkout` after every run; it never goes into a commit. Worth a fix later.

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
| 2.1 Commons are plain | done, 1 question | (this commit) | 15269 passed / 0 failed |

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

### 1.6 Chain rules confirmed — done, with one question
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

## Questions for the user
- **Q1 (D2.1):** the active patch chain turns `comboSystem.resetChainOnNormalHit` off. Should a plain HIT reset the
  chain as D2.1 says? If yes, the fix is one line in the next patch (`"resetChainOnNormalHit": true`) or I can make
  the rule code-only and ignore the patch value. Which do you want?
