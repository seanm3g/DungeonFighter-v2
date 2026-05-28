using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.Avalonia;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Renderers.Text
{
    /// <summary>
    /// Renders colored text segments on the hover overlay layer (same positioning rules as <see cref="SegmentRenderer"/>, but uses overlay text APIs).
    /// </summary>
    public sealed class OverlaySegmentRenderer
    {
        private readonly GameCanvasControl canvas;

        public OverlaySegmentRenderer(GameCanvasControl canvas)
        {
            this.canvas = canvas ?? throw new System.ArgumentNullException(nameof(canvas));
        }

        public void RenderSegments(List<ColoredText> segments, int x, int y, System.Func<ColoredText, Color> colorConverter)
        {
            if (segments == null || segments.Count == 0)
                return;

            int currentX = x;
            int lastRenderedX = int.MinValue;
            Color? lastColor = null;

            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment.Text))
                    continue;

                var canvasColor = colorConverter(segment);
                int segmentCharWidth = segment.Text.Length;
                bool isWhitespaceSegment = segment.Text.Trim().Length == 0 && segment.Text.Length > 0;
                int renderX = currentX;

                if (lastRenderedX != int.MinValue && renderX == lastRenderedX)
                {
                    if (canvasColor == lastColor && !isWhitespaceSegment)
                    {
                        canvas.AppendOverlayText(renderX, y, segment.Text, canvasColor);
                        lastRenderedX = renderX;
                        currentX = renderX + segmentCharWidth;
                        continue;
                    }
                    renderX = currentX;
                }

                canvas.AddOverlayText(renderX, y, segment.Text, canvasColor);
                lastRenderedX = renderX;
                currentX = renderX + segmentCharWidth;
                lastColor = canvasColor;
            }
        }
    }
}
