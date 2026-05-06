using System;
using System.Collections.Generic;
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
        /// Loot table: 50% weapon, 12.5% each armor slot (head / chest / legs / feet).
        /// </summary>
        public (bool isWeapon, string? armorJsonSlot) RollLootCategory()
        {
            double r = _random.NextDouble();
            if (r < 0.5)
                return (true, null);
            if (r < 0.625)
                return (false, "head");
            if (r < 0.75)
                return (false, "chest");
            if (r < 0.875)
                return (false, "legs");
            return (false, "feet");
        }

        /// <summary>Uniform pick among the four armor JSON slots (for fallback when switching item category).</summary>
        public string RollArmorJsonSlotUniform()
        {
            double r = _random.NextDouble();
            if (r < 0.25) return "head";
            if (r < 0.5) return "chest";
            if (r < 0.75) return "legs";
            return "feet";
        }

        /// <summary>
        /// Selects an item (weapon or armor) based on tier.
        /// When <paramref name="armorJsonSlot"/> is set and <paramref name="isWeapon"/> is false, prefers that catalog slot; falls back if no rows match.
        /// When <paramref name="armorJsonSlot"/> is null and not weapon, any armor at tier (legacy / trade-up).
        /// </summary>
        public Item? SelectItem(int tier, bool isWeapon, string? armorJsonSlot = null)
        {
            return isWeapon ? RollWeapon(tier) : RollArmor(tier, armorJsonSlot);
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

            return ItemGenerator.GenerateWeaponItem(selectedWeapon);
        }

        /// <summary>
        /// Rolls for a random armor piece of the specified tier, optionally constrained to a catalog <c>slot</c> value.
        /// </summary>
        public Item? RollArmor(int tier, string? preferredJsonSlot = null)
        {
            IEnumerable<ArmorData> source = _dataCache.ArmorData;
            IEnumerable<ArmorData> atTier = source.Where(a => a.Tier == tier);
            List<ArmorData> pool;

            if (!string.IsNullOrEmpty(preferredJsonSlot))
            {
                pool = atTier.Where(a => string.Equals(a.Slot, preferredJsonSlot, StringComparison.OrdinalIgnoreCase)).ToList();
                if (!pool.Any())
                    pool = atTier.ToList();
            }
            else
            {
                pool = atTier.ToList();
            }

            if (!pool.Any())
                pool = source.ToList();

            if (!pool.Any())
                return null;

            var selectedArmor = pool[_random.Next(pool.Count)];

            Item? item = ItemGenerator.GenerateArmorItem(selectedArmor);

            if (item != null)
                item.GearAction = GetRandomArmorAction();

            return item;
        }

        /// <summary>
        /// Gets a random armor action for variety
        /// </summary>
        private string? GetRandomArmorAction()
        {
            var allActions = ActionLoader.GetAllActions();
            var availableActions = allActions
                .Where(action => action.IsComboAction &&
                               !action.Tags.Contains("environment") &&
                               !action.Tags.Contains("enemy") &&
                               !action.Tags.Contains("unique"))
                .Select(action => action.Name)
                .ToList();

            if (availableActions.Count > 0)
                return availableActions[_random.Next(availableActions.Count)];

            return null;
        }
    }
}
