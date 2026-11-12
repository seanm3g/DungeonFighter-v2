# Build audit: brainstorm builds vs the game (2026-11-11, branch v43-actions)

The 90 detailed builds in `BUILD_SPACE_EXHAUSTIVE.md` (40 single-mechanic M01–M40, 50 pairs P01–P50), checked
against `Actions.json`, `SkillTrees.json`, `Modifications.json`, `Conversions.json` and the code. The 49 one-line pair
ideas in Part 4 were not audited. Equivalents count as present (e.g. DRUM CUT = DOWNBEAT, DISTILL = ASSAY).

Grades: **FULL** every piece exists · **CORE** the central loop plays, supporting item/skill pieces missing ·
**PARTIAL** the mechanic exists, the defining hook doesn't · **NONE** the mechanic isn't in the game.

| | FULL | CORE | PARTIAL | NONE |
|---|---|---|---|---|
| M01–M20 | 0 | 19 | 1 | 0 |
| M21–M40 | 0 | 15 | 3 | 2 |
| P01–P25 | 2 | 16 | 7 | 0 |
| P26–P50 | 1 | 7 | 11 | 6 |
| **All 90** | **3** | **57** | **22** | **8** |

FULL: P09 Sore Loser, P17 Drumline (both via equivalents), P31 Living Alloy.

## What blocks builds, ranked by reach

1. **Item effects (~70 of 90 builds).** Every named unique item needs an effect type that doesn't roll today. See
   the item-effect section below: it is one system, not 40 items.
2. **No strip action applies Poison, Bleed, Burn or Stun** (~15 builds: Plague Doctor, Pyromancer, Concussion,
   Odd Venom, Toxic Ledger, Thermal Shock, Hemorrhage, Helpless Prey, ...). They come only from weapon prefixes,
   skills and cross-wires. Fortify, Reflect, Absorb and Silence exist as statuses with no action that applies them.
   The `APPLY <status> n` verb already exists (only MARK uses it), so this is content, not code.
3. **Skill rule families Threshold, Alias, Exemption have zero nodes** (~30 named skills: Cascade, Fail Forward,
   Harmonics, Fate Debt, Diminishing Returns, Debugger, ...). ONSTOREFULL already fires for Threshold.
4. **Missing systems** (each blocks 1–4 builds):
   - Retaliation: `Actor.ReflectStacks` / `ReflectPercentage` exist but nothing in combat reads them (M29, P29, P44, P24).
   - Enemy prefixes drive no mechanics (`Enemy.PrefixTags`), so "vs Undead / Giant" can't exist (M36, P38, P50).
   - Hero swarms: swarm actions are enemy-only (M37, P39, P40). COMMAND is dropped, so these stay NONE unless swarms come back.
   - Daze store with stun at 3 (M19, P22, P46).
   - Mark as a stacking store of damage (M24, P22, P47).
   - Status tick / expire events: ONSTATUSTICK, ONSTATUSEXPIRE (M16, M17, M18, M20); ONREROLL (M08); ONSTRIPOP (M13);
     "enemy missed" (M21, M25); hero HP threshold (M30).
   - Dungeon-scoped stores (stacks across a dungeon, not just fight-to-fight Persistence) (M31, P33).
   - Per-hit crit rolls (P04), element tags (M15, P15), hit margin as a value (M06), turn-number roll checks (P48).
5. **~100 named brainstorm actions missing**, but they collapse into a few templates: conditional damage
   (EXECUTION LINE, GUILLOTINE, PUNISH THE HELPLESS), resource-as-damage (SHIELD BASH armor, SHIELD SLAM temp HP,
   LAST STAND missing HP, CATACLYSM strip length), status appliers (above), tempo (QUICKSTEP, DOUBLE TIME, STEAL TURN),
   strip routing (GEAR SWAP, ENCORE, CATCH), beat (DOWNBEAT, SYNCOPATE).

Dropped by design and therefore weak: Swarmlord (COMMAND), Scrapper / Curator / Scrap Heap (item wear, SACRIFICE),
Echo Chamber / Hall of Mirrors (REVERB, partly), Heirloom vs Scrapper (Condition-band).

Data bugs found: BLUDGEON's skill text says "(stuns)" but the action only disrupts; FLURRY is enemy-only though
brainstorm builds treat it as a hero action; the brainstorm's "Brutal: Weaken on crit" and "Long: pierce" don't match
the real affixes (flat damage, crit).

## Item effects: the system they need

Today's affix kinds: stat suffixes, On-hit Poison/Burn/Bleed prefixes, Listening, Attuned, Charged-Up, Doubled,
Opening/Closing, Bloodbound, 8 cross-wire prefixes. Almost every missing item fits one rule shape, which is the
cross-wire grammar extended:

```
WHEN event[:arg] [EVERY n] [IF condition] -> EFFECT target thing n[%] [FOR scope]
```

plus two passive kinds that have no trigger.

**Triggers.** All existing events plus: per hit of a multihit, ONSTATUSTICK, ONSTATUSEXPIRE, ONREROLL, enemy missed,
HP band crossed, kill of a marked / prefixed target; counters ("every 3rd hit").

**Conditions.** Target has status X (or per stack of X) · own HP band · store ≥ n (exists) · amount spent ≥ n ·
combo step ≥ n / strip position · action length < L · roll parity / value · on the beat · enemy prefix ·
another worn piece has affix Z (pair bonus) · all worn materials distinct · count of statuses on self.

**Effects.**
- APPLY status to target (Weaken, Expose, Daze, Slow, Stun, Mark — not just DoTs)
- HEAL (flat or % of damage dealt = lifesteal)
- REFLECT % of damage taken
- ADD to self: stat or AMP or speed, for the next action / fight / dungeon (Jester's Bells, Ember Robe, Hourglass)
- crit or hit threshold delta (Bloodied Helm, Lens Monocle, Metronome Charm)
- MINT store (exists), GRANT reroll / roll bank per fight (Weighted Die, Fated)
- loot bias (Bounty Ledger)

**Passive modifiers (no trigger).**
- **Tilt**: +A / −B (Ponderous, Tower Plate, Twin Daggers, Headsman's Axe, Tome).
- **Tuning** of a store or status: cap +n, duration +n, stacks per apply +n, no decay while condition, conversion
  ratio, optionally per worn piece of material M (Vial Bandolier, Cinder Wand, Inspector's Lens, Capacitor Plate,
  Vault Belt, Alchemist's Gloves).
- **Counts-as**: piece counts as material M, event A counts as event B (Mixed Ingot, Seven-Sided Charm,
  Cracked Mirror, Scarred Gauntlets).
- **Grant**: a conversion row, +1 strip slot, +1 tag on every action (Envenomed Edge from an item, Score Robe, Lexicon).
- **Exemption**: misses don't end the combo, loops aren't repeats, opener costs 0 length (Plumb Line, Escapement).

Carry-between-fights for items (Vault Belt, Trophy Necklace) uses the Persistence machinery with an item source.

## Per-build results

### M01–M20
| Id | Name | Grade | Main gaps |
|---|---|---|---|
| M01 | Siegebreaker | CORE | CATACLYSM, HEFT; Tilt (Ponderous); Earthshaker overkill carry |
| M02 | Metronome | CORE | QUICKSTEP/DOUBLE TIME/STEAL TURN; "actions since enemy acted" counter; Winged Sandals |
| M03 | Thousand Cuts | CORE | hero FLURRY, RAKE, SPLINTER; Twin Daggers (Tilt); Rhythm Strikes |
| M04 | Crescendo Mage | CORE | AMP as spendable store; CODA; Score Robe (+slot) |
| M05 | Glass Edge | CORE | EXECUTION LINE, SHATTERPOINT; Lens Monocle (crit per piece); Cascade |
| M06 | Surgeon | CORE | hit-margin value; can't-miss STEADY; Plumb Line (miss → graze) |
| M07 | Numerologist | CORE | PARITY BLADE, SUM; Seven-Sided Charm, Odd Gloves / Even Boots |
| M08 | Loaded Hand | CORE | ONREROLL event; Weighted Die, Fated; Fate Debt |
| M09 | Lucky Fool | CORE | RECKLESS SWING; Jester's Bells, Cracked Mirror; Fail Forward |
| M10 | Endless Chain | CORE | HOLD THE LINE, LINK; Chain Shirt; Infinite Form |
| M11 | Ambusher | CORE | FROM THE SHADOWS; Assassin's Hood (double first-hit); Every Fight Is New |
| M12 | Executioner | CORE | GUILLOTINE, ENCORE; Headsman's Axe (Tilt); Last Word |
| M13 | Clockmaker | CORE | GEAR SWAP; ONSTRIPOP; Escapement; Program/Debugger |
| M14 | Echo Chamber | CORE | REVERB dropped; echoes carry statuses / fire ONCONNECT; Harmonics |
| M15 | Linguist | CORE | element tags; CONJUGATE; Lexicon; Etymology |
| M16 | Plague Doctor | CORE | no Poison action; ONSTATUSTICK; Plague Mask, Vial Bandolier |
| M17 | Hemomancer | CORE | OPEN VEIN, TOURNIQUET; ONSTATUSTICK; Butcher's Apron (+dmg per Bleed) |
| M18 | Pyromancer | CORE | no Burn action; Ember Robe, Cinder Wand |
| M19 | Concussion | PARTIAL | Daze store (the hook); no Stun action; Concussion Maul |
| M20 | Tide Anchor | CORE | MOLASSES, UNDERTOW; ONSTATUSEXPIRE; Lead Chain, Hourglass |

### M21–M40
| Id | Name | Grade | Main gaps |
|---|---|---|---|
| M21 | Hollow Out | CORE | SAP; "enemy missed" event; Leeching Grip |
| M22 | Breaker | CORE | SUNDER; Crowbar, Inspector's Lens; Weak Point |
| M23 | Lance | CORE | SKEWER; pierce affix; Through and Through |
| M24 | Bounty Hunter | CORE | Mark as damage store (DEEP MARK); Bounty Ledger, Hunter's Hood |
| M25 | Trickster | CORE | SILENCE / accuracy hex actions; Mirror Cloak; Chaos Theory |
| M26 | Fortress | CORE | SHIELD BASH, BRACE (Fortify); Tower Plate; Bulwark Strike |
| M27 | Leech | CORE | TRANSFUSION; Vampire Fang lifesteal affix; Overflow |
| M28 | Bastion | PARTIAL | SHIELD SLAM (spend temp HP); ABSORB; Capacitor Plate |
| M29 | Thornback | CORE | reflect system unused; RIPOSTE; Thorn Mail |
| M30 | Berserker | CORE | LAST STAND; Bloodied Helm (HP band); Deathwish |
| M31 | Collector | CORE | dungeon-scoped kill stacks; Trophy Necklace; Apex/Glutton |
| M32 | Purist | CORE | Mixed Ingot (counts-as material), Unmatched Ring |
| M33 | Scrapper / Curator | NONE | item wear and SACRIFICE dropped |
| M34 | Banker | CORE | HEDGE, LOAN; Vault Belt (cap/carry), Ledger (spend ≥ n) |
| M35 | Alchemist | CORE | CATALYST, REACTION; Alchemist's Gloves, Retort |
| M36 | Hunter of Kinds | PARTIAL | enemy prefixes have no mechanics; Silvered, Giantslayer |
| M37 | Swarmlord | NONE | no hero swarms; COMMAND dropped |
| M38 | Martyr | CORE | TITHE; Hair Shirt, Scarred Gauntlets, Martyr's Brand |
| M39 | Scholar | CORE | slot-count INDEX; Score Robe / Tome; Bookmark |
| M40 | Drummer | PARTIAL | ONBEAT invisible to action triggers (DOWNBEAT hook); Metronome Charm |

### P01–P25
| Id | Name | Grade | Main gaps |
|---|---|---|---|
| P01 | Pendulum | CORE | BACKSWING; speed↔damage bank item |
| P02 | Avalanche | PARTIAL | SHATTER BLOW (hit splits into hits) |
| P03 | Accelerando | PARTIAL | speed → AMP bridge; AMP stacks until enemy acts |
| P04 | Spray and Pray | PARTIAL | per-hit crit rolls; RICOCHET |
| P05 | Hall of Mirrors | CORE | REVERB dropped; echo counts as a slot |
| P06 | Perfect Form | CORE | margin → crit threshold item |
| P07 | Card Counter | CORE | NUDGE; Abacus running sum; Weighted Die |
| P08 | Rigged Loss | CORE | THROW THE MATCH; miss → banked roll |
| P09 | Sore Loser | FULL* | needs the right Listening helm to drop |
| P10 | Odd Venom | PARTIAL | ONODD → Poison not in content (grammar can say it) |
| P11 | Loaded Edge | CORE | SHATTERPOINT; +1 reroll per fight |
| P12 | Long Fuse | CORE | GUILLOTINE; +Combo per link |
| P13 | Second Ambush | CORE | first-hit amplifier; Every Fight Is New |
| P14 | Encore | CORE | jump to finisher on crit; Program |
| P15 | Elemental Sentence | CORE | element tags; Burn action |
| P16 | Epic Poem | CORE | +1 strip slot item |
| P17 | Drumline | FULL* | equivalents only |
| P18 | Error Handler | PARTIAL | jump on miss (CATCH); loops aren't repeats |
| P19 | Toxic Ledger | CORE | Poison action; item grants a conversion row |
| P20 | Thermal Shock | CORE | Burn action; Burn duration per piece, +% per Slow stack |
| P21 | Hemorrhage | CORE | Bleed action; +dmg per Bleed stack |
| P22 | Helpless Prey | PARTIAL | Mark as store; Daze |
| P23 | Drawn Out | CORE | haste the enemy; Slow applied → hero speed |
| P24 | Whittle | CORE | Thorn Mail reflect |
| P25 | Can Opener | PARTIAL | pierce overflow → Bleed; Expose per multihit hit |

### P26–P50
| Id | Name | Grade | Main gaps |
|---|---|---|---|
| P26 | Gaslight | PARTIAL | confused self-hit applies its statuses; Mirror Cloak |
| P27 | Juggernaut | CORE | SHIELD BASH, BRACE; Tower Plate |
| P28 | Edge Walker | PARTIAL | heal cap / overflow → temp HP; LAST STAND |
| P29 | Spiked Barrier | NONE | reflect system; ABSORB; Thorn Mail |
| P30 | Blood Bank | CORE | TRANSFUSION; Vampire Fang |
| P31 | Living Alloy | FULL | — |
| P32 | Desperado | PARTIAL | crit threshold drops at low HP |
| P33 | Bounty Board | PARTIAL | marked kill → dungeon stat; DEEP MARK |
| P34 | Etymology | PARTIAL | materials count as action tags (Alias) |
| P35 | Scrap Heap | PARTIAL | item wear dropped |
| P36 | Payday | CORE | Vault Belt (store cap) |
| P37 | Assayer | CORE | status → matching mint item |
| P38 | Trophy Room | NONE | enemy prefixes have no mechanics |
| P39 | Hive Mind | NONE | no hero swarms |
| P40 | Plague Rats | NONE | no hero swarms; PURGE |
| P41 | Calculated Risk | PARTIAL | FEINT; streak exemption |
| P42 | Slow Epic | CORE | CATACLYSM (strip-length damage); Score Robe |
| P43 | Hit and Run | PARTIAL | fast action → Harden |
| P44 | Bloodbath | NONE | reflect system; RIPOSTE |
| P45 | Bookends | CORE | CLASP; Bookend Pair two-piece bonus |
| P46 | Time Thief | PARTIAL | cheaper actions while enemy stunned; Daze |
| P47 | Echoed Bounty | PARTIAL | Mark as store; REVERB dropped |
| P48 | Clockwork Numbers | PARTIAL | turn number in roll checks; SUM, PARITY BLADE |
| P49 | Heirloom vs Scrapper | CORE | all-Broken / all-Perfect extreme bonus |
| P50 | Haunt the Haunted | NONE | enemy prefixes have no mechanics |
