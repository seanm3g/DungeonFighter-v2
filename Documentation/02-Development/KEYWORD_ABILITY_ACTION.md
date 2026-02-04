# Keyword Rename: ACTION → ABILITY, ATTACK → ACTION

## End-user terminology

- **ABILITY**: The move/skill defined in **GameData/Actions.json** (what the Actions settings menu edits). When we say "for the next ability," we mean the next use of one of these moves.
- **For next action**: The outcome of the **next roll** — which could be an ability use, a hit, or a miss. Consumed when that roll happens (regardless of success).

## Summary

Game mechanics keywords were renamed for clarity:

- **ACTION** (old) → **ABILITY** (new): Bonuses that apply "for the next ability" and are **consumed only when the ability succeeds** (e.g. hit).
- **ATTACK** (old) → **ACTION** (new): Bonuses that apply "for the next action/attack roll" and are **consumed per roll attempt** (regardless of hit or miss).

## Mechanics (unchanged behavior)

- **ABILITY keyword**: Bonuses are queued when an action with an ABILITY cadence is used. They are consumed and applied only when the character’s next ability use **succeeds** (e.g. a hit). Used for things like "For the Next Ability: +1 STR".
- **ACTION keyword**: Bonuses are queued when an action with an ACTION cadence is used. They are consumed and applied on the **next attack roll** (before hit/miss is resolved). Used for things like "For the Next Action: +1 HIT".

## Data and code

- **Actions.json** `cadence` values: `"ACTION"` → `"ABILITY"`, `"ATTACK"` → `"ACTION"`, `"ACTIONS"` → `"ABILITIES"`, `"ATTACKS"` → `"ACTIONS"`.
- **Spreadsheet/CSV**: Parser still accepts legacy cadence values and maps them: `ACTION`/`ACTIONS` → keyword ABILITY; `ATTACK`/`ATTACKS` → keyword ACTION.
- **CharacterEffects**: `ActionBonuses` (old, for "ACTION" keyword) → `AbilityBonuses` (for "ABILITY"); `AttackBonuses` (old, for "ATTACK") → `ActionBonuses` (for "ACTION"). Methods: `GetAndConsumeAbilityBonuses(bool)`, `GetAndConsumeActionBonuses()`.

## Display text

- Plural display: "N ABILITIES" (was "N ACTIONS"), "N ACTIONS" (was "N ATTACKS").
- Flavor: "For the Next ABILITY" / "For the Next ACTION".
