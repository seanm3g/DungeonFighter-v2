using System.Text.Json;

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

        public static Item? GenerateLoot(int playerLevel, int dungeonLevel, Character? player = null)
        {
            if (_tierDistributions == null || _armorData == null || _weaponData == null || 
                _statBonuses == null || _actionBonuses == null || _modifications == null || _rarityData == null)
            {
                Initialize();
            }

            var tuning = TuningConfig.Instance;
            
            // Calculate loot chance based on tuning config
            double lootChance = tuning.Loot.LootChanceBase + (playerLevel * tuning.Loot.LootChancePerLevel);
            
            // Apply magic find modifier to loot chance
            double magicFind = player?.GetMagicFind() ?? 0.0;
            lootChance += magicFind * tuning.Loot.MagicFindLootChanceMultiplier;
            
            lootChance = Math.Min(lootChance, tuning.Loot.MaximumLootChance);
            
            // Roll for loot chance
            if (_random.NextDouble() > lootChance) return null;

            // ROLL 1: Determine loot level (player level - dungeon level)
            int lootLevel = playerLevel - dungeonLevel;
            
            // Special rules: 3+ levels below = 0% chance, 3+ levels above = 100% chance
            if (lootLevel <= -3) return null; // No loot
            if (lootLevel >= 3) lootLevel = 100; // Guaranteed high-tier loot

            // ROLL 2: Item type (50% weapon, 50% armor)
            bool isWeapon = _random.NextDouble() < 0.5;
            
            // ROLL 3: Weapon class (ignored for now as per user request)
            
            // ROLL 4: Item tier based on loot level
            int tier = RollTier(lootLevel);
            
            // ROLL 5: Specific item selection
            Item? item = isWeapon ? RollWeapon(tier) : RollArmor(tier);
            if (item == null) return null;

            // ROLL 5b,c: Apply scaling formulas to base stats
            if (item is WeaponItem weapon)
            {
                // Apply scaling manager calculations
                double scaledDamage = ScalingManager.CalculateWeaponDamage(weapon.BaseDamage, weapon.Tier, playerLevel);
                weapon.BaseDamage = (int)Math.Round(scaledDamage);
                
                // Apply bonus damage and attack speed based on tuning config
                weapon.BonusDamage = _random.Next(tuning.Equipment.BonusDamageRange.Min, tuning.Equipment.BonusDamageRange.Max + 1);
                weapon.BonusAttackSpeed = _random.Next(tuning.Equipment.BonusAttackSpeedRange.Min, tuning.Equipment.BonusAttackSpeedRange.Max + 1);
            }
            else if (item is HeadItem headArmor)
            {
                // Apply scaling manager calculations for armor
                double scaledArmor = ScalingManager.CalculateArmorValue(headArmor.Armor, headArmor.Tier, playerLevel);
                headArmor.Armor = (int)Math.Round(scaledArmor);
            }
            else if (item is ChestItem chestArmor)
            {
                // Apply scaling manager calculations for armor
                double scaledArmor = ScalingManager.CalculateArmorValue(chestArmor.Armor, chestArmor.Tier, playerLevel);
                chestArmor.Armor = (int)Math.Round(scaledArmor);
            }
            else if (item is FeetItem feetArmor)
            {
                // Apply scaling manager calculations for armor
                double scaledArmor = ScalingManager.CalculateArmorValue(feetArmor.Armor, feetArmor.Tier, playerLevel);
                feetArmor.Armor = (int)Math.Round(scaledArmor);
            }

            // ROLL 6: Rarity (determines number of bonuses)
            var rarity = RollRarity(magicFind, playerLevel);
            item.Rarity = rarity.Name;
            
            // Apply rarity multipliers from scaling system
            double rarityMultiplier = ScalingManager.GetRarityMultiplier(rarity.Name);
            if (item is WeaponItem weaponForRarity)
            {
                weaponForRarity.BaseDamage = (int)Math.Round(weaponForRarity.BaseDamage * rarityMultiplier);
            }
            else if (item is HeadItem headForRarity)
            {
                headForRarity.Armor = (int)Math.Round(headForRarity.Armor * rarityMultiplier);
            }
            else if (item is ChestItem chestForRarity)
            {
                chestForRarity.Armor = (int)Math.Round(chestForRarity.Armor * rarityMultiplier);
            }
            else if (item is FeetItem feetForRarity)
            {
                feetForRarity.Armor = (int)Math.Round(feetForRarity.Armor * rarityMultiplier);
            }

            // ROLL 7: Bonus selection
            ApplyBonuses(item, rarity);

            return item;
        }

        private static int RollTier(int lootLevel)
        {
            // Clamp loot level to valid range
            lootLevel = Math.Max(1, Math.Min(100, lootLevel));
            
            var distribution = _tierDistributions!.FirstOrDefault(d => d.Level == lootLevel);
            if (distribution == null) return 1;

            double roll = _random.NextDouble() * 100;
            
            if (roll < distribution.Tier1) return 1;
            if (roll < distribution.Tier1 + distribution.Tier2) return 2;
            if (roll < distribution.Tier1 + distribution.Tier2 + distribution.Tier3) return 3;
            if (roll < distribution.Tier1 + distribution.Tier2 + distribution.Tier3 + distribution.Tier4) return 4;
            return 5;
        }

        private static Item? RollWeapon(int tier)
        {
            var weaponsInTier = _weaponData!.Where(w => w.Tier == tier).ToList();
            if (!weaponsInTier.Any()) return null;

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

            var weaponType = Enum.Parse<WeaponType>(selectedWeapon.Type);
            return new WeaponItem(selectedWeapon.Name, selectedWeapon.Tier, 
                selectedWeapon.BaseDamage, selectedWeapon.AttackSpeed, weaponType);
        }

        private static Item? RollArmor(int tier)
        {
            var armorInTier = _armorData!.Where(a => a.Tier == tier).ToList();
            if (!armorInTier.Any()) return null;

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

            Item? item = selectedArmor.Slot switch
            {
                "Head" => new HeadItem(selectedArmor.Name, selectedArmor.Tier, selectedArmor.Armor),
                "Chest" => new ChestItem(selectedArmor.Name, selectedArmor.Tier, selectedArmor.Armor),
                "Feet" => new FeetItem(selectedArmor.Name, selectedArmor.Tier, selectedArmor.Armor),
                _ => null
            };

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
            // Apply both magic find and level-based scaling to rarity weights
            var scaledRarities = _rarityData!.Select(r => new
            {
                Rarity = r,
                ScaledWeight = CalculateScaledRarityWeight(r, magicFind, playerLevel)
            }).ToList();
            
            double totalWeight = scaledRarities.Sum(sr => sr.ScaledWeight);
            double roll = _random.NextDouble() * totalWeight;
            double cumulative = 0;

            foreach (var scaledRarity in scaledRarities)
            {
                cumulative += scaledRarity.ScaledWeight;
                if (roll < cumulative)
                {
                    return scaledRarity.Rarity;
                }
            }

            return _rarityData?.First() ?? new RarityData { Name = "Common", Weight = 40, StatBonuses = 1, ActionBonuses = 0, Modifications = 0 }; // Fallback
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
            var config = TuningConfig.Instance.RarityScaling?.MagicFindScaling;
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
            var config = TuningConfig.Instance.RarityScaling?.LevelBasedRarityScaling;
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
            // Apply stat bonuses
            for (int i = 0; i < rarity.StatBonuses; i++)
            {
                var statBonus = _statBonuses![_random.Next(_statBonuses.Count)];
                item.StatBonuses.Add(statBonus);
            }

            // Apply action bonuses
            for (int i = 0; i < rarity.ActionBonuses; i++)
            {
                var actionBonus = _actionBonuses![_random.Next(_actionBonuses.Count)];
                item.ActionBonuses.Add(actionBonus);
            }

            // Apply modifications
            for (int i = 0; i < rarity.Modifications; i++)
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
                    }
                }
            }

            // Update item name to include modifications and stat bonuses
            item.Name = GenerateItemNameWithBonuses(item);
        }

        private static string GenerateItemNameWithBonuses(Item item)
        {
            string baseName = GetBaseItemName(item);
            var nameParts = new List<string>();
            
            // Add rarity prefix based on number of bonuses
            int totalBonuses = item.StatBonuses.Count + item.ActionBonuses.Count + item.Modifications.Count;
            string rarityPrefix = totalBonuses switch
            {
                >= 5 => "Legendary",
                >= 4 => "Epic", 
                >= 3 => "Rare",
                >= 2 => "Uncommon",
                _ => ""
            };
            
            if (!string.IsNullOrEmpty(rarityPrefix))
            {
                nameParts.Add(rarityPrefix);
            }
            
            // Add modification prefixes
            foreach (var mod in item.Modifications)
            {
                if (!string.IsNullOrEmpty(mod.Name))
                {
                    nameParts.Add(mod.Name);
                }
            }
            
            // Add base item name
            nameParts.Add(baseName);
            
            // Add stat bonus suffixes
            foreach (var statBonus in item.StatBonuses)
            {
                if (!string.IsNullOrEmpty(statBonus.Name))
                {
                    nameParts.Add(statBonus.Name);
                }
            }
            
            return string.Join(" ", nameParts);
        }
        
        private static string GetBaseItemName(Item item)
        {
            // Extract the base name without any prefixes/suffixes
            string originalName = item.Name;
            
            // Remove common rarity prefixes
            string[] rarityPrefixes = { "Legendary", "Epic", "Rare", "Uncommon" };
            foreach (var prefix in rarityPrefixes)
            {
                if (originalName.StartsWith(prefix + " "))
                {
                    originalName = originalName.Substring(prefix.Length + 1);
                }
            }
            
            // Remove modification prefixes
            foreach (var mod in item.Modifications)
            {
                if (!string.IsNullOrEmpty(mod.Name) && originalName.StartsWith(mod.Name + " "))
                {
                    originalName = originalName.Substring(mod.Name.Length + 1);
                }
            }
            
            return originalName;
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


        private static string? FindGameDataFile(string fileName)
        {
            string[] possiblePaths = {
                Path.Combine("GameData", fileName),
                Path.Combine("..", "GameData", fileName),
                Path.Combine("..", "..", "GameData", fileName),
                Path.Combine("DF4 - CONSOLE", "GameData", fileName),
                Path.Combine("..", "DF4 - CONSOLE", "GameData", fileName)
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }
            return null;
        }

        private static void LoadTierDistributions()
        {
            try
            {
                string? filePath = FindGameDataFile("TierDistribution.json");
                if (filePath != null)
                {
                    string json = File.ReadAllText(filePath);
                    _tierDistributions = JsonSerializer.Deserialize<List<TierDistribution>>(json);
                }
                else
                {
                    Console.WriteLine("Error loading tier distributions: TierDistribution.json not found");
                    _tierDistributions = new List<TierDistribution>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading tier distributions: {ex.Message}");
                _tierDistributions = new List<TierDistribution>();
            }
        }

        private static void LoadArmorData()
        {
            try
            {
                string? filePath = FindGameDataFile("Armor.json");
                if (filePath != null)
                {
                    string json = File.ReadAllText(filePath);
                    _armorData = JsonSerializer.Deserialize<List<ArmorData>>(json);
                }
                else
                {
                    Console.WriteLine("Error loading armor data: Armor.json not found");
                    _armorData = new List<ArmorData>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading armor data: {ex.Message}");
                _armorData = new List<ArmorData>();
            }
        }

        private static void LoadWeaponData()
        {
            try
            {
                string? filePath = FindGameDataFile("Weapons.json");
                if (filePath != null)
                {
                    string json = File.ReadAllText(filePath);
                    _weaponData = JsonSerializer.Deserialize<List<WeaponData>>(json);
                }
                else
                {
                    Console.WriteLine("Error loading weapon data: Weapons.json not found");
                    _weaponData = new List<WeaponData>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading weapon data: {ex.Message}");
                _weaponData = new List<WeaponData>();
            }
        }

        private static void LoadStatBonuses()
        {
            try
            {
                string? filePath = FindGameDataFile("StatBonuses.json");
                if (filePath != null)
                {
                    string json = File.ReadAllText(filePath);
                    _statBonuses = JsonSerializer.Deserialize<List<StatBonus>>(json);
                }
                else
                {
                    Console.WriteLine("Error loading stat bonuses: StatBonuses.json not found");
                    _statBonuses = new List<StatBonus>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading stat bonuses: {ex.Message}");
                _statBonuses = new List<StatBonus>();
            }
        }

        private static void LoadActionBonuses()
        {
            try
            {
                string? filePath = FindGameDataFile("Actions.json");
                if (filePath != null)
                {
                    string json = File.ReadAllText(filePath);
                    var actions = JsonSerializer.Deserialize<List<Action>>(json);
                    _actionBonuses = actions?.Select(a => new ActionBonus { Name = a.Name, Description = a.Description, Weight = 1 }).ToList() ?? new List<ActionBonus>();
                }
                else
                {
                    Console.WriteLine("Error loading action bonuses: Actions.json not found");
                    _actionBonuses = new List<ActionBonus>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading action bonuses: {ex.Message}");
                _actionBonuses = new List<ActionBonus>();
            }
        }

        private static void LoadModifications()
        {
            try
            {
                string? filePath = FindGameDataFile("Modifications.json");
                if (filePath != null)
                {
                    string json = File.ReadAllText(filePath);
                    _modifications = JsonSerializer.Deserialize<List<Modification>>(json);
                }
                else
                {
                    Console.WriteLine("Error loading modifications: Modifications.json not found");
                    _modifications = new List<Modification>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading modifications: {ex.Message}");
                _modifications = new List<Modification>();
            }
        }

        private static void LoadRarityData()
        {
            try
            {
                string? filePath = FindGameDataFile("RarityTable.json");
                if (filePath != null)
                {
                    string json = File.ReadAllText(filePath);
                    _rarityData = JsonSerializer.Deserialize<List<RarityData>>(json);
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
        public string Slot { get; set; } = "";
        public string Name { get; set; } = "";
        public int Armor { get; set; }
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
        public int Weight { get; set; }
        public int StatBonuses { get; set; }
        public int ActionBonuses { get; set; }
        public int Modifications { get; set; }
    }
}
