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
        /// Filters out rarities that require a higher player level
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

            // Filter rarities based on player level - only include rarities the player can get
            var availableRarities = _dataCache.RarityData.Where(r => IsRarityAvailableAtLevel(r.Name, playerLevel)).ToList();
            
            // If no rarities are available (shouldn't happen, but safety check), fall back to Common
            if (availableRarities.Count == 0)
            {
                availableRarities = _dataCache.RarityData.Where(r => r.Name.Equals("Common", StringComparison.OrdinalIgnoreCase)).ToList();
                if (availableRarities.Count == 0)
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
            }

            // Use base weights from RarityTable.json, but only for available rarities
            // This ensures the rarity distribution matches what's configured, but respects level restrictions
            double totalWeight = availableRarities.Sum(r => r.Weight);
            double roll = _random.NextDouble() * totalWeight;
            double cumulative = 0;

            RarityData initialRarity = availableRarities.First();
            foreach (var rarity in availableRarities)
            {
                cumulative += rarity.Weight;
                if (roll < cumulative)
                {
                    initialRarity = rarity;
                    break;
                }
            }

            // Apply upgrade system (upgrades also respect level restrictions)
            return ApplyRarityUpgrades(initialRarity, magicFind, playerLevel);
        }

        /// <summary>
        /// Applies cascading rarity upgrades after initial rarity roll
        /// Items can upgrade to the next rarity tier with exponentially decreasing probability
        /// Upgrades respect level restrictions - cannot upgrade to rarities unavailable at player level
        /// </summary>
        private RarityData ApplyRarityUpgrades(RarityData initialRarity, double magicFind = 0.0, int playerLevel = 1)
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

                // Check if the upgraded rarity is available at player level
                if (!IsRarityAvailableAtLevel(nextRarity.Name, playerLevel))
                    break; // Cannot upgrade to a rarity that requires higher level

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
        /// Checks if a rarity is available at the given player level
        /// Uses minLevel from config for Epic, Legendary, Mythic, and Transcendent
        /// Common, Uncommon, and Rare are always available
        /// </summary>
        private bool IsRarityAvailableAtLevel(string rarityName, int playerLevel)
        {
            var config = GameConfiguration.Instance;
            
            // Common, Uncommon, and Rare are always available
            if (rarityName.Equals("Common", StringComparison.OrdinalIgnoreCase) ||
                rarityName.Equals("Uncommon", StringComparison.OrdinalIgnoreCase) ||
                rarityName.Equals("Rare", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check Epic minimum level
            if (rarityName.Equals("Epic", StringComparison.OrdinalIgnoreCase))
            {
                int minLevel = config.RarityScaling?.LevelBasedRarityScaling?.Epic?.MinLevel ?? 0;
                return playerLevel >= minLevel;
            }

            // Check Legendary minimum level
            if (rarityName.Equals("Legendary", StringComparison.OrdinalIgnoreCase))
            {
                int minLevel = config.RarityScaling?.LevelBasedRarityScaling?.Legendary?.MinLevel ?? 0;
                return playerLevel >= minLevel;
            }

            // Check Mythic minimum level (hardcoded since not in config structure yet)
            if (rarityName.Equals("Mythic", StringComparison.OrdinalIgnoreCase))
            {
                // Use a reasonable default: level 15
                // Could be made configurable in the future
                return playerLevel >= 15;
            }

            // Check Transcendent minimum level (hardcoded since not in config structure yet)
            if (rarityName.Equals("Transcendent", StringComparison.OrdinalIgnoreCase))
            {
                // Use a reasonable default: level 20
                // Could be made configurable in the future
                return playerLevel >= 20;
            }

            // Unknown rarity - allow it (shouldn't happen, but be permissive)
            return true;
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

