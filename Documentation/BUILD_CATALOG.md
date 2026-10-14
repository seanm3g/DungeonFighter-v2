# Build Catalog

Status: design pass, 2026-10-12. Uses the sticky card from [BUILD_DESIGN_PROCESS_v3.md](BUILD_DESIGN_PROCESS_v3.md).

**Goal:** every class, material, action, item piece and class skill has a home in at least one build, and
ideally two. The coverage tables at the end show where each piece is used. The gap list says what has to change
for a piece that has no working home.

Status marks: **✅ works today** · **◐ partly works** · **✗ blocked** (the reason is in the card).

---

## How material builds work

| Pieces of one material | Effect |
|---|---|
| 2 | The WHEN trigger starts minting **+1** currency. The convert action joins the action pool. |
| 3–4 | Each mint = number of **feed** material pieces you wear (at least 1). |
| 5 | Each mint = feed count × feed count (5 Bone → 25 RAGE per hit taken; at least 1). |

- Feed material is the material itself, except **Gold feeds from Mithril and Mithril feeds from Gold**.
- A convert action adds **+5 damage per banked currency** (Forge Sigil ×1.5). In `overnight-v2` it pays a **% of the hit per
  banked unit** instead, with a rate per currency (`combat.materialConvertPercentByKeyword`, solved with BUILDLAB so a full
  set is worth about ×1.25–1.35 over the plain class strip): flat damage stopped mattering once hits grew past a few hundred.
- **Cash-out:** when a convert connects, it pays out and then spends that whole bank. A miss keeps the bank.
  Banks still last until the dungeon ends, so the loop is: build up, then cash out on the convert.
- ON SLOW and ON FOCUS fire when the hero slows the target or gains Focus **from any source** in that hit
  (action, item trigger or skill), not only from the action itself.
- Leather (class-less): 3+ pieces give **Lucky** (2d20, keep the higher).
- Each material piece also rolls one item trigger from that material's pool (`MaterialTriggerCatalog`) when
  `lootSystem.materialPieceTriggers` is on (`overnight-v2`). Before 2026-10-12 loot never stamped them and save/load cleared
  them, so no piece carried a trigger in play.
- Weapons always use their class ladder by rarity: Common → 1st material, Uncommon → 2nd, Rare+ → 3rd.

| Class | Weapon · stat | Material (rarity) → currency ← WHEN | Convert |
|---|---|---|---|
| Barbarian | Mace · STR | Bone (C) → RAGE ← hit taken · Steel (U) → TRIUMPH ← combo end · Iron (R) → CRISIS ← hit on foe ≤50% HP | BONE WRATH · STEEL CREST · IRON CULL |
| Warrior | Sword · AGI | Bronze (C) → BEAT ← turn start · Gold (U) → DRAG ← hit that slows · Mithril (R) → STREAK ← connect after 2+ connects | DRUM CUT · GOLD ANCHOR · MITHRIL FLOW |
| Rogue | Dagger · TEC | Glass (C) → GRAZE ← hit · Obsidian (U) → TROPHY ← kill · Shadow (R) → SWING ← any attack, hit or miss | GLASS NICK · HEADHUNT · UMBRA |
| Wizard | Wand · INT | Willow (C) → CHAIN ← combo · Silver (U) → LENS ← hit that gains Focus · Crystal (R) → CONTACT ← connect | WILLOW LASH · REFRACT · RESONATE |

---

## Changes made in this pass

1. **Convert actions paid +0 in the real game.** `Actions.json` stores the convert columns in sheet order:
   `materialScale` = bonus per currency ("5"), `keywordScale` = effect ("DAMAGE"), `scaleFormula` = currency
   ("CRISIS"). `MaterialSetController.GetConvertDamageBonus` read `materialScale` as a material name, so it
   returned 0 for all 12 converts. It now reads both the sheet layout and the old one. A test checks every convert
   as loaded from `Actions.json` (2 Iron + 4 CRISIS: IRON CULL +0 before, +20 now).
2. **UMBRA scaled on STREAK (Mithril's currency).** It now scales on SWING.
3. **Cash-out (G1).** A connecting convert spends the bank it scaled from. This caps each payout at what was
   built since the last cash-out, instead of a bank that only grows.
4. **Mint floor (G3).** A formed set mints at least 1. Mono Gold / mono Mithril now mint 1 per trigger instead of 0;
   the 3 + 2 cross-feed is still the strong version.
5. **Focus / Slow from any source (G2, G4).** LENS and DRAG mint when the hit gained the hero Focus or slowed the
   target, from any source.
6. **Trigger pools (G7).** 16 new item triggers: Silver +4 (two Focus procs, so Silver feeds itself), Crystal +4,
   Bronze / Gold / Mithril / Obsidian +2 each (Gold gets a Slow proc). Triggers.json was re-stamped. That also
   wrote the 12 Iron triggers, which were never stamped after the Damascus → Iron rename.
7. **Class action unlocks (G5).** Missing PUNCH HARDER / PUNCH HARDEST / OL' FAITHFUL / CAST and the tier 2–3
   JAB placeholders now unlock the class's own actions (see G5).
8. **Nimble (G6)** now rolls +0.25 SPEED instead of 0.

> **Tuned 2026-10-12 (overnight-v2):** converts pay % of the hit per currency; every build was measured step by step with
> BUILDLAB (`GameData/TuningProfiles/build-ladders.json`). See `Documentation/BUILD_LADDER_REPORT.md`.
>
> (Earlier note) **Balance not yet tuned.** Converts went from +0 to real damage, so every material build got stronger.
> Run SKILLVALUE and the smart bot before shipping. A 2-run smoke batch finished without errors, but that is
> far too small to say anything about balance.

> **Sheets:** `Actions.json` (UMBRA) and `ClassActions.json` are pulled from Google Sheets. Push them or make the
> same edits in the sheet, or the next pull will undo them.

## Barbarian — Mace · STR

### B1 · Pain Engine — Bone ×5 ✅
*Fantasy: get hit, get stronger, get healed.*
```
synthesis:  RAGE ← hits taken
alias:      Bone → Barbarian (Level 1 - Barbarian)
feed:       +1 / +Bone / ×Bone RAGE (25 per hit at 5)
criteria:   ON TAKE HIT
convert:    BONE WRATH +5 dmg/RAGE · Wrath Unbound heals 1% max HP/RAGE
joiner:     TAUNT / SELF TARGET → more incoming swings = more RAGE
```
- **Strip:** TAUNT · HARDEN · BONE WRATH · POUND (+ BERZERKER below 50% HP, ROOM CLEAR HEAL)
- **Gear:** Bone triggers CaveBear (hit taken → Harden), CapeBuffalo (hit taken → +10% next), Crab ·
  mods Reinforced / Hardened / Sturdy / Ancient · shell animals · Celestial Hydra (hit taken → heal) ·
  charm **Forge Sigil**
- **Skills:** Level 2 (dice → armor), Bone Skin, Bone Temper, Blood Price, Wrath Unbound, Scarred Hide,
  Tribal Kinship, Blood Synthesis, **Living Alloy**

### B2 · Grudge Bearer — Bone ×3 + Steel ×2 ✅
*Fantasy: bank the pain, spend it on the finisher.*
```
synthesis:  RAGE ← hits taken (3 per hit)
alias:      Bone + Steel → Barbarian
feed:       +3 RAGE (Bone 3); Steel 2 adds +1 TRIUMPH per combo end
criteria:   ON TAKE HIT
convert:    Pain Memory → next Finisher; BONE WRATH; STEEL CREST → Fortify (Crest of Triumph)
```
- **Strip:** WARCRY (Weaken + salvage) · BASH (vulnerable 2 turns) · RETRIGGER FINISHER · MIGHTY SWING (finisher)
  · COMBO END HEAL
- **Gear:** Steel WaterBuffalo (hit taken → Fortify), Gorilla / Lion (combo end → foe +1 to hit) · Bone
  Wolf / Velociraptor (combo step) · mods Reinforced / Sturdy
- **Skills:** Pain Memory, Bone Skin, Steel Knuckle, Crest of Triumph, Heavy Cadence, Might

### B3 · Executioner — Iron ×5 ✅
*Fantasy: the lower they go, the harder you hit.*
```
synthesis:  CRISIS ← each hit on a foe at ≤50% HP
alias:      Iron → Barbarian
feed:       +1 / +Iron / ×Iron CRISIS
criteria:   ON ENEMY HEALTH THRESHOLD (10 / 25 / 50%)
convert:    IRON CULL +5 dmg/CRISIS · Cull the Weak +4%/CRISIS under 25% HP
```
- **Strip:** BLUDGEON (stun) · HAYMAKER · IRON CULL · IRON CULL (Weasel: same action twice = +100%) ·
  STRIP REPEAT · CRIT HEAL
- **Gear:** Iron Bison (first hit +10%), Panther (crit on 18+), Weasel, Boar (miss salvage) · mods Heavy / Brutal /
  Long / Charged · beast animals · STRONGER, SHARPEN, CRITICAL ATTACK
- **Skills:** Bludgeon, Concussive Crit, Mace Mastery, Cull the Weak, Iron Will, Titan, Might, EXTRA EFFORT

### B4 · Empty Fury — Iron ×3 + Bone ×2 ✅
*Fantasy: two swings, no wasted motion.*
```
synthesis:  CRISIS ← each hit on a foe at ≤50% HP (3 per hit)
alias:      Iron + Bone → Barbarian
feed:       +3 CRISIS
criteria:   ON ENEMY HEALTH THRESHOLD
convert:    IRON CULL
joiner:     EMPTY SLOTS → Empty Fury +25 STR per empty slot; MIGHTY SWING gains per skipped/disabled slot
```
- **Strip (short):** IRON CULL · MIGHTY SWING · STRIP SKIP / STRIP DISABLE / STRIP JUMP
- **Gear:** Iron Mule (miss stops the strip), Monkey · Steel Marten / Orangutan (skip / jump) · mods Heavy / Brutal ·
  WHIRLWIND, TWISTING STRIKE
- **Skills:** Empty Fury, Mace Mastery, Gut Instinct (with INT gear), Titan, FIGHTER

### B5 · Triumph Cadence — Steel ×5 ✅
*Fantasy: short, loud combos that each end in a trophy.*
```
synthesis:  TRIUMPH ← combo end (break or wrap)
alias:      Steel → Barbarian
feed:       +1 / +Steel / ×Steel TRIUMPH
criteria:   ON COMBO END
convert:    STEEL CREST +5 dmg/TRIUMPH · Crest of Triumph Fortify/TRIUMPH
joiner:     STRIP STOP / STRIP LOOP → more combo ends per fight
```
- **Strip:** BARBARIAN RAGE · SLAM · RAPID STRIKE · STEEL CREST · STRIP LOOP (+ ANGRY ENRAGE, MEASURED ADVANCE,
  MOMENTUM BASH)
- **Gear:** Steel DireWolf (kill → retrigger opener), Gorilla, Lion, Orangutan · mods Balanced / Precise · quality
  **Battle Scarred / New / Masterwork** (Scrap Prefer)
- **Skills:** First Steel Age, Heavy Cadence, Second Steel Age, Third Steel Age, Scrap Prefer, Steel Knuckle,
  Crest of Triumph

### B6 · Toxic Bludgeon — Iron ×3 + Steel ×2 ✅
*Fantasy: rot the armor, then crush.*
```
synthesis:  CRISIS ← each hit on a foe at ≤50% HP (3 per hit)
alias:      Iron + Steel → Barbarian
feed:       +3 CRISIS
criteria:   ON ENEMY HEALTH THRESHOLD
convert:    IRON CULL
joiner:     DoTs drop foes under 50% sooner → CRISIS mints earlier in each fight
```
- **Strip:** BLUDGEON · VULNERABILITY · IRON CULL · SLAM
- **Gear:** mods **Poisonous / serrated** · Iron Hippopotamus (crit → Bleed), Mongoose (pierce vs DoT) · Steel
  GrizzlyBear (crit → Bleed) · Strange Stonefish (poison carrier)
- **Skills:** Venom Mace, Mace Mastery, Concussive Crit, Blood Price

---

## Warrior — Sword · AGI

### W1 · War Drum — Bronze ×5 ✅
*Fantasy: every turn is a drumbeat, and the drum speeds you up.*
```
synthesis:  BEAT ← every hero turn
alias:      Bronze → Warrior
feed:       +1 / +Bronze / ×Bronze BEAT
criteria:   ON TURN
convert:    DRUM CUT +5 dmg/BEAT · War Drum +2% speed/BEAT
```
- **Strip:** STRIKE · SPRINT / RUSH · DRUM CUT · RETRIGGER SLOT (slot 2 = Metronome) · REACTION TRAINING
- **Gear:** Bronze PeregrineFalcon, GoldenEagle (kill → next roll 20), Ibis (pierce) · mods Featherweight / Nimble /
  Swift · **bird** animals (+8% speed) · LOOSE GRIP / JUICED / BLUR
- **Skills:** First Formation, Level 2 (dice → speed), Drill Cadence, Metronome, Cadence Keeper, War Drum,
  Martial Kinship, Drill Synthesis

### W2 · Drill Sergeant — Bronze ×3 + Mithril ×2 ✅
*Fantasy: every action is drill-tagged; the strip runs like a parade.*
```
synthesis:  BEAT ← every turn (3 per turn)
alias:      Bronze + Mithril → Warrior; AVENGE adds the Warrior tag to every action
feed:       +3 BEAT; Mithril 2 adds +1 STREAK per streak connect
criteria:   ON TURN
convert:    DRUM CUT; Drill Cadence lowers the combo threshold per Warrior tag
```
- **Strip:** SLASH · STRIKE (alternate speeds for Cadence Keeper) · STRIP REPLACE · AVENGE · CUNNING / DANCE ·
  SET BONUS (per Warrior tag) · MOMENTUM / MARATHON
- **Gear:** Mithril Swift / Nighthawk (after a miss → speed) · mods Balanced / Precise · charm **Mark of Class**
- **Skills:** Drill Cadence, Cadence Keeper, Flow State (+ EXPLODING DICE), Metronome, Avenge, Cunning,
  Unbroken Line

### W3 · Anchor Line — Gold ×3 + Mithril ×2 ✅
*Fantasy: slow the foe to a crawl and hold the line.*
```
synthesis:  DRAG ← hits that slow the foe (action or gear)
alias:      Gold + Mithril → Warrior
feed:       +2 DRAG (feeds from Mithril)
criteria:   ON SLOW
convert:    GOLD ANCHOR +5 dmg/DRAG · Anchor slows 3%/DRAG
```
- **Strip:** SLOW · CHALLENGE (Mark + Weaken) · MEASURED CUT · GOLD ANCHOR · EXPOSE
- **Gear:** Gold **Ingot** (connect → Slow), Crown (combo end → Fortify), Hummingbird · Steel Ram, Glass
  SaltwaterCrocodile, Obsidian Python, Strange Spider also count · mods Reinforced / Hardened
- **Skills:** Phalanx, Mobile Bulwark, Second Wind, Anchor, Steady March
- **Note:** each piece rolls one trigger from its pool, so DRAG ramps once a piece rolls a Slow proc. Before
  G4, only the SLOW action counted.

### W4 · Guardian — Gold ×2 + Leather ×3 ✅
*Fantasy: lucky shield; armor turns blocks into counters.*
```
synthesis:  DRAG ← Slow hits (+1)
alias:      Gold → Warrior; Leather 3 → Lucky
feed:       +1 DRAG
criteria:   ON SLOW
convert:    GOLD ANCHOR; Guard Duty / Riposte / Shield Geometry turn absorbed hits into damage
```
- **Strip:** TAUNT · HARDEN · JAB (reset foe chain) · MEASURED CUT · LUCK
- **Gear:** Leather Mammoth (first hit → Fortify), Badger (hit taken → Harden), Moose · mods Reinforced / Hardened
  / Sturdy / Ancient · shell animals · Stone Abalone / Pangolin
- **Skills:** Riposte, Guard Duty, Shield Geometry, Phalanx, Field Brief, Iron Discipline
- **Note:** DRAG is a side income here (+1 per slowing hit); the defence skills carry the build.

### W5 · Unbroken Streak — Mithril ×3 + Gold ×2 ✅
*Fantasy: never miss, and every clean hit builds the blade.*
```
synthesis:  STREAK ← connect after 2+ connects in a row
alias:      Mithril + Gold → Warrior
feed:       +2 STREAK (feeds from Gold)
criteria:   ON CONSECUTIVE ATTACK
convert:    MITHRIL FLOW +5 dmg/STREAK · Mithril Flow Focus/STREAK
```
- **Strip:** STRIKE · CONCENTRATION · MITHRIL FLOW · AVENGE (finisher) · AFTER MISS HEAL · DEEP BREATHING
- **Gear:** Mithril Heron (first hit → Pierce), Jackdaw · Steel Bloodhound (+accuracy) · mods Swift / Short (HIT) ·
  quality **Worn / Like New / Heirloom** (Parade Dress) · PIERCE
- **Skills:** Swift Strike, Commander's Opening, Unbroken Stride, Field Brief, Parade Dress, Sword Mastery,
  Mithril Flow, Iron Discipline, **Unbroken Line**

> Mono Gold ×5 and mono Mithril ×5 now mint 1 per trigger (they minted 0 before G3). They are still the weak way
> to wear these materials; the 3 + 2 split is the intended build.

---

## Rogue — Dagger · TEC

### R1 · Glass Cannon — Glass ×5 ✅
*Fantasy: a hail of tiny cuts that shatter the target.*
```
synthesis:  GRAZE ← hits
alias:      Glass → Rogue
feed:       +1 / +Glass / ×Glass GRAZE
criteria:   ON HIT
convert:    GLASS NICK +5 dmg/GRAZE · Shatter Vulnerability/GRAZE
```
- **Strip:** STAB (3 hits) · CUT · RAPID STRIKE · GLASS NICK · TAKING AIM
- **Gear:** Glass Cobra (Poison), PistolShrimp (crit → Pierce), Firefly (even roll → Expose) · mods Light / Acrobatic /
  Featherweight · quality **Broken / Preowned / Perfect** (Fragile Prefer) · **bug** animals · Wood Ant
- **Skills:** First Cut, Level 2 (dice → damage), Precision, Quick Fingers, Fragile Prefer, Shatter

### R2 · Marked Hunter — Glass ×3 + Obsidian ×2 ✅
*Fantasy: mark one target, bleed it, move the mark on its death.*
```
synthesis:  GRAZE ← hits (3 per hit)
alias:      Glass + Obsidian → Rogue
feed:       +3 GRAZE; Obsidian 2 adds +1 TROPHY per kill
criteria:   ON HIT
convert:    GLASS NICK; Back Channel +3%/rank vs Marked
```
- **Strip:** MISDIRECT · EXPOSE · GLASS NICK · HEADHUNT
- **Gear:** Glass Firefly, SaltwaterCrocodile · Obsidian Basilisk · mods Long / Charged
- **Skills:** Shadowcraft (first hit Marks), Back Channel, No Witnesses, Snake Eyes, Perfect Crime

### R3 · Headhunter — Obsidian ×5 ✅
*Fantasy: each kill in the dungeon sharpens the next.*
```
synthesis:  TROPHY ← kills (bank lasts the whole dungeon)
alias:      Obsidian → Rogue
feed:       +1 / +Obsidian / ×Obsidian TROPHY
criteria:   ON KILL
convert:    HEADHUNT +5 dmg/TROPHY · Trophy Hunter +3%/TROPHY
```
- **Strip:** STAB · VULNERABILITY · HEADHUNT · RETRIGGER OPENER (on kill)
- **Gear:** Obsidian Viper, Python · Wood / Bronze Corvid & GoldenEagle (kill → next roll 20) · Bone Hyena (kill → heal)
  · mods serrated
- **Skills:** Trophy Hunter, Deep Cut, Dagger Mastery, No Witnesses, Perfect Crime

### R4 · Toxic Ledger — Obsidian ×3 + Shadow ×2 ✅
*Fantasy: stack every DoT, then collect.*
```
synthesis:  TROPHY ← kills (3 per kill; DoTs finish foes)
alias:      Obsidian + Shadow → Rogue
feed:       +3 TROPHY; Shadow 2 adds +1 SWING per attack
criteria:   ON KILL
convert:    HEADHUNT; Toxic Ledger +10% of the hit per distinct DoT
```
- **Strip:** CUT · STAB · HEADHUNT · UMBRA
- **Gear:** mods **Poisonous / serrated / Flaming** · Shadow FerdeLance · Obsidian Toad (hit taken → Poison the
  attacker) · **reptile** animals · Strange Stonefish
- **Skills:** Envenomed Edge, Toxin Pressure, Hemorrhage, Toxic Ledger, Dagger Mastery

### R5 · Shadow Gambler — Shadow ×5 ✅ (after the UMBRA fix)
*Fantasy: misses still count; luck is a resource.*
```
synthesis:  SWING ← every attack, hit or miss
alias:      Shadow → Rogue
feed:       +1 / +Shadow / ×Shadow SWING
criteria:   ON ATTACK
convert:    UMBRA +5 dmg/SWING · Umbral Step Expose/SWING
```
- **Strip:** GAMBLERS BLESSING · STRIP RANDOM / STRIP SHUFFLE · CONFUSE · UMBRA · SALVAGE MISS · CRIT MISS HEAL ·
  FOCUS (action)
- **Gear:** Shadow Crocodile (repeat), Gecko (salvage), Damselfly, ClockworkBeetle · Unknown Squid / Cuttlefish ·
  **fish** animals
- **Skills:** Second Chance, Steady Hand, Ghost Step, Technical Debt, Umbral Step

### R6 · Dice Thief — Shadow ×3 + Leather ×2 (or Leather ×3 + Shadow ×2 for Lucky) ✅
*Fantasy: you choose the dice.*
```
synthesis:  SWING ← every attack
alias:      Shadow → Rogue
feed:       +Shadow SWING
criteria:   ON ATTACK
convert:    UMBRA; dice rules (Snake Eyes, LOADED DICE, REPLACE ROLL) feed crits
```
- **Strip:** LOADED DICE · REPLACE ROLL · CRIT FACES · UNLUCK (low rolls feed Grease Palm) · NATURAL SEVEN ·
  QUIET MIND · CONCENTRATE · LUCKY CHARM / CRITICAL ATTACK
- **Gear:** Leather Jaguar (crit threshold), KodiakBear · mods Long / Charged / Swift / Short
- **Skills:** Grease Palm, Snake Eyes, Perfect Aim, Technical Debt, Concentrate

---

## Wizard — Wand · INT

### Z1 · Chain Weaver — Willow ×5 ✅
*Fantasy: every combo echoes.*
```
synthesis:  CHAIN ← combo hits
alias:      Willow → Wizard
feed:       +1 / +Willow / ×Willow CHAIN
criteria:   ON COMBO
convert:    WILLOW LASH +5 dmg/CHAIN · Lash +3% AMP/CHAIN
```
- **Strip:** MAGIC MISSLE · ECHO SPELL · PHANTOM FOLLOW-UP · RETRIGGER NEXT · WILLOW LASH · READ BOOK
- **Gear:** Willow Sparrowhawk / Owl (crit faces), Rook · Shadow ClockworkBeetle · mods Balanced / Precise /
  Keen / refined · quality **Second Hand / Cosmic** (Pure Prefer) · Cloth Hawk
- **Skills:** First Glyph, Wand Focus, Soft Echo, Spell Memory, Pure Prefer, Wand Mastery, Lash, Arcane Weave

### Z2 · Empty Glyph — Willow ×3 + Crystal ×2 ✅
*Fantasy: a strip with holes in it, and the holes are the spell.*
```
synthesis:  CHAIN ← combo hits (3 per combo)
alias:      Willow + Crystal → Wizard
feed:       +3 CHAIN; Crystal 2 adds +1 CONTACT per connect
criteria:   ON COMBO
convert:    WILLOW LASH; Concentrated Chaos +7% AMP per empty slot; CONCENTRATED CHAOS finisher
```
- **Strip:** CHANNEL · STRIP SKIP / STRIP DISABLE · CONCENTRATED CHAOS · MEASURED ADVANCE
- **Gear:** Willow Dragon (crit → Burn) · Crystal Golem · mods Keen / refined
- **Skills:** Concentrated Chaos, Spell Memory, Slot Mind, Arcane Weave

### Z3 · Lens Sage — Silver ×5 ✅
*Fantasy: focus on focus; every focused hit sharpens the lens.*
```
synthesis:  LENS ← hits on which the hero gains Focus (action or gear)
alias:      Silver → Wizard
feed:       +1 / +Silver / ×Silver LENS
criteria:   ON FOCUS
convert:    REFRACT +5 dmg/LENS · Refraction Focus/LENS
```
- **Strip:** CHANNEL · EMPOWERED · REFRACT · DUNGEON ANALYSIS
- **Gear:** Silver **Prism** (crit → Focus), **Mirror** (crit → Focus), Moonglass, Halo, Kirin, Kitsune ·
  Gold Crane / Kestrel, Willow Sphinx (Focus)
- **Skills:** Focus Lens, Refraction, Archmage
- **Note:** LENS needs a Focus proc on at least one piece. Refraction's own Focus doesn't feed LENS back (it
  lands before the mint check), so there is no runaway loop. Before G2 this build could not mint at all.

### Z4 · Probability Engine — Silver ×3 + Willow ×2 ✅
*Fantasy: prove theorems, then rewrite the roll.*
```
synthesis:  THEOREM ← natural prime faces (Prime Theory)
alias:      Silver → Wizard
feed:       +1 THEOREM per prime face; Loaded Odds +INT on 15+
criteria:   ON NATURAL ROLL (prime)
convert:    REWRITE FATE spends Theorems; 3 Theorems → next non-prime roll explodes
```
- **Strip:** REWRITE FATE · EXPLODING DICE · REPLACE ROLL · ROLL FIFTEEN · PRE-FIGHT PLANNING
- **Gear:** Silver Kitsune · Willow Owl · mods Swift / Short
- **Skills:** Level 2 (dice → AMP), Prime Theory, Loaded Odds, Dungeon Analysis, Read Book
- **Note:** the Theorem loop is a skill loop; Silver ×3 adds LENS income whenever a Focus proc fires.

### Z5 · Elementalist — Crystal ×5 ✅
*Fantasy: every connect resonates; statuses react.*
```
synthesis:  CONTACT ← connects
alias:      Crystal → Wizard
feed:       +1 / +Crystal / ×Crystal CONTACT
criteria:   ON CONNECT
convert:    RESONATE +5 dmg/CONTACT · Resonance +2%/CONTACT
joiner:     Burn + Poison + Slow reactions (Kindle, Thermal Shock, Catalyst) raise hit damage
```
- **Strip:** BOLT · SLOW · RESONATE · MAGIC MISSLE
- **Gear:** Crystal Thunderbird (crit → Burn) · Willow Dragon · Glass Salamander · mods **Flaming**, Keen ·
  **mythic** animals (+5% AMP)
- **Skills:** Kindle, Sigil Burn, Thermal Shock, Catalyst, Resonance, Archmage, **Grand Equation**

### Z6 · Golem Mind — Crystal ×3 + Silver ×2 ✅
*Fantasy: a tanky caster whose INT grows with each slot.*
```
synthesis:  CONTACT ← connects (3 per connect)
alias:      Crystal + Silver → Wizard
feed:       +3 CONTACT
criteria:   ON CONNECT
convert:    RESONATE; Slot Mind +INT per slot; Mana Bleed heals on Wand hits
```
- **Strip:** MAGIC MISSLE · CHANNEL · RESONATE · DUNGEON ANALYSIS
- **Gear:** Crystal Golem (miss → Harden) · Silver Kirin · Stone Tortoise / Pangolin · mods Ancient / Sturdy
- **Skills:** Mana Bleed, Slot Mind, Focus Lens, Arcane Kinship, Weave Synthesis

---

## Hybrid classes (two each: an attribute build and a tag build)

| Hybrid | Attribute build | Tag build |
|---|---|---|
| **Warbrute** (Barb + War) | **Body Slam** — STR + AGI armor; BODY BLOW · Body Surge · PUNCH. Any Bone / Bronze. | **Iron Legion** — Steel ×3 + Bronze ×2: WARBRUTE SMASH · Warbrute Momentum · First Steel Age and First Formation both count the shared tags · Mark of Class |
| **Ravager** (Barb + Rogue) | **Prowess Rush** — STR + TEC armor; PROWESS RUSH · Prowess Surge · Light / Acrobatic mods | **Butcher's Flurry** — Iron ×2 + Shadow ×3: RAVAGER FLURRY · CRISIS + SWING · Venom Mace with Envenomed Edge |
| **Ragecaller** (Barb + Wiz) | **Willfire** — STR + INT armor; WILLFIRE · Will Surge · Gut Instinct (INT → STR) | **Storm Howl** — Bone ×2 + Crystal ×3: RAGECALLER HOWL · RAGE + CONTACT · Blood Price + Mana Bleed sustain |
| **Duelist** (War + Rogue) | **Finesse Thrust** — AGI + TEC armor; FINESSE THRUST · Finesse Surge · Quick Fingers | **Riposte Blade** — Mithril ×2 + Glass ×3: DUELIST FEINT · STREAK + GRAZE · Riposte + Back Channel |
| **Spellblade** (War + Wiz) | **Wits Arc** — AGI + INT armor; WITS ARC · Wits Surge · Drill Cadence | **Drum Weave** — Bronze ×2 + Willow ×3: SPELLBLADE WEAVE · BEAT + CHAIN · Metronome + Spell Memory |
| **Hexblade** (Rogue + Wiz) | **Mind Hex** — TEC + INT armor; MIND HEX · Mind Surge · Toxic Ledger | **Plague Ember** — Obsidian ×2 + Crystal ×3: HEXBLADE CURSE · TROPHY + CONTACT · Kindle (Poison → Acid + Burn) + Toxin Pressure · mods Poisonous / Flaming |

All hybrid rows are ◐: the tree data and actions exist, but I didn't play-test them in this pass.

---

## Coverage

### Classes and materials

| Piece | Builds | Count |
|---|---|---|
| Barbarian | B1–B6, Warbrute, Ravager, Ragecaller | 6 + 6 |
| Warrior | W1–W5, Warbrute, Duelist, Spellblade | 5 + 6 |
| Rogue | R1–R6, Ravager, Duelist, Hexblade | 6 + 6 |
| Wizard | Z1–Z6, Ragecaller, Spellblade, Hexblade | 6 + 6 |
| Bone | B1, B2, B4, Storm Howl | 4 |
| Steel | B2, B5, B6, Iron Legion | 4 |
| Iron | B3, B4, B6, Butcher's Flurry | 4 |
| Bronze | W1, W2, Drum Weave, Iron Legion | 4 |
| Gold | W3, W4, W5 (as feed) | 3 |
| Mithril | W2, W3 (feed), W5, Riposte Blade | 4 |
| Glass | R1, R2, Riposte Blade | 3 |
| Obsidian | R2, R3, R4, Plague Ember | 4 |
| Shadow | R4, R5, R6, Butcher's Flurry | 4 |
| Willow | Z1, Z2, Z4, Drum Weave | 4 |
| Silver | Z3, Z4, Z6 | 3 |
| Crystal | Z2, Z5, Z6, Storm Howl, Plague Ember | 5 |
| Leather | W4, R6 | 2 |
| Stone / Wood / Cloth / Unknown / Strange / Celestial | trigger carriers in B1, W4, Z6 / R1, R3 / Z1 / R5 / B6, R4 / B1 | 1–3 each |

### Actions (player pool)

| Action group | Homes |
|---|---|
| Starters SLAM, POUND, STRIKE, SLASH, STAB, CUT, MAGIC MISSLE, BOLT, PUNCH | every class build · PUNCH in Body Slam |
| Converts (12) | one mono build + one duo build each |
| Class stat buffs (12: EXTRA EFFORT … DUNGEON ANALYSIS) | B3, B4, B5, W1, W2, W5, R1, R5, R6, Z1, Z3, Z4 |
| MIGHT, CUNNING, CONCENTRATE, READ BOOK (skill unlocks) | B2/B3, W2, R6, Z1/Z4 |
| Class rares / commons: MIGHTY SWING, WARCRY, BARBARIAN RAGE, CHALLENGE, MEASURED CUT, MISDIRECT, LOADED DICE, ECHO SPELL, REWRITE FATE, CHANNEL, BLUDGEON, AVENGE, CONCENTRATED CHAOS | B2/B4, B2, B5, W3, W3/W4, R2, R6, Z1, Z4, Z2/Z3/Z6, B3/B6, W2/W5, Z2 |
| Status actions WEAKEN, SLOW, VULNERABILITY, HARDEN, PIERCE, EXPOSE, JAB, CONFUSE, LUCK, UNLUCK | B2, W3/Z5, R3/B6, B1/W4, W5, W3/R2, W4, R5, W4, R6 |
| Generic mods SPRINT, RUSH, MOMENTUM, MARATHON, STRONGER, RAPID STRIKE, EMPOWERED, SHARPEN, TWISTING STRIKE, WHIRLWIND, LOOSE GRIP, JUICED, BLUR, PHANTOM FOLLOW-UP | W1/W2, B3, B5/R1, Z3, B3/B4, W1, Z1 |
| Strip control (9) + RETRIGGER (4) | every one has a home: B4, B5, W1, W2, R3, R5, Z1, Z2, B2 |
| Dice: SALVAGE MISS, CRIT FACES, REPLACE ROLL, EXPLODING DICE | R5, R6/B3, R6/Z4, W2/Z4 |
| Heals (7) | B1, B2, B3, B5, R5, R6, W5, Z4 |
| One-offs HAYMAKER, TAUNT, BASH, BERZERKER, ANGRY ENRAGE, GAMBLERS BLESSING, SET BONUS, SELF TARGET | B3, B1/W4, B2, B1, B5, R5, W2, B1 |
| Generic bonuses LUCKY CHARM, CONCENTRATION, MEASURED ADVANCE, CRITICAL ATTACK | R6, W5, B5/Z2, B3/R6 |
| Hybrid actions (12) | one each in the hybrid table |
| **No build home** | FLIP ROOM SWITCH (environment utility) · TURN / ACTION / FIGHT / DUNGEON BONUS (look like cadence templates; check they are kept out of loot) · BAT / RAT / ANT SWARM (enemy only) |

### Items

| Piece | Homes |
|---|---|
| Weapon types Mace / Sword / Dagger / Wand | their class builds + 2 hybrids each |
| Armor stat lines STR / AGI / TEC / INT (~50 bases each) | class builds; two-stat armor in the hybrid attribute builds |
| Mods ARMOR (Reinforced, Hardened) | B1, B2, W3, W4 |
| Mods COMBO (Balanced, Precise) | B5, W2, Z1 |
| Mods HIT (Swift, Short) | W1, W5, R6, Z4 |
| Mods SPEED (Featherweight, Nimble) | W1, R1 |
| Mods MULTI-HIT (Light, Acrobatic) | R1, Prowess Rush |
| Mods DAMAGE (Heavy, Brutal) | B3, B4 |
| Mods CRIT (Long, Charged) | B3, R2, R6 |
| Mods HEALTH (Ancient, Sturdy) | B1, W4, Z6 |
| Mods AMP (Keen, refined) | Z1, Z2, Z5 |
| Mods DoT (Flaming, Poisonous, serrated) | Z5, R4, B6, R3, Plague Ember — **may not apply at all** (gap G6) |
| Mods MAGIC FIND (Blessed, Enchanted) | no combat build; farming utility |
| Quality prefixes (11) | Scrap Prefer → B5 · Parade Dress → W5 · Fragile Prefer → R1 · Pure Prefer → Z1 (one build each) |
| Animal taxa shell / beast / bird / bug / fish / reptile / mythic | B1+W4 / B3+B5 / W1 / R1 / R5 / R4 / Z5 |
| Charms Mark of Class / Forge Sigil / Menagerie | W2 + tag hybrids / every mono build / any taxon build |

### Class skills

Every node in the four class trees is named by at least one build above:

- **Barbarian:** Core, Rite (Puberty, Blood Market and Tribal Kinship are generic) → B1–B6; Bone → B1, B2;
  Iron → B3, B4, B6; Steel → B2, B5; Living Alloy → B1.
- **Warrior:** Bronze → W1, W2; Gold → W3, W4; Mithril → W2, W5; Rite (Steady March, Iron Discipline) → W4, W5;
  Unbroken Line → W2, W5.
- **Rogue:** Glass → R1, R2, R6; Obsidian → R3, R4; Shadow → R5, R6; Shadowcraft → R2; Perfect Aim → R6;
  Perfect Crime → R2, R3.
- **Wizard:** Willow → Z1, Z2; Silver → Z3, Z4; Crystal → Z5, Z6; Arcane Weave → Z1, Z2; Archmage → Z3, Z5;
  Grand Equation → Z5.
- **Thin spots:** Refraction appears only in Z3, and Gut Instinct only in B4 and Willfire.

---

## Gaps

| # | Gap | Status |
|---|---|---|
| G1 | The bank was never spent; with converts paying out, damage had no ceiling. | **Done:** a connecting convert spends its bank. Still needs a SKILLVALUE / smart-bot pass. |
| G2 | No action applied Focus, so Silver LENS was never minted. | **Done:** LENS mints when the hero gains Focus from any source; Silver has its own Focus procs. |
| G3 | Gold and Mithril feed from each other, so 3+ of one without the other minted 0. | **Done:** minimum mint of 1. Mono Gold / Mithril work but stay weaker than 3 + 2. |
| G4 | DRAG minted only from the SLOW action. | **Done:** any Slow the hero applies counts; Gold has a Slow proc (Ingot). |
| G5 | Class unlock tiers 2–4 were JAB / TAUNT placeholders, and four names didn't exist. | **Done:** Barbarian WARCRY → BARBARIAN RAGE → MIGHTY SWING; Warrior CHALLENGE → MEASURED CUT; Rogue MISDIRECT → LOADED DICE; Wizard CHANNEL (tier 1) → ECHO SPELL → REWRITE FATE. Tier 4 stays JAB / TAUNT (Warrior and Rogue have no more non-skill class actions). |
| G6 | Nimble rolled 0 SPEED. | **Done:** 0.25. **Done (2026-10-12):** DoT mods were unwired (BURN / POISON / BLEED vs weaponBurn …); `NormalizeWeaponDotEffect` maps them; Poisonous applies 0.5% poison on crit. |
| G7 | Trigger pools were uneven (Silver and Crystal had 2 each). | **Done:** +16 triggers; the thinnest pool is now 6. |
| G8 | Each quality prefix has only one build (one "Prefer" node per class). | Open, optional: let hybrid Material rows count both parents' Prefer lists. |
| G9 | `PUNCH HARD` (tutorial action and unarmed grant trigger) doesn't exist in `Actions.json`, only in `Actions_test.json`. FLIP ROOM SWITCH and the four *BONUS templates have no build home. | Open. Aliasing PUNCH HARD → PUNCH would make the tutorial re-add its action every load, so it needs its own fix. |
| G10 | R2 Marked Hunter: MISDIRECT replaces the next slot with STAB, so the strip ping-pongs STAB / MISDIRECT and never reaches EXPOSE / GLASS NICK / HEADHUNT (BUILDLAB: the build is ×0.4–0.65 of its class strip). | Open: put MISDIRECT last, or point its replace at GLASS NICK. |
| G11 | Material piece triggers: loot never stamped them, and save/load (`RepairMissingMaterialTrigger`) cleared them. Silver had no Focus source, so LENS never minted. | **Done (2026-10-12):** `lootSystem.materialPieceTriggers` stamps one pool trigger per piece and keeps it on load. |
| G12 | Trigger values: "+1 multihit on combo/connect" was worth ×2.6 per piece, "retrigger next on combo" ×3.8, "+10% amp" ×1.8 (opener lift), Focus on connect ×1.6; poison triggers ignored their value. | **Done (2026-10-12):** those fire on crit; amp procs are +10% damage; Prism, Kitsune, Python, Firefly fire on crit; Ingot on combo; poison triggers use their value (0.25 / 0.5% per tick). Strip-control procs (skip / jump / loop / random / repeat on connect or combo) fire on crit: a Steel set of them kept the strip at its opener (Triumph Cadence ×0.3). |
| G13 | Obsidian TROPHY mints on kill and lasts the dungeon, so single-fight tools (BUILDLAB, SKILLVALUE) can't price HEADHUNT / Trophy Hunter. | Open: needs a multi-fight lab mode, or seed the bank. |

**Bottom line:** 23 class builds and 12 hybrid builds cover every class, material, weapon type, mod family,
quality prefix, animal taxon, charm and class skill node. All 23 class builds now have a working engine. What's
left is balance tuning (converts went from +0 to real damage), the DoT mod check (G6) and the PUNCH HARD cleanup
(G9).
