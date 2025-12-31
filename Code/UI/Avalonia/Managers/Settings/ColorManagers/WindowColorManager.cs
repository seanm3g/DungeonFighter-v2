using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using RPGGame;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers.Settings.ColorManagers
{
    /// <summary>
    /// Manages window and panel background colors
    /// </summary>
    public class WindowColorManager
    {
        private readonly SettingsPanel? settingsPanel;
        private readonly GameSettings settings;

        public WindowColorManager(SettingsPanel? settingsPanel, GameSettings settings)
        {
            this.settingsPanel = settingsPanel;
            this.settings = settings;
        }

        /// <summary>
        /// Applies window and panel background colors
        /// </summary>
        public void ApplyColors()
        {
            if (settingsPanel == null) return;

            try
            {
                // Apply window background
                var window = settingsPanel.GetLogicalAncestors().OfType<Window>().FirstOrDefault();
                if (window != null)
                {
                    window.Background = new SolidColorBrush(SettingsColorManager.ParseColor(settings.SettingsBackgroundColor));
                }

                // Apply panel background
                settingsPanel.Background = new SolidColorBrush(SettingsColorManager.ParseColor(settings.SettingsBackgroundColor));
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying window colors: {ex.Message}");
            }
        }
    }
}
