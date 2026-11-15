# Cadence timing: next-action / next-turn pass (2026-11-15)

Goal: make "next action / next N turns" bonuses varied and interesting, and give actions mechanics tied to strip slot number. Built as optional fields on the existing cadence bundle, not a new system.

## Semantics

- **TURN ×N**: the next N hero rolls, hit or miss. Unchanged.
- **ACTION ×N**: changed 2026-11-15. One copy now goes on each of the next N strip slots, through the per-slot pending queue. Each copy is redeemed when its slot lands a combo, and a miss leaves it waiting. It is clipped to the slots left in the current combo cycle, so a finisher grants nothing, as before. ×1 still uses the next-action bank. Before, ACTION ×N stacked N copies onto the one next combo action (ACTION BONUS's "next 3 actions +25%" was really +75% once). That burst is kept as `shape: charge`.
- Rows affected by the change: ACTION BONUS, PRELUDE, BARBARIAN RAGE. Their descriptions already read "next N actions", so they now match.

## `timing` fields

Set on a `cadenceBundlesJson` entry, which is the runtime source when present, and mirrored on the `actionAttackBonusesJson` group.

| Field | Cadence | Meaning |
|---|---|---|
| `slot` | ACTION | Start slot: `1`..`n` (clamped to the strip), `first`, `last`. Explicit placement may wrap into the next combo cycle |
| `delay` | ACTION / TURN | ACTION: skip that many slots (`1` = the action after next). TURN: wait that many rolls (delay rolls don't spend the duration) |
| `shape` | both | `ramp` ×1, ×2 … ×N; `decay` ×N … ×1; `charge` (ACTION) all N on the next action |
| `every` | TURN | Only every n-th roll of the duration pays out (ramp/decay count pulses) |
| `perSlot` | both | Values × this action's 1-based strip slot |
| `slotIf` | both | Grant only if this action's slot matches: `3`, `>=3`, `<=2`, `first`, `last`, `odd`, `even` |
| `untilMiss` | TURN | A miss ends the grant |

Rules (items / skills) gained `SLOT` (the swing's 1-based strip slot) and `SLOTS` (strip length): `WHILE IF SLOT>=3 -> DAMAGE 20%`.

Limitation: an action's bundles share one duration, the first bundle's (`ActionCadenceDurationResolver.SyncBonusGroupCountsFromDuration`). Status mechanics in a bundle (expose, weaken …) are not gated by `slotIf`.

## New actions (tier 3; retuned 2026-11-15, see below)

| Action | Class | Timing |
|---|---|---|
| GATHERING STORM | Barbarian | TURN ×3 ramp: +10/+20/+30% damage |
| LIT FUSE | Barbarian | TURN delay 2: +50% damage on the 3rd roll |
| HEADSMAN'S PROMISE | Barbarian | ACTION slot last: finisher +60% damage |
| BLOOD FRENZY (rare) | Ravager | TURN ×4 ramp until miss: +10..+40% damage |
| MARCHING DRUM | Warrior | TURN ×6 every 2: +3 COMBO on turns 2, 4, 6 |
| RELAY STRIKE | Warrior | ACTION ×2 delay 1: the 2 actions after next +20% speed |
| OPENING GAMBIT | Warrior | TURN ×3, slotIf first: +1 HIT, +1 COMBO |
| FLURRY STEP | Warrior | ACTION ×3 decay: +30/+20/+10% speed |
| PATIENT BLADE | Rogue | TURN ×2 delay 1: +2 CRIT, +20% damage |
| THIRD CUT | Rogue | ACTION slot 3: +60% damage |
| COUNTED PACES (rare) | Rogue | TURN ×1 perSlot: +15% damage per slot |
| EVEN ODDS | Hexblade | ACTION ×2, slotIf even: +40% damage |
| CRESCENDO | Wizard | ACTION ×1 perSlot: +25% AMP per slot |
| RUNE OF THE FIFTH (rare) | Wizard | ACTION slot 5 (or last): +50% AMP |
| RISING CHORD | Wizard | ACTION ×4 ramp: +5/+10/+15/+20% AMP |
| LATE BLOOM | Spellblade | TURN ×3, slotIf >=3: +1 COMBO, +15% damage |
| HOURGLASS | neutral | TURN ×3 delay 3: +1 CRIT, +15% damage |
| NEXT TIME | neutral | ACTION ×2 slot first: +35% damage on slots 1-2 |

## Pricing (2026-11-15)

Paired `balance-loop measure` (night-v45i, seeds 31201-31204): dungeon clear within ±1.5 pts in every band, so the spread and the new rows are neutral at the curve level. Then each row was priced with 5-slot SEQLAB (test action swapped into slot k of `basic, PUNCH, WILLFIRE, BODY BLOW, HEXBLADE CURSE`; Sword/Wand/Mace vs Wolf; enemy HP ×2 so fights run ~13 hero turns, like real ones; Wolf at ×1 dies in ~5 turns and hides delayed / late-slot effects). Target: +4..+12% faster kill at L33 in the intended slot, no worse than the slot-1 chassis penalty (about −9%) at L8. Overnight runs: `GameData/TuningRuns/overnight-timing-20261115-r2/`.

- Cut: LIT FUSE (+24 → +12 at L33), EVEN ODDS (+31 → +12.5; +1 hit → +40% damage), THIRD CUT (+1 hit → +60% damage, +13 → +6), RUNE OF THE FIFTH (+15 → +9).
- Raised: CRESCENDO (≈0 → +11 at slot 3), COUNTED PACES (−5 → +6), LATE BLOOM (−3 → +10, gate >=4 → >=3: slot 4 is rarely reached), BLOOD FRENZY (−1 → +11), PATIENT BLADE (−3.5 → +4.5), HOURGLASS (−2 → +4), MARCHING DRUM (+2 → +3 COMBO), NEXT TIME (+8 at L8, ≈0 at L33: an early-game card).
- CRIT alone is worth almost nothing in these sims (+10 CRIT for 2 turns ≈ +2%; KEEN EYE is −4 at L33). The crit rows now carry damage; crit as a payoff needs its own look.
- Not priced: RELAY STRIKE, FLURRY STEP (speed: SEQLAB counts hero turns, which speed doesn't change).

## Open

- The synergy / rules bot doesn't know `timing`, so it scores these as plain cadence grants.
