using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Utility class for validating and ensuring colors are visible on black background
    /// </summary>
    public static class ColorValidator
    {
        /// <summary>
        /// Minimum brightness threshold (0-255) to ensure visibility on black background
        /// Colors below this threshold will be lightened
        /// </summary>
        private const int MIN_BRIGHTNESS = 50;
        
        /// <summary>
        /// Calculates the perceived brightness (luminance) of a color
        /// Uses the standard formula: 0.299*R + 0.587*G + 0.114*B
        /// </summary>
        public static double GetBrightness(Color color)
        {
            return 0.299 * color.R + 0.587 * color.G + 0.114 * color.B;
        }
        
        /// <summary>
        /// Checks if a color is too dark to be visible on a black background
        /// </summary>
        public static bool IsTooDark(Color color)
        {
            return GetBrightness(color) < MIN_BRIGHTNESS;
        }
        
        /// <summary>
        /// Ensures a color is visible on black background by lightening it if necessary
        /// Preserves the color's hue while increasing brightness
        /// </summary>
        public static Color EnsureVisible(Color color)
        {
            if (!IsTooDark(color))
                return color;
            
            // Calculate current brightness
            double brightness = GetBrightness(color);
            
            // Calculate how much we need to lighten
            double lightenFactor = MIN_BRIGHTNESS / brightness;
            
            // Lighten the color while preserving hue
            // Use a blend with white to lighten while maintaining color character
            double blendFactor = 0.5; // Blend 50% with white for very dark colors
            
            byte newR = (byte)Math.Min(255, color.R + (255 - color.R) * blendFactor);
            byte newG = (byte)Math.Min(255, color.G + (255 - color.G) * blendFactor);
            byte newB = (byte)Math.Min(255, color.B + (255 - color.B) * blendFactor);
            
            // If still too dark, use a minimum brightness approach
            Color lightened = Color.FromRgb(newR, newG, newB);
            if (IsTooDark(lightened))
            {
                // For extremely dark colors, use a gray that's definitely visible
                // But try to preserve some color character
                int minComponent = Math.Max(newR, Math.Max(newG, newB));
                if (minComponent < MIN_BRIGHTNESS)
                {
                    // Scale up the brightest component to at least MIN_BRIGHTNESS
                    double scale = MIN_BRIGHTNESS / (double)minComponent;
                    newR = (byte)Math.Min(255, (int)(color.R * scale));
                    newG = (byte)Math.Min(255, (int)(color.G * scale));
                    newB = (byte)Math.Min(255, (int)(color.B * scale));
                }
            }
            
            return Color.FromRgb(newR, newG, newB);
        }
    }
    
    /// <summary>
    /// Represents a piece of text with a specific color
    /// </summary>
    public class ColoredText
    {
        public string Text { get; set; } = "";
        public Color Color { get; set; } = Colors.White;
        
        // Undulation properties for animated effects
        private double _undulationPhase = 0;
        private double _undulationSpeed = 0.1;
        private bool _isUndulating = false;
        
        // Brightness mask properties
        private double[]? _brightnessMask = null;
        private int _brightnessMaskLineOffset = 0;
        
        /// <summary>
        /// Gets whether this text has undulation enabled
        /// </summary>
        public bool IsUndulating => _isUndulating;
        
        /// <summary>
        /// Gets or sets the undulation offset
        /// </summary>
        public double UndulateOffset
        {
            get => _undulationPhase;
            set => _undulationPhase = value;
        }
        
        public ColoredText(string text, Color color)
        {
            Text = text;
            // Ensure color is visible on black background
            Color = ColorValidator.EnsureVisible(color);
        }
        
        public ColoredText(string text) : this(text, Colors.White) { }
        
        public override string ToString() => Text;
        
        /// <summary>
        /// Gets the segments of this colored text (returns itself as a single segment)
        /// </summary>
        public List<ColoredText> GetSegments()
        {
            return new List<ColoredText> { this };
        }
        
        /// <summary>
        /// Creates a ColoredText from a template name and text
        /// </summary>
        public static List<ColoredText> FromTemplate(string templateName, string text)
        {
            return ColorTemplateLibrary.GetTemplate(templateName, text);
        }
        
        /// <summary>
        /// Creates a ColoredText from a template name and text with undulation option
        /// </summary>
        public static List<ColoredText> FromTemplate(string templateName, string text, bool undulate = false)
        {
            var result = FromTemplate(templateName, text);
            
            if (undulate)
            {
                foreach (var segment in result)
                {
                    segment.Undulate();
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Creates a ColoredText with a solid color
        /// </summary>
        public static List<ColoredText> FromColor(string text, Color color)
        {
            return new List<ColoredText> { new ColoredText(text, color) };
        }
        
        /// <summary>
        /// Enables undulation effect on this text
        /// </summary>
        public ColoredText Undulate(double speed = 0.1)
        {
            _isUndulating = true;
            _undulationSpeed = speed;
            return this;
        }
        
        /// <summary>
        /// Advances the undulation animation
        /// </summary>
        public ColoredText AdvanceUndulation()
        {
            if (_isUndulating)
            {
                _undulationPhase += _undulationSpeed;
                if (_undulationPhase > Math.PI * 2)
                    _undulationPhase -= Math.PI * 2;
            }
            return this;
        }
        
        /// <summary>
        /// Sets a brightness mask for this text
        /// </summary>
        public ColoredText BrightnessMask(double[] mask)
        {
            _brightnessMask = mask;
            return this;
        }
        
        /// <summary>
        /// Sets the line offset for brightness mask
        /// </summary>
        public ColoredText BrightnessMaskLineOffset(int offset)
        {
            _brightnessMaskLineOffset = offset;
            return this;
        }
        
        /// <summary>
        /// Gets the current brightness adjustment from undulation
        /// </summary>
        public double GetUndulationBrightness()
        {
            if (!_isUndulating)
                return 0;
                
            return Math.Sin(_undulationPhase) * 0.3;
        }
        
        /// <summary>
        /// Gets the brightness adjustment from mask at a given position
        /// </summary>
        public double GetMaskBrightness(int position)
        {
            if (_brightnessMask == null || _brightnessMask.Length == 0)
                return 0;
                
            int adjustedPosition = (position + _brightnessMaskLineOffset) % _brightnessMask.Length;
            return _brightnessMask[adjustedPosition];
        }
    }
    
    /// <summary>
    /// Represents a collection of colored text segments
    /// </summary>
    public class ColoredTextBuilder
    {
        private readonly List<ColoredText> _segments = new List<ColoredText>();
        
        /// <summary>
        /// Adds text with a specific color
        /// </summary>
        public ColoredTextBuilder Add(string text, Color color)
        {
            if (!string.IsNullOrEmpty(text))
            {
                // Color validation happens in ColoredText constructor
                _segments.Add(new ColoredText(text, color));
            }
            return this;
        }
        
        /// <summary>
        /// Adds text with white color (default)
        /// </summary>
        public ColoredTextBuilder Add(string text)
        {
            return Add(text, Colors.White);
        }
        
        /// <summary>
        /// Adds text with a color from the color palette
        /// </summary>
        public ColoredTextBuilder Add(string text, ColorPalette color)
        {
            return Add(text, color.GetColor());
        }
        
        /// <summary>
        /// Adds an existing ColoredText segment
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
        /// Adds multiple ColoredText segments
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
        /// Adds a space
        /// </summary>
        public ColoredTextBuilder AddSpace()
        {
            return Add(" ");
        }
        
        /// <summary>
        /// Adds a newline
        /// </summary>
        public ColoredTextBuilder AddLine()
        {
            return Add(System.Environment.NewLine);
        }
        
        /// <summary>
        /// Adds text with a pattern (e.g., "damage", "healing", "warning")
        /// </summary>
        public ColoredTextBuilder AddWithPattern(string text, string pattern)
        {
            var color = ColorPatterns.GetColorForPattern(pattern);
            return Add(text, color);
        }
        
        /// <summary>
        /// Builds the final colored text collection
        /// </summary>
        public List<ColoredText> Build()
        {
            return new List<ColoredText>(_segments);
        }
        
        /// <summary>
        /// Gets the plain text (without color information)
        /// </summary>
        public string GetPlainText()
        {
            return string.Join("", _segments.Select(s => s.Text));
        }
        
        /// <summary>
        /// Gets the display length (ignoring color markup)
        /// </summary>
        public int GetDisplayLength()
        {
            return _segments.Sum(s => s.Text.Length);
        }
    }

    /// <summary>
    /// Static utility methods for Color type conversions (merged from ColorExtensions)
    /// </summary>
    public static class ColorUtils
    {
        /// <summary>
        /// Converts a System.Drawing.Color to Avalonia.Media.Color
        /// </summary>
        public static Color ToAvaloniaColor(this System.Drawing.Color color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }
        
        /// <summary>
        /// Converts an Avalonia.Media.Color to System.Drawing.Color
        /// </summary>
        public static System.Drawing.Color ToSystemColor(this Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
        
        /// <summary>
        /// Converts an Avalonia.Media.Color to itself (no-op for compatibility)
        /// </summary>
        public static Color ToAvaloniaColor(this Color color)
        {
            return color;
        }
        
        /// <summary>
        /// Creates an Avalonia.Media.Color from RGB values
        /// </summary>
        public static Color ToAvaloniaColor(byte r, byte g, byte b, byte a = 255)
        {
            return Color.FromArgb(a, r, g, b);
        }
        
        /// <summary>
        /// Creates an Avalonia.Media.Color from a hex string
        /// </summary>
        public static Color ToAvaloniaColor(this string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor))
                return Colors.White;
            
            // Remove # if present
            if (hexColor.StartsWith("#"))
                hexColor = hexColor.Substring(1);
            
            // Parse hex color
            if (hexColor.Length == 6)
            {
                var r = Convert.ToByte(hexColor.Substring(0, 2), 16);
                var g = Convert.ToByte(hexColor.Substring(2, 2), 16);
                var b = Convert.ToByte(hexColor.Substring(4, 2), 16);
                return Color.FromArgb(255, r, g, b);
            }
            else if (hexColor.Length == 8)
            {
                var a = Convert.ToByte(hexColor.Substring(0, 2), 16);
                var r = Convert.ToByte(hexColor.Substring(2, 2), 16);
                var g = Convert.ToByte(hexColor.Substring(4, 2), 16);
                var b = Convert.ToByte(hexColor.Substring(6, 2), 16);
                return Color.FromArgb(a, r, g, b);
            }
            
            return Colors.White;
        }
    }
}
