# Tuning profiles

Data the balance scripts read. The workflow is in `Documentation/BALANCE_PROCESS.md` (scan → focus → confirm → gate).

| File | Read by | Holds |
|------|---------|-------|
| `balance-targets.json` | `balance-scan.py`, `balance-check.ps1`, `bo-solve.py`, `dungeon-sim.py`, MCP `get_balance_context` | `realClearAnchors` (dungeon clear % by level), `heroTurnsAnchors` (fight length), retreat rule, scan thresholds. `winRateAnchors` / `fightsPerDungeon` / `realClearTarget` are legacy reference fields. |
| `build-ladders.json` | BUILDLAB, `median-curve-solve.py`, `balance-scan.py` | BUILD_CATALOG ladders: the median build the enemy HP curve is solved for |
| `hybrid-ladders.json` | BUILDLAB | Hybrid-class build ladders |
| `rules-bot.json` | DUNGEONSIM rules bot (`PlaythroughRulesPolicy`), `rules-bot-climb.py` | Fast bot skill / gear / strip priorities |
| `skill-tree-templates.json` | `skill-tree-tune.py` | Skill node effect templates and design power |

The single-fight win-rate profiles (TUNESIM / TUNEANALYZE / TUNEAPPLY / LEVELTUNING: `combat-dials`, `level-curve`, ...)
were removed on 2026-11-04 with the tuning stack that read them; they are in git history before that date.
