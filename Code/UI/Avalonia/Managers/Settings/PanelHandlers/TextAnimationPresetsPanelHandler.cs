using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Managers.Settings;
using RPGGame.UI.Avalonia.Settings;
using RPGGame.UI.Avalonia.Settings.Helpers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.TextAnimation;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    public class TextAnimationPresetsPanelHandler : ISettingsPanelHandler
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

        private void WirePresetCombo(TextAnimationPresetsSettingsPanel panel)
        {
            var combo = panel.PresetComboBoxControl ?? panel.PresetComboBox;
            if (combo == null)
                return;

            combo.SelectionChanged += (_, _) =>
            {
                if (suppressUiEvents)
                    return;
                LoadPresetControls(panel);
                RequestPreviewRefresh(panel);
            };
        }

        private void WireSampleText(TextAnimationPresetsSettingsPanel panel)
        {
            var sampleTextBox = panel.SampleTextTextBoxControl ?? panel.SampleTextTextBox;
            if (sampleTextBox == null)
                return;

            sampleTextBox.TextChanged += (_, _) =>
            {
                if (suppressUiEvents)
                    return;
                RequestPreviewRefresh(panel);
            };
        }

        private void WirePathIntroControls(TextAnimationPresetsSettingsPanel panel)
        {
            void OnChanged()
            {
                if (suppressUiEvents)
                    return;
                ApplyPathIntroFromUi(panel);
                RequestPreviewRefresh(panel);
            }

            if (panel.GradientStartTextBox != null)
                panel.GradientStartTextBox.LostFocus += (_, _) => OnChanged();
            if (panel.GradientEndTextBox != null)
                panel.GradientEndTextBox.LostFocus += (_, _) => OnChanged();
            if (panel.PhaseDivisorMsTextBox != null)
                panel.PhaseDivisorMsTextBox.LostFocus += (_, _) => OnChanged();
            if (panel.CharacterPhaseOffsetTextBox != null)
                panel.CharacterPhaseOffsetTextBox.LostFocus += (_, _) => OnChanged();
        }

        private void WireHsvControls(TextAnimationPresetsSettingsPanel panel)
        {
            if (panel.HsvAmplitudeSlider != null && panel.HsvAmplitudeTextBox != null)
            {
                WireSliderInteraction(panel, panel.HsvAmplitudeSlider);
                panel.HsvAmplitudeSlider.ValueChanged += (_, e) =>
                {
                    if (suppressUiEvents)
                        return;
                    panel.HsvAmplitudeTextBox.Text = e.NewValue.ToString("F1");
                    ApplyHsvFromUi(panel);
                };
                panel.HsvAmplitudeTextBox.LostFocus += (_, _) =>
                {
                    if (suppressUiEvents)
                        return;
                    if (double.TryParse(panel.HsvAmplitudeTextBox.Text, out double v))
                    {
                        v = Math.Clamp(v, 0.5, 8);
                        panel.HsvAmplitudeSlider.Value = v;
                        panel.HsvAmplitudeTextBox.Text = v.ToString("F1");
                    }
                    ApplyHsvFromUi(panel);
                    RequestPreviewRefresh(panel);
                };
            }

            if (panel.ClampMinTextBox != null)
            {
                panel.ClampMinTextBox.LostFocus += (_, _) =>
                {
                    if (suppressUiEvents) return;
                    ApplyHsvFromUi(panel);
                    RequestPreviewRefresh(panel);
                };
            }
            if (panel.ClampMaxTextBox != null)
            {
                panel.ClampMaxTextBox.LostFocus += (_, _) =>
                {
                    if (suppressUiEvents) return;
                    ApplyHsvFromUi(panel);
                    RequestPreviewRefresh(panel);
                };
            }
        }

        private void RequestPreviewRefresh(TextAnimationPresetsSettingsPanel panel)
        {
            RefreshPreview(panel);
        }

        private void WireSliderInteraction(TextAnimationPresetsSettingsPanel panel, Slider slider)
        {
            slider.PointerPressed += (_, _) => System.Threading.Interlocked.Increment(ref activeSliderInteractions);
            slider.PointerReleased += (_, _) =>
            {
                if (System.Threading.Interlocked.Decrement(ref activeSliderInteractions) < 0)
                    activeSliderInteractions = 0;
                if (activeSliderInteractions == 0)
                    RefreshPreview(panel);
            };
        }

        private void WireGlobalAnimationControls(TextAnimationPresetsSettingsPanel panel)
        {
            void RefreshAfterGlobalChange()
            {
                if (suppressUiEvents)
                    return;
                ApplyGlobalAnimationFromUi(panel);
                ApplyWorkingAnimConfigToRuntime();
                RequestPreviewRefresh(panel);
            }

            if (panel.BrightnessMaskEnabledCheckBox != null)
                panel.BrightnessMaskEnabledCheckBox.IsCheckedChanged += (_, _) => RefreshAfterGlobalChange();

            if (panel.BrightnessMaskIntensitySlider != null && panel.BrightnessMaskIntensityTextBox != null)
            {
                WireSliderInteraction(panel, panel.BrightnessMaskIntensitySlider);
                panel.BrightnessMaskIntensitySlider.ValueChanged += (_, e) =>
                {
                    if (suppressUiEvents) return;
                    panel.BrightnessMaskIntensityTextBox.Text = e.NewValue.ToString("F1");
                    ApplyGlobalAnimationFromUi(panel);
                    ApplyWorkingAnimConfigToRuntime();
                };
            }

            if (panel.BrightnessMaskWaveLengthSlider != null && panel.BrightnessMaskWaveLengthTextBox != null)
            {
                WireSliderInteraction(panel, panel.BrightnessMaskWaveLengthSlider);
                panel.BrightnessMaskWaveLengthSlider.ValueChanged += (_, e) =>
                {
                    if (suppressUiEvents) return;
                    panel.BrightnessMaskWaveLengthTextBox.Text = e.NewValue.ToString("F1");
                    ApplyGlobalAnimationFromUi(panel);
                    ApplyWorkingAnimConfigToRuntime();
                };
            }

            if (panel.BrightnessMaskUpdateIntervalTextBox != null)
                panel.BrightnessMaskUpdateIntervalTextBox.LostFocus += (_, _) => RefreshAfterGlobalChange();

            if (panel.UndulationSpeedSlider != null && panel.UndulationSpeedTextBox != null)
            {
                WireSliderInteraction(panel, panel.UndulationSpeedSlider);
                panel.UndulationSpeedSlider.ValueChanged += (_, e) =>
                {
                    if (suppressUiEvents) return;
                    panel.UndulationSpeedTextBox.Text = e.NewValue.ToString("F3");
                    ApplyGlobalAnimationFromUi(panel);
                    ApplyWorkingAnimConfigToRuntime();
                };
            }

            if (panel.UndulationWaveLengthSlider != null && panel.UndulationWaveLengthTextBox != null)
            {
                WireSliderInteraction(panel, panel.UndulationWaveLengthSlider);
                panel.UndulationWaveLengthSlider.ValueChanged += (_, e) =>
                {
                    if (suppressUiEvents) return;
                    panel.UndulationWaveLengthTextBox.Text = e.NewValue.ToString("F1");
                    ApplyGlobalAnimationFromUi(panel);
                    ApplyWorkingAnimConfigToRuntime();
                };
            }

            if (panel.UndulationIntervalTextBox != null)
                panel.UndulationIntervalTextBox.LostFocus += (_, _) => RefreshAfterGlobalChange();
        }

        private void PopulatePresetCombo(TextAnimationPresetsSettingsPanel panel)
        {
            var combo = panel.PresetComboBoxControl;
            if (combo == null)
                return;

            string? previous = GetSelectedPresetName(panel);
            var keys = new List<string>(workingPresets.Keys);
            keys.Sort(StringComparer.OrdinalIgnoreCase);
            combo.ItemsSource = keys;

            int selectedIndex = 0;
            if (!string.IsNullOrEmpty(previous))
            {
                int idx = keys.FindIndex(k => string.Equals(k, previous, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                    selectedIndex = idx;
            }
            else if (keys.FindIndex(k => string.Equals(k, "pathIntro", StringComparison.OrdinalIgnoreCase)) is int pathIdx and >= 0)
            {
                selectedIndex = pathIdx;
            }

            combo.SelectedIndex = selectedIndex;
            EnsureComboBoxReadable(combo);
        }

        private static void EnsureComboBoxReadable(ComboBox? comboBox)
        {
            if (comboBox == null)
                return;

            comboBox.Foreground = new SolidColorBrush(Color.Parse("#FF1A1A1A"));
            comboBox.Background = new SolidColorBrush(Color.Parse("#FFF0F0F0"));
            comboBox.BorderBrush = new SolidColorBrush(Color.Parse("#FF888888"));
        }

        private void LoadPresetControls(TextAnimationPresetsSettingsPanel panel)
        {
            var preset = GetSelectedPreset(panel);
            if (preset == null)
                return;

            string presetName = GetSelectedPresetName(panel) ?? "pathIntro";
            bool isPathIntro = TextAnimationPresetUiHelper.IsPathIntroPreset(presetName);

            if (panel.PathIntroControls != null)
                panel.PathIntroControls.IsVisible = isPathIntro;
            if (panel.HsvControls != null)
                panel.HsvControls.IsVisible = !isPathIntro;

            var sampleBox = panel.SampleTextTextBoxControl ?? panel.SampleTextTextBox;
            if (sampleBox != null && string.IsNullOrWhiteSpace(sampleBox.Text))
            {
                sampleBox.Text = isPathIntro
                    ? RPGGame.UI.Avalonia.Renderers.Menu.PreWeaponPathIntroRenderer.QuestLine
                    : "Shimmering sample text 123";
            }

            if (panel.LayerSummaryTextBlock != null)
                panel.LayerSummaryTextBlock.Text = TextAnimationPresetUiHelper.DescribeLayers(preset);

            if (isPathIntro)
                LoadPathIntroControls(panel, preset);
            else
                LoadHsvControls(panel, preset);

            LoadAccentHsvControls(panel, preset);
            UpdateAccentLayerFeedback(panel, preset);

            if (panel.PreviewTemplateComboBox != null)
                panel.PreviewTemplateComboBox.IsVisible = !isPathIntro;
        }

        private void PopulatePreviewTemplateCombo(TextAnimationPresetsSettingsPanel panel)
        {
            var combo = panel.PreviewTemplateComboBoxControl;
            if (combo == null)
                return;

            combo.ItemsSource = TextAnimationPresetUiHelper.PreviewTemplateNames;
            if (combo.SelectedIndex < 0)
                combo.SelectedIndex = 0;

            EnsureComboBoxReadable(combo);
        }

        private void WirePreviewTemplateCombo(TextAnimationPresetsSettingsPanel panel)
        {
            if (panel.PreviewTemplateComboBox == null)
                return;

            panel.PreviewTemplateComboBox.SelectionChanged += (_, _) =>
            {
                if (suppressUiEvents)
                    return;
                RequestPreviewRefresh(panel);
            };
        }

        private void LoadPathIntroControls(TextAnimationPresetsSettingsPanel panel, TextAnimationPresetConfig preset)
        {
            var (start, end) = TextAnimationPresetUiHelper.GetGradientColors(preset);
            var (phaseMs, charOffset) = TextAnimationPresetUiHelper.GetSineMask(preset);

            if (panel.GradientStartTextBox != null) panel.GradientStartTextBox.Text = start;
            if (panel.GradientEndTextBox != null) panel.GradientEndTextBox.Text = end;
            if (panel.PhaseDivisorMsTextBox != null) panel.PhaseDivisorMsTextBox.Text = phaseMs.ToString("F0");
            if (panel.CharacterPhaseOffsetTextBox != null) panel.CharacterPhaseOffsetTextBox.Text = charOffset.ToString("F2");

            UpdateColorPreview(panel.GradientStartPreview, start);
            UpdateColorPreview(panel.GradientEndPreview, end);
        }

        private void LoadHsvControls(TextAnimationPresetsSettingsPanel panel, TextAnimationPresetConfig preset)
        {
            var (min, max) = TextAnimationPresetUiHelper.GetClamp(preset);
            double amplitude = TextAnimationPresetUiHelper.GetHsvAmplitude(preset);

            if (panel.HsvAmplitudeSlider != null) panel.HsvAmplitudeSlider.Value = amplitude;
            if (panel.HsvAmplitudeTextBox != null) panel.HsvAmplitudeTextBox.Text = amplitude.ToString("F1");
            if (panel.ClampMinTextBox != null) panel.ClampMinTextBox.Text = ((int)Math.Round(min)).ToString();
            if (panel.ClampMaxTextBox != null) panel.ClampMaxTextBox.Text = ((int)Math.Round(max)).ToString();
        }

        private void LoadGlobalAnimationControls(TextAnimationPresetsSettingsPanel panel)
        {
            if (workingAnimConfig == null)
                return;

            if (panel.BrightnessMaskEnabledCheckBox == null
                || panel.BrightnessMaskIntensitySlider == null
                || panel.BrightnessMaskIntensityTextBox == null
                || panel.BrightnessMaskWaveLengthSlider == null
                || panel.BrightnessMaskWaveLengthTextBox == null
                || panel.BrightnessMaskUpdateIntervalTextBox == null
                || panel.UndulationSpeedSlider == null
                || panel.UndulationSpeedTextBox == null
                || panel.UndulationWaveLengthSlider == null
                || panel.UndulationWaveLengthTextBox == null
                || panel.UndulationIntervalTextBox == null)
                return;

            var anim = workingAnimConfig;
            panel.BrightnessMaskEnabledCheckBox.IsChecked = anim.BrightnessMask.Enabled;
            panel.BrightnessMaskIntensitySlider.Value = anim.BrightnessMask.Intensity;
            panel.BrightnessMaskIntensityTextBox.Text = anim.BrightnessMask.Intensity.ToString("F1");
            panel.BrightnessMaskWaveLengthSlider.Value = anim.BrightnessMask.WaveLength;
            panel.BrightnessMaskWaveLengthTextBox.Text = anim.BrightnessMask.WaveLength.ToString("F1");
            panel.BrightnessMaskUpdateIntervalTextBox.Text = anim.BrightnessMask.UpdateIntervalMs.ToString();
            panel.UndulationSpeedSlider.Value = anim.UndulationSpeed;
            panel.UndulationSpeedTextBox.Text = anim.UndulationSpeed.ToString("F3");
            panel.UndulationWaveLengthSlider.Value = anim.UndulationWaveLength;
            panel.UndulationWaveLengthTextBox.Text = anim.UndulationWaveLength.ToString("F1");
            panel.UndulationIntervalTextBox.Text = anim.UndulationIntervalMs.ToString();
        }

        private void LoadAccentHsvControls(TextAnimationPresetsSettingsPanel panel, TextAnimationPresetConfig preset)
        {
            EnsureAccentControlsWired(panel);

            var (hue, sat, phaseMs, charOffset) = TextAnimationPresetUiHelper.GetAccentHsv(preset);

            if (panel.AccentHueShiftSliderControl != null)
                panel.AccentHueShiftSliderControl.Value = hue;
            if (panel.AccentHueShiftTextBoxControl != null)
                panel.AccentHueShiftTextBoxControl.Text = ((int)Math.Round(hue)).ToString();
            if (panel.AccentSaturationSliderControl != null)
                panel.AccentSaturationSliderControl.Value = sat;
            if (panel.AccentSaturationTextBoxControl != null)
                panel.AccentSaturationTextBoxControl.Text = sat.ToString("F2");
            if (panel.AccentPhaseDivisorMsTextBoxControl != null)
                panel.AccentPhaseDivisorMsTextBoxControl.Text = phaseMs.ToString("F0");
            if (panel.AccentCharacterPhaseOffsetTextBoxControl != null)
                panel.AccentCharacterPhaseOffsetTextBoxControl.Text = charOffset.ToString("F2");

            UpdateAccentSwatches(panel);
        }

        private void EnsureAccentControlsWired(TextAnimationPresetsSettingsPanel panel)
        {
            if (accentControlsWired)
                return;

            if (panel.AccentHueShiftSliderControl == null
                || panel.AccentHueShiftTextBoxControl == null
                || panel.AccentSaturationSliderControl == null
                || panel.AccentSaturationTextBoxControl == null)
                return;

            WireAccentHsvControls(panel);
            accentControlsWired = true;
        }

        private void WireAccentHsvControls(TextAnimationPresetsSettingsPanel panel)
        {
            var hueSlider = panel.AccentHueShiftSliderControl;
            var hueTextBox = panel.AccentHueShiftTextBoxControl;
            var satSlider = panel.AccentSaturationSliderControl;
            var satTextBox = panel.AccentSaturationTextBoxControl;
            var phaseTextBox = panel.AccentPhaseDivisorMsTextBoxControl;
            var charOffsetTextBox = panel.AccentCharacterPhaseOffsetTextBoxControl;

            void CommitAccentAndRefresh()
            {
                if (suppressUiEvents)
                    return;
                ApplyAccentHsvToPreset(panel);
                RequestPreviewRefresh(panel);
            }

            if (hueSlider != null && hueTextBox != null)
            {
                WireSliderInteraction(panel, hueSlider);
                hueSlider.ValueChanged += (_, e) =>
                {
                    if (suppressUiEvents)
                        return;
                    hueTextBox.Text = ((int)Math.Round(e.NewValue)).ToString();
                    ApplyAccentHsvToPreset(panel);
                    RequestPreviewRefresh(panel);
                };
                WireAccentNumericCommit(hueTextBox, () => CommitAccentHueFromTextBox(panel));
            }

            if (satSlider != null && satTextBox != null)
            {
                WireSliderInteraction(panel, satSlider);
                satSlider.ValueChanged += (_, e) =>
                {
                    if (suppressUiEvents)
                        return;
                    satTextBox.Text = e.NewValue.ToString("F2");
                    ApplyAccentHsvToPreset(panel);
                    RequestPreviewRefresh(panel);
                };
                WireAccentNumericCommit(satTextBox, () => CommitAccentSaturationFromTextBox(panel));
            }

            if (phaseTextBox != null)
            {
                WireAccentNumericCommit(phaseTextBox, CommitAccentAndRefresh);
                phaseTextBox.TextChanged += (_, _) => ScheduleAccentFieldApply(panel);
            }

            if (charOffsetTextBox != null)
            {
                WireAccentNumericCommit(charOffsetTextBox, CommitAccentAndRefresh);
                charOffsetTextBox.TextChanged += (_, _) => ScheduleAccentFieldApply(panel);
            }
        }

        private static void WireAccentNumericCommit(TextBox? textBox, System.Action commit)
        {
            if (textBox == null)
                return;

            textBox.LostFocus += (_, _) => commit();
            textBox.KeyDown += (_, e) =>
            {
                if (e.Key != Key.Enter && e.Key != Key.Return)
                    return;
                commit();
                e.Handled = true;
            };
        }

        private void CommitAccentHueFromTextBox(TextAnimationPresetsSettingsPanel panel)
        {
            if (suppressUiEvents)
                return;

            if (!double.TryParse(panel.AccentHueShiftTextBoxControl?.Text, out double hue))
                return;

            hue = Math.Clamp(hue, -180, 180);
            suppressUiEvents = true;
            try
            {
                if (panel.AccentHueShiftSliderControl != null)
                    panel.AccentHueShiftSliderControl.Value = hue;
                if (panel.AccentHueShiftTextBoxControl != null)
                    panel.AccentHueShiftTextBoxControl.Text = ((int)Math.Round(hue)).ToString();
            }
            finally
            {
                suppressUiEvents = false;
            }

            ApplyAccentHsvToPreset(panel);
            RequestPreviewRefresh(panel);
        }

        private void CommitAccentSaturationFromTextBox(TextAnimationPresetsSettingsPanel panel)
        {
            if (suppressUiEvents)
                return;

            if (!double.TryParse(panel.AccentSaturationTextBoxControl?.Text, out double sat))
                return;

            sat = Math.Clamp(sat, 0.5, 2.0);
            suppressUiEvents = true;
            try
            {
                if (panel.AccentSaturationSliderControl != null)
                    panel.AccentSaturationSliderControl.Value = sat;
                if (panel.AccentSaturationTextBoxControl != null)
                    panel.AccentSaturationTextBoxControl.Text = sat.ToString("F2");
            }
            finally
            {
                suppressUiEvents = false;
            }

            ApplyAccentHsvToPreset(panel);
            RequestPreviewRefresh(panel);
        }

        private void ScheduleAccentFieldApply(TextAnimationPresetsSettingsPanel panel)
        {
            if (suppressUiEvents)
                return;

            accentFieldDebounceTimer?.Stop();
            accentFieldDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            accentFieldDebounceTimer.Tick += (_, _) =>
            {
                accentFieldDebounceTimer?.Stop();
                if (suppressUiEvents)
                    return;
                ApplyAccentHsvToPreset(panel);
                RequestPreviewRefresh(panel);
            };
            accentFieldDebounceTimer.Start();
        }

        private void ApplyAccentHsvToPreset(TextAnimationPresetsSettingsPanel panel)
        {
            var preset = GetSelectedPreset(panel);
            if (preset == null)
                return;

            double hue = Math.Clamp(panel.AccentHueShiftSliderControl?.Value ?? 0, -180, 180);
            double sat = Math.Clamp(panel.AccentSaturationSliderControl?.Value ?? 1.0, 0.5, 2.0);
            double phaseMs = ReadAccentPhaseMs(panel);
            double charOffset = ReadAccentCharacterPhaseOffset(panel);

            TextAnimationPresetUiHelper.SetAccentHsv(preset, hue, sat, phaseMs, charOffset);
            UpdateAccentLayerFeedback(panel, preset);
            UpdateAccentSwatches(panel);
        }

        private static double ReadAccentPhaseMs(TextAnimationPresetsSettingsPanel panel)
        {
            if (double.TryParse(panel.AccentPhaseDivisorMsTextBoxControl?.Text, out double phaseMs))
                return Math.Max(1, phaseMs);
            return 500;
        }

        private static double ReadAccentCharacterPhaseOffset(TextAnimationPresetsSettingsPanel panel)
        {
            if (double.TryParse(panel.AccentCharacterPhaseOffsetTextBoxControl?.Text, out double charOffset))
                return charOffset;
            return 0.2;
        }

        private void ApplyUiToWorkingState(TextAnimationPresetsSettingsPanel panel, bool includeAccentControls = true)
        {
            ApplyPathIntroFromUi(panel);
            ApplyHsvFromUi(panel);
            if (includeAccentControls)
                ApplyAccentHsvToPreset(panel);
            ApplyGlobalAnimationFromUi(panel);
        }

        private void ApplyPathIntroFromUi(TextAnimationPresetsSettingsPanel panel)
        {
            var preset = GetSelectedPreset(panel);
            if (preset == null || !TextAnimationPresetUiHelper.IsPathIntroPreset(GetSelectedPresetName(panel) ?? ""))
                return;

            string start = panel.GradientStartTextBox?.Text?.Trim() ?? "#FFF4DC";
            string end = panel.GradientEndTextBox?.Text?.Trim() ?? "#E2F1FF";
            TextAnimationPresetUiHelper.SetGradientColors(preset, start, end);

            var baseLayer = TextAnimationPresetUiHelper.FindBaseColorLayer(preset);
            if (baseLayer?.Source != null)
                baseLayer.Source.Solid = start;

            double phaseMs = 320;
            double charOffset = 0.36;
            if (double.TryParse(panel.PhaseDivisorMsTextBox?.Text, out double p))
                phaseMs = p;
            if (double.TryParse(panel.CharacterPhaseOffsetTextBox?.Text, out double c))
                charOffset = c;
            TextAnimationPresetUiHelper.SetSineMask(preset, phaseMs, charOffset);

            UpdateColorPreview(panel.GradientStartPreview, start);
            UpdateColorPreview(panel.GradientEndPreview, end);
        }

        private void ApplyHsvFromUi(TextAnimationPresetsSettingsPanel panel)
        {
            var preset = GetSelectedPreset(panel);
            if (preset == null || TextAnimationPresetUiHelper.IsPathIntroPreset(GetSelectedPresetName(panel) ?? ""))
                return;

            if (double.TryParse(panel.HsvAmplitudeTextBox?.Text, out double amp))
                TextAnimationPresetUiHelper.SetHsvAmplitude(preset, Math.Clamp(amp, 0.5, 8));

            double min = 0, max = 255;
            if (double.TryParse(panel.ClampMinTextBox?.Text, out double minParsed))
                min = Math.Clamp(minParsed, 0, 255);
            if (double.TryParse(panel.ClampMaxTextBox?.Text, out double maxParsed))
                max = Math.Clamp(maxParsed, 0, 255);
            if (min > max)
                (min, max) = (max, min);
            TextAnimationPresetUiHelper.SetClamp(preset, min, max);
        }

        private void ApplyGlobalAnimationFromUi(TextAnimationPresetsSettingsPanel panel)
        {
            if (workingAnimConfig == null)
                return;

            workingAnimConfig.BrightnessMask.Enabled = panel.BrightnessMaskEnabledCheckBox?.IsChecked ?? false;
            workingAnimConfig.BrightnessMask.Intensity = (float)(panel.BrightnessMaskIntensitySlider?.Value ?? 10);
            workingAnimConfig.BrightnessMask.WaveLength = (float)(panel.BrightnessMaskWaveLengthSlider?.Value ?? 5);
            if (int.TryParse(panel.BrightnessMaskUpdateIntervalTextBox?.Text, out int maskInterval))
                workingAnimConfig.BrightnessMask.UpdateIntervalMs = Math.Max(10, maskInterval);

            workingAnimConfig.UndulationSpeed = panel.UndulationSpeedSlider?.Value ?? -0.05;
            workingAnimConfig.UndulationWaveLength = (float)(panel.UndulationWaveLengthSlider?.Value ?? 4);
            if (int.TryParse(panel.UndulationIntervalTextBox?.Text, out int undInterval))
                workingAnimConfig.UndulationIntervalMs = Math.Max(10, undInterval);
        }

        private void ResetSelectedPresetToDefaults(TextAnimationPresetsSettingsPanel panel)
        {
            string? name = GetSelectedPresetName(panel);
            if (string.IsNullOrEmpty(name) || !TextAnimationPresetLoader.BuiltInDefaults.TryGetValue(name, out var defaults))
                return;

            workingPresets[name] = TextAnimationPresetUiHelper.ClonePreset(defaults);
            suppressUiEvents = true;
            try
            {
                LoadPresetControls(panel);
            }
            finally
            {
                suppressUiEvents = false;
            }
        }

        private void RefreshPreview(TextAnimationPresetsSettingsPanel panel)
        {
            try
            {
                var previewHost = panel.PreviewCharacterHostControl;
                if (previewHost == null)
                    return;

                var preset = GetSelectedPreset(panel);
                if (preset == null)
                    return;

                string presetName = GetSelectedPresetName(panel) ?? "pathIntro";
                string sample = panel.SampleTextTextBoxControl?.Text ?? panel.SampleTextTextBox?.Text ?? "Sample";
                if (string.IsNullOrWhiteSpace(sample))
                    sample = "Sample";

                string? previewTemplate = GetSelectedPreviewTemplateName(panel);
                ApplyUiToWorkingState(panel, includeAccentControls: false);
                ApplyWorkingAnimConfigToRuntime();

                TextAnimationPreviewHelper.RenderPreview(
                    previewHost,
                    preset,
                    presetName,
                    sample,
                    previewTemplate);

                UpdateAccentLayerFeedback(panel, preset);
                UpdateAccentSwatches(panel);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Preview error: {ex.Message}", false);
                System.Diagnostics.Debug.WriteLine($"TextAnimation preview refresh failed: {ex}");
            }
        }

        private void ApplyWorkingAnimConfigToRuntime()
        {
            if (workingAnimConfig == null)
                return;

            DungeonSelectionAnimationState.Instance.ApplyConfig(workingAnimConfig);
            CritAnimationState.Instance.ApplyConfig(workingAnimConfig);
        }

        private void StartPreviewTimer(TextAnimationPresetsSettingsPanel panel)
        {
            StopPreviewTimer();
            previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            previewTimer.Tick += (_, _) => RefreshPreview(panel);
            previewTimer.Start();
        }

        private void StopPreviewTimer()
        {
            if (previewTimer == null)
                return;
            previewTimer.Stop();
            previewTimer = null;
        }

        private void StopAccentFieldDebounceTimer()
        {
            if (accentFieldDebounceTimer == null)
                return;
            accentFieldDebounceTimer.Stop();
            accentFieldDebounceTimer = null;
        }

        private TextAnimationPresetConfig? GetSelectedPreset(TextAnimationPresetsSettingsPanel panel)
        {
            string? name = GetSelectedPresetName(panel);
            if (!string.IsNullOrEmpty(name) && workingPresets.TryGetValue(name, out var preset))
                return preset;

            if (workingPresets.TryGetValue("pathIntro", out preset))
                return preset;

            return workingPresets.Count > 0 ? workingPresets.Values.First() : null;
        }

        private static string? GetSelectedPresetName(TextAnimationPresetsSettingsPanel panel)
        {
            var combo = panel.PresetComboBoxControl ?? panel.PresetComboBox;
            if (combo?.SelectedItem is string selected)
                return selected;
            if (combo?.SelectedIndex is int idx && idx >= 0 && combo.ItemsSource is IList items && idx < items.Count)
                return items[idx]?.ToString();
            return null;
        }

        private static string? GetSelectedPreviewTemplateName(TextAnimationPresetsSettingsPanel panel)
        {
            var combo = panel.PreviewTemplateComboBoxControl ?? panel.PreviewTemplateComboBox;
            if (combo?.SelectedItem is string selected)
                return selected;
            if (combo?.SelectedIndex is int idx && idx >= 0 && combo.ItemsSource is IList items && idx < items.Count)
                return items[idx]?.ToString();
            return TextAnimationPresetUiHelper.PreviewTemplateNames[0];
        }

        private static void UpdateColorPreview(Border? preview, string colorSpec)
        {
            if (preview == null)
                return;
            try
            {
                var color = TextAnimationPresetLoader.ResolveColor(colorSpec);
                preview.Background = new SolidColorBrush(color);
            }
            catch
            {
                preview.Background = Brushes.Transparent;
            }
        }

        private static void UpdateColorPreview(Border? preview, Color color)
        {
            if (preview == null)
                return;
            preview.Background = new SolidColorBrush(color);
        }

        private static void UpdateAccentSwatches(TextAnimationPresetsSettingsPanel panel)
        {
            double hue = Math.Clamp(panel.AccentHueShiftSliderControl?.Value ?? 0, -180, 180);
            double sat = Math.Clamp(panel.AccentSaturationSliderControl?.Value ?? 1.0, 0.5, 2.0);
            var sample = Color.FromRgb(255, 180, 80);
            Color hueOnly = ColorValidator.AdjustAccentHueHsv(sample, hue, maskAlpha: 1.0);
            Color hueAndSat = ColorValidator.ScaleSaturationHsv(hueOnly, sat);
            UpdateColorPreview(panel.AccentHuePreviewControl, hueOnly);
            UpdateColorPreview(panel.AccentSaturationPreviewControl, hueAndSat);
        }

        private static void UpdateAccentLayerFeedback(TextAnimationPresetsSettingsPanel panel, TextAnimationPresetConfig preset)
        {
            var statusBlock = panel.AccentLayerStatusTextBlockControl;
            if (statusBlock == null)
                return;

            statusBlock.IsVisible = true;
            var layer = TextAnimationPresetUiHelper.FindAccentHsvLayer(preset);
            bool accentActive = layer != null;
            if (panel.AccentPhaseDivisorMsTextBoxControl != null)
                panel.AccentPhaseDivisorMsTextBoxControl.IsEnabled = accentActive;
            if (panel.AccentCharacterPhaseOffsetTextBoxControl != null)
                panel.AccentCharacterPhaseOffsetTextBoxControl.IsEnabled = accentActive;

            if (!accentActive)
            {
                statusBlock.Text =
                    "Accent HSV: inactive — hue is 0° and saturation is 1.0×, so no accent layer is applied. Move either slider away from those defaults to enable the accent; phase and char offset only take effect once the accent is active.";
                return;
            }

            double hue = layer!.HsvAdjust?.HueShift ?? 0;
            double sat = layer.HsvAdjust?.SaturationScale ?? 1.0;
            statusBlock.Text =
                $"Accent HSV: active — hue {hue:F0}°, saturation {sat:F2}×. Swatches beside the sliders show the shift; the preview animates it across characters.";
        }

        private static void ReloadCanvasAnimationConfiguration()
        {
            try
            {
                var uiManager = UIManager.GetCustomUIManager();
                if (uiManager is CanvasUICoordinator coordinator &&
                    coordinator.GetAnimationManager() is CanvasAnimationManager canvasAnimManager)
                {
                    canvasAnimManager.ReloadAnimationConfiguration();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReloadCanvasAnimationConfiguration: {ex.Message}");
            }

            DungeonSelectionAnimationState.Instance.ReloadConfiguration();
            CritAnimationState.Instance.ReloadConfiguration();
        }
    }
}
