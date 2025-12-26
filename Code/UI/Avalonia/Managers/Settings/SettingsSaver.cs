using Avalonia.Controls;
using RPGGame.UI.Avalonia.Helpers;
using System;

namespace RPGGame.UI.Avalonia.Managers.Settings
{
    /// <summary>
    /// Handles saving settings from UI controls to SettingsManager.
    /// Extracted from SettingsPersistenceManager to improve Single Responsibility Principle compliance.
    /// </summary>
    public class SettingsSaver
    {
        private readonly SettingsManager? settingsManager;
        private readonly GameVariablesTabManager? gameVariablesTabManager;

        public SettingsSaver(SettingsManager? settingsManager, GameVariablesTabManager? gameVariablesTabManager)
        {
            this.settingsManager = settingsManager;
            this.gameVariablesTabManager = gameVariablesTabManager;
        }

        /// <summary>
        /// Saves main settings from UI controls using DTO
        /// </summary>
        public void SaveMainSettings(MainSettingsControls controls)
        {
            if (settingsManager == null || controls == null) return;
            
            if (AreMainSettingsControlsValid(controls))
            {
                settingsManager.SaveSettings(
                    controls.NarrativeBalanceSlider!,
                    controls.EnableNarrativeEventsCheckBox!,
                    controls.EnableInformationalSummariesCheckBox!,
                    controls.CombatSpeedSlider!,
                    controls.ShowIndividualActionMessagesCheckBox!,
                    controls.EnableComboSystemCheckBox!,
                    controls.EnableTextDisplayDelaysCheckBox!,
                    controls.FastCombatCheckBox!,
                    controls.EnableAutoSaveCheckBox!,
                    controls.AutoSaveIntervalTextBox!,
                    controls.ShowDetailedStatsCheckBox!,
                    controls.EnableSoundEffectsCheckBox!,
                    controls.EnemyHealthMultiplierSlider!,
                    controls.EnemyDamageMultiplierSlider!,
                    controls.PlayerHealthMultiplierSlider!,
                    controls.PlayerDamageMultiplierSlider!,
                    controls.ShowHealthBarsCheckBox!,
                    controls.ShowDamageNumbersCheckBox!,
                    controls.ShowComboProgressCheckBox!,
                    () => gameVariablesTabManager?.SaveGameVariables());
            }
        }

        /// <summary>
        /// Saves text delay settings from UI controls using DTO
        /// </summary>
        public void SaveTextDelaySettings(TextDelaySettingsControls controls)
        {
            if (settingsManager == null || controls == null) return;
            
            if (AreTextDelaySettingsControlsValid(controls))
            {
                settingsManager.SaveTextDelaySettings(
                    controls.EnableGuiDelaysCheckBox!,
                    controls.EnableConsoleDelaysCheckBox!,
                    controls.ActionDelaySlider!,
                    controls.MessageDelaySlider!,
                    controls.CombatDelayTextBox!,
                    controls.SystemDelayTextBox!,
                    controls.MenuDelayTextBox!,
                    controls.TitleDelayTextBox!,
                    controls.MainTitleDelayTextBox!,
                    controls.EnvironmentalDelayTextBox!,
                    controls.EffectMessageDelayTextBox!,
                    controls.DamageOverTimeDelayTextBox!,
                    controls.EncounterDelayTextBox!,
                    controls.RollInfoDelayTextBox!,
                    controls.BaseMenuDelayTextBox!,
                    controls.ProgressiveReductionRateTextBox!,
                    controls.ProgressiveThresholdTextBox!,
                    controls.CombatPresetBaseDelayTextBox!,
                    controls.CombatPresetMinDelayTextBox!,
                    controls.CombatPresetMaxDelayTextBox!,
                    controls.DungeonPresetBaseDelayTextBox!,
                    controls.DungeonPresetMinDelayTextBox!,
                    controls.DungeonPresetMaxDelayTextBox!,
                    controls.RoomPresetBaseDelayTextBox!,
                    controls.RoomPresetMinDelayTextBox!,
                    controls.RoomPresetMaxDelayTextBox!,
                    controls.NarrativePresetBaseDelayTextBox!,
                    controls.NarrativePresetMinDelayTextBox!,
                    controls.NarrativePresetMaxDelayTextBox!,
                    controls.DefaultPresetBaseDelayTextBox!,
                    controls.DefaultPresetMinDelayTextBox!,
                    controls.DefaultPresetMaxDelayTextBox!);
            }
        }

        /// <summary>
        /// Saves animation settings from UI controls using DTO
        /// </summary>
        public void SaveAnimationSettings(AnimationSettingsControls controls)
        {
            if (settingsManager == null || controls == null) return;
            
            if (AreAnimationSettingsControlsValid(controls))
            {
                settingsManager.SaveAnimationSettings(
                    controls.BrightnessMaskEnabledCheckBox!,
                    controls.BrightnessMaskIntensitySlider!,
                    controls.BrightnessMaskWaveLengthSlider!,
                    controls.BrightnessMaskUpdateIntervalTextBox!,
                    controls.UndulationSpeedSlider!,
                    controls.UndulationWaveLengthSlider!,
                    controls.UndulationIntervalTextBox!);
            }
        }

        private bool AreMainSettingsControlsValid(MainSettingsControls controls)
        {
            return controls.NarrativeBalanceSlider != null && controls.EnableNarrativeEventsCheckBox != null &&
                controls.EnableInformationalSummariesCheckBox != null && controls.CombatSpeedSlider != null &&
                controls.ShowIndividualActionMessagesCheckBox != null && controls.EnableComboSystemCheckBox != null &&
                controls.EnableTextDisplayDelaysCheckBox != null && controls.FastCombatCheckBox != null &&
                controls.EnableAutoSaveCheckBox != null && controls.AutoSaveIntervalTextBox != null &&
                controls.ShowDetailedStatsCheckBox != null && controls.EnableSoundEffectsCheckBox != null &&
                controls.EnemyHealthMultiplierSlider != null && controls.EnemyDamageMultiplierSlider != null &&
                controls.PlayerHealthMultiplierSlider != null && controls.PlayerDamageMultiplierSlider != null &&
                controls.ShowHealthBarsCheckBox != null && controls.ShowDamageNumbersCheckBox != null &&
                controls.ShowComboProgressCheckBox != null;
        }

        private bool AreTextDelaySettingsControlsValid(TextDelaySettingsControls controls)
        {
            return controls.EnableGuiDelaysCheckBox != null && controls.EnableConsoleDelaysCheckBox != null &&
                controls.ActionDelaySlider != null && controls.MessageDelaySlider != null &&
                controls.CombatDelayTextBox != null && controls.SystemDelayTextBox != null &&
                controls.MenuDelayTextBox != null && controls.TitleDelayTextBox != null &&
                controls.MainTitleDelayTextBox != null && controls.EnvironmentalDelayTextBox != null &&
                controls.EffectMessageDelayTextBox != null && controls.DamageOverTimeDelayTextBox != null &&
                controls.EncounterDelayTextBox != null && controls.RollInfoDelayTextBox != null &&
                controls.BaseMenuDelayTextBox != null && controls.ProgressiveReductionRateTextBox != null &&
                controls.ProgressiveThresholdTextBox != null && controls.CombatPresetBaseDelayTextBox != null &&
                controls.CombatPresetMinDelayTextBox != null && controls.CombatPresetMaxDelayTextBox != null &&
                controls.DungeonPresetBaseDelayTextBox != null && controls.DungeonPresetMinDelayTextBox != null &&
                controls.DungeonPresetMaxDelayTextBox != null && controls.RoomPresetBaseDelayTextBox != null &&
                controls.RoomPresetMinDelayTextBox != null && controls.RoomPresetMaxDelayTextBox != null &&
                controls.NarrativePresetBaseDelayTextBox != null && controls.NarrativePresetMinDelayTextBox != null &&
                controls.NarrativePresetMaxDelayTextBox != null && controls.DefaultPresetBaseDelayTextBox != null &&
                controls.DefaultPresetMinDelayTextBox != null && controls.DefaultPresetMaxDelayTextBox != null;
        }

        private bool AreAnimationSettingsControlsValid(AnimationSettingsControls controls)
        {
            return controls.BrightnessMaskEnabledCheckBox != null && controls.BrightnessMaskIntensitySlider != null &&
                controls.BrightnessMaskWaveLengthSlider != null && controls.BrightnessMaskUpdateIntervalTextBox != null &&
                controls.UndulationSpeedSlider != null && controls.UndulationWaveLengthSlider != null &&
                controls.UndulationIntervalTextBox != null;
        }
    }
}

