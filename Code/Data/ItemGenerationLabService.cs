using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    public enum ItemGenerationItemType
    {
        Both,
        Weapons,
        Armor
    }

    public enum ItemGenerationArmorSlot
    {
        Any,
        Head,
        Chest,
        Feet
    }

    public sealed class ItemGenerationSpec
    {
        public ItemGenerationItemType ItemType { get; set; } = ItemGenerationItemType.Both;
        public string? Rarity { get; set; } = null; // null/empty => any
        public int? Tier { get; set; } = null; // null => any
        public WeaponType? WeaponType { get; set; } = null; // null => any
        public ItemGenerationArmorSlot ArmorSlot { get; set; } = ItemGenerationArmorSlot.Any;

        /// <summary>Hero level: when <see cref="Rarity"/> is Any, passed to <see cref="LootRarityProcessor.RollRarity"/> (gates Epic+ like real drops). When <see cref="Tier"/> is null (Any), combined with <see cref="DungeonLevel"/> for <see cref="LootTierCalculator"/> tier rolls.</summary>
        public int PlayerLevel { get; set; } = 1;
        public int DungeonLevel { get; set; } = 1;
        public int Seed { get; set; } = 12345;

        /// <summary>Magic find (0–100) for <see cref="LootBonusApplier.ApplyBonuses"/> only (affix tiers + optional extras); base rarity roll does not use MF.</summary>
        public double MagicFind { get; set; } = 0;

        /// <summary>
        /// Optional fixed rarity percent chances used when <see cref="Rarity"/> is Any.
        /// If set, the lab rolls rarity from these values (normalized) and ignores level gating.
        /// Keys are rarity names (Common/Uncommon/Rare/Epic/Legendary/Mythic).
        /// </summary>
        public Dictionary<string, double>? FixedRarityChancesPercent { get; set; } = null;
    }

    public sealed class ItemGeneratedRow
    {
        public required Item Item { get; init; }
        public required int Index { get; init; }
        public required string SortKey { get; init; }

        public int Tier => Item.Tier;
        public string Rarity => Item.Rarity?.Trim() ?? "Common";
        public string TypeLabel => Item.Type == ItemType.Weapon
            ? $"Weapon:{Item.WeaponType}"
            : $"Armor:{Item.Type}";
        public int Damage => Item is WeaponItem w ? w.GetTotalDamage() : 0;
        public double Speed => Item is WeaponItem w ? w.GetTotalAttackSpeed() : 0;
        public int Armor => Item is HeadItem h ? h.GetTotalArmor()
            : Item is ChestItem c ? c.GetTotalArmor()
            : Item is FeetItem f ? f.GetTotalArmor()
            : 0;
        public int PrefixCount => Item.Modifications?.Count ?? 0;
        public int SuffixCount => Item.StatBonuses?.Count ?? 0;
        public int ActionBonusCount => Item.ActionBonuses?.Count ?? 0;
    }

    public static class ItemGenerationLabService
    {
        private static readonly string[] RarityOrder =
        {
            "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic"
        };

        public static List<ItemGeneratedRow> GenerateBatch(ItemGenerationSpec spec, int count)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            count = Math.Clamp(count, 1, 1_000_000);

            var cache = LootDataCache.Load();
            var rnd = new Random(spec.Seed);

            bool rollRarityEachItem = string.IsNullOrWhiteSpace(spec.Rarity) ||
                                      spec.Rarity.Equals("Any", StringComparison.OrdinalIgnoreCase);
            RarityData? fixedRarity = rollRarityEachItem
                ? null
                : ResolveExplicitRarity(cache, spec.Rarity);

            var rarityProcessor = new LootRarityProcessor(cache, rnd);
            int heroLevel = Math.Clamp(spec.PlayerLevel, 1, 99);
            var results = new List<ItemGeneratedRow>(count);

            var availableWeaponTiers = GetAvailableWeaponTiers(cache, spec);
            var availableArmorTiers = GetAvailableArmorTiers(cache, spec);

            for (int i = 0; i < count; i++)
            {
                RarityData rarityForItem;
                if (!rollRarityEachItem)
                {
                    rarityForItem = fixedRarity!;
                }
                else if (spec.FixedRarityChancesPercent != null && spec.FixedRarityChancesPercent.Count > 0)
                {
                    rarityForItem = RollRarityFromFixedChances(cache, rnd, spec.FixedRarityChancesPercent);
                }
                else
                {
                    rarityForItem = rarityProcessor.RollRarity(magicFind: 0.0, playerLevel: heroLevel);
                }

                var item = GenerateSingleForced(cache, rnd, spec, rarityForItem, availableWeaponTiers, availableArmorTiers, lockRarityAfterBonuses: !rollRarityEachItem);
                if (item == null)
                    continue;

                results.Add(new ItemGeneratedRow
                {
                    Item = item,
                    Index = i + 1,
                    SortKey = BuildSortKey(item)
                });
            }

            return results;
        }

        private static RarityData RollRarityFromFixedChances(LootDataCache cache, Random rnd, Dictionary<string, double> chancesPercent)
        {
            // Always resolve against cache entries when possible so bonus counts match that rarity’s row.
            // Normalize inputs in case they do not sum to 100.
            var normalized = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in chancesPercent)
            {
                string k = (kv.Key ?? "").Trim();
                if (k.Length == 0) continue;
                normalized[k] = kv.Value;
            }

            static double read(Dictionary<string, double> m, string key)
                => m.TryGetValue(key, out var v) ? Math.Max(0.0, v) : 0.0;

            double c = read(normalized, "Common");
            double u = read(normalized, "Uncommon");
            double r = read(normalized, "Rare");
            double e = read(normalized, "Epic");
            double l = read(normalized, "Legendary");
            double m = read(normalized, "Mythic");
            double sum = c + u + r + e + l + m;
            if (sum <= 0.000001)
            {
                // Fallback: behave like Common
                return ResolveExplicitRarity(cache, "Common");
            }

            double roll = rnd.NextDouble() * sum;
            double acc = 0.0;
            acc += c; if (roll < acc) return ResolveExplicitRarity(cache, "Common");
            acc += u; if (roll < acc) return ResolveExplicitRarity(cache, "Uncommon");
            acc += r; if (roll < acc) return ResolveExplicitRarity(cache, "Rare");
            acc += e; if (roll < acc) return ResolveExplicitRarity(cache, "Epic");
            acc += l; if (roll < acc) return ResolveExplicitRarity(cache, "Legendary");
            return ResolveExplicitRarity(cache, "Mythic");
        }

        public static List<ItemGeneratedRow> SortBestToWorst(IReadOnlyList<ItemGeneratedRow> rows)
        {
            if (rows == null) return new List<ItemGeneratedRow>();
            return rows
                .OrderByDescending(r => IndexOfRarity(r.Rarity))
                .ThenByDescending(r => r.Tier)
                .ThenByDescending(r => r.Item is WeaponItem ? r.Damage : r.Armor)
                .ThenByDescending(r => r.Item is WeaponItem ? r.Speed : 0)
                .ThenByDescending(r => r.Item.StatBonuses?.Sum(s => s.Value) ?? 0)
                .ThenByDescending(r => (r.PrefixCount + r.SuffixCount + r.ActionBonusCount))
                .ThenBy(r => r.Item.Name ?? "", StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string BuildSortKey(Item item)
        {
            // Stable tiebreak to keep list deterministic.
            int r = IndexOfRarity(item.Rarity);
            int tier = item.Tier;
            int primary = item is WeaponItem w ? w.GetTotalDamage() : item is HeadItem h ? h.GetTotalArmor() : item is ChestItem c ? c.GetTotalArmor() : item is FeetItem f ? f.GetTotalArmor() : 0;
            double speed = item is WeaponItem ww ? ww.GetTotalAttackSpeed() : 0;
            int affix = (item.Modifications?.Count ?? 0) + (item.StatBonuses?.Count ?? 0) + (item.ActionBonuses?.Count ?? 0);
            return $"{r:D2}-{tier:D2}-{primary:D5}-{speed:0000.000}-{affix:D2}-{item.Name}";
        }

        /// <summary>
        /// Fixed <see cref="ItemGenerationSpec.Tier"/> uses that value; otherwise rolls like <see cref="LootGenerator"/>
        /// (<see cref="LootTierCalculator.CalculateLootLevel"/> + <see cref="LootTierCalculator.RollTier"/>).
        /// </summary>
        private static int ResolveLabItemTier(LootDataCache cache, Random rnd, ItemGenerationSpec spec, bool wantWeapon, IReadOnlyCollection<int> availableWeaponTiers, IReadOnlyCollection<int> availableArmorTiers)
        {
            if (spec.Tier is int fixedTier)
                return Math.Clamp(fixedTier, 1, 5);

            var tierCalc = new LootTierCalculator(cache, rnd);
            int player = Math.Clamp(spec.PlayerLevel, 1, 99);
            int dungeon = Math.Max(1, spec.DungeonLevel);
            int lootLevel = tierCalc.CalculateLootLevel(player, dungeon);

            var dist = tierCalc.GetTierDistribution(lootLevel);
            if (dist == null)
                return 1;

            var allowed = wantWeapon ? availableWeaponTiers : availableArmorTiers;
            return RollTierFiltered(dist, allowed, rnd);
        }

        private static RarityData ResolveExplicitRarity(LootDataCache cache, string? rarityName)
        {
            if (cache.RarityData == null || cache.RarityData.Count == 0)
                return new RarityData { Name = "Common", Weight = 1, StatBonuses = 0, ActionBonuses = 0, Modifications = 0 };

            string name = (rarityName ?? "").Trim();
            var found = cache.RarityData.FirstOrDefault(r =>
                string.Equals((r.Name ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase));
            if (found != null)
                return found;

            // Be permissive: keep the requested name even if the table is missing it.
            // This prevents fixed-chance overrides from collapsing to Common due to whitespace/mismatched rows.
            return new RarityData { Name = name.Length == 0 ? "Common" : name, Weight = 1, StatBonuses = 0, ActionBonuses = 0, Modifications = 0 };
        }

        private static Item? GenerateSingleForced(LootDataCache cache, Random rnd, ItemGenerationSpec spec, RarityData forcedRarity, IReadOnlyCollection<int> availableWeaponTiers, IReadOnlyCollection<int> availableArmorTiers, bool lockRarityAfterBonuses)
        {
            bool wantWeapon;
            if (spec.ItemType == ItemGenerationItemType.Weapons) wantWeapon = true;
            else if (spec.ItemType == ItemGenerationItemType.Armor) wantWeapon = false;
            else wantWeapon = rnd.NextDouble() < 0.5;

            int tier = ResolveLabItemTier(cache, rnd, spec, wantWeapon, availableWeaponTiers, availableArmorTiers);

            Item? item;
            if (wantWeapon)
            {
                var rows = cache.WeaponData.Where(w => w.Tier == tier).ToList();
                if (spec.WeaponType != null)
                    rows = rows.Where(w => string.Equals(w.Type, spec.WeaponType.Value.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();

                if (rows.Count == 0)
                    return null;

                var weaponData = rows[rnd.Next(rows.Count)];
                item = ItemGenerator.GenerateWeaponItem(weaponData);
            }
            else
            {
                var rows = cache.ArmorData.Where(a => a.Tier == tier).ToList();
                if (spec.ArmorSlot != ItemGenerationArmorSlot.Any)
                {
                    string slot = spec.ArmorSlot.ToString().ToLowerInvariant();
                    rows = rows.Where(a => string.Equals(a.Slot, slot, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (rows.Count == 0)
                    return null;

                var armorData = rows[rnd.Next(rows.Count)];
                item = ItemGenerator.GenerateArmorItem(armorData);
            }

            // Level + tier are needed for scaling and effect rolls
            item.Tier = tier;
            item.Level = Math.Max(1, spec.DungeonLevel);

            var tuning = GameConfiguration.Instance;
            if (tuning.ItemScaling != null)
                ItemGenerator.ApplyScaling(item, tuning.ItemScaling, item.Level);

            item.Rarity = forcedRarity.Name?.Trim() ?? "Common";

            var context = LootContext.Create(player: null, dungeonTheme: null, enemyArchetype: null);
            if (item is WeaponItem wItem)
                context.WeaponType = wItem.WeaponType.ToString();

            var applier = new LootBonusApplier(cache, rnd);
            int labMf = (int)Math.Clamp(spec.MagicFind, 0, 100);
            applier.ApplyBonuses(item, forcedRarity, context, labMf);

            // When a rarity was explicitly chosen, keep that tier/rarity even if bonus rolls imply higher ranks.
            if (lockRarityAfterBonuses)
                item.Rarity = forcedRarity.Name?.Trim() ?? item.Rarity;
            item.Tier = tier;

            // Name already finalized inside LootBonusApplier.ApplyBonuses; do not re-run name assembly here
            // or prefixes would be prepended again (GetBaseItemName would see an already-prefixed name).
            return item;
        }

        private static IReadOnlyCollection<int> GetAvailableWeaponTiers(LootDataCache cache, ItemGenerationSpec spec)
        {
            var rows = cache.WeaponData.AsEnumerable();
            if (spec.WeaponType != null)
                rows = rows.Where(w => string.Equals(w.Type, spec.WeaponType.Value.ToString(), StringComparison.OrdinalIgnoreCase));
            return rows.Select(w => w.Tier).Distinct().Where(t => t >= 1 && t <= 5).ToList();
        }

        private static IReadOnlyCollection<int> GetAvailableArmorTiers(LootDataCache cache, ItemGenerationSpec spec)
        {
            var rows = cache.ArmorData.AsEnumerable();
            if (spec.ArmorSlot != ItemGenerationArmorSlot.Any)
            {
                string slot = spec.ArmorSlot.ToString().ToLowerInvariant();
                rows = rows.Where(a => string.Equals(a.Slot, slot, StringComparison.OrdinalIgnoreCase));
            }
            return rows.Select(a => a.Tier).Distinct().Where(t => t >= 1 && t <= 5).ToList();
        }

        private static int RollTierFiltered(TierDistribution dist, IReadOnlyCollection<int> allowedTiers, Random rnd)
        {
            // Dist values are already percents. If filters remove tiers entirely (e.g., no tier-5 catalog rows),
            // renormalize across what’s available so generation still tracks the configured curve.
            if (allowedTiers == null || allowedTiers.Count == 0)
                return 1;

            double w1 = allowedTiers.Contains(1) ? dist.Tier1 : 0.0;
            double w2 = allowedTiers.Contains(2) ? dist.Tier2 : 0.0;
            double w3 = allowedTiers.Contains(3) ? dist.Tier3 : 0.0;
            double w4 = allowedTiers.Contains(4) ? dist.Tier4 : 0.0;
            double w5 = allowedTiers.Contains(5) ? dist.Tier5 : 0.0;
            double sum = w1 + w2 + w3 + w4 + w5;
            if (sum <= 0.000001)
                return allowedTiers.Min();

            double roll = rnd.NextDouble() * sum;
            double c = 0.0;
            c += w1; if (roll < c) return 1;
            c += w2; if (roll < c) return 2;
            c += w3; if (roll < c) return 3;
            c += w4; if (roll < c) return 4;
            return 5;
        }

        private static int IndexOfRarity(string? rarity)
        {
            if (string.IsNullOrWhiteSpace(rarity))
                return 0;
            int idx = Array.FindIndex(RarityOrder, r => string.Equals(r, rarity.Trim(), StringComparison.OrdinalIgnoreCase));
            return idx < 0 ? 0 : idx;
        }
    }
}

