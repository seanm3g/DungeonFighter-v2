using System;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Ctrl+/- UI zoom applies only to center-column glyphs. The character grid and
    /// left/right chrome stay at window-fit 100%; these helpers size center text/layout.
    /// </summary>
    public static class CenterColumnTextZoom
    {
        /// <summary>Font scale for center-column text (same as <see cref="GameFonts.ActiveZoom"/>).</summary>
        public static double FontScale
        {
            get
            {
                double z = GameFonts.ActiveZoom;
                if (z <= 0 || double.IsNaN(z) || double.IsInfinity(z))
                    return 1.0;
                return z;
            }
        }

        /// <summary>
        /// Grid rows reserved per visual center-column line so zoomed glyphs do not overlap.
        /// At 100% this is 1; above 100% it is ceil(zoom).
        /// </summary>
        public static int LineStride
        {
            get
            {
                double z = FontScale;
                if (z <= 1.0)
                    return 1;
                return Math.Max(1, (int)Math.Ceiling(z - 1e-9));
            }
        }

        /// <summary>
        /// Character columns available for wrapping when center glyphs are scaled by <see cref="FontScale"/>.
        /// </summary>
        public static int ScaleWrapWidth(int cellWidth)
        {
            if (cellWidth <= 1)
                return 1;
            double z = FontScale;
            if (z <= 1.0)
                return cellWidth;
            return Math.Max(1, (int)Math.Floor(cellWidth / z));
        }

        /// <summary>
        /// Visible grid rows that still fit zoomed center lines inside a cell-row viewport.
        /// </summary>
        public static int ScaleVisibleRows(int cellRows)
        {
            if (cellRows <= 1)
                return 1;
            int stride = LineStride;
            if (stride <= 1)
                return cellRows;
            return Math.Max(1, cellRows / stride);
        }

        /// <summary>
        /// True when grid X is in the flexible center column (not left/right chrome).
        /// Matches strip, sequence HUD, combat log, and menus drawn between the side panels.
        /// </summary>
        public static bool IsCenterColumnX(int gridX)
        {
            int leftEnd = LayoutConstants.LEFT_PANEL_X + LayoutConstants.LEFT_PANEL_WIDTH;
            int rightStart = LayoutConstants.RIGHT_PANEL_X;
            return gridX >= leftEnd && gridX < rightStart;
        }

        /// <summary>Snapped EM size for center-column text at the current UI zoom.</summary>
        public static double ScaledFontSize(double baseFontSize)
        {
            if (baseFontSize <= 0)
                return 1.0;
            return Math.Max(1.0, Math.Round(baseFontSize * FontScale));
        }
    }
}
