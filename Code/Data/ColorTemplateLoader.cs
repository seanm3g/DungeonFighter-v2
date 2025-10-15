using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using RPGGame.UI;

namespace RPGGame.Data
{
    /// <summary>
    /// Data structure for loading color templates from JSON
    /// </summary>
    public class ColorTemplateConfig
    {
        [JsonPropertyName("$schema")]
        public string? Schema { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("colorCodes")]
        public ColorCodeSection? ColorCodes { get; set; }
        
        [JsonPropertyName("templates")]
        public List<ColorTemplateData>? Templates { get; set; }
        
        [JsonPropertyName("usageExamples")]
        public List<UsageExample>? UsageExamples { get; set; }
    }
    
    public class ColorCodeSection
    {
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("codes")]
        public Dictionary<string, string>? Codes { get; set; }
    }
    
    public class ColorTemplateData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("shaderType")]
        public string ShaderType { get; set; } = "";
        
        [JsonPropertyName("colors")]
        public List<string>? Colors { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
    
    public class UsageExample
    {
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("example")]
        public string? Example { get; set; }
    }
    
    /// <summary>
    /// Loads color templates and color codes from ColorTemplates.json
    /// </summary>
    public static class ColorTemplateLoader
    {
        private static ColorTemplateConfig? _cachedConfig;
        private static bool _isLoaded = false;
        
        /// <summary>
        /// Loads color template configuration from GameData/ColorTemplates.json
        /// </summary>
        public static ColorTemplateConfig LoadColorTemplates()
        {
            if (_isLoaded && _cachedConfig != null)
            {
                return _cachedConfig;
            }
            
            var filePath = JsonLoader.FindGameDataFile("ColorTemplates.json");
            if (filePath == null)
            {
                ErrorHandler.LogWarning("ColorTemplates.json not found. Using empty configuration.", "ColorTemplateLoader");
                _cachedConfig = new ColorTemplateConfig
                {
                    Templates = new List<ColorTemplateData>(),
                    ColorCodes = new ColorCodeSection { Codes = new Dictionary<string, string>() }
                };
                _isLoaded = true;
                return _cachedConfig;
            }
            
            _cachedConfig = JsonLoader.LoadJson<ColorTemplateConfig>(filePath, true, new ColorTemplateConfig
            {
                Templates = new List<ColorTemplateData>(),
                ColorCodes = new ColorCodeSection { Codes = new Dictionary<string, string>() }
            });
            
            _isLoaded = true;
            return _cachedConfig;
        }
        
        /// <summary>
        /// Loads color templates and registers them with ColorTemplateLibrary
        /// </summary>
        public static void LoadAndRegisterTemplates()
        {
            var config = LoadColorTemplates();
            
            if (config.Templates == null)
            {
                ErrorHandler.LogWarning("No templates found in ColorTemplates.json", "ColorTemplateLoader");
                return;
            }
            
            foreach (var templateData in config.Templates)
            {
                try
                {
                    // Parse shader type
                    ColorShaderType shaderType = templateData.ShaderType.ToLower() switch
                    {
                        "sequence" => ColorShaderType.Sequence,
                        "alternation" => ColorShaderType.Alternation,
                        "solid" => ColorShaderType.Solid,
                        _ => ColorShaderType.Solid
                    };
                    
                    // Convert color strings to chars
                    var colors = new List<char>();
                    if (templateData.Colors != null)
                    {
                        foreach (var colorStr in templateData.Colors)
                        {
                            if (!string.IsNullOrEmpty(colorStr) && colorStr.Length > 0)
                            {
                                colors.Add(colorStr[0]);
                            }
                        }
                    }
                    
                    if (colors.Count == 0)
                    {
                        ErrorHandler.LogWarning($"Template '{templateData.Name}' has no valid colors", "ColorTemplateLoader");
                        continue;
                    }
                    
                    // Create and register the template
                    var template = new ColorTemplate(templateData.Name, shaderType, colors);
                    ColorTemplateLibrary.RegisterTemplate(template);
                }
                catch (Exception ex)
                {
                    ErrorHandler.LogError(ex, "ColorTemplateLoader", $"Failed to load template: {templateData.Name}");
                }
            }
        }
        
        /// <summary>
        /// Gets color code definitions from the config
        /// </summary>
        public static Dictionary<string, string> GetColorCodeDefinitions()
        {
            var config = LoadColorTemplates();
            return config.ColorCodes?.Codes ?? new Dictionary<string, string>();
        }
        
        /// <summary>
        /// Reloads the color template configuration from disk
        /// </summary>
        public static void Reload()
        {
            _isLoaded = false;
            _cachedConfig = null;
            JsonLoader.ClearCacheForFile("ColorTemplates.json");
        }
    }
}

