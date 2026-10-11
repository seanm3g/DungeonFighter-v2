using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.Avalonia.Canvas;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Builds a letter-cell opacity mask for DistortionPulse explosion frames
    /// (same per-glyph idea as <see cref="NarrativeVideoCellMask"/>).
    /// </summary>
    public static class EmphasisExplosionLetterMask
    {
        /// <summary>
        /// Fills <paramref name="opacityGrid"/> (row-major, length ≥ cols×rows) with opacities for
        /// occupied non-overlay center-panel glyphs whose cells fall in the grid window.
        /// Whitespace / empty cells stay 0.
        /// </summary>
        public static void BuildOccupiedGlyphOpacity(
            float[] opacityGrid,
            int gridOriginCol,
            int gridOriginRow,
            int cols,
            int rows,
            IReadOnlyList<CanvasText> textElements,
            double maxOpacity = 1.0)
        {
            if (opacityGrid == null)
                throw new ArgumentNullException(nameof(opacityGrid));
            if (cols <= 0 || rows <= 0)
                return;
            if (opacityGrid.Length < cols * rows)
                throw new ArgumentException("opacityGrid too small for cols×rows.", nameof(opacityGrid));

            Array.Clear(opacityGrid, 0, cols * rows);
            if (textElements == null || textElements.Count == 0)
                return;

            double max = Math.Clamp(maxOpacity, 0.0, 1.0);
            int x0 = gridOriginCol;
            int x1 = gridOriginCol + cols;
            int y0 = gridOriginRow;
            int y1 = gridOriginRow + rows;

            for (int i = 0; i < textElements.Count; i++)
            {
                var text = textElements[i];
                if (text == null || text.IsOverlay)
                    continue;
                if (!LayoutConstants.ContainsCenterPanelContent(text.X, text.Y))
                    continue;

                string content = text.Content ?? string.Empty;
                if (content.Length == 0)
                    continue;

                int row = text.Y;
                if (row < y0 || row >= y1)
                    continue;

                Color color = text.Color;
                for (int c = 0; c < content.Length; c++)
                {
                    int col = text.X + c;
                    if (col < x0 || col >= x1)
                        continue;

                    char glyph = content[c];
                    float opacity = NarrativeVideoCellMask.CellOpacity(glyph, color, max);
                    if (opacity <= 0f)
                        continue;

                    int localX = col - x0;
                    int localY = row - y0;
                    int idx = localY * cols + localX;
                    if (opacity > opacityGrid[idx])
                        opacityGrid[idx] = opacity;
                }
            }
        }

        /// <summary>
        /// Content-pixel AABB → inclusive grid cell window covering that rect.
        /// </summary>
        public static void ResolveGridWindow(
            double minX,
            double minY,
            double maxX,
            double maxY,
            double charWidth,
            double charHeight,
            out int originCol,
            out int originRow,
            out int cols,
            out int rows)
        {
            if (charWidth <= 0 || charHeight <= 0 || maxX <= minX || maxY <= minY)
            {
                originCol = originRow = 0;
                cols = rows = 0;
                return;
            }

            originCol = (int)Math.Floor(minX / charWidth);
            originRow = (int)Math.Floor(minY / charHeight);
            int endCol = (int)Math.Ceiling(maxX / charWidth);
            int endRow = (int)Math.Ceiling(maxY / charHeight);
            cols = Math.Max(0, endCol - originCol);
            rows = Math.Max(0, endRow - originRow);
        }
    }
}
