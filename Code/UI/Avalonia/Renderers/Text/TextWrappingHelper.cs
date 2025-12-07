using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Renderers.Text
{

    /// <summary>
    /// Handles text wrapping logic for both plain text and colored text segments.
    /// </summary>
    public class TextWrappingHelper
    {
        /// <summary>
        /// Wraps text to fit within a specified width
        /// </summary>
        public static List<string> WrapText(string text, int maxWidth)
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
        /// Wraps ColoredText segments while preserving their colors
        /// Handles newlines within segments by splitting them first
        /// </summary>
        public static List<List<ColoredText>> WrapColoredSegments(List<ColoredText> segments, int maxWidth)
        {
            var wrappedLines = new List<List<ColoredText>>();
            var currentLine = new List<ColoredText>();
            int currentLineWidth = 0;
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                // First, check if segment contains newlines - if so, split on newlines first
                if (segment.Text.Contains("\r\n") || segment.Text.Contains("\n") || segment.Text.Contains("\r"))
                {
                    // Split on newlines to preserve line breaks
                    var newlineSplit = segment.Text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
                    
                    for (int i = 0; i < newlineSplit.Length; i++)
                    {
                        var part = newlineSplit[i];
                        
                        // If this is not the first part, finish the current line and start a new one
                        if (i > 0)
                        {
                            if (currentLine.Count > 0)
                            {
                                wrappedLines.Add(currentLine);
                                currentLine = new List<ColoredText>();
                                currentLineWidth = 0;
                            }
                        }
                        
                        // Process this part (which may still need word wrapping)
                        if (!string.IsNullOrEmpty(part))
                        {
                            ProcessSegmentPart(part, segment.Color, ref currentLine, ref currentLineWidth, maxWidth, ref wrappedLines);
                        }
                    }
                    
                    continue;
                }
                
                // Use character count for width calculation (monospace font)
                int segmentCharWidth = segment.Text.Length;
                
                // Check if adding this segment would exceed max width
                if (currentLineWidth + segmentCharWidth > maxWidth && currentLine.Count > 0)
                {
                    // Current line is full, start a new line
                    wrappedLines.Add(currentLine);
                    currentLine = new List<ColoredText>();
                    currentLineWidth = 0;
                }
                
                // If segment itself is too long, split it
                if (segmentCharWidth > maxWidth)
                {
                    // Split the segment text and create new segments with same color
                    var words = segment.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in words)
                    {
                        int wordCharWidth = word.Length;
                        int spaceCharWidth = 1; // Space is 1 character
                        
                        // Check if word fits on current line (use character count)
                        if (currentLineWidth + wordCharWidth > maxWidth && currentLine.Count > 0)
                        {
                            wrappedLines.Add(currentLine);
                            currentLine = new List<ColoredText>();
                            currentLineWidth = 0;
                        }
                        
                        // Add space before word if not first on line
                        if (currentLine.Count > 0 && currentLineWidth > 0)
                        {
                            currentLine.Add(new ColoredText(" ", segment.Color));
                            currentLineWidth += spaceCharWidth;
                        }
                        
                        // Add word
                        currentLine.Add(new ColoredText(word, segment.Color));
                        currentLineWidth += wordCharWidth;
                    }
                }
                else
                {
                    // Segment fits, add it to current line
                    currentLine.Add(segment);
                    currentLineWidth += segmentCharWidth;
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
        /// Helper method to process a segment part (after splitting on newlines)
        /// Handles word wrapping for the part
        /// </summary>
        private static void ProcessSegmentPart(string part, global::Avalonia.Media.Color color, ref List<ColoredText> currentLine, ref int currentLineWidth, int maxWidth, ref List<List<ColoredText>> wrappedLines)
        {
            int partCharWidth = part.Length;
            
            // If part fits on current line, add it
            if (currentLineWidth + partCharWidth <= maxWidth)
            {
                currentLine.Add(new ColoredText(part, color));
                currentLineWidth += partCharWidth;
            }
            else if (partCharWidth > maxWidth)
            {
                // Part is too long, split by words
                var words = part.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    int wordCharWidth = word.Length;
                    int spaceCharWidth = 1;
                    
                    // Check if word fits on current line
                    if (currentLineWidth + wordCharWidth > maxWidth && currentLine.Count > 0)
                    {
                        wrappedLines.Add(currentLine);
                        currentLine = new List<ColoredText>();
                        currentLineWidth = 0;
                    }
                    
                    // Add space before word if not first on line
                    if (currentLine.Count > 0 && currentLineWidth > 0)
                    {
                        currentLine.Add(new ColoredText(" ", color));
                        currentLineWidth += spaceCharWidth;
                    }
                    
                    // Add word
                    currentLine.Add(new ColoredText(word, color));
                    currentLineWidth += wordCharWidth;
                }
            }
            else
            {
                // Part doesn't fit, start new line
                if (currentLine.Count > 0)
                {
                    wrappedLines.Add(currentLine);
                    currentLine = new List<ColoredText>();
                    currentLineWidth = 0;
                }
                
                // Add part to new line
                currentLine.Add(new ColoredText(part, color));
                currentLineWidth += partCharWidth;
            }
        }
    }
}

