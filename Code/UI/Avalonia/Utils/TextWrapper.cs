using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Utility class for wrapping text to fit within a specified width
    /// Handles color markup properly by using display length instead of raw length
    /// </summary>
    public static class TextWrapper
    {
        /// <summary>
        /// Wraps text to fit within a specified width, breaking at spaces when possible
        /// Handles color markup properly by using display length instead of raw length
        /// </summary>
        /// <param name="text">The text to wrap (may contain color markup)</param>
        /// <param name="maxWidth">Maximum width in characters</param>
        /// <returns>List of wrapped lines</returns>
        public static List<string> WrapText(string text, int maxWidth)
        {
            var lines = new List<string>();
            
            if (string.IsNullOrEmpty(text))
            {
                lines.Add(text ?? "");
                return lines;
            }
            
            // Use display length (excluding markup) for comparison
            var segments = ColoredTextParser.Parse(text);
            int displayLength = ColoredTextRenderer.GetDisplayLength(segments);
            if (displayLength <= maxWidth)
            {
                lines.Add(text);
                return lines;
            }
            
            // Break text into words
            string[] words = text.Split(' ');
            string currentLine = "";
            int currentLineDisplayLength = 0;
            
            foreach (string word in words)
            {
                var wordSegments = ColoredTextParser.Parse(word);
                int wordDisplayLength = ColoredTextRenderer.GetDisplayLength(wordSegments);
                
                // If adding this word would exceed the max width
                if (currentLineDisplayLength > 0 && (currentLineDisplayLength + 1 + wordDisplayLength) > maxWidth)
                {
                    // Add the current line and start a new one
                    lines.Add(currentLine);
                    currentLine = word;
                    currentLineDisplayLength = wordDisplayLength;
                }
                else
                {
                    // Add the word to the current line
                    if (currentLineDisplayLength > 0)
                    {
                        currentLine += " " + word;
                        currentLineDisplayLength += 1 + wordDisplayLength;
                    }
                    else
                    {
                        currentLine = word;
                        currentLineDisplayLength = wordDisplayLength;
                    }
                }
                
                // If a single word is longer than maxWidth, we need to break it
                // Note: This is a simplification - proper character-level breaking with markup is complex
                if (currentLineDisplayLength > maxWidth)
                {
                    lines.Add(currentLine);
                    currentLine = "";
                    currentLineDisplayLength = 0;
                }
            }
            
            // Add the last line if there's anything left
            if (currentLineDisplayLength > 0)
            {
                lines.Add(currentLine);
            }
            
            return lines;
        }
    }
}

