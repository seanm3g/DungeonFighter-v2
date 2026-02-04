using Avalonia.Controls;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Managers.Settings;
using RPGGame.UI.Avalonia.Settings;
using RPGGame.Utils;
using System;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>
    /// Handles wiring and loading for the Text Delays settings panel
    /// </summary>
    public class TextDelaysPanelHandler : ISettingsPanelHandler
    {
        private readonly SettingsManager? settingsManager;

        public string PanelType => "TextDelays";

        public TextDelaysPanelHandler(SettingsManager? settingsManager)
        {
            this.settingsManager = settingsManager;
        }

        public void WireUp(UserControl panel)
        {
            if (panel is not TextDelaysSettingsPanel textDelaysPanel || settingsManager == null) return;

            // Wire up checkboxes
            if (textDelaysPanel.EnableGuiDelaysCheckBox != null)
            {
                textDelaysPanel.EnableGuiDelaysCheckBox.IsCheckedChanged += (s, e) =>
                {
                    if (textDelaysPanel.EnableGuiDelaysCheckBox.IsChecked.HasValue)
                    {
                        try
                        {
                            RPGGame.Config.TextDelayConfiguration.SetEnableGuiDelays(textDelaysPanel.EnableGuiDelaysCheckBox.IsChecked.Value);
                        }
                        catch (Exception ex)
                        {
                            ScrollDebugLogger.Log($"Error updating EnableGuiDelays: {ex.Message}");
                        }
                    }
                };
            }

            if (textDelaysPanel.EnableConsoleDelaysCheckBox != null)
            {
                textDelaysPanel.EnableConsoleDelaysCheckBox.IsCheckedChanged += (s, e) =>
                {
                    if (textDelaysPanel.EnableConsoleDelaysCheckBox.IsChecked.HasValue)
                    {
                        try
                        {
                            RPGGame.Config.TextDelayConfiguration.SetEnableConsoleDelays(textDelaysPanel.EnableConsoleDelaysCheckBox.IsChecked.Value);
                        }
                        catch (Exception ex)
                        {
                            ScrollDebugLogger.Log($"Error updating EnableConsoleDelays: {ex.Message}");
                        }
                    }
                };
            }

            // Wire up combat timing (action block delay, message delay between lines)
            WireUpCombatTimingTextBox(textDelaysPanel.ActionDelayMsTextBox ?? textDelaysPanel.FindControl<TextBox>("ActionDelayMsTextBox"), isActionDelay: true);
            WireUpCombatTimingTextBox(textDelaysPanel.MessageDelayMsTextBox ?? textDelaysPanel.FindControl<TextBox>("MessageDelayMsTextBox"), isActionDelay: false);

            // Wire up textboxes for message type delays
            WireUpTextDelayTextBox(textDelaysPanel.CombatDelayTextBox, UIMessageType.Combat);
            WireUpTextDelayTextBox(textDelaysPanel.SystemDelayTextBox, UIMessageType.System);
            WireUpTextDelayTextBox(textDelaysPanel.MenuDelayTextBox, UIMessageType.Menu);
            WireUpTextDelayTextBox(textDelaysPanel.TitleDelayTextBox, UIMessageType.Title);
            WireUpTextDelayTextBox(textDelaysPanel.MainTitleDelayTextBox, UIMessageType.MainTitle);
            WireUpTextDelayTextBox(textDelaysPanel.EnvironmentalDelayTextBox, UIMessageType.Environmental);
            WireUpTextDelayTextBox(textDelaysPanel.EffectMessageDelayTextBox, UIMessageType.EffectMessage);
            WireUpTextDelayTextBox(textDelaysPanel.DamageOverTimeDelayTextBox, UIMessageType.DamageOverTime);
            WireUpTextDelayTextBox(textDelaysPanel.EncounterDelayTextBox, UIMessageType.Encounter);
            WireUpTextDelayTextBox(textDelaysPanel.RollInfoDelayTextBox, UIMessageType.RollInfo);

            // Wire up progressive menu delay textboxes
            WireUpProgressiveMenuDelayTextBoxes(textDelaysPanel);

            // Wire up preset delay textboxes
            WireUpPresetDelayTextBoxes(textDelaysPanel);

            // Load current settings after panel is fully loaded. Also load when IsLoaded is already true
            // (handler runs after Loaded due to deferred InitializePanelHandlers), so we don't miss the first show.
            textDelaysPanel.Loaded += (s, e) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    LoadSettings(textDelaysPanel);
                }, DispatcherPriority.Loaded);
            };
            if (panel is Control control && control.IsLoaded)
            {
                Dispatcher.UIThread.Post(() => LoadSettings(panel), DispatcherPriority.Loaded);
            }
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not TextDelaysSettingsPanel textDelaysPanel || settingsManager == null) return;

            try
            {
                // Use FindControl to ensure controls are found even if auto-generated properties aren't initialized yet
                // This is more reliable than relying on auto-generated properties which may be null during early Loaded events
                var enableGuiDelaysCheckBox = textDelaysPanel.EnableGuiDelaysCheckBox ?? textDelaysPanel.FindControl<CheckBox>("EnableGuiDelaysCheckBox");
                var enableConsoleDelaysCheckBox = textDelaysPanel.EnableConsoleDelaysCheckBox ?? textDelaysPanel.FindControl<CheckBox>("EnableConsoleDelaysCheckBox");
                var actionDelayMsTextBox = textDelaysPanel.ActionDelayMsTextBox ?? textDelaysPanel.FindControl<TextBox>("ActionDelayMsTextBox");
                var messageDelayMsTextBox = textDelaysPanel.MessageDelayMsTextBox ?? textDelaysPanel.FindControl<TextBox>("MessageDelayMsTextBox");

                // Find all text boxes using FindControl for reliability
                var combatDelayTextBox = textDelaysPanel.CombatDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("CombatDelayTextBox");
                var systemDelayTextBox = textDelaysPanel.SystemDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("SystemDelayTextBox");
                var menuDelayTextBox = textDelaysPanel.MenuDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("MenuDelayTextBox");
                var titleDelayTextBox = textDelaysPanel.TitleDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("TitleDelayTextBox");
                var mainTitleDelayTextBox = textDelaysPanel.MainTitleDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("MainTitleDelayTextBox");
                var environmentalDelayTextBox = textDelaysPanel.EnvironmentalDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("EnvironmentalDelayTextBox");
                var effectMessageDelayTextBox = textDelaysPanel.EffectMessageDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("EffectMessageDelayTextBox");
                var damageOverTimeDelayTextBox = textDelaysPanel.DamageOverTimeDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("DamageOverTimeDelayTextBox");
                var encounterDelayTextBox = textDelaysPanel.EncounterDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("EncounterDelayTextBox");
                var rollInfoDelayTextBox = textDelaysPanel.RollInfoDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("RollInfoDelayTextBox");
                var environmentalLineDelayTextBox = textDelaysPanel.EnvironmentalLineDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("EnvironmentalLineDelayTextBox");
                var baseMenuDelayTextBox = textDelaysPanel.BaseMenuDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("BaseMenuDelayTextBox");
                var progressiveReductionRateTextBox = textDelaysPanel.ProgressiveReductionRateTextBox ?? textDelaysPanel.FindControl<TextBox>("ProgressiveReductionRateTextBox");
                var progressiveThresholdTextBox = textDelaysPanel.ProgressiveThresholdTextBox ?? textDelaysPanel.FindControl<TextBox>("ProgressiveThresholdTextBox");
                var combatPresetBaseDelayTextBox = textDelaysPanel.CombatPresetBaseDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("CombatPresetBaseDelayTextBox");
                var combatPresetMinDelayTextBox = textDelaysPanel.CombatPresetMinDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("CombatPresetMinDelayTextBox");
                var combatPresetMaxDelayTextBox = textDelaysPanel.CombatPresetMaxDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("CombatPresetMaxDelayTextBox");
                var dungeonPresetBaseDelayTextBox = textDelaysPanel.DungeonPresetBaseDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("DungeonPresetBaseDelayTextBox");
                var dungeonPresetMinDelayTextBox = textDelaysPanel.DungeonPresetMinDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("DungeonPresetMinDelayTextBox");
                var dungeonPresetMaxDelayTextBox = textDelaysPanel.DungeonPresetMaxDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("DungeonPresetMaxDelayTextBox");
                var roomPresetBaseDelayTextBox = textDelaysPanel.RoomPresetBaseDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("RoomPresetBaseDelayTextBox");
                var roomPresetMinDelayTextBox = textDelaysPanel.RoomPresetMinDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("RoomPresetMinDelayTextBox");
                var roomPresetMaxDelayTextBox = textDelaysPanel.RoomPresetMaxDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("RoomPresetMaxDelayTextBox");
                var narrativePresetBaseDelayTextBox = textDelaysPanel.NarrativePresetBaseDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("NarrativePresetBaseDelayTextBox");
                var narrativePresetMinDelayTextBox = textDelaysPanel.NarrativePresetMinDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("NarrativePresetMinDelayTextBox");
                var narrativePresetMaxDelayTextBox = textDelaysPanel.NarrativePresetMaxDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("NarrativePresetMaxDelayTextBox");
                var defaultPresetBaseDelayTextBox = textDelaysPanel.DefaultPresetBaseDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("DefaultPresetBaseDelayTextBox");
                var defaultPresetMinDelayTextBox = textDelaysPanel.DefaultPresetMinDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("DefaultPresetMinDelayTextBox");
                var defaultPresetMaxDelayTextBox = textDelaysPanel.DefaultPresetMaxDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("DefaultPresetMaxDelayTextBox");

                var controls = BuildControls(
                    enableGuiDelaysCheckBox, enableConsoleDelaysCheckBox, actionDelayMsTextBox, messageDelayMsTextBox,
                    combatDelayTextBox, systemDelayTextBox, menuDelayTextBox, titleDelayTextBox, mainTitleDelayTextBox,
                    environmentalDelayTextBox, effectMessageDelayTextBox, damageOverTimeDelayTextBox, encounterDelayTextBox,
                    rollInfoDelayTextBox, environmentalLineDelayTextBox, baseMenuDelayTextBox, progressiveReductionRateTextBox,
                    progressiveThresholdTextBox, combatPresetBaseDelayTextBox, combatPresetMinDelayTextBox, combatPresetMaxDelayTextBox,
                    dungeonPresetBaseDelayTextBox, dungeonPresetMinDelayTextBox, dungeonPresetMaxDelayTextBox,
                    roomPresetBaseDelayTextBox, roomPresetMinDelayTextBox, roomPresetMaxDelayTextBox,
                    narrativePresetBaseDelayTextBox, narrativePresetMinDelayTextBox, narrativePresetMaxDelayTextBox,
                    defaultPresetBaseDelayTextBox, defaultPresetMinDelayTextBox, defaultPresetMaxDelayTextBox);
                settingsManager.LoadTextDelaySettings(controls);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsPanel: Error loading text delay settings: {ex.Message}");
            }
        }

        private void WireUpTextDelayTextBox(TextBox? textBox, UIMessageType messageType)
        {
            if (textBox == null) return;

            textBox.LostFocus += (s, e) =>
            {
                if (int.TryParse(textBox.Text, out int value) && value >= 0)
                {
                    try
                    {
                        RPGGame.Config.TextDelayConfiguration.SetMessageTypeDelay(messageType, value);
                    }
                    catch (Exception ex)
                    {
                        ScrollDebugLogger.Log($"Error updating {messageType} delay: {ex.Message}");
                        // Restore previous value
                        var currentValue = RPGGame.Config.TextDelayConfiguration.GetMessageTypeDelay(messageType);
                        textBox.Text = currentValue.ToString();
                    }
                }
                else
                {
                    // Restore previous value if invalid
                    var currentValue = RPGGame.Config.TextDelayConfiguration.GetMessageTypeDelay(messageType);
                    textBox.Text = currentValue.ToString();
                }
            };
        }

        private void WireUpCombatTimingTextBox(TextBox? textBox, bool isActionDelay)
        {
            if (textBox == null) return;

            textBox.LostFocus += (s, e) =>
            {
                if (int.TryParse(textBox.Text, out int value) && value >= 0)
                {
                    try
                    {
                        if (isActionDelay)
                            RPGGame.Config.TextDelayConfiguration.SetActionDelayMs(value);
                        else
                            RPGGame.Config.TextDelayConfiguration.SetMessageDelayMs(value);
                    }
                    catch (Exception ex)
                    {
                        ScrollDebugLogger.Log($"Error updating combat timing: {ex.Message}");
                        var currentValue = isActionDelay ? RPGGame.Config.TextDelayConfiguration.GetActionDelayMs() : RPGGame.Config.TextDelayConfiguration.GetMessageDelayMs();
                        textBox.Text = currentValue.ToString();
                    }
                }
                else
                {
                    var currentValue = isActionDelay ? RPGGame.Config.TextDelayConfiguration.GetActionDelayMs() : RPGGame.Config.TextDelayConfiguration.GetMessageDelayMs();
                    textBox.Text = currentValue.ToString();
                }
            };
        }

        private void WireUpProgressiveMenuDelayTextBoxes(TextDelaysSettingsPanel panel)
        {
            if (panel.BaseMenuDelayTextBox != null)
            {
                panel.BaseMenuDelayTextBox.LostFocus += (s, e) =>
                {
                    UpdateProgressiveMenuDelays(panel);
                };
            }

            if (panel.ProgressiveReductionRateTextBox != null)
            {
                panel.ProgressiveReductionRateTextBox.LostFocus += (s, e) =>
                {
                    UpdateProgressiveMenuDelays(panel);
                };
            }

            if (panel.ProgressiveThresholdTextBox != null)
            {
                panel.ProgressiveThresholdTextBox.LostFocus += (s, e) =>
                {
                    UpdateProgressiveMenuDelays(panel);
                };
            }
        }

        private void UpdateProgressiveMenuDelays(TextDelaysSettingsPanel panel)
        {
            if (panel.BaseMenuDelayTextBox != null &&
                panel.ProgressiveReductionRateTextBox != null &&
                panel.ProgressiveThresholdTextBox != null &&
                int.TryParse(panel.BaseMenuDelayTextBox.Text, out int baseDelay) &&
                int.TryParse(panel.ProgressiveReductionRateTextBox.Text, out int reductionRate) &&
                int.TryParse(panel.ProgressiveThresholdTextBox.Text, out int threshold))
            {
                try
                {
                    var config = new RPGGame.Config.ProgressiveMenuDelaysConfig
                    {
                        BaseMenuDelay = baseDelay,
                        ProgressiveReductionRate = reductionRate,
                        ProgressiveThreshold = threshold
                    };
                    RPGGame.Config.TextDelayConfiguration.SetProgressiveMenuDelays(config);
                }
                catch (Exception ex)
                {
                    ScrollDebugLogger.Log($"Error updating progressive menu delays: {ex.Message}");
                }
            }
        }

        private void WireUpPresetDelayTextBoxes(TextDelaysSettingsPanel panel)
        {
            // Combat preset
            WireUpPresetTextBoxes(panel.CombatPresetBaseDelayTextBox, panel.CombatPresetMinDelayTextBox, panel.CombatPresetMaxDelayTextBox, "Combat");

            // Dungeon preset
            WireUpPresetTextBoxes(panel.DungeonPresetBaseDelayTextBox, panel.DungeonPresetMinDelayTextBox, panel.DungeonPresetMaxDelayTextBox, "Dungeon");

            // Room preset
            WireUpPresetTextBoxes(panel.RoomPresetBaseDelayTextBox, panel.RoomPresetMinDelayTextBox, panel.RoomPresetMaxDelayTextBox, "Room");

            // Narrative preset
            WireUpPresetTextBoxes(panel.NarrativePresetBaseDelayTextBox, panel.NarrativePresetMinDelayTextBox, panel.NarrativePresetMaxDelayTextBox, "Narrative");

            // Default preset
            WireUpPresetTextBoxes(panel.DefaultPresetBaseDelayTextBox, panel.DefaultPresetMinDelayTextBox, panel.DefaultPresetMaxDelayTextBox, "Default");
        }

        private void WireUpPresetTextBoxes(TextBox? baseTextBox, TextBox? minTextBox, TextBox? maxTextBox, string presetName)
        {
            if (baseTextBox != null)
            {
                baseTextBox.LostFocus += (s, e) => UpdatePresetDelays(baseTextBox, minTextBox, maxTextBox, presetName);
            }

            if (minTextBox != null)
            {
                minTextBox.LostFocus += (s, e) => UpdatePresetDelays(baseTextBox, minTextBox, maxTextBox, presetName);
            }

            if (maxTextBox != null)
            {
                maxTextBox.LostFocus += (s, e) => UpdatePresetDelays(baseTextBox, minTextBox, maxTextBox, presetName);
            }
        }

        private void UpdatePresetDelays(TextBox? baseTextBox, TextBox? minTextBox, TextBox? maxTextBox, string presetName)
        {
            if (baseTextBox != null && minTextBox != null && maxTextBox != null &&
                int.TryParse(baseTextBox.Text, out int baseDelay) &&
                int.TryParse(minTextBox.Text, out int minDelay) &&
                int.TryParse(maxTextBox.Text, out int maxDelay))
            {
                try
                {
                    var preset = RPGGame.Config.TextDelayConfiguration.GetChunkedTextRevealPreset(presetName) ?? new RPGGame.Config.ChunkedTextRevealPreset();
                    preset.BaseDelayPerCharMs = baseDelay;
                    preset.MinDelayMs = minDelay;
                    preset.MaxDelayMs = maxDelay;
                    RPGGame.Config.TextDelayConfiguration.SetChunkedTextRevealPreset(presetName, preset);
                }
                catch (Exception ex)
                {
                    ScrollDebugLogger.Log($"Error updating {presetName} preset delays: {ex.Message}");
                }
            }
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not TextDelaysSettingsPanel textDelaysPanel || settingsManager == null) return;
            try
            {
                var enableGui = textDelaysPanel.EnableGuiDelaysCheckBox ?? textDelaysPanel.FindControl<CheckBox>("EnableGuiDelaysCheckBox");
                var enableConsole = textDelaysPanel.EnableConsoleDelaysCheckBox ?? textDelaysPanel.FindControl<CheckBox>("EnableConsoleDelaysCheckBox");
                var actionDelayMs = textDelaysPanel.ActionDelayMsTextBox ?? textDelaysPanel.FindControl<TextBox>("ActionDelayMsTextBox");
                var messageDelayMs = textDelaysPanel.MessageDelayMsTextBox ?? textDelaysPanel.FindControl<TextBox>("MessageDelayMsTextBox");
                var combat = textDelaysPanel.CombatDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("CombatDelayTextBox");
                var system = textDelaysPanel.SystemDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("SystemDelayTextBox");
                var menu = textDelaysPanel.MenuDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("MenuDelayTextBox");
                var title = textDelaysPanel.TitleDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("TitleDelayTextBox");
                var mainTitle = textDelaysPanel.MainTitleDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("MainTitleDelayTextBox");
                var environmental = textDelaysPanel.EnvironmentalDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("EnvironmentalDelayTextBox");
                var effectMessage = textDelaysPanel.EffectMessageDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("EffectMessageDelayTextBox");
                var damageOverTime = textDelaysPanel.DamageOverTimeDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("DamageOverTimeDelayTextBox");
                var encounter = textDelaysPanel.EncounterDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("EncounterDelayTextBox");
                var rollInfo = textDelaysPanel.RollInfoDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("RollInfoDelayTextBox");
                var environmentalLine = textDelaysPanel.EnvironmentalLineDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("EnvironmentalLineDelayTextBox");
                var baseMenu = textDelaysPanel.BaseMenuDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("BaseMenuDelayTextBox");
                var progressiveRate = textDelaysPanel.ProgressiveReductionRateTextBox ?? textDelaysPanel.FindControl<TextBox>("ProgressiveReductionRateTextBox");
                var progressiveThreshold = textDelaysPanel.ProgressiveThresholdTextBox ?? textDelaysPanel.FindControl<TextBox>("ProgressiveThresholdTextBox");
                var combatBase = textDelaysPanel.CombatPresetBaseDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("CombatPresetBaseDelayTextBox");
                var combatMin = textDelaysPanel.CombatPresetMinDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("CombatPresetMinDelayTextBox");
                var combatMax = textDelaysPanel.CombatPresetMaxDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("CombatPresetMaxDelayTextBox");
                var dungeonBase = textDelaysPanel.DungeonPresetBaseDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("DungeonPresetBaseDelayTextBox");
                var dungeonMin = textDelaysPanel.DungeonPresetMinDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("DungeonPresetMinDelayTextBox");
                var dungeonMax = textDelaysPanel.DungeonPresetMaxDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("DungeonPresetMaxDelayTextBox");
                var roomBase = textDelaysPanel.RoomPresetBaseDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("RoomPresetBaseDelayTextBox");
                var roomMin = textDelaysPanel.RoomPresetMinDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("RoomPresetMinDelayTextBox");
                var roomMax = textDelaysPanel.RoomPresetMaxDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("RoomPresetMaxDelayTextBox");
                var narrativeBase = textDelaysPanel.NarrativePresetBaseDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("NarrativePresetBaseDelayTextBox");
                var narrativeMin = textDelaysPanel.NarrativePresetMinDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("NarrativePresetMinDelayTextBox");
                var narrativeMax = textDelaysPanel.NarrativePresetMaxDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("NarrativePresetMaxDelayTextBox");
                var defaultBase = textDelaysPanel.DefaultPresetBaseDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("DefaultPresetBaseDelayTextBox");
                var defaultMin = textDelaysPanel.DefaultPresetMinDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("DefaultPresetMinDelayTextBox");
                var defaultMax = textDelaysPanel.DefaultPresetMaxDelayTextBox ?? textDelaysPanel.FindControl<TextBox>("DefaultPresetMaxDelayTextBox");

                var controls = BuildControls(
                    enableGui, enableConsole, actionDelayMs, messageDelayMs,
                    combat, system, menu, title, mainTitle, environmental, effectMessage,
                    damageOverTime, encounter, rollInfo, environmentalLine, baseMenu,
                    progressiveRate, progressiveThreshold,
                    combatBase, combatMin, combatMax, dungeonBase, dungeonMin, dungeonMax,
                    roomBase, roomMin, roomMax, narrativeBase, narrativeMin, narrativeMax,
                    defaultBase, defaultMin, defaultMax);
                settingsManager.SaveTextDelaySettings(controls);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsPanel: Error saving text delay settings: {ex.Message}");
            }
        }

        private static TextDelaySettingsControls BuildControls(
            CheckBox? enableGui, CheckBox? enableConsole, TextBox? actionDelayMs, TextBox? messageDelayMs,
            TextBox? combat, TextBox? system, TextBox? menu, TextBox? title, TextBox? mainTitle,
            TextBox? environmental, TextBox? effectMessage, TextBox? damageOverTime, TextBox? encounter,
            TextBox? rollInfo, TextBox? environmentalLine, TextBox? baseMenu, TextBox? progressiveRate, TextBox? progressiveThreshold,
            TextBox? combatBase, TextBox? combatMin, TextBox? combatMax,
            TextBox? dungeonBase, TextBox? dungeonMin, TextBox? dungeonMax,
            TextBox? roomBase, TextBox? roomMin, TextBox? roomMax,
            TextBox? narrativeBase, TextBox? narrativeMin, TextBox? narrativeMax,
            TextBox? defaultBase, TextBox? defaultMin, TextBox? defaultMax)
        {
            return new TextDelaySettingsControls
            {
                EnableGuiDelaysCheckBox = enableGui,
                EnableConsoleDelaysCheckBox = enableConsole,
                ActionDelayMsTextBox = actionDelayMs,
                MessageDelayMsTextBox = messageDelayMs,
                CombatDelayTextBox = combat,
                SystemDelayTextBox = system,
                MenuDelayTextBox = menu,
                TitleDelayTextBox = title,
                MainTitleDelayTextBox = mainTitle,
                EnvironmentalDelayTextBox = environmental,
                EffectMessageDelayTextBox = effectMessage,
                DamageOverTimeDelayTextBox = damageOverTime,
                EncounterDelayTextBox = encounter,
                RollInfoDelayTextBox = rollInfo,
                EnvironmentalLineDelayTextBox = environmentalLine,
                BaseMenuDelayTextBox = baseMenu,
                ProgressiveReductionRateTextBox = progressiveRate,
                ProgressiveThresholdTextBox = progressiveThreshold,
                CombatPresetBaseDelayTextBox = combatBase,
                CombatPresetMinDelayTextBox = combatMin,
                CombatPresetMaxDelayTextBox = combatMax,
                DungeonPresetBaseDelayTextBox = dungeonBase,
                DungeonPresetMinDelayTextBox = dungeonMin,
                DungeonPresetMaxDelayTextBox = dungeonMax,
                RoomPresetBaseDelayTextBox = roomBase,
                RoomPresetMinDelayTextBox = roomMin,
                RoomPresetMaxDelayTextBox = roomMax,
                NarrativePresetBaseDelayTextBox = narrativeBase,
                NarrativePresetMinDelayTextBox = narrativeMin,
                NarrativePresetMaxDelayTextBox = narrativeMax,
                DefaultPresetBaseDelayTextBox = defaultBase,
                DefaultPresetMinDelayTextBox = defaultMin,
                DefaultPresetMaxDelayTextBox = defaultMax
            };
        }
    }
}

