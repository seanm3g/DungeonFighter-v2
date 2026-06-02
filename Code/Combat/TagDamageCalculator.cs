using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.World.Tags;

namespace RPGGame.Combat
{
    /// <summary>
    /// Calculates damage modifiers based on tag matching
    /// </summary>
    public static class TagDamageCalculator
    {
        public static double GetDamageModifier(Action action, Actor target)
        {
            if (action == null || target == null)
                return 1.0;

            double modifier = 1.0;
            var actionTags = action.Tags ?? new List<string>();
            var targetTags = GetTargetTags(target);

            modifier *= GetElementModifier(actionTags, targetTags);
            modifier *= GetClassModifier(actionTags, targetTags);
            modifier *= GetRarityModifier(actionTags, targetTags);

            return modifier;
        }

        private static List<string> GetTargetTags(Actor target)
        {
            if (target is Character character && target is not Enemy)
                return TagAggregator.AggregateCharacterTags(character);

            if (target is Enemy enemy)
                return enemy.Tags.ToList();

            return new List<string>();
        }

        /// <summary>Fire → Earth → Water → Air → Fire weakness cycle.</summary>
        private static double GetElementModifier(List<string> actionTags, List<string> targetTags)
        {
            var elementPairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "fire", "earth" },
                { "earth", "water" },
                { "water", "air" },
                { "air", "fire" }
            };

            foreach (var actionTag in actionTags)
            {
                if (elementPairs.TryGetValue(actionTag.Trim(), out var weakElement) &&
                    targetTags.Any(t => string.Equals(t.Trim(), weakElement, StringComparison.OrdinalIgnoreCase)))
                {
                    return 1.5;
                }
            }

            return 1.0;
        }

        private static double GetClassModifier(List<string> actionTags, List<string> targetTags)
        {
            var classBonuses = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "wizard", new List<string> { "warrior" } },
                { "warrior", new List<string> { "rogue" } },
                { "rogue", new List<string> { "wizard" } }
            };

            foreach (var actionTag in actionTags)
            {
                if (classBonuses.TryGetValue(actionTag.Trim(), out var weakClasses) &&
                    targetTags.Any(t => weakClasses.Contains(t.Trim(), StringComparer.OrdinalIgnoreCase)))
                {
                    return 1.2;
                }
            }

            return 1.0;
        }

        private static double GetRarityModifier(List<string> actionTags, List<string> targetTags)
        {
            var rarityOrder = new List<string> { "common", "uncommon", "rare", "epic", "legendary", "mythic" };

            int? actionRarity = null;
            int? targetRarity = null;

            foreach (var tag in actionTags)
            {
                var index = rarityOrder.IndexOf(tag.Trim().ToLowerInvariant());
                if (index >= 0)
                {
                    actionRarity = index;
                    break;
                }
            }

            foreach (var tag in targetTags)
            {
                var index = rarityOrder.IndexOf(tag.Trim().ToLowerInvariant());
                if (index >= 0)
                {
                    targetRarity = index;
                    break;
                }
            }

            if (actionRarity.HasValue && targetRarity.HasValue)
            {
                int difference = actionRarity.Value - targetRarity.Value;
                return 1.0 + (difference * 0.05);
            }

            return 1.0;
        }
    }
}
