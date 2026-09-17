namespace RPGGame.UI.Avalonia.ArtLab;

/// <summary>Seeded visual sampler, independent of character state and live loot tuning.</summary>
public static class ItemRarityPreview
{
    public sealed record Group(string Name, string Color, IReadOnlyList<ItemIconRecipe> Recipes);
    public static readonly string[] Order = ["Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic"];
    private static readonly string[] Colors = ["#ADB8C7", "#79C996", "#73B9F4", "#BA91EE", "#EBC86D", "#F38DA6"];
    private static readonly Dictionary<string, int> suffixCounts = LoadSuffixCounts();

    public static IReadOnlyList<Group> Generate(int seed, int count, string family = "All gear", int? tier = null)
    {
        count = Math.Clamp(count, 1, 24);
        var entries = ItemIconCatalog.Items.Where(e =>
            (family == "All gear" ? e.Family != "consumable" : e.Family.Equals(family, StringComparison.OrdinalIgnoreCase)) &&
            (tier == null || e.Tier == tier)).ToArray();
        if (entries.Length == 0) return [];
        if (family.Equals("consumable", StringComparison.OrdinalIgnoreCase))
        {
            var random = new Random(seed);
            return [new("Consumables", Colors[0], Enumerable.Range(0,count).Select(_ =>
            {
                var e = entries[random.Next(entries.Length)];
                return new ItemIconRecipe(e.Id,e.DefaultMaterial,"",[],[],seed);
            }).ToArray())];
        }
        var result = new List<Group>();
        for (int rank = 0; rank < Order.Length; rank++)
        {
            string rarity = Order[rank];
            // Independent per-rarity streams keep a row stable when the number of samples changes.
            var random = new Random(unchecked(seed + rank * 7919));
            var qualities = Pool("quality", rank, exactFirst: true);
            var adjectives = Pool("adjective", rank);
            var suffixes = Pool("suffix", rank);
            var materials = Pool("material", rank, exactFirst: true);
            var recipes = new List<ItemIconRecipe>();
            for (int i = 0; i < count; i++)
            {
                var entry = entries[random.Next(entries.Length)];
                string material = Enum.TryParse<WeaponType>(entry.Family,true,out var weaponType)
                    ? ItemMaterialRules.ResolveWeaponMaterial(weaponType,rarity)
                    : materials[random.Next(materials.Length)].Name;
                var chosen = suffixes.OrderBy(_ => random.Next()).Take(suffixCounts.GetValueOrDefault(rarity)).Select(a=>a.Name).ToArray();
                recipes.Add(new(entry.Id, material, qualities[random.Next(qualities.Length)].Name,
                    rank == 0 ? [] : [adjectives[random.Next(adjectives.Length)].Name], chosen, seed));
            }
            result.Add(new(rarity,Colors[rank],recipes));
        }
        return result;
    }

    private static ItemIconCatalog.Affix[] Pool(string kind, int rank, bool exactFirst = false)
    {
        var pool = ItemIconCatalog.Affixes.Where(a => a.Kind == kind).ToArray();
        if (exactFirst)
        {
            var exact = pool.Where(a => a.Rarity.Equals(Order[rank],StringComparison.OrdinalIgnoreCase)).ToArray();
            if (exact.Length > 0) return exact;
        }
        var eligible = pool.Where(a => Array.FindIndex(Order,r => r.Equals(a.Rarity,StringComparison.OrdinalIgnoreCase)) <= rank).ToArray();
        return eligible.Length > 0 ? eligible : pool;
    }
    private static Dictionary<string,int> LoadSuffixCounts()
    {
        using var doc = ItemIconCatalog.Read("RarityTable");
        return doc.RootElement.EnumerateArray().ToDictionary(r=>r.GetProperty("Name").GetString()!,r=>r.GetProperty("StatBonuses").GetInt32(),StringComparer.OrdinalIgnoreCase);
    }
}
