using System;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Calculates layout positions for menu screens
    /// </summary>
    public static class MenuLayoutCalculator
    {
        // Design-time defaults (prefer live LayoutConstants / chromeless content rect at render time).
        public const int SCREEN_WIDTH = 210;
        /// <summary>Live grid center — tracks window width / Ctrl+/- column count (not the design 105).</summary>
        public static int SCREEN_CENTER => LayoutConstants.SCREEN_CENTER;
        public const int LEFT_MARGIN = 2;
        public const int CONTENT_WIDTH = 206;
        
        /// <summary>
        /// Calculates centered menu position.
        /// Vertical packing uses <see cref="CenterColumnTextZoom.LineStride"/> so Ctrl+/- zoomed
        /// center-column options do not overlap.
        /// </summary>
        public static (int x, int y) CalculateCenteredMenu(int x, int y, int width, int height, int optionCount, int menuWidth = 0)
        {
            int stride = CenterColumnTextZoom.LineStride;
            int menuStartX = menuWidth > 0 
                ? x + (width / 2) - (menuWidth / 2)
                : x + (width / 2) - 15; // Default center offset
            int menuStartY = y + (height / 2) - (optionCount * stride / 2);
            return (menuStartX, menuStartY);
        }

        /// <summary>Advance to the next menu option row under the current center-column text zoom.</summary>
        public static int NextMenuLineY(int y) => y + CenterColumnTextZoom.LineStride;
        
        /// <summary>
        /// Calculates top-left justified menu position
        /// </summary>
        public static (int x, int y) CalculateTopLeftMenu(int x, int y, int margin = 2)
        {
            return (x + margin, y + margin);
        }
        
        /// <summary>
        /// Calculates centered text position
        /// </summary>
        public static int CalculateCenteredTextX(int x, int width, int textLength)
        {
            return x + (width / 2) - (textLength / 2);
        }

        /// <summary>
        /// Grid columns a center-column string occupies after Ctrl+/- glyph zoom.
        /// Layout still uses character indices; paint scales glyphs, so centering must use this.
        /// </summary>
        public static int VisualCenterColumnColumns(int charCount)
        {
            if (charCount <= 0)
                return 0;
            double z = CenterColumnTextZoom.FontScale;
            if (z <= 1.0)
                return charCount;
            return Math.Max(1, (int)Math.Ceiling(charCount * z - 1e-9));
        }

        /// <summary>
        /// Centers text in <paramref name="width"/> accounting for center-column glyph zoom.
        /// </summary>
        public static int CalculateCenteredZoomedTextX(int x, int width, int textLength)
        {
            return CalculateCenteredTextX(x, width, VisualCenterColumnColumns(textLength));
        }

        /// <summary>
        /// Builds a decorative horizontal rule that fits inside a zoomed center-column content width.
        /// Uses ASCII '-' so pixel fonts without box-drawing glyphs do not paint wider than one cell.
        /// </summary>
        public static string BuildDecorativeRule(int desiredLength, int contentWidth)
        {
            int maxCells = Math.Max(1, contentWidth);
            int maxChars = CenterColumnTextZoom.ScaleWrapWidth(maxCells);
            int len = Math.Max(1, Math.Min(desiredLength, maxChars));
            return new string('-', len);
        }
        
        /// <summary>
        /// Calculates panel division for multi-panel layouts
        /// </summary>
        public static int CalculatePanelHeight(int totalHeight, int panelCount, int spacing = 1)
        {
            return (totalHeight - (spacing * (panelCount - 1))) / panelCount;
        }
    }
}

