# DungeonFighter v2 — Overview

DungeonFighter v2 is a turn-based RPG written in C# (.NET) with a data-driven combat/action system, character progression, and an Avalonia UI. Gameplay is built around **actions** (attacks, heals, effects) that are selected and executed in a structured combat flow, with systems for combo routing, thresholds, roll modifiers, and post-hit processing.

## Core gameplay pillars

- **Turn-based combat**: Entities take turns selecting and executing actions.
- **Actions as data**: Most combat behavior is configured through JSON/spreadsheet-driven action data that maps into runtime `Action` objects.
- **Combo system**: Actions can chain; combo routing can jump/skip/repeat via `ComboRouter`.
- **Advanced mechanics**: Thresholds, roll modification, status effects, and multi-hit processing participate in the action execution pipeline.
- **Settings-driven tuning**: The settings UI persists changes to `GameSettings` and applies some updates immediately (e.g., action pool refresh) while other settings apply on next use.

## Key systems (high-level)

- **Google Sheets ↔ JSON (enemies & environments)**: [`GameData/SheetsConfig.json`](GameData/SheetsConfig.json) can list published CSV URLs (`enemiesSheetUrl`, `environmentsSheetUrl`). `PULL_SHEETS` / `GameDataSheetsPullService` writes **`Enemies.json`** and **`Rooms.json`** via [`JsonArraySheetConverter`](Code/Data/JsonArraySheetConverter.cs) (one header row; any extra column becomes JSON). **`PUSH_SHEETS`** uses [`GameData/SheetsPushConfig.json`](GameData/SheetsPushConfig.template.json) (`enemiesSheetTabName`, `environmentsSheetTabName`); if those entries were left blank (common on older configs that only set weapons/armor), the game now defaults them to **`ENEMIES`** and **`ENVIRONMENTS`** so local `Enemies.json` / `Rooms.json` still upload—edit the config if your tab titles differ. **ENEMIES tab** canonical columns: `name`, `archetype`, then one column per nested stat (`overrides.health`, `overrides.strength`, `overrides.agility`, `overrides.technique`, `overrides.intelligence`, `overrides.armor`, `baseAttributes.strength`, …, `growthPerLevel.intelligence`), then `baseHealth`, `healthGrowthPerLevel`, `actions`, `isLiving`, `description`, `colorOverride` (still a JSON object cell when used). Import still accepts a legacy single **`overrides`** / **`baseAttributes`** / **`growthPerLevel`** column as JSON; dotted columns merge on top. **`actions`** may be pipe-separated (`JAB|TAUNT`) and are normalized to a JSON array on import. **ENVIRONMENTS tab** (→ `Rooms.json`): `name`, `description`, `theme`, `isHostile`, `actions` (JSON array of `{name,weight}`), optional **`enemies`** (same shape—weighted enemy template names from `Enemies.json`). When `enemies` is non-empty for a room, hostile generation uses that pool (weighted) and ignores the dungeon’s `possibleEnemies` filter for template choice.

- **Actions spreadsheet (`GameData/Actions.json`)**: Rows can be tagged for **non-hero** pools. Use **category** `ENEMY` with **tags** including `enemy` for enemy-only attacks (so loot/weapon resolution does not assign them to the hero). Use **category** `ENVIRONMENT` with **tags** including `environment` plus a **lowercase dungeon theme** (e.g. `forest`, `crypt`); optional `room-{type}` in tags scopes data. Environment hazards use spreadsheet **target** `AOE`. Runtime room loading filters environment actions by theme (or the `generic` tag). Environmental rows use the legacy `EnvironmentalActions.json` **id** as the spreadsheet **action** key (**uppercase**, words separated by spaces; room-scoped legacy ids omit the old `room_` prefix) so every row stays unique.

- **Settings system**: Avalonia settings panels save through an orchestrator and apply via a single apply service. See `Documentation/05-Systems/SETTINGS_SYSTEM_ARCHITECTURE.md`.
- **Action execution**: Action execution is orchestrated and delegated to specialized executors and processors (attack/heal, multi-hit, etc.). See `Documentation/01-Core/ARCHITECTURE.md` for file entry points.
- **Character effects**: Character/actor state includes transient “next action” modifiers (speed/damage/amp/multihit) that should affect the following action execution.

## Current focus area (for this change)

### Action Interaction Lab — controls pop-out layout

The **Action Lab** auxiliary window lists the current **foe**, **HP**, a scrollable **enemy type** picker (▲ types / ▼ types), then turn info, d20 grid, and the scrollable **action catalog**. The enemy-type list shows **five** visible rows (was two) so more foes are visible at once; the catalog uses the remaining vertical space above the fixed footer.

### Action Interaction Lab — enemy level (right panel)

The **Action Interaction Lab** is a stepped combat sandbox (cloned hero, lab enemy, forced catalog action, fixed or random d20). The combat **right panel** shows the current foe (HP, combat stats, thresholds). This change adds:

- **Enemy level** (`Lvl N`) on the right panel in lab combat, styled like the hero’s level on the left.
- **Left / right click** on that row to raise or lower enemy level (1–99). The lab **rebuilds** the enemy at the new level (loader enemies via `EnemyLoader.CreateEnemy`; default dummy via `TestCharacterFactory` with per-level HP/damage/armor/speed scaling), then **clears step history** and **re-bootstraps** combat so encounter state stays consistent.
- **Undo / replay** and **batch simulation** snapshots carry the effective enemy level (`LabCombatSnapshot.EnemyLevel`, `CaptureSimulationSnapshot`).
- **Direct-stat dummies**: `Enemy.GetEffectiveStrength()` returns base **Damage** (so damage math and the DMG line match); the panel also shows **attack speed**.

### MultiHit Mod (Hero base stats → next action) — prior milestone

The settings UI exposes **Hero base stats** including action speed %, action damage %, **MultiHit mod**, and amp mod %. Those modifiers apply to the hero’s **next** executed action; prior work ensured MultiHit mod flows through data, execution, and tests.

