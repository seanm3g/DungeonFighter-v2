using Avalonia.Media;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Shared styling and painting for pointer-hover detail panels (center column and overlapping regions).
    /// </summary>
    public static class HoverTooltipDrawing
    {
        /// <summary>Tooltip frame color (matches existing action/stat hover chrome).</summary>
        public static Color BorderColor => AsciiArtAssets.Colors.Yellow;

        /// <summary>Fully opaque fill so underlying grid text does not show through.</summary>
        public static Color FillColor => Colors.Black;

        /// <summary>
        /// Device pixels to expand opaque fill on each side so glyph/halos do not reveal content outside the cell grid.
        /// </summary>
        public const int OpaqueFillBleedDevicePixels = 2;

        /// <summary>
        /// Clears a padded character region (neighbor columns/rows), then draws a bordered box with solid black fill.
        /// </summary>
        public static void DrawFramedPanel(
            GameCanvasControl canvas,
            int boxX,
            int boxY,
            int boxW,
            int boxH,
            int innerLeft,
            int innerTop,
            int innerRightInclusive,
            int maxBottomInclusive,
            int cellPad = 1)
        {
            // Wipe prior hover overlay in the whole inner content band first. A shorter tooltip (e.g. Armor) after a
            // taller one (e.g. Speed) would otherwise leave orphan overlay lines below the new box.
            int innerFullW = innerRightInclusive - innerLeft + 1;
            int innerFullH = maxBottomInclusive - innerTop + 1;
            canvas.ClearOverlayTextInArea(innerLeft, innerTop, innerFullW, innerFullH);
            canvas.ClearOverlayBoxesInArea(innerLeft, innerTop, innerFullW, innerFullH);

            var (gx, gy, gw, gh) = GetPaddedClearRegion(
                boxX, boxY, boxW, boxH, innerLeft, innerTop, innerRightInclusive, maxBottomInclusive, cellPad);

            canvas.ClearTextInArea(gx, gy, gw, gh);
            canvas.ClearBoxesInArea(gx, gy, gw, gh);
            canvas.AddOverlayBox(boxX, boxY, boxW, boxH, BorderColor, FillColor, OpaqueFillBleedDevicePixels);
        }

        /// <summary>
        /// Character-grid clear rectangle one cell larger per side than the box when possible (clamped to inner bounds).
        /// </summary>
        public static (int gx, int gy, int gw, int gh) GetPaddedClearRegion(
            int boxX,
            int boxY,
            int boxW,
            int boxH,
            int innerLeft,
            int innerTop,
            int innerRightInclusive,
            int maxBottomInclusive,
            int pad = 1)
        {
            int gx = System.Math.Max(innerLeft, boxX - pad);
            int gy = System.Math.Max(innerTop, boxY - pad);
            int gx2 = System.Math.Min(innerRightInclusive, boxX + boxW - 1 + pad);
            int gy2 = System.Math.Min(maxBottomInclusive, boxY + boxH - 1 + pad);
            int gw = gx2 - gx + 1;
            int gh = gy2 - gy + 1;
            return (gx, gy, gw, gh);
        }
    }
}
