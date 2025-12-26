using Avalonia.Controls;
using RPGGame.UI.Avalonia.Managers.Settings;
using System;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Facade for loading and saving settings from/to UI controls.
    /// Delegates to SettingsLoader and SettingsSaver to improve Single Responsibility Principle compliance.
    /// </summary>
    public class SettingsPersistenceManager
    {
        private readonly SettingsLoader loader;
        private readonly SettingsSaver saver;
        private readonly SettingsManager? settingsManager;

        public SettingsPersistenceManager(SettingsManager? settingsManager, GameVariablesTabManager? gameVariablesTabManager)
        {
            this.settingsManager = settingsManager;
            this.loader = new SettingsLoader(settingsManager);
            this.saver = new SettingsSaver(settingsManager, gameVariablesTabManager);
        }

        /// <summary>
        /// Loads current settings into the UI controls (backward compatibility facade)
        /// </summary>
        public void LoadSettings(
            Slider? narrativeBalanceSlider,
            TextBox? narrativeBalanceTextBox,
            CheckBox? enableNarrativeEventsCheckBox,
            CheckBox? enableInformationalSummariesCheckBox,
            Slider? combatSpeedSlider,
            TextBox? combatSpeedTextBox,
            CheckBox? showIndividualActionMessagesCheckBox,
            CheckBox? enableComboSystemCheckBox,
            CheckBox? enableTextDisplayDelaysCheckBox,
            CheckBox? fastCombatCheckBox,
            CheckBox? enableAutoSaveCheckBox,
            TextBox? autoSaveIntervalTextBox,
            CheckBox? showDetailedStatsCheckBox,
            CheckBox? enableSoundEffectsCheckBox,
            Slider? enemyHealthMultiplierSlider,
            TextBox? enemyHealthMultiplierTextBox,
            Slider? enemyDamageMultiplierSlider,
            TextBox? enemyDamageMultiplierTextBox,
            Slider? playerHealthMultiplierSlider,
            TextBox? playerHealthMultiplierTextBox,
            Slider? playerDamageMultiplierSlider,
            TextBox? playerDamageMultiplierTextBox,
            CheckBox? showHealthBarsCheckBox,
            CheckBox? showDamageNumbersCheckBox,
            CheckBox? showComboProgressCheckBox)
        {
            var controls = new MainSettingsControls
            {
                NarrativeBalanceSlider = narrativeBalanceSlider,
                NarrativeBalanceTextBox = narrativeBalanceTextBox,
                EnableNarrativeEventsCheckBox = enableNarrativeEventsCheckBox,
                EnableInformationalSummariesCheckBox = enableInformationalSummariesCheckBox,
                CombatSpeedSlider = combatSpeedSlider,
                CombatSpeedTextBox = combatSpeedTextBox,
                ShowIndividualActionMessagesCheckBox = showIndividualActionMessagesCheckBox,
                EnableComboSystemCheckBox = enableComboSystemCheckBox,
                EnableTextDisplayDelaysCheckBox = enableTextDisplayDelaysCheckBox,
                FastCombatCheckBox = fastCombatCheckBox,
                EnableAutoSaveCheckBox = enableAutoSaveCheckBox,
                AutoSaveIntervalTextBox = autoSaveIntervalTextBox,
                ShowDetailedStatsCheckBox = showDetailedStatsCheckBox,
                EnableSoundEffectsCheckBox = enableSoundEffectsCheckBox,
                EnemyHealthMultiplierSlider = enemyHealthMultiplierSlider,
                EnemyHealthMultiplierTextBox = enemyHealthMultiplierTextBox,
                EnemyDamageMultiplierSlider = enemyDamageMultiplierSlider,
                EnemyDamageMultiplierTextBox = enemyDamageMultiplierTextBox,
                PlayerHealthMultiplierSlider = playerHealthMultiplierSlider,
                PlayerHealthMultiplierTextBox = playerHealthMultiplierTextBox,
                PlayerDamageMultiplierSlider = playerDamageMultiplierSlider,
                PlayerDamageMultiplierTextBox = playerDamageMultiplierTextBox,
                ShowHealthBarsCheckBox = showHealthBarsCheckBox,
                ShowDamageNumbersCheckBox = showDamageNumbersCheckBox,
                ShowComboProgressCheckBox = showComboProgressCheckBox
            };
            loader.LoadMainSettings(controls);
        }

        /// <summary>
        /// Loads text delay settings into UI controls (backward compatibility facade)
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
            TextBox? defaultPresetMaxDelayTextBox,
            Action<Slider, TextBox>? wireUpActionDelaySlider = null,
            Action<Slider, TextBox>? wireUpMessageDelaySlider = null)
        {
            var controls = new TextDelaySettingsControls
            {
                EnableGuiDelaysCheckBox = enableGuiDelaysCheckBox,
                EnableConsoleDelaysCheckBox = enableConsoleDelaysCheckBox,
                ActionDelaySlider = actionDelaySlider,
                ActionDelayTextBox = actionDelayTextBox,
                MessageDelaySlider = messageDelaySlider,
                MessageDelayTextBox = messageDelayTextBox,
                CombatDelayTextBox = combatDelayTextBox,
                SystemDelayTextBox = systemDelayTextBox,
                MenuDelayTextBox = menuDelayTextBox,
                TitleDelayTextBox = titleDelayTextBox,
                MainTitleDelayTextBox = mainTitleDelayTextBox,
                EnvironmentalDelayTextBox = environmentalDelayTextBox,
                EffectMessageDelayTextBox = effectMessageDelayTextBox,
                DamageOverTimeDelayTextBox = damageOverTimeDelayTextBox,
                EncounterDelayTextBox = encounterDelayTextBox,
                RollInfoDelayTextBox = rollInfoDelayTextBox,
                BaseMenuDelayTextBox = baseMenuDelayTextBox,
                ProgressiveReductionRateTextBox = progressiveReductionRateTextBox,
                ProgressiveThresholdTextBox = progressiveThresholdTextBox,
                CombatPresetBaseDelayTextBox = combatPresetBaseDelayTextBox,
                CombatPresetMinDelayTextBox = combatPresetMinDelayTextBox,
                CombatPresetMaxDelayTextBox = combatPresetMaxDelayTextBox,
                DungeonPresetBaseDelayTextBox = dungeonPresetBaseDelayTextBox,
                DungeonPresetMinDelayTextBox = dungeonPresetMinDelayTextBox,
                DungeonPresetMaxDelayTextBox = dungeonPresetMaxDelayTextBox,
                RoomPresetBaseDelayTextBox = roomPresetBaseDelayTextBox,
                RoomPresetMinDelayTextBox = roomPresetMinDelayTextBox,
                RoomPresetMaxDelayTextBox = roomPresetMaxDelayTextBox,
                NarrativePresetBaseDelayTextBox = narrativePresetBaseDelayTextBox,
                NarrativePresetMinDelayTextBox = narrativePresetMinDelayTextBox,
                NarrativePresetMaxDelayTextBox = narrativePresetMaxDelayTextBox,
                DefaultPresetBaseDelayTextBox = defaultPresetBaseDelayTextBox,
                DefaultPresetMinDelayTextBox = defaultPresetMinDelayTextBox,
                DefaultPresetMaxDelayTextBox = defaultPresetMaxDelayTextBox
            };
            loader.LoadTextDelaySettings(controls, wireUpActionDelaySlider, wireUpMessageDelaySlider);
        }

        /// <summary>
        /// Saves current UI values to settings (backward compatibility facade)
        /// </summary>
        public void SaveSettings(
            Slider? narrativeBalanceSlider,
            CheckBox? enableNarrativeEventsCheckBox,
            CheckBox? enableInformationalSummariesCheckBox,
            Slider? combatSpeedSlider,
            CheckBox? showIndividualActionMessagesCheckBox,
            CheckBox? enableComboSystemCheckBox,
            CheckBox? enableTextDisplayDelaysCheckBox,
            CheckBox? fastCombatCheckBox,
            CheckBox? enableAutoSaveCheckBox,
            TextBox? autoSaveIntervalTextBox,
            CheckBox? showDetailedStatsCheckBox,
            CheckBox? enableSoundEffectsCheckBox,
            Slider? enemyHealthMultiplierSlider,
            Slider? enemyDamageMultiplierSlider,
            Slider? playerHealthMultiplierSlider,
            Slider? playerDamageMultiplierSlider,
            CheckBox? showHealthBarsCheckBox,
            CheckBox? showDamageNumbersCheckBox,
            CheckBox? showComboProgressCheckBox)
        {
            var controls = new MainSettingsControls
            {
                NarrativeBalanceSlider = narrativeBalanceSlider,
                EnableNarrativeEventsCheckBox = enableNarrativeEventsCheckBox,
                EnableInformationalSummariesCheckBox = enableInformationalSummariesCheckBox,
                CombatSpeedSlider = combatSpeedSlider,
                ShowIndividualActionMessagesCheckBox = showIndividualActionMessagesCheckBox,
                EnableComboSystemCheckBox = enableComboSystemCheckBox,
                EnableTextDisplayDelaysCheckBox = enableTextDisplayDelaysCheckBox,
                FastCombatCheckBox = fastCombatCheckBox,
                EnableAutoSaveCheckBox = enableAutoSaveCheckBox,
                AutoSaveIntervalTextBox = autoSaveIntervalTextBox,
                ShowDetailedStatsCheckBox = showDetailedStatsCheckBox,
                EnableSoundEffectsCheckBox = enableSoundEffectsCheckBox,
                EnemyHealthMultiplierSlider = enemyHealthMultiplierSlider,
                EnemyDamageMultiplierSlider = enemyDamageMultiplierSlider,
                PlayerHealthMultiplierSlider = playerHealthMultiplierSlider,
                PlayerDamageMultiplierSlider = playerDamageMultiplierSlider,
                ShowHealthBarsCheckBox = showHealthBarsCheckBox,
                ShowDamageNumbersCheckBox = showDamageNumbersCheckBox,
                ShowComboProgressCheckBox = showComboProgressCheckBox
            };
            saver.SaveMainSettings(controls);
        }

        /// <summary>
        /// Saves text delay settings from UI controls (backward compatibility facade)
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
            var controls = new TextDelaySettingsControls
            {
                EnableGuiDelaysCheckBox = enableGuiDelaysCheckBox,
                EnableConsoleDelaysCheckBox = enableConsoleDelaysCheckBox,
                ActionDelaySlider = actionDelaySlider,
                MessageDelaySlider = messageDelaySlider,
                CombatDelayTextBox = combatDelayTextBox,
                SystemDelayTextBox = systemDelayTextBox,
                MenuDelayTextBox = menuDelayTextBox,
                TitleDelayTextBox = titleDelayTextBox,
                MainTitleDelayTextBox = mainTitleDelayTextBox,
                EnvironmentalDelayTextBox = environmentalDelayTextBox,
                EffectMessageDelayTextBox = effectMessageDelayTextBox,
                DamageOverTimeDelayTextBox = damageOverTimeDelayTextBox,
                EncounterDelayTextBox = encounterDelayTextBox,
                RollInfoDelayTextBox = rollInfoDelayTextBox,
                BaseMenuDelayTextBox = baseMenuDelayTextBox,
                ProgressiveReductionRateTextBox = progressiveReductionRateTextBox,
                ProgressiveThresholdTextBox = progressiveThresholdTextBox,
                CombatPresetBaseDelayTextBox = combatPresetBaseDelayTextBox,
                CombatPresetMinDelayTextBox = combatPresetMinDelayTextBox,
                CombatPresetMaxDelayTextBox = combatPresetMaxDelayTextBox,
                DungeonPresetBaseDelayTextBox = dungeonPresetBaseDelayTextBox,
                DungeonPresetMinDelayTextBox = dungeonPresetMinDelayTextBox,
                DungeonPresetMaxDelayTextBox = dungeonPresetMaxDelayTextBox,
                RoomPresetBaseDelayTextBox = roomPresetBaseDelayTextBox,
                RoomPresetMinDelayTextBox = roomPresetMinDelayTextBox,
                RoomPresetMaxDelayTextBox = roomPresetMaxDelayTextBox,
                NarrativePresetBaseDelayTextBox = narrativePresetBaseDelayTextBox,
                NarrativePresetMinDelayTextBox = narrativePresetMinDelayTextBox,
                NarrativePresetMaxDelayTextBox = narrativePresetMaxDelayTextBox,
                DefaultPresetBaseDelayTextBox = defaultPresetBaseDelayTextBox,
                DefaultPresetMinDelayTextBox = defaultPresetMinDelayTextBox,
                DefaultPresetMaxDelayTextBox = defaultPresetMaxDelayTextBox
            };
            saver.SaveTextDelaySettings(controls);
        }

        /// <summary>
        /// Loads animation settings into UI controls (backward compatibility facade)
        /// </summary>
        public void LoadAnimationSettings(
            CheckBox? brightnessMaskEnabledCheckBox,
            Slider? brightnessMaskIntensitySlider,
            TextBox? brightnessMaskIntensityTextBox,
            Slider? brightnessMaskWaveLengthSlider,
            TextBox? brightnessMaskWaveLengthTextBox,
            TextBox? brightnessMaskUpdateIntervalTextBox,
            Slider? undulationSpeedSlider,
            TextBox? undulationSpeedTextBox,
            Slider? undulationWaveLengthSlider,
            TextBox? undulationWaveLengthTextBox,
            TextBox? undulationIntervalTextBox)
        {
            var controls = new AnimationSettingsControls
            {
                BrightnessMaskEnabledCheckBox = brightnessMaskEnabledCheckBox,
                BrightnessMaskIntensitySlider = brightnessMaskIntensitySlider,
                BrightnessMaskIntensityTextBox = brightnessMaskIntensityTextBox,
                BrightnessMaskWaveLengthSlider = brightnessMaskWaveLengthSlider,
                BrightnessMaskWaveLengthTextBox = brightnessMaskWaveLengthTextBox,
                BrightnessMaskUpdateIntervalTextBox = brightnessMaskUpdateIntervalTextBox,
                UndulationSpeedSlider = undulationSpeedSlider,
                UndulationSpeedTextBox = undulationSpeedTextBox,
                UndulationWaveLengthSlider = undulationWaveLengthSlider,
                UndulationWaveLengthTextBox = undulationWaveLengthTextBox,
                UndulationIntervalTextBox = undulationIntervalTextBox
            };
            loader.LoadAnimationSettings(controls);
        }

        /// <summary>
        /// Saves animation settings from UI controls (backward compatibility facade)
        /// </summary>
        public void SaveAnimationSettings(
            CheckBox? brightnessMaskEnabledCheckBox,
            Slider? brightnessMaskIntensitySlider,
            Slider? brightnessMaskWaveLengthSlider,
            TextBox? brightnessMaskUpdateIntervalTextBox,
            Slider? undulationSpeedSlider,
            Slider? undulationWaveLengthSlider,
            TextBox? undulationIntervalTextBox)
        {
            var controls = new AnimationSettingsControls
            {
                BrightnessMaskEnabledCheckBox = brightnessMaskEnabledCheckBox,
                BrightnessMaskIntensitySlider = brightnessMaskIntensitySlider,
                BrightnessMaskWaveLengthSlider = brightnessMaskWaveLengthSlider,
                BrightnessMaskUpdateIntervalTextBox = brightnessMaskUpdateIntervalTextBox,
                UndulationSpeedSlider = undulationSpeedSlider,
                UndulationWaveLengthSlider = undulationWaveLengthSlider,
                UndulationIntervalTextBox = undulationIntervalTextBox
            };
            saver.SaveAnimationSettings(controls);
        }

        /// <summary>
        /// Resets settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            if (settingsManager == null) return;
            settingsManager.ResetToDefaults();
        }
    }
}

