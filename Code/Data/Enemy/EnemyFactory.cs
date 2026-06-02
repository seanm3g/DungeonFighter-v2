using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;
using RPGGame.World.Tags;

namespace RPGGame
{
    /// <summary>
    /// Factory for creating Enemy instances from EnemyData
    /// </summary>
    public static class EnemyDataFactory
    {
        public static Enemy? CreateEnemyFromData(EnemyData data, int level)
        {
            var tuning = GameConfiguration.Instance;

            if (!string.IsNullOrEmpty(data.Archetype) && tuning.EnemySystem != null &&
                TagDefinitions.IsValidEnemyArchetype(data.Archetype))
            {
                return CreateEnemyWithNewSystem(data, level, tuning);
            }

            UIManager.WriteSystemLine($"Warning: Enemy '{data.Name}' has invalid archetype '{data.Archetype}'. Using default Berserker archetype.");
            data.Archetype = "Berserker";
            return CreateEnemyWithNewSystem(data, level, tuning);
        }

        private static Enemy CreateEnemyWithNewSystem(EnemyData data, int level, GameConfiguration tuning)
        {
            var stats = EnemyStatCalculator.CalculateStats(data, level, tuning.EnemySystem);
            TagDefinitions.TryParseEnemyArchetype(data.Archetype, out var enemyArchetype);

            var enemy = new Enemy(
                data.Name,
                level,
                stats.Health,
                stats.Strength,
                stats.Agility,
                stats.Technique,
                stats.Intelligence,
                stats.Armor,
                stats.PrimaryAttribute,
                data.IsLiving,
                enemyArchetype);

            enemy.SetTags(BuildRuntimeTags(data));

            if (data.ColorOverride != null)
            {
                var colorOverrideProperty = typeof(Enemy).GetProperty("ColorOverride", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (colorOverrideProperty != null && colorOverrideProperty.CanWrite)
                    colorOverrideProperty.SetValue(enemy, data.ColorOverride);
            }

            EnemyRarityRewardHelper.ApplyToEnemy(enemy, data.Rarity);

            return enemy;
        }

        internal static List<string> BuildRuntimeTags(EnemyData data)
        {
            var tags = GameDataTagHelper.NormalizeDistinct(data.Tags);
            var substance = data.IsLiving ? "living" : "undead";
            if (!GameDataTagHelper.HasTag(tags, substance))
                tags.Add(substance);
            return tags;
        }
    }
}
