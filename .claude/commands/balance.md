Measure and tune game balance with the headless DUNGEONSIM tools: scan → focus → confirm → gate → new patch.

Usage: /balance [scan | solve <level> | confirm <candidate> | gate] [notes]
- No argument: run the whole loop below, from the scan.
- `scan`: step 1 only, then report the problem areas.
- `solve <level>`: steps 2–3 for one region band (use a band midpoint, e.g. 8, 13, 18, 33, 48).
- `confirm <candidate>`: step 3 for a candidate patch already in `GameData/Patches/Balance/`.
- `gate`: step 4 only.

Full reference: `Documentation/BALANCE_PROCESS.md`. Targets: `GameData/TuningProfiles/balance-targets.json`
(`realClearAnchors` for dungeon clear %, `heroTurnsAnchors` for fight length).

## The model (what "balanced" means here)
- Balance is judged on **real dungeons**: DUNGEONSIM, HP carried between fights, no post-fight heal, retreat below 50% HP,
  fast rules bot, hero level ±1 dungeon roll. Never judge on a single-fight win rate.
- Two enemy knobs per 5-level region band: **health bands set fight length** (~11 hero actions from L11), **damage bands
  set clear rate**. Regions cycle every 5 levels and differ a lot (Air is ~35 pts harder), so test band midpoints.
- The HP curve is calibrated to the **median catalog build** (`median-curve-solve.py --check`), not a bare hero.
  `balance-check.ps1` bare-hero fight rows fail at L20+ by design.
- Class parity: compare per-class clear rates and enemy swings per kill; per-class knob is
  `classBalance.<class>.damageCurve`. Enemy tempo is `combat.enemyAttackTimeMultiplier`
  (`enemySystem.globalMultipliers.speedMultiplier` does nothing for tempo).

## Before any sim
1. Check `GameData/GeneralSettings.json` `enemyHealthMultiplier`. Sims inherit it; anything but 1 silently scales every
   result. Report it and ask before changing the user's settings.
2. Note the active patch in `GameData/PatchProfile.json` (`activeBalancePatch`). Every tool prints a `Provenance:` /
   patch line first: check it matches what you meant to test.
3. Other sessions may share this checkout and a running DF.exe: never `git stash`; tools build into their own
   `Code/bin/BalanceTools` / `ScanTools` folders.
4. Start cheap: a ~20 s single-level `dungeon-sim.py --levels L --shards 2` probe answers most "is X broken?" questions.

## Loop
1. **Scan** (~8 min): `python Scripts/balance-scan.py`, with `--compare <newest GameData/TuningRuns/balance-scan-*.json>`
   when one exists. Its dungeon clear rows are a few heroes per level: treat them as gross-miss detectors only. Pass
   `--dungeon-levels 8,18,33,48` (band midpoints; the default 10,20,35,50 sits on band edges).
2. **Focus**: run only what the scan flagged.
   - Clear rate off: `python Scripts/bo-solve.py --level <band midpoint> [--spark 18]` (target defaults to
     `realClearAnchors` at that level; `--scope band` scales the whole band). It prints the band anchors to write.
     Do not use `dungeon-sim.py --solve` or `band-solve.py` (bisection on one seed set, up to 27% off).
   - Fight length off: `python Scripts/median-curve-solve.py --only health --levels L`, then `--check`.
   - Many knobs / what-ifs: `Scripts/surrogate.py predict|sweep` for screening only (gaps under ~6 pts are noise).
   - Write the change into a **new candidate patch**: copy the active patch to
     `GameData/Patches/Balance/<name>.json` and edit the copy. Run every tool with `--patch <name>`. Never edit the active
     patch file in place.
3. **Confirm** on a fresh seed, paired against the base patch (same seed for both):
   `python Scripts/dungeon-sim.py --levels <levels> --shards 8 --seed <new seed> --patch <base>` and again with
   `--patch <candidate>`. Use ≥8 shards (16 heroes per class) to read class gaps; 12-hero samples swing ±20 pts, and
   dungeon-sim's Wilson intervals count dungeons, so they read too narrow. A solve that misses on the fresh seed is not
   done (bo-solve's L73 answer overshot to 40% vs a 30% target).
4. **Gate**: `powershell -File Scripts/balance-check.ps1 -Patch <candidate>` once, before proposing the patch. Read its
   dungeon rows; the bare-hero fight rows are diagnostic only.
5. **Hand off**: report the before/after table (level × class clear %, hero actions per fight, swing size). Ask the user
   before activating it in `PatchProfile.json` (see `/patch`). Patch JSONs are under a gitignored path: commit with
   `git add -f GameData/Patches/Balance/<name>.json`.

## Do not use
The legacy single-fight win-rate tooling: MCP `run_full_cycle`, `balance_tuner_agent_run`, `suggest_tuning`,
`apply_tuning_suggestion`, `validate_balance`, `get_balance_quality_score`, `test_what_if`,
`analyze_parameter_sensitivity`, `adjust_*`, `apply_preset`, `save_configuration`, `load_patch`, and the
TUNESIM / TUNEANALYZE / TUNEAPPLY / LEVEL* / TUNING / PLAYTHROUGHTUNING / WRSOLVE modes. They target an 85–98%
per-fight win rate on a bare hero and adjust `globalMultipliers`. Their disk writes are blocked by
`LegacyTuningWriteGuard`; do not set `DF_ALLOW_LEGACY_TUNING_WRITES` to get around it.
