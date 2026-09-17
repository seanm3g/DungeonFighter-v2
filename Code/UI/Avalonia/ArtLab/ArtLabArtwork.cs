using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace RPGGame.UI.Avalonia.ArtLab;

/// <summary>Composes the live equipment slots over a shared unarmored base.</summary>
public sealed class HeroPortrait : Control
{
    private readonly Dictionary<string, Bitmap> sheets = new();
    private RenderTargetBitmap? frame;
    private string? frameKey;
    public Character? Character { get; set; }

    public HeroPortrait() => RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#101313")), new Rect(Bounds.Size));
        double scale = Math.Min(Bounds.Width / 400, Bounds.Height / 600);
        double left = (Bounds.Width - 400 * scale) / 2;
        double top = (Bounds.Height - 600 * scale) / 2;
        Rect Target(double x, double y, double w, double h) => new(left + x * scale, top + y * scale, w * scale, h * scale);
        var equipped = new[] { Character?.Feet, Character?.Legs, Character?.Body, Character?.Head, Character?.Weapon }
            .Select(EquipmentSpriteCatalog.Resolve).ToArray();
        string key = string.Join("|", equipped.Select(s => s == null ? "-" : $"{s.Sheet}:{s.CatalogIndex}"));
        if (frame == null || frameKey != key)
        {
            frame ??= new RenderTargetBitmap(new PixelSize(400, 600), new Vector(96, 96));
            using var pixels = frame.CreateDrawingContext();
            using var options = pixels.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None, EdgeMode = EdgeMode.Aliased });
            foreach (var sprite in equipped)
                if (sprite != null) DrawSprite(pixels, sprite, "rear");
            var body = Sheet("base");
            var full = new Rect(0, 0, 400, 600);
            if (equipped[3]?.HideHair == true)
            {
                // Hair must not protrude through a hood. Keep the face and neck behind its opening.
                using (pixels.PushGeometryClip(new CombinedGeometry(GeometryCombineMode.Exclude,
                    new RectangleGeometry(full), new RectangleGeometry(new Rect(136, 12, 128, 104)))))
                    pixels.DrawImage(body, new Rect(body.Size), full);
                using (pixels.PushClip(new Rect(176, 64, 48, 52)))
                    pixels.DrawImage(body, new Rect(body.Size), full);
            }
            else pixels.DrawImage(body, new Rect(body.Size), full);
            // Feet intentionally precede legs: trouser cuffs and greaves cover boot tops.
            foreach (var sprite in equipped)
                if (sprite != null) DrawSprite(pixels, sprite, "front");
            if (equipped[4] != null)
                using (pixels.PushClip(new Rect(280, 312, 16, 12)))
                    pixels.DrawImage(body, new Rect(body.Size), full);
            frameKey = key;
        }
        using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None, EdgeMode = EdgeMode.Aliased }))
            context.DrawImage(frame, new Rect(0, 0, 400, 600), Target(0, 0, 400, 600));
    }

    private void DrawSprite(DrawingContext context, EquipmentSpriteCatalog.Sprite sprite, string layer)
    {
        var source = new Rect(sprite.Source[0], sprite.Source[1], sprite.Source[2], sprite.Source[3]);
        foreach (var part in sprite.Parts.Where(p => p.Layer == layer))
        {
            DrawPart(new Rect(part.Source[0],part.Source[1],part.Source[2],part.Source[3]),
                new Rect(part.Target[0],part.Target[1],part.Target[2],part.Target[3]));
        }

        void DrawPart(Rect region, Rect destination)
        {
            // Coverage is sprite mesh metadata: it excludes background and authored face/neck openings.
            var geometry = new StreamGeometry();
            using (var path = geometry.Open())
            {
                path.SetFillRule(FillRule.NonZero);
                foreach (var r in sprite.Coverage)
                {
                    var clipped = new Rect(r[0],r[1],r[2],r[3]).Intersect(region);
                    if (clipped.Width <= 0 || clipped.Height <= 0) continue;
                    double x = destination.X + (clipped.X-region.X) / region.Width * destination.Width;
                    double y = destination.Y + (clipped.Y-region.Y) / region.Height * destination.Height;
                    double w = clipped.Width / region.Width * destination.Width, h = clipped.Height / region.Height * destination.Height;
                    path.BeginFigure(new Point(x, y), true);
                    path.LineTo(new Point(x + w, y)); path.LineTo(new Point(x + w, y + h));
                    path.LineTo(new Point(x, y + h)); path.EndFigure(true);
                }
            }
            using (context.PushGeometryClip(geometry)) context.DrawImage(Sheet(sprite.Sheet),
                new Rect(source.X+region.X,source.Y+region.Y,region.Width,region.Height), destination);
        }
    }

    private Bitmap Sheet(string name)
    {
        if (sheets.TryGetValue(name, out var bitmap)) return bitmap;
        using var stream = AssetLoader.Open(new Uri($"avares://DF/UI/Avalonia/Assets/ArtLab/equipment-pixel-{name}.png"));
        return sheets[name] = new Bitmap(stream);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        foreach (var bitmap in sheets.Values) bitmap.Dispose();
        sheets.Clear();
        frame?.Dispose();
        frame = null;
        frameKey = null;
        base.OnDetachedFromVisualTree(e);
    }
}

public sealed class DungeonMapLines : Control
{
    public ArtLabSession? Session { get; set; }
    public static Point Position(ArtLabSession.Room room) => new(20 + room.X * 48, 24 + room.Y * 40);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var grid = new SolidColorBrush(Color.Parse("#26302C"));
        for (int x = 8; x < Bounds.Width; x += 12)
            for (int y = 4; y < Bounds.Height; y += 12)
                context.FillRectangle(grid, new Rect(x, y, 1, 1));
        if (Session == null) return;
        var rooms = Session.Rooms;
        foreach (var room in rooms)
        foreach (int other in room.Neighbors)
        {
            if (other <= room.Id || !Session.IsOnMapPage(room.Id) || !Session.IsOnMapPage(other)) continue;
            var to = rooms[other];
            var a = Position(room);
            var b = Position(to);
            var bend = new Point(b.X, a.Y);
            bool visited = Session.IsVisited(room.Id) && Session.IsVisited(other);
            bool discovered = Session.IsDiscovered(room.Id) && Session.IsDiscovered(other);
            var pen = new Pen(new SolidColorBrush(Color.Parse(visited ? "#8A9E47" : discovered ? "#435046" : "#28332D")), 2);
            context.DrawLine(pen, a, bend);
            context.DrawLine(pen, bend, b);
        }
    }
}

/// <summary>Fixed-seed print flecks; static so repainting never shimmers.</summary>
public sealed class PrintGrain : Control
{
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var random = new Random(1979);
        var brush = new SolidColorBrush(Color.Parse("#A0AD84"));
        for (int i = 0; i < 1600; i++)
            context.FillRectangle(brush, new Rect(random.NextDouble() * Bounds.Width, random.NextDouble() * Bounds.Height, i % 5 == 0 ? 3 : 1, 1));
        var red = new SolidColorBrush(Color.Parse("#B83932"));
        context.FillRectangle(red, new Rect(0, 0, Bounds.Width, 3));
    }
}
