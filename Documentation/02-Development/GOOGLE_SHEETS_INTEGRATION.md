# Google Sheets Integration Guide

This guide explains how to connect Google Sheets to DungeonFighter for **actions** and for **optional extra tabs** (weapons, modifications, armor, and class presentation).

## Publishing each tab as CSV

For **each** tab you want to pull from the web:

1. Open your Google Sheet.
2. **File → Share → Publish to web** (or use an export CSV link with the correct `gid` for that tab).
3. Select the tab (e.g. **ACTIONS**, **WEAPONS**, **MODIFICATIONS**, **ARMOR**, **CLASSES**).
4. Choose **Comma-separated values (.csv)**.
5. Publish and copy the link.

The published CSV is **public** (anyone with the link can read it). Push uses the **Google Sheets API** with OAuth (`SheetsPushConfig.json`); the spreadsheet must be one you can edit with that account.

## Configuration files

### `GameData/SheetsConfig.json` (pull / Resync)

| JSON field | Purpose |
|------------|---------|
| `spreadsheetEditUrl` | **Browser Edit link** (`…/spreadsheets/d/<realId>/edit…`). Used to sync **`spreadsheetId`** into `SheetsPushConfig.json` for OAuth **push**. Published CSV links often use `d/e/2PACX-…` — that value is **not** accepted by the Sheets API as `spreadsheetId` (you get HTTP 404 on push). |
| `actionsSheetUrl` | Published CSV URL for the **Actions** tab (two-row header). Acts as the **template** for other tabs when you use gids (same link, different `gid=`). |
| `weaponsSheetUrl`, `modificationsSheetUrl`, `armorSheetUrl`, `classPresentationSheetUrl` | Full published CSV URLs per tab. The Balance Tuning panel **derives** these from `actionsSheetUrl` + numeric tab gids when you save; you can still hand-edit full URLs here. |

Leave a derived URL / gid empty to skip that section on pull.

**One spreadsheet workflow:** paste one Actions published CSV URL, then enter each other tab’s numeric **gid** (shown when you publish that tab to web). The game replaces only the `gid=` query parameter so all pulls use the same document pattern.

### `GameData/SheetsPushConfig.json` (push / OAuth)

Copy from `SheetsPushConfig.template.json` if needed. Important fields:

- `spreadsheetId` — spreadsheet ID from the edit URL.
- `actionsSheetTabName` — tab name for actions (must match your sheet; default template uses `ACTIONS`).
- `weaponsSheetTabName`, `modificationsSheetTabName`, `armorSheetTabName`, `classPresentationSheetTabName` — optional; if set, push writes that tab when the local file exists (weapons / mods / armor) or when `TuningConfig.json` exists (classes). Empty string skips that tab.
- `oauthClientSecretsPath`, `oauthTokenStorePath` — OAuth desktop client and token directory.

## Tab layouts and headers

### ACTIONS

Unchanged: **two-row** header block (context + labels), then data rows. Push clears only rows **below** the detected header; pull uses `SpreadsheetParserRunner` as before.

### WEAPONS, MODIFICATIONS, ARMOR

- **Row 1:** column headers (stable order: canonical JSON property names, then any extra keys found in the data, sorted).
- **Row 2+:** one object per row.
- **Weapons / Armor:** header names use **camelCase** (`type`, `name`, `baseDamage`, …). Nested `attributeRequirements` is stored as **JSON text** in the cell.
- **Modifications:** header names use **PascalCase** (`DiceResult`, `ItemRank`, `Name`, …) to match `Modifications.json`.

Pull maps columns **by header name** (case-insensitive), not by fixed column index.

### ENEMIES

Two-row header (category band + short names), then data rows. Canonical columns **A–U**:

| Col | Field | Notes |
|-----|-------|-------|
| A–D | `region`, `biome`, `location`, `rarity` | Spawn placement |
| E | `name` | Enemy name |
| F | `tags` | Comma-separated registry tags, e.g. `undead, boss, minion` — **not** a JSON array. See [TAG_REGISTRY.md](../05-Systems/TAG_REGISTRY.md). Pull also accepts semicolon/pipe separators. |
| G | `archetype` | One of **10** values: Knight, Assassin, Berserker, Acrobat, Brute, Warlord, Sage, Duelist, Artificer, Trickster |
| H–K | base attributes | `strength`, `agility`, `technique`, `intelligence` |
| L–O | growth per level | same four stats (sum normalized to 6/level in game) |
| P–Q | HEALTH | `baseHealth`, `healthGrowthPerLevel` |
| R–U | `actions`, `isLiving`, `description`, `colorOverride` | `actions`: pipe list (`JAB\|TAUNT`), not a JSON array |

**Authoring flow:** edit **Archetype** and **tags** on the ENEMIES tab → **PULL** → `Enemies.json` updates (archetype Title Case normalized on import). **Push** writes local `Enemies.json` back to the sheet (enable **Push ENEMIES** in Balance Tuning). Push exports `tags` comma-separated and `actions` pipe-separated.

The full 58-tag vocabulary is defined in code (`TagDefinitions.cs`), not as a separate sheet tab. Valid values are documented in [TAG_REGISTRY.md](../05-Systems/TAG_REGISTRY.md).

### ENVIRONMENTS

Single header row; columns **A–G** (pull → `GameData/Rooms.json`):

| Col | Field | Notes |
|-----|-------|-------|
| A | `region` | Travel region id (same vocabulary as ENEMIES **region**) |
| B | `biome` | Dungeon theme match (`Forest`, `Lava`, `Crypt`, …). **Leave blank** = room can appear in **any** theme |
| C | `location` | Room display name and catalog key |
| D | `tags` | Comma-separated environment tags (`fire`, `scorched`, …) — see [TAG_REGISTRY.md](../05-Systems/TAG_REGISTRY.md) |
| E | `description` | Room flavor text |
| F | `actions` | Environmental hazard actions: `Action A\|Action B` or `Action:0.8\|Other:0.2` |
| G | `enemies` | Optional weighted spawn pool: `Enemy A\|Enemy B` or `Enemy:1.0` |

Runtime: `RoomData` → `Environment` with `Environment.Tags`. Legacy JSON keys `Location`, `name`, `theme` are normalized on import/load and on **push** (mapped to `location` / `biome`; `actions` / `enemies` export as pipe cells, `tags` as comma-separated).

### DUNGEONS

Single header row; columns match `Dungeons.json`: `name`, `theme`, `minLevel`, `maxLevel`, `possibleEnemies`, `colorOverride`. **Push** exports `possibleEnemies` as a pipe list (`Goblin|Wolf|Spider`), not a JSON array. Pull accepts pipe lists or JSON arrays.

### ACTIONS — TAGS column

Optional **TAGS** cell (column **E** on the standard layout): comma/semicolon list of extra tokens (pool gates like `environment`, `enemy`, `weapon`, elements, etc.). Category and rarity are merged into runtime tags separately on import. **Push** writes TAGS from `Actions.json`; column **F** (e.g. `e(V)` formulas) is left unchanged.

### WEAPONS / ARMOR — tags

Optional `tags` column: comma-separated registry tags on push (e.g. `undead, boss`); pull also accepts JSON arrays or semicolon/pipe lists. Material prefix names (Bone, Steel, …) live on the **Prefix** tab and map to material tags at loot time.

### CLASSES (`classPresentation`)

**Push (current):** two columns, **one property per row** (easy to read in Sheets):

- **Column A header:** `property` — **Column B header:** `value`
- Each following row: property name (same keys as the former wide layout, e.g. `defaultNoPointsClassName`, `tierThresholds_0`, …) in A and the cell value in B.

**Pull** accepts, in order of detection:

1. **Legacy:** `field` + `jsonPayload` (or two rows with `field` + `value` where the value is a full `classPresentation` JSON object).
2. **Vertical:** `property` or `key` or `field` (when not legacy) plus `value`, with one property per row.
3. **Horizontal (compat):** one header row containing `defaultNoPointsClassName` and one wide data row (older exports).

## In-game UI

**Settings → Balance Tuning (Import):**

- **Actions** URL and an expander **Other game data (CSV URLs)** for the four optional tabs.
- **PULL from Google Sheets** — pulls only tabs that are **checked** and have a URL in `SheetsConfig.json` (per-tab flags in `SheetsPushConfig.json`: `pushActionsTab`, `pushWeaponsTab`, …).
- **Push game data to Google Sheets** — pushes only **checked** tabs (same flags; unchecked tabs are not cleared or written).

Always back up your sheet or JSON before push; Sheets has no automatic undo.

## Command line

```bash
# Pull everything configured in SheetsConfig.json
dotnet run --project Code/Code.csproj -- PULL_SHEETS

# Push actions + configured tabs (OAuth; browser may open on first run)
dotnet run --project Code/Code.csproj -- PUSH_SHEETS
```

PowerShell helpers: `Scripts/pull-sheets.ps1`, `Scripts/push-sheets.ps1`.

Legacy single-tab push still works:

```bash
dotnet run --project Code/Code.csproj -- PUSH_ACTIONS
dotnet run --project Code/Code.csproj -- UPDATE_ACTIONS
```

## Parser / PARSE mode

You can still point `PARSE` at a single CSV URL and output path for actions:

```bash
dotnet run --project Code/Code.csproj -- PARSE "https://..." "GameData/Actions.json"
```

## Troubleshooting

- **403 / permission on push:** Ensure the Google account you OAuth with can edit the spreadsheet; enable the Sheets API for your Cloud project.
- **Wrong or empty tab on push:** Tab names in `SheetsPushConfig.json` must match the sheet **exactly** (including spaces). Create the tab in the spreadsheet before pushing.
- **Pull corrupts columns:** Do not rename header cells for WEAPONS/MODS/ARMOR if you rely on pull; extra unknown columns are ignored on deserialize paths depending on content—prefer keeping headers aligned with the canonical list in code (`JsonArraySheetConverter`).
- **Connection errors:** Verify each published URL uses the correct `gid` for that tab.
