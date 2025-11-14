using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Library of color templates for different themes and effects
    /// </summary>
    public static class ColorTemplateLibrary
    {
        /// <summary>
        /// Creates fiery-themed colored text
        /// </summary>
        public static List<ColoredText> Fiery(string text)
        {
            var colors = new[] { ColorPalette.Orange.GetColor(), ColorPalette.Red.GetColor(), ColorPalette.Yellow.GetColor() };
            return CreateMultiColorText(text, colors);
        }
        
        /// <summary>
        /// Creates icy-themed colored text
        /// </summary>
        public static List<ColoredText> Icy(string text)
        {
            var colors = new[] { ColorPalette.Cyan.GetColor(), ColorPalette.Blue.GetColor(), ColorPalette.White.GetColor() };
            return CreateMultiColorText(text, colors);
        }
        
        /// <summary>
        /// Creates toxic-themed colored text
        /// </summary>
        public static List<ColoredText> Toxic(string text)
        {
            var colors = new[] { ColorPalette.Green.GetColor(), ColorPalette.Lime.GetColor(), ColorPalette.Yellow.GetColor() };
            return CreateMultiColorText(text, colors);
        }
        
        /// <summary>
        /// Creates crystalline-themed colored text
        /// </summary>
        public static List<ColoredText> Crystalline(string text)
        {
            var colors = new[] { ColorPalette.Silver.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Cyan.GetColor() };
            return CreateMultiColorText(text, colors);
        }
        
        /// <summary>
        /// Creates golden-themed colored text
        /// </summary>
        public static List<ColoredText> Golden(string text)
        {
            var colors = new[] { ColorPalette.Gold.GetColor(), ColorPalette.Yellow.GetColor(), ColorPalette.Orange.GetColor() };
            return CreateMultiColorText(text, colors);
        }
        
        /// <summary>
        /// Creates holy-themed colored text
        /// </summary>
        public static List<ColoredText> Holy(string text)
        {
            var colors = new[] { ColorPalette.White.GetColor(), ColorPalette.Gold.GetColor(), ColorPalette.Yellow.GetColor() };
            return CreateMultiColorText(text, colors);
        }
        
        /// <summary>
        /// Creates shadow-themed colored text
        /// </summary>
        public static List<ColoredText> Shadow(string text)
        {
            var colors = new[] { ColorPalette.DarkGray.GetColor(), ColorPalette.Black.GetColor(), ColorPalette.Purple.GetColor() };
            return CreateMultiColorText(text, colors);
        }
        
        /// <summary>
        /// Creates electric-themed colored text
        /// </summary>
        public static List<ColoredText> Electric(string text)
        {
            var colors = new[] { ColorPalette.Cyan.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Blue.GetColor() };
            return CreateMultiColorText(text, colors);
        }
        
        /// <summary>
        /// Creates multi-color text by alternating colors for each character
        /// </summary>
        private static List<ColoredText> CreateMultiColorText(string text, Color[] colors)
        {
            if (string.IsNullOrEmpty(text))
                return new List<ColoredText>();
            
            var result = new List<ColoredText>();
            var colorIndex = 0;
            
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c))
                {
                    // Keep whitespace in default color
                    result.Add(new ColoredText(c.ToString(), Colors.White));
                }
                else
                {
                    // Use next color in sequence
                    var color = colors[colorIndex % colors.Length];
                    result.Add(new ColoredText(c.ToString(), color));
                    colorIndex++;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Creates a template with a single color
        /// </summary>
        public static List<ColoredText> SingleColor(string text, Color color)
        {
            return new List<ColoredText> { new ColoredText(text, color) };
        }
        
        /// <summary>
        /// Creates a template with a color from the palette
        /// </summary>
        public static List<ColoredText> PaletteColor(string text, ColorPalette palette)
        {
            return new List<ColoredText> { new ColoredText(text, palette.GetColor()) };
        }
        
        /// <summary>
        /// Checks if a template name exists in the library
        /// </summary>
        public static bool HasTemplate(string templateName)
        {
            return templateName.ToLower() switch
            {
                "fiery" => true,
                "icy" => true,
                "toxic" => true,
                "crystalline" => true,
                "golden" => true,
                "holy" => true,
                "shadow" => true,
                "electric" => true,
                _ => false
            };
        }
    }
}
