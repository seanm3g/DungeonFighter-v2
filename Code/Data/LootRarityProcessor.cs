using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Rarity table helpers for previews (Item Generation lab). The roll itself lives in
    /// <see cref="Data.ItemGeneration.ItemRoller"/>: weighted pick among rarities unlocked at the item level
    /// (<see cref="RarityData.MinLevel"/>), then the rarity roll-up.
    /// </summary>
    public static class LootRarityProcessor
    {
        /// <summary>True when <paramref name="rarityName"/> can drop at <paramref name="itemLevel"/> (<c>RarityTable.json</c> MinLevel).</summary>
        public static bool IsRarityUnlockedAtLevel(LootDataCache cache, string rarityName, int itemLevel)
        {
            var row = cache.RarityData?.FirstOrDefault(r => string.Equals(r.Name, rarityName?.Trim(), StringComparison.OrdinalIgnoreCase));
            return row == null || row.MinLevel <= itemLevel;
        }

        /// <summary>Base roll weight: the balance patch's rarity weights when set, else <c>RarityTable.json</c>.</summary>
        public static double RollWeight(RarityData rarity) =>
            GameConfiguration.Instance?.ItemAffixByRarity?.ResolveRarityWeight(rarity) ?? rarity.Weight;

        /// <summary>Rarity odds at <paramref name="itemLevel"/> before the rarity roll-up.</summary>
        public static IReadOnlyList<(string Name, double Weight, double ProbabilityPercent)> GetBaseRollDistribution(
            LootDataCache cache, int itemLevel)
        {
            if (cache.RarityData == null || cache.RarityData.Count == 0)
                return Array.Empty<(string, double, double)>();

            var available = cache.RarityData.Where(r => r.MinLevel <= itemLevel && RollWeight(r) > 0).ToList();
            double total = available.Sum(RollWeight);
            if (total <= 0)
                return Array.Empty<(string, double, double)>();

            return available
                .Select(r => (r.Name, RollWeight(r), 100.0 * RollWeight(r) / total))
                .ToList();
        }
    }
}
