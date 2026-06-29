# Tag Registry

Authoritative tag vocabulary for actions, items, enemies, and environment matching.

## Layers

| Layer | Purpose |
|-------|---------|
| **Pool gate** | Controls loot/action pools (`environment`, `enemy`, `weapon`, …) |
| **Match** | Combat and conditional matching (elements, substance, materials, …) |
| **Field duplicate** | Mirrors structured fields (action rarity tags from sheet import) |
| **Discouraged** | Legacy/special tags (`modtrade`) |

Comparison is **case-insensitive**. Canonical registry lives in `Code/World/Tags/TagDefinitions.cs`.

## Layer 1 — Pool / routing (23)

- **Pool gates:** `environment`, `enemy`, `weapon`, `class`, `item`, `action`, `unique`, `starter`
- **Primary classes:** `warrior`, `barbarian`, `rogue`, `wizard`
- **Weapon types:** `sword`, `mace`, `dagger`, `wand`
- **Action rarity (field dup):** `common`, `uncommon`, `rare`, `epic`, `legendary`, `mythic`

## Layer 2 — Match / flavor (52)

- **Elements:** `fire`, `earth`, `water`, `air`
- **Environment terrain:** `scorched`, `flooded`, `overgrown`, `exposed`
- **Environment structure:** `elegant` (+dungeon level roll), `dilapidated` (−dungeon level roll)
- **Environment activity:** `dormant`, `cycling`, `active` (hazard frequency)
- **Life / substance:** `living`, `undead`, `plant`, `elemental`, `celestial`
- **Creature attributes (enemy only):** `giant`, `young`, `tiny`, `frail`, `has_hands`, `has_feet`, `has_legs`, `has_head`
- **Encounter role:** `boss`, `minion`
- **Materials (prefix names):** `bone`, `bronze`, `glass`, `willow`, `steel`, `gold`, `obsidian`, `silver`, `damascus`, `mithril`, `shadow`, `crystal`, `stone`, `unknown`, `strange`
- **Action routing:** `required`, `opener`, `finisher`
- **Mechanic tags:** `swift`, `bludgeon`, `focus`, `insight` (next-action bonuses)
- **Roll tags:** `confidence`, `footwork`, `target`, `aim` (threshold shifts)

Material tags are copied onto `Item.Tags` when a Material prefix is rolled at loot time.

## Enemy archetypes (field — not freeform tags)

| Knight | Assassin | Berserker | Acrobat | Brute |
| Warlord | Sage | Duelist | Trickster |

## ENVIRONMENTS sheet (→ `Rooms.json`)

Columns: `region`, `biome`, `location`, `tags`, `description`, `actions`, `enemies`, optional `unstableThresholdMod` (`4`, `-2`, `2`, `0`).

Tags may include elements, terrain, structure, and activity tags. Creature attributes are not valid on environments.

## Runtime wiring

- **Loot pools:** `GameDataTagHelper.IsGrantableOnHeroGear` excludes `environment` / `enemy`
- **Pool routing:** `weapon` / `item` / `action` / `class` gate loot and action tables
- **Mechanic tags:** `ActionMechanicTagProcessor` queues next-action bonuses
- **Roll tags:** threshold shifts applied during hero rolls
- **Environment:** elegant/dilapidated adjust d20; activity tags tune hazard frequency; `unstableThresholdMod` shifts thresholds
