using Avalonia.Controls;
using System;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Handles loading and saving of settings from/to UI controls
    /// Extracted from SettingsPanel to separate persistence logic
    /// </summary>
    public class SettingsPersistenceManager
    {
        private readonly SettingsManager? settingsManager;
        private readonly GameVariablesTabManager? gameVariablesTabManager;

        public SettingsPersistenceManager(SettingsManager? settingsManager, GameVariablesTabManager? gameVariablesTabManager)
        {
            this.settingsManager = settingsManager;
            this.gameVariablesTabManager = gameVariablesTabManager;
        }

        /// <summary>
        /// Loads current settings into the UI controls
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
            if (settingsManager == null) return;
            
            // Check all required parameters are non-null before calling
            if (narrativeBalanceSlider != null && narrativeBalanceTextBox != null &&
                enableNarrativeEventsCheckBox != null && enableInformationalSummariesCheckBox != null &&
                combatSpeedSlider != null && combatSpeedTextBox != null &&
                showIndividualActionMessagesCheckBox != null && enableComboSystemCheckBox != null &&
                enableTextDisplayDelaysCheckBox != null && fastCombatCheckBox != null &&
                enableAutoSaveCheckBox != null && autoSaveIntervalTextBox != null &&
                showDetailedStatsCheckBox != null && enableSoundEffectsCheckBox != null &&
                enemyHealthMultiplierSlider != null && enemyHealthMultiplierTextBox != null &&
                enemyDamageMultiplierSlider != null && enemyDamageMultiplierTextBox != null &&
                playerHealthMultiplierSlider != null && playerHealthMultiplierTextBox != null &&
                playerDamageMultiplierSlider != null && playerDamageMultiplierTextBox != null &&
                showHealthBarsCheckBox != null && showDamageNumbersCheckBox != null &&
                showComboProgressCheckBox != null)
            {
                settingsManager.LoadSettings(
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
                    enableSoundEffectsCheckBox,
                    enemyHealthMultiplierSlider,
                    enemyHealthMultiplierTextBox,
                    enemyDamageMultiplierSlider,
                    enemyDamageMultiplierTextBox,
                    playerHealthMultiplierSlider,
                    playerHealthMultiplierTextBox,
                    playerDamageMultiplierSlider,
                    playerDamageMultiplierTextBox,
                    showHealthBarsCheckBox,
                    showDamageNumbersCheckBox,
                    showComboProgressCheckBox);
            }
        }

        /// <summary>
        /// Loads text delay settings into UI controls
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
            if (settingsManager == null) return;
            
            if (enableGuiDelaysCheckBox != null && enableConsoleDelaysCheckBox != null &&
                actionDelaySlider != null && actionDelayTextBox != null &&
                messageDelaySlider != null && messageDelayTextBox != null &&
                combatDelayTextBox != null && systemDelayTextBox != null &&
                menuDelayTextBox != null && titleDelayTextBox != null &&
                mainTitleDelayTextBox != null && environmentalDelayTextBox != null &&
                effectMessageDelayTextBox != null && damageOverTimeDelayTextBox != null &&
                encounterDelayTextBox != null && rollInfoDelayTextBox != null &&
                baseMenuDelayTextBox != null && progressiveReductionRateTextBox != null &&
                progressiveThresholdTextBox != null && combatPresetBaseDelayTextBox != null &&
                combatPresetMinDelayTextBox != null && combatPresetMaxDelayTextBox != null &&
                dungeonPresetBaseDelayTextBox != null && dungeonPresetMinDelayTextBox != null &&
                dungeonPresetMaxDelayTextBox != null && roomPresetBaseDelayTextBox != null &&
                roomPresetMinDelayTextBox != null && roomPresetMaxDelayTextBox != null &&
                narrativePresetBaseDelayTextBox != null && narrativePresetMinDelayTextBox != null &&
                narrativePresetMaxDelayTextBox != null && defaultPresetBaseDelayTextBox != null &&
                defaultPresetMinDelayTextBox != null && defaultPresetMaxDelayTextBox != null)
            {
                settingsManager.LoadTextDelaySettings(
                    enableGuiDelaysCheckBox,
                    enableConsoleDelaysCheckBox,
                    actionDelaySlider,
                    actionDelayTextBox,
                    messageDelaySlider,
                    messageDelayTextBox,
                    combatDelayTextBox,
                    systemDelayTextBox,
                    menuDelayTextBox,
                    titleDelayTextBox,
                    mainTitleDelayTextBox,
                    environmentalDelayTextBox,
                    effectMessageDelayTextBox,
                    damageOverTimeDelayTextBox,
                    encounterDelayTextBox,
                    rollInfoDelayTextBox,
                    baseMenuDelayTextBox,
                    progressiveReductionRateTextBox,
                    progressiveThresholdTextBox,
                    combatPresetBaseDelayTextBox,
                    combatPresetMinDelayTextBox,
                    combatPresetMaxDelayTextBox,
                    dungeonPresetBaseDelayTextBox,
                    dungeonPresetMinDelayTextBox,
                    dungeonPresetMaxDelayTextBox,
                    roomPresetBaseDelayTextBox,
                    roomPresetMinDelayTextBox,
                    roomPresetMaxDelayTextBox,
                    narrativePresetBaseDelayTextBox,
                    narrativePresetMinDelayTextBox,
                    narrativePresetMaxDelayTextBox,
                    defaultPresetBaseDelayTextBox,
                    defaultPresetMinDelayTextBox,
                    defaultPresetMaxDelayTextBox);
                
                // Wire up slider events for action/message delays
                if (wireUpActionDelaySlider != null)
                {
                    actionDelaySlider.ValueChanged += (s, e) =>
                    {
                        if (actionDelayTextBox != null)
                            actionDelayTextBox.Text = ((int)actionDelaySlider.Value).ToString();
                    };
                }
                
                if (wireUpMessageDelaySlider != null)
                {
                    messageDelaySlider.ValueChanged += (s, e) =>
                    {
                        if (messageDelayTextBox != null)
                            messageDelayTextBox.Text = ((int)messageDelaySlider.Value).ToString();
                    };
                }
            }
        }

        /// <summary>
        /// Saves current UI values to settings
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
            if (settingsManager == null) return;
            
            // Check all required parameters are non-null before calling
            if (narrativeBalanceSlider != null && enableNarrativeEventsCheckBox != null &&
                enableInformationalSummariesCheckBox != null && combatSpeedSlider != null &&
                showIndividualActionMessagesCheckBox != null && enableComboSystemCheckBox != null &&
                enableTextDisplayDelaysCheckBox != null && fastCombatCheckBox != null &&
                enableAutoSaveCheckBox != null && autoSaveIntervalTextBox != null &&
                showDetailedStatsCheckBox != null && enableSoundEffectsCheckBox != null &&
                enemyHealthMultiplierSlider != null && enemyDamageMultiplierSlider != null &&
                playerHealthMultiplierSlider != null && playerDamageMultiplierSlider != null &&
                showHealthBarsCheckBox != null && showDamageNumbersCheckBox != null &&
                showComboProgressCheckBox != null)
            {
                settingsManager.SaveSettings(
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
                    enableSoundEffectsCheckBox,
                    enemyHealthMultiplierSlider,
                    enemyDamageMultiplierSlider,
                    playerHealthMultiplierSlider,
                    playerDamageMultiplierSlider,
                    showHealthBarsCheckBox,
                    showDamageNumbersCheckBox,
                    showComboProgressCheckBox,
                    () => gameVariablesTabManager?.SaveGameVariables());
            }
        }

        /// <summary>
        /// Saves text delay settings from UI controls
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
            if (settingsManager == null) return;
            
            if (enableGuiDelaysCheckBox != null && enableConsoleDelaysCheckBox != null &&
                actionDelaySlider != null && messageDelaySlider != null &&
                combatDelayTextBox != null && systemDelayTextBox != null &&
                menuDelayTextBox != null && titleDelayTextBox != null &&
                mainTitleDelayTextBox != null && environmentalDelayTextBox != null &&
                effectMessageDelayTextBox != null && damageOverTimeDelayTextBox != null &&
                encounterDelayTextBox != null && rollInfoDelayTextBox != null &&
                baseMenuDelayTextBox != null && progressiveReductionRateTextBox != null &&
                progressiveThresholdTextBox != null && combatPresetBaseDelayTextBox != null &&
                combatPresetMinDelayTextBox != null && combatPresetMaxDelayTextBox != null &&
                dungeonPresetBaseDelayTextBox != null && dungeonPresetMinDelayTextBox != null &&
                dungeonPresetMaxDelayTextBox != null && roomPresetBaseDelayTextBox != null &&
                roomPresetMinDelayTextBox != null && roomPresetMaxDelayTextBox != null &&
                narrativePresetBaseDelayTextBox != null && narrativePresetMinDelayTextBox != null &&
                narrativePresetMaxDelayTextBox != null && defaultPresetBaseDelayTextBox != null &&
                defaultPresetMinDelayTextBox != null && defaultPresetMaxDelayTextBox != null)
            {
                settingsManager.SaveTextDelaySettings(
                    enableGuiDelaysCheckBox,
                    enableConsoleDelaysCheckBox,
                    actionDelaySlider,
                    messageDelaySlider,
                    combatDelayTextBox,
                    systemDelayTextBox,
                    menuDelayTextBox,
                    titleDelayTextBox,
                    mainTitleDelayTextBox,
                    environmentalDelayTextBox,
                    effectMessageDelayTextBox,
                    damageOverTimeDelayTextBox,
                    encounterDelayTextBox,
                    rollInfoDelayTextBox,
                    baseMenuDelayTextBox,
                    progressiveReductionRateTextBox,
                    progressiveThresholdTextBox,
                    combatPresetBaseDelayTextBox,
                    combatPresetMinDelayTextBox,
                    combatPresetMaxDelayTextBox,
                    dungeonPresetBaseDelayTextBox,
                    dungeonPresetMinDelayTextBox,
                    dungeonPresetMaxDelayTextBox,
                    roomPresetBaseDelayTextBox,
                    roomPresetMinDelayTextBox,
                    roomPresetMaxDelayTextBox,
                    narrativePresetBaseDelayTextBox,
                    narrativePresetMinDelayTextBox,
                    narrativePresetMaxDelayTextBox,
                    defaultPresetBaseDelayTextBox,
                    defaultPresetMinDelayTextBox,
                    defaultPresetMaxDelayTextBox);
            }
        }

        /// <summary>
        /// Loads animation settings into UI controls
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
            if (settingsManager == null) return;
            
            if (brightnessMaskEnabledCheckBox != null && brightnessMaskIntensitySlider != null &&
                brightnessMaskIntensityTextBox != null && brightnessMaskWaveLengthSlider != null &&
                brightnessMaskWaveLengthTextBox != null && brightnessMaskUpdateIntervalTextBox != null &&
                undulationSpeedSlider != null && undulationSpeedTextBox != null &&
                undulationWaveLengthSlider != null && undulationWaveLengthTextBox != null &&
                undulationIntervalTextBox != null)
            {
                settingsManager.LoadAnimationSettings(
                    brightnessMaskEnabledCheckBox!,
                    brightnessMaskIntensitySlider!,
                    brightnessMaskIntensityTextBox!,
                    brightnessMaskWaveLengthSlider!,
                    brightnessMaskWaveLengthTextBox!,
                    brightnessMaskUpdateIntervalTextBox!,
                    undulationSpeedSlider!,
                    undulationSpeedTextBox!,
                    undulationWaveLengthSlider!,
                    undulationWaveLengthTextBox!,
                    undulationIntervalTextBox!);
            }
        }

        /// <summary>
        /// Saves animation settings from UI controls
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
            if (settingsManager == null) return;
            
            if (brightnessMaskEnabledCheckBox != null && brightnessMaskIntensitySlider != null &&
                brightnessMaskWaveLengthSlider != null && brightnessMaskUpdateIntervalTextBox != null &&
                undulationSpeedSlider != null && undulationWaveLengthSlider != null &&
                undulationIntervalTextBox != null)
            {
                settingsManager.SaveAnimationSettings(
                    brightnessMaskEnabledCheckBox!,
                    brightnessMaskIntensitySlider!,
                    brightnessMaskWaveLengthSlider!,
                    brightnessMaskUpdateIntervalTextBox!,
                    undulationSpeedSlider!,
                    undulationWaveLengthSlider!,
                    undulationIntervalTextBox!);
            }
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

