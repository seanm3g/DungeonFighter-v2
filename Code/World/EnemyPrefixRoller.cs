using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Rolls <c>EnemyPrefixes.json</c> prefixes onto freshly spawned room enemies, and gives each dungeon's final boss the
    /// boss prefix (Demon). Prefixes rename the enemy ("Giant Wolf"), add <see cref="Enemy.PrefixTags"/> (read by
    /// <see cref="RPGGame.Combat.StatusAffinity"/>) and add their effects as enemy affixes (<see cref="EnemyAffixRuntime.ApplyPrefixEffects"/>).
    /// </summary>
    public static class EnemyPrefixRoller
    {
        /// <summary>Prefixes this enemy could take: not boss-only, weight above 0, level and tag rules met.</summary>
        public static List<EnemyPrefixData> Eligible(Enemy enemy, IEnumerable<EnemyPrefixData> prefixes)
        {
            var tags = new HashSet<string>(enemy.Tags, StringComparer.OrdinalIgnoreCase);
            return prefixes
                .Where(p => !p.BossOnly && p.Weight > 0 && enemy.Level >= p.MinLevel)
                .Where(p => p.RequiresTags.All(tags.Contains))
                .Where(p => !p.ExcludesTags.Any(tags.Contains))
                .ToList();
        }

        /// <summary>
        /// Room spawn roll: with the file's level-scaled chance, picks one eligible prefix by weight. Returns the prefix
        /// applied, or null. Never re-prefixes an enemy.
        /// </summary>
        public static EnemyPrefixData? TryApplyRoomPrefix(Enemy? enemy, Random random, double chanceMultiplier = 1.0)
        {
            if (enemy == null || enemy.Prefix != null)
                return null;
            var file = EnemyPrefixLoader.Get();
            double chance = Math.Min(1.0, file.RoomRoll.ChanceAt(enemy.Level) * Math.Max(0.0, chanceMultiplier));
            if (file.Prefixes.Count == 0 || random.NextDouble() >= chance)
                return null;
            var eligible = Eligible(enemy, file.Prefixes);
            double total = eligible.Sum(p => p.Weight);
            if (total <= 0)
                return null;
            double roll = random.NextDouble() * total;
            foreach (var p in eligible)
            {
                roll -= p.Weight;
                if (roll < 0)
                {
                    enemy.ApplyPrefix(p.Name, p.Tags);
                    EnemyAffixRuntime.ApplyPrefixEffects(enemy, p);
                    return p;
                }
            }
            var last = eligible[^1];
            enemy.ApplyPrefix(last.Name, last.Tags);
            EnemyAffixRuntime.ApplyPrefixEffects(enemy, last);
            return last;
        }

        /// <summary>The final boss always carries the boss prefix; it replaces any room prefix it rolled.</summary>
        public static void ApplyBossPrefix(Enemy? enemy)
        {
            if (enemy == null)
                return;
            var prefix = EnemyPrefixLoader.Find(EnemyPrefixLoader.Get().BossPrefix);
            if (prefix != null)
                enemy.ApplyPrefix(prefix.Name, prefix.Tags);
        }
    }
}
