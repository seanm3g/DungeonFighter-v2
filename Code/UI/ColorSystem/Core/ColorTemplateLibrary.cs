using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.Data;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Library of color templates for different themes and effects
    /// Loads templates from GameData/ColorTemplates.json
    /// </summary>
    public static class ColorTemplateLibrary
    {
        /// <summary>
        /// Creates multi-color text by alternating colors for each character
        /// Merges consecutive characters with the same color to reduce segments and prevent spacing issues
        /// </summary>
        private static List<ColoredText> CreateMultiColorText(string text, Color[] colors, string? sourceTemplate = null)
        {
            if (string.IsNullOrEmpty(text))
                return new List<ColoredText>();
            
            var segments = new List<ColoredText>();
            var colorIndex = 0;
            
            // Determine the color to use for whitespace
            // For title screen templates (yellow/orange), use the first color instead of white
            // This prevents white from appearing in the title text
            Color whitespaceColor = colors.Length > 0 ? colors[0] : Colors.White;
            
            // Check if template contains white - if so, use white for whitespace
            // Otherwise, use the first color from the template
            foreach (var color in colors)
            {
                if (color.R == 255 && color.G == 255 && color.B == 255)
                {
                    whitespaceColor = color;
                    break;
                }
            }
            
            // First pass: create segments for each character
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c))
                {
                    // Use the determined whitespace color (white if template has white, otherwise first color)
                    // IMPORTANT: Whitespace should NOT increment colorIndex to preserve color sequence for non-whitespace chars
                    segments.Add(new ColoredText(c.ToString(), whitespaceColor, sourceTemplate));
                }
                else
                {
                    // Use next color in sequence
                    var color = colors[colorIndex % colors.Length];
                    segments.Add(new ColoredText(c.ToString(), color, sourceTemplate));
                    colorIndex++;
                }
            }
            
            // Second pass: merge consecutive segments with the same color
            // IMPORTANT: Always preserve whitespace as separate segments to prevent character loss
            if (segments.Count == 0)
                return segments;
            
            var merged = new List<ColoredText>();
            ColoredText? currentSegment = null;
            
            foreach (var segment in segments)
            {
                // Skip empty segments (but this should never happen since we create segments for each char)
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                bool isWhitespace = segment.Text.Length == 1 && char.IsWhiteSpace(segment.Text[0]);
                bool currentIsWhitespace = currentSegment != null && 
                                          currentSegment.Text.Length == 1 && 
                                          char.IsWhiteSpace(currentSegment.Text[0]);
                
                if (currentSegment == null)
                {
                    // First segment - always add it
                    currentSegment = new ColoredText(segment.Text, segment.Color, segment.SourceTemplate);
                }
                else if (ColorValidator.AreColorsEqual(currentSegment.Color, segment.Color) && 
                         isWhitespace == currentIsWhitespace)
                {
                    // Same color AND same type (both whitespace or both non-whitespace) - merge
                    currentSegment = new ColoredText(currentSegment.Text + segment.Text, currentSegment.Color, currentSegment.SourceTemplate);
                }
                else
                {
                    // Different color or different type - add current segment and start new one
                    merged.Add(currentSegment);
                    currentSegment = new ColoredText(segment.Text, segment.Color, segment.SourceTemplate);
                }
            }
            
            // Add the last segment (if not empty)
            if (currentSegment != null && !string.IsNullOrEmpty(currentSegment.Text))
            {
                merged.Add(currentSegment);
            }
            
            // Final pass: remove any empty segments that might have been created
            merged.RemoveAll(s => string.IsNullOrEmpty(s.Text));
            
            return merged;
        }
        
        
        /// <summary>
        /// Creates a template with a single color
        /// </summary>
        public static List<ColoredText> SingleColor(string text, Color color, string? sourceTemplate = null)
        {
            return new List<ColoredText> { new ColoredText(text, color, sourceTemplate) };
        }
        
        /// <summary>
        /// Creates a template with a color from the palette
        /// </summary>
        public static List<ColoredText> PaletteColor(string text, ColorPalette palette)
        {
            return new List<ColoredText> { new ColoredText(text, palette.GetColor()) };
        }
        
        /// <summary>
        /// Converts color code strings to Color array
        /// </summary>
        private static Color[] ConvertColorCodesToColors(string[] colorCodes)
        {
            return colorCodes.Select(code => ConvertColorCodeToColor(code)).ToArray();
        }
        
        /// <summary>
        /// Converts a list of color code strings to Color array
        /// </summary>
        private static Color[] ConvertColorCodesToColors(List<string> colorCodes)
        {
            if (colorCodes == null || colorCodes.Count == 0)
                return new[] { Colors.White };
            
            return colorCodes.Select(code => ConvertColorCodeToColor(code)).ToArray();
        }
        
        /// <summary>
        /// Converts a single color code to a Color
        /// Ensures the color is visible on black background
        /// Public helper for use by other systems (e.g., title screen)
        /// </summary>
        public static Color ColorCodeToColor(string colorCode)
        {
            return ConvertColorCodeToColor(colorCode);
        }
        
        /// <summary>
        /// Converts a single color code to a Color
        /// Loads color codes from GameData/ColorCodes.json for efficient lookup
        /// Ensures the color is visible on black background
        /// </summary>
        private static Color ConvertColorCodeToColor(string colorCode)
        {
            if (string.IsNullOrEmpty(colorCode))
                return Colors.White;

            // Use ColorCodeLoader for efficient dictionary lookup
            // This is faster than switch + enum + dictionary lookup
            Color color = ColorCodeLoader.GetColor(colorCode);
            
            // Ensure color is visible on black background
            return ColorValidator.EnsureVisible(color);
        }
        
        /// <summary>
        /// Checks if a template name exists in the library
        /// </summary>
        public static bool HasTemplate(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
                return false;
            
            return ColorTemplateLoader.HasTemplate(templateName);
        }
        
        /// <summary>
        /// Reloads color templates and color codes from disk and clears related caches
        /// Also clears KeywordColorSystem cache so it picks up new template colors
        /// </summary>
        public static void Reload()
        {
            ColorTemplateLoader.Reload();
            ColorCodeLoader.Reload();
            // Clear KeywordColorSystem cache so it picks up new template colors
            KeywordColorSystem.ClearColorPatternCache();
        }
        
        /// <summary>
        /// Gets a representative color from a template (first non-white color, or first color if all are white)
        /// Used by KeywordColorSystem to get a single color for keyword highlighting
        /// </summary>
        public static Color GetRepresentativeColorFromTemplate(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
                return Colors.White;
            
            // Try to load template from JSON
            var templateData = ColorTemplateLoader.GetTemplate(templateName);
            if (templateData == null || templateData.Colors == null || templateData.Colors.Count == 0)
            {
                return Colors.White;
            }
            
            // Convert color codes to Color objects
            var colors = ConvertColorCodesToColors(templateData.Colors);
            if (colors.Length == 0)
            {
                return Colors.White;
            }
            
            // For solid templates, use the first color
            if (templateData.ShaderType?.ToLower() == "solid")
            {
                return ColorValidator.EnsureVisible(colors[0]);
            }
            
            // For sequence/alternation templates, find the first non-white color
            // This gives a better representative color than pure white
            foreach (var color in colors)
            {
                // Skip pure white (255,255,255) and very light colors
                // A color is not white if any RGB component is significantly below 255
                if (!(color.R >= 250 && color.G >= 250 && color.B >= 250))
                {
                    return ColorValidator.EnsureVisible(color);
                }
            }
            
            // If all colors are white/very light, use the first one
            return ColorValidator.EnsureVisible(colors[0]);
        }
        
        /// <summary>
        /// Gets a template by name (case-insensitive)
        /// Loads template data from GameData/ColorTemplates.json
        /// </summary>
        public static List<ColoredText> GetTemplate(string templateName, string text)
        {
            if (string.IsNullOrEmpty(templateName))
                return SingleColor(text, Colors.White, null);
            
            // Try to load template from JSON
            var templateData = ColorTemplateLoader.GetTemplate(templateName);
            if (templateData != null)
            {
                return ApplyTemplate(templateData, text, templateName);
            }
            
            // Fallback to default white if template not found
            return SingleColor(text, Colors.White, null);
        }
        
        /// <summary>
        /// Applies a template data structure to text
        /// </summary>
        private static List<ColoredText> ApplyTemplate(ColorTemplateData templateData, string text, string? templateName = null)
        {
            if (templateData.Colors == null || templateData.Colors.Count == 0)
            {
                return SingleColor(text, Colors.White, templateName);
            }
            
            // Convert color codes to Color objects
            var colors = ConvertColorCodesToColors(templateData.Colors);
            
            // Apply based on shader type
            switch (templateData.ShaderType?.ToLower())
            {
                case "solid":
                    // Use first color for solid
                    if (colors.Length > 0)
                    {
                        return SingleColor(text, colors[0], templateName);
                    }
                    return SingleColor(text, Colors.White, templateName);
                    
                case "sequence":
                case "alternation":
                default:
                    // Both sequence and alternation use the same multi-color logic
                    // (alternation could be enhanced later if needed)
                    return CreateMultiColorText(text, colors, templateName);
            }
        }
    }
}
