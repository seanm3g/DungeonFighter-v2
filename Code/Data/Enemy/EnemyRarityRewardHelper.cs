using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>Reward and loot bonuses keyed by enemy rarity tier from <c>Enemies.json</c>.</summary>
    public static class EnemyRarityRewardHelper
    {
        private static readonly Dictionary<string, double> DefaultRewardMultipliers =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Common"] = 1.0,
                ["Uncommon"] = 1.25,
                ["Rare"] = 1.5,
                ["Epic"] = 2.0,
                ["Legendary"] = 3.0,
                ["Mythic"] = 5.0
            };

        /// <summary>Added to player magic find when rolling loot from a defeated enemy of this rarity.</summary>
        private static readonly Dictionary<string, double> DefaultLootMagicFindBonus =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Common"] = 0.0,
                ["Uncommon"] = 0.02,
                ["Rare"] = 0.05,
                ["Epic"] = 0.10,
                ["Legendary"] = 0.15,
                ["Mythic"] = 0.25
            };

        public static double GetRewardMultiplier(string? rarity)
        {
            string name = EnemySpawnFilter.ResolveRarityName(rarity);
            var config = GameConfiguration.Instance?.EnemySystem?.RarityRewardMultipliers;
            if (config != null && config.TryGetValue(name, out double configured) && configured > 0)
                return configured;
            if (DefaultRewardMultipliers.TryGetValue(name, out double fallback))
                return fallback;
            return 1.0;
        }

        public static double GetLootMagicFindBonus(string? rarity)
        {
            string name = EnemySpawnFilter.ResolveRarityName(rarity);
            var config = GameConfiguration.Instance?.EnemySystem?.RarityLootMagicFindBonus;
            if (config != null && config.TryGetValue(name, out double configured) && configured >= 0)
                return configured;
            if (DefaultLootMagicFindBonus.TryGetValue(name, out double fallback))
                return fallback;
            return 0.0;
        }

        public static void ApplyToEnemy(Enemy enemy, string? rarity)
        {
            if (enemy == null)
                return;

            string resolved = EnemySpawnFilter.ResolveRarityName(rarity);
            double mult = GetRewardMultiplier(resolved);
            enemy.ApplyRarityScaling(resolved, mult);
        }
    }
}
