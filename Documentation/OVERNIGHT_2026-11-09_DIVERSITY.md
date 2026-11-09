# Overnight 2026-11-08 → 09: build diversity, materials and affixes

Branch `balance/build-diversity-2026-11-08` (worktree `../DungeonFighter-v2-diversity`). Base commit 7ac6092f snapshots the
shared checkout's uncommitted material / synergy WIP so every number here is the game you are playing. Nothing is merged.

## 1. New metrics (`Documentation/FUN_METRICS.md`, `Scripts/diversity-report.py`)

| Family | Metrics |
|---|---|
| Build diversity | material_eff, offclass_share, classless_share, set_rate / set4_rate, set_eff, affix_eff (stat / mod / act), strip_eff, skill_eff, build_eff (distinct builds), build_distance |
| Material / affix balance | forced-material grid (`--material`), forced-affix grid (`--affix stat:X` / `mod:Y`, priced against stripped references `stat:-` / `mod:-`), adoption (worn / offered item-passes) |
| Choice quality | slot_contest, contest_share, upgrades_per_hero |
| Fight dynamics | close_call, lead_changes, length_cv, p90_ratio, one_shot, miss_streak, action_eff, dot_mix, status_eff, enemy_move_eff, combo / crit / convert share, strip_completion |

Sim plumbing: fight rows log worn affixes; DUNGEONSIM JSON has `GearPasses`; `dungeon-sim.py --affix`.

## 2. What the baseline (fastbot-syn47) showed

- **Material sets were far apart.** Forced full sets spread 45–78 clear points inside one class. Wizard at L58: Crystal 74%, Willow / Silver 17–18%. Wood and Cloth were worst for everyone. Iron +25 at L18.
- **Class-less sets were almost never worn** (2% of pieces at L18, 0.3% at L58): the bot always took a home set first.
- **Several affixes were dead or broken:**
  - MULTI-HIT prefixes (Light / Acrobatic) stacked whole extra hits per piece — six Light pieces = 13 hits per attack (+28 pts).
  - ARMOR / HEALTH BONUS prefixes were flat (+1 armor, +25 HP) and measured as nothing from L18 up.
  - HIT and COMBO gear is saturated by ~L20: a HIT prefix at +1 and at +6 gave seed-identical results; COMBO the same at L38 / L58.
  - Gear DoT (Flaming / serrated / Poisonous) adds ~0 damage even at value 500: weapon-only, crit-only, intensity decays 1 per tick.
  - Single-attribute suffixes (STR / AGI / TEC / INT) and ARMOR suffixes ≈ 0; the ALL suffix +30.
  - The bot's item score ignored adjective prefixes entirely.
- **Wood's trigger (ON_MISS) never fired** — heroes almost never miss.
- **Skills:** every Barbarian learns the same 15 nodes at L38.
- **Headless sims were broken by the main-menu WIP**: digit `1` now loads fighter card 1, so every sim hero loaded save #1. Fixed on the branch (`ClassPlaythroughPolicy` sends the new-fighter key). **Rebuilding the shared checkout's tools without this fix breaks DUNGEONSIM.**

## 3. Changes on the branch

| Commit | Change |
|---|---|
| a5414c31 | affix logging, gear-pass offers, `--affix`, bot new-fighter key |
| 4c7cdfcb | ARMOR / HEALTH BONUS prefixes are % of the suffix reference; gear MULTI-HIT is fractional (whole part every attack, fraction fills a per-swing meter) |
| d42845bb | bot values adjective prefixes; class-less sets compete with home sets on piece count |
| 15971a95 | **fastbot-div1**: per-material strength multipliers (forced-set solve); Wood synthesis ON_ODD; Cloth FRAY mint-DR knob |
| 69e1105c | prefix values and stat-suffix multipliers solved to ~+8 clear points per full forced set |
| 239d3b47 | fastbot-div2/div3 damage re-fit; synergy bot branchBonus 0.35 → 1.0 (skills follow the worn set) |
| d3e2f967 / b9559469 | fastbot-div4 / div5 class re-fits |
| 67190b34 | **fastbot-div6** (final candidate) |
| 27bb4de2 | HEALTH BONUS prefix test updated to % semantics (no other new test failures vs the snapshot) |

## 4. Results

### Material parity (forced full set, all classes, fresh seeds)
SD of clear across each class's 9 viable sets (own trio + 6 class-less):

| | L18 | L38 | L58 |
|---|---|---|---|
| syn47 | 13–24 | 13–18 | 18–25 |
| div1 (materials only) | 6–12 | 6–10 | 9–14 |
| div6 (all changes, final tools) | 5–11 | 9–15 | 9–15 |

Weakest left: Wood for Barbarian (37–46%), Wizard sets at L58 (17–66%).

### Natural play, syn47 → fastbot-div6 (synergy bot, 128 heroes per level on both sides, same seeds)

| Metric | L18 | L38 | L58 |
|---|---|---|---|
| clear | 69% → 64% | 69% → 68% | 56% → 55% |
| build_eff | 119 → 120 | 103 → 106 | 73 → 81 |
| build_distance | 0.885 → 0.896 | 0.888 → 0.888 | 0.876 → 0.868 |
| material_eff | 11.6 → 13.0 | 12.0 → 13.1 | 10.2 → 11.4 |
| classless_share | 3% → 15% | 3% → 12% | 0% → 5% |
| set_eff | 10.9 → 11.6 | 11.3 → 11.8 | 9.9 → 10.8 |
| set4_rate | 39% → 46% | 75% → 83% | 97% → 100% |
| stat_eff | 11.0 → 14.4 | 10.1 → 11.9 | 10.0 → 11.2 |
| mod_eff | 11.7 → 15.0 | 15.3 → 17.6 | 14.5 → 16.0 |
| strip_eff | 29.1 → 31.4 | 22.6 → 26.3 | 24.5 → 28.1 |
| skill_eff | 5.4 → 7.8 | 6.1 → 11.2 | 5.4 → 6.2 |
| contest_share | 62% → 47% | 60% → 50% | 74% → 64% |
| close_call | 2% → 2% | 2% → 4% | 3% → 4% |
| one_shot | 11% → 7% | 8% → 11% | 15% → 15% |
| lead_changes | 0.95 → 0.99 | 0.99 → 0.95 | 0.90 → 0.92 |
| crit_share | 25% → 32% | 26% → 36% | 34% → 41% |
| convert_share | 45% → 48% | 28% → 32% | 16% → 17% |

Wider check (L8 / L28 / L48 / L68, seed 8800): class-less wear 10→17%, 2→16%, 1→8%, 0→1%; material_eff +0.1 to +1.4;
skill_eff +0 to +2.3; clear 93→91, 70→68, 60→67, 46→43.

Reading it honestly:
- **What got more varied:** which materials and sets heroes wear (class-less sets went from never to 5–17% of pieces),
  which suffixes and prefixes they carry, which skills they learn (L18–L48), and their strips.
- **build_eff / build_distance did not move.** Almost every hero is already a unique build at 128 heroes, so build_eff saturates;
  the variety gain shows in the component metrics, not in "number of distinct builds". (An earlier draft claimed 61 → 120:
  that compared a 64-hero baseline with a 128-hero run. Compare equal samples only.)
- **Fight dynamics barely moved:** close calls 2–4% (target 5–15%), DoT ~0, one-shots unchanged on average. Those need design
  levers (section 5), not set balance.
- contest_share fell: gear slots are less often a near-tie now that prefixes carry value.

### Curve (balance-loop measure, 4 seeds × 32 heroes per class, L3–L73)

| Seeds | syn47 gap / spread / bands over parity | fastbot-div6 |
|---|---|---|
| 9301–9304 (unseen by any solve) | 2.27 / 10.4 / 7 | **1.73 / 9.9 / 7** |
| 8501–8504 (div6's last diagnose seeds) | 1.98 / 9.4 / 6 | 1.19 / 7.8 / 3 |

Late game (L78–98, 2 seeds): unchanged (div6 vs syn47 within noise at every level: 25/24/11/9/3% vs 28/25/11/7/4%).

Biggest misses on div6: L23 65 vs 70 (Wizard 57), L58 50 vs 54, L68 36 vs 40 (Barbarian 27).
At L58 two enemies cause ~70% of deaths for every class: **Tomb Creeper** and **Poison Dart Sentinel**.

## 5. Prototypes on their own branches (not in the diversity branch)

| Branch | Change | Measured (forced full set vs stripped reference) | Natural play |
|---|---|---|---|
| `proto/gear-dot-2026-11-09` (1cb659d1) | DoT prefixes count from every equipped piece and apply on any landed hit (was weapon-only, crit-only); serrated 40, Poisonous 2 | Flaming +7..+10, serrated +5, Poisonous +2 (were ~0 even at value 500); poison looks capped | unchanged within noise (DoT prefixes are rare on worn gear) |
| `proto/hit-overflow-2026-11-09` (679250e2) | COMBO applied before HIT; HIT / COMBO a floored threshold can't use spill into CRIT at half rate | Balanced / Precise COMBO prefixes +9 (were ~0 past L20), COMBO suffix +8; HIT still +1..+4 (needs full-rate spill or a different payoff) | unchanged within noise |

Both branch off the diversity branch; merge either on top of it.

## 6. Needs your call

1. **DoT on gear** is dead weight. Prototype branch `proto/gear-dot-2026-11-09` makes it work (any piece, any hit) — merge it or pick another rule.
2. **HIT / COMBO saturation**: past ~L20 every HIT / COMBO line is wasted. `ThresholdManager` clamps hit ≤ combo − 2 and combo ≥ `MinComboThreshold`; TEC milestones put combo on that floor by ~L20, so neither stat can move a threshold. Prototype `proto/hit-overflow-2026-11-09` revives COMBO lines; HIT still needs a payoff (full-rate spill, or overflow into damage).
3. **Defense is cheap by construction.** Armor is already % mitigation (DR = def / (def + 275)), and HEALTH works, yet ARMOR ×51 still bought only ~+5: enemies swing ~2–3 times per fight, so offense decides fights. Options: longer enemy turns / more enemy swings, or price armor lines low on purpose.
4. **Wood ON_ODD** (my pick, pairs with the old Gold ON_EVEN idea) — keep or choose another trigger. **Celestial** ON_AFTER_MISS works only through its knobs (×3.8).
5. **L58 spike enemies**: Tomb Creeper and Poison Dart Sentinel cause ~70% of L58 deaths for every class. Wizard sets are weakest there (forced 17–66%).
6. **Skill trees**: all Barbarians (and nearly all Wizards) learn identical nodes — point budget vs node count.
7. **Class-less sets differ by class**: Cloth and Wood are weak for Barbarian (−22..−24 vs class median) but strong for Warrior / Wizard; one knob per material can't fix it (Cloth FRAY pays on taking hits, Wood on odd rolls).
8. Stat suffix descriptions in StatBonuses.json still show the old numbers (values were scaled ×0.1–×20).
9. **Shared checkout warning**: the uncommitted main-menu WIP (digit = fighter card) breaks DUNGEONSIM as soon as tools are rebuilt there. The one-line fix (`ClassPlaythroughPolicy` "MainMenu" → `MainMenuHandler.NewFighterKey`) is in a5414c31.

## 7. How to merge / reuse

- Merge `balance/build-diversity-2026-11-08` and set `activeBalancePatch` to `fastbot-div6`. It includes the WIP snapshot commit (7ac6092f), so merge after (or instead of) committing that WIP in the shared checkout.
- Re-run: `python Scripts/diversity-report.py report <keep dir>`; forced grids: `dungeon-sim.py --material X` / `--affix mod:Y` + `diversity-report.py grid`; solves: `Scripts/material-solve.py`, `Scripts/affix-solve.py --baseline-affix mod:-|stat:-`.
