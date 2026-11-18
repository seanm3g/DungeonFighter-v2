# Rebuild status — overnight run, steps 0–2 (2026-11-18)

Branch `claude/rebuild-steps-0-2-c250d7` (worktree `rebuild-steps-0-2-c250d7`), based on `805ab9cd`. Brief:
[`REBUILD_AGENT_BRIEF_2026-11-18.md`](REBUILD_AGENT_BRIEF_2026-11-18.md); decisions:
[`CORE_DESIGN_PASS_2026-11-17.md`](CORE_DESIGN_PASS_2026-11-17.md). Nothing pushed, no PR, no balance work.

**Baseline before any change:** 15102 passed, **2 failed** (both pre-existing, in the cadence `+25%`
tests filed under `EnemyLevelCaption_ShowsHeroDelta`: "Mid with +25% deals more than unbuffed mid" and
"Finisher gets its own +25%"). Every run below is compared with that.

**Test-run side effect (pre-existing):** a full `--run-tests` rewrites `GameData/Patches/Balance/7-30-26.json`.
I restore it with `git checkout` after every run; it never goes into a commit. Worth a fix later.

## Summary

| Sub-step | Status | Commit | Tests |
|---|---|---|---|
| 0.2 Dev build flag | done | `4cc9a3dd` | 15128 passed / 2 failed (baseline) |
| 0.3 System unlocks | done | (this commit) | 15169 passed / 2 failed (baseline) |

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

## Questions for the user
_(none yet)_
