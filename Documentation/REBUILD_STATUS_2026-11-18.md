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
| 0.2 Dev build flag | done | (this commit) | 15128 passed / 2 failed (baseline) |

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

## Questions for the user
_(none yet)_
