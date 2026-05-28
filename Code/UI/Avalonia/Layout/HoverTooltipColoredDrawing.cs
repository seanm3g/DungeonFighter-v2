using System.Collections.Generic;
using RPGGame.UI.Avalonia.Renderers.Text;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Renders multi-segment colored tooltip lines on the hover overlay layer.
    /// </summary>
    public static class HoverTooltipColoredDrawing
    {
        /// <summary>
        /// Wraps and draws colored tooltip lines inside a framed panel. Returns the number of character rows used.
        /// </summary>
        public static int DrawColoredLines(
            GameCanvasControl canvas,
            int textX,
            int textY,
            List<List<ColoredText>> lines,
            int maxWidth,
            int maxLines)
        {
            if (lines == null || lines.Count == 0 || maxLines < 1)
                return 0;

            var overlayRenderer = new OverlaySegmentRenderer(canvas);
            int currentY = textY;
            int rowsUsed = 0;

            foreach (var line in lines)
            {
                if (rowsUsed >= maxLines)
                    break;

                if (line == null || line.Count == 0)
                {
                    currentY++;
                    rowsUsed++;
                    continue;
                }

                var wrapped = TextWrappingHelper.WrapColoredSegments(line, maxWidth);
                foreach (var wrappedLine in wrapped)
                {
                    if (rowsUsed >= maxLines)
                        break;
                    if (wrappedLine != null && wrappedLine.Count > 0)
                    {
                        ColoredTextRenderer.TrimTrailingWhitespaceFromSegments(wrappedLine);
                        if (wrappedLine.Count > 0)
                            overlayRenderer.RenderSegments(wrappedLine, textX, currentY, ColorConverter.ConvertSegmentToCanvasColor);
                    }
                    currentY++;
                    rowsUsed++;
                }
            }

            return rowsUsed;
        }

        /// <summary>
        /// Flattens colored lines to wrapped rows for tooltip box sizing (one display row per wrapped line; blank lines count as 1).
        /// </summary>
        public static int CountDisplayRows(List<List<ColoredText>> lines, int maxWidth, int maxLines)
        {
            if (lines == null || lines.Count == 0)
                return 0;

            int count = 0;
            foreach (var line in lines)
            {
                if (count >= maxLines)
                    break;
                if (line == null || line.Count == 0)
                {
                    count++;
                    continue;
                }
                var wrapped = TextWrappingHelper.WrapColoredSegments(line, maxWidth);
                count += wrapped.Count > 0 ? wrapped.Count : 1;
            }
            return System.Math.Min(count, maxLines);
        }
    }
}
