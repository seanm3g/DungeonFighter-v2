# Action Bonus System

## Overview

The action bonus system applies temporary modifiers to future actions or attacks based on cadence (ACTION, ATTACK, or ABILITY). Bonuses are defined in the spreadsheet/JSON and displayed during combat in the action strip.

## Cadences

| Cadence | When Applied | When Consumed | Example |
|---------|--------------|---------------|---------|
| **ACTION** | When the next action in the combo sequence executes | When that action's roll is made (before hit/miss) | "Next Action 2: +3 COMBO, 2x DMG" |
| **ATTACK** | On the next roll | On every roll; stat bonuses apply only on hit | "Next 3 Attacks: +1 HIT" |
| **ABILITY** | On hit | When consumed on hit | "STR +1 (Enemy)" |

### ACTION Cadence (Slot-Based)

- Bonuses attach to the **next action slot** in the combo sequence when the current action succeeds (roll ≥ combo threshold).
- If the next action misses, the bonus stays and can stack (e.g., Action 1 succeeds twice → Action 2 gets 4x damage).
- Stored in `PendingActionBonusesBySlot` (key = combo slot index).
- Cleared on combo change (e.g., weapon swap) or combat end.
- **`rollBonus` / Hero accuracy column**: For ACTION cadence, `Advanced.RollBonus` is **not** added to the granting action’s attack roll. The same value is delivered via ACTION keyword bonuses (ACCURACY on the next slot) or, if the data has `rollBonus` without an ACTION bonus group, via `SetTempRollBonus` for the next roll execution. This avoids double-applying hero accuracy on both the buffing action and the next action.

### ATTACK Cadence (Roll-Based)

- Bonuses apply to the **next attack roll**.
- Consumed on every roll; applied only on hit for stat bonuses (STR, AGI, TECH, INT).
- Roll modifiers (ACCURACY, HIT, COMBO, CRIT) apply to the roll immediately.
- Stat bonuses use `ConsumedAttackBonusesThisRoll` and are applied only on hit.

### ABILITY Cadence

- Bonuses consumed on successful ability use (hit).
- DurationType can scope bonuses (e.g., Enemy, Room, Dungeon).
- **Modifier bonuses (SpeedMod, DamageMod, MultiHitMod, AmpMod)**: When the character is in a combo and the source action succeeds (hit + combo), these attach to the **next action slot** in the combo, same as ACTION cadence. They are consumed when that slot executes (before roll). Example: Adrenal Surge (Ability cadence, SpeedMod 20) hits in slot 1 → slot 2 (e.g., Rage) gets +20% speed; Rage's panel shows the modified speed and the modifier applies when Rage executes. On miss, Ability-cadence modifier bonuses are **not** added (consumed only when the next ability succeeds).

## Duration ("For Next X")

Both ACTION and ATTACK support X = 1, 2, 3, etc. (e.g., "for next 3 attacks").

## Bonus Types

### Roll Modifiers

- **ACCURACY** – Bonus to base roll
- **HIT** – Lowers hit threshold (positive = easier to hit)
- **COMBO** – Lowers combo threshold (positive = easier combo)
- **CRIT** – Modifies critical hit threshold: **+5 = easier** (need 14 instead of 19), **-5 = harder**

### Threshold Cascading

Threshold hierarchy (highest to lowest): **CRIT > COMBO > HIT > MISS > CRIT MISS**.

When a higher threshold is modified to go lower, the next lower threshold cascades with it:

- **CRIT 19→12** → COMBO cascades to 12
- **COMBO 14→4** → HIT cascades to 3

When two thresholds share the same value, the higher category is triggered (e.g. roll 12 with CRIT=12 and COMBO=12 → CRIT).

### Stat Bonuses

- **STR** – Strength
- **AGI** – Agility
- **TECH** – Technique
- **INT** – Intelligence

### Modifier Bonuses

- **SPEED_MOD** – Speed modifier (percent)
- **DAMAGE_MOD** – Damage modifier (percent or multiplier)
- **MULTIHIT_MOD** – Multi-hit modifier
- **AMP_MOD** – Amplifier modifier (percent)

## Spreadsheet Columns

All bonus data comes from spreadsheet columns:

- **Cadence** – ACTION, ATTACK, ABILITY
- **Duration** – X in "for next X"
- **HeroAccuracy, HeroHit, HeroCombo, HeroCrit** – Roll thresholds
- **HeroSTR, HeroAGI, HeroTECH, HeroINT** – Stat bonuses
- **SpeedMod, DamageMod, MultiHitMod, AmpMod** – Modifier bonuses

## Combat UI

Active modifiers are shown at the bottom of the action strip during combat:

- "Next Action 2: +3 COMBO, 2x DMG"
- "Next 3 Attacks: +1 HIT"
- "STR +1 (Enemy)"

Per-slot panels (damage, speed) use `GetPendingActionBonusesForSlot` so that slot-based bonuses (ACTION cadence or Ability-cadence modifiers in combo) show the modified values before the action executes. Built by `CombatActionStripBuilder.BuildPanelData` and `BuildActiveModifierLines(Character)`; rendered in `DungeonRenderer.RenderActionInfoStrip`.

## Key Files

- `Code/Data/ActionAttackBonus.cs` – Data model
- `Code/Data/ActionAttackKeywordProcessor.cs` – Cadence parsing and modifier bonuses
- `Code/Entity/CharacterEffectsState.cs` – `PendingActionBonusesBySlot`, `AttackBonuses`, consumed-attack tracking
- `Code/Actions/Execution/ActionExecutionFlow.cs` – Per-slot ACTION and ATTACK bonus logic
- `Code/UI/CombatActionStripBuilder.cs` – `BuildActiveModifierLines`
- `Code/UI/Avalonia/Renderers/DungeonRenderer.RoomAndCombat.cs` – Modifier strip rendering

## Clearing

- **Combat end** – `ClearAllTempEffects` → `ClearActionBonus` clears `PendingActionBonusesBySlot` and all bonus groups.
- **Combo change** – `EquipmentManager` calls `ClearPendingActionBonuses()` after `UpdateComboSequenceAfterGearChange` since slot indices may be invalid.
