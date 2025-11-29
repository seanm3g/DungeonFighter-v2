using Avalonia.Media;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Avalonia.Renderers.SegmentRenderers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Handles colored text rendering with the new color system.
    /// Uses Strategy pattern for different rendering approaches (template vs standard).
    /// </summary>
    public class ColoredTextWriter
    {
        private readonly GameCanvasControl canvas;
        private readonly ISegmentRenderer templateRenderer;
        private readonly ISegmentRenderer standardRenderer;
        
        public ColoredTextWriter(GameCanvasControl canvas)
        {
            this.canvas = canvas;
            this.templateRenderer = new TemplateSegmentRenderer(canvas);
            this.standardRenderer = new StandardSegmentRenderer(canvas);
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
        /// </summary>
        public void RenderSegments(List<ColoredText> segments, int x, int y)
        {
            if (segments == null || segments.Count == 0)
                return;
            
            // Select appropriate renderer using Strategy pattern
            ISegmentRenderer renderer = templateRenderer.ShouldUseRenderer(segments) 
                ? templateRenderer 
                : standardRenderer;
            
            int currentX = x;
            int lastRenderedX = int.MinValue;
            Color? lastColor = null;
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                var canvasColor = ConvertToCanvasColor(segment.Color);
                int renderedX = int.MinValue;
                
                // Use selected renderer to render segment
                currentX = renderer.RenderSegment(segment, canvasColor, currentX, 
                    lastRenderedX, lastColor, y, ref renderedX);
                
                if (renderedX != int.MinValue)
                {
                    lastRenderedX = renderedX;
                }
                lastColor = canvasColor;
            }
        }
        
        /// <summary>
        /// Converts Avalonia color to canvas color format
        /// 
        /// This method does NOT store or hardcode color values. It relies entirely on the
        /// color configuration system (ColorPalette.json, ColorCodes.json) which are loaded
        /// dynamically. Colors passed to this method should already be resolved through:
        /// - ColorPalette.GetColor() - loads from ColorPalette.json
        /// - ColorCodeLoader.GetColor() - loads from ColorCodes.json
        /// - ColorPatterns.GetColorForPattern() - uses pattern-based mapping
        /// 
        /// The only processing done here is ensuring visibility on black background.
        /// The canvas supports any RGB color value directly, so no mapping is needed.
        /// </summary>
        private Color ConvertToCanvasColor(Color color)
        {
            // Ensure color is visible on black background, then return as-is
            // All color values come from the JSON configuration system, not hardcoded here
            // The canvas can render any RGB color value directly
            return ColorValidator.EnsureVisible(color);
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
        /// </summary>
        private List<List<ColoredText>> WrapColoredSegments(List<ColoredText> segments, int maxWidth)
        {
            var wrappedLines = new List<List<ColoredText>>();
            var currentLine = new List<ColoredText>();
            int currentLineWidth = 0;
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                // Measure segment width
                int segmentWidth = (int)canvas.MeasureTextWidth(segment.Text);
                
                // Check if adding this segment would exceed max width
                if (currentLineWidth + segmentWidth > maxWidth && currentLine.Count > 0)
                {
                    // Current line is full, start a new line
                    wrappedLines.Add(currentLine);
                    currentLine = new List<ColoredText>();
                    currentLineWidth = 0;
                }
                
                // If segment itself is too long, split it
                if (segmentWidth > maxWidth)
                {
                    // Split the segment text and create new segments with same color
                    var words = segment.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in words)
                    {
                        int wordWidth = (int)canvas.MeasureTextWidth(word);
                        int spaceWidth = (int)canvas.MeasureTextWidth(" ");
                        
                        // Check if word fits on current line
                        if (currentLineWidth + wordWidth > maxWidth && currentLine.Count > 0)
                        {
                            wrappedLines.Add(currentLine);
                            currentLine = new List<ColoredText>();
                            currentLineWidth = 0;
                        }
                        
                        // Add space before word if not first on line
                        if (currentLine.Count > 0 && currentLineWidth > 0)
                        {
                            currentLine.Add(new ColoredText(" ", segment.Color));
                            currentLineWidth += spaceWidth;
                        }
                        
                        // Add word
                        currentLine.Add(new ColoredText(word, segment.Color));
                        currentLineWidth += wordWidth;
                    }
                }
                else
                {
                    // Segment fits, add it to current line
                    currentLine.Add(segment);
                    currentLineWidth += segmentWidth;
                }
            }
            
            // Add the last line if it has content
            if (currentLine.Count > 0)
            {
                wrappedLines.Add(currentLine);
            }
            
            // If no lines were created, add an empty line
            if (wrappedLines.Count == 0)
            {
                wrappedLines.Add(new List<ColoredText>());
            }
            
            return wrappedLines;
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