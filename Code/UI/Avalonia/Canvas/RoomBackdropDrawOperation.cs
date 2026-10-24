using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using RPGGame.UI.Avalonia.Effects;
using SkiaSharp;

namespace RPGGame.UI.Avalonia.Canvas
{
    /// <summary>
    /// Draws a <see cref="CanvasBackdrop"/> through Skia so it gets real blend modes (Avalonia's bitmap blending is
    /// Porter-Duff only) and color filters. The filtered + blurred plate is cached at device size and rebuilt only when
    /// the plate, size or filter knobs change; blend mode, base color and opacity apply per frame. In Ascii style the
    /// filtered plate is averaged per text-grid cell and redrawn as glyphs in the game font (cached the same way).
    /// </summary>
    internal sealed class RoomBackdropDrawOperation : ICustomDrawOperation
    {
        private const int MaxCacheSide = 4096;

        private static readonly object CacheGate = new();
        private static SKImage? cachedImage;
        private static (SKImage Source, int W, int H, double Brightness, double Contrast, double Saturation, double Blur) cachedKey;

        private static SKImage? cachedAscii;
        private static (SKImage Plate, int Cols, int Rows, double CellW, double CellH, double FontSize, string Family, string Ramp, double Background, double Gain) cachedAsciiKey;

        private readonly SKImage source;
        private readonly RoomBackdropConfig config;
        private readonly Rect grid;
        private readonly double cellWidth;
        private readonly double cellHeight;
        private readonly double fontSize;

        /// <param name="grid">The whole cell rectangle the plate belongs to (<paramref name="bounds"/> is it minus the border inset); Ascii glyphs sit on its cells.</param>
        public RoomBackdropDrawOperation(Rect bounds, SKImage source, RoomBackdropConfig config, Rect grid, double cellWidth, double cellHeight, double fontSize)
        {
            Bounds = bounds;
            this.source = source;
            this.config = config;
            this.grid = grid;
            this.cellWidth = cellWidth;
            this.cellHeight = cellHeight;
            this.fontSize = fontSize;
        }

        public Rect Bounds { get; }

        public bool HitTest(Point p) => false;

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Dispose() { }

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null)
                return;
            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;

            var dest = new SKRect((float)Bounds.Left, (float)Bounds.Top, (float)Bounds.Right, (float)Bounds.Bottom);
            var matrix = canvas.TotalMatrix;
            double scale = Math.Max(0.01, Math.Max(Math.Abs(matrix.ScaleX), Math.Abs(matrix.ScaleY)));
            int pixelW = Math.Clamp((int)Math.Ceiling(dest.Width * scale), 1, MaxCacheSide);
            int pixelH = Math.Clamp((int)Math.Ceiling(dest.Height * scale), 1, MaxCacheSide);

            bool ascii = config.IsAscii && cellWidth > 0 && cellHeight > 0;
            var target = dest;
            if (ascii)
            {
                // Glyphs line up with the text grid, so the plate covers the whole cell rectangle; the inset is clipped off.
                target = new SKRect((float)grid.Left, (float)grid.Top, (float)grid.Right, (float)grid.Bottom);
                pixelW = Math.Clamp((int)Math.Ceiling(target.Width * scale), 1, MaxCacheSide);
                pixelH = Math.Clamp((int)Math.Ceiling(target.Height * scale), 1, MaxCacheSide);
            }

            lock (CacheGate)
            {
                var plate = GetProcessed(pixelW, pixelH, scale);
                if (plate != null && ascii)
                    plate = GetAscii(plate, pixelW, pixelH, scale);
                if (plate == null)
                    return;

                canvas.Save();
                canvas.ClipRect(dest);
                using (var basePaint = new SKPaint { Color = config.ResolveBaseColor(), IsAntialias = false })
                    canvas.DrawRect(dest, basePaint);

                byte alpha = (byte)Math.Round(Math.Clamp(config.Opacity, 0, 1) * 255);
                using var paint = new SKPaint
                {
                    BlendMode = config.ResolveBlendMode(),
                    Color = SKColors.White.WithAlpha(alpha),
                    FilterQuality = SKFilterQuality.Medium
                };
                canvas.DrawImage(plate, target, paint);
                canvas.Restore();
            }
        }

        /// <summary>The cover-cropped, color-filtered and blurred plate at device size. Caller holds <see cref="CacheGate"/>.</summary>
        private SKImage? GetProcessed(int w, int h, double scale)
        {
            double brightness = Math.Clamp(config.Brightness, 0, RoomBackdropConfig.MaxBrightness);
            double contrast = Math.Clamp(config.Contrast, 0, RoomBackdropConfig.MaxContrast);
            double saturation = Math.Clamp(config.Saturation, 0, RoomBackdropConfig.MaxSaturation);
            double blur = Math.Clamp(config.Blur, 0, RoomBackdropConfig.MaxBlur);
            var key = (source, w, h, brightness, contrast, saturation, blur);
            if (cachedImage != null && cachedKey.Equals(key))
                return cachedImage;

            using var surface = SKSurface.Create(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul));
            if (surface == null)
                return null;

            // Cover: scale the plate to fill w×h, centred; Clamp so blur samples past the edges stay opaque.
            float coverScale = Math.Max((float)w / source.Width, (float)h / source.Height);
            var local = SKMatrix.CreateScaleTranslation(
                coverScale, coverScale,
                (w - source.Width * coverScale) / 2f, (h - source.Height * coverScale) / 2f);

            using var shader = source.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, local);
            using var saturationFilter = SKColorFilter.CreateColorMatrix(SaturationMatrix((float)saturation));
            using var contrastFilter = SKColorFilter.CreateColorMatrix(ScaleOffsetMatrix((float)contrast, 0.5f * (1 - (float)contrast)));
            using var brightnessFilter = SKColorFilter.CreateColorMatrix(ScaleOffsetMatrix((float)brightness, 0));
            using var innerFilter = SKColorFilter.CreateCompose(contrastFilter, saturationFilter);
            using var colorFilter = SKColorFilter.CreateCompose(brightnessFilter, innerFilter);

            float sigma = (float)(blur * scale / 2);
            using var blurFilter = sigma > 0 ? SKImageFilter.CreateBlur(sigma, sigma, SKShaderTileMode.Clamp) : null;
            using var paint = new SKPaint
            {
                Shader = shader,
                ColorFilter = colorFilter,
                ImageFilter = blurFilter,
                FilterQuality = SKFilterQuality.Medium
            };

            // Draw past the surface edges by the blur's reach so its faded rim falls outside the plate.
            float pad = sigma * 3;
            surface.Canvas.Clear(SKColors.Black);
            surface.Canvas.DrawRect(new SKRect(-pad, -pad, w + pad, h + pad), paint);

            cachedImage?.Dispose();
            cachedImage = surface.Snapshot();
            cachedKey = key;
            return cachedImage;
        }

        /// <summary>The processed plate redrawn as glyphs on the grid cells (<see cref="AsciiGlyphRaster"/>). Caller holds <see cref="CacheGate"/>.</summary>
        private SKImage? GetAscii(SKImage plate, int w, int h, double scale)
        {
            // Density splits each text cell into density×density glyph cells (same aspect, font scaled down to match).
            int density = config.ResolveAsciiDensity();
            double glyphCellWidth = cellWidth / density;
            double glyphCellHeight = cellHeight / density;
            double glyphFontSize = fontSize / density;
            int cols = Math.Max(1, (int)Math.Round(grid.Width / cellWidth) * density);
            int rows = Math.Max(1, (int)Math.Round(grid.Height / cellHeight) * density);
            string family = GameFonts.ActiveFamilyName;
            string ramp = config.ResolveAsciiRamp();
            double background = Math.Clamp(config.AsciiBackground, 0, 1);
            double gain = Math.Clamp(config.AsciiGlyphGain, 0, RoomBackdropConfig.MaxAsciiGlyphGain);
            var key = (plate, cols, rows, glyphCellWidth * scale, glyphCellHeight * scale, glyphFontSize * scale, family, ramp, background, gain);
            if (cachedAscii != null && cachedAsciiKey.Equals(key))
                return cachedAscii;

            var ascii = AsciiGlyphRaster.Render(plate, w, h, cols, rows, (float)(glyphCellWidth * scale), (float)(glyphCellHeight * scale),
                (float)(glyphFontSize * scale), family, ramp, background, gain, SKColors.Black);
            if (ascii == null)
                return null;

            cachedAscii?.Dispose();
            cachedAscii = ascii;
            cachedAsciiKey = key;
            return cachedAscii;
        }

        private static float[] ScaleOffsetMatrix(float scale, float offset) => new[]
        {
            scale, 0, 0, 0, offset,
            0, scale, 0, 0, offset,
            0, 0, scale, 0, offset,
            0, 0, 0, 1, 0
        };

        private static float[] SaturationMatrix(float s)
        {
            const float lr = 0.2126f, lg = 0.7152f, lb = 0.0722f;
            float inv = 1 - s;
            return new[]
            {
                lr * inv + s, lg * inv, lb * inv, 0, 0,
                lr * inv, lg * inv + s, lb * inv, 0, 0,
                lr * inv, lg * inv, lb * inv + s, 0, 0,
                0, 0, 0, 1, 0
            };
        }
    }
}
