# Combo slot rise (2026-11-15)

A combo chain gets harder the longer it runs: each strip slot past the first needs a higher d20 roll.

## Rule

- Slot 1 rolls at the actor's combo threshold (base 14, lowered by gear, skills and potions).
- From `comboThresholdRiseStartSlot` on, the threshold rises by `comboThresholdRisePerSlot` every `comboThresholdRiseEvery` slots.
- The rise never pushes past 20, so a natural 20 always continues the chain.
- Applies to heroes and enemies. Threshold adjustments (`AdjustComboThreshold`, overrides) change the stored base, never the risen value.

Shipped defaults (`ComboSystemConfig`, overridable under `comboSystem` in a balance patch):

| Key | Default | Meaning |
|---|---|---|
| `comboThresholdRisePerSlot` | 1 | +1 per step, 0 = flat (old behavior) |
| `comboThresholdRiseEvery` | 1 | a step every slot |
| `comboThresholdRiseStartSlot` | 2 | slot 2 is the first risen slot |

Code: `ThresholdManager.GetComboThreshold` (adds `GetComboChainRise` for the slot about to roll, slot = `ComboStep + 1`). Every caller (strip advance, IsCombo, mid-chain miss band, dice UI, bots) reads that method.

## Why

With a flat threshold and chains that roll until a miss (`freeActionOnComboHit`), combo hits per turn grow ~10x from threshold 14 to 4 (0.54 → 5.7), so late fights collapse and every threshold step past ~6 feels the same. With +1 per slot from slot 2:

| Base threshold | 14 | 12 | 10 | 8 | 6 | 4 | 3 |
|---|---|---|---|---|---|---|---|
| Combo hits per turn | 0.49 | 0.72 | 1.0 | 1.4 | 2.0 | 2.9 | 3.5 |
| Longest chain every fight (8 turns) | 1 | 2 | 2 | 3 | 4 | 6 | 7 |

Each threshold step adds about one slot of reach. At threshold 4 (the code floor) 10-chains come about 1 in 18 fights at 8 turns.

## Fight length

The design was chosen at 8 turns per fight (a turn = one chain attempt). Balance tools count hero actions (every roll, free combo actions included), not turns: `heroTurnsAnchors` in `GameData/TuningProfiles/balance-targets.json` is still 11 actions at L10+. At threshold 14 a turn is about 1.5 actions, so 8 turns ≈ 12 actions. On 2026-11-15 the target was set to 10 hero actions per fight (`heroTurnsTarget` 10, L10 anchor 10; L1 stays 3). Re-solve the active patch once this lands, since late-game hero damage per turn roughly halves.
