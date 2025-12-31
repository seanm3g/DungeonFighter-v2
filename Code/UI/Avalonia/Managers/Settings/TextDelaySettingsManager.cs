using System;
using Avalonia.Controls;
using RPGGame.Config;
using RPGGame.UI;

namespace RPGGame.UI.Avalonia.Managers.Settings
{
    /// <summary>
    /// Manages loading and saving of text delay settings
    /// Extracted from SettingsManager to separate text delay configuration logic
    /// </summary>
    public class TextDelaySettingsManager
    {
        private readonly Action<string, bool>? showStatusMessage;

        public TextDelaySettingsManager(Action<string, bool>? showStatusMessage)
        {
            this.showStatusMessage = showStatusMessage;
        }

        /// <summary>
        /// Helper method to safely set text box value
        /// </summary>
        private static void SetTextBoxValue(TextBox? textBox, int value)
        {
            if (textBox != null)
            {
                textBox.Text = value.ToString();
            }
        }

        /// <summary>
        /// Loads text delay settings from TextDelayConfiguration into UI controls
        /// </summary>
        public void LoadTextDelaySettings(
            CheckBox? enableGuiDelaysCheckBox,
            CheckBox? enableConsoleDelaysCheckBox,
            Slider? actionDelaySlider, // Deprecated - kept for compatibility but not used
            TextBox? actionDelayTextBox, // Deprecated - kept for compatibility but not used
            Slider? messageDelaySlider, // Deprecated - kept for compatibility but not used
            TextBox? messageDelayTextBox, // Deprecated - kept for compatibility but not used
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
            try
            {
                // Ensure config is loaded before accessing values
                TextDelayConfiguration.ReloadConfig();
                
                // Enable flags
                if (enableGuiDelaysCheckBox != null)
                    enableGuiDelaysCheckBox.IsChecked = TextDelayConfiguration.GetEnableGuiDelays();
                if (enableConsoleDelaysCheckBox != null)
                    enableConsoleDelaysCheckBox.IsChecked = TextDelayConfiguration.GetEnableConsoleDelays();
                
                // Note: ActionDelay and MessageDelay sliders removed - combat timing is now controlled by
                // MessageTypeDelays.Combat and ChunkedTextReveal.Combat presets
                
                // Message type delays - always set values even if 0
                SetTextBoxValue(combatDelayTextBox, TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.Combat));
                SetTextBoxValue(systemDelayTextBox, TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.System));
                SetTextBoxValue(menuDelayTextBox, TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.Menu));
                SetTextBoxValue(titleDelayTextBox, TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.Title));
                SetTextBoxValue(mainTitleDelayTextBox, TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.MainTitle));
                SetTextBoxValue(environmentalDelayTextBox, TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.Environmental));
                SetTextBoxValue(effectMessageDelayTextBox, TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.EffectMessage));
                SetTextBoxValue(damageOverTimeDelayTextBox, TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.DamageOverTime));
                SetTextBoxValue(encounterDelayTextBox, TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.Encounter));
                SetTextBoxValue(rollInfoDelayTextBox, TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.RollInfo));
                
                // Progressive menu delays
                var progressiveMenuDelays = TextDelayConfiguration.GetProgressiveMenuDelays();
                SetTextBoxValue(baseMenuDelayTextBox, progressiveMenuDelays.BaseMenuDelay);
                SetTextBoxValue(progressiveReductionRateTextBox, progressiveMenuDelays.ProgressiveReductionRate);
                SetTextBoxValue(progressiveThresholdTextBox, progressiveMenuDelays.ProgressiveThreshold);
                
                // Chunked text reveal presets
                var combatPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Combat");
                if (combatPreset != null)
                {
                    SetTextBoxValue(combatPresetBaseDelayTextBox, combatPreset.BaseDelayPerCharMs);
                    SetTextBoxValue(combatPresetMinDelayTextBox, combatPreset.MinDelayMs);
                    SetTextBoxValue(combatPresetMaxDelayTextBox, combatPreset.MaxDelayMs);
                }
                
                var dungeonPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Dungeon");
                if (dungeonPreset != null)
                {
                    SetTextBoxValue(dungeonPresetBaseDelayTextBox, dungeonPreset.BaseDelayPerCharMs);
                    SetTextBoxValue(dungeonPresetMinDelayTextBox, dungeonPreset.MinDelayMs);
                    SetTextBoxValue(dungeonPresetMaxDelayTextBox, dungeonPreset.MaxDelayMs);
                }
                
                var roomPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Room");
                if (roomPreset != null)
                {
                    SetTextBoxValue(roomPresetBaseDelayTextBox, roomPreset.BaseDelayPerCharMs);
                    SetTextBoxValue(roomPresetMinDelayTextBox, roomPreset.MinDelayMs);
                    SetTextBoxValue(roomPresetMaxDelayTextBox, roomPreset.MaxDelayMs);
                }
                
                var narrativePreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Narrative");
                if (narrativePreset != null)
                {
                    SetTextBoxValue(narrativePresetBaseDelayTextBox, narrativePreset.BaseDelayPerCharMs);
                    SetTextBoxValue(narrativePresetMinDelayTextBox, narrativePreset.MinDelayMs);
                    SetTextBoxValue(narrativePresetMaxDelayTextBox, narrativePreset.MaxDelayMs);
                }
                
                var defaultPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Default");
                if (defaultPreset != null)
                {
                    SetTextBoxValue(defaultPresetBaseDelayTextBox, defaultPreset.BaseDelayPerCharMs);
                    SetTextBoxValue(defaultPresetMinDelayTextBox, defaultPreset.MinDelayMs);
                    SetTextBoxValue(defaultPresetMaxDelayTextBox, defaultPreset.MaxDelayMs);
                }
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error loading text delay settings: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Saves text delay settings from UI controls to TextDelayConfiguration
        /// </summary>
        public void SaveTextDelaySettings(
            CheckBox? enableGuiDelaysCheckBox,
            CheckBox? enableConsoleDelaysCheckBox,
            Slider? actionDelaySlider, // Deprecated - kept for compatibility but not used
            Slider? messageDelaySlider, // Deprecated - kept for compatibility but not used
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
            try
            {
                // Enable flags
                if (enableGuiDelaysCheckBox != null)
                    TextDelayConfiguration.SetEnableGuiDelays(enableGuiDelaysCheckBox.IsChecked ?? true);
                if (enableConsoleDelaysCheckBox != null)
                    TextDelayConfiguration.SetEnableConsoleDelays(enableConsoleDelaysCheckBox.IsChecked ?? true);
                
                // Note: ActionDelay and MessageDelay sliders removed - combat timing is now controlled by
                // MessageTypeDelays.Combat and ChunkedTextReveal.Combat presets
                
                // Message type delays
                if (combatDelayTextBox != null && int.TryParse(combatDelayTextBox.Text, out int combatDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.Combat, combatDelay);
                if (systemDelayTextBox != null && int.TryParse(systemDelayTextBox.Text, out int systemDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.System, systemDelay);
                if (menuDelayTextBox != null && int.TryParse(menuDelayTextBox.Text, out int menuDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.Menu, menuDelay);
                if (titleDelayTextBox != null && int.TryParse(titleDelayTextBox.Text, out int titleDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.Title, titleDelay);
                if (mainTitleDelayTextBox != null && int.TryParse(mainTitleDelayTextBox.Text, out int mainTitleDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.MainTitle, mainTitleDelay);
                if (environmentalDelayTextBox != null && int.TryParse(environmentalDelayTextBox.Text, out int environmentalDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.Environmental, environmentalDelay);
                if (effectMessageDelayTextBox != null && int.TryParse(effectMessageDelayTextBox.Text, out int effectMessageDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.EffectMessage, effectMessageDelay);
                if (damageOverTimeDelayTextBox != null && int.TryParse(damageOverTimeDelayTextBox.Text, out int damageOverTimeDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.DamageOverTime, damageOverTimeDelay);
                if (encounterDelayTextBox != null && int.TryParse(encounterDelayTextBox.Text, out int encounterDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.Encounter, encounterDelay);
                if (rollInfoDelayTextBox != null && int.TryParse(rollInfoDelayTextBox.Text, out int rollInfoDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.RollInfo, rollInfoDelay);
                
                // Progressive menu delays
                if (baseMenuDelayTextBox != null && progressiveReductionRateTextBox != null && progressiveThresholdTextBox != null &&
                    int.TryParse(baseMenuDelayTextBox.Text, out int baseMenuDelay) &&
                    int.TryParse(progressiveReductionRateTextBox.Text, out int progressiveReductionRate) &&
                    int.TryParse(progressiveThresholdTextBox.Text, out int progressiveThreshold))
                {
                    var progressiveConfig = new ProgressiveMenuDelaysConfig
                    {
                        BaseMenuDelay = baseMenuDelay,
                        ProgressiveReductionRate = progressiveReductionRate,
                        ProgressiveThreshold = progressiveThreshold
                    };
                    TextDelayConfiguration.SetProgressiveMenuDelays(progressiveConfig);
                }
                
                // Chunked text reveal presets
                if (combatPresetBaseDelayTextBox != null && combatPresetMinDelayTextBox != null && combatPresetMaxDelayTextBox != null &&
                    int.TryParse(combatPresetBaseDelayTextBox.Text, out int combatBase) &&
                    int.TryParse(combatPresetMinDelayTextBox.Text, out int combatMin) &&
                    int.TryParse(combatPresetMaxDelayTextBox.Text, out int combatMax))
                {
                    var combatPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Combat") ?? new ChunkedTextRevealPreset();
                    combatPreset.BaseDelayPerCharMs = combatBase;
                    combatPreset.MinDelayMs = combatMin;
                    combatPreset.MaxDelayMs = combatMax;
                    TextDelayConfiguration.SetChunkedTextRevealPreset("Combat", combatPreset);
                }
                
                if (dungeonPresetBaseDelayTextBox != null && dungeonPresetMinDelayTextBox != null && dungeonPresetMaxDelayTextBox != null &&
                    int.TryParse(dungeonPresetBaseDelayTextBox.Text, out int dungeonBase) &&
                    int.TryParse(dungeonPresetMinDelayTextBox.Text, out int dungeonMin) &&
                    int.TryParse(dungeonPresetMaxDelayTextBox.Text, out int dungeonMax))
                {
                    var dungeonPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Dungeon") ?? new ChunkedTextRevealPreset();
                    dungeonPreset.BaseDelayPerCharMs = dungeonBase;
                    dungeonPreset.MinDelayMs = dungeonMin;
                    dungeonPreset.MaxDelayMs = dungeonMax;
                    TextDelayConfiguration.SetChunkedTextRevealPreset("Dungeon", dungeonPreset);
                }
                
                if (roomPresetBaseDelayTextBox != null && roomPresetMinDelayTextBox != null && roomPresetMaxDelayTextBox != null &&
                    int.TryParse(roomPresetBaseDelayTextBox.Text, out int roomBase) &&
                    int.TryParse(roomPresetMinDelayTextBox.Text, out int roomMin) &&
                    int.TryParse(roomPresetMaxDelayTextBox.Text, out int roomMax))
                {
                    var roomPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Room") ?? new ChunkedTextRevealPreset();
                    roomPreset.BaseDelayPerCharMs = roomBase;
                    roomPreset.MinDelayMs = roomMin;
                    roomPreset.MaxDelayMs = roomMax;
                    TextDelayConfiguration.SetChunkedTextRevealPreset("Room", roomPreset);
                }
                
                if (narrativePresetBaseDelayTextBox != null && narrativePresetMinDelayTextBox != null && narrativePresetMaxDelayTextBox != null &&
                    int.TryParse(narrativePresetBaseDelayTextBox.Text, out int narrativeBase) &&
                    int.TryParse(narrativePresetMinDelayTextBox.Text, out int narrativeMin) &&
                    int.TryParse(narrativePresetMaxDelayTextBox.Text, out int narrativeMax))
                {
                    var narrativePreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Narrative") ?? new ChunkedTextRevealPreset();
                    narrativePreset.BaseDelayPerCharMs = narrativeBase;
                    narrativePreset.MinDelayMs = narrativeMin;
                    narrativePreset.MaxDelayMs = narrativeMax;
                    TextDelayConfiguration.SetChunkedTextRevealPreset("Narrative", narrativePreset);
                }
                
                if (defaultPresetBaseDelayTextBox != null && defaultPresetMinDelayTextBox != null && defaultPresetMaxDelayTextBox != null &&
                    int.TryParse(defaultPresetBaseDelayTextBox.Text, out int defaultBase) &&
                    int.TryParse(defaultPresetMinDelayTextBox.Text, out int defaultMin) &&
                    int.TryParse(defaultPresetMaxDelayTextBox.Text, out int defaultMax))
                {
                    var defaultPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Default") ?? new ChunkedTextRevealPreset();
                    defaultPreset.BaseDelayPerCharMs = defaultBase;
                    defaultPreset.MinDelayMs = defaultMin;
                    defaultPreset.MaxDelayMs = defaultMax;
                    TextDelayConfiguration.SetChunkedTextRevealPreset("Default", defaultPreset);
                }
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving text delay settings: {ex.Message}", false);
            }
        }
    }
}

