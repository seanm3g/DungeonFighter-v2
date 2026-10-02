using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Brightens interactive (clickable) canvas text when the mouse wake is near,
    /// using the same wake radius / near→far falloff as <see cref="WindSwayField"/>.
    /// </summary>
    public static class InteractiveTextHighlight
    {
        /// <summary>Packs a character-grid cell for set membership tests.</summary>
        public static long PackCell(int x, int y) => ((long)y << 32) | (uint)x;

        /// <summary>
        /// Builds a set of character cells covered by clickable hit regions.
        /// </summary>
        public static HashSet<long> BuildInteractiveCells(IReadOnlyList<ClickableElement>? elements)
        {
            var cells = new HashSet<long>();
            if (elements == null || elements.Count == 0)
                return cells;

            for (int i = 0; i < elements.Count; i++)
            {
                var e = elements[i];
                if (e == null || e.Width <= 0 || e.Height <= 0)
                    continue;

                int x1 = e.X + e.Width;
                int y1 = e.Y + e.Height;
                for (int y = e.Y; y < y1; y++)
                {
                    for (int x = e.X; x < x1; x++)
                        cells.Add(PackCell(x, y));
                }
            }

            return cells;
        }

        /// <summary>
        /// Cheap fingerprint of clickable regions so callers can reuse a cell set when unchanged.
        /// Mixes count and each region's bounds (identity of element objects is ignored).
        /// </summary>
        public static int FingerprintClickableRegions(IReadOnlyList<ClickableElement>? elements)
        {
            if (elements == null || elements.Count == 0)
                return 0;

            unchecked
            {
                int hash = elements.Count * 397;
                for (int i = 0; i < elements.Count; i++)
                {
                    var e = elements[i];
                    if (e == null)
                    {
                        hash = (hash * 31) + 1;
                        continue;
                    }

                    hash = (hash * 31) + e.X;
                    hash = (hash * 31) + e.Y;
                    hash = (hash * 31) + e.Width;
                    hash = (hash * 31) + e.Height;
                }

                return hash;
            }
        }

        /// <summary>
        /// True when any glyph of the text run overlaps an interactive cell.
        /// </summary>
        public static bool TextOverlapsInteractive(int gridX, int gridY, int length, HashSet<long>? cells)
        {
            if (cells == null || cells.Count == 0 || length <= 0)
                return false;

            for (int i = 0; i < length; i++)
            {
                if (cells.Contains(PackCell(gridX + i, gridY)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Lerps <paramref name="color"/> toward cool-white by <paramref name="influence"/> × <paramref name="brightenAmount"/>.
        /// </summary>
        public static Color ApplyBrighten(Color color, double influence, double brightenAmount)
        {
            double t = Math.Clamp(influence, 0.0, 1.0) * Math.Clamp(brightenAmount, 0.0, 1.0);
            if (t <= 1e-6)
                return color;

            // Cool white keeps readable contrast on warm and cool base colors.
            const byte targetR = 235;
            const byte targetG = 250;
            const byte targetB = 255;
            byte r = (byte)Math.Clamp(color.R + (targetR - color.R) * t, 0, 255);
            byte g = (byte)Math.Clamp(color.G + (targetG - color.G) * t, 0, 255);
            byte b = (byte)Math.Clamp(color.B + (targetB - color.B) * t, 0, 255);
            return Color.FromArgb(color.A, r, g, b);
        }

        /// <summary>
        /// Soft cyan glow color scaled by influence (for the existing text-glow path).
        /// </summary>
        public static Color GlowColorForInfluence(double influence)
        {
            double a = Math.Clamp(influence, 0.0, 1.0);
            byte alpha = (byte)Math.Clamp((int)(180 * a), 0, 255);
            return Color.FromArgb(alpha, 120, 230, 255);
        }

        /// <summary>
        /// Cyan proximity glow is only safe on near-neutral colors. Saturated hues (primary STR red,
        /// gold section headers) shift toward muddy orange/yellow when a cyan glow is layered on top.
        /// </summary>
        public static bool ShouldApplyProximityGlow(Color color)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            if (max <= 0)
                return false;

            int min = Math.Min(color.R, Math.Min(color.G, color.B));
            double saturation = (max - min) / (double)max;
            return saturation < 0.35;
        }
    }
}
