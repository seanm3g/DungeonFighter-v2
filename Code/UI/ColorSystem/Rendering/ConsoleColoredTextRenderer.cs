using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Renders colored text to console output using ANSI escape codes
    /// </summary>
    public class ConsoleColoredTextRenderer : IColoredTextRenderer
    {
        /// <summary>
        /// Renders colored text segments to console
        /// </summary>
        public void Render(IEnumerable<ColoredText> segments, int x, int y)
        {
            if (segments == null)
                return;
                
            var output = new StringBuilder();
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                    
                var ansiCode = ColorToAnsi(segment.Color);
                var escapedText = EscapeAnsi(segment.Text);
                
                output.Append($"\x1b[{ansiCode}m{escapedText}\x1b[0m");
            }
            
            Console.Write(output.ToString());
        }
        
        /// <summary>
        /// Renders colored text segments with wrapping
        /// </summary>
        public void RenderWrapped(IEnumerable<ColoredText> segments, int x, int y, int maxWidth)
        {
            if (segments == null)
                return;
                
            var wrappedSegments = WrapText(segments, maxWidth);
            
            foreach (var line in wrappedSegments)
            {
                Render(line, x, y);
                Console.WriteLine(); // Move to next line
            }
        }
        
        /// <summary>
        /// Renders colored text segments centered within a width
        /// </summary>
        public void RenderCentered(IEnumerable<ColoredText> segments, int x, int y, int width)
        {
            if (segments == null)
                return;
                
            var centeredSegments = Center(segments, width);
            Render(centeredSegments, x, y);
        }
        
        /// <summary>
        /// Renders colored text segments with padding
        /// </summary>
        public void RenderPadded(IEnumerable<ColoredText> segments, int x, int y, int minWidth, char paddingChar = ' ')
        {
            if (segments == null)
                return;
                
            var paddedSegments = PadRight(segments, minWidth, paddingChar);
            Render(paddedSegments, x, y);
        }
        
        /// <summary>
        /// Gets the display length of colored text (ignoring color markup)
        /// </summary>
        public int GetDisplayLength(IEnumerable<ColoredText> segments)
        {
            if (segments == null)
                return 0;
                
            return segments.Sum(s => s.Text.Length);
        }
        
        /// <summary>
        /// Truncates colored text to a maximum length
        /// </summary>
        public List<ColoredText> Truncate(IEnumerable<ColoredText> segments, int maxLength)
        {
            if (segments == null)
                return new List<ColoredText>();
                
            var result = new List<ColoredText>();
            var currentLength = 0;
            
            foreach (var segment in segments)
            {
                if (currentLength >= maxLength)
                    break;
                    
                var remainingLength = maxLength - currentLength;
                var segmentLength = segment.Text.Length;
                
                if (segmentLength <= remainingLength)
                {
                    result.Add(segment);
                    currentLength += segmentLength;
                }
                else
                {
                    // Truncate this segment
                    var truncatedText = segment.Text.Substring(0, remainingLength);
                    result.Add(new ColoredText(truncatedText, segment.Color));
                    break;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Pads colored text to a minimum length
        /// </summary>
        public List<ColoredText> PadRight(IEnumerable<ColoredText> segments, int minLength, char paddingChar = ' ')
        {
            if (segments == null)
                return new List<ColoredText>();
                
            var result = new List<ColoredText>(segments);
            var currentLength = GetDisplayLength(result);
            
            if (currentLength < minLength)
            {
                var paddingLength = minLength - currentLength;
                var padding = new string(paddingChar, paddingLength);
                result.Add(new ColoredText(padding, Colors.White));
            }
            
            return result;
        }
        
        /// <summary>
        /// Centers colored text within a given width
        /// </summary>
        public List<ColoredText> Center(IEnumerable<ColoredText> segments, int width, char paddingChar = ' ')
        {
            if (segments == null)
                return new List<ColoredText>();
                
            var result = new List<ColoredText>();
            var currentLength = GetDisplayLength(segments);
            
            if (currentLength >= width)
            {
                // Text is already wider than target width
                return new List<ColoredText>(segments);
            }
            
            var paddingLength = (width - currentLength) / 2;
            var leftPadding = new string(paddingChar, paddingLength);
            var rightPadding = new string(paddingChar, width - currentLength - paddingLength);
            
            result.Add(new ColoredText(leftPadding, Colors.White));
            result.AddRange(segments);
            result.Add(new ColoredText(rightPadding, Colors.White));
            
            return result;
        }
        
        /// <summary>
        /// Converts Avalonia color to ANSI escape code
        /// </summary>
        private static string ColorToAnsi(Color color)
        {
            // Simple ANSI color mapping
            if (color == Colors.Red) return "31";
            if (color == Colors.Green) return "32";
            if (color == Colors.Blue) return "34";
            if (color == Colors.Yellow) return "33";
            if (color == Colors.Cyan) return "36";
            if (color == Colors.Magenta) return "35";
            if (color == Colors.White) return "37";
            if (color == Colors.Black) return "30";
            if (color == Colors.Gray) return "90";
            
            // Try to match based on RGB values for custom colors
            if (color.R > 200 && color.G < 100 && color.B < 100) return "31"; // Red-ish
            if (color.R < 100 && color.G > 200 && color.B < 100) return "32"; // Green-ish
            if (color.R < 100 && color.G < 100 && color.B > 200) return "34"; // Blue-ish
            if (color.R > 200 && color.G > 200 && color.B < 100) return "33"; // Yellow-ish
            if (color.R < 100 && color.G > 200 && color.B > 200) return "36"; // Cyan-ish
            if (color.R > 200 && color.G < 100 && color.B > 200) return "35"; // Magenta-ish
            
            // Default to white
            return "37";
        }
        
        /// <summary>
        /// Escapes text for ANSI output
        /// </summary>
        private static string EscapeAnsi(string text)
        {
            // ANSI doesn't need much escaping, but we'll handle basic cases
            return text.Replace("\x1b", "\\x1b");
        }
        
        /// <summary>
        /// Wraps text to fit within a maximum width
        /// </summary>
        private List<List<ColoredText>> WrapText(IEnumerable<ColoredText> segments, int maxWidth)
        {
            var lines = new List<List<ColoredText>>();
            var currentLine = new List<ColoredText>();
            var currentLength = 0;
            
            foreach (var segment in segments)
            {
                var segmentLength = segment.Text.Length;
                
                if (currentLength + segmentLength <= maxWidth)
                {
                    // Segment fits on current line
                    currentLine.Add(segment);
                    currentLength += segmentLength;
                }
                else
                {
                    // Segment doesn't fit, need to wrap
                    if (currentLine.Count > 0)
                    {
                        // Finish current line
                        lines.Add(new List<ColoredText>(currentLine));
                        currentLine.Clear();
                        currentLength = 0;
                    }
                    
                    // If segment is longer than maxWidth, split it at word boundaries
                    if (segmentLength > maxWidth)
                    {
                        var remainingText = segment.Text;
                        while (remainingText.Length > maxWidth)
                        {
                            // Try to find the last space before maxWidth to split at word boundary
                            int splitPoint = maxWidth;
                            int lastSpace = remainingText.LastIndexOf(' ', maxWidth - 1);
                            
                            if (lastSpace > 0 && lastSpace > maxWidth / 2)
                            {
                                // Split at word boundary
                                splitPoint = lastSpace + 1; // Include the space
                            }
                            
                            var chunk = remainingText.Substring(0, splitPoint);
                            if (chunk.Length > 0)
                            {
                                currentLine.Add(new ColoredText(chunk, segment.Color));
                            }
                            lines.Add(new List<ColoredText>(currentLine));
                            currentLine.Clear();
                            currentLength = 0;
                            
                            // Remove the chunk (and trailing space if we split at word boundary)
                            remainingText = remainingText.Substring(splitPoint).TrimStart();
                        }
                        
                        if (remainingText.Length > 0)
                        {
                            currentLine.Add(new ColoredText(remainingText, segment.Color));
                            currentLength = remainingText.Length;
                        }
                    }
                    else
                    {
                        // Add segment to new line
                        currentLine.Add(segment);
                        currentLength = segmentLength;
                    }
                }
            }
            
            // Add final line if it has content
            if (currentLine.Count > 0)
            {
                lines.Add(currentLine);
            }
            
            return lines;
        }
    }
}
