using Avalonia.Controls;
using RPGGame;
using RPGGame.Config;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Settings;
using RPGGame.Utils;
using System;

namespace RPGGame.UI.Avalonia.Managers.Settings
{
    /// <summary>
    /// Resolves the panel instance for a category (displayed or cached). Used so the orchestrator does not duplicate panel-resolution logic.
    /// </summary>
    public delegate UserControl? GetPanelForCategoryResolver(string categoryTag, UserControl? currentlyDisplayed);

    /// <summary>
    /// Orchestrates saving settings across all loaded panels. Delegates to panel handlers where available.
    /// Uses a single panel resolver for consistent "displayed or cached" resolution per category.
    /// </summary>
    public class SettingsSaveOrchestrator
    {
        private readonly SettingsManager? settingsManager;
        private readonly PanelHandlerRegistry? panelHandlerRegistry;
        private readonly GameVariablesTabManager? gameVariablesTabManager;
        private readonly ActionsTabManager? actionsTabManager;
        private readonly ItemModifiersTabManager? itemModifiersTabManager;
        private readonly ItemsTabManager? itemsTabManager;
        private readonly EnemiesTabManager? enemiesTabManager;
        private readonly Action<string, bool>? showStatusMessage;
        private readonly GetPanelForCategoryResolver getPanelForCategory;

        /// <summary>Category tags that use ISettingsPanelHandler for save. Add new handler-based panels here so the orchestrator saves them without code change.</summary>
        private static readonly string[] HandlerSaveCategoryTags = { "TextDelays", "Appearance", "BalanceTuning", "Classes", "ItemGeneration" };

        public SettingsSaveOrchestrator(
            SettingsManager? settingsManager,
            PanelHandlerRegistry? panelHandlerRegistry,
            GameVariablesTabManager? gameVariablesTabManager,
            ActionsTabManager? actionsTabManager,
            ItemModifiersTabManager? itemModifiersTabManager,
            ItemsTabManager? itemsTabManager,
            EnemiesTabManager? enemiesTabManager,
            Action<string, bool>? showStatusMessage,
            GetPanelForCategoryResolver getPanelForCategory)
        {
            this.settingsManager = settingsManager;
            this.panelHandlerRegistry = panelHandlerRegistry;
            this.gameVariablesTabManager = gameVariablesTabManager;
            this.actionsTabManager = actionsTabManager;
            this.itemModifiersTabManager = itemModifiersTabManager;
            this.itemsTabManager = itemsTabManager;
            this.enemiesTabManager = enemiesTabManager;
            this.showStatusMessage = showStatusMessage;
            this.getPanelForCategory = getPanelForCategory ?? ((_, __) => null);
        }

        /// <summary>Save settings. Persists all loaded panels; when not on a given tab, values are read from the cached panel for that type. Pass the panel currently visible so we read from what the user sees when it applies. Returns a result so the caller can run post-save apply (e.g. SettingsApplyService.ApplyAfterSave).</summary>
        public SettingsSaveResult SaveSettings(UserControl? currentlyDisplayedPanel = null)
        {
            if (settingsManager == null) return new SettingsSaveResult(false);

            try
            {
                bool savedSuccessfully = false;
                bool actionsSaved = false;
                bool textDelaysSaved = false;

                // Always save Game Variables first (they can be edited from any panel). Pass displayed panel when it is GameVariables so the tab manager can use it for flush/validation if needed.
                try
                {
                    var gameVariablesPanel = getPanelForCategory("GameVariables", currentlyDisplayedPanel);
                    gameVariablesTabManager?.SaveGameVariables(gameVariablesPanel);
                }
                catch (Exception ex)
                {
                    ScrollDebugLogger.Log($"SettingsPanel: Error saving game variables: {ex.Message}");
                }

                // Gameplay: use handler if panel is loaded, otherwise persist in-memory settings only
                var gameplayPanel = getPanelForCategory("Gameplay", currentlyDisplayedPanel) as GameplaySettingsPanel;
                var gameplayHandler = panelHandlerRegistry?.GetHandler("Gameplay");
                if (SettingsPanel.GetCategoryTagForPanel(currentlyDisplayedPanel) == "Gameplay" && gameplayPanel == null)
                    ScrollDebugLogger.Log("SettingsPanel: SaveSettings warning - displayed panel is Gameplay but getPanelForCategory returned null; in-memory settings will be persisted.");
                if (gameplayPanel != null && gameplayHandler != null)
                {
                    try
                    {
                        gameplayHandler.SaveSettings(gameplayPanel);
                        savedSuccessfully = true;
                    }
                    catch (Exception ex)
                    {
                        ScrollDebugLogger.Log($"SettingsPanel: Error saving gameplay settings: {ex.Message}");
                        showStatusMessage?.Invoke("Error: Failed to save gameplay settings.", false);
                        return new SettingsSaveResult(false);
                    }
                }
                else
                {
                    // Gameplay panel not loaded: in-memory settings used; single persist at end of save
                    savedSuccessfully = true;
                }

                // Handler-based panels: delegate to handlers when panel is loaded (single resolution; list is table-driven)
                foreach (var tag in HandlerSaveCategoryTags)
                {
                    var panel = getPanelForCategory(tag, currentlyDisplayedPanel);
                    if (panel == null) continue;
                    var handler = panelHandlerRegistry?.GetHandler(tag);
                    if (handler == null) continue;
                    try
                    {
                        handler.SaveSettings(panel);
                        if (tag == "TextDelays") textDelaysSaved = true;
                    }
                    catch (Exception ex)
                    {
                        ScrollDebugLogger.Log($"SettingsPanel: Error saving {tag} settings: {ex.Message}");
                    }
                }

                // Save item modifier rarities if panel is loaded
                if (itemModifiersTabManager != null)
                {
                    try
                    {
                        itemModifiersTabManager.SaveModifierRarities();
                    }
                    catch (Exception ex)
                    {
                        ScrollDebugLogger.Log($"SettingsPanel: Error saving item modifier rarities: {ex.Message}");
                    }
                }

                // Save items if panel is loaded
                if (itemsTabManager != null)
                {
                    try
                    {
                        itemsTabManager.SaveItems();
                    }
                    catch (Exception ex)
                    {
                        ScrollDebugLogger.Log($"SettingsPanel: Error saving items: {ex.Message}");
                    }
                }

                if (enemiesTabManager != null)
                {
                    try
                    {
                        if (getPanelForCategory("Enemies", currentlyDisplayedPanel) is EnemiesSettingsPanel)
                            enemiesTabManager.SaveEnemies();
                    }
                    catch (Exception ex)
                    {
                        ScrollDebugLogger.Log($"SettingsPanel: Error saving enemies: {ex.Message}");
                    }
                }

                // Save actions when the Actions panel exists; pass that panel so Default/Starting is read from it (single resolution)
                var actionsPanelForFlush = getPanelForCategory("Actions", currentlyDisplayedPanel) as ActionsSettingsPanel;
                if (actionsTabManager != null && actionsPanelForFlush != null)
                {
                    try
                    {
                        actionsTabManager.FlushCurrentActionAndSaveToFile(actionsPanelForFlush);
                        actionsSaved = true;
                    }
                    catch (Exception ex)
                    {
                        ScrollDebugLogger.Log($"SettingsPanel: Error saving actions: {ex.Message}");
                    }
                }

                // Single persist of GameSettings after all handlers have updated in-memory state
                var settings = GameSettings.Instance;
                settings.ValidateAndFix();
                if (!settings.SaveSettings())
                {
                    showStatusMessage?.Invoke("Error: Failed to save settings to file.", false);
                    return new SettingsSaveResult(false);
                }
                savedSuccessfully = true;

                if (savedSuccessfully)
                {
                    showStatusMessage?.Invoke("Settings saved successfully", true);
                }
                else
                {
                    showStatusMessage?.Invoke("Error: Some settings panels failed to load", false);
                }
                return new SettingsSaveResult(savedSuccessfully, actionsSaved, textDelaysSaved);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsPanel: Error in SaveSettings: {ex.Message}");
                showStatusMessage?.Invoke($"Error saving settings: {ex.Message}", false);
                return new SettingsSaveResult(false);
            }
        }
    }
}

