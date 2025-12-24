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

        public SettingsManager(GameSettings settings, Action<string, bool>? showStatusMessage, Action<string>? updateStatus)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.showStatusMessage = showStatusMessage;
            this.updateStatus = updateStatus;
            this.textDelayManager = new TextDelaySettingsManager(showStatusMessage);
        }

        public void LoadSettings(
            Slider narrativeBalanceSlider,
            TextBox narrativeBalanceTextBox,
            CheckBox enableNarrativeEventsCheckBox,
            CheckBox enableInformationalSummariesCheckBox,
            Slider combatSpeedSlider,
            TextBox combatSpeedTextBox,
            CheckBox showIndividualActionMessagesCheckBox,
            CheckBox enableComboSystemCheckBox,
            CheckBox enableTextDisplayDelaysCheckBox,
            CheckBox fastCombatCheckBox,
            CheckBox enableAutoSaveCheckBox,
            TextBox autoSaveIntervalTextBox,
            CheckBox showDetailedStatsCheckBox,
            CheckBox enableSoundEffectsCheckBox,
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
            // Narrative Settings
            narrativeBalanceSlider.Value = settings.NarrativeBalance;
            narrativeBalanceTextBox.Text = settings.NarrativeBalance.ToString("F2");
            enableNarrativeEventsCheckBox.IsChecked = settings.EnableNarrativeEvents;
            enableInformationalSummariesCheckBox.IsChecked = settings.EnableInformationalSummaries;
            
            // Combat Settings
            combatSpeedSlider.Value = settings.CombatSpeed;
            combatSpeedTextBox.Text = settings.CombatSpeed.ToString("F2");
            showIndividualActionMessagesCheckBox.IsChecked = settings.ShowIndividualActionMessages;
            enableComboSystemCheckBox.IsChecked = settings.EnableComboSystem;
            enableTextDisplayDelaysCheckBox.IsChecked = settings.EnableTextDisplayDelays;
            fastCombatCheckBox.IsChecked = settings.FastCombat;
            
            // Gameplay Settings
            enableAutoSaveCheckBox.IsChecked = settings.EnableAutoSave;
            autoSaveIntervalTextBox.Text = settings.AutoSaveInterval.ToString();
            showDetailedStatsCheckBox.IsChecked = settings.ShowDetailedStats;
            enableSoundEffectsCheckBox.IsChecked = settings.EnableSoundEffects;
            
            // Difficulty Settings
            enemyHealthMultiplierSlider.Value = settings.EnemyHealthMultiplier;
            enemyHealthMultiplierTextBox.Text = settings.EnemyHealthMultiplier.ToString("F2");
            enemyDamageMultiplierSlider.Value = settings.EnemyDamageMultiplier;
            enemyDamageMultiplierTextBox.Text = settings.EnemyDamageMultiplier.ToString("F2");
            playerHealthMultiplierSlider.Value = settings.PlayerHealthMultiplier;
            playerHealthMultiplierTextBox.Text = settings.PlayerHealthMultiplier.ToString("F2");
            playerDamageMultiplierSlider.Value = settings.PlayerDamageMultiplier;
            playerDamageMultiplierTextBox.Text = settings.PlayerDamageMultiplier.ToString("F2");
            
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
            CheckBox enableComboSystemCheckBox,
            CheckBox enableTextDisplayDelaysCheckBox,
            CheckBox fastCombatCheckBox,
            CheckBox enableAutoSaveCheckBox,
            TextBox autoSaveIntervalTextBox,
            CheckBox showDetailedStatsCheckBox,
            CheckBox enableSoundEffectsCheckBox,
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
                // Narrative Settings
                settings.NarrativeBalance = narrativeBalanceSlider.Value;
                settings.EnableNarrativeEvents = enableNarrativeEventsCheckBox.IsChecked ?? true;
                settings.EnableInformationalSummaries = enableInformationalSummariesCheckBox.IsChecked ?? true;
                
                // Combat Settings
                settings.CombatSpeed = combatSpeedSlider.Value;
                settings.ShowIndividualActionMessages = showIndividualActionMessagesCheckBox.IsChecked ?? false;
                settings.EnableComboSystem = enableComboSystemCheckBox.IsChecked ?? true;
                settings.EnableTextDisplayDelays = enableTextDisplayDelaysCheckBox.IsChecked ?? true;
                settings.FastCombat = fastCombatCheckBox.IsChecked ?? false;
                
                // Gameplay Settings
                settings.EnableAutoSave = enableAutoSaveCheckBox.IsChecked ?? true;
                if (int.TryParse(autoSaveIntervalTextBox.Text, out int autoSaveInterval))
                {
                    settings.AutoSaveInterval = Math.Max(1, autoSaveInterval);
                }
                settings.ShowDetailedStats = showDetailedStatsCheckBox.IsChecked ?? true;
                settings.EnableSoundEffects = enableSoundEffectsCheckBox.IsChecked ?? false;
                
                // Difficulty Settings
                settings.EnemyHealthMultiplier = enemyHealthMultiplierSlider.Value;
                settings.EnemyDamageMultiplier = enemyDamageMultiplierSlider.Value;
                settings.PlayerHealthMultiplier = playerHealthMultiplierSlider.Value;
                settings.PlayerDamageMultiplier = playerDamageMultiplierSlider.Value;
                
                // UI Settings
                settings.ShowHealthBars = showHealthBarsCheckBox.IsChecked ?? true;
                settings.ShowDamageNumbers = showDamageNumbersCheckBox.IsChecked ?? true;
                settings.ShowComboProgress = showComboProgressCheckBox.IsChecked ?? true;
                
                // Save to file
                settings.SaveSettings();
                
                // Save Game Variables if any were modified
                saveGameVariables?.Invoke();
                
                showStatusMessage?.Invoke("Settings saved successfully!", true);
                updateStatus?.Invoke("Settings saved successfully!");
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving settings: {ex.Message}", false);
                updateStatus?.Invoke($"Error saving settings: {ex.Message}");
            }
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
            CheckBox enableGuiDelaysCheckBox,
            CheckBox enableConsoleDelaysCheckBox,
            Slider actionDelaySlider,
            TextBox actionDelayTextBox,
            Slider messageDelaySlider,
            TextBox messageDelayTextBox,
            TextBox combatDelayTextBox,
            TextBox systemDelayTextBox,
            TextBox menuDelayTextBox,
            TextBox titleDelayTextBox,
            TextBox mainTitleDelayTextBox,
            TextBox environmentalDelayTextBox,
            TextBox effectMessageDelayTextBox,
            TextBox damageOverTimeDelayTextBox,
            TextBox encounterDelayTextBox,
            TextBox rollInfoDelayTextBox,
            TextBox baseMenuDelayTextBox,
            TextBox progressiveReductionRateTextBox,
            TextBox progressiveThresholdTextBox,
            TextBox combatPresetBaseDelayTextBox,
            TextBox combatPresetMinDelayTextBox,
            TextBox combatPresetMaxDelayTextBox,
            TextBox dungeonPresetBaseDelayTextBox,
            TextBox dungeonPresetMinDelayTextBox,
            TextBox dungeonPresetMaxDelayTextBox,
            TextBox roomPresetBaseDelayTextBox,
            TextBox roomPresetMinDelayTextBox,
            TextBox roomPresetMaxDelayTextBox,
            TextBox narrativePresetBaseDelayTextBox,
            TextBox narrativePresetMinDelayTextBox,
            TextBox narrativePresetMaxDelayTextBox,
            TextBox defaultPresetBaseDelayTextBox,
            TextBox defaultPresetMinDelayTextBox,
            TextBox defaultPresetMaxDelayTextBox)
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
            CheckBox enableGuiDelaysCheckBox,
            CheckBox enableConsoleDelaysCheckBox,
            Slider actionDelaySlider,
            Slider messageDelaySlider,
            TextBox combatDelayTextBox,
            TextBox systemDelayTextBox,
            TextBox menuDelayTextBox,
            TextBox titleDelayTextBox,
            TextBox mainTitleDelayTextBox,
            TextBox environmentalDelayTextBox,
            TextBox effectMessageDelayTextBox,
            TextBox damageOverTimeDelayTextBox,
            TextBox encounterDelayTextBox,
            TextBox rollInfoDelayTextBox,
            TextBox baseMenuDelayTextBox,
            TextBox progressiveReductionRateTextBox,
            TextBox progressiveThresholdTextBox,
            TextBox combatPresetBaseDelayTextBox,
            TextBox combatPresetMinDelayTextBox,
            TextBox combatPresetMaxDelayTextBox,
            TextBox dungeonPresetBaseDelayTextBox,
            TextBox dungeonPresetMinDelayTextBox,
            TextBox dungeonPresetMaxDelayTextBox,
            TextBox roomPresetBaseDelayTextBox,
            TextBox roomPresetMinDelayTextBox,
            TextBox roomPresetMaxDelayTextBox,
            TextBox narrativePresetBaseDelayTextBox,
            TextBox narrativePresetMinDelayTextBox,
            TextBox narrativePresetMaxDelayTextBox,
            TextBox defaultPresetBaseDelayTextBox,
            TextBox defaultPresetMinDelayTextBox,
            TextBox defaultPresetMaxDelayTextBox)
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
    }
}

