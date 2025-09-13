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

        public static Item? GenerateLoot(int playerLevel, int dungeonLevel)
        {
            if (_tierDistributions == null || _armorData == null || _weaponData == null || 
                _statBonuses == null || _actionBonuses == null || _modifications == null || _rarityData == null)
            {
                Initialize();
            }

            var tuning = TuningConfig.Instance;
            
            // Calculate loot chance based on tuning config
            double lootChance = tuning.Loot.LootChanceBase + (playerLevel * tuning.Loot.LootChancePerLevel);
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

            // ROLL 5b,c: Bonus damage and attack speed based on tuning config
            if (item is WeaponItem weapon)
            {
                weapon.BonusDamage = _random.Next(tuning.Equipment.BonusDamageRange.Min, tuning.Equipment.BonusDamageRange.Max + 1);
                weapon.BonusAttackSpeed = _random.Next(tuning.Equipment.BonusAttackSpeedRange.Min, tuning.Equipment.BonusAttackSpeedRange.Max + 1);
            }

            // ROLL 6: Rarity (determines number of bonuses)
            var rarity = RollRarity();
            item.Rarity = rarity.Name;

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
            
            // Check for "highest item jumps to next tier up" rule
            if (IsHighestItemInTier(selectedWeapon, weaponsInTier) && tier < 5)
            {
                // Roll again on next tier
                var nextTierWeapons = _weaponData?.Where(w => w.Tier == tier + 1).ToList() ?? new List<WeaponData>();
                if (nextTierWeapons.Any())
                {
                    selectedWeapon = nextTierWeapons[_random.Next(nextTierWeapons.Count)];
                }
            }

            var weaponType = Enum.Parse<WeaponType>(selectedWeapon.Type);
            return new WeaponItem(selectedWeapon.Name, selectedWeapon.Tier, 
                selectedWeapon.BaseDamage, selectedWeapon.AttackSpeed, weaponType);
        }

        private static Item? RollArmor(int tier)
        {
            var armorInTier = _armorData!.Where(a => a.Tier == tier).ToList();
            if (!armorInTier.Any()) return null;

            var selectedArmor = armorInTier[_random.Next(armorInTier.Count)];
            
            // Check for "highest item jumps to next tier up" rule
            if (IsHighestItemInTier(selectedArmor, armorInTier) && tier < 5)
            {
                // Roll again on next tier
                var nextTierArmor = _armorData?.Where(a => a.Tier == tier + 1).ToList() ?? new List<ArmorData>();
                if (nextTierArmor.Any())
                {
                    selectedArmor = nextTierArmor[_random.Next(nextTierArmor.Count)];
                }
            }

            return selectedArmor.Slot switch
            {
                "Head" => new HeadItem(selectedArmor.Name, selectedArmor.Tier, selectedArmor.Armor),
                "Chest" => new ChestItem(selectedArmor.Name, selectedArmor.Tier, selectedArmor.Armor),
                "Feet" => new FeetItem(selectedArmor.Name, selectedArmor.Tier, selectedArmor.Armor),
                _ => null
            };
        }

        private static bool IsHighestItemInTier(WeaponData item, List<WeaponData> itemsInTier)
        {
            return item.BaseDamage == itemsInTier.Max(w => w.BaseDamage);
        }

        private static bool IsHighestItemInTier(ArmorData item, List<ArmorData> itemsInTier)
        {
            return item.Armor == itemsInTier.Max(a => a.Armor);
        }

        private static RarityData RollRarity()
        {
            double roll = _random.NextDouble() * 100;
            double cumulative = 0;

            foreach (var rarity in _rarityData!)
            {
                cumulative += rarity.Weight;
                if (roll < cumulative)
                {
                    return rarity;
                }
            }

            return _rarityData.First(); // Fallback
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

            // Update item name to include modifications
            item.Name = GenerateItemNameWithModifications(item);
        }

        private static Modification? RollModification(int itemTier = 1, int bonus = 0)
        {
            // Use the new Dice.RollModification method for 1-24 system
            int diceRoll = Dice.RollModification(itemTier, bonus);
            return _modifications!.FirstOrDefault(m => m.DiceResult == diceRoll);
        }

        private static string GenerateItemNameWithModifications(Item item)
        {
            // Get the base name (remove "Starter" prefix if present for cleaner names)
            string baseName = item.Name;
            if (baseName.StartsWith("Starter "))
            {
                baseName = baseName.Substring(8); // Remove "Starter "
            }

            // Collect modification names (excluding negative ones for better naming)
            var modificationNames = new List<string>();
            foreach (var mod in item.Modifications)
            {
                // Skip negative modifications in the name (they're still applied, just not in name)
                if (mod.Name != "Worn" && mod.Name != "Dull")
                {
                    modificationNames.Add(mod.Name);
                }
            }

            // Collect stat bonus names (limit to first 2 to avoid overly long names)
            var statBonusNames = new List<string>();
            var bonusCount = 0;
            foreach (var statBonus in item.StatBonuses)
            {
                if (bonusCount >= 2) break; // Limit to 2 stat bonuses in name
                statBonusNames.Add(statBonus.Name);
                bonusCount++;
            }

            // Build the full name: [Rarity] [Modifications] [Base Name] [Stat Bonuses]
            var nameComponents = new List<string>();

            // Add rarity if not Common
            if (item.Rarity != "Common")
            {
                nameComponents.Add(item.Rarity);
            }

            // Add modifications
            nameComponents.AddRange(modificationNames);

            // Add base name
            nameComponents.Add(baseName);

            // Add stat bonuses at the end
            nameComponents.AddRange(statBonusNames);

            return string.Join(" ", nameComponents);
        }

        private static void LoadTierDistributions()
        {
            try
            {
                string json = File.ReadAllText("../GameData/TierDistribution.json");
                _tierDistributions = JsonSerializer.Deserialize<List<TierDistribution>>(json);
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
                string json = File.ReadAllText("../GameData/Armor.json");
                _armorData = JsonSerializer.Deserialize<List<ArmorData>>(json);
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
                string json = File.ReadAllText("../GameData/Weapons.json");
                _weaponData = JsonSerializer.Deserialize<List<WeaponData>>(json);
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
                string json = File.ReadAllText("../GameData/StatBonuses.json");
                _statBonuses = JsonSerializer.Deserialize<List<StatBonus>>(json);
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
                string json = File.ReadAllText("../GameData/Actions.json");
                var actions = JsonSerializer.Deserialize<List<Action>>(json);
                _actionBonuses = actions?.Select(a => new ActionBonus { Name = a.Name, Description = a.Description, Weight = 1 }).ToList() ?? new List<ActionBonus>();
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
                string json = File.ReadAllText("../GameData/Modifications.json");
                _modifications = JsonSerializer.Deserialize<List<Modification>>(json);
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
                string json = File.ReadAllText("../GameData/RarityTable.json");
                _rarityData = JsonSerializer.Deserialize<List<RarityData>>(json);
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
