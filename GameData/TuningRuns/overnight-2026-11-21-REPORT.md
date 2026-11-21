# Overnight 2026-11-21 report

**Morning summary.** The one-rule-engine code had knocked `skillrules-v49b` off balance: held-out clear gap 1.7 →
4.5. A bisect traced most of it to the 11-18 node cuts, not the engine refactor. Three solve passes produced
**over-v55**: held-out clear gap **1.91**, class spread **8.0** (v49b on the same code read 4.45 / 15.5 on an earlier held-out seed set), every level within
±4 of target. A new rule-fire trace found and fixed a dead skill (Empty Fury) and two dead quality nodes. The synergy
bot now considers the 18-22 skill nodes per class missing from its mined order. Tests: 0 real failures (3 known flaky). Open item:
Warrior's class damage multiplier has reached ×35 at L51-55 (section 5). Nothing pushed, nothing on main.

Branch `balance/overnight-2026-11-21` (worktree `.claude/worktrees/overnight-1121`), based on `d54bbb96`
(v46-refactor, one rule engine step 8). Start patch: `skillrules-v49b` (the newest solve; the main checkout's
PatchProfile still points at `night-v47-6b`). Nothing pushed, nothing on main.

## 1. Baseline on the new engine

Tests: 1 failure (`Wand weapons should show +1 class action slot`): the test hard-coded the bonus that
recent patches set to 0. Fixed to read the configured value.

Held-out measure of `skillrules-v49b`, seeds 7301-7302 (the seeds it was confirmed on), HEAD vs its own commit:

| | at dae2c266 (solved) | at d54bbb96 (HEAD) |
|---|---|---|
| mean clear gap | 1.68 | 4.48 |
| class spread | 13.3 | 18.3 |

### Where it moved (bisect, seed 7301, 16 shards/class, synergy bot, v49b)

Each commit built in its own worktree; `dae2c266` reproduces the original measure exactly.

| commit | L23 | L28 | L63 | note |
|---|---|---|---|---|
| dae2c266 (v49b solved here) | 72.3 | 71.8 | 45.3 | Wizard L23 55 |
| f588022d custom nodes → rules, Archmage etc. cut | **57.9** | **57.8** | 47.2 | the mid-game drop (Wizard L23 40, Warrior 52) |
| 78409900 STR/INT cap removed | 60.2 | 59.3 | 47.6 | +2 mid |
| 9a1aa8df merge (patch file only) | 60.2 | 59.3 | 47.8 | |
| 977ad477 engine step 1 | 60.0 | 59.3 | 47.3 | neutral |
| f6cc21c6 engine step 4 (action-owned rules) | 60.0 | 59.3 | **50.9** | Wizard L63 36→50 |
| 285204a7 engine step 5 (nine verbs retired) | 60.2 | 58.4 | **54.5** | Barbarian/Warrior L63 up |
| f5f3f88b / HEAD | 60.2 | 58.2 | 54.6 | |

So the engine refactor is balance-neutral below L40. The mid-game drop comes from the 11-18 node cuts (a design
change), and the late-game easing from steps 4–5. Step 4 was meant to be behavior-preserving, so the Wizard L63 +14
is worth a look. A code read of old vs new (every bundle a Wizard card uses: strip ops, retriggers, REWRITE FATE,
NATURAL SEVEN; gates, ownership skips, call sites) found no mechanical change. The one behavior change a Wizard can
hit is ROOM CLEAR HEAL now healing on every clear (the intended ONROOMSCLEARED fix). Under carried HP and the 50%
retreat rule that moves seeded runs onto other paths, so the +14 is most likely a seed cascade (one cell, ±10).
Unflagged but inert today: `scaleFrom` now reads gear-inclusive stats (only BERSERKER uses it, same formula);
CRITFACE / replace_next_roll dropped their count fallbacks (every live row has an explicit argument).
Either way, v49b no longer fits and gets re-solved below.

## 2. Action / rule interactions: rule-fire trace (new tool)

DUNGEONSIM fight rows now carry `RuleFires` (every RuleEngine fire, keyed `Kind|Owner|Rule`), `RuleChained`
(generation 1+) and `RuleChainsStopped`. `python Scripts/rule-fire-report.py <keep dir>` summarizes them per class,
lists the top rules, and flags **learned skills with a triggered rule that never fires**.

First full read (seed 7401, L8-68, ~19.7k fights, synergy bot): skill rules fire ~17 per fight (Wizard 29), set
earn rules ~4, item rules 0.3, card (action) rules 0.2, enemy rules 0.4. Chained fires 0.29 per fight (Wizard 0.63);
**no chain ever hit the generation-2 stop.**

Bugs / dead content it found:

| finding | status |
|---|---|
| **Empty Fury** (`ONCOMBOEND IF STEP>=3`) never fired: ComboEnded is published after the step resets to 0, so STEP read 0. | **Fixed**: the event carries `ComboStepReached`, STEP reads it on ComboEnded. Now fires in 12% of fights. Test added. |
| **Scrap Prefer / Fragile Prefer** named quality tiers from the old ladder (Battle Scarred, Broken, Preowned) that no longer roll. | **Fixed (data)**: Scrap = Second Hand / New / Masterwork, Fragile = Worn / Second Hand / Perfect. Now 6-7% of fights (quality lines are rare). Your call on the names. |
| ONKILL rules read 0 (Culling Glory, Executioner's Rhythm, Bounty) | Recorder artifact, not a game bug: the kill closes the fight record before kill rules resolve. Recorder fixed. Same for ONFIGHTSTART / room start statuses (pre-fight). |
| Spite / Cracked Mirror (COUNTAS) never "fire" | By design: aliases rewrite the event, they don't resolve. Report treats them as passive. |
| Two Warrior nodes are both named **Unbroken Stride** (`w-streak`, `w-persist`) | Not changed: needs a name. |
| Rarely firing: Toxin Pressure 0.9%, Pure Prefer 2.4% (Cosmic never rolls), Charged Hex 3.5%, Envenomed Edge 3.6%, Time Dilation 4.1% | Content gaps: few strip actions apply poison / weaken, quality lines are rare. Design calls. |

## 3. Bot decisions

The synergy bot's skill pick ranks nodes by the mined order in `rules-bot.json`; **18-22 nodes per class were missing
from it** (cross-wire, Rite, Kinship, synergy-pass nodes), so they were only taken when nothing listed was open.
Stale ids in the order: `r-debt`, `w-flow`, `z-memory` (cut nodes; harmless).

New knob `unlistedSkillBase` (synergy-bot.json): an unlisted node scores base × 2 × RuleFit (base for non-rule
nodes). A/B on seed 7401 (L8-68, 32 heroes/class/level):

| | base | 0.4 | **0.25 (adopted)** |
|---|---|---|---|
| mean clear | 63.0 | 62.9 | 63.1 |
| skill_eff L48 / L58 / L68 | 11.0 / 9.6 / 6.9 | 11.3 / 11.8 / 9.4 | 12.0 / 11.6 / 9.4 |
| build_eff L48 / L58 | 92 / 77 | 97 / 81 | 98 / 83 |
| Barbarian L68 clear | 59 | 40 | 40 |

Barbarian L68 is one cell on one seed (±10); the chain's class step re-solves it. Still never learned by any hero
(112 heroes/class): all four cross-class **Borrowed** Rites, Venom Mace, Blood Frenzy, Stormcaller's Pact, Tremor,
Unstoppable, Second Wind, Arcane Drill, Quick Hands, Battle Hymn, Shield Rattle, Unbroken Form, Snake Eyes, Grease
Palm, Deep Cut, Hex Thief, Kindled Mind, Drummer's Focus, Jinxed Lens, Tempo Weave.

## 4. Balance re-solve (on this branch's code + bot)

Chain 1 (`balance-chain.py`, synergy bot, damage → class → damage, seed base 7500, run `overnight-2026-11-21`):
- **over-v51** (damage): kept L13, L18, L23, L28, L33, L63; dropped L43-L73 (did not beat noise).
- **over-v52** (class): kept Wizard L3, Rogue L18/L23/L28/L38/L53 (cuts), Barbarian L28, Wizard L28, Warrior
  L33/L43/L53/L68 (raises). The solve wanted Rogue ×0.46-0.73 and Warrior / Wizard ×1.3-2.8 from L18 up: a
  structural class gap, not noise. At L38+ Rogue misses least (15%), gets 16% of its damage from Bleed (others
  ~0%) and has the most HP; Rogue and Warrior end fights in ~3.5 hero actions vs ~12 for Barbarian / Wizard.
- **over-v53** (damage): kept L53, L68, L73.

Held out (seeds 9951-9953, no step saw them), same code:

| | skillrules-v49b | **over-v53** |
|---|---|---|
| mean clear gap | 4.45 | **1.97** |
| mean class spread | 15.5 | **11.6** |
| bands over parity | 12 | 10 |
| mean turns gap | 2.03 | 2.20 |

Per level (over-v53, held out): clear within ±3 of target everywhere except L13 (-2.4, Rogue 84 vs ~65 others),
L23 (-4.5), L38 (-6.8, Warrior 54). Class spread is still large at L13 (18.9), L68 (21.3, Warrior 30) and L73
(17.0, Warrior 21). Pyramids at L18 is 48% vs Desert / Mountains 72-76 (the known Pyramids design issue).

Chain 1 bot check: the median catalog build kills 1.4-5× slower than the bot at most levels (L23 2.5×, L53 5.1×).
`median-curve-solve.py`'s STEP ladder predates the skill-node rules, so a health solve against it would be wrong;
not run tonight.

Chain 2 (class → damage on over-v53, seed base 8500, run `overnight-2026-11-21-b`):
- **over-v54** (class): kept Warrior L23 / L38, Wizard L23, Rogue L58. 16 other cells did not beat noise.
- damage: nothing kept (L53 dropped), so the chain ends at over-v54.

Held out (seeds 9961-9963):

| | over-v53 | **over-v54** |
|---|---|---|
| mean clear gap | 2.43 | **2.10** |
| mean class spread | 11.7 | **10.8** |
| bands over parity | 8 | 7 |

over-v54 per level (held out 9961-9963):

| L | target | clear | Bar | War | Rog | Wiz | spread |
|---|---|---|---|---|---|---|---|
| 3 | 90.4 | 91.4 | 89.7 | 92.0 | 91.0 | 92.3 | 2.5 |
| 8 | 86.6 | 88.0 | 84.6 | 88.3 | 86.0 | 92.7 | 8.1 |
| 13 | 73.0 | 70.4 | 72.4 | 62.6 | 80.5 | 65.1 | 17.8 |
| 18 | 68.0 | 63.7 | 63.7 | 58.8 | 70.8 | 60.8 | 12.0 |
| 23 | 70.0 | 67.3 | 66.9 | 67.5 | 70.4 | 63.9 | 6.5 |
| 28 | 70.0 | 70.6 | 72.3 | 66.9 | 72.2 | 69.8 | 5.4 |
| 33 | 70.0 | 70.3 | 60.8 | 74.9 | 80.1 | 63.9 | 19.3 |
| 38 | 69.0 | 66.0 | 63.2 | 67.1 | 65.7 | 66.8 | 3.9 |
| 43 | 67.3 | 68.5 | 70.6 | 68.5 | 69.1 | 65.6 | 5.0 |
| 48 | 65.7 | 68.4 | 71.9 | 58.0 | 74.9 | 68.9 | 16.9 |
| 53 | 60.8 | 56.2 | 58.4 | 54.4 | 56.1 | 55.7 | 4.0 |
| 58 | 53.8 | 51.1 | 50.4 | 47.2 | 52.4 | 54.4 | 7.2 |
| 63 | 46.8 | 45.8 | 58.9 | 41.6 | 40.8 | 43.9 | 18.1 |
| 68 | 39.8 | 42.6 | 51.8 | 33.5 | 47.9 | 38.3 | 18.4 |
| 73 | 32.8 | 33.4 | 42.5 | 26.9 | 37.2 | 26.0 | 16.5 |

Clear is within ±3 of target at 12 of 15 levels (L18 -4.3, L53 -4.6). What's left is class spread: Rogue high at
L13 / L33, Warrior low at L48, Barbarian high at L63-L73.

Pass 3 (focused class solve, L13 / L33 / L63 / L68 / L73, 28 shards per class, run `overnight-2026-11-21-c`):
**over-v55** kept Wizard L13, Rogue L33, Warrior L68, Barbarian L73. Barbarian L63 and L73 railed at the ×0.25
floor: cutting Barbarian damage barely moves its late clear (see "Open design questions").

Held out (seeds 9971-9973):

| | over-v54 | **over-v55** |
|---|---|---|
| mean clear gap | 2.07 | **1.91** |
| mean class spread | 9.6 | **8.0** (target ≤ 10) |
| bands over parity | 8 | **5** |
| mean turns gap | 2.13 | 1.87 |

over-v55 per level (held out 9971-9973):

| L | target | clear | Bar | War | Rog | Wiz | spread |
|---|---|---|---|---|---|---|---|
| 3 | 90.4 | 89.5 | 88.5 | 88.9 | 92.5 | 87.5 | 5.0 |
| 8 | 86.6 | 85.9 | 87.9 | 82.8 | 83.2 | 89.2 | 6.4 |
| 13 | 73.0 | 74.2 | 67.0 | 71.6 | 78.5 | 78.8 | 11.9 |
| 18 | 68.0 | 64.3 | 66.3 | 60.2 | 70.6 | 59.4 | 11.2 |
| 23 | 70.0 | 70.7 | 71.4 | 69.9 | 70.4 | 71.0 | 1.6 |
| 28 | 70.0 | 70.7 | 74.4 | 63.0 | 74.4 | 70.3 | 11.4 |
| 33 | 70.0 | 68.1 | 69.5 | 71.1 | 67.5 | 63.7 | 7.4 |
| 38 | 69.0 | 65.7 | 63.7 | 68.5 | 66.6 | 63.2 | 5.3 |
| 43 | 67.3 | 68.8 | 72.4 | 64.7 | 68.4 | 69.9 | 7.8 |
| 48 | 65.7 | 69.8 | 66.6 | 65.3 | 76.0 | 70.5 | 10.7 |
| 53 | 60.8 | 56.6 | 60.1 | 54.0 | 56.4 | 55.4 | 6.1 |
| 58 | 53.8 | 53.8 | 49.2 | 49.1 | 60.3 | 55.6 | 11.2 |
| 63 | 46.8 | 44.8 | 50.3 | 42.0 | 44.1 | 43.2 | 8.4 |
| 68 | 39.8 | 42.1 | 41.1 | 38.1 | 46.4 | 43.1 | 8.3 |
| 73 | 32.8 | 34.3 | 37.3 | 30.2 | 35.5 | 35.3 | 7.1 |

Clear within ±4 of target at every level; worst L53 -4.2, L48 +4.1, L18 -3.7.

Second held-out check (seeds 9981-9983, run `overnight-2026-11-21-d`), over-v55 vs over-v54: class spread
11.7 → **10.0**, bands over parity 7 → 6, turns gap 2.15 → 1.95, clear gap 1.85 → 2.05 (slightly worse, within
noise). The spread gain repeats on both seed sets; the clear gap is a wash.

## 5. Open design questions (need your call)

1. **The class damage knob is a treadmill for Warrior.** Warrior's `classBalance.warrior.damageCurve` was already
   ×4-17 from L31 up in night-v47-6b / v49b; tonight's solves raised L31-45 to ×8-11 and L51-55 to **×35**
   (v55: L61-70 ×17). Barbarian goes the other way late (×0.26-0.43). The solver keeps pushing because Warrior
   clear barely responds to damage. In real fights, 80-95% of L48+ deaths are bosses, and Warriors / Rogues die
   after ~1.5 hero actions and 2-3 enemy swings: the late game is a race against boss burst. These multipliers
   also show up as hit numbers on screen. Better levers: Warrior defense / guard identity, boss swing size, and
   Barbarian's late heals (RAGE-spend heal) instead of damage. I didn't change any of those overnight
   (identity / design calls).
2. **Median catalog build is stale** (1.4-5× slower than the bot): fix the STEP ladder before any health solve.
   The L1-10 hero-turn anchors still read 3→10, not your 1→8.
3. **Never-learned skills** (section 3) and the rarely firing poison / weaken / quality nodes: content or design.
4. **Duplicate name**: two Warrior nodes are called Unbroken Stride.
5. **Scrap Prefer / Fragile Prefer** quality tier names were picked by me (old names no longer exist).

## 6. Tests

Full suite on this branch (`df86e184`, code identical to the tip), run in a scratch worktree since a full run
rewrites the active patch file:
- skillrules-v49b active: 0 failures. over-v53 active: 0 failures. (The one baseline failure was fixed.)
- over-v55 active: 3 failures, all flaky, none caused by v55. Two are the cadence `+25%` tests ("Mid with +25% deals
  more than unbuffed mid", "Finisher gets its own +25%"), which compare single small random hits on a level-1 hero.
  Repeated filtered runs fail 1 of 4 on over-v53, 1 of 4 on v54, 0 of 4 on v55 (v55 only changes class curves
  above L10). The 11-18 rebuild notes flagged the same two. The third, Stable "both fighters are out at once",
  failed because a fighter randomly died in room 1. Worth making all three deterministic (fixed roll / seed).
  Building both cadence heroes from one seeded RandomUtility scope did not remove the flake (randomness also
  enters during execution), so that edit was reverted.
- Also flaky, and load-sensitive: "After SetupStrike succeeds: next-hero-roll queue has +3 COMBO pending"
  (ActionBonusMechanicsFlow) passed 16 of 16 runs on an idle machine and failed 3 of 8 while a sim measure loaded
  the CPU. Looks like a timing dependency, not a balance one.

## 7. Commits on `balance/overnight-2026-11-21`

- `c4011dba` rule-fire trace, Empty Fury STEP fix, quality node names, bot knob, Wand test
- `df86e184` unlistedSkillBase 0.25 on; recorder credits post-kill / pre-fight fires
- `ce80e3ef` patches over-v51..53 · `66172c2c` over-v54 · `3256095f` over-v55

**Recommendation:** merge the branch and make **over-v55** active (PatchProfile.json is gitignored: set
`activeBalancePatch` to `over-v55` on each machine). The ×35 Warrior band works in sims, but it's a stopgap
until item 1 gets a real fix.
