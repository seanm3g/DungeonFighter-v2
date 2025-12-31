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
        private readonly LootModificationSelector? _modificationSelector;
        private readonly LootActionSelector? _actionSelector;

        public LootBonusApplier(LootDataCache dataCache, Random random)
        {
            _dataCache = dataCache;
            _random = random;
            // Initialize selectors for contextual loot
            _modificationSelector = new LootModificationSelector(random);
            _actionSelector = new LootActionSelector(random);
        }

        /// <summary>
        /// Applies all bonuses to an item based on its rarity
        /// </summary>
        public void ApplyBonuses(Item item, RarityData rarity, LootContext? context = null)
        {
            // Special handling for Common items: 10% chance to have stat bonuses only
            if (rarity.Name.Equals("Common", StringComparison.OrdinalIgnoreCase))
            {
                // 10% chance for Common items to have stat bonuses (no modifications)
                if (_random.NextDouble() < 0.10)
                {
                    // Apply 1 stat bonus only for Common items that get bonuses
                    ApplyStatBonuses(item, 1);
                }
                // If the 10% roll fails, Common items get no bonuses (as intended)
            }
            else if (rarity.Name.Equals("Uncommon", StringComparison.OrdinalIgnoreCase))
            {
                // Uncommon items: 80% chance to get modifications (not guaranteed)
                ApplyStatBonuses(item, rarity.StatBonuses);
                ApplyActionBonuses(item, rarity.ActionBonuses, context);
                
                // Roll for modifications: 80% chance to get the full count
                if (_random.NextDouble() < 0.80)
                {
                    ApplyModifications(item, rarity.Modifications, context);
                }
                // 20% chance Uncommon items get no modifications
            }
            else
            {
                // Apply bonuses normally for all other rarities (Rare+)
                ApplyStatBonuses(item, rarity.StatBonuses);
                ApplyActionBonuses(item, rarity.ActionBonuses, context);
                ApplyModifications(item, rarity.Modifications, context);
            }

            // Update item name to include modifications and stat bonuses
            item.Name = ItemGenerator.GenerateItemNameWithBonuses(item);
            
            // Adjust rarity based on bonuses: some bonuses require minimum rarities
            AdjustRarityBasedOnBonuses(item, rarity);
        }
        
        /// <summary>
        /// Adjusts item rarity based on the bonuses it has
        /// Some bonuses require minimum rarities (e.g., "Sharp" requires Uncommon, "of the Sage" requires Rare)
        /// </summary>
        private void AdjustRarityBasedOnBonuses(Item item, RarityData currentRarity)
        {
            string? requiredRarity = null;
            
            // Check modifications for their ItemRank (minimum rarity requirement)
            if (item.Modifications != null && item.Modifications.Count > 0)
            {
                foreach (var mod in item.Modifications)
                {
                    if (!string.IsNullOrEmpty(mod.ItemRank))
                    {
                        // Get the highest required rarity from all modifications
                        if (requiredRarity == null || IsRarityHigher(mod.ItemRank, requiredRarity))
                        {
                            requiredRarity = mod.ItemRank;
                        }
                    }
                }
            }
            
            // Check stat bonuses for specific rare ones
            if (item.StatBonuses != null && item.StatBonuses.Count > 0)
            {
                foreach (var statBonus in item.StatBonuses)
                {
                    // "of the Sage" requires Rare rarity
                    if (statBonus.Name.Equals("of the Sage", StringComparison.OrdinalIgnoreCase))
                    {
                        if (requiredRarity == null || IsRarityHigher("Rare", requiredRarity))
                        {
                            requiredRarity = "Rare";
                        }
                    }
                }
            }
            
            // Upgrade rarity if needed
            if (requiredRarity != null && IsRarityHigher(requiredRarity, currentRarity.Name))
            {
                // Update item rarity to match the minimum required by bonuses
                item.Rarity = requiredRarity;
            }
        }
        
        /// <summary>
        /// Checks if rarity1 is higher than rarity2
        /// </summary>
        private bool IsRarityHigher(string rarity1, string rarity2)
        {
            var rarityOrder = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic", "Transcendent" };
            
            int index1 = Array.IndexOf(rarityOrder, rarity1);
            int index2 = Array.IndexOf(rarityOrder, rarity2);
            
            if (index1 < 0) index1 = 0; // Unknown rarities default to Common
            if (index2 < 0) index2 = 0;
            
            return index1 > index2;
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
        /// Uses contextual selection if context is provided (80/20 split by weapon type)
        /// Falls back to random selection if no context or tables not available
        /// </summary>
        public void ApplyActionBonuses(Item item, int count, LootContext? context = null)
        {
            if (_dataCache.ActionBonuses == null || _dataCache.ActionBonuses.Count == 0)
                return;

            for (int i = 0; i < count; i++)
            {
                // Try contextual selection first if selector is available
                if (_actionSelector != null && context != null)
                {
                    var contextualAction = _actionSelector.SelectAction(context, item);
                    if (!string.IsNullOrEmpty(contextualAction))
                    {
                        // Add action bonus by name lookup
                        var actionBonus = _dataCache.ActionBonuses.FirstOrDefault(
                            a => a.Name.Equals(contextualAction, StringComparison.OrdinalIgnoreCase));
                        if (actionBonus != null)
                        {
                            item.ActionBonuses.Add(actionBonus);
                            continue;
                        }
                    }
                }

                // Fallback: use random selection
                var randomAction = _dataCache.ActionBonuses[_random.Next(_dataCache.ActionBonuses.Count)];
                item.ActionBonuses.Add(randomAction);
            }
        }

        /// <summary>
        /// Applies modifications to an item
        /// Implements 70/30 bias: 70% contextual modifications, 30% standard random
        /// </summary>
        public void ApplyModifications(Item item, int count, LootContext? context = null)
        {
            if (_dataCache.Modifications == null || _dataCache.Modifications.Count == 0)
                return;

            for (int i = 0; i < count; i++)
            {
                Modification? modification = null;

                // Try contextual selection first if available (70% chance)
                if (_modificationSelector != null && context != null && _random.NextDouble() < 0.70)
                {
                    // Get favored dice results from context
                    var favoredResults = _modificationSelector.GetFavoredDiceResults(context);
                    if (favoredResults.Count > 0)
                    {
                        // Roll a die and bias toward favored results
                        int diceRoll = Dice.RollModification(item.Tier);

                        // 70% chance: try to match a favored result
                        if (favoredResults.Contains(diceRoll))
                        {
                            // Roll matched a favored result - use contextual modification
                            modification = RollModification(item.Tier);
                        }
                        else
                        {
                            // Didn't match - pick a random favored result instead
                            int favoredRoll = favoredResults[_random.Next(favoredResults.Count)];
                            modification = _dataCache.Modifications.FirstOrDefault(m => m.DiceResult == favoredRoll);
                        }
                    }
                }

                // Fallback: standard random selection (30% or if contextual failed)
                if (modification == null)
                {
                    modification = RollModification(item.Tier);
                }

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

