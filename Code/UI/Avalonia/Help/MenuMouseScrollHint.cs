using Avalonia.Media;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Help
{
    /// <summary>
    /// Soft-gray animal-mouse + up/down arrow shown in menus when mouse-wheel scrolling is available.
    /// Kept deemphasized so it cues scroll without competing with menu content.
    /// </summary>
    public static class MenuMouseScrollHint
    {
        public const string Glyph = AsciiArtAssets.UIElements.MouseScrollHint;

        /// <summary>Approximate character columns for right-alignment (emoji mouse ~2 + ↕).</summary>
        public const int GlyphColumns = 3;

        /// <summary>Soft gray so the affordance stays secondary to menu text.</summary>
        public static Color Color => AsciiArtAssets.Colors.DarkGray;

        public static void Draw(GameCanvasControl canvas, int x, int y)
        {
            if (canvas == null)
                return;

            canvas.AddText(x, y, Glyph, Color);
        }

        /// <summary>
        /// Draws the affordance near the right edge of a panel row.
        /// </summary>
        public static void DrawRightAligned(GameCanvasControl canvas, int panelX, int panelWidth, int y, int rightPad = 2)
        {
            if (canvas == null || panelWidth <= 0)
                return;

            int x = panelX + panelWidth - rightPad - GlyphColumns;
            if (x < panelX)
                x = panelX;

            Draw(canvas, x, y);
        }
    }
}
