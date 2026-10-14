# System Structure Review — 2026-10-12 night (partial)

Short pass on balance-system structure and simplification candidates. Written while another session
("Character healing between fights bug") was re-solving the L35 / L50 / L75 damage anchors on `overnight-v2` with
DUNGEONSIM. Nothing here changes the game; `GameData/Patches/Balance/overnight-v3.json` is an untouched copy of v2,
made as a sandbox for this session's experiments.

## 1. Dead config (verified)

These `CombatConfig` properties are declared and given defaults by the sanitizer (`CombatConfig.cs:79–90`,
`:127–136`), and no other code reads them. Setting them in a patch does nothing:

`levelDifferenceDamageScaling`, `enemyEnrageThresholdPercent`, `enemyEnrageDamageMult`, `comboBreakPenaltyPercent`,
`statusEffectBaseProcChance`, `globalHealEffectiveness`, `firstStrikeBonusPercent`, `overkillDamagePercent`,
`attributeSoftCap`, `techniqueMilestoneRollShift`, `agilitySpeedReduction`.

## 2. Probably dead config (heuristic, not yet verified)

A scan of the overnight-v2 patch flagged ~100 leaves whose property name appears nowhere outside its declaration (in
code outside Tests). Whole sections stand out: `enemyDPS` (21 leaves, incl. `sustainBalance.*`), `balanceAnalysis`
(9), `balanceValidation` (8), `xpRewards` formulas (7), `progressionCurves` (5), `earlyGame.naivete.*` (4; Naiveté
was removed), `combatBalance.environmentalEffects.*` (4), `dungeonGeneration.minRooms / roomCountScaling /
hostileRoomChance / bossRoomName`, `gameData.autoGenerate*`, `rarityScaling.levelBasedRarityScaling.*` (partial).
The scan has false positives: it flagged `combat.retriggerDamageMultiplier` and `equipmentScaling.armorFlatPerPiece`,
which are read through differently named accessors. Check each one before deleting it. The scan script lives in this
session's scratchpad (`deadcfg.py`). It walks every patch leaf and greps `Code/` (minus bin/obj/Tests) for
`.PropertyName` or `"key"`.

The patch has 1179 leaves. 433 of them are `itemAffixByRarity` and 251 are `enemySystem`. Most of the enemySystem
leaves are per-enemy calibration.

## 3. Damage pipeline: how many layers

Hero raw damage (`DamageCalculator.CalculateRawDamage`) multiplies about 15 independent factors: base (attr + weapon +
equipment + mods + cadence flat) × action mult × early-game starting mult × DAMAGE_MOD × Rogue counter × AMP (TEC
combo × AMP_MOD × skill AMP × INT potency) × next-attack mult × STR mult × speed overflow × retrigger × roll band
(crit / combo / basic) × class weapon mult × class-tag charm, plus a flat convert added after flooring. Then
defense, tag damage and status in/out multipliers.

Enemy damage multiplies three tuning layers on top of base: `damageCurve(level)` × identity `HitDamage` × per-enemy
`Calibration`. Calibration was solved under one curve and the curve keeps getting re-solved, so the two drift and both
have to be re-solved together (this is the "re-solve after any hero-side change" loop in BALANCE_PROCESS).

Simplification candidates:
- AMP has four sources multiplied together (TEC combo, AMP_MOD, skill AMP, INT potency). The opener-lift bug
  (`ConsumedSkillAmpPercent`) happened because two of them share a code path. One additive "AMP bonus %" pool
  feeding a single multiplier would be easier to reason about and to price.
- `ClassBalanceHelper.GetDamageMultiplier` is applied to any `Character` with a weapon (`DamageCalculator.cs:298`),
  and that includes `Enemy`. If any `classBalance.*.damageMultiplier` ≠ 1, armed enemies get it too. Unverified
  whether any patch sets one.

## 4. Defense math (open item 1 in BUILD_LADDER_REPORT)

Hero DR = A / (A + K) (`ClassDefenseCalculator.ComputeDr`), so effective HP = 1 + A/K: every armor point is worth the
same 1/K of EHP at every level. Flat armor from skills fades only relative to gear: gear armor grows with item level
(typical 23 at L1 → 198 at L90). So +21 armor is +7.6% EHP at any level, but against L90 gear it's +4.4% on top of
1.72×. Because the enemy damage curve is solved after gear armor, it already absorbs the gear's DR growth. That leaves
two clean options:
- **K by level** (K ∝ expected gear armor at that level): typical-gear DR stays constant, armor upgrades keep a
  constant relative value, and the damage curve no longer has to chase armor growth.
- **Defensive skills as % DR / % EHP** instead of flat armor, so they don't depend on gear level.

`ClassDefenseCalculator` also still carries the Warrior Tempo / Rogue Counter / Barbarian Grit / Wizard Shield helpers.
Its own comments say they are unused on the live DR path, but `DamageCalculator` still consumes
`PendingDefenseCounterDamagePct`. Check whether anything still mints it before removing them.

## 5. Class parity in real dungeons (DUNGEONSIM baseline, overnight-v2)

Smart bot, retreat below 50%, HP carried between fights, 288 dungeons per level (72 per class):

| Level | Dmg anchor | Clear | Retreat | Death | HP lost/fight | Barbarian | Warrior | Rogue | Wizard |
|---|---|---|---|---|---|---|---|---|---|
| L10 | 0.38 | 84.4% [80–88] | 15.6% | 0.0% | 6.4% | 75% | 89% | 88% | 86% |
| L15 | 0.55 | 67.4% [62–73] | 32.6% | 0.0% | 9.4% | **46%** | 69% | 65% | 89% |
| L20 | 0.52 | 64.8% [59–70] | 34.5% | 0.7% | 9.8% | **47%** | 82% | 58% | 71% |

Overall clear rates sit on the realClearAnchors (L10 75 target is exceeded at 84; L15/L20 ≈ 65 target). Deaths are
near zero: the retreat rule converts almost every loss into a retreat, so "clear" is the only meaningful dial.

**Barbarian is the outlier**: 20–35 points below the other classes at L15–L20 (72 dungeons per class, so roughly
±11 points per cell). With 6 dungeons per shard the per-class cells are noisy, but Barbarian is lowest at all three
levels. Candidate lever: `classBalance.<Mace>.healthMultiplier` (the documented class-parity knob), solved with
DUNGEONSIM per class. Wizard at L15 (89%) may be high, but L10/L20 put it in line.
L35–L75 rows were still running when this was written; the other session's solves cover those levels.

## 6. Not done tonight (and why)

The plan was: (a) measure real dungeon clear per class at L10–L75 with DUNGEONSIM to check class parity, (b) prototype
K-by-level defense behind a knob and price it with BUILDLAB, (c) re-price the weak wizard builds. A DUNGEONSIM
baseline (L10,15,20,35,50,75 on overnight-v2, `Code/bin/BalanceTools`) was started at 23:34. After that, this
session's permission classifier began refusing actions as interference with the other session's overnight solves,
which run on the same 24 cores. The other session owns overnight-v2, the damage-curve anchors, balance-targets.json
and balance-check.ps1. The two sessions agreed on that split.
