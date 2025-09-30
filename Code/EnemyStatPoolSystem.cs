using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Stat pool system for consistent enemy balance between DPS and survivability
    /// </summary>
    public static class EnemyStatPoolSystem
    {
        /// <summary>
        /// Configuration for stat pool distribution
        /// </summary>
        public class StatPoolConfig
        {
            // Base values
            public int BaseTotalPower { get; set; } = 20; // Base total power at level 1
            public double PowerPerLevel { get; set; } = 3.0; // Additional power per level
            
            // Archetype-specific configurations
            public Dictionary<EnemyArchetype, ArchetypeStatConfig> ArchetypeConfigs { get; set; } = new();
        }

        /// <summary>
        /// Archetype-specific stat distribution configuration with two-layer allocation
        /// </summary>
        public class ArchetypeStatConfig
        {
            // Layer 1: DPS vs SUSTAIN allocation
            public double DPSPoolRatio { get; set; } = 0.5; // % of total power for DPS
            public double SUSTAINPoolRatio { get; set; } = 0.5; // % of total power for SUSTAIN
            
            // Layer 2a: DPS sub-allocation (Attack vs Attack Speed)
            public double DPSAttackRatio { get; set; } = 0.6; // % of DPS pool to Attack
            public double DPSAttackSpeedRatio { get; set; } = 0.4; // % of DPS pool to Attack Speed
            
            // Layer 2b: SUSTAIN sub-allocation (Health vs Armor)
            public double SUSTAINHealthRatio { get; set; } = 0.7; // % of SUSTAIN pool to Health
            public double SUSTAINArmorRatio { get; set; } = 0.3; // % of SUSTAIN pool to Armor
        }

        /// <summary>
        /// Calculates total power budget for an enemy at a given level
        /// </summary>
        public static double CalculateTotalPower(int level, StatPoolConfig? config = null)
        {
            config ??= new StatPoolConfig();
            return config.BaseTotalPower + (level * config.PowerPerLevel);
        }

        /// <summary>
        /// Distributes stat pools for an enemy based on level and archetype using two-layer allocation
        /// </summary>
        public static EnemyStatDistribution DistributeStats(int level, EnemyArchetype archetype, StatPoolConfig? config = null)
        {
            config ??= GetDefaultConfig();
            double totalPower = CalculateTotalPower(level, config);
            
            // Get archetype-specific configuration
            var archetypeConfig = GetArchetypeConfig(archetype, config);
            
            // Layer 1: Allocate to DPS and SUSTAIN pools
            double dpsPool = totalPower * archetypeConfig.DPSPoolRatio;
            double sustainPool = totalPower * archetypeConfig.SUSTAINPoolRatio;
            
            // Layer 2a: Allocate DPS pool to Attack and Attack Speed
            double attackPoints = dpsPool * archetypeConfig.DPSAttackRatio;
            double attackSpeedPoints = dpsPool * archetypeConfig.DPSAttackSpeedRatio;
            
            // Layer 2b: Allocate SUSTAIN pool to Health and Armor
            double healthPoints = sustainPool * archetypeConfig.SUSTAINHealthRatio;
            double armorPoints = sustainPool * archetypeConfig.SUSTAINArmorRatio;
            
            // Convert points to final stats using tuning config
            var tuning = GameConfiguration.Instance;
            int damage = Math.Max(1, (int)Math.Round(attackPoints * tuning.EnemyBalance.StatConversionRates.DamagePerPoint));
            double attackSpeed = attackSpeedPoints * tuning.EnemyBalance.StatConversionRates.AttackSpeedPerPoint;
            int health = Math.Max(10, (int)Math.Round(healthPoints * tuning.EnemyBalance.StatConversionRates.HealthPerPoint));
            int armor = Math.Max(0, (int)Math.Round(armorPoints * tuning.EnemyBalance.StatConversionRates.ArmorPerPoint));
            
            return new EnemyStatDistribution
            {
                Level = level,
                Archetype = archetype,
                Damage = damage,
                AttackSpeed = attackSpeed,
                Health = health,
                Armor = armor,
                TotalPower = totalPower,
                DPSPool = dpsPool,
                SUSTAINPool = sustainPool,
                AttackPoints = attackPoints,
                AttackSpeedPoints = attackSpeedPoints,
                HealthPoints = healthPoints,
                ArmorPoints = armorPoints
            };
        }

        /// <summary>
        /// Validates that an enemy's stats are balanced according to the stat pool system
        /// </summary>
        public static StatPoolValidationResult ValidateEnemyStats(Enemy enemy, StatPoolConfig? config = null)
        {
            config ??= new StatPoolConfig();
            
            // Calculate expected distribution
            var expectedDistribution = DistributeStats(enemy.Level, enemy.Archetype, config);
            
            // Calculate actual power usage
            double actualDPSPower = enemy.Strength + enemy.Agility;
            double actualSurvivabilityPower = (enemy.MaxHealth / 2.0) + enemy.Armor;
            double actualTotalPower = actualDPSPower + actualSurvivabilityPower;
            
            // Calculate deviations
            double dpsDeviation = Math.Abs(actualDPSPower - expectedDistribution.DPSPool) / expectedDistribution.DPSPool * 100;
            double survivabilityDeviation = Math.Abs(actualSurvivabilityPower - expectedDistribution.SUSTAINPool) / expectedDistribution.SUSTAINPool * 100;
            double totalDeviation = Math.Abs(actualTotalPower - expectedDistribution.TotalPower) / expectedDistribution.TotalPower * 100;
            
            return new StatPoolValidationResult
            {
                EnemyName = enemy.Name ?? "Unknown",
                Level = enemy.Level,
                Archetype = enemy.Archetype,
                ExpectedDistribution = expectedDistribution,
                ActualDPSPower = actualDPSPower,
                ActualSurvivabilityPower = actualSurvivabilityPower,
                ActualTotalPower = actualTotalPower,
                DPSDeviation = dpsDeviation,
                SurvivabilityDeviation = survivabilityDeviation,
                TotalDeviation = totalDeviation,
                IsBalanced = totalDeviation <= 15.0 && dpsDeviation <= 20.0 && survivabilityDeviation <= 20.0
            };
        }

        /// <summary>
        /// Analyzes all enemies and their stat pool balance
        /// </summary>
        public static void AnalyzeEnemyStatPools(StatPoolConfig? config = null)
        {
            UIManager.WriteSystemLine("=== ENEMY STAT POOL ANALYSIS ===");
            UIManager.WriteSystemLine("");
            
            config ??= new StatPoolConfig();
            EnemyLoader.LoadEnemies();
            var enemyDataList = EnemyLoader.GetAllEnemyData();
            
            if (!enemyDataList.Any())
            {
                UIManager.WriteSystemLine("No enemy data found!");
                return;
            }
            
            UIManager.WriteSystemLine("Enemy\t\tLevel\tArchetype\tDPS Pool\tSurv Pool\tTotal\tBalance");
            UIManager.WriteSystemLine("".PadRight(85, '='));
            
            var testLevels = new[] { 1, 5, 10, 15, 20 };
            
            foreach (var enemyData in enemyDataList.Take(8))
            {
                foreach (int level in testLevels)
                {
                    var enemy = EnemyLoader.CreateEnemy(enemyData.Name, level);
                    if (enemy == null) continue;
                    
                    var validation = ValidateEnemyStats(enemy, config);
                    string balanceStatus = validation.IsBalanced ? "✓" : "⚠";
                    
                    UIManager.WriteSystemLine($"{enemyData.Name.PadRight(12)}\tL{level}\t{validation.Archetype.ToString().PadRight(8)}\t{validation.ActualDPSPower:F1}\t\t{validation.ActualSurvivabilityPower:F1}\t\t{validation.ActualTotalPower:F1}\t{balanceStatus}");
                }
                UIManager.WriteSystemLine("");
            }
            
            UIManager.WriteSystemLine("");
            UIManager.WriteSystemLine("=== STAT POOL DISTRIBUTION EXAMPLES ===");
            UIManager.WriteSystemLine("Level\tArchetype\tSTR\tAGI\tHealth\tArmor\tDPS Pool\tSurv Pool");
            UIManager.WriteSystemLine("".PadRight(85, '-'));
            
            foreach (int level in new[] { 1, 5, 10, 15 })
            {
                foreach (var archetype in Enum.GetValues<EnemyArchetype>())
                {
                    var distribution = DistributeStats(level, archetype, config);
                    UIManager.WriteSystemLine($"{level}\t{archetype.ToString().PadRight(8)}\t{distribution.Damage}\t{distribution.AttackSpeed:F1}\t{distribution.Health}\t{distribution.Armor}\t{distribution.DPSPool:F1}\t\t{distribution.SUSTAINPool:F1}");
                }
                UIManager.WriteSystemLine("");
            }
        }

        /// <summary>
        /// Creates a balanced enemy using the stat pool system with direct stats
        /// </summary>
        public static Enemy CreateBalancedEnemy(string name, int level, EnemyArchetype archetype, StatPoolConfig? config = null)
        {
            var distribution = DistributeStats(level, archetype, config);
            
            // Determine primary attribute based on archetype (for compatibility)
            PrimaryAttribute primaryAttribute = archetype switch
            {
                EnemyArchetype.Berserker => PrimaryAttribute.Agility,
                EnemyArchetype.Assassin => PrimaryAttribute.Agility,
                EnemyArchetype.Warrior => PrimaryAttribute.Strength,
                EnemyArchetype.Brute => PrimaryAttribute.Strength,
                EnemyArchetype.Juggernaut => PrimaryAttribute.Strength,
                _ => PrimaryAttribute.Strength
            };
            
            return new Enemy(
                name: name,
                level: level,
                maxHealth: distribution.Health,
                damage: distribution.Damage,
                armor: distribution.Armor,
                attackSpeed: distribution.AttackSpeed,
                primaryAttribute: primaryAttribute,
                isLiving: true,
                archetype: archetype
            );
        }

        /// <summary>
        /// Gets the default stat pool configuration with two-layer archetype-specific settings from TuningConfig
        /// </summary>
        private static StatPoolConfig GetDefaultConfig()
        {
            var config = new StatPoolConfig();
            var tuning = GameConfiguration.Instance;
            
            // Load archetype configurations from TuningConfig
            foreach (var archetype in Enum.GetValues<EnemyArchetype>())
            {
                string archetypeName = archetype.ToString();
                if (tuning.EnemyBalance.ArchetypeConfigs.TryGetValue(archetypeName, out var archetypeConfig))
                {
                    config.ArchetypeConfigs[archetype] = new ArchetypeStatConfig
                    {
                        DPSPoolRatio = archetypeConfig.DPSPoolRatio,
                        SUSTAINPoolRatio = archetypeConfig.SUSTAINPoolRatio,
                        DPSAttackRatio = archetypeConfig.DPSAttackRatio,
                        DPSAttackSpeedRatio = archetypeConfig.DPSAttackSpeedRatio,
                        SUSTAINHealthRatio = archetypeConfig.SUSTAINHealthRatio,
                        SUSTAINArmorRatio = archetypeConfig.SUSTAINArmorRatio
                    };
                }
            }
            
            return config;
        }

        /// <summary>
        /// Gets archetype-specific configuration, falling back to default if not found
        /// </summary>
        private static ArchetypeStatConfig GetArchetypeConfig(EnemyArchetype archetype, StatPoolConfig config)
        {
            if (config.ArchetypeConfigs.TryGetValue(archetype, out var archetypeConfig))
            {
                return archetypeConfig;
            }
            
            // Fallback to balanced warrior config
            return new ArchetypeStatConfig
            {
                DPSPoolRatio = 0.6,
                SUSTAINPoolRatio = 0.4,
                DPSAttackRatio = 0.6,
                DPSAttackSpeedRatio = 0.4,
                SUSTAINHealthRatio = 0.7,
                SUSTAINArmorRatio = 0.3
            };
        }
    }

    /// <summary>
    /// Represents the distribution of stats for an enemy using the two-layer system
    /// </summary>
    public class EnemyStatDistribution
    {
        public int Level { get; set; }
        public EnemyArchetype Archetype { get; set; }
        
        // Final calculated stats (no attributes needed)
        public int Damage { get; set; }        // Final damage value
        public double AttackSpeed { get; set; } // Final attack speed value
        public int Health { get; set; }        // Final health value
        public int Armor { get; set; }         // Final armor value
        
        // Power allocation tracking
        public double TotalPower { get; set; }
        public double DPSPool { get; set; }
        public double SUSTAINPool { get; set; }
        
        // Sub-allocation tracking for analysis
        public double AttackPoints { get; set; }
        public double AttackSpeedPoints { get; set; }
        public double HealthPoints { get; set; }
        public double ArmorPoints { get; set; }
    }

    /// <summary>
    /// Result of stat pool validation
    /// </summary>
    public class StatPoolValidationResult
    {
        public string EnemyName { get; set; } = "";
        public int Level { get; set; }
        public EnemyArchetype Archetype { get; set; }
        public EnemyStatDistribution ExpectedDistribution { get; set; } = new();
        public double ActualDPSPower { get; set; }
        public double ActualSurvivabilityPower { get; set; }
        public double ActualTotalPower { get; set; }
        public double DPSDeviation { get; set; }
        public double SurvivabilityDeviation { get; set; }
        public double TotalDeviation { get; set; }
        public bool IsBalanced { get; set; }
    }
}
