using Avalonia.Controls;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Settings;
using RPGGame.Utils;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Managers.Settings
{
    /// <summary>
    /// Orchestrates saving settings across all loaded panels
    /// </summary>
    public class SettingsSaveOrchestrator
    {
        private readonly SettingsManager? settingsManager;
        private readonly GameVariablesTabManager? gameVariablesTabManager;
        private readonly ItemModifiersTabManager? itemModifiersTabManager;
        private readonly ItemsTabManager? itemsTabManager;
        private readonly GameSettings settings;
        private readonly Action<string, bool>? showStatusMessage;
        private readonly Dictionary<string, UserControl> loadedPanels;
        private readonly Action<string> loadCategoryPanel;

        public SettingsSaveOrchestrator(
            SettingsManager? settingsManager,
            GameVariablesTabManager? gameVariablesTabManager,
            ItemModifiersTabManager? itemModifiersTabManager,
            ItemsTabManager? itemsTabManager,
            GameSettings settings,
            Action<string, bool>? showStatusMessage,
            Dictionary<string, UserControl> loadedPanels,
            Action<string> loadCategoryPanel)
        {
            this.settingsManager = settingsManager;
            this.gameVariablesTabManager = gameVariablesTabManager;
            this.itemModifiersTabManager = itemModifiersTabManager;
            this.itemsTabManager = itemsTabManager;
            this.settings = settings;
            this.showStatusMessage = showStatusMessage;
            this.loadedPanels = loadedPanels;
            this.loadCategoryPanel = loadCategoryPanel;
        }

        public void SaveSettings()
        {
            if (settingsManager == null) return;

            try
            {
                // Ensure required panels are loaded before saving
                if (!loadedPanels.ContainsKey("Gameplay"))
                {
                    loadCategoryPanel("Gameplay");
                }

                // Wait a moment for panels to initialize
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        bool savedSuccessfully = false;

                        // Collect controls from loaded panels
                        CheckBox? showIndividualActionMessagesCheckBox = null;
                        CheckBox? enableTextDisplayDelaysCheckBox = null;
                        CheckBox? fastCombatCheckBox = null;
                        CheckBox? showDetailedStatsCheckBox = null;
                        CheckBox? showHealthBarsCheckBox = null;
                        CheckBox? showDamageNumbersCheckBox = null;
                        CheckBox? showComboProgressCheckBox = null;

                        // Get all controls from Gameplay panel
                        if (loadedPanels.TryGetValue("Gameplay", out var gameplayPanel) && gameplayPanel is GameplaySettingsPanel gameplay)
                        {
                            showIndividualActionMessagesCheckBox = gameplay.ShowIndividualActionMessagesCheckBox;
                            fastCombatCheckBox = gameplay.FastCombatCheckBox;
                            enableTextDisplayDelaysCheckBox = gameplay.EnableTextDisplayDelaysCheckBox;
                            showDetailedStatsCheckBox = gameplay.ShowDetailedStatsCheckBox;
                            showHealthBarsCheckBox = gameplay.ShowHealthBarsCheckBox;
                            showDamageNumbersCheckBox = gameplay.ShowDamageNumbersCheckBox;
                            showComboProgressCheckBox = gameplay.ShowComboProgressCheckBox;
                        }

                        // Always save Game Variables first (they can be edited from any panel)
                        try
                        {
                            gameVariablesTabManager?.SaveGameVariables();
                        }
                        catch (Exception ex)
                        {
                            ScrollDebugLogger.Log($"SettingsPanel: Error saving game variables: {ex.Message}");
                        }

                        // Save gameplay settings if panel is loaded
                        if (showIndividualActionMessagesCheckBox != null)
                        {
                            // This will save gameplay settings AND appearance settings
                            // (settingsManager.SaveSettings calls settings.SaveSettings internally)
                            settingsManager.SaveGameplaySettings(
                                showIndividualActionMessagesCheckBox,
                                enableTextDisplayDelaysCheckBox!,
                                fastCombatCheckBox!,
                                showDetailedStatsCheckBox!,
                                showHealthBarsCheckBox!,
                                showDamageNumbersCheckBox!,
                                showComboProgressCheckBox!,
                                null); // Game Variables already saved above

                            savedSuccessfully = true;
                        }
                        else
                        {
                            // If gameplay panel isn't loaded, just save appearance settings
                            // Appearance settings are updated in real-time via WireUpColorTextBox
                            // and are already in the GameSettings object
                            try
                            {
                                settings.SaveSettings();
                                savedSuccessfully = true;
                            }
                            catch (Exception ex)
                            {
                                ScrollDebugLogger.Log($"SettingsPanel: Error saving appearance settings: {ex.Message}");
                            }
                        }

                        // Save text delay settings if panel is loaded
                        if (loadedPanels.TryGetValue("TextDelays", out var textDelaysPanel) && textDelaysPanel is TextDelaysSettingsPanel textDelays)
                        {
                            try
                            {
                                settingsManager.SaveTextDelaySettings(
                                    textDelays.EnableGuiDelaysCheckBox,
                                    textDelays.EnableConsoleDelaysCheckBox,
                                    null, // ActionDelaySlider (removed - redundant)
                                    null, // MessageDelaySlider (removed - redundant)
                                    textDelays.CombatDelayTextBox,
                                    textDelays.SystemDelayTextBox,
                                    textDelays.MenuDelayTextBox,
                                    textDelays.TitleDelayTextBox,
                                    textDelays.MainTitleDelayTextBox,
                                    textDelays.EnvironmentalDelayTextBox,
                                    textDelays.EffectMessageDelayTextBox,
                                    textDelays.DamageOverTimeDelayTextBox,
                                    textDelays.EncounterDelayTextBox,
                                    textDelays.RollInfoDelayTextBox,
                                    textDelays.BaseMenuDelayTextBox,
                                    textDelays.ProgressiveReductionRateTextBox,
                                    textDelays.ProgressiveThresholdTextBox,
                                    textDelays.CombatPresetBaseDelayTextBox,
                                    textDelays.CombatPresetMinDelayTextBox,
                                    textDelays.CombatPresetMaxDelayTextBox,
                                    textDelays.DungeonPresetBaseDelayTextBox,
                                    textDelays.DungeonPresetMinDelayTextBox,
                                    textDelays.DungeonPresetMaxDelayTextBox,
                                    textDelays.RoomPresetBaseDelayTextBox,
                                    textDelays.RoomPresetMinDelayTextBox,
                                    textDelays.RoomPresetMaxDelayTextBox,
                                    textDelays.NarrativePresetBaseDelayTextBox,
                                    textDelays.NarrativePresetMinDelayTextBox,
                                    textDelays.NarrativePresetMaxDelayTextBox,
                                    textDelays.DefaultPresetBaseDelayTextBox,
                                    textDelays.DefaultPresetMinDelayTextBox,
                                    textDelays.DefaultPresetMaxDelayTextBox);
                            }
                            catch (Exception ex)
                            {
                                ScrollDebugLogger.Log($"SettingsPanel: Error saving text delay settings: {ex.Message}");
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

                        if (savedSuccessfully)
                        {
                            showStatusMessage?.Invoke("Settings saved successfully", true);
                        }
                        else
                        {
                            showStatusMessage?.Invoke("Error: Some settings panels failed to load", false);
                        }
                    }
                    catch (Exception ex)
                    {
                        ScrollDebugLogger.Log($"SettingsPanel: Error saving settings: {ex.Message}");
                        showStatusMessage?.Invoke($"Error saving settings: {ex.Message}", false);
                    }
                }, DispatcherPriority.Normal);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsPanel: Error in SaveSettings: {ex.Message}");
                showStatusMessage?.Invoke($"Error saving settings: {ex.Message}", false);
            }
        }
    }
}

