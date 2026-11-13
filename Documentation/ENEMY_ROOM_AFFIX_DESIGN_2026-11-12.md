# Enemy, room and boss affixes (implemented 2026-11-12)

Same shape as the action class sort: every enemy and room gets a home identity, most rolls stay home (90%), a few wander in (10%), and the dungeon's Demon is built from everything its home enemies do.

Rule: no new combat mechanics. Every affix is one of the levers enemies and rooms already support:

| Lever | Exists today as |
|---|---|
| **ON HIT** | enemy action trigger bundles (`ONCONNECT` → poison / burn / bleed / slow / weaken / expose / disrupt / stun-on-crit), the 15 `EnemyActions.json` templates |
| **START** | harden / fortify applied at fight start (HP regen, temp HP and reflect have no combat effect today, so no affix uses them) |
| **STAT** | HP / damage / speed multipliers (`EnemyIdentity`, `EncounterDamageMultiplier`) |
| **HAZARD** | room environment actions: power (`damageMultiplier`), frequency (activity tags), status (`causes*`), targets |
| **ROOM** | level roll (elegant / dilapidated), enemy count, prefix roll chance, loot rarity |

Implementation reuses the `EnemyPrefixes.json` pipeline (`EnemyPrefixRoller`, `Enemy.PrefixTags`), which today rolls a name and tags with no effect.

---

## 1. Enemy affixes

Complexity ladder: **common / uncommon enemies carry 1 affix** (region), **Rare enemies 2** (region + dungeon), **the Demon 3 + a phase** (section 4).

### 1a. Region affixes: 4 element families × 5 ranks = 20

Applied to every enemy spawned in a region's dungeons. Rank = the region's cycle (forest = I, forest-2 = II … forest-5 = V).

| Family | Regions | Lever | Rank I → V |
|---|---|---|---|
| **Rooted** (Forest) | Whisperledger Wilds, Thornveil Deepwood, Elderroot Expanse, Gloamhollow Weald, Worldroot Heart | STAT HP | +5% → +25% max HP |
| **Undertow** (Water) | Mirrormere Shallows, Tideglass Reach, Sunken Choir, Abyssal Mirror, Drowned Crown | ON HIT Slow | 10% → 50% chance on connect |
| **Kindled** (Fire) | Ashglass Covenant, Cinderfall Marches, Emberforge Depths, Pyre of Oaths, Molten Throne | ON HIT Burn | 10% → 50% chance on connect |
| **Windswept** (Air) | Sandglass Dunes, Sunscour Wastes, Tombwind Heights, Skyreach Spires, Stormcrown Summit | STAT speed | +4% → +20% speed |

### 1b. Dungeon affixes: 12 (one per dungeon theme)

On Rare enemies and the Demon only. Each one is the dungeon's signature effect in a single lever.

| Region | Dungeon | Affix | Lever |
|---|---|---|---|
| Forest | The Den | **Bloodscent**: 25% Bleed on connect | ON HIT (REND) |
| Forest | Hedge Labyrinth | **Turnabout**: 25% resets your combo on connect | ON HIT (DISRUPT) |
| Forest | Dense Brush | **Overgrowth**: +8% max HP (was Entangle, a 25% Slow: in a poison dungeon the slow added ticks, +18% HP cost per fight) | STAT HP |
| Water | Mirror Pond | **Leeching**: heals 25% of damage dealt | ON HIT (DRAIN) |
| Water | Hidden Spring | **Mineral**: 25% Acid on connect | ON HIT (acid) |
| Water | Water Temple | **Bulwark**: starts with 2 Fortify | START fortify |
| Fire | Ashen Land | **Smolder**: 25% Weaken on connect | ON HIT (HEX) |
| Fire | Fire Temple | **Zealot**: +25% damage below 50% HP | STAT (`IFSOURCEHEALTHBELOW`, as BERSERKER) |
| Fire | Lava Cave | **Molten Shell**: starts with 2 Harden | START harden |
| Air | Glass Desert | **Glare**: 25% Expose on connect | ON HIT (SUNDER) |
| Air | The Pyramids | **Embalmed**: 25% Poison on connect, 0.25% max HP per tick | ON HIT (poison) |
| Air | Windswept Mountains | **Thunderhead**: 10% Stun on connect | ON HIT (stun) |

The 16 existing prefixes (Undead, Giant, Blazing, …) stay a separate random roll and stay tags-only for now.

---

## 2. Room affixes

Two parts: give the authored terrain tags an effect (always on), and roll one extra affix per room.

### 2a. Terrain tags made real: 4 (authored on rooms today, read by nothing)

| Tag | Rooms tagged | Effect |
|---|---|---|
| **scorched** | 13 | The room's hazards also Burn |
| **flooded** | 11 | The room's hazards also Slow |
| **overgrown** | 17 | The room's hazards also Poison |
| **exposed** | 70 | Everyone crits one face lower (19-20) |

Activity tags (dormant / cycling / active → hazard frequency) and structure tags (elegant / dilapidated → level roll) already work and stay as they are.

### 2b. Rolled room affixes: 10, one per room at 40%

| Affix | Kind | Effect |
|---|---|---|
| **Volatile** | hazard | Hazards ×1.5 damage and one step more active (dormant → cycling → active) |
| **Smothered** | hazard | Hazards ×0.5 damage and one step less active |
| **Infused** | hazard | Hazard statuses become the region's element (Forest → Poison, Water → Slow, Fire → Burn, Air → Weaken) |
| **Restless** | hazard | Hazards can act one extra time per fight |
| **Sheltered** | hazard | Hazards hit only enemies |
| **Treacherous** | hazard | Hazards hit only the hero |
| **Crowded** | room | +1 enemy (cap 3) |
| **Lair** | room | Enemies roll as Rare (so they carry the dungeon affix) |
| **Haunted** | room | Prefix roll chance ×2 |
| **Garrisoned** | room | Enemies start with 1 Fortify |

Rooms.json enemy `weight` values are collected today but never used; with this pass they become the in-room spawn weights.

---

## 3. Home vs wandering rolls (90 / 10)

| Roll | 90% home | 10% wander |
|---|---|---|
| **Room** | one of the dungeon theme's 3 rooms (today: uniform, with replacement) | any of the other 33 region rooms, with its own hazards, tags and enemy list |
| **Enemy** | the room's enemy list (by `weight`) | any of the 108 regional enemies + 20 General wanderers |

- Wanderers take the current dungeon level ("appropriate level"); nothing is level-gated today, so every row qualifies.
- A wandering enemy **keeps its home identity**: its home region affix at the *current* region's rank, and its home dungeon affix if Rare. A Fire enemy that turns up in the Den still Kindles.
- The current 15% General-wanderer chance folds into the 10% enemy wander roll (Generals are in that pool), so off-pool enemies drop from 15% to 10%.

Expected wanderers per dungeon at L7+ (4 rooms, 2 enemies each): ~0.4 rooms and ~0.8 enemies.

---

## 4. Enemy identity and the Demon

### 4a. Enemy identity: one signature kit, one material per dungeon

Each dungeon gets 3 signature templates from the existing 15. Every regional enemy's 2 actions come from its dungeon's 3 (flavor names kept, aliases retargeted); its archetype decides which 2. Each dungeon also owns one class material (each of the 12 used once): enemies' weapons are made of it (no loot effect).

| Dungeon | Signature kit | Material | Read |
|---|---|---|---|
| The Den | REND, FRENZY, CRUSH | Bone | pack predators: bleed, flurries, maulings |
| Hedge Labyrinth | DISRUPT, HEX, CONCUSS | Shadow | tricksters that break your combo |
| Dense Brush | VENOM, ENSNARE, MEND | Willow | poison, roots, regrowth |
| Mirror Pond | ENSNARE, DRAIN, PIERCE | Silver | lurkers that hold and feed |
| Hidden Spring | ENSNARE, SCORCH, MEND | Crystal | cold, scalding vents, healing water |
| Water Temple | SUNDER, SHELL, CRUSH | Mithril | armored guards |
| Ashen Land | SCORCH, REND, HEX | Obsidian | burning scavengers |
| Fire Temple | SCORCH, SHELL, SUNDER | Steel | fire knights |
| Lava Cave | SCORCH, DRAIN, SHELL | Iron | molten shells and leeches |
| Glass Desert | PIERCE, ENSNARE, DRAIN | Glass | glare and sand |
| The Pyramids | CURSE, VENOM, MEND | Gold | curses, darts, undeath |
| Windswept Mountains | CRUSH, CONCUSS, FRENZY | Bronze | rockfall and storm |

Today 119 of the 216 regional action slots are off-signature (Water has SCORCH 8 times, Labyrinth is mostly DISRUPT). Archetype → slot rule: Brute / Knight / Warlord take the heavy or defensive template (CRUSH, SHELL, SUNDER), Assassin / Acrobat / Berserker the fast one (REND, FRENZY, PIERCE, VENOM), Sage / Trickster / Duelist the control one (HEX, DISRUPT, ENSNARE, CURSE, MEND, DRAIN).

Some flavor names are shared across dungeons, so retargeting needs a per-enemy override rather than editing a shared alias.

### 4b. The Demon: the dungeon's culmination

| Part | Normal enemy | Demon |
|---|---|---|
| Actions | 2 of the signature kit | 2 signature templates ("Infernal Rend", ...) + the merged room enemies' actions, capped at 5 |
| Affixes | region (+ dungeon if Rare) | region at rank +1, dungeon affix, Demon mark |
| Phase | none | at 50% HP: the third signature action joins its sequence and its region affix doubles |
| Name | flavor name | "Demon <Name>, Demon of <Dungeon>" (e.g. "Demon Vigil Alpha, Demon of the Den") |

Also fixes two boss gaps: the last room is always hostile (a 20% non-hostile roll could leave a dungeon with no boss), and at L1-2 (one enemy per room) the lone enemy is still made the Demon (no merge and no boss damage multiplier, as before).

---

## Counts

| Thing | Count |
|---|---|
| Region affixes | 20 (4 families × 5 ranks) |
| Dungeon affixes | 12 |
| Terrain room tags given effects | 4 |
| Rolled room affixes | 10 |
| Dungeon signature kits | 12 (3 templates each, from the existing 15) |
| Dungeon materials | 12 (each class material once) |
| Regional enemies retargeted | 108 enemies get a `kit` (137 of 216 slots change, including fixing duplicate templates) |
| New combat mechanics | 0 (new code is the affix loader / applier and the roll changes) |

## Balance

Every affix adds enemy strength, so curves will shift: Rare enemies and Demons most, Air (speed) and Water (slow) most among regions. Plan after implementing: one paired DUNGEONSIM measure per region theme, then re-solve the health bands (DUNGEONSIM uses the same generation path, so all of this is measured automatically).

## Implementation (2026-11-12)

- Data: `GameData/EnemyAffixes.json` (wander chances, region families, dungeon identities with signature / material / affix / Demon title, Demon config) and `GameData/RoomAffixes.json` (terrain tags, rolled affixes, element statuses). Enemies.json rows gain `kit` (template per action slot).
- Code: `Data/AffixDataLoader.cs`, `World/EnemyAffixRuntime.cs` (home affixes, Demon, on-hit / damage / speed / phase), `World/RoomAffixRuntime.cs` (terrain, rolled affix, hazard targets, enemy count). Hooks: `EnemyGenerationManager` (weighted home pick, wander, affixes, Haunted prefix chance, Crowded), `RoomGenerator` (room wander), `RoomLoader.CreateRoom` (room affixes), `Dungeon.Generate` (last room hostile, Demon), `CombatEffectsSimplified` (on-hit, fight-start statuses), `DamageCalculator` / `SpeedCalculator` (damage, speed), `ActionExecutionFlow.Selection` (exposed crit shift), `CombatTurnHandlerSimplified` (hazard targets).
- The 15% element-filtered General wanderer is gone; the 10% wander roll can bring any enemy into any room.
- Tests: `Tests/Unit/World/EnemyRoomAffixTests.cs` (suite `EnemyRoomAffix`).
- The Demon's material drop was dropped (2026-11-12, user call).

## Prefixes (2026-11-12)

The 16 `EnemyPrefixes.json` prefixes now do something. Each row has `effects` (affix levers) and a `description`; the prefix's tags also reach `StatusAffinity` (immunities / weaknesses), with rows added for venomous, armored, frozen and holy. New levers: `taken` (damage taken, optional hero weapon filter), `evasion` (hero hit threshold), `extraaction` (one more signature action), and speed / damage can be HP-gated.

| Prefix | Effect |
|---|---|
| Demon | Final-boss mark (mechanics come from the Demon build above) |
| Undead | +15% HP; immune to Poison and Bleed, 1.5x Burn |
| Skeleton | Takes 25% less from swords and daggers, 30% more from maces; undead immunities |
| Ghost | Takes 30% less from maces, swords and daggers (wands normal); undead immunities |
| Giant | +30% HP, +20% damage, 15% slower |
| Tiny | -25% HP, 20% faster, hero needs 1 higher to hit |
| Feral | 30% faster below half HP |
| Armored | Starts with 3 Fortify; 2x Acid |
| Ancient | +10% HP, one more signature action |
| Corrupted | 25% Weaken on hit |
| Blazing | 30% Burn on hit; immune to Burn |
| Frozen | 30% Slow on hit; takes 15% more damage, 2x Burn |
| Venomous | 30% Poison on hit (0.25% max HP per tick); immune to Poison |
| Hallowed | +15% HP, starts with 1 Harden; immune to Weaken |
| Cursed | 25% Expose on hit |

## UI

- Enemy panel (right side, STATUS EFFECTS): one purple row per affix (region, dungeon, prefix, Demon phase), full text on hover (`eafx:N`).
- Room rows in STATUS EFFECTS: terrain tags (Scorched, Flooded, Overgrown, Exposed), the rolled affix and "Wandering room", explained on hover (`RoomStatusEffects`).
- Combat log: the Demon's second phase prints when a hit drops it below half HP.

## Tools

- Action Lab and Settings quick sims (`BattleExecutor`) give single enemies their home affixes (`EnemyAffixRuntime.WithHomeAffixes`, rank from level). Fundamentals harnesses (FUNDMATRIX / FIGHTLOG) and `TestCharacterFactory` stay bare on purpose: they price actions against plain enemies.

## Poison pass (2026-11-12)

The enemy/environment report flagged 14 deadly enemies, nearly all Venom / Mend kits (Dense Brush, Pyramids). Enemy poison adds a % of max HP to every tick for the rest of the fight, so it grows with fight length and the damage knobs do not reach it. Changes: Venom kit 0.5% -> 0.25% per hit; affix poison now has its own amount (it was the generic 1% per application): Embalmed and Venomous 0.25%; Dense Brush's Entangle (slow) replaced by Overgrowth (+15% HP).

## Testing layers

`Scripts/enemy-env-report.py` runs one full-coverage DUNGEONSIM (every band midpoint, all three dungeons, every feature) and writes `enemy-env-report-<patch>-s<seed>.json`. The balance dashboard reads it as two layers with limits and Overview rows:

- **Enemies**: each enemy's HP cost per fight vs regular fights at its level, death rate vs the level's, hero / enemy actions; flagged deadly past +8 pts or 2x deaths, soft past -8. Demons vs other boss fights; prefix, region and dungeon affix tables.
- **Environments**: clear % by dungeon per band (spread within a band, limit 8 pts), each dungeon's mean gap to its band, room affix / terrain / wander tables.

Per-dungeon tuning knob: `enemySystem.dungeonScale` (damage / health per dungeon theme, by enemy home dungeon), solved by `Scripts/dungeon-calibrate.py` (damage within 0.5-2x, HP takes the rest).

### Dungeon pass after the band solve (2026-11-12)

The first dungeon calibration (affix-v44h) ran while several bands sat near 0% clear, so every dungeon looked alike there. After the band chain (affix-v44i3: mean clear gap 2.0 pts vs 20.1 on fastbot-div16, held-out), the real per-dungeon split showed: Den 93-96% vs Dense Brush 24-47% in the Forest bands, Pyramids ~30 pts under its band at L18. Dense Brush compounds: permanent Venom ticks, Ensnare's slow and extra HP all lengthen the fight that the poison scales with. Changes: Overgrowth +15% -> +8% HP; Tomb Creeper (the one deadly Pyramids enemy, +19.6) kit Venom/Mend -> Venom/Curse; then dungeon-calibrate re-run on the solved bands. Per-dungeon tallies now take the dungeon's theme from the game state when no fight was recorded ("?" was ~2% of dungeons and inflated dungeon spread).

affix-v44k (dungeon calibration on the solved bands + one damage pass) cut the mean dungeon spread in a band from 17.1 to 11.0 pts (held-out). Pyramids and Dense Brush still sat at the 0.5x damage/HP floor. The remaining deadly enemies were fast-tempo Venom Assassins: poison stacks per hit and lasts the fight, so attack rate multiplies it. Tomb Creeper (also tiny: evasion) -> Curse/Mend, Living Wall Crawler -> Ensnare/Mend; slow and normal-tempo Venom enemies keep Venom.
affix-v44l: deadly enemies 9 -> 5. Tomb Creeper stayed deadly without Venom (+13.7, 9.7x its level's death rate, 4.4 enemy acts per fight): it is the strongest fast Assassin in the region, so its tempo went fast -> normal. Ancient prefix (+8.8, over the limit): +20% -> +10% HP, keeps its extra signature action.

### Hazard poison (2026-11-12, after affix-v44m)

Tomb Creeper's deaths (14 of 18 in a dungeon's first two fights) came from the room, not the enemy: Tomb Entrance's Curse of the Tomb poisons all combatants, Tomb Creeper and Sarcopha Gus are undead (poison immune), and hazard poison used the generic 1% of max HP per application, which lasts the fight. Hero DoT in Tomb Creeper fights averaged ~1,900 (up to 14k, more than the hero's max HP) vs ~220 for the living Guardian Scarab in the same room. Fix: EnvironmentalActions.json `poisonPercent` (Curse of the Tomb, Pollen Cloud = 0.25, like enemy poison), and terrain / room-affix poison (overgrown, Forest element) defaults to 0.25. This is likely most of why Pyramids and Dense Brush sat at the 0.5x dungeonScale floor.

It was not: affix-v44o (hazard 0.25% plus the class solve) still had Pyramids at 49% vs 79% Desert at L18 with both scales on the 0.5 floor, and Pyramids hero DoT was 676 per fight vs 62 in Desert. The real cause (2026-11-13): `combat.enemyDotFollowsDamageCurve` multiplied every hero DoT tick, poison included, by `enemySystem.damageCurve` at the hero's level (x6.9 at L18, x47 at L58, x147 at L68). Poison is already a % of max HP, so a 0.25% Venom / Embalmed / hazard tick became ~12% of max HP at L58, and each application stacks for the fight. Fix: StatusEffectProcessor scales only flat DoT (burn / acid / bleed) by the curve, not poison. Paired probe on seed 13961, Pyramids clear L18 49 -> 57, L38 62 -> 79, L58 50 -> 74. Poison everywhere (Venom kits, Embalmed, hazards) got weaker, so the bands and dungeon scales are re-solved on top (affix-v44p damage, affix-v44q dungeons).
