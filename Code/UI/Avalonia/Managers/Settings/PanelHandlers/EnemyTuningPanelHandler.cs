using System;
using Avalonia.Controls;
using RPGGame;
using RPGGame.UI.Avalonia.Components;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>
    /// Loads/saves <see cref="EnemySystemConfig.ProgressionScales"/> from the Enemy tuning settings tab.
    /// </summary>
    public sealed class EnemyTuningPanelHandler : ISettingsPanelHandler
    {
        private readonly Action<string, bool>? showStatusMessage;

        public EnemyTuningPanelHandler(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
        }

        public string PanelType => "EnemyTuning";

        public void WireUp(UserControl panel)
        {
            if (panel is not EnemyTuningSettingsPanel p) return;

            void hook(SliderWithTextBox? c)
            {
                if (c?.Slider != null)
                    c.Slider.ValueChanged += (_, _) => PushFromPanelToConfig(p);
            }

            hook(p.EnemyBaseHealthScaleControl);
            hook(p.EnemyHealthGrowthScaleControl);
            hook(p.EnemyAttributeGrowthScaleControl);

            if (p.EnemyBaseHealthScaleControl?.TextBox != null)
                p.EnemyBaseHealthScaleControl.TextBox.LostFocus += (_, _) => PushFromPanelToConfig(p);
            if (p.EnemyHealthGrowthScaleControl?.TextBox != null)
                p.EnemyHealthGrowthScaleControl.TextBox.LostFocus += (_, _) => PushFromPanelToConfig(p);
            if (p.EnemyAttributeGrowthScaleControl?.TextBox != null)
                p.EnemyAttributeGrowthScaleControl.TextBox.LostFocus += (_, _) => PushFromPanelToConfig(p);

            LoadSettings(p);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not EnemyTuningSettingsPanel p) return;

            GameConfiguration.Instance.EnemySystem?.EnsureSanitizedDefaults();
            var s = GameConfiguration.Instance.EnemySystem?.ProgressionScales ?? new EnemyProgressionScalesConfig();

            SetSlider(p.EnemyBaseHealthScaleControl, s.BaseHealthScale);
            SetSlider(p.EnemyHealthGrowthScaleControl, s.HealthGrowthScale);
            SetSlider(p.EnemyAttributeGrowthScaleControl, s.AttributeGrowthScale);
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not EnemyTuningSettingsPanel p) return;
            try
            {
                PushFromPanelToConfig(p);
                GameConfiguration.Instance.EnemySystem?.EnsureSanitizedDefaults();
                if (!GameConfiguration.Instance.SaveToFile())
                    throw new InvalidOperationException("SaveToFile returned false.");
                showStatusMessage?.Invoke("Enemy tuning saved to TuningConfig.json.", true);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Enemy tuning: {ex.Message}", false);
            }
        }

        private static void SetSlider(SliderWithTextBox? control, double value)
        {
            if (control == null) return;
            double v = Math.Clamp(value, control.Minimum, control.Maximum);
            control.Value = v;
        }

        private static void PushFromPanelToConfig(EnemyTuningSettingsPanel p)
        {
            var sys = GameConfiguration.Instance.EnemySystem ??= new EnemySystemConfig();
            sys.ProgressionScales ??= new EnemyProgressionScalesConfig();

            if (p.EnemyBaseHealthScaleControl != null)
                sys.ProgressionScales.BaseHealthScale = Math.Clamp(p.EnemyBaseHealthScaleControl.Value, p.EnemyBaseHealthScaleControl.Minimum, p.EnemyBaseHealthScaleControl.Maximum);
            if (p.EnemyHealthGrowthScaleControl != null)
                sys.ProgressionScales.HealthGrowthScale = Math.Clamp(p.EnemyHealthGrowthScaleControl.Value, p.EnemyHealthGrowthScaleControl.Minimum, p.EnemyHealthGrowthScaleControl.Maximum);
            if (p.EnemyAttributeGrowthScaleControl != null)
                sys.ProgressionScales.AttributeGrowthScale = Math.Clamp(p.EnemyAttributeGrowthScaleControl.Value, p.EnemyAttributeGrowthScaleControl.Minimum, p.EnemyAttributeGrowthScaleControl.Maximum);

            sys.EnsureSanitizedDefaults();
        }
    }
}
