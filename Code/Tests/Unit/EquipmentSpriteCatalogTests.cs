using System;
using System.Linq;
using RPGGame.UI.Avalonia.ArtLab;

namespace RPGGame.Tests.Unit;

public static class EquipmentSpriteCatalogTests
{
    public static void Run()
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            checks++;
        }
        Check(EquipmentSpriteCatalog.Resolve(null) == null, "Empty equipment must draw no overlay.");
        Check(EquipmentSpriteCatalog.Resolve(new Item(ItemType.Consumable, "Potion")) == null, "Consumables are not worn.");
        foreach (var entry in EquipmentSpriteCatalog.All)
        {
            Item item = entry.Sheet switch
            {
                "head" => new HeadItem(entry.Name, entry.Tier),
                "chest" => new ChestItem(entry.Name, entry.Tier),
                "legs" => new LegsItem(entry.Name, entry.Tier),
                "feet" => new FeetItem(entry.Name, entry.Tier),
                _ => new WeaponItem(entry.Name, entry.Tier, weaponType: Enum.Parse<WeaponType>(entry.Sheet, true))
            };
            var exact = EquipmentSpriteCatalog.Resolve(item);
            Check(exact?.Name == entry.Name && exact?.Tier == entry.Tier && exact?.Sheet == entry.Sheet,
                $"Missing catalog mapping: {entry.Sheet}/{entry.Name}/{entry.Tier}");
            item.Name = "Legendary Tempered Steel " + item.Name + " of the Fox";
            Check(ReferenceEquals(exact, EquipmentSpriteCatalog.Resolve(item)), "Affixes must not change base gear selection.");
            Check(entry.Source.Length == 4 && entry.Source.All(v => v >= 0) && entry.Source[2] > 0 && entry.Source[3] > 0, "Invalid source rectangle.");
            Check(entry.Coverage.Length > 0 && entry.Coverage.All(r => r.Length == 4 && r[0]>=0 && r[1]>=0 && r[2]>0 && r[3]>0 && r[0]+r[2]<=entry.Source[2] && r[1]+r[3]<=entry.Source[3]), "Sprite coverage must stay inside its source rectangle.");
            Check(entry.Parts.Length > 0 && entry.Parts.All(p => p.Source.Length==4 && p.Target.Length==4 && p.Source[0]>=0 && p.Source[1]>=0 && p.Source[2]>0 && p.Source[3]>0 && p.Source[0]+p.Source[2]<=entry.Source[2]+.001 && p.Source[1]+p.Source[3]<=entry.Source[3]+.001 && p.Target[2]>0 && p.Target[3]>0), "Fitted garment parts must have valid source and destination bounds.");
            Check(entry.Parts.All(p => p.Layer is "rear" or "front"), "Unknown equipment layer.");
            if (entry.Name is "Veil" or "Headdress")
            {
                Check(entry.Parts.Any(p => p.Layer == "rear") && entry.Parts.Any(p => p.Layer == "front"), "Draped headwear needs both rear and front pieces.");
                Check(!entry.HideHair && entry.Parts.Where(p => p.Layer == "front").All(p => p.Target[0]+p.Target[2]<=176 || p.Target[0]>=224 || p.Target[1]+p.Target[3]<=64 || p.Target[1]>=116), "Draped headwear must leave the face clear.");
            }
            if (entry.Sheet == "legs") Check(entry.Parts.All(p => p.Target[1]+p.Target[3]<=520.001), "Leg armor must stop at the ankle instead of adding a second foot.");
            if (entry.Sheet == "feet") Check(entry.Parts.Length>=2 && entry.Parts.All(p => p.Target[1]+p.Target[3]<=564.001) && entry.Parts.Count(p => Math.Abs(p.Target[1]+p.Target[3]-564)<.001)==2, "Each boot must meet the shared sole baseline.");
            if (entry.Grip.Length == 2)
            {
                Check(Math.Abs(entry.Target[0]+entry.Grip[0]*entry.Target[2]-288)<=2 &&
                    Math.Abs(entry.Target[1]+entry.Grip[1]*entry.Target[3]-316)<=2,
                    "Weapon grip must meet the hand within half a native pixel.");
            }
        }
        Check(EquipmentSpriteCatalog.Resolve(new FeetItem("Full Plate Boots", 5))?.Name == "Full Plate Boots", "Longest catalog name must beat Boots.");
        Check(EquipmentSpriteCatalog.Resolve(new WeaponItem("New modded weapon", 4, weaponType: WeaponType.Wand))?.Sheet == "wand", "Unknown weapons must retain their weapon family.");
        Console.WriteLine($"Equipment sprites: {checks} mapping checks passed.");
    }
}
