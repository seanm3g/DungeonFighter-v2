# Action cut — Rebuild step 11.1 (2026-11-22) — **REVIEW**

The D5.2 budget (about 100 actions across every source) applied to `GameData/Actions.json`,
`EnemyActions.json` and `EnvironmentalActions.json`. Decisions used: D5.1 (three roles; a plain hit is a payoff),
D5.2, D5.6, D3.14 (class signature statuses), D3.21 (Wizard = composition), D3.22 (hybrids remix their parents),
the 7-status core plus Mark and Haste. Every line here is a first pass for you to review.

## How the cut works

- **Nothing is deleted from `Actions.json`.** A cut card gets the tag `retired`
  (`GameDataTagHelper.IsRetired`). It still loads, so old saves, strips, skill nodes, tests and gear that already
  grants it keep working; it just never rolls again:
  `GearActionPools.CanRollOnGear` (item roller, base-modifier action picks), `LootDataCache.ActionBonuses` (the loot
  action pool) and every `LootActionSelector` pool use `IsRollableOnHeroGear` (= grantable and not retired).
  `IsGrantableOnHeroGear` is unchanged, so an old item's retired action still shows and works.
- Bots need no change: they only choose among actions the hero owns. The Action Lab catalog keeps every row
  (it is a sandbox; testing a retired card there is useful).
- `Scripts/build-item-mods.py` skips retired cards when it deals base modifiers, and the base modifiers were
  regenerated (`--modifiers-only`), so no new item's modifier names a retired card. The regen also refreshed some
  `RANK` picks that were stale against the current trees.
- Roles below are authored by hand. Rule used: a card that hits and applies a status to the foe is a **payoff**
  (D5.1's `STAB — payoff`); a card whose job is to buff later slots or yourself (Guard, Haste, stats, next-action
  grants) is a **setup**; a card that moves, repeats or fires other cards is a **sequence**.

## Count, player-reachable (before → after)

| Source | Before (rollable or granted) | After | Budget |
|---|---|---|---|
| Classes (4) | 103 class rows + 8 starters | **48** (10 each + 2 starters) | ~48 |
| Hybrids (6) | 61 | **12** rollable (2 each) | ~12 |
| Neutral | 42 | **10** | ~10 |
| Materials (set converts, granted by the set) | 15 | **15** (one per kept material) | ~12 |
| Enemy kits | 16 templates (+ 3 swarm rows) | **11** templates (+ 3 swarms, unchanged) | ~10 |
| Environments | 30 rows | 30 named rows on **8 hazard kinds** (see below) | ~8 |
| **Total distinct mechanics** | ~270 | **~104** | ~100 |

Outside the count: the 5 `unique` rows (OPENER, FINISHER, SELF TARGET, SET BONUS, FLIP ROOM SWITCH) and PUNCH are
internal rows that never roll. The **12 hybrid skill-node cards** (BODY BLOW … HEXBLADE CURSE) are retired from
rolls but their skill nodes still grant them — see question 1.

## Kept (85 rows; 1 new)

### Starters (exempt from D5.6; they come on starter gear)
| Card | Role | Why |
|---|---|---|
| SLAM | payoff | Barbarian class hit, applies Stagger |
| WIND UP | setup | Barbarian starter setup (+25% next) |
| STRIKE | payoff | Warrior class hit, gains Guard |
| SLASH | setup | Warrior starter setup (+25% speed next) |
| STAB | payoff | Rogue class hit, applies Bleed |
| CUT | setup | Rogue starter setup (+1 hit next) |
| MAGIC MISSILE | payoff | Wizard class hit, applies Burn |
| BOLT | setup | Wizard starter setup (+25% AMP next) |

### Barbarian — Stagger; short and heavy; payoffs from pain
| Card | Role | Why |
|---|---|---|
| SHATTERING BLOW | payoff | step 10 Fragile card; the big one-shot |
| BLUDGEON | payoff | step 10 Jam; skill node `b-bludgeon` grants it |
| MIGHT | setup | skill node `b-might` grants it; +3 STR for the dungeon |
| STAGGERING BLOW | payoff | the class status source (2 Stagger) |
| TOPPLE | payoff | cashes the target's Stagger: the class status payoff |
| UNLEASH | payoff | RELEASE: everything queued lands here (charge shape) |
| BERSERKER | payoff | "payoffs from pain": damage per % HP missing |
| HAYMAKER | payoff | the heavy hit (300%, drawback) |
| HEADSMAN'S PROMISE | setup | finisher slot +60%: reads the strip end |
| RETRIGGER OPENER | sequence | the class's one sequence card (kill → opener again) |

### Warrior — Guard; steady; Guard turned into tempo
| Card | Role | Why |
|---|---|---|
| LAST OATH | payoff | step 10 Fragile card (3 Guard) |
| HOLD THE LINE | setup | the Guard source (2 Guard); text now says Guard |
| CUNNING | setup | skill node `w-cunning` grants it |
| AVENGE | payoff | skill node `w-avenge` grants it; rare finisher |
| RETRIGGER FINISHER | sequence | "clean sequences": combo fires the finisher |
| MARCHING DRUM | setup | steady cadence (+3 COMBO every 2nd turn) |
| OPENING GAMBIT | setup | slot-1 setup for the whole strip |
| QUICKSTEP | setup | Haste: the tempo half |
| FLASH CUT | payoff | cashes Haste |
| **SWORN RETORT** (new) | payoff | cashes your Guard (`CASH harden 100`, existing verb): Guard into damage. The class had no Guard payoff. 105%, tier 3, like FLASH CUT |

### Rogue — Bleed; many quick hits; exact rolls
| Card | Role | Why |
|---|---|---|
| SNAPPING EDGE | payoff | step 10 Fragile card (3 Bleed) |
| CONCENTRATE | setup | skill node `r-concentrate` grants it |
| FAN OF KNIVES | payoff | the many-quick-hits card (4 hits) |
| BLOODLET | payoff | cashes a material store into Bleed |
| VENOM COAT | payoff | Bleed source; was `APPLY poison 6` (a retired status) → `APPLY bleed 2`. Name is a rename candidate |
| LOADED DICE | setup | exact rolls: next natural roll = 12 |
| CRIT FACES | setup | exact rolls: 19s crit for the fight |
| GAMBLERS BLESSING | sequence | crit → next card fires again |
| MISDIRECT | sequence | replaces the next strip card |
| THIRD CUT | setup | slot 3 +60%: reads the strip |

### Wizard — Burn; composition (cards acting on later cards)
| Card | Role | Why |
|---|---|---|
| GLASS NOVA | payoff | step 10 Fragile card (3 Burn) |
| READ BOOK | setup | skill node `z-readbook` grants it |
| CONCENTRATED CHAOS | payoff | skill node `z-chaos`; pays off skipped / disabled slots |
| ECHO SPELL | sequence | TRIGGER: retriggers the next slot |
| RETRIGGER SLOT | sequence | TRIGGER: slot 2 fires again |
| STRIP JUMP | sequence | FLOW: the clearest of the strip-flow cards |
| RISING CHORD | setup | MODIFY: ramp over the next 4 |
| CHANNEL | setup | MODIFY: +50% AMP next, slower |
| CRESCENDO | setup | MODIFY by slot position |
| STOKE | payoff | Burn source (2 Burn); moved from Ragecaller to Wizard (tags) because the class had no Burn card besides its starter |

### Hybrids — two each, remixing the parents (D3.22)
| Card | Hybrid | Role | Why |
|---|---|---|---|
| OPEN VEIN | Ravager | payoff | Rogue's Bleed on a Barbarian swing |
| GUT CHECK | Ravager | payoff | Barbarian's Stagger on a Rogue swing |
| SHOULDER CHECK | Warbrute | payoff | step 10 Jam |
| IRON GUARD | Warbrute | setup | Warrior's Guard turned into Barbarian damage |
| JAB | Duelist | payoff | step 10 Jam |
| RIPOSTE | Duelist | setup | Guard plus a crit: Warrior defense into Rogue crits |
| CODA | Spellblade | payoff | RELEASE 40: the Wizard queue paid by a Warrior swing |
| STRIP LOOP | Spellblade | sequence | loops the strip to the opener |
| EXPLODING DICE | Hexblade | setup | Rogue dice luck, Wizard amplification |
| SET THE TRAP | Hexblade | setup | next status card +60% |
| DETONATE | Ragecaller | payoff | the generic status verb; text no longer names Poison |
| FLAME BRAND | Ragecaller | payoff | Wizard's Burn on a heavy Barbarian hit |

### Neutral (10)
| Card | Role | Why |
|---|---|---|
| CASH IN | payoff | the generic store verb (skill trees and the synergy bot use it) |
| RESTORE | payoff | the one heal cash-in |
| STEADY HAND | setup | +1 HIT for 2 turns (dice reliability) |
| KEEN EYE | setup | +1 CRIT for 3 turns |
| LUCK | setup | roll twice, keep the higher |
| RAPID STRIKE | payoff | the neutral multi-hit |
| PHANTOM FOLLOW-UP | setup | +1 hit next |
| NEXT TIME | setup | first 2 slots +35% |
| MEASURED ADVANCE | setup | +1 COMBO for 2 turns |
| CRITICAL ATTACK | payoff | rare big hit (150%) |

### Materials (15; granted by the worn set, never rolled)
IRON CULL, STEEL CREST, BONE WRATH, DRUM CUT, GOLD ANCHOR, MITHRIL FLOW, GLASS NICK, UMBRA, HEADHUNT, WILLOW LASH,
REFRACT, RESONATE, LANDSLIDE, QUIRK, STARFALL — all payoffs; one per kept material set (Wood / Cloth / Unknown
converts were deleted in step 12; the Mythic Legend set has no convert).

## Retired (129 rows, tag `retired`)

| Card | Reason |
|---|---|
| ACTION BONUS | Barbarian next-3 +25% duplicates WIND UP / RISING CHORD |
| FIGHT BONUS | flat +1 HIT for the fight: stat filler |
| LUCKY CHARM | Rogue +1 HIT +1 CRIT: covered by STEADY HAND / KEEN EYE |
| CONCENTRATION | +1 COMBO next turn: MEASURED ADVANCE covers it |
| EXTRA EFFORT | +5 STR next turn: stat filler |
| FIGHTER | +1 STR for the fight: stat filler |
| MOMENTUM BASH | +1 STR for the dungeon: stat filler (MIGHT kept) |
| REACTION TRAINING | +5 AGI next turn: stat filler |
| DEEP BREATHING | +1 AGI for the fight: stat filler |
| DANCE | +1 AGI for the dungeon: stat filler (CUNNING kept) |
| TAKING AIM | +5 TECH next turn: stat filler |
| QUIET MIND | +1 TECH for the fight: stat filler |
| FOCUS | +1 TECH for the dungeon: stat filler (CONCENTRATE kept); name clashes with the Focus status |
| PRE-FIGHT PLANNING | +1 INT for the fight: stat filler |
| DUNGEON ANALYSIS | +1 INT for the dungeon: stat filler (READ BOOK kept) |
| SPRINT | +5% speed next turn: too small to read |
| RUSH | +5% speed next action: too small to read |
| MOMENTUM | +1% speed for the fight: too small to read |
| MARATHON | +0.5% speed for the dungeon: too small to read |
| STRONGER | next +25%: duplicates WIND UP |
| EMPOWERED | next +10% AMP: duplicates BOLT |
| SHARPEN | +4 weapon damage next turn: weapon-stat filler |
| HONE | +1 weapon damage for the fight: weapon-stat filler |
| WHETSTONE | +1 weapon damage for the dungeon: weapon-stat filler |
| LOOSE GRIP | weapon speed/damage trade: weapon-stat filler |
| JUICED | +1 weapon speed: weapon-stat filler |
| BLUR | +1 weapon speed for 2 turns: weapon-stat filler |
| WEAKEN | Weaken is retired (lands as Rattle) |
| SLOW | Slow is retired (lands as Stagger) |
| HARDEN | bare Guard card: HOLD THE LINE covers it |
| PIERCE | ignores armor: overlaps Expose; MIGHTY SWING/CLEAVE THROUGH duplicate it |
| CONFUSE | Confusion is dropped |
| UNLUCK | roll twice keep lower: a curse card with no payoff |
| EXPOSE | bare Expose card: the neutral status comes from gear and payoffs |
| ENRAGE | +2 CRIT −1 HIT: KEEN EYE covers it |
| TAUNT | Expose + combo: two half-mechanics; the class-point unlock path is gone (D5.3) |
| BASH | Expose hit: duplicate of SKULL CRACK |
| STRIP SKIP | FLOW: STRIP JUMP is the clearer jump |
| STRIP REPEAT | FLOW: ECHO SPELL / RETRIGGER SLOT cover repeats |
| STRIP STOP | ends your own combo on a miss: a trap card |
| STRIP RANDOM | FLOW: randomness against the "compose the strip" identity |
| STRIP SHUFFLE | FLOW: same |
| STRIP REPLACE | MISDIRECT does the same with clearer text |
| RETRIGGER NEXT | duplicates ECHO SPELL |
| SALVAGE MISS | miss insurance: covered by skill nodes |
| REPLACE ROLL | duplicates LOADED DICE |
| NATURAL SEVEN | heal on a natural 7: trivia |
| CRIT HEAL | flat heal on crit: heal filler |
| COMBO END HEAL | flat heal on combo end: heal filler |
| CRIT MISS HEAL | flat heal on a crit miss: heal filler |
| AFTER MISS HEAL | flat heal after a miss: heal filler |
| ROOM CLEAR HEAL | flat heal after a room: heal filler |
| ROLL FIFTEEN | heal on exactly 15: trivia |
| STRIP DISABLE | locks your own slot: CONCENTRATED CHAOS keeps the idea as a payoff |
| MIGHTY SWING | ignores armor: overlaps HAYMAKER |
| WARCRY | applies Weaken (retired) |
| BARBARIAN RAGE | two buffs plus a retrigger: RETRIGGER OPENER does the sequence part cleanly |
| CHALLENGE | applies Weaken (retired) |
| MEASURED CUT | +1 COMBO next turn: MEASURED ADVANCE |
| REWRITE FATE | duplicates LOADED DICE for Wizard |
| SKULL CRACK | Expose hit: Barbarian keeps Stagger |
| PARRY | Guard card: duplicate of HOLD THE LINE |
| LEECHING CUT | 2% lifesteal: too small to read |
| FROST BOLT | applies Slow (retired) |
| BODY BLOW / WARBRUTE SMASH | hybrid "+25% per 10 attribute / tags" filler (D7.5); skill nodes `wb-act-*` still grant them |
| PROWESS RUSH / RAVAGER FLURRY | same; nodes `rv-act-*` |
| WILLFIRE / RAGECALLER HOWL | same; nodes `rc-act-*` |
| FINESSE THRUST / DUELIST FEINT | same; nodes `du-act-*` |
| WITS ARC / SPELLBLADE WEAVE | same; nodes `sb-act-*` |
| MIND HEX / HEXBLADE CURSE | same (also reads Weaken); nodes `hx-act-*` |
| SURGE | cash into a stat: CASH IN / RESTORE cover the neutral verb |
| BULWARK | cash into temp HP: SWORN RETORT is the Warrior's store payoff now |
| GRIT TEETH | cash into Guard for Barbarian: off-identity |
| CENTER | cash into Focus: off-identity for Wizard (store verbs dilute it, D3.21) |
| QUICKEN | cash into Haste: duplicate of QUICKSTEP / FLASH CUT |
| IGNITE | cash into Burn: FLAME BRAND / STOKE cover Ragecaller / Wizard Burn |
| LAY BARE | cash into Expose: generic |
| STEEL NERVE | cash into Fortify (a hero resource without a status) |
| PRELUDE | next 3 +15% AMP: RISING CHORD / CHANNEL cover it (PREP tests keep it loading) |
| BLOOD OATH | HP cost PREP: BERSERKER is the pain payoff (tests keep it loading) |
| CORRODE | Acid is retired (lands as Expose) |
| DAZE | Stun is retired (lands as Stagger); STAGGERING BLOW covers it |
| BRACE | Fortify is a status application of a retired id |
| LUNGE | +10% speed next: small |
| CADENCE STRIKE | +1 COMBO for 2 turns: MEASURED ADVANCE |
| LOW BLOW | applies Weaken (retired) |
| CLEAVE THROUGH | ignores armor: overlaps HAYMAKER / Expose |
| GUTTING STRIKE | 1 Bleed hit: OPEN VEIN covers Ravager Bleed |
| THUNDER CLAP | Expose + AMP: two half-mechanics |
| FEINT AND THRUST | +1 HIT next turn: STEADY HAND |
| DISARM | Weaken + Slow (both retired) |
| GATHERING STORM | ramp: RISING CHORD is the ramp card |
| LIT FUSE | delayed +50%: UNLEASH is the Barbarian charge card |
| BLOOD FRENZY | ramp that a miss ends: RISING CHORD |
| RELAY STRIKE | delayed speed: tempo is QUICKSTEP / FLASH CUT |
| FLURRY STEP | speed decay: same |
| PATIENT BLADE | delayed crit window: CRIT FACES / KEEN EYE |
| COUNTED PACES | slot-scaled next turn: THIRD CUT reads the strip for Rogue |
| EVEN ODDS | even-slot buff: SET THE TRAP is Hexblade's setup |
| RUNE OF THE FIFTH | slot 5 only: rarely reachable |
| LATE BLOOM | slot 3+ window: OPENING GAMBIT / HEADSMAN'S PROMISE read the strip |
| HOURGLASS | delayed crit window: KEEN EYE |
| BLOOD RUSH | Barbarian Haste: tempo belongs to Warrior |
| SUNDER | applies Brittle (now Mark); name shared with the enemy kit |
| HAMSTRING | applies Slow (retired) |
| RATTLING BLOW | Rattle: neutral dice status; Warrior keeps Guard |
| BARGE | 1 Stagger: GUT CHECK covers it |
| FIND WEAKNESS | applies Brittle (retired, lands as Mark) |
| FADE STEP | 1 Haste: QUICKSTEP |
| HEX | applies Weaken (retired) |
| JINX | 3 Rattle: neutral dice status off the Wizard identity |
| ENERVATE | applies Weaken (retired) |
| TIME WARP | 2 Haste: QUICKSTEP |
| CRYSTALLIZE | applies Brittle (retired) |
| SHATTERPOINT | cashes Brittle (retired) |
| MIND BREAK | cashes Rattle: off-identity |
| COILED STRIKE | next Bludgeon card +50%: tag-waiting setup, hard to read |
| DRILL | next 2 Swift cards: tag-waiting setup |
| PRIMER | next Focus card: tag-waiting setup |
| FEINT | next Rogue card: tag-waiting setup; skill node text still names it |
| WAR BAND | per Warbrute card in strip: tag counter (D7.5) |
| CHORUS | per Wizard card in strip: tag counter |
| PACK TACTICS | per Status card in strip: tag counter |

## Enemy kits: 16 → 11 templates

Kept: ENEMY SCORCH (Burn), ENEMY REND (Bleed), ENEMY CRUSH (heavy), ENEMY SUNDER (Expose), ENEMY HEX (Rattle; was
Weaken), ENEMY ENSNARE (Stagger; was Slow), ENEMY JAM, ENEMY SHATTER (Burn out), ENEMY SHELL (Guard), ENEMY DRAIN
(lifesteal), ENEMY FRENZY (3 hits). Merged (template rows removed, the names stay as aliases so anything that names
them still resolves): VENOM → REND (Poison lands as Bleed anyway; this also makes the Venom enemies actually Bleed,
which they didn't since step 5), CURSE → SUNDER (identical), PIERCE → SUNDER (armor ignore ≈ Expose), CONCUSS →
ENSNARE (Stun is Stagger; note ENSNARE staggers on every hit where CONCUSS only did on crits), MEND → DRAIN. The
`kit` arrays in `Enemies.json` and the dungeon signatures in `EnemyAffixes.json` were remapped; no dungeon ended up
with a duplicate signature. **Gameplay effect, no sims:** former Pierce enemies Expose instead of ignoring armor,
Mend enemies heal 50% instead of 100% (but hit at 90% instead of 60%), Concuss enemies Stagger more often.

## Environments: 30 named hazards on 8 kinds

After the status core, the 30 hazards already reduce to 8 mechanics; their names are theme flavor (like enemy kit
flavor names). The descriptions now name the core status, and no hazard text says weaken / stun / slow / poison /
corrode any more. Numbers are unchanged.

| Kind | Rows |
|---|---|
| Rattle haze (no damage) | Ash Cloud, Musk Haze, Heat Wave, Fog Bank |
| Rattle + damage | Ancient Curse (chamber, ruins), Steam Burst |
| Stagger (no damage) | Rising Dead, Sandstorm, Shifting Hedges, Slick Mud, Gale Force, Cold Current |
| Stagger + damage | Chamber Collapse, Falling Branch, Collapsing Pillar, Mirror Glare, Rockfall, Tidal Surge |
| Bleed + damage | Ice Shards, Bone-Strewn Floor, Thorn Vines, Dart Trap |
| Bleed (no damage) | Pollen Cloud, Curse of the Tomb |
| Burn + damage | Ember Rain, Sacred Flames, Lava Splash, Scalding Vent |
| Expose | Corroding Brine |

## Rename proposals (not done; D9.12 says renames happen once)

Cards whose names fight their new mechanics or the lore, for one rename pass with you:
- VENOM COAT (now Bleed) → e.g. "ASH IN THE CUT" (Long Grief: rub ash into old cuts).
- Venom Mace / Envenomed Edge skill nodes (now Bleed → Stagger / Burn → Bleed) → names without "venom".
- FLASH CUT / QUICKSTEP fine; SWORN RETORT is new and lore-flavored (TM-word-warrior: "a warrior's sworn").
- Regions / dungeons / enemies: the lore's places fit the 12 dungeon themes (e.g. Ashen Land → the Embers' Crater
  country, Desert → the Glass Plains, Mirror Pond → Kessara reeds, Labyrinth → Yveth's Ruin). Not renamed here.

## Questions for you
1. **Hybrid skill-node cards.** The 12 hybrid skill nodes still grant BODY BLOW … HEXBLADE CURSE (D7.5 filler).
   Re-point those nodes at the kept hybrid cards, or rewrite the nodes in the step 9 node pass?
2. Is SWORN RETORT (a new card, existing verb) acceptable, or should the Warrior's Guard payoff come from a skill node?
3. STOKE moved from Ragecaller to Wizard. OK, or give Wizard a different Burn source?
4. Environments: is "8 kinds under 30 themed names" what you meant by ~8, or should the rows themselves shrink to 8?
