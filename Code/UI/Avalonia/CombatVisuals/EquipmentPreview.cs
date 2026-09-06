using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace RPGGame.UI.Avalonia.CombatVisuals;

public sealed class EquipmentPreview : IDisposable
{
    private Bitmap? bitmap;
    private string? key;
    public Rect Bounds { get; private set; }
    public void Set(string asset, Rect bounds)
    {
        Bounds = bounds;
        if (key == asset) return;
        Dispose();
        using var stream = AssetLoader.Open(new Uri($"avares://DF/Visuals/{asset}-layer.png"));
        bitmap = new Bitmap(stream);
        key = asset;
    }
    public void Draw(DrawingContext context, double cellWidth, double cellHeight)
    {
        if (bitmap == null) return;
        var area = new Rect(Bounds.X * cellWidth, Bounds.Y * cellHeight, Bounds.Width * cellWidth, Bounds.Height * cellHeight);
        double size = Math.Min(area.Width / 300, area.Height / 430);
        var destination = new Rect(area.Center.X - 150 * size, area.Y, 300 * size, 430 * size);
        context.DrawImage(bitmap, new Rect(330, 200, 300, 430), destination);
    }
    public void Dispose() { bitmap?.Dispose(); bitmap = null; key = null; }
    public static string Asset(Character hero, Item item)
    {
        string weapon = (item as WeaponItem ?? hero.Weapon)?.WeaponType.ToString().ToLowerInvariant() ?? "unarmed";
        var body = item as ChestItem ?? hero.Body as ChestItem;
        return $"fighter-{weapon}-{(body?.GetTotalArmor() > 0 ? "plate" : "cloth")}";
    }
}
