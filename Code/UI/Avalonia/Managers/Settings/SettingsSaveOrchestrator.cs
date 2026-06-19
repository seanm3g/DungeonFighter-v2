using Avalonia.Controls;
using RPGGame;
using RPGGame.Audio;
using RPGGame.Config;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Helpers;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Settings;
using RPGGame.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        private readonly ItemSuffixesTabManager? itemSuffixesTabManager;
        private readonly ItemsTabManager? itemsTabManager;
        private readonly EnemiesTabManager? enemiesTabManager;
        private readonly Action<string, bool>? showStatusMessage;
        private readonly GetPanelForCategoryResolver getPanelForCategory;

        private static readonly IReadOnlyList<string> HandlerSaveCategoryTags = SettingsPanelCatalog.HandlerSaveCategoryTags;

        public SettingsSaveOrchestrator(
            SettingsManager? settingsManager,
            PanelHandlerRegistry? panelHandlerRegistry,
            GameVariablesTabManager? gameVariablesTabManager,
            ActionsTabManager? actionsTabManager,
            ItemModifiersTabManager? itemModifiersTabManager,
            ItemSuffixesTabManager? itemSuffixesTabManager,
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
            this.itemSuffixesTabManager = itemSuffixesTabManager;
            this.itemsTabManager = itemsTabManager;
            this.enemiesTabManager = enemiesTabManager;
            this.showStatusMessage = showStatusMessage;
            this.getPanelForCategory = getPanelForCategory ?? ((_, __) => null);
        }

        private static readonly string[] BalanceHandlerTags = { "ItemGeneration", "CombatTuning", "Classes" };

        /// <summary>Save settings with patch dialogs. Pass the panel currently visible when it applies.</summary>
        public async Task<SettingsSaveResult> SaveSettingsAsync(UserControl? currentlyDisplayedPanel = null, Window? dialogOwner = null)
        {
            if (settingsManager == null) return new SettingsSaveResult(false);

            dialogOwner = WindowOwnerResolver.ResolveUsableOwnerWindow(dialogOwner);

            try
            {
                bool savedSuccessfully = false;
                bool actionsSaved = false;
                bool textDelaysSaved = false;
                bool audioNeedsPatchSave = false;
                bool balanceNeedsPatchSave = false;

                try
                {
                    var gameVariablesPanel = getPanelForCategory("GameVariables", currentlyDisplayedPanel);
                    gameVariablesTabManager?.SaveGameVariables(gameVariablesPanel);
                }
                catch (Exception ex)
                {
                    ScrollDebugLogger.Log($"SettingsPanel: Error saving game variables: {ex.Message}");
                }

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
                    savedSuccessfully = true;
                }

                foreach (var tag in HandlerSaveCategoryTags)
                {
                    var panel = getPanelForCategory(tag, currentlyDisplayedPanel);
                    if (panel == null) continue;
                    var handler = panelHandlerRegistry?.GetHandler(tag);
                    if (handler == null) continue;
                    try
                    {
                        handler.SaveSettings(panel);
                        if (tag == "TextAndAnimation") textDelaysSaved = true;
                        if (string.Equals(tag, "Audio", StringComparison.OrdinalIgnoreCase)) audioNeedsPatchSave = true;
                        if (Array.IndexOf(BalanceHandlerTags, tag) >= 0) balanceNeedsPatchSave = true;
                    }
                    catch (Exception ex)
                    {
                        ScrollDebugLogger.Log($"SettingsPanel: Error saving {tag} settings: {ex.Message}");
                    }
                }

                if (itemModifiersTabManager != null)
                {
                    try { itemModifiersTabManager.SaveModifierRarities(); }
                    catch (Exception ex) { ScrollDebugLogger.Log($"SettingsPanel: Error saving item modifier rarities: {ex.Message}"); }
                }

                if (itemSuffixesTabManager != null)
                {
                    try { itemSuffixesTabManager.SaveSuffixes(); }
                    catch (Exception ex) { ScrollDebugLogger.Log($"SettingsPanel: Error saving item suffixes: {ex.Message}"); }
                }

                if (itemsTabManager != null)
                {
                    try { itemsTabManager.SaveItems(); }
                    catch (Exception ex) { ScrollDebugLogger.Log($"SettingsPanel: Error saving items: {ex.Message}"); }
                }

                if (enemiesTabManager != null)
                {
                    try
                    {
                        if (getPanelForCategory("Enemies", currentlyDisplayedPanel) is EnemiesSettingsPanel)
                            enemiesTabManager.SaveEnemies();
                    }
                    catch (Exception ex) { ScrollDebugLogger.Log($"SettingsPanel: Error saving enemies: {ex.Message}"); }
                }

                var actionsPanelForFlush = getPanelForCategory("Actions", currentlyDisplayedPanel) as ActionsSettingsPanel;
                if (actionsTabManager != null && actionsPanelForFlush != null)
                {
                    try
                    {
                        actionsTabManager.FlushCurrentActionAndSaveToFile(actionsPanelForFlush);
                        actionsSaved = true;
                    }
                    catch (Exception ex) { ScrollDebugLogger.Log($"SettingsPanel: Error saving actions: {ex.Message}"); }
                }

                if (balanceNeedsPatchSave)
                {
                    if (!await PatchSaveCoordinator.SaveBalanceAsync(dialogOwner, GameConfiguration.Instance))
                    {
                        showStatusMessage?.Invoke("Balance patch save cancelled.", false);
                        return new SettingsSaveResult(false);
                    }
                }

                if (audioNeedsPatchSave)
                {
                    if (!await PatchSaveCoordinator.SaveAudioAsync(dialogOwner, AudioConfig.Instance))
                    {
                        showStatusMessage?.Invoke("Audio patch save cancelled.", false);
                        return new SettingsSaveResult(false);
                    }
                    AudioBootstrap.ApplyConfigToEngine();
                }

                var settings = GameSettings.Instance;
                settings.ValidateAndFix();
                if (!await PatchSaveCoordinator.SaveGameSettingsAsync(dialogOwner, settings))
                {
                    showStatusMessage?.Invoke("Game settings patch save cancelled.", false);
                    return new SettingsSaveResult(false);
                }
                savedSuccessfully = true;

                if (savedSuccessfully)
                {
                    var status = "Settings saved successfully";
                    if (textDelaysSaved && !string.IsNullOrWhiteSpace(RPGGame.Config.TextDelay.TextDelayLoader.LastSavedConfigPath))
                        status += $". Text delays saved to {RPGGame.Config.TextDelay.TextDelayLoader.LastSavedConfigPath}";
                    showStatusMessage?.Invoke(status, true);
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

