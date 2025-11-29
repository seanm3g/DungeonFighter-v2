using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Renderers.SegmentRenderers
{
    /// <summary>
    /// Renders template-based segments (primarily single-character) using integer positioning.
    /// This ensures proper alignment for character-by-character color templates.
    /// </summary>
    internal class TemplateSegmentRenderer : ISegmentRenderer
    {
        private readonly GameCanvasControl canvas;
        
        public TemplateSegmentRenderer(GameCanvasControl canvas)
        {
            this.canvas = canvas;
        }
        
        public bool ShouldUseRenderer(List<ColoredText> segments)
        {
            if (segments == null || segments.Count == 0)
                return false;
            
            int validSegments = 0;
            int singleCharSegments = 0;
            
            foreach (var segment in segments)
            {
                if (!string.IsNullOrEmpty(segment.Text))
                {
                    validSegments++;
                    if (segment.Text.Length == 1)
                    {
                        singleCharSegments++;
                    }
                }
            }
            
            // Use template rendering if majority are single characters
            return validSegments > 0 && (singleCharSegments * 2) > validSegments;
        }
        
        public int RenderSegment(ColoredText segment, Color canvasColor, int currentX, 
            int lastRenderedX, Color? lastColor, int y, ref int lastRenderedXOut)
        {
            bool isSingleChar = segment.Text.Length == 1;
            int alignedX = currentX;
            
            // Align to integer when transitioning from multi-char to single-char
            if (!isSingleChar && lastRenderedX != int.MinValue)
            {
                alignedX = (int)System.Math.Round((double)currentX);
            }
            
            // Prevent overlap for different colors at same position
            if (alignedX == lastRenderedX && lastRenderedX != int.MinValue && canvasColor != lastColor)
            {
                alignedX = lastRenderedX + 1;
            }
            
            // Render segment
            if (!isSingleChar)
            {
                // Multi-char in template rendering - use character count
                canvas.AddText(alignedX, y, segment.Text, canvasColor);
                lastRenderedXOut = alignedX;
                return currentX + segment.Text.Length;
            }
            else
            {
                // Single character - render and advance by exactly 1
                canvas.AddText(alignedX, y, segment.Text, canvasColor);
                lastRenderedXOut = alignedX;
                return alignedX + 1;
            }
        }
    }
}

