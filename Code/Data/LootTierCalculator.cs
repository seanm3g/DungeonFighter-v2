using System;

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
    }
}

