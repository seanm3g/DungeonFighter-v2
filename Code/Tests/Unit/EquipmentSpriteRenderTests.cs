using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Media.Imaging;
using RPGGame.UI.Avalonia.ArtLab;

namespace RPGGame.Tests.Unit;

/// <summary>Renders the reported loadout through the actual Avalonia portrait without opening a window or save.</summary>
public static class EquipmentSpriteRenderTests
{
    public static void Run(string output)
    {
        AppBuilder.Configure<Application>().UsePlatformDetect().SetupWithoutStarting();
        var player = TestDataBuilders.Character().WithName("Sprite fixture").Build();
        player.Equipment.Head = new HeadItem("Veil", 1);
        player.Equipment.Body = new ChestItem("Duelist Coat", 1);
        player.Equipment.Legs = new LegsItem("Knight's Greaves", 4);
        player.Equipment.Feet = new FeetItem("Pelt Boots", 2);
        player.Equipment.Weapon = new WeaponItem("Mace", 2, weaponType: WeaponType.Mace);
        var portrait = new HeroPortrait { Character = player, Width = 400, Height = 600 };
        portrait.Measure(new Size(400, 600));
        portrait.Arrange(new Rect(0, 0, 400, 600));
        using var capture = new RenderTargetBitmap(new PixelSize(400, 600), new Vector(96, 96));
        capture.Render(portrait);
        byte[] equipped = Snapshot();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        capture.Save(output);
        // Exercise frame-cache invalidation for changing and removing equipment.
        player.Equipment.Head = new HeadItem("Cowl", 1);
        player.Equipment.Feet = null;
        capture.Render(portrait);
        byte[] changed = Snapshot();
        if (equipped.SequenceEqual(changed)) throw new InvalidOperationException("Replacing equipment did not invalidate the portrait frame.");
        player.Equipment.Head = null;
        player.Equipment.Body = null;
        player.Equipment.Legs = null;
        player.Equipment.Weapon = null;
        capture.Render(portrait);
        if (changed.SequenceEqual(Snapshot())) throw new InvalidOperationException("Unequipping did not remove the portrait layers.");
        Console.WriteLine("Native sprite rendering: reported loadout, gear replacement and unequip passed.");

        byte[] Snapshot()
        {
            using var stream = new MemoryStream();
            capture.Save(stream);
            return stream.ToArray();
        }
    }
}
