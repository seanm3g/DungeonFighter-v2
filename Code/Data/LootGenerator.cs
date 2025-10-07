using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame
{
    public class LootGenerator
    {
        private static Random _random = new Random();
        private static List<TierDistribution>? _tierDistributions;
        private static List<ArmorData>? _armorData;
        private static List<WeaponData>? _weaponData;
        private static List<StatBonus>? _statBonuses;
        private static List<ActionBonus>? _actionBonuses;
        private static List<Modification>? _modifications;
        private static List<RarityData>? _rarityData;

        public static void Initialize()
        {
            LoadTierDistributions();
            LoadArmorData();
            LoadWeaponData();
            LoadStatBonuses();
            LoadActionBonuses();
            LoadModifications();
            LoadRarityData();
        }

        public static Item? GenerateLoot(int playerLevel, int dungeonLevel, Character? player = null, bool guaranteedLoot = false)
        {
            if (_tierDistributions == null || _armorData == null || _weaponData == null || 
                _statBonuses == null || _actionBonuses == null || _modifications == null || _rarityData == null)
            {
                Initialize();
            }

            var tuning = GameConfiguration.Instance;
            
            // Calculate loot chance based on tuning config
            double lootChance;
            if (guaranteedLoot)
            {
                // Use 100% drop chance for guaranteed loot (dungeon completion)
                lootChance = tuning.LootSystem.GuaranteedLootChance;
            }
            else
            {
                // Calculate normal loot chance
                lootChance = tuning.LootSystem.BaseDropChance + (playerLevel * tuning.LootSystem.DropChancePerLevel);
                
                // Apply magic find modifier to loot chance
                double magicFind = player?.GetMagicFind() ?? 0.0;
                lootChance += magicFind * tuning.LootSystem.MagicFindEffectiveness;
                
                lootChance = Math.Min(lootChance, tuning.LootSystem.MaxDropChance);
            }
            
            // Roll for loot chance
            double roll = _random.NextDouble();
            if (roll >= lootChance) 
            {
                return null;
            }

            // ROLL 1: Determine loot level based on character level relative to dungeon level
            // If character is higher level than dungeon, they get lower-tier loot
            // If character is lower level than dungeon, they get higher-tier loot
            int lootLevel = dungeonLevel - (playerLevel - dungeonLevel);
            
            // Special rules: Clamp loot level to valid range
            if (lootLevel <= 0) 
            {
                lootLevel = 1; // Minimum level 1 loot
            }
            if (lootLevel >= 100) 
            {
                lootLevel = 100; // Cap at level 100
            }

            // ROLL 2: Item type (25% weapon, 75% armor)
            bool isWeapon = _random.NextDouble() < 0.25;
            
            // ROLL 3: Weapon class (ignored for now as per user request)
            
            // ROLL 4: Item tier based on loot level
            int tier = RollTier(lootLevel);
            
            // ROLL 5: Specific item selection
            Item? item = isWeapon ? RollWeapon(tier) : RollArmor(tier);
            if (item == null) 
            {
                return null;
            }

            // ROLL 5b,c: Apply scaling formulas to base stats
            if (item is WeaponItem weapon)
            {
                // Simple damage scaling
                weapon.BaseDamage = weapon.BaseDamage;
                
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
            else if (item is HeadItem headArmor)
            {
                // Simple armor scaling
                headArmor.Armor = headArmor.Armor;
            }
            else if (item is ChestItem chestArmor)
            {
                // Simple armor scaling
                chestArmor.Armor = chestArmor.Armor;
            }
            else if (item is FeetItem feetArmor)
            {
                // Simple armor scaling
                feetArmor.Armor = feetArmor.Armor;
            }

            // ROLL 6: Rarity (determines number of bonuses)
            var rarity = RollRarity(0.0, playerLevel);
            item.Rarity = rarity.Name;
            
            // Simple rarity multiplier
            double rarityMultiplier = 1.0; // No scaling for now
            if (item is WeaponItem weaponForRarity)
            {
                weaponForRarity.BaseDamage = (int)Math.Round(weaponForRarity.BaseDamage * rarityMultiplier);
            }
            // Note: Armor rarity multipliers are now handled by stat bonuses and modifications
            // to prevent double scaling that was causing integer overflow

            // ROLL 7: Bonus selection
            ApplyBonuses(item, rarity);

            return item;
        }

        private static int RollTier(int lootLevel)
        {
            // Clamp loot level to valid range
            lootLevel = Math.Max(1, Math.Min(100, lootLevel));
            
            var distribution = _tierDistributions!.FirstOrDefault(d => d.Level == lootLevel);
            if (distribution == null) 
            {
                return 1;
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

        private static Item? RollWeapon(int tier)
        {
            var weaponsInTier = _weaponData!.Where(w => w.Tier == tier).ToList();
            
            if (!weaponsInTier.Any()) 
            {
                return null;
            }

            var selectedWeapon = weaponsInTier[_random.Next(weaponsInTier.Count)];
            
            // DISABLED: "highest item jumps to next tier up" rule - this was breaking tier distribution
            // if (IsHighestItemInTier(selectedWeapon, weaponsInTier) && tier < 5)
            // {
            //     // Roll again on next tier
            //     var nextTierWeapons = _weaponData?.Where(w => w.Tier == tier + 1).ToList() ?? new List<WeaponData>();
            //     if (nextTierWeapons.Any())
            //     {
            //         selectedWeapon = nextTierWeapons[_random.Next(nextTierWeapons.Count)];
            //     }
            // }

            return ItemGenerator.GenerateWeaponItem(selectedWeapon);
        }

        private static Item? RollArmor(int tier)
        {
            var armorInTier = _armorData!.Where(a => a.Tier == tier).ToList();
            
            if (!armorInTier.Any()) 
            {
                return null;
            }

            var selectedArmor = armorInTier[_random.Next(armorInTier.Count)];
            
            // DISABLED: "highest item jumps to next tier up" rule - this was breaking tier distribution
            // if (IsHighestItemInTier(selectedArmor, armorInTier) && tier < 5)
            // {
            //     // Roll again on next tier
            //     var nextTierArmor = _armorData?.Where(a => a.Tier == tier + 1).ToList() ?? new List<ArmorData>();
            //     if (nextTierArmor.Any())
            //     {
            //         selectedArmor = nextTierArmor[_random.Next(nextTierArmor.Count)];
            //     }
            // }

            Item? item = ItemGenerator.GenerateArmorItem(selectedArmor);

            // Assign random action for all armor types to provide variety
            if (item != null)
            {
                item.GearAction = GetRandomArmorAction();
            }

            return item;
        }

        private static bool IsHighestItemInTier(WeaponData item, List<WeaponData> itemsInTier)
        {
            return item.BaseDamage == itemsInTier.Max(w => w.BaseDamage);
        }

        private static bool IsHighestItemInTier(ArmorData item, List<ArmorData> itemsInTier)
        {
            return item.Armor == itemsInTier.Max(a => a.Armor);
        }

        private static string? GetRandomArmorAction()
        {
            // Get ALL combo actions (not just armor-tagged ones) for maximum variety
            var allActions = ActionLoader.GetAllActions();
            var availableActions = allActions
                .Where(action => action.IsComboAction && 
                               !action.Tags.Contains("environment") &&
                               !action.Tags.Contains("enemy") &&
                               !action.Tags.Contains("unique"))
                .Select(action => action.Name)
                .ToList();

            // Return completely random action if any are available
            if (availableActions.Count > 0)
            {
                return availableActions[_random.Next(availableActions.Count)];
            }
            
            return null; // No action if none available
        }

        private static RarityData RollRarity(double magicFind = 0.0, int playerLevel = 1)
        {
            // Ensure _rarityData is loaded
            if (_rarityData == null)
            {
                Initialize();
            }

            // Ensure _rarityData is still not null after initialization
            if (_rarityData == null || _rarityData.Count == 0)
            {
                return new RarityData { Name = "Common", Weight = 500, StatBonuses = 1, ActionBonuses = 0, Modifications = 0 };
            }

            // Use base weights from RarityTable.json without additional scaling
            // This ensures the rarity distribution matches exactly what's configured
            double totalWeight = _rarityData.Sum(r => r.Weight);
            double roll = _random.NextDouble() * totalWeight;
            double cumulative = 0;

            foreach (var rarity in _rarityData)
            {
                cumulative += rarity.Weight;
                if (roll < cumulative)
                {
                    return rarity;
                }
            }

            return _rarityData.First();
        }
        
        private static double CalculateScaledRarityWeight(RarityData rarity, double magicFind, int playerLevel)
        {
            // Base weight from JSON configuration
            double baseWeight = rarity.Weight;
            
            // Apply Magic Find scaling from configuration
            double magicFindMultiplier = CalculateMagicFindMultiplier(rarity.Name, magicFind);
            
            // Apply Level-based scaling from configuration
            double levelMultiplier = CalculateLevelMultiplier(rarity.Name, playerLevel);
            
            // Combine both multipliers
            double finalWeight = baseWeight * magicFindMultiplier * levelMultiplier;
            
            // Ensure weights don't go negative
            return Math.Max(0.1, finalWeight);
        }
        
        private static double CalculateMagicFindMultiplier(string rarityName, double magicFind)
        {
            var config = GameConfiguration.Instance.RarityScaling?.MagicFindScaling;
            if (config == null) return 1.0;
            
            double perPointMultiplier = rarityName.ToLower() switch
            {
                "common" => config.Common.PerPointMultiplier,
                "uncommon" => config.Uncommon.PerPointMultiplier,
                "rare" => config.Rare.PerPointMultiplier,
                "epic" => config.Epic.PerPointMultiplier,
                "legendary" => config.Legendary.PerPointMultiplier,
                _ => 0.0
            };
            
            return 1.0 + (magicFind * perPointMultiplier);
        }
        
        private static double CalculateLevelMultiplier(string rarityName, int playerLevel)
        {
            var config = GameConfiguration.Instance.RarityScaling?.LevelBasedRarityScaling;
            if (config == null) return 1.0;
            
            double levelFactor = Math.Max(0.1, playerLevel / 100.0); // 0.1 to 1.0+ scaling
            
            return rarityName.ToLower() switch
            {
                "common" => config.Common.BaseMultiplier - (levelFactor * config.Common.LevelReduction),
                "uncommon" => config.Uncommon.BaseMultiplier + (levelFactor * config.Uncommon.LevelBonus),
                "rare" => config.Rare.BaseMultiplier + (levelFactor * config.Rare.LevelBonus),
                "epic" => CalculateEpicLevelMultiplier(config.Epic, playerLevel, levelFactor),
                "legendary" => CalculateLegendaryLevelMultiplier(config.Legendary, playerLevel, levelFactor),
                _ => 1.0
            };
        }
        
        private static double CalculateEpicLevelMultiplier(EpicRarityScalingConfig config, int playerLevel, double levelFactor)
        {
            if (playerLevel < config.MinLevel)
                return config.EarlyMultiplier;
            
            return config.BaseMultiplier + (levelFactor * config.LevelBonus);
        }
        
        private static double CalculateLegendaryLevelMultiplier(LegendaryRarityScalingConfig config, int playerLevel, double levelFactor)
        {
            if (playerLevel < config.MinLevel)
                return config.EarlyMultiplier;
            
            if (playerLevel < config.EarlyThreshold)
                return config.MidMultiplier;
            
            return config.BaseMultiplier + (levelFactor * config.LevelBonus);
        }

        private static void ApplyBonuses(Item item, RarityData rarity)
        {
            // Special handling for Common items: 25% chance to have mods/stat bonuses
            if (rarity.Name.Equals("Common", StringComparison.OrdinalIgnoreCase))
            {
                // 25% chance for Common items to have bonuses
                if (_random.NextDouble() < 0.25)
                {
                    // Apply 1 stat bonus and 1 modification for Common items that get bonuses
                    ApplyStatBonuses(item, 1);
                    ApplyModifications(item, 1);
                }
                // If the 25% roll fails, Common items get no bonuses (as intended)
            }
            else
            {
                // Apply bonuses normally for all other rarities
                ApplyStatBonuses(item, rarity.StatBonuses);
                ApplyActionBonuses(item, rarity.ActionBonuses);
                ApplyModifications(item, rarity.Modifications);
            }

            // Update item name to include modifications and stat bonuses
            item.Name = ItemGenerator.GenerateItemNameWithBonuses(item);
        }

        private static void ApplyStatBonuses(Item item, int count)
        {
            if (_statBonuses != null && _statBonuses.Count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var statBonus = _statBonuses[_random.Next(_statBonuses.Count)];
                    item.StatBonuses.Add(statBonus);
                }
            }
        }

        private static void ApplyActionBonuses(Item item, int count)
        {
            if (_actionBonuses != null && _actionBonuses.Count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var actionBonus = _actionBonuses[_random.Next(_actionBonuses.Count)];
                    item.ActionBonuses.Add(actionBonus);
                }
            }
        }

        private static void ApplyModifications(Item item, int count)
        {
            if (_modifications != null && _modifications.Count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var modification = RollModification(item.Tier);
                    if (modification != null)
                    {
                        item.Modifications.Add(modification);
                        
                        // Handle reroll effect (Divine modification)
                        if (modification.Effect == "reroll")
                        {
                            var additionalMod = RollModification(item.Tier, 3); // +3 bonus for reroll
                            if (additionalMod != null)
                            {
                                item.Modifications.Add(additionalMod);
                            }
                            // Note: Divine modification itself is already added above, 
                            // the reroll result is the additional modification
                        }
                    }
                }
            }
        }


        private static Modification? RollModification(int itemTier = 1, int bonus = 0)
        {
            // Use the new Dice.RollModification method for 1-24 system
            int diceRoll = Dice.RollModification(itemTier, bonus);
            var baseModification = _modifications!.FirstOrDefault(m => m.DiceResult == diceRoll);
            
            if (baseModification == null) return null;
            
            // Create a copy of the modification and roll a value between MinValue and MaxValue
            var rolledModification = new Modification
            {
                DiceResult = baseModification.DiceResult,
                ItemRank = baseModification.ItemRank,
                Name = baseModification.Name,
                Description = baseModification.Description,
                Effect = baseModification.Effect,
                MinValue = baseModification.MinValue,
                MaxValue = baseModification.MaxValue,
                RolledValue = RollValueBetween(baseModification.MinValue, baseModification.MaxValue)
            };
            
            return rolledModification;
        }
        
        private static double RollValueBetween(double minValue, double maxValue)
        {
            // If min and max are the same, return that value
            if (Math.Abs(minValue - maxValue) < 0.001)
                return minValue;
                
            // Roll a random value between min and max (inclusive)
            return minValue + (_random.NextDouble() * (maxValue - minValue));
        }



        private static void LoadTierDistributions()
        {
            string? filePath = JsonLoader.FindGameDataFile("TierDistribution.json");
            if (filePath != null)
            {
                _tierDistributions = JsonLoader.LoadJsonList<TierDistribution>(filePath);
            }
            else
            {
                UIManager.WriteLine("Error loading tier distributions: TierDistribution.json not found", UIMessageType.System);
                _tierDistributions = new List<TierDistribution>();
            }
        }

        private static void LoadArmorData()
        {
            string? filePath = JsonLoader.FindGameDataFile("Armor.json");
            if (filePath != null)
            {
                _armorData = JsonLoader.LoadJsonList<ArmorData>(filePath);
            }
            else
            {
                UIManager.WriteLine("Error loading armor data: Armor.json not found", UIMessageType.System);
                _armorData = new List<ArmorData>();
            }
        }

        private static void LoadWeaponData()
        {
            string? filePath = JsonLoader.FindGameDataFile("Weapons.json");
            if (filePath != null)
            {
                _weaponData = JsonLoader.LoadJsonList<WeaponData>(filePath);
            }
            else
            {
                UIManager.WriteLine("Error loading weapon data: Weapons.json not found", UIMessageType.System);
                _weaponData = new List<WeaponData>();
            }
        }

        private static void LoadStatBonuses()
        {
            string? filePath = JsonLoader.FindGameDataFile("StatBonuses.json");
            if (filePath != null)
            {
                _statBonuses = JsonLoader.LoadJsonList<StatBonus>(filePath);
            }
            else
            {
                UIManager.WriteLine("Error loading stat bonuses: StatBonuses.json not found", UIMessageType.System);
                _statBonuses = new List<StatBonus>();
            }
        }

        private static void LoadActionBonuses()
        {
            string? filePath = JsonLoader.FindGameDataFile("Actions.json");
            if (filePath != null)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                };
                var actions = JsonLoader.LoadJsonList<ActionData>(filePath, true);
                _actionBonuses = actions?.Select(a => new ActionBonus { Name = a.Name, Description = a.Description, Weight = 1 }).ToList() ?? new List<ActionBonus>();
            }
            else
            {
                UIManager.WriteLine("Error loading action bonuses: Actions.json not found", UIMessageType.System);
                _actionBonuses = new List<ActionBonus>();
            }
        }

        private static void LoadModifications()
        {
            string? filePath = JsonLoader.FindGameDataFile("Modifications.json");
            if (filePath != null)
            {
                _modifications = JsonLoader.LoadJsonList<Modification>(filePath);
            }
            else
            {
                UIManager.WriteLine("Error loading modifications: Modifications.json not found", UIMessageType.System);
                _modifications = new List<Modification>();
            }
        }

        private static void LoadRarityData()
        {
            try
            {
                string? filePath = JsonLoader.FindGameDataFile("RarityTable.json");
                if (filePath != null)
                {
                    string json = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    _rarityData = JsonSerializer.Deserialize<List<RarityData>>(json, options);
                    
                    // Ensure _rarityData is never null
                    if (_rarityData == null)
                    {
                        _rarityData = new List<RarityData>();
                    }
                }
                else
                {
                    Console.WriteLine("Error loading rarity data: RarityTable.json not found");
                    _rarityData = new List<RarityData>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading rarity data: {ex.Message}");
                _rarityData = new List<RarityData>();
            }
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
