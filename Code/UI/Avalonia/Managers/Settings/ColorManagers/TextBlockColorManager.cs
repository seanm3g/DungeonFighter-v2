using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using RPGGame;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers.Settings.ColorManagers
{
    /// <summary>
    /// Manages TextBlock foreground colors (title, text)
    /// </summary>
    public class TextBlockColorManager
    {
        private readonly SettingsPanel? settingsPanel;
        private readonly GameSettings settings;

        public TextBlockColorManager(SettingsPanel? settingsPanel, GameSettings settings)
        {
            this.settingsPanel = settingsPanel;
            this.settings = settings;
        }

        /// <summary>
        /// Applies TextBlock foreground colors
        /// </summary>
        public void ApplyColors()
        {
            if (settingsPanel == null) return;

            try
            {
                // Find and update title color
                var titleBlock = settingsPanel.FindControl<TextBlock>("TitleTextBlock");
                if (titleBlock == null)
                {
                    // Try to find it by searching for the title text
                    var titleBlocks = settingsPanel.GetLogicalDescendants().OfType<TextBlock>()
                        .Where(tb => tb.Text == "GAME SETTINGS").ToList();
                    if (titleBlocks.Any())
                    {
                        titleBlock = titleBlocks.First();
                    }
                }
                if (titleBlock != null)
                {
                    titleBlock.Foreground = new SolidColorBrush(SettingsColorManager.ParseColor(settings.SettingsTitleColor));
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying TextBlock colors: {ex.Message}");
            }
        }
    }
}
