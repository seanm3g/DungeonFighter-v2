DEMON FIGHTER

*Flavor Text Pipeline — Status & Findings*

As with everything in this project: this is a breadcrumb reflecting where things stood as of this session, not a permanent spec. Later conversations and dev-meeting decisions override anything here if they conflict.

# 1. WHY THIS DOC EXISTS

This session did real diagnostic work on how flavor text actually reaches players in DungeonFighter-v2, using Cursor to trace the live codebase rather than guessing from the data files alone. It also built and proved out a working content pipeline from the Excel `flavor` sheet to the live game. This doc captures both, so future sessions don't have to re-derive them.

# 2. THE THREE-TIER MAP (CODE-CONFIRMED)

Every player-facing text source falls into one of three tiers. This was confirmed via Cursor tracing actual call sites in the codebase, not inferred from the data structure.

## Tier A — Live today, edit freely

| Source | Field | Player sees it via |
|---|---|---|
| `Rooms.json` (ENVIRONMENTS sheet) | `location`, `description` | Room entry text — confirmed on screen |
| `Dungeons.json` | `name`, `theme` | Dungeon selection / entry banner |
| `Regions.json` | `displayName`, `description` | Region travel screen |
| `TravelEvents.json` | `title`, `narrative` | Road/journey vignettes between dungeons (~100+ events) |
| `FlavorText.json` | `combatNarratives.*` | Poetic combat lines — **see bug below** |
| `FlavorText.json` | `names.*` | Random hero name if player doesn't choose one |
| `FlavorText.json` | `classQualifiers.*` | Level-up title suffix ("Aric the Bold") |
| `Actions.json` | `name` (not `description`) | Combat log action names (STAB, CHEAP SHOT, etc.) |
| `EnvironmentalActions.json` | `name`, `description` | Hazard name + `{effect}` in `environmentalAction` narrative |
| `ModificationTables.json` / `StatBonuses.json` | affix descriptions | Item tooltip flavor text — **newly discovered, unwritten** |
| Enemy encounter line | fixed template | "A [enemy] with [weapon] appears." — pulls name + weapon only |

## Tier B — Authored, saved, but NOT wired to any live call site

| Source | Status |
|---|---|
| `environments.locationDescriptions` (13 biomes) | Explicitly dormant per code comments |
| `environments.roomContexts` (biome/room-type) | Same — dead code path (`Dungeon.GetRoomDescription`, never called) |
| `forms` / `categories` (`<intro>`/`<detail>`, Room Enter template) | Settings-lab sandbox only; explicitly documented as "not yet wired into live game call sites" |
| `names.bossNames`, `items.consumableNames`, `environments.locationNames` | No runtime consumer found |

**Note:** As of this session, `environments.locationDescriptions` has been wired into room entry as a **prototype, on the `joey_sandbox_v1` branch only** — see Section 4. This does not change its dormant status on `main`.

## Tier C — Hardcoded in C#, not editable via any data file

- Pre-combat exploration flavor (`ExplorationManager.cs`, `EnvironmentDiscoveryGenerator.cs`)
- Procedural fallback room text when no `Rooms.json` match exists (`RoomGenerator.cs`)
- "Good combo" (3+ step) combat narrative
- Level-up banner text (qualifier itself comes from FlavorText, but the banner shell is hardcoded)

## Not player-facing at all (despite living in editable sheets)

- `Enemies.json` → `description` — stored, validated, **never displayed**
- `Actions.json` → `description` — editor/combo-menu only, explicitly excluded from combat tooltips

# 3. THE COMBAT NARRATIVE BUG (CONFIRMED, DIAGNOSED, NOT YET FIXED)

**Symptom:** In actual gameplay, zero `combatNarratives` lines appeared across multiple full fights (first blood, crits, health thresholds, taunts) — despite `enableNarrativeEvents: true` and `narrativeBalance: 0.8` in settings.

**Root cause:** `BattleEventAnalyzer.cs` sets one-shot significance flags (e.g. `HasFirstBloodOccurred`) at the moment it generates narrative text. `BattleNarrative.cs` (`GetTriggeredNarrativesIfSignificant()`, ~lines 181–197) then checks `!HasFirstBloodOccurred` to decide whether to *display* that same text — but the flag is already `true` by then, so the check always fails and the narrative is silently dropped.

**Fix direction (not yet implemented):** decide significance once, before setting the flag — or display whenever `AnalyzeEvent()` actually returned non-empty narrative text for that event.

**Impact:** All of the rewritten `combatNarratives` content (Section 5) is correctly authored and technically wired to the right call site — it just doesn't reach the screen until this display-gate bug is fixed. This is a real, specific, actionable bug report, not a settings or content-richness issue.

**Also confirmed, related:** biome-taunt selection (`enemyTaunt_forest`, etc.) works via **substring match on the room's display name** (not tags, not a biome field) — e.g. "forest," "grove" → `_forest`; "crypt," "tomb," "grave" → `_crypt`; "library," "study," "archive" → `_library`; "water," "ocean," "sea," "underwater" → `_underwater`. This resolves the earlier confusion about `library`/`underwater` not appearing as biomes elsewhere — it's a naming-convention mechanism, not a data gap.

# 4. THE SANDBOX BRANCH & LIVE PROTOTYPE

Per Sean's guidance, a personal sandbox branch (`joey_sandbox_v1`, on fork `madfamspeakes/DungeonFighter-v2`) was created for free experimentation — isolated from `main`, cannot break the live game, no write access needed since it's a fork.

**Prototype built and confirmed working on this branch:** `RoomInfoBuilder.cs` was edited (one file, ~10 lines) to append a random `environments.locationDescriptions` line (matched to the room's dungeon theme, falling back to `Generic`) underneath the existing `Rooms.json` description on room entry. Confirmed live in-game across three separate dungeons/rooms (Forest/Time Chamber, Forest/Kitchen, Forest/Library, Crypt/Nebula Observatory).

**Known behavior to note:** `locationDescriptions` matches on **dungeon theme**, not individual room type — so a "Kitchen" or "Library" room inside a Forest-themed dungeon will still pull Forest atmosphere lines. This can read as a slightly odd juxtaposition (moss/roots language in a kitchen). `roomContexts` (room-type-specific) would resolve this if it's ever wired in, since it's scoped to both biome *and* room type.

This is a demo, not a merge candidate — useful as a concrete "here's what wiring one dormant system looks like" artifact for pitching Max/Sean, not a production change.

# 5. THE FLAVOR SHEET → GAME SYNC PIPELINE (BUILT AND WORKING)

**The gap this solves:** the in-game Spreadsheet Import tool (Game Settings → Spreadsheet Import) syncs specific tabs (Actions, Weapons, Prefix, Armor, Suffixes, Consumables, Enemies, Environments, Dungeons, Classes, Class Actions) — but **does not include the `flavor` tab**, which is where all `locationDescriptions`, `roomContexts`, and `combatNarratives` content lives. Content written into that Excel tab has no automatic path into `FlavorText.json`.

**Two scripts now exist in the repo (`Scripts/`), built this session, to bridge that gap:**

1. **`apply-flavor-content-package.py`** — takes a structured content package (section/bank/key/text) and writes it into the `flavor` sheet of the Excel workbook, replacing existing rows at matching keys. Leaves all other rows/sections untouched.
2. **`sync-flavor-text-from-xlsx.py --xlsx <path>`** — reads the `flavor` sheet from the Excel workbook and writes it into `GameData/FlavorText.json`, targeting only `environments.locationDescriptions`, `environments.roomContexts`, and `combatNarratives`. Defaults to a dry-run (prints a summary, changes nothing) unless run with `--write`. Backs up `FlavorText.json` before writing.

**Known quirk:** the script originally had hardcoded validation expecting 5 `roomContexts` biomes and 13 `combatNarratives` banks. The actual sheet has 6 biomes (Generic counts separately) and 27 banks (13 generic + 14 biome-taunt pairs). This validation was relaxed to match reality — it does not affect correctness, since the script only ever updates keys present in the spreadsheet and never deletes untouched content.

**Confirmed end-to-end, live in-game (screenshots taken):** Excel `flavor` sheet → `apply-flavor-content-package.py` → `sync-flavor-text-from-xlsx.py --write` → rebuild/relaunch → visible in actual gameplay. Verified across Forest and Crypt dungeon themes.

**Workflow for future content passes:**
1. New/revised content is written (in chat, or directly).
2. Compiled into a content package doc (section/bank/key/text format — see `DemonFighter_FlavorSheet_Content_Package.md` for the template/example).
3. Handed to Cursor to run `apply-flavor-content-package.py` (or a quick adaptation of it) against the current Excel file.
4. `sync-flavor-text-from-xlsx.py` run as dry-run first to confirm, then `--write`.
5. Game rebuilt/relaunched, spot-checked in a room or two.

This loop is now fast (~10–15 minutes) since all infrastructure is proven — the first pass took much longer because the pipeline had to be discovered and built from scratch.

# 6. CONTENT STATUS AS OF THIS SESSION

**Fully written and synced into the spreadsheet + confirmed live:**
- `locationDescriptions` — all 13 biomes (Castle, Cavern, Crypt, Crystal, Desert, Forest, Generic, Graveyard, Ice, Lava, Ruins, Swamp, Temple)
- `roomContexts` — Forest, Crypt, Crystal, Lava, Temple (8 room types each: armory, boss, chamber, kitchen, library, sanctum, shrine, treasure)
- `combatNarratives` — all 13 generic banks (firstBlood, healthRecovery, below50Percent, below10Percent, criticalHit, criticalMiss, environmentalAction, healthLeadChange, intenseBattle, enemyDefeated, playerDefeated, playerTaunt, enemyTaunt) + Forest biome taunt pair (playerTaunt_forest, enemyTaunt_forest)

**Not yet written (still holding old placeholder content in the sheet):**
- `roomContexts` for Castle, Cavern, Desert, Graveyard, Ice, Ruins, Swamp, and Generic
- Biome taunt pairs for Crypt, Crystal, Lava, Temple, Library, Underwater

**Newly discovered, entirely unwritten:**
- Item/affix tooltip flavor text (`ModificationTables.json` / `StatBonuses.json`) — confirmed live channel, no content authored yet

# 7. OPEN ITEMS FOR THE DEV MEETING

1. Prioritize fixing the `BattleNarrative.cs` display-gate bug (Section 3) — content is ready, just blocked.
2. Decide whether dormant systems (`locationDescriptions`/`roomContexts`/`forms`) should be wired into live gameplay, and if so, how they should merge with existing `Rooms.json` descriptions (replace / append / fallback-only) — avoid maintaining two parallel description systems long-term.
3. Consider adding the `flavor` tab to the in-game Spreadsheet Import tool's checklist, the same way `ENVIRONMENTS` etc. already are — would eliminate the need for the manual sync scripts going forward.
4. Tag vocabulary — confirmed via Cursor that most descriptive tags (`boss`, `giant`, `fire` on environments, etc.) are currently inert/decorative, only a small mechanical set actually affects gameplay (elements, class triangle, living/undead, rarity words on enemies; elegant/dilapidated, dormant/active/cycling on environments). Worth confirming with Max whether the inert tags are safe to reuse freely or whether there's a reason they're reserved.
5. `aaaSTARTER` — a debug/placeholder enemy name was observed live in-game during testing; likely worth flagging as a stray test artifact.

# 8. TOOLS & LOCATIONS REFERENCE

- **Workbook:** `DEMON FIGHTER - DATA (1).xlsx`, `flavor` sheet — columns `section`, `bank`, `key`, `text`
- **Repo:** `DungeonFighter-v2`, local path `~/Documents/GitHub/DungeonFighter-v2`
- **Fork:** `madfamspeakes/DungeonFighter-v2`
- **Sandbox branch:** `joey_sandbox_v1`
- **New scripts:** `Scripts/apply-flavor-content-package.py`, `Scripts/sync-flavor-text-from-xlsx.py`
- **Game Settings tool:** Flavor Text panel (Forms/Categories sandbox, dormant), Spreadsheet Import panel (live tabs, does not include `flavor`)
- **Content package doc:** `DemonFighter_FlavorSheet_Content_Package.md` (in project files) — template and current written content, section/bank/key/text format
