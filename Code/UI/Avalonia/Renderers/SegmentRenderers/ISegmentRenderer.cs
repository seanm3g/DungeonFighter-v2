using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Renderers.SegmentRenderers
{
    /// <summary>
    /// Interface for rendering colored text segments to the canvas.
    /// Strategy pattern allows different rendering approaches (template vs standard).
    /// </summary>
    internal interface ISegmentRenderer
    {
        /// <summary>
        /// Renders a segment and returns the next X position
        /// </summary>
        /// <param name="segment">The segment to render</param>
        /// <param name="canvasColor">The color to use for rendering</param>
        /// <param name="currentX">Current X position</param>
        /// <param name="lastRenderedX">Last rendered X position (for overlap detection)</param>
        /// <param name="lastColor">Last rendered color (for overlap detection)</param>
        /// <param name="y">Y position</param>
        /// <param name="lastRenderedXOut">Output parameter for the rendered X position</param>
        /// <returns>Next X position after rendering</returns>
        int RenderSegment(ColoredText segment, Color canvasColor, int currentX, 
            int lastRenderedX, Color? lastColor, int y, ref int lastRenderedXOut);
        
        /// <summary>
        /// Determines if this renderer should be used for the given segments
        /// </summary>
        bool ShouldUseRenderer(List<ColoredText> segments);
    }
}

