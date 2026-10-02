using System;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Computes character-grid size and font scale from available pixel space.
    /// Font scale fills the window height (panel rows + a thin outer bottom pad);
    /// column count shrinks/grows with width, leaving a thin outer right pad so the
    /// frame is not flush with the window edge. Per-font UI zoom resizes the main
    /// window (see MainWindowStartupSizing); it is not applied here.
    /// </summary>
    public static class CanvasGridSizer
    {
        public const int DesignGridWidth = 210;
        public const int DesignGridHeight = 52;

        /// <summary>
        /// Rows painted by persistent panels (<see cref="LayoutConstants.LEFT_PANEL_HEIGHT"/> = gridHeight + 1).
        /// </summary>
        public const int DesignLayoutRowCount = DesignGridHeight + 1;

        /// <summary>Empty character columns outside the right panel border (matches the existing left inset feel).</summary>
        public const int OuterPaddingRight = 1;

        /// <summary>Empty character rows below the panel bottom borders.</summary>
        public const int OuterPaddingBottom = 1;

        /// <summary>
        /// Total character rows the main canvas occupies (painted panels + bottom outer pad).
        /// </summary>
        public const int DesignCanvasRowCount = DesignLayoutRowCount + OuterPaddingBottom;

        /// <summary>
        /// Comfortable minimum column count for the action strip (not enforced by scaling).
        /// </summary>
        public const int MinGridWidth = 75;

        /// <summary>
        /// Absolute floor for column count when the window is extremely narrow.
        /// </summary>
        public const int AbsoluteMinGridWidth = 1;

        public const double MinCanvasScale = 0.4;
        public const double MaxCanvasScale = 20.0;

        /// <summary>Pixel-row count for painted panel borders (no bottom pad).</summary>
        public static int LayoutRowCount(int gridHeight) => Math.Max(1, gridHeight + 1);

        /// <summary>Pixel-row count for the full canvas including bottom outer pad.</summary>
        public static int CanvasRowCount(int gridHeight) => LayoutRowCount(gridHeight) + OuterPaddingBottom;

        /// <summary>Max character columns used by chrome (excludes right outer pad).</summary>
        public static int LayoutColumnCount(int gridWidth) =>
            Math.Max(AbsoluteMinGridWidth, gridWidth - OuterPaddingRight);

        /// <summary>
        /// Calculates scale and grid dimensions for the main game canvas.
        /// Scale fills <paramref name="availableHeight"/> across <see cref="DesignCanvasRowCount"/> rows.
        /// Per-font UI zoom (<c>Ctrl+/-</c>) resizes the main window instead of multiplying scale here,
        /// so the canvas always fills the client area with no letterbox gap.
        /// Grid width follows <paramref name="availableWidth"/> (includes right pad column).
        /// </summary>
        public static (double ScaleFactor, int GridWidth, int GridHeight) Calculate(
            double availableWidth,
            double availableHeight,
            double baseCharWidth,
            double baseCharHeight)
        {
            if (availableWidth <= 0
                || availableHeight <= 0
                || double.IsInfinity(availableWidth)
                || double.IsInfinity(availableHeight)
                || baseCharWidth <= 0
                || baseCharHeight <= 0)
            {
                return (1.0, DesignGridWidth, DesignGridHeight);
            }

            // Fill vertically for painted panels + thin bottom outer pad.
            double scaleFactor = Math.Clamp(
                availableHeight / (DesignCanvasRowCount * baseCharHeight),
                MinCanvasScale,
                MaxCanvasScale);

            // Column count fits the available pixel width (last column is outer right pad).
            double scaledCharWidth = baseCharWidth * scaleFactor;
            int gridWidth = (int)Math.Floor(availableWidth / scaledCharWidth + 1e-6);
            gridWidth = Math.Max(AbsoluteMinGridWidth + OuterPaddingRight, gridWidth);

            return (scaleFactor, gridWidth, DesignGridHeight);
        }

        /// <summary>
        /// Aspect-locked sizing for compact auxiliary canvases (Action Lab tools, etc.).
        /// </summary>
        public static (double ScaleFactor, int GridWidth, int GridHeight) CalculateFixedGrid(
            double availableWidth,
            double availableHeight,
            double baseCharWidth,
            double baseCharHeight,
            int fixedGridWidth,
            int fixedGridHeight)
        {
            int targetW = Math.Max(8, fixedGridWidth);
            int targetH = Math.Max(8, fixedGridHeight);

            if (availableWidth <= 0
                || availableHeight <= 0
                || double.IsInfinity(availableWidth)
                || double.IsInfinity(availableHeight)
                || baseCharWidth <= 0
                || baseCharHeight <= 0)
            {
                return (1.0, targetW, targetH);
            }

            double scaleX = availableWidth / (targetW * baseCharWidth);
            double scaleY = availableHeight / (targetH * baseCharHeight);
            double scaleFactor = Math.Clamp(Math.Min(scaleX, scaleY), MinCanvasScale, MaxCanvasScale);
            return (scaleFactor, targetW, targetH);
        }
    }
}
