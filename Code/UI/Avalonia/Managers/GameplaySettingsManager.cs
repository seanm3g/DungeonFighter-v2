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
        private readonly Action<string, bool>? showStatusMessage;
        
        public GameplaySettingsManager(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
        }
        
        /// <summary>
        /// Loads narrative and combat settings into UI controls (uses GameSettings.Instance at use time).
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
            narrativeBalanceSlider.Value = GameSettings.Instance.NarrativeBalance;
            narrativeBalanceTextBox.Text = GameSettings.Instance.NarrativeBalance.ToString("F2");
            enableNarrativeEventsCheckBox.IsChecked = GameSettings.Instance.EnableNarrativeEvents;
            enableInformationalSummariesCheckBox.IsChecked = GameSettings.Instance.EnableInformationalSummaries;
            
            // Combat Settings
            combatSpeedSlider.Value = GameSettings.Instance.CombatSpeed;
            combatSpeedTextBox.Text = GameSettings.Instance.CombatSpeed.ToString("F2");
            showIndividualActionMessagesCheckBox.IsChecked = GameSettings.Instance.ShowIndividualActionMessages;
            if (enableComboSystemCheckBox != null)
                enableComboSystemCheckBox.IsChecked = GameSettings.Instance.EnableComboSystem;
            enableTextDisplayDelaysCheckBox.IsChecked = GameSettings.Instance.EnableTextDisplayDelays;
            fastCombatCheckBox.IsChecked = GameSettings.Instance.FastCombat;
            
            // Gameplay Settings
            if (enableAutoSaveCheckBox != null)
                enableAutoSaveCheckBox.IsChecked = GameSettings.Instance.EnableAutoSave;
            if (autoSaveIntervalTextBox != null)
                autoSaveIntervalTextBox.Text = GameSettings.Instance.AutoSaveInterval.ToString();
            showDetailedStatsCheckBox.IsChecked = GameSettings.Instance.ShowDetailedStats;
            if (enableSoundEffectsCheckBox != null)
                enableSoundEffectsCheckBox.IsChecked = GameSettings.Instance.EnableSoundEffects;
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
            GameSettings.Instance.NarrativeBalance = narrativeBalanceSlider.Value;
            GameSettings.Instance.EnableNarrativeEvents = enableNarrativeEventsCheckBox.IsChecked ?? true;
            GameSettings.Instance.EnableInformationalSummaries = enableInformationalSummariesCheckBox.IsChecked ?? true;
            
            // Combat Settings
            GameSettings.Instance.CombatSpeed = combatSpeedSlider.Value;
            GameSettings.Instance.ShowIndividualActionMessages = showIndividualActionMessagesCheckBox.IsChecked ?? false;
            if (enableComboSystemCheckBox != null)
                GameSettings.Instance.EnableComboSystem = enableComboSystemCheckBox.IsChecked ?? true;
            GameSettings.Instance.EnableTextDisplayDelays = enableTextDisplayDelaysCheckBox.IsChecked ?? true;
            GameSettings.Instance.FastCombat = fastCombatCheckBox.IsChecked ?? false;
            
            // Gameplay Settings
            if (enableAutoSaveCheckBox != null)
                GameSettings.Instance.EnableAutoSave = enableAutoSaveCheckBox.IsChecked ?? true;
            if (autoSaveIntervalTextBox != null && int.TryParse(autoSaveIntervalTextBox.Text, out int autoSaveInterval))
            {
                GameSettings.Instance.AutoSaveInterval = Math.Max(1, autoSaveInterval);
            }
            GameSettings.Instance.ShowDetailedStats = showDetailedStatsCheckBox.IsChecked ?? true;
            if (enableSoundEffectsCheckBox != null)
                GameSettings.Instance.EnableSoundEffects = enableSoundEffectsCheckBox.IsChecked ?? false;
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
            var fastChecked = fastCombatCheckBox.IsChecked ?? false;
            // Gameplay Settings
            GameSettings.Instance.ShowIndividualActionMessages = showIndividualActionMessagesCheckBox.IsChecked ?? false;
            GameSettings.Instance.EnableTextDisplayDelays = enableTextDisplayDelaysCheckBox.IsChecked ?? true;
            GameSettings.Instance.FastCombat = fastChecked;
            GameSettings.Instance.ShowDetailedStats = showDetailedStatsCheckBox.IsChecked ?? true;
        }
        
        /// <summary>
        /// Restores gameplay settings from a backup
        /// </summary>
        public void RestoreSettings(GameSettings backup)
        {
            GameSettings.Instance.NarrativeBalance = backup.NarrativeBalance;
            GameSettings.Instance.EnableNarrativeEvents = backup.EnableNarrativeEvents;
            GameSettings.Instance.EnableInformationalSummaries = backup.EnableInformationalSummaries;
            GameSettings.Instance.CombatSpeed = backup.CombatSpeed;
            GameSettings.Instance.ShowIndividualActionMessages = backup.ShowIndividualActionMessages;
            GameSettings.Instance.EnableComboSystem = backup.EnableComboSystem;
            GameSettings.Instance.EnableTextDisplayDelays = backup.EnableTextDisplayDelays;
            GameSettings.Instance.FastCombat = backup.FastCombat;
            GameSettings.Instance.EnableAutoSave = backup.EnableAutoSave;
            GameSettings.Instance.AutoSaveInterval = backup.AutoSaveInterval;
            GameSettings.Instance.ShowDetailedStats = backup.ShowDetailedStats;
            GameSettings.Instance.EnableSoundEffects = backup.EnableSoundEffects;
        }
    }
}
