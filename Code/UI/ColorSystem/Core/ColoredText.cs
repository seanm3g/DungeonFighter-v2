using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Represents a piece of text with a specific color.
    /// Supports undulation effects and brightness masks for animated text.
    /// </summary>
    public class ColoredText
    {
        public string Text { get; set; } = "";
        public Color Color { get; set; } = Colors.White;
        
        // Undulation properties for animated effects
        private double _undulationPhase = 0;
        private double _undulationSpeed = 0.005;
        private bool _isUndulating = false;
        
        // Brightness mask properties
        private double[]? _brightnessMask = null;
        private int _brightnessMaskLineOffset = 0;
        
        /// <summary>
        /// Gets whether this text has undulation enabled.
        /// </summary>
        public bool IsUndulating => _isUndulating;
        
        /// <summary>
        /// Gets or sets the undulation offset.
        /// </summary>
        public double UndulateOffset
        {
            get => _undulationPhase;
            set => _undulationPhase = value;
        }
        
        /// <summary>
        /// Creates a new ColoredText with the specified text and color.
        /// The color will be validated to ensure visibility on black background.
        /// </summary>
        public ColoredText(string text, Color color)
        {
            Text = text;
            // Ensure color is visible on black background
            Color = ColorValidator.EnsureVisible(color);
        }
        
        /// <summary>
        /// Creates a new ColoredText with the specified text and white color.
        /// </summary>
        public ColoredText(string text) : this(text, Colors.White) { }
        
        /// <summary>
        /// Returns the text content (without color information).
        /// </summary>
        public override string ToString() => Text;
        
        /// <summary>
        /// Gets the segments of this colored text (returns itself as a single segment).
        /// </summary>
        public List<ColoredText> GetSegments()
        {
            return new List<ColoredText> { this };
        }
        
        /// <summary>
        /// Creates a ColoredText from a template name and text.
        /// </summary>
        public static List<ColoredText> FromTemplate(string templateName, string text)
        {
            return ColorTemplateLibrary.GetTemplate(templateName, text);
        }
        
        /// <summary>
        /// Creates a ColoredText from a template name and text with undulation option.
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
        /// Creates a ColoredText with a solid color.
        /// </summary>
        public static List<ColoredText> FromColor(string text, Color color)
        {
            return new List<ColoredText> { new ColoredText(text, color) };
        }
        
        /// <summary>
        /// Enables undulation effect on this text.
        /// </summary>
        /// <param name="speed">The speed of the undulation animation (default: 0.005)</param>
        /// <returns>This instance for method chaining</returns>
        public ColoredText Undulate(double speed = 0.005)
        {
            _isUndulating = true;
            _undulationSpeed = speed;
            return this;
        }
        
        /// <summary>
        /// Advances the undulation animation by one step.
        /// </summary>
        /// <returns>This instance for method chaining</returns>
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
        /// Sets a brightness mask for this text.
        /// </summary>
        /// <param name="mask">Array of brightness values (0.0 to 1.0)</param>
        /// <returns>This instance for method chaining</returns>
        public ColoredText BrightnessMask(double[] mask)
        {
            _brightnessMask = mask;
            return this;
        }
        
        /// <summary>
        /// Sets the line offset for brightness mask.
        /// </summary>
        /// <param name="offset">The offset to apply to the mask position</param>
        /// <returns>This instance for method chaining</returns>
        public ColoredText BrightnessMaskLineOffset(int offset)
        {
            _brightnessMaskLineOffset = offset;
            return this;
        }
        
        /// <summary>
        /// Gets the current brightness adjustment from undulation.
        /// </summary>
        public double GetUndulationBrightness()
        {
            if (!_isUndulating)
                return 0;
                
            return Math.Sin(_undulationPhase) * 0.3;
        }
        
        /// <summary>
        /// Gets the brightness adjustment from mask at a given position.
        /// </summary>
        /// <param name="position">The character position in the text</param>
        public double GetMaskBrightness(int position)
        {
            if (_brightnessMask == null || _brightnessMask.Length == 0)
                return 0;
                
            int adjustedPosition = (position + _brightnessMaskLineOffset) % _brightnessMask.Length;
            return _brightnessMask[adjustedPosition];
        }
    }
}
