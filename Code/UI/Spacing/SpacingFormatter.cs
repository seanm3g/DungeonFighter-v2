using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Spacing
{

    /// <summary>
    /// Handles formatting utilities for spacing (roll info, segments).
    /// </summary>
    public static class SpacingFormatter
    {
        /// <summary>
        /// Formats roll info with standard indentation and spacing.
        /// This ensures consistent formatting across all roll info displays.
        /// </summary>
        public static string FormatRollInfo(string rollText, string attackText, string? speedText = null, string? amplifierText = null)
        {
            var parts = new List<string> { SpacingRules.IndentSpacing + "(" };
            
            // Roll information
            if (!string.IsNullOrEmpty(rollText))
            {
                parts.Add("roll:");
                parts.Add(rollText);
            }
            
            // Attack vs Defense
            if (!string.IsNullOrEmpty(attackText))
            {
                parts.Add("|");
                parts.Add(attackText);
            }
            
            // Speed
            if (!string.IsNullOrEmpty(speedText))
            {
                parts.Add("|");
                parts.Add("speed:");
                parts.Add(speedText);
            }
            
            // Amplifier
            if (!string.IsNullOrEmpty(amplifierText))
            {
                parts.Add("|");
                parts.Add("amp:");
                parts.Add(amplifierText);
            }
            
            parts.Add(")");
            
            return SpacingRules.FormatWithSpacing(parts.ToArray());
        }
        
        /// <summary>
        /// Adds spacing between ColoredText segments using standard rules.
        /// This should be used when manually building ColoredText lists.
        /// </summary>
        public static List<ColoredText> AddSpacingBetweenSegments(List<ColoredText> segments)
        {
            if (segments == null || segments.Count <= 1)
                return segments ?? new List<ColoredText>();
            
            var result = new List<ColoredText>(segments.Count * 2);
            
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                
                if (i == 0)
                {
                    // First segment - no space before
                    result.Add(segment);
                }
                else
                {
                    var prevSegment = result[result.Count - 1];
                    
                    // Check if space is needed
                    if (SpacingRules.ShouldAddSpaceBetween(prevSegment.Text, segment.Text))
                    {
                        result.Add(new ColoredText(SpacingRules.SingleSpace, Colors.White));
                    }
                    
                    result.Add(segment);
                }
            }
            
            return result;
        }
    }
}

