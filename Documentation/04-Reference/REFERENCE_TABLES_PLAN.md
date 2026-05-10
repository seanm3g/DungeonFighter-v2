# Reference Tables Plan: Enemies, Environments, Equations

**Summary**: Create **interactive** reference tables (HTML and/or CSV) for enemies, environments, and equations. The **enemies table** includes **per-enemy scaling** columns (no game implementation required yet—the table is the design).

---

## 0. Format: best for copy/paste into Google Sheets

Deliver tables so you can **copy/paste directly into Google Sheets** (or open the file there):

- **Primary**: **TSV (tab-separated values)** files — e.g. `enemies.tsv`, `dungeons.tsv`, `rooms.tsv`, `environmental_actions.tsv`, `equations.tsv`, `equation_variables.tsv`. When you copy the file contents and paste into a Sheet, Google Sheets splits on tabs and puts each value in its own cell, so columns and rows line up. You can also use File > Import and upload the .tsv.
- **Alternative**: **CSV** files — use File > Import in Google Sheets and choose the CSV; or copy/paste and then use Data > Split text to columns (choose comma). CSV is fine if no cell contains commas; TSV is usually better when copy/pasting.
- Put files in e.g. `Documentation/04-Reference/`. Optional: a single markdown doc as a readable snapshot.

---

## 1. Enemies table

**Source**: [GameData/Enemies.json](GameData/Enemies.json) (9 enemies).

**Columns**:  
Name | Archetype | Health (override) | Strength | Agility | Technique | Intelligence | Armor | **Health per level** | **Attributes per level** | **Armor per level** | Actions | Living | Description  

- **Stat columns** (Health through Armor): override multipliers from JSON (e.g. 0.85, 1.2) or "base" where null; base comes from `EnemySystem.BaselineStats` × archetype in TuningConfig.
- **Per-enemy scaling (on the table only; no game implementation yet)**:
  - **Health per level** — amount added to health each level for this enemy (design: per enemy; current game uses one global value from `enemySystem.scalingPerLevel.health`).
  - **Attributes per level** — amount added to STR/AGI/TEC/INT each level for this enemy (design: per enemy; current game uses one global `scalingPerLevel.attributes`, often 0).
  - **Armor per level** — amount added to armor each level for this enemy (design: per enemy; current game uses one global `scalingPerLevel.armor`).
- For now, table rows can use the **current global defaults** (e.g. Health +5, Attributes 0, Armor 0 from TuningConfig) in those columns, with a note that the table is the **design** for per-enemy scaling when you implement it later. Or leave scaling columns blank/editable in the CSV/HTML so you can fill per-enemy values when you add them.
- One row per enemy (9 rows).

**Note**: In-game values today are `(baseline × archetype × override)` then `+ (level - 1) × globalScaling` then `× globalMultipliers` per `Code/Data/Enemy/EnemyFactory.cs`. The table's per-enemy scaling columns define the intended design (per enemy) for when you implement it.

---

## 2. Environments table(s)

- **Dungeons**: Name | Theme | Min level | Max level | Possible enemies | Rooms — from [GameData/Dungeons.json](GameData/Dungeons.json) (3 rows). Rooms column lists room types whose theme matches the dungeon theme (from Rooms.json).
- **Room types**: Name | Description | Theme | Is hostile | Environmental actions (name: weight, …) — from [GameData/Rooms.json](GameData/Rooms.json); can group by theme.
- **Environmental actions**: Id | Name | Theme | Room type | Type | Damage multiplier | Length | Description | Causes stun | Causes bleed | Causes weaken | Causes slow | Causes poison — from [GameData/EnvironmentalActions.json](GameData/EnvironmentalActions.json). Delivered as `environmental_actions.tsv`.

---

## 3. Equations table (game logic only)

Document only equations that affect **game logic**: character attributes/health, level-up stats/health, XP and progression, enemy stats/scaling/rewards, combat damage/speed, and hit/combo/crit thresholds. Exclude UI, layout, text display, settings persistence, etc.

- **equations.tsv**: Columns — Category | Name | Formula | Variables used | Config / source. Each row lists the variables used in that formula.
- **equation_variables.tsv**: Columns — Variable | Description | Source. Full list of variables that appear in equations, with description and source (config path or "Computed"/"Game state"/"From equipment", etc.).

---

## Deliverable

1. **TSV (or CSV) files** for copy/paste into Google Sheets: `enemies.tsv`, `dungeons.tsv`, `rooms.tsv`, `environmental_actions.tsv`, `equations.tsv`, `equation_variables.tsv` in `Documentation/04-Reference/`.
2. **Enemies table** must include the three per-enemy scaling columns (Health per level, Attributes per level, Armor per level) on every row; values can be global defaults or blank until you set per-enemy values.
3. Optional: Markdown snapshot of the same tables.

No game code changes required for per-enemy scaling; the table is the design for when you implement it.
