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
        /// Loads main settings into UI controls using DTO
        /// </summary>
        public void LoadMainSettings(MainSettingsControls controls)
        {
            if (settingsManager == null || controls == null) return;
            
            if (AreMainSettingsControlsValid(controls))
            {
#pragma warning disable CS8604 // Nullable parameters are handled by SettingsManager
                settingsManager.LoadSettings(
                    controls.NarrativeBalanceSlider!,
                    controls.NarrativeBalanceTextBox!,
                    controls.EnableNarrativeEventsCheckBox!,
                    controls.EnableInformationalSummariesCheckBox!,
                    controls.CombatSpeedSlider!,
                    controls.CombatSpeedTextBox!,
                    controls.ShowIndividualActionMessagesCheckBox!,
                    controls.EnableComboSystemCheckBox!, // Nullable - handled by SettingsManager
                    controls.EnableTextDisplayDelaysCheckBox!,
                    controls.FastCombatCheckBox!,
                    controls.EnableAutoSaveCheckBox!, // Nullable - handled by SettingsManager
                    controls.AutoSaveIntervalTextBox!, // Nullable - handled by SettingsManager
                    controls.ShowDetailedStatsCheckBox!,
                    controls.EnableSoundEffectsCheckBox!, // Nullable - handled by SettingsManager
                    controls.EnemyHealthMultiplierSlider!,
                    controls.EnemyHealthMultiplierTextBox!,
                    controls.EnemyDamageMultiplierSlider!,
                    controls.EnemyDamageMultiplierTextBox!,
                    controls.PlayerHealthMultiplierSlider!,
                    controls.PlayerHealthMultiplierTextBox!,
                    controls.PlayerDamageMultiplierSlider!,
                    controls.PlayerDamageMultiplierTextBox!,
                    controls.ShowHealthBarsCheckBox!,
                    controls.ShowDamageNumbersCheckBox!,
                    controls.ShowComboProgressCheckBox!);
#pragma warning restore CS8604
            }
        }

        /// <summary>
        /// Loads text delay settings into UI controls using DTO
        /// </summary>
        public void LoadTextDelaySettings(TextDelaySettingsControls controls, Action<Slider, TextBox>? wireUpActionDelaySlider = null, Action<Slider, TextBox>? wireUpMessageDelaySlider = null)
        {
            if (settingsManager == null || controls == null) return;
            
            if (AreTextDelaySettingsControlsValid(controls))
            {
                settingsManager.LoadTextDelaySettings(
                    controls.EnableGuiDelaysCheckBox!,
                    controls.EnableConsoleDelaysCheckBox!,
                    null, // ActionDelaySlider - Deprecated, removed from DTO
                    null, // ActionDelayTextBox - Deprecated, removed from DTO
                    null, // MessageDelaySlider - Deprecated, removed from DTO
                    null, // MessageDelayTextBox - Deprecated, removed from DTO
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
                    controls.EnvironmentalLineDelayTextBox!,
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
                
                // Note: ActionDelay and MessageDelay sliders removed - combat timing is now controlled by
                // MessageTypeDelays.Combat and ChunkedTextReveal.Combat presets
                // Wire-up code removed as these controls no longer exist
            }
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

