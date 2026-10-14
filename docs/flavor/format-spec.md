# Demon Fighter — Flavor Format Spec

Living reference for flavor-text format work. Lore-free on purpose: it describes the slots and pipes, not the world. If this doc disagrees with the code, the code wins; note the difference at the bottom.

---

## 1. How Joey describes a format

Joey types a mockup of what the screen should show. Square-bracket words are blanks filled from data:

```
[ROOM_DESCRIPTION]

[TRIGGER_EVENT]. You [EFFECT_ON_PLAYER].

A [SIZE_TAG] [ENEMY_NAME] with [WEAPON_NAME] appears.
```

Cursor's job: figure out where each blank comes from (section 3), make the screen match the mockup, and handle missing data per section 4. If a blank has no source yet, say so and use a placeholder. Do not create a new data field without a Max sign-off (see rules file).

Spacing, line breaks, punctuation, and capitalization in the mockup are part of the format. Match them.

---

## 2. What the game shows today (baseline)

The live game is compact:
1. A one-to-two sentence room description
2. A short trigger beat (where present)
3. A templated enemy line: `A [enemy] with [weapon] appears.`
4. A one-line loot result, e.g. `After a careful examination, you find nothing useful.`
5. Combat log lines, with poetic `combatNarratives` mixed in

New formats should add energy to this, not bury it. Long text is fine when Joey asks for it, but the default target is short.

---

## 3. Slot vocabulary

Status key: **LIVE** = player can see it now. **SANDBOX** = working on `joey_sandbox_v1` only. **DORMANT** = authored, not wired. **PROPOSED** = does not exist yet (needs Max sign-off before it becomes a real field).

### 3a. Encounter blanks (from the Madlib Spectrum)

| Blank | Source | Status |
|---|---|---|
| `[ROOM_DESCRIPTION]` | `ENVIRONMENTS.description` → `Rooms.json` `description`. Some rooms already have 2 variants (the pattern to extend). | LIVE |
| `[TRIGGER_EVENT]` | No field yet. Proposed as a 2–4 line pool per location. | PROPOSED |
| `[EFFECT_ON_PLAYER]` | `ACTIONS` status-effect columns (Stun, Poison, Burn, Bleed, Expose, Weaken, Slow, Vulnerability, Harden, Silence, Confuse, Pierce) or `CONSUMABLE` stat effects. Hazard text: `EnvironmentalActions.json` `name`/`description`, shown via `{effect}`. | LIVE (hazards) |
| `[SIZE_TAG]` | `ENEMIES.tags` (tiny, giant, frail, young...). May be sparse — format must read fine without it. | LIVE data, sparse |
| `[ENEMY_NAME]` | `ENEMIES.name` | LIVE |
| `[WEAPON_NAME]` | Enemy's item: `WEAPONS.name`. Player's own weapon: hero gear. | LIVE |
| `[ARCHETYPE_TELL]` | No field yet. Proposed: one short behavior phrase per `ENEMIES.archetype` (Trickster, Assassin, Brute, Warlord, Sage, Berserker, Knight, Acrobat, Duelist). | PROPOSED |
| `[LOOT_LINE]` | Item naming: Prefix + item + Suffix(es). Rarity shows as name length (see `rarity` sheet). | LIVE |

Tiers (from the Madlib Spectrum — useful as a ladder for trials):
- **Tier 1:** room description, trigger + effect, enemy appears
- **Tier 2:** adds archetype tell to the enemy line (`...appears, [ARCHETYPE_TELL].`)
- **Tier 3:** adds the loot line

### 3b. Flavor banks (spreadsheet `flavor` tab → `FlavorText.json`)

The `flavor` tab has columns `section`, `bank`, `key`, `text`. One row = one line.

| Bank | Keyed by | Status |
|---|---|---|
| `environments.locationDescriptions` | dungeon theme (biome), with `Generic` fallback | SANDBOX (appended on room entry) |
| `environments.roomContexts` | `Biome/roomType` (armory, boss, chamber, kitchen, library, sanctum, shrine, treasure) | SANDBOX (appended on room entry) — verify in code |
| `combatNarratives.*` | event banks: firstBlood, healthRecovery, below50Percent, below10Percent, criticalHit, criticalMiss, environmentalAction, healthLeadChange, intenseBattle, enemyDefeated, playerDefeated, playerTaunt, enemyTaunt, plus `_biome` taunt pairs | Wired, but a display bug in `BattleNarrative.cs` blocks most lines (known, Max's call) |
| `names.*`, `classQualifiers.*` | hero names, level-up titles | LIVE |
| `TravelEvents.json` `title`/`narrative` | road events between dungeons | LIVE |
| Item affix descriptions (`ModificationTables.json` / `StatBonuses.json`) | per affix | LIVE channel, mostly unwritten |

Biome taunt selection works by substring match on the room's display name ("grove" → `_forest`, "tomb" → `_crypt`, etc.), not by tags.

### 3c. Tokens inside lines

| Token | Means |
|---|---|
| `{name}` | the actor of the event (player or enemy, depending on bank) |
| `{player}` | the player character |
| `{enemy}` | the opposing enemy |
| `{effect}` | the environmental effect text |

New tokens get added to this table before use.

---

## 4. Scale rules (every format, every slot)

1. **Any size pool.** Works with 1 line or thousands. No hardcoded counts.
2. **Fallback chain.** Most specific → more general → `Generic` → drop the line cleanly. Example: `Forest/kitchen` → `Forest` → `Generic` → nothing.
3. **Never show a broken blank.** No raw `[SLOT]`, `{token}`, "null", double spaces, or orphan punctuation when something is missing. Write the format so the sentence still reads if an optional blank is empty (e.g. `A [SIZE_TAG] Wight` → `A Wight`; fix "a/an").
4. **No back-to-back repeats** when the pool has more than one option.
5. **Content changes never need code changes.** New lines go in data only.
6. **Weighting is optional and PROPOSED.** Flat random pick unless Max approves weighting.

---

## 5. Open questions (do not decide — these belong to Max/Sean)

From the Madlib Spectrum and pipeline notes:
1. Should `TRIGGER_EVENT` become a real pooled field on ENVIRONMENTS?
2. Should `EFFECT_ON_PLAYER` be limited to effects that make sense as room hazards?
3. Does `SIZE_TAG` populate for every enemy?
4. Does the player's trigger line need to branch by weapon type (ranged vs. melee)?
5. Should description/trigger pools be weighted by rarity?
6. Long-term: append, replace, or fallback between `Rooms.json` descriptions and the `flavor` banks?
7. Should the `flavor` tab join the in-game Spreadsheet Import tool?

---

## 6. Pipeline (how text gets from sheet to screen)

1. Text written into the workbook's `flavor` tab (or via `Scripts/apply-flavor-content-package.py`)
2. `python Scripts/sync-flavor-text-from-xlsx.py --xlsx <path>` (dry run) → check summary
3. Same command with `--write` (backs up `FlavorText.json` first)
4. Rebuild, launch, look at a room or two

Other tabs (Environments, Enemies, etc.) sync through the in-game Spreadsheet Import tool.

---

## 7. Spec vs. code differences found

(Cursor: add a line here whenever this doc turns out to be wrong.)

-
