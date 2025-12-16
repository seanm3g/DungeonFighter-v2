using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Data
{
    /// <summary>
    /// Data structure for a single color template
    /// </summary>
    public class ColorTemplateData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("shaderType")]
        public string ShaderType { get; set; } = "sequence"; // "solid", "sequence", or "alternation"
        
        [JsonPropertyName("colors")]
        public List<string> Colors { get; set; } = new List<string>(); // Color codes like "R", "G", "B", etc.
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
    
    /// <summary>
    /// Root configuration structure for ColorTemplates.json
    /// </summary>
    public class ColorTemplateConfig
    {
        [JsonPropertyName("$schema")]
        public string? Schema { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("colorCodes")]
        public object? ColorCodes { get; set; } // Documentation section, not used in code
        
        [JsonPropertyName("templates")]
        public List<ColorTemplateData> Templates { get; set; } = new List<ColorTemplateData>();
        
        [JsonPropertyName("usageExamples")]
        public object? UsageExamples { get; set; } // Documentation section, not used in code
    }
    
    /// <summary>
    /// Loads color templates from GameData/ColorTemplates.json
    /// </summary>
    public static class ColorTemplateLoader
    {
        private static ColorTemplateConfig? _cachedConfig;
        private static Dictionary<string, ColorTemplateData>? _templateCache;
        private static bool _isLoaded = false;
        
        /// <summary>
        /// Loads color template configuration from GameData/ColorTemplates.json
        /// First tries unified ColorConfiguration.json, then falls back to individual file
        /// </summary>
        public static ColorTemplateConfig LoadColorTemplates()
        {
            if (_isLoaded && _cachedConfig != null)
            {
                return _cachedConfig;
            }
            
            // Try to load from unified configuration first
            var unifiedConfig = ColorConfigurationLoader.LoadColorConfiguration();
            if (unifiedConfig.ColorTemplates != null && unifiedConfig.ColorTemplates.Count > 0)
            {
                // Convert unified config to ColorTemplateConfig format
                _cachedConfig = new ColorTemplateConfig
                {
                    Templates = unifiedConfig.ColorTemplates.Select(t => new ColorTemplateData
                    {
                        Name = t.Name,
                        ShaderType = t.ShaderType,
                        Colors = t.Colors,
                        Description = t.Description
                    }).ToList()
                };
                
                // Build template cache for fast lookup
                _templateCache = new Dictionary<string, ColorTemplateData>(StringComparer.OrdinalIgnoreCase);
                foreach (var template in _cachedConfig.Templates)
                {
                    if (!string.IsNullOrEmpty(template.Name))
                    {
                        _templateCache[template.Name] = template;
                    }
                }
                
                _isLoaded = true;
                return _cachedConfig;
            }
            
            // Fallback to individual JSON file
            var filePath = JsonLoader.FindGameDataFile("ColorTemplates.json");
            if (filePath == null)
            {
                ErrorHandler.LogWarning("ColorTemplates.json not found. Using empty configuration.", "ColorTemplateLoader");
                _cachedConfig = new ColorTemplateConfig
                {
                    Templates = new List<ColorTemplateData>()
                };
                _isLoaded = true;
                _templateCache = new Dictionary<string, ColorTemplateData>();
                return _cachedConfig;
            }
            
            _cachedConfig = JsonLoader.LoadJson<ColorTemplateConfig>(filePath, true, new ColorTemplateConfig
            {
                Templates = new List<ColorTemplateData>()
            });
            
            // Build template cache for fast lookup
            _templateCache = new Dictionary<string, ColorTemplateData>(StringComparer.OrdinalIgnoreCase);
            if (_cachedConfig.Templates != null)
            {
                foreach (var template in _cachedConfig.Templates)
                {
                    if (!string.IsNullOrEmpty(template.Name))
                    {
                        _templateCache[template.Name] = template;
                    }
                }
            }
            
            _isLoaded = true;
            return _cachedConfig;
        }
        
        /// <summary>
        /// Gets a specific template by name (case-insensitive)
        /// First tries unified ColorConfiguration.json, then falls back to individual file
        /// </summary>
        public static ColorTemplateData? GetTemplate(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
                return null;
            
            // Try unified configuration first by checking the config data directly
            // (avoiding circular call to ColorConfigurationLoader.GetTemplate)
            var unifiedConfig = ColorConfigurationLoader.LoadColorConfiguration();
            if (unifiedConfig?.ColorTemplates != null && unifiedConfig.ColorTemplates.Count > 0)
            {
                var unifiedTemplate = unifiedConfig.ColorTemplates.FirstOrDefault(
                    t => string.Equals(t.Name, templateName, StringComparison.OrdinalIgnoreCase));
                if (unifiedTemplate != null)
                {
                    return unifiedTemplate;
                }
            }
            
            // Fallback to individual file loading
            LoadColorTemplates(); // Ensure templates are loaded
            
            if (_templateCache == null)
                return null;
            
            _templateCache.TryGetValue(templateName, out var template);
            return template;
        }
        
        /// <summary>
        /// Checks if a template exists (case-insensitive)
        /// </summary>
        public static bool HasTemplate(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
                return false;
            
            LoadColorTemplates(); // Ensure templates are loaded
            
            if (_templateCache == null)
                return false;
            
            return _templateCache.ContainsKey(templateName);
        }
        
        /// <summary>
        /// Gets all available template names
        /// </summary>
        public static IEnumerable<string> GetAllTemplateNames()
        {
            LoadColorTemplates(); // Ensure templates are loaded
            
            if (_templateCache == null)
                return new List<string>();
            
            return _templateCache.Keys;
        }
        
        /// <summary>
        /// Reloads the color template configuration from disk
        /// </summary>
        public static void Reload()
        {
            _isLoaded = false;
            _cachedConfig = null;
            _templateCache = null;
            JsonLoader.ClearCacheForFile("ColorTemplates.json");
        }
    }
}

