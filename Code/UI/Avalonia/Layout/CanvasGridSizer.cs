using System;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Computes character-grid size and font scale from available pixel space.
    /// Font scale fills the window height (panel rows + a thin outer bottom pad), then
    /// multiplies by per-font UI zoom (<c>Ctrl+/-</c>) so glyphs grow/shrink inside a
    /// fixed window. Column count shrinks/grows with width, leaving a thin outer right pad.
    /// Side/top chrome character sizes are adjusted separately in <see cref="LayoutConstants"/>
    /// so left/top/right panels keep near their design pixel footprints while the center flexes.
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
        /// Base scale fills <paramref name="availableHeight"/> across <see cref="DesignCanvasRowCount"/> rows,
        /// then multiplies by <paramref name="uiZoom"/> (window size stays fixed).
        /// Grid width follows <paramref name="availableWidth"/> (includes right pad column).
        /// </summary>
        public static (double ScaleFactor, int GridWidth, int GridHeight) Calculate(
            double availableWidth,
            double availableHeight,
            double baseCharWidth,
            double baseCharHeight,
            double uiZoom = 1.0)
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

            double safeZoom = uiZoom > 0 && !double.IsNaN(uiZoom) && !double.IsInfinity(uiZoom)
                ? uiZoom
                : 1.0;

            // Fill vertically for painted panels + thin bottom outer pad, then apply UI zoom.
            double scaleFactor = Math.Clamp(
                availableHeight / (DesignCanvasRowCount * baseCharHeight) * safeZoom,
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

        /// <summary>
        /// Pixel origin for drawing the character grid inside the control viewport.
        /// Centers the content so Ctrl+/- zoom keeps menus/panels oriented on screen
        /// (letterbox when content is smaller; centered crop when zoom makes it larger).
        /// </summary>
        public static (double OffsetX, double OffsetY) CalculateContentOrigin(
            double viewportWidth,
            double viewportHeight,
            double contentWidth,
            double contentHeight)
        {
            if (viewportWidth <= 0
                || viewportHeight <= 0
                || contentWidth <= 0
                || contentHeight <= 0
                || double.IsInfinity(viewportWidth)
                || double.IsInfinity(viewportHeight)
                || double.IsInfinity(contentWidth)
                || double.IsInfinity(contentHeight)
                || double.IsNaN(viewportWidth)
                || double.IsNaN(viewportHeight)
                || double.IsNaN(contentWidth)
                || double.IsNaN(contentHeight))
            {
                return (0, 0);
            }

            // Whole pixels keep pixel fonts on the device grid.
            double offsetX = Math.Round((viewportWidth - contentWidth) / 2.0);
            double offsetY = Math.Round((viewportHeight - contentHeight) / 2.0);
            return (offsetX, offsetY);
        }
    }
}
