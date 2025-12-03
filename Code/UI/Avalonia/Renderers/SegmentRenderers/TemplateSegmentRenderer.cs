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
        
        public double RenderSegment(ColoredText segment, Color canvasColor, int currentX, 
            int lastRenderedX, Color? lastColor, int y, ref int lastRenderedXOut)
        {
            // Use character count for positioning (monospace font = 1 char = 1 position)
            // This eliminates floating point precision issues that cause text shifting
            int segmentCharWidth = segment.Text.Length;
            
            // Check if this is a whitespace segment - these should always be rendered separately
            // to preserve spacing in multi-word names like "Celestial Observatory"
            bool isWhitespaceSegment = segment.Text.Trim().Length == 0 && segment.Text.Length > 0;
            
            // Use integer X position directly (no rounding needed)
            int renderX = currentX;
            
            // CRITICAL: Ensure segments are positioned sequentially to prevent overwrites
            // If this would render at the same position as the last segment, we need to handle it carefully
            // Note: lastRenderedX is the START position of the previous segment
            // currentX should already be the correct next position (lastRenderedX + previous segment length)
            if (lastRenderedX != int.MinValue && renderX == lastRenderedX)
            {
                // Same position - check if we can append or need to offset
                if (canvasColor == lastColor && !isWhitespaceSegment)
                {
                    // Same color, not whitespace - safe to append (text will be concatenated)
                    canvas.AppendText(renderX, y, segment.Text, canvasColor);
                    // Advance position by character count
                    // CRITICAL: Use renderX (actual rendered position) + segmentCharWidth for next position
                    lastRenderedXOut = renderX;
                    return (double)(renderX + segmentCharWidth); // Return as double for interface compatibility
                }
                else
                {
                    // Different color or whitespace - must render at different position to avoid overwrite
                    // Use currentX (which should be the correct next position) instead of lastRenderedX + 1
                    // This ensures we position after the previous segment, not just 1 position after its start
                    renderX = currentX;
                }
            }
            
            // Render at calculated position
            // IMPORTANT: AddText removes existing text at this position, so we've ensured
            // this position is unique or we're appending above
            canvas.AddText(renderX, y, segment.Text, canvasColor);
            
            // Advance position by character count (no floating point needed)
            // CRITICAL: Use renderX (actual rendered position) + segmentCharWidth for next position
            // This ensures correct positioning even when renderX was adjusted
            lastRenderedXOut = renderX;
            
            // Return the position AFTER this segment (renderX + segment length)
            // This is the correct next position for the next segment
            return (double)(renderX + segmentCharWidth);
        }
    }
}

