using System.Globalization;
using Avalonia.Controls;
using RPGGame;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    public sealed class TravelPanelHandler : ISettingsPanelHandler
    {
        public string PanelType => "Travel";

        public void WireUp(UserControl panel)
        {
            if (panel is not TravelSettingsPanel travelPanel) return;

            var box = travelPanel.TravelTimeMultiplierTextBox ?? travelPanel.FindControl<TextBox>("TravelTimeMultiplierTextBox");
            if (box != null)
            {
                box.TextChanged += (_, _) =>
                {
                    if (string.IsNullOrWhiteSpace(box.Text))
                        return;
                    ApplyTravelTimeMultiplierFromText(box.Text);
                };
            }

            LoadSettings(travelPanel);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not TravelSettingsPanel travelPanel) return;

            var box = travelPanel.TravelTimeMultiplierTextBox ?? travelPanel.FindControl<TextBox>("TravelTimeMultiplierTextBox");
            if (box != null)
                box.Text = GameSettings.Instance.TravelTimeMultiplier.ToString(CultureInfo.InvariantCulture);
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not TravelSettingsPanel travelPanel) return;

            var box = travelPanel.TravelTimeMultiplierTextBox ?? travelPanel.FindControl<TextBox>("TravelTimeMultiplierTextBox");
            ApplyTravelTimeMultiplierFromText(box?.Text);
        }

        private static void ApplyTravelTimeMultiplierFromText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                GameSettings.Instance.TravelTimeMultiplier = 1.0;
                GameSettings.Instance.ValidateAndFix();
                return;
            }

            if (double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ||
                double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                GameSettings.Instance.TravelTimeMultiplier = value;
                GameSettings.Instance.ValidateAndFix();
            }
        }
    }
}
