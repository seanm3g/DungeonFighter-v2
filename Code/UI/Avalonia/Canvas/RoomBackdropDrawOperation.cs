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
    /// Each cache keeps two entries so a crossfade between two rooms' plates does not rebuild them every frame.
    /// </summary>
    internal sealed class RoomBackdropDrawOperation : ICustomDrawOperation
    {
        private const int MaxCacheSide = 4096;

        private static readonly object CacheGate = new();
        private static readonly SKImage?[] cachedImages = new SKImage?[2];
        private static readonly (SKImage Source, int W, int H, double Brightness, double Contrast, double Saturation, double Blur)[] cachedKeys = new (SKImage, int, int, double, double, double, double)[2];
        private static int lastImageSlot;

        private static readonly SKImage?[] cachedAsciis = new SKImage?[2];
        private static readonly (SKImage Plate, int Cols, int Rows, double CellW, double CellH, double FontSize, string Family, string Ramp, double Background, double Gain, double Underlay, double Variety)[] cachedAsciiKeys = new (SKImage, int, int, double, double, double, string, string, double, double, double, double)[2];
        private static int lastAsciiSlot;

        private readonly SKImage source;
        private readonly RoomBackdropConfig config;
        private readonly Rect grid;
        private readonly double cellWidth;
        private readonly double cellHeight;
        private readonly double fontSize;
        private readonly double opacity;
        private readonly (double Start, double End)? fade;
        private readonly bool fadeHorizontal;

        /// <param name="grid">The whole cell rectangle the plate belongs to (<paramref name="bounds"/> is it minus the border inset); Ascii glyphs sit on its cells.</param>
        /// <param name="opacity">Whole-result opacity (base fill and plate together), for the location crossfade.</param>
        /// <param name="fade">Fade-in (x or y in the same space as <paramref name="bounds"/>): transparent before Start, full after End.</param>
        /// <param name="fadeHorizontal">True: <paramref name="fade"/> runs left to right; false: top to bottom.</param>
        public RoomBackdropDrawOperation(Rect bounds, SKImage source, RoomBackdropConfig config, Rect grid, double cellWidth, double cellHeight, double fontSize, double opacity = 1, (double Start, double End)? fade = null, bool fadeHorizontal = false)
        {
            this.fade = fade;
            this.fadeHorizontal = fadeHorizontal;
            Bounds = bounds;
            this.source = source;
            this.config = config;
            this.grid = grid;
            this.cellWidth = cellWidth;
            this.cellHeight = cellHeight;
            this.fontSize = fontSize;
            this.opacity = Math.Clamp(opacity, 0, 1);
        }

        public Rect Bounds { get; }

        public bool HitTest(Point p) => false;

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Dispose() { }

        public void Render(ImmediateDrawingContext context)
        {
            if (opacity <= 0)
                return;
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

                int saveCount = canvas.Save();
                canvas.ClipRect(dest);
                if (opacity < 1 || fade != null)
                {
                    // Base fill and blended plate composite together, then fade as one over what is beneath.
                    using var layerPaint = new SKPaint { Color = SKColors.White.WithAlpha((byte)Math.Round(opacity * 255)) };
                    canvas.SaveLayer(dest, layerPaint);
                }
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
                if (ascii && config.AnimatesLightRays())
                    DrawLightRays(canvas, plate, target, Math.Clamp(config.Opacity, 0, 1));
                if (fade is { } f)
                {
                    // Mask the layer: clear before the fade, eased ramp (smoothstep-ish stops) to full.
                    float a = (float)f.Start, b = (float)f.End;
                    using var shader = SKShader.CreateLinearGradient(
                        fadeHorizontal ? new SKPoint(a, 0) : new SKPoint(0, a),
                        fadeHorizontal ? new SKPoint(b, 0) : new SKPoint(0, b),
                        new[] { SKColors.Transparent, SKColors.Black.WithAlpha(40), SKColors.Black.WithAlpha(150), SKColors.Black },
                        new[] { 0f, 0.3f, 0.65f, 1f },
                        SKShaderTileMode.Clamp);
                    using var maskPaint = new SKPaint { Shader = shader, BlendMode = SKBlendMode.DstIn };
                    canvas.DrawRect(dest, maskPaint);
                }
                canvas.RestoreToCount(saveCount);
            }
        }

        /// <summary>Wall clock for the light rays, shared by every backdrop so a crossfade keeps one ray position.</summary>
        private static readonly System.Diagnostics.Stopwatch RayClock = System.Diagnostics.Stopwatch.StartNew();

        /// <summary>
        /// Light over the plate, two parts that add together into one brightness mask:
        /// a fan of god rays from a fixed vanishing point above the room (a repeating pattern that rotates steadily one way,
        /// so it always fills the screen) and a soft cloudiness (two Perlin fields drifting past each other, so the bright patches
        /// form, merge and dissolve). The mask is drawn at 1/<see cref="LightMaskDownscale"/> size and scaled up, which blurs
        /// it. Where the mask is bright the plate's own colors are added again (Plus), so the picture itself brightens.
        /// </summary>
        private void DrawLightRays(SKCanvas canvas, SKImage plate, SKRect target, double plateOpacity)
        {
            double rays = Math.Clamp(config.LightRayStrength, 0, RoomBackdropConfig.MaxLightRayStrength);
            double clouds = Math.Clamp(config.LightCloudStrength, 0, RoomBackdropConfig.MaxLightCloudStrength);
            double t = RayClock.Elapsed.TotalSeconds * Math.Clamp(config.LightRaySpeed, 0, RoomBackdropConfig.MaxLightRaySpeed);
            if ((rays <= 0 && clouds <= 0) || plateOpacity <= 0 || target.Width <= 0 || target.Height <= 0)
                return;

            using var mask = BuildLightMask(target.Width, target.Height, t, rays, clouds);
            if (mask == null)
                return;

            // The added light is the plate's colors times LightGain, so a full-strength ray brightens past the plate itself.
            using var gain = SKColorFilter.CreateColorMatrix(ScaleOffsetMatrix(LightGain, 0));
            using var addPaint = new SKPaint { BlendMode = SKBlendMode.Plus, ColorFilter = gain };
            canvas.SaveLayer(target, addPaint);
            using (var platePaint = new SKPaint { FilterQuality = SKFilterQuality.Medium })
                canvas.DrawImage(plate, target, platePaint);
            // Upscaling the small mask with high-quality filtering is the blur.
            using (var maskPaint = new SKPaint { BlendMode = SKBlendMode.DstIn, FilterQuality = SKFilterQuality.High })
                canvas.DrawImage(mask, target, maskPaint);
            canvas.Restore();
        }

        private const float LightMaskDownscale = 8;
        private const float LightGain = 2.2f;
        private const int RayCount = 40;
        /// <summary>Fan rotation at speed 1: one ray spacing (9°) every ~6 s.</summary>
        private const double RayDegreesPerSecond = 1.5;

        private static SKImage? BuildLightMask(float width, float height, double t, double rayStrength, double cloudStrength)
        {
            const double Tau = Math.PI * 2;
            int w = Math.Max(16, (int)Math.Ceiling(width / LightMaskDownscale));
            int h = Math.Max(16, (int)Math.Ceiling(height / LightMaskDownscale));
            using var surface = SKSurface.Create(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul));
            if (surface == null)
                return null;
            var c = surface.Canvas;
            c.Clear(SKColors.Transparent);

            if (cloudStrength > 0)
            {
                // Whole-room swell on top of the drifting patches.
                double swell = 0.8 + 0.2 * Math.Sin(t * Tau / 13);
                float cycles = 2.2f;
                DrawCloudField(c, w, h, cycles / w, seed: 7, dx: t * w * 0.011, dy: t * h * 0.004, cloudStrength * swell);
                DrawCloudField(c, w, h, cycles * 1.6f / w, seed: 23, dx: -t * w * 0.008, dy: t * h * 0.007, cloudStrength * swell * 0.7);
            }

            if (rayStrength > 0)
            {
                // Fixed vanishing point above the top centre. The fan is one repeating pattern of RayCount rays around the
                // whole circle, rotating at a constant rate, so rays always cover the screen and never reverse.
                var vp = new SKPoint(w * 0.5f, -h * 0.25f);
                double rotation = t * RayDegreesPerSecond;

                // Sample the fan's brightness around the circle (sweep angle 0 = +x, 90° = straight down).
                const int Samples = 360;
                const double Spacing = 360.0 / RayCount;
                var colors = new SKColor[Samples + 1];
                var stops = new float[Samples + 1];
                for (int i = 0; i <= Samples; i++)
                {
                    double deg = 360.0 * i / Samples;
                    double sum = 0;
                    for (int k = 0; k < RayCount; k++)
                    {
                        // Per-ray offset, width and brightness are fixed (golden-ratio hashes), so the pattern is uneven but
                        // rigid: it only turns.
                        double jitter = Spacing * 0.35 * Math.Sin(k * 2.399);
                        double center = k * Spacing + jitter + rotation;
                        double spread = 1.8 + 2.4 * (0.5 + 0.5 * Math.Sin(k * 1.618 + 0.4));
                        double amp = 0.45 + 0.55 * (0.5 + 0.5 * Math.Sin(k * 3.883 + 1.2));
                        double d = deg - center;
                        d -= 360 * Math.Round(d / 360); // wrap to [-180, 180] so the pattern is seamless
                        d /= spread;
                        sum += amp * Math.Exp(-d * d);
                    }
                    colors[i] = SKColors.Black.WithAlpha((byte)Math.Round(Math.Clamp(sum * rayStrength, 0, 1) * 255));
                    stops[i] = (float)i / Samples;
                }
                colors[Samples] = colors[0];

                // Rays in their own layer so the distance falloff does not dim the clouds; light falloff only, so the rays
                // still reach the bottom of the screen.
                using var plus = new SKPaint { BlendMode = SKBlendMode.Plus };
                c.SaveLayer(plus);
                using (var sweep = SKShader.CreateSweepGradient(vp, colors, stops))
                using (var sweepPaint = new SKPaint { Shader = sweep })
                    c.DrawRect(0, 0, w, h, sweepPaint);
                float reach = (float)Math.Sqrt(Math.Pow(Math.Max(vp.X, w - vp.X), 2) + Math.Pow(h - vp.Y, 2));
                using (var falloff = SKShader.CreateRadialGradient(vp, reach,
                    new[] { SKColors.Black, SKColors.Black, SKColors.Black.WithAlpha(150) },
                    new[] { 0f, Math.Clamp(-vp.Y / reach, 0f, 0.9f), 1f },
                    SKShaderTileMode.Clamp))
                using (var falloffPaint = new SKPaint { Shader = falloff, BlendMode = SKBlendMode.DstIn })
                    c.DrawRect(0, 0, w, h, falloffPaint);
                c.Restore();
            }

            return surface.Snapshot();
        }

        /// <summary>Adds one drifting Perlin field to the mask: noise red channel → alpha, contrast-stretched so only the bright puffs show.</summary>
        private static void DrawCloudField(SKCanvas c, int w, int h, float frequency, int seed, double dx, double dy, double strength)
        {
            using var noise = SKShader.CreatePerlinNoiseFractalNoise(frequency, frequency, 2, seed);
            using var moved = noise.WithLocalMatrix(SKMatrix.CreateTranslation((float)dx, (float)dy));
            float gain = (float)(2.6 * strength);
            float offset = (float)(-1.0 * strength);
            using var toAlpha = SKColorFilter.CreateColorMatrix(new[]
            {
                0f, 0, 0, 0, 0,
                0, 0, 0, 0, 0,
                0, 0, 0, 0, 0,
                gain, 0, 0, 0, offset
            });
            using var paint = new SKPaint { Shader = moved, ColorFilter = toAlpha, BlendMode = SKBlendMode.Plus };
            c.DrawRect(0, 0, w, h, paint);
        }

        /// <summary>The cover-cropped, color-filtered and blurred plate at device size. Caller holds <see cref="CacheGate"/>.</summary>
        private SKImage? GetProcessed(int w, int h, double scale)
        {
            double brightness = Math.Clamp(config.Brightness, 0, RoomBackdropConfig.MaxBrightness);
            double contrast = Math.Clamp(config.Contrast, 0, RoomBackdropConfig.MaxContrast);
            double saturation = Math.Clamp(config.Saturation, 0, RoomBackdropConfig.MaxSaturation);
            double blur = Math.Clamp(config.Blur, 0, RoomBackdropConfig.MaxBlur);
            var key = (source, w, h, brightness, contrast, saturation, blur);
            for (int i = 0; i < cachedImages.Length; i++)
            {
                if (cachedImages[i] != null && cachedKeys[i].Equals(key))
                {
                    lastImageSlot = i;
                    return cachedImages[i];
                }
            }

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

            int slot = 1 - lastImageSlot;
            cachedImages[slot]?.Dispose();
            cachedImages[slot] = surface.Snapshot();
            cachedKeys[slot] = key;
            lastImageSlot = slot;
            return cachedImages[slot];
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
            double underlay = Math.Clamp(config.AsciiImageUnderlay, 0, 1);
            double variety = Math.Clamp(config.AsciiVariety, 0, 1);
            var key = (plate, cols, rows, glyphCellWidth * scale, glyphCellHeight * scale, glyphFontSize * scale, family, ramp, background, gain, underlay, variety);
            for (int i = 0; i < cachedAsciis.Length; i++)
            {
                if (cachedAsciis[i] != null && cachedAsciiKeys[i].Equals(key))
                {
                    lastAsciiSlot = i;
                    return cachedAsciis[i];
                }
            }

            var ascii = AsciiGlyphRaster.Render(plate, w, h, cols, rows, (float)(glyphCellWidth * scale), (float)(glyphCellHeight * scale),
                (float)(glyphFontSize * scale), family, ramp, background, gain, underlay > 0 ? SKColors.Transparent : SKColors.Black, variety: variety);
            if (ascii == null)
                return null;

            if (underlay > 0)
            {
                // Faded picture under the glyphs fills the gaps between them; composited here so blend/opacity apply as before.
                using var surface = SKSurface.Create(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul));
                if (surface != null)
                {
                    surface.Canvas.Clear(SKColors.Black);
                    using (var underlayPaint = new SKPaint { Color = SKColors.White.WithAlpha((byte)Math.Round(underlay * 255)) })
                        surface.Canvas.DrawImage(plate, new SKRect(0, 0, w, h), underlayPaint);
                    surface.Canvas.DrawImage(ascii, 0, 0);
                    ascii.Dispose();
                    ascii = surface.Snapshot();
                }
            }

            int slot = 1 - lastAsciiSlot;
            cachedAsciis[slot]?.Dispose();
            cachedAsciis[slot] = ascii;
            cachedAsciiKeys[slot] = key;
            lastAsciiSlot = slot;
            return ascii;
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
