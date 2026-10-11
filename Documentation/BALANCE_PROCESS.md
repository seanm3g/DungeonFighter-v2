# Balance Process

How to measure, tune and verify DungeonFighter balance with the headless tools. Every tool runs from the repo root
against a built `DF.dll` (or `DF.exe`) and prints the balance patch it loaded on its first line — always check it.

```
dotnet <build>/DF.dll <MODE> [--patch <name|path>] [--set path=value ...] [--seed N]
```

- `--patch <name>` loads `GameData/Patches/Balance/<name>.json` for that run only (the active patch in
  `GameData/PatchProfile.json` is not changed). Works in every headless mode.
- `--set path=value` overrides any config leaf for that run (case-insensitive dotted path, e.g.
  `--set enemySystem.baselineStats.health=125`). Fight sims only: playthroughs reload the patch file, so make a patch
  variant instead.
- `--seed N` replays the same dice. Compare before/after on the same seed.
- `DF_GAMEDATA_DIR=<folder>` points the game at another GameData folder. Use a scratch copy for playthroughs —
  the death screen writes character saves.

## One command

```powershell
.\Scripts\balance-check.ps1 -Patch overnight-v1
```

Builds the tools into `Code/bin/BalanceTools`, runs the level matrix in parallel, survival playthroughs on a scratch
copy of GameData, and loot stats, then prints a PASS/FAIL scorecard against
`GameData/TuningProfiles/balance-targets.json` (exit code 0 = all pass). Edit that file to change the targets.
About 2 minutes (4 with `-RunsPerClass 60`). `-SkipSurvival` skips playthroughs; `-SkipBuild` reuses the last build.
Win-rate anchors are **dungeon clear rates** (`winRateMeaning: dungeonClear`); the matrix checks the matching single-fight
rate (clear^(1/fightsPerDungeon)) and the playthrough batch checks the real clear rate at `realClearCheckLevel`.

## Targets (designer, 2026-10-10)

| Target | Measure | Tool |
|---|---|---|
| ~11 hero turns per even-level fight, every level | mean hero turns | FUNDMATRIX `--gear typical` per level |
| Dungeon clear 100% at L1, 95% at L10, 0% at L100 (linear) | real clear rate + matrix fight win rate | PLAYTODEATHBATCH / FUNDMATRIX / WRSOLVE |
| Hero survives 100% of runs to L10 | % of playthroughs reaching L10 | PLAYTODEATHBATCH `--stop-at-level 10` |
| All four weapons ~11 turns, differentiated by speed and variety | per-weapon turns, swing time | FUNDMATRIX, FIGHTLOG |
| Starter swing 7–10 s, mastery ~2 s | swing time at AGI 10 / 100 | FIGHTLOG header |
| Rarity: Rare ≈ 2–3% of drops | rarity mix | ITEMSTATS |

## Tools

| Mode | What it answers |
|---|---|
| `FUNDMATRIX --level L [--gear none\|typical]` | Every weapon × enemy at one level: hero turns, damage/turn, enemy HP, win rate. Writes `GameData/TuningRuns/fundmatrix-*.json`. |
| `FIGHTLOG --weapon W --enemy E [--strip A,B,C]` | One seeded fight, event by event (rolls, damage, HP, swing times). `--strip` runs the real combo routing through that sequence. |
| `SEQLAB --weapon W --seq A,B --seq B,A` | Compares action sequences with real combo routing. |
| `ACTIONVALUE [--shard i/n]` | Prices every hero action: slot 1 or slot 2 next to the weapon basic, vs basic alone. Value 100 = no change; offense = faster kills, defense = less net HP lost. Run 16 shards in parallel. |
| `WRSOLVE --level L --target P` | Finds the enemy damage multiplier that gives win rate P at level L. Feed results into `enemySystem.damageCurve.anchors`. |
| `ITEMSTATS --levels 1,10,50,100` | Real loot drops: rarity mix, affix counts, what sets each item's rarity, top prefixes/suffixes. |
| `STATVALUE --level 10 --gear typical [--attr-amount 60]` | Exchange rates: what +1 of each item stat is worth in weapon-damage points. Feeds `ItemValueModel`. Use `--attr-amount 60` for STR/AGI/TEC/INT and one weapon at a time: the default step sits inside fight-to-fight noise. |
| `ITEMSTATS --detail` | Also prints gear actions by slot: class / neutral / other-class / basic share and the top actions. |
| `PLAYTODEATHBATCH N [--stop-at-level 10]` | N full playthroughs per class (bot equips upgrades). Survival %, deaths by level / fight # in dungeon / killer. |
| `RUNLOG --class C [--verbose]` | One playthrough traced fight by fight, with gear at each hub visit; `--verbose` prints every action with swing times and thresholds. |

## Loop

1. **Fundamentals first (L1, no gear):** FUNDMATRIX `--level 1`. Hero turns ≈ 11, every weapon within ±1 turn.
2. **Action sequence:** SEQLAB / ACTIONVALUE. Outliers (value far from 100 at their tier) get data changes in `Actions.json`.
3. **Level scaling (typical gear):** FUNDMATRIX at 1, 5, 10, 20, 35, 50, 75, 100. Enemy HP curve keeps turns ≈ 11
   (`enemySystem.baselineStats.health`, `progressionScales.healthGrowthScale`, `progressionPivotLevel`).
4. **Win-rate curve:** WRSOLVE at each anchor level → `enemySystem.damageCurve.anchors`. Re-run step 3 to confirm.
5. **Items:** ITEMSTATS for rarity; ItemValueModel scores for gear power per level.
6. **Survival:** PLAYTODEATHBATCH `--stop-at-level 10` on a scratch GameData copy. Read the death breakdown.
7. Save the patch, note what changed and why.

## Knobs added for this process

| Config | Default | Meaning |
|---|---|---|
| `combat.agilitySpeedCurveExponent` | 0.5 | Shape of agility→speed curve (1 = linear). |
| `combat.weaponSpeedSpread` | 1.0 | How far weapon `attackSpeed` sits from 1.0 (0.7 = 70% of authored spread). |
| `enemySystem.baselineStats.weaponDamage` | 0 | Enemy weapon damage; 0 = legacy (copies hero starting weapon damage). |
| `enemySystem.damageCurve` | off | Enemy damage × multiplier by level (formula or anchor table). |
| `lootSystem.rarityFromAffixes` | true | false = rarity table decides rarity; affixes roll at or below it and never raise it. |
| `lootSystem.affixTierFalloff` | 3 | Lower affix tiers this many times likelier on non-signature lines. |
| `dungeonScaling.maxRoomCount` | 0 | Cap on rooms per dungeon (0 = none). |
| `dungeonScaling.postFightHealPercent` | 0 | % of max HP restored after each won fight. |
| `dungeonScaling.maxEnemiesPerRoom` | 0 | Cap on enemies per room (0 = legacy ceil(level/2), which grows without limit). |
| `combat.roomStructureRollShiftCap` | 0 | Cap on the elegant/dilapidated room roll shift (0 = legacy ±hero level). |
| `lootSystem.equippedWeaponTypeBias` | 0 | Chance a weapon drop matches the hero's equipped weapon type. |
| `comboSystem.resetChainOnNormalHit` | true | false = only a miss breaks the combo chain, so later sequence slots matter. |
| `enemySystem.damageCurve.anchors` | [] | Level → enemy damage multiplier table (log-interpolated). Solve entries with WRSOLVE. |
| `enemySystem.healthCurve` | off | Level → enemy max HP multiplier (same anchor format). Set from FUNDMATRIX "Enemy HP needed … (×k)" to hold ~11 hero turns. |
| `combat.strengthDamagePerPoint` | 0 | Hero damage × (1 + STR × this), STR capped at `strengthDamageMaxStat` (100). 0.025 ≈ +1 STR worth +1 AGI. When set, the primary stat no longer adds damage. 0 = legacy floor((STR + primary) × `attributeDamageScale`). |
| `combat.statusWeakenDamageMultiplier` | 0.5 | Weakened actor's damage on its next action. |
| `combat.statusVulnerabilityDamagePerStack` | 0.15 | Extra damage taken per Vulnerability stack. |
| `combat.statusHardenDefensePerStack` / `statusExposeDefensePerStack` | 0.15 | Defense added / removed per stack, in units of the DR constant K (Expose can push Defense below 0 = extra damage taken). |
| `combat.statusFocusDamagePerStack` / `statusMaxStacks` | 0.15 / 3 | Focus damage per stack; most stacks of one status that count. |
| `lootSystem.weaponGearActionClassShare` / `armorGearActionClassShare` | 0.7 / 0 | Chance a gear action is a class action (weapon: its own class; armor: hero's class), else neutral. Starter attacks never roll. See `GearActionPools`. |

## Tests

`dotnet DF.dll --run-tests` runs every suite; a suite that throws is reported under **SUITES THAT CRASHED** and the run
continues. Exit code 0 = every test passed and nothing crashed. Baseline on 2026-10-11 (active patch 10-10-2026):
8875 tests, 8784 passed, 91 failed, 5 suites crashed (EquippedItemTriggerTests, DataSystemTestRunner,
ActionsSystemTestRunner, CombatSystemTestRunner, UISystemTestRunner — the last needs the Avalonia platform).
Treat new failures against that baseline as regressions.

## Measurement notes

- FUNDMATRIX `--gear typical` is deterministic (typical gear is sampled from seeded loot). Same patch + seed = same numbers.
- The matrix is an even fight in a plain room with no consumables. Real play (PLAYTODEATHBATCH) adds room tags,
  room-search buffs and food, and the bot's own gear; past ~L15 real play is easier than the matrix.
- ACTIONVALUE tests an action in a 2-slot strip next to the weapon basic. Actions whose power depends on the rest of
  the strip (RETRIGGER NEXT re-resolving BASH) or on longer cadences (FIGHT/DUNGEON bonuses) are mispriced there —
  confirm with SEQLAB on a real 4–5 slot strip or with the smart bot. Speed changes only move the defense side of
  ACTIONVALUE (offense is counted in hero turns).
- Smart bot (`--smart`) vs basic bot: from L10 the smart bot kills in ~5–7 hero turns (target 11) and loses about
  half the HP per fight. Raising enemy damage cannot target both bots: ×1.4 on the L9–L20 anchors drops the basic
  bot to ~4% reaching L21 while the smart bot still clears ~97% of dungeons. The anchors are calibrated to the basic bot.
- Class parity: compare death-level medians *and* reach-L20 rates over ≥40 basic runs per class to death; 12 runs per
  class moves the median by ±3 levels from noise alone. Lever: `classBalance.<class>.healthMultiplier` (health per level,
  keyed by equipped weapon type).
- STATVALUE before 2026-10-11 leaked each row's stat line into the next (shared typical-gear weapon), so earlier
  stat prices are wrong. Corrected L10 prices (+~23 points): legacy formula STR +8–22%, AGI +45%, TEC +11–21%,
  INT 0–14%; with `strengthDamagePerPoint` 0.025, STR ≈ AGI (+21–33%), TEC small, INT ~0 (only combo slots).
- Smart shards use ~70 MB each; 24–48 parallel processes are fine on 32 GB.
- Per-fight win rate and dungeon clear rate are different targets: a 95% fight win rate over ~8 fights per dungeon is
  roughly a 66% dungeon clear rate. The batch report prints both per level.
