# Action Roster Pass — 135 → ~60

Draft, 2026-10-25. A proposal only: `GameData/Actions.json` is unchanged.

This sorts every action in `Actions.json` by the layer it teaches (L0–L7, simplest to most complex), and marks each one **Keep**, **Merge** (becomes a variant of another action) or **Cut**. Each class's ladder is marked in the **Class** column and summarised at the end.

## Result

| Decision | Rows | Player-facing concepts |
|---|---|---|
| Keep | 61 | 61 |
| Keep as family (L7 scaling: rows stay, shown as variants) | 24 | 7 |
| Merge into a variant of a kept action | 28 | 0 |
| Cut | 19 | 0 |
| Enemy-only (out of scope) | 3 | — |
| **Total** | **135** | **68** |

That's 61 single actions plus 7 families. The families are the 12 material actions and the 6 hybrid pairs.

## Rules applied

1. **A separate action is a separate decision.** If two actions differ only in magnitude, duration count or rarity, one becomes a variant (tier I/II/III) of the other.
2. **One layer adds one idea.** L0 strike → L1 next action → L2 longer scope → L3 other target → L4 condition → L5 roll ops → L6 strip ops → L7 build scaling.
3. **Dungeon scope belongs to classes.** Generic actions stop at fight scope. "+X for the rest of the dungeon" is reserved for each class's stat action (MIGHT, CUNNING, CONCENTRATE, DUNGEON ANALYSIS).
4. **Each class owns a quantity**, following the identities in `SkillTrees.json`:
   - **Barbarian** (STR, "impacts, momentum"): damage, enemy vulnerability, on-kill.
   - **Warrior** (AGI, "defense into tempo, clean sequences"): hit, combo, defense, weaken/expose.
   - **Rogue** (TEC, "failure, exact rolls, delayed payoffs"): miss/exact-roll payoffs, roll replacement, strip replace.
   - **Wizard** (INT, "reactions, echoes, probability rules"): amp, slow, retrigger, roll rewriting, strip randomness.

   Generic actions cover the core quantities everyone needs: damage, time, hit, crit and hits.
5. **One idea per action at common rarity.** Composites survive only as class capstones.
6. **Flat weapon modifiers fold into %.** `weaponDamageMod` and `weaponSpeedMod` are the same player-facing ideas as damage % and speed %. See open question 1.

**Refs** shows where else a name is used: `AT` ActionTables.json, `ST` SkillTrees.json, `BL` build-ladders.json, `HL` hybrid-ladders.json, `EN` Enemies.json, `MB` MaterialBuilds.json, `CAM` ClassActionManager.cs, `AL` ActionLoader.cs rename alias, `RT` SkillEffectRouter. A merge or cut of a referenced name needs a legacy alias plus an update to those files (see Migration).

---

## L0 — Strike chassis

*New idea: damage, speed, hits and block. The player tracks one number: damage over time.*

| Action | Decision | Into / note | Class | Refs |
|---|---|---|---|---|
| PUNCH | Keep | Unarmed fallback, 100% / 1.0 | — | GameConstants |
| STRIKE | Keep | Sword basic | Warrior | AT, BL, HL |
| SLASH | Keep | Sword aggressive | Warrior | AT, BL |
| SLAM | Keep | Mace basic | Barbarian | AT, EN, BL |
| POUND | Keep | Mace fast/defensive | Barbarian | AT, BL |
| STAB | Keep | Dagger basic, multi-hit chassis (3×40%) | Rogue | AT, BL, HL |
| CUT | Keep | Dagger quick | Rogue | AT, BL, HL |
| MAGIC MISSILE | Keep | Wand basic; "+ AMP" lives in code (converter: not in data) | Wizard | AT (as MAGIC MISSLE), AL |
| BOLT | Keep | Wand quick | Wizard | AT, BL, HL |
| HAYMAKER | Keep | The slow, heavy chassis (300% / 2.5, −2 HIT next turn) | generic | BL |
| RAPID STRIKE | Merge | → STAB-style multi-hit variant (2×50%) | — | BL, HL |

Chassis problem: STRIKE, POUND and CUT share the same numbers (90% / 0.75) and differ only in block (180 / 180 / 0). SLAM and SLASH match each other the same way (120% / 1.0). Each weapon pair should differ along an axis the player can feel.

## L1 — Next action / next turn (self)

*New idea: this action sets up the next one. The player tracks two slots in a row.*

| Action | Decision | Into / note | Class | Refs |
|---|---|---|---|---|
| STRONGER | Keep | Next action +25% damage | generic | — |
| ACTION BONUS | Merge | → STRONGER II (×3 actions) | — | — |
| SHARPEN | Merge | → STRONGER (flat weapon damage folds into %) | — | — |
| RUSH | Keep | Next action +5% speed | generic | — |
| SPRINT | Merge | → RUSH (next turn ≡ next action in a single-hero strip) | — | BL |
| JUICED | Merge | → RUSH (weapon speed folds into %) | — | — |
| BLUR | Merge | → RUSH II (2 turns) | — | — |
| STEADY HAND | Keep | Next 2 turns +1 HIT | generic | AL |
| KEEN EYE | Keep | Next 3 turns +1 CRIT | generic | AL |
| CRITICAL ATTACK | Merge | → KEEN EYE I (1 turn) | — | **EN** |
| LUCKY CHARM | Merge | → KEEN EYE rare variant (+1 HIT too) | — | — |
| PHANTOM FOLLOW-UP | Keep | Next action +1 hit | generic | BL |
| EXTRA EFFORT | Keep | Next turn +5 STR (**ladder rung 1**) | **Barbarian** | — |
| REACTION TRAINING | Keep | Next turn +5 AGI (**ladder rung 1**) | **Warrior** | BL |
| TAKING AIM | Keep | Next turn +5 TECH (**ladder rung 1**) | **Rogue** | BL |
| READ BOOK | Keep | Next turn +5 INT (**ladder rung 1**) | **Wizard** | ST, CAM |
| MEASURED CUT | Keep | Next turn +1 COMBO, 90% strike; Warrior owns combo | **Warrior** | CAM, BL, HL |
| CONCENTRATION | Merge | → MEASURED CUT (same +1 COMBO, 1 turn) | — | BL |
| MEASURED ADVANCE | Merge | → MEASURED CUT II (2 turns) | — | BL |
| CHANNEL | Keep | Next action +50% AMP, −20% speed; Wizard owns amp | **Wizard** | CAM, BL |
| EMPOWERED | Merge | → CHANNEL I (+10% AMP, no speed cost) | — | BL |
| LEECHING CUT | Keep | Lifesteal on hit; the simplest sustain | **Rogue** | — |
| ENRAGE | Cut | +CRIT/−HIT composite; crit is generic and the trade isn't a Barbarian idea | — | AL |
| LOOSE GRIP | Cut | Weapon speed/damage trade; folds away with weapon modifiers | — | — |

## L2 — Longer scope (fight / dungeon)

*New idea: spend a turn now, get paid later. The player tracks how long the fight or dungeon lasts.*

| Action | Decision | Into / note | Class | Refs |
|---|---|---|---|---|
| MOMENTUM | Keep | +1% speed for the fight | generic | — |
| FIGHT BONUS | Keep | +1 HIT for the fight | generic | — |
| HONE | Cut | Flat weapon damage, fight scope; folds away | — | AL |
| WHETSTONE | Cut | Flat weapon damage, dungeon scope; dungeon scope is class-only | — | AL |
| MARATHON | Cut | +0.5% speed for the dungeon; too small to feel, and dungeon scope is class-only | — | — |
| WARCRY | Keep — **rework** | Becomes Barbarian's fight-scope stacking +damage %. Today it's WEAKEN + SALVAGE MISS, which belong to Warrior and Rogue | **Barbarian** | CAM, BL |
| MIGHT | Keep | +3 STR for the dungeon (**ladder rung 2**) | **Barbarian** | ST |
| MOMENTUM BASH | Merge | → MIGHT I (+1) | — | — |
| CUNNING | Keep | +3 AGI for the dungeon (**ladder rung 2**) | **Warrior** | ST, BL |
| DANCE | Merge | → CUNNING I (+1) | — | — |
| CONCENTRATE | Keep | +3 TECH for the dungeon (**ladder rung 2**) | **Rogue** | ST |
| FOCUS | Merge | → CONCENTRATE I (+1); also frees the name, which collides with the Focus status | — | — |
| DUNGEON ANALYSIS | Keep | +1 INT for the dungeon (**ladder rung 2**); add a +3 tier to match the others | **Wizard** | BL |
| FIGHTER | Cut | +1 STR for the fight; MIGHT dominates it, and generic fight buffs already teach fight scope | — | — |
| DEEP BREATHING | Cut | Same, AGI | — | — |
| QUIET MIND | Cut | Same, TECH | — | — |
| PRE-FIGHT PLANNING | Cut | Same, INT | — | — |

## L3 — Other target (enemy debuffs and self defense)

*New idea: change the enemy instead of yourself. Each status is the mirror of a self modifier: WEAKEN↔STRONGER, SLOW↔RUSH, EXPOSE↔PIERCE/armor, HARDEN↔VULNERABILITY.*

| Action | Decision | Into / note | Class | Refs |
|---|---|---|---|---|
| WEAKEN | Keep | Enemy deals less | generic | EN |
| SLOW | Keep | Enemy acts slower | generic | — |
| EXPOSE | Keep | Enemy armor down | generic | BL |
| HARDEN | Keep | You take less | generic | BL, HL |
| JAB | Keep | Disrupt: resets the enemy's combo (a StripOp on the enemy) | generic | **EN**, CAM, BL |
| BASH | Keep | Heavy strike + Vulnerable | **Barbarian** | BL |
| VULNERABILITY | Merge | → BASH (Barbarian owns Vulnerable) | — | BL |
| MIGHTY SWING | Keep | Strike that ignores armor | **Barbarian** | CAM, BL |
| PIERCE | Merge | → MIGHTY SWING (pierce is EXPOSE for one hit) | — | — |
| BLUDGEON | Keep | Mace disrupt; Barbarian mace drop | **Barbarian** | **AT**, ST, BL, HL |
| SKULL CRACK | Cut | Barbarian Expose; Barbarian already has 3 L3 strikes and Expose belongs to Warrior | — | — |
| CHALLENGE | Keep | Weaken + next turn +1 HIT | **Warrior** | CAM, BL |
| TAUNT | Keep | Expose + 2 turns +1 COMBO; Warrior tier-0 unlock | **Warrior** | **EN**, CAM, BL, HL |
| PARRY | Keep | Strike + Harden | **Warrior** | — |
| FROST BOLT | Keep | Wand strike + Slow | **Wizard** | — |
| CONFUSE | Cut | Random enemy targeting; only matters with several targets | — | BL |

Rogue has no L3 action. Its tree identity ("marks, status conversion") suggests a Mark action belongs here (see open question 4).

## L4 — Conditions (when)

*New idea: the effect only fires on an event. The player tracks how often that event happens.*

| Action | Decision | Into / note | Class | Refs |
|---|---|---|---|---|
| BERSERKER | Keep | Below 50% HP: this hit +50% | **Barbarian** | AL |
| COMBO END HEAL | Keep | Heal on combo end; reliable, teaches the condition | generic | — |
| AFTER MISS HEAL | Keep | Heal on a hit right after a miss; failure payoff | **Rogue** | BL |
| CRIT MISS HEAL | Merge | → AFTER MISS HEAL (same failure family, rarer trigger) | — | — |
| ROLL FIFTEEN | Keep | Heal on attack total exactly 15; exact-roll payoff | **Rogue** | BL |
| NATURAL SEVEN | Merge | → ROLL FIFTEEN variant (natural face instead of total) | — | — |
| CRIT HEAL | Cut | On crit fires about 5–10% of the time: a lottery at common tier | — | — |
| ROOM CLEAR HEAL | Cut | Fires once per dungeon; a post-fight heal works against the no-post-fight-heal design | — | — |
| OPENER | Cut as an action | It's a flag with no effect. Keep `opener` as a **condition** other actions use | — | parser |
| FINISHER | Cut as an action | Same: keep `finisher` as a condition | — | parser |

All 7 heal actions list `heal` both in a TURN cadence and in a trigger. Check whether runtime ever heals from the cadence copy before merging them.

## L5 — Roll ops (changing the odds)

*New idea: change the die's distribution itself.*

| Action | Decision | Into / note | Class | Refs |
|---|---|---|---|---|
| LUCK | Keep | Next action: advantage | generic | — |
| UNLUCK | Merge | → LUCK, **enemy-target variant** (enemy rolls with disadvantage). As written it only hurts you | — | BL |
| REWRITE FATE | Keep | Next roll becomes 18 | **Wizard** | CAM, BL |
| REPLACE ROLL | Merge | → REWRITE FATE high tier (becomes 20) | — | BL |
| LOADED DICE | Keep | Next natural roll becomes 12; sets up exact-roll payoffs | **Rogue** | CAM, BL |
| CRIT FACES | Keep | Natural 19s crit for the fight | Wizard lean | BL |
| EXPLODING DICE | Keep | Natural 20s explode | Wizard lean | BL |
| SALVAGE MISS | Keep | Next miss this fight becomes a hit | Rogue lean | — |

## L6 — Strip ops (changing the sequence)

*New idea: change the sequence itself. The player tracks the whole strip as a graph.*

| Action | Decision | Into / note | Class | Refs |
|---|---|---|---|---|
| STRIP SKIP | Keep | Skip the next slot | Wizard lean | BL |
| STRIP JUMP | Merge | → STRIP SKIP (skip N) | — | — |
| STRIP DISABLE | Merge | → STRIP SKIP (permanent variant; both feed CONCENTRATED CHAOS) | — | — |
| STRIP LOOP | Keep | Loop back to the opener | generic | BL |
| STRIP REPEAT | Keep | Repeat the previous slot | generic | — |
| RETRIGGER SLOT | Merge | → STRIP REPEAT (fixed-slot variant) | — | BL |
| STRIP RANDOM | Keep | Next slot is random | Wizard lean | BL |
| STRIP SHUFFLE | Merge | → STRIP RANDOM (whole-strip variant) | — | — |
| STRIP STOP | Cut | On miss, end the combo: pure downside with no current payoff | — | — |
| MISDIRECT | Keep | Replace the next slot (with STAB) | **Rogue** | CAM, BL |
| STRIP REPLACE | Merge | → MISDIRECT (replaces with STRIKE) | — | BL |
| ECHO SPELL | Keep | On combo, retrigger the next slot | **Wizard** | CAM, BL |
| RETRIGGER NEXT | Merge | → ECHO SPELL (identical trigger and mechanic) | — | BL |
| GAMBLERS BLESSING | Merge | → ECHO SPELL on-crit variant | — | BL |
| RETRIGGER FINISHER | Keep | On combo, the finisher fires now | **Warrior** | BL |
| RETRIGGER OPENER | Merge | → BARBARIAN RAGE (it's already RAGE's kill payoff) | — | BL |

## Capstones — class composites (rare)

| Action | Decision | Into / note | Class | Refs |
|---|---|---|---|---|
| BARBARIAN RAGE | Keep | Next 2 actions +25% damage, +10% speed; a kill retriggers the opener | **Barbarian** | CAM, BL |
| AVENGE | Keep — **implement** | Finisher that adds the Warrior tag to all actions. Only the finisher flag is in the data, and no code references it by name | **Warrior** | ST, BL |
| CONCENTRATED CHAOS | Keep — **implement** | Finisher scaling with skipped/disabled slots. Same status as AVENGE | **Wizard** | ST, BL |

Rogue has no capstone (see open question 4).

## L7 — Build scaling (families)

*New idea: value grows with gear, tags or stats across the run. The rows stay, and the player sees each family as one concept with variants.*

| Family | Rows | Decision | Note |
|---|---|---|---|
| Material signature | IRON CULL, STEEL CREST, BONE WRATH, DRUM CUT, GOLD ANCHOR, MITHRIL FLOW, GLASS NICK, UMBRA, HEADHUNT, WILLOW LASH, REFRACT, RESONATE | Keep as family (1 concept) | +5 damage per material counter. Refs: MB, RT, BL. Stored dps is stale on all 12 (110% vs computed 100%) |
| Warbrute | BODY BLOW, WARBRUTE SMASH | Keep as family | Stat-scaled vs tag-scaled variant. Logic is in `SkillEffectRouter.Hybrids.cs`; move it to `Modifier` + `ScaleFrom` |
| Ravager | PROWESS RUSH, RAVAGER FLURRY | Keep as family | Same |
| Ragecaller | WILLFIRE, RAGECALLER HOWL | Keep as family | Same |
| Duelist | FINESSE THRUST, DUELIST FEINT | Keep as family | Same |
| Spellblade | WITS ARC, SPELLBLADE WEAVE | Keep as family | Same |
| Hexblade | MIND HEX, HEXBLADE CURSE | Keep as family | Same |
| — | SET BONUS | Cut | Not implemented; the hybrid tag-scaled variants already cover per-tag scaling |
| — | SELF TARGET | Cut | A target field, not an action |
| — | FLIP ROOM SWITCH | Cut | Not implemented; belongs in `EnvironmentalActions.json` |

## Enemy-only (out of scope)

BAT SWARM, RAT SWARM and ANT SWARM are enemy multi-hit kits referenced from `Enemies.json`. They stay as they are.

---

## Class ladders

Each class climbs the same layers using its own quantity. *Italic* entries are generic actions the class leans on.

| Layer | Barbarian (STR) | Warrior (AGI) | Rogue (TEC) | Wizard (INT) |
|---|---|---|---|---|
| L0 | SLAM, POUND | STRIKE, SLASH | STAB, CUT | MAGIC MISSILE, BOLT |
| L1 | EXTRA EFFORT | REACTION TRAINING, MEASURED CUT | TAKING AIM, LEECHING CUT | READ BOOK, CHANNEL |
| L2 | WARCRY *(rework)*, MIGHT | CUNNING | CONCENTRATE | DUNGEON ANALYSIS *(+3 tier)* |
| L3 | BASH, MIGHTY SWING, BLUDGEON | CHALLENGE, TAUNT, PARRY | **gap: Mark** | FROST BOLT |
| L4 | BERSERKER | *FIGHT BONUS* | AFTER MISS HEAL, ROLL FIFTEEN | — |
| L5 | — | — | LOADED DICE, *SALVAGE MISS* | REWRITE FATE, *CRIT FACES*, *EXPLODING DICE* |
| L6 | — | RETRIGGER FINISHER | MISDIRECT | ECHO SPELL, *STRIP SKIP*, *STRIP RANDOM* |
| Cap | BARBARIAN RAGE | AVENGE *(implement)* | **gap** | CONCENTRATED CHAOS *(implement)* |

The ladder that started this pass, STRONGER → WARCRY → MIGHT, is Barbarian's L1 → L2: the same damage idea stretched from one hit to the fight to the dungeon.

## Migration notes (when this is applied)

- **Aliases.** Every merged or cut name that appears in saves, strips or other data should resolve to its target through the existing legacy-alias path in `ActionLoader` (the MAGIC MISSLE handling). This matters most for **CRITICAL ATTACK** and **JAB** (Enemies.json), **BLUDGEON** (ActionTables.json, SkillTrees.json) and all the `BL`/`HL` ladder entries.
- **Cut names that are rename-alias targets.** ENRAGE, HONE and WHETSTONE are the targets of old-name aliases (ANGRY ENRAGE, TWISTING STRIKE, WHIRLWIND), and `ActionCardConsistencyTests` asserts those names load. Update the test with the change.
- **Tuning ladders.** `GameData/TuningProfiles/build-ladders.json` and `hybrid-ladders.json` name many merged actions. Repoint them to the kept action and tier.
- **Stale dps.** Recompute `dps` from damage × hits / speed during the edit (24 rows are stale, including MAGIC MISSILE at "1%").
- **Converter check.** Run `ActionModelConverter` before and after (see `Code/Data/ActionModel/`). A merge is safe when the merged action's model equals the target's model apart from magnitude, scope count or condition.

## Open design questions

1. **Flat weapon modifiers.** Should `weaponDamageMod`/`weaponSpeedMod` stay as separate quantities? They scale differently on fast and slow weapons. This pass assumes no.
2. **EXPOSE vs VULNERABILITY.** Armor down and damage taken up read the same to a player. This pass keeps both because VULNERABILITY is HARDEN's mirror, and puts VULNERABILITY on Barbarian.
3. **Fight-scope class stat rung.** Cutting FIGHTER and its siblings leaves two rungs per class (turn burst, dungeon growth). Keep the middle rung only if scope pricing (turn ×1 / fight ×4 / dungeon ×12) can make it a real choice.
4. **Rogue gaps.** Rogue has no L3 action and no capstone. Its tree identity points to a Mark status and a "delayed payoff" capstone, such as cashing in accumulated misses or marks. POISON and MARK, proposed in the enemy section below, fill the L3 gap.
5. **Generic vs class-owned quantities.** At this size, combo belongs to Warrior and amp to Wizard. If other classes need access, add one generic tier rather than restoring the duplicates.

---

## Enemies on the shared roster

Draft addition, 2026-10-25. No JSON is changed.

Enemies already roll the same d20 combo strip as heroes (`ActionSelector.SelectEnemyActionBasedOnRoll`), and the combat board already draws the enemy's strip across the top. What's missing is the content. Today every enemy action is one of 15 `ENEMY …` templates in `EnemyActions.json` (229 flavour names map onto them through `aliases`) or one of 8 `Actions.json` names. 124 of the 128 enemies carry exactly two actions, so there's no pattern to read.

The proposal:

1. Enemies use the **same action rows** as heroes.
2. Every enemy strip is a **worked example of actions combining**: a setup and a payoff the player can read, beat, and later build themselves.

### Principle: enemies demonstrate, the player copies

Each enemy strip is a small combo with a reason it works. The player meets it from the losing side, learns the counter, and later loots the pieces to run the same combo. That gives three rules:

- **Every enemy strip has a named synergy.** Write it down, the same way templates already state their counter.
- **Every piece of that synergy is a shared action** the player can own, at the layer where the player unlocks it.
- **Depth controls which layer an enemy demonstrates.** A layer appears on enemies just before the player can unlock it.

### Enemy template → shared action

| Enemy template | Shared action | Layer | Enemy uses today | Note |
|---|---|---|---|---|
| ENEMY CRUSH | HAYMAKER | L0 | 19 aliases | Slow heavy blow |
| ENEMY FRENZY | STAB multi-hit chassis | L0 | 8 | Hits = 3 |
| ENEMY SUNDER | EXPOSE | L3 | 13 | |
| ENEMY HEX | WEAKEN (next-action variant) | L3 | 12 | Template weakens your *next action*, so it's the WEAKEN tier with ACTION scope |
| ENEMY ENSNARE | SLOW | L3 | 19 | |
| ENEMY SHELL | HARDEN | L3 | 12 | |
| ENEMY CURSE | VULNERABILITY (Barbarian's BASH status) | L3 | 12 | Enemy uses the bare status row |
| ENEMY PIERCE | MIGHTY SWING / pierce | L3 | 6 | |
| ENEMY DISRUPT | JAB | L3 | 28 | Already the same mechanic (`disrupt`) |
| ENEMY DRAIN | LEECHING CUT, enemy tier | L1 | 9 | Template heals 50% of damage; hero row 2%. One row, two tiers |
| ENEMY MEND | Heal on connect (L4 heal family) | L4 | 7 | |
| ENEMY VENOM | **POISON** (new) | L3 | 17 | No hero version today |
| ENEMY SCORCH | **BURN** (new) | L3 | 38 | Most-used template; also the top Knight template, which doesn't fit Knight's identity |
| ENEMY REND | **BLEED** (new) | L3 | 12 | |
| ENEMY CONCUSS | **CONCUSS** (new) | L4 | 17 | Stun on crit |
| BAT / RAT / ANT SWARM | STAB multi-hit chassis, hits = pack size | L0 | 3 | Already in `Actions.json` |

Enemies keep their flavour names: an alias like "Rusty Shiv" points at JAB instead of `ENEMY DISRUPT`. Two things replace the separate `EnemyActions.json` rows:

- **A `heroLoot` flag on the shared row** decides whether heroes can loot it.
- **Enemy-strength values become tiers** of the shared action.

One loose end: `Enemies.json` lists `Misdirect` (mixed case) on one enemy. It matches neither an alias nor the `MISDIRECT` row.

### Counter table

Each template's "Answer:" text, turned into actions the player owns:

| Enemy does | Player answers with | Layer of the answer |
|---|---|---|
| HAYMAKER (slow, telegraphed) | HARDEN / PARRY before it lands; JAB or **INTERRUPT** to break it; RUSH to act first | L1–L3, L6 |
| Multi-hit (FRENZY, swarms) | Armor, HARDEN (applies per hit) | L3 |
| HARDEN (SHELL) | MIGHTY SWING (pierce), EXPOSE | L3 |
| LEECHING CUT / heal (DRAIN, MEND) | Burst: STRONGER, HAYMAKER; WEAKEN | L0–L3 |
| EXPOSE / VULNERABILITY (SUNDER, CURSE) | **FORTIFY** | L3 |
| POISON / BURN / BLEED | Kill fast; **CLEANSE**; fewer, bigger actions against BLEED | L0, L2 |
| WEAKEN on your next action (HEX) | A cheap filler slot to absorb it; LUCK | L1, L5 |
| SLOW (ENSNARE) | RUSH, MOMENTUM | L1–L2 |
| JAB / disrupt | Short strips, STRIP LOOP, RETRIGGER FINISHER | L6 |
| CONCUSS (stun on crit) | LUCK, enemy-target variant (the UNLUCK rework), to push its rolls down | L5 |

### New actions

These are the four mirrors enemies already use plus the counters the table needs. They take the roster from 68 to **76** concepts.

| Action | Layer | Owner | Effect (sketch) | Why |
|---|---|---|---|---|
| POISON | L3 | **Rogue** | DoT on connect, stacking | Mirrors VENOM; a delayed payoff, and fills Rogue's L3 gap |
| MARK | L3 | **Rogue** | Marked enemy takes bonus damage from your next payoff action | Rogue tree identity ("marks"); the setup half of Rogue combos |
| BLEED | L3 | **Barbarian** | Enemy takes damage each time it acts | Mirrors REND; punishes fast enemies, fits momentum |
| CONCUSS | L4 | **Barbarian** | Crits stun | Mirrors ENEMY CONCUSS; impact payoff |
| BURN | L3 | **Wizard** | DoT on connect | Mirrors SCORCH; the base for "elemental reactions" (BURN + SLOW, …) |
| FORTIFY | L3 | **Warrior** | Your armor can't be reduced for N turns | Counter to SUNDER/CURSE; defense into tempo |
| CLEANSE | L2 | generic | Remove your debuffs and DoTs | Counter to DoTs and curses |
| INTERRUPT | L6 | generic | If the enemy's next slot is slow (speed ≥ ~1.3), cancel it | Answer to telegraphed heavy blows; gives the time axis weight every fight |

Updated class ladders: Rogue L3 gets POISON and MARK, Barbarian L3 adds BLEED and L4 adds CONCUSS, Wizard L3 adds BURN, and Warrior L3 adds FORTIFY.

### Enemy strips: named synergies by archetype

Each archetype's strip is a combo the player can read and later build. Slots are listed in order. "Today" shows the archetype's most-used templates, so each shape grows out of what the data already has.

| Archetype (count, tempo) | Today (top templates) | Strip shape | Synergy it teaches | Player builds it with |
|---|---|---|---|---|
| Brute (16, heavy/slow) | CRUSH, SCORCH, ENSNARE | WEAKEN → HAYMAKER | **Soften, then slam.** Weaken your answer so the big hit lands | STRONGER → HAYMAKER (Barbarian) |
| Knight (13, heavy) | SCORCH, SUNDER, SHELL | HARDEN → EXPOSE → HAYMAKER | **Turtle, crack, punish.** Defend while setting up, then cash in | PARRY → TAUNT → finisher (Warrior) |
| Assassin (26, fast) | ENSNARE, VENOM, REND | SLOW → POISON → JAB | **Outpace and bleed out.** Slow you, stack DoT, break your combo before your payoff | RUSH → POISON → MISDIRECT (Rogue) |
| Acrobat (15, fast) | SCORCH, DISRUPT, CRUSH, FRENZY | RUSH → STAB×3 → STAB×3 | **Tempo multi-hit.** Speed buff makes every hit come sooner; per-hit effects multiply | RUSH → PHANTOM FOLLOW-UP → STAB |
| Trickster (24, normal) | DISRUPT, SCORCH, CURSE | VULNERABILITY → JAB → CONCUSS | **Open, deny, punish.** Make you take more, stop your combo, crit-stun you while you're down | Hexblade/Rogue: MARK → JAB → payoff |
| Sage (19, normal) | SCORCH, CURSE, VENOM, DRAIN | VULNERABILITY → BURN → LEECHING CUT | **Amplify the DoT, then sustain on it.** DoT plus a damage-taken debuff, healing off the damage | Wizard: EXPOSE → BURN → ECHO SPELL |
| Berserker (10, normal) | HEX, SCORCH, REND, CONCUSS | STRONGER → CONCUSS → BERSERKER | **Wind up, crit, get scarier when hurt.** A condition (low HP) turns a normal strip into a threat | Barbarian: EXTRA EFFORT → CONCUSS → BERSERKER |
| Duelist (3, normal/heavy) | CONCUSS, FRENZY, SUNDER, MEND, PIERCE | EXPOSE → MIGHTY SWING → heal | **Strip armor twice over, then recover.** Expose plus pierce stack against armor builds | Warrior/Rogue: TAUNT → MIGHTY SWING |
| Warlord (2, heavy/normal) | HEX, REND, SCORCH, CRUSH | WARCRY → BLEED → HAYMAKER → retrigger opener | **Momentum loop.** Fight-scope buff, bleed, big hit, and the strip restarts | BARBARIAN RAGE capstone |

Today the Knight's top template is SCORCH, which doesn't fit "turtle, crack, punish". Move those aliases to HARDEN/EXPOSE when the strips are rebuilt.

### Combo patterns enemies should demonstrate

The strips above are instances of a small set of ways actions work together. Every pattern should appear on at least one enemy before the player can build it. A new enemy should be written as "pattern + flavour", not as two random actions.

| Pattern | Shape | Why it works | Enemy example | First seen |
|---|---|---|---|---|
| **Setup → payoff** | Buff or debuff, then a big action | The setup multiplies the payoff | Brute: WEAKEN → HAYMAKER | Early |
| **Tempo → volume** | Speed buff, then multi-hit | Faster swings plus per-hit effects | Acrobat: RUSH → STAB×3 | Early |
| **Defend while setting up** | Defensive action that also prepares | Covers the setup turn | Knight: HARDEN → EXPOSE → HAYMAKER | Mid |
| **Amplify the tick** | Damage-taken debuff, then DoT | Every tick is multiplied | Sage: VULNERABILITY → BURN | Mid |
| **Deny, then punish** | Disrupt or slow, then strike | Breaks your answer before it lands | Assassin: SLOW → POISON → JAB | Mid |
| **Double up on one weakness** | Two actions attacking the same stat | Stacks past what one counter can fix | Duelist: EXPOSE → MIGHTY SWING | Mid |
| **Conditional threat** | Normal strip that changes under a condition | Forces you to watch HP or crits, not slots | Berserker: STRONGER → CONCUSS → BERSERKER | Deep |
| **Sustain off damage** | Damage, then heal from it | Drags the fight; punishes slow builds | Sage: … → LEECHING CUT | Deep |
| **Bend the odds** | Roll op, then a crit-gated action | The roll op makes the gate reliable | Elite: LUCK → CONCUSS | Elite |
| **Loop** | Payoff restarts the strip | The strongest slot repeats | Warlord: … → retrigger opener | Boss |
| **Attack the strip** | Strip op aimed at your sequence | Your combo is the target | Boss: STRIP SHUFFLE, STRIP DISABLE | Boss |

The new-action and counter tables are built so every pattern has both sides on the roster: the player can **counter** it when they first meet it and **build** it a band later.

### Depth: which layer enemies demonstrate

Strip length and layer rise with depth. The player sees each layer on an enemy one band before they can unlock it. Bands are relative here; tie them to the dungeon level bands in tuning.

| Band | Strip slots | Layers enemies use | Patterns introduced |
|---|---|---|---|
| Early | 2 | L0 chassis + one L1 | Setup → payoff, tempo → volume |
| Mid | 3 | Adds L2/L3 statuses (WEAKEN, SLOW, POISON, HARDEN) | Defend while setting up, amplify the tick, deny then punish, double up |
| Deep | 3–4 | Adds L4 conditions (CONCUSS, BERSERKER, heal on connect) | Conditional threat, sustain off damage |
| Elite (Rare) | +1 slot | Adds L5 roll ops (LUCK on itself, enemy-target LUCK on you) | Bend the odds |
| Boss | 4–5 | Adds L6 strip ops aimed at **your** strip | Loop, attack the strip |

Rarity already exists on enemies (20 common, 72 uncommon, 36 Rare) and can drive the "+1 slot" step.

### Cut player actions that become enemy-only

Several actions cut from the hero roster make good enemy and boss moves. On an enemy, strip ops target the hero's strip (`StripOp.Target = Enemy` in the draft schema).

| Action | Hero decision | Enemy role |
|---|---|---|
| CONFUSE | Cut | Boss: your next action picks a random slot |
| UNLUCK | Merged into LUCK (enemy-target) | The same shared row, used by an enemy on you: disadvantage on your next roll |
| STRIP STOP | Cut | Boss: if you miss, your combo ends |
| STRIP SHUFFLE | Merged into STRIP RANDOM | Boss: shuffles your strip on its first hit |
| STRIP DISABLE | Merged into STRIP SKIP | Boss: locks one of your slots for the fight |
| ENRAGE | Cut | Berserker elites: +CRIT, −HIT for 3 turns, a readable glass-cannon phase |
| CRIT HEAL | Cut | Elite variant of MEND: heals on crit |

These are the roster's "boss vocabulary". Each teaches the counter-play the player's own L5/L6 actions provide (STRIP LOOP, RETRIGGER, LUCK).

### Migration notes (enemy side)

- Repoint the aliases in `EnemyActions.json` from templates to shared rows, and retire the 15 templates. The four mirrors (POISON, BURN, BLEED, CONCUSS) need rows first.
- Add `heroLoot` (or reuse the `enemy` tag) so enemy-strength tiers never drop as hero loot.
- Grow `actions` in `Enemies.json` from 2 to the band's slot count, in strip order.
- Shared rows move both sides at once. Enemies are solved against the median build, so run `Scripts/balance-scan.py` after any change to a shared action's numbers.

---

## Builds: every source touches every axis

Draft addition, 2026-10-25. No JSON is changed.

A build comes together when pieces from different places feed each other: an item that boosts a class action, a class node that scales with a stat, an action that bends the dice another piece cares about. This section widens the core system from **actions** to all three sources the player collects:

- **Actions** (the strip).
- **Items**: affixes (`StatBonuses.json`), prefixes (`Modifications.json`), materials (`MaterialBuilds.json`), item procs (`Triggers.json`) and charms (`Charms.json`).
- **Classes**: class points, `ClassActions.json` unlocks and `SkillTrees.json` nodes, including the hybrids.

Each source should be able to act on the five things a build stacks: **dice, attributes, actions, items and classes**. All four classes should get positive hooks in every one of those cells.

### One grammar for actions, items and classes

Items and class nodes already reuse the action mechanic ids. `Triggers.json` procs use the same `when` and `mechanics` tokens as `triggerBundlesJson`, and the trees' effects read as Modifiers. So the draft `ActionModel` schema generalises to one `Effect`, wherever it lives:

```
Effect   = Modifier | RollOp | StripOp | Status          (what changes; the draft schema)
Selector = this action | next action | slot N | actions with tag T | class C actions | weapon W | all
Scale    = per attribute | class points | equipped items with material/tag M | tags in strip | empty slots
           | enemy status stacks | mint counter (RAGE, BEAT, GRAZE, …) | level | natural roll
When     = the existing WHEN tokens (ONCONNECT, ONCRITICAL, WHILE_EQUIPPED, ONTAKEHIT, …)
Source   = Action | Item (affix, prefix, material, proc, charm) | Class (node, unlock, class points)
```

Two of these are new compared with the action-only schema:

- **Selector** is how an item or class node reaches *other* actions, for example "Mace actions +3%" or "class-tagged actions".
- **Scale** is how one piece grows with another, for example "+1 STR per Bone/Steel/Iron piece". `triggerBundles.scaleFrom` already exists but only 9 of 254 procs use it.

Build interactions are then edges in a graph. A build is a loop through it: items count as class tags → class nodes scale per class item → class actions scale with the class stat → the stat comes from items.

### Source × target matrix

What each source can do to each target, with an existing example from today's data.

| Source ↓ / Target → | Dice | Attributes | Actions | Items | Classes |
|---|---|---|---|---|---|
| **Actions** | Roll ops, hit/crit modifiers (LUCK, REWRITE FATE, LOADED DICE, STEADY HAND) | Stat ladders (EXTRA EFFORT → MIGHT) | Next-action modifiers, strip ops (STRONGER, ECHO SPELL, MISDIRECT) | Material actions scale per material counter (IRON CULL, DRUM CUT, …) | Class tags on actions; AVENGE adds the Warrior tag to every action |
| **Items** | HIT/CRIT/ACCURACY affixes; Long/Charged prefixes; procs on ONCRITICAL, ONEVEN/ONODD, `crit_face_min` | STR/AGI/TEC/INT affixes; `attributeRequirements` on prefixes gate items by attribute | Procs that buff the next action (WoundMomentum); MULTI-HIT/AMP/DAMAGE prefixes; items grant actions through weapon action pools | Material stacks (2/3/5 pieces), Leather and animal-taxon sets, charms amplifying gear | Materials count as class tags (`b-tribe`, `w-lineage`, `r-veil`, `z-circle`); the Mark of Class charm |
| **Classes** | `r-snake`, `z-odds`, `w-lvl2`, `b-stun` | `*-puberty`, `*-kin`, `b-gut` (INT→STR), `z-lens` (TEC on wands) | Action unlocks; slot and tag nodes (`w-metronome`, `z-memory`, `r-debt`) | `*-age1` (+% per class item), `*-loot`, `b-scar`/`w-polish` (item condition) | Hybrid trees (6), tag conversion |

Attributes and dice also act as *carriers*. STR sets damage, AGI speed, TEC the roll thresholds (`r-aim`: every 25 TEC lowers crit by 1), and INT sets AMP and strip slots. Item and class investment reaches the strip through them.

### Class coverage grid

For each class and target: **A** action, **I** item, **C** class node. ★ marks a proposed new piece (listed below). Everything else exists today.

| | Dice | Attributes | Actions | Items | Classes |
|---|---|---|---|---|---|
| **Barbarian** (STR, Mace) | A: CONCUSS★, FOLLOW THROUGH★ · I: Charged/Long crit prefixes, Steel low-roll proc★ · C: `b-stun` | A: EXTRA EFFORT, MIGHT · I: STR affixes · C: `b-puberty`, `b-kin`, `b-gut`, `b-empty`, `b-titan` | A: STRONGER, WARCRY, BARBARIAN RAGE · I: Heavy/Brutal prefixes, next-action damage procs · C: `b-knuckle`, `b-alloy` | A: IRON CULL, STEEL CREST, BONE WRATH · I: material stacks · C: `b-age1`, `b-scar`, `b-loot` | A: BARBARIAN RAGE class-tag bonus★ · I: Mark of Class · C: `b-tribe`; Warbrute, Ravager, Ragecaller |
| **Warrior** (AGI, Sword) | A: STEADY HAND, FIGHT BONUS, RIPOSTE★ · I: Swift/Short hit prefixes, Bronze roll proc★ · C: `w-lvl2`, `w-flow` | A: REACTION TRAINING, CUNNING · I: AGI affixes · C: `w-puberty`, `w-kin`, `w-march`, `w-hitagi`, `w-streak` | A: MEASURED CUT, RETRIGGER FINISHER, FORTIFY★ · I: Balanced/Precise combo prefixes · C: `w-metronome`, `w-cadence`, `w-wind`, `w-phalanx` | A: DRUM CUT, GOLD ANCHOR, MITHRIL FLOW · I: material stacks · C: `w-age1`, `w-polish`, `w-loot` | A: AVENGE · I: Mark of Class · C: `w-lineage`; Warbrute, Duelist, Spellblade |
| **Rogue** (TEC, Dagger) | A: LOADED DICE, SALVAGE MISS · I: crit affixes, Glass natural-7 proc★ · C: `r-snake`, `r-grease`, `r-aim`, `r-lvl2` | A: TAKING AIM, CONCENTRATE · I: TEC affixes (**only 3 today, the thinnest stat on items**) · C: `r-puberty`, `r-kin`, `r-synth` | A: MISDIRECT, MARK★, POISON★ · I: Poisonous/serrated prefixes · C: `r-debt`, `r-mastery`, `r-edge`, `r-toxin`, `r-hemo` | A: GLASS NICK, UMBRA, HEADHUNT · I: material stacks · C: `r-age1`, `r-loot` | A: CASH OUT★ · I: Mark of Class · C: `r-veil`; Ravager, Duelist, Hexblade |
| **Wizard** (INT, Wand) | A: REWRITE FATE, CRIT FACES, EXPLODING DICE · I: Crystal high-roll proc★ · C: `z-odds`, `z-lvl2`, `z-equation`, `z-soft` | A: READ BOOK, DUNGEON ANALYSIS · I: INT affixes · C: `z-puberty`, `z-kin`, `z-weave`, `z-archmage`, `z-slotInt`, `z-comboInt` | A: ECHO SPELL, CHANNEL, BURN★ · I: Keen/refined AMP prefixes, Flaming · C: `z-memory`, `z-chaos`, `z-sigil`, `z-shock` | A: WILLOW LASH, REFRACT, RESONATE · I: material stacks · C: `z-age1`, `z-loot` | A: CONCENTRATED CHAOS · I: Mark of Class · C: `z-circle`; Ragecaller, Spellblade, Hexblade |

What the grid shows:

- **Class trees already cover every cell** for all four classes. The trees are the most complete part of the system.
- **Actions are thin on dice for Barbarian and Warrior, and on classes for Barbarian and Rogue.**
- **Items reach classes only through materials and one generic charm.** No item touches dice in a class-specific way.

### New pieces to close the gaps

| Piece | Kind | Class | Cell | Effect (sketch) | Why it fits the identity |
|---|---|---|---|---|---|
| FOLLOW THROUGH | Action, L4 | Barbarian | Dice | A miss still deals 50% damage | Barbarian makes dice matter *less*; the name already sits in the legacy class-action list in `ClassActionManager` |
| RIPOSTE | Action, L4 | Warrior | Dice | After the enemy misses you: next action +2 HIT, +25% damage | "Defense into tempo" |
| CASH OUT | Action, capstone | Rogue | Actions, Classes | Consume MARK, Poison and Debt stacks for damage per stack; +X per Rogue-tagged action in the strip | Rogue's delayed-payoff capstone; fills Rogue's capstone gap |
| BARBARIAN RAGE class-tag bonus | Action tweak | Barbarian | Classes | +X% per Barbarian-tagged item equipped | Gives Barbarian an action → class edge like AVENGE's |
| Steel low-roll proc | Item proc (`Triggers.json`) | Barbarian | Dice | Connect on a natural 1–7: mint a TRIUMPH | Rewards bad rolls, in keeping with Barbarian's dice indifference |
| Bronze roll proc | Item proc | Warrior | Dice | Natural roll ≥ your last roll: mint a BEAT | Rhythm and streaks |
| Glass natural-7 proc | Item proc | Rogue | Dice | Natural 7: double GRAZE | Exact rolls |
| Crystal high-roll proc | Item proc | Wizard | Dice | Natural 15+: mint a CONTACT | Probability rules (pairs with `z-odds`) |
| Class weapon action pools | Item → action data | All four | Actions, Items, Classes | Fill each weapon's pool with its class's L0/L1/L3 actions | See findings: the pools are placeholders today |
| TEC affixes | Item affixes | Rogue | Attributes | Bring TEC up to the 6–9 affixes the other stats have | Evens out the attribute row on items |

With POISON, MARK, BURN, BLEED, CONCUSS and FORTIFY from the enemy section, the roster goes from 76 to **79** concepts (FOLLOW THROUGH, RIPOSTE, CASH OUT). The procs and affixes are item rows, not actions.

### Findings from the survey

- **Weapon action pools are placeholders.** In `ClassActions.json` the Mace, Sword, Dagger and Wand rules are 24 rows each of only JAB and TAUNT. Prophet, Godkiller, Worldbreaker and Eternal have only JAB and TAUNT too. TAUNT also appears at tiers 2–4 for Barbarian, Rogue and Wizard, which looks like filler.
- **Tree text and action data disagree.** `b-might` says MIGHT is +1 STR for the dungeon, but the row is +3. `z-readbook` says READ BOOK is +3 INT for the dungeon, but the row is "next turn +5 INT". Decide which is right before the ladders are retuned.
- **`ClassActions.json` and the roster ladders differ.** For example Barbarian unlocks TAUNT at tier 1 and MIGHTY SWING at tier 4, while MIGHT comes from the tree. The class ladders in this doc should become the source for `ClassActions.json`.
- **`scaleFrom` is barely used.** 9 of 254 item procs use it, one per value. It's the hook that makes items scale with classes and stats, so most class-specific item procs should use it.

### Keeping it complete

When the unified `Effect` model exists, add a coverage test next to `ActionModelConverterTests`. It should assert two things:

1. For each class and each of the five targets, at least one action, one item and one class node has a positive effect whose selector or scale references that class. A reference can be a class tag, a class material, a class weapon or the class stat.
2. No class has a target cell fed by only one source kind.

That turns "every class can build every way" from a design intention into a check that runs with the tests.
