using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.Avalonia;
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

            WireWindSwayControls(panel, RefreshAfterGlobalChange);
            WireClickBurstControls(panel, RefreshAfterGlobalChange);
        }

        private void WireWindSwayControls(TextAnimationPresetsSettingsPanel panel, System.Action refreshAfterGlobalChange)
        {
            var enabled = panel.WindSwayEnabledCheckBoxControl ?? panel.WindSwayEnabledCheckBox;
            if (enabled != null)
                enabled.IsCheckedChanged += (_, _) => refreshAfterGlobalChange();

            var chromaticEnabled = panel.WindSwayChromaticEnabledCheckBoxControl;
            if (chromaticEnabled != null)
                chromaticEnabled.IsCheckedChanged += (_, _) => refreshAfterGlobalChange();

            void WireInfluenceSlider(Slider? slider, TextBox? textBox, string format)
            {
                if (slider == null || textBox == null)
                    return;
                WireSliderInteraction(panel, slider);
                slider.ValueChanged += (_, e) =>
                {
                    if (suppressUiEvents) return;
                    textBox.Text = e.NewValue.ToString(format);
                    ApplyGlobalAnimationFromUi(panel);
                    ApplyWorkingAnimConfigToRuntime();
                };
            }

            WireInfluenceSlider(
                panel.WindSwayRadiusSliderControl ?? panel.WindSwayRadiusSlider,
                panel.WindSwayRadiusTextBoxControl ?? panel.WindSwayRadiusTextBox,
                "F0");
            WireInfluenceSlider(
                panel.WindSwayNearInfluenceSliderControl ?? panel.WindSwayNearInfluenceSlider,
                panel.WindSwayNearInfluenceTextBoxControl ?? panel.WindSwayNearInfluenceTextBox,
                "F2");
            WireInfluenceSlider(
                panel.WindSwayFarInfluenceSliderControl ?? panel.WindSwayFarInfluenceSlider,
                panel.WindSwayFarInfluenceTextBoxControl ?? panel.WindSwayFarInfluenceTextBox,
                "F2");
            WireInfluenceSlider(
                panel.WindSwayRearBiasSliderControl ?? panel.WindSwayRearBiasSlider,
                panel.WindSwayRearBiasTextBoxControl ?? panel.WindSwayRearBiasTextBox,
                "F2");
            WireInfluenceSlider(
                panel.WindSwayChromaticSpreadSliderControl,
                panel.WindSwayChromaticSpreadTextBoxControl,
                "F2");
            WireInfluenceSlider(
                panel.WindSwayChromaticOpacitySliderControl,
                panel.WindSwayChromaticOpacityTextBoxControl,
                "F2");

            void WireLostFocus(TextBox? box, Slider? slider, double min, double max, string format)
            {
                if (box == null)
                    return;
                box.LostFocus += (_, _) =>
                {
                    if (suppressUiEvents)
                        return;
                    if (slider != null && double.TryParse(box.Text, out double parsed))
                    {
                        double clamped = Math.Clamp(parsed, min, max);
                        box.Text = clamped.ToString(format);
                        suppressUiEvents = true;
                        try { slider.Value = clamped; }
                        finally { suppressUiEvents = false; }
                    }
                    refreshAfterGlobalChange();
                };
            }

            WireLostFocus(
                panel.WindSwayRadiusTextBoxControl ?? panel.WindSwayRadiusTextBox,
                panel.WindSwayRadiusSliderControl ?? panel.WindSwayRadiusSlider,
                8, 80, "F0");
            WireLostFocus(
                panel.WindSwayNearInfluenceTextBoxControl ?? panel.WindSwayNearInfluenceTextBox,
                panel.WindSwayNearInfluenceSliderControl ?? panel.WindSwayNearInfluenceSlider,
                0, 1.5, "F2");
            WireLostFocus(
                panel.WindSwayFarInfluenceTextBoxControl ?? panel.WindSwayFarInfluenceTextBox,
                panel.WindSwayFarInfluenceSliderControl ?? panel.WindSwayFarInfluenceSlider,
                0, 1, "F2");
            WireLostFocus(
                panel.WindSwayRearBiasTextBoxControl ?? panel.WindSwayRearBiasTextBox,
                panel.WindSwayRearBiasSliderControl ?? panel.WindSwayRearBiasSlider,
                0, 1, "F2");
            WireLostFocus(
                panel.WindSwayChromaticSpreadTextBoxControl,
                panel.WindSwayChromaticSpreadSliderControl,
                0, 0.4, "F2");
            WireLostFocus(
                panel.WindSwayChromaticOpacityTextBoxControl,
                panel.WindSwayChromaticOpacitySliderControl,
                0, 1, "F2");

            var debugButton = panel.WindSwayDebugRadiusButtonControl ?? panel.WindSwayDebugRadiusButton;
            if (debugButton != null)
            {
                debugButton.Click += (_, _) => ToggleWakeRadiusDebugOverlay(panel);
                UpdateWakeDebugButtonCaption(panel);
            }
        }

        private void WireClickBurstControls(TextAnimationPresetsSettingsPanel panel, System.Action refreshAfterGlobalChange)
        {
            var enabled = panel.ClickBurstEnabledCheckBoxControl;
            if (enabled != null)
                enabled.IsCheckedChanged += (_, _) => refreshAfterGlobalChange();

            void WireBurstSlider(Slider? slider, TextBox? textBox, string format)
            {
                if (slider == null || textBox == null)
                    return;
                WireSliderInteraction(panel, slider);
                slider.ValueChanged += (_, e) =>
                {
                    if (suppressUiEvents) return;
                    textBox.Text = e.NewValue.ToString(format);
                    ApplyGlobalAnimationFromUi(panel);
                    ApplyWorkingAnimConfigToRuntime();
                };
            }

            void WireBurstLostFocus(TextBox? box, Slider? slider, double min, double max, string format)
            {
                if (box == null)
                    return;
                box.LostFocus += (_, _) =>
                {
                    if (suppressUiEvents)
                        return;
                    if (slider != null && double.TryParse(box.Text, out double parsed))
                    {
                        double clamped = Math.Clamp(parsed, min, max);
                        box.Text = clamped.ToString(format);
                        suppressUiEvents = true;
                        try { slider.Value = clamped; }
                        finally { suppressUiEvents = false; }
                    }
                    refreshAfterGlobalChange();
                };
            }

            WireBurstSlider(panel.ClickBurstRadiusSliderControl, panel.ClickBurstRadiusTextBoxControl, "F0");
            WireBurstSlider(panel.ClickBurstExplodeStrengthSliderControl, panel.ClickBurstExplodeStrengthTextBoxControl, "F0");
            WireBurstSlider(panel.ClickBurstExplodeOutSliderControl, panel.ClickBurstExplodeOutTextBoxControl, "F2");
            WireBurstSlider(panel.ClickBurstClicksToExplodeSliderControl, panel.ClickBurstClicksToExplodeTextBoxControl, "F0");
            WireBurstSlider(panel.ClickBurstDistanceVarianceMinSliderControl, panel.ClickBurstDistanceVarianceMinTextBoxControl, "F2");
            WireBurstSlider(panel.ClickBurstDistanceVarianceMaxSliderControl, panel.ClickBurstDistanceVarianceMaxTextBoxControl, "F2");
            WireBurstSlider(panel.ClickBurstMaxRotationSliderControl, panel.ClickBurstMaxRotationTextBoxControl, "F1");
            WireBurstSlider(panel.ClickBurstVerticalScaleSliderControl, panel.ClickBurstVerticalScaleTextBoxControl, "F2");
            WireBurstSlider(panel.ClickBurstChromaticVelocitySliderControl, panel.ClickBurstChromaticVelocityTextBoxControl, "F2");
            WireBurstSlider(panel.ClickBurstChargeDecaySliderControl, panel.ClickBurstChargeDecayTextBoxControl, "F2");
            WireBurstSlider(panel.ClickBurstImpulseDecaySliderControl, panel.ClickBurstImpulseDecayTextBoxControl, "F1");
            WireBurstSlider(panel.ClickBurstAutoRebuildIdleSliderControl, panel.ClickBurstAutoRebuildIdleTextBoxControl, "F1");
            WireBurstSlider(panel.ClickBurstAutoRebuildIntervalSliderControl, panel.ClickBurstAutoRebuildIntervalTextBoxControl, "F2");

            WireBurstLostFocus(panel.ClickBurstRadiusTextBoxControl, panel.ClickBurstRadiusSliderControl, 2, 40, "F0");
            WireBurstLostFocus(panel.ClickBurstExplodeStrengthTextBoxControl, panel.ClickBurstExplodeStrengthSliderControl, 1, 60, "F0");
            WireBurstLostFocus(panel.ClickBurstExplodeOutTextBoxControl, panel.ClickBurstExplodeOutSliderControl, 0.05, 1.5, "F2");
            WireBurstLostFocus(panel.ClickBurstClicksToExplodeTextBoxControl, panel.ClickBurstClicksToExplodeSliderControl, 1, 20, "F0");
            WireBurstLostFocus(panel.ClickBurstDistanceVarianceMinTextBoxControl, panel.ClickBurstDistanceVarianceMinSliderControl, 0, 4, "F2");
            WireBurstLostFocus(panel.ClickBurstDistanceVarianceMaxTextBoxControl, panel.ClickBurstDistanceVarianceMaxSliderControl, 0, 4, "F2");
            WireBurstLostFocus(panel.ClickBurstMaxRotationTextBoxControl, panel.ClickBurstMaxRotationSliderControl, 0, 12.5, "F1");
            WireBurstLostFocus(panel.ClickBurstVerticalScaleTextBoxControl, panel.ClickBurstVerticalScaleSliderControl, 0.1, 2, "F2");
            WireBurstLostFocus(panel.ClickBurstChromaticVelocityTextBoxControl, panel.ClickBurstChromaticVelocitySliderControl, 0, 0.5, "F2");
            WireBurstLostFocus(panel.ClickBurstChargeDecayTextBoxControl, panel.ClickBurstChargeDecaySliderControl, 0, 2, "F2");
            WireBurstLostFocus(panel.ClickBurstImpulseDecayTextBoxControl, panel.ClickBurstImpulseDecaySliderControl, 0, 10, "F1");
            WireBurstLostFocus(panel.ClickBurstAutoRebuildIdleTextBoxControl, panel.ClickBurstAutoRebuildIdleSliderControl, 0, 60, "F1");
            WireBurstLostFocus(panel.ClickBurstAutoRebuildIntervalTextBoxControl, panel.ClickBurstAutoRebuildIntervalSliderControl, 0.05, 2, "F2");
        }

        private void ToggleWakeRadiusDebugOverlay(TextAnimationPresetsSettingsPanel panel)
        {
            ApplyGlobalAnimationFromUi(panel);
            ApplyWorkingAnimConfigToRuntime();

            var canvas = TryGetMainGameCanvas();
            if (canvas == null)
            {
                showStatusMessage?.Invoke("Main canvas not available for wake debug.", false);
                return;
            }

            bool next = !canvas.WindSway.ShowWakeRadiusDebug;
            canvas.WindSway.ShowWakeRadiusDebug = next;
            canvas.Refresh();
            UpdateWakeDebugButtonCaption(panel);
            showStatusMessage?.Invoke(
                next ? "Wake radius circle ON — move mouse over the game canvas." : "Wake radius circle OFF.",
                true);
        }

        private static void UpdateWakeDebugButtonCaption(TextAnimationPresetsSettingsPanel panel)
        {
            var debugButton = panel.WindSwayDebugRadiusButtonControl ?? panel.WindSwayDebugRadiusButton;
            if (debugButton == null)
                return;

            bool on = TryGetMainGameCanvas()?.WindSway.ShowWakeRadiusDebug == true;
            debugButton.Content = on ? "Hide wake radius on canvas" : "Show wake radius on canvas";
        }

        private static GameCanvasControl? TryGetMainGameCanvas()
        {
            try
            {
                if (UIManager.GetCustomUIManager() is CanvasUICoordinator coordinator)
                    return coordinator.GetMainWindow()?.GameCanvas;
            }
            catch
            {
                // ignore
            }

            return null;
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

            var anim = workingAnimConfig;

            if (panel.BrightnessMaskEnabledCheckBox != null
                && panel.BrightnessMaskIntensitySlider != null
                && panel.BrightnessMaskIntensityTextBox != null
                && panel.BrightnessMaskWaveLengthSlider != null
                && panel.BrightnessMaskWaveLengthTextBox != null
                && panel.BrightnessMaskUpdateIntervalTextBox != null
                && panel.UndulationSpeedSlider != null
                && panel.UndulationSpeedTextBox != null
                && panel.UndulationWaveLengthSlider != null
                && panel.UndulationWaveLengthTextBox != null
                && panel.UndulationIntervalTextBox != null)
            {
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

            LoadWindSwayControls(panel, anim);
            LoadClickBurstControls(panel, anim);
            UpdateWakeDebugButtonCaption(panel);
        }

        private static void LoadWindSwayControls(TextAnimationPresetsSettingsPanel panel, DungeonSelectionAnimationConfig anim)
        {
            var wind = anim.WindSway ?? new RPGGame.UI.Avalonia.Effects.WindSwayConfig();

            var enabled = panel.WindSwayEnabledCheckBoxControl ?? panel.WindSwayEnabledCheckBox;
            if (enabled != null)
                enabled.IsChecked = wind.Enabled;

            var chromaticEnabled = panel.WindSwayChromaticEnabledCheckBoxControl;
            if (chromaticEnabled != null)
                chromaticEnabled.IsChecked = wind.ChromaticAberrationEnabled;

            void SetSlider(Slider? slider, TextBox? textBox, double value, string format)
            {
                if (slider != null)
                    slider.Value = value;
                if (textBox != null)
                    textBox.Text = value.ToString(format);
            }

            SetSlider(
                panel.WindSwayRadiusSliderControl ?? panel.WindSwayRadiusSlider,
                panel.WindSwayRadiusTextBoxControl ?? panel.WindSwayRadiusTextBox,
                wind.WakeRadiusCells, "F0");
            SetSlider(
                panel.WindSwayNearInfluenceSliderControl ?? panel.WindSwayNearInfluenceSlider,
                panel.WindSwayNearInfluenceTextBoxControl ?? panel.WindSwayNearInfluenceTextBox,
                wind.NearInfluence, "F2");
            SetSlider(
                panel.WindSwayFarInfluenceSliderControl ?? panel.WindSwayFarInfluenceSlider,
                panel.WindSwayFarInfluenceTextBoxControl ?? panel.WindSwayFarInfluenceTextBox,
                wind.FarInfluence, "F2");
            SetSlider(
                panel.WindSwayRearBiasSliderControl ?? panel.WindSwayRearBiasSlider,
                panel.WindSwayRearBiasTextBoxControl ?? panel.WindSwayRearBiasTextBox,
                wind.WakeRearBias, "F2");
            SetSlider(
                panel.WindSwayChromaticSpreadSliderControl,
                panel.WindSwayChromaticSpreadTextBoxControl,
                wind.ChromaticSpreadFraction, "F2");
            SetSlider(
                panel.WindSwayChromaticOpacitySliderControl,
                panel.WindSwayChromaticOpacityTextBoxControl,
                wind.ChromaticOpacity, "F2");
        }

        private static void LoadClickBurstControls(TextAnimationPresetsSettingsPanel panel, DungeonSelectionAnimationConfig anim)
        {
            var burst = anim.WindSway?.ClickBurst
                ?? new RPGGame.UI.Avalonia.Effects.TextClickBurstConfig();

            var enabled = panel.ClickBurstEnabledCheckBoxControl;
            if (enabled != null)
                enabled.IsChecked = burst.Enabled;

            void SetSlider(Slider? slider, TextBox? textBox, double value, string format)
            {
                if (slider != null)
                    slider.Value = value;
                if (textBox != null)
                    textBox.Text = value.ToString(format);
            }

            SetSlider(panel.ClickBurstRadiusSliderControl, panel.ClickBurstRadiusTextBoxControl,
                burst.ImpulseRadiusCells, "F0");
            SetSlider(panel.ClickBurstExplodeStrengthSliderControl, panel.ClickBurstExplodeStrengthTextBoxControl,
                burst.ExplodeMaxOffsetFraction, "F0");
            SetSlider(panel.ClickBurstExplodeOutSliderControl, panel.ClickBurstExplodeOutTextBoxControl,
                burst.ExplodeOutSeconds, "F2");
            SetSlider(panel.ClickBurstClicksToExplodeSliderControl, panel.ClickBurstClicksToExplodeTextBoxControl,
                burst.ClicksToExplode, "F0");
            SetSlider(panel.ClickBurstDistanceVarianceMinSliderControl, panel.ClickBurstDistanceVarianceMinTextBoxControl,
                burst.DistanceVarianceMin, "F2");
            SetSlider(panel.ClickBurstDistanceVarianceMaxSliderControl, panel.ClickBurstDistanceVarianceMaxTextBoxControl,
                burst.DistanceVarianceMax, "F2");
            SetSlider(panel.ClickBurstMaxRotationSliderControl, panel.ClickBurstMaxRotationTextBoxControl,
                burst.MaxRotationRadians, "F1");
            SetSlider(panel.ClickBurstVerticalScaleSliderControl, panel.ClickBurstVerticalScaleTextBoxControl,
                burst.VerticalScale, "F2");
            SetSlider(panel.ClickBurstChromaticVelocitySliderControl, panel.ClickBurstChromaticVelocityTextBoxControl,
                burst.ChromaticVelocitySeconds, "F2");
            SetSlider(panel.ClickBurstChargeDecaySliderControl, panel.ClickBurstChargeDecayTextBoxControl,
                burst.ChargeDecayPerSecond, "F2");
            SetSlider(panel.ClickBurstImpulseDecaySliderControl, panel.ClickBurstImpulseDecayTextBoxControl,
                burst.ImpulseDecayPerSecond, "F1");
            SetSlider(panel.ClickBurstAutoRebuildIdleSliderControl, panel.ClickBurstAutoRebuildIdleTextBoxControl,
                burst.AutoRebuildIdleSeconds, "F1");
            SetSlider(panel.ClickBurstAutoRebuildIntervalSliderControl, panel.ClickBurstAutoRebuildIntervalTextBoxControl,
                burst.AutoRebuildIntervalSeconds, "F2");
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

            ApplyWindSwayFromUi(panel);
            ApplyClickBurstFromUi(panel);
        }

        private void ApplyWindSwayFromUi(TextAnimationPresetsSettingsPanel panel)
        {
            if (workingAnimConfig == null)
                return;

            workingAnimConfig.WindSway ??= new RPGGame.UI.Avalonia.Effects.WindSwayConfig();
            var wind = workingAnimConfig.WindSway;

            var enabled = panel.WindSwayEnabledCheckBoxControl ?? panel.WindSwayEnabledCheckBox;
            wind.Enabled = enabled?.IsChecked ?? true;

            var chromaticEnabled = panel.WindSwayChromaticEnabledCheckBoxControl;
            wind.ChromaticAberrationEnabled = chromaticEnabled?.IsChecked ?? true;

            var radiusSlider = panel.WindSwayRadiusSliderControl ?? panel.WindSwayRadiusSlider;
            var radiusText = panel.WindSwayRadiusTextBoxControl ?? panel.WindSwayRadiusTextBox;
            if (double.TryParse(radiusText?.Text, out double radiusFromText))
                wind.WakeRadiusCells = Math.Clamp(radiusFromText, 8, 80);
            else if (radiusSlider != null)
                wind.WakeRadiusCells = Math.Clamp(radiusSlider.Value, 8, 80);

            var nearSlider = panel.WindSwayNearInfluenceSliderControl ?? panel.WindSwayNearInfluenceSlider;
            var nearText = panel.WindSwayNearInfluenceTextBoxControl ?? panel.WindSwayNearInfluenceTextBox;
            if (double.TryParse(nearText?.Text, out double nearFromText))
                wind.NearInfluence = Math.Clamp(nearFromText, 0, 1.5);
            else if (nearSlider != null)
                wind.NearInfluence = Math.Clamp(nearSlider.Value, 0, 1.5);

            var farSlider = panel.WindSwayFarInfluenceSliderControl ?? panel.WindSwayFarInfluenceSlider;
            var farText = panel.WindSwayFarInfluenceTextBoxControl ?? panel.WindSwayFarInfluenceTextBox;
            if (double.TryParse(farText?.Text, out double farFromText))
                wind.FarInfluence = Math.Clamp(farFromText, 0, 1);
            else if (farSlider != null)
                wind.FarInfluence = Math.Clamp(farSlider.Value, 0, 1);

            var rearSlider = panel.WindSwayRearBiasSliderControl ?? panel.WindSwayRearBiasSlider;
            var rearText = panel.WindSwayRearBiasTextBoxControl ?? panel.WindSwayRearBiasTextBox;
            if (double.TryParse(rearText?.Text, out double rearFromText))
                wind.WakeRearBias = Math.Clamp(rearFromText, 0, 1);
            else if (rearSlider != null)
                wind.WakeRearBias = Math.Clamp(rearSlider.Value, 0, 1);

            var caSpreadSlider = panel.WindSwayChromaticSpreadSliderControl;
            var caSpreadText = panel.WindSwayChromaticSpreadTextBoxControl;
            if (double.TryParse(caSpreadText?.Text, out double caSpreadFromText))
                wind.ChromaticSpreadFraction = Math.Clamp(caSpreadFromText, 0, 0.4);
            else if (caSpreadSlider != null)
                wind.ChromaticSpreadFraction = Math.Clamp(caSpreadSlider.Value, 0, 0.4);

            var caOpacitySlider = panel.WindSwayChromaticOpacitySliderControl;
            var caOpacityText = panel.WindSwayChromaticOpacityTextBoxControl;
            if (double.TryParse(caOpacityText?.Text, out double caOpacityFromText))
                wind.ChromaticOpacity = Math.Clamp(caOpacityFromText, 0, 1);
            else if (caOpacitySlider != null)
                wind.ChromaticOpacity = Math.Clamp(caOpacitySlider.Value, 0, 1);
        }

        private void ApplyClickBurstFromUi(TextAnimationPresetsSettingsPanel panel)
        {
            if (workingAnimConfig == null)
                return;

            workingAnimConfig.WindSway ??= new RPGGame.UI.Avalonia.Effects.WindSwayConfig();
            workingAnimConfig.WindSway.ClickBurst ??= new RPGGame.UI.Avalonia.Effects.TextClickBurstConfig();
            var burst = workingAnimConfig.WindSway.ClickBurst;

            burst.Enabled = panel.ClickBurstEnabledCheckBoxControl?.IsChecked ?? true;

            var radiusSlider = panel.ClickBurstRadiusSliderControl;
            var radiusText = panel.ClickBurstRadiusTextBoxControl;
            if (double.TryParse(radiusText?.Text, out double radiusFromText))
                burst.ImpulseRadiusCells = Math.Clamp(radiusFromText, 2, 40);
            else if (radiusSlider != null)
                burst.ImpulseRadiusCells = Math.Clamp(radiusSlider.Value, 2, 40);

            var strengthSlider = panel.ClickBurstExplodeStrengthSliderControl;
            var strengthText = panel.ClickBurstExplodeStrengthTextBoxControl;
            if (double.TryParse(strengthText?.Text, out double strengthFromText))
                burst.ExplodeMaxOffsetFraction = Math.Clamp(strengthFromText, 1, 60);
            else if (strengthSlider != null)
                burst.ExplodeMaxOffsetFraction = Math.Clamp(strengthSlider.Value, 1, 60);

            var outSlider = panel.ClickBurstExplodeOutSliderControl;
            var outText = panel.ClickBurstExplodeOutTextBoxControl;
            if (double.TryParse(outText?.Text, out double outFromText))
                burst.ExplodeOutSeconds = Math.Clamp(outFromText, 0.05, 1.5);
            else if (outSlider != null)
                burst.ExplodeOutSeconds = Math.Clamp(outSlider.Value, 0.05, 1.5);

            var clicksSlider = panel.ClickBurstClicksToExplodeSliderControl;
            var clicksText = panel.ClickBurstClicksToExplodeTextBoxControl;
            if (double.TryParse(clicksText?.Text, out double clicksFromText))
                burst.ClicksToExplode = (int)Math.Clamp(Math.Round(clicksFromText), 1, 20);
            else if (clicksSlider != null)
                burst.ClicksToExplode = (int)Math.Clamp(Math.Round(clicksSlider.Value), 1, 20);

            ApplyBurstDouble(panel.ClickBurstDistanceVarianceMinSliderControl,
                panel.ClickBurstDistanceVarianceMinTextBoxControl, 0, 4, v => burst.DistanceVarianceMin = v);
            ApplyBurstDouble(panel.ClickBurstDistanceVarianceMaxSliderControl,
                panel.ClickBurstDistanceVarianceMaxTextBoxControl, 0, 4, v => burst.DistanceVarianceMax = v);
            if (burst.DistanceVarianceMax < burst.DistanceVarianceMin)
                burst.DistanceVarianceMax = burst.DistanceVarianceMin;

            ApplyBurstDouble(panel.ClickBurstMaxRotationSliderControl,
                panel.ClickBurstMaxRotationTextBoxControl, 0, 12.5, v => burst.MaxRotationRadians = v);
            ApplyBurstDouble(panel.ClickBurstVerticalScaleSliderControl,
                panel.ClickBurstVerticalScaleTextBoxControl, 0.1, 2, v => burst.VerticalScale = v);
            ApplyBurstDouble(panel.ClickBurstChromaticVelocitySliderControl,
                panel.ClickBurstChromaticVelocityTextBoxControl, 0, 0.5, v => burst.ChromaticVelocitySeconds = v);
            ApplyBurstDouble(panel.ClickBurstChargeDecaySliderControl,
                panel.ClickBurstChargeDecayTextBoxControl, 0, 2, v => burst.ChargeDecayPerSecond = v);
            ApplyBurstDouble(panel.ClickBurstImpulseDecaySliderControl,
                panel.ClickBurstImpulseDecayTextBoxControl, 0, 10, v => burst.ImpulseDecayPerSecond = v);
            ApplyBurstDouble(panel.ClickBurstAutoRebuildIdleSliderControl,
                panel.ClickBurstAutoRebuildIdleTextBoxControl, 0, 60, v => burst.AutoRebuildIdleSeconds = v);
            ApplyBurstDouble(panel.ClickBurstAutoRebuildIntervalSliderControl,
                panel.ClickBurstAutoRebuildIntervalTextBoxControl, 0.05, 2, v => burst.AutoRebuildIntervalSeconds = v);
        }

        private static void ApplyBurstDouble(
            Slider? slider,
            TextBox? textBox,
            double min,
            double max,
            Action<double> assign)
        {
            if (double.TryParse(textBox?.Text, out double fromText))
                assign(Math.Clamp(fromText, min, max));
            else if (slider != null)
                assign(Math.Clamp(slider.Value, min, max));
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
