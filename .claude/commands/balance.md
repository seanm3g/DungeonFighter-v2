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
  (`enemySystem.attributeScale` scales enemy STR/TEC/INT, not hit damage; curves are step bands, see BALANCE_PROCESS).

## Running the tools
Run the scripts from the repo root with Bash, or through the `dungeonfighter` MCP server: `get_balance_context` (active
patch, targets, settings warnings), then background jobs `start_balance_scan`, `start_bo_solve`, `start_dungeon_sim`,
`start_median_check`, polled with `get_balance_job` (one job at a time; they use every core). MCP jobs run the scripts
below against the server's own DF.dll and write results to `GameData/TuningRuns/mcp-*.json`.

## Before any sim
1. Check `GameData/GeneralSettings.json` `enemyHealthMultiplier`. Sims inherit it; anything but 1 silently scales every
   result. Report it and ask before changing the user's settings.
2. Note the active patch in `GameData/PatchProfile.json` (`activeBalancePatch`). Every tool prints a `Provenance:` /
   patch line first: check it matches what you meant to test.
3. Other sessions may share this checkout and a running DF.exe: never `git stash`; tools build into their own
   `Code/bin/BalanceTools` / `ScanTools` folders.
4. Start cheap: a ~20 s single-level `dungeon-sim.py --levels L --shards 2` probe answers most "is X broken?" questions.

## Loop
Use `Scripts/balance-loop.py` (full reference: `Documentation/BALANCE_PROCESS.md`, "The balance loop").
1. **Measure** where the patch stands, on seeds nothing was tuned on:
   `python Scripts/balance-loop.py measure --patch <p> --ref <previous>` (clear vs target, hero actions vs target, class
   spread vs `classParityMaxSpread`, per class and band). `balance-scan.py` is the wider quick look (per-enemy, pacing,
   variety) when you need it.
2. **Solve** one knob family at a time, health first (fight length), then damage (clear rate), then class (parity),
   then damage again: `python Scripts/balance-loop.py solve --patch <p> --knob health|damage|class --next <p2>`, or the
   whole sequence with `python Scripts/balance-chain.py --start <p> --first <N> --ref <previous>`. Each solve confirms
   on other fresh seeds, paired, and keeps only what beats noise; it writes an overlay patch (`"base": "<p>"`).
   Do not use `dungeon-sim.py --solve`, `band-solve.py` or `bo-solve --prior surrogate`.
3. **Held-out check**: `measure` the result against the starting patch on new seeds. Check `botcheck` (bot vs median
   build kill speed) when fight length moved.
4. **Gate**: `powershell -File Scripts/balance-check.ps1 -Patch <candidate>` once, before proposing the patch. Read its
   dungeon rows; the bare-hero fight rows are diagnostic only.
5. **Hand off**: report the before/after (level × class clear %, hero actions per fight, class spread). Ask the user
   before activating it in `PatchProfile.json` (see `/patch`). Patch JSONs are under a gitignored path: commit with
   `git add -f GameData/Patches/Balance/<name>.json`.

## Do not use
The legacy single-fight win-rate tooling: MCP `run_full_cycle`, `balance_tuner_agent_run`, `suggest_tuning`,
`apply_tuning_suggestion`, `validate_balance`, `get_balance_quality_score`, `test_what_if`,
`analyze_parameter_sensitivity`, `adjust_*`, `apply_preset`, `save_configuration`, `load_patch`, and the
TUNESIM / TUNEANALYZE / TUNEAPPLY / LEVEL* / TUNING / PLAYTHROUGHTUNING / WRSOLVE modes. They target an 85–98%
per-fight win rate on a bare hero. Their disk writes are blocked by
`LegacyTuningWriteGuard`; do not set `DF_ALLOW_LEGACY_TUNING_WRITES` to get around it.
