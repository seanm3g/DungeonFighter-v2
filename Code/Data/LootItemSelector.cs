using System;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Selects specific items (weapons or armor) based on tier
    /// Handles weapon vs armor decision and item filtering/selection
    /// </summary>
    public class LootItemSelector
    {
        private readonly LootDataCache _dataCache;
        private readonly Random _random;

        public LootItemSelector(LootDataCache dataCache, Random random)
        {
            _dataCache = dataCache;
            _random = random;
        }

        /// <summary>
        /// Determines if the generated item should be a weapon (25% chance) or armor (75% chance)
        /// </summary>
        public bool DetermineIsWeapon()
        {
            return _random.NextDouble() < 0.25;
        }

        /// <summary>
        /// Selects an item (weapon or armor) based on tier
        /// </summary>
        public Item? SelectItem(int tier, bool isWeapon)
        {
            return isWeapon ? RollWeapon(tier) : RollArmor(tier);
        }

        /// <summary>
        /// Rolls for a random weapon of the specified tier
        /// </summary>
        public Item? RollWeapon(int tier)
        {
            var weaponsInTier = _dataCache.WeaponData.Where(w => w.Tier == tier).ToList();
            
            if (!weaponsInTier.Any())
            {
                return null;
            }

            var selectedWeapon = weaponsInTier[_random.Next(weaponsInTier.Count)];
            
            // DISABLED: "highest item jumps to next tier up" rule - this was breaking tier distribution
            // if (IsHighestItemInTier(selectedWeapon, weaponsInTier) && tier < 5)
            // {
            //     // Roll again on next tier
            //     var nextTierWeapons = _dataCache.WeaponData?.Where(w => w.Tier == tier + 1).ToList() ?? new List<WeaponData>();
            //     if (nextTierWeapons.Any())
            //     {
            //         selectedWeapon = nextTierWeapons[_random.Next(nextTierWeapons.Count)];
            //     }
            // }

            return ItemGenerator.GenerateWeaponItem(selectedWeapon);
        }

        /// <summary>
        /// Rolls for a random armor piece of the specified tier
        /// </summary>
        public Item? RollArmor(int tier)
        {
            var armorInTier = _dataCache.ArmorData.Where(a => a.Tier == tier).ToList();
            
            if (!armorInTier.Any())
            {
                return null;
            }

            var selectedArmor = armorInTier[_random.Next(armorInTier.Count)];
            
            // DISABLED: "highest item jumps to next tier up" rule - this was breaking tier distribution
            // if (IsHighestItemInTier(selectedArmor, armorInTier) && tier < 5)
            // {
            //     // Roll again on next tier
            //     var nextTierArmor = _dataCache.ArmorData?.Where(a => a.Tier == tier + 1).ToList() ?? new List<ArmorData>();
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

        /// <summary>
        /// Gets a random armor action for variety
        /// </summary>
        private string? GetRandomArmorAction()
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
    }
}

