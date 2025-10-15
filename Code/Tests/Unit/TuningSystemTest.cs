using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Test class to demonstrate the improved tuning system
    /// </summary>
    public static class TuningSystemTest
    {
        /// <summary>
        /// Tests the improved enemy stat calculation system
        /// </summary>
        public static void TestEnemyStatCalculation()
        {
            UIManager.WriteSystemLine("=== Testing Improved Tuning System ===");
            
            // Create test enemy system configuration
            var enemySystem = new EnemySystemConfig
            {
                GlobalMultipliers = new GlobalMultipliersConfig
                {
                    HealthMultiplier = 1.0,
                    DamageMultiplier = 1.0,
                    ArmorMultiplier = 1.0,
                    SpeedMultiplier = 1.0
                },
                BaselineStats = new BaselineStatsConfig
                {
                    Health = 50,
                    Strength = 3,
                    Agility = 3,
                    Technique = 3,
                    Intelligence = 3,
                    Armor = 2
                },
                ScalingPerLevel = new ScalingPerLevelConfig
                {
                    Health = 3,
                    Attributes = 2,
                    Armor = 0.1
                },
                Archetypes = new Dictionary<string, ArchetypeMultipliersConfig>
                {
                    ["Berserker"] = new ArchetypeMultipliersConfig
                    {
                        Health = 0.8,
                        Strength = 1.5,
                        Agility = 0.9,
                        Technique = 0.9,
                        Intelligence = 0.8,
                        Armor = 0.5
                    },
                    ["Assassin"] = new ArchetypeMultipliersConfig
                    {
                        Health = 0.7,
                        Strength = 1.0,
                        Agility = 1.6,
                        Technique = 1.2,
                        Intelligence = 1.0,
                        Armor = 0.4
                    }
                }
            };

            // Test 1: Basic Berserker at Level 1
            var goblinData = new EnemyData
            {
                Name = "Goblin",
                Archetype = "Berserker",
                IsLiving = true,
                Actions = new List<string> { "JAB", "TAUNT" },
                Overrides = new StatOverridesConfig
                {
                    Health = 0.8,
                    Strength = 1.2
                }
            };

            var goblinStats = EnemyStatCalculator.CalculateStats(goblinData, 1, enemySystem);
            UIManager.WriteSystemLine($"Level 1 Goblin (Berserker): Health={goblinStats.Health}, STR={goblinStats.Strength}, AGI={goblinStats.Agility}, TEC={goblinStats.Technique}, INT={goblinStats.Intelligence}, Armor={goblinStats.Armor}");

            // Test 2: Same Goblin at Level 5
            var goblinStatsL5 = EnemyStatCalculator.CalculateStats(goblinData, 5, enemySystem);
            UIManager.WriteSystemLine($"Level 5 Goblin (Berserker): Health={goblinStatsL5.Health}, STR={goblinStatsL5.Strength}, AGI={goblinStatsL5.Agility}, TEC={goblinStatsL5.Technique}, INT={goblinStatsL5.Intelligence}, Armor={goblinStatsL5.Armor}");

            // Test 3: Assassin with no overrides
            var banditData = new EnemyData
            {
                Name = "Bandit",
                Archetype = "Assassin",
                IsLiving = true,
                Actions = new List<string> { "QUICK STRIKE", "DODGE" }
            };

            var banditStats = EnemyStatCalculator.CalculateStats(banditData, 3, enemySystem);
            UIManager.WriteSystemLine($"Level 3 Bandit (Assassin): Health={banditStats.Health}, STR={banditStats.Strength}, AGI={banditStats.Agility}, TEC={banditStats.Technique}, INT={banditStats.Intelligence}, Armor={banditStats.Armor}");

            // Test 4: Global multiplier effect
            enemySystem.GlobalMultipliers.HealthMultiplier = 1.5;
            enemySystem.GlobalMultipliers.DamageMultiplier = 1.2;
            
            var goblinStatsBoosted = EnemyStatCalculator.CalculateStats(goblinData, 1, enemySystem);
            UIManager.WriteSystemLine($"Level 1 Goblin (with 1.5x health, 1.2x damage): Health={goblinStatsBoosted.Health}, STR={goblinStatsBoosted.Strength}, AGI={goblinStatsBoosted.Agility}, TEC={goblinStatsBoosted.Technique}, INT={goblinStatsBoosted.Intelligence}, Armor={goblinStatsBoosted.Armor}");

            UIManager.WriteSystemLine("=== Tuning System Test Complete ===");
        }

        /// <summary>
        /// Demonstrates the calculation flow step by step
        /// </summary>
        public static void DemonstrateCalculationFlow()
        {
            UIManager.WriteSystemLine("=== Enemy Stat Calculation Flow Demo ===");
            
            var enemySystem = new EnemySystemConfig
            {
                BaselineStats = new BaselineStatsConfig
                {
                    Health = 50,
                    Strength = 3,
                    Agility = 3,
                    Technique = 3,
                    Intelligence = 3,
                    Armor = 2
                },
                ScalingPerLevel = new ScalingPerLevelConfig
                {
                    Health = 3,
                    Attributes = 2,
                    Armor = 0.1
                },
                Archetypes = new Dictionary<string, ArchetypeMultipliersConfig>
                {
                    ["Assassin"] = new ArchetypeMultipliersConfig
                    {
                        Health = 0.7,
                        Strength = 1.0,
                        Agility = 1.6,
                        Technique = 1.2,
                        Intelligence = 1.0,
                        Armor = 0.4
                    }
                }
            };

            var enemyData = new EnemyData
            {
                Name = "Test Assassin",
                Archetype = "Assassin",
                Overrides = new StatOverridesConfig
                {
                    Health = 0.8,
                    Agility = 1.1
                }
            };

            UIManager.WriteSystemLine("Step 1 - Baseline Stats:");
            UIManager.WriteSystemLine($"  Health: {enemySystem.BaselineStats.Health}, STR: {enemySystem.BaselineStats.Strength}, AGI: {enemySystem.BaselineStats.Agility}");

            UIManager.WriteSystemLine("Step 2 - Apply Assassin Archetype:");
            UIManager.WriteSystemLine($"  Health: {enemySystem.BaselineStats.Health} * 0.7 = {enemySystem.BaselineStats.Health * 0.7}");
            UIManager.WriteSystemLine($"  Agility: {enemySystem.BaselineStats.Agility} * 1.6 = {enemySystem.BaselineStats.Agility * 1.6}");

            UIManager.WriteSystemLine("Step 3 - Apply Individual Overrides:");
            UIManager.WriteSystemLine($"  Health: {enemySystem.BaselineStats.Health * 0.7} * 0.8 = {enemySystem.BaselineStats.Health * 0.7 * 0.8}");
            UIManager.WriteSystemLine($"  Agility: {enemySystem.BaselineStats.Agility * 1.6} * 1.1 = {enemySystem.BaselineStats.Agility * 1.6 * 1.1}");

            UIManager.WriteSystemLine("Step 4 - Scale by Level (Level 3):");
            UIManager.WriteSystemLine($"  Health: {enemySystem.BaselineStats.Health * 0.7 * 0.8} + (3-1) * {enemySystem.ScalingPerLevel.Health} = {enemySystem.BaselineStats.Health * 0.7 * 0.8 + 2 * enemySystem.ScalingPerLevel.Health}");
            UIManager.WriteSystemLine($"  Agility: {enemySystem.BaselineStats.Agility * 1.6 * 1.1} + (3-1) * {enemySystem.ScalingPerLevel.Attributes} = {enemySystem.BaselineStats.Agility * 1.6 * 1.1 + 2 * enemySystem.ScalingPerLevel.Attributes}");

            var finalStats = EnemyStatCalculator.CalculateStats(enemyData, 3, enemySystem);
            UIManager.WriteSystemLine("Final Result:");
            UIManager.WriteSystemLine($"  Health: {finalStats.Health}, STR: {finalStats.Strength}, AGI: {finalStats.Agility}, TEC: {finalStats.Technique}, INT: {finalStats.Intelligence}, Armor: {finalStats.Armor}");

            UIManager.WriteSystemLine("=== Calculation Flow Demo Complete ===");
        }

        /// <summary>
        /// Tests the actual game configuration loading and enemy creation
        /// </summary>
        public static void TestGameConfigurationIntegration()
        {
            UIManager.WriteSystemLine("=== Testing Game Configuration Integration ===");
            
            try
            {
                // Test loading the game configuration
                var config = GameConfiguration.Instance;
                UIManager.WriteSystemLine($"Configuration loaded successfully");
                
                // Test that EnemySystem is loaded
                if (config.EnemySystem != null)
                {
                    UIManager.WriteSystemLine($"EnemySystem configuration loaded:");
                    UIManager.WriteSystemLine($"  Global Health Multiplier: {config.EnemySystem.GlobalMultipliers.HealthMultiplier}");
                    UIManager.WriteSystemLine($"  Baseline Health: {config.EnemySystem.BaselineStats.Health}");
                    UIManager.WriteSystemLine($"  Health per Level: {config.EnemySystem.ScalingPerLevel.Health}");
                    UIManager.WriteSystemLine($"  Available Archetypes: {string.Join(", ", config.EnemySystem.Archetypes.Keys)}");
                    
                    // Test enemy creation
                    var testEnemy = EnemyLoader.CreateEnemy("Goblin", 3);
                    if (testEnemy != null)
                    {
                        UIManager.WriteSystemLine($"Test enemy created successfully:");
                        UIManager.WriteSystemLine($"  Name: {testEnemy.Name}");
                        UIManager.WriteSystemLine($"  Level: {testEnemy.Level}");
                        UIManager.WriteSystemLine($"  Health: {testEnemy.Health}");
                        UIManager.WriteSystemLine($"  Strength: {testEnemy.Strength}");
                        UIManager.WriteSystemLine($"  Agility: {testEnemy.Agility}");
                        UIManager.WriteSystemLine($"  Technique: {testEnemy.Technique}");
                        UIManager.WriteSystemLine($"  Intelligence: {testEnemy.Intelligence}");
                        UIManager.WriteSystemLine($"  Armor: {testEnemy.Armor}");
                    }
                    else
                    {
                        UIManager.WriteSystemLine("Warning: Could not create test enemy");
                    }
                }
                else
                {
                    UIManager.WriteSystemLine("Error: EnemySystem configuration not loaded");
                }
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error during configuration test: {ex.Message}");
            }
            
            UIManager.WriteSystemLine("=== Game Configuration Integration Test Complete ===");
        }
    }
}
