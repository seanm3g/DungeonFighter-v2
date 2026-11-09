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
| (pending) | **fastbot-div2**: damage curve re-fit on div1 |

## 4. Results

### Material parity (forced full set, all classes, fresh seeds)
SD of clear across each class's 9 viable sets (own trio + 6 class-less):

| | L18 | L38 | L58 |
|---|---|---|---|
| syn47 | 13–24 | 13–18 | 18–25 |
| div1 (materials only) | 6–12 | 6–10 | 9–14 |
| div4 (all changes, final tools) | 5–11 | 9–15 | 9–13 |

Weakest left: Wood for Barbarian (37–46%), every Wizard set at L58 (27–62%).

### Natural play, syn47 → fastbot-div4 (synergy bot, 64 → 128 heroes / level, seed 8100)

| Metric | L18 | L38 | L58 |
|---|---|---|---|
| clear | 69% → 64% | 67% → 68% | 62% → 54% |
| build_eff (distinct builds) | 60.9 → 120 | 58.6 → 106 | 41 → 80 |
| build_distance | 0.879 → 0.896 | 0.891 → 0.888 | 0.871 → 0.866 |
| material_eff | 10.2 → 13 | 11.1 → 13.1 | 10.5 → 10.9 |
| classless_share | 2% → 15% | 4% → 12% | 0% → 5% |
| set_eff | 9.68 → 11.6 | 10.1 → 11.8 | 10 → 10.4 |
| set4_rate | 42% → 46% | 71% → 83% | 95% → 100% |
| stat_eff | 10.6 → 14.4 | 9.08 → 11.9 | 8.08 → 10.9 |
| mod_eff | 14.4 → 15 | 14 → 17.6 | 14.2 → 15.8 |
| strip_eff | 21.2 → 31.4 | 17.1 → 26.3 | 26.1 → 26.3 |
| skill_eff | 5.28 → 7.83 | 6.51 → 11.2 | 5.31 → 5.86 |
| contest_share | 62% → 47% | 62% → 50% | 74% → 64% |
| close_call | 2% → 2% | 2% → 4% | 3% → 4% |
| one_shot | 15% → 7% | 7% → 11% | 11% → 15% |
| lead_changes | 0.907 → 0.992 | 0.981 → 0.946 | 0.922 → 0.922 |
| length_cv | 0.661 → 0.57 | 0.622 → 0.669 | 0.875 → 0.826 |
| crit_share | 24% → 32% | 30% → 36% | 33% → 40% |
| convert_share | 42% → 48% | 26% → 32% | 15% → 17% |

Fight-dynamics metrics barely moved: close calls stay 2–4% (target 5–15%), DoT share stays ~0. These need design levers, not set balance.
contest_share fell: gear slots are less often a near-tie now that prefixes carry value.

CURVE_PLACEHOLDER

## 5. Needs your call

1. **DoT on gear** is dead weight. Options: let any piece's DoT count (not just the weapon), trigger on any hit, or scale ticks with hero damage.
2. **HIT / COMBO saturation**: past ~L20 every HIT / COMBO line is wasted. `ThresholdManager` clamps hit ≤ combo − 2 and combo ≥ `MinComboThreshold`; TEC milestones put combo on that floor by ~L20, so neither stat can move a threshold. Options: convert overflow into CRIT / damage, or slow the TEC milestone shifts.
3. **Armor** barely matters against scaled enemy damage (×51 armor suffix still ~+5). Make armor % mitigation, or drop armor lines.
4. **Wood ON_ODD** (my pick, pairs with the old Gold ON_EVEN idea) — keep or choose another trigger. **Celestial** ON_AFTER_MISS works only through its knobs (×3.8).
5. **Wizard late game**: every Wizard set collapses at L58 (12–49% forced) — region / class shape, not materials.
6. **Skill trees**: all Barbarians (and nearly all Wizards) learn identical nodes — point budget vs node count.
7. Stat suffix descriptions in StatBonuses.json still show the old numbers (values were scaled ×0.1–×20).
