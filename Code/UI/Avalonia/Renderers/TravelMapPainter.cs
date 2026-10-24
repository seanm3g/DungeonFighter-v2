using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>What the frame layer should show on top of the cached terrain.</summary>
    public sealed class TravelMapFrame
    {
        public IReadOnlyList<TravelMapPin> Pins { get; init; } = Array.Empty<TravelMapPin>();
        public string? CurrentRegionId { get; init; }
        public string? HoveredRegionId { get; init; }
        public TravelMapPin? RouteFrom { get; init; }
        public TravelMapPin? RouteTo { get; init; }
        public double WalkProgress { get; init; }
        public bool RouteComplete { get; init; }

        /// <summary>Dungeon markers on a region view, drawn as plain level tiles.</summary>
        public IReadOnlyList<TravelMapSite> Sites { get; init; } = Array.Empty<TravelMapSite>();
        public int HoveredSite { get; init; } = -1;

        /// <summary>Name shown over the hovered site.</summary>
        public string? HoveredSiteLabel { get; init; }
    }

    /// <summary>
    /// Paints the travel map as a dense field of small coloured glyphs on dark ground, one per fine cell,
    /// with a few large landmark glyphs over them. Regions are plain dots; while travelling, an @ moves from
    /// one dot to the other (no route line). Terrain is cached in a bitmap; the dots are drawn live over it
    /// by <see cref="PaintFrame"/>, so a redraw never touches the terrain pixels.
    /// </summary>
    public sealed class TravelMapPainter
    {
        private static readonly Color DotFill = Color.FromRgb(250, 226, 120);
        private static readonly Color DotHere = Color.FromRgb(255, 255, 255);
        private static readonly Color DotHover = Color.FromRgb(255, 72, 56);
        private static readonly Color DotRing = Color.FromRgb(12, 10, 8);
        private static readonly Color Walker = Color.FromRgb(255, 255, 255);
        private static readonly Color SiteFill = Color.FromRgb(214, 52, 40);
        private static readonly Color SiteHover = Color.FromRgb(255, 214, 64);
        private static readonly Color LabelText = Color.FromRgb(255, 236, 170);
        private static readonly Color LabelBack = Color.FromArgb(225, 10, 8, 6);

        private static readonly Dictionary<Color, IBrush> SolidBrushes = new();

        private RenderTargetBitmap? baseBitmap;
        private string baseKey = "";
        private GlyphCache? glyphs;

        /// <summary>
        /// The cached terrain bitmap, rebuilt only when the layout or font changes. Draw the frame's dots over it
        /// with <see cref="PaintFrame"/> (see <see cref="Overlay"/>).
        /// </summary>
        /// <param name="cellWidth">Game grid cell width in DIPs (one cell = two fine map cells).</param>
        /// <param name="hole">Game-cell rectangle (relative to the map) left transparent for the log slip.</param>
        public IImage Paint(
            TravelMapGeographyResult geography,
            string geographyKey,
            int gridWidth,
            int gridHeight,
            double cellWidth,
            double cellHeight,
            double renderScaling,
            (int X, int Y, int W, int H) hole)
        {
            var dipSize = new Size(gridWidth * cellWidth, gridHeight * cellHeight);
            var pixelSize = new PixelSize(
                Math.Max(1, (int)Math.Ceiling(dipSize.Width * renderScaling)),
                Math.Max(1, (int)Math.Ceiling(dipSize.Height * renderScaling)));
            var dpi = new Vector(96 * renderScaling, 96 * renderScaling);
            var holeRect = new Rect(hole.X * cellWidth, hole.Y * cellHeight, hole.W * cellWidth, hole.H * cellHeight);

            string key = $"{geographyKey}|{pixelSize}|{cellWidth:F3}x{cellHeight:F3}|{holeRect}|{GameFonts.ActiveFamilyName}";
            if (baseBitmap == null || key != baseKey)
            {
                glyphs = new GlyphCache(GameFonts.ActiveTypeface, cellWidth / 2, cellHeight / 2);
                baseBitmap?.Dispose();
                baseBitmap = new RenderTargetBitmap(pixelSize, dpi);
                using (var ctx = baseBitmap.CreateDrawingContext())
                    PaintBase(ctx, geography, dipSize, cellWidth / 2, cellHeight / 2, holeRect);
                baseKey = key;
            }
            return baseBitmap;
        }

        /// <summary>A canvas overlay that draws <paramref name="frame"/> over the terrain from the last <see cref="Paint"/>.</summary>
        public Action<DrawingContext> Overlay(TravelMapFrame frame, int gridWidth, int gridHeight, double cellWidth, double cellHeight)
        {
            var size = new Size(gridWidth * cellWidth, gridHeight * cellHeight);
            return ctx => PaintFrame(ctx, frame, cellWidth, cellHeight, size);
        }

        private static IBrush Solid(Color color)
        {
            if (!SolidBrushes.TryGetValue(color, out var brush))
                SolidBrushes[color] = brush = new SolidColorBrush(color);
            return brush;
        }

        /// <summary>DIP center of a pin's dot: the middle of its game-grid cell.</summary>
        public static Point PinCenter(TravelMapPin pin, double cellWidth, double cellHeight) =>
            new((pin.FineX / 2 + 0.5) * cellWidth, (pin.FineY / 2 + 0.5) * cellHeight);

        private void PaintBase(DrawingContext ctx, TravelMapGeographyResult geography, Size size, double fineW, double fineH, Rect hole)
        {
            var full = new Rect(size);
            var map = new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(full), new RectangleGeometry(hole));
            using (ctx.PushGeometryClip(map))
            {
                ctx.FillRectangle(new SolidColorBrush(InkColor(TravelMapInk.SeaGround)), full);
                PaintGround(ctx, geography.Terrain, fineW, fineH);
                PaintGlyphs(ctx, geography.Terrain, fineW, fineH);
                foreach (var big in geography.BigGlyphs)
                {
                    var at = new Point((big.FineX + 0.5) * fineW, (big.FineY + 0.5) * fineH);
                    // A dark offset copy underneath lifts the landmark off the terrain, like a sprite's outline.
                    glyphs!.Draw(ctx, big.Text, DotRing, big.Scale, new Point(at.X + 1.5, at.Y + 1.5));
                    glyphs.Draw(ctx, big.Text, InkColor(big.Ink), big.Scale, at);
                }

                // Soft vignette toward the map border.
                for (int i = 0; i < 10; i++)
                {
                    var pen = new Pen(new SolidColorBrush(Color.FromArgb((byte)Math.Max(0, 70 - i * 7), 0, 0, 0)), 3);
                    ctx.DrawRectangle(null, pen, full.Deflate(1.5 + i * 3));
                }

                // Shadow of the log slip resting on the map.
                for (int i = 1; i <= 4; i++)
                {
                    var pen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(90 - i * 18), 0, 0, 0)), 2);
                    ctx.DrawRectangle(null, pen, hole.Inflate(i * 2 - 1).Translate(new Vector(2, 2)));
                }
            }
        }

        /// <summary>Fills each cell's ground colour, merging horizontal runs so the fill is a few thousand rects.</summary>
        private static void PaintGround(DrawingContext ctx, TravelMapGrid terrain, double fineW, double fineH)
        {
            var brushes = new Dictionary<TravelMapInk, IBrush>();
            for (int y = 0; y < terrain.Height; y++)
            {
                int x = 0;
                while (x < terrain.Width)
                {
                    var ground = terrain[x, y].Ground;
                    int start = x;
                    while (x < terrain.Width && terrain[x, y].Ground == ground)
                        x++;
                    if (ground == TravelMapInk.SeaGround)
                        continue;
                    if (!brushes.TryGetValue(ground, out var brush))
                        brushes[ground] = brush = new SolidColorBrush(InkColor(ground));
                    // A hair of overlap hides seams between rows at fractional DIP sizes.
                    ctx.FillRectangle(brush, new Rect(start * fineW, y * fineH, (x - start) * fineW + 0.5, fineH + 0.5));
                }
            }
        }

        private void PaintGlyphs(DrawingContext ctx, TravelMapGrid terrain, double fineW, double fineH)
        {
            for (int y = 0; y < terrain.Height; y++)
            {
                for (int x = 0; x < terrain.Width; x++)
                {
                    var cell = terrain[x, y];
                    if (cell.Ink == TravelMapInk.Blank)
                        continue;
                    glyphs!.Draw(ctx, cell.Glyph, InkColor(cell.Ink), 1.0, new Point((x + 0.5) * fineW, (y + 0.5) * fineH));
                }
            }
        }

        private void PaintFrame(DrawingContext ctx, TravelMapFrame frame, double cellWidth, double cellHeight, Size size)
        {
            bool walking = frame.RouteFrom != null && frame.RouteTo != null && !frame.RouteComplete;
            double radius = Math.Max(2.5, Math.Min(cellWidth, cellHeight) * 0.38);
            foreach (var pin in frame.Pins)
            {
                var c = PinCenter(pin, cellWidth, cellHeight);
                bool isCurrent = !walking && string.Equals(pin.Region.Id, frame.CurrentRegionId, StringComparison.OrdinalIgnoreCase);
                bool isHovered = string.Equals(pin.Region.Id, frame.HoveredRegionId, StringComparison.OrdinalIgnoreCase);
                bool isTarget = walking && frame.RouteTo is TravelMapPin target && target.Region.Id == pin.Region.Id;

                var fill = isHovered || isTarget ? DotHover : isCurrent ? DotHere : DotFill;
                double r = isHovered || isCurrent || isTarget ? radius * 1.3 : radius;
                ctx.DrawEllipse(Solid(DotRing), null, c, r + 2, r + 2);
                ctx.DrawEllipse(Solid(fill), null, c, r, r);
                if (isCurrent || isHovered)
                    ctx.DrawEllipse(null, new Pen(Solid(fill), 1.2), c, r + 4, r + 4);
            }

            foreach (var site in frame.Sites)
                PaintSite(ctx, site, site.Number == frame.HoveredSite, cellWidth, cellHeight);
            foreach (var site in frame.Sites)
                if (site.Number == frame.HoveredSite && !string.IsNullOrEmpty(frame.HoveredSiteLabel))
                    PaintSiteLabel(ctx, site, frame.HoveredSiteLabel!, cellWidth, cellHeight, size);

            if (walking && frame.RouteFrom is TravelMapPin from && frame.RouteTo is TravelMapPin to)
            {
                var a = PinCenter(from, cellWidth, cellHeight);
                var b = PinCenter(to, cellWidth, cellHeight);
                double t = Math.Clamp(frame.WalkProgress, 0, 1);
                var head = new Point(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t - cellHeight * 0.6);
                glyphs!.Draw(ctx, '@', DotRing, 2.2, new Point(head.X + 1.5, head.Y + 1.5));
                glyphs.Draw(ctx, '@', Walker, 2.2, head);
            }
        }

        /// <summary>DIP center of a dungeon site: the middle of its game-grid cell.</summary>
        public static Point SiteCenter(TravelMapSite site, double cellWidth, double cellHeight) =>
            new((site.FineX / 2 + 0.5) * cellWidth, (site.FineY / 2 + 0.5) * cellHeight);

        /// <summary>A Mario-style level tile: a plain dark-edged rounded square, yellow and ringed when hovered.</summary>
        private static void PaintSite(DrawingContext ctx, TravelMapSite site, bool hovered, double cellWidth, double cellHeight)
        {
            var c = SiteCenter(site, cellWidth, cellHeight);
            double half = Math.Max(3, Math.Min(cellWidth, cellHeight) * (hovered ? 0.8 : 0.65));
            var tile = new Rect(c.X - half, c.Y - half, half * 2, half * 2);
            ctx.DrawRectangle(Solid(DotRing), null, tile.Inflate(2).Translate(new Vector(1.5, 1.5)), 3, 3);
            ctx.DrawRectangle(Solid(hovered ? SiteHover : SiteFill), new Pen(Solid(DotRing), 1.5), tile, 2, 2);
            if (hovered)
                ctx.DrawRectangle(null, new Pen(Solid(SiteHover), 1.2), tile.Inflate(4), 4, 4);
        }

        /// <summary>The hovered dungeon's name on a dark slip just above its tile, kept inside the map.</summary>
        private void PaintSiteLabel(DrawingContext ctx, TravelMapSite site, string label, double cellWidth, double cellHeight, Size size)
        {
            var c = SiteCenter(site, cellWidth, cellHeight);
            var text = glyphs!.Shape(label, LabelText, 2.0);
            double w = text.WidthIncludingTrailingWhitespace + cellWidth;
            double h = text.Height + 4;
            double left = Math.Clamp(c.X - w / 2, 2, Math.Max(2, size.Width - w - 2));
            double top = c.Y - cellHeight - h;
            if (top < 2)
                top = c.Y + cellHeight;
            var slip = new Rect(left, top, w, h);
            ctx.DrawRectangle(Solid(LabelBack), new Pen(Solid(SiteHover), 1), slip, 3, 3);
            ctx.DrawText(text, new Point(slip.X + cellWidth / 2, slip.Y + 2));
        }

        public static Color InkColor(TravelMapInk ink) => ink switch
        {
            TravelMapInk.SeaGround => Color.FromRgb(12, 34, 40),
            TravelMapInk.Wave => Color.FromRgb(52, 98, 102),
            TravelMapInk.WaveBright => Color.FromRgb(150, 200, 200),
            TravelMapInk.ShallowGround => Color.FromRgb(16, 50, 58),
            TravelMapInk.Surf => Color.FromRgb(140, 196, 196),
            TravelMapInk.CliffGround => Color.FromRgb(132, 70, 30),
            TravelMapInk.CliffFace => Color.FromRgb(224, 146, 72),
            TravelMapInk.CliffShade => Color.FromRgb(70, 34, 14),
            TravelMapInk.BasaltGround => Color.FromRgb(34, 16, 14),
            TravelMapInk.BasaltFace => Color.FromRgb(120, 48, 30),
            TravelMapInk.ForestGround => Color.FromRgb(10, 36, 16),
            TravelMapInk.ForestRim => Color.FromRgb(30, 78, 26),
            TravelMapInk.TreeDark => Color.FromRgb(30, 112, 40),
            TravelMapInk.TreeLight => Color.FromRgb(70, 186, 64),
            TravelMapInk.Grass => Color.FromRgb(120, 168, 70),
            TravelMapInk.MarshGround => Color.FromRgb(16, 44, 40),
            TravelMapInk.MarshRim => Color.FromRgb(34, 80, 66),
            TravelMapInk.Reed => Color.FromRgb(96, 196, 150),
            TravelMapInk.ReedDark => Color.FromRgb(50, 128, 104),
            TravelMapInk.LakeGround => Color.FromRgb(14, 66, 190),
            TravelMapInk.LakeWave => Color.FromRgb(110, 176, 255),
            TravelMapInk.AshGround => Color.FromRgb(40, 14, 12),
            TravelMapInk.AshRim => Color.FromRgb(76, 26, 18),
            TravelMapInk.Ash => Color.FromRgb(132, 72, 58),
            TravelMapInk.Cinder => Color.FromRgb(176, 62, 36),
            TravelMapInk.LavaGround => Color.FromRgb(156, 44, 8),
            TravelMapInk.Lava => Color.FromRgb(255, 170, 40),
            TravelMapInk.Ember => Color.FromRgb(255, 214, 80),
            TravelMapInk.SandGround => Color.FromRgb(70, 52, 24),
            TravelMapInk.SandRim => Color.FromRgb(112, 86, 42),
            TravelMapInk.Sand => Color.FromRgb(196, 160, 92),
            TravelMapInk.Dune => Color.FromRgb(236, 196, 116),
            TravelMapInk.Mesa => Color.FromRgb(170, 104, 58),
            TravelMapInk.Snow => Color.FromRgb(238, 238, 236),
            TravelMapInk.Rock => Color.FromRgb(160, 160, 168),
            TravelMapInk.FirePeak => Color.FromRgb(240, 96, 44),
            _ => Color.FromRgb(200, 200, 200)
        };

        /// <summary>
        /// Shaped glyphs keyed by character, color and size bucket, drawn centered on a point.
        /// Thousands of loose characters reuse a few hundred shaped texts.
        /// </summary>
        private sealed class GlyphCache
        {
            private readonly Typeface typeface;
            private readonly double baseEm;
            private readonly Dictionary<(string, uint, int), FormattedText> cache = new();
            private readonly Dictionary<uint, IBrush> brushes = new();

            public GlyphCache(Typeface typeface, double fineW, double fineH)
            {
                this.typeface = typeface;
                // Size so one glyph's monospace advance is about one fine cell.
                var probe = new FormattedText("MMMMMMMMMM", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 100, Brushes.Black);
                double advancePerEm = probe.WidthIncludingTrailingWhitespace / 10 / 100;
                double em = advancePerEm > 0 ? fineW / advancePerEm : fineH;
                baseEm = Math.Min(em * 1.1, fineH * 1.3);
            }

            public void Draw(DrawingContext ctx, char glyph, Color color, double scale, Point center) =>
                Draw(ctx, glyph.ToString(), color, scale, center);

            public void Draw(DrawingContext ctx, string glyph, Color color, double scale, Point center)
            {
                var text = Shape(glyph, color, scale);
                ctx.DrawText(text, new Point(center.X - text.WidthIncludingTrailingWhitespace / 2, center.Y - text.Height / 2));
            }

            public FormattedText Shape(string glyph, Color color, double scale)
            {
                // Quarter-step size buckets keep the cache small without visible stepping.
                int bucket = (int)Math.Round(scale * 4);
                uint argb = color.ToUInt32();
                var key = (glyph, argb, bucket);
                if (!cache.TryGetValue(key, out var text))
                {
                    if (!brushes.TryGetValue(argb, out var brush))
                        brushes[argb] = brush = new SolidColorBrush(color);
                    text = new FormattedText(glyph, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        typeface, Math.Max(1, baseEm * bucket / 4.0), brush);
                    cache[key] = text;
                }
                return text;
            }
        }
    }
}
