# Combat readability playtest — 2026-09-05

Executed the actual headless `CombatManager` through `BattleExecutor.RunSingleBattleWithWeapon`, using a separately compiled harness referencing the verification build. No gameplay balance, live saves, or live game configuration was changed; the running DF process was left alone. Native visual timing and sound were not assessed by these accelerated simulations.

## Final sample

The final run used a copied active balance patch **7-1-2026**, confirmed by the `BALANCE` path in `PlaytestHarness/run.log`. Each of Sword, Mace, Dagger, and Wand fought Wolf, Skeleton, and Lava Golem at equal levels 1 and 3: 30 fights per matchup, **720 fights with zero reported errors**. Raw results are in `PlaytestHarness/results.json`.

Pooled across the three enemies (90 fights per weapon and level):

- Level 1: Mace won 89/90, averaging 10.96 actions/turn-count units; Sword 84/90, 14.74; Wand 80/90, 14.86; Dagger 70/90, 16.58.
- Level 3: Mace won 90/90, averaging 6.74; Sword 86/90, 8.40; Wand 87/90, 8.43; Dagger 89/90, 8.21.

The factory produces unarmored characters, tier-1 weapons, and one class action each (STRIKE, SLAM, STAB, MAGIC MISSLE). It does not reproduce a progressed player's acquired equipment or complete combo loadout. All three loaded enemy samples had armor 1 and 150 HP under this configuration, so this is **not evidence about a highly armored matchup**. Skeleton and Lava Golem are nonliving; the one-action baseline does not establish how status-focused builds fare against their immunities.

The L1 Mace/Dagger difference is a useful follow-up target, not enough evidence to change balance. The level-3 convergence, gear omissions, 30-fight samples, and uncontrolled RNG all limit causal conclusions. The setup is reproducible; exact outcomes are not deterministic because the production RNG has no seed injection.

## Focused combo-order probe

**Correction during the experience pass:** the original harness edited a copied ComboSequence list, so its intended two-action ordering was not installed. Those 40 fights are smoke tests only and provide no combo-order evidence. The harness now uses AddToCombo/RemoveFromCombo. The original record follows for provenance: Forty additional real combats used level-3 Sword characters against level-3 Wolf, INT 10, `InitializeDefaultCombo()`, and an explicit two-action sequence. STRIKE→SLAM and SLAM→STRIKE each won 20/20. Actions were deliberately supplied for a sandbox probe; this does not claim the build is naturally unlocked at level 3. This saturated outcome provides **no demonstrated advantage for either order**. The manager's direct `GetCurrentTurn()` returned zero in this path, so `combo-results.json`'s `averageTurns` field is unavailable telemetry, not instant victories. A future probe should log executed action identities, status setup/payoff, action-count fallback, and remaining HP with a harder enemy before comparing ordering.

## Recommended player-facing changes

1. Call out a one-action loadout and show how many additional actions the player can add. Do not present its order as a strategic choice.
2. Put per-hit damage and enemy armor together. Several weak hits versus one heavy hit should be understandable before combat.
3. Display actual nonliving immunity, rather than inferring it from model appearance. The baseline cannot prove a poison build failed because of immunity unless rejected effects are recorded.
4. Explain linear versus routed combo progression using the actual INT threshold, then show the next action. Source review confirms that lower INT still follows the sequence linearly; higher INT enables authored ComboRouter jumps. It is not random selection.
5. Add a post-encounter summary of actions used, damage absorbed by armor, healing, failed status applications, and combo advances. Win rate alone hid the substantial L1 action-count difference.
6. Before balancing, test acquired-gear builds and a setup/payoff combo against an explicitly high-armor target, using matched random seeds if seed injection is later added.

## Isolation defect discovered

`PatchProfileService.GetGameDataRoot()` falls back to `GetDirectoryName(GetGameDataFilePath(string.Empty))`. In this alternate launch layout it dropped the final `GameData` directory and bootstrapped `PlaytestHarness/Patches/Balance/default.json`, despite `GameData/PatchProfile.json` selecting 7-1-2026. Earlier exploratory results therefore used default tuning and must not be compared with the final sample. For the final run, the active profile and patches were copied to that resolved **isolated** path; the logged patch was checked. The source path-resolution defect was subsequently fixed in PatchProfileService by preserving the GameData directory itself, with a regression assertion for alternate launch paths. The sample above predates that fix and used the documented isolated workaround.

## Re-run

Copy non-save GameData JSON and patch folders into an isolated harness workspace; verify the logged DATA and BALANCE paths and active patch before trusting results. Build `PlaytestHarness.csproj` against `../bin/*.dll`, then run `dotnet bin/Debug/net8.0/PlaytestHarness.dll` from the harness folder. This compiles the harness only and does not invoke the game's pre-build process-kill target. The harness output contains its own copies of referenced assemblies.


