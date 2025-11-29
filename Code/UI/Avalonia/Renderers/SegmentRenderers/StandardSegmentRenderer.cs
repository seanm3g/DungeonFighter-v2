using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Renderers.SegmentRenderers
{
    /// <summary>
    /// Renders standard multi-character segments using measured width positioning.
    /// This provides accurate spacing for normal text rendering.
    /// </summary>
    internal class StandardSegmentRenderer : ISegmentRenderer
    {
        private readonly GameCanvasControl canvas;
        
        public StandardSegmentRenderer(GameCanvasControl canvas)
        {
            this.canvas = canvas;
        }
        
        public bool ShouldUseRenderer(List<ColoredText> segments)
        {
            // Standard renderer is the default - always returns true
            // Template renderer will be checked first
            return true;
        }
        
        public int RenderSegment(ColoredText segment, Color canvasColor, int currentX, 
            int lastRenderedX, Color? lastColor, int y, ref int lastRenderedXOut)
        {
            double segmentWidth = canvas.MeasureTextWidth(segment.Text);
            double preciseX = currentX;
            int roundedX = (int)System.Math.Round(preciseX);
            
            // Prevent overlap for different colors at same position
            if (roundedX == lastRenderedX && lastRenderedX != int.MinValue && canvasColor != lastColor)
            {
                roundedX = lastRenderedX + 1;
                preciseX = roundedX;
            }
            
            // Combine segments at same position with same color
            if (roundedX == lastRenderedX && lastRenderedX != int.MinValue && canvasColor == lastColor)
            {
                canvas.AppendText(roundedX, y, segment.Text, canvasColor);
                preciseX += segmentWidth;
            }
            else
            {
                canvas.AddText(roundedX, y, segment.Text, canvasColor);
                preciseX += segmentWidth;
            }
            
            lastRenderedXOut = roundedX;
            return (int)System.Math.Round(preciseX);
        }
    }
}

