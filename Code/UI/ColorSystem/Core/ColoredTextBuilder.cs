using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Builder for creating collections of colored text segments.
    /// Provides a fluent API for building colored text with automatic space normalization.
    /// </summary>
    public class ColoredTextBuilder
    {
        private readonly List<ColoredText> _segments = new List<ColoredText>();
        
        /// <summary>
        /// Creates a new ColoredTextBuilder instance.
        /// </summary>
        public ColoredTextBuilder() { }
        
        /// <summary>
        /// Creates a new ColoredTextBuilder and returns it for fluent chaining.
        /// </summary>
        public static ColoredTextBuilder Start()
        {
            return new ColoredTextBuilder();
        }
        
        #region Core Add Methods
        
        /// <summary>
        /// Adds text with a specific color.
        /// </summary>
        public ColoredTextBuilder Add(string text, Color color)
        {
            if (!string.IsNullOrEmpty(text))
            {
                _segments.Add(new ColoredText(text, color));
            }
            return this;
        }
        
        /// <summary>
        /// Adds text with white color (default).
        /// </summary>
        public ColoredTextBuilder Add(string text)
        {
            return Add(text, Colors.White);
        }
        
        /// <summary>
        /// Adds text with a color from the color palette.
        /// </summary>
        public ColoredTextBuilder Add(string text, ColorPalette color)
        {
            return Add(text, color.GetColor());
        }
        
        /// <summary>
        /// Adds an existing ColoredText segment.
        /// </summary>
        public ColoredTextBuilder Add(ColoredText segment)
        {
            if (segment != null)
            {
                _segments.Add(segment);
            }
            return this;
        }
        
        /// <summary>
        /// Adds multiple ColoredText segments.
        /// </summary>
        public ColoredTextBuilder AddRange(List<ColoredText> segments)
        {
            if (segments != null)
            {
                foreach (var segment in segments)
                {
                    if (segment != null)
                    {
                        _segments.Add(segment);
                    }
                }
            }
            return this;
        }
        
        /// <summary>
        /// Adds text with a pattern (e.g., "damage", "healing", "warning").
        /// </summary>
        public ColoredTextBuilder AddWithPattern(string text, string pattern)
        {
            var color = ColorPatterns.GetColorForPattern(pattern);
            return Add(text, color);
        }
        
        #endregion
        
        #region Fluent Color Methods
        
        /// <summary>
        /// Adds text in red color.
        /// </summary>
        public ColoredTextBuilder Red(string text) => Add(text, ColorPalette.Red);
        
        /// <summary>
        /// Adds text in dark red color.
        /// </summary>
        public ColoredTextBuilder DarkRed(string text) => Add(text, ColorPalette.DarkRed);
        
        /// <summary>
        /// Adds text in green color.
        /// </summary>
        public ColoredTextBuilder Green(string text) => Add(text, ColorPalette.Green);
        
        /// <summary>
        /// Adds text in dark green color.
        /// </summary>
        public ColoredTextBuilder DarkGreen(string text) => Add(text, ColorPalette.DarkGreen);
        
        /// <summary>
        /// Adds text in blue color.
        /// </summary>
        public ColoredTextBuilder Blue(string text) => Add(text, ColorPalette.Blue);
        
        /// <summary>
        /// Adds text in dark blue color.
        /// </summary>
        public ColoredTextBuilder DarkBlue(string text) => Add(text, ColorPalette.DarkBlue);
        
        /// <summary>
        /// Adds text in yellow color.
        /// </summary>
        public ColoredTextBuilder Yellow(string text) => Add(text, ColorPalette.Yellow);
        
        /// <summary>
        /// Adds text in cyan color.
        /// </summary>
        public ColoredTextBuilder Cyan(string text) => Add(text, ColorPalette.Cyan);
        
        /// <summary>
        /// Adds text in magenta color.
        /// </summary>
        public ColoredTextBuilder Magenta(string text) => Add(text, ColorPalette.Magenta);
        
        /// <summary>
        /// Adds text in orange color.
        /// </summary>
        public ColoredTextBuilder Orange(string text) => Add(text, ColorPalette.Orange);
        
        /// <summary>
        /// Adds text in gold color.
        /// </summary>
        public ColoredTextBuilder Gold(string text) => Add(text, ColorPalette.Gold);
        
        /// <summary>
        /// Adds text in white color.
        /// </summary>
        public ColoredTextBuilder White(string text) => Add(text, Colors.White);
        
        /// <summary>
        /// Adds text in gray color.
        /// </summary>
        public ColoredTextBuilder Gray(string text) => Add(text, ColorPalette.Gray);
        
        /// <summary>
        /// Adds text in purple color.
        /// </summary>
        public ColoredTextBuilder Purple(string text) => Add(text, ColorPalette.Purple);
        
        /// <summary>
        /// Adds text in brown color.
        /// </summary>
        public ColoredTextBuilder Brown(string text) => Add(text, ColorPalette.Brown);
        
        // Semantic color methods
        /// <summary>
        /// Adds text in damage color (red).
        /// </summary>
        public ColoredTextBuilder Damage(string text) => Add(text, ColorPalette.Damage);
        
        /// <summary>
        /// Adds text in healing color (green).
        /// </summary>
        public ColoredTextBuilder Healing(string text) => Add(text, ColorPalette.Healing);
        
        /// <summary>
        /// Adds text in player color (cyan).
        /// </summary>
        public ColoredTextBuilder Player(string text) => Add(text, ColorPalette.Player);
        
        /// <summary>
        /// Adds text in enemy color (red).
        /// </summary>
        public ColoredTextBuilder Enemy(string text) => Add(text, ColorPalette.Enemy);
        
        /// <summary>
        /// Adds text in success color (green).
        /// </summary>
        public ColoredTextBuilder Success(string text) => Add(text, ColorPalette.Success);
        
        /// <summary>
        /// Adds text in warning color (orange).
        /// </summary>
        public ColoredTextBuilder Warning(string text) => Add(text, ColorPalette.Warning);
        
        /// <summary>
        /// Adds text in error color (red).
        /// </summary>
        public ColoredTextBuilder Error(string text) => Add(text, ColorPalette.Error);
        
        /// <summary>
        /// Adds text in info color (cyan).
        /// </summary>
        public ColoredTextBuilder Info(string text) => Add(text, ColorPalette.Info);
        
        /// <summary>
        /// Adds plain text (white color).
        /// </summary>
        public ColoredTextBuilder Plain(string text) => Add(text, Colors.White);
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Adds a space.
        /// </summary>
        public ColoredTextBuilder AddSpace()
        {
            return Add(" ");
        }
        
        /// <summary>
        /// Adds a newline.
        /// </summary>
        public ColoredTextBuilder AddLine()
        {
            return Add(System.Environment.NewLine);
        }
        
        /// <summary>
        /// Gets the plain text (without color information).
        /// </summary>
        public string GetPlainText()
        {
            return string.Join("", _segments.Select(s => s.Text));
        }
        
        /// <summary>
        /// Gets the display length (ignoring color markup).
        /// </summary>
        public int GetDisplayLength()
        {
            return _segments.Sum(s => s.Text.Length);
        }
        
        #endregion
        
        #region Build Method
        
        /// <summary>
        /// Builds the final colored text collection.
        /// Automatically adds spaces between segments and merges adjacent segments with the same color.
        /// Text segments should be provided without spaces - spacing is handled automatically.
        /// </summary>
        public List<ColoredText> Build()
        {
            if (_segments.Count == 0)
                return new List<ColoredText>();
            
            // Step 1: Strip leading/trailing spaces from all segments (spacing will be added automatically)
            var trimmed = TrimSegmentSpaces(_segments);
            
            // Step 2: Automatically add spaces between adjacent segments FIRST
            // This ensures separate words get spaces before merging
            var spaced = AddAutomaticSpacing(trimmed);
            
            // Step 3: Merge adjacent segments with the same color AFTER spacing is added
            // This merges segments that are already properly spaced
            var merged = ColoredTextMerger.MergeSameColorSegments(spaced);
            
            // Step 4: Remove empty segments
            merged.RemoveAll(s => string.IsNullOrEmpty(s.Text));
            
            return merged;
        }
        
        /// <summary>
        /// Trims leading and trailing whitespace from segments (but preserves internal spaces).
        /// This ensures spacing is handled at the Build() level, not in individual text segments.
        /// </summary>
        private static List<ColoredText> TrimSegmentSpaces(List<ColoredText> segments)
        {
            var trimmed = new List<ColoredText>(segments.Count);
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                // Preserve newlines and internal spaces, but trim leading/trailing whitespace
                // Use TrimStart() and TrimEnd() without parameters to trim ALL whitespace characters
                var text = segment.Text.TrimStart().TrimEnd();
                
                if (!string.IsNullOrEmpty(text))
                {
                    trimmed.Add(new ColoredText(text, segment.Color));
                }
            }
            
            return trimmed;
        }
        
        /// <summary>
        /// Automatically adds exactly one space between adjacent segments that need it.
        /// Skips spacing for segments that end/start with punctuation or newlines.
        /// Always adds spaces as separate segments to avoid merging issues.
        /// </summary>
        private static List<ColoredText> AddAutomaticSpacing(List<ColoredText> segments)
        {
            if (segments.Count == 0)
                return segments;
            
            var spaced = new List<ColoredText>(segments.Count * 2); // Pre-allocate for potential spaces
            
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                
                if (i == 0)
                {
                    // First segment - no space before
                    spaced.Add(segment);
                }
                else
                {
                    var prevSegment = spaced[spaced.Count - 1];
                    var prevText = prevSegment.Text;
                    var currentText = segment.Text;
                    
                    // Check if we need a space between segments
                    bool needsSpace = ShouldAddSpace(prevText, currentText);
                    
                    if (needsSpace)
                    {
                        // Always add space as separate segment to ensure proper spacing
                        // This prevents issues when same-color segments are merged later
                        spaced.Add(new ColoredText(" ", Colors.White));
                    }
                    
                    // Add the current segment
                    spaced.Add(segment);
                }
            }
            
            return spaced;
        }
        
        /// <summary>
        /// Determines if a space should be added between two text segments.
        /// Returns false for punctuation boundaries, newlines, or if either segment is empty.
        /// Also returns false if either segment is whitespace-only to prevent double spacing.
        /// </summary>
        private static bool ShouldAddSpace(string prevText, string currentText)
        {
            if (string.IsNullOrEmpty(prevText) || string.IsNullOrEmpty(currentText))
                return false;
            
            // Don't add space if either segment is whitespace-only
            // This prevents double spacing when template-based coloring creates separate whitespace segments
            if (string.IsNullOrWhiteSpace(prevText) || string.IsNullOrWhiteSpace(currentText))
                return false;
            
            // Don't add space if previous ends with punctuation that shouldn't have space after
            char prevLast = prevText[prevText.Length - 1];
            if (prevLast == '!' || prevLast == '?' || prevLast == '.' || prevLast == ',' || 
                prevLast == ':' || prevLast == ';' || prevLast == '\n' || prevLast == '\r')
                return false;
            
            // Don't add space if current starts with punctuation that shouldn't have space before
            char currentFirst = currentText[0];
            if (currentFirst == '!' || currentFirst == '?' || currentFirst == '.' || 
                currentFirst == ',' || currentFirst == ':' || currentFirst == ';' ||
                currentFirst == '\n' || currentFirst == '\r')
                return false;
            
            // Add space for all other cases
            return true;
        }
        
        // Merging logic has been moved to ColoredTextMerger for centralized maintenance
        
        
        #endregion
    }
}

