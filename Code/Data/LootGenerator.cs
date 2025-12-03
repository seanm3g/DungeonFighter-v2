using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame
{
    /// <summary>
    /// Centralized loot generation system
    /// 
    /// This is now a facade coordinating specialized managers:
    /// - LootDataCache: Centralized data loading and caching
    /// - LootTierCalculator: Tier determination logic
    /// - LootItemSelector: Item selection logic
    /// - LootRarityProcessor: Rarity determination and scaling
    /// - LootBonusApplier: Bonus and modification application
    /// </summary>
    public class LootGenerator
    {
        private static Random _random = new Random();
        
        // Shared data cache
        private static LootDataCache? _dataCache;
        
        // Specialized managers
        private static LootTierCalculator? _tierCalculator;
        private static LootItemSelector? _itemSelector;
        private static LootRarityProcessor? _rarityProcessor;
        private static LootBonusApplier? _bonusApplier;

        /// <summary>
        /// Gets or creates the shared data cache
        /// </summary>
        private static LootDataCache DataCache
        {
            get
            {
                if (_dataCache == null)
                {
                    _dataCache = LootDataCache.Load();
                }
                return _dataCache;
            }
        }

        /// <summary>
        /// Gets or creates the tier calculator
        /// </summary>
        private static LootTierCalculator TierCalculator
        {
            get
            {
                if (_tierCalculator == null)
                {
                    _tierCalculator = new LootTierCalculator(DataCache, _random);
                }
                return _tierCalculator;
            }
        }

        /// <summary>
        /// Gets or creates the item selector
        /// </summary>
        private static LootItemSelector ItemSelector
        {
            get
            {
                if (_itemSelector == null)
                {
                    _itemSelector = new LootItemSelector(DataCache, _random);
                }
                return _itemSelector;
            }
        }

        /// <summary>
        /// Gets or creates the rarity processor
        /// </summary>
        private static LootRarityProcessor RarityProcessor
        {
            get
            {
                if (_rarityProcessor == null)
                {
                    _rarityProcessor = new LootRarityProcessor(DataCache, _random);
                }
                return _rarityProcessor;
            }
        }

        /// <summary>
        /// Gets or creates the bonus applier
        /// </summary>
        private static LootBonusApplier BonusApplier
        {
            get
            {
                if (_bonusApplier == null)
                {
                    _bonusApplier = new LootBonusApplier(DataCache, _random);
                }
                return _bonusApplier;
            }
        }

        /// <summary>
        /// Initializes the loot generation system by loading all data
        /// </summary>
        public static void Initialize()
        {
            // Reset cache and managers to force reload
            _dataCache = null;
            _tierCalculator = null;
            _itemSelector = null;
            _rarityProcessor = null;
            _bonusApplier = null;
            
            // Load data (will be created on first access)
            _ = DataCache;
        }

        /// <summary>
        /// Generates a loot item based on player level, dungeon level, and other factors
        /// </summary>
        public static Item? GenerateLoot(int playerLevel, int dungeonLevel, Character? player = null, bool guaranteedLoot = false)
        {
            var tuning = GameConfiguration.Instance;
            
            // Calculate loot chance based on tuning config
            double lootChance = CalculateLootChance(playerLevel, player, guaranteedLoot, tuning);
            
            // Roll for loot chance
            double roll = _random.NextDouble();
            if (roll >= lootChance) 
            {
                return null;
            }

            // ROLL 1: Calculate loot level
            int lootLevel = TierCalculator.CalculateLootLevel(playerLevel, dungeonLevel);
            
            // ROLL 2: Determine item type (25% weapon, 75% armor)
            bool isWeapon = ItemSelector.DetermineIsWeapon();
            
            // ROLL 3: Item tier based on loot level
            int tier = TierCalculator.RollTier(lootLevel);
            
            // ROLL 4: Specific item selection
            Item? item = ItemSelector.SelectItem(tier, isWeapon);
            if (item == null) 
            {
                return null;
            }

            // ROLL 5: Apply scaling formulas to base stats
            ApplyItemScaling(item, tuning);

            // ROLL 6: Rarity (determines number of bonuses)
            var rarity = RarityProcessor.RollRarity(0.0, playerLevel);
            item.Rarity = rarity.Name?.Trim() ?? "Common";
            RarityProcessor.ApplyRarityScaling(item, rarity);

            // ROLL 7: Bonus selection
            BonusApplier.ApplyBonuses(item, rarity);

            return item;
        }

        /// <summary>
        /// Calculates the loot drop chance based on player level, magic find, and guaranteed flag
        /// </summary>
        private static double CalculateLootChance(int playerLevel, Character? player, bool guaranteedLoot, GameConfiguration tuning)
        {
            if (guaranteedLoot)
            {
                // Use configured drop chance for guaranteed loot (dungeon completion)
                return tuning.LootSystem.GuaranteedLootChance;
            }
            else
            {
                // Calculate normal loot chance
                double lootChance = tuning.LootSystem.BaseDropChance + (playerLevel * tuning.LootSystem.DropChancePerLevel);
                
                // Apply magic find modifier to loot chance
                double magicFind = player?.GetMagicFind() ?? 0.0;
                lootChance += magicFind * tuning.LootSystem.MagicFindEffectiveness;
                
                return Math.Min(lootChance, tuning.LootSystem.MaxDropChance);
            }
        }

        /// <summary>
        /// Applies scaling formulas to base stats based on item type and tier
        /// </summary>
        private static void ApplyItemScaling(Item item, GameConfiguration tuning)
        {
            if (item is WeaponItem weapon)
            {
                // Apply bonus damage and attack speed based on tuning config
                var equipmentScaling = tuning.EquipmentScaling;
                if (equipmentScaling != null)
                {
                    // Use new EquipmentScaling configuration
                    weapon.BonusDamage = (int)(weapon.Tier * equipmentScaling.WeaponDamagePerTier);
                    weapon.BonusAttackSpeed = (int)(weapon.Tier * equipmentScaling.SpeedBonusPerTier);
                }
                else
                {
                    // Fallback to simple system
                    weapon.BonusDamage = weapon.Tier <= 1 ? 1 : Dice.Roll(1, Math.Max(2, weapon.Tier));
                    weapon.BonusAttackSpeed = (int)(weapon.Tier * 0.1); // Simple fallback
                }
            }
            // Note: Armor scaling is currently handled by base values (no additional scaling needed)
        }

    }

    // Data classes for JSON deserialization
    public class TierDistribution
    {
        public int Level { get; set; }
        public double Tier1 { get; set; }
        public double Tier2 { get; set; }
        public double Tier3 { get; set; }
        public double Tier4 { get; set; }
        public double Tier5 { get; set; }
    }

    public class ArmorData
    {
        [JsonPropertyName("slot")]
        public string Slot { get; set; } = "";
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("armor")]
        public int Armor { get; set; }
        
        [JsonPropertyName("tier")]
        public int Tier { get; set; }
    }

    public class WeaponData
    {
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";
        public int BaseDamage { get; set; }
        public double AttackSpeed { get; set; }
        public int Tier { get; set; }
    }

    public class RarityData
    {
        public string Name { get; set; } = "";
        public double Weight { get; set; }
        public int StatBonuses { get; set; }
        public int ActionBonuses { get; set; }
        public int Modifications { get; set; }
    }
}
