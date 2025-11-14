using Avalonia.Media;
using RPGGame.UI.ColorSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Handles colored text rendering with the new color system
    /// </summary>
    public class ColoredTextWriter
    {
        private readonly GameCanvasControl canvas;
        
        public ColoredTextWriter(GameCanvasControl canvas)
        {
            this.canvas = canvas;
        }
        
        /// <summary>
        /// Adds colored text to canvas using the new color system
        /// </summary>
        public void WriteLineColored(string message, int x, int y)
        {
            // Check if message has old-style color markup (&X format)
            if (CompatibilityLayer.HasColorMarkup(message))
            {
                // Use old-style parser for &X format
                var coloredText = CompatibilityLayer.ConvertOldMarkup(message);
                RenderSegments(coloredText, x, y);
            }
            else
            {
                // Parse the message using the new color system
                var coloredText = ColoredTextParser.Parse(message);
                RenderSegments(coloredText, x, y);
            }
        }
        
        /// <summary>
        /// Adds colored text to canvas using ColoredText object
        /// </summary>
        public void WriteLineColored(ColoredText coloredText, int x, int y)
        {
            var segments = new List<ColoredText> { coloredText };
            RenderSegments(segments, x, y);
        }
        
        /// <summary>
        /// Renders a list of colored text segments to the canvas
        /// </summary>
        public void RenderSegments(List<ColoredText> segments, int x, int y)
        {
            if (segments == null || segments.Count == 0)
                return;
            
            int currentX = x;
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                // Convert Avalonia color to the canvas color format
                var canvasColor = ConvertToCanvasColor(segment.Color);
                
                // Add text to canvas
                canvas.AddText(currentX, y, segment.Text, canvasColor);
                
                // Calculate next position (simple character-based positioning)
                currentX += segment.Text.Length;
            }
        }
        
        /// <summary>
        /// Renders colored text with word-based positioning
        /// </summary>
        public void RenderSegmentsWithWordPositioning(List<ColoredText> segments, int x, int y)
        {
            if (segments == null || segments.Count == 0)
                return;
            
            int currentX = x;
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                // Convert Avalonia color to the canvas color format
                var canvasColor = ConvertToCanvasColor(segment.Color);
                
                // Add text to canvas
                canvas.AddText(currentX, y, segment.Text, canvasColor);
                
                // Calculate next position using measured width
                currentX += MeasureTextWidth(segment.Text);
            }
        }
        
        /// <summary>
        /// Renders colored text with character-based positioning (for title screen)
        /// </summary>
        public void RenderSegmentsWithCharacterPositioning(List<ColoredText> segments, int x, int y)
        {
            if (segments == null || segments.Count == 0)
                return;
            
            int currentX = x;
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                // Convert Avalonia color to the canvas color format
                var canvasColor = ConvertToCanvasColor(segment.Color);
                
                // Add text to canvas
                canvas.AddText(currentX, y, segment.Text, canvasColor);
                
                // Calculate next position (character-based)
                currentX += segment.Text.Length;
            }
        }
        
        /// <summary>
        /// Converts Avalonia color to canvas color format
        /// </summary>
        private Color ConvertToCanvasColor(Color color)
        {
            // Simple color mapping - you may need to adjust this based on your canvas color system
            if (color == Colors.Red) return AsciiArtAssets.Colors.Red;
            if (color == Colors.Green) return AsciiArtAssets.Colors.Green;
            if (color == Colors.Blue) return AsciiArtAssets.Colors.Blue;
            if (color == Colors.Yellow) return AsciiArtAssets.Colors.Yellow;
            if (color == Colors.Cyan) return AsciiArtAssets.Colors.Cyan;
            if (color == Colors.Magenta) return AsciiArtAssets.Colors.Magenta;
            if (color == Colors.White) return AsciiArtAssets.Colors.White;
            if (color == Colors.Black) return AsciiArtAssets.Colors.Black;
            if (color == Colors.Gray) return AsciiArtAssets.Colors.Gray;
            
            // Default to white for unknown colors
            return AsciiArtAssets.Colors.White;
        }
        
        /// <summary>
        /// Measures text width (placeholder implementation)
        /// </summary>
        private int MeasureTextWidth(string text)
        {
            // This is a placeholder - you may need to implement proper text measurement
            // based on your canvas system
            return text.Length;
        }
        
        /// <summary>
        /// Writes plain text (no color)
        /// </summary>
        public void WriteLine(string message, int x, int y)
        {
            canvas.AddText(x, y, message, AsciiArtAssets.Colors.White);
        }
        
        /// <summary>
        /// Writes text with a specific color
        /// </summary>
        public void WriteLine(string message, int x, int y, Color color)
        {
            var canvasColor = ConvertToCanvasColor(color);
            canvas.AddText(x, y, message, canvasColor);
        }
        
        /// <summary>
        /// Writes text with a color palette
        /// </summary>
        public void WriteLine(string message, int x, int y, ColorPalette color)
        {
            var canvasColor = ConvertToCanvasColor(color.GetColor());
            canvas.AddText(x, y, message, canvasColor);
        }
        
        /// <summary>
        /// Writes text with a pattern
        /// </summary>
        public void WriteLineWithPattern(string message, int x, int y, string pattern)
        {
            var color = ColorPatterns.GetColorForPattern(pattern);
            var canvasColor = ConvertToCanvasColor(color);
            canvas.AddText(x, y, message, canvasColor);
        }
        
        /// <summary>
        /// Wraps text to fit within a specified width
        /// </summary>
        public List<string> WrapText(string text, int maxWidth)
        {
            var result = new List<string>();
            
            if (string.IsNullOrEmpty(text))
                return result;
            
            var words = text.Split(' ');
            var currentLine = "";
            
            foreach (var word in words)
            {
                var testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
                
                if (testLine.Length <= maxWidth)
                {
                    currentLine = testLine;
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        result.Add(currentLine);
                    }
                    currentLine = word;
                }
            }
            
            if (!string.IsNullOrEmpty(currentLine))
            {
                result.Add(currentLine);
            }
            
            return result;
        }
        
        /// <summary>
        /// Writes colored text with word wrapping
        /// </summary>
        /// <returns>Number of lines written</returns>
        public int WriteLineColoredWrapped(string message, int x, int y, int maxWidth)
        {
            var lines = WrapText(message, maxWidth);
            int currentY = y;

            foreach (var line in lines)
            {
                WriteLineColored(line, x, currentY);
                currentY++;
            }
            
            return lines.Count;
        }

        /// <summary>
        /// Writes colored text with word wrapping using List of ColoredText
        /// </summary>
        /// <returns>Number of lines written</returns>
        public int WriteLineColoredWrapped(List<ColoredText> segments, int x, int y, int maxWidth)
        {
            // Combine segments into plain text for wrapping
            var plainText = string.Join("", segments.Select(s => s.Text));
            var lines = WrapText(plainText, maxWidth);

            int currentY = y;
            foreach (var line in lines)
            {
                WriteLineColored(line, x, currentY);
                currentY++;
            }
            
            return lines.Count;
        }
    }
}