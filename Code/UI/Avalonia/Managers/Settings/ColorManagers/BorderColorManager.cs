using System;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using RPGGame;
using RPGGame.Config;
using System.Linq;
using TextBlock = Avalonia.Controls.TextBlock;

namespace RPGGame.UI.Avalonia.Managers.Settings.ColorManagers
{
    /// <summary>
    /// Manages border colors for panels. Uses GameSettings.Instance at apply time.
    /// </summary>
    public class BorderColorManager
    {
        private readonly SettingsPanel? settingsPanel;

        public BorderColorManager(SettingsPanel? settingsPanel)
        {
            this.settingsPanel = settingsPanel;
        }

        /// <summary>
        /// Applies border colors to panel borders
        /// </summary>
        public void ApplyColors()
        {
            if (settingsPanel == null) return;

            try
            {
                var s = GameSettings.Instance;
                var panelColor = SettingsColorManager.ParseColor(s.PanelBackgroundColor);
                var borderColor = SettingsColorManager.ParseColor(s.PanelBorderColor);
                var textColor = SettingsColorManager.ParseColor(s.PanelTextColor);

                // Find all borders in settings panels
                var borders = settingsPanel.GetLogicalDescendants().OfType<Border>()
                    .Where(b => b.Name != "PanelColorsPreview" && 
                                b.Name != "PanelBackgroundPreview" &&
                                b.Name != "PanelBorderPreview" &&
                                b.Name != "PanelTextPreview" &&
                                b.Name != "SettingsBackgroundPreview" &&
                                b.Name != "SettingsTitlePreview" &&
                                b.Name != "ListBoxSelectedPreview" &&
                                b.Name != "ListBoxSelectedBackgroundPreview" &&
                                b.Name != "ListBoxHoverBackgroundPreview" &&
                                b.Name != "ButtonPrimaryPreview" &&
                                b.Name != "ButtonSecondaryPreview" &&
                                b.Name != "ButtonBackPreview")
                    .ToList();

                foreach (var border in borders)
                {
                    // Only update borders that look like settings section borders
                    // (have white background and dark border by default)
                    if (border.Background is SolidColorBrush brush && 
                        brush.Color == Colors.White)
                    {
                        border.Background = new SolidColorBrush(panelColor);
                        border.BorderBrush = new SolidColorBrush(borderColor);
                    }
                }

                // Update text colors in panels
                var textBlocks = settingsPanel.GetLogicalDescendants().OfType<TextBlock>()
                    .Where(tb => tb.Foreground is SolidColorBrush fg && fg.Color == Colors.Black)
                    .ToList();

                foreach (var textBlock in textBlocks)
                {
                    // Only update text in settings sections, not previews
                    if (!textBlock.Name?.Contains("Preview") ?? true)
                    {
                        textBlock.Foreground = new SolidColorBrush(textColor);
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying border colors: {ex.Message}");
            }
        }
    }
}
