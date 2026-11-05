# Design Process (fun-seeking)

The balance process (`BALANCE_PROCESS.md`, `/balance`) answers *can a player win, and how long does it take?* This
process answers three different questions:

1. **What is new?** Does each stretch of play hand the player a new decision, verb or discovery?
2. **What is learnable?** Does the game teach its pieces in layers, simple before complex?
3. **What is mine?** Do my items, skills and luck add up to a build that differs from the next player's?

Metrics for these already live in the Experience Harness (`EXPERIENCE_HARNESS_DESIGN.md`: choice, pacing and identity
rows in `balance-scan.py`). This doc is the loop that uses them, plus the design reads the harness can't make.

## The loop

1. **Read**: the current state, from data (kit ladder, cadence timeline, item anatomy), sim reads (harness rows) and
   playtest notes. Write the read down below with a date.
2. **Card**: turn one felt problem into a card: *symptom* (what the player feels), *cause* (what the data shows),
   *lever* (what to change), *probe* (how we'd know).
3. **Ideate**: several candidates per card, not one. Item and action ideas use the sticky card in
   `BUILD_DESIGN_PROCESS_v3.md`.
4. **Prototype**: behind a config flag or in a patch variant (`--patch`). Never in the active patch.
5. **Probe at matched power**: compare experience metrics with clear rate held level (re-solve with `bo-solve.py` or
   compare against a power-matched control). Otherwise anything that just makes the hero stronger reads as "more fun".
6. **Feel**: narrated fight logs (RUNLOG / FIGHTLOG) plus a human playtest. The designer decides.
7. **Hand off**: the winner goes through `/balance` (scan → focus → confirm → gate) before it ships.

Design changes are made first and balance fixes come after. Don't reject a design change because it moves power;
re-solve the power instead.

## Lenses

### A. Ladder: build up from the bottom

Each layer should be learnable on its own and should change how the next layer plays.

| Layer | Content | Teaches |
|---|---|---|
| 0. Class verb | the simplest kit that makes each class play differently | one sentence per class |
| 1. Core statuses | a small vocabulary (a handful of statuses) that hero actions apply and enemies use | "this does X over time / next hit / on threshold" |
| 2. Action nuance | opener, finisher, combo position, crit, threshold | "order matters" |
| 3. Item rules | an item's material × slot or type adds a small rule | "this piece changes how my kit plays" |
| 4. Sets and converts | material currency, feed and convert | "build around a currency" |
| 5. Build-defining | skill-tree masteries, rare item rules, hybrids, chance | "my build is unique" |

Audit questions: at what level does each layer first appear? Does layer N appear before N+1? Is anything taught only
by enemies?

### B. Cadence: decisions per level

A level-by-level timeline of new decisions (slot, action, skill tier, branch, material, item rule). Target from
2026-10-26: at most 1 dry level in a row (`maxDryLevels`). Count only *felt* changes (a new verb or decision), not
number growth.

### C. Item identity: what makes this item mine

What two items of the same slot and material differ by, and whether the difference is mechanical (changes a decision
or a rule) or numeric.

## Read 2026-11-05 (fastbot-v18)

### R1. Cadence: the strip doesn't grow from L5 to L21

- A strip slot comes from each class tier reached (`CharacterProgression.cs:410`). The tiers sit at 2, 20, 60 and 120
  class points (`classPresentation.tierThresholds`), at 1 point per level. A single-class hero gets slot 2 at L3,
  slot 3 at L21 and slot 4 at L61. The `comboSlotCapByLevel` anchors at L10, L15, L25 and later never bind.
- Tier-2 class actions also wait for 20 points (L21), and tier 3 for 60 (L61). Tier 4 (120 points) can never be reached.
- New decision types per band: 1–5 ~6, 6–10 ~3, 11–15 ~2 (the material branch at L11 is the big one), 16–20 ~1,
  21–30 ~3, 31–50 ~1, 51+ ~3 (sparse).
- After L15, most growth is numbers: rank-5 passives, item tiers 3–5 on the same templates, region revisits.
- Sim strips converge on one convert plus one trunk action per class. Action breadth falls from 1.3–1.7 at L8 to
  1.0–1.2 at L48 (balance-scan-fastbot-v18-20261105).
- Gap: sims spawn heroes at a fixed level, so no data shows re-decisions as a hero levels. That needs a smart-bot
  PLAYTODEATHBATCH run from L1 with `Milestones`.

### R2. Item identity: an item is numbers plus one proc plus 1–2 actions

- Within one slot and material, items differ mechanically only by one per-piece trigger and their gear actions. The
  base item name (200 weapons, 209 armor) carries no behaviour; tier, quality, rarity and stat suffixes are numbers.
- No uniques, no named items, and no material × item-type rules (no "+1 DEF on Bone helms").
- Hooks that exist: `IFGEARHASTAG:<tag>[:count>=N]` trigger filters (all worn gear, not "this item"), class-skill
  "+8% per class-material piece", animal taxon sets, charms that scale a layer.
- Dead or misleading data that could be repurposed:
  - `Modifications.json` was 23 copies of Sturdy (+25 HP) from 2026-10-28: the Settings → Item Modifiers save matched
    rows by `DiceResult` (0 on every row) and collapsed them into the last one. Fixed and restored 2026-11-05 from the
    sheet-matching version (22 adjectives incl. Flaming/Poisonous/serrated weapon DoT). fastbot-v13..v18 were solved
    with Sturdy only. The adjective layer is a natural home for item rules (C2) and item-applied statuses (C3).
  - `ModificationTables.json` theme/archetype bias never matches (`LootBonusApplier.cs:654-666`).
  - Animal suffix trigger text ("On take hit, apply Fortify") is shown but never applied (`Item.cs:65-78`).
  - Catalog `triggerName/equipEffects` on Weapons/Armor are ignored and wiped by loot (`ItemGenerator.cs:83-86`,
    `LootBonusApplier.cs:77`).
  - 106 authored item triggers (`ItemTriggerIdentityCatalog.cs`) reach only the lab tools, never loot.
  - Armor hit/combo/crit/attackSpeed columns are 0 on all 209 rows; modification effects `reroll`, `autoSuccess`,
    weapon DoT and `StatusEffects` are supported in code but used by no data row.

### R3. Ladder: statuses aren't taught by what the player presses

- None of the 132 hero actions apply poison, burn or bleed. About a dozen apply weaken, expose, vulnerability, slow,
  harden, pierce or confuse.
- Statuses come from triggers (`Triggers.json`), skill-tree nodes and room hazards (`EnvironmentalActions.json`
  uses almost every core status). The player meets statuses mostly as something done *to* them.
- `StatusAffinities.json` (17 rows: undead immune to poison/bleed, burn ×1.5, ...) is a good teaching hook that
  heroes have little way to use.

## Cards

### C1. Mid-game dry spell (R1)

- **Symptom:** "mapped out by L15".
- **Cause:** no new slot, no new class action and few new decisions from L7 to L21.
- **Levers:** (a) class tier thresholds closer together (e.g. 2/8/16/28/45) or slots per level band instead of per
  class tier; (b) tier-2 class actions earlier; (c) a new decision type introduced at L13–19 (item rules, first
  rare-rule item, a second branch).
- **Probe:** cadence timeline (dry levels ≤ 1 through L30), strip breadth at L18 and L33, then re-solve power.

### C2. Items are interchangeable within a material (R2)

- **Symptom:** materials are the only thing worth building around.
- **Cause:** an item's own identity is numbers; behaviour comes from the set and one random proc.
- **Levers:**
  - (a) material × slot rules: each material says what it does on a helm, chest, legs, feet and weapon (e.g. Bone
    helm: +1 RAGE when hit);
  - (b) weapon-type × material rules (Bone mace vs Bone dagger);
  - (c) rare item rules, a small pool of named effects that roll on Rare and better, drawn from the unused trigger
    catalog;
  - (d) skill nodes that read specific item rules, so items + skills + chance make the build.
- **Probe:** build distinctness (how many distinct item-rule sets heroes end up with), strip breadth, viable builds
  per class within 15% of the best.

### C3. Status vocabulary arrives sideways (R3)

- **Symptom:** statuses don't feel like the player's tools.
- **Cause:** hero actions don't apply the core statuses.
- **Lever:** pick a core vocabulary per class, give each class's early actions one status of it, and let item rules
  and sets build on that vocabulary.
- **Probe:** ladder audit (first level each status is player-applied), status share of damage per class.

## Tools to build

| Tool | Serves | Notes |
|---|---|---|
| Kit ladder audit | A, C3 | per class and level: owned actions, statuses the player can apply, layer first seen. Data-only, seconds |
| Cadence timeline | B, C1 | data timeline (seconds) plus smart-bot L1→30 PLAYTODEATHBATCH with `Milestones` for real re-decisions |
| Item identity report | C, C2 | per item: mechanical vs numeric parts; build distinctness across sim heroes |
| Experience rows | all | existing harness rows in `balance-scan.py` (choice, pacing, identity) |
