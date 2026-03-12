using System;
using Avalonia.Controls;
using Avalonia.Media;
using RPGGame;
using RPGGame.Config;

namespace RPGGame.UI.Avalonia.Managers.Settings.ColorManagers
{
    /// <summary>
    /// Manages button background colors (primary, secondary, back). Uses GameSettings.Instance at apply time.
    /// </summary>
    public class ButtonColorManager
    {
        private readonly SettingsPanel? settingsPanel;

        public ButtonColorManager(SettingsPanel? settingsPanel)
        {
            this.settingsPanel = settingsPanel;
        }

        /// <summary>
        /// Applies button background colors
        /// </summary>
        public void ApplyColors()
        {
            if (settingsPanel == null) return;

            try
            {
                var s = GameSettings.Instance;
                // Update button colors
                var saveButton = settingsPanel.FindControl<Button>("SaveButton");
                if (saveButton != null)
                {
                    saveButton.Background = new SolidColorBrush(SettingsColorManager.ParseColor(s.ButtonPrimaryColor));
                }

                var resetButton = settingsPanel.FindControl<Button>("ResetButton");
                if (resetButton != null)
                {
                    resetButton.Background = new SolidColorBrush(SettingsColorManager.ParseColor(s.ButtonSecondaryColor));
                }

                var backButton = settingsPanel.FindControl<Button>("BackButton");
                if (backButton != null)
                {
                    backButton.Background = new SolidColorBrush(SettingsColorManager.ParseColor(s.ButtonBackColor));
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying button colors: {ex.Message}");
            }
        }
    }
}
