using System.Linq;
using System;

namespace RPGGame
{
    /// <summary>
    /// Helper class for enemy stat calculations (same rules as <see cref="EnemyDataFactory"/>).
    /// </summary>
    public static class EnemyStatCalculator
    {
        /// <summary>
        /// Calculates final enemy stats: optional <see cref="EnemyData.BaseAttributes"/> / <see cref="EnemyData.GrowthPerLevel"/>
        /// for attributes; optional <see cref="EnemyData.BaseHealth"/> / <see cref="EnemyData.HealthGrowthPerLevel"/> for HP
        /// (same level formula). Otherwise falls back to baseline × archetype × overrides and tuning
        /// <see cref="ScalingPerLevelConfig"/>.
        /// </summary>
        public static CalculatedEnemyStats CalculateStats(EnemyData enemyData, int level, EnemySystemConfig enemySystem)
        {
            var baseline = enemySystem.BaselineStats;
            var scaling = enemySystem.ScalingPerLevel;
            var global = enemySystem.GlobalMultipliers;

            var archetype = enemySystem.Archetypes.GetValueOrDefault(enemyData.Archetype);
            if (archetype == null)
            {
                archetype = enemySystem.Archetypes.GetValueOrDefault("Berserker") ?? new ArchetypeMultipliersConfig();
            }

            var overrides = enemyData.Overrides ?? new StatOverridesConfig();

            double baseHealth = enemyData.BaseHealth ?? (baseline.Health * archetype.Health * (overrides.Health ?? 1.0));
            double baseStrength = enemyData.BaseAttributes?.Strength ?? (baseline.Strength * archetype.Strength * (overrides.Strength ?? 1.0));
            double baseAgility = enemyData.BaseAttributes?.Agility ?? (baseline.Agility * archetype.Agility * (overrides.Agility ?? 1.0));
            double baseTechnique = enemyData.BaseAttributes?.Technique ?? (baseline.Technique * archetype.Technique * (overrides.Technique ?? 1.0));
            double baseIntelligence = enemyData.BaseAttributes?.Intelligence ?? (baseline.Intelligence * archetype.Intelligence * (overrides.Intelligence ?? 1.0));
            double baseArmor = baseline.Armor * archetype.Armor * (overrides.Armor ?? 1.0);

            double growthStrength = enemyData.GrowthPerLevel?.Strength ?? (scaling.Attributes * (overrides.Strength ?? 1.0));
            double growthAgility = enemyData.GrowthPerLevel?.Agility ?? (scaling.Attributes * (overrides.Agility ?? 1.0));
            double growthTechnique = enemyData.GrowthPerLevel?.Technique ?? (scaling.Attributes * (overrides.Technique ?? 1.0));
            double growthIntelligence = enemyData.GrowthPerLevel?.Intelligence ?? (scaling.Attributes * (overrides.Intelligence ?? 1.0));
            double growthHealth = enemyData.HealthGrowthPerLevel ?? (scaling.Health * (overrides.Health ?? 1.0));

            int lv = Math.Max(0, level - 1);

            int levelScaledHealth = FloorToInt(baseHealth + lv * growthHealth);
            int levelScaledStrength = FloorToInt(baseStrength + lv * growthStrength);
            int levelScaledAgility = FloorToInt(baseAgility + lv * growthAgility);
            int levelScaledTechnique = FloorToInt(baseTechnique + lv * growthTechnique);
            int levelScaledIntelligence = FloorToInt(baseIntelligence + lv * growthIntelligence);
            int levelScaledArmor = FloorToInt(baseArmor + lv * scaling.Armor);

            int finalHealth = Math.Max(1, FloorToInt(levelScaledHealth * global.HealthMultiplier));
            int finalStrength = Math.Max(0, FloorToInt(levelScaledStrength * global.DamageMultiplier));
            int finalAgility = Math.Max(0, FloorToInt(levelScaledAgility * global.SpeedMultiplier));
            int finalTechnique = Math.Max(0, FloorToInt(levelScaledTechnique * global.DamageMultiplier));
            int finalIntelligence = Math.Max(0, FloorToInt(levelScaledIntelligence * global.DamageMultiplier));
            int finalArmor = Math.Max(0, FloorToInt(levelScaledArmor * global.ArmorMultiplier));

            var primaryAttribute = DeterminePrimaryAttribute(finalStrength, finalAgility, finalTechnique, finalIntelligence);

            return new CalculatedEnemyStats
            {
                Health = finalHealth,
                Strength = finalStrength,
                Agility = finalAgility,
                Technique = finalTechnique,
                Intelligence = finalIntelligence,
                Armor = finalArmor,
                PrimaryAttribute = primaryAttribute,
                Archetype = ConvertStringToEnemyArchetype(enemyData.Archetype)
            };
        }

        private static int FloorToInt(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0;
            return (int)Math.Floor(value);
        }

        private static PrimaryAttribute DeterminePrimaryAttribute(int strength, int agility, int technique, int intelligence)
        {
            var stats = new[]
            {
                (strength, PrimaryAttribute.Strength),
                (agility, PrimaryAttribute.Agility),
                (technique, PrimaryAttribute.Technique),
                (intelligence, PrimaryAttribute.Intelligence)
            };
            return stats.OrderByDescending(s => s.Item1).First().Item2;
        }

        private static EnemyArchetype ConvertStringToEnemyArchetype(string archetype)
        {
            return archetype.ToLower() switch
            {
                "berserker" => EnemyArchetype.Berserker,
                "guardian" => EnemyArchetype.Guardian,
                "assassin" => EnemyArchetype.Assassin,
                "brute" => EnemyArchetype.Brute,
                "mage" => EnemyArchetype.Mage,
                "ranger" => EnemyArchetype.Assassin,
                "tank" => EnemyArchetype.Guardian,
                _ => EnemyArchetype.Berserker
            };
        }
    }

    /// <summary>
    /// Calculated enemy stats result
    /// </summary>
    public class CalculatedEnemyStats
    {
        public int Health { get; set; }
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Technique { get; set; }
        public int Intelligence { get; set; }
        public int Armor { get; set; }
        public PrimaryAttribute PrimaryAttribute { get; set; }
        public EnemyArchetype Archetype { get; set; }
    }
}
