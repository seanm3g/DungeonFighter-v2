using Avalonia.Media;
using RPGGame.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Handles colored text rendering with color markup support and text wrapping
    /// </summary>
    public class ColoredTextWriter
    {
        private readonly GameCanvasControl canvas;
        
        public ColoredTextWriter(GameCanvasControl canvas)
        {
            this.canvas = canvas;
        }
        
        /// <summary>
        /// Adds colored text to canvas with color markup support
        /// Uses simple character positioning for per-character coloring (title screen)
        /// Uses measured widths for word-based coloring (combat text)
        /// </summary>
        public void WriteLineColored(string message, int x, int y)
        {
            if (ColorParser.HasColorMarkup(message))
            {
                var segments = ColorParser.Parse(message);
                RenderSegments(segments, x, y);
            }
            else
            {
                canvas.AddText(x, y, message, AsciiArtAssets.Colors.White);
            }
        }
        
        /// <summary>
        /// Adds colored text to canvas using ColoredText object (pattern applied at render time)
        /// This is the preferred method as it avoids parsing issues
        /// </summary>
        public void WriteLineColored(ColoredText coloredText, int x, int y)
        {
            var segments = coloredText.GetSegments();
            RenderSegments(segments, x, y);
        }
        
        /// <summary>
        /// Renders a list of segments to the canvas
        /// This is public for direct segment rendering (avoids string conversion)
        /// </summary>
        public void RenderSegments(List<ColorDefinitions.ColoredSegment> segments, int x, int y)
        {
            // Check if this should use simple character positioning (title screen)
            // This applies to:
            // 1. Old per-character coloring (many tiny segments)
            // 2. New efficient title screen lines (few segments, but very long with block chars)
            // 3. Sequence/Alternation templates that create per-character segments (even if mixed with longer segments)
            bool hasBlockCharacters = segments.Any(s => s.Text != null && 
                s.Text.Contains("█") && s.Text.Length > 20);
            
            // Check if MOST segments are single characters (>=70% are length 1-2)
            int singleCharSegments = segments.Count(s => !string.IsNullOrEmpty(s.Text) && s.Text.Length <= 2);
            bool isPerCharacterColoring = segments.Count >= 5 && 
                (singleCharSegments >= segments.Count * 0.7);
            
            if (isPerCharacterColoring || hasBlockCharacters)
            {
                // Simple character-by-character positioning (title screen)
                // Avoids fractional error accumulation across hundreds of segments
                int charPosition = 0;
                foreach (var segment in segments)
                {
                    if (!string.IsNullOrEmpty(segment.Text))
                    {
                        // IMPORTANT: Even whitespace needs a color to render properly
                        Color color = segment.Foreground.HasValue 
                            ? segment.Foreground.Value.ToAvaloniaColor() 
                            : AsciiArtAssets.Colors.White;
                        
                        canvas.AddText(x + charPosition, y, segment.Text, color);
                        charPosition += segment.Text.Length;
                    }
                }
            }
            else
            {
                // Use simple character position tracking for word-based coloring (combat text)
                // Monospace font means we can just track character count
                int charPosition = 0;
                foreach (var segment in segments)
                {
                    if (!string.IsNullOrEmpty(segment.Text))
                    {
                        Color color = segment.Foreground.HasValue 
                            ? segment.Foreground.Value.ToAvaloniaColor() 
                            : AsciiArtAssets.Colors.White;
                        
                        canvas.AddText(x + charPosition, y, segment.Text, color);
                        charPosition += segment.Text.Length;
                    }
                }
            }
        }
        
        /// <summary>
        /// Adds colored text to canvas with color markup support and text wrapping
        /// Returns the number of lines rendered
        /// </summary>
        public int WriteLineColoredWrapped(string message, int x, int y, int maxWidth)
        {
            // Wrap the text first (preserving color markup)
            var wrappedLines = WrapText(message, maxWidth);
            
            // Render each wrapped line
            int currentY = y;
            foreach (var line in wrappedLines)
            {
                WriteLineColored(line, x, y + (currentY - y));
                currentY++;
            }
            
            return wrappedLines.Count;
        }
        
        /// <summary>
        /// Adds colored text to canvas with text wrapping using ColoredText object
        /// Returns the number of lines rendered
        /// </summary>
        public int WriteLineColoredWrapped(ColoredText coloredText, int x, int y, int maxWidth)
        {
            // For ColoredText, we wrap the plain text and apply color to each line
            var wrappedLines = WrapText(coloredText.Text, maxWidth);
            
            int currentY = y;
            foreach (var line in wrappedLines)
            {
                // Create a new ColoredText with the wrapped line but same color pattern
                var wrappedColoredText = new ColoredText(line, coloredText.Template, coloredText.SimpleColorCode);
                WriteLineColored(wrappedColoredText, x, y + (currentY - y));
                currentY++;
            }
            
            return wrappedLines.Count;
        }
        
        /// <summary>
        /// Wraps text to fit within maxWidth, preserving leading whitespace (indentation)
        /// </summary>
        public List<string> WrapText(string text, int maxWidth)
        {
            var lines = new List<string>();
            
            // Preserve leading whitespace (important for combat log formatting)
            int leadingSpaces = text.Length - text.TrimStart().Length;
            string indentation = text.Substring(0, leadingSpaces);
            string trimmedText = text.TrimStart();
            
            // If the line is entirely whitespace, just return it
            if (string.IsNullOrWhiteSpace(trimmedText))
            {
                lines.Add(text);
                return lines;
            }
            
            // For indented text (status messages), use a minimal continuation indent
            // to show it's part of the same message but avoid excessive nesting
            string continuationIndent = leadingSpaces > 0 ? "  " : "";
            
            // Split by spaces but keep color markup together as single units
            var words = SplitPreservingMarkup(trimmedText);
            
            // Pre-calculate word lengths once to avoid repeated regex operations
            // This significantly improves performance when wrapping long text blocks
            var wordLengths = new int[words.Count];
            for (int i = 0; i < words.Count; i++)
            {
                wordLengths[i] = ColorParser.GetDisplayLength(words[i]);
            }
            
            // Pre-calculate indent lengths
            int indentationLength = ColorParser.GetDisplayLength(indentation);
            int continuationIndentLength = ColorParser.GetDisplayLength(continuationIndent);
            
            var currentLine = "";
            int currentLineLength = 0; // Track visible length of current line
            bool isFirstLine = true;
            
            for (int i = 0; i < words.Count; i++)
            {
                string word = words[i];
                
                // Skip empty words from multiple spaces
                if (string.IsNullOrEmpty(word)) continue;
                
                int wordDisplayLength = wordLengths[i];
                int currentIndentLength = isFirstLine ? indentationLength : continuationIndentLength;
                
                // Check if adding this word would exceed max width
                // Use currentLineLength (visible length) instead of currentLine.Length (which includes markup)
                int spaceNeeded = currentLineLength > 0 ? 1 : 0; // Add 1 for space if not first word
                int totalLength = currentIndentLength + currentLineLength + spaceNeeded + wordDisplayLength;
                
                if (totalLength <= maxWidth)
                {
                    // Use currentLineLength instead of currentLine.Length to check if space is needed
                    currentLine += (currentLineLength > 0 ? " " : "") + word;
                    currentLineLength += spaceNeeded + wordDisplayLength;
                }
                else
                {
                    // Use currentLineLength instead of currentLine.Length
                    if (currentLineLength > 0)
                    {
                        // First line gets original indentation, continuation lines get minimal indent
                        lines.Add(isFirstLine ? indentation + currentLine : continuationIndent + currentLine);
                        isFirstLine = false;
                    }
                    currentLine = word;
                    currentLineLength = wordDisplayLength;
                }
            }
            
            // Use currentLineLength instead of currentLine.Length
            if (currentLineLength > 0)
            {
                lines.Add(isFirstLine ? indentation + currentLine : continuationIndent + currentLine);
            }
            
            return lines;
        }
        
        /// <summary>
        /// Splits text by spaces while keeping color markup templates together
        /// </summary>
        private List<string> SplitPreservingMarkup(string text)
        {
            var words = new List<string>();
            var currentWord = "";
            int templateDepth = 0;
            
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                
                // Track template markup depth
                if (i < text.Length - 1 && c == '{' && text[i + 1] == '{')
                {
                    templateDepth++;
                    currentWord += "{{";
                    i++; // Skip next '{'
                    continue;
                }
                else if (i < text.Length - 1 && c == '}' && text[i + 1] == '}')
                {
                    templateDepth--;
                    currentWord += "}}";
                    i++; // Skip next '}'
                    continue;
                }
                
                // Split on space only if not inside a template
                if (c == ' ' && templateDepth == 0)
                {
                    if (currentWord.Length > 0)
                    {
                        words.Add(currentWord);
                        currentWord = "";
                    }
                }
                else
                {
                    currentWord += c;
                }
            }
            
            // Add the last word
            if (currentWord.Length > 0)
            {
                words.Add(currentWord);
            }
            
            return words;
        }
    }
}

