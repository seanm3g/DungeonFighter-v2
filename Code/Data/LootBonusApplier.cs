using System;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Applies bonuses, modifications, and stat adjustments to items
    /// Handles stat bonuses, action bonuses, and modifications based on rarity
    /// </summary>
    public class LootBonusApplier
    {
        private readonly LootDataCache _dataCache;
        private readonly Random _random;

        public LootBonusApplier(LootDataCache dataCache, Random random)
        {
            _dataCache = dataCache;
            _random = random;
        }

        /// <summary>
        /// Applies all bonuses to an item based on its rarity
        /// </summary>
        public void ApplyBonuses(Item item, RarityData rarity)
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

        /// <summary>
        /// Applies stat bonuses to an item
        /// </summary>
        public void ApplyStatBonuses(Item item, int count)
        {
            if (_dataCache.StatBonuses != null && _dataCache.StatBonuses.Count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var statBonus = _dataCache.StatBonuses[_random.Next(_dataCache.StatBonuses.Count)];
                    item.StatBonuses.Add(statBonus);
                }
            }
        }

        /// <summary>
        /// Applies action bonuses to an item
        /// </summary>
        public void ApplyActionBonuses(Item item, int count)
        {
            if (_dataCache.ActionBonuses != null && _dataCache.ActionBonuses.Count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var actionBonus = _dataCache.ActionBonuses[_random.Next(_dataCache.ActionBonuses.Count)];
                    item.ActionBonuses.Add(actionBonus);
                }
            }
        }

        /// <summary>
        /// Applies modifications to an item
        /// </summary>
        public void ApplyModifications(Item item, int count)
        {
            if (_dataCache.Modifications != null && _dataCache.Modifications.Count > 0)
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

        /// <summary>
        /// Rolls for a modification based on item tier
        /// </summary>
        private Modification? RollModification(int itemTier = 1, int bonus = 0)
        {
            // Use the new Dice.RollModification method for 1-24 system
            int diceRoll = Dice.RollModification(itemTier, bonus);
            var baseModification = _dataCache.Modifications!.FirstOrDefault(m => m.DiceResult == diceRoll);
            
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
        
        /// <summary>
        /// Rolls a random value between min and max (inclusive)
        /// </summary>
        private double RollValueBetween(double minValue, double maxValue)
        {
            // If min and max are the same, return that value
            if (Math.Abs(minValue - maxValue) < 0.001)
                return minValue;
                
            // Roll a random value between min and max (inclusive)
            return minValue + (_random.NextDouble() * (maxValue - minValue));
        }
    }
}

