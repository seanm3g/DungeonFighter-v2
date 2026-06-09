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

        private static void SetTextBoxValue(TextBox? textBox, double value, string format = "F1")
        {
            if (textBox != null)
            {
                textBox.Text = value.ToString(format);
            }
        }

        /// <summary>
        /// Loads text delay settings from TextDelayConfiguration into UI controls using DTO.
        /// </summary>
        public void LoadTextDelaySettings(TextDelaySettingsControls? controls)
        {
            if (controls == null) return;
            LoadTextDelaySettings(
                controls.EnableGuiDelaysCheckBox,
                controls.EnableConsoleDelaysCheckBox,
                controls.ActionDelayMsTextBox,
                controls.MessageDelayMsTextBox,
                controls.TutorialCombatDelayMultiplierTextBox,
                controls.CombatDelayTextBox,
                controls.SystemDelayTextBox,
                controls.MenuDelayTextBox,
                controls.TitleDelayTextBox,
                controls.MainTitleDelayTextBox,
                controls.EnvironmentalDelayTextBox,
                controls.EffectMessageDelayTextBox,
                controls.DamageOverTimeDelayTextBox,
                controls.EncounterDelayTextBox,
                controls.RollInfoDelayTextBox,
                controls.EnvironmentalLineDelayTextBox,
                controls.BaseMenuDelayTextBox,
                controls.ProgressiveReductionRateTextBox,
                controls.ProgressiveThresholdTextBox,
                controls.CombatPresetBaseDelayTextBox,
                controls.CombatPresetMinDelayTextBox,
                controls.CombatPresetMaxDelayTextBox,
                controls.DungeonPresetBaseDelayTextBox,
                controls.DungeonPresetMinDelayTextBox,
                controls.DungeonPresetMaxDelayTextBox,
                controls.RoomPresetBaseDelayTextBox,
                controls.RoomPresetMinDelayTextBox,
                controls.RoomPresetMaxDelayTextBox,
                controls.NarrativePresetBaseDelayTextBox,
                controls.NarrativePresetMinDelayTextBox,
                controls.NarrativePresetMaxDelayTextBox,
                controls.DefaultPresetBaseDelayTextBox,
                controls.DefaultPresetMinDelayTextBox,
                controls.DefaultPresetMaxDelayTextBox,
                controls.TravelStepDelayBaseMsTextBox,
                controls.TravelStepExtraDelayMsPerPointTextBox,
                controls.TravelSummaryBaseMinutesTextBox,
                controls.TravelSummaryExtraMinutesPerPointTextBox);
        }

        /// <summary>
        /// Loads text delay settings from TextDelayConfiguration into UI controls (internal implementation).
        /// </summary>
        internal void LoadTextDelaySettings(
            CheckBox? enableGuiDelaysCheckBox,
            CheckBox? enableConsoleDelaysCheckBox,
            TextBox? actionDelayMsTextBox,
            TextBox? messageDelayMsTextBox,
            TextBox? tutorialCombatDelayMultiplierTextBox,
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
            TextBox? environmentalLineDelayTextBox,
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
            TextBox? travelStepDelayBaseMsTextBox,
            TextBox? travelStepExtraDelayMsPerPointTextBox,
            TextBox? travelSummaryBaseMinutesTextBox,
            TextBox? travelSummaryExtraMinutesPerPointTextBox)
        {
            try
            {
                TextDelayConfiguration.ReloadConfig();

                if (enableGuiDelaysCheckBox != null)
                    enableGuiDelaysCheckBox.IsChecked = TextDelayConfiguration.GetEnableGuiDelays();
                if (enableConsoleDelaysCheckBox != null)
                    enableConsoleDelaysCheckBox.IsChecked = TextDelayConfiguration.GetEnableConsoleDelays();

                SetTextBoxValue(actionDelayMsTextBox, TextDelayConfiguration.GetActionDelayMs());
                SetTextBoxValue(messageDelayMsTextBox, TextDelayConfiguration.GetMessageDelayMs());
                SetTextBoxValue(tutorialCombatDelayMultiplierTextBox, TextDelayConfiguration.GetTutorialCombatDelayMultiplier());

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

                SetTextBoxValue(environmentalLineDelayTextBox, TextDelayConfiguration.GetEnvironmentalLineDelay());

                var progressiveMenuDelays = TextDelayConfiguration.GetProgressiveMenuDelays();
                SetTextBoxValue(baseMenuDelayTextBox, progressiveMenuDelays.BaseMenuDelay);
                SetTextBoxValue(progressiveReductionRateTextBox, progressiveMenuDelays.ProgressiveReductionRate);
                SetTextBoxValue(progressiveThresholdTextBox, progressiveMenuDelays.ProgressiveThreshold);

                ApplyPresetToControls("Combat", combatPresetBaseDelayTextBox, combatPresetMinDelayTextBox, combatPresetMaxDelayTextBox);
                ApplyPresetToControls("Dungeon", dungeonPresetBaseDelayTextBox, dungeonPresetMinDelayTextBox, dungeonPresetMaxDelayTextBox);
                ApplyPresetToControls("Room", roomPresetBaseDelayTextBox, roomPresetMinDelayTextBox, roomPresetMaxDelayTextBox);
                ApplyPresetToControls("Narrative", narrativePresetBaseDelayTextBox, narrativePresetMinDelayTextBox, narrativePresetMaxDelayTextBox);
                ApplyPresetToControls("Default", defaultPresetBaseDelayTextBox, defaultPresetMinDelayTextBox, defaultPresetMaxDelayTextBox);

                var travelPacing = TextDelayConfiguration.GetTravelRouteRollPacing();
                SetTextBoxValue(travelStepDelayBaseMsTextBox, travelPacing.StepDelayBaseMs);
                SetTextBoxValue(travelStepExtraDelayMsPerPointTextBox, travelPacing.StepExtraDelayMsPerPointBelow20);
                SetTextBoxValue(travelSummaryBaseMinutesTextBox, travelPacing.SummaryBaseMinutes);
                SetTextBoxValue(travelSummaryExtraMinutesPerPointTextBox, travelPacing.SummaryExtraMinutesPerPointBelow20);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error loading text delay settings: {ex.Message}", false);
            }
        }

        private static void ApplyPresetToControls(string presetName, TextBox? baseBox, TextBox? minBox, TextBox? maxBox)
        {
            var preset = TextDelayConfiguration.GetChunkedTextRevealPreset(presetName);
            if (preset == null) return;
            SetTextBoxValue(baseBox, preset.BaseDelayPerCharMs);
            SetTextBoxValue(minBox, preset.MinDelayMs);
            SetTextBoxValue(maxBox, preset.MaxDelayMs);
        }

        /// <summary>
        /// Saves text delay settings from UI controls to TextDelayConfiguration using DTO.
        /// </summary>
        public void SaveTextDelaySettings(TextDelaySettingsControls? controls)
        {
            if (controls == null) return;
            SaveTextDelaySettings(
                controls.EnableGuiDelaysCheckBox,
                controls.EnableConsoleDelaysCheckBox,
                controls.ActionDelayMsTextBox,
                controls.MessageDelayMsTextBox,
                controls.TutorialCombatDelayMultiplierTextBox,
                controls.CombatDelayTextBox,
                controls.SystemDelayTextBox,
                controls.MenuDelayTextBox,
                controls.TitleDelayTextBox,
                controls.MainTitleDelayTextBox,
                controls.EnvironmentalDelayTextBox,
                controls.EffectMessageDelayTextBox,
                controls.DamageOverTimeDelayTextBox,
                controls.EncounterDelayTextBox,
                controls.RollInfoDelayTextBox,
                controls.EnvironmentalLineDelayTextBox,
                controls.BaseMenuDelayTextBox,
                controls.ProgressiveReductionRateTextBox,
                controls.ProgressiveThresholdTextBox,
                controls.CombatPresetBaseDelayTextBox,
                controls.CombatPresetMinDelayTextBox,
                controls.CombatPresetMaxDelayTextBox,
                controls.DungeonPresetBaseDelayTextBox,
                controls.DungeonPresetMinDelayTextBox,
                controls.DungeonPresetMaxDelayTextBox,
                controls.RoomPresetBaseDelayTextBox,
                controls.RoomPresetMinDelayTextBox,
                controls.RoomPresetMaxDelayTextBox,
                controls.NarrativePresetBaseDelayTextBox,
                controls.NarrativePresetMinDelayTextBox,
                controls.NarrativePresetMaxDelayTextBox,
                controls.DefaultPresetBaseDelayTextBox,
                controls.DefaultPresetMinDelayTextBox,
                controls.DefaultPresetMaxDelayTextBox,
                controls.TravelStepDelayBaseMsTextBox,
                controls.TravelStepExtraDelayMsPerPointTextBox,
                controls.TravelSummaryBaseMinutesTextBox,
                controls.TravelSummaryExtraMinutesPerPointTextBox);
        }

        /// <summary>
        /// Saves text delay settings from UI controls to TextDelayConfiguration (internal implementation).
        /// </summary>
        internal void SaveTextDelaySettings(
            CheckBox? enableGuiDelaysCheckBox,
            CheckBox? enableConsoleDelaysCheckBox,
            TextBox? actionDelayMsTextBox,
            TextBox? messageDelayMsTextBox,
            TextBox? tutorialCombatDelayMultiplierTextBox,
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
            TextBox? environmentalLineDelayTextBox,
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
            TextBox? travelStepDelayBaseMsTextBox,
            TextBox? travelStepExtraDelayMsPerPointTextBox,
            TextBox? travelSummaryBaseMinutesTextBox,
            TextBox? travelSummaryExtraMinutesPerPointTextBox)
        {
            try
            {
                if (enableGuiDelaysCheckBox != null)
                    TextDelayConfiguration.SetEnableGuiDelays(enableGuiDelaysCheckBox.IsChecked ?? true);
                if (enableConsoleDelaysCheckBox != null)
                    TextDelayConfiguration.SetEnableConsoleDelays(enableConsoleDelaysCheckBox.IsChecked ?? true);

                if (actionDelayMsTextBox != null && int.TryParse(actionDelayMsTextBox.Text, out int actionDelayMs))
                    TextDelayConfiguration.SetActionDelayMs(actionDelayMs);
                if (messageDelayMsTextBox != null && int.TryParse(messageDelayMsTextBox.Text, out int messageDelayMs))
                    TextDelayConfiguration.SetMessageDelayMs(messageDelayMs);

                if (tutorialCombatDelayMultiplierTextBox != null &&
                    double.TryParse(tutorialCombatDelayMultiplierTextBox.Text, out double tutorialCombatDelayMultiplier))
                {
                    TextDelayConfiguration.SetTutorialCombatDelayMultiplier(tutorialCombatDelayMultiplier);
                }

                TrySetMessageTypeDelay(combatDelayTextBox, UIMessageType.Combat);
                TrySetMessageTypeDelay(systemDelayTextBox, UIMessageType.System);
                TrySetMessageTypeDelay(menuDelayTextBox, UIMessageType.Menu);
                TrySetMessageTypeDelay(titleDelayTextBox, UIMessageType.Title);
                TrySetMessageTypeDelay(mainTitleDelayTextBox, UIMessageType.MainTitle);
                TrySetMessageTypeDelay(environmentalDelayTextBox, UIMessageType.Environmental);
                TrySetMessageTypeDelay(effectMessageDelayTextBox, UIMessageType.EffectMessage);
                TrySetMessageTypeDelay(damageOverTimeDelayTextBox, UIMessageType.DamageOverTime);
                TrySetMessageTypeDelay(encounterDelayTextBox, UIMessageType.Encounter);
                TrySetMessageTypeDelay(rollInfoDelayTextBox, UIMessageType.RollInfo);

                if (environmentalLineDelayTextBox != null && int.TryParse(environmentalLineDelayTextBox.Text, out int environmentalLineDelay))
                    TextDelayConfiguration.SetEnvironmentalLineDelay(environmentalLineDelay);

                if (baseMenuDelayTextBox != null && progressiveReductionRateTextBox != null && progressiveThresholdTextBox != null &&
                    int.TryParse(baseMenuDelayTextBox.Text, out int baseMenuDelay) &&
                    int.TryParse(progressiveReductionRateTextBox.Text, out int progressiveReductionRate) &&
                    int.TryParse(progressiveThresholdTextBox.Text, out int progressiveThreshold))
                {
                    TextDelayConfiguration.SetProgressiveMenuDelays(new ProgressiveMenuDelaysConfig
                    {
                        BaseMenuDelay = baseMenuDelay,
                        ProgressiveReductionRate = progressiveReductionRate,
                        ProgressiveThreshold = progressiveThreshold
                    });
                }

                TrySetPreset("Combat", combatPresetBaseDelayTextBox, combatPresetMinDelayTextBox, combatPresetMaxDelayTextBox);
                TrySetPreset("Dungeon", dungeonPresetBaseDelayTextBox, dungeonPresetMinDelayTextBox, dungeonPresetMaxDelayTextBox);
                TrySetPreset("Room", roomPresetBaseDelayTextBox, roomPresetMinDelayTextBox, roomPresetMaxDelayTextBox);
                TrySetPreset("Narrative", narrativePresetBaseDelayTextBox, narrativePresetMinDelayTextBox, narrativePresetMaxDelayTextBox);
                TrySetPreset("Default", defaultPresetBaseDelayTextBox, defaultPresetMinDelayTextBox, defaultPresetMaxDelayTextBox);

                TrySetTravelRouteRollPacing(
                    travelStepDelayBaseMsTextBox,
                    travelStepExtraDelayMsPerPointTextBox,
                    travelSummaryBaseMinutesTextBox,
                    travelSummaryExtraMinutesPerPointTextBox);

                TextDelayConfiguration.SaveCurrentConfigToFile();
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving text delay settings: {ex.Message}", false);
            }
        }

        private static void TrySetMessageTypeDelay(TextBox? textBox, UIMessageType messageType)
        {
            if (textBox != null && int.TryParse(textBox.Text, out int value))
                TextDelayConfiguration.SetMessageTypeDelay(messageType, value);
        }

        private static void TrySetPreset(string presetName, TextBox? baseBox, TextBox? minBox, TextBox? maxBox)
        {
            if (baseBox == null || minBox == null || maxBox == null) return;
            if (!int.TryParse(baseBox.Text, out int baseVal) || !int.TryParse(minBox.Text, out int minVal) || !int.TryParse(maxBox.Text, out int maxVal))
                return;
            var preset = TextDelayConfiguration.GetChunkedTextRevealPreset(presetName) ?? new ChunkedTextRevealPreset();
            preset.BaseDelayPerCharMs = baseVal;
            preset.MinDelayMs = minVal;
            preset.MaxDelayMs = maxVal;
            TextDelayConfiguration.SetChunkedTextRevealPreset(presetName, preset);
        }

        private static void TrySetTravelRouteRollPacing(
            TextBox? stepBaseMs,
            TextBox? stepExtraPerPoint,
            TextBox? summaryBaseMinutes,
            TextBox? summaryExtraPerPoint)
        {
            if (stepBaseMs == null || stepExtraPerPoint == null || summaryBaseMinutes == null || summaryExtraPerPoint == null)
                return;
            if (!int.TryParse(stepBaseMs.Text, out int b) ||
                !int.TryParse(stepExtraPerPoint.Text, out int e) ||
                !int.TryParse(summaryBaseMinutes.Text, out int sb) ||
                !int.TryParse(summaryExtraPerPoint.Text, out int se))
                return;

            TextDelayConfiguration.SetTravelRouteRollPacing(new TravelRouteRollPacingConfig
            {
                StepDelayBaseMs = b,
                StepExtraDelayMsPerPointBelow20 = e,
                SummaryBaseMinutes = sb,
                SummaryExtraMinutesPerPointBelow20 = se
            });
        }
    }
}

