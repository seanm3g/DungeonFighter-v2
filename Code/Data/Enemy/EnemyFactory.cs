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
            
            // Use the new unified enemy system
            if (!string.IsNullOrEmpty(data.Archetype) && tuning.EnemySystem != null && 
                IsValidArchetype(data.Archetype))
            {
                return CreateEnemyWithNewSystem(data, level, tuning);
            }
            else
            {
                // If no valid archetype, create a basic enemy with default stats
                UIManager.WriteSystemLine($"Warning: Enemy '{data.Name}' has invalid archetype '{data.Archetype}'. Using default Berserker archetype.");
                data.Archetype = "Berserker";
                return CreateEnemyWithNewSystem(data, level, tuning);
            }
        }

        private static Enemy CreateEnemyWithNewSystem(EnemyData data, int level, GameConfiguration tuning)
        {
            // Use the new unified EnemySystem configuration
            var enemySystem = tuning.EnemySystem;
            
            // 1. Start with baseline stats
            var baseline = enemySystem.BaselineStats;
            var scaling = enemySystem.ScalingPerLevel;
            var global = enemySystem.GlobalMultipliers;
            
            // 2. Apply archetype multipliers
            var archetype = enemySystem.Archetypes.GetValueOrDefault(data.Archetype);
            if (archetype == null)
            {
                // Fallback to Berserker archetype if not found
                archetype = enemySystem.Archetypes.GetValueOrDefault("Berserker") ?? new ArchetypeMultipliersConfig();
            }
            
            // 3. Apply individual enemy overrides
            var overrides = data.Overrides ?? new StatOverridesConfig();
            
            // 4. Calculate base stats with archetype multipliers and overrides
            var baseHealth = (int)(baseline.Health * archetype.Health * (overrides.Health ?? 1.0));
            var baseStrength = (int)(baseline.Strength * archetype.Strength * (overrides.Strength ?? 1.0));
            var baseAgility = (int)(baseline.Agility * archetype.Agility * (overrides.Agility ?? 1.0));
            var baseTechnique = (int)(baseline.Technique * archetype.Technique * (overrides.Technique ?? 1.0));
            var baseIntelligence = (int)(baseline.Intelligence * archetype.Intelligence * (overrides.Intelligence ?? 1.0));
            var baseArmor = (int)(baseline.Armor * archetype.Armor * (overrides.Armor ?? 1.0));
            
            // 5. Scale by level
            var levelScaledStats = new
            {
                Health = baseHealth + (level - 1) * scaling.Health,
                Strength = baseStrength + (level - 1) * scaling.Attributes,
                Agility = baseAgility + (level - 1) * scaling.Attributes,
                Technique = baseTechnique + (level - 1) * scaling.Attributes,
                Intelligence = baseIntelligence + (level - 1) * scaling.Attributes,
                Armor = (int)(baseArmor + (level - 1) * scaling.Armor)
            };
            
            // 6. Apply global multipliers
            var finalStats = new
            {
                Health = (int)(levelScaledStats.Health * global.HealthMultiplier),
                Strength = (int)(levelScaledStats.Strength * global.DamageMultiplier),
                Agility = (int)(levelScaledStats.Agility * global.SpeedMultiplier),
                Technique = (int)(levelScaledStats.Technique * global.DamageMultiplier),
                Intelligence = (int)(levelScaledStats.Intelligence * global.DamageMultiplier),
                Armor = (int)(levelScaledStats.Armor * global.ArmorMultiplier)
            };
            
            // Determine primary attribute based on highest stat
            var primaryAttribute = DeterminePrimaryAttribute(finalStats.Strength, finalStats.Agility, finalStats.Technique, finalStats.Intelligence);
            
            // Convert archetype string to enum
            var enemyArchetype = ConvertStringToEnemyArchetype(data.Archetype);
            
            var enemy = new Enemy(data.Name, level, finalStats.Health, finalStats.Strength, finalStats.Agility, finalStats.Technique, finalStats.Intelligence, finalStats.Armor, primaryAttribute, data.IsLiving, enemyArchetype);
            
            return enemy;
        }

        private static PrimaryAttribute DeterminePrimaryAttribute(int strength, int agility, int technique, int intelligence)
        {
            var stats = new[] { (strength, PrimaryAttribute.Strength), (agility, PrimaryAttribute.Agility), (technique, PrimaryAttribute.Technique), (intelligence, PrimaryAttribute.Intelligence) };
            return stats.OrderByDescending(s => s.Item1).First().Item2;
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

