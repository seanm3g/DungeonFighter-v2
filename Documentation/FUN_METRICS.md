# Fun & Dynamics Metrics

Balance is judged on clear rate and fight length (`BALANCE_PROCESS.md`). These metrics judge whether the game is
**varied and lively**: whether many builds, materials and affixes are viable, whether choices are real, and whether fights
have shape. Computed by `Scripts/diversity-report.py` from DUNGEONSIM raw shards (`dungeon-sim.py --keep DIR`).

"eff" = inverse Simpson effective count: 1 means one option everywhere, N means N options used evenly.

## Build diversity (per class and level)

| Metric | Meaning | Target (first pass) |
|---|---|---|
| `material_eff` | effective number of materials worn across pieces | ≥ 4 |
| `offclass_share` | worn pieces from another class's material trio (materials are shared by design) | 10–40% |
| `classless_share` | worn pieces from class-less materials (Stone, Unknown, Strange, Celestial, Wood, Cloth) | 10–30% |
| `set_rate` / `set4_rate` | fights with a 2+ / 4+ piece set active | 2+: ≥ 70% from L18 |
| `set_eff` | effective number of dominant sets across heroes | ≥ 3 |
| `affix_eff`, `stat_eff`, `mod_eff`, `act_eff` | effective number of worn affix lines, by family | rising with level |
| `strip_eff` | effective number of distinct strips | ≥ 3 |
| `skill_eff` | effective number of distinct learned-skill sets | ≥ 3 |
| `build_eff` | effective number of build fingerprints (set, top-2 stat suffixes, skills, strip) | ≥ half the heroes |
| `build_distance` | mean Jaccard distance between two heroes' builds (materials, affixes, skills, strip actions) | ≥ 0.6 |

## Material and affix balance

| Metric | Meaning | Target |
|---|---|---|
| forced-material grid (`grid`) | clear per class with every drop forced to one material (`--material X`) | within-class spread ≤ 15 pts; no material > 20 pts under the class's best |
| forced-affix grid | clear and hero actions with one extra affix line on every piece (`--affix stat:X` / `mod:Y`) vs no affix | each family's gain within 0.5–2× the median gain |
| `adoption` worn share | per material / affix: item-passes worn / item-passes the bot could wear | none < 0.5× family median (dead), none > 2× (must-pick) |
| value vs adoption | strong but rarely worn = bot / scorer blind spot; weak but often worn = scorer overvalues | flag both |

Adoption measures what the calibration bot picks (ItemValueModel + set rules), not true power; the forced grids measure power.

## Choice quality

| Metric | Meaning | Target |
|---|---|---|
| `slot_contest` | per gear pass and slot: runner-up item value / best | 0.6–0.9 |
| `contest_share` | slot passes where runner-up is within 10% of best (a real choice) | 20–50% |
| `upgrades_per_hero` | gear swaps per hero over the run | > 0 every band |

## Fight dynamics

| Metric | Meaning | Target |
|---|---|---|
| `close_call` | won fights where the hero dropped under 25% HP | 5–15% |
| `lead_changes` | HP-share lead swaps per fight | > 0.5 |
| `length_cv` / `p90_ratio` | fight-length spread / slog tail | ≥ 0.25 / ≤ 2.5 |
| `one_shot` | won fights ending in ≤ 2 hero actions | ≤ 25% |
| `miss_streak` | fights with a 4+ hero miss streak | ≤ 10% |
| `action_eff` | effective number of hero actions used in a fight | ≥ 2.5 |
| `dot_mix` | effective number of hero damage kinds (hit vs burn / poison / bleed) | ≥ 1.3 |
| `status_eff` | effective number of statuses the hero applies in a fight | ≥ 1.5 |
| `enemy_move_eff` | effective number of enemy moves faced in a fight | ≥ 2 |
| `combo_share`, `crit_share`, `convert_share` | damage share from combos / crits / material converts | no single source > 70% |
| `strip_completion` | strip completions per hero action (combo payoff) | rising with level |

Targets are first-pass judgement calls (2026-11-08); tighten them once baselines exist.
