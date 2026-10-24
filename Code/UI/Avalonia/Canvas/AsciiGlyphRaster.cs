using System;
using System.Collections.Generic;
using Avalonia.Platform;
using SkiaSharp;

namespace RPGGame.UI.Avalonia.Canvas
{
    /// <summary>
    /// The Ascii style shared by room plates and item art: a picture averaged per glyph cell and redrawn as glyphs in
    /// the game font, picked from a ramp by the cell's auto-levelled brightness and colored by its average color.
    /// </summary>
    internal static class AsciiGlyphRaster
    {
        private static readonly object TypefaceGate = new();
        private static readonly Dictionary<string, SKTypeface> Typefaces = new(StringComparer.Ordinal);

        /// <summary>
        /// Redraws <paramref name="plate"/> (stretched over the whole <paramref name="w"/>×<paramref name="h"/> surface) as
        /// <paramref name="cols"/>×<paramref name="rows"/> glyphs of <paramref name="cellW"/>×<paramref name="cellH"/> device
        /// pixels on a <paramref name="clear"/> surface. Fully transparent cells stay empty and are left out of the auto-level.
        /// Cells below <paramref name="cutoff"/> of the levelled range stay blank; the rest spread over the ramp's visible glyphs.
        /// </summary>
        public static SKImage? Render(SKImage plate, int w, int h, int cols, int rows, float cellW, float cellH, float fontSize,
            string family, string ramp, double background, double gain, SKColor clear, double cutoff = 0)
        {
            // Average each cell: draw the plate down to one pixel per cell with mipmapped sampling.
            using var cellBitmap = new SKBitmap(new SKImageInfo(cols, rows, SKColorType.Rgba8888, SKAlphaType.Unpremul));
            using (var cellCanvas = new SKCanvas(cellBitmap))
            using (var sample = new SKPaint { FilterQuality = SKFilterQuality.High })
            {
                cellCanvas.Clear(SKColors.Transparent);
                cellCanvas.DrawImage(plate, new SKRect(0, 0, cols, rows), sample);
            }

            // Auto-level: spread the 2nd–98th percentile of cell brightness over the ramp so dark plates still use all of it.
            var colors = cellBitmap.Pixels;
            var lumas = new float[colors.Length];
            var opaque = new List<float>(colors.Length);
            for (int i = 0; i < colors.Length; i++)
            {
                lumas[i] = Luma(colors[i]);
                if (colors[i].Alpha > 0)
                    opaque.Add(lumas[i]);
            }
            if (opaque.Count == 0)
                return null;
            opaque.Sort();
            float low = opaque[(int)(0.02 * (opaque.Count - 1))];
            float high = opaque[(int)(0.98 * (opaque.Count - 1))];
            float range = Math.Max(1, high - low);

            using var surface = SKSurface.Create(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul));
            if (surface == null)
                return null;
            var canvas = surface.Canvas;
            canvas.Clear(clear);

            using var font = new SKFont(ResolveTypeface(family), fontSize) { Edging = SKFontEdging.Antialias };
            float baselineOffset = cellH / 2 - (font.Metrics.Ascent + font.Metrics.Descent) / 2;
            using var fill = new SKPaint { IsAntialias = false };
            using var glyph = new SKPaint { IsAntialias = true, Typeface = font.Typeface, TextSize = font.Size };
            var glyphs = new string[ramp.Length];
            var advances = new float[ramp.Length];
            for (int i = 0; i < ramp.Length; i++)
            {
                glyphs[i] = ramp[i].ToString();
                advances[i] = glyph.MeasureText(glyphs[i]);
            }

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    var c = colors[row * cols + col];
                    if (c.Alpha == 0)
                        continue;
                    float level = (lumas[row * cols + col] - low) / range;
                    if (cutoff > 0 && level < cutoff)
                        continue;
                    float x = col * cellW, y = row * cellH;
                    if (background > 0)
                    {
                        fill.Color = Scale(c, background);
                        canvas.DrawRect(x, y, cellW, cellH, fill);
                    }
                    int index = cutoff > 0
                        ? Math.Clamp(1 + (int)Math.Round((level - cutoff) / (1 - cutoff) * (ramp.Length - 2)), 1, ramp.Length - 1)
                        : Math.Clamp((int)Math.Round(level * (ramp.Length - 1)), 0, ramp.Length - 1);
                    if (char.IsWhiteSpace(ramp[index]))
                        continue;
                    glyph.Color = Scale(c, gain);
                    canvas.DrawText(glyphs[index], x + (cellW - advances[index]) / 2, y + baselineOffset, font, glyph);
                }
            }

            return surface.Snapshot();
        }

        private static float Luma(SKColor c) => 0.2126f * c.Red + 0.7152f * c.Green + 0.0722f * c.Blue;

        private static SKColor Scale(SKColor c, double factor) => new(
            (byte)Math.Clamp(c.Red * factor, 0, 255),
            (byte)Math.Clamp(c.Green * factor, 0, 255),
            (byte)Math.Clamp(c.Blue * factor, 0, 255),
            c.Alpha);

        /// <summary>The game font as a Skia typeface: embedded avares fonts load from the assembly, others by family name.</summary>
        private static SKTypeface ResolveTypeface(string family)
        {
            lock (TypefaceGate)
            {
                if (Typefaces.TryGetValue(family, out var cached))
                    return cached;
                SKTypeface? typeface = null;
                try
                {
                    if (family.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
                    {
                        using var stream = AssetLoader.Open(new Uri(family.Split('#')[0]));
                        typeface = SKTypeface.FromStream(stream);
                    }
                    else
                    {
                        typeface = SKTypeface.FromFamilyName(family.Split(',')[0].Trim());
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.Log("AsciiGlyphRaster", $"Could not load font {family}: {ex.Message}");
                }
                typeface ??= SKTypeface.FromFamilyName("Courier New") ?? SKTypeface.Default;
                Typefaces[family] = typeface;
                return typeface;
            }
        }
    }
}
