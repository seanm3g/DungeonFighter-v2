using Avalonia.Media;
using RPGGame.Data;
using RPGGame.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Settings.Helpers
{
    /// <summary>
    /// Shared helper methods for appearance settings panels
    /// </summary>
    public static class AppearanceSettingsHelper
    {
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
                hexColor = hexColor.TrimStart('#');
                if (hexColor.Length == 6)
                {
                    hexColor = "FF" + hexColor;
                }
                if (hexColor.Length == 8)
                {
                    var a = Convert.ToByte(hexColor.Substring(0, 2), 16);
                    var r = Convert.ToByte(hexColor.Substring(2, 2), 16);
                    var g = Convert.ToByte(hexColor.Substring(4, 2), 16);
                    var b = Convert.ToByte(hexColor.Substring(6, 2), 16);
                    return Color.FromArgb(a, r, g, b);
                }
                return Color.Parse(hexColor);
            }
            catch
            {
                return Colors.White;
            }
        }

        /// <summary>
        /// Saves color templates to file
        /// </summary>
        public static void SaveColorTemplates(List<ColorTemplateData> colorTemplates)
        {
            try
            {
                var templateFilePath = JsonLoader.FindGameDataFile("ColorTemplates.json");
                if (templateFilePath != null)
                {
                    var config = new ColorTemplateConfig
                    {
                        Templates = colorTemplates
                    };
                    
                    var existingConfig = ColorTemplateLoader.LoadColorTemplates();
                    config.Schema = existingConfig.Schema;
                    config.Description = existingConfig.Description;

                    JsonLoader.SaveJson(config, templateFilePath, false);
                    ColorTemplateLoader.Reload();
                    ColorConfigurationLoader.Reload();
                    
                    System.Diagnostics.Debug.WriteLine("Color templates saved successfully!");
                }
                else
                {
                    var unifiedPath = JsonLoader.FindGameDataFile("ColorConfiguration.json");
                    if (unifiedPath != null)
                    {
                        var unifiedConfig = ColorConfigurationLoader.LoadColorConfiguration();
                        unifiedConfig.ColorTemplates = colorTemplates;
                        JsonLoader.SaveJson(unifiedConfig, unifiedPath, false);
                        ColorConfigurationLoader.Reload();
                        System.Diagnostics.Debug.WriteLine("Color templates saved to unified config!");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving color templates: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves keyword colors to file
        /// </summary>
        public static void SaveKeywordColors(List<KeywordGroupData> keywordGroups)
        {
            try
            {
                var keywordFilePath = JsonLoader.FindGameDataFile("KeywordColorGroups.json");
                if (keywordFilePath != null)
                {
                    var config = new KeywordColorConfig
                    {
                        Groups = keywordGroups
                    };
                    
                    var existingConfig = KeywordColorLoader.LoadKeywordColors();
                    config.Schema = existingConfig.Schema;
                    config.Description = existingConfig.Description;

                    JsonLoader.SaveJson(config, keywordFilePath, false);
                    KeywordColorLoader.Reload();
                    ColorConfigurationLoader.Reload();
                    
                    System.Diagnostics.Debug.WriteLine("Keyword colors saved successfully!");
                }
                else
                {
                    var unifiedPath = JsonLoader.FindGameDataFile("ColorConfiguration.json");
                    if (unifiedPath != null)
                    {
                        var unifiedConfig = ColorConfigurationLoader.LoadColorConfiguration();
                        unifiedConfig.KeywordGroups = keywordGroups;
                        JsonLoader.SaveJson(unifiedConfig, unifiedPath, false);
                        ColorConfigurationLoader.Reload();
                        System.Diagnostics.Debug.WriteLine("Keyword colors saved to unified config!");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving keyword colors: {ex.Message}");
            }
        }
    }
}
