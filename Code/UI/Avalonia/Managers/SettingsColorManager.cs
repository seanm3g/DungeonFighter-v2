using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.Avalonia.Managers.Settings.ColorManagers;
using System;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Coordinates color application to settings UI elements by delegating to specialized color managers
    /// </summary>
    public class SettingsColorManager
    {
        private readonly SettingsPanel? settingsPanel;
        
        // Specialized color managers (each uses GameSettings.Instance at apply time)
        private readonly WindowColorManager windowColorManager;
        private readonly TextBlockColorManager textBlockColorManager;
        private readonly ListBoxColorManager listBoxColorManager;
        private readonly ButtonColorManager buttonColorManager;
        private readonly BorderColorManager borderColorManager;
        private readonly TextBoxColorManager textBoxColorManager;

        public SettingsColorManager(SettingsPanel panel)
        {
            settingsPanel = panel;
            
            windowColorManager = new WindowColorManager(settingsPanel);
            textBlockColorManager = new TextBlockColorManager(settingsPanel);
            listBoxColorManager = new ListBoxColorManager(settingsPanel);
            buttonColorManager = new ButtonColorManager(settingsPanel);
            borderColorManager = new BorderColorManager(settingsPanel);
            textBoxColorManager = new TextBoxColorManager(settingsPanel);
        }

        /// <summary>
        /// Applies all color settings to the settings panel UI by delegating to specialized managers (uses GameSettings.Instance at use time).
        /// </summary>
        public void ApplyColors()
        {
            if (settingsPanel == null) return;

            try
            {
                windowColorManager.ApplyColors();
                textBlockColorManager.ApplyColors();
                listBoxColorManager.ApplyColors();
                buttonColorManager.ApplyColors();
                borderColorManager.ApplyColors();
                textBoxColorManager.ApplyColors();
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error applying colors: {ex.Message}");
            }
        }


        /// <summary>
        /// Parses a hex color string to an Avalonia Color
        /// Supports formats: #RRGGBB, #AARRGGBB, RRGGBB, AARRGGBB
        /// </summary>
        public static Color ParseColor(string hexColor)
        {
            if (string.IsNullOrWhiteSpace(hexColor))
                return Colors.White;

            try
            {
                // Remove # if present
                hexColor = hexColor.TrimStart('#');

                // Handle 6-digit hex (RRGGBB) - add FF for alpha
                if (hexColor.Length == 6)
                {
                    hexColor = "FF" + hexColor;
                }

                // Handle 8-digit hex (AARRGGBB)
                if (hexColor.Length == 8)
                {
                    var a = Convert.ToByte(hexColor.Substring(0, 2), 16);
                    var r = Convert.ToByte(hexColor.Substring(2, 2), 16);
                    var g = Convert.ToByte(hexColor.Substring(4, 2), 16);
                    var b = Convert.ToByte(hexColor.Substring(6, 2), 16);
                    return Color.FromArgb(a, r, g, b);
                }

                // Try parsing as named color or fallback
                return Color.Parse(hexColor);
            }
            catch
            {
                return Colors.White;
            }
        }

        /// <summary>
        /// Validates a hex color string
        /// </summary>
        public static bool IsValidHexColor(string hexColor)
        {
            if (string.IsNullOrWhiteSpace(hexColor))
                return false;

            try
            {
                var color = ParseColor(hexColor);
                return color != Colors.Transparent || hexColor.ToUpper().Contains("TRANSPARENT");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Picks a readable text color when configured foreground/background are too similar (e.g. black on black).
        /// </summary>
        public static Color EnsureContrastingTextColor(Color textColor, Color backgroundColor)
        {
            double textLuminance = GetRelativeLuminance(textColor);
            double bgLuminance = GetRelativeLuminance(backgroundColor);
            double contrast = (Math.Max(textLuminance, bgLuminance) + 0.05) / (Math.Min(textLuminance, bgLuminance) + 0.05);

            if (contrast >= 4.5)
                return textColor;

            return bgLuminance < 0.35 ? Colors.White : Colors.Black;
        }

        private static double GetRelativeLuminance(Color color)
        {
            static double Channel(byte c)
            {
                double s = c / 255.0;
                return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
            }

            double r = Channel(color.R);
            double g = Channel(color.G);
            double b = Channel(color.B);
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }
    }
}

