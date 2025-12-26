using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Helpers
{
    /// <summary>
    /// Helper class to convert ColoredText segments to Avalonia Inline elements
    /// Used for rendering colored text in TextBlock controls
    /// </summary>
    public static class ColoredTextToAvaloniaHelper
    {
        /// <summary>
        /// Converts a list of ColoredText segments to Avalonia Inline elements
        /// </summary>
        public static IEnumerable<Inline> ConvertToInlines(IEnumerable<ColoredText> segments)
        {
            if (segments == null)
                yield break;
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                var run = new Run(segment.Text)
                {
                    Foreground = new SolidColorBrush(segment.Color)
                };
                
                yield return run;
            }
        }
        
        /// <summary>
        /// Converts multiple lines of ColoredText segments to a formatted text block
        /// Each line becomes a separate paragraph with line breaks
        /// </summary>
        public static IEnumerable<Inline> ConvertLinesToInlines(IEnumerable<IEnumerable<ColoredText>> lines)
        {
            if (lines == null)
                yield break;
            
            var lineList = lines.ToList();
            
            for (int i = 0; i < lineList.Count; i++)
            {
                var line = lineList[i];
                
                // Add all segments from this line
                foreach (var inline in ConvertToInlines(line))
                {
                    yield return inline;
                }
                
                // Add line break after each line (except the last)
                if (i < lineList.Count - 1)
                {
                    yield return new LineBreak();
                }
            }
        }
        
        /// <summary>
        /// Converts a single line of ColoredText segments to a string with color markup
        /// This is a fallback method that preserves color information as markup
        /// </summary>
        public static string ConvertToMarkupString(IEnumerable<ColoredText> segments)
        {
            if (segments == null)
                return "";
            
            return ColoredTextRenderer.RenderAsMarkup(segments);
        }
    }
}

