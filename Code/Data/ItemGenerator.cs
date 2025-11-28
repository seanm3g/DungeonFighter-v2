using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Base class for item generation that provides common patterns and utilities
    /// Eliminates duplication between GameDataGenerator.cs and LootGenerator.cs
    /// </summary>
    public static class ItemGenerator
    {

        /// <summary>
        /// Generates a weapon item from weapon data
        /// </summary>
        /// <param name="weaponData">The weapon data to use</param>
        /// <returns>A new WeaponItem instance</returns>
        public static WeaponItem GenerateWeaponItem(WeaponData weaponData)
        {
            var weaponType = Enum.Parse<WeaponType>(weaponData.Type);
            return new WeaponItem(weaponData.Name, weaponData.Tier, 
                weaponData.BaseDamage, weaponData.AttackSpeed, weaponType);
        }

        /// <summary>
        /// Generates an armor item from armor data
        /// </summary>
        /// <param name="armorData">The armor data to use</param>
        /// <returns>A new armor Item instance</returns>
        public static Item GenerateArmorItem(ArmorData armorData)
        {
            return armorData.Slot.ToLower() switch
            {
                "head" => new HeadItem(armorData.Name, armorData.Tier, armorData.Armor),
                "chest" => new ChestItem(armorData.Name, armorData.Tier, armorData.Armor),
                "feet" => new FeetItem(armorData.Name, armorData.Tier, armorData.Armor),
                _ => throw new ArgumentException($"Unknown armor slot: {armorData.Slot}")
            };
        }

        /// <summary>
        /// Selects a random item from a list of items of the specified tier
        /// </summary>
        /// <typeparam name="T">The type of item data</typeparam>
        /// <param name="items">The list of items to select from</param>
        /// <param name="tier">The tier to filter by</param>
        /// <returns>A random item of the specified tier, or null if none found</returns>
        public static T? SelectRandomItemByTier<T>(IEnumerable<T> items, int tier) where T : class
        {
            var itemsInTier = items.Where(t => GetTierValue(t) == tier).ToList();
            if (!itemsInTier.Any()) return null;
            
            return itemsInTier[RandomUtility.Next(itemsInTier.Count)];
        }

        /// <summary>
        /// Gets the tier value from an item (works with both WeaponData and ArmorData)
        /// </summary>
        /// <typeparam name="T">The type of item data</typeparam>
        /// <param name="item">The item to get the tier from</param>
        /// <returns>The tier value</returns>
        private static int GetTierValue<T>(T item)
        {
            if (item is WeaponData weapon)
                return weapon.Tier;
            if (item is ArmorData armor)
                return armor.Tier; // Fixed: was using armor.Armor instead of armor.Tier
            return 0;
        }

        /// <summary>
        /// Generates an item name with bonuses and modifications
        /// Note: Rarity is NOT included in the name - it should be displayed separately in brackets
        /// </summary>
        /// <param name="item">The item to generate a name for</param>
        /// <returns>The generated item name (without rarity prefix)</returns>
        public static string GenerateItemNameWithBonuses(Item item)
        {
            string baseName = GetBaseItemName(item);
            var nameParts = new List<string>();
            
            // Do NOT add rarity prefix - rarity should only come from item.Rarity property
            // and be displayed separately in brackets, not as part of the name
            
            // Add modification names as prefixes (like "Balanced", "Sharp", etc.)
            foreach (var modification in item.Modifications)
            {
                if (!string.IsNullOrEmpty(modification.Name))
                {
                    nameParts.Add(modification.Name);
                }
            }
            
            // Add base name
            nameParts.Add(baseName);
            
            // Add stat bonus names as suffixes (these are typically "of Protection", "of Swiftness", etc.)
            foreach (var statBonus in item.StatBonuses)
            {
                if (!string.IsNullOrEmpty(statBonus.Name))
                {
                    nameParts.Add(statBonus.Name);
                }
            }
            
            return string.Join(" ", nameParts);
        }

        /// <summary>
        /// Gets the base item name without modifications
        /// </summary>
        /// <param name="item">The item to get the base name from</param>
        /// <returns>The base item name</returns>
        private static string GetBaseItemName(Item item)
        {
            // Remove any existing prefixes/suffixes to get the base name
            string name = item.Name;
            
            // Remove common prefixes
            string[] prefixes = { "Legendary", "Epic", "Rare", "Uncommon", "Common" };
            foreach (string prefix in prefixes)
            {
                if (name.StartsWith(prefix + " "))
                {
                    name = name.Substring(prefix.Length + 1);
                    break;
                }
            }
            
            return name;
        }

        /// <summary>
        /// Applies scaling to an item based on configuration
        /// </summary>
        /// <param name="item">The item to scale</param>
        /// <param name="scalingConfig">The scaling configuration</param>
        /// <param name="level">The level to scale for</param>
        public static void ApplyScaling(Item item, ItemScalingConfig scalingConfig, int level = 1)
        {
            if (item is WeaponItem weapon)
            {
                ApplyWeaponScaling(weapon, scalingConfig, level);
            }
            else if (item is HeadItem || item is ChestItem || item is FeetItem)
            {
                ApplyArmorScaling(item, scalingConfig, level);
            }
        }

        /// <summary>
        /// Applies scaling to a weapon item
        /// </summary>
        /// <param name="weapon">The weapon to scale</param>
        /// <param name="scalingConfig">The scaling configuration</param>
        /// <param name="level">The level to scale for</param>
        private static void ApplyWeaponScaling(WeaponItem weapon, ItemScalingConfig scalingConfig, int level)
        {
            // Apply simplified weapon scaling
            if (scalingConfig != null)
            {
                // Apply simplified tier-based scaling
                double tierMultiplier = 1.0 + (weapon.Tier - 1) * 0.3; // 30% increase per tier
                double globalMultiplier = scalingConfig.GlobalDamageMultiplier;
                weapon.BaseDamage = (int)Math.Round(weapon.BaseDamage * tierMultiplier * globalMultiplier);
                
                // Apply speed bonus per tier
                double speedBonusPerTier = scalingConfig.SpeedBonusPerTier;
                weapon.BaseAttackSpeed = Math.Max(0.1, weapon.BaseAttackSpeed + (weapon.Tier - 1) * speedBonusPerTier);
            }
        }

        /// <summary>
        /// Applies scaling to an armor item
        /// </summary>
        /// <param name="armor">The armor item to scale</param>
        /// <param name="scalingConfig">The scaling configuration</param>
        /// <param name="level">The level to scale for</param>
        private static void ApplyArmorScaling(Item armor, ItemScalingConfig scalingConfig, int level)
        {
            string slot = armor switch
            {
                HeadItem => "Head",
                ChestItem => "Chest",
                FeetItem => "Feet",
                _ => "Unknown"
            };

            // TODO: Fix armor scaling - Item class doesn't have Armor property
            // if (scalingConfig.ArmorTypes.TryGetValue(slot, out var armorConfig))
            // {
            //     var variables = new Dictionary<string, double>
            //     {
            //         ["BaseArmor"] = armor.Armor, // Item doesn't have Armor property
            //         ["Tier"] = armor.Tier,
            //         ["Level"] = level,
            //         ["BaseChance"] = 0.1
            //     };

            //     if (!string.IsNullOrEmpty(armorConfig.ArmorFormula))
            //     {
            //         double scaledArmor = FormulaEvaluator.Evaluate(armorConfig.ArmorFormula, variables);
            //         armor.Armor = (int)Math.Round(scaledArmor); // Item doesn't have Armor property
            //     }
            // }
        }

        /// <summary>
        /// Generates a random tier based on distribution
        /// </summary>
        /// <param name="tierDistributions">The tier distribution data</param>
        /// <returns>A random tier</returns>
        public static int GenerateRandomTier(List<TierDistribution> tierDistributions)
        {
            // TODO: Fix tier distribution logic - TierDistribution doesn't have Probability property
            // For now, return a random tier between 1-5
            return RandomUtility.Next(1, 6);
        }

        /// <summary>
        /// Applies random bonuses to an item
        /// </summary>
        /// <param name="item">The item to apply bonuses to</param>
        /// <param name="statBonuses">Available stat bonuses</param>
        /// <param name="actionBonuses">Available action bonuses</param>
        /// <param name="modifications">Available modifications</param>
        /// <param name="maxBonuses">Maximum number of bonuses to apply</param>
        public static void ApplyRandomBonuses(Item item, List<StatBonus> statBonuses, 
            List<ActionBonus> actionBonuses, List<Modification> modifications, int maxBonuses = 3)
        {
            var availableBonuses = new List<object>();
            availableBonuses.AddRange(statBonuses);
            availableBonuses.AddRange(actionBonuses);
            availableBonuses.AddRange(modifications);

            int bonusCount = RandomUtility.Next(0, Math.Min(maxBonuses + 1, availableBonuses.Count + 1));
            
            for (int i = 0; i < bonusCount; i++)
            {
                if (!availableBonuses.Any()) break;
                
                var selectedBonus = availableBonuses[RandomUtility.Next(availableBonuses.Count)];
                availableBonuses.Remove(selectedBonus);
                
                switch (selectedBonus)
                {
                    case StatBonus statBonus:
                        item.StatBonuses.Add(statBonus);
                        break;
                    case ActionBonus actionBonus:
                        item.ActionBonuses.Add(actionBonus);
                        break;
                    case Modification modification:
                        item.Modifications.Add(modification);
                        break;
                }
            }
        }
    }
}
