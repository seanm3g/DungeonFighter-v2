using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI.Avalonia.Settings;
using RPGGame.UI.Avalonia.Settings.Helpers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.TextAnimation;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    public partial class TextAnimationPresetsPanelHandler
    {
        private const double AccentSaturationMin = 0.0;
        private const double AccentSaturationMax = 1.5;

        private void LoadAccentHsvControls(TextAnimationPresetsSettingsPanel panel, TextAnimationPresetConfig preset)
        {
            EnsureAccentControlsWired(panel);

            var (hue, sat, phaseMs, charOffset) = TextAnimationPresetUiHelper.GetAccentHsv(preset);

            if (panel.AccentHueShiftSliderControl != null)
                panel.AccentHueShiftSliderControl.Value = hue;
            if (panel.AccentHueShiftTextBoxControl != null)
                panel.AccentHueShiftTextBoxControl.Text = ((int)Math.Round(hue)).ToString();
            sat = Math.Clamp(sat, AccentSaturationMin, AccentSaturationMax);
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

            sat = Math.Clamp(sat, AccentSaturationMin, AccentSaturationMax);
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
            double sat = Math.Clamp(panel.AccentSaturationSliderControl?.Value ?? 1.0, AccentSaturationMin, AccentSaturationMax);
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

        private static void UpdateColorPreview(Border? preview, Color color)
        {
            if (preview == null)
                return;
            preview.Background = new SolidColorBrush(color);
        }

        private static void UpdateAccentSwatches(TextAnimationPresetsSettingsPanel panel)
        {
            double hue = Math.Clamp(panel.AccentHueShiftSliderControl?.Value ?? 0, -180, 180);
            double sat = Math.Clamp(panel.AccentSaturationSliderControl?.Value ?? 1.0, AccentSaturationMin, AccentSaturationMax);
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
    }
}
