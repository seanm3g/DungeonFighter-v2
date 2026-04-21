# Tasklist

This file tracks the work currently in progress. Only items listed here should be modified/implemented.

## Active

_(none)_

## Completed (reference)

- [x] **Data / Sheets:** Enemies & environments spreadsheet import support
  - [x] `JsonArraySheetConverter`: full `EnemyData`-aligned canonical headers; `enemies` on environments; pipe-separated `actions` normalization for ENEMIES CSV
  - [x] `RoomData` + `RoomEnemyData`, `RoomLoader` → `Environment` → `EnemyGenerationManager` weighted room spawn pool; `RoomDataValidator` + `ReferenceValidator` for `enemies` references
  - [x] Tests: `JsonArraySheetConverterTests` (full enemy stats round-trip, pipe actions, environments with `enemies`)
  - [x] Docs: `OVERVIEW.md` (Sheets subsection) + this task list

- [x] **UI:** Action Lab controls pop-out — show more enemy type rows, fewer catalog rows (`ActionLabControlsRenderer`).

- [x] **Feature:** Action Lab — interactive enemy level on right combat panel
  - [x] Session: `_labEnemyBaseLevel`, `_labPanelEnemyLevelDelta`, `ApplyLabEnemyLevelDelta`, `BuildLabEnemyFromPanelState`, restore + `CaptureSimulationSnapshot`
  - [x] UI: `Lvl` row + `rphover:enemy:level` click target when rendering lab combat; DMG/Spd lines aligned with direct-stat enemies
  - [x] Input: `ActionLabRightPanelEnemyAdjustment` + `MouseInteractionHandler` left/right click
  - [x] `TestCharacterFactory.CreateTestEnemy(..., level)` scaling; `Enemy.UsesDirectCombatStats` / `GetEffectiveStrength` for direct damage
  - [x] Tests: `ActionInteractionLabTests`, `ActionLabEncounterSimulatorTests`
  - [x] Docs: `OVERVIEW.md` + this task list

- [x] **Data:** Enemy + environment actions in `GameData/Actions.json` (spreadsheet format) with `enemy` / `environment` tags; loader and weapon fallbacks exclude them from the hero.

- [x] **Bugfix:** Hero base stats “MultiHit mod” applies to the next action (see `OVERVIEW.md` milestone note)

