using System.Text.Json;

namespace RPGGame.UI.Avalonia.ArtLab;

/// <summary>Read-only snapshot of shipped catalogs. IDs include source row to preserve duplicate names.</summary>
public static class ItemIconCatalog
{
    public sealed record Entry(string Id, string Name, string Family, int Tier, string Shape, string DefaultMaterial);
    public sealed record Affix(string Name, string Kind, string Stat, string Rarity = "");
    public static readonly IReadOnlyList<Entry> Items = LoadItems();
    public static readonly IReadOnlyList<Affix> Affixes = LoadAffixes();
    public static IEnumerable<string> Names(string kind) => Affixes.Where(a => a.Kind == kind).Select(a => a.Name);
    internal static JsonDocument Read(string name) => JsonDocument.Parse(typeof(ItemIconCatalog).Assembly
        .GetManifestResourceStream("IconCatalog." + name) ?? throw new InvalidOperationException("Missing icon catalog: " + name));
    private static string S(JsonElement e, string key) => e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
    private static IReadOnlyList<Entry> LoadItems()
    {
        var list = new List<Entry>();
        foreach (string file in new[] { "Weapons", "Armor", "Consumables" })
        {
            using var doc = Read(file);
            int index = 0;
            foreach (var row in doc.RootElement.EnumerateArray())
            {
                int id = index++;
                string name = S(row, file == "Consumables" ? "displayName" : "name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                string family = file == "Weapons" ? S(row, "type").ToLowerInvariant() : file == "Armor" ? S(row, "slot") : "consumable";
                int tier = row.TryGetProperty("tier", out var t) ? t.GetInt32() : 1;
                string shape = ShapeFor(family, name);
                string material = family switch { "sword" => "Bronze", "dagger" => "Glass", "mace" => "Bone", "wand" => "Willow", _ =>
                    shape is "hood" or "cap" or "shirt" or "pants" or "wrap" ? "Cloth" : shape is "coat" or "shoe" or "sandals" ? "Leather" : "Steel" };
                if (family == "consumable") material = S(row, "internalKind") == "Food" ? "Wood" : "Glass";
                list.Add(new($"{file.ToLowerInvariant()}:{id}", name, family, tier, shape, material));
            }
        }
        return list.AsReadOnly();
    }
    private static IReadOnlyList<Affix> LoadAffixes()
    {
        var list = new List<Affix>();
        foreach (string file in new[] { "PrefixMaterialQuality", "Modifications", "StatBonuses" })
        {
            using var doc = Read(file);
            foreach (var row in doc.RootElement.EnumerateArray())
            {
                string name = S(row, "Name");
                if (name.Length == 0) continue;
                string stat = S(row, "StatType");
                if (stat.Length == 0 && row.TryGetProperty("Mechanics", out var mechanics) && mechanics.ValueKind == JsonValueKind.Array)
                    stat = string.Join("|", mechanics.EnumerateArray().Select(m => S(m, "StatType")));
                list.Add(new(name, file == "StatBonuses" ? "suffix" : S(row, "prefixCategory").ToLowerInvariant(), stat,
                    S(row, file == "StatBonuses" ? "Rarity" : "ItemRank")));
            }
        }
        foreach (string name in new[] { "Wood", "Leather", "Cloth" }) list.Add(new(name, "material", ""));
        return list.AsReadOnly();
    }
    public static Entry Resolve(Item item)
    {
        string family = item.Type == ItemType.Consumable ? "consumable" : EquipmentSpriteCatalog.SheetFor(item);
        string full = item.Name ?? "";
        var candidates = Items.Where(e => e.Family == family);
        var match = candidates.Where(e => ContainsName(full, e.Name)).OrderByDescending(e => e.Name.Length)
            .ThenBy(e => Math.Abs(e.Tier - item.Tier)).ThenBy(e => e.Id, StringComparer.Ordinal).FirstOrDefault();
        return match ?? new("custom:" + family + ":" + full, full, family, item.Tier, ShapeFor(family, full), "Steel");
    }
    private static bool ContainsName(string full, string part)
    {
        int start = 0;
        while ((start = full.IndexOf(part, start, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            int end = start + part.Length;
            if ((start == 0 || !char.IsLetterOrDigit(full[start - 1])) && (end == full.Length || !char.IsLetterOrDigit(full[end]))) return true;
            start++;
        }
        return false;
    }
    public static string ShapeFor(string family, string name)
    {
        string n = name.ToLowerInvariant();
        bool Has(params string[] words) => words.Any(n.Contains);
        return family switch
        {
            "sword" => Has("scythe", "reaper", "halbre", "glaive") ? "polearm" : Has("cleav", "chop", "great", "bastard", "claymore", "splitter") ? "cleaver" : Has("rapier", "foil", "stiletto") ? "rapier" : Has("saber", "scimitar", "cutlass", "kukri") ? "saber" : "sword",
            "dagger" => Has("hook", "claw", "kris") ? "kris" : Has("needle", "spike", "pick", "stiletto", "thorn") ? "needle" : "dagger",
            "mace" => Has("hammer", "mallet", "maul") ? "hammer" : Has("flail") ? "flail" : Has("club", "log", "cudgel", "bludgeon") ? "club" : "mace",
            "wand" => Has("tome", "grimoire", "covenant") ? "book" : Has("scroll", "binding") ? "scroll" : Has("skull", "head") ? "skull" : Has("vial", "vessel", "cauldron") ? "potion" : Has("lantern", "candle") ? "lantern" : Has("orb", "lens", "bead", "essence", "axiom", "convergence", "seed") ? "orb" : Has("crystal", "shard", "prism", "geode") ? "crystal" : Has("rod", "staff", "stick", "prong") ? "wand" : Has("monolith", "obelisk", "pillar", "altar", "totem") ? "totem" : "charm",
            "head" => Has("crown", "circlet", "band") ? "crown" : Has("hood", "cowl", "veil", "wrap", "headdress", "kerf") ? "hood" : Has("cap", "toque", "hat", "scraps") ? "cap" : Has("mask", "faceplate", "visor", "jaw") ? "mask" : "helm",
            "chest" => Has("coat", "jacket", "jerkin", "mantle") ? "coat" : Has("shirt", "wrap", "tabard", "vest") ? "shirt" : Has("mail", "scale", "splint") ? "mail" : "plate",
            "legs" => Has("breech", "trouser", "legging", "october") ? "pants" : Has("wrap") ? "wrap" : Has("tasset") ? "tassets" : "guards",
            "feet" => Has("sandal", "binding") ? "sandals" : Has("shoe", "slipper", "sneaker", "brogue", "mocc") ? "shoe" : "boot",
            "consumable" => Has("apple") ? "apple" : Has("bread", "ration") ? "bread" : Has("cheese") ? "cheese" : Has("jerky", "sausage") ? "meat" : "potion",
            _ => "charm"
        };
    }
}
