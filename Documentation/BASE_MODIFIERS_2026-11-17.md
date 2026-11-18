# Base modifiers modify systems (2026-11-17)

User direction: an item's base modifier should change or enhance other systems (actions, tags, class skills, items),
not grant a "+n% damage when ..." bonus. Replaces the base-modifier shapes from the synergy pass
(`SYNERGY_PASS_2026-11-16.md` §2). **Unbalanced: no sims run.**

## Rule

No base modifier uses a stat verb (DAMAGE, SPEED, AMP, CRIT, HIT, COMBO, MULTIHIT, attributes, NEXT, BUFF, HEAL,
REFLECT). The one percent left is `POTENCY` (a DoT ticks harder), which strengthens a status rather than the hero.
`SynergyPassTests` fails if a catalog modifier breaks this.

## Ladder (`Scripts/build-item-mods.py --modifiers-only`, `MODIFIER_SHAPES`)

| Tier | Target | Shapes (example card text) |
|---|---|---|
| 1 | one action / the class | CLEAVE applies Stagger every 2nd connect · CLEAVE also counts as a bludgeon action · CLEAVE combos mint 1 RAGE · every 3rd class connect applies Bleed · Bleed potency |
| 2 | a tag / a status | bludgeon actions apply Expose every 2nd connect · bludgeon combos grant Harden every 2nd · applying Bleed applies Brittle every 2nd · Barbarian actions also count as status actions |
| 3 | skill tree / set / store | +1 rank to Tribal Kinship · counts twice toward its material set · tag connects mint the class store · spending RAGE applies Stagger |
| 4 | bends a rule | misses don't end your combo (while HP<50 / step ≥ 3 / target status) · a miss also counts as a crit (event alias) · a full store grants 2 of a resource · tag A actions also count as tag B · tagged crits apply 2 of a status |
| 5 | a pair that feeds itself | tag A counts as tag B + tag B applies a status · +1 rank to two skills · applying a status mints a store + spending it applies the status · counts twice + a skill rank |

## New rule verbs (`Code/Combat/CrossWires.cs`, passive only)

| Verb | Syntax | Effect |
|---|---|---|
| TAGAS | `WHILE [IF ACTION x \| TAG t \| CLASS c \| WEAPON w] -> TAGAS tag` | The swing also counts as `tag` for rule conditions (TAG / CLASS / PREVTAG / NEXTTAG), `PER TAGS`, cadence `ifTag` / `perTag` and class-tag counts (`CrossWires.HasTag`). Alias conditions see the action's own tags only (no chains). Skill nodes can use it too. |
| RANK | `WHILE -> RANK node_id n` | +n effective rank on a **learned** node: rule nodes scale by it, the router's `SkillRanks.GetRank / GetPower` and per-rank pack mods include it. Learned rank (prerequisites, skill screen, save) is unchanged. |
| SETCOUNT | `WHILE -> SETCOUNT n` | This piece counts n more toward its material set (`MaterialSetController.CountEquipped`), like the COUNTSDOUBLE affix. |

Existing verbs the ladder uses: APPLY, ADD, MINT, POTENCY, EXEMPT (now on items), COUNTAS (now on items), and the hub
WHENs `ONSTATUSAPPLY:x`, `ONCASH:store`, `ONSTOREFULL:store`. Rule-applied statuses do not raise ONSTATUSAPPLY, so
status chains cannot loop.

`CrossWires.View` caches tag aliases and rank bonuses per hero, rebuilt when worn items, their modifiers or learned
ranks change.

## Saves and fallbacks

- `MaterialTriggerMerge.RefreshCatalogModifier`: on load, a saved modifier whose rule shape (numbers ignored) differs from
  its base's catalog rule re-rolls from the catalog (values stable per item name + base).
- `BaseModifierRoller.Roll` (bases no longer in the catalog) builds APPLY / POTENCY rules only.

## Bot

`ItemValueModel.ModifierVerbValue`: RANK = `ImplicitRankShare` (0.06) of the weapon value per rank on a learned node;
SETCOUNT = `ImplicitSetCountShare` (0.08) when another worn piece shares the material; TAGAS = grant share × strip
actions re-tagged; EXEMPT / COUNTAS = one grant share. First guesses.

## Not done

- No balance sims; base modifiers lost all their direct damage, so expect lower clear rates until tuned.
- Not playtested in the UI.
- `HasTag` builds a signature over worn items and learned ranks per call; measure sim speed before a long balance run.
