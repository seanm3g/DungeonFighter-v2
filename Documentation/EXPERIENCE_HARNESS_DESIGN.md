# Player Experience Harness — Design (draft, 2026-10-26)

**Goal:** a balance environment that measures every layer of what a player feels, not just whether the game is
beatable. Today the loop (`BALANCE_PROCESS.md`: scan → focus → gate) tunes **power** very well: turns per fight, clear
rates, survival, cost of each enemy. A patch can pass every one of those rows and still be samey: one strip per class,
four classes that fight the same way, fights that all feel alike, levels where nothing changes, enemies that differ only
in numbers. This harness adds the layers that catch that.

Status: **Power** exists. **Choice** got its first rows on 2026-10-26 (see below), and phase 1 instrumentation
(pacing, damage sources, skill / gear choices, milestones) is recorded but not yet read by the scan. Everything else
here is proposed.

## Principles

1. **Record once, read many layers.** Every layer reads the same real runs (DUNGEONSIM / PLAYTODEATHBATCH, smart and
   basic bot). New layers mostly cost instrumentation, not sim time. The scan must stay ~8 minutes.
2. **Every row has five parts:** a metric, a target (or a relative band), a sample rule (when it may read FAIL), a
   **focus command** to run next, and the **lever** that moves it. A row without a lever is a report, not a gate.
3. **No single "fun score".** A weighted sum hides trade-offs (more power can shrink choice). Each layer reports its own
   rows; the scorecard puts them side by side.
4. **Relative before absolute.** Where the designer has no number yet, judge against the other classes / enemies / levels
   (as `real fights per enemy` already does) and mark the target `TBD`.
5. **Samples are heroes, not fights.** A hero's fights share its build. Intervals and FAIL thresholds count heroes, as
   the class spread and per-enemy rows already do.
6. **Bots are a lower bound.** One scoring function means one idea of "best". Variety measured through it is the least
   variety the game has. Personas (below) and human playtests widen it.

## Shared data model

What one real fight carries. ✓ = recorded today.

| Field | Status | Used by |
|---|---|---|
| Enemy, level, hero/enemy actions, HP start/end/max, fight # in dungeon, died, retreated | ✓ | Power |
| `Strip` (at fight start), `ActionUses` (name → count) | ✓ 2026-10-26 | Choice |
| `DamageBySource`: base hit, combo, crit bonus, DoT ticks, convert/trigger, retrigger, reflect | new | Identity |
| `StatusesApplied` / `StatusesReceived` (type → stacks), status uptime on the enemy | new | Identity, Encounter |
| `HpMinPct` (lowest hero HP in the fight), `LeadChanges`, `LongestMissStreak`, `DeadActions` (miss or 0 damage) | new | Pacing |
| `EnemyMechanicsFired` (EnemyActions kit templates used) | new | Encounter |
| Room tags / room structure of the fight | new (exists as `DF_SIM_ROOM` study) | Encounter |

What one run carries:

| Field | Status | Used by |
|---|---|---|
| `SequenceDecisions` (strip, slot max, required basic, `ScoreVsBest` per owned action) | ✓ 2026-10-26 | Choice |
| `SkillDecisions` / `GearDecisions` (same shape: each candidate's score vs the best) | new | Choice |
| `Milestones`: each level-up, skill point, new slot, equipped drop, with kill-speed and HP-kept before/after | new | Progression |

`CombatEvent` already has `Damage`, `IsCombo`, `IsCritical`, `IsMiss`, `StatusEffectType`, `HealthPercentage`, so most
pacing fields are a recorder change. `DamageBySource` needs DoT ticks and trigger/convert damage to carry a source tag on
the event; that is the one engine-side change.

## Layer 1 — Power (exists)

*Can a player win, and how long does it take?* Median turns per level and per enemy, real dungeon clear, survival to
L10, bot vs median build speed, real fights / HP cost per enemy, class clear spread. Levers: health / damage curves,
identity calibration, `classBalance`, STEP ladders. No change proposed except that the other layers report next to it.

## Layer 2 — Choice

*Does the player have several good options, and do they use them?*

Exists (2026-10-26), per class and level, in `balance-scan.py`:
- **eff**: effective actions used in fights, 1/Σp² over `ActionUses`.
- **breadth**: effective actions over every hero's strip ÷ mean strip length (1 = every hero runs the same actions).
- **viable**: owned actions within 3% of the best strip. Findings: *viable actions* (viable ≤ slot max), *dominant
  action* (> 75% of strips), *dead actions* (never slotted, never within 6%).

First read (L20, 2 heroes per class): viable 2.2–3.0 for 2–3 slots for Barbarian, Warrior and Rogue (no real choice);
Warrior AVENGE and Wizard CONCENTRATED CHAOS in every strip.

Proposed:
- **Skill and gear choice** with the same `ScoreVsBest` trick in `SpendSkillPointsAsync` and `OptimizeGearAsync`
  (no extra sims). Rows: skill node pick rate per class, viable skill picks per point, material / set pick rate.
- **Build convergence:** distinct (material set, skill branch, strip) triples per class over heroes. Target: at least
  2 builds per class per level whose kill speed is within 15% of the best.
- **Build spread gate (BUILDLAB):** catalog builds more than ±25% from the level median kill time are flagged
  as dominant or trap builds.
- **Targets (proposed):** viable > slot max + 1 from L10; breadth ≥ 1.5 from L20; no non-required action over 75%.
- **Levers:** `Actions.json` values (ACTIONVALUE / SEQLAB to price), skill node `power`, gear action pools.

## Layer 3 — Identity

*Do classes, weapons and builds feel different from each other?* Parity says they are equally strong; identity says
they are not the same.

- **Fingerprint** per class (and per weapon, per catalog build), from real fights, each normalized across the roster:
  swing time; hero actions per kill; damage share by source (base / combo / crit / DoT / trigger / retrigger);
  statuses applied per fight; defensive or heal action share; miss rate; HP-loss shape (one big hit vs a steady drip);
  enemy swings per kill (the measure in the class-parity memory).
- **Rows:**
  - *class distance*: the smallest pairwise distance between class fingerprints (z-scored, Euclidean). Flag pairs
    under a threshold `TBD` (calibrate on today's roster: the closest pair today is the floor to beat).
  - *signature share*: each class plays to the affordance of its primary attribute (designer, 2026-10-26), so it should
    lead the roster on that attribute's read: Barbarian (Mace/STR, damage) **hit size**, Warrior (Sword/AGI, swing
    speed) **tempo**, Rogue (Dagger/TEC, hit/combo/crit thresholds) **crit share**, Wizard (Wand/INT, combo amplifier)
    **combo lift**. Flag when it doesn't.
  - *build distance within a class*: catalog builds of one class should differ (material sets exist for this).
- **Focus:** RUNLOG `--verbose` for the two closest classes; SKILLVALUE for signature nodes.
- **Levers:** class skill trees, gear action class share (`weaponGearActionClassShare`), material triggers, weapon speed spread.

## Layer 4 — Pacing

*Does a fight, and a dungeon, have shape?* Averages hide this: the L1 rat pack took 21 turns while every mean passed.

- **Fight rows (per level):**
  - fight length spread: coefficient of variation of hero actions and p90/p50. Too low = every fight the same; too high = slogs.
  - *close calls*: won fights with `HpMinPct` < 25%. Proposed band 5–15% of fights.
  - *lead changes* per fight (the hero's HP % vs the enemy's crosses).
  - *dead streaks*: share of fights with `LongestMissStreak` ≥ 4, and dead-action share. Lever: hit thresholds, TEC.
  - *one-shot share*: fights won in ≤ 2 hero actions (with paper enemies the encounter didn't matter).
- **Dungeon rows:** the HP arc across fights (start → last room), retreat point, whether the last room is the hardest
  fight; deaths spread over fight # (all deaths in fight 1 = a bad opener, all in the last = a good climax).
- **Reuse:** `FunMomentAnalyzer` already defines CloseCall, Comeback, HealthLeadChange, HealthSwing and DominantStreak.
  Feed it the recorder's events instead of the old BattleStatistics path.
- **Levers:** per-enemy HP / bodies, damage curve shape, `maxEnemiesPerRoom`, room count, retreat and heal rules.

## Layer 5 — Progression

*Does every step forward feel like something?*

- **Milestone value:** for each level-up, skill point, new strip slot and equipped drop, the change in kill speed and
  HP kept (scored like the smart bot does, before vs after). Rows: median value per milestone type per level band.
- **Dry spells:** runs of consecutive levels where no milestone moved the score more than 5%. Flag spells of 3+ levels.
- **Felt change vs power change:** a milestone that changes the strip or unlocks a new action counts as felt even when
  it is power-neutral. Report both.
- **Unlock cadence:** levels between new strip slots, new action types and skill tiers (`comboSlotCapByLevel`, skill
  tree tiers). Compare against the designer's pacing table (`TBD`).
- **Loot excitement:** upgrade rate per hub visit, Rare share (ITEMSTATS already gates 1.5–3.5%), drops the bot equips
  within one level.
- **Levers:** XP curve, slot cap anchors, skill point rate, loot rarity and `equippedWeaponTypeBias`.

## Layer 6 — Encounter variety

*Do enemies and rooms ask for different answers?*

- **Counter matrix:** score the hero's owned actions against each matrix enemy separately (the same search the smart
  bot runs, done per enemy; focus tier only, it costs about 12× one search). Row: the share of enemies whose best strip
  differs from the overall best. 0% = one strip answers everything.
- **Enemy fingerprints:** as for classes (swing time, damage shape, statuses applied, HP-loss shape). Flag enemy pairs
  that differ only in numbers. Identity mode equalizes threat; this checks shape survives it.
- **Mechanic engagement:** share of fights where the enemy's kit (`EnemyActions.json` templates) fired, and whether the
  hero lost more HP to that mechanic than to plain hits.
- **Rooms:** effect size of each room tag on kill speed and HP lost (the `DF_SIM_ROOM` study as a scan row). Rooms that
  change nothing are flavor only.
- **Dungeon composition:** effective number of distinct enemies per dungeon and per level.
- **Levers:** `Enemies.json` growth shapes, enemy action kits, room tags, dungeon enemy pools.

## Scorecard

One table per scan, a row per class × level band, a column per layer, each cell PASS / LIKELY / FAIL with its worst
finding. Findings keep today's format (status, value, target, focus command) and rank together, so a power fix that
kills choice shows up as resolved + new in `--compare`.

## Bots and humans

- **Personas:** score functions besides win rate × speed × HP kept: *aggressive* (speed only), *careful* (HP kept),
  *showy* (crits / combos), *status* (status uptime). Actions viable for at least one persona aren't dead. Overnight tier only.
- **Score against more enemies:** today the smart bot scores against Wolf and Goblin only, so "viable" means viable
  against two plain attackers. Add one tank, one caster and one pack (~2× evaluation cost).
- **Basic bot** stays the floor for power rows.
- **Humans:** `/playtest` and the MCP server are the ground truth. Each playtest note ("this fight dragged", "I always
  use X") should map to a row. When it doesn't, the harness is missing a metric.

## Cost and tiers

| Tier | Adds | Budget |
|---|---|---|
| Scan (~8 min) | Choice, pacing, identity and progression rows from the existing DUNGEONSIM runs | +instrumentation only |
| Focus | Counter matrix, build spread, persona runs for one class / level | minutes each |
| Gate (`balance-check.ps1`) | Experience rows whose targets the designer has set, from `balance-targets.json` | +~2 min |
| Overnight | All personas, more heroes per class (6+), room study | hours |

Sample sizes: choice and identity rows need ≥ 6 heroes per class before FAIL; the default scan has 2 per level (8
pooled), so most will read LIKELY in the scan and be confirmed in focus.

## Targets file sketch

Live in `GameData/TuningProfiles/balance-targets.json` (`experience`, 2026-10-26); scan flags override them:

```json
"experience": {
  "classSignatures": { "Barbarian": "hit size", "Warrior": "tempo", "Rogue": "crit", "Wizard": "combo lift" },
  "signatureActionsMayDominate": true,
  "closeCallBand": [5, 15],
  "maxDryLevels": 1,
  "minDistinctCounterShare": 50,
  "viableWithin": 0.03, "dominantShare": 0.75,
  "minLengthCv": 0.25, "maxLengthP90": 2.5, "maxMissStreakShare": 10, "maxOneShotShare": 25,
  "minClassDistance": 0.25
}
```

`maxDryLevels` and `minDistinctCounterShare` have no scan row yet (phase 3). `minClassDistance` 0.25 sits just under the 2026-10-26 baseline's closest pair (Barbarian ~ Wizard 0.27), so it flags convergence from today. Baseline: `GameData/TuningRuns/balance-scan-baseline-2026-10-26.json` (local, gitignored; 6 heroes x 4 dungeons per class at L10-50).

## Phases

0. **Done:** strip / action-use logging, sequence `ScoreVsBest`, choice rows in the scan.
1. **Instrumentation (done 2026-10-26, no scan rows yet):** fights carry `HpMinPct`, `LeadChanges`, `HeroMisses`,
   `LongestMissStreak`, `DamageBySource` (hit / dot / retrigger / reflect / environment) and `CritDamage` /
   `ComboDamage` / `ConvertDamage` (overlapping parts of hit). Runs carry `SkillDecisions`, `GearDecisions` (each
   option's score over the best, "keep" included) and `Milestones` (smart bot only). DUNGEONSIM `--json` and
   PLAYTODEATHBATCH `--fights-json` write them. Damage sources come from `DamageLedger`: a null-by-default tap in
   `CharacterHealthManager.TakeDamageWithNotifications` plus a tag scope around DoT ticks in `StatusEffectProcessor`.
   It reads HP only. Converts and triggers don't deal separate damage (converts add to the hit; triggers buff), so
   convert damage is the hit damage of convert actions and triggers show up through the stats they move.
   Still to record: `StatusesApplied` / `StatusesReceived`, `EnemyMechanicsFired`, room tags.
2. **Scan rows (done 2026-10-26):** pacing (length CV / p90, close calls, miss streaks, one-shot fights, lead changes)
   and identity (class fingerprints, closest class pair, signature share) in `balance-scan.py`; see BALANCE_PROCESS.md
   for flags and defaults. Not yet: dungeon-arc rows, per-weapon / per-build fingerprints, the scorecard table.
3. **Progression and encounter:** milestone values, dry spells, enemy fingerprints, room effect sizes; counter matrix
   as a focus tool.
4. **Gate and personas:** `experience` targets in `balance-targets.json`, set by the designer from phase 2–3 reads;
   persona scoring and wider evaluation enemies in the smart bot.

## Designer answers (2026-10-26)

- **Signature:** each class plays differently, from the affordances of its primary attribute (Mace STR, Sword AGI,
  Dagger TEC, Wand INT; `CharacterStats.GetPrimaryStatLabelForWeapon`). Encoded as the four signature reads above.
- **Close calls:** a won fight should feel dangerous periodically, not all the time. Band 5-15% of won fights under
  25% HP, at every level.
- **Unlock pacing:** at most 1 level may pass with no new action, slot or skill tier (`maxDryLevels` 1).
- **Encounters:** enemies may share a best strip, but most should want a different one (`minDistinctCounterShare` 50%).
- **Signature dominance:** a class's rare class actions (tags `rare` + `class` + the class name, e.g. Warrior AVENGE)
  may dominate its strips; the scan tracks their share as a metric instead of flagging them.
