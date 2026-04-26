using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Calculates loot tier based on player and dungeon levels
    /// Handles tier distribution lookups and tier rolling
    /// </summary>
    public class LootTierCalculator
    {
        private readonly LootDataCache _dataCache;
        private readonly Random _random;

        public LootTierCalculator(LootDataCache dataCache, Random random)
        {
            _dataCache = dataCache;
            _random = random;
        }

        /// <summary>
        /// Calculates the adjusted loot level based on player level relative to dungeon level
        /// If character is higher level than dungeon, they get lower-tier loot
        /// If character is lower level than dungeon, they get higher-tier loot
        /// </summary>
        public int CalculateLootLevel(int playerLevel, int dungeonLevel)
        {
            // Determine loot level based on character level relative to dungeon level
            int lootLevel = dungeonLevel - (playerLevel - dungeonLevel);
            
            // Clamp loot level to valid range
            return ClampLootLevel(lootLevel);
        }

        /// <summary>
        /// Clamps loot level to valid range (1-100)
        /// </summary>
        private int ClampLootLevel(int lootLevel)
        {
            if (lootLevel <= 0)
            {
                return 1; // Minimum level 1 loot
            }
            if (lootLevel >= 100)
            {
                return 100; // Cap at level 100
            }
            return lootLevel;
        }

        /// <summary>
        /// Rolls for a tier based on loot level using tier distribution tables
        /// </summary>
        public int RollTier(int lootLevel)
        {
            // Clamp loot level to valid range
            lootLevel = ClampLootLevel(lootLevel);
            
            var distribution = GetTierDistribution(lootLevel);
            if (distribution == null)
            {
                return 1; // Default to tier 1 if no distribution found
            }

            double roll = _random.NextDouble() * 100;
            
            if (roll < distribution.Tier1)
            {
                return 1;
            }
            if (roll < distribution.Tier1 + distribution.Tier2)
            {
                return 2;
            }
            if (roll < distribution.Tier1 + distribution.Tier2 + distribution.Tier3)
            {
                return 3;
            }
            if (roll < distribution.Tier1 + distribution.Tier2 + distribution.Tier3 + distribution.Tier4)
            {
                return 4;
            }
            return 5;
        }

        /// <summary>
        /// Gets the tier distribution for a specific loot level
        /// </summary>
        public TierDistribution? GetTierDistribution(int lootLevel)
        {
            return _dataCache.TierDistributions.FirstOrDefault(d => d.Level == lootLevel);
        }

        /// <summary>
        /// Loot level from <see cref="CalculateLootLevel"/> and per-tier percentages from
        /// <c>TierDistribution.json</c> for that row — same inputs as <see cref="RollTier"/> (first roll only).
        /// </summary>
        /// <param name="cache">Loaded loot cache (tier table).</param>
        /// <param name="playerLevel">Hero / player level (clamped 1–99 for preview).</param>
        /// <param name="dungeonLevel">Dungeon content level (minimum 1).</param>
        /// <returns>Loot level and five percentages (Tier1..Tier5), or <c>Rows</c> null if no table row.</returns>
        public static (int LootLevel, IReadOnlyList<(int Tier, double ProbabilityPercent)>? Rows) GetTierRollPreview(
            LootDataCache cache,
            int playerLevel,
            int dungeonLevel)
        {
            var calc = new LootTierCalculator(cache, new Random(0));
            playerLevel = Math.Clamp(playerLevel, 1, 99);
            dungeonLevel = Math.Max(1, dungeonLevel);
            int lootLevel = calc.CalculateLootLevel(playerLevel, dungeonLevel);
            var dist = calc.GetTierDistribution(lootLevel);
            if (dist == null)
                return (lootLevel, null);

            IReadOnlyList<(int Tier, double ProbabilityPercent)> rows = new (int, double)[]
            {
                (1, dist.Tier1),
                (2, dist.Tier2),
                (3, dist.Tier3),
                (4, dist.Tier4),
                (5, dist.Tier5),
            };
            return (lootLevel, rows);
        }
    }
}

