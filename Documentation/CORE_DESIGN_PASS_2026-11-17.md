# Core Design Pass — whole-game simplification toward a 10 (2026-11-17)

A working document we go through together. It covers the whole game: combat, systems,
items and inventory, progression, dungeons and the player's experience from screen to
screen. The goal is to make it **simpler and clearer**, mainly by controlling the
**order** in which things happen and are shown, not by adding features.

Each phase has an **inventory** (what exists today, measured from code and data), a
**starting proposal** from Claude, **questions for you** and a **decision log**. Nothing is
implemented until a phase is closed. Once implementation starts, all closed phases are built
before any sims run.

**Out of scope for this pass:** balance numbers, and in-fight player decisions (the game
stays an autobattler).

---

## 1. The four pillars

| # | Pillar | Test for any mechanic, screen or line of text |
|---|---|---|
| P1 | **The streak is the fantasy.** Roll as many successes in a row as you can; sequence and combo are how you experience it. | Does it make a streak longer, shorter or more dramatic, or help you see one? |
| P2 | **Decisions live in gear, actions and dungeon choice.** Builds and enemies/dungeons mirror each other. | Can the player see it before committing and plan around it? Does the enemy side use the same word? |
| P3 | **Expressive combat.** Momentum swings, and clear feedback on which build decisions caused them. | When it happens, can the game say *who* caused it and *why* in one line? |
| P4 | **Funny or interesting things happen.** | Can it produce a moment worth retelling? |

**Verdicts:** **Keep**, **Merge** (into another thing), **Rework** (same idea, new rule),
**Defer** (introduce later in the reveal schedule) or **Cut**. Anything that passes no pillar
is cut. Anything that passes only P4 can stay if it is cheap.

---

## 2. The organizing idea: clarity through sequence

The game has a lot of depth. The problem is that too much of it arrives **at the same
time**: on one item card, in one combat line, or in one early dungeon. We fix that on three
time scales.

### 2a. The beat: one action resolves in a fixed, readable order

Every action in combat reads the same way, one cause per step:

```
1. WHO acts, WHICH action, which strip slot   ATTACK · slot 3 of 5
2. THE ROLL and its band                       rolled 16 → COMBO (needed 15)
3. THE STREAK                                  streak 4 → 5
4. DAMAGE, with the top source named           62 dmg (+40% Thorned Gauntlets)
5. WHAT IT TRIGGERED                           → Rattle 2 on Ghoul · +1 RAGE
6. WHAT CHANGED                                Ghoul 38% · next roll −2
```

Rules: steps never reorder, an empty step is skipped, and a step never shows more than
one attributed source (the rest go in a hover or the post-fight report). The log and the
on-screen board use the same order, and the beat pacing (`CombatDelayConfig`) follows it
so the eye can track it.

### 2b. The loop: one decision at a time between fights

```
choose dungeon → enter room → fight → reward (one thing) → adjust (gear / strip) → next room
```

Each step shows **only what that decision needs**. Dungeon select shows what the dungeon
tests and what it drops. A reward is shown as *one* clear comparison ("better than your
gloves because…"), not as a full inventory screen. Adjusting happens between rooms or at
the end of a dungeon, not mid-flow.

### 2c. The run: the complexity ladder (the backbone of this pass)

**Guiding decision G1 (2026-11-17):** *Simplify, then build back up from the ground up, adding
complexity one step at a time. The levels and enemies dictate and communicate each step.*

**The teaching rule (G1.2, agreed): see it, suffer it, own it.** A new thing debuts on an enemy,
the dungeon and enemy rows name it, the same band's loot answers it, and then you wield it.

**Two tracks (G1.1, revised):** a one-concept-per-5-levels pace was too slow. What gets
slow-rolled is the **systems**. The **content pools** (actions, items, skills, statuses) keep
growing with level for the whole game.

**Track 1: systems, introduced one at a time, mostly in the first ~15 levels**

| Level (draft) | System introduced | How it arrives |
|---|---|---|
| 1 | Dice bands, 1 action slot, the combat board | the first fight (~2 swings) |
| 2 | **Items**: the first drop and the comparison | a Common drops: stats and an action |
| 2–3 | **2nd action slot → combos** and AMP growth | the strip lights up across two cards |
| 3 | **Action interactions**: order matters; your class status | the first gear action that pairs with your starter |
| 4 | **Dungeon selection** as a real choice | 2–3 dungeons with matchup lines |
| 1–4 | **Rooms**: room twists (affix or terrain) from the first dungeon; non-hostile encounters (D8.7, D8.11) | the first room with a twist, shown before the fight |
| 5 | **Bosses** | the first band boss |
| 6 | **Regions** and **dice modifications** (Focus / Rattle) | travel to Water; Undertow foes Rattle you |
| 7–8 | **Synergies**: Uncommon items with a base modifier | the first Uncommon drop |
| 9 | **Loot generation** depth: rarity lines, item level, trade-up | the first Rare |
| 10 | **Material bonuses** (sets) | the second piece of one material |
| 11–12 | **Skill tree** (one branch) | the first skill point with a prompt |
| 13+ | Stores and verbs, cross-class nodes, rule items, strip effects (Jam / Burn out), expiring actions | spread over the next bands |

Action slots keep growing with level along the way (the combo slot rise already exists).

**Track 2: content, which expands with level for the whole game.** The rollable actions, item bases,
skill nodes and statuses unlock by level and tier: early pools are small and readable, later ones
wide. Each region band's enemies feature what's new in that band's pool.

**Rules that fall out of the ladder**
- **Nothing appears before its rung**: no system, screen element or line on an item card.
- Each band's enemy roster is a lesson, and its boss is the exam.
- **The build plan follows the ladder** (Phase 10): strip the game down to level 1's systems, make
  that feel great, then add systems in order. The game stays playable and testable at every step.

**Questions**
- G1.4 Is the order of track 1 right? In particular: skill tree at 11–12, sets at 10, and dungeon
  selection as early as level 4?
- G1.5 Should each system arrive by **level**, or by **first event** (the first Uncommon drop,
  whenever it happens)?

**Decisions**
- **G1.4** The track 1 order is **a good start**.
- **G1.5** Systems arrive **whenever their first event happens** (the first Uncommon drop, the second
  piece of a material…), not at a fixed level. The level column is a typical arrival point.
  **Rarity is independent of level**: every rarity can drop from level 1 (already true in
  `RarityTable.json`, MinLevel 1 everywhere), and a lucky early Rare simply introduces its system early.

---|---|---|
| 1 (first dungeon) | Roll bands, the streak, the action strip, HP | Statuses, materials, stores, skills |
| 2 | Gear with plain stat lines, one gear action, swap strip order | Base modifiers, affixes |
| 3 | The core statuses (first enemy that Rattles you) | The rest of the statuses |
| 4 | Materials and the first set bonus | Stores and verbs |
| 5 | The skill tree (one branch open) | Cross-class nodes |
| 6 | Stores and verbs (the first item that mints) | Rule-bending items |
| 7+ | Base modifiers, rule items, cross-wires, cadence timing | — |

---

## 3. How we work each session

1. Claude brings the phase's inventory and a starting proposal, with 2–3 options where the
   right call isn't obvious. Where it helps, Claude plays that part of the game over MCP first
   and brings real screens and log excerpts.
2. You make the calls. "Not sure yet" goes to the parked list instead of blocking the phase.
3. Claude writes the decisions into the log and updates the ledgers at the bottom.
4. **The one-line check** before closing: write the exact text the player would see. If it
   needs more than one sentence, the design is too complicated.

**Suggested order:** Phase 0 (done) → 3 (statuses, done) → 5 (actions and the strip, done) → 4 (items and inventory, done) → 8 (enemies and dungeons, done) → 2 (dice and combo, done) → 7 (progression, done) → 6 (vocabulary, done) → 9 (feedback, done) → 1 (journey walk, done) → 10 (build plan, drafted) → place every decision on the
ladder (2c) as we go → remaining phases in any order → 10 (build plan, one rung at a time).

---

## Phase 0 — North star

Write what a 10 *looks like on screen* before we cut anything: a mock fight log, a mock
dungeon-select screen, a mock loot moment and a mock item card. Every later phase is
checked against these four.

### Today (new Rogue, first two dungeons over MCP, 2026-11-17)

- **First item in the game** (Iron Coat, Common, level 1) shows a modifier that does nothing
  yet ("BLOOD RUSH also counts as a [warbrute] action — nothing you wear reads [warbrute] yet"),
  three stat lines, a five-step material ladder, a store (CRISIS), a spend verb (IRON CULL),
  banked stores, affixes, a granted action (WITS ARC) and a bonus that scales off "Wits",
  which isn't one of the four attributes. That's about 12 concepts before the first fight.
- **Dungeon select** lists three dungeons with only a name and level. There's nothing to choose between.
- **The first dungeon was one roll:** a natural 20 against a 3-HP Alpha Wolf. The second dungeon
  was two fights, each settled by one hero swing against 2-HP enemies. **No streak is possible
  in the first dungeons**, so the core fantasy never appears.
- The **streak isn't shown anywhere** in the log. Rolls read "attack roll 5 hit" while the
  documented bands make 2–5 a miss, so the modified roll math is invisible.
- **Unexplained events:** "The atmosphere feels charged with energy" (twice), a "Demonic" enemy
  at level 2, a full heal at the end, and a strip with 2 slots holding 1 action with no prompt to
  fill it. There's 1 unspent skill point, also with no prompt.
- **Already funny (P4):** "A Ghost Living Wall Crawler with Cleaver appears." The name generator
  produces good moments; the fight then ignores them.

### Mock 1 (revised) — the combat board is the main focus

Today's board (`DungeonRenderer.CombatBoard.cs`) already has the right bones: the enemy strip
on top, PREV → NOW cards with the turn queue, the die, a sequence panel (attacker → roll →
outcome → action → defense → effects → damage), and the hero strip along the bottom. The
redesign makes **the combo coming together** the thing you watch:

```
 GHOUL  ▓▓▓▓▓▓▓░░░ 68%        [ CLAW ]━[ CLAW ]━[ GNAW ]        its chain: 2
 Rattle 1 → you · Bleed 2 on it

                    ┌───────────── NOW ─────────────┐
                    │  OPEN VEIN        slot 4      │
                    │        ⚀  17  → COMBO         │
   1   2–5   6–13   14+   20     ← roll ruler, marker on 17
                    │  44 dmg   ×1.6 combo power    │
                    └───────────────────────────────┘

 YOU  ▓▓▓▓▓▓▓▓▓░ 88%
  [ STAB ]═[ FEINT ]═[ STAB ]═[ OPEN VEIN ]═[ CODA ]
    lit      lit       lit       ▲ firing       next
    ×1.0     ×1.2      ×1.4      ×1.6           ×1.8 + releases 3 bonuses
```

- **The hero strip is the streak.** Each slot lights as the combo reaches it, and the cards
  get physically bigger and brighter, with a growing power multiplier. The animation escalates
  the further you get. A broken combo visibly collapses the chain back to slot 1.
- **The enemy strip mirrors it** on top, with its own chain count, so momentum swings are
  visible both ways.
- **The roll ruler** shows the five bands with a marker, so you can read hit/combo/crit without
  doing math. A modifier appears as a small labeled nudge on the marker (`−2 Rattle`).
- **Status chips** sit under each health bar, at most a few per side, in plain words.
- **The log is secondary.** A *simple* log (default) gives one line per swing: band and cause.
  A *detailed* log (toggle) shows the full roll math and every attributed source.

The text log below shows the content of the beats. The board presents the same beats visually.

Names are illustrative. What matters is the order of each beat, the streak and the attribution.

```
HAUNTED CRYPT · room 3/5 · GHOUL (Undead)           tests: RATTLE
Your strip: STAB · OPEN VEIN · STAB · FEINT · CODA     best streak today: 3

 You    STAB       slot 1   rolled 14 → COMBO              streak 1
        18 dmg
 You    OPEN VEIN  slot 2   rolled 17 → COMBO              streak 2
        22 dmg · Bleed 2 on Ghoul
 Ghoul  CLAW                rolled 15 → HIT
        14 dmg to you · Rattle 1 on you
 You    STAB       slot 3   rolled 16 −2 Rattle = 14 → HIT (combo needs 15)
        ✗ STREAK BROKEN at 2 — Rattle cost you this one
 Ghoul  CLAW                rolled 3 → MISS · bleeds 9 as it swings
 You    STAB       slot 1   rolled 19 +1 Focus → COMBO     streak 1   (Leather: Lucky kept the high die)
 ...
 You    CODA       slot 5   rolled 20 → CRIT               STREAK 5 ★ new best
        CODA releases 3 queued bonuses: 96 dmg (+60%)
 Ghoul  bleeds out mid-swing.  Its CLAW hits nothing but air.

 WON in 11 swings · best streak 5 ★
 Top damage: CODA release 38% · Bleed (OPEN VEIN) 31% · STAB 20%
 Hurt you most: Rattle broke 2 streaks (Ghoul CLAW) → Focus answers Rattle
```

### Mock 2 (revised) — dungeon select as a build matchup

Dungeon select is about **build dynamics and soft counters**: who you'll meet, how they play,
and how *your* build lines up against them.

```
WHERE NEXT?                                        Whisperledger Wilds · levels 1–5

▸ HAUNTED CRYPT   Lv 6 · 5 rooms · drops BONE

  You'll meet
    Ghoul        chains long, Rattles you on hit       punished by: Bleed (it acts a lot)
    Wight        Hardens each time you land a combo     punished by: Expose, short strips
    Lich (boss)  breaks your combo on its crits         punished by: Focus, Mark

  Your build here
    ✓ OPEN VEIN's Bleed punishes the Ghoul's long chains
    ✗ Your 5-slot strip feeds the Wight's Harden
    ~ Focus 1 vs Rattle 2

  Matchup: favored early, the Wight is your wall

  DENSE BRUSH     Lv 6 · drops WOOD        Matchup: even
  LAVA CAVES      Lv 7 · drops OBSIDIAN    Matchup: hard (no answer to Burn)
```

- Every enemy row says **how it plays** and **what punishes it**, in the same words the hero's
  gear uses (the mirror).
- **"Your build here"** shows up to three soft-counter lines, computed from your strip, gear and
  statuses against the roster, each with ✓ / ✗ / ~.
- The matchup is a hint, not a gate. Bringing the wrong build is allowed, and it's how you
  learn what the dungeon wants.

### Mock 3 — the loot moment (one comparison, one decision)

```
FOUND  Iron Breeches of the Young Ox            Uncommon legs · Iron

  vs your Leather Pants
    +6 Defense, +3 STR
    breaks your Leather set (3 → 2): you lose Lucky (2d20, keep the higher)

  Verdict: keep Leather Pants for now.        [Equip anyway]  [Keep in bag]
```

### Mock 4 (revised) — the item card: Commons are plain

**A Common item has no mechanic.** It's stats only. The core benefit (the base modifier, the
thing that changes how you play) starts at Uncommon.

```
IRON COAT                              Common body · Iron
  +11 Defense   +5 STR   +3 INT
  Requires STR 10
```

```
IRON COAT OF THE YOUNG OX              Uncommon body · Iron
  +11 Defense   +5 STR   +3 INT   +4 STR (Ox)
  ◆ Your crushing hits count as Warbrute hits
  Iron set 1/5 — wear 2 to start storing CRISIS
  ── details ───────────────────────────────────────────────
  set ladder · requirements
```

Rules the mocks imply:
- **The streak is on screen at all times**, and its end is always explained.
- **Every result names its single biggest cause**, whether good or bad.
- **Hidden until relevant:** a rule that does nothing for you yet isn't shown on the card.
- **Every screen has one question**: where next, wear this or not, what broke my streak.
- **The game notices when names collide with events** ("bleeds out mid-swing").

**Questions**
- Q0.1 Is the streak the *score* of a fight (shown big, remembered, rewarded) or only its feel?
- Q0.2 Do enemies have visible streaks too, so momentum swings go both ways?
- Q0.3 Name one moment from your own play that felt like the game at its best, and one that
  felt like noise.
- Q0.4 The first dungeons are one-swing fights, so the streak can't happen. Should the opening
  fights be built to show a streak (for example 3–5 hero swings each), even at level 1?
- Q0.5 Roll display: show the math (`16 −2 Rattle = 14`) or only the band and its cause?
- Q0.6 Which of the four mocks is closest to right, and which is most wrong?

**Decision log (2026-11-17)**
- **D0.1** The streak is **feel, not score**. It plays as an animation that escalates the further
  the combo gets, and it represents your attacks getting stronger as the combo comes together.
  It's the most build-like part of the experience.
- **D0.2** Enemies have **visible combo chains** too. Momentum swings go both ways.
- **D0.3** Best moment: combining a few actions and finding a fun way to play (strip synergy).
  Noise: **status effects**, so Phase 3 is high priority.
- **D0.4** Level 1 fights last **about 2 hero swings**, ramping slowly from there.
- **D0.5** Two log levels: **simple** (band and cause, the default) and **detailed** (full roll
  math and every source). The board shows the simple version.
- **D0.6** **The combat board is the main focus**, not the log. Mock 1 is redrawn around the board.
- **D0.7** **Common items have no mechanic** (stats only). The core benefit (base modifier) starts
  at Uncommon. This changes the 11-15 rule that every base, starters included, carries a modifier.
- **D0.8** Dungeon select is built around **build dynamics and soft counters**, with more
  information about the enemies you'll meet. Mock 2 is redrawn.

- **D0.9** (was Q0.7; **superseded by D5.6**: actions start at Uncommon) Commons **keep their gear action and count toward
  material sets**. They lose only the base modifier. Revisit in Phase 4 if the card still reads
  as too busy.
- **D0.10** (was Q0.8) Combo power comes from **both**. A shared rule makes longer combos stronger,
  and **AMP from Intelligence** amplifies it. That already exists: slot-based amplification in
  `ActionUtilities` (`ComboAmplifierLinearSlots`, `GetComboAmplifier`). On top of that, the core of the
  game is finding the combination of items and actions that exploits that growth best. The board
  should show the shared multiplier per slot, and attribute the build-driven extra to its source.
- **D0.11** (was Q0.9) An enemy's chain **escalates the same way** as the hero's (enemies already
  have `GetComboAmplifier`).

---

## Phase 1 — The player journey walk

Walked over MCP on 2026-11-17/18 as a new Rogue through level 2, plus code reads for the main menu.

### Screen by screen: today → against our decisions

| # | Screen | What it shows / asks today | What our decisions say | Proposal |
|---|---|---|---|---|
| 1 | **Main menu** | fighter cards 1–9, N new, A all fighters, F Stable mode, R history, S settings, C character selection, Q quit | one thing at a time (2b) | **One roster:** Continue (latest) · fighter cards · New · Fallen (history) · Settings · Quit. "All fighters", "Character selection" and the Stable become one list |
| 2 | **Training Ground offer** | "Enter tutorial dungeon / Skip" (and the agent tools recommend skipping) | the ladder teaches by playing (G1) | **No offer.** The first dungeon *is* the tutorial |
| 3 | **Path introduction** | one "Continue" screen | — | **Cut**; fold its text into weapon choice |
| 4 | **Weapon selection** | 4 rows: name, class, damage, swing | the weapon decides the build (D7.1) | **4 class cards:** weapon, strip style (D5), signature status (D3), one Fernis line. The most important choice in the game should look like it |
| 5 | **Inventory (right after)** | starter gear + a random Iron Coat; 11 options | items arrive on the first drop (G1.5), Commons are plain (D0.7) | **Skip it.** Go straight into the first dungeon; no extra coat |
| 6 | **Hub** | stats, gear list; dungeons / inventory / skills / travel / save | one question per screen | **Dungeon select *is* the hub**, with Gear, Strip, Skills and Travel as side tabs that **appear when unlocked** |
| 7 | **Dungeon select** | 3 rows: name + level (plus hidden custom-level / reset-difficulty entries) | matchup cards (D0.8, D8.3) | Matchup cards: lesson, enemy rows, material, ✓ / ✗ / ~ for your build, one Fernis line (bad advice, D9.7) |
| 8 | **Dungeon run** | auto-play; log lines; room events | board-first (D0.6), room twists from the start (D8.11), no gear changes (D5.8) | The board, room twist shown before each fight, 1-in-4 encounters (D8.10), Fernis |
| 9 | **Dungeon complete** | stats line; **loot silently lands in the bag**; full heal | Fernis summary (D9.2), staged loot reveal (D4.5) | Summary, then the staged reveal, then Equip / Keep / Salvage, then the hub |
| 10 | **Inventory** | equip, strip tab, auto-equip, unequip, discard, trade up, sort, hide unmet, slot filter, material filter (11 options) | small smart bag (D4 proposal 7) | **Two tabs: Gear and Strip.** "Upgrades for you" first; filters only when the bag is big; trade-up moves to the spoils screen |
| 11 | **Skill tree** | all 43 Rogue nodes listed at level 2, most locked, tier numbers, **template braces leak** ("+{8}% damage") | Core opens first, branches on events (D7 proposal 5), one-time picks (D7.6) | Show Core, plus only what's available or next; a prompt when a point arrives; fix the braces |
| 12 | **Region travel** | **all 20 regions open at level 2**, up to the level 96–100 band | the ladder (G1), regions own their rosters (D8.8) | **Regions unlock by beating the previous region's boss.** Each region card carries its lore (D9.12) |
| 13 | **Death** | clone screen (dev tool, D7.9) | permadeath with items (D7.7) | Fernis eulogy, a run summary, then the roster. The clone stays in the dev build |
| 14 | **Settings** | includes a Testing panel and developer panels | — | Player settings only (audio, display, text speed, log level D0.5) |

### Developer screens to move out of the player game (11 states)
Testing, DeveloperMenu, BattleStatistics, VariableEditor, TuningParameters, ActionEditor, CreateAction,
EditAction, ViewAction, DeleteActionConfirmation, ActionInteractionLab. Proposal: behind a dev build flag.
They keep working for you, but players never see them. This also feeds the line-count cut.

### The first 60 seconds (proposal)
```
Title (Demon Fighter) → New → 4 class cards (pick a weapon; Fernis introduces himself)
→ name rolled (reroll allowed) → first room of the first dungeon: the board, 1 card in the strip,
  a ~2-swing fight (D0.4) → next room has a twist → boss → summary → the first loot reveal → the hub
```
No menus before the first fight; the hub's tabs appear as their systems arrive.

### Questions
- Q1.1 Is that the right first 60 seconds? Does the Training Ground go away (the first dungeon is the tutorial)?
- Q1.2 One roster on the main menu? What is Stable mode for, and does it stay?
- Q1.3 Make dungeon select the hub, with tabs that appear as systems unlock?
- Q1.4 Regions unlock by beating the previous region's boss?
- Q1.5 Move the 11 developer screens behind a dev build flag?
- Q1.6 Inventory as two tabs (Gear, Strip), with filters hidden until the bag is big?

**Decision log (2026-11-18)**
- **D1.1** **The first 60 seconds flow is right**, but **the tutorial choice stays**: the Training Ground
  offer remains as an option before the first dungeon.
- **D1.2** **The Stable stays.** It lets you run characters in parallel. (Claude's lean, not decided:
  "All fighters" and "Character selection" can still fold into the fighter cards.)
- **D1.4** **Regions have requirements, which can be any number of things**: level, an attribute, and so on.
  A region card shows its requirements, and the ladder (G1) can use them.
- **D1.5** **The 11 developer screens move behind a dev build flag.**
- **D1.6** **Inventory keeps its current tabs:** Inventory, Actions, Skills, Trade Up.

- **D1.3** (Q1.3) **Keep the hub menu** (Dungeons / Inventory / Skills / Travel / Save) as it is. Dungeon
  select stays its own screen, now with the matchup cards (D0.8).

**Phase 1 closed (2026-11-18). Phases 0–9 are all closed; next is Phase 10, the build plan.**

---

## Phase 2 — Combat core: dice and the combo

### Inventory: how a swing works today (from code, 2026-11-17)

1. Roll a d20, plus roll modifiers.
2. **Natural 1:** crit miss. **Below HIT (≤5):** miss. **Combo threshold or above** (14, +1 per slot from
   slot 2, capped at 20): **COMBO**. The strip card in the current slot fires, and the combo advances.
   **20:** crit.
3. **A plain HIT** (6 up to the combo threshold) fires a **basic attack, not a strip card**, and
   **resets the chain** (`comboSystem.resetChainOnNormalHit`, on by default). A **miss** resets it too,
   unless a rule exempts it (EXEMPT COMBO).
4. **Damage** of a strip card = base × **slot multiplier**: AMP^(slot−1) (or linear), × a per-slot damage
   bonus for heroes, × INT potency.

So a five-card combo needs five rolls in a row at **14+, 15+, 16+, 17+, 18+**. On a bare d20 that's
about 35% × 30% × 25% × 20% × 15% ≈ **0.08%**. Builds exist to beat those odds, and that's the fantasy.

**Things that touch the roll today (at least 10):** roll bonus/penalty (Focus, Rattle), TEC milestones
(threshold shifts), gear HIT/COMBO/CRIT shifts, consumable shifts, the per-slot combo rise, advantage
(Lucky, encounter advantage: 2d20 keep highest), rerolls, exploding dice, "your next roll becomes 18",
stuck HIT/COMBO points spilling into CRIT, and cadence ACCURACY grants.

**Things that feed AMP today (5):** the INT curve (`ComboAmplifierCurve`), *again* INT potency
(`IntelligenceAmpPerPoint`), gear AMP mods, the Mythic animal set, and a per-slot hero damage percent.

**Attribute jobs today:** STR → damage, AGI → speed (`SpeedCalculator`), TEC → threshold milestones,
INT → AMP (twice), plus an **INT 10 gate**: strip routing (jump / repeat / loop cards) does nothing below
INT 10 (`ComboSequenceIntelligenceThreshold`).

### Starting proposal

**1. The combo is a ladder of odds, and the board shows them.** Each upcoming strip card shows what it
needs and the chance ("slot 3 · needs 16+ · 30%"). The roll ruler marks where the roll landed and what
moved it. Beating the odds is visible, and so is the build that beat them.

**2. Three kinds of roll modifier, each with its own look on the ruler:**

| Kind | What it does | Sources (kept) | On the ruler |
|---|---|---|---|
| **Nudge** | ± to the roll | Focus, Rattle | the marker slides |
| **Bands** | moves HIT / COMBO / CRIT thresholds | TEC, gear shifts, the per-slot rise | the bands slide |
| **Dice** | more dice or a second chance | advantage (Lucky), rerolls | a second die appears |

Exploding dice and "next roll becomes 18" stay as **cards** (Wizard sequence and dice tricks), not
general modifiers. Spill-into-crit, consumable threshold shifts and cadence ACCURACY fold into these three
kinds.

**3. One AMP formula:** `AMP = INT curve × gear AMP`, and **slot multiplier = AMP^(slot−1)**. The
double INT, the Mythic animal set bonus and the per-slot damage percent go. The board shows one number
per card (×1.0, ×1.3, ×1.7…).

**4. One job per attribute (as today, minus the duplicates):**

| Attribute | Job | Feels like |
|---|---|---|
| STR | damage per success | each card hits harder |
| AGI | tempo: more swings per enemy swing | more rolls at the ladder |
| TEC | reliability: moves the bands | the ladder gets shorter |
| INT | growth: AMP per slot | the top of the ladder pays more |

**5. Remove the INT 10 gate.** Sequence cards (jump / repeat / loop) always work, and the Wizard's edge
comes from having them, not from a hidden threshold.

**6. Enemies roll by the same rules** (D0.11), with their own thresholds and strip, shown on the top
strip.

### Questions
- Q2.1 **A plain HIT** (lands, but below combo): should it **reset** the chain (today), **hold** it (no
  advance, no reset; only misses break it), or **advance** it at reduced power?
- Q2.2 Do the three modifier kinds (nudge / bands / dice) cover the dice modifications you want?
- Q2.3 One AMP formula (INT curve × gear AMP), cutting the duplicates?
- Q2.4 Do the attribute jobs match your intent (STR damage, AGI tempo, TEC reliability, INT growth)?
- Q2.5 **Crit (20) and crit miss (1):** what should each do for the combo? For example, a crit could
  advance two slots, and a crit miss could be a funny self-inflicted moment (P4) as well as a reset.
- Q2.6 Remove the INT 10 gate on strip routing?
- Q2.7 Show the odds on upcoming strip cards ("needs 16+ · 30%")?

**Decision log (2026-11-17)**
- **D2.1** A plain **HIT resets the chain** (as today).
- **D2.2** **Three kinds of roll modifier:** nudge (± roll), bands (thresholds) and dice (advantage,
  rerolls).
- **D2.3** **One AMP formula:** INT curve × gear AMP; slot multiplier = AMP^(slot−1). The duplicates go.
- **D2.4** **Attribute jobs:** STR = damage per success, AGI = tempo, TEC = reliability (moves the bands),
  INT = growth (AMP).
- **D2.5** **A crit amplifies the action's effects**: damage *and* its statuses and other effects. **A crit
  miss counts as a miss with 2× recovery time** (the next turn comes later).
- **D2.6** **Remove the INT 10 gate** on strip routing.
- **D2.7** **Show the odds** on upcoming strip cards ("needs 16+ · 30%").
- **D2.8 (core rule, already in the code): the combo happens inside one turn.** In a turn, **when you roll an action**
  (a combo: the strip card fires), **you get a bonus roll**. On a bonus roll, the **action threshold is
  raised by 1**, and **HIT is removed from the pool**: a bonus roll is either the next action or a miss.
  A turn is therefore a chain of rolls that runs until one fails. That's the streak, literally.

**D2.8 is already implemented** (Claude first missed it while tracing only the combo-step code). These
defaults are active, with no patch override:
- `comboSystem.freeActionOnComboHit = true`: a combo roll earns a free action in the same turn
  (`CombatTurnHandlerSimplified.RunFreeComboActionsAsync`), for heroes and enemies.
- `missBelowComboMidChain = true`: past slot 1, the HIT band is gone, so a link is either the next action
  or a miss.
- The per-slot combo rise (+1 per slot) is the "+1 on each bonus roll".
- `comboChainGrowth = 0.25`: chain time, below.

```
TURN  roll 1:  1 = crit miss · 2–5 miss · 6–13 HIT (basic attack, turn ends) · 14+ ACTION (slot 1 fires)
      bonus:   needs 15+ → ACTION (slot 2 fires) · else miss, chain resets, turn ends
      bonus:   needs 16+ → ACTION (slot 3 fires) · …   until a roll fails or the strip ends
```

- **D2.9** (Q2.8) The +1 **adds up** with each bonus roll (15, 16, 17…). It's the same rule as the per-slot
  rise, so there's only one rule.
- **D2.10** (Q2.9) A failed bonus roll **resets the chain**: the next turn starts at slot 1.
- **D2.11** (Q2.10) **By default the chain doesn't loop.** Reaching the end of the strip ends the turn
  with the combo complete. Looping exists only through cards (Wizard FLOW).
- **D2.12** (Q2.11) **Chain time = avg speed × (1 + g × (actions − 1))**, **g = 0.25** to start. The first
  action costs one full average swing, and each extra link adds g × that average:

  | Actions | g = 0.25 | g = 0.5 | g = 0.75 | Full price (g = 1) |
  |---|---|---|---|---|
  | 1 | 1.70 | 1.70 | 1.70 | 1.70 |
  | 3 | 2.75 | 3.67 | 4.58 | 5.50 |
  | 5 | 3.96 | 5.94 | 7.92 | 9.90 |

  (Example chain of slow actions 1.7 → 2.0 → 1.8 → 2.5 → 1.9 s. At g = 0.25, a full 5-link chain costs
  60% less than paying for each action.)

**Consequences worth keeping in view**
- Neither side can act during the other's chain, which is why Disrupt became strip effects (D3.15).
- AGI = more turns (more chances to start a chain), TEC = lower bar on every link, INT = the late links
  pay, STR = every link hits harder.
- Bleed ticks on every action, so it punishes long chains on both sides.
- The board's escalating animation (D0.1) plays out within one turn, link by link.

**Phase 2 closed (2026-11-17).**

---

## Phase 3 — Status effects

### Inventory (measured 2026-11-17)

**Registered effects:** bleed, weaken, slow, poison, stun, burn, acid, absorb, confusion,
disrupt, fortify, plus the stack-and-turn family (harden, focus, expose, regen, pierce, reflect,
silence, stat drain, temp HP, mark) and the 11-16 tempo statuses (brittle, stagger, haste,
rattle). On top of those sit creature affinities (`StatusAffinities.json`: undead immune to
bleed and poison, plants take double burn, …).

**Data mentions** (Actions / EnemyActions / Weapons / Armor / Skills / Mods):

| Status | What it does today | Uses | Effect on a combo |
|---|---|---|---|
| Poison / Burn / Bleed / Acid | 4 DoTs: %max-HP / on time / when the holder acts / + armor shred | 31 / 45 / 65 / 14 | Bleed punishes long chains; the others don't touch the combo |
| Disrupt | **resets the holder's combo** | 16 (13 enemy) | breaks a combo directly |
| Stun | skip turns | 16 | stops a combo |
| Stagger / Slow | next action slower / all actions slower | 52 / 45 | fewer swings |
| Haste | own actions faster | 44 | more swings |
| Rattle / Focus | −roll / +roll | 47 / 72 | breaks / keeps a combo going |
| Harden / Fortify | damage reduction / armor | 43 / 18 | none |
| Expose / Brittle / Pierce | −defense / crits taken harder / ignore armor | 70 / 64 / 18 | none (Brittle: crit payoff) |
| Weaken | −50% damage dealt | 39 | none |
| Mark | next landed hit crits | 42 | crit payoff |
| Confusion | chance to hit self | 6 | P4 candidate |
| Reflect / Regen / Temp HP | | ≤ 3 each | none |
| Silence / Stat Drain / Absorb | | 0 | dead |

**The panel is the bigger noise.** The status list beside each health bar
(`StatusEffectDisplayLines`) puts about 40 kinds of lines in one list: real statuses, 12 material
stores (CRISIS, RAGE, STREAK…), cadence grants (Next action, TURN, Fight:, Dungeon:), rerolls,
advantage and disadvantage, Lucky, shield, skip turn, guarantee hit and combo mode.

### The test for a status (from D0.10 and D0.11)

**A status earns its place by changing how a combo grows, keeps going or breaks**, on either
side, since enemies chain too. A status that only nudges a damage number is a cut or merge
candidate: damage numbers belong to gear, AMP and the combo multiplier.

### Starting proposal: 10 statuses, grouped by what they do to a combo

| Group | Status | Rule (one line, as the chip says it) | Absorbs |
|---|---|---|---|
| **Keep it going** | Focus | +1 to each roll per stack | Accuracy, Advantage? |
| **Break it** | Rattle | −1 to each roll per stack | Roll Penalty, Weaken?, Confusion? |
| | Disrupt | the holder's combo drops back to slot 1 | Stun? |
| **Tempo** | Haste | your swings come faster | — |
| | Stagger | the holder's next swing is slower; at 4 stacks it loses the swing | Slow, Stun |
| **Pay it off** | Mark | the holder's next landed hit against it crits; extra stacks add crit damage | Brittle |
| | Expose | the holder takes more damage; at 4 stacks its armor breaks | Pierce, Acid, Brittle? |
| | Guard | the holder takes less damage | Harden, Fortify, Temp HP, Absorb |
| **Punish it** | Bleed | ticks each time the holder acts (long chains bleed more) | Poison? |
| | Burn | ticks over time, no matter what | Poison?, Acid |

**Cut outright:** Silence, Stat Drain, Absorb, Reflect and Regen as statuses (the REFLECT and HEAL
rule verbs stay), Pierce, Slow and Weaken (see Q3.2).

**One rule for every status:** stacks cap at 4, and the holder loses one stack **each time it
acts**. Everything happens on the holder's turn, which makes the rule easy to read on the
board and matches Bleed.

**What leaves the status panel** (Phase 9 places them on the board):
- **Material stores** (CRISIS, RAGE, …) → a meter beside the strip, since they're what your combo
  turns into.
- **Cadence grants** (Next action, TURN, ACTION ×N) → shown **on the strip slots they'll land on**.
- **Rerolls, advantage, Lucky** → shown on the die.
- **Shield** → part of the health bar.

So each health bar carries only the chips from the table above: at most 10 kinds, usually 2–3.

**Creature affinities** stay but simplify to *immune* or *weak*, and they show on dungeon select
("Undead — immune to Bleed"). That makes them part of the build matchup (D0.8).

### The one-line check (chip text)

```
 Focus 2    +2 to your rolls             Rattle 3   −3 to its rolls
 Disrupt    its combo resets             Haste 2    you swing faster
 Stagger 4  it loses its next swing      Mark       its next hit taken crits
 Expose 4   takes +40%, armor broken     Guard 2    takes −20%
 Bleed 3    3 dmg each time it acts      Burn 5     5 dmg every beat
```

**Questions**
- Q3.1 Is grouping statuses by what they do to a combo (keep it going / break it / tempo /
  pay it off / punish it) the right organizing idea?
- Q3.2 Weaken (39 uses) has no combo role. Cut it and retarget its uses to Rattle, or keep it
  paired with an "Empower"?
- Q3.3 Stun: fold into Stagger (4 stacks = lose the swing), or keep it as its own word?
- Q3.4 Since enemies chain too (D0.11), should heroes get more ways to Disrupt? Today 13 of its
  16 uses are on enemies.
- Q3.5 DoTs: Bleed (on act) and Burn (on time) only, with Poison and Acid folded in?
- Q3.6 Confusion: cut, or keep it as an enemy-only funny status (P4)?
- Q3.7 Move stores, cadence grants, rerolls and shield off the status panel as described?
- Q3.8 One shared stack and decay rule, even where a status loses some nuance?

### Revision 1 (after your answers, 2026-11-17)

**Decision log**
- **D3.1** Explore **grouping statuses by what they do to a combo** (keep it going / break it /
  tempo / pay it off / punish it). Adopted as the working structure.
- **D3.2** Statuses have a **loose rarity**: common, uncommon, rare. There must be a full
  **damage quadrant**: raise damage dealt, lower damage dealt, raise damage taken, lower damage
  taken. Weaken stays as the common "deal less", paired with a new **Empower**. 39 uses is
  plausible for a common status; trim if it crowds the pool.
- **D3.3** **Stun is emergent, not an explicit mechanic.** Claude's call for simplicity: there is
  no Stun status. Stagger stacks that overflow cost the holder a swing, which is a stun when it
  happens. It's rare because it takes real investment to reach.
- **D3.4** **Disrupt needs a rework.** In your words, you can't take an action while the other
  person is in a combo. (Today turns interleave on the speed timeline, so the reset *can* land
  mid-combo, but as a design it doesn't read as a real counter.) Options are below.
- **D3.5** **DoTs are barely used and need a better implementation**, not just merging. Sub-design
  below.
- **D3.6** **Confusion stays, for both sides.** Hitting yourself can pay off: it counts as Took
  Hit and self-cost for builds that feed on those (Blood Price, ONSELFCOST). Rare.
- **D3.7** **Things live on their owner.** Queued grants live on the actions that made them,
  stores live with their material. Statuses stay on the actor, and **the board must show what's
  attached to the current turn**: the NOW card lists everything that will apply to this swing.
- **D3.8** **No shared stack/decay rule.** Each status keeps its own duration. The chip always
  shows what's left (stacks, swings or time).
- **New idea (→ Phase 5):** **actions that expire during combat** and get removed from the combo
  for the rest of the fight or dungeon.

**Revised status set (13)**

| Rarity | Group | Status | Chip text |
|---|---|---|---|
| Common | Damage | **Empower** / **Weaken** | deals +X% / deals −X% |
| Common | Damage | **Expose** / **Guard** | takes +X% (armor breaks at the cap) / takes −X% |
| Common | Roll | **Focus** / **Rattle** | +N / −N to its rolls |
| Uncommon | Tempo | **Haste** / **Stagger** | swings faster / next swing slower; overflow costs a swing |
| Uncommon | Pay off | **Mark** | the next hit against it crits |
| Uncommon | Punish | **Bleed** / **Burn** | see the DoT sub-design |
| Rare | Chaos | **Confusion** | may hit itself (and builds can profit from that) |
| Rare | Break | **Disrupt** (reworked) | see options |

**Absorbed:** Harden, Fortify, Temp HP and Absorb → Guard. Pierce and Acid → Expose. Brittle → Mark.
Slow → Stagger. Stun → Stagger overflow. Roll Penalty → Rattle. Poison → the DoT redesign.
**Cut:** Silence, Stat Drain, and Reflect and Regen as statuses (the REFLECT and HEAL verbs stay).

**Disrupt rework: options**
- **A. Jam:** the holder's next strip card is jammed. When its combo reaches that slot, the card
  is skipped and the combo jumps past it. It works no matter whose turn it is, and it shows on
  the strip as a crossed-out card.
- **B. Burn out a card:** the holder's *last* strip card is lost for the rest of the fight. This
  ties into the expiring-actions idea, and it's a rare, dramatic counter to long strips.
- **C. Break on entry:** Disrupt arms on the holder. Its next combo can't advance past slot N
  (a ceiling, not a reset).

**DoT sub-design: make DoTs part of the combo**

Today the four DoTs tick in four different ways, and almost nothing builds around them. Proposal:
couple each DoT to a combo.
- **Bleed punishes the holder's chain.** It ticks each time the holder lands a combo step, so a
  long enemy chain bleeds it harder. It's a soft counter to enemy chains (P2 mirror).
- **Burn rides *your* chain.** Each combo step you land adds a stack, and when your combo ends
  (broken or completed), the stacks burst for damage. That's a build payoff for long strips.
- Both show on the strip they're tied to: Bleed drips on the enemy's lit cards, and Burn glows on
  yours.

**Questions**
- Q3.9 Disrupt: A (jam a card), B (burn out a card) or C (combo ceiling)?
- Q3.10 DoTs: is coupling Bleed to *their* chain and Burn to *your* chain the right direction?
  Is there a role for a third, slow DoT (old Poison) on bosses, or is two enough?
- Q3.11 Is 13 statuses the right size, or should the uncommon/rare rows shrink further?
- Q3.12 Rarity: should a status's rarity follow the rarity of the gear and actions that grant it
  (common statuses on Common/Uncommon gear, rare ones on Rare+)?


### Revision 2 (2026-11-17)

**Decision log**
- **D3.13** **Start small and build up as needed.** The status set shrinks to a core, and statuses
  get added back only when a rung needs one.
- **D3.14** **Each class has one signature status** it's strongly associated with. A few more
  statuses can belong to other places (dice modifications, regions, enemies).
- **D3.15** (Q3.9) Your question was whether to convert Disrupt into the three ideas. Claude's
  recommendation: **retire the name Disrupt** and don't make these statuses at all. *Jam* (skip the
  foe's next card) and *Burn out* (the foe loses a strip card for the fight) become **strip
  effects** on actions, introduced on the same late rung as expiring actions. *Ceiling* is dropped
  because it overlaps Rattle.
- **D3.16** (Q3.10) **Bleed** stacks, its damage decrements over time, and it ticks whenever the
  holder takes any action, misses included. That **matches the code today**
  (`ProcessBleedAfterActorTurn`). **Burn** is fun as it is. The DoT problem is **usage, not
  rules**: few actions and items apply them.
- **D3.17** (Q3.11) The size of the set is Claude's call; we refine or expand later.
- **D3.18** (Q3.12) **No:** a status's rarity doesn't follow the rarity of the gear that grants it.

**The core set (7): one per class, plus three neutral**

| Owner | Status | Why it's theirs (class identity, data today) | Chip text |
|---|---|---|---|
| Barbarian | **Stagger** | "take pain, make momentum": impacts; mace actions Stagger; the tree leans Harden/Stun, and Stun now emerges from Stagger overflow | next swing slower; overflow costs a swing |
| Warrior | **Guard** | "convert defense into tempo"; the tree and actions lean Harden/Fortify | takes −X% |
| Rogue | **Bleed** | "marks, status conversion, exact rolls"; the tree mentions Bleed 23 times | stacks; ticks on every action, decaying |
| Wizard | **Burn** | "elemental reactions"; the tree mentions Burn 13 times | ticks over time, decaying |
| Dice (neutral) | **Focus** / **Rattle** | the dice-modification system; enemies Rattle you, gear gives Focus | +N / −N to rolls |
| Neutral | **Expose** | the general payoff: everything else hits it harder | takes +X% (armor breaks at the cap) |

Each class has the means to *cause* its signature status and the skills to *exploit* it. The other
classes can still touch it through gear, but less often.

**Added later, when a rung needs them:** Weaken and Empower (completing the damage quadrant), Haste,
Mark, Confusion. **Gone:** Disrupt (now strip effects), Stun (emergent), and everything else from
the revision 1 absorbed list.

**Questions**
- Q3.19 Do these four class statuses fit (Barbarian Stagger, Warrior Guard, Rogue Bleed, Wizard
  Burn)? Mark is the other strong Rogue candidate, and Focus the other Wizard one.
- Q3.20 Do the six hybrid classes each get a status, or do hybrids mix their parents' statuses?

### Revision 3 (2026-11-17) — Phase 3 closed

**Decision log**
- **D3.21** (Q3.19) **The Wizard's identity lives in its action sequence**, in the spirit of Noita's
  spell composition: actions that trigger, repeat or modify the actions after them ("this action
  triggers the next two"). **Burn stays as its status, tentatively.** It works best as what echoes
  and retriggers multiply. The content mostly exists already: of 56 Wizard-tagged actions, the strip
  manipulators (STRIP JUMP / SKIP / REPEAT / LOOP / RANDOM / SHUFFLE, RETRIGGER NEXT / SLOT, ECHO SPELL,
  CODA, PRELUDE, RISING CHORD, CONCENTRATED CHAOS) are the identity. Weaken / Hex / Enervate and the
  material-store verbs dilute it. → Phase 5.
- **D3.22** (Q3.20) **Hybrids don't get their own statuses.** They mix or modify their parents'
  things, recontextualizing them. Examples to test: Ravager (Barbarian + Rogue): Stagger overflow
  makes Bleed tick. Spellblade (Warrior + Wizard): each retrigger adds Guard. Hexblade (Rogue +
  Wizard): echoed actions re-apply Bleed. The six hybrid attributes (Wits, Body, Prowess, Will,
  Finesse, Mind) go to Phase 7 as a complexity question.
- **D3.23** **Bleed and Burn need more sources, not new rules.** That's a content task for the
  class pools (Phase 5 and the content track).

**Final status core (7):** Barbarian **Stagger**, Warrior **Guard**, Rogue **Bleed**, Wizard **Burn**
(tentative), plus **Focus**, **Rattle** and **Expose**. Others come back only when a rung needs them.

---

## Phase 4 — Items and inventory

### Inventory (measured 2026-11-17)

- **Bases:** 409 (200 weapons: Dagger 51, Wand 51, Sword 50, Mace 48; 209 armor across 4 slots) and
  3 charms. Every base has a fixed **base modifier** (a rule), a tier and attribute requirements.
- **Rarity:** 6 levels (Common → Mythic), 1–10 lines with an "explode" chance, all available from
  level 1.
- **Mod families:** 89 (62 effect mods, 16 stats, 10 hybrid attributes such as Wits or Will, and 1
  extra-slot family), each with tiers. 3 suffix kinds (descriptor, animal, poetic), plus 25 unique
  rule prefixes and affix templates (LISTEN, TUNE, OPENER…).
- **Materials:** 18 (Iron, Steel, Bone, Bronze, Gold, Mithril, Glass, Shadow, Obsidian, Willow,
  Silver, Crystal, Stone, Unknown, Strange, Celestial, Wood, Cloth), each with a 2/3/5-piece ladder,
  a store and a spend action.
- **Two numbers** on every card: tier and item level.
- **Inventory screen:** equip, unequip, discard, trade up 5 → 1, sort, slot filter, material filter,
  hide unmet requirements.

**What a real Uncommon shows today** (Iron Breeches of the Young Ox, level 2):
- A **modifier tied to an action you don't have**: "On connect (every 2) with BODY BLOW → 1 Expose.
  BODY BLOW is not in your strip". The starter Leather Pants do the same with BLUDGEON.
- **Strength on three lines** (item base +5, Iron +4) beside Agility +3, Max HP +9.22% and Defense +5.
- A full material ladder, the CRISIS store, IRON CULL, banked stores, affixes, a granted action
  (LOW BLOW: Weaken) and a requirement. That's about 20 lines for a level 2 pair of pants.

### Starting proposal

**1. Rarity tells you what an item *does*** (independent of level, G1.5):

| Rarity | Carries | New concept it introduces (on first drop) |
|---|---|---|
| Common | stats and a material | items, materials |
| Uncommon | + **an action** and **a modifier about that action** | actions from gear, synergies |
| Rare | + more stat lines (bigger numbers) | loot depth |
| Epic | + a second modifier, or an affix template | stacking rules |
| Legendary | + a **unique rule** (Vampiric, Thorned…) that bends the game | rule-bending items |

Mythic merges into Legendary (5 rarities instead of 6).

**2. A modifier only refers to things the item brings, or to your class.** Never "with BODY BLOW"
for an action you don't have. A modifier talks about *its own action*, the class status or a
role ("your Payoffs…"). That makes every Uncommon self-contained: one item, one idea.

**3. The card has a fixed reading order, about 5 lines:**
```
IRON BREECHES OF THE YOUNG OX                 Uncommon legs · Iron · T1
  vs Leather Pants:  +7 STR  +3 AGI  +9% HP  −2 DEF   · breaks Leather (lose Lucky)
  ▸ LOW BLOW — payoff · applies 2 Weaken
  ◆ every 2nd LOW BLOW that connects also applies Expose
  Iron 1/5 — wear 2 to start storing CRISIS
```
- **One line per attribute** (base, material and suffix summed; the breakdown is in details).
- **One power number:** Tier. Item level is hidden (it still drives the roll).
- **Material shows only the next step**, not the whole ladder.

**4. Fewer materials.** 18 → **12 class materials** (3 per class, which the class trees already count)
plus **Leather** as the neutral starter set. Stone, Unknown, Strange, Celestial, Wood and Cloth go, or
become cosmetic names.

**5. Fewer bases, and only tier-scaled.** With actions and modifiers coming only at Uncommon, a base
mainly sets slot, class, tier and material. Target about **4 per slot per class tier band**, instead of
50 per weapon type.

**6. The loot moment happens once, at the end of the dungeon** (D5.8): a **spoils screen**.
- Each item shows as **one comparison** against what you wear, with a verdict: *upgrade*,
  *sidegrade (why)* or *worse*.
- One button per item: **Equip**, **Keep** or **Salvage**. "Worse" Commons are pre-marked for
  salvage.
- Trade-up (5 → 1) stays as the salvage sink.

**7. A small, smart bag.** The bag holds what you chose to keep. The default view is "upgrades for
you", and the full list with filters is secondary.

**8. Hybrid attribute lines leave items** (Wits, Will, Mind…), pending Phase 7.

### Questions
- Q4.1 Does the rarity table (what each rarity adds) match how you want items to grow? Mythic into
  Legendary?
- Q4.2 Should a modifier only ever talk about its own action, the class status or a role?
- Q4.3 Materials: 12 class materials plus Leather. Which of the 18 must survive?
- Q4.4 Are attribute requirements worth keeping? (They add a "Requires STR 10" line and a filter.)
- Q4.5 The spoils screen at dungeon end: is that the loot moment you want?
- Q4.6 Should base modifiers, affix templates and unique rules become one "item rule" concept, unlocked
  by rarity (Uncommon: about its action; Epic: a template; Legendary: unique)?
- Q4.7 Charms: keep the slot (3 charms today), grow it, or cut it?

**Decision log (2026-11-17)**
- **D4.1** **Keep all 6 rarities at their current scale.** Many players will play this, so there must
  be exceptionally rare items (Mythic weight is 5 in about 100,000 today). Revised table:

  | Rarity | Carries |
  |---|---|
  | Common | stats and a material |
  | Uncommon | + an action and a modifier about that action |
  | Rare | + more stat lines |
  | Epic | + a second modifier (or an affix template) |
  | Legendary | + a unique rule that bends the game |
  | Mythic | Legendary, plus something that changes a whole system (draft: a strip slot, a class-bending rule, or a cross-class effect). Exceptionally rare and worth telling people about |

- **D4.2** **Start with self-contained modifiers.** A modifier refers only to its item's own action,
  the class status or an action role.
- **D4.3** **Materials:** the 12 class materials (already 3 per class: Iron, Steel, Bone / Bronze,
  Gold, Mithril / Glass, Shadow, Obsidian / Willow, Silver, Crystal), the starting **Leather**, and
  **at least 3 non-class materials**, chosen from Stone, Unknown, Strange, Celestial, Wood and Cloth.
- **D4.4** **Attribute requirements stay.** They're crucial to builds.
- **D4.5** **The loot reveal is dramatic and happens in parts**, not one compressed screen. The same
  sequencing principle as the combat beat applies.
- **D4.6** Unifying base modifiers, affix templates and unique rules into one "item rule" concept:
  **not sure; parked.** Claude's lean: decide after the self-contained modifier rewrite, since that
  shows how far apart the three really are.
- **D4.7** **Charms are cut for now.** They added depth that isn't needed.

**The staged reveal (draft for D4.5).** At the end of a dungeon, each item reveals in steps, and higher
rarities take longer:
1. **The drop:** a glow and sound in the rarity's color. Rare and up get a beat of anticipation.
2. **What it is:** name, slot, material and tier.
3. **How it compares:** stat changes against what you wear, one line per attribute, with a set
   gained or broken.
4. **Its action** (Uncommon+): the card slides in with its role.
5. **Its modifier** (Uncommon+), then its **unique rule** (Legendary+), each as its own beat.
6. **The verdict:** upgrade, sidegrade or worse, then Equip / Keep / Salvage.

Commons reveal fast and in a batch, so the screen's time goes to the items that matter.

- **D4.8** (Q4.8) Non-class materials: **Stone** (sturdy, early), **Strange** (chaos, home of
  Confusion) and **Celestial** (rare, high-end). Wood, Cloth and Unknown become cosmetic names or go.
- **D4.9** (Q4.9) A Mythic's system change is **a rule that bends your class**: for example, a Rogue
  whose Bleed ticks on *its own* actions, or a Barbarian whose Stagger overflow stuns itself into a
  rage. One per class at first.

**Phase 4 closed (2026-11-17)**, with D4.6 (item-rule unification) parked.

---

## Phase 5 — Actions and the strip

### Inventory (measured 2026-11-17)

- **220 actions:** 52 neutral, about 25 per class (103), 60 hybrid (6 × ~10), 5 unique. 162 are tier 3.
- **Rough kinds** (by description and columns): about 98 setups and buffs (cadence grants: "next
  action +X", "next 3 actions…"), 40 store verbs, 31 scaling payoffs ("+25% AMP per 10 Will"), about 21
  sequence manipulators (strip jump / skip / repeat / loop / retrigger), and only about 30 plain hits.
  **Almost no action simply applies its class's status.**
- **50 data columns** in use, plus 9 cadence-timing options.
- **Strip length has 5 sources:** a base of 2, class upgrades, a Wand bonus, gear extra slots and a
  level cap, all under an absolute max of 8.
- **Actions come from gear:** every item grants one action (Commons too, D0.9), plus the starter.

### Starting proposal

**1. Every action has one role.** Four roles, shown as an icon on the card, and every action's text
fits one line: *"[role] [when] → [what]"*.

| Role | What it does | Example line |
|---|---|---|
| **Hit** | deals damage; the backbone. Class hits apply the class status | `STAB — hit · applies 1 Bleed` |
| **Setup** | makes later slots stronger | `PRELUDE — setup · next 3 actions +15% AMP` |
| **Sequence** | acts on other actions: trigger, repeat, jump | `ECHO — sequence · on combo, the next card fires twice` |
| **Payoff** | cashes in what came before | `CODA — payoff · everything queued lands here at +40%` |

The fun of "combining actions" (D0.3) is then legible: Setup → Hit, Hit → Payoff, and Sequence
multiplies whatever it touches. The strip editor can show those links as arrows between cards.

**2. Each class plays the strip differently**

| Class | Strip style | Status |
|---|---|---|
| Barbarian | short and heavy: big hits, Stagger build-up, payoffs from pain | Stagger |
| Warrior | steady: Guard setups that turn into tempo, clean sequences | Guard |
| Rogue | many quick hits, Bleed stacking, payoffs on exact rolls | Bleed |
| Wizard | **composition** (D3.21): modifiers and triggers that act on the cards after them | Burn |

**3. The Wizard's sequence vocabulary: three verbs**
- **MODIFY**: the next N cards get X (PRELUDE, RISING CHORD, CHANNEL).
- **TRIGGER**: fire the next N cards *now*, as part of this action ("triggers the next two") —
  multicast. RETRIGGER NEXT, ECHO SPELL.
- **FLOW**: repeat, jump or loop the strip. Keep the 2–3 clearest of STRIP JUMP / SKIP / REPEAT / LOOP /
  RANDOM / SHUFFLE.

The material-store verbs (DISTILL, TRANSMUTE, INVEST…) and Weaken / Hex / Enervate leave the Wizard pool.

**4. A smaller pool that grows along the ladder.** Target about **100 actions** instead of 220:
about 14 per class, about 12 neutral, and about 5 per hybrid (hybrids remix their parents' cards, D3.22).

| Arrives with | Actions available |
|---|---|
| Level 1 | the class starter (a Hit) |
| Items (L2) | class Hits and a few neutral Hits |
| Action interactions (~L3) | Setups and Payoffs |
| Wizard / Sequence (~L3+ for Wizards, later for others) | Sequence cards |
| Stores (L13+) | store verbs |
| Cross-class (later) | hybrid cards |

**5. Strip length from one or two sources.** Proposal: slots grow with **level** (as the slot rise
already does), plus **rare gear** that adds a slot. Class upgrades, the Wand bonus and the 5-way stack go.
Open: does INT also add slots (the old intent: the Wizard has the most slots)?

**6. Expiring actions (your idea)**
- An action can be **Fragile N**: it works N times, then **burns out**. The card cracks on the board and
  leaves the strip for the rest of the fight (or the dungeon).
- Fragile cards are **stronger than normal** for their tier: one-shots worth planning around.
- The strip **closes up** when a card burns out, so the combo is shorter afterwards. A burned-out card
  can be a payoff trigger ("when a card burns out, …").
- The enemy-side **Burn out** strip effect (D3.15) uses the same mechanic, and so does **Jam** (skip
  the next card, one time).

**7. Cadence timing stays mostly internal.** The card shows *where* its effect lands, as arrows on the
strip. Only shapes a player can see survive: ramp, charge and RELEASE.

### Questions
- Q5.1 Do the four roles (Hit, Setup, Sequence, Payoff) cover what you want an action to be?
- Q5.2 Is about 100 actions the right target, or should it be smaller still at first?
- Q5.3 Strip length: level plus rare gear only, or should INT (or the Wizard) add slots too?
- Q5.4 Expiring actions: do they last for the fight or the dungeon? Does the strip close up, or does
  the dead slot stay (and break the combo)?
- Q5.5 Should the strip be editable mid-dungeon (between rooms) or only between dungeons?
- Q5.6 Do Commons still grant an action at all, or do actions start at Uncommon like base modifiers?
  (D0.9 said yes, tentatively.)

**Decision log (2026-11-17)**
- **D5.1** The roles are right, but **Hit is redundant with Payoff**. There are **three roles**:
  **Setup**, **Sequence** and **Payoff**. A plain attack is a Payoff that cashes in only the combo
  multiplier and its class status (`STAB — payoff · applies 1 Bleed`).
- **D5.2** **About 100 actions in total, covering every source**: characters and classes, random
  (neutral) actions, materials, enemies and environments. Sources can overlap (an enemy can use a
  hero card, which is the mirror), and an overlapping card counts once. Draft budget:

  | Source | Today | Budget |
  |---|---|---|
  | Classes (4) | 103 | ~48 (12 each, incl. starter) |
  | Hybrids (6) | 60 | ~12 (2 each; they remix parents' cards) |
  | Neutral / random | 52 | ~10 |
  | Materials | 18 builds, spend actions mixed into the neutral pool | ~12 (one per kept material) |
  | Enemy kits | 15 templates + 232 flavor names | ~10 templates; mostly hero cards (mirror); names are free |
  | Environments | 30 | ~8 |
  | **Total** | **~300 distinct mechanics** | **~100** |

- **D5.3** Strip length = **level + gear + class skill upgrades**. Skill-tree nodes that add a
  slot **need to be added**. The Wand bonus and the class-point upgrade path go.
- **D5.4** Fragile (expiring) cards burn out **for the rest of the fight**.
- **D5.5** The strip is **editable only between dungeons**. The strip you bring is the build you
  commit to (P2).
- **D5.6** **Actions start at Uncommon.** A Common is stats and material only. This **replaces
  D0.9's "Commons keep their gear action"**. Starter gear is the exception: it carries the class
  starter.

- **D5.7** (Q5.4b) When a Fragile card burns out, **the strip closes up**: the combo is one card
  shorter for the rest of the fight.
- **D5.8** (Q5.7) **No gear mid-dungeon.** Gear neither changes nor arrives during a run, and loot is
  handed out at the end of the dungeon (room searches already give only consumables). Between rooms, the
  only things that change are HP and consumable buffs.

**Phase 5 closed (2026-11-17).**

---

## Phase 6 — Build vocabulary: events, stores, verbs, rules

### Inventory (measured 2026-11-18, mentions across item, skill, action, material and enemy data)

**Events (41 tokens defined).** Used in data: ONCONNECT 178, ONCASH 76, ONCOMBO 72, ONCRITICAL 55,
ONSTATUSAPPLY 51, ONFIRSTHIT 27, ONTAKEHIT 25, ONMISS 25, ONSTOREFULL 22, ONCOMBOEND 18, ONKILL 17,
ONAFTERMISS 12, ONROOMSCLEARED 8, ONCONSECUTIVEATTACK 6, ONCRITICALMISS 5, ONATTACK 5, and about 20 tokens
used 1–3 times (ODD, EVEN, ROLLVALUE, NATURALROLL, BEAT, SLOW, FOCUS, ENEMY_HEALTH_THRESHOLD…), with both
`ON_X` and `ONX` spellings. Plus WHILE (passive) 285.

**Stores: one per material (18).** Each material mints its own store on its own event, and has its own
spend action:

| Material | Earns on | Store | Spend |
|---|---|---|---|
| Iron / Steel / Bone | enemy HP mark / combo end / taking a hit | CRISIS / TRIUMPH / RAGE | IRON CULL / STEEL CREST / BONE WRATH |
| Bronze / Gold / Mithril | each turn / Slow / consecutive attacks | BEAT / DRAG / STREAK | DRUM CUT / GOLD ANCHOR / MITHRIL FLOW |
| Glass / Shadow / Obsidian | crit / each attack / kill | GRAZE / SWING / TROPHY | GLASS NICK / UMBRA / HEADHUNT |
| Willow / Silver / Crystal | combo / Focus / connect | CHAIN / LENS / CONTACT | WILLOW LASH / REFRACT / RESONATE |
| Stone / Unknown / Strange / Celestial / Wood / Cloth | connect / turn / first hit / after a miss / odd roll / taking a hit | GRIT / ENIGMA / ODDITY / OMEN / KINDLING / FRAY | LANDSLIDE / REVEAL / QUIRK / STARFALL / KINDLE / UNRAVEL |

**Generic verbs (11):** CASH IN, DETONATE, LIQUIDATE, RECKONING, INDEX, MARK, BARRIER, DISTILL, TRANSMUTE,
INVEST, PALM. They act on the **attuned** store (your most-worn material) or the target's statuses.

**Rule keywords used:** APPLY 205, EVERY 145, RANK 83, MINT 77, TAGAS 75, DAMAGE 60, ADD 57, AMP 40,
SETCOUNT 39, COMBO 23, COUNTAS 21, EXEMPT 21, POTENCY 17, CRIT 16, NEXT 16, SPEED 15, WEAPON 14, BUFF 13,
HIT 13, HEAL 12, PER WORN 8. **Unused:** PREVTAG, NEXTTAG, PER SELF, PER TAGS (0); ONCE, CHANCE, REFLECT,
MULTIHIT (1 each). RANK, TAGAS and SETCOUNT come almost entirely from the 11-17 base modifiers.

### Starting proposal

**1. About 11 player-facing events, built on the dice and the chain (D2.8):**

| Event | Means | Absorbs |
|---|---|---|
| **On hit** | a roll connects | ONCONNECT, ONATTACK, ONANYHIT, ONNORMALHIT |
| **On miss** | a roll misses | ONAFTERMISS (as "your next roll after a miss") |
| **On crit** / **On crit miss** | natural 20 / natural 1 | ONCRITICAL, ONCRITICALHIT, ONCRITMISS |
| **On action** | a chain link fires (a strip card) | ONCOMBO, ONCOMBOHIT, ONCONSECUTIVEATTACK |
| **On chain end** | your chain stops (with its length) | ONCOMBOEND, ONCOMBOENDED |
| **On chain complete** | the chain reached the end of the strip | (new name; it exists as the strip-end stop) |
| **On kill** / **On take hit** | | ONRECEIVEHIT, ONHEROHURT |
| **On status** | you apply a status (On status: Bleed) | ONSTATUSAPPLY, ONSLOW, ONFOCUS |
| **On turn** | your turn starts | ONTURN, ONBEAT |

Roll details (odd, even, natural 7, first hit, the enemy's HP marks) become **conditions** ("On hit, if
the roll is odd"), not events. Store events (ONCASH, ONSTOREFULL) stay internal to stores. ONROOMSCLEARED
stays a dungeon-level rule.

**2. Stores are named after their material.** "Iron 3", not "CRISIS 3". The earn rule is written plainly
("Iron charges when your hit pushes an enemy past an HP mark"). That's 18 words removed from the vocabulary.
Each kept material (16, per D4.3/D4.8) has one store and **one spend action, its own**.

**3. Re-map each class's three materials onto the chain.** Since the chain is now the core (D2.8), a class's
materials should each earn from a different chain moment. For example, Rogue: Glass on crit, Shadow on hit,
Obsidian on kill; Wizard: Willow on action, Silver on status, Crystal on chain complete. That way, every set
is a different way of playing the same chain.

**4. Generic verbs: 11 → 2.** Keep **CASH IN** (spend your attuned store, so any material set can cash out)
and **DETONATE** (spend a target's status stacks for damage). LIQUIDATE, RECKONING, INDEX, MARK, BARRIER,
DISTILL, TRANSMUTE, INVEST and PALM go, or become a material's own spend action.

**5. One rule language for items and skills, scoped by rarity** (answers the parked D4.6). The grammar
stays internal; every rule renders as one plain line.

| Source | May touch |
|---|---|
| Uncommon modifier | its own action, the class status, an action role (D4.2) |
| Epic second modifier | also your set, skills and tags (SETCOUNT, RANK, TAGAS) |
| Legendary unique rule | the game's rules (EXEMPT, COUNTAS, crit / miss rules) |
| Mythic | your class (D4.9) |
| Skill nodes | anything their tree is about |

So the 11-17 base modifiers that use RANK / TAGAS / SETCOUNT move up to Epic, and Uncommon modifiers get
rewritten to be self-contained.

**6. Cut unused grammar:** PREVTAG, NEXTTAG, PER SELF, PER TAGS, and both spellings of each event (`ON_X` →
`ONX`).

### Questions
- Q6.1 Are these ~11 events the right player-facing set?
- Q6.2 Should stores be named after their material ("Iron 3") instead of CRISIS, RAGE, GRAZE…?
- Q6.3 Re-map each class's three materials onto different moments of the chain?
- Q6.4 Generic verbs down to CASH IN and DETONATE?
- Q6.5 One rule language scoped by rarity (the table above), which closes the parked D4.6?
- Q6.6 Attunement (your most-worn material decides what CASH IN spends): keep, or make CASH IN choose your
  fullest store?
- Q6.7 Stores are banked until you leave the dungeon (today). Keep that?

**Decision log (2026-11-18)**
- **D6.1** **About 11 player-facing events:** hit, miss, crit, crit miss, action, chain end, chain complete,
  kill, take hit, status applied, turn start. Roll details become conditions.
- **D6.2** **Stores keep their currency names** (CRISIS, RAGE, GRAZE…). The name describes the mechanic the
  material affords, which a material name can't.
- **D6.3** **Each material approaches builds differently, off a different trigger.** Within a class, the three
  materials must not overlap in what they reward. (The chain moments in proposal 3 are one way to do this,
  not a requirement.)
- **D6.4** **Generic verbs: CASH IN and DETONATE only.** The other nine go or become a material's own spend.
- **D6.5** **One rule language for items and skills, scoped by rarity** (closes D4.6).
- **D6.6** **CASH IN spends your fullest store.** Attunement (most-worn material) goes.
- **D6.7** **Stores stay banked until you leave the dungeon.**
- **D6.8 (new):** **Stores come in two kinds.** Some are **spent**: the trigger or spend action consumes
  the currency. Others **scale**: the currency feeds an effect *without being consumed* when it triggers
  (for example "+2% damage per RAGE held").

**Your question on D6.5: does everything use the same trigger system?** Yes, that's the goal: every rule,
wherever it lives (item, skill node, material, action, enemy kit), is `WHEN event [IF condition] → effect`
or a passive `WHILE [IF condition] → effect`, fired from the same ~11 events by one engine. Today the
*vocabulary* is already shared (`TriggerEvents`), but there are **parallel paths**: action trigger bundles
(`triggerBundlesJson`), material earn rules (`"ON_X = STORE"`), CrossWires rules, Listener affixes, and hub
events (ONCASH, ONSTOREFULL…) that action bundles can't see. Merging those into one path is part of the build
plan (Phase 10).

- **D6.9** (Q6.8) **A material does one thing: it creates its currency** (its single affordance, earned
  on its trigger). **Spend versus Scale is decided by whatever uses the currency**, not by the store: some
  triggers and actions **spend** it (consume it for an effect), others **scale** off it (read the amount
  without consuming it). Other sources (items, skills) can also grant passive amounts of a currency.
  The draft "each class has one Spend and one Scale material" rule is withdrawn.

  | Consumer | Example card line |
  |---|---|
  | Spend action / trigger | "IRON CULL: spend all CRISIS → +STR for the fight" |
  | Scale action / trigger / passive | "BONE WRATH: +4% damage per RAGE you hold" |
  | Passive source | "Ox Hide Boots: start each fight with 2 RAGE" |

- **D6.10** (Q6.9) **No caps or decay.** Every amount of currency has a **scope** (it lasts for the **turn**,
  the **fight** or the **dungeon**), set by whatever granted it, and that's the limit. Store banking for the
  dungeon (D6.7) is the longest scope.

**Phase 6 closed (2026-11-18).**

---

## Phase 7 — Progression: levels, attributes, skills, classes

### Inventory (measured 2026-11-17)

- **Levels 1–100.** Each level-up gives attributes (**+3 to the main attribute of the class of the
  weapon you're holding, +1 to the others**), **1 class point on that weapon's path**, and skill points.
- **Class points** (4 paths: Barbarian, Warrior, Rogue, Wizard). Tier thresholds at 2 / 20 / 60 / 120
  points each add a **strip slot**. You multiclass by levelling while holding another class's weapon,
  which isn't explained anywhere.
- **4 class trees** with 40–45 nodes each, in branches **Core (11), one per class material (6–11 each),
  Rite (6–7)**, plus a stray "Confluence" node. Tiers up to 19, ranks up to 5, 15 either-or pairs per tree.
  About half are rule nodes; many are "+X% damage per Rogue piece / tag, per rank".
- **6 hybrid trees** (7 nodes each) that open at 10 + 10 points in two classes. Each introduces a
  **hybrid attribute** (Body, Prowess, Will, Finesse, Wits, Mind = sums of two attributes) that "becomes
  your primary attribute", and "+25% per 10 hybrid tags" actions.
- **Tags as currency:** class materials count as class tags; hybrid tags count both parents' tags.
- **Persistence** capstones; the **death clone** keeps your gear.

### Starting proposal

**1. One currency: skill points. Your class is where you spend them.** Weapon-path class points go away.
Class tiers (and their strip slots, D5.3) come from **slot nodes in each tree's Core**. You multiclass by
spending in a second tree, out in the open.

**2. Each tree has a fixed shape, about 20 nodes:**
```
CORE (6)       class identity: the signature status (D3), the strip style (D5), slot nodes
├─ MATERIAL A (4)   synergy with that material's set: opens when you wear 2 pieces of it
├─ MATERIAL B (4)
├─ MATERIAL C (4)
└─ RITE (2–3)       the either-or capstone (cross-class Rite pick stays)
```
- Nodes **bend rules or add actions**; flat "+X% per tag, per rank" filler goes.
- Ranks: 1 for most nodes, 3 at most for a few.

**3. Hybrids become bridge nodes, not trees.** When you've invested in two class trees, a small **bridge**
(2–3 nodes) opens between them. It **recontextualizes the parents' things** (D3.22): Ravager: Stagger overflow
makes Bleed tick. Its 2 hybrid action cards (D5.2) come from there. **The 6 hybrid attributes go**, and so do
the hybrid tag counters.

**4. Every level-up is visible.** Attributes still grow automatically by class, and each level gives
**a skill point with a prompt** (once the tree has opened, G1 ladder), shown in a short level-up moment:
"+3 TEC · +1 to the rest · 1 skill point → Shadowcraft".

**5. The tree opens in parts (ladder).** The first skill point opens Core only (~L11–12). A material
branch opens when you first wear 2 pieces of that material (first event, G1.5). Rite opens deep in Core.
Bridges open when the second tree is invested.

**6. Tags stay internal.** Player-facing text says "per Rogue piece you wear", never "per 10 Ravager tags".

### Questions
- Q7.1 Merge class points into skill points (your class = where you spend), and drop weapon-path points?
- Q7.2 Tree shape Core + 3 material branches + Rite, about 20 nodes per class?
- Q7.3 Hybrids as bridge nodes, and drop the 6 hybrid attributes?
- Q7.4 Attribute growth: automatic by class (as today), or a point you allocate each level?
- Q7.5 Cut "+X% per tag, per rank" filler nodes so every node bends a rule or adds an action?
- Q7.6 Ranks: mostly 1, a few up to 3?
- Q7.7 Death: what should it cost and keep? (Today the clone keeps gear.)

**Decision log (2026-11-17)**
- **D7.1** **Class points come from your weapon** (as today). The weapon deciding your class is the most
  important part of the build. Proposal 1 is withdrawn. Make it visible instead: the level-up moment says
  "+1 Rogue point (Dagger)", and the weapon card says which path it levels.
- **D7.2** **Trees are bigger than ~20 nodes:** enough that **you can't take everything** and have to make
  meaningful choices. The Core / material branches / Rite shape stays. The target is a ratio rather than a
  count: a tree should offer clearly more nodes than the points a hero will spend in it.
- **D7.3** **Hybrid attributes stay** (Body, Prowess, Will, Finesse, Wits, Mind). Hybrid trees keep
  remixing their parents' things (D3.22).
- **D7.4** **Attribute growth stays automatic**, by the weapon's class.
- **D7.5** **Cut the filler:** every node bends a rule or adds an action. No flat "+X% per tag, per rank".
- **D7.7** **Death is permanent:** your character dies and **all the items on them are lost**. **The clone
  keeps everything.**

- **D7.6** (Q7.6) **Most nodes are one-time picks**, so each point buys a new effect. A few may keep ranks.
- **D7.8** (Q7.8) **Strip slots come from both:** reaching a class tier (2 / 20 / 60 / 120 weapon-path points)
  **grants a slot**, *and* Core has **slot nodes you can buy** with skill points. Both feed the strip length
  (D5.3: level + gear + class skill upgrades).
- **D7.9** (Q7.9) **The clone is a dev tool for now.** Its player-facing rules are deferred.

**Phase 7 closed (2026-11-18).**

---

## Phase 8 — Enemies and dungeons: the mirror

### Inventory (measured 2026-11-17)

**An enemy today stacks about eight layers:**

| Layer | Data | Count |
|---|---|---|
| Base enemy | `Enemies.json`: archetype, tempo, 2 actions, attribute growth, tags | 128 (20 common, 72 uncommon, 36 rare); 13 bosses |
| Archetype | Assassin 26, Trickster 24, Sage 19, Brute 16, Acrobat 15, Knight 13, Berserker 10, Duelist 3, Warlord 2 | 9 |
| Kit | `EnemyActions.json` templates (Venom, Scorch, Rend, Crush, Sunder, Hex, Ensnare, Disrupt, Shell, Drain, Frenzy, Mend, Curse, Concuss, Pierce) | 15 (+232 flavor names) |
| Spawn prefix | Demonic, Undead, Skeleton, Ghost, Giant, Tiny, Feral, Armored, Ancient, Corrupted, Blazing, Frozen, Venomous, Hallowed, Cursed (12–35% per room enemy) | 15 |
| Region family | Rooted (+HP), Undertow, Kindled, Windswept; rank = region cycle | 4 |
| Dungeon identity | 3 signature kits, a material, an affix, a Demon boss with a phase at 50% | 12 |
| Wander | 10% of rooms and enemies come from another theme | — |
| Room | element status, terrain, 1 of 10 room affixes at 40%, 30 environment actions | — |

Plus creature affinities (undead immune to Bleed…), a weapon in the name ("with Cleaver") and 100 travel
events. **Dungeon select shows none of this**: just a name and a level.

A dungeon identity is already close to a lesson (the Den: Bleed, Frenzy, Crush; drops Bone), but its
three signature kits usually teach three unrelated things.

### Starting proposal

**1. An enemy is an archetype plus a kit, in the hero's words.** Archetypes mirror the classes, and kits
use only the 7 core statuses (D3) and the three action roles (D5).

| Archetype | Mirrors | Plays like | Punished by |
|---|---|---|---|
| **Brute** | Barbarian | slow heavy hits; Staggers you | Guard, Haste |
| **Knight** | Warrior | Guards up, then hits back | Expose, Bleed (Guard doesn't stop it) |
| **Assassin** | Rogue | quick hits that Bleed you | short fights, Guard |
| **Sage** | Wizard | builds a chain of setups, then bursts; Burns you | breaking its chain (Rattle, Stagger) |
| **Trickster** | dice | Rattles your rolls | Focus |
| **Beast** | none | plain hits, packs; the early teacher | anything |

That's 9 archetypes → 6. Berserker, Acrobat, Duelist and Warlord fold into these, or come back as
bosses.

**2. Enemies have strips that grow with the ladder** (D0.11: they chain and escalate like you).
Early enemies have 1 card, mid-game enemies 2–3, and bosses the longest. The enemy strip on the board
shows it.

**3. Layer budget: an enemy shows at most one modifier beyond its kit.** That modifier is *either* a
prefix *or* the dungeon's affix, never both. Region rank stays as invisible scaling.

**4. A dungeon is one lesson** (see it, suffer it, own it):
- Its enemies **teach one thing** (one status or behavior), with a twist from the dungeon's affix.
- Its **material answers that thing**, so its loot is the counter.
- Its **boss is the exam**: the lesson at full strength, a long chain and a phase change.
- The 12 identities map onto the ladder's systems and core statuses (Undertow → Rattle, a Brute
  dungeon → Stagger, …).

**5. Dungeon select = the matchup** (D0.8). The card shows the lesson, 3–4 enemy rows ("plays like …
· punished by …"), the material, and 1–3 ✓ / ✗ / ~ lines about *your* build. The lines are computed
from the archetype rows' "punished by" against your strip, gear and statuses. That's why the data must
use the hero's words.

**6. Prefixes are funny tags with one rule each**, introduced on a later rung: Undead = immune to Bleed,
Giant = immune to Stagger overflow, Tiny = Bleeds double… Each prefix ties to an affinity, which makes it
a matchup line, not noise. Weapon names stay as free flavor (P4).

**7. Rooms get one optional twist**, introduced on a later rung: a room affix *or* terrain, shown before
the fight. Environment actions shrink to about 8 (D5.2).

### Questions
- Q8.1 Collapse 9 archetypes to 6 that mirror the classes (Brute, Knight, Assassin, Sage, Trickster, Beast)?
- Q8.2 At most one visible modifier per enemy beyond its kit?
- Q8.3 Is a dungeon one lesson, with its material as the answer and its boss as the exam?
- Q8.4 Should enemy strip length grow with the ladder (1 card → 2–3 → bosses longest)?
- Q8.5 Keep prefixes as funny tags with one affinity rule each, or cut them?
- Q8.6 Wander (10% off-theme rooms and enemies): keep it as surprise, or cut it for clarity?
- Q8.7 Rooms: which survive (room affixes, terrain, environment actions), and should they come later
  on the ladder?
- Q8.8 128 enemies: with names free, should the *mechanical* variety be archetype × kit × strip
  length, and the 128 become skins over that?

**Decision log (2026-11-17)**
- **D8.1** **Six archetypes** mirroring the classes: Brute, Knight, Assassin, Sage, Trickster, Beast.
- **D8.2** **At most one visible modifier** per enemy beyond its kit.
- **D8.3** **A dungeon is one lesson by default**: its default enemies and rooms teach one thing, its
  material answers it, and its boss is the exam. **There's a chance of randomness** that adds chaos
  (wander, D8.6).
- **D8.4** **Enemy strips:** enemies can have sequences, but typically short ones. **Demons have the
  longest sequences.**
- **D8.5** **Prefixes stay, but need a rework.** Direction to explore: one rule each, tied to an
  affinity or a status, so a prefix is a matchup line (Undead = immune to Bleed) and still funny.
- **D8.6** **Wander stays** (off-theme rooms and enemies). **New:** a room without enemies can hold
  **someone or something you interact with that isn't hostile**.
- **D8.7** **Rooms are core to the experience, close to the beginning.** Room affixes and terrain move
  early on the ladder (see 2c), not to a late rung.
- **D8.8** **The 128 enemies become skins** over a smaller set of mechanics (archetype × kit × strip
  length). **Structure: region → dungeons → rooms → enemies.** Each region has its own set of enemies,
  matched to the rooms that make up its environments. A region can mix different dungeon types.

**Draft structure (for D8.8)**
```
REGION (5-level band, e.g. Whisperledger Wilds · Forest)
 ├─ its own enemy roster (skins over archetype × kit), each enemy tagged to room types it lives in
 ├─ room types that make up its environments (den, thicket, hollow, spring…) with their twists
 └─ dungeons = a mix of those room types; one dungeon's default mix is its lesson
```
Today each dungeon lists `possibleEnemies` directly, and each region is only a level band and a theme.
The rework makes rooms the link between them: dungeon → rooms → the enemies that live there.

**Non-hostile encounters (D8.6), with minimal new mechanics.** These reuse what exists: the 100
travel events and the consumables. Draft kinds: a **wanderer** who trades or tells you about the boss
(a matchup hint), a **shrine** that offers a choice (a buff for a cost), a **trapped creature** you can
free (a funny outcome, P4), and a **merchant** of consumables.

- **D8.9** (Q8.9) **Prefix rework rule:** each prefix has one rule, tied to an affinity or a status
  (Undead = immune to Bleed), so it reads as a matchup line and stays funny.
- **D8.10** (Q8.10) **1 in 4 empty rooms** holds a non-hostile encounter (wanderer, shrine, trapped
  creature or merchant, built from travel events and consumables).
- **D8.11** (Q8.11, "yes", read as the first option) **Room twists appear from the very first
  dungeon.**

**Phase 8 closed (2026-11-17).**

---

## Phase 9 — Feedback, momentum and the reveal schedule

### Inventory (measured 2026-11-18)

**Already built:**
- **The combat board** (`DungeonRenderer.CombatBoard`): enemy strip on top, PREV → NOW cards, the turn
  queue, the die, a staged sequence panel (attacker → roll → outcome → action → defense → effects →
  damage), and the hero strip at the bottom.
- **ComboTension:** one charge value (0 on the opener → 1 when the finisher is next) drives card states, an
  accelerating glow on the next card and a screen vignette. A miss drops it to 0.
- **Audio:** `Combat_ComboStep1–7`, `Combat_ComboFinish1–7`, `Combat_ComboComplete`, crit, crit miss, hit,
  miss, hero hurt, low health (both sides), status applied, enemy died, level up, loot.
- **Feedback widgets:** turn banner, turn spotlight, strip flashes (`HeroActionStripFeedback`),
  threshold-bar feedback, action-bonus shimmer, a transition wipe.
- **Ambient effects:** wind sway, click bursts, text-emphasis explosions and pulses, header glow, and a
  narrative video overlay.

**Missing:** attribution (*why* a roll or a hit came out the way it did), a report after a fight or a
dungeon, callouts for build moments, and any first-time introduction for a new system.

### Starting proposal

**1. The chain is the show** (D0.1, D2.8). One turn's chain escalates on every channel, link by link:
the cards grow and brighten, ComboTension charges, and the audio steps up (ComboStep1 → 7).
**Chain complete** (the strip end) is the biggest payoff: ComboComplete, the strip flashes, and the
damage total lands. **Chain broken** collapses the strip back to slot 1 with its cause shown.

**2. Momentum swings both ways** (D0.2, D0.11). The enemy's chain plays on the top strip with the same
escalation, which reads as a threat. Named swing moments:

| Swing | When | Shown as |
|---|---|---|
| **Chain broken** | your chain fails | the strip collapses; cause chip ("Rattle −2 cost this one") |
| **Comeback** | a long chain while you're under 25% HP | banner and audio lift |
| **Overkill** | a chain ends far past the enemy's remaining HP | damage total stamps big |
| **Close call** | you survive a hit with under 10% HP | heartbeat, held frame |
| **Threat** | the enemy's chain reaches 3+ | the top strip glows and the vignette shifts to the enemy side |

**3. Everything attached to the turn is on the NOW card** (D3.7): the statuses that apply, queued grants
from actions, store amounts being spent or scaled, and the odds of the next link (D2.7). The roll ruler
shows nudges, band shifts and extra dice (D2.2).

**4. Reports.** A **one-line fight result** after each fight ("Won in 6 turns · best chain 4 · Bleed did
31%"). A **dungeon summary** at the end, before the staged loot reveal (D4.5): best chain, top 3 damage
sources (named items, actions, statuses), *what hurt you most* (and what answers it, which teaches the
matchup), and new things seen.

**5. Build moments get a one-time callout per run:** the first time a set forms, a skill node or item rule
triggers, a Fragile card burns out, or a capstone fires.

**6. New-concept introductions** (the ladder, G1): the first time a system or status appears, a small
**NEW** card shows on the board, on the enemy that brings it ("see it, suffer it"), and again on the loot
that answers it ("own it"). One sentence, no help page.

**7. Funny moments get noticed** (P4): the log and board call out collisions (bleeding out mid-chain, a
Confusion self-hit that triggers your own Blood Price), plus generated names ("Ghost Living Wall Crawler
with Cleaver").

**8. An effects budget:** effects that don't serve the chain, the roll or a swing moment are candidates to
cut (wind sway, click bursts, emphasis explosions, the narrative video overlay). That also feeds the
line-count cut.

### Questions
- Q9.1 Are those the right swing moments (chain broken, comeback, overkill, close call, threat)? Add or cut any?
- Q9.2 Reports: a one-line result per fight plus a dungeon summary at the end?
- Q9.3 A **NEW** card the first time a system or status appears?
- Q9.4 Ambient effects (wind sway, click bursts, text explosions, narrative video): keep, trim, or cut
  those that don't serve the chain?
- Q9.5 Tone of the funny lines: slapstick, dry narrator, or both?
- Q9.6 Should the enemy's chain escalate as loudly as yours, or a little quieter so your chain stays the star?

**Decision log (2026-11-18)**
- **D9.1** **Swing moments:** chain broken (with cause), comeback, overkill, close call, threat.
- **D9.2** **Reports need more flavor** than a stats line. Fernis delivers them (below).
- **D9.3** **A NEW card** the first time a system or status appears, on the enemy that brings it and on the
  loot that answers it.
- **D9.4** **Ambient effects stay for now** (wind sway, click bursts, text explosions, narrative video).
- **D9.5** **New character: Fernis D Man**, a devil-on-your-shoulder narrator who talks to you in combat
  and in dungeons. Funny and absurd.
- **D9.6** **The enemy's chain escalates a bit quieter**, and its sounds are about *you being hit*
  (thuds, impacts, taking damage). Your chain sounds like *you hitting* (clangs, strikes).

### Fernis D Man (draft)

**Who he is.** A small devil who rides on your shoulder. He's on your side (mostly), knows a lot about the
dungeon (some of it true), and finds your suffering hilarious. He's the voice of P4, and he's how the game
explains itself without help pages.

**What he does.** He reuses what the game already knows: no new mechanics, just a line bank keyed to events.

| Moment | What Fernis does | Example line (draft) |
|---|---|---|
| Fight result | flavors the one-line result | "Six turns. I've seen faster funerals, but fine." |
| Dungeon summary | narrates best chain, top damage, what hurt most | "Your Bleed did a third of the work. Your sword did the paperwork." |
| NEW concept | introduces it in one sentence | "Rattle. They're touching your dice. I'd be furious." |
| Chain broken | names the cause | "That Rattle cost you a link. I'm writing it down." |
| Chain complete / overkill | celebrates | "Five links! I'd clap, but I'm holding a tiny fork." |
| Close call | panics | "Ten percent HP. I'm updating my will. You're not in it." |
| Enemy threat | warns | "Its chain is at three. This is the part where you worry." |
| Funny collision | notices | "It bled out mid-swing. Rude to the swing, honestly." |
| Matchup / dungeon select | a hint in his voice | "Lots of Knights in there. Bring something that Bleeds through armor." |
| Non-hostile encounter (D8.6) | commentary, maybe bad advice | "Free the trapped goblin. What's the worst that happens? Don't answer." |
| Death | a eulogy | "Here lies you. Tried hard. Rolled a one." |

**Rules (draft)**
- **He never blocks the board.** His lines appear in a speech bubble or the log, and he never stops play.
- **A budget:** at most one line per turn, and fewer in long fights. Big moments (NEW, chain complete,
  close call, death) always get a line; small ones roll a chance.
- **Data-driven:** a line bank in `GameData` keyed by event and moment, with tokens for names and numbers,
  like `FlavorText.json`.
- **The simple log mode keeps him**; the detailed log shows the math under his line.

- **D9.7** (Q9.7) **Fernis helps you find things, but his advice is the opposite of good: he's
  trying to get you killed.** Where something is, what's in the next room, where the boss waits: true.
  What to bring, what to equip, which door to take into danger: inverted. The matchup lines on dungeon
  select stay truthful, so Fernis contradicting them is the joke, and players learn to read him backwards.
  Example: *"Knights ahead. Wear your heaviest armor and don't bother with Bleed, trust me."*
- **D9.8** (Q9.8) Fernis is a **small sprite on your shoulder** on the board.
- **D9.9** (Q9.9) **His lines vary by class** (different snark for each of the four classes).

**Phase 9 closed (2026-11-18).**

### Lore source (added 2026-11-18)

A collaborator is writing world-building, lore and flavor text in a claude.ai artifact:
**https://claude.ai/artifact/3Rawb4TqTbdgdi3uxRB5bY** (titled *Demon Fighter*; owned outside this
workspace, so read it as reference data).

- **Setting:** the world of **Faerlan**, reached by Earth's Ark 1 colonists. Peoples: the Non (their
  descendants), the native Antem, androids called Vessels, the Faerl (Ark 2 exiles), the Rimefolk and the Ash
  Vigil. **The Crash**, the end of Earth's emotion-extraction program, made emotion land as color-coded
  phenomena and created the **demons**.
- **Tone:** melancholic and tragic (fear, grief, retaliation).
- **Places:** Antarkeia, the Glass Plains, Yveth's Ruin, Amber City, the Fire zone, Ossmoor, Faerling, the
  Vantry Isles, Kessara, the Embers' Crater, Vann's Rest, the Ammerin Hillside.
- **Creatures:** demons, wraiths, cragwings, Stillwings, salamanders, vent sprites.
- **Materials already shared with this game:** Star-Iron (Celestial), Mithril (from the robots' frames),
  Shadow (from the Black Hides' skins).
- **Structure:** a Chronicle in 14 eras. Each entry has lenses (Truth — creator only, Common knowledge,
  Legend/scholar, Original prose), a record ID (EV-, PL-, TM-, PE-, FA-, GR-, CX-) and a dating line.

**Where it plugs in:** region and dungeon names and descriptions (D8.8), enemy skins (D8.8), material
descriptions (D4.3), Demon bosses (the Crash explains them), non-hostile encounters (D8.6), item flavor
lines, and the "Common knowledge" lens as what Fernis and NPCs say (while Fernis twists the advice).

**D9.12 (2026-11-18): the lore drives names and flavor.** The Faerlan world-building shapes the names of
things (regions, dungeons, enemy skins, materials, items, actions, Demons) and gives items and actions a
**history flavor line, like the flavor text on a Magic card**:
```
OPEN VEIN                                    payoff · Rogue
  Applies 2 Bleed.                                   ← rules: mechanics only (11-15 rule stands)
  "Rub ash into the old cuts. The land should
   know someone saw them."  — Long Grief saying     ← flavor: italic, separate line, from the lore
```
- **The rules text stays mechanics-only.** Flavor is a separate field and line, never mixed into the
  description.
- Flavor shows on the card and in the loot reveal (D4.5), and is skipped in compact views.
- It's sourced from the lore's *Common knowledge* and *Legend/scholar* lenses (never the creator-only *Truth*
  lens), tagged with the record ID it came from so it stays in sync.
- Rarer items get more history: a Common has none, an Uncommon gets a line, and a Legendary or Mythic gets
  a short story beat.

- **D9.10** (Q9.10, "yes", read as the first option) **The contrast is intended:** a comic, absurd devil
  narrator in a melancholic, tragic world.
- **D9.11** (Q9.11) **The public name is *Demon Fighter*.** The project and code keep calling it
  DungeonFighter.

---

## Phase 10 — Build plan (draft, 2026-11-18)

### How we build

- **Strip down, then rebuild in ladder order** (G1). Each step ends with a game that's playable and tested,
  and the next step only adds.
- **Switch off before deleting.** Systems we're cutting are first turned off behind config or the unlock
  service (step 0.3). Once the rebuilt version of a system works, its old code is deleted in that step's
  cleanup. That's when the line count actually drops.
- **No balance work during the rebuild** (out of scope for this pass, and per the implement-before-testing
  rule). Each step is checked with unit tests and an MCP walk of the first hour (`/playtest`), not sims.
  Balance comes after the rebuild.
- **Old saves keep loading:** retired IDs map to their replacements (as Expose already does for
  Vulnerability). See the save migration table at the end.
- **One step = one branch and PR.** Steps that don't share files can run in parallel sessions.

### Step 0 — Foundation (no player-visible change)

| # | Work | Decisions | Touches |
|---|---|---|---|
| 0.1 | Land or park the in-flight work (the synergy pass and the 11-17 base modifiers) so the rebuild starts from a clean base | — | git |
| 0.2 | **Dev build flag:** the 11 developer states (Testing, DeveloperMenu, BattleStatistics, VariableEditor, TuningParameters, the five action editor states, ActionInteractionLab) and the clone become dev-only | D1.5, D7.9 | `GameState`, `MainMenuHandler`, settings handler, state validators |
| 0.3 | **System unlocks:** a per-character record of which systems have arrived, each unlocked by its first event (G1.5) and saved with the character. Screens, item cards and tabs ask it what to show | G1, G1.5 | new `SystemUnlocks` (Entity), `CharacterSaveService`, UI renderers |
| 0.4 | **Text pipeline:** one renderer turns any rule into one plain line; fix the leaking template braces ("+{8}%") | D6.5, Phase 1 bug | `CrossWires` descriptions, `SkillTreeRenderer`, tooltips |

### Step 1 — Rung 1: dice, one slot, the board

| Work | Decisions | Touches |
|---|---|---|
| **Board:** hero strip cards grow and brighten per link; the strip collapses on a break; the roll ruler shows bands, nudges and extra dice; odds on upcoming cards; the enemy strip on top with quieter escalation | D0.1, D0.6, D2.2, D2.7, D0.11, D9.6 | `DungeonRenderer.CombatBoard`, `ComboTension`, `HeroActionStripFeedback`, `CombatActionStripBuilder` |
| **Audio split:** your chain = clang / strike cues; the enemy's chain = thud / impact cues | D9.6 | `AudioCue`, `AudioCueDispatcher`, audio assets |
| **One AMP formula** (INT curve × gear AMP; drop INT potency, the animal-set bonus and the per-slot damage percent) | D2.3, D0.10 | `CharacterCombatCalculator.GetComboAmplifier`, `DamageCalculator`, `ActionUtilities.SlotMultiplier`, `AnimalSetController` |
| **Remove the INT 10 routing gate** | D2.6 | `Character.IncrementComboStep`, `ActionUtilities.UsesOrderedComboSequence`, `GameConstants` |
| **Crit amplifies effects** (statuses and other effects too); confirm a crit miss = miss + 2× recovery (`HasCriticalMissPenalty` exists) | D2.5 | `CombatEffectsSimplified.ApplyStatusEffects`, `ActionSpeedSystem` |
| Chain rules confirmed as already built (free action, +1 per link, no HIT mid-chain, no loop, g = 0.25) | D2.1, D2.8–D2.12 | config only |
| **Simple / detailed log** | D0.5 | `BattleNarrativeDisplay`, log settings |
| **First 60 seconds:** keep the Training Ground offer, cut the path intro, weapon choice as 4 class cards, no random extra armor, straight into the first dungeon | D1.1 | `PreWeaponPathIntro`, weapon selection renderer, starting gear |
| **Level 1 fights about 2 swings** (a data knob, not a solve) | D0.4 | level 1 enemy HP data |

### Step 2 — Items, the second slot, the dungeon's end

| Work | Decisions | Touches |
|---|---|---|
| **Commons are stats and a material only** (no base modifier, no action); starter gear keeps its action | D0.7, D5.6 | `ItemRoller`, `BaseModifierRoller`, `LootGenerator`, `StartingGear.json` |
| **The item card reading order:** comparison first, one line per attribute, next set step only, details behind a toggle | D4 proposal 3 (confirm in review) | item tooltip builders, `ItemComparisonFormatter` |
| **No gear changes mid-dungeon**; the strip locks inside a dungeon | D5.5, D5.8 | `InventoryMenuHandler`, dungeon state guards |
| **Dungeon end:** summary, then the **staged loot reveal**, then Equip / Keep / Salvage | D4.5, D9.2 | `DungeonCompletionRenderer`, new reveal sequence |
| Charms off | D4.7 | equipment slots, `Charms.json` |

### Step 3 — Action interactions, roles, Fernis

| Work | Decisions | Touches |
|---|---|---|
| **Action roles** (Setup / Sequence / Payoff), one-line cards, class hits apply the class status; arrows between linked cards in the strip editor | D5.1, D3.23 | `Actions.json` (new `role` column), strip editor, card renderer |
| **Fernis D Man:** a line bank in `GameData` keyed by event and moment, with per-class lines and tokens; one line per turn at most; a shoulder sprite; finds things truthfully, gives bad advice | D9.5, D9.7–D9.9 | new `GameData/Fernis.json`, a small presenter, board sprite |
| **Fight result lines** and **swing moments** (chain broken with cause, comeback, overkill, close call, threat) | D9.1, D9.2 | combat flow events, board, Fernis |

### Step 4 — Dungeons, rooms, bosses, regions

| Work | Decisions | Touches |
|---|---|---|
| **6 archetypes** with "plays like" / "punished by" in hero words; enemies become **skins** over archetype × kit × strip length | D8.1, D8.8 | `Enemies.json`, `EnemyActions.json`, `EnemyGenerationManager` |
| **Region → dungeons → rooms → enemies:** regions own rosters, enemies tagged to room types, dungeons are room mixes | D8.8 | `Regions.json`, `Dungeons.json`, `Rooms.json`, `RoomProcessor` |
| **Dungeon = one lesson** by default, plus wander chaos | D8.3, D8.6 | `EnemyAffixes.json` identities |
| **Matchup cards** on dungeon select (lesson, enemy rows, material, ✓ / ✗ / ~ for your build, a Fernis line) | D0.8 | `DungeonSelectionRenderer`, a matchup evaluator |
| **Room twists from the first dungeon; 1 in 4 empty rooms has an encounter** (wanderer, shrine, trapped creature, merchant, built from travel events) | D8.7, D8.10, D8.11 | `RoomAffixes.json`, `TravelEvents.json`, `RoomProcessor` |
| **One visible modifier per enemy; prefixes reworked** to one affinity or status rule each | D8.2, D8.5, D8.9 | `EnemyPrefixes.json`, `EnemyAffixRuntime` |
| **Enemy strips short; Demons longest** | D8.4 | enemy kits, Demon config |
| **Region requirements** (level, attribute, …) | D1.4 | `Regions.json`, region travel |

### Step 5 — Statuses

| Work | Decisions | Touches |
|---|---|---|
| **The 7-status core** (Stagger, Guard, Bleed, Burn, Focus, Rattle, Expose); retired IDs load as their replacements; Stun becomes Stagger overflow | D3.13–D3.16 | `Actor`, `EffectHandlerRegistry`, `StackTurnEffectHandler`, `TempoStatuses`, data files |
| **The status panel shows statuses only;** stores, grants, rerolls and shields move to their owners; **the NOW card** lists everything attached to the turn | D3.7 | `StatusEffectDisplayLines`, board |
| **NEW cards** for a first-seen status or system | D9.3 | board, `SystemUnlocks` |
| Affinities as immune / weak | Phase 3 proposal | `StatusAffinities.json` |

### Step 6 — Synergies and the one rule engine

| Work | Decisions | Touches |
|---|---|---|
| **Merge the trigger paths** (action trigger bundles, material earn rules, CrossWires, Listener affixes, hub events) into one engine on **11 events** | D6.1, D6.5 | `TriggerEvents`, `HeroEventHub`, `CrossWires`, `ActionTriggerGate`, `MaterialSetController` |
| **Self-contained Uncommon modifiers**; RANK / TAGAS / SETCOUNT modifiers move to Epic; rules scoped by rarity | D4.2, D6.5 | `Weapons.json`, `Armor.json`, `Scripts/build-item-mods.py`, `BaseModifierRoller` |

### Step 7 — Loot depth

| Work | Decisions | Touches |
|---|---|---|
| Six rarities kept; what each one carries; **Mythic = one class-bending rule per class** | D4.1, D4.9 | `RarityTable.json`, `ItemRoller`, new Mythic rules |
| Trade-up stays as the salvage sink | D1.6 | inventory |

### Step 8 — Materials, sets, stores

| Work | Decisions | Touches |
|---|---|---|
| **16 materials** (12 class + Leather + Stone, Strange, Celestial); Wood, Cloth and Unknown become cosmetic | D4.3, D4.8 | `MaterialBuilds.json`, item catalogs |
| **A material creates its currency** (names kept); **consumers spend or scale**; every amount has a turn / fight / dungeon scope; banked until dungeon exit | D6.2, D6.7, D6.9, D6.10 | `Stores`, `MaterialSetController` |
| **The three materials of a class reward different triggers** | D6.3 | material data |
| **Verbs: CASH IN (fullest store) and DETONATE only**; attunement goes | D6.4, D6.6 | `Code/Combat/Verbs/`, `Actions.json` |

### Step 9 — Skills and classes

| Work | Decisions | Touches |
|---|---|---|
| **Cut filler nodes; most nodes one-time picks**; trees stay bigger than you can finish | D7.2, D7.5, D7.6 | `SkillTrees.json`, `SkillEffectRouter` |
| **Strip slots from class tiers *and* buyable Core slot nodes** (level + gear + skills) | D5.3, D7.8 | `ComboSequenceMaxHelper`, `CharacterProgression`, Core nodes |
| **Core opens first;** material branches open on 2 pieces; a prompt when a point arrives | Phase 7 proposal 5, G1.5 | skill tree screen, `SystemUnlocks` |
| **Wizard sequence vocabulary** (MODIFY / TRIGGER / FLOW); hybrid trees keep their attributes and remix their parents' statuses | D3.21, D3.22, D7.3 | Wizard and hybrid data |
| Class points from the weapon, shown in the level-up moment | D7.1 | `LevelUpManager`, level-up display |

### Step 10 — Late rungs

| Work | Decisions | Touches |
|---|---|---|
| **Fragile cards** (burn out for the fight; the strip closes up) | D5.4, D5.7 | strip, combo routing |
| **Jam / Burn out strip effects** (the old Disrupt) | D3.15 | enemy kits, strip |
| Re-add uncommon / rare statuses only when a rung needs them (Confusion, which builds can profit from) | D3.6, D3.13 | status system |
| Build-moment callouts | Phase 9 proposal 5 | board, Fernis |

### Step 11 — Content and lore pass

| Work | Decisions | Touches |
|---|---|---|
| **About 100 actions** across classes, hybrids, neutral, materials, enemies and environments | D5.2 | `Actions.json`, `EnemyActions.json`, `EnvironmentalActions.json` |
| **Lore names and flavor lines** (Common: none; more history with rarity), from the public lenses of the Faerlan artifact | D9.12 | all name tables, a new `flavor` field |
| Fernis's full line bank, per class | D9.9 | `Fernis.json` |

### Step 12 — Cleanup

Delete what's switched off: the old status fields and handlers, the 9 retired verbs, attunement, the extra AMP
paths, the hybrid tag counters (if unused), the dead skill nodes, the old item affix templates folded into
rules, and the tests that only covered them. Re-measure the line count (373k at the start).

### Save migration

| Old | Loads as |
|---|---|
| Harden, Fortify, Temp HP, Absorb | Guard |
| Acid, Pierce, Vulnerability, Armor Break | Expose |
| Brittle | Mark (when re-added), else dropped |
| Slow, Stun | Stagger |
| Roll Penalty | Rattle |
| Poison | Bleed |
| Silence, Stat Drain, Reflect (status), Regen (status), Disrupt | dropped |
| Wood / Cloth / Unknown items | the same item, material shown as cosmetic |
| A Common with a base modifier or action | keeps its stats; the modifier and action drop (starters exempt) |
| An equipped charm | moved to the bag (the slot is off) |
| Retired skill nodes | refunded as skill points |

### Defaults settled for the build (2026-11-18)
- **The item card layout** (Phase 4 proposal 3) is **accepted**: comparison first, one line per attribute,
  the next set step only, details behind a toggle.
- **Burn as the Wizard's status stays tentative.** Nothing needs it before step 5.
- **The example hybrid remixes, prefix rules and Mythic rules** may be used as placeholders, each marked
  `REVIEW` for the user.
- **Lore renames happen once, in step 11**, to avoid churn.

---

## Running ledgers

**Vocabulary budget** (words the player sees)

| Area | Today | Target | After decisions |
|---|---|---|---|
| Statuses | ~22 (+~20 non-status panel lines) | 10, panel shows statuses only | 7 core (one per class + Focus/Rattle/Expose), add as needed |
| Events | 41 tokens | ~10 | 11, one trigger engine |
| Stores | 18 | one per kept material (16) | currency names kept; material creates it, consumers spend or scale; turn / fight / dungeon scope |
| Verbs | 11 | 4–6 | 2 (CASH IN, DETONATE) + one spend per material |
| Concepts per item card | ~12 | 3 above the fold | — |
| Player screens | 19 (+11 dev) | ~10, dev screens behind a flag | — |
| Actions | 220 (+15 enemy kits, 30 environment) | ~100 across all sources | 3 roles: Setup, Sequence, Payoff |
| Skill nodes | 210 (4 × 40–45 + 6 hybrid × 7) | more nodes than points; no filler | hybrid trees and attributes stay |

**Cut list** (decided only): _(empty)_

**Parked questions:** _(empty)_
