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
| `weaponsSheetUrl`, `modificationsSheetUrl`, `armorSheetUrl`, `classPresentationSheetUrl`, `classActionsSheetUrl`, `enemiesSheetUrl`, `environmentsSheetUrl`, `dungeonsSheetUrl`, `statBonusesSheetUrl`, `consumablesSheetUrl` | Full published CSV or **edit?gid=…** URLs per tab. The Balance Tuning panel **derives** these from `actionsSheetUrl` + numeric tab gids when you save; you can still hand-edit full URLs here. |

Leave a derived URL / gid empty to skip that section on pull.

**One spreadsheet workflow:** paste one Actions published CSV URL, then enter each other tab’s numeric **gid** (shown when you publish that tab to web). The game replaces only the `gid=` query parameter so all pulls use the same document pattern.

### Project reference spreadsheet (DungeonFighter)

These are the canonical authoring links for this repo (also stored in `GameData/SheetsConfig.json`):

| Resource | URL |
|----------|-----|
| **Spreadsheet (edit)** | https://docs.google.com/spreadsheets/d/1bN3vmkQGdbO_4TkdgRXy_5KeuxUAcPtuarzSOOAyArc/edit |
| **Testing / features design doc** | https://docs.google.com/document/d/e/2PACX-1vTGbqd7i56nTpfTa5g6moBajpkb9iq_ReCVWh1zoP1OA62YC9rK7IAB-vlccLO9iFIRJqqB0wE67Nfn/pub |

**Tab gids** (same document; append `?gid=<digits>` to the edit URL to jump to a tab):

| Sheet tab | gid | Pull → local file |
|-----------|-----|-------------------|
| ACTIONS | `2020359111` | `GameData/Actions.json` |
| WEAPONS | `1440786122` | `GameData/Weapons.json` |
| PREFIX (modifications) | `1340975005` | `GameData/Modifications.json` |
| ARMOR | `1580430780` | `GameData/Armor.json` |
| CLASSES | `178471389` | `classPresentation` in `GameData/TuningConfig.json` |
| CLASS ACTIONS | `1280106899` | `GameData/ClassActions.json` |
| ENEMIES | `1292949962` | `GameData/Enemies.json` |
| ENVIRONMENTS | `1652426036` | `GameData/Rooms.json` |
| DUNGEONS | `1068091644` | `GameData/Dungeons.json` |
| SUFFIXES (stat bonuses) | `388294050` | `GameData/StatBonuses.json` |
| CONSUMABLES | `828815998` | `GameData/Consumables.json` |
| flavor | `825117964` | **PUSH only** → from `GameData/FlavorText.json` (PULL does not overwrite local JSON) |

Example **ACTIONS** tab edit link:  
https://docs.google.com/spreadsheets/d/1bN3vmkQGdbO_4TkdgRXy_5KeuxUAcPtuarzSOOAyArc/edit?gid=2020359111

**CSV pull:** edit links work with the game’s fetch layer (`GoogleSheetsUrlHelper.TryBuildSpreadsheetCsvExportUrl` converts `…/edit?gid=…` → `…/export?format=csv&gid=…`). Legacy **Publish to web** URLs (`…/d/e/2PACX-…/pub?…&output=csv`) still work for read-only pull but cannot be used as OAuth `spreadsheetId`.

### `GameData/SheetsPushConfig.json` (push / OAuth)

Copy from `SheetsPushConfig.template.json` if needed. Important fields:

- `spreadsheetId` — spreadsheet ID from the edit URL.
- `actionsSheetTabName` — tab name for actions (must match your sheet; default template uses `ACTIONS`).
- `weaponsSheetTabName`, `modificationsSheetTabName`, `armorSheetTabName`, `classPresentationSheetTabName`, `flavorSheetTabName` — optional; if set, push writes that tab when the local file exists (weapons / mods / armor / FlavorText) or when `TuningConfig.json` exists (classes). Empty string skips that tab.
- `pushFlavorTab` — when true (default), OAuth push writes the **flavor** tab from `FlavorText.json` as a long table (`section` / `bank` / `key` / `text`). PULL never reconstructs FlavorText from the sheet.
- `oauthClientSecretsPath`, `oauthTokenStorePath` — OAuth desktop client and token directory.

## Tab layouts and headers

### ACTIONS

**Two-row header** (row 1 = section/context bands, row 2 = column labels), then data rows. Push clears only rows **below** the detected header; pull uses `SpreadsheetParserRunner`.

**Section breaks:** Rows whose column **A** is `LAYER n ACTIONS` (e.g. `LAYER 2 ACTIONS`) are **ignored on pull**. They separate action groups on the sheet; the same header columns apply to all sections.

**Column F:** Reserved for on-sheet formulas (e.g. `e(V)`). Push preserves column F; pull never writes it.

On pull, the console prints a **column usage summary** (see `SpreadsheetActionColumnUsage`). Labels are matched by row-2 header text (and row-1 context when columns repeat), not fixed letters.

| Tier | Meaning | Examples |
|------|---------|----------|
| **Combat / runtime** | Pulled → `Actions.json` → `ActionData` → `Action` → combat | `ACTION`, `DAMAGE` / `DAMAGE(%)`, `SPEED(x)`, `# OF HITS`, `TARGET` (column **M**: `enemy` / `self` / `environment`; empty = enemy), action-sheet status columns (`WEAKEN`, `CONFUSE`, `DISRUPT`, `LIFESTEAL`, …), hero/enemy dice mods, `CADENCE`+`DURATION` keyword bonuses, `MECHANICS` (declarative; validated on pull), next-action mods under `HERO BASE STATS` / `ENEMY BASE STATS`, flat **WEAPON SPEED** / **WEAPON DAMAGE** under `HERO BASE` / `ENEMY BASE` (or `… BASE STATS`), `JUMP`/`SHIFT`, `OPENER`/`FINISHER`, `HEAL` (under **HERO HEAL**) |
| **Loot / pools only** | Pool assignment, not combat math | `RARITY`, `CATEGORY`, `TAGS` |
| **JSON round-trip / sheet reference** | Stored in `Actions.json`; not applied in combat | `DPS(%)` (authoring reference — combat uses `DAMAGE(%)`), `DESCRIPTION` |
| **Not ingested on CSV pull** | Push/Settings know these labels; **pull ignores** sheet cells | `WEAPON TYPES`, `CHAIN LENGTH`, `RESET`, `GRACE`, `LOOP CHAIN`, JSON blob columns, threshold flat columns, … |
| **Sheet-only** | Never pulled | Column **F** (formulas) |

**Known gaps (ingested but not wired to combat today):** `CONSUME` — stored in `Actions.json` but not mapped to runtime `Action` effects. **`MAX HEALTH` is wired** (`ActionData.MaxHealthIncrease` → `Action.Advanced` / trigger bundles / heal path). Hero/enemy `STR`/`AGI`/`TECH`/`INT` only apply as combat bonuses when `CADENCE` is `TURN`/`ACTION`.

**Legacy:** JSON `targetType: "AreaOfEffect"` imports as `Environment`. The removed **SELF DAMAGE** column is deprecated — use `target=self` for self-directed effects instead.

### ACTIONS — TARGET column (column M)

`TARGET` controls who receives **damage**, **heals**, and **status effects** from the row. Empty or `enemy` = the combat opponent (relative: an enemy using the action hits the hero, not itself).

| Pattern | Column M (`TARGET`) | Status columns | Use when |
|---------|---------------------|----------------|----------|
| Normal attack / debuff | empty / `enemy` | **ENEMY TARGET** block | Hit and debuff the opponent |
| Full self-buff / self-heal | `self` | same block (or **HERO HEAL**) | All effects on the attacker |
| Attack + one self effect | empty / `enemy` | **SELF TARGET** block for that effect | Mixed: e.g. damage enemy + fortify self |
| Room-wide hazard | `environment` | status + `TAGS` | Environmental / room actions |

**Sheet authoring checklist (after code merge + pull):**

- Set `target=self` on pure self-buffs: **HARDEN**, **FOCUS** (when the effect should apply to the user).
- Set `damage=0%` on pure buff/debuff rows, or rely on auto **Buff**/**Debuff** classification when `target=self` + defensive-only status.
- Set **CONFUSE**=`1` under **ENEMY TARGET** for confusion actions.
- **SELF TARGET** demo row should keep `target=SELF` — verify in Action Interaction Lab.

**Removed status columns (not applied in combat):** **FORTIFY**, **REFLECT**, **CLENSE** — ingested into `Actions.json` only; use **HARDEN** / **FOCUS** for defensive self-buffs instead.

**Removed columns:** **SELF DAMAGE** (column BE) — no longer pulled; use `target=self` instead of self-damage percent.

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
| P–Q | HEALTH | `healthPercent`, `healthGrowthPercent` — % of tuning baseline health (`enemySystem.baselineStats.health`; default 70). E.g. `125` = 125% of baseline at level 1; `3.36` = 3.36% of baseline per level after level 1. Optional `%` suffix accepted on pull. Legacy headers `baseHealth` / `healthGrowthPerLevel` still import. **Final displayed HP also multiplies by Combat Tuning knobs** (`baseHealthScale`, `globalEnemyHealthMult`, runtime difficulty) — sheet `100%` alone does not mean 100 displayed HP. |
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

Optional **TAGS** cell (column **E** on the standard layout): comma/semicolon list of extra tokens (pool gates like `environment`, `enemy`, `weapon`, `reserve_pool`, elements, etc.). Category and rarity are merged into runtime tags separately on import. **Push** writes TAGS from `Actions.json`; column **F** (e.g. `e(V)` formulas) is left unchanged.

### ACTIONS — RESERVE POOL column

Optional **RESERVE POOL** column (ensured on ACTIONS push, typically after **FINISHER**): mark with `1` / `true` to put the action in the **reserve pool**. Runtime tag: `reserve_pool` (also accepted in **TAGS** alone).

**Behavior:** the action remains in the entity’s action pool and can still fire from the **combo strip** or other explicit picks, but it is **excluded from default weighted rolls** (`Actor.SelectAction` and enemy non-combo fallback). Settings → Actions has a **Reserve Pool** checkbox that syncs the flag and tag.

### ACTIONS — CADENCES band (replaces compact DURATION / CADENCE / MECHANICS)

Independent cadence bundles are authored as **per-family triples** under row-1 context **`CADENCES`** (push appends missing columns; push **deletes** legacy columns when present — see below):

| Row 2 labels (per family) | Cell meaning |
|---------------------------|--------------|
| `TURN` / `ACTION` / `FIGHT` / `DUNGEON` | Enable (usually `1`) |
| `TURN DURATION` (etc.) | Application count (blank ⇒ 1) |
| `TURN →` (etc.) | Comma-separated **mechanic IDs**; magnitudes stay in detail columns |

**Example — one action with both TURN and ACTION bundles:**

| TURN | TURN DURATION | TURN → | ACTION | ACTION DURATION | ACTION → | COMBO | DAMAGE MOD |
|------|---------------|--------|--------|-----------------|----------|-------|------------|
| `1` | `1` | `hero_combo_threshold` | `1` | `1` | `hero_next_action_damage` | `3` | `10` |

**Legacy columns removed on push (typically K–Y):** Push detects and **deletes** (Sheets `DeleteDimension`):

1. Old per-family contexts **`TURN CADENCE` / `ACTION CADENCE` / `FIGHT CADENCE` / `DUNGEON CADENCE`** (and their duration / `→` siblings) — superseded by the single **`CADENCES`** band.
2. Compact **`DURATION` / `CADENCE` / `MECHANICS`** (historically K/L/M; later W/X/Y after the old triples were inserted).

The authoritative **`CADENCES`** triples are kept. Pull still accepts compact DURATION/CADENCE/MECHANICS when CADENCES triples are empty (for older CSV snapshots). ATTACK cadence normalizes to TURN.

### ACTIONS — CADENCE, DURATION, and MECHANICS (legacy pull-only)

Three related columns in the old cadence band (**accepted on pull** when CADENCES triples are empty; **deleted on push**):

| Row 1 context | Row 2 label | Content | Role |
|---------------|-------------|---------|------|
| `STATUS EFFECT` (existing) | `DURATION` | `1`, `2`, `3` only | Application **count** (legacy) |
| (existing) | `CADENCE` | `TURN`, `ACTION`, … | Application **type** (legacy) |
| **`MECHANICS`** | **`MECHANICS`** | multi-value list | Declarative checklist (legacy) |

**Authoring rules:**

- Prefer **CADENCES** triples above for new rows.
- **DURATION** = number only when using legacy columns (pull fallback only).
- **MECHANICS** does **not** drive combat by itself — hero dice, status, and mod columns remain required.

**Reference tabs (refreshed on ACTIONS push):**

| Tab | Purpose |
|-----|---------|
| **CADENCE_LIST** | Four rows — **TURN**, **ACTION**, **FIGHT**, **DUNGEON** — each with SUMMARY + DETAIL (uncompacted). Hover notes on the cadence keyword cells. |
| **MECHANIC_LIST** | One row per mechanic ID: DESCRIPTION + ALLOWED_CADENCES as full words (`TURN, FIGHT, DUNGEON`, never `T/F/D`). Hover note on each id explains the mechanic and flags redundancies (e.g. `hero_next_action_damage` ≠ swing `DAMAGE(%)`). |

ACTIONS header cells also get Sheets **notes** (hover) for CADENCES triples, TRIGGERS triples, and easy-to-confuse columns like DAMAGE vs DAMAGE MOD.

### ACTIONS — TRIGGERS band (3-column triples)

Independent outcome triggers are authored as **per-family triples** under row-1 context **`TRIGGERS`** (push appends any missing columns on the live header):

| Row 2 labels (per family) | Cell meaning |
|---------------------------|--------------|
| `ON KILL` (or `ON HIT`, `ON MISS`, …) | Enable / count (usually `1`). For `ON ROLL VALUE` this is the **attack total**; for `ON NATURAL ROLL` / `ON ROOMS CLEARED` it is the face or Nth-clear target. |
| `ON KILL SCOPE` | Cadence scope for a **lasting grant**: `TURN` / `ACTION` / `FIGHT` / `DUNGEON`. **Blank = instant** one-shot when the event fires. |
| `ON KILL →` | Comma-separated **mechanic IDs** pointing at existing columns. Magnitudes stay in those columns (`DAMAGE MOD`, `HEAL`, `WEAKEN`, …) — not in the → cell. |

**Example — on kill, grant fight-scoped +5 action damage:**

| ON KILL | ON KILL SCOPE | ON KILL → | DAMAGE MOD |
|---------|---------------|-----------|------------|
| `1` | `FIGHT` | `hero_action_damage` | `5` |

**Example — on miss, heal once (instant):**

| ON MISS | ON MISS SCOPE | ON MISS → | HEAL |
|---------|---------------|-----------|------|
| `1` | *(blank)* | `heal` | `10` |

Families ensured on push: ON HIT, ON MISS, ON CRIT, ON KILL, ON CONNECT, ON COMBO, ON COMBO END, ON CRIT MISS, ON FIRST HIT, ON AFTER MISS, ON ROOMS CLEARED, ON ROLL VALUE, ON NATURAL ROLL. Tier 3 `Actions.json` samples cover each family (e.g. `HIT HEAL`, `ROLL FIFTEEN`, `NATURAL SEVEN`).

Pull stores triples as `triggerBundles` / `triggerBundlesJson`. **Combat** (`ActionTriggerBundleApplicator`): each enabled triple with mechanic pointers fires those mechanics only when its WHEN matches; blank SCOPE = instant, otherwise lasting grant via TURN/ACTION/FIGHT/DUNGEON deposit APIs. Magnitudes still come from the pointed columns. Mechanics listed under any TRIGGERS → are skipped by whole-row status / sheet-mod apply (`gate_only_listed`). WHEN with a non-empty `→` is **not** merged into `triggerConditions`; empty-`→` enable cells still merge for legacy whole-row gating. Legacy count-only `ON KILL` / `TRIGGER CONDITIONS` cells keep working. **Constraint:** one magnitude per mechanic column per row — a trigger-attached `DAMAGE MOD` is owned by that trigger for the row.

**Runtime timing (cadence matrix):**

| Matrix column | Maps to `CADENCE` | Behavior |
|---------------|-------------------|----------|
| Turn bonus | `TURN` | Next roll / turn-scoped bonus queue |
| Action bonus | `ACTION` | Hit+combo deposits into an **additive bank** for the next hit+combo; duration ×N stacks values additively; bank clears when the room ends |
| Fight | `FIGHT` | Fight-scoped duration |
| Dungeon | `DUNGEON` | Dungeon-scoped duration |

**Implicit defaults when CADENCE and DURATION are both empty:**

- Hero/enemy **dice** and **stat** mods (`hero_combo_threshold`, `hero_stat_bonus`, …) → **`TURN` / `1`**
- **Next-action** mods (`hero_next_action_speed`, `enemy_next_action_damage`, …) → **`ACTION` / `1`**
- Mixed row (dice + next-action mod) → **`TURN` / `1`** + pull warning
- Instant status on the sheet (`weaken`, `slow`, `fortify`, …) → no cadence default
- **Stun, poison, burn, bleed** are **item-applied only** — use item mods, not the ACTIONS **MECHANICS** column, mechanic IDs, or combat pull. Legacy **STUN** / **POISON** / **BURN** / **BLEED** columns may still round-trip in spreadsheet JSON but are **not** mapped to `ActionData` on pull (`SpreadsheetColumnUsage.IngestedNotRuntime`).

**MECHANIC_LIST** (~33 IDs): `ActionMechanicsRegistry.AllMechanicIds` — only **GOOD** mechanics with **ON ACTIONS=TRUE**. Excluded from the dropdown but still work in-game: instant status columns, **CUT** mechanics (`silence`, `lifesteal`, `consume`, `multi_dice`, `exploding_dice`, `replace_next_roll`). **REDUNDANT** mechanic IDs removed (`damage`, `multi_hit`, `accumulations`, `threshold_bonus`, `stat_bonuses`) — use core columns instead.

**NEEDS IMPROVEMENT backlog** (not in MECHANIC_LIST): `expose`, `reflect`, `cleanse`, `self_damage`, combo routing (`combo_jump`, `chain_*`, …), `modify_room`. Trigger outcome + filter gates (`on_hit`, `on_connect`, `on_miss`, `on_crit`, `on_crit_miss`, `on_combo`, `on_combo_end`, `on_kill`, `on_roll_value`, `on_health_threshold`, `on_rooms_cleared`, `on_wield:…`, `on_first_hit`, `on_after_miss`, `if_clutch`, `if_same_action`, `if_different_action`, `if_last_enemy`, `if_source/target_under_dot`, `if_*_status`, `if_*_has_tag`) are runtime-wired via `ActionTriggerGate` + Settings/sheet `TRIGGER CONDITIONS` / `ON ROOMS CLEARED`.

**Multi-select in Google Sheets:**

1. Add a reference tab **`MECHANIC_LIST`** with one mechanic ID per row in column A (copy from `ActionMechanicsRegistry.AllMechanicIds` in code).
2. On ACTIONS → **MECHANICS** column: **Data → Data validation → Dropdown (from a range)** → `MECHANIC_LIST!A:A`, enable **Allow multiple selections**.
3. Pull/push stores a **comma-separated** cell (e.g. `hero_combo_threshold, weaken`) — same pattern as TAGS.

Fallback: type `hero_combo_threshold, weaken` manually if multi-select chips are unavailable. Legacy unprefixed hero IDs (`combo_threshold`, etc.) are normalized to `hero_*` on import.

### WEAPONS / ARMOR — tags

Optional `tags` column: comma-separated registry tags on push (e.g. `undead, boss`); pull also accepts JSON arrays or semicolon/pipe lists. Material prefix names (Bone, Steel, …) live on the **Prefix** tab and map to material tags at loot time.

### flavor (`FlavorText.json`)

**Push only** (gid `825117964`, tab title exactly `flavor`). One header row + one string per data row:

| section | bank | key | text |
|---------|------|-----|------|
| names | characterFirstNames | *(empty)* | Aric |
| environments | locationDescriptions | Forest | A dense forest… |
| environments | roomContexts | Forest/boss | This ancient grove… |
| classQualifiers | classNames | barbarian | barbarian |
| combatNarratives | firstBlood | *(empty)* | The first drop… |
| forms | roomEnter | displayName | Room Enter |
| forms | roomEnter | template | You step inside… |
| categories | intro | *(empty)* | Moss coats the walls. |

Built by `FlavorTextSheetConverter` from `FlavorText.json`. **PULL** leaves local FlavorText unchanged (logs a skip).

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
