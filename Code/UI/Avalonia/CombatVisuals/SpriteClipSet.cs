using System;
using System.Collections.Generic;
using System.Text.Json;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace RPGGame.UI.Avalonia.CombatVisuals;

/// <summary>One actor's atlases, loaded once; positions use the authored 800x450 canvas.</summary>
public sealed class SpriteClipSet : IDisposable
{
    private sealed record Clip(Bitmap Image, int Count, int Width, int Height, int X, int Y, int Fps, int? Impact);
    private readonly Dictionary<string, Clip> clips = new();
    public SpriteClipSet(string actor)
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://DF/Visuals/Animations/clips.json"));
            using var manifest = JsonDocument.Parse(stream);
            var data = manifest.RootElement.GetProperty("actors").GetProperty(actor);
            foreach (var entry in data.EnumerateObject())
            {
                var c = entry.Value;
                int width = c.GetProperty("frameWidth").GetInt32(), height = c.GetProperty("frameHeight").GetInt32();
                int count = c.GetProperty("frames").GetInt32();
                int fps = c.GetProperty("fps").GetInt32();
                int x = c.GetProperty("x").GetInt32(), y = c.GetProperty("y").GetInt32();
                int? contact = c.TryGetProperty("impactFrame", out var impact) && impact.ValueKind == JsonValueKind.Number
                    ? impact.GetInt32() : null;
                if (count < 1 || width < 1 || height < 1 || fps < 1 || contact is < 0 || contact >= count
                    || x < 0 || y < 0 || (long)x + width > 800 || (long)y + height > 450)
                    throw new InvalidOperationException("Invalid sprite atlas metadata");
                int atlasWidth = checked(width * count);
                using var png = AssetLoader.Open(new Uri("avares://DF/Visuals/Animations/" + c.GetProperty("file").GetString()));
                var image = new Bitmap(png);
                if (image.PixelSize != new PixelSize(atlasWidth, height))
                { image.Dispose(); throw new InvalidOperationException("Invalid sprite atlas dimensions"); }
                clips.Add(entry.Name, new Clip(image, count, width, height, x, y, fps, contact));
            }
            foreach (string required in new[] { "idle", "attack", "hit", "cast", "guard", "evade", "death", "victory" })
                if (!clips.ContainsKey(required)) throw new InvalidOperationException($"Missing {actor}/{required} clip");
        }
        catch { Dispose(); throw; }
    }
    public bool Draw(DrawingContext context, Rect stage, string name, double elapsed, double duration)
    {
        bool preparing = name.StartsWith("prepare-", StringComparison.Ordinal);
        if (preparing)
        {
            name = name[8..];
            elapsed = Math.Min(elapsed, duration * .3);
        }
        if (!clips.TryGetValue(name, out var c)) return false;
        int frame = SelectFrame(c.Count, c.Fps, c.Impact, name == "idle", preparing, elapsed, duration);
        var source = new Rect(frame * c.Width, 0, c.Width, c.Height);
        var destination = new Rect(stage.X + c.X * stage.Width / 800, stage.Y + c.Y * stage.Height / 450,
            c.Width * stage.Width / 800, c.Height * stage.Height / 450);
        context.DrawImage(c.Image, source, destination);
        return true;
    }
    internal static int SelectFrame(int count, int fps, int? impact, bool idle, bool preparing, double elapsed, double duration)
    {
        if (idle) return (int)(Math.Max(0, elapsed) * fps) % count;
        double progress = Math.Max(0, elapsed) / Math.Max(.04, duration);
        if (preparing) progress = Math.Min(progress, .3);
        // A short contact hold gives strikes weight without pausing the simulation.
        if (!preparing && impact.HasValue && progress >= .5 && progress < 1)
        {
            double contact = Math.Min(.09, .035 / Math.Max(.04, duration));
            progress = progress < .5 + contact ? impact.Value / (double)count
                : .5 + (progress - .5 - contact) * .5 / (.5 - contact);
        }
        int frame = Math.Clamp((int)(progress * count), 0, count - 1);
        return preparing && impact.HasValue ? Math.Min(frame, Math.Max(0, impact.Value - 1)) : frame;
    }
    public void Dispose() { foreach (var clip in clips.Values) clip.Image.Dispose(); clips.Clear(); }
}
