# Keyword Rename: ACTION → ABILITY, ATTACK → ACTION

## End-user terminology

- **ABILITY**: The move/skill defined in **GameData/Actions.json** (what the Actions settings menu edits). When we say "for the next ability," we mean the next use of one of these moves.
- **For next action**: The outcome of the **next roll** — which could be an ability use, a hit, or a miss. Consumed when that roll happens (regardless of success).

## Summary

Game mechanics keywords were renamed for clarity:

- **ACTION** (old) → **ABILITY** (new): Bonuses that apply "for the next ability" and are **consumed only when the ability succeeds** (e.g. hit).
- **ATTACK** (old) → **ACTION** (new): Bonuses that apply "for the next action/attack roll" and are **consumed per roll attempt** (regardless of hit or miss).

## Mechanics

- **ABILITY keyword**: Bonuses are queued when an action with an ABILITY cadence is used. Roll/threshold bonuses (ACCURACY, HIT, COMBO, CRIT) apply to the next ability's roll and are consumed only when that ability succeeds (on hit). Stat bonuses (STR, AGI, TECH, INT) are applied when consumed on hit. Used for things like "For the Next Ability: +1 STR" or "+1 ACCURACY".
- **ACTION keyword**: Bonuses are queued when an action with an ACTION cadence is used. They are consumed and applied on the **next attack roll** (before hit/miss is resolved). Roll and threshold bonuses (ACCURACY, HIT, COMBO, CRIT) therefore apply to that next action's roll. Used for things like "For the Next Action: +1 HIT".
- **Cadence** defines when bonuses apply: ACTION = next roll (consumed immediately); ABILITY = next ability's roll (applied to roll, consumed on success).

## Data and code

- **Actions.json** `cadence` values: `"ACTION"` → `"ABILITY"`, `"ATTACK"` → `"ACTION"`, `"ACTIONS"` → `"ABILITIES"`, `"ATTACKS"` → `"ACTIONS"`.
- **Spreadsheet/CSV**: Parser accepts cadence values and maps them: `ACTION`/`ACTIONS` → slot-based (next action in combo); `ATTACK`/`ATTACKS` → roll-based (next roll); `ABILITY`/`ABILITIES` → consumed on hit.
- **CharacterEffects**: `AbilityBonuses` (ABILITY cadence); `AttackBonuses` (ATTACK cadence); `PendingActionBonusesBySlot` (ACTION cadence, slot-based). Methods: `GetAndConsumeAbilityBonuses(bool)`, `GetAndConsumeAttackBonuses()`, `AddPendingActionBonuses`, `ConsumePendingActionBonusesForSlot`.
- **Full system**: See [ACTION_BONUS_SYSTEM.md](../05-Systems/ACTION_BONUS_SYSTEM.md).

## Display text

- Plural display: "N ABILITIES" (was "N ACTIONS"), "N ACTIONS" (was "N ATTACKS").
- Flavor: "For the Next ABILITY" / "For the Next ACTION".
