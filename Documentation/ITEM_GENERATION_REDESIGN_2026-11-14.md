# Item generation redesign (proposal, 2026-11-14)

Status: **proposal**, nothing implemented. Replaces the roll pipeline in `Code/Data/LootGenerator.cs` +
`LootTierCalculator` / `LootItemSelector` / `LootRarityProcessor` / `LootBonusApplier`.

## Goals (from the 11-14 request)

1. **Item level drives everything.** Loot level becomes the item level; mod eligibility, mod values and base stats all
   read it.
2. **Rarity decides how many mods, not which mods.** Rarity sets a minimum count per pool and a max; extra lines come
   from an exploding roll (keep rolling while it succeeds, stop at the rarity max).
3. **Mods come from level tables, Diablo 2 style.** Each mod is a *family* (e.g. +Life) with ranked *tiers*. Each tier
   has its own keyword, its own value range and a minimum item level, which can differ per slot. A rolled mod also
   rolls its value inside its tier's range.
4. **Material first.** Material is rolled before any mod. It and every line rolled after it tilt what comes next,
   actions last.
5. **Four mod pools on every item:** Material, Stat, Mechanic, Action (the whiteboard sketch).
6. **Every base item belongs (loosely) to a class.** Every class has bases in every slot. The base's own mechanic, its
   *implicit*, is what ties it to the class: something a build in that class can use, not always a plain buff.
7. **Cleanup:** remove the drop roll, step 4.5, dead code and the duplicated new-game pipeline, and fix the seeding and
   weapon-speed bugs.

Constraint carried over from 11-11: **minimize new mechanics**. Mechanic-pool lines and implicits are drawn from
things combat already runs: triggers (`Triggers.json`, the 120 animal suffixes), on-hit statuses, stores and rule lines.
New systems are a last resort.

---

## 1. The new pipeline

One request goes in and one spec comes out. A builder turns the spec into an `Item`. Every random draw happens while
the spec is built, so the spec can be logged, replayed, priced by tools and shown in the Item Generation lab.

```
ItemRollRequest  (hero, dungeon level, theme, constraints: slot/class/material/forced lines)
      │
      ▼
ItemRoller.Roll(request, seed) ──► ItemRollSpec   (pure data, every roll recorded)
      │
      ▼
ItemBuilder.Build(spec) ──► Item
```

### RNG: one seed, one sub-stream per stage

`seed → Random` per stage, derived as `Hash(itemSeed, stageName)`. Adding a roll to the material stage no longer
shifts the action stage, and no stage reads `RandomUtility.Ambient`. That fixes today's bug where the catalog
damage-bonus and extra-slot rolls ignore `ReseedForTools`. It also stops seeded sims from cascading every time a
loot rule changes.

### Stages

| # | Stage | Rolls | Notes |
|---|---|---|---|
| 0 | ~~Drop chance~~ | — | **Removed.** Every gameplay call site is already guaranteed loot: dungeon end (`RewardManager` ×2), travel events (`TravelModels`) and new game. `baseDropChance` / `dropChancePerLevel` / `magicFindEffectiveness` / `maxDropChance` / `guaranteedLootChance` go away. |
| 1 | **Item level** | 0 (+ roll-up) | `ilvl = clamp(dungeonLevel − (heroLevel − dungeonLevel), 1, 100)`. Same formula as today's loot level. It is stored on the item and **step 4.5 is deleted**. The only randomness is the item-level roll-up (§4b). |
| 2 | **Slot + class** | 2 | See §2. |
| 3 | **Tier** | 1 | `TierDistribution.json` row for `ilvl` (unchanged). Tier now means "which base archetype", not power: power comes from ilvl. |
| 4 | **Base item** | 1 (+ catalog ranges) | Pick from the catalog by slot × tier × class. Catalog min/max ranges roll here on the stage stream. Base stats and the implicit scale with ilvl (§5). |
| 5 | **Rarity** | 1 (+ roll-up) | Weighted roll as today (90 / 8.5 / 1.2 / 0.25 / 0.045 / 0.005). Unlock levels move into the rarity table (Mythic's hardcoded L15 included). The disabled upgrade cascade comes back as the rarity roll-up (§4b). No MF parameter. |
| 6 | **Material** | 1–2 | Rolled first among the mod stages (§3). |
| 7 | **Line counts** | n | Per-pool minimums from rarity, then exploding extras (§4). |
| 8 | **Lines** | per line (+ roll-ups) | For each line: pool roll-up → family (weighted, tilted by influence) → tier (by ilvl) → tier roll-up → value (in range). Order: Material → Mechanic → Stat → Action. |
| 9 | **Name** | none | Built from the keywords (§6). |

Guaranteed, new-game, travel and tool-forced loot all become **constraints on the request**, not separate code
paths:

| Today | Becomes |
|---|---|
| `GenerateNewGameBonusLoot` + `GenerateGuaranteedArmorLootOnce` (a hand copy of steps 1–7) | `request.SlotFilter = armor`, `request.ExcludeStarter = true`, `request.DungeonLevel = 1` |
| `ItemSelector.PreferredWeaponType` set/cleared around the call | `request.Hero` (read by the slot/class weights) |
| static `ToolForcedMaterial` / `ToolForcedAffix` | `request.ForcedMaterial`, `request.ForcedLines` |
| "try tiers 1–5, then the other category, then Basic Sword" | the roller relaxes filters in a fixed order (tier → class → slot) and records which one it relaxed |

---

## 2. Slot and class (stage 2)

Today: 50% weapon, 12.5% per armor slot, then (weapons only) 50% "same type as your equipped weapon". You said the job
of this roll is to balance class items against general items. The proposal makes both axes explicit and data-driven:

```jsonc
// GameData/LootTable.json
"slotWeights":  { "weapon": 4, "head": 1, "chest": 1, "legs": 1, "feet": 1 },
"classLean": {
  "heroClass": 0.40,       // share of drops on the hero's class bases (uniform would be 0.25)
  "otherClasses": 0.60,    // the rest, split evenly across the other three classes (0.20 each)
  "missingSlotBoost": 1.5  // optional: ×weight on slots the hero has empty or far below ilvl
}
```

* **Slot** is a weighted roll. The defaults reproduce today's 50 / 12.5 ×4.
* **Class** is a second roll over the four classes. It is how "class items vs general items" stays tunable. Off-class
  bases are what feed cross-class builds, and their implicits still pay into shared things (materials, stores).
  **Default 0.40 hero class / 0.20 each other class:** you see your own class's bases about twice as often as any
  other class's, but most drops (60%) are still off-class, which keeps cross-class builds alive. With no hero
  (tools), it's a uniform 0.25 each.
* **Dynamic option:** `missingSlotBoost` nudges toward slots where the hero is behind, so drops feel useful. Off by
  default and priced later.

---

## 3. Material first (stage 6)

The rules are the same as today, but they read the base item's class instead of the weapon type, so armor joins in:

* Share `classMaterialShare` (0.9 for weapons as today, **0.5 for armor**): one of the base class's three set
  materials. Armor is lower so armor stays the main source of off-class materials for mixed sets (11-07 design:
  materials are shared, not class-owned). Otherwise: any of the 12 set materials, or the class-less pool (Stone / Unknown / Strange / Celestial /
  Wood / Leather / Cloth) by the existing `ItemRank` match.
* The market-skill class-material bias (`LootContext.ClassMaterialChance`) is kept as one more weight.
* Materials still pay their own attribute whoever wears them (11-07 decision). This only changes how often each one
  shows up.

The material then seeds the item's **influence tags** (§7): its name (`bone`), its attribute (`str`), its store
(`rage`) and its synthesis event (`on_receive_hit`).

---

## 4. Line counts: rarity minimums + exploding extras (stage 7)

Rarity rows grow per-pool counts and one explode chance:

```jsonc
// GameData/RarityTable.json (columns replace StatBonuses / ActionBonuses / Modifications)
{ "Name": "Common",    "Weight": 90000, "MinLevel": 1,
  "Pools": { "material": [0,1], "stat": [1,2], "mechanic": [0,1], "action": [1,1] }, "Max": 3, "Explode": 0.25 },
{ "Name": "Rare",      "Weight": 1200,  "MinLevel": 1,
  "Pools": { "material": [1,1], "stat": [2,3], "mechanic": [0,2], "action": [1,2] }, "Max": 6, "Explode": 0.35 },
{ "Name": "Mythic",    "Weight": 5,     "MinLevel": 15,
  "Pools": { "material": [1,2], "stat": [3,4], "mechanic": [1,3], "action": [1,3] }, "Max": 10, "Explode": 0.50 }
```

`[min, max]` per pool. The roll:

1. Place every pool's **min** lines.
2. **Explode:** while `total < Max` and `roll < Explode`, add one line. Its pool is picked by weight among pools still
   under their own max, with the weights tilted by influence (§7). Stop on the first failed roll.

That is your "keep rolling until you hit the max" rule, kept bounded by both the item max and each pool's max. It
replaces the current `affixRolls × 50%` lottery, which can come up empty. (`ItemAffixByRaritySettings.RollAxis`
already does the exploding part for a single axis and is reused.)

The numbers above are placeholders shaped like the whiteboard: Common = 1 stat + 1 action, Rare = 1 material +
2 stat + 1 action. They get priced in the balance pass.

### 4a. Re-routing

**A line whose pool has no eligible family at this ilvl is re-routed, not lost.** It tries the next pool up the
ladder, then the pools below, and is dropped only when no pool has anything eligible. For example, mechanic families
might start at ilvl 8, so a level-1 Rare's mechanic line becomes a stat line. Item power stays tied to rarity at every
level, and low-level items still look simple because only the simple pools have rows there.

**Pool ladder** (data, `poolLadder`): `stat → material → mechanic → action`, roughly from plain numbers up to
build-defining effects. Re-routing and the pool roll-up below both walk this ladder.

### 4b. Roll-ups: any rank can roll up

A **roll-up** is one rule used at every ranked roll: after the roll, a small chance to step up one rank. It explodes,
so each success rolls again at a decayed chance, up to a cap. It reuses the code of today's disabled rarity cascade
(`ApplyRarityUpgrades`: base chance × decay^n, cap, stop on the first failure), turned into one helper.

```jsonc
// GameData/LootTable.json
"rollUp": {
  "itemLevel": { "chance": 0.10, "decay": 0.5, "max": 5, "step": 1 }, // ilvl +1 per success, capped at 100
  "rarity":    { "chance": 0.05, "decay": 0.5, "max": 2 },            // next rarity (still respects unlock level)
  "pool":      { "chance": 0.05, "decay": 0.5, "max": 1 },            // per line: move up the pool ladder
  "tier":      { "chance": 0.08, "decay": 0.5, "max": 2 }             // per line: next tier, even above ilvl
}
```

| Roll-up | When | Effect |
|---|---|---|
| **Item level** | stage 1, once per item | ilvl +1 per success. Everything downstream (tier table, base scaling, mod eligibility) sees the higher level. |
| **Rarity** | stage 5, once per item | Next rarity, so more lines and a higher max. Stops at a rarity locked at this level. |
| **Pool** | each line, before the family pick | The line moves one step up the pool ladder, e.g. a stat line becomes a material line. |
| **Tier** | each line, after the tier pick | Next tier of the same family, **even if its level requirement is above ilvl**. This is the "lucky drop" that beats the level table. The value is then rolled in the new tier's range. |

Every roll-up is recorded on the spec (`rolledUp: ["ilvl+2", "tier:Life+1"]`). That lets the tooltip or the drop
banner show it ("Rolled up!"), and lets tools count how often roll-ups happen.

All chances are first guesses for the balance pass. Expected effect at these values: about 10% of items are 1+ levels
above their loot level, and about 8% of lines are one tier above what the level allows.

---

## 5. Base items: class, implicit, level scaling (stage 4)

### Catalog changes (`Weapons.json`, `Armor.json`)

| Column | New / changed | Meaning |
|---|---|---|
| `class` | **new** (weapons already carry it as a tag) | Barbarian / Warrior / Rogue / Wizard. Every armor row needs one: 209 rows over 4 slots × 5 tiers × 4 classes = 80 cells, about 2–3 bases per cell. |
| `implicit` | **reuses the dead `triggerName` column** | One trigger or rule line from the existing vocabulary that leans toward the class or a build in it. Its value scales with ilvl. |
| `damageMin` / `damageMax` (weapons) | **replaces** `baseDamage` + `damageBonusMin/Max` | Damage range at ilvl 1. |
| `defenseMin` / `defenseMax` (armor) | **replaces** `armor` | Defense range at ilvl 1. Armor's value is now called **defense**. |
| attribute columns (`strength` …), `hit` / `combo` / `crit` | changed meaning | Values at ilvl 1; the level curve multiplies them. |
| `triggerBundles`, `equipEffects` | **removed** | Legacy; already ignored at generation. |

**Implicit examples** (existing mechanics only; values are first guesses):

| Class | Base | Implicit |
|---|---|---|
| Barbarian | Bludgeon (mace) | `ON_RECEIVE_HIT → +1 RAGE` (Bone's event, on any material) |
| Barbarian | Spiked Helm | `IF HP < 50% → +10% damage` (helps Bloodied builds, does nothing at full HP) |
| Warrior | Drum Greaves (legs) | `ON_COMBO_END → +1 combo step next combo` |
| Rogue | Glass Hood | `ON_CRITICAL → apply Bleed`; `−5% armor` (a tilt, not purely positive) |
| Wizard | Willow Band | `ON_NATURAL_ROLL odd → +1 INT for the fight` |

### Level scaling: damage and defense are rolled the same way

Weapon damage and armor defense both work like a mod line: **a range scaled by item level, then one roll inside it.**

```
range(ilvl) = [min × curve(ilvl), max × curve(ilvl)]        // curve per slot: weapon uses the damage curve,
value       = uniform integer in range(ilvl)                 // armor uses the defense curve × slot weight
```

* The curves come from the same anchors as the hero's power (`character.healthCurve`, the damage anchors), so gear
  tracks the hero instead of running on its own constants.
* Armor slot weights stay as data (chest 1.3, legs 1.1, head 0.8, feet 0.8, today's `armorSlotWeights`).
* The value is rolled once at generation and stored. Two Broadswords at ilvl 50 differ the way two "+Life" lines do.
* "Armor" is renamed to **defense** everywhere item-facing: catalog, tooltip, item lab. The combat-side armor
  calculation keeps its formula and just reads the item's defense value. (Renaming the combat code is a separate,
  optional pass.)
* Tier stays the *shape* of the base (fast dagger, heavy mace, light boots); ilvl sets its power.

Fixes in this stage:
* `BonusAttackSpeed = (int)(tier × 0.05)` is always 0 today. Speed becomes a double on the level curve, or is dropped
  if catalog `attackSpeed` already covers it.
* `EquipmentScaling.weaponDamagePerTier` is replaced by the curve.
* Armor already scales by ilvl at read time (`ItemScalingConfig.armorFlatPerPiece / armorPerItemLevel /
  armorPerItemTier`). That is replaced by the rolled, stored defense value, so weapons and armor use one model.

---

## 6. Mod tables (stage 8): families, tiers, keywords

New file `GameData/ItemMods.json`. One row per **family**, with its tier ladder:

```jsonc
{
  "family": "Life",
  "pool": "stat",                    // material | stat | mechanic | action
  "affix": "suffix",                 // where the keyword goes in the name
  "stat": "Health",
  "slots": ["head","chest","legs","feet","weapon"],
  "weight": 10,
  "tags": ["defense","hp","barbarian","warrior"],
  "tiers": [
    { "keyword": "of the Sprout",    "min": 4,   "max": 8,   "level": 1  },
    { "keyword": "of the Sapling",   "min": 9,   "max": 18,  "level": 7,  "slotLevel": { "weapon": 17 } },
    { "keyword": "of the Oak",       "min": 19,  "max": 40,  "level": 15, "slotLevel": { "weapon": 34 } },
    { "keyword": "of the Elder",     "min": 41,  "max": 90,  "level": 25, "slotLevel": { "weapon": 51 } },
    { "keyword": "of the Ancient",   "min": 91,  "max": 180, "level": 40 },
    { "keyword": "of the Worldroot", "min": 181, "max": 320, "level": 60, "slots": ["chest","head"] }
  ]
}
```

That is the D2 table in the screenshot: keyword per tier, value range per tier, a minimum level that can differ by
slot (`slotLevel`, like D2's "Clubs(8)" vs "Amulets(1)"), and top tiers allowed only on some slots (`slots` on the
tier, like Squid/Whale). Value ranges are filled in by a generator script from the hero curve so every tier stays in
proportion, then hand-edited.

### Rolling one line

1. **Family:** weighted pick among families in the line's pool that allow this slot and have at least one tier with
   `level ≤ ilvl`. The weights are tilted by influence (§7).
2. **Tier:** among the eligible tiers, weighted toward the top: the top tier ×1, the next ×1/3, then ×1/9
   (`tierFalloff`, default 3). *Decided:* top-heavy rather than pure D2 uniform. With uniform picks, a level-60 item
   would roll its level-1 tier as often as its best, so higher levels would feel unrewarding. Top-heavy keeps the
   level table meaningful and still leaves some variance. Then the tier roll-up (§4b) can lift it one step.
3. **Value:** uniform integer in `[min, max]` of that tier, stored on the line. Rerolling a value later is a possible
   crafting hook, not part of this proposal.
4. **No duplicate family on one item.**

### What each pool holds (migration of today's data)

| Pool | Holds | Comes from |
|---|---|---|
| **Material** | Tuning of the item's own material store: gain rate ("gain Bone 25% faster"), cap, carry, counts-as. Rows key off the material's tags, so only matching rows are eligible. | Condensed build doc affix templates 2 / 5 / 9 (Tuning, Counts-as, Carry). New data on existing stores. |
| **Stat** | Life, Damage, Defense, STR / AGI / TEC / INT, Hit, Crit, Combo, Speed, Regen, Magic Find, **Quality** | Today's `StatBonuses.json` stat rows (Common/Rare become tiers), stat-type adjectives (Heavy, Swift, Reinforced …) |
| **Mechanic** | Triggers, on-hit statuses, rule lines, a tagged extra action slot ("+1 action slot, Bone actions only") | The 120 animal suffixes (`triggerName` rows, already tagged shell/bug/…), Flaming / Poisonous / serrated adjectives, catalog extra-slot ranges |
| **Action** | Gear actions | `Actions.json`, gear-eligible actions (as now) |

`Modifications.json` (adjectives) and the stat rows of `StatBonuses.json` fold into `ItemMods.json`. Material prefix
rows stay in `PrefixMaterialQuality.json`, since a material is not a mod.

**Quality is a stat family** (decided). It multiplies the base item's own numbers (damage / defense / attributes,
today's `gearPrimaryStatMultiplier`). Its tiers are the existing names on a level ladder:

| Tier | Keyword (prefix) | Base stats | Level |
|---|---|---|---|
| 1 | Worn | ×1.02–1.05 | 1 |
| 2 | Second Hand | ×1.05–1.08 | 8 |
| 3 | Like New | ×1.08–1.12 | 18 |
| 4 | New | ×1.12–1.18 | 30 |
| 5 | Perfect | ×1.18–1.25 | 45 |
| 6 | Masterwork | ×1.25–1.35 | 60 |
| 7 | Heirloom | ×1.35–1.50 | 80 |

Cosmic (×2.5) becomes a tier-roll-up-only top tier (`level: 101`, so only a roll-up from Heirloom reaches it). The
negative Broken / Battle Scarred rows are cut, because a mod line should not be a penalty for rolling well. Values are
placeholders for the balance pass.

### Keywords: one naming theme per family (decided)

Each family gets its own ladder of 6–8 keywords, so the name tells you the family *and* how high it rolled. Examples:

| Family | Affix | Ladder (low → high) |
|---|---|---|
| Life | suffix | of the Sprout · Sapling · Oak · Elder · Ancient · Worldroot |
| Damage | prefix | Sharp · Keen · Vicious · Savage · Brutal · Cruel · Merciless |
| Defense | prefix | Padded · Sturdy · Reinforced · Hardened · Bulwark · Fortress |
| STR | suffix | of the Ox · Bear · Bull · Mammoth · Titan · Colossus |
| Bone gain (material) | prefix | Splinter · Marrow · Ossuary · Reliquary · Catacomb |
| Quality | prefix | Worn → Heirloom (table above) |

The 120 animal suffixes stay the Mechanic pool's names (one keyword per trigger). Existing stat names that fit a
ladder (Reinforced, Hardened, Sturdy, Brutal …) are reused there, and the poetic Rare suffixes ("stitched by a
mother") become top-tier keywords of their stat's ladder. A naming pass fills in the rest during implementation step 4.

### Names

`[Material] [prefix keyword] Base [suffix keyword]`. At most one prefix and one suffix keyword make it into the name,
taken from the highest-tier prefix and suffix lines. Every line still shows in the tooltip as "Oak: +27 Life". This
keeps names readable at 6–10 lines.

---

## 7. Influence: everything rolled tilts what comes next

The item carries an **influence tag set** that grows as it rolls:

```
base class (barbarian) + slot event family (body: on_take_hit …) + material (bone, str, rage, on_receive_hit)
  + tags of every line already rolled
```

Each family's and each action's weight is multiplied by `1 + affinity × matches(row.tags, influence)`, capped (e.g.
`affinity` 0.5, at most ×3). Since order is Material → Mechanic → Stat → Action, the action is rolled last and sees
everything:

* **Weapon actions:** keep today's class share (`weaponGearActionClassShare` 0.7 on the weapon's class), then the
  influence tilt inside the chosen pool.
* **Non-weapon actions:** drawn from the **whole gear pool, any class**, with no class lock (your "random"). The only
  thing tilting the pick is the influence set. A Bone chest with an `on_receive_hit` line leans toward reactive
  actions but can still roll anything.

The pool choice for exploding extras (§4) uses the same tilt, so a Bone item leans toward Material and Mechanic extras.

---

## 8. Worked example: Bone Sword, Rare, ilvl 50

| Stage | Roll | Result |
|---|---|---|
| 1 | hero 48, dungeon 49 → 49 − (48 − 49) = 50; ilvl roll-up 0.63 ✗ | **ilvl 50** |
| 2 | slot 0.31 (< 0.5) → weapon; class roll → Warrior | weapon, Warrior |
| 3 | TierDistribution[50] → 3 | tier 3 |
| 4 | Warrior × weapon × tier 3 → Broadsword; damage range at ilvl 50 → rolled; implicit `ON_CONSECUTIVE_ATTACK → +4% damage` (scaled to ilvl 50) | Broadsword |
| 5 | rarity roll | **Rare** |
| 6 | class share 0.9 failed (0.94) → any of 12 → Bone | **Bone**; influence + bone, str, rage, on_receive_hit |
| 7 | mins: material 1, stat 2, action 1. Explode 0.35: 0.12 ✓ → pool roll → mechanic; 0.71 ✗ stop | 5 lines |
| 8a | material: "Bone gain" family, ilvl 50 → tier "Marrow" 60–80% → 75 | "gain Bone 75% faster" |
| 8b | mechanic: tilt favors `bone` tags → "Bone action slot" family | +1 action slot (Bone actions only) |
| 8c | stat: Life → tier "Elder" (41–90) → tier roll-up 0.05 ✓ → "Ancient" (91–180) → 112; Damage → "Savage" → 25 | +112 Life (rolled up), +25 damage |
| 8d | action (weapon, Warrior 70% / neutral 30%, tilted by bone/rage) | CHOP |
| 9 | name: material + top prefix keyword ("Marrow", from 8a) + base + top suffix keyword ("of the Ancient", from Life) | **Bone Marrow Broadsword of the Ancient** *(keywords illustrative)* |

That is the third whiteboard column.

---

## 9. Cleanup list

**Removed (dead or vestigial).** "Dead" = runs but has no effect. "Vestigial" = left over from an older design and
overridden by a newer one.

| Piece | Why |
|---|---|
| Drop-chance roll + its 5 config keys | Only guaranteed loot remains in play |
| Step 4.5 (item level ±1) | Item level = loot level |
| `LootRarityProcessor.ApplyRarityScaling` | Multiplies by 1.0 |
| `RollRarity(magicFind)` parameter | MF ignored. (The disabled upgrade cascade is **not** removed: it becomes the generic roll-up helper, §4b.) |
| `rarityFromAffixes` legacy path, `AdjustRarityBasedOnBonuses` | Disabled; rarity is rolled, not derived |
| Rarity-capped affix tiers (today's step 7b, see below), Common-suffix fusion, `affixTierFalloff` on rarity | Replaced by ilvl tiers |
| `affixRolls` / `affixRollChance` / `affixPoolWeights` lottery | Replaced by min + explode |
| `RarityData.StatBonuses / ActionBonuses / Modifications` columns | Overridden by `itemAffixByRarity` today; replaced by `Pools` |
| `ItemGenerator.ApplyRandomBonuses`, `GenerateRandomTier`, `SelectRandomItemByTier`, `ApplyScaling` | Parallel older generators. Remove once their callers (tests, tools) move to the roller. |
| `EquipmentScaling.weaponDamagePerTier / speedBonusPerTier / armorValuePerTier`, read-time `armorFlatPerPiece / armorPerItemLevel / armorPerItemTier` | Replaced by rolled damage / defense on the ilvl curve |
| Catalog `baseDamage` + `damageBonusMin/Max` + `RolledDamageBonus`; catalog `armor` | Replaced by `damageMin/Max` and `defenseMin/Max` |
| Quality rows Broken / Battle Scarred | Penalty lines; cut when Quality becomes a stat family |
| Catalog `triggerBundles` / `equipEffects`; reroll adjective (`effect: reroll`) | Ignored at generation / special case not needed with tiers |
| `GenerateGuaranteedArmorLootOnce` duplicate pipeline | Becomes a request constraint |
| Static `PreferredWeaponType`, `ToolForcedMaterial`, `ToolForcedAffix`, lazy static helpers | Request fields; roller is an instance, safe across threads |

**What step 7b was:** with `rarityFromAffixes` off, a Rare item's first stat suffix was forced to come from the
Rare-tier rows of `StatBonuses.json`. Later suffixes picked a random tier at or below the item's rarity, each lower
tier 3× likelier. Two Common suffixes merged into one Uncommon. In short, rarity decided affix *strength*. Under this
proposal item level decides strength and rarity decides *count*, so 7b goes away.

**Bugs fixed:**
* Weapon speed per tier is always 0 (int cast).
* Catalog damage-bonus and extra-slot rolls skip the seeded RNG.
* A fallback item's `Level` is set and then overwritten by step 4.5.

---

## 10. Saves and other systems

* **Item data:** a line is stored as `{ pool, family, tier, keyword, value }`. On load, old `StatBonuses` /
  `Modifications` / `ActionBonuses` convert to lines (closest family, nearest tier by value). Old items keep their
  numbers, so nothing silently changes power.
* **Sets / material builds / triggers:** unchanged consumers. Material still sets `item.Material`; Mechanic lines and
  implicits produce the same `ActionTriggerBundle`s combat already reads.
* **Synergy bot:** item scoring reads lines instead of suffix/prefix lists; set/skill synergy is unchanged.
* **Tools:** the Item Generation lab, `LootStatsRunner`, MCP loot tools and DUNGEONSIM `--material` / `--affix` move
  to the request constraints. The lab can show the full `ItemRollSpec` for any seed.
* **Balance:** every gear number moves, and the active patch (night-v45i) and median-build calibration were measured
  on the old loot. Expect a full re-measure after implementation (one pass, when you ask, per the 11-11 "implement
  before testing" rule).

---

## 11. Implementation order

0. **Bring in `v43-actions`.** Its affix templates (TUNE, STORECOND, COUNTSDOUBLE, OPENER, FINISHER, BLOODPRICE), the
   item rule language and the RULE prefixes are exactly the Material and Mechanic pool content this design needs, but
   they live only on `v43-actions`, which is merged into neither this branch nor `main`. Merging it first means
   step 4 migrates them as families instead of rebuilding them. One conflict to settle in the merge: v43's
   "class-tied gear actions, 95% in-class" also applies to armor. Under this design only weapons keep a class share;
   non-weapon actions are random (§7).
1. **Skeleton:** `ItemRollRequest` → `ItemRoller` → `ItemRollSpec` → `ItemBuilder`, per-stage RNG, all call sites on it,
   duplicate pipeline and static state removed. Rules unchanged apart from removing stage 0, step 4.5 and dead code.
   (Seeded outputs change once here because of the RNG split.)
2. **ilvl scaling** of bases: rolled damage and defense ranges, armor → defense rename (catalog, tooltip, lab), speed
   fix.
3. **Rarity pools + explode + re-routing + roll-ups** (RarityTable columns, `rollUp` config, old lottery removed).
4. **ItemMods.json:** schema, loader, migration script from StatBonuses / Modifications / animal suffixes / Quality,
   tier-value generator from the hero curve, keywords.
5. **Material first + influence tags**, action pick last.
6. **Catalog:** `class` on every armor row, implicits for all 409 bases (data pass, existing mechanics only).
7. **Save conversion**, lab/tool updates, unit tests (determinism per seed, eligibility by ilvl/slot, explode bounds,
   no duplicate families, influence weights).
8. Balance pass, when asked.

---

## 12. Decisions (2026-11-14)

| # | Question | Decision | Where |
|---|---|---|---|
| 1 | Armor value | **Armor becomes defense**, rolled like damage: a range scaled by item level, rolled once. | §5 |
| 1b | Armor material share | 0.5 class materials (weapons stay 0.9); my call, not discussed. | §3 |
| 2 | Lines with no eligible pool | **Re-routed** up the pool ladder, then down. Plus **roll-ups**: item level, rarity, pool and tier can each roll up one rank, exploding with decay. | §4a, §4b |
| 3 | Tier pick | Top-heavy falloff (my call): uniform would make high-level items feel no better. | §6 |
| 4 | Quality | **A stat family** (Worn → Heirloom; Cosmic only through a roll-up; penalty rows cut). | §6 |
| 5 | Keywords | **One naming theme per family.** | §6 |
| 6 | Class lean | 0.40 hero class / 0.20 each other class (my call). | §2 |
| 7 | v43 affix templates | Merge `v43-actions` first, then migrate its templates and RULE prefixes as pool families (my call). | §11 step 0 |

Still open, none of it blocking: roll-up chances, pool counts per rarity and tier value ranges are all placeholders for
the balance pass.
