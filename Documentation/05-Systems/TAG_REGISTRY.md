# Tag Registry

Authoritative tag vocabulary for actions, items, enemies, and (future) environment/hero matching.

## Layers

| Layer | Purpose |
|-------|---------|
| **Pool gate** | Controls loot/action pools (`environment`, `enemy`, `weapon`, …) |
| **Match** | Combat and conditional matching (elements, substance, materials, …) |
| **Field duplicate** | Mirrors structured fields (action rarity tags from sheet import) |
| **Discouraged** | Prefer enums/fields instead (`head`, `chest`, …) |

Comparison is **case-insensitive**. Canonical registry lives in `Code/World/Tags/TagDefinitions.cs`.

## Layer 1 — Pool / routing (21)

- **Pool gates:** `environment`, `enemy`, `weapon`, `class`, `unique`, `starter`, `modtrade`
- **Primary classes:** `warrior`, `barbarian`, `rogue`, `wizard`
- **Weapon types:** `sword`, `mace`, `dagger`, `wand`
- **Action rarity (field dup):** `common`, `uncommon`, `rare`, `epic`, `legendary`, `mythic`

## Layer 2 — Match / flavor (38)

- **Elements:** `fire`, `earth`, `water`, `air` (only these four; not ice/lightning)
- **Environment states:** `scorched`, `flooded`, `overgrown`, `exposed`
- **Life / substance:** `living`, `undead`, `plant`, `elemental`, `celestial`
- **Creature attributes:** `giant`, `tiny`, `has_hands`
- **Encounter role:** `boss`, `minion`
- **Hero subclasses (hero only):** `trickster`, `warlord`, `duelist`, `artificer`, `acrobat`
- **Materials (prefix names):** `bone`, `bronze`, `glass`, `willow`, `steel`, `gold`, `obsidian`, `silver`, `damascus`, `mithril`, `shadow`, `crystal`, `stone`, `unknown`, `strange`

Material tags are copied onto `Item.Tags` when a Material prefix is rolled at loot time.

## Enemy archetypes (field — not freeform tags)

**Author on the ENEMIES spreadsheet tab**, column **Archetype**. Pull via `Scripts/pull-sheets.ps1` or in-game sheet pull → `GameData/Enemies.json`.

| Knight | Assassin | Berserker | Acrobat | Brute |
| Warlord | Sage | Duelist | Artificer | Trickster |

- Heroes **never** have an `archetype` field.
- Same spellings as hero subclass **tags** on enemies belong in **`archetype`**, not in the freeform `tags` array.

## ENEMIES sheet columns (canonical)

`region`, `biome`, `location`, `rarity`, `name`, `tags`, `archetype`, base attributes, growth, HEALTH band, `actions`, `isLiving`, `description`

Import normalizes archetype to Title Case and validates against the 10-name allowlist.

## Runtime wiring

- **Loot pools:** `GameDataTagHelper.IsGrantableOnHeroGear` excludes `environment` / `enemy`
- **Enemy spawn:** `EnemyDataFactory` copies `EnemyData.Tags` to `Enemy.Tags` and adds `living`/`undead`
- **Combat:** `TagDamageCalculator` uses fire→earth→water→air weakness cycle and reads hero tags via `TagAggregator` or enemy `Tags`
- **Validation:** `EnemyDataValidator`, `ActionDataValidator`, weapon/armor validators warn on unknown tags

## Naming collisions

| Collision | Rule |
|-----------|------|
| `environment` (pool) vs entity kind | Pool tag on **actions** only |
| `weapon` (pool) vs equipment slot | Pool tag on **actions**; slot = `ItemType.Weapon` |
| `celestial` | Substance tag and material prefix share one string; context = entity type |
