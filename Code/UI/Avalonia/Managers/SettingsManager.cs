using Avalonia.Controls;
using RPGGame;
using RPGGame.Config;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers.Settings;
using System;
using ActionDelegate = System.Action;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages loading and saving of game settings
    /// Extracted from SettingsPanel.axaml.cs to reduce file size
    /// Refactored to use specialized managers for different setting categories
    /// </summary>
    public class SettingsManager
    {
        private readonly GameSettings settings;
        private readonly Action<string, bool>? showStatusMessage;
        private readonly Action<string>? updateStatus;
        private readonly TextDelaySettingsManager textDelayManager;
        private readonly Managers.Settings.AnimationSettingsManager animationSettingsManager;
        private readonly GameplaySettingsManager gameplaySettingsManager;
        private readonly DifficultySettingsManager difficultySettingsManager;

        public SettingsManager(GameSettings settings, Action<string, bool>? showStatusMessage, Action<string>? updateStatus)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.showStatusMessage = showStatusMessage;
            this.updateStatus = updateStatus;
            this.textDelayManager = new TextDelaySettingsManager(showStatusMessage);
            this.animationSettingsManager = new Managers.Settings.AnimationSettingsManager(showStatusMessage);
            this.gameplaySettingsManager = new GameplaySettingsManager(settings, showStatusMessage);
            this.difficultySettingsManager = new DifficultySettingsManager(settings, showStatusMessage);
        }

        public void LoadSettings(
            Slider narrativeBalanceSlider,
            TextBox narrativeBalanceTextBox,
            CheckBox enableNarrativeEventsCheckBox,
            CheckBox enableInformationalSummariesCheckBox,
            Slider combatSpeedSlider,
            TextBox combatSpeedTextBox,
            CheckBox showIndividualActionMessagesCheckBox,
            CheckBox? enableComboSystemCheckBox,
            CheckBox enableTextDisplayDelaysCheckBox,
            CheckBox fastCombatCheckBox,
            CheckBox? enableAutoSaveCheckBox,
            TextBox? autoSaveIntervalTextBox,
            CheckBox showDetailedStatsCheckBox,
            CheckBox? enableSoundEffectsCheckBox,
            Slider enemyHealthMultiplierSlider,
            TextBox enemyHealthMultiplierTextBox,
            Slider enemyDamageMultiplierSlider,
            TextBox enemyDamageMultiplierTextBox,
            Slider playerHealthMultiplierSlider,
            TextBox playerHealthMultiplierTextBox,
            Slider playerDamageMultiplierSlider,
            TextBox playerDamageMultiplierTextBox,
            CheckBox showHealthBarsCheckBox,
            CheckBox showDamageNumbersCheckBox,
            CheckBox showComboProgressCheckBox)
        {
            // Load gameplay settings (narrative, combat, auto-save)
            gameplaySettingsManager.LoadSettings(
                narrativeBalanceSlider,
                narrativeBalanceTextBox,
                enableNarrativeEventsCheckBox,
                enableInformationalSummariesCheckBox,
                combatSpeedSlider,
                combatSpeedTextBox,
                showIndividualActionMessagesCheckBox,
                enableComboSystemCheckBox,
                enableTextDisplayDelaysCheckBox,
                fastCombatCheckBox,
                enableAutoSaveCheckBox,
                autoSaveIntervalTextBox,
                showDetailedStatsCheckBox,
                enableSoundEffectsCheckBox);
            
            // Load difficulty settings (multipliers)
            difficultySettingsManager.LoadSettings(
                enemyHealthMultiplierSlider,
                enemyHealthMultiplierTextBox,
                enemyDamageMultiplierSlider,
                enemyDamageMultiplierTextBox,
                playerHealthMultiplierSlider,
                playerHealthMultiplierTextBox,
                playerDamageMultiplierSlider,
                playerDamageMultiplierTextBox);
            
            // UI Settings
            showHealthBarsCheckBox.IsChecked = settings.ShowHealthBars;
            showDamageNumbersCheckBox.IsChecked = settings.ShowDamageNumbers;
            showComboProgressCheckBox.IsChecked = settings.ShowComboProgress;
        }

        public void SaveSettings(
            Slider narrativeBalanceSlider,
            CheckBox enableNarrativeEventsCheckBox,
            CheckBox enableInformationalSummariesCheckBox,
            Slider combatSpeedSlider,
            CheckBox showIndividualActionMessagesCheckBox,
            CheckBox? enableComboSystemCheckBox,
            CheckBox enableTextDisplayDelaysCheckBox,
            CheckBox fastCombatCheckBox,
            CheckBox? enableAutoSaveCheckBox,
            TextBox? autoSaveIntervalTextBox,
            CheckBox showDetailedStatsCheckBox,
            CheckBox? enableSoundEffectsCheckBox,
            Slider enemyHealthMultiplierSlider,
            Slider enemyDamageMultiplierSlider,
            Slider playerHealthMultiplierSlider,
            Slider playerDamageMultiplierSlider,
            CheckBox showHealthBarsCheckBox,
            CheckBox showDamageNumbersCheckBox,
            CheckBox showComboProgressCheckBox,
            ActionDelegate? saveGameVariables = null)
        {
            try
            {
                // Validate controls exist
                if (narrativeBalanceSlider == null || combatSpeedSlider == null ||
                    enemyHealthMultiplierSlider == null || enemyDamageMultiplierSlider == null ||
                    playerHealthMultiplierSlider == null || playerDamageMultiplierSlider == null)
                {
                    showStatusMessage?.Invoke("Error: Some settings controls are missing", false);
                    return;
                }

                // Store original values for rollback
                var originalSettings = new GameSettings
                {
                    NarrativeBalance = settings.NarrativeBalance,
                    EnableNarrativeEvents = settings.EnableNarrativeEvents,
                    EnableInformationalSummaries = settings.EnableInformationalSummaries,
                    CombatSpeed = settings.CombatSpeed,
                    ShowIndividualActionMessages = settings.ShowIndividualActionMessages,
                    EnableComboSystem = settings.EnableComboSystem,
                    EnableTextDisplayDelays = settings.EnableTextDisplayDelays,
                    FastCombat = settings.FastCombat,
                    EnableAutoSave = settings.EnableAutoSave,
                    AutoSaveInterval = settings.AutoSaveInterval,
                    ShowDetailedStats = settings.ShowDetailedStats,
                    EnableSoundEffects = settings.EnableSoundEffects,
                    EnemyHealthMultiplier = settings.EnemyHealthMultiplier,
                    EnemyDamageMultiplier = settings.EnemyDamageMultiplier,
                    PlayerHealthMultiplier = settings.PlayerHealthMultiplier,
                    PlayerDamageMultiplier = settings.PlayerDamageMultiplier,
                    ShowHealthBars = settings.ShowHealthBars,
                    ShowDamageNumbers = settings.ShowDamageNumbers,
                    ShowComboProgress = settings.ShowComboProgress
                };

                // Save gameplay settings (narrative, combat, auto-save)
                gameplaySettingsManager.SaveSettings(
                    narrativeBalanceSlider,
                    enableNarrativeEventsCheckBox,
                    enableInformationalSummariesCheckBox,
                    combatSpeedSlider,
                    showIndividualActionMessagesCheckBox,
                    enableComboSystemCheckBox,
                    enableTextDisplayDelaysCheckBox,
                    fastCombatCheckBox,
                    enableAutoSaveCheckBox,
                    autoSaveIntervalTextBox,
                    showDetailedStatsCheckBox,
                    enableSoundEffectsCheckBox);
                
                // Save difficulty settings (multipliers)
                difficultySettingsManager.SaveSettings(
                    enemyHealthMultiplierSlider,
                    enemyDamageMultiplierSlider,
                    playerHealthMultiplierSlider,
                    playerDamageMultiplierSlider);
                
                // UI Settings
                settings.ShowHealthBars = showHealthBarsCheckBox.IsChecked ?? true;
                settings.ShowDamageNumbers = showDamageNumbersCheckBox.IsChecked ?? true;
                settings.ShowComboProgress = showComboProgressCheckBox.IsChecked ?? true;
                
                // Validate all settings before saving
                settings.ValidateAndFix();
                
                // Save to file (with atomic write)
                settings.SaveSettings();
                
                // Save Game Variables if any were modified
                try
                {
                    saveGameVariables?.Invoke();
                }
                catch (Exception ex)
                {
                    // Rollback settings if game variables save fails
                    RestoreSettings(originalSettings);
                    showStatusMessage?.Invoke($"Error saving game variables: {ex.Message}. Settings rolled back.", false);
                    updateStatus?.Invoke($"Error saving game variables: {ex.Message}");
                    return;
                }
                
                showStatusMessage?.Invoke("Settings saved successfully!", true);
                updateStatus?.Invoke("Settings saved successfully!");
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving settings: {ex.Message}", false);
                updateStatus?.Invoke($"Error saving settings: {ex.Message}");
                RPGGame.Utils.ScrollDebugLogger.Log($"SettingsManager.SaveSettings error: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Saves gameplay settings without sliders (for simplified gameplay panel)
        /// </summary>
        public void SaveGameplaySettings(
            CheckBox showIndividualActionMessagesCheckBox,
            CheckBox enableTextDisplayDelaysCheckBox,
            CheckBox fastCombatCheckBox,
            CheckBox showDetailedStatsCheckBox,
            CheckBox showHealthBarsCheckBox,
            CheckBox showDamageNumbersCheckBox,
            CheckBox showComboProgressCheckBox,
            ActionDelegate? saveGameVariables = null)
        {
            try
            {
                // Store original values for rollback
                var originalSettings = new GameSettings
                {
                    ShowIndividualActionMessages = settings.ShowIndividualActionMessages,
                    EnableTextDisplayDelays = settings.EnableTextDisplayDelays,
                    FastCombat = settings.FastCombat,
                    ShowDetailedStats = settings.ShowDetailedStats,
                    ShowHealthBars = settings.ShowHealthBars,
                    ShowDamageNumbers = settings.ShowDamageNumbers,
                    ShowComboProgress = settings.ShowComboProgress
                };

                // Save gameplay settings
                gameplaySettingsManager.SaveGameplaySettings(
                    showIndividualActionMessagesCheckBox,
                    enableTextDisplayDelaysCheckBox,
                    fastCombatCheckBox,
                    showDetailedStatsCheckBox);
                
                // UI Settings
                settings.ShowHealthBars = showHealthBarsCheckBox.IsChecked ?? true;
                settings.ShowDamageNumbers = showDamageNumbersCheckBox.IsChecked ?? true;
                settings.ShowComboProgress = showComboProgressCheckBox.IsChecked ?? true;
                
                // Validate all settings before saving
                settings.ValidateAndFix();
                
                // Save to file (with atomic write)
                settings.SaveSettings();
                
                // Save Game Variables if any were modified
                try
                {
                    saveGameVariables?.Invoke();
                }
                catch (Exception ex)
                {
                    // Rollback settings if game variables save fails
                    RestoreSettings(originalSettings);
                    showStatusMessage?.Invoke($"Error saving game variables: {ex.Message}. Settings rolled back.", false);
                    updateStatus?.Invoke($"Error saving game variables: {ex.Message}");
                    return;
                }
                
                showStatusMessage?.Invoke("Settings saved successfully!", true);
                updateStatus?.Invoke("Settings saved successfully!");
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving settings: {ex.Message}", false);
                updateStatus?.Invoke($"Error saving settings: {ex.Message}");
                RPGGame.Utils.ScrollDebugLogger.Log($"SettingsManager.SaveGameplaySettings error: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Restores settings from a backup
        /// </summary>
        private void RestoreSettings(GameSettings backup)
        {
            // Restore gameplay settings
            gameplaySettingsManager.RestoreSettings(backup);
            
            // Restore difficulty settings
            difficultySettingsManager.RestoreSettings(backup);
            
            // Restore UI settings
            settings.ShowHealthBars = backup.ShowHealthBars;
            settings.ShowDamageNumbers = backup.ShowDamageNumbers;
            settings.ShowComboProgress = backup.ShowComboProgress;
        }

        public void ResetToDefaults()
        {
            settings.ResetToDefaults();
        }

        /// <summary>
        /// Loads text delay settings from TextDelayConfiguration into UI controls
        /// Delegates to TextDelaySettingsManager
        /// </summary>
        public void LoadTextDelaySettings(
            CheckBox? enableGuiDelaysCheckBox,
            CheckBox? enableConsoleDelaysCheckBox,
            Slider? actionDelaySlider,
            TextBox? actionDelayTextBox,
            Slider? messageDelaySlider,
            TextBox? messageDelayTextBox,
            TextBox? combatDelayTextBox,
            TextBox? systemDelayTextBox,
            TextBox? menuDelayTextBox,
            TextBox? titleDelayTextBox,
            TextBox? mainTitleDelayTextBox,
            TextBox? environmentalDelayTextBox,
            TextBox? effectMessageDelayTextBox,
            TextBox? damageOverTimeDelayTextBox,
            TextBox? encounterDelayTextBox,
            TextBox? rollInfoDelayTextBox,
            TextBox? baseMenuDelayTextBox,
            TextBox? progressiveReductionRateTextBox,
            TextBox? progressiveThresholdTextBox,
            TextBox? combatPresetBaseDelayTextBox,
            TextBox? combatPresetMinDelayTextBox,
            TextBox? combatPresetMaxDelayTextBox,
            TextBox? dungeonPresetBaseDelayTextBox,
            TextBox? dungeonPresetMinDelayTextBox,
            TextBox? dungeonPresetMaxDelayTextBox,
            TextBox? roomPresetBaseDelayTextBox,
            TextBox? roomPresetMinDelayTextBox,
            TextBox? roomPresetMaxDelayTextBox,
            TextBox? narrativePresetBaseDelayTextBox,
            TextBox? narrativePresetMinDelayTextBox,
            TextBox? narrativePresetMaxDelayTextBox,
            TextBox? defaultPresetBaseDelayTextBox,
            TextBox? defaultPresetMinDelayTextBox,
            TextBox? defaultPresetMaxDelayTextBox)
        {
            textDelayManager.LoadTextDelaySettings(
                enableGuiDelaysCheckBox, enableConsoleDelaysCheckBox,
                actionDelaySlider, actionDelayTextBox,
                messageDelaySlider, messageDelayTextBox,
                combatDelayTextBox, systemDelayTextBox, menuDelayTextBox,
                titleDelayTextBox, mainTitleDelayTextBox, environmentalDelayTextBox,
                effectMessageDelayTextBox, damageOverTimeDelayTextBox, encounterDelayTextBox,
                rollInfoDelayTextBox, baseMenuDelayTextBox, progressiveReductionRateTextBox,
                progressiveThresholdTextBox, combatPresetBaseDelayTextBox, combatPresetMinDelayTextBox,
                combatPresetMaxDelayTextBox, dungeonPresetBaseDelayTextBox, dungeonPresetMinDelayTextBox,
                dungeonPresetMaxDelayTextBox, roomPresetBaseDelayTextBox, roomPresetMinDelayTextBox,
                roomPresetMaxDelayTextBox, narrativePresetBaseDelayTextBox, narrativePresetMinDelayTextBox,
                narrativePresetMaxDelayTextBox, defaultPresetBaseDelayTextBox, defaultPresetMinDelayTextBox,
                defaultPresetMaxDelayTextBox);
        }

        /// <summary>
        /// Saves text delay settings from UI controls to TextDelayConfiguration
        /// Delegates to TextDelaySettingsManager
        /// </summary>
        public void SaveTextDelaySettings(
            CheckBox? enableGuiDelaysCheckBox,
            CheckBox? enableConsoleDelaysCheckBox,
            Slider? actionDelaySlider,
            Slider? messageDelaySlider,
            TextBox? combatDelayTextBox,
            TextBox? systemDelayTextBox,
            TextBox? menuDelayTextBox,
            TextBox? titleDelayTextBox,
            TextBox? mainTitleDelayTextBox,
            TextBox? environmentalDelayTextBox,
            TextBox? effectMessageDelayTextBox,
            TextBox? damageOverTimeDelayTextBox,
            TextBox? encounterDelayTextBox,
            TextBox? rollInfoDelayTextBox,
            TextBox? baseMenuDelayTextBox,
            TextBox? progressiveReductionRateTextBox,
            TextBox? progressiveThresholdTextBox,
            TextBox? combatPresetBaseDelayTextBox,
            TextBox? combatPresetMinDelayTextBox,
            TextBox? combatPresetMaxDelayTextBox,
            TextBox? dungeonPresetBaseDelayTextBox,
            TextBox? dungeonPresetMinDelayTextBox,
            TextBox? dungeonPresetMaxDelayTextBox,
            TextBox? roomPresetBaseDelayTextBox,
            TextBox? roomPresetMinDelayTextBox,
            TextBox? roomPresetMaxDelayTextBox,
            TextBox? narrativePresetBaseDelayTextBox,
            TextBox? narrativePresetMinDelayTextBox,
            TextBox? narrativePresetMaxDelayTextBox,
            TextBox? defaultPresetBaseDelayTextBox,
            TextBox? defaultPresetMinDelayTextBox,
            TextBox? defaultPresetMaxDelayTextBox)
        {
            textDelayManager.SaveTextDelaySettings(
                enableGuiDelaysCheckBox, enableConsoleDelaysCheckBox,
                actionDelaySlider, messageDelaySlider,
                combatDelayTextBox, systemDelayTextBox, menuDelayTextBox,
                titleDelayTextBox, mainTitleDelayTextBox, environmentalDelayTextBox,
                effectMessageDelayTextBox, damageOverTimeDelayTextBox, encounterDelayTextBox,
                rollInfoDelayTextBox, baseMenuDelayTextBox, progressiveReductionRateTextBox,
                progressiveThresholdTextBox, combatPresetBaseDelayTextBox, combatPresetMinDelayTextBox,
                combatPresetMaxDelayTextBox, dungeonPresetBaseDelayTextBox, dungeonPresetMinDelayTextBox,
                dungeonPresetMaxDelayTextBox, roomPresetBaseDelayTextBox, roomPresetMinDelayTextBox,
                roomPresetMaxDelayTextBox, narrativePresetBaseDelayTextBox, narrativePresetMinDelayTextBox,
                narrativePresetMaxDelayTextBox, defaultPresetBaseDelayTextBox, defaultPresetMinDelayTextBox,
                defaultPresetMaxDelayTextBox);
        }

        /// <summary>
        /// Loads animation settings from UIConfiguration into UI controls
        /// Delegates to AnimationSettingsManager
        /// </summary>
        public void LoadAnimationSettings(
            CheckBox brightnessMaskEnabledCheckBox,
            Slider brightnessMaskIntensitySlider,
            TextBox brightnessMaskIntensityTextBox,
            Slider brightnessMaskWaveLengthSlider,
            TextBox brightnessMaskWaveLengthTextBox,
            TextBox brightnessMaskUpdateIntervalTextBox,
            Slider undulationSpeedSlider,
            TextBox undulationSpeedTextBox,
            Slider undulationWaveLengthSlider,
            TextBox undulationWaveLengthTextBox,
            TextBox undulationIntervalTextBox)
        {
            animationSettingsManager.LoadAnimationSettings(
                brightnessMaskEnabledCheckBox,
                brightnessMaskIntensitySlider,
                brightnessMaskIntensityTextBox,
                brightnessMaskWaveLengthSlider,
                brightnessMaskWaveLengthTextBox,
                brightnessMaskUpdateIntervalTextBox,
                undulationSpeedSlider,
                undulationSpeedTextBox,
                undulationWaveLengthSlider,
                undulationWaveLengthTextBox,
                undulationIntervalTextBox);
        }

        /// <summary>
        /// Saves animation settings from UI controls to UIConfiguration
        /// Delegates to AnimationSettingsManager
        /// </summary>
        public void SaveAnimationSettings(
            CheckBox brightnessMaskEnabledCheckBox,
            Slider brightnessMaskIntensitySlider,
            Slider brightnessMaskWaveLengthSlider,
            TextBox brightnessMaskUpdateIntervalTextBox,
            Slider undulationSpeedSlider,
            Slider undulationWaveLengthSlider,
            TextBox undulationIntervalTextBox)
        {
            animationSettingsManager.SaveAnimationSettings(
                brightnessMaskEnabledCheckBox,
                brightnessMaskIntensitySlider,
                brightnessMaskWaveLengthSlider,
                brightnessMaskUpdateIntervalTextBox,
                undulationSpeedSlider,
                undulationWaveLengthSlider,
                undulationIntervalTextBox);
        }
        
        /// <summary>
        /// Gets the animation settings manager for event wiring
        /// </summary>
        public Managers.Settings.AnimationSettingsManager GetAnimationSettingsManager()
        {
            return animationSettingsManager;
        }
    }
}

