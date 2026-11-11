# Build System — Condensed

Status: **design proposal, condense pass 1; open questions decided** (2026-11-11, see section 12). Source: [BUILD_SPACE_EXHAUSTIVE.md](BUILD_SPACE_EXHAUSTIVE.md)
(40 single-mechanic builds, 50 pair builds). Nothing here is implemented or balanced.

## The reduction

| | Brainstorm | Condensed |
|---|---|---|
| Actions | 176 distinct names | **8 verbs**, 19 new presets + the 18 existing material converts + existing basics |
| Skills / rules | 68 bespoke rules | **8 rule families**, ~24 rules spread over the class trees |
| Items | ~230 named items/affixes | **material × slot × affix**: 5 slot roles, 11 affix templates |
| New stores / currencies | dozens implied | **one store model** covering mints, statuses and hero resources |
| New trigger events | ad hoc | **6 new events** on top of the existing WHEN list |

The trick: nearly every brainstorm piece was one of a few shapes (charge then spend, next-action modifier,
"per X" scaling, convert A to B, "X counts as Y", "survives the fight"). Each shape becomes one primitive, and
**materials and triggers supply the variety** instead of hundreds of unique actions.

---

## 1. The spine: Events → Stores → Verbs

```
 TRIGGER EVENTS                 STORES                         VERBS (actions)
 (what happened)                (what accumulated)             (what you do with it)

 ONCONNECT ─┐   material   ┌─► CONTACT / GRIT / ...  ─┐
 ONCRITICAL ┼── listens ───┼─► GRAZE                  ├──► CASH     (spend for effect)
 ONTAKEHIT ─┤   (worn set) ├─► RAGE / FRAY            ├──► CONVERT  (store A → store B)
 ON_TURN ───┤              ├─► BEAT / ENIGMA          ├──► SCALE    (hit grows with store, no spend)
 ...        ┘              │                          │
 hit with affix / APPLY ───┴─► Poison, Bleed, Burn... ┘
                                     │
                                     └─► store events (ON_CASH, ON_STORE_FULL) feed back into listeners
```

- **Events** are the existing trigger WHEN list plus the material synthesis events.
- **Stores** are anything that accumulates: the 18 material mints, enemy statuses, hero resources.
- **Verbs** are the 8 action shapes. Most new actions read *a store* rather than having their own mechanic.
- **Materials** are the wiring. Each material is a link: *event → store*. Wearing more pieces strengthens the
  link (2 / 3 / 5 thresholds already exist: unlock / +feed / ×feed).
- **Skills** rewire the links (alias events, count stores as other stores, promote scope, cross-wire materials).
- **Combo** order decides which events fire when, so the strip is literally *an event generator* that the worn
  materials listen to.

The build question becomes: **which events does my strip emit, which stores do my materials fill from them, and
which verbs spend those stores?**

---

## 2. Events

### Existing (keep as is)

Trigger WHEN list: ONCONNECT, ONCRITICAL, ONFIRSTHIT, ONCOMBO, ONMISS, ONTAKEHIT, ONAFTERMISS, ONCOMBOEND,
ONKILL, ONROOMSCLEARED, ONCRITICALMISS, ONEVEN, ONODD, ONNATURALROLL:n, ONROLLVALUE:n, WHILE_EQUIPPED.

Material synthesis events: ON_ENEMY_HEALTH_THRESHOLD, ON_COMBO_END, ON_RECEIVE_HIT, ON_TURN, ON_SLOW,
ON_CONSECUTIVE_ATTACK, ON_CRITICAL, ON_ATTACK, ON_KILL, ON_COMBO, ON_FOCUS, ON_CONNECT, ON_FIRST_HIT,
ON_AFTER_MISS, ON_ODD, ON_TAKE_HIT.

(These two lists overlap with different spellings; worth merging into one enum during implementation.)

### New (6)

| Event | Fires when | Why it's needed (brainstorm sources) |
|---|---|---|
| `ON_STATUS_APPLY:<status>` | you apply a status | Blood Frenzy / Pinning Drill / Toxic Insight / Kindled Mind already need it (StatusEffectApplied nodes); ON_SLOW becomes a special case |
| `ON_CASH:<store>` | a store is spent | the key cross-wire: spending one material's mint can mint another. Covers Payday, Assayer, INVEST, Spite chains |
| `ON_STORE_FULL:<store>` | a store reaches its cap | Daze → stun, Apex, Pure Blood, Living Alloy-style "at N, do X" |
| `ON_BEAT:<n>` | every n-th hero turn | Downbeat, Polyrhythm, every-4th-hit rules, Clockwork Numbers |
| `ON_SELF_COST` | you pay HP, condition or a self-status | Martyr builds, Scarred Gauntlets, Covenant |
| `ON_ENEMY_ACT` | the enemy takes an action | Bleed's "ticks on their actions", Molasses, Tourniquet, Riposte |

---

## 3. Stores (one model for mints, statuses and resources)

Every store has the same fields, so every verb, affix and rule can target any of them:

| Field | Meaning |
|---|---|
| `owner` | hero or enemy |
| `sources` | events that add to it (set by materials, affixes, actions, rules) |
| `cap` | max stacks (affixes and rules raise it) |
| `decay` | per turn / per action / never |
| `scope` | ACTION / TURN / FIGHT / DUNGEON: what survives |
| `tick` | optional per-event effect (Poison per turn, Bleed per enemy action) |
| `cash` | default effect when spent (damage, heal, speed, AMP…) |

### Store list

**Material mints (hero, exist)**: 18, one per material.

| Class | Material → event → store | Convert action (= CASH preset) |
|---|---|---|
| Barbarian | Iron: enemy HP threshold → CRISIS · Steel: combo end → TRIUMPH · Bone: receive hit → RAGE | IRON CULL · STEEL CREST · BONE WRATH |
| Warrior | Bronze: turn → BEAT · Gold: slow → DRAG · Mithril: consecutive attack → STREAK | DRUM CUT · GOLD ANCHOR · MITHRIL FLOW |
| Rogue | Glass: critical → GRAZE · Shadow: attack → SWING · Obsidian: kill → TROPHY | GLASS NICK · UMBRA · HEADHUNT |
| Wizard | Willow: combo → CHAIN · Silver: focus → LENS · Crystal: connect → CONTACT | WILLOW LASH · REFRACT · RESONATE |
| Classless | Stone: connect → GRIT · Unknown: turn → ENIGMA · Strange: first hit → ODDITY · Celestial: after miss → OMEN · Wood: odd → KINDLING · Cloth: take hit → FRAY | LANDSLIDE · REVEAL · QUIRK · STARFALL · KINDLE · UNRAVEL |

**Enemy statuses (exist, now treated as stores)**: Poison, Bleed, Burn, Slow, Weaken, Expose, Daze (new; full
→ Stun), Mark (new; stores a share of damage dealt, cash = release it).
Merge proposal: **Vulnerability folds into Expose**; **Armor Break becomes Expose's second tier** (Expose at cap
also strips armor). Fewer statuses, same build space (Breaker, Can Opener, Hemorrhage all still work).

**Hero resources**: Guard (Harden + Fortify), Temp HP, Focus (exists), AMP (exists), Banked Roll (new: PALM,
THROW THE MATCH, Fate Debt), Dungeon Stacks (new: the snowball store), Swarm (bodies; exist as actions today),
Debt (a negative store: LOAN, Fate Debt).

---

## 4. Verbs: actions condensed to 8 shapes

Every action is one verb plus parameters. Shared fields on every action:
`length`, `hits`, `damage`, `tags`, **`cost`** (time / HP / store / item condition), **`condition`** (store ≥ n,
HP band, enemy prefix, slot position), **`attune`** (which store it reads; default = your *attuned* material, below).

### Attunement: the material hook for generic actions

Your **attuned material** is the material you wear the most pieces of (ties go to the weapon's material). Generic
presets resolve against its store. One action, different meaning per material:

- `RECKONING` with Bone attuned is GRUDGE; with Celestial it's TANTRUM; with Iron it's LAST STAND; with Bronze
  it's CADENCE BREAK; with Willow it's CHAIN SNAP; with Obsidian it's the Collector payoff.
- `CASH IN` with any attunement is that material's convert action at 75%, freeing you from slotting a
  material-specific convert (useful for mixed sets).

This is what makes materials matter to *every* action, not only to their own convert.

### The 8 verbs and the preset catalog

| Verb | Shape | Existing presets | New presets | Replaces (brainstorm) |
|---|---|---|---|---|
| **STRIKE** | deal damage; shape from hits / length | PUNCH, STAB, SLASH, HAYMAKER, FLURRY-type basics | — | CATACLYSM, FLURRY, RICOCHET, JAB variants, SMITE (= STRIKE + prefix condition), GUILLOTINE / FROM THE SHADOWS / CODA (= STRIKE + position condition from items/skills) |
| **PREP** | modify the next action(s) | WIND UP, MEASURED CUT, CRIT FACES, LOADED DICE | `QUICKSTEP` (speed), `HONE` (crit), `STEADY` (accuracy), `PRELUDE` (AMP for rest of combo), `BLOOD OATH` (any PREP paid in HP) | HEFT, BACKSWING, DOUBLE TIME, HONE THE EDGE, STEADY, KEY CHANGE, RUSH NOTE, BRACE, PENANCE, SYNCOPATE, FERMATA, LINK, CATALYST, TEMPER, NUDGE |
| **APPLY** | add stacks to a store | KINDLE, WEAKEN, SLOW, EXPOSE, BLUDGEON, LEECHING CUT, HARDEN, swarms | `MARK`, `BARRIER` (temp HP) | VENOM COAT, OPEN VEIN, STOKE, MOLASSES, SUNDER, DAZE, DEEP MARK, HEX, SELF IMMOLATE, FEED THE SWARM, ABSORB |
| **CASH** | spend a store for its cash effect | the 18 material converts | `DETONATE` (spend the target's largest status), `CASH IN` (attuned mint at 75%), `LIQUIDATE` (every store at 50%), `COMMAND` (spend Swarm), `SACRIFICE` (spend an item) | PURGE, FLASHPOINT, CRACK OPEN, SHIELD SLAM, SHIELD BASH, PUNISH, HARVEST, TRANSFUSION, mark detonate, SCATTER, OVERSTRAIN |
| **CONVERT** | move store A → store B | — | `TRANSMUTE` (by the conversion table), `DISTILL` (enemy store → hero store), `INVEST` (store → delayed ×2) | SAP, ASSAY, REACTION, HEDGE, LOAN, TITHE, CAUTERIZE, TRANSFER MARK, MEND |
| **SCALE** | damage grows with a store, without spending it | — | `RECKONING` (attuned store), `INDEX` (filled slots / distinct materials) | GRUDGE, TANTRUM, LAST STAND, CADENCE BREAK, CHAIN SNAP, SECOND WIND STRIKE, UNDERTOW, TROPHY CUT, CRESCENDO, SURE STRIKE, ASSAY (rainbow) |
| **ROUTE** | change the strip | STRIP JUMP / SKIP / LOOP / STOP / RANDOM / SHUFFLE / REPLACE, RETRIGGER NEXT / OPENER / FINISHER / SLOT, ECHO SPELL, PHANTOM FOLLOW-UP | `REVERB` (echo the rest of the combo at 25%), `RECURSE` (loop previous 2) | GEAR SWAP, CATCH, ENCORE, APPENDIX, RETRIGGER variants |
| **FATE** | change dice | REPLACE ROLL, EXPLODING DICE, REWRITE FATE, SALVAGE MISS | `PALM` (bank this roll as a Banked Roll store) | SWAP FATE, CALL THE NUMBER, THROW THE MATCH, PRATFALL, RECKLESS SWING (= STRIKE with accuracy cost) |

New presets: QUICKSTEP, HONE, STEADY, PRELUDE, BLOOD OATH, MARK, BARRIER, DETONATE, CASH IN, LIQUIDATE, COMMAND,
SACRIFICE, TRANSMUTE, DISTILL, INVEST, RECKONING, INDEX, REVERB, RECURSE, PALM = **20** (BLOOD OATH could instead
be a `cost` option on any PREP, making it 19).

Rule of thumb for future content: **a new action must be a new verb or a preset that serves 2+ builds**. If it
only serves one build, it's probably an item affix or a skill rule.

---

## 5. Items: material × slot × affix

An item already has a material (sets, the event → store link) and rolled mods. The condensed rule:

> **Material** decides *which store* the piece feeds. **Slot** decides *which event family* the piece can listen to
> and which affixes it can roll. **Affix** is one of 11 templates.

### Slot roles (the event families)

| Slot | Event family it owns | Affix leaning |
|---|---|---|
| Weapon | hit events: ONCONNECT, ONFIRSTHIT, ONKILL, per-hit | on-hit status, hit shape, grants a preset, counts double for its material |
| Head | dice events: ONCRITICAL, ONMISS, ONCRITICALMISS, ONNATURALROLL, ONODD / ONEVEN | crit / accuracy / fate, information (show counters) |
| Body | defense events: ONTAKEHIT, ON_SELF_COST, ON_STATUS_APPLY (on you) | store caps, Guard / Temp HP, carry |
| Legs | combo events: ONCOMBO, ONCOMBOEND, ON_CONSECUTIVE_ATTACK, ON_CASH | combo threshold, position (opener / finisher) |
| Feet | time events: ON_TURN, ON_BEAT, ON_ENEMY_ACT, ONROOMSCLEARED, ON_SLOW | speed, cadence, context (regions, hazards) |

### The 11 affix templates

| # | Template | Form | Brainstorm items it absorbs |
|---|---|---|---|
| 1 | **Listener** ★ | this piece's material store also fills on `<event from the slot's family>` | Jester's Bells, Cracked Mirror, Scarred Gauntlets, Bone-mints-on-miss (Spite), Pendulum Weight |
| 2 | **Tuning** | store S: +cap / no decay / +1 per gain | Vial Bandolier, Vault Belt, Capacitor Plate, Hive Charm, Cinder Wand |
| 3 | **On-hit** | hits apply status S | Poisonous, Serrated, Flaming (exist), Crowbar, Concussion Maul |
| 4 | **Condition** | while <store ≥ n / HP band / prefix / status on target>, +X | Steel Knuckle-likes, Bloodied Helm, Lead Chain, Silvered, Earplugs, Butcher's Apron |
| 5 | **Counts-as** | counts as material M too / counts double / counts for rarity | Mixed Ingot, Ancestral Weapon, Provenance |
| 6 | **Tilt** | +A, −B | Ponderous, Featherweight, Tower Plate, Twin Daggers, Tome, Headsman's Axe |
| 7 | **Grant** | adds a preset to the pool, attuned to this piece's material | Plague Mask / Bounty Ledger style uniques |
| 8 | **Position** | opener / finisher / slot n gets +X | Assassin's Hood, Headsman's Axe, Bookend Pair |
| 9 | **Carry** | 25% of store S survives the fight | Vault Belt, Trophy Necklace, Momentum Carry-lite |
| 10 | **Cost** | actions cost X (HP / condition / store), +Y | Martyr's Brand, Hair Shirt, Glass Dagger |
| 11 | **Condition-band** | +X per worn piece in condition band B | Patchwork Armor, Display Case (Scrap / Pure Prefer as items) |

★ **Listener is the main material-trigger hook.** A Bone chest with *Listener: ONMISS* makes misses mint RAGE;
Glass boots with *Listener: ON_BEAT* make every 4th turn mint GRAZE. Because slot limits the event family, a set of
five pieces can wire up to five extra events into its stores. That is where most pair builds now come from,
**without new actions**.

Named uniques from the brainstorm (*Plague Mask*, *Glass Dagger*, *Mirror Cloak*…) become fixed material + slot +
two templates. Example: *Glass Dagger* = Glass weapon + Tilt (crit ×2 / −condition per crit) + Counts-as (rerolls into
a new Glass item at Broken).

---

## 6. Skills: 8 rule families

Skills are the only things allowed to **change how other systems are read**. Every brainstorm rule fits one of
eight families:

| Family | Form | Rules (brainstorm → kept) |
|---|---|---|
| **Alias** | event A also counts as event B | Spite (miss ≡ hit taken), Harmonics (echoes fire ONCONNECT), Hive Mind (swarm hits ≡ your hits), Chaos Theory (enemy miss ≡ your Focus event) |
| **Counts-as** | store / tag / material equivalence | Living Alloy (exists), Etymology (materials ≡ action tags), Glutton (dungeon stacks ≡ set pieces), Provenance, Level 1 class-tag rules (exist) |
| **Exemption** | X doesn't break / doesn't count | Fail Forward, Margin Notes, Bookmark, Escapement, Hold the Line, Unhurried |
| **Ratio** | conversion-table rows and their rates | Periodic Table, Transmutation, Compound Interest, Envenomed Edge / Thermal Shock / Venom Mace (exist) |
| **Persistence** | promote a store's scope (TURN → FIGHT → DUNGEON) | Virulence, Wildfire, Contract, Brood, Momentum Carry, Earthshaker: **one rule per class**: "Carry <class's stores>" |
| **Threshold** | `ON_STORE_FULL` → effect | Daze → Stun, Apex, Pure Blood, Diminishing Returns |
| **Coupling** | primary stat → store gain | Synthesis masteries (exist), Archmage, Queen |
| **Cross-wire** ★ | `ON_CASH:<A>` mints `<B>`, or A's source events also feed B | new: the engine for 2+3 and 3+2 sets |

★ **Cross-wire is the second material-trigger hook.** It turns a two-material set into an engine: cashing one
store fills the next. Proposed one cross-wire per material pair inside each class (3 per class, 12 total) plus a few
classless ones:

| Class | Cross-wires |
|---|---|
| Barbarian | cashing RAGE mints CRISIS (pain becomes execution) · TRIUMPH spend restores Guard (Bone ← Steel) · CRISIS kills mint TRIUMPH |
| Warrior | every 4th BEAT applies Slow → DRAG (Bronze → Gold) · DRAG spend keeps STREAK from resetting · STREAK ≥ 5 counts as BEAT |
| Rogue | GRAZE spend applies Mark · TROPHY on a marked kill refunds GRAZE · SWING ticks Bleed on Exposed |
| Wizard | CHAIN spend grants Focus → LENS · LENS spend adds CONTACT per slot · CONTACT on Burning adds CHAIN |
| Classless | OMEN (after miss) spend → Banked Roll · FRAY (take hit) spend → Temp HP · KINDLING (odd) also applies Burn · ODDITY (first hit) re-arms on combo restart · GRIT spend → Guard · ENIGMA spend → random ROUTE |

### Per-class shape

Each class tree keeps its current layout (Core + 3 material branches + Rite + Confluence) but each branch gets:
one **Coupling** (exists as Synthesis), one **Threshold** or **Condition**, and one **Cross-wire**. The class's
**Persistence** rule is its capstone. Class identity then reads from which event families its materials listen to:

| Class | Event families | Store emphasis | Persistence capstone |
|---|---|---|---|
| Barbarian | defense + HP thresholds | RAGE, CRISIS, Guard, Daze | RAGE carries between fights |
| Warrior | time + combo | BEAT, STREAK, Slow / DRAG, Weaken | STREAK and Focus carry |
| Rogue | dice + hits | GRAZE, Bleed, Poison, Mark, Expose | Marks / statuses jump to the next enemy (Virulence + Contract) |
| Wizard | combo + status apply | CHAIN, LENS, Burn, AMP | AMP and CHAIN carry into the next opener |

Hybrids (Warbrute, Ravager, Ragecaller, Duelist, Spellblade, Hexblade) get the cross-wires between their two
classes' materials. Example: Hexblade = Ratio family (it owns the full conversion table).

---

## 7. Combo grammar

The strip doesn't need new structures; it needs to be read as an event generator.

- **Slot emits events**: a 3-hit action at slot 4 emits ONCONNECT ×3, ONCOMBO, maybe ONCRITICAL. Materials pick
  which of those count.
- **Position** lives on items (Position affix) and skills, not on unique actions (no GUILLOTINE / CODA / FROM THE
  SHADOWS; they're STRIKE + a finisher / opener bonus).
- **Charge → Spend order** is the main combo skill: APPLY / PREP early, CASH / SCALE late, ROUTE to repeat the
  payoff.
- **Breakage** (a miss ends the combo) stays; Exemption rules and Listener-on-miss turn it into a resource.
- Existing pieces cover the rest: combo threshold (Cadence rules), strip ops, retriggers, AMP per slot.

---

## 8. The conversion table

One data table instead of bespoke conversion skills. Rows are owned by rules (Ratio family) or items.

| From | To | Base ratio | Owner (existing or proposed) |
|---|---|---|---|
| Poison | Bleed | 1 : 2 | Envenomed Edge (exists) |
| Poison | Acid / tick now | 1 : 1 | Venom Mace (exists) |
| Burn + Slow | Stun | both : 1 | Thermal Shock (exists) |
| Burn on Slowed | Expose | 1 : 1 | Thermal Shock (exists) |
| Bleed tick | Expose (then armor strip) | 1 : 1 | Hemorrhage (exists) |
| Harden | slot repeat (on crit) | 1 : 1 | Living Alloy (exists) |
| Weaken | hero STR | 1 : 1 | SAP-type rule |
| Temp HP (lost) | Guard | 2 : 1 | Conservation |
| Overheal | Temp HP | 1 : 1 | Overflow |
| Resisted stun | Weaken | 1 : 2 | Diminishing Returns |
| Pierce overflow | Bleed | 5 : 1 | Through and Through |
| any status | matching mint | 3 : 1 | Assay (DISTILL default) |
| Enemy Poison | hero Focus | 3 : 1 | DISTILL |
| Enemy Burn | hero AMP | 1 : 2% | DISTILL / Ember Robe |
| Enemy Bleed | heal | per tick | Blood Chalice / Blood Draught (exists) |

`TRANSMUTE` uses whichever rows the hero owns; unowned rows convert at half rate.

---

## 9. All 40 mechanics on the spine

| Id | Mechanic | Material home(s) | Store | Key events | Verbs / presets | Item affix | Skill family |
|---|---|---|---|---|---|---|---|
| M01 | Heavy Hit | Iron, Stone | CRISIS, GRIT | ONCONNECT, enemy HP threshold | WIND UP, STRIKE (long) | Tilt | Coupling (Titan) |
| M02 | Tempo | Mithril, Bronze | STREAK, BEAT | consecutive attack, ON_TURN | QUICKSTEP, RECKONING | Tilt, Feet Listener | Exemption (Unhurried) |
| M03 | Multihit | Shadow, Stone, Crystal | SWING, GRIT, CONTACT | ONCONNECT per hit | STRIKE (hits) | On-hit | Alias (swarm hits) |
| M04 | AMP | Willow, Silver | CHAIN, LENS, AMP | ONCOMBO, ON_FOCUS | PRELUDE, RECKONING, REVERB | Position, Tuning | Coupling (Archmage) |
| M05 | Crit | Glass | GRAZE | ONCRITICAL | HONE, CRIT FACES | Head Condition, Tilt | Threshold (Cascade) |
| M06 | Precision | Silver, Mithril | LENS, STREAK | ONCONNECT, ON_FOCUS | STEADY, MEASURED CUT | Head Condition | Exemption |
| M07 | Exact Numbers | Wood, Silver | KINDLING, LENS | ONODD/EVEN, ONNATURALROLL | PALM, existing number actions | Head Listener | Alias |
| M08 | Fate Control | Celestial, Silver | OMEN, Banked Roll | ONAFTERMISS | PALM, FATE presets | Head Tuning | Ratio (Fate Debt) |
| M09 | Failure | Celestial | OMEN | ONMISS, ONAFTERMISS | RECKONING (Celestial), SALVAGE MISS | Head Listener (miss) | Exemption (Fail Forward) |
| M10 | Chain Length | Willow | CHAIN | ONCOMBO | RECKONING (Willow), PREP | Legs Tuning | Exemption |
| M11 | Opener | Strange | ODDITY | ONFIRSTHIT | STRIKE + Position | Position (opener) | Cross-wire (re-arm) |
| M12 | Finisher | Steel | TRIUMPH | ONCOMBOEND | CASH in last slot | Position (finisher) | Threshold |
| M13 | Strip Manipulation | Unknown | ENIGMA | ON_TURN, ON_CASH | ROUTE presets | Legs Condition | Exemption (Escapement) |
| M14 | Echo | Crystal, Willow | CONTACT, CHAIN | ONCONNECT | REVERB, ECHO SPELL | Grant | Alias (Harmonics) |
| M15 | Tag Grammar | Willow, Shadow | CHAIN, SWING | ONCOMBO | any (tags) | Counts-as | Counts-as (Etymology) |
| M16 | Poison | Iron (Venom Mace) | Poison | ON_STATUS_APPLY | APPLY, DETONATE | On-hit, Tuning | Persistence (Virulence) |
| M17 | Bleed | Obsidian | Bleed | ON_ENEMY_ACT | APPLY, DETONATE | On-hit, Condition | Ratio, Threshold |
| M18 | Burn | Crystal, Wood | Burn, KINDLING | ON_STATUS_APPLY, ONODD | KINDLE, DETONATE | On-hit | Persistence (Wildfire) |
| M19 | Stun | Bone (Bludgeon line) | Daze | ON_STORE_FULL | BLUDGEON, APPLY Daze | On-hit (crit) | Threshold |
| M20 | Slow | Gold | DRAG, Slow | ON_SLOW, ON_ENEMY_ACT | SLOW, GOLD ANCHOR | Feet Condition | Cross-wire |
| M21 | Weaken | Gold | Weaken | ON_STATUS_APPLY | WEAKEN, CONVERT (SAP row) | On-hit | Ratio |
| M22 | Expose | Shadow, Glass | Expose | ON_STATUS_APPLY | EXPOSE, DETONATE | On-hit, Condition | Ratio (Hemorrhage) |
| M23 | Pierce | — (gap) | — | ONCONNECT | STRIKE param | Weapon Tilt | Ratio (overflow → Bleed) |
| M24 | Mark | Obsidian, Glass | Mark | ON_CASH | MARK, DETONATE | Condition | Persistence (Contract) |
| M25 | Disrupt | Unknown, Cloth | ENIGMA, FRAY | ON_ENEMY_ACT | CONFUSE, MISDIRECT, TAUNT | Body Condition | Alias (Chaos Theory) |
| M26 | Bulwark | Steel, Bone, Stone | Guard, GRIT | ONTAKEHIT | HARDEN, PARRY, CASH (Guard) | Body Tuning | Counts-as (Living Alloy) |
| M27 | Sustain | Bone, Crystal, Obsidian | (heal on events) | ONCONNECT | LEECHING CUT | On-hit heal | Ratio (Overflow) |
| M28 | Shield | Cloth | FRAY, Temp HP | ONTAKEHIT | BARRIER, CASH | Body Tuning | Ratio (Conservation) |
| M29 | Retaliation | Bone, Cloth | RAGE, FRAY | ONTAKEHIT | TAUNT, BONE WRATH, RECKONING | Body Listener | Alias (Spite) |
| M30 | Brink | Iron | CRISIS | HP threshold (hero mirror) | BERSERKER, RECKONING (Iron) | Condition (HP band) | Threshold |
| M31 | Snowball | Obsidian | TROPHY, Dungeon Stacks | ONKILL, ONROOMSCLEARED | RECKONING, CASH (HARVEST) | Carry | Persistence |
| M32 | Material Count | all | all mints | — | CASH IN, INDEX | Counts-as | Counts-as (Glutton) |
| M33 | Condition | (class prefers) | — | ON_SELF_COST | SACRIFICE, cost param | Condition-band, Cost | Counts-as (Provenance) |
| M34 | Mint & Spend | all | all mints | ON_CASH | INVEST, LIQUIDATE | Tuning, Carry | Cross-wire |
| M35 | Conversion | Crystal, Iron | statuses | ON_STATUS_APPLY | TRANSMUTE, DISTILL | Tuning | Ratio |
| M36 | Context | Unknown, Strange | ENIGMA, ODDITY | prefix / region | STRIKE + condition | Feet Condition | Alias |
| M37 | Swarm | — (gap) | Swarm | per body hit | swarms, COMMAND | Tuning | Alias, Persistence |
| M38 | Self-Cost | Bone, Celestial | RAGE, OMEN | ON_SELF_COST | BLOOD OATH, cost param | Cost | Ratio (Covenant) |
| M39 | Long Strip | Willow | CHAIN | ONCOMBO | INDEX, PRELUDE | Tilt (+slot) | Exemption (Margin Notes) |
| M40 | Cadence | Bronze, Unknown | BEAT, ENIGMA | ON_TURN, ON_BEAT | DRUM CUT, RECKONING (Bronze) | Feet Listener | Threshold |

**Gaps** (no material home): Pierce, Swarm. Either leave them as action parameters, or they're the natural
candidates if a new material is ever added (e.g. a "Chitin" material: ON_ENEMY_ACT → BROOD).

---

## 10. Rebuilding brainstorm builds from the condensed set

Each build below uses **only** the primitives above. No bespoke pieces.

### Hemomancer (M17 Bleed) — Rogue, Obsidian ×3 + Cloth ×2
- Strip: `LEECHING CUT → TAUNT → DETONATE`.
- Items: Obsidian dagger *On-hit Bleed*; Cloth body *Listener: ON_ENEMY_ACT* (FRAY now fills when the enemy acts);
  Obsidian legs *Condition: target Bleeding → +X*.
- Skills: Deep Cut / Blood Draught (exist); cross-wire *SWING ticks Bleed on Exposed*.
- Why it still feels like the brainstorm: Bleed ticks on enemy actions, TAUNT invites them, DETONATE cashes.

### Pendulum (P01 Heavy × Tempo) — Warrior, Mithril ×3 + Iron ×2
- Strip: `QUICKSTEP → HAYMAKER → QUICKSTEP → HAYMAKER`.
- Items: Iron weapon *Tilt: +damage per 1% slower*; Mithril feet *Listener: ON_TURN → STREAK*.
- Skills: Cadence Keeper (exists); cross-wire *STREAK ≥ 5 counts as BEAT*.

### Banker (M34) — any class, two-material set
- Strip: `(mint actions) → INVEST → CASH IN`.
- Items: *Tuning: cap +50%* on body; *Carry 25%* on feet.
- Skills: the class's cross-wire on cash (e.g. Barbarian: cashing RAGE mints CRISIS) makes INVEST → CASH IN a
  two-store engine.

### Lucky Fool (M09 Failure) — Rogue, Celestial ×3 + Shadow ×2
- Strip: `RECKLESS STRIKE (accuracy-cost STRIKE) → STAB → RECKONING` (attuned to Celestial, so it scales with OMEN).
- Items: Celestial head *Listener: ONCRITICALMISS*; Shadow legs *Position: finisher +X*.
- Skills: Second Chance (exists); classless cross-wire *OMEN spend → Banked Roll*, then `PALM` the banked roll.

### Drummer (M40) — Warrior, Bronze ×5
- Strip: `QUICKSTEP → QUICKSTEP → DRUM CUT` with `RECKONING` (Bronze → BEAT) as finisher.
- Items: Bronze feet *Listener: ON_BEAT:4 → +1 BEAT*; Bronze head *Condition: on beat, crit −2*.
- Skills: War Drum (exists); Threshold *BEAT full → next action free*.

### Scrapper (M33) — Barbarian, Bone ×3 + Iron ×2
- Strip: `BLOOD OATH (paid in item condition) → HAYMAKER → SACRIFICE`.
- Items: *Condition-band: +5% per Broken piece*; Bone body *Listener: ON_SELF_COST → RAGE*.
- Skills: Scrap Prefer (exists); Ratio *Salvage Rights*: SACRIFICE returns a material piece.

### Thermal Shock (P20 Burn × Slow) — Wizard, Crystal ×3 + Gold ×2 (or Spellblade)
- Strip: `KINDLE → KINDLE → SLOW → STRIKE`.
- Items: Crystal weapon *On-hit Burn*; Gold feet *Listener: ON_STATUS_APPLY:Slow → DRAG*.
- Skills: Thermal Shock (exists, a conversion-table row); Threshold *Daze full → Stun*.

---

## 11. What went where (brainstorm → condensed)

- **Charge → Spend actions** (PURGE, FLASHPOINT, CRACK OPEN, SHIELD SLAM, HARVEST, mark detonate, LIQUIDATE,
  COMMAND, SCATTER, TRANSFUSION) → `DETONATE` / `CASH IN` / `LIQUIDATE` / `COMMAND` on stores.
- **Next-action modifiers** (WIND UP family, ~15 names) → PREP presets with a `cost` field.
- **Per-X payoff actions** (~11 names) → `RECKONING` + attunement, or `INDEX`.
- **Positional uniques** (GUILLOTINE, CODA, FROM THE SHADOWS, CLASP, FIRST BLOOD) → STRIKE + Position affix / rule.
- **Status appliers** (VENOM COAT, OPEN VEIN, STOKE, SUNDER, DAZE…) → APPLY with stack parameters; most already
  exist as basic actions.
- **Conversion actions and skills** → the conversion table + TRANSMUTE / DISTILL / INVEST.
- **"Survives the fight" rules** (Virulence, Wildfire, Contract, Brood, Momentum Carry, Earthshaker, Vault Belt)
  → one Persistence rule per class + the Carry affix.
- **"Also counts as" rules** → Counts-as and Alias families.
- **~230 items** → material × slot × affix templates; uniques are fixed template combos.

---

## 12. Decisions (2026-11-11)

1. **Attunement → dominant material.** Generic presets read the material with the most worn pieces; ties go to
   the weapon's material.
2. **Status merge → both.** Vulnerability folds into Expose; Armor Break becomes Expose's second tier (Expose at
   cap also strips armor).
3. **Listener affixes → roll on loot only.** No reroll or crafting. They are the loot chase; build completion
   comes from set counts, market rules (Blood Market etc.) and skills, not from fixing an affix.
4. **Cross-wires → skills + rare items.** In-class cross-wires live on the class trees (section 6 table);
   cross-class cross-wires (e.g. RAGE spend → GRAZE) roll only as rare item affixes, so off-class material mixes
   can become engines.
5. **Gaps → leave as parameters.** Pierce stays a STRIKE value; swarms stay actions (COMMAND spends the Swarm
   store). No new material.
6. **Event list → merge first.** One event enum covering trigger WHEN names and material synthesis names, with
   aliases so existing data loads. This is the first implementation step: Listener affixes and cross-wires both
   target events by name.

### Implementation order this implies

1. Merge the event enum (with aliases) and add the 6 new events.
2. Generalise stores (mints + statuses + hero resources on one model); fold Vulnerability / Armor Break into Expose.
3. Attunement + the 20 new presets.
4. Item affix templates, starting with Listener (loot-rolled, slot-limited event families).
5. Skill rule families: cross-wires per class, then Persistence capstones; rare-item cross-class cross-wires.
6. Conversion table as data; move Envenomed Edge / Thermal Shock / Venom Mace / Hemorrhage onto it.

Each step should land with a bot update (synergy bot scoring for new events / stores) and a balance scan, since
the synergy bot is the calibration bot.
