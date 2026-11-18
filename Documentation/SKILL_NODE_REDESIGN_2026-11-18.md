# Dead skill nodes → rules (draft for review, 2026-11-18)

**Status: draft, nothing changed in `GameData/SkillTrees.json` yet.**

## Problem

40 of 210 skill nodes have `power` under 0.25. `Scripts/skill-node-tune.py` targets about 0.8% total power per point
for 5-rank nodes, so these nodes do almost nothing per point:

- First Cut shows "+0.16% damage per Rogue piece".
- Martial Kinship is +0.057 AGI per piece.
- Perfect Aim is −0.081 crit threshold per 25 TEC.

Integer effects round to 0 in play.

## Approach

The approach follows the base-modifier rule (`BASE_MODIFIERS_2026-11-17.md`): a node changes a system — tags,
statuses, stores, combo rules — instead of adding a stat trickle.

- Only existing verbs and events are used ([[minimize-new-mechanics]]).
- Every redesigned node becomes **rank 1, power 1.0**, so the number on the card is the number in play.
- Every rule below was run through `CrossWireRule.Parse` and parses. Behaviour (when each event fires, which way CRIT
  moves) gets checked when this is implemented.

Three outcomes:

- **Rule**: replaced by the rule shown (30 nodes).
- **Fix value**: the node already changes a system; only its number is raised (2 nodes).
- **Keep**: the node scales with a stat or store and is live at the levels where it unlocks (8 nodes).

## Shared patterns (one per class, same shape)

| Pattern | Barbarian | Warrior | Rogue | Wizard |
|---|---|---|---|---|
| **First \<Age\>** (Core t2): your class weapon's actions count as class actions. This feeds the t7 Cadence node (+1 Combo per 2 class tags) from the start, and every class-tag rule. | First Steel Age `WHILE IF WEAPON mace -> TAGAS barbarian` | First Formation `WHILE IF WEAPON sword -> TAGAS warrior` | First Cut `WHILE IF WEAPON dagger -> TAGAS rogue` | First Glyph `WHILE IF WEAPON wand -> TAGAS wizard` |
| **\<Quality\> Prefer** (Core t10): matching-quality pieces feed the class signature status | Scrap Prefer: every 4th connect applies 1 Stagger per Battle Scarred / New / Masterwork piece `ONCONNECT EVERY 4 -> APPLY stagger 1 PER QUALITY Battle_Scarred\|New\|Masterwork` | Parade Dress: every 3rd combo grants 1 Fortify per Worn / Like New / Heirloom piece `ONCOMBO EVERY 3 -> ADD fortify 1 PER QUALITY Worn\|Like_New\|Heirloom` | Fragile Prefer: every 3rd connect applies 1 Bleed per Broken / Preowned / Perfect piece `ONCONNECT EVERY 3 -> APPLY bleed 1 PER QUALITY Broken\|Preowned\|Perfect` | Pure Prefer: every 3rd combo applies 1 Burn per Second Hand / Cosmic piece `ONCOMBO EVERY 3 -> APPLY burn 1 PER QUALITY Second_Hand\|Cosmic` |
| **\<Class\> Kinship** (Rite t13): class pieces pre-load your first store at the first hit of each fight | Tribal Kinship `ONFIRSTHIT -> MINT RAGE 1 PER WORN barbarian` | Martial Kinship `ONFIRSTHIT -> MINT BEAT 1 PER WORN warrior` | Shadow Kinship `ONFIRSTHIT -> MINT GRAZE 1 PER WORN rogue` | Arcane Kinship `ONFIRSTHIT -> MINT CHAIN 1 PER WORN wizard` |

## Per class

### Barbarian (take pain, make momentum; Stagger, RAGE)

| Node | Now | Draft | Outcome |
|---|---|---|---|
| Empty Fury (Core t8, r5) | +0.21 STR per extra filled slot | A combo that ends at step 3+ mints 1 RAGE per extra slot. `ONCOMBOEND IF STEP>=3 -> MINT RAGE 1 PER EXTRASLOTS` | Rule |
| Blood Price (Bone t14, r5; either-or with Battle Trance) | Mace connects heal 0.1% max HP per rank | Mace crits heal 5% max HP and mint 1 RAGE. `ONCRITICAL IF WEAPON mace -> HEAL 5; ONCRITICAL IF WEAPON mace -> MINT RAGE 1` | Rule |
| Wrath Unbound (Bone t17, r3) | Spending RAGE heals 0.11% per RAGE per rank | Spending RAGE: your next action +3% damage per RAGE spent (a full cash of 10 = +30%). `ONCASH:RAGE -> NEXT DAMAGE 3% PER SPENT`, r1 | Rule |
| Titan (Rite t18 capstone) | STR damage cap +8.6 | Misses against a Staggered enemy don't end your combo, and your crits apply 2 Stagger. `WHILE IF TARGET stagger -> EXEMPT COMBO; ONCRITICAL -> APPLY stagger 2` | Rule |
| Blood Synthesis (Rite t16, r5) | Every 20 STR: +0.086 per RAGE / CRISIS / TRIUMPH mint, per rank | Unchanged: at 200 STR that is +0.9 per mint per rank (+4.3 at r5) | Keep |

### Warrior (defense and clean sequences into tempo; Haste, Focus, BEAT)

| Node | Now | Draft | Outcome |
|---|---|---|---|
| Swift Strike (Core t8, r5) | +0.57 AGI on next action per rank | Every 3rd Sword connect Slows the enemy (feeds DRAG). `ONCONNECT EVERY 3 IF WEAPON sword -> APPLY slow 1` | Rule |
| Unbroken Stride (Core t8, r5) | +0.57 AGI per streak hit per rank | Every 4th hit in a row grants 1 Haste. `ONCONSECUTIVEATTACK EVERY 4 -> ADD haste 1` | Rule |
| Cadence Keeper (Bronze t15, r5) | +0.2 Focus per alternation per rank (rounds to 0) | Each BEAT (every 4th turn) grants 1 Focus. `ONBEAT -> ADD focus 1` | Rule |
| War Drum (Bronze t17, r3; either-or with Battle Hymn) | +0.28% speed per BEAT spent | Spending BEAT: your next Swift action +3% damage per BEAT spent (it waits for one). `ONCASH:BEAT -> NEXT DAMAGE 3% PER SPENT IFTAG swift` | Rule |
| Second Wind (Gold t16, r5; either-or with Hamstrung) | A clean combo end heals 0.43% per rank | Same custom effect at power 1, r1: heals 8% max HP and converts leftover Focus to Fortify | Fix value |
| Drill Synthesis (Rite t16, r5) | Every 20 AGI: +0.057 per BEAT / DRAG / STREAK mint, per rank | Unchanged (+0.6 per mint per rank at 200 AGI) | Keep |

### Rogue (delayed payoffs, marks, exact rolls; Bleed, Mark, GRAZE)

| Node | Now | Draft | Outcome |
|---|---|---|---|
| Steady Hand (Core t8, r5) | +0.64 TEC per streak hit per rank | Every 5th hit in a row Marks the target (its next hit taken crits). `ONCONSECUTIVEATTACK EVERY 5 -> APPLY mark 1` | Rule |
| Perfect Aim (Rite t18 capstone) | −0.081 crit threshold per 25 TEC | Natural 19s crit; at 100+ TEC, 18s too. `WHILE -> CRIT 1; WHILE IF TEC>=100 -> CRIT 1` | Rule |
| Edge Synthesis (Rite t16, r5) | Every 20 TEC: +0.13 per GRAZE / SWING / TROPHY mint, per rank | Unchanged (Rogues sit at 140-230 TEC by L18-33: +0.9 to +1.5 per mint per rank) | Keep |

### Wizard (reactions, echoes, probability; Burn, Focus, CONTACT)

| Node | Now | Draft | Outcome |
|---|---|---|---|
| Wand Focus (Core t8, r5) | +0.83 INT on next action per rank | Every 3rd Wand combo grants 1 Focus (feeds LENS, Margin Notes). `ONCOMBO EVERY 3 IF WEAPON wand -> ADD focus 1` | Rule |
| Slot Mind (Core t8, r2) | +0.83 INT per strip slot per rank | Combos from slot 3 on apply 1 Burn (later slots matter, per the INT design intent). `ONCOMBO IF SLOT>=3 -> APPLY burn 1` | Rule |
| Mana Bleed (Crystal t11, r5) | Wand connects heal 0.1% per rank | Wand crits heal 3% max HP and mint 1 CONTACT. `ONCRITICAL IF WEAPON wand -> HEAL 3; ONCRITICAL IF WEAPON wand -> MINT CONTACT 1` | Rule |
| Sigil Burn (Crystal t13, r5; either-or with Fracture Glyph) | +0.62% damage vs Burning per rank | Every 3rd connect on a Burning enemy applies 1 Expose. `ONCONNECT EVERY 3 IF TARGET burn -> APPLY expose 1` | Rule |
| Arcane Weave (Rite t14, r5; three-way Rite pick) | With a Wand: +0.05% AMP per 10 INT per rank | With a Wand, your Burn ticks 25% harder. `WHILE IF WEAPON wand -> POTENCY burn 25%` | Rule |
| Resonance (Crystal t17, r3) | RESONATE: +0.41% damage per CONTACT per rank | Power 0.5: +1% per CONTACT per rank (+30% at r3 with full CONTACT) | Fix value |
| Spell Memory (Willow t14, r5) | +2.5% AMP per tag repeat per rank, cashed on a tag change | Unchanged: stacks within a fight | Keep |
| Weave Synthesis (Rite t16, r5) | Every 20 INT: +0.19 per CHAIN / LENS / CONTACT mint, per rank | Unchanged (Wizards sit at 100-200 INT: +1 to +1.9 per mint per rank) | Keep |

### Hybrids

The six hybrid `act-attr` actions (Body Blow, Prowess Rush, Willfire, Finesse Thrust, Wits Arc, Mind Hex) read
+1% per 10 of the hybrid attribute per rank. A hybrid attribute is two stats added together, so it is about 100-400
mid-game, which makes them +10-40% per rank. **Keep**; they only look dead at low level.

## Cashing pays damage (user, 2026-11-18)

Actions that cash a material store convert it into damage, not a stat gain. Since `slotrise-v46a` the patch chain set
`combat.materialConvertPaysDamage: false`, so BONE WRATH / CASH IN / LIQUIDATE paid the class primary stat for the
fight. `materialConvertPrimaryStatPerUnit` also paid that stat *alongside* damage when damage was on. Patch
`cashdmg-v48a` turns damage on and zeroes the stat rates. The two cash nodes in this draft (Wrath Unbound, War Drum)
pay damage on the next action for the same reason.

Decided (user, 2026-11-18): existing ONCASH nodes that grant a hero status on a cash stay as they are: Mithril Flow
(Focus), Victor's Guard and Stormcaller's Pact (Harden), Arcane Drill (Haste), Drummer's Focus and Focused Chain
(Focus). The rule covers the cash-in action's own payout, not side effects that skill nodes hang on a cash.

## Not decided here

- **Balance.** Skill trees get stronger in some places (Titan, Perfect Aim, the Kinship pre-loads) and lose raw
  damage in others (First Cut, the Prefer nodes). This needs a scan after implementation, and the tuner must be told
  to leave rule nodes at power 1.0 (it would cut them again).
- **Bot.** The synergy bot prices skills via `RulesSynergy`; TAGAS and status rules need weights in `synergy-bot.json`
  or the bot will skip these nodes.
- **Freed points.** 5-rank nodes becoming rank 1 frees about 4 points per node. Either accept faster trees or slow
  skill point gain.
