using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame.Data.ItemGeneration
{
    /// <summary>The four mod pools; each line picks one by <c>lootSystem.itemGeneration.poolWeights</c>.</summary>
    public static class ModPools
    {
        public const string Stat = "stat";
        public const string Material = "material";
        public const string Mechanic = "mechanic";
        public const string Action = "action";

        public static readonly string[] All = { Material, Stat, Mechanic, Action };
    }

    /// <summary>
    /// Suffix kinds, in name order: common = descriptor ("of Might"), uncommon = animal ("of the Dire Bear", Pack-able),
    /// rare = poetic phrase for the fanciest effects ("Bathed in Blood"). An item holds at most one of each.
    /// </summary>
    public static class SuffixKinds
    {
        public const string Common = "common";
        public const string Uncommon = "uncommon";
        public const string Rare = "rare";

        public static readonly string[] All = { Common, Uncommon, Rare };

        /// <summary>0 / 1 / 2 by name order; unknown (old lines) count as uncommon.</summary>
        public static int Rank(string? kind)
        {
            int i = Array.FindIndex(All, k => string.Equals(k, kind?.Trim(), StringComparison.OrdinalIgnoreCase));
            return i < 0 ? 1 : i;
        }
    }

    /// <summary>One tier of a mod family: its keyword, value range, item-level gate, weight and rarity gate.</summary>
    public sealed class ItemModTier
    {
        [JsonPropertyName("keyword")] public string Keyword { get; set; } = "";
        [JsonPropertyName("min")] public double Min { get; set; }
        [JsonPropertyName("max")] public double Max { get; set; }

        /// <summary>Item level needed (<see cref="SlotLevel"/> overrides it per slot).</summary>
        [JsonPropertyName("level")] public int Level { get; set; } = 1;

        [JsonPropertyName("slotLevel")] public Dictionary<string, int>? SlotLevel { get; set; }

        /// <summary>Relative chance among the family's eligible tiers.</summary>
        [JsonPropertyName("weight")] public double Weight { get; set; } = 100;

        /// <summary>Highest item rarity that can roll this tier (null = any). "Common" marks the common-only highs.</summary>
        [JsonPropertyName("maxRarity")] public string? MaxRarity { get; set; }

        /// <summary>Optional slot limit for this tier only.</summary>
        [JsonPropertyName("slots")] public List<string>? Slots { get; set; }

        public int LevelFor(string slot)
        {
            if (SlotLevel != null)
                foreach (var kv in SlotLevel)
                    if (string.Equals(kv.Key, slot, StringComparison.OrdinalIgnoreCase))
                        return kv.Value;
            return Level;
        }

        public bool AllowsSlot(string slot) =>
            Slots == null || Slots.Count == 0 || Slots.Any(s => string.Equals(s, slot, StringComparison.OrdinalIgnoreCase));

        public bool AllowsRarity(int rarityIndex) =>
            string.IsNullOrWhiteSpace(MaxRarity) || rarityIndex <= RarityOrder.IndexOf(MaxRarity);
    }

    /// <summary>A mod family (+Life, Vampiric, of the Ant …) and its tier ladder.</summary>
    public sealed class ItemModFamily
    {
        [JsonPropertyName("family")] public string Family { get; set; } = "";
        [JsonPropertyName("pool")] public string Pool { get; set; } = ModPools.Stat;

        /// <summary>prefix / suffix: where the keyword goes in the item name.</summary>
        [JsonPropertyName("affix")] public string Affix { get; set; } = "suffix";

        /// <summary>stat (a StatBonus of <see cref="StatType"/>), mod (clone of <see cref="Template"/> modification),
        /// suffix (clone of the <see cref="Template"/> StatBonuses row), slots (+combo strip slots), hybrid (one keyword
        /// that rolls each of <see cref="Parts"/> at the hybrid part share).</summary>
        [JsonPropertyName("kind")] public string Kind { get; set; } = "stat";

        /// <summary>hybrid only: the single-stat families it combines (same slot, same tier index).</summary>
        [JsonPropertyName("parts")] public List<string>? Parts { get; set; }

        /// <summary>Suffix families only: common / uncommon / rare (<see cref="SuffixKinds"/>). Null for prefixes.</summary>
        [JsonPropertyName("suffixKind")] public string? SuffixKind { get; set; }

        [JsonPropertyName("statType")] public string? StatType { get; set; }
        [JsonPropertyName("template")] public string? Template { get; set; }

        /// <summary>Tooltip text for stat lines; <c>{n}</c> is the rolled value.</summary>
        [JsonPropertyName("description")] public string? Description { get; set; }

        /// <summary>Slot domain (D11.4): the slots this family rolls on; empty = any. An off-slot line can still roll
        /// through the rare escape (D11.5) unless <see cref="Escape"/> is false.</summary>
        [JsonPropertyName("slots")] public List<string>? Slots { get; set; }

        /// <summary>Class affinity (D11.2): the classes (4 core + 6 hybrids, <see cref="ClassAffinity"/>) whose builds
        /// this family serves; "material" = the item's material's class. Empty = neutral.</summary>
        [JsonPropertyName("classes")] public List<string>? Classes { get; set; }

        /// <summary>False: the slot domain is absolute (never rolls off-slot).</summary>
        [JsonPropertyName("escape")] public bool? Escape { get; set; }

        [JsonPropertyName("weight")] public double Weight { get; set; } = 1;

        /// <summary>Influence tags: what this family leans toward (class, events, stores, stats).</summary>
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; }

        /// <summary>Values round to whole numbers.</summary>
        [JsonPropertyName("integer")] public bool Integer { get; set; }

        [JsonPropertyName("tiers")] public List<ItemModTier> Tiers { get; set; } = new();

        public bool AllowsSlot(string slot) =>
            Slots == null || Slots.Count == 0 || Slots.Any(s => string.Equals(s, slot, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary><c>ItemMods.json</c>: every family a rolled item line can come from.</summary>
    public sealed class ItemModTable
    {
        [JsonPropertyName("families")] public List<ItemModFamily> Families { get; set; } = new();

        public IEnumerable<ItemModFamily> InPool(string pool) =>
            Families.Where(f => string.Equals(f.Pool, pool, StringComparison.OrdinalIgnoreCase));

        public ItemModFamily? Find(string name) =>
            Families.FirstOrDefault(f => string.Equals(f.Family, name, StringComparison.OrdinalIgnoreCase));

        public static ItemModTable Load()
        {
            try
            {
                string? path = JsonLoader.FindGameDataFile("ItemMods.json");
                if (path == null)
                    return new ItemModTable();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<ItemModTable>(File.ReadAllText(path), options) ?? new ItemModTable();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading ItemMods.json: {ex.Message}");
                return new ItemModTable();
            }
        }
    }

    /// <summary>Rarity ladder order (Common = 0 … Mythic = 5).</summary>
    public static class RarityOrder
    {
        public static readonly string[] Names = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };

        /// <summary>Index of <paramref name="rarity"/>; unknown names count as Mythic so gates stay permissive.</summary>
        public static int IndexOf(string? rarity)
        {
            if (string.IsNullOrWhiteSpace(rarity))
                return 0;
            int i = Array.FindIndex(Names, n => string.Equals(n, rarity.Trim(), StringComparison.OrdinalIgnoreCase));
            return i < 0 ? Names.Length - 1 : i;
        }
    }
}
