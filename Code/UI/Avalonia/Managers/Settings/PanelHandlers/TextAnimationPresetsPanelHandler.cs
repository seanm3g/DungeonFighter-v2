using System;
using System.Collections.Generic;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI.Avalonia.Managers.Settings;
using RPGGame.UI.Avalonia.Settings;
using RPGGame.UI.Avalonia.Settings.Helpers;
using RPGGame.UI.TextAnimation;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    public partial class TextAnimationPresetsPanelHandler : ISettingsPanelHandler
    {
        private readonly Action<string, bool>? showStatusMessage;

        private Dictionary<string, TextAnimationPresetConfig> workingPresets = new(StringComparer.OrdinalIgnoreCase);
        private DungeonSelectionAnimationConfig? workingAnimConfig;
        private DispatcherTimer? previewTimer;
        private DispatcherTimer? accentFieldDebounceTimer;
        private bool suppressUiEvents;
        private bool accentControlsWired;
        private int activeSliderInteractions;
        private TextAnimationPresetsSettingsPanel? boundPanel;
        private UserControl? wiredPanel;

        public string PanelType => "TextAnimation";

        public TextAnimationPresetsPanelHandler(Action<string, bool>? showStatusMessage)
        {
            this.showStatusMessage = showStatusMessage;
        }

        public void WireUp(UserControl panel)
        {
            if (panel is not TextAnimationPresetsSettingsPanel textPanel)
                return;

            if (wiredPanel == panel)
                return;

            accentControlsWired = false;
            wiredPanel = panel;
            boundPanel = textPanel;

            textPanel.Unloaded += (_, _) =>
            {
                StopPreviewTimer();
                StopAccentFieldDebounceTimer();
                accentControlsWired = false;
                ReloadCanvasAnimationConfiguration();
            };
            textPanel.Loaded += (_, _) =>
            {
                EnsureAccentControlsWired(textPanel);
                if (previewTimer == null)
                    StartPreviewTimer(textPanel);
                RefreshPreview(textPanel);
            };

            WirePresetCombo(textPanel);
            WireSampleText(textPanel);
            WirePathIntroControls(textPanel);
            WireHsvControls(textPanel);
            EnsureAccentControlsWired(textPanel);
            WireGlobalAnimationControls(textPanel);
            WirePreviewTemplateCombo(textPanel);

            if (textPanel.ResetPresetButton != null)
            {
                textPanel.ResetPresetButton.Click += (_, _) =>
                {
                    ResetSelectedPresetToDefaults(textPanel);
                    RequestPreviewRefresh(textPanel);
                };
            }

            // Initial load once when wired (do not use Loaded — it can refire during focus/layout and reset controls mid-edit).
            Dispatcher.UIThread.Post(() =>
            {
                EnsureAccentControlsWired(textPanel);
                LoadSettings(textPanel);
                StartPreviewTimer(textPanel);
            }, DispatcherPriority.Loaded);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not TextAnimationPresetsSettingsPanel textPanel)
                return;

            suppressUiEvents = true;
            try
            {
                var uiConfig = UIConfiguration.LoadFromFile();
                workingPresets = TextAnimationPresetUiHelper.ClonePresets(
                    uiConfig.TextAnimationPresets?.Count > 0
                        ? uiConfig.TextAnimationPresets
                        : TextAnimationPresetLoader.BuiltInDefaults);

                string json = JsonSerializer.Serialize(uiConfig.DungeonSelectionAnimation);
                workingAnimConfig = JsonSerializer.Deserialize<DungeonSelectionAnimationConfig>(json)
                    ?? new DungeonSelectionAnimationConfig();

                PopulatePresetCombo(textPanel);
                PopulatePreviewTemplateCombo(textPanel);
                LoadGlobalAnimationControls(textPanel);
                LoadPresetControls(textPanel);
                RefreshPreview(textPanel);
            }
            finally
            {
                suppressUiEvents = false;
            }
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not TextAnimationPresetsSettingsPanel textPanel)
                return;

            ApplyUiToWorkingState(textPanel);

            try
            {
                var uiConfig = UIConfiguration.LoadFromFile();
                uiConfig.TextAnimationPresets = TextAnimationPresetUiHelper.ClonePresets(workingPresets);

                if (workingAnimConfig != null)
                    uiConfig.DungeonSelectionAnimation = workingAnimConfig;

                string? foundPath = JsonLoader.FindGameDataFile("UIConfiguration.json");
                if (foundPath != null)
                {
                    var jsonOptions = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    string json = JsonSerializer.Serialize(uiConfig, jsonOptions);
                    System.IO.File.WriteAllText(foundPath, json);
                }

                TextAnimationPresetLoader.Reload();
                ReloadCanvasAnimationConfiguration();
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving text animation settings: {ex.Message}", false);
                throw;
            }
        }
    }
}
