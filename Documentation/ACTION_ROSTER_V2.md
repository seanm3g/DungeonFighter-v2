# Action Roster v2 — Every Mechanic Expressed

Draft, 2026-10-25. A proposal only: no JSON is changed.

[ACTION_ROSTER_PASS.md](ACTION_ROSTER_PASS.md) (v1) aimed for the *smallest* roster, about 60 actions. v2 aims for a roster where **every mechanic the engine supports is carried by at least one action**, so a player can find, see and build around all of them. Where v1 and v2 disagree, v2 wins. v1's layer model (L0–L7), its enemy section and its build matrix still apply.

★ marks a proposed new action. Names are placeholders.

## 1. Decisions applied

| # | Decision | What changes from v1 |
|---|---|---|
| 1 | **Flat weapon modifiers on common items and actions; % modifiers on rare and higher tiers** | Flat and % are separate quantities with their own ladders. SHARPEN, HONE, WHETSTONE, JUICED, BLUR and LOOSE GRIP come back as the **common** flat rows. STRONGER, RUSH and the other % actions are the **rare** rows |
| 2 | **Vulnerability is the one status** for "enemy takes more" | EXPOSE merges into VULNERABILITY (the reverse of v1). TAUNT, SKULL CRACK and ENEMY SUNDER/CURSE apply Vulnerability. The `expose` mechanic is retired and kept as an alias. Armor break folds in too (see section 9) |
| 3 | **Every bonus has an action at each of the four scopes: next action, turn, fight, dungeon** | The class stat actions for the fight (FIGHTER, DEEP BREATHING, QUIET MIND, PRE-FIGHT PLANNING) come back. Dungeon scope is no longer class-only. See section 2 for what each scope means |
| 4 | **No poison on Rogue. Rogue starts with multi-hit and dice roll bonuses** | POISON becomes a generic action. Rogue owns the multi-hit, HIT, CRIT and ACCURACY rows plus roll ops. BLEED moves to Rogue as a later payoff for multi-hit, which matches the tree's `r-cut`, `r-mastery` and `r-hemo`. MARK and CASH OUT stay as later Rogue tiers |
| 5 | **AMP belongs to Wizard first, Warrior second** | Wizard owns the AMP ladder. Warrior gets CRESCENDO★ (fight AMP that builds along a clean sequence) and FLOW STATE★ |

## 2. Scopes: what "next action / turn / fight / dungeon" means

The engine has these cadences (`ActionCadenceBundle`):

| Scope | Lasts for | When it differs from the others | Typical size |
|---|---|---|---|
| **This action** | Only the swing that carries it (`hero_action_*`) | It's part of the chassis: BERSERKER's +50% on this hit | Biggest |
| **Next action** | The next N strip actions (`ACTION` ×N) | Retriggers and echoes count as actions, so ECHO SPELL can spend a next-action buff twice in one turn | Large |
| **Turn** | Your next N turns (`TURN` ×N) | A turn can contain several actions (retrigger, echo, multi-hit), and a turn buff covers all of them | Large |
| **Fight** | The rest of this fight (`FIGHT`) | Pays off over about 6–12 enemy swings | Small, often stacking |
| **Dungeon** | The rest of this dungeon (`DUNGEON`) | Pays off across every fight. A full heal on level-up, clear or leave doesn't reset it | Smallest |

"Fight-scope class stat" in v1 meant actions like FIGHTER: +1 STR for the rest of this fight. Under decision 3, every stat has all four rungs.

**Exchange rate.** Size each rung from one value budget: next action ≈ turn ×1, fight ×4, dungeon ×12 (tune with the balance tools). FIGHTER and MIGHT are then both fair picks, not one dominating the other.

## 3. Self ladders: quantity × scope

Each row is one quantity. Each cell is the action that grants it at that scope. "Owner" is the class whose kit and tree lean on that row. Generic rows can drop for anyone.

| Quantity | Rarity | Owner | Next action | Turn | Fight | Dungeon |
|---|---|---|---|---|---|---|
| STR | — | **Barbarian** | HEAVE★ | EXTRA EFFORT | FIGHTER | MIGHT (MOMENTUM BASH = MIGHT I) |
| AGI | — | **Warrior** | SIDESTEP★ | REACTION TRAINING | DEEP BREATHING | CUNNING (DANCE = CUNNING I) |
| TEC | — | **Rogue** | DEAD EYE★ | TAKING AIM | QUIET MIND | CONCENTRATE (FOCUS = CONCENTRATE I; the old name collides with the Focus status) |
| INT | — | **Wizard** | RECALL★ | READ BOOK | PRE-FIGHT PLANNING | DUNGEON ANALYSIS (+3 tier added) |
| Weapon damage (flat) | Common | Barbarian lean | TEST EDGE★ | SHARPEN | HONE | WHETSTONE |
| Damage % | Rare | **Barbarian** | STRONGER (ACTION BONUS = ×3 actions) | BEAR DOWN★ | WARCRY (reworked to fight +damage %) | BLOODLUST★ |
| Weapon speed (flat) | Common | Warrior lean | QUICK HANDS★ | JUICED (BLUR = 2 turns; LOOSE GRIP = speed-for-damage trade) | LIMBER★ | FLEET★ |
| Speed % | Rare | **Warrior** | RUSH | SPRINT | MOMENTUM | MARATHON |
| AMP % | — | **Wizard** (Warrior 2nd) | EMPOWERED; CHANNEL (Wizard, AMP for speed) | ATTUNE★ | CRESCENDO★ (**Warrior**: AMP per consecutive connect) | LEY LINE★ |
| Multi-hit | — | **Rogue** | PHANTOM FOLLOW-UP | FLURRY★ | THOUSAND CUTS★ | PRACTICED BLADE★ |
| HIT threshold | — | **Rogue** | SURE STRIKE★ | STEADY HAND | FIGHT BONUS | MARKSMAN★ |
| CRIT threshold | — | **Rogue** | VITAL STRIKE★ | CRITICAL ATTACK (KEEN EYE = 3 turns) | CRIT FACES | KILLER INSTINCT★ |
| COMBO threshold | — | **Warrior** | SET UP★ | MEASURED CUT (CONCENTRATION = generic copy; MEASURED ADVANCE = 2 turns) | FLOW STATE★ | DRILLED★ |
| ACCURACY (flat roll +) | — | **Rogue** | LINE UP★ | — | ZEROED IN★ | — |
| CRIT MISS threshold | — | generic | SURE FOOTING★ (fewer fumbles) | — | — | — |
| Defense | — | Warrior / Barbarian | BRACE★ (temp HP / absorb) | HARDEN; PARRY (Warrior strike) | FORTIFY★ (armor stacks, per the registry's meaning) | TOUGHEN★ (max HP, **Barbarian**) |

Composite and trade versions sit on top of these rows as rare variants: LUCKY CHARM (HIT + CRIT), ENRAGE (+CRIT/−HIT, Berserker elites and Barbarian rare), LOOSE GRIP and CHANNEL.

## 4. Enemy-target ladder: the mirror of every self row

Every self quantity has an enemy-side debuff, so the player can answer any enemy with the same vocabulary. On enemies, these same rows are what they use against you (see v1's enemy section).

| Enemy quantity | Mechanic(s) | Action | Owner |
|---|---|---|---|
| Damage dealt | `weaken`, `enemy_next_action_damage` | WEAKEN; CHALLENGE (Warrior) | Warrior |
| Speed | `slow`, `enemy_next_action_speed` | SLOW; FROST BOLT (Wizard) | Wizard |
| Damage taken | `vulnerability` (EXPOSE merged) | VULNERABILITY; BASH (Barbarian); TAUNT (Warrior); SKULL CRACK (Barbarian mace) | Barbarian / Warrior |
| Multi-hit | `enemy_next_action_multihit` | PIN★ | Rogue |
| AMP | `enemy_next_action_amp` | DAMPEN★ | Wizard |
| Weapon damage | `enemy_weapon_damage` | DISARM★ | Warrior |
| Weapon speed | `enemy_weapon_speed` | TANGLE★ | generic |
| Attributes | `enemy_stat_bonus` (negative), `stat_drain` | SAP★ | Wizard |
| Accuracy / roll | `enemy_accuracy`, roll penalty | RATTLE★ | Rogue |
| Hit threshold | `enemy_hit_threshold` | FEINT★ | Rogue |
| Crit threshold | `enemy_crit_threshold` | UNNERVE★ (the counter to CONCUSS) | generic |
| Crit miss | `enemy_crit_miss_threshold` | GOAD★ (enemy fumbles more) | Warrior |
| Combo threshold | `enemy_combo_threshold` | OFF-BEAT★ | Warrior |
| Combo routing | `disrupt` | JAB; BLUDGEON (Barbarian) | generic |
| Dice | `disadvantage` | LUCK, enemy-target variant (was UNLUCK) | generic |
| Targeting | `confuse` | CONFUSE (restored; mostly enemy and boss use) | generic |
| Armor | `pierce` | MIGHTY SWING (PIERCE = generic variant) | Barbarian |
| Stun | `stun` | CONCUSS★ (on crit); BLUDGEON (the tree says it stuns) | Barbarian |
| Damage over time | `poison`, `burn`, `bleed` | POISON★ (generic), BURN★ (Wizard), BLEED★ (Rogue, more per extra hit) | — |
| Mark | `mark` | MARK★ | Rogue (later tier) |

## 5. Mechanic coverage map

Every mechanic id in `ActionMechanicsRegistry` and `Triggers.json`, with the action(s) that carry it. The status comes from the registry's own table: Good / Needs work / Cut.

| Mechanic | Registry status | Carried by |
|---|---|---|
| `hero_accuracy` | Good | LINE UP★, ZEROED IN★ |
| `hero_hit_threshold` | Good | SURE STRIKE★, STEADY HAND, FIGHT BONUS, MARKSMAN★ |
| `hero_combo_threshold` | Good | SET UP★, MEASURED CUT, FLOW STATE★, DRILLED★ |
| `hero_crit_threshold` | Good | VITAL STRIKE★, CRITICAL ATTACK, KEEN EYE, KILLER INSTINCT★ |
| `hero_crit_miss_threshold` | Good | SURE FOOTING★ |
| `enemy_accuracy` | Good | RATTLE★ |
| `enemy_hit_threshold` | Good | FEINT★ |
| `enemy_combo_threshold` | Good | OFF-BEAT★ |
| `enemy_crit_threshold` | Good | UNNERVE★ |
| `enemy_crit_miss_threshold` | Good | GOAD★ |
| `hero_stat_bonus` | Good | The four stat ladders (16 actions) |
| `enemy_stat_bonus` | Good | SAP★ |
| `hero_next_action_damage` | Good | STRONGER, ACTION BONUS |
| `hero_next_action_speed` | Good | RUSH, SPRINT, MOMENTUM, MARATHON |
| `hero_next_action_multihit` | Good | PHANTOM FOLLOW-UP, FLURRY★, THOUSAND CUTS★, PRACTICED BLADE★ |
| `hero_next_action_amp` | Good | EMPOWERED, CHANNEL, ATTUNE★, CRESCENDO★, LEY LINE★ |
| `hero_action_damage` | Good | BERSERKER, EXECUTE★, RELENTLESS★, EXPLOIT★ (all "this hit") |
| `hero_action_speed` | Good | QUICK HANDS★ variant "this swing"; PROWESS RUSH (hybrid) |
| `hero_action_amp` | Good | WILLFIRE (hybrid); LONG FORM★ |
| `enemy_next_action_damage` | Good | WEAKEN |
| `enemy_next_action_speed` | Good | SLOW |
| `enemy_next_action_multihit` | Good | PIN★ |
| `enemy_next_action_amp` | Good | DAMPEN★ |
| `hero_weapon_damage` | Good | TEST EDGE★, SHARPEN, HONE, WHETSTONE |
| `hero_weapon_speed` | Good | QUICK HANDS★, JUICED, BLUR, LOOSE GRIP, LIMBER★, FLEET★ |
| `enemy_weapon_damage` | Good | DISARM★ |
| `enemy_weapon_speed` | Good | TANGLE★ |
| `weaken` | Good | WEAKEN, CHALLENGE |
| `slow` | Good | SLOW, FROST BOLT |
| `vulnerability` | Good | VULNERABILITY, BASH, TAUNT, SKULL CRACK |
| `expose` | Needs work | **Retired**: alias to `vulnerability` (decision 2) |
| `harden` | Good | HARDEN, PARRY |
| `fortify` | Good | FORTIFY★ |
| `armor` (procs) | — | FORTIFY★ (same armor stacks) |
| `focus` | Good | CENTER★ (gain Focus; feeds `w-cadence`, `w-wind`, Silver LENS) |
| `confuse` | Good | CONFUSE |
| `stat_drain` | Good | SAP★ |
| `disrupt` | Good | JAB, BLUDGEON |
| `heal` | Good | COMBO END HEAL, AFTER MISS HEAL, CRIT MISS HEAL, ROLL FIFTEEN, NATURAL SEVEN, CRIT HEAL (elite/rare) |
| `max_health` | Good | TOUGHEN★ |
| `lifesteal` | **Cut** (but LEECHING CUT uses it) | LEECHING CUT. Un-cut it in the registry or rebuild LEECHING CUT on `heal` with a damage scale |
| `pierce` | Good | MIGHTY SWING, PIERCE |
| `poison` | — | POISON★ |
| `burn` | — | BURN★ |
| `bleed` | — | BLEED★ |
| `stun` | — | CONCUSS★, BLUDGEON |
| `mark` | — | MARK★, CASH OUT★ (consumes) |
| `reflect` | Needs work | THORNS★ (Barbarian: "take pain") |
| `self_damage` | Needs work | BLOOD OATH★ (Barbarian: pay HP, gain STR for the fight) |
| `cleanse` | Needs work | CLEANSE★ |
| `advantage` | Good | LUCK |
| `disadvantage` | Good | LUCK, enemy-target variant |
| `replace_next_roll` | Good | REWRITE FATE (18), LOADED DICE (12), REPLACE ROLL (20) |
| `crit_face_min` | Good | CRIT FACES |
| `exploding_dice` | Good | EXPLODING DICE |
| `salvage_miss` | Good | SALVAGE MISS |
| reroll (tree `w-flow`) | — | SECOND CHANCE★ (Rogue: reroll your next miss) |
| `strip_jump` / `combo_jump` | Good | STRIP JUMP |
| `strip_skip` / `skip` | Good | STRIP SKIP |
| `strip_repeat` | Good | STRIP REPEAT |
| `strip_loop` / `loop_chain` | Good | STRIP LOOP |
| `strip_stop` | Good | STRIP STOP (enemy/boss) |
| `strip_random` | Good | STRIP RANDOM |
| `strip_disable` | Good | STRIP DISABLE (enemy/boss; feeds CONCENTRATED CHAOS) |
| `strip_shuffle` / `shuffle` | Good | STRIP SHUFFLE (enemy/boss) |
| `strip_replace_next` / `replace_action` | Good | MISDIRECT, STRIP REPLACE |
| `retrigger_next` | Good | ECHO SPELL, RETRIGGER NEXT, GAMBLERS BLESSING (on crit) |
| `retrigger_opener` | Good | RETRIGGER OPENER, BARBARIAN RAGE |
| `retrigger_finisher` | Good | RETRIGGER FINISHER |
| `retrigger_slot` | Good | RETRIGGER SLOT |
| `grant_action` | — | MIMIC★ (copy the enemy's last action for the fight; uses the shared roster) |
| `grant_action_tag` | — | AVENGE (Warrior tag on all actions) |
| `opener` | Good | OPENER flag; OPENING GAMBIT★ (Warrior) |
| `finisher` | Good | AVENGE, CONCENTRATED CHAOS |
| `chain_length` | Needs work | LONG FORM★ (+AMP per filled slot); CONCENTRATED CHAOS (per empty/skipped slot) |
| `chain_position` / `chain_position_bonus` | Needs work | CRESCENDO★ (grows along the strip) |
| `modify_chain_position` | Needs work | **Define or retire.** No clear player-facing meaning yet |
| `grace`, `curse` | Needs work | **Define or retire.** No description or data uses either |
| `modify_room` | Needs work | FLIP ROOM SWITCH (restore as an environment action once implemented) |
| `silence`, `consume`, `multi_dice` | **Cut** | Not expressed (the registry cut them) |
| `multi_hit` (swing hit count) | Good | The chassis: STAB, RAPID STRIKE, the swarms |
| `acid` | — | CORRODE★ (turn a Poison stack into Acid; `b-venom` does this from the tree) |
| `stat_bonuses` | Needs work | The stat ladders (section 3) |
| `accumulations` | Needs work | No action uses it today. See "Accumulations" below |
| `threshold_bonus` | Needs work | TALLY★ and BODY COUNT★ threshold variants ("every N hits / kills") |
| `material scale` (CRISIS, TRIUMPH, RAGE, BEAT, DRAG, STREAK, GRAZE, SWING, TROPHY, CHAIN, LENS, CONTACT) | — | The 12 material actions (v1 L7 family) |
| stat/tag scaling (`IFATTR`, `IFACTIONHASTAG`, `IFCLASSTAG`) | — | The 12 hybrid actions (v1 L7 families) |

### Accumulations: "+X per thing that happened"

`AccumulationEntry` already supports a growing bonus per unit of a counter: hits landed, blocks, dodges, kills, damage taken, turns taken, combos used, self damage, health restored. No action uses it yet. It's the "Scale" half of the build grammar inside a single fight, so each counter gets one action:

| Counter | Action | Effect (sketch) | Owner |
|---|---|---|---|
| HitsLanded | TALLY★ | +1% damage per hit landed this fight (threshold variant: every 10 hits, +1 multi-hit) | Rogue |
| DamageTaken | GRUDGE★ | +damage per 10 damage taken this fight | Barbarian |
| SelfDamage | BLOOD OATH★ | Pay HP; +STR per HP paid, for the fight | Barbarian |
| Kills | BODY COUNT★ | +1 STR per kill this dungeon (threshold variant: every 5 kills, +1 strip slot for the fight) | Barbarian |
| Blocks | BULWARK★ | +damage per block this fight (uses the chassis `block` value) | Warrior |
| Dodges | RIPOSTE★ | +damage per dodge this fight. This avoids the new `ONDODGE` event if it reads the Dodges counter | Warrior |
| TurnsTaken | PATIENCE★ | +1 COMBO per 3 turns this fight | Warrior |
| CombosUsed | ENCORE★ | +AMP per combo this fight | Wizard |
| HealthRestored | RENEWAL★ | +AMP per 10 HP restored this fight | Wizard |
| Cadence counters (action/turn/fight/dungeon) | The scope ladders | Section 3 | — |

## 6. Trigger and filter coverage

Every WHEN event and filter the engine checks, with the action that teaches it. Item procs (`Triggers.json`) and tree nodes also use these; this table covers the actions.

### WHEN events

| Event | Action(s) | Owner |
|---|---|---|
| `ONCONNECT` / `ONANYHIT` | Most status strikes; STRIP JUMP; MISDIRECT; SALVAGE MISS | — |
| `ONHIT` / `ONNORMALHIT` | LEECHING CUT; BLEED★ (per hit) | Rogue |
| `ONMISS` | RECOVER★ (next action +2 HIT after a miss); STRIP STOP (boss) | Rogue |
| `ONAFTERMISS` | AFTER MISS HEAL | Rogue |
| `ONCRITICAL` / `ONCRIT` / `ONCRITICALHIT` | CONCUSS★; GAMBLERS BLESSING; CRIT HEAL | Barbarian / generic |
| `ONCRITICALMISS` / `ONCRITMISS` | CRIT MISS HEAL | Rogue |
| `ONKILL` | BARBARIAN RAGE; RETRIGGER OPENER | Barbarian |
| `ONCOMBO` / `ONCOMBOHIT` | ECHO SPELL; RETRIGGER FINISHER; STRIP REPEAT | Wizard / Warrior |
| `ONCOMBOEND` / `ONCOMBOENDED` | COMBO END HEAL | generic |
| `ONFIRSTHIT` / `ONFIRSTBLOOD` | OPENING GAMBIT★; STRIP SHUFFLE (boss) | Warrior |
| `ONTAKEHIT` / `ONRECEIVEHIT` / `ONHEROHURT` | RETALIATE★ (next action +damage after you're hit); THORNS★ | Barbarian |
| (enemy misses you) | RIPOSTE★. **Needs a new WHEN, e.g. `ONDODGE`**; the closest existing event is `ONTAKEHIT` | Warrior |
| `ONROLLVALUE` | ROLL FIFTEEN | Rogue |
| `ONNATURALROLL` | NATURAL SEVEN | Rogue |
| `ONEVEN` / `ONODD` | EVEN ODDS★ (bonus on even faces, a different one on odd) | Wizard |
| `ONROOMSCLEARED` / `ONROOMCLEARED` | TROPHY HUNTER★ (+damage per room cleared this dungeon; replaces ROOM CLEAR HEAL, which conflicts with the no-post-fight-heal rule) | Rogue |
| `ONHEALTHTHRESHOLD` | BERSERKER | Barbarian |
| `ONENEMYHEALTHTHRESHOLD` | EXECUTE★ (+damage below 25% enemy HP; pairs with Iron CRISIS) | Barbarian |
| `ONTURN`, `ONSLOW`, `ONFOCUS`, `ONCONSECUTIVEATTACK`, `ONATTACK` | Material mint events. Carried by DRUM CUT, GOLD ANCHOR, REFRACT, MITHRIL FLOW and UMBRA, which spend the mints | per material |
| `ONWIELD` / `ONEQUIP` | Items only (`WHILE_EQUIPPED` procs). Not an action event | — |

### Filters (IF…)

| Filter | Action(s) | Owner |
|---|---|---|
| `IFSOURCEHEALTHBELOW` / `IFCLUTCH` | BERSERKER | Barbarian |
| `IFSOURCEHEALTHABOVE` | PRISTINE FORM★ (+damage above 90% HP) | Warrior |
| `IFTARGETHEALTHBELOW` | EXECUTE★ | Barbarian |
| `IFTARGETHEALTHABOVE` | AMBUSH★ (+CRIT against a full-HP target) | Rogue |
| `IFTARGETUNDERDOT` | FESTER★ (+damage per DoT on the target) | generic |
| `IFSOURCEUNDERDOT` | PAIN TOLERANCE★ (+STR while you're under a DoT) | Barbarian |
| `IFTARGETSTATUS` | EXPLOIT★ (+damage against any debuffed target) | generic |
| `IFSOURCESTATUS` | SHIELD SLAM★ (+damage while Fortified; mirrors `w-phalanx`) | Warrior |
| `IFSAMESACTION` (sic, as spelled in code) / `IFMIRROR` | RELENTLESS★ (+damage when it repeats the last action) | Barbarian |
| `IFDIFFERENTACTION` / `IFSWITCHUP` | SWITCH-UP★ (+CRIT when different from the last action) | Rogue |
| `IFLASTENEMY` / `IFLASTSTAND` | LAST STAND★ (bonus against the last enemy in a pack) | Warrior |
| `IFTARGETHASTAG` | BANE★ (bonus against a tag, e.g. undead; uses `StatusAffinities`) | generic |
| `IFUNARMED` | BRAWLER★ (PUNCH builds) | generic |
| `IFSLOT` | OPENING GAMBIT★ (slot 1); CRESCENDO★ (by slot) | Warrior |
| `IFACTIONHASTAG`, `IFCLASSTAG`, `IFGEARHASTAG`, `IFATTR` | Hybrid actions; BARBARIAN RAGE's class-tag bonus | per class |

## 7. Class kits

Each class starts with its identity and gains the rest of its rows over time. "Start" means tier 1 in `ClassActions.json` plus the weapon pool.

| Class | Start (tier 1–2) | Mid (tier 3–4) | Late (tree / capstone) |
|---|---|---|---|
| **Barbarian** (STR, damage %, flat weapon damage, take pain) | SLAM, POUND · HEAVE★, EXTRA EFFORT · STRONGER · SHARPEN | FIGHTER, BEAR DOWN★, WARCRY · BASH, MIGHTY SWING · BERSERKER, RETALIATE★, RELENTLESS★ · BLOOD OATH★ | MIGHT, BLOODLUST★, TOUGHEN★ · CONCUSS★, EXECUTE★, THORNS★, PAIN TOLERANCE★ · BARBARIAN RAGE |
| **Warrior** (AGI, speed %, combo, defense, AMP 2nd) | STRIKE, SLASH · SIDESTEP★, REACTION TRAINING · RUSH, SPRINT · MEASURED CUT | DEEP BREATHING, MOMENTUM · SET UP★, FLOW STATE★ · HARDEN, PARRY, FORTIFY★, CENTER★ · CHALLENGE, TAUNT · OPENING GAMBIT★ | CUNNING, MARATHON · CRESCENDO★, DRILLED★ · RIPOSTE★, SHIELD SLAM★, PRISTINE FORM★, LAST STAND★ · RETRIGGER FINISHER · AVENGE |
| **Rogue** (TEC, multi-hit, hit/crit/accuracy, roll ops) | STAB, CUT · PHANTOM FOLLOW-UP, FLURRY★ · SURE STRIKE★, STEADY HAND · DEAD EYE★, TAKING AIM · LINE UP★ | QUIET MIND, THOUSAND CUTS★ · VITAL STRIKE★, CRITICAL ATTACK, FIGHT BONUS · LOADED DICE, SECOND CHANCE★, RECOVER★ · FEINT★, RATTLE★, PIN★ · MISDIRECT, SWITCH-UP★ | CONCENTRATE, PRACTICED BLADE★, MARKSMAN★, KILLER INSTINCT★, ZEROED IN★ · BLEED★, MARK★, AMBUSH★, TROPHY HUNTER★ · ROLL FIFTEEN, AFTER MISS HEAL · CASH OUT★ |
| **Wizard** (INT, AMP, roll rewriting, echoes, elements) | MAGIC MISSILE, BOLT · RECALL★, READ BOOK · EMPOWERED, CHANNEL | PRE-FIGHT PLANNING, ATTUNE★ · ECHO SPELL · FROST BOLT, BURN★ · REWRITE FATE, EVEN ODDS★ · DAMPEN★, SAP★ | DUNGEON ANALYSIS, LEY LINE★ · LONG FORM★ · CRIT FACES, EXPLODING DICE · STRIP SKIP, STRIP RANDOM · CONCENTRATED CHAOS |

Generic rows anyone can loot: weapon flat damage (TEST EDGE★ … WHETSTONE), weapon flat speed (QUICK HANDS★ … FLEET★), WEAKEN, SLOW, VULNERABILITY, POISON★, JAB, CONFUSE, LUCK, SALVAGE MISS, CLEANSE★, BRACE★, SURE FOOTING★, UNNERVE★, TANGLE★, FESTER★, EXPLOIT★, BANE★, BRAWLER★, MIMIC★, the strip ops, the heals, and the material and hybrid families.

## 8. Size

| Group | Rows |
|---|---|
| Today's rows that stay | 133 of 135. Only SELF TARGET (a target field) and SET BONUS (not implemented) stay cut. The L0 chassis (HAYMAKER, RAPID STRIKE, weapon starters), the 12 material and 12 hybrid actions and the 3 swarms carry over from v1 unchanged. Some rows survive only as tier variants of another action (MOMENTUM BASH = MIGHT I) |
| New ★ | 78 |
| **Total** | **211 rows**, against 135 today |

Compared with v1's ~60, the difference is the four-scope ladders (decision 3), the flat/% split (decision 1), the enemy-target mirrors, and one action per trigger, filter and accumulation counter.

Every new row is a simple instance of an existing mechanic: a cadence bundle, a trigger bundle or a status cell. That's data, not code. Code is needed only for the "Needs work" mechanics they lean on (`reflect`, `self_damage`, `cleanse`, `chain_length`, `chain_position`), one new WHEN (`ONDODGE` for RIPOSTE), and CASH OUT, MIMIC and INTERRUPT (v1).

Not every row needs to drop on day one. The class kits order the class rows; the generic rows can phase in by rarity.

## 9. Open items

- **Tree text vs action data** (unresolved). `b-might` says MIGHT is +1 STR for the dungeon, but the row is +3. `z-readbook` says READ BOOK is +3 INT for the dungeon, but the row is next-turn +5 INT. Recommendation: the trees unlock the **dungeon** rung (MIGHT, DUNGEON ANALYSIS) and their text matches the row, with READ BOOK staying the turn rung.
- **Rogue tree vs decision 4.** `r-edge` and `r-toxin` convert Poison into Bleed. With POISON generic, they still work as a cross-class payoff, but Rogue's own kit no longer feeds them. Consider retargeting them to "per extra hit".
- **Armor break.** With EXPOSE retired, `r-hemo` ("if already Exposed, add Armor Break") needs rewording against Vulnerability.
- **`lifesteal` is marked Cut in the registry** while LEECHING CUT uses it.
- **`grace`, `curse`, `modify_chain_position`** have no definition. Define them or drop them from the registry.
- **Scope pricing.** The four-rung ladders only work if each rung is sized from the same budget (section 2). Pin the exchange rate with BUILDLAB before tuning the rows one by one.
