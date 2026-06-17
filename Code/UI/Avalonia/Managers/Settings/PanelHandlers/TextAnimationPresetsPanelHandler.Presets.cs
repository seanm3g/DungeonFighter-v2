using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.Avalonia.Resources;
using RPGGame.UI.Avalonia.Settings;
using RPGGame.UI.Avalonia.Settings.Helpers;
using RPGGame.UI.TextAnimation;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    public partial class TextAnimationPresetsPanelHandler
    {
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

            SettingsInputApplier.ApplyComboBox(comboBox);
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
    }
}
