using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using RPGGame;
using RPGGame.Config;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers.Settings.ColorManagers
{
    /// <summary>
    /// Manages window and panel background colors (uses GameSettings.Instance at apply time).
    /// </summary>
    public class WindowColorManager
    {
        private readonly SettingsPanel? settingsPanel;

        public WindowColorManager(SettingsPanel? settingsPanel)
        {
            this.settingsPanel = settingsPanel;
        }

        /// <summary>
        /// Applies window and panel background colors
        /// </summary>
        public void ApplyColors()
        {
            if (settingsPanel == null) return;

            try
            {
                var s = GameSettings.Instance;
                // Apply window background
                var window = settingsPanel.GetLogicalAncestors().OfType<Window>().FirstOrDefault();
                if (window != null)
                {
                    window.Background = new SolidColorBrush(SettingsColorManager.ParseColor(s.SettingsBackgroundColor));
                }

                // Apply panel background
                settingsPanel.Background = new SolidColorBrush(SettingsColorManager.ParseColor(s.SettingsBackgroundColor));
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying window colors: {ex.Message}");
            }
        }
    }
}
