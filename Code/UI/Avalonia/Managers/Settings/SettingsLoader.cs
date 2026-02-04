using Avalonia.Controls;
using RPGGame.UI.Avalonia.Helpers;
using System;

namespace RPGGame.UI.Avalonia.Managers.Settings
{
    /// <summary>
    /// Handles loading settings from SettingsManager into UI controls.
    /// Extracted from SettingsPersistenceManager to improve Single Responsibility Principle compliance.
    /// </summary>
    public class SettingsLoader
    {
        private readonly SettingsManager? settingsManager;

        public SettingsLoader(SettingsManager? settingsManager)
        {
            this.settingsManager = settingsManager;
        }

        /// <summary>
        /// Loads main settings into UI controls using DTO. Delegates to SettingsManager.LoadSettings(controls).
        /// </summary>
        public void LoadMainSettings(MainSettingsControls controls)
        {
            if (settingsManager == null || controls == null) return;
            if (AreMainSettingsControlsValid(controls))
                settingsManager.LoadSettings(controls);
        }

        /// <summary>
        /// Loads text delay settings into UI controls using DTO
        /// </summary>
        public void LoadTextDelaySettings(TextDelaySettingsControls controls, Action<Slider, TextBox>? wireUpActionDelaySlider = null, Action<Slider, TextBox>? wireUpMessageDelaySlider = null)
        {
            if (settingsManager == null || controls == null) return;
            
            if (AreTextDelaySettingsControlsValid(controls))
                settingsManager.LoadTextDelaySettings(controls, wireUpActionDelaySlider, wireUpMessageDelaySlider);
        }

        /// <summary>
        /// Loads animation settings into UI controls using DTO
        /// </summary>
        public void LoadAnimationSettings(AnimationSettingsControls controls)
        {
            if (settingsManager == null || controls == null) return;
            
            if (AreAnimationSettingsControlsValid(controls))
            {
                settingsManager.LoadAnimationSettings(
                    controls.BrightnessMaskEnabledCheckBox!,
                    controls.BrightnessMaskIntensitySlider!,
                    controls.BrightnessMaskIntensityTextBox!,
                    controls.BrightnessMaskWaveLengthSlider!,
                    controls.BrightnessMaskWaveLengthTextBox!,
                    controls.BrightnessMaskUpdateIntervalTextBox!,
                    controls.UndulationSpeedSlider!,
                    controls.UndulationSpeedTextBox!,
                    controls.UndulationWaveLengthSlider!,
                    controls.UndulationWaveLengthTextBox!,
                    controls.UndulationIntervalTextBox!);
            }
        }

        private bool AreMainSettingsControlsValid(MainSettingsControls controls)
        {
            return controls.NarrativeBalanceSlider != null && controls.NarrativeBalanceTextBox != null &&
                controls.EnableNarrativeEventsCheckBox != null && controls.EnableInformationalSummariesCheckBox != null &&
                controls.CombatSpeedSlider != null && controls.CombatSpeedTextBox != null &&
                controls.ShowIndividualActionMessagesCheckBox != null &&
                controls.EnableTextDisplayDelaysCheckBox != null && controls.FastCombatCheckBox != null &&
                controls.ShowDetailedStatsCheckBox != null &&
                controls.EnemyHealthMultiplierSlider != null && controls.EnemyHealthMultiplierTextBox != null &&
                controls.EnemyDamageMultiplierSlider != null && controls.EnemyDamageMultiplierTextBox != null &&
                controls.PlayerHealthMultiplierSlider != null && controls.PlayerHealthMultiplierTextBox != null &&
                controls.PlayerDamageMultiplierSlider != null && controls.PlayerDamageMultiplierTextBox != null &&
                controls.ShowHealthBarsCheckBox != null && controls.ShowDamageNumbersCheckBox != null &&
                controls.ShowComboProgressCheckBox != null;
            // Note: EnableComboSystemCheckBox, EnableAutoSaveCheckBox, AutoSaveIntervalTextBox, 
            // and EnableSoundEffectsCheckBox are nullable and handled by SettingsManager
        }

        private bool AreTextDelaySettingsControlsValid(TextDelaySettingsControls controls)
        {
            return controls.EnableGuiDelaysCheckBox != null && controls.EnableConsoleDelaysCheckBox != null &&
                // ActionDelaySlider and MessageDelaySlider removed - no longer required
                controls.CombatDelayTextBox != null && controls.SystemDelayTextBox != null &&
                controls.MenuDelayTextBox != null && controls.TitleDelayTextBox != null &&
                controls.MainTitleDelayTextBox != null && controls.EnvironmentalDelayTextBox != null &&
                controls.EffectMessageDelayTextBox != null && controls.DamageOverTimeDelayTextBox != null &&
                controls.EncounterDelayTextBox != null && controls.RollInfoDelayTextBox != null &&
                controls.EnvironmentalLineDelayTextBox != null && controls.BaseMenuDelayTextBox != null && controls.ProgressiveReductionRateTextBox != null &&
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
                controls.BrightnessMaskIntensityTextBox != null && controls.BrightnessMaskWaveLengthSlider != null &&
                controls.BrightnessMaskWaveLengthTextBox != null && controls.BrightnessMaskUpdateIntervalTextBox != null &&
                controls.UndulationSpeedSlider != null && controls.UndulationSpeedTextBox != null &&
                controls.UndulationWaveLengthSlider != null && controls.UndulationWaveLengthTextBox != null &&
                controls.UndulationIntervalTextBox != null;
        }
    }
}

