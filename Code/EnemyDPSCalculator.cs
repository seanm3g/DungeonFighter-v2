using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Defines different enemy attack archetypes based on DPS distribution
    /// </summary>
    public enum EnemyArchetype
    {
        Berserker,    // Very fast, low damage (high speed, low damage per hit)
        Assassin,     // Fast, moderate damage (moderate speed, moderate damage)
        Warrior,      // Balanced speed and damage (average speed, average damage)
        Brute,        // Slow, high damage (low speed, high damage per hit)
        Juggernaut    // Very slow, very high damage (very low speed, very high damage)
    }

    /// <summary>
    /// Configuration for enemy attack patterns
    /// </summary>
    public class EnemyAttackProfile
    {
        public EnemyArchetype Archetype { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        
        // Speed multiplier (affects attack time - lower = faster attacks)
        public double SpeedMultiplier { get; set; } = 1.0;
        
        // Damage multiplier (affects base damage calculation)
        public double DamageMultiplier { get; set; } = 1.0;
        
        // Attribute focus (which attributes to emphasize)
        public Dictionary<string, double> AttributeWeights { get; set; } = new();
        
        // Target DPS at level 1 (for balancing purposes)
        public double TargetDPSAtLevel1 { get; set; } = 5.0;
    }

    /// <summary>
    /// Calculates and manages DPS-based enemy configurations
    /// </summary>
    public static class EnemyDPSCalculator
    {
        private static readonly Dictionary<EnemyArchetype, EnemyAttackProfile> _archetypes = new()
        {
            {
                EnemyArchetype.Berserker,
                new EnemyAttackProfile
                {
                    Archetype = EnemyArchetype.Berserker,
                    Name = "Berserker",
                    Description = "Lightning-fast attacks with low damage per hit",
                    SpeedMultiplier = 0.6,  // 40% faster attacks
                    DamageMultiplier = 0.7, // 30% less damage per hit
                    AttributeWeights = new() { ["Agility"] = 2.0, ["Strength"] = 0.5 },
                    TargetDPSAtLevel1 = 5.0
                }
            },
            {
                EnemyArchetype.Assassin,
                new EnemyAttackProfile
                {
                    Archetype = EnemyArchetype.Assassin,
                    Name = "Assassin",
                    Description = "Quick strikes with moderate damage",
                    SpeedMultiplier = 0.8,  // 20% faster attacks
                    DamageMultiplier = 0.85, // 15% less damage per hit
                    AttributeWeights = new() { ["Agility"] = 1.5, ["Technique"] = 1.2, ["Strength"] = 0.8 },
                    TargetDPSAtLevel1 = 5.2
                }
            },
            {
                EnemyArchetype.Warrior,
                new EnemyAttackProfile
                {
                    Archetype = EnemyArchetype.Warrior,
                    Name = "Warrior",
                    Description = "Balanced attack speed and damage",
                    SpeedMultiplier = 1.0,  // Normal speed
                    DamageMultiplier = 1.0, // Normal damage
                    AttributeWeights = new() { ["Strength"] = 1.2, ["Agility"] = 1.0, ["Technique"] = 1.0 },
                    TargetDPSAtLevel1 = 5.0
                }
            },
            {
                EnemyArchetype.Brute,
                new EnemyAttackProfile
                {
                    Archetype = EnemyArchetype.Brute,
                    Name = "Brute",
                    Description = "Slow but powerful attacks",
                    SpeedMultiplier = 1.3,  // 30% slower attacks
                    DamageMultiplier = 1.4, // 40% more damage per hit
                    AttributeWeights = new() { ["Strength"] = 2.0, ["Agility"] = 0.6 },
                    TargetDPSAtLevel1 = 5.1
                }
            },
            {
                EnemyArchetype.Juggernaut,
                new EnemyAttackProfile
                {
                    Archetype = EnemyArchetype.Juggernaut,
                    Name = "Juggernaut",
                    Description = "Devastating attacks with long wind-up",
                    SpeedMultiplier = 1.6,  // 60% slower attacks
                    DamageMultiplier = 1.8, // 80% more damage per hit
                    AttributeWeights = new() { ["Strength"] = 2.5, ["Technique"] = 1.2, ["Agility"] = 0.4 },
                    TargetDPSAtLevel1 = 5.3
                }
            }
        };

        /// <summary>
        /// Gets the attack profile for a specific archetype
        /// </summary>
        public static EnemyAttackProfile GetArchetypeProfile(EnemyArchetype archetype)
        {
            return _archetypes.TryGetValue(archetype, out var profile) ? profile : _archetypes[EnemyArchetype.Warrior];
        }

        /// <summary>
        /// Calculates the theoretical DPS for an enemy given their stats and archetype
        /// </summary>
        public static double CalculateEnemyDPS(Enemy enemy, EnemyArchetype archetype)
        {
            var profile = GetArchetypeProfile(archetype);
            
            // Calculate base damage using the same formula as Combat.CalculateDamage for enemies
            int strength = enemy.Strength;
            int highestAttribute = strength; // For enemies, typically use strength as highest
            int levelDamage = enemy.Level * 0; // Level-based damage scaling (handled by DPS system)
            
            // Calculate base damage: STR + highest attribute + level scaling
            int baseDamage = strength + highestAttribute + levelDamage;
            
            // Apply archetype damage multiplier
            double adjustedDamage = baseDamage * profile.DamageMultiplier;
            
            // Calculate attack speed using the same logic as Character.GetTotalAttackSpeed
            var tuning = TuningConfig.Instance;
            double baseAttackTime = tuning.Combat.BaseAttackTime;
            
            // Agility reduces attack time (makes you faster)
            double agilityReduction = enemy.Agility * tuning.Combat.AgilitySpeedReduction;
            double agilityAdjustedTime = baseAttackTime - agilityReduction;
            
            // Apply archetype speed multiplier (0.9 = 10% faster, 1.1 = 10% slower)
            double finalAttackTime = agilityAdjustedTime * profile.SpeedMultiplier;
            
            // Apply minimum cap
            finalAttackTime = Math.Max(tuning.Combat.MinimumAttackTime, finalAttackTime);
            
            // Calculate DPS: damage per hit / time between hits
            return adjustedDamage / finalAttackTime;
        }

        /// <summary>
        /// Suggests the best archetype for an enemy based on their base stats
        /// </summary>
        public static EnemyArchetype SuggestArchetypeForEnemy(string enemyName, int baseStrength, int baseAgility, int baseTechnique, int baseIntelligence)
        {
            // Calculate stat ratios to determine natural archetype
            int totalStats = baseStrength + baseAgility + baseTechnique + baseIntelligence;
            double strengthRatio = (double)baseStrength / totalStats;
            double agilityRatio = (double)baseAgility / totalStats;
            double techniqueRatio = (double)baseTechnique / totalStats;
            
            // Determine archetype based on stat distribution
            if (agilityRatio >= 0.4) // High agility
            {
                return strengthRatio >= 0.25 ? EnemyArchetype.Assassin : EnemyArchetype.Berserker;
            }
            else if (strengthRatio >= 0.4) // High strength
            {
                return agilityRatio <= 0.15 ? EnemyArchetype.Juggernaut : EnemyArchetype.Brute;
            }
            else if (techniqueRatio >= 0.3) // Decent technique
            {
                return EnemyArchetype.Assassin;
            }
            
            // Default to balanced
            return EnemyArchetype.Warrior;
        }

        /// <summary>
        /// Calculates optimal stats for an enemy to achieve target DPS at a given level
        /// </summary>
        public static (int strength, int agility, int technique, int intelligence) CalculateOptimalStatsForDPS(
            EnemyArchetype archetype, int level, double targetDPS)
        {
            var profile = GetArchetypeProfile(archetype);
            var tuning = TuningConfig.Instance;
            
            // Start with base stats scaled by level
            int baseStatPerLevel = tuning.Attributes.EnemyAttributesPerLevel;
            int primaryBonus = tuning.Attributes.EnemyPrimaryAttributeBonus;
            
            // Calculate base stats
            int baseStrength = 3 + (level * baseStatPerLevel);
            int baseAgility = 2 + (level * baseStatPerLevel);
            int baseTechnique = 1 + (level * baseStatPerLevel);
            int baseIntelligence = 1 + (level * baseStatPerLevel);
            
            // Apply archetype weights
            int strength = baseStrength + (int)(level * primaryBonus * profile.AttributeWeights.GetValueOrDefault("Strength", 1.0));
            int agility = baseAgility + (int)(level * primaryBonus * profile.AttributeWeights.GetValueOrDefault("Agility", 1.0));
            int technique = baseTechnique + (int)(level * primaryBonus * profile.AttributeWeights.GetValueOrDefault("Technique", 1.0));
            int intelligence = baseIntelligence + (int)(level * primaryBonus * profile.AttributeWeights.GetValueOrDefault("Intelligence", 1.0));
            
            return (strength, agility, technique, intelligence);
        }

        /// <summary>
        /// Analyzes all enemies and their DPS characteristics
        /// </summary>
        public static void AnalyzeEnemyDPS(int sampleSize = 100)
        {
            Console.WriteLine("=== ENEMY DPS ANALYSIS ===");
            Console.WriteLine();
            
            // Load enemy data
            EnemyLoader.LoadEnemies();
            var enemyDataList = EnemyLoader.GetAllEnemyData();
            
            if (enemyDataList == null || !enemyDataList.Any())
            {
                Console.WriteLine("No enemy data found!");
                return;
            }
            
            Console.WriteLine("Enemy\t\tLevel\tArchetype\tDPS\tSpeed\tDamage\tBalance");
            Console.WriteLine("".PadRight(80, '='));
            
            var testLevels = new[] { 1, 5, 10, 20, 30 };
            
            foreach (var enemyData in enemyDataList.Take(10)) // Analyze first 10 enemies
            {
                foreach (int level in testLevels)
                {
                    // Create enemy at test level
                    var enemy = EnemyLoader.CreateEnemy(enemyData.Name, level);
                    if (enemy == null) continue;
                    
                    // Suggest archetype based on stats
                    var suggestedArchetype = SuggestArchetypeForEnemy(
                        enemy.Name ?? "Unknown", 
                        enemy.Strength, 
                        enemy.Agility, 
                        enemy.Technique, 
                        enemy.Intelligence
                    );
                    
                    // Calculate DPS for suggested archetype
                    double dps = CalculateEnemyDPS(enemy, suggestedArchetype);
                    double attackSpeed = enemy.GetTotalAttackSpeed();
                    
                    // Calculate average damage per hit
                    int baseDamage = enemy.Strength + enemy.Strength; // Simplified calculation
                    var profile = GetArchetypeProfile(suggestedArchetype);
                    double avgDamage = baseDamage * profile.DamageMultiplier;
                    
                    // Balance score (how close to target DPS)
                    double targetDPS = profile.TargetDPSAtLevel1 * level;
                    double balanceScore = Math.Abs(dps - targetDPS) / targetDPS * 100;
                    
                    Console.WriteLine($"{enemy.Name?.PadRight(12)}\tL{level}\t{suggestedArchetype.ToString().PadRight(10)}\t{dps:F1}\t{attackSpeed:F1}s\t{avgDamage:F1}\t{balanceScore:F0}%");
                }
                Console.WriteLine(); // Blank line between enemies
            }
            
            Console.WriteLine();
            Console.WriteLine("=== ARCHETYPE PROFILES ===");
            foreach (var archetype in _archetypes.Values)
            {
                Console.WriteLine($"{archetype.Name}: {archetype.Description}");
                Console.WriteLine($"  Speed: {archetype.SpeedMultiplier:F1}x, Damage: {archetype.DamageMultiplier:F1}x, Target DPS: {archetype.TargetDPSAtLevel1:F1}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Gets all available archetypes
        /// </summary>
        public static IEnumerable<EnemyArchetype> GetAllArchetypes()
        {
            return _archetypes.Keys;
        }

        /// <summary>
        /// Tests DPS calculations with different configurations
        /// </summary>
        public static void TestDPSCalculations()
        {
            Console.WriteLine("=== DPS CALCULATION TEST ===");
            Console.WriteLine();
            
            // Create test enemy
            var testEnemy = new Enemy("Test Enemy", level: 5, maxHealth: 50, strength: 10, agility: 8, technique: 6, intelligence: 4);
            
            Console.WriteLine($"Test Enemy Stats: STR {testEnemy.Strength}, AGI {testEnemy.Agility}, TEC {testEnemy.Technique}, INT {testEnemy.Intelligence}");
            Console.WriteLine();
            
            Console.WriteLine("Archetype\tDPS\tAttack Time\tDamage/Hit\tHits/10s");
            Console.WriteLine("".PadRight(60, '-'));
            
            foreach (var archetype in GetAllArchetypes())
            {
                double dps = CalculateEnemyDPS(testEnemy, archetype);
                var profile = GetArchetypeProfile(archetype);
                
                // Calculate attack time and damage per hit
                var tuning = TuningConfig.Instance;
                double baseAttackTime = tuning.Combat.BaseAttackTime;
                double agilityReduction = testEnemy.Agility * tuning.Combat.AgilitySpeedReduction;
                double attackTime = Math.Max(tuning.Combat.MinimumAttackTime, 
                    (baseAttackTime - agilityReduction) * profile.SpeedMultiplier);
                
                int baseDamage = testEnemy.Strength + testEnemy.Strength;
                double damagePerHit = baseDamage * profile.DamageMultiplier;
                double hitsPerTenSeconds = 10.0 / attackTime;
                
                Console.WriteLine($"{archetype.ToString().PadRight(12)}\t{dps:F1}\t{attackTime:F1}s\t\t{damagePerHit:F1}\t\t{hitsPerTenSeconds:F1}");
            }
        }
    }
}
