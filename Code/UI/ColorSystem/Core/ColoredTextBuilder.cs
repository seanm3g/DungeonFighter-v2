using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.ColorSystem.Helpers;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Builder for creating collections of colored text segments.
    /// Provides a fluent API for building colored text with automatic space normalization.
    /// 
    /// SPACING STANDARDIZATION:
    /// This class uses CombatLogSpacingManager for standardized spacing rules.
    /// Text segments should be provided without spaces - spacing is handled automatically.
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
        /// Maximum allowed length for a single text segment to prevent memory issues.
        /// </summary>
        private const int MaxSegmentLength = 10000;
        
        /// <summary>
        /// Adds text with a specific color.
        /// </summary>
        /// <param name="text">The text to add. Cannot be null.</param>
        /// <param name="color">The color for the text.</param>
        /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
        /// <exception cref="ArgumentException">Thrown when text exceeds maximum length.</exception>
        public ColoredTextBuilder Add(string text, Color color)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text), "Text cannot be null. Use empty string for empty text.");
            
            if (text.Length > MaxSegmentLength)
                throw new ArgumentException(
                    $"Text segment exceeds maximum length of {MaxSegmentLength} characters. " +
                    $"Consider splitting into multiple segments. " +
                    $"Text preview: {text.Substring(0, Math.Min(50, text.Length))}...",
                    nameof(text));
            
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
        /// <param name="segment">The ColoredText segment to add. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when segment is null.</exception>
        public ColoredTextBuilder Add(ColoredText segment)
        {
            if (segment == null)
                throw new ArgumentNullException(nameof(segment), "ColoredText segment cannot be null.");
            
            if (segment.Text != null && segment.Text.Length > MaxSegmentLength)
                throw new ArgumentException(
                    $"ColoredText segment text exceeds maximum length of {MaxSegmentLength} characters. " +
                    $"Text preview: {segment.Text.Substring(0, Math.Min(50, segment.Text.Length))}...",
                    nameof(segment));
            
            _segments.Add(segment);
            return this;
        }
        
        /// <summary>
        /// Adds multiple ColoredText segments.
        /// </summary>
        /// <param name="segments">The list of segments to add. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when segments is null.</exception>
        public ColoredTextBuilder AddRange(List<ColoredText> segments)
        {
            if (segments == null)
                throw new ArgumentNullException(nameof(segments), "Segments list cannot be null.");
            
            foreach (var segment in segments)
            {
                if (segment == null)
                    throw new ArgumentException("Segments list contains null entries. All segments must be non-null.", nameof(segments));
                
                if (segment.Text != null && segment.Text.Length > MaxSegmentLength)
                    throw new ArgumentException(
                        $"ColoredText segment text exceeds maximum length of {MaxSegmentLength} characters. " +
                        $"Text preview: {segment.Text.Substring(0, Math.Min(50, segment.Text.Length))}...",
                        nameof(segments));
                
                _segments.Add(segment);
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
        /// </summary>
        public List<ColoredText> Build() => SpacingHelper.ProcessSegments(_segments);
        
        #endregion
    }
}

