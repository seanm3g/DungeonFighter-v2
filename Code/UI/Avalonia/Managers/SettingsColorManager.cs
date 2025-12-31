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
        private readonly GameSettings settings;
        
        // Specialized color managers
        private readonly WindowColorManager windowColorManager;
        private readonly TextBlockColorManager textBlockColorManager;
        private readonly ListBoxColorManager listBoxColorManager;
        private readonly ButtonColorManager buttonColorManager;
        private readonly BorderColorManager borderColorManager;
        private readonly TextBoxColorManager textBoxColorManager;

        public SettingsColorManager(SettingsPanel panel, GameSettings gameSettings)
        {
            settingsPanel = panel;
            settings = gameSettings;
            
            // Initialize specialized managers
            windowColorManager = new WindowColorManager(settingsPanel, settings);
            textBlockColorManager = new TextBlockColorManager(settingsPanel, settings);
            listBoxColorManager = new ListBoxColorManager(settingsPanel, settings);
            buttonColorManager = new ButtonColorManager(settingsPanel, settings);
            borderColorManager = new BorderColorManager(settingsPanel, settings);
            textBoxColorManager = new TextBoxColorManager(settingsPanel, settings);
        }

        /// <summary>
        /// Applies all color settings to the settings panel UI by delegating to specialized managers
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
    }
}

