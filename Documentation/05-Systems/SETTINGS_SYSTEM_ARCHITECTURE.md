# Settings System Architecture

## Overview

The settings system provides a comprehensive UI for managing game configuration, including gameplay settings, difficulty adjustments, text delays, animations, and developer tools. Saved settings apply immediately to the running game where applicable (action pool, text delays, gameplay/difficulty on next use).

## Architecture

### Component Structure

```
SettingsPanel (UI)
├── SettingsPanelCatalog (category → panel type; main content area panels)
├── PanelHandlerRegistry (category → ISettingsPanelHandler)
├── SettingsSaveOrchestrator (save coordination; delegates to handlers and tab managers)
├── SettingsApplyService (post-save: apply to running game)
├── SettingsManager (core settings, gameplay/difficulty, text delay persistence)
├── SettingsInitialization (tab initialization: GameVariables, Actions)
├── GameVariablesTabManager
├── ActionsTabManager
├── ItemModifiersTabManager
├── ItemsTabManager
├── StatusEffectsTabManager
├── Panel handlers (Gameplay, TextDelays, Appearance, Testing)
│   └── ISettingsPanelHandler: WireUp, LoadSettings, SaveSettings
└── SettingsActionTestGenerator
```

### Key Components

#### 1. SettingsPanel (UI Layer)
- **File**: `Code/UI/Avalonia/SettingsPanel.axaml` / `SettingsPanel.axaml.cs`
- **Responsibility**: Main UI container with category list and content areas
- **Content areas**: Main (ContentScrollViewer), Testing, Actions (separate areas for layout)
- **Panel creation**: Uses `SettingsPanelCatalog.CreatePanel(categoryTag)` for main-area panels; explicit branches for Testing and Actions
- **Panel resolution**: `GetPanelForCategory(categoryTag, currentlyDisplayed)` returns the displayed panel if it matches the tag, else the cached panel from `loadedPanels`. Passed to the orchestrator as `GetPanelForCategoryResolver` so save uses a single resolution path.
- **Initializers**: Table-driven `panelInitializers` (category → action) and `PanelInitializerContext` replace the long switch in `InitializePanelHandlers`; adding a new tab is "add to catalog + add initializer."
- **Save flow**: Resolves currently displayed panel, calls `SettingsSaveOrchestrator.SaveSettings(displayed)`, then `SettingsApplyService.ApplyAfterSave(result, gameStateManager)` when save succeeds. No duplicate "apply to game" on window close.

#### 2. SettingsPanelCatalog
- **File**: `Code/UI/Avalonia/Managers/Settings/SettingsPanelCatalog.cs`
- **Responsibility**: Single mapping from category tag to panel type for the main content area
- **Categories**: Gameplay, GameVariables, StatusEffects, TextDelays, Appearance, ItemModifiers, Items, BalanceTuning, About
- **Testing and Actions** use different content areas and are not created via the catalog

#### 3. Panel handlers and registry
- **Interface**: `ISettingsPanelHandler` — `PanelType`, `WireUp(panel)`, `LoadSettings(panel)`, `SaveSettings(panel)`
- **Registry**: `PanelHandlerRegistry` — register and resolve handler by category tag
- **Handlers**: GameplayPanelHandler, TextDelaysPanelHandler, AppearancePanelHandler, TestingPanelHandler (no-op save)
- **Use**: Orchestrator calls `handler.SaveSettings(panel)` when a panel is loaded; SettingsPanel uses handlers for load and wire-up
- **Panel load contract**: Load from settings once when the panel is wired (e.g. call `LoadSettings` once from `WireUp`, with a single deferred post if needed for FindControl). Do **not** subscribe to `Loaded` to re-run `LoadSettings`, because `Loaded` can fire again on layout/focus (e.g. when the user clicks Save), which would overwrite user edits or post-save state with stale values. All handlers follow this load-once behavior.

#### 4. SettingsSaveOrchestrator (Persistence coordination)
- **File**: `Code/UI/Avalonia/Managers/Settings/SettingsSaveOrchestrator.cs`
- **Responsibility**: Save all loaded panels in a defined order; delegate to handlers or tab managers. Uses a single **panel resolver** (`GetPanelForCategoryResolver`) so "displayed or cached" is resolved in one place (SettingsPanel.GetPanelForCategory).
- **Save order**: Game Variables → Gameplay (handler or in-memory) → handler categories from `HandlerSaveCategoryTags` (TextDelays, Appearance, Testing) → ItemModifiers → Items → Actions
- **Returns**: `SettingsSaveResult` (Success, ActionsSaved, TextDelaysSaved) so the panel can run post-save apply
- **Table-driven**: Handler-based categories are in `HandlerSaveCategoryTags`; add a new handler-based panel by registering the handler and adding its tag to that list.

#### 5. SettingsApplyService (Game integration)
- **File**: `Code/UI/Avalonia/Managers/Settings/SettingsApplyService.cs`
- **Responsibility**: Single place for applying saved settings to the running game after a successful save
- **When**: Called by SettingsPanel once after `SaveSettings` returns with `Success == true`
- **Effects**:
  - **Actions saved**: Refreshes current player action pool (`ActionsTabManager.RefreshCurrentPlayerActionPool`) so in-game actions match Settings
  - **Text delays**: Already applied in-place during save (TextDelaysPanelHandler / TextDelayConfiguration); no extra step
  - **Gameplay/difficulty**: Stored in `GameSettings`; apply on next combat or when the game reads them

#### 6. SettingsManager (Core logic)
- **File**: `Code/UI/Avalonia/Managers/SettingsManager.cs`
- **Responsibility**: Core settings persistence, gameplay/difficulty save, text delay save (via SaveGameplaySettings, SaveTextDelaySettings), validation
- **Used by**: GameplayPanelHandler, TextDelaysPanelHandler; orchestrator uses it indirectly through handlers

#### 7. GameSettings (Data model)
- **File**: `Code/Game/GameSettings.cs`
- **Responsibility**: Singleton settings data and file persistence (e.g. gamesettings.json)
- **Single source of truth**: The settings UI does **not** store a copy of `GameSettings`. Every component (panel handlers, SettingsManager, GameplaySettingsManager, DifficultySettingsManager, SettingsSaveOrchestrator, SettingsColorManager, and color sub-managers) resolves the current instance via `GameSettings.Instance` at use time. This avoids stale references when the panel is created at load time but settings are reloaded when the user opens the settings window.
- **Reload**: When the settings UI is opened, the host calls `GameSettings.ReloadFromFile()` and then `RefreshSettingsFromFile()` so the UI shows the last persisted state.

### Data Flow

#### Opening settings (open-settings contract)
- **Contract**: When opening the settings UI (overlay or window), the host **must** call `GameSettings.ReloadFromFile()` then `settingsPanel.RefreshSettingsFromFile()`. This ensures the UI always shows the last persisted state.
- **Entry points**: (1) **MainWindow overlay** — `ShowSettingsPanel()` in MainWindow.axaml.cs calls both. (2) **SettingsWindow** — `Opened` calls `ReloadFromFile()`; after creating the panel and calling `InitializeHandlers`, it must call `RefreshSettingsFromFile()` on the panel.
- `RefreshSettingsFromFile()` updates the panel’s local reference to `GameSettings.Instance` and ensures the selected category’s panel is loaded. No per-component refresh is needed because all consumers read from `GameSettings.Instance` at use time.

#### Saving settings
```
User clicks Save
  → SettingsPanel.SaveSettings()
  → SettingsSaveOrchestrator.SaveSettings(displayedPanel)
      → GameVariablesTabManager.SaveGameVariables()
      → Gameplay: handler.SaveSettings(panel) or settings.SaveSettings()
      → TextDelays / Appearance / Testing: handler.SaveSettings(panel)
      → ItemModifiersTabManager, ItemsTabManager
      → ActionsTabManager.FlushCurrentActionAndSaveToFile(panel)
  → Returns SettingsSaveResult
  → If result.Success: SettingsApplyService.ApplyAfterSave(result, gameStateManager)
  → Status message already shown by orchestrator
```

### Game integration (when settings apply)

| What was saved     | When it applies |
|--------------------|------------------|
| Actions            | Immediately: `SettingsApplyService` refreshes current player action pool after save. |
| Text delays        | Immediately: handlers update `TextDelayConfiguration` during save; consumers read from it. |
| Gameplay / difficulty | On next use: combat and other systems read `GameSettings.Instance` when needed. |
| Game variables     | On next use: read from `GameSettings` / variable store when needed. |

### Panel types and ownership

| Category     | Handler / manager           | Load/Save owner        |
|-------------|-----------------------------|------------------------|
| Gameplay    | GameplayPanelHandler        | Handler                |
| TextDelays  | TextDelaysPanelHandler      | Handler                |
| Appearance  | AppearancePanelHandler      | Handler                |
| Testing     | TestingPanelHandler         | Handler (save no-op)   |
| GameVariables | —                         | GameVariablesTabManager |
| Actions     | —                           | ActionsTabManager      |
| ItemModifiers | —                         | ItemModifiersTabManager |
| Items       | —                           | ItemsTabManager        |
| StatusEffects, BalanceTuning, About | — | No persist or panel-specific |

### Error handling

1. **Control validation**: Handlers and managers check controls for null before use.
2. **Settings validation**: Values clamped/validated before save where applicable.
3. **Orchestrator**: Logs errors per panel; returns `SettingsSaveResult(success: false)` on critical failure; status message shown to user.
4. **Atomic writes**: GameSettings and other persistors use temp file + replace where appropriate.

### Best practices

1. **Add new panels**: Register panel type in `SettingsPanelCatalog` (if main content area); add handler and register in `PanelHandlerRegistry` if it has load/save.
2. **Post-save side effects**: Implement in `SettingsApplyService.ApplyAfterSave` so there is a single place for “apply to game.”
3. **Open-settings contract**: Ensure host calls `ReloadFromFile()` and `RefreshSettingsFromFile()` when opening the settings UI.
4. **New controls**: Add each new editable control to the control inventory and to the handler/tab manager save path (see "Control inventory" below).

### Control inventory (save path audit)

Each editable control in a settings panel must be included in that panel's save path so the main Save button persists it.

| Panel | Handler / manager | Persistence | Verified |
|-------|-------------------|-------------|----------|
| Gameplay | GameplayPanelHandler: 7 checkboxes | GameSettings | handler.SaveSettings |
| TextDelays | TextDelaysPanelHandler: checkboxes, all delay TextBoxes, presets | TextDelayConfiguration | BuildControls + SaveTextDelaySettings |
| Appearance | AppearancePanelHandler: all color TextBoxes, SubsequentLineDarkening | GameSettings + UIConfiguration.json | SaveSettings + SaveSubsequentLineDarkening |
| GameVariables | GameVariablesTabManager (VariableEditor) | GameConfiguration | SaveGameVariables |
| Actions | ActionsTabManager | Actions.json | FlushCurrentActionAndSaveToFile(panel) |
| ItemModifiers | ItemModifiersTabManager | Modifications.json | SaveModifierRarities |
| Items | ItemsTabManager | Items data files | SaveItems |

When adding a new control, ensure the handler or tab manager's save path includes it.

## Recent improvements (simplification pass)

1. **Open-settings contract**: Both entry points (MainWindow overlay, SettingsWindow) call `ReloadFromFile()` then `RefreshSettingsFromFile()` so the standalone window shows file-backed values immediately.
2. **Single panel resolution**: `GetPanelForCategory(categoryTag, currentlyDisplayed)` in SettingsPanel; orchestrator takes `GetPanelForCategoryResolver` and uses it for all categories (no duplicated "displayed as X ?? loadedPanels" logic).
3. **Single apply point**: Post-save apply (e.g. action pool refresh) is only in `SettingsApplyService.ApplyAfterSave` when the user clicks Save; duplicate refresh on SettingsWindow close removed.
4. **Table-driven handler save**: Orchestrator uses `HandlerSaveCategoryTags` for handler-based panels; add a tag to extend without new branches.
5. **Table-driven initializers**: `panelInitializers` and `PanelInitializerContext` replace the long switch in `InitializePanelHandlers`; new tab = catalog entry + initializer entry.
6. **Documentation**: This file describes the open contract, single save pipeline (with panel resolution), and single apply point.

## Future enhancements

- [ ] Persistent tab index storage (user preferences file)
- [ ] Search/filter for Game Variables and Actions lists
- [ ] Auto-save on control change (with debouncing)
- [ ] Settings import/export
- [ ] Settings presets/profiles
