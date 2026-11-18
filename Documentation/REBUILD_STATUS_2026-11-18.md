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
| 1.3 One AMP formula | done | (this commit) | 15196 passed / 0 failed* |

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

## Questions for the user
_(none yet)_
