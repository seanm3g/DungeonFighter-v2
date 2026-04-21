using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Factory for creating Enemy instances from EnemyData
    /// </summary>
    public static class EnemyDataFactory
    {
        /// <summary>
        /// Creates an enemy from enemy data
        /// </summary>
        public static Enemy? CreateEnemyFromData(EnemyData data, int level)
        {
            var tuning = GameConfiguration.Instance;

            if (!string.IsNullOrEmpty(data.Archetype) && tuning.EnemySystem != null &&
                IsValidArchetype(data.Archetype))
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
            var enemyArchetype = ConvertStringToEnemyArchetype(data.Archetype);

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

            if (data.ColorOverride != null)
            {
                var colorOverrideProperty = typeof(Enemy).GetProperty("ColorOverride", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (colorOverrideProperty != null && colorOverrideProperty.CanWrite)
                    colorOverrideProperty.SetValue(enemy, data.ColorOverride);
            }

            return enemy;
        }

        private static bool IsValidArchetype(string archetypeString)
        {
            return archetypeString.ToLower() switch
            {
                "berserker" => true,
                "guardian" => true,
                "assassin" => true,
                "brute" => true,
                "mage" => true,
                _ => false
            };
        }

        private static EnemyArchetype ConvertStringToEnemyArchetype(string archetypeString)
        {
            return archetypeString.ToLower() switch
            {
                "berserker" => EnemyArchetype.Berserker,
                "guardian" => EnemyArchetype.Guardian,
                "assassin" => EnemyArchetype.Assassin,
                "brute" => EnemyArchetype.Brute,
                "mage" => EnemyArchetype.Mage,
                _ => EnemyArchetype.Berserker
            };
        }
    }
}
