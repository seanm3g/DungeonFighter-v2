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
            // Always use ColoredTextParser.Parse() which handles:
            // - Old-style color codes (&X format)
            // - Template syntax ({{template|text}})
            // - New-style markup ([color:pattern]text[/color])
            var coloredText = ColoredTextParser.Parse(message);
            RenderSegments(coloredText, x, y);
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
        /// Maps colors by RGB values to handle ColorPalette colors properly
        /// Ensures colors are visible on black background
        /// </summary>
        private Color ConvertToCanvasColor(Color color)
        {
            // Ensure color is visible on black background before mapping
            color = ColorValidator.EnsureVisible(color);
            
            // Extract RGB values for comparison
            byte r = color.R;
            byte g = color.G;
            byte b = color.B;
            
            // Map common colors by RGB values
            // Basic colors
            if (r == 255 && g == 255 && b == 255) return AsciiArtAssets.Colors.White;
            if (r == 0 && g == 0 && b == 0) return AsciiArtAssets.Colors.Black;
            if (r == 128 && g == 128 && b == 128) return AsciiArtAssets.Colors.Gray;
            if (r == 64 && g == 64 && b == 64) return AsciiArtAssets.Colors.DarkGray;
            
            // Primary colors
            if (r == 255 && g == 0 && b == 0) return AsciiArtAssets.Colors.Red;
            if (r == 0 && g == 255 && b == 0) return AsciiArtAssets.Colors.Green;
            if (r == 0 && g == 0 && b == 255) return AsciiArtAssets.Colors.Blue;
            if (r == 255 && g == 255 && b == 0) return AsciiArtAssets.Colors.Yellow;
            if (r == 0 && g == 255 && b == 255) return AsciiArtAssets.Colors.Cyan;
            if (r == 255 && g == 0 && b == 255) return AsciiArtAssets.Colors.Magenta;
            
            // Dark variants
            if (r == 139 && g == 0 && b == 0) return AsciiArtAssets.Colors.DarkRed;
            if (r == 0 && g == 100 && b == 0) return AsciiArtAssets.Colors.DarkGreen;
            if (r == 0 && g == 0 && b == 139) return AsciiArtAssets.Colors.DarkBlue;
            
            // Game-specific colors
            // Gold: #cfc041 (207, 192, 65) per template rules
            if (r == 207 && g == 192 && b == 65) return AsciiArtAssets.Colors.Gold;
            // Also support standard gold (255, 215, 0) for backwards compatibility
            if (r == 255 && g == 215 && b == 0) return AsciiArtAssets.Colors.Gold;
            if (r == 192 && g == 192 && b == 192) return AsciiArtAssets.Colors.Silver;
            if (r == 255 && g == 165 && b == 0) return AsciiArtAssets.Colors.Orange;
            if (r == 128 && g == 0 && b == 128) return AsciiArtAssets.Colors.Purple;
            
            // Combat colors - map to closest available color
            // Damage (220, 20, 60) - crimson red, map to Red
            if (r == 220 && g == 20 && b == 60) return AsciiArtAssets.Colors.Red;
            // Critical (255, 0, 0) - pure red
            if (r == 255 && g == 0 && b == 0) return AsciiArtAssets.Colors.Red;
            // Miss (128, 128, 128) - gray
            if (r == 128 && g == 128 && b == 128) return AsciiArtAssets.Colors.Gray;
            // Healing (0, 255, 127) - spring green, map to Green
            if (r == 0 && g == 255 && b == 127) return AsciiArtAssets.Colors.Green;
            
            // Status colors
            // Success (0, 128, 0) - dark green, map to DarkGreen
            if (r == 0 && g == 128 && b == 0) return AsciiArtAssets.Colors.DarkGreen;
            // Warning (255, 165, 0) - orange
            if (r == 255 && g == 165 && b == 0) return AsciiArtAssets.Colors.Orange;
            // Error (220, 20, 60) - crimson, map to Red
            if (r == 220 && g == 20 && b == 60) return AsciiArtAssets.Colors.Red;
            // Info (0, 191, 255) - deep sky blue, map to Cyan
            if (r == 0 && g == 191 && b == 255) return AsciiArtAssets.Colors.Cyan;
            
            // Actor colors
            // Player (0, 255, 255) - cyan
            if (r == 0 && g == 255 && b == 255) return AsciiArtAssets.Colors.Cyan;
            // Enemy (255, 0, 0) - red
            if (r == 255 && g == 0 && b == 0) return AsciiArtAssets.Colors.Red;
            
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
            
            // Preserve leading spaces (important for indented text like roll info)
            string leadingSpaces = "";
            int leadingSpaceCount = 0;
            for (int i = 0; i < text.Length && char.IsWhiteSpace(text[i]); i++)
            {
                leadingSpaces += text[i];
                leadingSpaceCount++;
            }
            
            // Get the text without leading spaces for word splitting
            string textWithoutLeading = text.Substring(leadingSpaceCount);
            
            // If the entire text is just whitespace, return it as-is
            if (string.IsNullOrEmpty(textWithoutLeading))
            {
                result.Add(text);
                return result;
            }
            
            var words = textWithoutLeading.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var currentLine = leadingSpaces; // Start with leading spaces
            
            foreach (var word in words)
            {
                var testLine = currentLine == leadingSpaces ? $"{currentLine}{word}" : $"{currentLine} {word}";
                
                if (testLine.Length <= maxWidth)
                {
                    currentLine = testLine;
                }
                else
                {
                    // Only add line if it has content beyond just leading spaces
                    if (!string.IsNullOrEmpty(currentLine.Trim()))
                    {
                        result.Add(currentLine);
                    }
                    // Start new line with leading spaces preserved
                    currentLine = leadingSpaces + word;
                }
            }
            
            // Add the last line if it has content
            if (!string.IsNullOrEmpty(currentLine.Trim()))
            {
                result.Add(currentLine);
            }
            
            return result;
        }
        
        /// <summary>
        /// Writes colored text with word wrapping
        /// Handles newlines in the message by splitting on newlines first, then wrapping each line
        /// </summary>
        /// <returns>Number of lines written</returns>
        public int WriteLineColoredWrapped(string message, int x, int y, int maxWidth)
        {
            if (string.IsNullOrEmpty(message))
                return 0;
            
            // First, split on newlines to preserve line structure (e.g., for roll info indentation)
            var newlineSplit = message.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            int currentY = y;
            int totalLines = 0;

            foreach (var line in newlineSplit)
            {
                // Wrap each line separately to preserve indentation
                var wrappedLines = WrapText(line, maxWidth);
                foreach (var wrappedLine in wrappedLines)
                {
                    WriteLineColored(wrappedLine, x, currentY);
                    currentY++;
                    totalLines++;
                }
            }
            
            return totalLines;
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