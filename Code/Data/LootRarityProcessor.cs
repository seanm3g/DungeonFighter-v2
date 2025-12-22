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
        /// Applies cascading rarity upgrades after initial roll
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

            RarityData initialRarity = _dataCache.RarityData.First();
            foreach (var rarity in _dataCache.RarityData)
            {
                cumulative += rarity.Weight;
                if (roll < cumulative)
                {
                    initialRarity = rarity;
                    break;
                }
            }

            // Apply upgrade system
            return ApplyRarityUpgrades(initialRarity, magicFind);
        }

        /// <summary>
        /// Applies cascading rarity upgrades after initial rarity roll
        /// Items can upgrade to the next rarity tier with exponentially decreasing probability
        /// </summary>
        private RarityData ApplyRarityUpgrades(RarityData initialRarity, double magicFind = 0.0)
        {
            var config = GameConfiguration.Instance;

            // Check if upgrade system is enabled
            if (!config.LootSystem.RarityUpgrade.Enabled)
                return initialRarity;

            var currentRarity = initialRarity;
            int upgradesMade = 0;
            int maxUpgrades = config.LootSystem.RarityUpgrade.MaxUpgradeTiers;

            // Keep trying to upgrade until we fail or hit the cap
            while (upgradesMade < maxUpgrades)
            {
                // Calculate upgrade chance with exponential decay
                double baseChance = config.LootSystem.RarityUpgrade.BaseUpgradeChance;
                double decay = Math.Pow(config.LootSystem.RarityUpgrade.UpgradeChanceDecayPerTier, upgradesMade);
                double magicFindBonus = magicFind * config.LootSystem.RarityUpgrade.MagicFindBonus;
                double upgradeChance = (baseChance * decay) + magicFindBonus;

                // Roll for upgrade
                if (_random.NextDouble() >= upgradeChance)
                    break; // Upgrade failed

                // Find next rarity tier
                var nextRarity = GetNextRarityTier(currentRarity);
                if (nextRarity == null)
                    break; // Already at max rarity

                currentRarity = nextRarity;
                upgradesMade++;
            }

            return currentRarity;
        }

        /// <summary>
        /// Gets the next rarity tier in progression
        /// </summary>
        private RarityData? GetNextRarityTier(RarityData current)
        {
            // Define rarity order
            var rarityOrder = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic", "Transcendent" };

            int currentIndex = Array.IndexOf(rarityOrder, current.Name);
            if (currentIndex < 0 || currentIndex >= rarityOrder.Length - 1)
                return null; // Not found or already at max

            string nextRarityName = rarityOrder[currentIndex + 1];
            return _dataCache.RarityData.FirstOrDefault(r => r.Name.Equals(nextRarityName, StringComparison.OrdinalIgnoreCase));
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

