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

## Scan → focus → gate

Don't run the full check after every change. Run the three tiers in order:

1. **Scan** (`python Scripts/balance-scan.py`, ~7–8 min on 24 cores): the median catalog build grid at every matrix
   level plus 2 heroes × 4 dungeons per class at L10/20/35/50, in one pool. It prints a level summary and a ranked list
   of problem areas, each with the **focus command** to run next. Each scan writes
   `GameData/TuningRuns/balance-scan-<patch>-<time>.json`. Pass it to the next scan with `--compare` to see every metric
   before/after, plus which findings were resolved and which are new. `--from <json>` re-analyzes a saved scan with no
   sims (use it after changing thresholds). Narrow it to one area with `--median-levels 20 --dungeon-levels 20`.
2. **Focus**: run only the commands the scan printed. Examples: `dungeon-sim.py --solve L --target P` for a clear
   rate, `median-curve-solve.py --only health --levels L` for one health anchor, BUILDLAB / FIGHTLOG for one enemy.
   Re-scan with `--compare` to confirm the fix and catch side effects.
3. **Gate**: `balance-check.ps1` once, before saving the patch.

What the scan rows mean:
- **median turns / per enemy**: the median catalog build (the hero the HP curve is solved for). These rows are nearly
  deterministic: a FAIL here is real.
- **dungeon clear**: real dungeons. A hero's dungeons share its build, so the scan's sample is the number of heroes (8 per
  level), not the number of dungeons. The interval is a t-interval over per-hero clear rates, never narrower than the
  Wilson interval, and comes out about ±30 points. The scan only catches gross misses here: fit to ±4 with
  `dungeon-sim.py`. The first scan read one hero per class: class clear swung 20–36 points between two runs of the same
  patch.
- **bot vs median build**: the bot's hero actions per real fight vs the median build's hero turns. Outside 1/1.33–1.33×,
  the stepping stone `STEP[L]` in `median-curve-solve.py` is not what real play has at that level. The HP curve is then
  solved for the wrong hero: fix `STEP` (or the build ladders) before re-solving health.
- **real fights per enemy / real HP cost per enemy**: enemies judged against the level's real mean (±36%, the same share as
  `perEnemyTurnsTolerance`), not against 11 turns. A level-wide gap belongs to the bot-vs-median row. FAIL needs ≥ 3 ×
  `--min-fights` fights from ≥ 4 heroes; anything less reads LIKELY.
- **class spread**: per-class clear over all scanned levels vs the all-class mean (per-hero interval).
- **action variety** (2026-10-26): DUNGEONSIM fights carry the hero's `Strip` and `ActionUses`, and each smart-bot
  sequence search writes a `Decisions` row (`ScoreVsBest`: every owned action's best evaluated strip over the best strip).
  The scan prints per class and level *eff* (1/Σp² over action uses), *breadth* (effective actions over all heroes'
  strips ÷ mean strip length; 1 = every hero runs the same actions) and *viable* (owned actions within
  `--viable-within`, default 3%, of the best strip). Findings: **viable actions** (viable ≤ slot max: the strip is not a
  choice), **dominant action** (a non-required action in more than `--dominant-share`, default 75%, of a class's strips,
  pooled over levels; FAIL from 6 heroes) and **dead actions** (owned in ≥ 3 searches, never slotted, never within 6%).
  The bot's score is win rate × kill speed × HP kept against Wolf and Goblin, so "viable" means viable for that goal.
- **pacing** (2026-10-26, per level over all classes): fight length CV (`--min-length-cv` 0.25) and p90/p50
  (`--max-length-p90` 2.5), **close calls** (won fights where the hero fell under 25% HP, band `--close-call-band` 5,15),
  fights with a 4+ hero miss streak (`--max-miss-streak-share` 10%), one-shot fights (won in ≤ 2 hero actions,
  `--max-one-shot-share` 25%), lead changes per fight (metric only). FAIL from 6 heroes at the level.
- **identity** (2026-10-26): a fingerprint per class over every scanned level (won fights): damage shares (dot,
  retrigger, reflect; crit / combo / convert parts of hit), miss rate, HP lost per fight, kill speed and enemy swings per
  kill (log vs level mean), the primary-attribute reads (hit size per hero hit, tempo = hero actions per enemy action,
  both log vs level mean; combo lift = log of combo-hit over plain-hit damage), action spread. **class distance** flags
  the closest pair under `--min-class-distance` (0.25, just under the 2026-10-26 baseline's Barbarian ~ Wizard 0.27). **signature share** flags a class that doesn't
  lead the roster on its primary attribute's read: Barbarian hit size (STR), Warrior tempo (AGI), Rogue crit (TEC),
  Wizard combo lift (INT).
- **strips** (2026-10-27, designer: INT amplifies later slots, Wizard runs the most slots throttled by reliability, TEC
  drives the sequence toward always completing): DUNGEONSIM fights carry `SlotSwings` / `SlotHitDamage` (by the slot
  the AMP exponent uses), `SlotMax`, `HeroTechnique`, `HeroIntelligence`. Per class and level the scan prints slots
  used / max, **strip completion** (last-slot over first-slot swings), **step advance** (chance a swing moves the strip
  on), **late-slot damage** (share of strip hit damage from slot 2 on), TEC and INT, plus step advance by TEC band over
  all classes. Finding: **wizard slots** (Wizard doesn't use the most slots at a level). Completion and advance are
  metrics only until the designer sets bands. **Strip breaks** (`StripBreaks`, `StripCompletions`, `CarriedInStrip`):
  per 100 hero swings, why a strip under way fell back to slot 1 — the hero's normal hit, miss, combo routing, an
  enemy action in between (Disrupt kits and the like), environment, or the strip itself changing — by class / level
  and by TEC band. Fights that start mid-strip (pack bodies inherit the strip) are left out of step advance; step
  advance still runs above 100% where a wrap restarts on slot 2, so read breaks and completions first.
- Experience bands default to `experience` in balance-targets.json (designer answers 2026-10-26; the flags override).
  A class's rare class actions (e.g. Warrior AVENGE) may dominate its strips: tracked as `signature action share`, not
  flagged.

First scan (overnight-v2, 2026-10-13): median turns L35 9.1 (FAIL), Goblin at L50 16.4 turns for the median build. The bot
kills ×1.67 faster than the median build at L10 and ×0.72 slower at L35: `STEP[10]` (two-piece + convert) undershoots
real play, `STEP[35]` (full set) overshoots it.

## One command

```powershell
.\Scripts\balance-check.ps1
```

Uses the active patch (`GameData/PatchProfile.json`; `-Patch <name>` to override). Builds the tools into
`Code/bin/BalanceTools`, then runs in parallel: the **median catalog build** at each target level
(`Scripts/median-curve-solve.py --check`: fight win rate + hero turns, the hero enemies are calibrated to), smart-bot
playthroughs to L10 on a scratch copy of GameData (survival + real L10 clear, 6 shards × 4 runs per class), and loot
stats. Prints a PASS/FAIL scorecard against `GameData/TuningProfiles/balance-targets.json` (exit code 0 = all pass).
About 10–16 minutes on 24 cores (it was ~25–28 before 2026-10-12; see measurement notes). `-SkipSurvival` skips
playthroughs (~5 min); `-SkipBuild` reuses the last build; `-ToolsDir <folder>` runs from another folder (use it when
sims still hold `Code/bin/BalanceTools/DF.dll`); `-BasicBot` adds the non-optimizing bot as a reported floor;
`-BareHero` adds the old typical-gear one-action matrix as reference rows (not counted).
**Since 2026-10-13 (no post-fight heal):** the check also runs `Scripts/dungeon-sim.py` at every `realClearAnchors` /
matrix level above `realClearCheckLevel` (below 100) and gates on real dungeon clear (HP carried between fights,
bot retreats below `retreatBelowHpPercent`), interpolated from `realClearAnchors` (L10 75, L15–L50 65, L75 30, L100 0).
The per-fight win-rate rows then print as `INFO` (full-HP reference only); hero turns stay gated. Adds ~8 min per level;
`-SkipDungeonSim` skips it. Fit a level's damage anchor with `dungeon-sim.py --solve L --target P`, not WRSOLVE.
Win-rate anchors are **dungeon clear rates** (`winRateMeaning: dungeonClear`); the matrix checks the matching single-fight
rate (clear^(1/fightsPerDungeon)) and the playthrough batch checks the real clear rate at `realClearCheckLevel`.

**Per-enemy checks (2026-10-13).** A playtest's first L1 fight (rat pack) took 21 hero turns while every averaged row
passed: pack turns jump between levels (bodies × swings per body, overkill wasted), and one slow enemy hides in a mean.
Two gates now look per enemy, both against `heroTurnsTarget` ± `perEnemyTurnsTolerance` (balance-targets.json):
- `L<level> per-enemy turns`: median catalog build per enemy (BUILDLAB rows carry `PerEnemy` mean / p90 turns / win rate).
  Its L1 hero has typical L1 armor and so hits harder than a fresh character: it read the old rats as 11.8, not 16.
- `L1-3 / L4-6 / L7-10 real fights per enemy`: every fight the smart bot actually played to the survival level
  (`PLAYTODEATHBATCH --fights-json F`), enemies with at least `perEnemyMinFights` fights (default 20). This is the
  one that sees what a player sees.
`median-curve-solve.py --check --levels 1` runs a single level (~30 s) for quick per-enemy reads.

## Targets (designer, 2026-10-10)

| Target | Measure | Tool |
|---|---|---|
| ~11 hero turns per even-level fight, every level, for the **median catalog build** at the level's expected stepping stone (2026-10-12) | median hero turns over the 23 builds | `Scripts/median-curve-solve.py` (BUILDLAB `--level-slots`) |
| Win-rate curve for the median build; the late game may punish builds that fall behind | median win rate | `Scripts/median-curve-solve.py` |
| Action slots grow with level: 1 at L1 (`lootSystem.comboSlotCapByLevel`) | slot max | BUILDLAB `Slt` column |
| Dungeon clear 100% at L1, 95% at L10, 0% at L100 (linear) | real clear rate + matrix fight win rate | PLAYTODEATHBATCH / FUNDMATRIX / WRSOLVE |
| Hero survives 100% of runs to L10 | % of playthroughs reaching L10 | PLAYTODEATHBATCH `--stop-at-level 10` |
| All four weapons ~11 turns, differentiated by speed and variety | per-weapon turns, swing time | FUNDMATRIX, FIGHTLOG |
| Starter swing 7–10 s, mastery ~2 s near L100 (2026-10-14: `agilityMax` 200, curve exponent 0.8, so a Warrior stops hitting the floor at L35) | swing time at AGI 10 / 200 | FIGHTLOG header |
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
| `SKILLVALUE --level L [--classes C] [--slots 2]` | Prices every skill-tree node at rank 1 and max rank on a typical-gear hero (value 100 = no change, plus value per skill point), and the whole tree at max rank (`ALL`). Gear-dependent passives are priced with typical gear only, so they read low next to a hero who collects matching pieces. |
| `ITEMSTATS --detail` | Also prints gear actions by slot: class / neutral / other-class / basic share and the top actions. |
| `PLAYTODEATHBATCH N [--stop-at-level 10] [--retreat-below 35]` | N full playthroughs per class (bot equips upgrades). Survival %, deaths by level / fight # in dungeon / killer. `--retreat-below N` makes the bot leave a dungeon between rooms when HP is under N% of max (leaving fully heals, no rewards; counts as an uncleared dungeon in DungeonClear). `balance-check.ps1` passes `retreatBelowHpPercent` from `balance-targets.json`. |
| `BUILDLAB [--builds B1,W3] [--level L] [--file F] [--long 4] [--loo-step N] [--aoo-base N --aoo-upto M] [--max-step N] [--power-all X] [--power id=X]` | Walks each build in `GameData/TuningProfiles/build-ladders.json` (or `hybrid-ladders.json`) up its stepping stones (strip, material pieces, named piece triggers, weapon mods, skills, gear upgrade) and prints each step's speed-up (kill time in enemy swings) and HP-lost change. `--long 4` = enemy HP ×4 and hero HP ×40 so damage per turn is measured without whole-turn rounding (offense only: % max HP heals and poison are inflated there, take defense from a normal run). `--loo-step` prices each learned skill by removing it; `--aoo-base` prices each skill added alone on top of a step. `--power*` multiply node power for the run (×10 finds nodes that never respond). |
| `RUNLOG --class C [--verbose]` | One playthrough traced fight by fight, with gear at each hub visit; `--verbose` prints every action with swing times and thresholds. |
| `PLAYTODEATHBATCH ... --start-level N [--start-loot-per-level K]` | Starts each run at level N: normal level-up rules plus a bag of K real drops per level (default max(4, level/2): matches heroes who played up from L1 at L10 and L15; unverified above L15) for the 8 levels below N, then the bot picks gear / skills / sequence. Mid-game probes without playing from L1. |
| `DUNGEONSIM --level L [--runs N] [--dungeons D] [--retreat-below P] [--classes C] [--basic] [--json F]` | Real dungeons at a fixed hero level with HP carried between fights: start-level hero, XP held at 0, D dungeons per hero through the real game loop (rooms, room search, exit prompt, retreat rule). Clear / retreat / death per dungeon with 95% intervals. Use `Scripts/dungeon-sim.py` to shard it. |

**`Scripts/dungeon-sim.py`** shards DUNGEONSIM across cores on a scratch GameData copy:
`--levels 10,20` measures the active patch; `--anchor 20=1.2` tries a damage-curve variant; `--solve 20 --target 70`
bisects the L20 damage anchor to a target clear rate. This is the multi-fight counterpart of WRSOLVE: WRSOLVE assumes
every fight starts at full HP (clear^(1/fightsPerDungeon)), which stopped being true when the post-fight heal went to 0.

**Provenance.** Every headless tool prints `Provenance: build=<DF.dll hash> git=<commit>[+dirty] data=<GameData hash>
patch=<name>@<hash>` first. Two results are comparable only when build, data and patch match; `balance-check.ps1` adds
the line to its JSON and marks it `MIXED` when its runs disagree. **Error bars.** Survival and real clear rows show a 95%
interval and `n`; a row whose value misses the target but whose interval still reaches it reads `UNSURE` (rerun with
more runs) instead of FAIL.

## Loop

**Since 2026-10-12, enemy curves are calibrated to the median build.** `python Scripts/median-curve-solve.py --patch
<patch> --tools <dir> --passes 2` runs all 23 catalog ladders in BUILDLAB at the stepping stone a player should have by
each anchor level (L1 default strip, L5 class actions, L10–20 two-piece set + convert, L35 full set, L50 weapon mods,
L75 catalog skills, L100 core & rite), with strips trimmed to the level's slot cap (`--level-slots`). Health anchors
target ~11 hero turns for the median build (against near-harmless enemies), damage anchors the win-rate targets. A
bare typical hero (FUNDMATRIX, the basic bot) now sits below the curve by design: builds are expected, and the late
game punishes heroes who skip them. The steps below still apply for fundamentals and items.

1. **Fundamentals first (L1, no gear):** FUNDMATRIX `--level 1`. Hero turns ≈ 11, every weapon within ±1 turn.
2. **Action sequence:** SEQLAB / ACTIONVALUE. Outliers (value far from 100 at their tier) get data changes in `Actions.json`.
3. **Level scaling (typical gear):** FUNDMATRIX at 1, 5, 10, 20, 35, 50, 75, 100. Enemy HP curve keeps turns ≈ 11
   (`enemySystem.baselineStats.health`, `progressionScales.healthGrowthScale`, `progressionPivotLevel`).
4. **Win-rate curve:** WRSOLVE at each anchor level → `enemySystem.damageCurve.anchors`. Re-run step 3 to confirm.
5. **Items:** ITEMSTATS for rarity; ItemValueModel scores for gear power per level.
6. **Survival:** PLAYTODEATHBATCH `--stop-at-level 10` on a scratch GameData copy. Read the death breakdown.
7. **Enemy parity (identity mode):** `python Scripts/enemy-calibrate.py --patch <patch> --iterations 3` (whole roster,
   ~3 min per pass), then WRSOLVE again for the damage curve. Check a different seed with FUNDMATRIX `--seed 7`.
8. Save the patch, note what changed and why.

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
| `enemySystem.archetypeCombat.<Archetype>.damage` / `.attackTime` | profile default | Per-archetype outgoing damage and swing-time multipliers (attack time below 1 = faster). Defaults live in `ArchetypeManager.GetArchetypeProfile`. Solve damage so each archetype costs the same HP per fight (see notes). |
| `enemySystem.identity` | off | Identity mode: every enemy's damage base, swing time and HP come from level references, and its growth row only sets its shape (AGI-leaning = faster + lighter hits, STR-leaning = slower + heavier, tanky = more HP + lighter hits; threat stays the same). Shape strength `intensityAtLevel1` (0.5) → 1 at `fullIntensityLevel` (50). Archetype attack time folds into the shape; archetype `damage` overrides are ignored. |
| `SkillTrees.json` node `power` | 1.0 | Scales a skill's per-rank magnitude (custom effect amount × rank × power). Keep the node's `effect` text in step with the scaled number. |
| `combat.materialConvertPercentPerUnit` | 0 | > 0: converts pay a % of the hit per banked unit instead of flat damage (flat stops mattering as hits grow). |
| `combat.materialConvertPercentByKeyword` | {} | Percent mode: % per unit by currency (RAGE, CRISIS, CONTACT …). Mint rates differ by 10–100×, so each currency has its own rate. |
| `combat.materialFiveStackMintMultiplier` | 0 | Mint at 5 pieces = feed × this (0 = legacy feed × feed = 25 per trigger). |
| `combat.speedOverflowDamageEfficiency` | 0 | Hero swing faster than `minimumAttackTime` turns the excess into damage: × (1 + this × (min / raw − 1)). |
| `combat.agilityOverflowSpeedPerPoint` | 0 | Hero AGI above `agilityMax` keeps speeding the raw swing (feeds overflow). |
| `combat.strengthOverflowEfficiency` | 0 | STR (and INT potency) above the 100 cap counts at this share. |
| `combat.intelligenceAmpPerPoint` | 0 | INT potency: hero AMP × (1 + INT × this). Without it INT only gates strip slots. |
| `lootSystem.materialPieceTriggers` | false | Each material piece rolls one trigger from its material's pool (`MaterialTriggerCatalog`) and keeps it on load. |
| `combat.statusFortifyDefensePerStack` | 0 | Defense per active Fortify stack in units of K (as Harden). 0 = legacy: 1 flat armor per stack, and skill-granted Fortify gave no defense at all. |
| `combat.dotDamagePerLevel` | 0 | Burn / bleed tick damage × (1 + target level × this). Stacks are flat integers, so a 1-stack bleed did 1 damage at L60. |
| `enemySystem.actionKits` | false | Enemy flavor actions resolve through `GameData/EnemyActions.json` (15 mechanic templates: Venom, Scorch, Rend, Crush, Sunder, Hex, Ensnare, Disrupt, Shell, Drain, Frenzy, Mend, Curse, Concuss, Pierce). 229 of 238 flavor names had no action and fought as plain attacks. |
| `enemySystem.identity.calibration` | {} | Enemy name → level anchors × damage, solved by `Scripts/enemy-calibrate.py` so every enemy costs the same HP at every level. Re-run after editing `Enemies.json` or enemy actions. |
| `equipmentScaling.armorMultiplier` / `armorFlatPerPiece` / `armorPerItemLevel` / `armorSlotWeights` | 1 / 0 / 0 / {} | Armor piece base = catalog × multiplier + slot weight × (flat + per level × item level), read at armor-total time (saved items follow the patch). Catalog head/feet armor is 0–3, so defense barely mattered. |
| `combat.retriggerDamageMultiplier` | 1 | Hero damage on retrigger (encore) swings, which re-resolve late, high-AMP strip slots. |
| `lootSystem.comboSlotCapByLevel` | [] | `[level, slots]` anchors capping base + gear + class strip slots (1 slot at L1). Action Lab sandbox slots bypass it; test runners clear it (`TestBase.ClearBalancePacingForTests`). |

## Tests

`dotnet DF.dll --run-tests` runs every suite; a suite that throws is reported under **SUITES THAT CRASHED** and the run
continues. Exit code 0 = every test passed and nothing crashed. Baseline on 2026-10-11 (active patch overnight-v1, identity mode): 8884 tests, 8801 passed, 83 failed, same 5 suites
crashed. Flaky: "Test enemy has combo strip" and "Enemy selected action should be from enemy's action pool" pass or fail run to run.
Earlier baseline (active patch 10-10-2026):
8875 tests, 8784 passed, 91 failed, 5 suites crashed (EquippedItemTriggerTests, DataSystemTestRunner,
ActionsSystemTestRunner, CombatSystemTestRunner, UISystemTestRunner — the last needs the Avalonia platform).
Treat new failures against that baseline as regressions.

## Measurement notes

- **Simulation speed (2026-10-12).** A smart-bot playthrough to L10 cost ~2.5 CPU-min (≈7 under a full parallel check):
  98% was candidate fights. Profiling (`DF_PROFILE=1` prints `ToolProfiler` timings for PLAYTODEATHBATCH and
  FUNDMATRIX; `DF_SMARTPROF=1` splits smart decisions) found ~45% in `SkillEffectRouter.OnHit` (60 skill lookups per
  hit, each re-running `EnsureSkillTreeRootsGranted`) and ~30% building combat-log text nobody sees. `SkillRanks` now
  caches each hero's effect table (rebuilt when ranks, node powers, class points or trees change) and pins it per hit;
  scoped-muted simulations (`CombatUiMuteScope.IsScopedMuted`) skip log text. A simulated turn went 1.40 → 0.33 ms;
  seeded BUILDLAB output is identical before and after.

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
- Enemy archetypes: the matrix covers all nine archetypes (12 enemies, `matrixEnemies`). Solve
  `archetypeCombat.<A>.damage` by scaling each archetype by (mean HP lost / its HP lost) over L10–L50, one damped
  repeat, then re-run WRSOLVE for the damage curve. Win rate is useless for this: easy archetypes sit at 100%.
  Superseded by identity mode (below) for overnight-v1: a constant archetype multiplier cannot fix per-enemy drift.
- Identity mode + calibration (2026-10-11): whole roster (128) within ±1–3 points of the level mean HP lost from L1 to
  L75 on an unseen seed; worst enemy win rate 84% at L50 and 69% at L75 (was 9% / 17%). Calibration multipliers stay
  0.81–1.56 (pack enemies need the most). TEC (hit/crit thresholds) and INT still scale from raw stats, which is
  what the calibration mostly absorbs. The damage curve dips after L35 because reference enemy agility keeps
  speeding enemies up until ~L64.
- Early survival vs L10 clear: the matrix L10 target (99.1% per fight) and 97% survival to L10 (~86 fights) conflict
  if L1–L9 enemies are as dangerous as L10. Keep L1–L9 anchors low (0.40–0.45) and let the curve rise at L10.
  Measure survival with `-RunsPerClass 60`; 25 runs per class moves the result by ±3 points.
- Skill tree (2026-10-12): the basic bot never spends skill points, so skills were never tuned. The smart bot killed
  in 2–4 hero actions from L6 (Barbarian / Wizard). Cause: magnitudes written for the old stat scale (+25 STR per
  empty slot, +8–10% per matching item per rank, level as STR) plus a bug where any skill AMP lifted openers to the
  full TEC combo amp. Fixes: per-node `power` (gear passives 0.5% per item per rank, Empty Fury +1 STR per empty slot
  per rank, Puberty +10 STR, Commission +10 AGI, AGI-per-hit skills +1 per rank); skill AMP is its own
  multiplier (`ConsumedSkillAmpPercent`). Whole tree at max rank (SKILLVALUE ALL): Barbarian 125–133, Warrior 122–149,
  Rogue 108–114, Wizard 99–112. Smart bot L16–21: Barbarian 4.5, Warrior 8.3, Rogue 6.5, Wizard 7.8 hero actions per
  fight, about 1.3–1.6× the basic bot on the same weapon (target 30–40% faster kills).
- Dead skills: Wizard INT skills (Apprenticeship, Combo-INT, Slot Mind), Rogue Steady Hand (TEC streak) and
  Initiation price at ~100 because INT and TEC barely change combat outcomes; Concentrated Chaos (`concentrated_chaos`)
  has no handler. These need a design decision on what INT / TEC do, not a power change.
- Overnight build pass (2026-10-12, overnight-v2): BUILDLAB prices every BUILD_CATALOG build step by step; see
  `Documentation/BUILD_LADDER_REPORT.md`. Health-curve solving must measure against near-harmless enemies
  (`Scripts/curve-solve.py`): at lethal levels the hero dies in ~3 turns and FUNDMATRIX's "HP needed for 11 turns" reads
  that as "kills too fast" (the L100 anchor ran 2.8 → 60). Re-solve after any hero-side change: the matrix hero wears
  typical gear, so piece triggers, DoT mods and knobs all move it (piece triggers alone were ×2.9 before the trigger pool
  was re-priced).
- BUILDLAB noise: paired seeds, but any change shifts the dice, so 20–30 fights × 3 enemies is about ±3–5% per value.
  Skill nodes worth +4% sit inside that; read the solver's "no response" list as "below noise", not "proven dead".

