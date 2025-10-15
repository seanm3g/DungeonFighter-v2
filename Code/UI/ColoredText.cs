using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI
{
    /// <summary>
    /// Represents text with an associated color pattern that is applied at render time.
    /// This keeps the text and color information SEPARATE to avoid parsing issues.
    /// </summary>
    public class ColoredText
    {
        /// <summary>
        /// The actual text content (clean, no color codes embedded)
        /// </summary>
        public string Text { get; set; } = "";
        
        /// <summary>
        /// The color template to apply (null = use default color)
        /// </summary>
        public ColorTemplate? Template { get; set; }
        
        /// <summary>
        /// Optional: Single color code for simple coloring (overrides template if set)
        /// </summary>
        public char? SimpleColorCode { get; set; }
        
        /// <summary>
        /// When true, offsets the color pattern by one character to create a shimmering effect.
        /// Can be animated over time for dynamic undulation.
        /// </summary>
        public bool Undulate { get; set; } = false;
        
        /// <summary>
        /// The offset amount for undulation (advances with each frame for animation)
        /// </summary>
        public int UndulateOffset { get; set; } = 0;
        
        /// <summary>
        /// When set, applies a moving brightness mask over the text for cloud-like lighting effects
        /// </summary>
        public BrightnessMask? BrightnessMask { get; set; } = null;
        
        /// <summary>
        /// Random offset for the brightness mask to make each line independent
        /// </summary>
        public int BrightnessMaskLineOffset { get; set; } = 0;
        
        /// <summary>
        /// Creates a ColoredText with clean text and optional color pattern
        /// </summary>
        public ColoredText(string text, ColorTemplate? template = null, char? simpleColorCode = null, bool undulate = false)
        {
            Text = text;
            Template = template;
            SimpleColorCode = simpleColorCode;
            Undulate = undulate;
        }
        
        private static readonly Random random = new Random();
        
        /// <summary>
        /// Creates a ColoredText from template name
        /// </summary>
        public static ColoredText FromTemplate(string text, string templateName, bool undulate = false)
        {
            var template = ColorTemplateLibrary.GetTemplate(templateName);
            var coloredText = new ColoredText(text, template, null, undulate);
            
            // If undulating, start at a random offset so duplicate text doesn't look identical
            if (undulate && template != null && template.ColorSequence.Count > 0)
            {
                coloredText.UndulateOffset = random.Next(0, template.ColorSequence.Count);
                Console.WriteLine($"[UNDULATE] Created '{text}' with random start offset: {coloredText.UndulateOffset} (out of {template.ColorSequence.Count} colors)");
            }
            
            return coloredText;
        }
        
        /// <summary>
        /// Creates a ColoredText with a simple single color
        /// </summary>
        public static ColoredText FromColor(string text, char colorCode)
        {
            return new ColoredText(text, null, colorCode, false);
        }
        
        /// <summary>
        /// Creates a plain ColoredText (default color)
        /// </summary>
        public static ColoredText Plain(string text)
        {
            return new ColoredText(text, null, null, false);
        }
        
        /// <summary>
        /// Applies the color pattern to the text at render time
        /// Returns segments ready for rendering
        /// </summary>
        public List<ColorDefinitions.ColoredSegment> GetSegments()
        {
            List<ColorDefinitions.ColoredSegment> segments;
            
            // Simple single color
            if (SimpleColorCode.HasValue)
            {
                var color = ColorDefinitions.GetColor(SimpleColorCode.Value);
                segments = new List<ColorDefinitions.ColoredSegment>
                {
                    new ColorDefinitions.ColoredSegment(Text, color ?? ColorDefinitions.DefaultTextColor)
                };
            }
            // Template-based coloring
            else if (Template != null)
            {
                // Apply with undulation offset if enabled
                int offset = Undulate ? UndulateOffset : 0;
                segments = Template.Apply(Text, offset);
            }
            // Default coloring
            else
            {
                segments = new List<ColorDefinitions.ColoredSegment>
                {
                    new ColorDefinitions.ColoredSegment(Text, ColorDefinitions.DefaultTextColor)
                };
            }
            
            // Apply brightness mask if enabled
            if (BrightnessMask != null)
            {
                ApplyBrightnessMaskToSegments(segments);
            }
            
            return segments;
        }
        
        /// <summary>
        /// Applies the brightness mask to segments
        /// </summary>
        private void ApplyBrightnessMaskToSegments(List<ColorDefinitions.ColoredSegment> segments)
        {
            if (BrightnessMask == null) return;
            
            int charPosition = 0;
            foreach (var segment in segments)
            {
                if (!string.IsNullOrEmpty(segment.Text) && segment.Foreground.HasValue)
                {
                    // Get brightness for the START of this segment
                    // For single-character segments (sequence mode), this applies to that character
                    // For multi-character segments (solid mode), this applies to the whole segment
                    // Use the line offset to make each line independent
                    float brightness = BrightnessMask.GetBrightnessAt(charPosition, BrightnessMaskLineOffset);
                    
                    if (brightness != 0)
                    {
                        segment.Foreground = ColorDefinitions.AdjustBrightness(segment.Foreground.Value, brightness);
                    }
                    
                    charPosition += segment.Text.Length;
                }
            }
        }
        
        /// <summary>
        /// Advances the undulation offset for animation
        /// Call this each frame to create a shimmering effect
        /// </summary>
        public void AdvanceUndulation()
        {
            if (Undulate && Template != null)
            {
                UndulateOffset = (UndulateOffset + 1) % Template.ColorSequence.Count;
            }
        }
        
        /// <summary>
        /// Gets the plain text length (for layout calculations)
        /// </summary>
        public int Length => Text.Length;
        
        /// <summary>
        /// Implicit conversion from string to ColoredText
        /// </summary>
        public static implicit operator ColoredText(string text) => Plain(text);
        
        public override string ToString() => Text;
    }
    
    /// <summary>
    /// Builder for constructing complex colored text from multiple parts
    /// </summary>
    public class ColoredTextBuilder
    {
        private readonly List<ColoredText> parts = new();
        
        public ColoredTextBuilder Add(string text)
        {
            parts.Add(ColoredText.Plain(text));
            return this;
        }
        
        public ColoredTextBuilder Add(string text, string templateName, bool undulate = false)
        {
            parts.Add(ColoredText.FromTemplate(text, templateName, undulate));
            return this;
        }
        
        public ColoredTextBuilder Add(string text, char colorCode)
        {
            parts.Add(ColoredText.FromColor(text, colorCode));
            return this;
        }
        
        public ColoredTextBuilder Add(ColoredText coloredText)
        {
            parts.Add(coloredText);
            return this;
        }
        
        /// <summary>
        /// Builds all parts into a single list of segments
        /// </summary>
        public List<ColorDefinitions.ColoredSegment> Build()
        {
            var allSegments = new List<ColorDefinitions.ColoredSegment>();
            
            foreach (var part in parts)
            {
                allSegments.AddRange(part.GetSegments());
            }
            
            return allSegments;
        }
        
        /// <summary>
        /// Gets the total text length
        /// </summary>
        public int GetTotalLength()
        {
            int total = 0;
            foreach (var part in parts)
            {
                total += part.Length;
            }
            return total;
        }
        
        /// <summary>
        /// Gets the plain text (no colors)
        /// </summary>
        public string GetPlainText()
        {
            return string.Concat(parts.Select(p => p.Text));
        }
    }
}

