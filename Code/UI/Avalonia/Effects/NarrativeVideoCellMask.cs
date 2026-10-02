using System;
using Avalonia.Media;

namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Builds a per-cell opacity grid from combat-log glyph colors (luminance → alpha, optional halo).
    /// </summary>
    public static class NarrativeVideoCellMask
    {
        /// <summary>Rec. 709 relative luminance in 0–1.</summary>
        public static double Luminance(byte r, byte g, byte b) =>
            (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255.0;

        /// <summary>Rec. 709 relative luminance in 0–1 from an Avalonia color.</summary>
        public static double Luminance(Color color) => Luminance(color.R, color.G, color.B);

        /// <summary>
        /// True when any cell is a non-whitespace glyph (narrative log has started / has content).
        /// </summary>
        public static bool HasOccupiedGlyphs(ReadOnlySpan<char> glyphs)
        {
            for (int i = 0; i < glyphs.Length; i++)
            {
                char c = glyphs[i];
                if (!char.IsWhiteSpace(c) && c != '\0')
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Occupied-cell opacity before halo: luminance × maxOpacity, clamped to 0–1.
        /// Spaces and other whitespace contribute 0.
        /// </summary>
        public static float CellOpacity(char glyph, Color color, double maxOpacity)
        {
            if (char.IsWhiteSpace(glyph) || glyph == '\0')
                return 0f;
            double max = Math.Clamp(maxOpacity, 0.0, 1.0);
            return (float)Math.Clamp(Luminance(color) * max, 0.0, 1.0);
        }

        /// <summary>
        /// Fills <paramref name="dest"/> (row-major, length = width×height) with cell opacities,
        /// then dilates by <paramref name="haloCells"/> using <paramref name="haloOpacityScale"/>.
        /// </summary>
        public static void BuildOpacityGrid(
            float[] dest,
            int width,
            int height,
            ReadOnlySpan<char> glyphs,
            ReadOnlySpan<Color> colors,
            double maxOpacity,
            int haloCells,
            double haloOpacityScale)
        {
            if (dest == null)
                throw new ArgumentNullException(nameof(dest));
            if (width <= 0 || height <= 0)
                return;
            if (dest.Length < width * height)
                throw new ArgumentException("dest is too small for width×height.", nameof(dest));
            if (glyphs.Length < width * height || colors.Length < width * height)
                throw new ArgumentException("glyphs/colors must cover width×height.");

            int n = width * height;
            for (int i = 0; i < n; i++)
                dest[i] = CellOpacity(glyphs[i], colors[i], maxOpacity);

            int halo = Math.Max(0, haloCells);
            if (halo == 0)
                return;

            double scale = Math.Clamp(haloOpacityScale, 0.0, 1.0);
            if (scale <= 0.0)
                return;

            // Copy primary values so halo reads a stable source.
            var primary = new float[n];
            Array.Copy(dest, primary, n);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float src = primary[y * width + x];
                    if (src <= 0f)
                        continue;
                    float haloVal = (float)(src * scale);
                    for (int dy = -halo; dy <= halo; dy++)
                    {
                        int ny = y + dy;
                        if (ny < 0 || ny >= height)
                            continue;
                        for (int dx = -halo; dx <= halo; dx++)
                        {
                            if (dx == 0 && dy == 0)
                                continue;
                            int nx = x + dx;
                            if (nx < 0 || nx >= width)
                                continue;
                            int idx = ny * width + nx;
                            if (haloVal > dest[idx])
                                dest[idx] = haloVal;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Writes an opacity grid into a premultiplied BGRA buffer (white RGB, alpha from opacity).
        /// Each cell becomes a <paramref name="cellPixelWidth"/> × <paramref name="cellPixelHeight"/> block.
        /// </summary>
        public static void WriteOpacityMaskBgra(
            Span<byte> bgraPixels,
            int bitmapWidth,
            int bitmapHeight,
            float[] opacityGrid,
            int gridWidth,
            int gridHeight,
            int cellPixelWidth,
            int cellPixelHeight)
        {
            if (bitmapWidth <= 0 || bitmapHeight <= 0 || gridWidth <= 0 || gridHeight <= 0)
                return;
            if (cellPixelWidth <= 0 || cellPixelHeight <= 0)
                return;
            if (opacityGrid == null || opacityGrid.Length < gridWidth * gridHeight)
                throw new ArgumentException("opacityGrid too small.", nameof(opacityGrid));

            bgraPixels.Clear();

            for (int gy = 0; gy < gridHeight; gy++)
            {
                int y0 = gy * cellPixelHeight;
                int y1 = Math.Min(bitmapHeight, y0 + cellPixelHeight);
                if (y0 >= bitmapHeight)
                    break;
                for (int gx = 0; gx < gridWidth; gx++)
                {
                    float opacity = opacityGrid[gy * gridWidth + gx];
                    if (opacity <= 0.001f)
                        continue;
                    byte a = (byte)Math.Clamp((int)Math.Round(opacity * 255.0), 0, 255);
                    int x0 = gx * cellPixelWidth;
                    int x1 = Math.Min(bitmapWidth, x0 + cellPixelWidth);
                    if (x0 >= bitmapWidth)
                        break;
                    for (int y = y0; y < y1; y++)
                    {
                        int row = y * bitmapWidth * 4;
                        for (int x = x0; x < x1; x++)
                        {
                            int i = row + x * 4;
                            bgraPixels[i] = a;       // B (premul)
                            bgraPixels[i + 1] = a;   // G
                            bgraPixels[i + 2] = a;   // R
                            bgraPixels[i + 3] = a;   // A
                        }
                    }
                }
            }
        }
    }
}
