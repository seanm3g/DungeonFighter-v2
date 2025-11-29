using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.ColorSystem.Helpers
{
    /// <summary>
    /// Helper for spacing logic in ColoredTextBuilder
    /// </summary>
    internal static class SpacingHelper
    {
        /// <summary>
        /// Processes segments with spacing and merging logic
        /// </summary>
        public static List<ColoredText> ProcessSegments(List<ColoredText> segments)
        {
            if (segments.Count == 0)
                return new List<ColoredText>();
            
            var result = new List<ColoredText>(segments.Count);
            ColoredText? currentSegment = null;
            bool previousWasSpace = false;
            
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                bool isSpaceOnly = segment.Text.Trim().Length == 0 && segment.Text.Length > 0;
                string processedText;
                
                if (isSpaceOnly)
                {
                    processedText = CombatLogSpacingManager.SingleSpace;
                }
                else
                {
                    processedText = segment.Text.TrimStart().TrimEnd();
                    if (string.IsNullOrEmpty(processedText))
                        continue;
                }
                
                if (currentSegment != null && !previousWasSpace)
                {
                    bool needsSpace = CombatLogSpacingManager.ShouldAddSpaceBetween(currentSegment.Text, processedText);
                    
                    if (needsSpace)
                    {
                        result.Add(currentSegment);
                        result.Add(new ColoredText(CombatLogSpacingManager.SingleSpace, Colors.White));
                        previousWasSpace = true;
                        currentSegment = new ColoredText(processedText, segment.Color);
                        continue;
                    }
                }
                
                if (isSpaceOnly)
                {
                    if (currentSegment != null)
                        result.Add(currentSegment);
                    result.Add(new ColoredText(processedText, segment.Color));
                    currentSegment = null;
                    previousWasSpace = true;
                }
                else if (currentSegment == null)
                {
                    currentSegment = new ColoredText(processedText, segment.Color);
                    previousWasSpace = false;
                }
                else if (ColorValidator.AreColorsEqual(currentSegment.Color, segment.Color))
                {
                    currentSegment = new ColoredText(currentSegment.Text + processedText, currentSegment.Color);
                    previousWasSpace = false;
                }
                else
                {
                    result.Add(currentSegment);
                    currentSegment = new ColoredText(processedText, segment.Color);
                    previousWasSpace = false;
                }
            }
            
            if (currentSegment != null && !string.IsNullOrEmpty(currentSegment.Text))
                result.Add(currentSegment);
            
            return result;
        }
    }
}

