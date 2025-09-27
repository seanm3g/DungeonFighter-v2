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
        /// Archetype-specific stat distribution configuration
        /// </summary>
        public class ArchetypeStatConfig
        {
            public double DPSPoolRatio { get; set; } = 0.6; // % of total power for DPS
            public double SurvivabilityPoolRatio { get; set; } = 0.4; // % of total power for survivability
            
            // DPS pool distribution
            public double DPSStrengthRatio { get; set; } = 0.7; // % of DPS pool to strength
            public double DPSAgilityRatio { get; set; } = 0.3; // % of DPS pool to agility
            
            // Survivability pool distribution
            public double SurvivabilityHealthRatio { get; set; } = 0.7; // % of survivability to health
            public double SurvivabilityArmorRatio { get; set; } = 0.3; // % of survivability to armor
            
            // Archetype flavor multipliers
            public double StrengthFlavorMultiplier { get; set; } = 1.0; // Extra strength emphasis
            public double AgilityFlavorMultiplier { get; set; } = 1.0; // Extra agility emphasis
            public double HealthFlavorMultiplier { get; set; } = 1.0; // Extra health emphasis
            public double ArmorFlavorMultiplier { get; set; } = 1.0; // Extra armor emphasis
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
        /// Distributes stat pools for an enemy based on level and archetype
        /// </summary>
        public static EnemyStatDistribution DistributeStats(int level, EnemyArchetype archetype, StatPoolConfig? config = null)
        {
            config ??= GetDefaultConfig();
            double totalPower = CalculateTotalPower(level, config);
            
            // Get archetype-specific configuration
            var archetypeConfig = GetArchetypeConfig(archetype, config);
            
            // Calculate pool sizes based on archetype
            double dpsPool = totalPower * archetypeConfig.DPSPoolRatio;
            double survivabilityPool = totalPower * archetypeConfig.SurvivabilityPoolRatio;
            
            // Distribute DPS pool with archetype-specific ratios
            double strengthPower = dpsPool * archetypeConfig.DPSStrengthRatio;
            double agilityPower = dpsPool * archetypeConfig.DPSAgilityRatio;
            
            // Distribute survivability pool with archetype-specific ratios
            double healthPower = survivabilityPool * archetypeConfig.SurvivabilityHealthRatio;
            double armorPower = survivabilityPool * archetypeConfig.SurvivabilityArmorRatio;
            
            // Apply archetype flavor multipliers for extra emphasis
            strengthPower *= archetypeConfig.StrengthFlavorMultiplier;
            agilityPower *= archetypeConfig.AgilityFlavorMultiplier;
            healthPower *= archetypeConfig.HealthFlavorMultiplier;
            armorPower *= archetypeConfig.ArmorFlavorMultiplier;
            
            // Convert power to actual stats
            int strength = Math.Max(1, (int)Math.Round(strengthPower));
            int agility = Math.Max(1, (int)Math.Round(agilityPower));
            int health = Math.Max(10, (int)Math.Round(healthPower * 2.0)); // Health scales 2x power
            int armor = Math.Max(0, (int)Math.Round(armorPower));
            
            // Set technique and intelligence based on archetype profile
            var archetypeProfile = EnemyDPSCalculator.GetArchetypeProfile(archetype);
            int technique = Math.Max(1, (int)Math.Round(archetypeProfile.AttributeWeights.GetValueOrDefault("Technique", 1.0) * 3));
            int intelligence = Math.Max(1, (int)Math.Round(archetypeProfile.AttributeWeights.GetValueOrDefault("Intelligence", 1.0) * 2));
            
            return new EnemyStatDistribution
            {
                Level = level,
                Archetype = archetype,
                Strength = strength,
                Agility = agility,
                Technique = technique,
                Intelligence = intelligence,
                Health = health,
                Armor = armor,
                TotalPower = totalPower,
                DPSPool = dpsPool,
                SurvivabilityPool = survivabilityPool
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
            double survivabilityDeviation = Math.Abs(actualSurvivabilityPower - expectedDistribution.SurvivabilityPool) / expectedDistribution.SurvivabilityPool * 100;
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
            Console.WriteLine("=== ENEMY STAT POOL ANALYSIS ===");
            Console.WriteLine();
            
            config ??= new StatPoolConfig();
            EnemyLoader.LoadEnemies();
            var enemyDataList = EnemyLoader.GetAllEnemyData();
            
            if (!enemyDataList.Any())
            {
                Console.WriteLine("No enemy data found!");
                return;
            }
            
            Console.WriteLine("Enemy\t\tLevel\tArchetype\tDPS Pool\tSurv Pool\tTotal\tBalance");
            Console.WriteLine("".PadRight(85, '='));
            
            var testLevels = new[] { 1, 5, 10, 15, 20 };
            
            foreach (var enemyData in enemyDataList.Take(8))
            {
                foreach (int level in testLevels)
                {
                    var enemy = EnemyLoader.CreateEnemy(enemyData.Name, level);
                    if (enemy == null) continue;
                    
                    var validation = ValidateEnemyStats(enemy, config);
                    string balanceStatus = validation.IsBalanced ? "✓" : "⚠";
                    
                    Console.WriteLine($"{enemyData.Name.PadRight(12)}\tL{level}\t{validation.Archetype.ToString().PadRight(8)}\t{validation.ActualDPSPower:F1}\t\t{validation.ActualSurvivabilityPower:F1}\t\t{validation.ActualTotalPower:F1}\t{balanceStatus}");
                }
                Console.WriteLine();
            }
            
            Console.WriteLine();
            Console.WriteLine("=== STAT POOL DISTRIBUTION EXAMPLES ===");
            Console.WriteLine("Level\tArchetype\tSTR\tAGI\tHealth\tArmor\tDPS Pool\tSurv Pool");
            Console.WriteLine("".PadRight(85, '-'));
            
            foreach (int level in new[] { 1, 5, 10, 15 })
            {
                foreach (var archetype in Enum.GetValues<EnemyArchetype>())
                {
                    var distribution = DistributeStats(level, archetype, config);
                    Console.WriteLine($"{level}\t{archetype.ToString().PadRight(8)}\t{distribution.Strength}\t{distribution.Agility}\t{distribution.Health}\t{distribution.Armor}\t{distribution.DPSPool:F1}\t\t{distribution.SurvivabilityPool:F1}");
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Creates a balanced enemy using the stat pool system
        /// </summary>
        public static Enemy CreateBalancedEnemy(string name, int level, EnemyArchetype archetype, StatPoolConfig? config = null)
        {
            var distribution = DistributeStats(level, archetype, config);
            
            // Determine primary attribute based on archetype
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
                strength: distribution.Strength,
                agility: distribution.Agility,
                technique: distribution.Technique,
                intelligence: distribution.Intelligence,
                armor: distribution.Armor,
                primaryAttribute: primaryAttribute,
                isLiving: true,
                archetype: archetype
            );
        }

        /// <summary>
        /// Gets the default stat pool configuration with archetype-specific settings
        /// </summary>
        private static StatPoolConfig GetDefaultConfig()
        {
            var config = new StatPoolConfig();
            
            // Berserker: High DPS, low survivability, agility-focused
            config.ArchetypeConfigs[EnemyArchetype.Berserker] = new ArchetypeStatConfig
            {
                DPSPoolRatio = 0.75, // 75% DPS, 25% survivability
                SurvivabilityPoolRatio = 0.25,
                DPSStrengthRatio = 0.3, // Low strength
                DPSAgilityRatio = 0.7, // High agility
                SurvivabilityHealthRatio = 0.8, // Mostly health
                SurvivabilityArmorRatio = 0.2, // Low armor
                AgilityFlavorMultiplier = 1.3, // Extra agility emphasis
                HealthFlavorMultiplier = 0.8 // Less health emphasis
            };
            
            // Assassin: Balanced DPS, moderate survivability, technique-focused
            config.ArchetypeConfigs[EnemyArchetype.Assassin] = new ArchetypeStatConfig
            {
                DPSPoolRatio = 0.65, // 65% DPS, 35% survivability
                SurvivabilityPoolRatio = 0.35,
                DPSStrengthRatio = 0.4, // Moderate strength
                DPSAgilityRatio = 0.6, // High agility
                SurvivabilityHealthRatio = 0.6, // Balanced health/armor
                SurvivabilityArmorRatio = 0.4,
                AgilityFlavorMultiplier = 1.2, // Extra agility emphasis
                ArmorFlavorMultiplier = 1.1 // Slight armor emphasis
            };
            
            // Warrior: Balanced everything
            config.ArchetypeConfigs[EnemyArchetype.Warrior] = new ArchetypeStatConfig
            {
                DPSPoolRatio = 0.6, // 60% DPS, 40% survivability
                SurvivabilityPoolRatio = 0.4,
                DPSStrengthRatio = 0.6, // Moderate strength
                DPSAgilityRatio = 0.4, // Moderate agility
                SurvivabilityHealthRatio = 0.7, // Slightly more health
                SurvivabilityArmorRatio = 0.3, // Slightly less armor
                StrengthFlavorMultiplier = 1.1, // Slight strength emphasis
                HealthFlavorMultiplier = 1.1 // Slight health emphasis
            };
            
            // Brute: High DPS, moderate survivability, strength-focused
            config.ArchetypeConfigs[EnemyArchetype.Brute] = new ArchetypeStatConfig
            {
                DPSPoolRatio = 0.7, // 70% DPS, 30% survivability
                SurvivabilityPoolRatio = 0.3,
                DPSStrengthRatio = 0.8, // High strength
                DPSAgilityRatio = 0.2, // Low agility
                SurvivabilityHealthRatio = 0.6, // Balanced health/armor
                SurvivabilityArmorRatio = 0.4,
                StrengthFlavorMultiplier = 1.3, // Extra strength emphasis
                ArmorFlavorMultiplier = 1.2 // Extra armor emphasis
            };
            
            // Juggernaut: Moderate DPS, very high survivability, tank-focused
            config.ArchetypeConfigs[EnemyArchetype.Juggernaut] = new ArchetypeStatConfig
            {
                DPSPoolRatio = 0.45, // 45% DPS, 55% survivability
                SurvivabilityPoolRatio = 0.55,
                DPSStrengthRatio = 0.7, // High strength
                DPSAgilityRatio = 0.3, // Low agility
                SurvivabilityHealthRatio = 0.5, // Balanced health/armor
                SurvivabilityArmorRatio = 0.5,
                StrengthFlavorMultiplier = 1.2, // Extra strength emphasis
                HealthFlavorMultiplier = 1.3, // Extra health emphasis
                ArmorFlavorMultiplier = 1.4 // Extra armor emphasis
            };
            
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
                SurvivabilityPoolRatio = 0.4,
                DPSStrengthRatio = 0.6,
                DPSAgilityRatio = 0.4,
                SurvivabilityHealthRatio = 0.7,
                SurvivabilityArmorRatio = 0.3
            };
        }
    }

    /// <summary>
    /// Represents the distribution of stats for an enemy
    /// </summary>
    public class EnemyStatDistribution
    {
        public int Level { get; set; }
        public EnemyArchetype Archetype { get; set; }
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Technique { get; set; }
        public int Intelligence { get; set; }
        public int Health { get; set; }
        public int Armor { get; set; }
        public double TotalPower { get; set; }
        public double DPSPool { get; set; }
        public double SurvivabilityPool { get; set; }
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
