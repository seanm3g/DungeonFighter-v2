using Avalonia.Media;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Avalonia.Renderers.Text;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Handles colored text rendering with the new color system.
    /// Facade coordinator that delegates to specialized text rendering components.
    /// </summary>
    public class ColoredTextWriter
    {
        private readonly GameCanvasControl canvas;
        private readonly SegmentRenderer segmentRenderer;
        
        public ColoredTextWriter(GameCanvasControl canvas)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.segmentRenderer = new SegmentRenderer(canvas);
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
        /// Renders a list of colored text segments to the canvas.
        /// Uses Strategy pattern to select appropriate renderer (template vs standard).
        /// Enhanced with overlap detection and validation.
        /// </summary>
        public void RenderSegments(List<ColoredText> segments, int x, int y)
        {
            segmentRenderer.RenderSegments(segments, x, y, ColorConverter.ConvertToCanvasColor);
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
            var canvasColor = ColorConverter.ConvertToCanvasColor(color);
            canvas.AddText(x, y, message, canvasColor);
        }
        
        /// <summary>
        /// Writes text with a color palette
        /// </summary>
        public void WriteLine(string message, int x, int y, ColorPalette color)
        {
            var canvasColor = ColorConverter.ConvertToCanvasColor(color.GetColor());
            canvas.AddText(x, y, message, canvasColor);
        }
        
        /// <summary>
        /// Writes text with a pattern
        /// </summary>
        public void WriteLineWithPattern(string message, int x, int y, string pattern)
        {
            var color = ColorPatterns.GetColorForPattern(pattern);
            var canvasColor = ColorConverter.ConvertToCanvasColor(color);
            canvas.AddText(x, y, message, canvasColor);
        }
        
        /// <summary>
        /// Wraps text to fit within a specified width
        /// </summary>
        public List<string> WrapText(string text, int maxWidth)
        {
            return TextWrappingHelper.WrapText(text, maxWidth);
        }
        
        /// <summary>
        /// Writes colored text with word wrapping
        /// Handles newlines in the message by splitting on newlines first, then wrapping each line
        /// </summary>
        /// <returns>Number of lines written</returns>
        public int WriteLineColoredWrapped(string message, int x, int y, int maxWidth)
        {
            if (string.IsNullOrEmpty(message))
                return 1; // Return 1 for blank lines to preserve spacing between sections
            
            // First, split on newlines to preserve line structure (e.g., for roll info indentation)
            var newlineSplit = message.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            int currentY = y;
            int totalLines = 0;

            foreach (var line in newlineSplit)
            {
                // Wrap each line separately to preserve indentation
                var wrappedLines = WrapText(line, maxWidth);
                
                // If the line is empty, wrappedLines will be empty, but we still need to advance Y
                // to preserve blank line spacing between sections
                if (wrappedLines.Count == 0)
                {
                    currentY++;
                    totalLines++;
                }
                else
                {
                    foreach (var wrappedLine in wrappedLines)
                    {
                        WriteLineColored(wrappedLine, x, currentY);
                        currentY++;
                        totalLines++;
                    }
                }
            }
            
            return totalLines;
        }

        /// <summary>
        /// Writes colored text with word wrapping using List of ColoredText
        /// Preserves colors while wrapping text across multiple lines
        /// </summary>
        /// <returns>Number of lines written</returns>
        public int WriteLineColoredWrapped(List<ColoredText> segments, int x, int y, int maxWidth)
        {
            if (segments == null || segments.Count == 0)
                return 1; // Return 1 for blank lines to preserve spacing
            
            // Wrap segments while preserving colors
            var wrappedLines = WrapColoredSegments(segments, maxWidth);
            
            int currentY = y;
            foreach (var lineSegments in wrappedLines)
            {
                if (lineSegments != null && lineSegments.Count > 0)
                {
                    RenderSegments(lineSegments, x, currentY);
                }
                currentY++;
            }
            
            return wrappedLines.Count;
        }
        
        /// <summary>
        /// Wraps ColoredText segments while preserving their colors
        /// Handles newlines within segments by splitting them first
        /// </summary>
        public List<List<ColoredText>> WrapColoredSegments(List<ColoredText> segments, int maxWidth)
        {
            return TextWrappingHelper.WrapColoredSegments(segments, maxWidth);
        }
        
        /// <summary>
        /// Clears text elements within a specific Y range (inclusive)
        /// Clears ALL text in the Y range across the entire canvas width
        /// This is the standard method for clearing panels/areas - always clears full width
        /// </summary>
        public void ClearTextInRange(int startY, int endY)
        {
            canvas.ClearTextInRange(startY, endY);
        }
        
        /// <summary>
        /// Clears text elements within a specific rectangular area (inclusive)
        /// Use this only when you need to clear a specific rectangular region (e.g., center panel only)
        /// For clearing full-width panels, use ClearTextInRange() instead
        /// </summary>
        public void ClearTextInArea(int startX, int startY, int width, int height)
        {
            canvas.ClearTextInArea(startX, startY, width, height);
        }
    }
}