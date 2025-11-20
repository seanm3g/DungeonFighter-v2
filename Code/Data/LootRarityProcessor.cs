using System;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Handles rarity determination and rarity-based scaling
    /// Rolls for rarity and applies rarity scaling to items
    /// </summary>
    public class LootRarityProcessor
    {
        private readonly LootDataCache _dataCache;
        private readonly Random _random;

        public LootRarityProcessor(LootDataCache dataCache, Random random)
        {
            _dataCache = dataCache;
            _random = random;
        }

        /// <summary>
        /// Rolls for a rarity level based on rarity table weights
        /// </summary>
        public RarityData RollRarity(double magicFind = 0.0, int playerLevel = 1)
        {
            // Ensure rarity data is available
            if (_dataCache.RarityData == null || _dataCache.RarityData.Count == 0)
            {
                return new RarityData 
                { 
                    Name = "Common", 
                    Weight = 500, 
                    StatBonuses = 1, 
                    ActionBonuses = 0, 
                    Modifications = 0 
                };
            }

            // Use base weights from RarityTable.json without additional scaling
            // This ensures the rarity distribution matches exactly what's configured
            double totalWeight = _dataCache.RarityData.Sum(r => r.Weight);
            double roll = _random.NextDouble() * totalWeight;
            double cumulative = 0;

            foreach (var rarity in _dataCache.RarityData)
            {
                cumulative += rarity.Weight;
                if (roll < cumulative)
                {
                    return rarity;
                }
            }

            return _dataCache.RarityData.First();
        }

        /// <summary>
        /// Applies rarity scaling to an item
        /// Currently applies simple rarity multiplier (1.0 for now)
        /// </summary>
        public void ApplyRarityScaling(Item item, RarityData rarity)
        {
            // Simple rarity multiplier
            double rarityMultiplier = 1.0; // No scaling for now
            
            if (item is WeaponItem weaponForRarity)
            {
                weaponForRarity.BaseDamage = (int)Math.Round(weaponForRarity.BaseDamage * rarityMultiplier);
            }
            // Note: Armor rarity multipliers are now handled by stat bonuses and modifications
            // to prevent double scaling that was causing integer overflow
        }
    }
}

