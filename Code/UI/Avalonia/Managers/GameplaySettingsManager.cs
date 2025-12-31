using Avalonia.Controls;
using RPGGame.Config;
using System;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages gameplay-related settings (narrative, combat, auto-save).
    /// Extracted from SettingsManager to improve Single Responsibility Principle compliance.
    /// </summary>
    public class GameplaySettingsManager
    {
        private readonly GameSettings settings;
        private readonly Action<string, bool>? showStatusMessage;
        
        public GameplaySettingsManager(GameSettings settings, Action<string, bool>? showStatusMessage = null)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.showStatusMessage = showStatusMessage;
        }
        
        /// <summary>
        /// Loads narrative and combat settings into UI controls
        /// </summary>
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
            CheckBox? enableSoundEffectsCheckBox)
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
            if (enableComboSystemCheckBox != null)
                enableComboSystemCheckBox.IsChecked = settings.EnableComboSystem;
            enableTextDisplayDelaysCheckBox.IsChecked = settings.EnableTextDisplayDelays;
            fastCombatCheckBox.IsChecked = settings.FastCombat;
            
            // Gameplay Settings
            if (enableAutoSaveCheckBox != null)
                enableAutoSaveCheckBox.IsChecked = settings.EnableAutoSave;
            if (autoSaveIntervalTextBox != null)
                autoSaveIntervalTextBox.Text = settings.AutoSaveInterval.ToString();
            showDetailedStatsCheckBox.IsChecked = settings.ShowDetailedStats;
            if (enableSoundEffectsCheckBox != null)
                enableSoundEffectsCheckBox.IsChecked = settings.EnableSoundEffects;
        }
        
        /// <summary>
        /// Saves narrative and combat settings from UI controls
        /// </summary>
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
            CheckBox? enableSoundEffectsCheckBox)
        {
            // Narrative Settings
            settings.NarrativeBalance = narrativeBalanceSlider.Value;
            settings.EnableNarrativeEvents = enableNarrativeEventsCheckBox.IsChecked ?? true;
            settings.EnableInformationalSummaries = enableInformationalSummariesCheckBox.IsChecked ?? true;
            
            // Combat Settings
            settings.CombatSpeed = combatSpeedSlider.Value;
            settings.ShowIndividualActionMessages = showIndividualActionMessagesCheckBox.IsChecked ?? false;
            if (enableComboSystemCheckBox != null)
                settings.EnableComboSystem = enableComboSystemCheckBox.IsChecked ?? true;
            settings.EnableTextDisplayDelays = enableTextDisplayDelaysCheckBox.IsChecked ?? true;
            settings.FastCombat = fastCombatCheckBox.IsChecked ?? false;
            
            // Gameplay Settings
            if (enableAutoSaveCheckBox != null)
                settings.EnableAutoSave = enableAutoSaveCheckBox.IsChecked ?? true;
            if (autoSaveIntervalTextBox != null && int.TryParse(autoSaveIntervalTextBox.Text, out int autoSaveInterval))
            {
                settings.AutoSaveInterval = Math.Max(1, autoSaveInterval);
            }
            settings.ShowDetailedStats = showDetailedStatsCheckBox.IsChecked ?? true;
            if (enableSoundEffectsCheckBox != null)
                settings.EnableSoundEffects = enableSoundEffectsCheckBox.IsChecked ?? false;
        }
        
        /// <summary>
        /// Saves gameplay settings without sliders (for simplified gameplay panel)
        /// </summary>
        public void SaveGameplaySettings(
            CheckBox showIndividualActionMessagesCheckBox,
            CheckBox enableTextDisplayDelaysCheckBox,
            CheckBox fastCombatCheckBox,
            CheckBox showDetailedStatsCheckBox)
        {
            // Gameplay Settings
            settings.ShowIndividualActionMessages = showIndividualActionMessagesCheckBox.IsChecked ?? false;
            settings.EnableTextDisplayDelays = enableTextDisplayDelaysCheckBox.IsChecked ?? true;
            settings.FastCombat = fastCombatCheckBox.IsChecked ?? false;
            settings.ShowDetailedStats = showDetailedStatsCheckBox.IsChecked ?? true;
        }
        
        /// <summary>
        /// Restores gameplay settings from a backup
        /// </summary>
        public void RestoreSettings(GameSettings backup)
        {
            settings.NarrativeBalance = backup.NarrativeBalance;
            settings.EnableNarrativeEvents = backup.EnableNarrativeEvents;
            settings.EnableInformationalSummaries = backup.EnableInformationalSummaries;
            settings.CombatSpeed = backup.CombatSpeed;
            settings.ShowIndividualActionMessages = backup.ShowIndividualActionMessages;
            settings.EnableComboSystem = backup.EnableComboSystem;
            settings.EnableTextDisplayDelays = backup.EnableTextDisplayDelays;
            settings.FastCombat = backup.FastCombat;
            settings.EnableAutoSave = backup.EnableAutoSave;
            settings.AutoSaveInterval = backup.AutoSaveInterval;
            settings.ShowDetailedStats = backup.ShowDetailedStats;
            settings.EnableSoundEffects = backup.EnableSoundEffects;
        }
    }
}
