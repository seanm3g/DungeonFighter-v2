using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.World.Tags;

namespace RPGGame.Combat
{
    /// <summary>
    /// Calculates damage modifiers based on tag matching
    /// Supports element matching, class bonuses, rarity bonuses
    /// </summary>
    public static class TagDamageCalculator
    {
        /// <summary>
        /// Gets damage modifier based on tag matching between action and target
        /// </summary>
        /// <param name="action">The action being performed</param>
        /// <param name="target">The target actor</param>
        /// <returns>Damage multiplier (1.0 = no change, 1.5 = +50%, 0.5 = -50%)</returns>
        public static double GetDamageModifier(Action action, Actor target)
        {
            if (action == null || target == null)
                return 1.0;

            double modifier = 1.0;
            var actionTags = action.Tags ?? new List<string>();
            var targetTags = GetTargetTags(target);

            // Element matching (FIRE vs ICE, etc.)
            modifier *= GetElementModifier(actionTags, targetTags);

            // Class bonuses (WIZARD vs WARRIOR, etc.)
            modifier *= GetClassModifier(actionTags, targetTags);

            // Rarity bonuses (EPIC vs COMMON, etc.)
            modifier *= GetRarityModifier(actionTags, targetTags);

            return modifier;
        }

        /// <summary>
        /// Gets tags for the target actor (from equipment, class, etc.)
        /// </summary>
        private static List<string> GetTargetTags(Actor target)
        {
            var tags = new List<string>();

            if (target is Character character)
            {
                // Add class tags
                // Note: This would need to be implemented based on character class system
                // For now, we'll check equipment tags
                if (character.Equipment?.Weapon is Item weapon)
                {
                    // Assuming items have tags - this would need to be implemented
                    // tags.AddRange(weapon.Tags ?? new List<string>());
                }
            }
            else if (target is Enemy enemy)
            {
                // Enemies might have tags defined in their data
                // This would need to be implemented based on enemy data structure
            }

            return tags;
        }

        /// <summary>
        /// Calculates element-based damage modifier
        /// </summary>
        private static double GetElementModifier(List<string> actionTags, List<string> targetTags)
        {
            // Element weaknesses (FIRE > ICE, ICE > FIRE, etc.)
            var elementPairs = new Dictionary<string, string>
            {
                { "FIRE", "ICE" },
                { "ICE", "FIRE" },
                { "WATER", "FIRE" },
                { "EARTH", "AIR" },
                { "AIR", "EARTH" },
                { "LIGHTNING", "WATER" }
            };

            foreach (var actionTag in actionTags)
            {
                var upperTag = actionTag.ToUpper();
                if (elementPairs.TryGetValue(upperTag, out var weakElement))
                {
                    if (targetTags.Any(t => t.ToUpper() == weakElement))
                    {
                        return 1.5; // +50% damage for element advantage
                    }
                }
            }

            return 1.0;
        }

        /// <summary>
        /// Calculates class-based damage modifier
        /// </summary>
        private static double GetClassModifier(List<string> actionTags, List<string> targetTags)
        {
            // Class bonuses (WIZARD actions vs WARRIOR targets, etc.)
            // This is a placeholder - actual implementation would depend on game balance
            var classBonuses = new Dictionary<string, List<string>>
            {
                { "WIZARD", new List<string> { "WARRIOR" } },
                { "WARRIOR", new List<string> { "ROGUE" } },
                { "ROGUE", new List<string> { "WIZARD" } }
            };

            foreach (var actionTag in actionTags)
            {
                var upperTag = actionTag.ToUpper();
                if (classBonuses.TryGetValue(upperTag, out var weakClasses))
                {
                    if (targetTags.Any(t => weakClasses.Contains(t.ToUpper())))
                    {
                        return 1.2; // +20% damage for class advantage
                    }
                }
            }

            return 1.0;
        }

        /// <summary>
        /// Calculates rarity-based damage modifier
        /// </summary>
        private static double GetRarityModifier(List<string> actionTags, List<string> targetTags)
        {
            // Rarity bonuses (higher rarity actions deal more damage to lower rarity targets)
            var rarityOrder = new List<string> { "COMMON", "UNCOMMON", "RARE", "EPIC", "LEGENDARY", "MYTHIC", "TRANSCENDENT" };

            int? actionRarity = null;
            int? targetRarity = null;

            foreach (var tag in actionTags)
            {
                var index = rarityOrder.IndexOf(tag.ToUpper());
                if (index >= 0)
                {
                    actionRarity = index;
                    break;
                }
            }

            foreach (var tag in targetTags)
            {
                var index = rarityOrder.IndexOf(tag.ToUpper());
                if (index >= 0)
                {
                    targetRarity = index;
                    break;
                }
            }

            if (actionRarity.HasValue && targetRarity.HasValue)
            {
                int difference = actionRarity.Value - targetRarity.Value;
                if (difference > 0)
                {
                    // Higher rarity action vs lower rarity target: +5% per tier
                    return 1.0 + (difference * 0.05);
                }
                else if (difference < 0)
                {
                    // Lower rarity action vs higher rarity target: -5% per tier
                    return 1.0 + (difference * 0.05);
                }
            }

            return 1.0;
        }
    }
}

