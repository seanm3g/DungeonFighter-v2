using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using RPGGame.UI.Avalonia.Effects;
using SkiaSharp;

namespace RPGGame.UI.Avalonia.Canvas
{
    /// <summary>
    /// Draws an item icon in the room plate's Ascii style (<see cref="RoomBackdropConfig.IsAscii"/>): the icon is laid
    /// out in its cell rectangle as usual, then redrawn as glyphs on that rectangle's text-grid cells with the room
    /// plate's ramp and the item knobs (<see cref="RoomBackdropConfig.ItemAsciiDensity"/> etc.). Results are cached per icon, size and knobs.
    /// </summary>
    internal sealed class ItemArtAsciiDrawOperation : ICustomDrawOperation
    {
        private const int MaxCacheSide = 4096;
        private const int MaxCacheEntries = 160;

        private static readonly object CacheGate = new();
        private static readonly Dictionary<(Bitmap Source, Rect SourceRect, Rect Dest, int W, int H, int Cols, int Rows, double FontSize, string Family, string Ramp, double Background, double Gain, double Cutoff), SKImage?> Cache = new();

        private readonly Bitmap source;
        private readonly Rect sourceRect;
        private readonly Rect dest;
        private readonly RoomBackdropConfig config;
        private readonly double cellWidth;
        private readonly double cellHeight;
        private readonly double fontSize;

        /// <param name="grid">The icon's whole cell rectangle; glyphs sit on its cells.</param>
        /// <param name="dest">Where the icon itself sits inside <paramref name="grid"/> (padding and square fit applied).</param>
        public ItemArtAsciiDrawOperation(Rect grid, Rect dest, Bitmap source, Rect sourceRect, RoomBackdropConfig config,
            double cellWidth, double cellHeight, double fontSize)
        {
            Bounds = grid;
            this.dest = dest;
            this.source = source;
            this.sourceRect = sourceRect;
            this.config = config;
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
            if (leaseFeature == null || cellWidth <= 0 || cellHeight <= 0)
                return;
            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;

            var target = new SKRect((float)Bounds.Left, (float)Bounds.Top, (float)Bounds.Right, (float)Bounds.Bottom);
            var matrix = canvas.TotalMatrix;
            double scale = Math.Max(0.01, Math.Max(Math.Abs(matrix.ScaleX), Math.Abs(matrix.ScaleY)));
            int pixelW = Math.Clamp((int)Math.Ceiling(target.Width * scale), 1, MaxCacheSide);
            int pixelH = Math.Clamp((int)Math.Ceiling(target.Height * scale), 1, MaxCacheSide);

            // Density splits each text cell into density×density glyph cells (same aspect, font scaled down to match).
            int density = config.ResolveItemAsciiDensity();
            int cols = Math.Max(1, (int)Math.Round(Bounds.Width / cellWidth) * density);
            int rows = Math.Max(1, (int)Math.Round(Bounds.Height / cellHeight) * density);
            string family = GameFonts.ActiveFamilyName;
            string ramp = config.ResolveAsciiRamp();
            double background = Math.Clamp(config.ItemAsciiBackground, 0, 1);
            double gain = Math.Clamp(config.ItemAsciiGlyphGain, 0, RoomBackdropConfig.MaxAsciiGlyphGain);
            double cutoff = Math.Clamp(config.ItemAsciiCutoff, 0, RoomBackdropConfig.MaxItemAsciiCutoff);
            var relative = new Rect(dest.X - Bounds.X, dest.Y - Bounds.Y, dest.Width, dest.Height);
            var key = (source, sourceRect, relative, pixelW, pixelH, cols, rows, fontSize * scale, family, ramp, background, gain, cutoff);

            lock (CacheGate)
            {
                if (!Cache.TryGetValue(key, out var ascii))
                {
                    if (Cache.Count >= MaxCacheEntries)
                    {
                        foreach (var image in Cache.Values)
                            image?.Dispose();
                        Cache.Clear();
                    }
                    ascii = Build(pixelW, pixelH, scale, relative, cols, rows, density, family, ramp, background, gain, cutoff);
                    Cache[key] = ascii;
                }
                if (ascii == null)
                    return;

                using var paint = new SKPaint { FilterQuality = SKFilterQuality.Medium };
                canvas.DrawImage(ascii, target, paint);
            }
        }

        private SKImage? Build(int w, int h, double scale, Rect relative, int cols, int rows, int density,
            string family, string ramp, double background, double gain, double cutoff)
        {
            using var icon = CopyIcon();
            if (icon == null)
                return null;

            // The icon placed in its cell rectangle as the plain image path would draw it; the rest stays transparent.
            using var surface = SKSurface.Create(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul));
            if (surface == null)
                return null;
            surface.Canvas.Clear(SKColors.Transparent);
            var place = new SKRect(
                (float)(relative.Left * scale), (float)(relative.Top * scale),
                (float)(relative.Right * scale), (float)(relative.Bottom * scale));
            using (var paint = new SKPaint { FilterQuality = SKFilterQuality.High })
                surface.Canvas.DrawImage(icon, place, paint);
            using var plate = surface.Snapshot();

            return AsciiGlyphRaster.Render(plate, w, h, cols, rows,
                (float)(cellWidth / density * scale), (float)(cellHeight / density * scale), (float)(fontSize / density * scale),
                family, ramp, background, gain, SKColors.Transparent, cutoff);
        }

        /// <summary>The <see cref="sourceRect"/> pixels of <see cref="source"/> as a Skia image; null if they cannot be read.</summary>
        private SKImage? CopyIcon()
        {
            var rect = new PixelRect((int)sourceRect.X, (int)sourceRect.Y, (int)sourceRect.Width, (int)sourceRect.Height)
                .Intersect(new PixelRect(source.PixelSize));
            if (rect.Width <= 0 || rect.Height <= 0)
                return null;

            var colorType = source.Format == PixelFormat.Rgba8888 ? SKColorType.Rgba8888 : SKColorType.Bgra8888;
            var alphaType = source.AlphaFormat == AlphaFormat.Unpremul ? SKAlphaType.Unpremul
                : source.AlphaFormat == AlphaFormat.Opaque ? SKAlphaType.Opaque
                : SKAlphaType.Premul;
            using var bitmap = new SKBitmap(new SKImageInfo(rect.Width, rect.Height, colorType, alphaType));
            try
            {
                source.CopyPixels(rect, bitmap.GetPixels(), bitmap.ByteCount, bitmap.RowBytes);
            }
            catch (Exception ex)
            {
                DebugLogger.Log("ItemArtAsciiDrawOperation", $"Could not read icon pixels: {ex.Message}");
                return null;
            }
            return SKImage.FromBitmap(bitmap);
        }
    }
}
