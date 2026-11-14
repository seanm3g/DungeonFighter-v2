# Action class sort (proposal, 2026-11-12, rev 2)

Goal: gear rolls an action from the hero's in-class pool (its own class, the three hybrids that include it, and the shared neutral actions) 95% of the time, and from another class's actions 5% of the time. Items themselves roll as before.

`*` = no class tag today (newly assigned). `NEW` = proposed new action (existing effect columns only, no new mechanics).

## Rollable actions: 138 (14 of them new)

| Category | Count | Already tagged | Reassigned | New |
|---|---|---|---|---|
| Neutral (every class) | 20 | - | 20 | 0 |
| Barbarian | 19 | 11 | 8 | 0 |
| Warrior | 18 | 8 | 7 | 3 |
| Rogue | 18 | 7 | 9 | 2 |
| Wizard | 17 | 8 | 9 | 0 |
| Warbrute (Barb+War) | 7 | 2 | 2 | 3 |
| Ravager (Barb+Rog) | 7 | 2 | 4 | 1 |
| Ragecaller (Barb+Wiz) | 8 | 2 | 4 | 2 |
| Duelist (War+Rog) | 8 | 2 | 3 | 3 |
| Spellblade (War+Wiz) | 8 | 2 | 6 | 0 |
| Hexblade (Rog+Wiz) | 8 | 2 | 6 | 0 |

Class-specific pool per hero (own class + its three hybrids): **41 for every class**. Plus the 20 neutrals = 61 in-class actions each; 77 sit in the 5% bucket.

### Neutral (20)
- Weapon tuning: SHARPEN, HONE, WHETSTONE, LOOSE GRIP, JUICED, BLUR
- Speed: SPRINT, RUSH
- Accuracy / crit / combo: STEADY HAND, FIGHT BONUS, KEEN EYE, CRITICAL ATTACK, CONCENTRATION, MEASURED ADVANCE
- Dice: LUCK, UNLUCK
- Basics: RAPID STRIKE, PHANTOM FOLLOW-UP, ROOM CLEAR HEAL, CASH IN

### Barbarian (19)
ACTION BONUS*, STRONGER*, BERSERKER*, HAYMAKER*, RETRIGGER OPENER*, CRIT MISS HEAL*, RECKONING*, DAZE*, EXTRA EFFORT, FIGHTER, MOMENTUM BASH, TAUNT, BASH, MIGHTY SWING, WARCRY, BARBARIAN RAGE, MIGHT, BLUDGEON, SKULL CRACK

### Warrior (18)
MOMENTUM*, MARATHON*, HARDEN*, RETRIGGER FINISHER*, COMBO END HEAL*, BARRIER*, BRACE*, REACTION TRAINING, DEEP BREATHING, DANCE, CHALLENGE, MEASURED CUT, CUNNING, AVENGE, PARRY, LUNGE NEW, HOLD THE LINE NEW, CADENCE STRIKE NEW

### Rogue (18)
LUCKY CHARM*, GAMBLERS BLESSING*, STRIP REPLACE*, CRIT FACES*, REPLACE ROLL*, ROLL FIFTEEN*, MARK*, PALM*, VENOM COAT*, TAKING AIM, QUIET MIND, FOCUS, MISDIRECT, LOADED DICE, CONCENTRATE, LEECHING CUT, LOW BLOW NEW, FAN OF KNIVES NEW

### Wizard (17)
EMPOWERED*, STRIP JUMP*, STRIP SKIP*, STRIP RANDOM*, STRIP SHUFFLE*, STRIP DISABLE*, RETRIGGER SLOT*, INDEX*, TRANSMUTE*, READ BOOK, PRE-FIGHT PLANNING, DUNGEON ANALYSIS, ECHO SPELL, REWRITE FATE, CHANNEL, CONCENTRATED CHAOS, FROST BOLT

### Warbrute (7)
SALVAGE MISS*, AFTER MISS HEAL*, BODY BLOW, WARBRUTE SMASH, SHOULDER CHECK NEW, IRON GUARD NEW, CLEAVE THROUGH NEW

### Ravager (7)
EXPOSE*, ENRAGE*, CRIT HEAL*, OPEN VEIN*, PROWESS RUSH, RAVAGER FLURRY, GUTTING STRIKE NEW

### Ragecaller (8)
DETONATE*, LIQUIDATE*, BLOOD OATH*, STOKE*, WILLFIRE, RAGECALLER HOWL, FLAME BRAND NEW, THUNDER CLAP NEW

### Duelist (8)
PIERCE*, JAB*, STRIP STOP*, FINESSE THRUST, DUELIST FEINT, FEINT AND THRUST NEW, RIPOSTE NEW, DISARM NEW

### Spellblade (8)
SLOW*, STRIP REPEAT*, STRIP LOOP*, RETRIGGER NEXT*, INVEST*, PRELUDE*, WITS ARC, SPELLBLADE WEAVE

### Hexblade (8)
WEAKEN*, CONFUSE*, EXPLODING DICE*, NATURAL SEVEN*, DISTILL*, CORRODE*, MIND HEX, HEXBLADE CURSE

## New actions (14)

All use columns existing rows already use (`harden`, `weaken`, `expose`, `pierce`, `disrupt`, `slow`, `heroHit` / `heroCrit` / `heroCombo`, `speedMod`, `damageMod`, `ampMod`, `numberOfHits`, `verb APPLY <status> n`). Tier 3, rarity COMMON, damage/speed in the range of the existing tier-3 class actions; numbers are starting points for the balance pass.

| Action | Class | Hits x dmg | Speed | Effect (column) | Description |
|---|---|---|---|---|---|
| LUNGE | Warrior | 1 x 90% | 0.60 | `speedMod` 10, ACTION | Close the gap fast. Next action: +10% speed. |
| HOLD THE LINE | Warrior | 1 x 40% | 0.80 | `harden` 2 (self) | Plant your feet: 2 Harden. |
| CADENCE STRIKE | Warrior | 1 x 80% | 0.90 | `heroCombo` 1, 2 turns | Strike on the beat. Next 2 turns: +1 COMBO. |
| LOW BLOW | Rogue | 2 x 40% | 0.80 | `weaken` 1 | Dirty cut below the guard: Weaken. |
| FAN OF KNIVES | Rogue | 4 x 25% | 1.00 | `numberOfHits` 4 | Four thrown blades, each its own hit. |
| SHOULDER CHECK | Warbrute | 1 x 110% | 1.10 | `disrupt` 1 | Barge into them: resets their combo. |
| IRON GUARD | Warbrute | 1 x 60% | 0.90 | `harden` 1 (self) + `damageMod` 25, ACTION | Brace, then hit back: Harden, next action +25% damage. |
| CLEAVE THROUGH | Warbrute | 1 x 160% | 1.50 | `pierce` 1 | Two-handed cut through plate: ignores armor. |
| GUTTING STRIKE | Ravager | 2 x 55% | 0.90 | verb `APPLY bleed 1` | Rip and tear: 1 Bleed. |
| FLAME BRAND | Ragecaller | 1 x 120% | 1.20 | verb `APPLY burn 2` | Furious, burning blow: 2 Burn. |
| THUNDER CLAP | Ragecaller | 1 x 70% | 1.00 | `expose` 1 + `ampMod` 25, ACTION | Crack the air: Expose, next action +25% AMP. |
| FEINT AND THRUST | Duelist | 2 x 45% | 0.80 | `heroHit` 1, TURN | Draw the guard, then thrust. Next turn: +1 HIT. |
| RIPOSTE | Duelist | 1 x 70% | 0.80 | `harden` 1 (self) + `heroCrit` 1, TURN | Turn the blow, answer it. Harden, next turn +1 CRIT. |
| DISARM | Duelist | 1 x 50% | 0.80 | `weaken` 1 + `slow` 1 | Twist the weapon from their grip: Weaken and Slow. |

## Not in the gear roll: 35 (unchanged)

| Group | Count | Actions |
|---|---|---|
| Material set converts (class by material) | 12 | Barbarian: IRON CULL, STEEL CREST, BONE WRATH · Warrior: DRUM CUT, GOLD ANCHOR, MITHRIL FLOW · Rogue: GLASS NICK, UMBRA, HEADHUNT · Wizard: WILLOW LASH, REFRACT, RESONATE |
| Material set converts (classless) | 6 | LANDSLIDE, REVEAL, QUIRK, STARFALL, KINDLE, UNRAVEL |
| Starter / basic weapon attacks | 9 | PUNCH, SLAM, WIND UP, STRIKE, SLASH, STAB, CUT, MAGIC MISSILE, BOLT |
| System stubs | 5 | OPENER, FINISHER, SELF TARGET, SET BONUS, FLIP ROOM SWITCH |
| Enemy-only | 3 | BAT SWARM, RAT SWARM, ANT SWARM |

Total: 159 rows today + 14 new = 173 in `GameData/Actions.json`.

## Applied (2026-11-12)
- `GameData/Actions.json`: 58 rows retagged (`class, <class>` or `hybrid, <name>, <classA>, <classB>`), 14 new rows, the 5 system stubs tagged `unique`.
- `GearActionPools.Pick`: in-class pool = neutral + actions carrying any of the hero's class tags (hybrids count for both classes), drawn `LootSystem.GearActionInClassShare` (0.95) of the time; otherwise another class's action. Starter attacks, `unique` rows and material set converts (`materialScale`) never roll on gear. Replaces `weaponGearActionClassShare` / `armorGearActionClassShare`.
- Hero classes (`LootContext.HeroClassTags`): top class-point path, plus the second when it has points; else the equipped weapon's class. With no hero, a weapon uses its own class and armor only treats neutral as in-class.
- ITEMSTATS check (no hero, 6000 drops): weapons 60-74% own class, 18-39% neutral, 2-8% other class; armor 95% neutral, 5% other class.
- Side effects to watch in the balance pass: more actions now carry class tags, so strip class-tag counts (ClassTags, charm class-tag damage multiplier, hybrid "per 10 tags" actions) rise.

## Before vs proposal
- Today: 34 single-class + 12 hybrid tagged, 78 rollable actions neutral. Weapons roll their class 70% (`WeaponGearActionClassShare`), else neutral; armor uses `ArmorGearActionClassShare` (default 0). Hybrids only count for a class through their class tag.
- Proposal: 20 neutral. 95% from the in-class pool (41 class-specific + 20 neutral, equal weight, so ~1 in 3 in-class rolls is neutral), 5% from the 77 actions of the other classes and hybrids. Same rule for weapons and armor; the hero's class decides, not the weapon type.
