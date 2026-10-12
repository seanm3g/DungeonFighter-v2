# Skill Trees to Level 100 — Design

Status: **proposal** (2026-10-12). Builds on the current 44-node trees, the August 104-node design
(`git show 62499fe6:GameData/SkillTrees.json`) and the material sets in `GameData/MaterialBuilds.json`.

## Shape (all four classes)

- **Budget:** 1 class point per level with that weapon equipped, so ~99 points by L100 for a pure build.
  Each tree holds ~99 points, so a pure hero fills it at L100 and a hybrid has to choose.
- **Columns:** `Core` (free root) · `Rite` (class stat) · one column per class material (3) · `Confluence` capstone.
  Branch prerequisites already unlock root → tip; no UI change is needed for deeper trees.
- **Depth gate (new):** tier *t* needs class points earned ≥ `[0,0,2,5,10,18,28,40,55,70]`, so capstones arrive
  around L55–L70 instead of the first time a player pours points down one branch.
- **Stats live in materials (new, same rule for every class):**
  - *Kinship* (Rite, 5 ranks): +1 class stat per equipped class-material piece, per rank.
  - *Synthesis feed* (Rite, 5 ranks): every 20 class stat adds +1 to each mint of that class's material resources
    (RAGE, CRISIS, BEAT, GRAZE, CHAIN …), per rank.
  - Class stat → material resource → convert action. STR / AGI already work in combat; this gives TEC and INT the
    same path. Wizard also gets *Arcane Weave*: INT feeds AMP on Wand actions.
- **Convert masteries (new):** every material convert action is "+5 damage per resource" today. Each material column
  ends in a mastery that gives its convert a distinct second effect.
- **Sinks:** the August "pack" idea returns as 3 ranked nodes per tree (+0.2% damage per rank) that absorb leftover
  points late. They use the existing `damageModPerRank` node field.
- **Power:** tuned with `SKILLVALUE` + the smart bot. Target: whole tree ≈ +30% at L30, ≈ +40–50% at L100
  (smart bot 30–40% faster kills than the basic bot).

Legend: **cur** = node in today's tree · **aug** = August idea with working code · **new** = needs code.
Ranks in brackets.

## Barbarian — Mace · STR · Bone / Iron / Steel

| Tier | Core / Rite (STR) | Bone — RAGE (hit taken) | Iron — CRISIS (enemy low HP) | Steel — TRIUMPH (combo end) |
|---|---|---|---|---|
| 0 | Tribe Metal (cur) | | | |
| 1 | Rite of Passage — +1 STR / 10 levels [1] (cur) · Blood Market [1] (cur) | Iron Hide Roll [1] (cur) | Crushing Lesson — BLUDGEON [1] (cur) | First Steel Age — dmg per Barbarian item [5] (cur) |
| 2 | Might — MIGHT action [1] (cur) | Bone Skin — first hit → Harden [1] (aug `bronze_skin`) | Concussive Crit [1] (cur) | Heavy Cadence [1] (cur) |
| 3 | Tribal Kinship [5] (new) | Bone Temper — armor while Harden [5] (aug) | Venom Mace [1] (aug) | Second Steel Age — slot 2 extra hit [5] (aug) |
| 4 | | Blood Price — heal on hit [5] (aug) | Empty Fury [5] (cur) | Scrap Prefer [5] (cur) |
| 5 | Gut Instinct — INT → STR [3] (aug) | | Mace Mastery — armor-broken bank [5] (aug) | Third Steel Age — slot 3 speed [5] (aug) |
| 6 | Blood Synthesis — feed [5] (new) | Pain Memory — store dmg → finisher [5] (aug) | | Steel Knuckle — dmg while Harden [5] (aug) |
| 7 | | Wrath Unbound — BONE WRATH heals 1% max HP per RAGE [3] (new) | Cull — IRON CULL +% vs <25% HP per CRISIS [3] (new) | Crest — STEEL CREST grants Fortify per TRIUMPH [3] (new) |
| 8 | Titan — STR damage cap 100 → 150 [1] (new) | sink [5] | sink [5] | sink [5] |
| 9 | **Living Alloy** — Harden/Fortify count as class items (Confluence) [1] (aug) | | | |

## Warrior — Sword · AGI · Bronze / Gold / Mithril

| Tier | Core / Rite (AGI) | Bronze — BEAT (each turn) | Gold — DRAG (slow) | Mithril — STREAK (consecutive attacks) |
|---|---|---|---|---|
| 0 | Iron Lineage (cur) | | | |
| 1 | Commission — +6 AGI [1] (cur) · Muster Hall [1] (cur) | First Formation — speed per Warrior item [5] (cur) | Riposte — Pierce after taking a hit [5] (aug) | Sword Hit AGI [5] (cur) |
| 2 | Cunning — CUNNING action [1] (cur) | Roll Speed [1] (cur) | Guard Duty — dmg after armor absorbs [5] (aug) | Avenge — finisher [1] (cur) |
| 3 | Martial Kinship [5] (new) | Drill Cadence [1] (cur) | Shield Geometry [1] (aug) | Commander's Opening [1] (aug) |
| 4 | Steady March — Sword +AGI [5] (aug) | Metronome — slot 2 speed [5] (aug) | Phalanx — dmg while Fortify [5] (aug) | Unbroken Stride [5] (cur) |
| 5 | | Cadence Keeper [5] (aug) | Mobile Bulwark [5] (aug) | Field Brief — crit → Fortify [5] (aug) |
| 6 | Drill Synthesis — feed [5] (new) | Flow State [1] (aug) | Second Wind — combo end heals [5] (aug) | Sword Mastery [1] (aug) · Parade Dress [5] (cur) |
| 7 | | War Drum — DRUM CUT +speed per BEAT [3] (new) | Anchor — GOLD ANCHOR slows per DRAG [3] (new) | Flow — MITHRIL FLOW keeps STREAK on hit taken [3] (new) |
| 8 | Iron Discipline — a miss keeps the strip once/fight [1] (aug) | sink [5] | sink [5] | sink [5] |
| 9 | **Unbroken Line** (Confluence) [5] (aug) | | | |

## Rogue — Dagger · TEC · Glass / Shadow / Obsidian

| Tier | Core / Rite (TEC) | Glass — GRAZE (hit) | Shadow — SWING (attack) | Obsidian — TROPHY (kill) |
|---|---|---|---|---|
| 0 | Glass Veil (cur) | | | |
| 1 | Initiation — +TEC [1] (cur) · Black Market [1] (cur) | First Cut — dmg per Rogue item [5] (cur) | Crit-miss Reroll [1] (cur) | Envenomed Edge [1] (aug) |
| 2 | Concentrate — action [1] (cur) | Roll Damage [1] (cur) | Grease Palm — low roll → TEC [5] (aug) | Deep Cut — Bleed on hit [5] (aug) |
| 3 | Shadow Kinship [5] (new) | Precision — Multihit +1 [1] (cur) | Steady Hand — TEC streak [5] (cur) | Toxin Pressure [5] (aug) |
| 4 | Shadowcraft — first hit Marks [1] (aug) | Quick Fingers [5] (aug) | Ghost Step — miss salvage [5] (aug) | Hemorrhage [1] (aug) |
| 5 | | Back Channel — dmg vs Marked [5] (aug) | Technical Debt [1] (aug) | Toxic Ledger [5] (aug) |
| 6 | Edge Synthesis — feed [5] (new) | Snake Eyes [1] (aug) · Fragile Prefer [5] (cur) | No Witnesses [5] (aug) | Dagger Mastery [1] (aug, needs code) |
| 7 | | Shatter — GLASS NICK crit threshold −1 per 3 GRAZE [3] (new) | Umbral Step — UMBRA refunds SWING on miss [3] (new) | Trophy Hunter — HEADHUNT +TEC per TROPHY for the dungeon [3] (new) |
| 8 | Perfect Aim — every 25 TEC also lowers crit by 1 [1] (new) | sink [5] | sink [5] | sink [5] |
| 9 | **Perfect Crime** (Confluence) [1] (aug) | | | |

## Wizard — Wand · INT · Willow / Silver / Crystal

| Tier | Core / Rite (INT) | Willow — CHAIN (combo) | Silver — LENS (focus) | Crystal — CONTACT (connect) |
|---|---|---|---|---|
| 0 | Mage Circle (cur) | | | |
| 1 | Apprenticeship — +INT [1] (cur) · Reagent Market [1] (cur) | First Glyph — AMP per Wizard item [5] (cur) | Roll AMP [1] (cur) | Mana Bleed — heal on spell hit [5] (aug) |
| 2 | Read Book — action [1] (cur) | Combo INT [5] (cur) | Prime Theory [1] (aug) | Kindle [1] (aug) |
| 3 | Arcane Kinship [5] (new) | Soft Echo [5] (aug) | Focus Lens — Wand +TEC [5] (aug) | Sigil Burn — dmg vs Burning [5] (aug) |
| 4 | **Arcane Weave** — every 10 INT +2% AMP on Wand actions [5] (new) | Spell Memory [5] (aug) | Loaded Odds — 15+ roll → INT [5] (aug) | Thermal Shock [1] (aug) |
| 5 | | Concentrated Chaos [1] (cur, needs code) | Dungeon Analysis [1] (aug) | Slot Mind [5] (cur) |
| 6 | Weave Synthesis — feed [5] (new) | Wand Mastery [1] (aug, needs code) · Pure Prefer [5] (cur) | | Catalyst [1] (aug, needs code) |
| 7 | | Lash — WILLOW LASH +AMP per CHAIN [3] (new) | Refraction — REFRACT grants Focus per LENS [3] (new) | Resonance — RESONATE echoes 10% per CONTACT [3] (new) |
| 8 | Archmage — INT also adds to status duration ×2 [1] (new) | sink [5] | sink [5] | sink [5] |
| 9 | **Grand Equation** (Confluence) [1] (aug) | | | |

## Hybrid trees (from the design board, 2026-10-12)

One small tree per class pair, using the duo names in `classPresentation`. Unlocks with points in **both** parent
trees (10 + 10 to open; deeper rows gate on the lower parent total). A rank spends a point from whichever parent has
more unspent points. Replaces the August `sharedWith` side rail.

Every hybrid has the same four rows. The rarity on the board sets the order: common rows first, rare last.

| Row (board rarity) | Rule |
|---|---|
| **Secondary attribute** (common) | A derived stat = parent stat A + parent stat B. While the hybrid tree is open it is your **primary attribute** (where the game reads "primary", e.g. `PRIMARY` scaling). |
| **Material** (uncommon) | Both parents' class tags, materials included, count as the hybrid tag. Parent-tree effects that count "Barbarian / Warrior items or tags" also count the hybrid tag. |
| **Action** (common) | Unique hybrid actions: +25% of the hybrid's bonus for every 10 of the secondary attribute, and for every 10 hybrid tags you have. |
| **Accumulation** (rare) | +1 secondary attribute, or +1 hybrid tag, for every connect (dice roll) this fight. |

| Hybrid | Parents | Secondary attribute | Action bonus per 10 (attribute or hybrid tags) |
|---|---|---|---|
| Warbrute | Barbarian + Warrior | **Body** = STR + AGI | +25% damage |
| Ravager | Barbarian + Rogue | **Prowess** = STR + TEC | +25% speed |
| Ragecaller | Barbarian + Wizard | **Will** = STR + INT | +25% AMP |
| Duelist | Warrior + Rogue | **Finesse** = AGI + TEC | +25% accuracy |
| Spellblade | Warrior + Wizard | **Wits** = AGI + INT | +25% combo |
| Hexblade | Rogue + Wizard | **Mind** = TEC + INT | +25% status potency |

**Gear requirements:** some items require a hybrid secondary attribute (Body, Prowess, Will, Finesse, Wits, Mind)
instead of, or alongside, a base attribute. They are the gear that only a hybrid build can equip at that level.

"Hybrid tags you have" = class tags on equipped gear (materials count) plus class tags on the actions in your strip.
Numbers (+25% per 10) are the board's; they go through `power` like every other node so tuning stays a data edit.

Open questions from the board: the green marks over the Duelist and Spellblade action cards (keep, or rethink
accuracy / combo?); the Ravager material card reads "count as WARBRUTE" (assumed: Ravager).

## Foundation found while designing

19 of the current 44 nodes have no code (learning them does nothing): every class root (materials count as class
tags), the four loot-bias rules, the four "+1 Combo per 2 class tags" rules, the four dice-roll passives,
Concussive Crit, Precision (Multihit +1) and the Dagger crit-miss reroll. The class-tag system they need is the same
one the hybrids need, so it is built first.

## Build order

1. **Phase 1 — class tags + dead nodes:** a class-tag counter (gear + strip, materials count as their class, hybrid
   aliasing ready), then code for the 19 dead nodes.
2. **Phase 2 — class trees to L100:** depth gate, Kinship, Synthesis feed, convert masteries, sinks, caps, the
   August ideas and the missing legacy effects.
3. **Phase 3 — hybrid trees:** dual-parent unlock and gating, cost from the richer parent, secondary attributes as
   primary, hybrid tag aliasing, hybrid actions and accumulation; a hybrid tab on the skill tree screen.
4. Tune `power` per node with `SKILLVALUE` at L10 / L30 / L60 / L100 and the smart bot; run the balance check.
5. Tests per new effect; docs.

Save compatibility: node ids already in `SkillTrees.json` keep their ids; ranks above a node's new `maxRank`
are clamped on load.
