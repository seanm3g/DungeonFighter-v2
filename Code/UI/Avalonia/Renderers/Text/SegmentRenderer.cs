using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Renderers.SegmentRenderers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Renderers.Text
{

    /// <summary>
    /// Handles segment rendering with Strategy pattern for template vs standard rendering.
    /// </summary>
    public class SegmentRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ISegmentRenderer templateRenderer;
        private readonly ISegmentRenderer standardRenderer;
        
        public SegmentRenderer(GameCanvasControl canvas)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.templateRenderer = new TemplateSegmentRenderer(canvas);
            this.standardRenderer = new StandardSegmentRenderer(canvas);
        }
        
        /// <summary>
        /// Renders a list of colored text segments to the canvas.
        /// Uses Strategy pattern to select appropriate renderer (template vs standard).
        /// </summary>
        public void RenderSegments(List<ColoredText> segments, int x, int y, Func<Color, Color> colorConverter)
        {
            if (segments == null || segments.Count == 0)
                return;
            
            // Select appropriate renderer using Strategy pattern
            ISegmentRenderer renderer = templateRenderer.ShouldUseRenderer(segments) 
                ? templateRenderer 
                : standardRenderer;
            
            // Use integer X positions throughout (character grid system)
            // No floating point needed - monospace font means 1 char = 1 position
            int currentX = x;
            int lastRenderedX = int.MinValue;
            Color? lastColor = null;
            
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;
                
                var canvasColor = colorConverter(segment.Color);
                int renderedX = int.MinValue;
                
                // Use selected renderer to render segment
                // Renderer returns double for interface compatibility, but value is always integer
                double newXDouble = renderer.RenderSegment(segment, canvasColor, currentX, 
                    lastRenderedX, lastColor, y, ref renderedX);
                
                // Convert back to int (should always be exact since renderer uses character count)
                currentX = (int)System.Math.Round(newXDouble);
                
                if (renderedX != int.MinValue)
                {
                    lastRenderedX = renderedX;
                }
                lastColor = canvasColor;
            }
        }
    }
}

