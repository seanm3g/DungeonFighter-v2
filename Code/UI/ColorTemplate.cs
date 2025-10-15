using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI
{
    /// <summary>
    /// Represents a color template with shader-like effects (inspired by Caves of Qud)
    /// </summary>
    public class ColorTemplate
    {
        public string Name { get; set; } = "";
        public ColorShaderType ShaderType { get; set; }
        public List<char> ColorSequence { get; set; } = new();

        public ColorTemplate(string name, ColorShaderType type, List<char> colors)
        {
            Name = name;
            ShaderType = type;
            ColorSequence = colors;
        }

        /// <summary>
        /// Applies this template to text
        /// </summary>
        /// <param name="text">The text to colorize</param>
        /// <param name="offset">Color sequence offset for undulation effect (default: 0)</param>
        public List<ColorDefinitions.ColoredSegment> Apply(string text, int offset = 0)
        {
            var segments = new List<ColorDefinitions.ColoredSegment>();

            if (ColorSequence.Count == 0 || string.IsNullOrEmpty(text))
            {
                segments.Add(new ColorDefinitions.ColoredSegment(text, ColorDefinitions.DefaultTextColor));
                return segments;
            }

            switch (ShaderType)
            {
                case ColorShaderType.Sequence:
                    return ApplySequence(text, offset);
                
                case ColorShaderType.Alternation:
                    return ApplyAlternation(text, offset);
                
                case ColorShaderType.Solid:
                    return ApplySolid(text);
                
                default:
                    segments.Add(new ColorDefinitions.ColoredSegment(text, ColorDefinitions.DefaultTextColor));
                    return segments;
            }
        }

        /// <summary>
        /// Sequence shader: Each character gets the next color in sequence
        /// Preserves whitespace without coloring it to avoid rendering issues
        /// </summary>
        /// <param name="text">The text to colorize</param>
        /// <param name="offset">Starting offset in the color sequence (for undulation)</param>
        private List<ColorDefinitions.ColoredSegment> ApplySequence(string text, int offset = 0)
        {
            var segments = new List<ColorDefinitions.ColoredSegment>();
            int colorIndex = offset % ColorSequence.Count;  // Start with offset
            
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    // Add whitespace WITH default color so it renders properly
                    segments.Add(new ColorDefinitions.ColoredSegment(
                        text[i].ToString(), 
                        ColorDefinitions.DefaultTextColor
                    ));
                }
                else
                {
                    // Apply color sequence to non-whitespace characters
                    char colorCode = ColorSequence[colorIndex % ColorSequence.Count];
                    var color = ColorDefinitions.GetColor(colorCode);
                    
                    segments.Add(new ColorDefinitions.ColoredSegment(
                        text[i].ToString(),
                        color ?? ColorDefinitions.DefaultTextColor
                    ));
                    
                    colorIndex++;
                }
            }
            
            return segments;
        }

        /// <summary>
        /// Alternation shader: Characters alternate between colors
        /// </summary>
        /// <param name="text">The text to colorize</param>
        /// <param name="offset">Starting offset in the color sequence (for undulation)</param>
        private List<ColorDefinitions.ColoredSegment> ApplyAlternation(string text, int offset = 0)
        {
            var segments = new List<ColorDefinitions.ColoredSegment>();
            int colorIndex = offset % ColorSequence.Count;  // Start with offset
            
            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    char colorCode = ColorSequence[colorIndex % ColorSequence.Count];
                    var color = ColorDefinitions.GetColor(colorCode);
                    
                    segments.Add(new ColorDefinitions.ColoredSegment(
                        text[i].ToString(),
                        color ?? ColorDefinitions.DefaultTextColor
                    ));
                    
                    colorIndex++;
                }
                else
                {
                    // Add whitespace WITH default color so it renders properly
                    segments.Add(new ColorDefinitions.ColoredSegment(
                        text[i].ToString(), 
                        ColorDefinitions.DefaultTextColor
                    ));
                }
            }
            
            return segments;
        }

        /// <summary>
        /// Solid shader: All text uses the same color
        /// </summary>
        private List<ColorDefinitions.ColoredSegment> ApplySolid(string text)
        {
            var segments = new List<ColorDefinitions.ColoredSegment>();
            var color = ColorDefinitions.GetColor(ColorSequence[0]);
            
            segments.Add(new ColorDefinitions.ColoredSegment(
                text,
                color ?? ColorDefinitions.DefaultTextColor
            ));
            
            return segments;
        }
    }

    /// <summary>
    /// Color shader types matching Caves of Qud system
    /// </summary>
    public enum ColorShaderType
    {
        /// <summary>
        /// Each character gets next color in sequence
        /// </summary>
        Sequence,
        
        /// <summary>
        /// Characters alternate between colors (skips whitespace)
        /// </summary>
        Alternation,
        
        /// <summary>
        /// All text uses the same color
        /// </summary>
        Solid
    }

    /// <summary>
    /// Collection of pre-defined color templates
    /// Loads configuration from GameData/ColorTemplates.json
    /// </summary>
    public static class ColorTemplateLibrary
    {
        private static readonly Dictionary<string, ColorTemplate> templates = new();
        private static bool _isInitialized = false;

        static ColorTemplateLibrary()
        {
            Initialize();
        }
        
        /// <summary>
        /// Initializes the color template library by loading from config file
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }
            
            try
            {
                // Load templates from JSON config
                RPGGame.Data.ColorTemplateLoader.LoadAndRegisterTemplates();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                RPGGame.ErrorHandler.LogError(ex, "ColorTemplateLibrary.Initialize", "Failed to load templates from config, using hardcoded defaults");
                // Fall back to hardcoded defaults if loading fails
                InitializeTemplates();
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Initializes hardcoded default templates
        /// Only used as a fallback if JSON loading fails
        /// </summary>
        private static void InitializeTemplates()
        {
            // Basic colors (solid)
            templates["red"] = new ColorTemplate("red", ColorShaderType.Solid, new List<char> { 'R' });
            templates["green"] = new ColorTemplate("green", ColorShaderType.Solid, new List<char> { 'G' });
            templates["blue"] = new ColorTemplate("blue", ColorShaderType.Solid, new List<char> { 'B' });
            templates["yellow"] = new ColorTemplate("yellow", ColorShaderType.Solid, new List<char> { 'W' });
            templates["white"] = new ColorTemplate("white", ColorShaderType.Solid, new List<char> { 'Y' });
            templates["cyan"] = new ColorTemplate("cyan", ColorShaderType.Solid, new List<char> { 'C' });
            templates["magenta"] = new ColorTemplate("magenta", ColorShaderType.Solid, new List<char> { 'M' });
            templates["grey"] = new ColorTemplate("grey", ColorShaderType.Solid, new List<char> { 'y' });

            // Dark variants (solid)
            templates["dark red"] = new ColorTemplate("dark red", ColorShaderType.Solid, new List<char> { 'r' });
            templates["dark green"] = new ColorTemplate("dark green", ColorShaderType.Solid, new List<char> { 'g' });
            templates["dark blue"] = new ColorTemplate("dark blue", ColorShaderType.Solid, new List<char> { 'b' });
            templates["dark orange"] = new ColorTemplate("dark orange", ColorShaderType.Solid, new List<char> { 'o' });
            templates["orange"] = new ColorTemplate("orange", ColorShaderType.Solid, new List<char> { 'O' });
            templates["brown"] = new ColorTemplate("brown", ColorShaderType.Solid, new List<char> { 'w' });

            // Special effects (sequence)
            templates["fiery"] = new ColorTemplate("fiery", ColorShaderType.Sequence, 
                new List<char> { 'R', 'O', 'W', 'Y', 'W', 'O', 'R' });
            
            templates["icy"] = new ColorTemplate("icy", ColorShaderType.Sequence, 
                new List<char> { 'C', 'B', 'Y', 'C', 'b', 'C', 'Y' });
            
            templates["toxic"] = new ColorTemplate("toxic", ColorShaderType.Sequence, 
                new List<char> { 'g', 'G', 'Y', 'G', 'g' });
            
            templates["crystalline"] = new ColorTemplate("crystalline", ColorShaderType.Sequence, 
                new List<char> { 'm', 'M', 'B', 'Y', 'B', 'M', 'm' });
            
            templates["electric"] = new ColorTemplate("electric", ColorShaderType.Sequence, 
                new List<char> { 'C', 'Y', 'W', 'Y', 'C' });
            
            templates["holy"] = new ColorTemplate("holy", ColorShaderType.Sequence, 
                new List<char> { 'W', 'Y', 'W', 'Y', 'W' });
            
            templates["demonic"] = new ColorTemplate("demonic", ColorShaderType.Sequence, 
                new List<char> { 'r', 'R', 'K', 'r', 'R' });
            
            templates["arcane"] = new ColorTemplate("arcane", ColorShaderType.Sequence, 
                new List<char> { 'm', 'M', 'C', 'M', 'm' });
            
            templates["natural"] = new ColorTemplate("natural", ColorShaderType.Sequence, 
                new List<char> { 'g', 'G', 'w', 'G', 'g' });
            
            templates["shadow"] = new ColorTemplate("shadow", ColorShaderType.Sequence, 
                new List<char> { 'K', 'k', 'y', 'k', 'K' });
            
            templates["golden"] = new ColorTemplate("golden", ColorShaderType.Sequence, 
                new List<char> { 'W', 'O', 'W', 'O', 'W' });
            
            templates["bloodied"] = new ColorTemplate("bloodied", ColorShaderType.Sequence, 
                new List<char> { 'r', 'R', 'r', 'K' });
            
            templates["ethereal"] = new ColorTemplate("ethereal", ColorShaderType.Sequence, 
                new List<char> { 'C', 'M', 'Y', 'M', 'C' });
            
            templates["corrupted"] = new ColorTemplate("corrupted", ColorShaderType.Sequence, 
                new List<char> { 'm', 'K', 'r', 'K', 'm' });
            
            // Alternation effects
            templates["rainbow"] = new ColorTemplate("rainbow", ColorShaderType.Alternation, 
                new List<char> { 'R', 'O', 'W', 'G', 'C', 'B', 'M' });
            
            templates["amorous"] = new ColorTemplate("amorous", ColorShaderType.Alternation, 
                new List<char> { 'r', 'R', 'M', 'm' });
            
            templates["bee"] = new ColorTemplate("bee", ColorShaderType.Alternation, 
                new List<char> { 'K', 'W', 'W', 'K' });
            
            templates["forest"] = new ColorTemplate("forest", ColorShaderType.Alternation, 
                new List<char> { 'g', 'G', 'w', 'G' });
            
            // Rarity colors (for items)
            templates["common"] = new ColorTemplate("common", ColorShaderType.Solid, new List<char> { 'y' });
            templates["uncommon"] = new ColorTemplate("uncommon", ColorShaderType.Solid, new List<char> { 'G' });
            templates["rare"] = new ColorTemplate("rare", ColorShaderType.Solid, new List<char> { 'B' });
            templates["epic"] = new ColorTemplate("epic", ColorShaderType.Solid, new List<char> { 'M' });
            templates["legendary"] = new ColorTemplate("legendary", ColorShaderType.Solid, new List<char> { 'O' });
            
            // Menu gradient colors (warm to cool whites)
            // These are approximated using existing color codes
            templates["warmwhite"] = new ColorTemplate("warmwhite", ColorShaderType.Solid, new List<char> { 'W' }); // RGB 255, 245, 220 - warm yellowish white
            templates["neutralwarm"] = new ColorTemplate("neutralwarm", ColorShaderType.Solid, new List<char> { 'Y' }); // RGB 250, 250, 240 - neutral warm white
            templates["neutralcool"] = new ColorTemplate("neutralcool", ColorShaderType.Solid, new List<char> { 'Y' }); // RGB 240, 250, 250 - neutral cool white
            templates["coolwhite"] = new ColorTemplate("coolwhite", ColorShaderType.Solid, new List<char> { 'C' }); // RGB 220, 240, 255 - cool bluish white
            
            // Combat related
            templates["critical"] = new ColorTemplate("critical", ColorShaderType.Sequence, 
                new List<char> { 'R', 'O', 'Y', 'O', 'R' });
            
            templates["miss"] = new ColorTemplate("miss", ColorShaderType.Solid, new List<char> { 'K' });
            
            templates["damage"] = new ColorTemplate("damage", ColorShaderType.Solid, new List<char> { 'R' });
            
            templates["heal"] = new ColorTemplate("heal", ColorShaderType.Solid, new List<char> { 'G' });
            
            templates["mana"] = new ColorTemplate("mana", ColorShaderType.Solid, new List<char> { 'B' });
            
            // Status effects
            templates["poisoned"] = new ColorTemplate("poisoned", ColorShaderType.Sequence, 
                new List<char> { 'g', 'G', 'g', 'k' });
            
            templates["stunned"] = new ColorTemplate("stunned", ColorShaderType.Sequence, 
                new List<char> { 'W', 'Y', 'W', 'y' });
            
            templates["burning"] = new ColorTemplate("burning", ColorShaderType.Sequence, 
                new List<char> { 'R', 'O', 'R', 'r' });
            
            templates["frozen"] = new ColorTemplate("frozen", ColorShaderType.Sequence, 
                new List<char> { 'C', 'B', 'Y', 'C' });
            
            templates["bleeding"] = new ColorTemplate("bleeding", ColorShaderType.Sequence, 
                new List<char> { 'r', 'R', 'r', 'r' });
        }

        /// <summary>
        /// Gets a template by name
        /// </summary>
        public static ColorTemplate? GetTemplate(string name)
        {
            return templates.TryGetValue(name.ToLower(), out var template) ? template : null;
        }

        /// <summary>
        /// Checks if a template exists
        /// </summary>
        public static bool HasTemplate(string name)
        {
            return templates.ContainsKey(name.ToLower());
        }

        /// <summary>
        /// Gets all template names
        /// </summary>
        public static IEnumerable<string> GetAllTemplateNames()
        {
            return templates.Keys;
        }

        /// <summary>
        /// Adds or updates a custom template
        /// </summary>
        public static void RegisterTemplate(ColorTemplate template)
        {
            templates[template.Name.ToLower()] = template;
        }
        
        /// <summary>
        /// Reloads templates from the config file
        /// </summary>
        public static void ReloadFromConfig()
        {
            templates.Clear();
            RPGGame.Data.ColorTemplateLoader.Reload();
            _isInitialized = false;
            Initialize();
        }
        
        /// <summary>
        /// Clears all templates
        /// </summary>
        public static void Clear()
        {
            templates.Clear();
        }
    }
}

