using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Helper class for enemy stat calculations
    /// </summary>
    public static class EnemyStatCalculator
    {
        /// <summary>
        /// Calculates final enemy stats using the unified system
        /// </summary>
        /// <param name="enemyData">Enemy data from JSON</param>
        /// <param name="level">Enemy level</param>
        /// <param name="enemySystem">Enemy system configuration</param>
        /// <returns>Calculated enemy stats</returns>
        public static CalculatedEnemyStats CalculateStats(EnemyData enemyData, int level, EnemySystemConfig enemySystem)
        {
            // 1. Start with baseline stats
            var baseline = enemySystem.BaselineStats;
            var scaling = enemySystem.ScalingPerLevel;
            var global = enemySystem.GlobalMultipliers;

            // 2. Apply archetype multipliers
            var archetype = enemySystem.Archetypes.GetValueOrDefault(enemyData.Archetype);
            if (archetype == null)
            {
                // Fallback to Berserker if archetype not found
                archetype = enemySystem.Archetypes.GetValueOrDefault("Berserker") ?? new ArchetypeMultipliersConfig();
            }

            // 3. Apply individual enemy overrides
            var overrides = enemyData.Overrides ?? new StatOverridesConfig();

            // 4. Calculate base stats with archetype multipliers and overrides
            var baseHealth = (int)(baseline.Health * archetype.Health * (overrides.Health ?? 1.0));
            var baseStrength = (int)(baseline.Strength * archetype.Strength * (overrides.Strength ?? 1.0));
            var baseAgility = (int)(baseline.Agility * archetype.Agility * (overrides.Agility ?? 1.0));
            var baseTechnique = (int)(baseline.Technique * archetype.Technique * (overrides.Technique ?? 1.0));
            var baseIntelligence = (int)(baseline.Intelligence * archetype.Intelligence * (overrides.Intelligence ?? 1.0));
            var baseArmor = (int)(baseline.Armor * archetype.Armor * (overrides.Armor ?? 1.0));

            // 5. Scale by level
            var scaledHealth = baseHealth + (level - 1) * scaling.Health;
            var scaledStrength = baseStrength + (level - 1) * scaling.Attributes;
            var scaledAgility = baseAgility + (level - 1) * scaling.Attributes;
            var scaledTechnique = baseTechnique + (level - 1) * scaling.Attributes;
            var scaledIntelligence = baseIntelligence + (level - 1) * scaling.Attributes;
            var scaledArmor = (int)(baseArmor + (level - 1) * scaling.Armor);

            // 6. Apply global multipliers
            var finalHealth = (int)(scaledHealth * global.HealthMultiplier);
            var finalStrength = (int)(scaledStrength * global.DamageMultiplier);
            var finalAgility = (int)(scaledAgility * global.SpeedMultiplier);
            var finalTechnique = (int)(scaledTechnique * global.DamageMultiplier);
            var finalIntelligence = (int)(scaledIntelligence * global.DamageMultiplier);
            var finalArmor = (int)(scaledArmor * global.ArmorMultiplier);

            // 7. Determine primary attribute
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
        /// Determines the primary attribute based on highest stat
        /// </summary>
        private static PrimaryAttribute DeterminePrimaryAttribute(int strength, int agility, int technique, int intelligence)
        {
            var stats = new[] { 
                (strength, PrimaryAttribute.Strength), 
                (agility, PrimaryAttribute.Agility), 
                (technique, PrimaryAttribute.Technique), 
                (intelligence, PrimaryAttribute.Intelligence) 
            };
            return stats.OrderByDescending(s => s.Item1).First().Item2;
        }

        /// <summary>
        /// Converts archetype string to enum
        /// </summary>
        private static EnemyArchetype ConvertStringToEnemyArchetype(string archetype)
        {
            return archetype.ToLower() switch
            {
                "berserker" => EnemyArchetype.Berserker,
                "guardian" => EnemyArchetype.Guardian,
                "assassin" => EnemyArchetype.Assassin,
                "brute" => EnemyArchetype.Brute,
                "mage" => EnemyArchetype.Mage,
                "ranger" => EnemyArchetype.Assassin, // Map to existing enum
                "tank" => EnemyArchetype.Guardian, // Map to existing enum
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

