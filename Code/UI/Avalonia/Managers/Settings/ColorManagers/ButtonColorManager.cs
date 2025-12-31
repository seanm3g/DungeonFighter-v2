using Avalonia.Controls;
using Avalonia.Media;
using RPGGame;

namespace RPGGame.UI.Avalonia.Managers.Settings.ColorManagers
{
    /// <summary>
    /// Manages button background colors (primary, secondary, back)
    /// </summary>
    public class ButtonColorManager
    {
        private readonly SettingsPanel? settingsPanel;
        private readonly GameSettings settings;

        public ButtonColorManager(SettingsPanel? settingsPanel, GameSettings settings)
        {
            this.settingsPanel = settingsPanel;
            this.settings = settings;
        }

        /// <summary>
        /// Applies button background colors
        /// </summary>
        public void ApplyColors()
        {
            if (settingsPanel == null) return;

            try
            {
                // Update button colors
                var saveButton = settingsPanel.FindControl<Button>("SaveButton");
                if (saveButton != null)
                {
                    saveButton.Background = new SolidColorBrush(SettingsColorManager.ParseColor(settings.ButtonPrimaryColor));
                }

                var resetButton = settingsPanel.FindControl<Button>("ResetButton");
                if (resetButton != null)
                {
                    resetButton.Background = new SolidColorBrush(SettingsColorManager.ParseColor(settings.ButtonSecondaryColor));
                }

                var backButton = settingsPanel.FindControl<Button>("BackButton");
                if (backButton != null)
                {
                    backButton.Background = new SolidColorBrush(SettingsColorManager.ParseColor(settings.ButtonBackColor));
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying button colors: {ex.Message}");
            }
        }
    }
}
