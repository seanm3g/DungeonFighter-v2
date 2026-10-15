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
    /// the plate, size or filter knobs change; blend mode, base color and opacity apply per frame.
    /// </summary>
    internal sealed class RoomBackdropDrawOperation : ICustomDrawOperation
    {
        private const int MaxCacheSide = 4096;

        private static readonly object CacheGate = new();
        private static SKImage? cachedImage;
        private static (SKImage Source, int W, int H, double Brightness, double Contrast, double Saturation, double Blur) cachedKey;

        private readonly SKImage source;
        private readonly RoomBackdropConfig config;

        public RoomBackdropDrawOperation(Rect bounds, SKImage source, RoomBackdropConfig config)
        {
            Bounds = bounds;
            this.source = source;
            this.config = config;
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

            lock (CacheGate)
            {
                var plate = GetProcessed(pixelW, pixelH, scale);
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
                canvas.DrawImage(plate, dest, paint);
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
