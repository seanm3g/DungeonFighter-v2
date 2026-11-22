# Environments — content pass (2026-11-22) — **REVIEW**

Your call (2026-11-22): "3 environments with 3 enemies each, with 2 environment actions per environment (positive and
negative), and 1-2 actions per enemy", at **3 environments per region**. An environment is a region theme × one of
its three dungeon themes (`Regions.json` `linkedDungeonThemes`), so there are **12 environments** (the 20 regions cycle
the 4 themes, so each environment recurs every 20 levels), **36 enemies**, **24 room actions** (12 hazards, 12 boons).
No balance work: enemy stats, kits and hazard numbers are unchanged; only who spawns and which room actions roll.

## How it works

- **Enemies.** Each environment's dungeon (`Dungeons.json` `possibleEnemies`) and each of its three rooms
  (`Rooms.json` `enemies`) list the same three enemies; each room leads with a different one (weight 2, the others 1),
  so the rooms still feel different. Picks follow the dungeon identity (`EnemyAffixes.json`): the three enemies'
  kits together cover the dungeon's three signature kits (so the Demon's signature attacks are ones you met on the way),
  they come from three different families where the dungeon has them, and each environment has one Rare enemy (Rare
  enemies carry the dungeon affix) except Dense Brush (see below).
- **Room actions.** Every room lists exactly two: its environment's **hazard** (one of the existing rows, chosen so
  the 12 spread over the core statuses: Bleed 3, Burn 3, Stagger 3, Rattle 2, Expose 1) and its **boon**.
- **Boons (the helpful action).** No helpful room action existed: every row was a `Debuff`, and
  `EnvironmentalActionExecutor` applies its effect to everyone the room targets. A boon is a new row with
  `"type": "Buff"` and a `grants` column naming a status from the existing set (`harden` = Guard, `focus`, `haste`,
  4 environments each). It runs through the same room turn and roll trigger as a hazard; the executor adds the rolled
  1–2 stacks (the hazard duration roll, so 1 in 4 still does nothing) with `Stores.Add`, to **the hero only**
  (`Action.RoomBoon`; `CombatTurnHandlerSimplified` targets the hero, the executor drops enemies). Room affixes and
  terrain tags that change hazards (status, damage, targets) skip boons (`RoomAffixRuntime.Hazards`). No new store,
  status or verb.
- **Fix found on the way:** Corroding Brine (Water Temple's hazard) set `causesAcid`, which the executor never
  applied, so it did nothing. It now adds 1 Expose (Acid loads as Expose), like the other aliased hazards.
- **Retire, don't delete.** `retired: true` on 92 enemies (72 regional + the 20 `region: "n/a"` General enemies,
  swarms included), the 80 Legacy rooms, the 3 Legacy dungeons (Ancient Forest, Lava Caves, Haunted Crypt) and 18
  hazard rows. Retired rows still load by name (old saves, tuning tools that fight Wolf / Goblin, tests, the Action
  Lab), but never spawn or roll: room pools, dungeon pools, the theme fallback and **wanderers** (the 10% wander now
  draws only from the other environments' enemies) skip retired enemies (`EnemyGenerationManager`),
  `RoomLoader.GetRoomsByTheme` skips retired rooms, and `DungeonManagerWithRegistry` never offers retired dungeons.
  Nothing core needed the Legacy / General rows: the tutorial fights the coded Training Dummy, the Demon is the merged
  last room of a region dungeon, and the General rows were reachable only as wanderers or through the Legacy
  dungeons (which no region links).
- **Tests:** new `Environments` suite (12 environments, 3 kept enemies with 1–2 actions per dungeon and room, one
  hazard + one boon per room, 24 live room actions, all six families, retired rows load but never spawn incl.
  wanderers, the boon lands Guard on the hero and never the enemy, room affixes leave boons alone, Brine lands
  Expose). `FeatureScenario` expects Thicket's Thorn Vines + Sunlit Gap; `EnemyRoomAffix`'s scorched check now reads
  hazards only (its 9 other failures predate this pass).

## The 12 environments

Names in this section are the pre-rename ones (content step 2); the lore renames of step 3 are tabled at the end.

Region family in brackets (`EnemyAffixes.json`): Forest = Rooted (+HP), Water = Undertow (Stagger on hit), Fire =
Kindled (Burn on hit), Air = Windswept (speed). Kit names are `ENEMY <kit>` templates.

### Forest [Rooted]

**The Den** — signature Rend / Frenzy / Crush, affix Bloodscent (Bleed on hit). *Beasts and big hitters; bleeding and
heavy blows.*
| Enemy | Family | Rarity | Actions (kit) | Why |
|---|---|---|---|---|
| Alpha Wolf | Beast | uncommon | Leader's Fang (Rend), Rally Pack (Frenzy) | the Beast teacher: Bleed plus a 3-hit flurry |
| Cursed Bear | Brute | uncommon | Crushing Maul (Crush), Mauling Frenzy (Frenzy) | heavy tempo: the slow 170% hit to Guard against |
| Bone Spreader | Sage | Rare | Bone Hammer (Crush), Bone Splinter (Rend) | the Rare that carries Bloodscent; a Sage in a den of animals |
- Hazard **Bone-Strewn Floor** (Bleed). Boon **Warm Hollow** (Guard): back to the roots.

**Hedge Labyrinth** — signature Jam / Hex / Ensnare, affix Turnabout. *Disruption: your strip gets skipped,
burned out and slowed.*
| Enemy | Family | Rarity | Actions (kit) | Why |
|---|---|---|---|---|
| Hedge Hound | Beast | uncommon | Hedge Ram (Ensnare), Bush Ambush (Jam) | Stagger + Jam on a plain-reading animal |
| Dead End Mimic | Trickster | Rare | Trap Door (Jam), Dead End Dread (Hex) | the Rare; Jam + Rattle |
| Shattered Reflection | Trickster | uncommon | Feeble Reflection (Hex), Fragment (Shatter) | the one **Burn out** enemy kept (counter to long strips) |
- Hazard **Shifting Hedges** (Stagger). Boon **Straight Run** (Haste).

**Dense Brush** — signature Rend / Ensnare / Drain, affix Overgrowth (+HP). *Snares and draining growth.*
| Enemy | Family | Rarity | Actions (kit) | Why |
|---|---|---|---|---|
| Valley Prowler | Beast | Rare | Toxic Fang (Rend), Hamstring (Ensnare) | Bleed + Stagger hunter; the Rare |
| Living Wall Crawler | Assassin | uncommon | Mortar Grip (Ensnare), Stone Feast (Drain) | fast Stagger + lifesteal |
| Animated Vine | Knight | uncommon | Binding Whip (Ensnare), Grow (Drain) | the patient one (Duelist): Stagger and outlast |
- Hazard **Thorn Vines** (Bleed). Boon **Sunlit Gap** (Focus).

### Water [Undertow]

**Mirror Pond** — signature Ensnare / Drain / Sunder, affix Leeching. *Expose and drain from the reeds.*
| Enemy | Family | Rarity | Actions (kit) | Why |
|---|---|---|---|---|
| Reed Stalker | Assassin | uncommon | Reed Tangle (Ensnare), Predator's Eye (Sunder) | Stagger, then Expose |
| Mosquito Swarm | Trickster | uncommon | Needle Proboscis (Sunder), Blood Suck (Drain) | the one **swarm** tempo kept: many small hits |
| Shelf Specter | Trickster | Rare | Veil Snare (Ensnare), Phantom Siphon (Drain) | the Rare (Leeching on a drainer) |
- Hazard **Fog Bank** (Rattle). Boon **Still Water** (Focus).

**Hidden Spring** — signature Ensnare / Scorch / Drain, affix Mineral (Acid = Expose). *Scalding water: Burn and
Stagger together.*
| Enemy | Family | Rarity | Actions (kit) | Why |
|---|---|---|---|---|
| Cave Lurcher | Brute | Rare | Lurching Grab (Ensnare), Cavern Thrash (Scorch) | slow Brute; the Rare |
| Chronicle Watcher | Sage | uncommon | Calcified Gaze (Ensnare), Basin Renewal (Drain) | the Sage that sustains |
| Sinkhole Lurker | Assassin | uncommon | Scalding Mud (Scorch), Pit Pull (Ensnare) | fast Burn |
- Hazard **Scalding Vent** (Burn). Boon **Cold Spring** (Haste).

**Water Temple** — signature Sunder / Shell / Crush, affix Bulwark (starts Guarded). *Armor: Guard on them, Expose
on you.*
| Enemy | Family | Rarity | Actions (kit) | Why |
|---|---|---|---|---|
| Fourecourt Guard | Knight | uncommon | Tidal Bulwark (Sunder), Shield Wall (Shell) | the Knight teacher: Guard up, Expose you |
| Depth Drainer | Sage | uncommon | Abyssal Shell (Shell), Abyssal Crush (Crush) | Guard then a heavy hit |
| Throne Revenant | Brute | Rare | Armor Rot (Sunder), Drowned Wrath (Crush) | Warlord; the Rare |
- Hazard **Corroding Brine** (Expose; now works). Boon **Temple Ward** (Guard).

### Fire [Kindled]

**Ashen Land** — signature Scorch / Rend / Hex, affix Smolder (Rattle). *Burn and Bleed from the smoke.*
| Enemy | Family | Rarity | Actions (kit) | Why |
|---|---|---|---|---|
| Cinder Hound | Beast | uncommon | Scorching Bite (Scorch), Ripping Fang (Rend) | Burn + Bleed animal |
| Cave Ember Toad | Brute | uncommon | Ember Spit (Scorch), Numbing Tongue (Hex) | slow Burn + Rattle |
| Smog Lurker | Assassin | Rare | Smog Step (Rend), Smog Ignition (Scorch) | fast; the Rare |
- Hazard **Ember Rain** (Burn). Boon **Ash Lee** (Guard).

**Fire Temple** — signature Scorch / Shell / Sunder, affix Zealot. *Guarded zealots who Burn.*
| Enemy | Family | Rarity | Actions (kit) | Why |
|---|---|---|---|---|
| Temple Knight | Knight | uncommon | Iron Vow (Shell), Shield Bash (Scorch) | Guard + Burn |
| Lava Spitter | Sage | uncommon | Armor Melt (Sunder), Lava Glob (Scorch) | Expose + Burn |
| Torch Wraith | Trickster | Rare | Ash Veil (Shell), Wall of Ash (Scorch) | the Rare |
- Hazard **Sacred Flames** (Burn). Boon **Brazier Glow** (Focus).

**Lava Cave** — signature Scorch / Drain / Shell, affix Molten Shell (starts Guarded). *Shells and leeches.*
| Enemy | Family | Rarity | Actions (kit) | Why |
|---|---|---|---|---|
| Molten Crab | Knight | Rare | Magma Carapace (Scorch), Crust Guard (Shell) | the armored one; the Rare |
| Blood Feeder | Assassin | uncommon | Fevered Bite (Scorch), Draining Bite (Drain) | fast Burn + lifesteal |
| Overflow Imp | Trickster | uncommon | Lava Douse (Scorch), Leeching Pounce (Drain) | Trickster drainer |
- Hazard **Steam Burst** (Rattle, chosen over Lava Splash so the Fire region isn't all Burn). Boon **Hot Draft** (Haste).

### Air [Windswept]

**Glass Desert** — signature Sunder / Ensnare / Drain, affix Glare (Expose). *Glare and sand: Expose and Stagger.*
| Enemy | Family | Rarity | Actions (kit) | Why |
|---|---|---|---|---|
| Sandstorm Flanker | Brute | uncommon | Grit Lance (Sunder), Sand Shackle (Ensnare) | Berserker with hands: Expose + Stagger |
| Glass Shard Golem | Knight | Rare | Glass Shackles (Ensnare), Shard Siphon (Drain) | heavy Duelist; the Rare |
| Mirage Hunter | Trickster | uncommon | Mirage Mire (Ensnare), Thirsting Mirage (Drain) | the Trickster |
- Hazard **Sandstorm** (Stagger). Boon **Standing Stone** (Guard).

**The Pyramids** — signature Sunder / Rend / Drain, affix Embalmed (Bleed). *Old dead who Expose and Bleed you.*
| Enemy | Family | Rarity | Actions (kit) | Why |
|---|---|---|---|---|
| Guardian Scarab | Knight | uncommon | Scarab Curse (Sunder), Venom Mandibles (Rend) | Expose + Bleed |
| Mummy | Brute | uncommon | Rot Wrap (Rend), Linen Leech (Drain) | slow Bleed + lifesteal |
| Sun Priest | Sage | Rare | Sun's Judgment (Sunder), Sunlit Renewal (Drain) | the Rare (and the lore's Sun Priests) |
- Hazard **Dart Trap** (Bleed). Boon **Sun Shaft** (Focus).

**Windswept Mountains** — signature Crush / Ensnare / Frenzy, affix Thunderhead (Stagger). *Tempo: flurries, drops
and falling rock.*
| Enemy | Family | Rarity | Actions (kit) | Why |
|---|---|---|---|---|
| Hawk | Trickster | uncommon | Talon Flurry (Frenzy), Plummet Strike (Crush) | fast multi-hit |
| Edge Stalker | Assassin | Rare | Cliff Drop (Crush), Ledge Flurry (Frenzy) | the Rare |
| Storm Caller | Sage | uncommon | Lightning Strike (Ensnare), Thunderfall (Crush) | Stagger caster |
- Hazard **Rockfall** (Stagger). Boon **Tailwind** (Haste).

**Coverage:** families Beast 4, Brute 6, Knight 6, Assassin 6, Sage 6, Trickster 8 (the Labyrinth is the trickster
maze); every enemy kit template appears (Scorch, Rend, Crush, Sunder, Hex, Ensnare, Jam, Shatter, Shell, Drain,
Frenzy); the core statuses all show up on enemies, hazards or boons (Focus only as a boon); one swarm, one Burn-out.

## Retired

- **Enemies (92):** the other six of each dungeon's nine (e.g. Den: Territorial Hound, Tribute Beast, Mound Warden,
  Moon Blessed, Echo Stalker, Vigil Alpha) and the 20 General rows (Goblin, Spider, Wolf, Fire Elemental, Lava Golem,
  Bats, Rats, Giant Ants, Skeleton, Zombie, Wraith, Bear, Treant, Boar, Salamander, Ember Spirit, Magma Beast, Ghoul,
  Lich, Wight). Their flavor action aliases stay in `EnemyActions.json`.
- **Room actions (18):** Ancient Curse ×2, Chamber Collapse, Rising Dead, Ice Shards, Ash Cloud, Musk Haze, Falling
  Branch, Pollen Cloud, Heat Wave, Collapsing Pillar, Mirror Glare, Lava Splash, Slick Mud, Gale Force, Curse of the
  Tomb, Cold Current, Tidal Surge.
- **Rooms (80)** with `region: "Legacy"` and the **3 Legacy dungeons**.

## Open, for you

1. **Dense Brush has no single-Rare pick that keeps a Beast and an Assassin**; it has one Rare (Valley Prowler), with
   Living Wall Crawler and Animated Vine sharing the Ensnare / Drain kit (different family and tempo).
2. **Turnabout** (Labyrinth's dungeon affix) applies `disrupt`, a dropped status, so it does nothing (since step 12).
   Jam would be the core-mechanic version; not changed (an affix, not content I was asked to touch).
3. **Slow hazards** (Shifting Hedges, Sandstorm) slow the hero (`CharacterEffects` Slow) and do nothing to enemies,
   though their text says Stagger (since step 5). Switching them to the Stagger store is one line; not changed here.
4. Boon names and amounts (1–2 stacks, same roll as hazards) are first guesses; no sims.

## Lore renames (D9.12, done once, 2026-11-22) — **REVIEW**

Source: the collaborator's Faerlan artifact, **Common knowledge and Legend / scholar lenses only** (the Truth lens and
the original prose were not read for this; the saved page was parsed for those two lenses). Record IDs below; a name
taken from a record's title (no lens line used) says "title". Display names change; ids, themes, code identifiers and
file keys don't. Old names keep loading:

- **Enemies** have no id, so each renamed row keeps `formerNames` (`EnemyData.FormerNames`): `EnemyLoader`
  (`CreateEnemy`, `GetEnemyData`, `HasEnemy`), room pools and the per-enemy **calibration** keys in the balance patches
  (`EnemyIdentity`, keyed by name) all resolve an old name to the renamed row, so no numbers move. Renamed flavor
  actions get new `EnemyActions.json` aliases; the old alias keys stay.
- **Dungeons** have no id either: `formerNames` on `Dungeons.json` rows (`DungeonData.formerNames`), read by
  `DungeonThemeColors.GetThemeForDungeonName` (the only name → dungeon lookup); history entries keep whatever name
  they recorded. Themes (Den, Labyrinth, ...) are unchanged, so rooms, affixes and matchups are untouched.
- **Regions**: `displayName` only; ids (`forest`, `water-2`, ...) are what saves and code use.
- The Demon titles in `EnemyAffixes.json` ("Demon of the Den") are unshown data and were left.
- Player text now says **Demon Fighter** in the window titles, the About panel, the quit line and the "already
  running" line (the in-game "DUNGEON FIGHTERS" panel header was left: code compares that string).

### Regions (20; id → new display name)
| Id | Old | New | Record |
|---|---|---|---|
| forest | Whisperledger Wilds | Widow's Blush | PL-widows-blush (the forest the Widows saved by running roads through it) |
| water | Mirrormere Shallows | Reed Hollow | PL-reed-hollow (title), EV-long-wet |
| lava | Ashglass Covenant | The Ember Valleys | EV-emptying-of-the-ember-valleys |
| air | Sandglass Dunes | The Glass Road | EV-caravan-mistress ("every well on the glass road") |
| forest-2 | Thornveil Deepwood | Greenreach | PL-greenreach, EV-greenreach-refusal |
| water-2 | Tideglass Reach | Lockmere | EV-lockmere-raised |
| fire-2 | Cinderfall Marches | Aldergate | EV-third-day-at-aldergate (a morning of ash), EV-second-inheritance |
| air-2 | Sunscour Wastes | Blackglass | EV-noon-at-blackglass |
| forest-3 | Elderroot Expanse | Duskmoor | PL-duskmoor-wood, EV-duskmoor-wood |
| water-3 | Sunken Choir | Siltmoor Mere | EV-siltmoor-barley |
| fire-3 | Emberforge Depths | Cresthollow | EV-vent-keepers-of-cresthollow (miners) |
| air-3 | Tombwind Heights | The Cracked Ascension | PL-cracked-ascension (title), EV-names-in-the-ascension |
| forest-4 | Gloamhollow Weald | Hallow's Bend | PL-hallows-bend, EV-hound-village |
| water-4 | Abyssal Mirror | Stillmoot | PL-sallowmere (title "Stillmoot") |
| fire-4 | Pyre of Oaths | Emberfall | EV-standing-ban (four houses swore at Emberfall) |
| air-4 | Skyreach Spires | The Highspine | EV-featherfalls-echo |
| forest-5 | Worldroot Heart | The Bramble Mother's Wood | EV-greenreach-refusal |
| water-5 | Drowned Crown | The Marsh-Heart | EV-marsh-heart-scouts, EV-lines-to-the-heart |
| fire-5 | Molten Throne | The Embers' Crater | PL-embers-crater (title), EV-crater-loaf |
| air-5 | Stormcrown Summit | Antarkeia | PL-antarkeia (ground everyone has reason to leave alone) |

### Dungeons (12; theme unchanged)
| Theme | Old | New | Record |
|---|---|---|---|
| Den | The Den | Denwatch | EV-denwatch-squatters, EV-alphas-reach |
| Labyrinth | Hedge Labyrinth | The Hedge Nobody Leaves | EV-hedge-nobody-leaves |
| Dense Brush | Dense Brush | The Gray Ring | EV-grey-ring-in-the-wood |
| Mirror Pond | Mirror Pond | The Millpond | EV-boy-in-the-millpond, EV-fog-voice |
| Spring | Hidden Spring | The Crystal Beds | EV-steam-crystals |
| Water Temple | Water Temple | The Drowned Court | PL-water-temple |
| Ashen Land | Ashen Land | The Charwood | EV-the-charwood |
| Fire Temple | Fire Temple | The Burning Hall | EV-order-of-the-burning-hall |
| Lava Cave | Lava Cave | The Smoke-Cave | EV-golems-wall ("the smoke-cave") |
| Desert | Glass Desert | The Dune-Hollows | EV-caravan-mistress ("out of the dune-hollows at dusk") |
| Pyramids | The Pyramids | The Tomb of Too Many Eyes | PL-tomb-of-too-many-eyes (title) |
| Mountains | Windswept Mountains | The Ridgeway | EV-ridgeway-partys-toll |

### Enemies (33 of the 36; kept: Chronicle Watcher, Mummy, Sun Priest, already lore names: GR-chronicle-keepers, EV-mummy-brown-trade, FA-sun-priests)
| Old | New | Record | Renamed flavor actions |
|---|---|---|---|
| Alpha Wolf | Mother's Wolf | EV-wolf-taboo, EV-bramble-mothers-grave | Leader's Fang → Wolf-Stone Fang; Rally Pack → Warband Rush (EV-alphas-reach) |
| Cursed Bear | Orchard Bear | EV-bare-orchard | — |
| Bone Spreader | Bone-Reader | EV-bone-readers | — |
| Hedge Hound | Hedge-King's Hound | EV-hedge-king | Bush Ambush → Hedge Ambush |
| Dead End Mimic | Wrong Door | EV-wrong-door ("No ribbon, no door") | Trap Door → No Ribbon |
| Shattered Reflection | Echo Widow | EV-echo-widow | Feeble Reflection → Last Words |
| Valley Prowler | Bounty Prowler | EV-wolf-bounty | Toxic Fang → Bounty Fang |
| Living Wall Crawler | Gray Ring Crawler | EV-grey-ring-in-the-wood | Mortar Grip → Root Grip |
| Animated Vine | Mosshaven Thorn-Ring | EV-thorn-rings-of-mosshaven | — |
| Reed Stalker | Reed-Man | EV-reed-boat-raiders | Predator's Eye → Fog Raid |
| Mosquito Swarm | Marsh-Hum Swarm | EV-edge-sitters ("the marsh hums") | — |
| Shelf Specter | Fog Voice | EV-fog-voice | Phantom Siphon → Only the Voice |
| Cave Lurcher | Cold Shrine Lurcher | PL-long-cold-shrine (title) | — |
| Sinkhole Lurker | Crystal-Bed Leech | EV-steam-crystals ("the leech is already there") | — |
| Fourecourt Guard | Drowned Court Guard | PL-water-temple | Tidal Bulwark → Court Bulwark |
| Depth Drainer | Channel-Keeper | EV-channel-that-stopped | — |
| Throne Revenant | Crowned Revenant | EV-crowned-revenant | — |
| Cinder Hound | Charwood Hound | EV-the-charwood | — |
| Cave Ember Toad | Crater Toad | EV-crater-loaf | Ember Spit → Crater Spit |
| Smog Lurker | Dry Fog Lurker | EV-dry-fog | Smog Step → Dry Fog Step; Smog Ignition → Dry Fog Ignition |
| Temple Knight | Burning Hall Knight | EV-order-of-the-burning-hall | Iron Vow → Burning Hall Vow |
| Lava Spitter | Black-Glass Spitter | EV-smoking-mirrors | — |
| Torch Wraith | Ember Watch Wraith | EV-ember-watch | — |
| Molten Crab | Hearthstone Crab | EV-crab-stones | — |
| Blood Feeder | Amber Leech | EV-amber-cutters | — |
| Overflow Imp | Salamander Imp | EV-salamander-charter | — |
| Sandstorm Flanker | Glass-Road Raider | EV-caravan-mistress | — (also the Action Lab's default enemy) |
| Glass Shard Golem | Blackglass Golem | EV-noon-at-blackglass, EV-golems-wall | Glass Shackles → Blackglass Shackles; Shard Siphon → Blackglass Siphon |
| Mirage Hunter | Moving Water | EV-lost-caravan-of-the-oasis | Mirage Mire → Dry Mire; Thirsting Mirage → False Water |
| Guardian Scarab | Scarab Warden | EV-scarab-wardens | Scarab Curse → Warden's Curse; Venom Mandibles → Grave Mandibles |
| Hawk | Cragwing | EV-cragwings-face-west | — |
| Edge Stalker | Highspine Stalker | EV-featherfalls-echo | — |
| Storm Caller | Ridgeway Caller | EV-ridgeway-partys-toll | Thunderfall → Pass Toll |

Count: 20 regions, 12 dungeons, 33 enemies, 21 enemy flavor actions, 3 player-facing cards / nodes (see
`ACTION_CUT_2026-11-22.md`). The 12 boon names are new and plain (Warm Hollow, Straight Run, ...), not lore names.