# Task List - DungeonFighter v6.2

**Last Updated**: April 13, 2026  
**Current Status**: Production Ready  
**Version**: 6.2

---

## 📋 Current Status Summary

- **Recent**: ✅ **STATS Speed stuck at 0.0s** — `TuningConfig.json` had `combat.baseAttackTime` (and `minimumAttackTime`) at **0**, so attack time sat on the hard floor and the left panel showed `0.0s` regardless of weapon/AGI. Fix: defaults **8s** / **0.1s** in JSON; `CombatConfig.EnsureValidCombatTimingDefaults()` after every `GameConfiguration` load; STATS line uses **F2** for seconds. Files: `CombatConfig.cs`, `GameConfiguration.cs`, `TuningConfig.json`, `CharacterPanelRenderer.cs`, `CombatConfigTests.cs`.
- **Recent**: ✅ **Google Sheets multi-tab push/pull** — `SheetsConfig.json` optional URLs (`weaponsSheetUrl`, `modificationsSheetUrl`, `armorSheetUrl`, `classPresentationSheetUrl`) plus existing `actionsSheetUrl`; `SheetsPushConfig.json` optional tab names (`weaponsSheetTabName`, …). Pull: `GameDataSheetsPullService` / **Resync from Google Sheets**. Push: `GameDataSheetsPushService` / **Push game data to Google Sheets** (clears Actions below the two-row header; other tabs from row 1 with canonical column headers). CLI: `PUSH_SHEETS`, `PULL_SHEETS`; scripts `Scripts/push-sheets.ps1`, `pull-sheets.ps1`. Tests: `JsonArraySheetConverterTests`, `ClassPresentationSheetConverterTests`. Docs: `GOOGLE_SHEETS_INTEGRATION.md`.
- **Recent (prior)**: ✅ Action Interaction Lab — **Exit lab** / ESC from lab now returns to **Settings** when no save character is loaded (`ShowGameLoop` previously skipped rendering because the lab hero is not `CurrentPlayer`); with a loaded hero, exit still returns to **GameLoop** (`GameCoordinator.ExitActionInteractionLab`, `EscapeKeyHandler`).
- **Major Features**: ✅ All Core Systems Complete
- **Code Quality**: ✅ Well-Organized with Design Patterns
- **Documentation**: ✅ Comprehensive (90+ documents)
- **Testing**: ✅ 27+ Test Categories; Settings Testing tab is mechanics & reliability only (see [SETTINGS_TESTING_MENU.md](05-Systems/SETTINGS_TESTING_MENU.md)); full suite via `Scripts\run-tests.bat` / `--run-tests`
- **Production Ready**: ✅ Yes

---

## ✅ COMPLETED MAJOR FEATURES (v6.2)

### Core Systems ✅
1. ✅ **Modern GUI Implementation** - Avalonia-based GUI with ASCII canvas rendering
2. ✅ **Combat System** - Turn-based with combo mechanics and narrative
3. ✅ **Character System** - Progression, equipment, and specialized managers
4. ✅ **Enemy System** - 18+ types with AI and scaling
5. ✅ **Dungeon System** - Procedural generation with 10 themed dungeons
6. ✅ **Action System** - 30+ advanced actions with special mechanics
7. ✅ **Item System** - Generation, loot, and inventory management
8. ✅ **Data System** - JSON-driven content management
9. ✅ **Class presentation tuning** — Settings → Classes edits `classPresentation` in TuningConfig. Panel section order: **Starting name**, **Path display names**, **Class point tier thresholds**, **Solo–trio tier words**, **2-class hybrid cores**, **Third-path suffix (per path)** (`attributeModifierMace` / Sword / Dagger / Wand — appended only when 3+ paths are active **and** the third-highest path’s points ≥ **`tierThresholds[0]`**), **Quad tier words**, path preview, live summary. Weapon-path ranked title tier words (`Adept` / `Expert` / `Master` / `Paragon`), pre-tier label (`Novice`), and hybrid joiner (`-`) are **fixed in code** (not in JSON or Sheets). Quad tier strings and other class presentation fields remain in JSON and are **preserved** on save. **HUD class title** via `AttributeClassNameComposer`: solo/duo **prefix + core** only; **3+ paths** use duo core from **top two**, then third-path phrase only if third-highest pts ≥ first gate. **Weapon-path unlock title** (`GetWeaponPointsClassTitle`) uses ranked fragments and the hybrid joiner when the top two paths differ in tier band. **Weapon points** unchanged for unlocks. Unit tests: `AttributeClassNameComposerTests`, `CharacterProgressionClassReferenceTests`, `ClassPresentationConfigTests`.

### GUI & Visual Features ✅
1. ✅ **Action-info strip (fixed slots)** — Top center strip always shows at least 5 panels when a character is present (`LayoutConstants.ACTION_INFO_STRIP_FIXED_SLOT_COUNT`, `ActionInfoStripLayout.GetDisplayPanelCount`); empty slots use a dim border; tooltips and strip drag use the same display count (drop on empty slot ignored). See `DungeonRenderer.RenderActionInfoStrip`, `MouseInteractionHandler`.
1b. ✅ **Left-panel STATS/GEAR hover tooltips** — Per-line `lphover:*` targets, formulas via `LeftPanelTooltipBuilder`, center overlay in `RenderActionStripTooltipOverlay` (priority over strip when active). Secondary stats (magic find, roll bonus, etc.) always shown under STATS when the section is open.
2. ✅ **Actions workspace (inventory)** — Pool-centric center panel with `cpi:` pointer tokens; sequence vs pool labels on right panel; equip/unequip sequence prune messaging (see `ComboManagementRenderer`, `ComboPointerInput`, `InventoryComboManager`).
3. ✅ **Action pool hover tooltip** — Tooltip for POOL rows draws after the pool list, anchored just below the hovered line, horizontally straddling the center/right boundary (`InventoryRightPanelLayout`, `RightPanelRenderer.DrawPoolHoverTooltipIfNeeded`); strip overlay skips pool hover (`DungeonRenderer.RenderActionStripTooltipOverlay`).
4. ✅ **Title Screen Animation** - 30 FPS color transitions
5. ✅ **Item Color System** - Rarity-based coloring with 7 tiers
6. ✅ **Color Configuration System** - 166+ templates, 200+ keywords
7. ✅ **Inventory Management** - Equip, unequip, discard, trade-up, return to game menu (combo editing via actions workspace / pointer)
8. ✅ **Chunked Text Reveal** - Progressive text display
9. ✅ **Dungeon Shimmer Effects** - Continuous animations
10. ✅ **Persistent Layout** - Character panel always visible
11. ✅ **Combat Display** - Renders with proper color parsing
12. ✅ **Right panel border stability** — Purple frame no longer stacks duplicate `CanvasBox` entries on incremental renders (`RightPanelRenderer.ClearBoxesInArea`); 1px border strokes are inset in `CanvasPrimitivesRenderer` to avoid edge clipping/flicker; effective visible width uses floor+epsilon and clamps to grid width (`LayoutConstants.UpdateEffectiveVisibleWidth`).
13. ✅ **Dungeon entry action strip** — `RenderCoordinator.PerformRender` re-reads context on the UI thread before layout, coalesces overlapping render requests, and `DisplayRenderer` no longer clears rows inside the action-info strip band (`DisplayRendererClearBandRegressionTests`).
14. ✅ **Dungeon strip vs CurrentPlayer** — `DisplayStateCoordinator.IsCharacterActive` matches by registry id; `UIContextCoordinator.SetCharacter` / `SetCurrentEnemy` / `RenderStateManager` / `RenderCoordinator` use the same rules; `PerformRender` falls back to `CurrentPlayer` for layout+strip; `RebuildCharacterActionsComboTests` guards combo after double rebuild.
15. ✅ **Per-character display buffer sync** — `CanvasTextManager.DisplayManager` stayed on a stale manager while `MessageRouter` wrote to `GetDisplayManagerForCharacter(player)`, so reactive `PerformRender` cleared/redrew the wrong buffer and wiped the strip. `SwitchDisplayBufferToCharacter` + `EnsureDisplayManagerForPlayer` align current manager with the dungeon player before clear/restore/render (`DungeonOrchestrator`, `DungeonDisplayManager.StartDungeon`, `CanvasRenderer` dungeon/combat paths).
16. ✅ **Strip layout character vs stale context reference** — `PerformRender` previously preferred `RenderState.CurrentCharacter` (UI context) over `GetActiveCharacter()`, so a non-null but stale `Character` instance could win and the action strip showed the wrong/empty combo. Resolution now prefers the registered active character (`RenderCoordinator.ResolveLayoutCharacter`, `LayoutCharacterResolutionTests`).
17. ✅ **Dungeon SetUIContext vs display buffer** — `SetCharacter` cleared the center buffer whenever the context reference changed; after `ShowRoomEntry` filled the buffer, `SetUIContext` → `SetCharacter` could wipe that content and queue `PerformRender` (full canvas clear + strip race). Dungeon sync now uses `SetCharacterContextOnly`; `SetCharacter` skips buffer clear when syncing from a stale/unregistered reference to the canonical active player (`UIContextCoordinator.ShouldClearDisplayBufferForCharacterChange`).
18. ✅ **Action strip paint order** — The strip was drawn inside the center callback *before* `RightPanelRenderer`, so the right panel could paint over the center column when layout overlap occurred (tight effective width / first frame). Strip is now drawn after full persistent chrome (`RenderCoordinator`, `CanvasRenderer` dungeon/combat/inventory paths); `CENTER_PANEL_WIDTH` clamped with `Math.Max(1, …)` to avoid negative widths when effective visible width is tiny.
19. ✅ **Crit-line / ForceRender vs per-character buffer** — Dungeon narrative uses `GetDisplayManagerForCharacter(player)` while crit-line animation and `RestoreDisplayBufferRendering` called `DisplayManager.ForceRender()` (whatever `currentDisplayManager` was), repainting the wrong buffer ~24fps and wiping correct chrome. Use `CanvasTextManager.ForceRenderForActiveCharacter` / `ForceFullLayoutRenderForActiveCharacter` so reactive renders target the same manager as `MessageRouter`.
20. ✅ **DisplayManager getter vs per-character keys** — The initial `CenterPanelDisplayManager` was not registered under `__default__`, and `DisplayManager` only synced to the active player when `currentDisplayManager` was null — so after menus/pre-dungeon it could stay on the default buffer while `MessageRouter` wrote to the id-keyed manager. `DisplayManager` now always `SwitchToCharacterDisplayManager(GetActiveCharacter())` when a player is active; constructor registers the first manager in the dictionary.

### Code Quality ✅
1. ✅ **Refactoring Complete** - ~1500+ lines eliminated through design patterns
2. ✅ **Design Patterns** - 12+ patterns implemented
3. ✅ **Code Organization** - Organized by system domains
4. ✅ **Documentation Consolidation** - 90+ comprehensive documents

---

## 🔄 CURRENT DEVELOPMENT TASKS

### 1. Combat Freeze Fix ✅
**Status**: Completed  
**Description**: Fixed critical bug causing game freeze during combat  
**Solution**: Removed non-existent `WaitForMessageQueueCompletionAsync()` calls and replaced with `Task.Delay(50)` for UI synchronization
**Files Modified**: `Code/Combat/CombatManager.cs`
**Verification**: All 4 method calls fixed, no linter errors

### 2. Real-Time Combat Display ✅
**Status**: Completed  
**Description**: Added real-time UI rendering during combat  
**Solution**: Added `Dispatcher.UIThread.InvokeAsync()` calls after each action to yield to UI thread  
**Files Modified**: `Code/Combat/CombatManager.cs`  
**Result**: Combat actions now display in real-time as they happen

### 2b. Left-panel stat & gear hover tooltips ✅
**Status**: Completed  
**Description**: Hover on left-panel STATS shows formula/breakdown tooltips; GEAR slots show item detail (stats, gear action, mods). Uses same center-panel tooltip box as action strip.  
**Implementation**: `LeftPanelHoverState`, `LeftPanelTooltipBuilder`, `lphover:*` hit targets in `CharacterPanelRenderer`, `GetElementAt` last-wins; secondary stat lines always visible when STATS is expanded.  
**Tests**: `LeftPanelHoverStateTests`, `LeftPanelTooltipBuilderTests`, `CanvasInteractionManagerHitTestTests` in UI suite.

### 3. Narrative System Validation 🔄
**Status**: In Progress  
**Description**: Verify narrative system works after refactoring  
**Tasks**:
- [x] Verify BattleNarrative integration - working with new formatters
- [ ] Test narrative event detection in combat
- [ ] Validate narrator output formatting with ColoredText
- [ ] Document narrative flow updates

### 3. Environmental Actions System 🔄
**Status**: In Progress  
**Description**: Get environmental actions working after refactoring  
**Tasks**:
- [ ] Verify EnvironmentalActionInitializer loading
- [ ] Test environmental action execution
- [ ] Validate effect application
- [ ] Document environmental mechanics

### 4. Balance Tuning System 🔄
**Status**: In Progress  
**Description**: Dynamic tuning with save/load capabilities  
**Tasks**:
- [ ] Test tuning parameter save functionality
- [ ] Implement load from saved configs
- [ ] Add export/import features
- [ ] Document tuning workflow

### 5. Large-Class Refactor (Plan) ✅
**Status**: Completed  
**Description**: Refactored nine largest files per plan (split DTOs, partials, control-bundle overloads, extracted phases/helpers).  
**Completed**:
- [x] ActionLoader: split ActionData into ActionData.cs  
- [x] CharacterSaveService: centralize error reporting, trim debug, extract helpers  
- [x] ActionExecutionFlow: extract phases from Execute into private static methods  
- [x] CombatEffectsSimplified: extract ProcessPoisonDamage/ProcessBurnDamage  
- [x] CharacterEffects: split into ComboState, RollAndShield, Reroll, ActionBonus, NextAttack  
- [x] CanvasRenderer: partial class split by menu/inventory/combat-dungeon  
- [x] CanvasUICoordinator: partial class split (IUIManager, Context, Rendering)  
- [x] SettingsManager: prefer control-bundle overloads, move logic into bundle path  
- [x] SpreadsheetActionJson: partial split by column groups (Core, Stats, StatusEffects, Mechanics, Triggers, Thresholds)  

### 6. Screen Flow Coordination ✅
**Status**: Completed (Phase 1)  
**Description**: Centralize high-level screen transitions through a dedicated coordinator to simplify UI flow and debugging  
**Tasks**:
- [x] Create `GameScreenCoordinator` to manage core screen transitions (GameLoop, DungeonCompletion, Inventory)  
- [x] Wire `Game.ShowGameLoop`, `Game.ShowDungeonCompletion`, and `Game.ShowInventory` through `GameScreenCoordinator`  
- [x] Simplify `InventoryMenuHandler.ShowInventory()` to delegate through the coordinator  
- [x] Document the new screen coordination pattern in `UI_RENDERER_ARCHITECTURE.md`  
- [ ] Gradually migrate remaining display entry points (e.g., dungeon selection, death screen) to use `GameScreenCoordinator` (Future)  

### 6. Actions Settings Game Integration ✅
**Status**: Completed  
**Description**: Plug Actions settings menu data into the game (modifiers, refresh on close, Rarity/Category, docs/UI).  
**Tasks**:
- [x] Modifiers (SpeedMod, DamageMod, MultiHitMod, AmpMod) apply only to next action/ability (cadence + duration); ACTION = consume on next roll, ABILITY = consume on success; AmpMod % multiply, SpeedMod positive = faster
- [x] Apply consumed modifiers in DamageCalculator, ActionSpeedCalculator, multi-hit, combo amp
- [x] Refresh current player action pool when closing Settings after saving an action
- [x] Rarity = selection weight when choosing actions for items; Category = pool filter with 5% bypass
- [x] Update ACTIONS_SETTINGS_MENU_PLAN.md, KEYWORD_ABILITY_ACTION.md (end-user terminology), ActionFormBuilder Modifiers section copy

### 6b. Actions Settings Verification (Checklist + Tests) ✅
**Status**: Completed  
**Description**: Systematic verification of Actions menu: UI checklist, apply-to-game tests, Testing menu flow.  
**Tasks**:
- [x] Component 1: Add ACTIONS_SETTINGS_UI_CHECKLIST.md (filters, list, Create/Delete, form, Default/Starting)
- [x] Component 2: Add SettingsApplyServiceTests and ActionsTabManagerTests (ApplyAfterSave, RefreshCurrentPlayerActionPool); register in UISystemTestRunner and RunActionsSettingsIntegrationTests
- [x] Component 3: Document Testing menu steps in checklist (Actions Settings Integration, Action Mechanics, Combat, Run All)
- [x] Verification: Run Settings → Testing → "Actions Settings Integration" (and optionally "Run All") after changing actions to confirm mechanics
**Files**: `Documentation/02-Development/ACTIONS_SETTINGS_UI_CHECKLIST.md`, `Code/Tests/Unit/UI/SettingsApplyServiceTests.cs`, `Code/Tests/Unit/UI/ActionsTabManagerTests.cs`

---

## 📚 DOCUMENTATION TASKS

### Root Level Documentation ✅
1. ✅ **OVERVIEW.md** - Created with quick start and feature overview
2. ✅ **TASKLIST.md** - This file, tracking all tasks
3. ✅ **README.md** - Enhanced with performance targets and quick start

### Refactoring History Organization ✅
1. ✅ **REFACTORING_HISTORY.md** - Consolidated recent refactoring changes
2. ✅ **Archive Old Docs** - Organized refactoring summaries

### Documentation Updates 🔄
- [ ] Update PERFORMANCE_NOTES.md with latest metrics
- [ ] Review and update DEBUGGING_GUIDE.md
- [ ] Validate all code examples in documentation
- [ ] Create VIDEO_TUTORIALS.md for onboarding

---

## 🎯 FUTURE ENHANCEMENTS (Backlog)

### UI Enhancements
- [ ] Equipment comparison tooltips
- [ ] Advanced combo management UI
- [ ] Sound effects and audio system
- [ ] Additional color themes

### Gameplay Features
- [ ] Additional dungeon themes
- [ ] More enemy types (20+)
- [ ] Quest system
- [ ] Achievement system
- [ ] Leaderboards

### Technical Improvements
- [ ] Multiplayer support
- [ ] Cloud save system
- [ ] Performance profiling
- [ ] Accessibility features

### Platform Support
- [ ] Unity port for enhanced graphics
- [ ] Mobile platform support
- [ ] Web version
- [ ] Console port

---

## 📊 CODE METRICS (Latest)

| Metric | Value | Status |
|--------|-------|--------|
| Total C# Files | 290+ | ✅ Well organized |
| Architecture Patterns | 12+ | ✅ Well-designed |
| Test Categories | 27+ | ✅ Comprehensive |
| Documentation Files | 90+ | ✅ Thorough |
| Largest File | <300 lines | ✅ Maintainable |
| Design Pattern Usage | 100% | ✅ Architecture-first |

### Recent Refactoring Results
- BattleNarrative: 550 → 118 lines (78.5% ↓)
- Environment: 763 → 182 lines (76% ↓)
- CharacterEquipment: 590 → 112 lines (81% ↓)
- GameDataGenerator: 684 → 68 lines (90% ↓)
- **Total Lines Eliminated**: 1500+ lines

---

## 🔧 DEVELOPMENT WORKFLOW

### Before Starting Work
1. Read `OVERVIEW.md` (feature overview)
2. Check `Documentation/01-Core/ARCHITECTURE.md` (system design)
3. Review `Documentation/02-Development/DEVELOPMENT_GUIDE.md`
4. Follow patterns in `Documentation/02-Development/CODE_PATTERNS.md`

### During Development
1. Make incremental changes
2. Test frequently using built-in test suite
3. Keep code focused (single responsibility)
4. Use established design patterns

### After Development
1. Run full test suite (27+ categories)
2. Update relevant documentation
3. Check performance with metrics tool
4. Commit with descriptive message

---

## 🧪 TESTING REQUIREMENTS

### Pre-Commit Testing
- [ ] Run relevant system tests
- [ ] Verify balance with balance analysis
- [ ] Check for regressions
- [ ] Validate performance targets

### Test Categories Available
- Character Leveling & Stats
- Item Creation & Properties
- Combat Mechanics
- Combo System Tests
- Battle Narrative Generation
- Enemy Scaling & AI
- Intelligent Delay System
- Balance Analysis
- And 19+ more...

---

## 📁 FILES & ORGANIZATION

### Root Level Files
- `OVERVIEW.md` - This file overview
- `TASKLIST.md` - Development tasks (this file)
- `README.md` - Installation and getting started

### Key Documentation Folders
- `Documentation/01-Core/` - Essential guides
- `Documentation/02-Development/` - Development resources
- `Documentation/03-Quality/` - Testing and debugging
- `Documentation/04-Reference/` - Quick references
- `Documentation/05-Systems/` - System-specific docs

### Code Organization
- `Code/Actions/` - Action system
- `Code/Combat/` - Combat mechanics
- `Code/Config/` - Configurations
- `Code/Data/` - Data loaders and generators
- `Code/Entity/` - Character and enemy systems
- `Code/Game/` - Main game loop
- `Code/Items/` - Item system
- `Code/UI/` - User interface (Console + Avalonia)
- `Code/Utils/` - Utilities and helpers
- `Code/World/` - Dungeon and environment
- `Code/Tests/` - Test framework

### Data Files
- `GameData/Actions.json` - Action definitions
- `GameData/Enemies.json` - Enemy configurations
- `GameData/Weapons.json` - Weapon data
- `GameData/Armor.json` - Armor data
- `GameData/Dungeons.json` - Dungeon definitions
- `GameData/TuningConfig.json` - Balance parameters
- And 15+ additional JSON configuration files

---

## 🎓 LEARNING RESOURCES

### For New Developers
1. Start: `OVERVIEW.md` (this repository overview)
2. Next: `Documentation/01-Core/ARCHITECTURE.md` (system design)
3. Then: `Documentation/02-Development/DEVELOPMENT_GUIDE.md` (workflow)
4. Practice: Follow patterns in `Documentation/02-Development/CODE_PATTERNS.md`

### For Problem Solving
- Common Issues: `Documentation/03-Quality/PROBLEM_SOLUTIONS.md`
- Debugging: `Documentation/03-Quality/DEBUGGING_GUIDE.md`
- Performance: `Documentation/03-Quality/PERFORMANCE_NOTES.md`
- Quick Lookup: `Documentation/04-Reference/QUICK_REFERENCE.md`

### For System Understanding
- Overall: `Documentation/01-Core/ARCHITECTURE.md`
- Combat: `Documentation/05-Systems/COMBAT_SYSTEM_DEEP_DIVE.md`
- Characters: `Documentation/05-Systems/CHARACTER_SYSTEM_DEEP_DIVE.md`
- UI: `Documentation/05-Systems/UI_SYSTEM_REFERENCE.md`

---

## 📝 RECENT CHANGES (v6.2)

### Documentation
- ✅ Created root-level OVERVIEW.md
- ✅ Created root-level TASKLIST.md
- ✅ Enhanced README.md with performance targets
- ✅ Consolidated refactoring documentation
- ✅ Organized REFACTORING_HISTORY.md

### Code Refactoring
- ✅ BattleNarrative refactored (78.5% smaller)
- ✅ Environment refactored (76% smaller)
- ✅ CharacterEquipment refactored (81% smaller)
- ✅ Improved design patterns throughout

### Features
- ✅ Modern Avalonia GUI
- ✅ Item color system with 7 tiers
- ✅ Title screen animations
- ✅ Persistent layout system
- ✅ Chunked text reveal

---

## ✨ VERSION HISTORY

### v6.2 (Current - Production Ready)
- Modern GUI implementation (Avalonia)
- Comprehensive refactoring (1500+ lines eliminated)
- Enhanced documentation
- All core systems complete and tested

### v6.1
- Environment system enhancements
- Combat improvements
- Equipment system refactoring

### v6.0
- Major architecture overhaul
- Design pattern implementation
- Code quality improvements

---

## 🚀 GETTING STARTED

### Quick Start
```bash
cd Code
dotnet run
```

### Developer Setup
1. Read `OVERVIEW.md` (this directory)
2. Read `Documentation/01-Core/ARCHITECTURE.md`
3. Read `Documentation/02-Development/DEVELOPMENT_GUIDE.md`
4. Clone the repo and start exploring code

### Running Tests
In-game: Settings → Tests → [Choose Category]

---

## 📞 SUPPORT & QUESTIONS

### Documentation Resources
- Architecture: `Documentation/01-Core/ARCHITECTURE.md`
- Development: `Documentation/02-Development/DEVELOPMENT_GUIDE.md`
- Testing: `Documentation/03-Quality/TESTING_STRATEGY.md`
- Quick Help: `Documentation/04-Reference/QUICK_REFERENCE.md`

### Common Questions
- Q: How do I add a new action?  
  A: See `CODE_PATTERNS.md` and `Actions.json`

- Q: How do I balance the game?  
  A: Use Tuning Console or modify `TuningConfig.json`

- Q: Where are the tests?  
  A: Access via Settings menu (in-game) or `Code/Tests/`

- Q: How do I debug?  
  A: See `DEBUGGING_GUIDE.md`

---

**Last Updated**: April 12, 2026  
**Maintained By**: Development Team  
**Status**: Active Development

