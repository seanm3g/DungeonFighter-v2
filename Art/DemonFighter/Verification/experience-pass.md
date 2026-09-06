# Experience pass

Implemented in the Avalonia build:

- Played cards show resolved damage/healing, known block values and effect messages. Basic hits remain distinct from used combo actions. Result data follows presentation reveals and matching actor/action identity.
- The illustrated stage shows the scheduler's current readiness order, including hazards. This is a snapshot of scheduled readiness, not a prediction of future action choices.
- P pauses live combat at a safe presentation/turn boundary; P resumes. Page Up/Down use the existing speed controls. Pausing suppresses wall-clock ticker accumulation; headless simulation bypasses the wait. Already-resolved damage is not rolled back.
- Selected enemy actions announce strike/heavy strike, spell, healing or support during anticipation. Unselected future actions remain unknown.
- Equipment comparisons retain stat/action comparison and equip requirements, and add an appearance thumbnail plus weapon damage/cadence guidance. Previewing does not equip or modify the character. Head/feet/leg items still share the existing cloth/plate appearance.
- Encounter reports count applied HP loss and restoration through the health setter, including DoT, reflection and healing. These are HP totals, excluding overkill and shield absorption; they do not assign all enemy HP loss to the fighter. XP awards and completion XP progress are displayed alongside the existing loot flow.
- Checkpoints show rooms remaining and offer rest: recover 10% max HP in exchange for the next enemy's first attack. The cost persists through noncombat rooms, is consumed once at a hostile encounter and clears on dungeon exit. Existing continue/leave choices retain their reward rules.
- Build guidance recognizes item DoT procs and supported combo setup/protection/payoff mechanics. Poison/bleed are item-applied in the current rules; removed reflection action mechanics were not reintroduced. No action coefficients or existing item balance were changed.

## Verification

An isolated build avoids the pre-build target that kills running DF processes. Targeted suites cover health, scheduler, card state, native scene rendering, sequence builder/presenter, checkpoint routing, completion, inventory and dungeon runner.

The final equipped harness sample runs 120 combats at level 8: four weapon types against Wolf, Skeleton and Lava Golem, 10 each. It uses tier-2 plate (6 armor), direct-stat enemies with verified armor 10, HARDEN/WEAKEN setup actions, and critical-triggered bleed/poison weapon modifiers for dagger/wand. Installed combo names are asserted. Higher action tiers and three extra slots are enabled only in the sandbox; these are controlled equipped builds, not evidence that a player naturally acquires this exact loadout. Raw output: `PlaytestHarness/equipped-results.json` and `equipped-run.log`.

All 120 fights completed; wins saturated at 10/10 per matchup, so this sample supports integration, not a claim of balanced builds. Poison ticks appeared in the living Wolf encounters. An earlier one-action equipped smoke test was superseded after the harness was changed to use the game's actual item-based status rules. RNG is unseeded. Live sound, pacing, keyboard focus and enjoyment still require interactive review.

The previous combo-order probe is explicitly corrected in `playtest-notes.md`: its copied-list edits did not install the intended sequence. No balance decisions use those results.
