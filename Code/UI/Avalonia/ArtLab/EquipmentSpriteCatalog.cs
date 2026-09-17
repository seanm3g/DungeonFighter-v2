using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace RPGGame.UI.Avalonia.ArtLab;

/// <summary>Authored atlas rectangles keyed to the game's catalog, not randomized affixes.</summary>
public static class EquipmentSpriteCatalog
{
    public sealed class Sprite
    {
        public string Sheet { get; set; } = "";
        public string Name { get; set; } = "";
        public int Tier { get; set; }
        public int CatalogIndex { get; set; }
        public double[] Source { get; set; } = [];
        public double[] Target { get; set; } = [];
        public double[][] Coverage { get; set; } = [];
        public bool HideHair { get; set; }
        public bool SplitFeet { get; set; }
        public double[] Grip { get; set; } = [];
        public Part[] Parts { get; set; } = [];
    }

    public sealed class Part
    {
        public string Layer { get; set; } = "front";
        public double[] Source { get; set; } = [];
        public double[] Target { get; set; } = [];
    }

    private static readonly Lazy<List<Sprite>> sprites = new(() =>
    {
        using var stream = typeof(EquipmentSpriteCatalog).Assembly.GetManifestResourceStream("EquipmentSprites")
            ?? throw new InvalidOperationException("Equipment sprite manifest is missing from the build.");
        return JsonSerializer.Deserialize<List<Sprite>>(stream) ?? [];
    });

    public static IReadOnlyList<Sprite> All => sprites.Value;

    public static string SheetFor(Item item) => item.Type switch
    {
        ItemType.Head => "head",
        ItemType.Chest => "chest",
        ItemType.Legs => "legs",
        ItemType.Feet => "feet",
        ItemType.Weapon when item is WeaponItem weapon => weapon.WeaponType.ToString().ToLowerInvariant(),
        ItemType.Weapon => "sword",
        _ => ""
    };

    public static Sprite? Resolve(Item? item)
    {
        if (item == null) return null;
        string sheet = SheetFor(item);
        var candidates = All.Where(s => s.Sheet == sheet).ToList();
        string name = item.Name ?? "";
        // Longest whole catalog name wins: "Full Plate Boots" must beat "Boots".
        // Tier disambiguates duplicate catalog names. Same-name/same-tier duplicates
        // cannot be distinguished in existing saves, which store no catalog row id.
        return candidates.Where(s => ContainsName(name, s.Name))
            .OrderByDescending(s => s.Name.Length)
            .ThenBy(s => Math.Abs(s.Tier - item.Tier))
            .ThenBy(s => s.CatalogIndex).FirstOrDefault()
            ?? candidates.OrderBy(s => Math.Abs(s.Tier - item.Tier)).ThenBy(s => s.CatalogIndex).FirstOrDefault();
    }

    private static bool ContainsName(string full, string name)
    {
        for (int start = 0; start <= full.Length - name.Length; start++)
        {
            if (string.Compare(full, start, name, 0, name.Length, StringComparison.OrdinalIgnoreCase) != 0) continue;
            int end = start + name.Length;
            if ((start == 0 || !char.IsLetterOrDigit(full[start - 1])) &&
                (end == full.Length || !char.IsLetterOrDigit(full[end]))) return true;
        }
        return false;
    }
}
