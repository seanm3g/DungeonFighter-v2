using System;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Helper class for enemy stat calculations (same rules as <see cref="EnemyDataFactory"/>).
    /// </summary>
    public static class EnemyStatCalculator
    {
        /// <summary>Total STR+AGI+TECH+INT growth budget per level after resolving partial sheet data.</summary>
        public const double EnemyAttributeGrowthBudgetPerLevel = 6.0;

        /// <summary>
        /// Calculates final enemy stats from <see cref="EnemyData.BaseAttributes"/> / <see cref="EnemyData.GrowthPerLevel"/>,
        /// <see cref="EnemyData.BaseHealth"/> / <see cref="EnemyData.HealthGrowthPerLevel"/>, and tuning baselines.
        /// Attribute growth is normalized so STR+AGI+TECH+INT growth sums to <see cref="EnemyAttributeGrowthBudgetPerLevel"/> per level.
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

            double baseHealth = enemyData.BaseHealth ?? (baseline.Health * archetype.Health);
            double baseStrength = enemyData.BaseAttributes?.Strength ?? (baseline.Strength * archetype.Strength);
            double baseAgility = enemyData.BaseAttributes?.Agility ?? (baseline.Agility * archetype.Agility);
            double baseTechnique = enemyData.BaseAttributes?.Technique ?? (baseline.Technique * archetype.Technique);
            double baseIntelligence = enemyData.BaseAttributes?.Intelligence ?? (baseline.Intelligence * archetype.Intelligence);
            double baseArmor = baseline.Armor * archetype.Armor;

            var (growthStrength, growthAgility, growthTechnique, growthIntelligence) =
                ComputeNormalizedAttributeGrowthPerLevel(enemyData.GrowthPerLevel);

            double growthHealth = enemyData.HealthGrowthPerLevel ?? scaling.Health;

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

        /// <summary>
        /// Resolves partial growth columns: missing stats share the remainder of the 6-point budget; explicit rows scale to sum 6.
        /// </summary>
        internal static (double strength, double agility, double technique, double intelligence)
            ComputeNormalizedAttributeGrowthPerLevel(EnemyAttributeSet? growth)
        {
            const double B = EnemyAttributeGrowthBudgetPerLevel;
            bool hs = growth?.Strength.HasValue == true;
            bool ha = growth?.Agility.HasValue == true;
            bool ht = growth?.Technique.HasValue == true;
            bool hi = growth?.Intelligence.HasValue == true;
            int set = (hs ? 1 : 0) + (ha ? 1 : 0) + (ht ? 1 : 0) + (hi ? 1 : 0);
            int nullCount = 4 - set;

            double s = growth?.Strength ?? 0;
            double a = growth?.Agility ?? 0;
            double t = growth?.Technique ?? 0;
            double i = growth?.Intelligence ?? 0;

            if (set == 0)
            {
                double v = B / 4.0;
                return (v, v, v, v);
            }

            double sumExplicit = (hs ? s : 0) + (ha ? a : 0) + (ht ? t : 0) + (hi ? i : 0);
            if (sumExplicit < 1e-9)
            {
                double v = B / 4.0;
                return (v, v, v, v);
            }

            if (nullCount > 0)
            {
                if (sumExplicit <= B + 1e-9)
                {
                    double fill = (B - sumExplicit) / nullCount;
                    return (hs ? s : fill, ha ? a : fill, ht ? t : fill, hi ? i : fill);
                }

                double k = B / sumExplicit;
                return (hs ? s * k : 0, ha ? a * k : 0, ht ? t * k : 0, hi ? i * k : 0);
            }

            if (Math.Abs(sumExplicit - B) < 1e-9)
                return (s, a, t, i);

            double kn = B / sumExplicit;
            return (s * kn, a * kn, t * kn, i * kn);
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
