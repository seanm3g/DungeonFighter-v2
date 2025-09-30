using System;

namespace RPGGame
{
    /// <summary>
    /// Test class to verify the new enemy balance system is working correctly
    /// </summary>
    public static class EnemyBalanceTest
    {
        public static void TestEnemyBalanceSystem()
        {
            UIManager.WriteSystemLine("=== ENEMY BALANCE SYSTEM TEST ===");
            UIManager.WriteSystemLine("");
            
            // Test different archetypes at different levels
            var testLevels = new[] { 1, 3, 5 };
            var testArchetypes = new[] 
            { 
                EnemyArchetype.Berserker, 
                EnemyArchetype.Assassin, 
                EnemyArchetype.Warrior, 
                EnemyArchetype.Brute, 
                EnemyArchetype.Juggernaut,
                EnemyArchetype.Guardian
            };
            
            var baseStats = new EnemyBaseStats
            {
                Strength = 4,
                Agility = 4,
                Technique = 4,
                Intelligence = 4
            };
            
            UIManager.WriteSystemLine("Archetype".PadRight(12) + "Level".PadRight(6) + "Health".PadRight(8) + "Armor".PadRight(6) + "Strength".PadRight(8) + "DPS".PadRight(8) + "SUSTAIN");
            UIManager.WriteSystemLine(new string('-', 70));
            
            foreach (var archetype in testArchetypes)
            {
                foreach (var level in testLevels)
                {
                    var stats = EnemyBalanceCalculator.CalculateStats(level, archetype, baseStats);
                    
                    // Calculate DPS and SUSTAIN values
                    double dps = stats.Strength * stats.AttackSpeed;
                    double sustain = stats.Health + stats.Armor;
                    
                    UIManager.WriteSystemLine(
                        archetype.ToString().PadRight(12) +
                        level.ToString().PadRight(6) +
                        stats.Health.ToString().PadRight(8) +
                        stats.Armor.ToString().PadRight(6) +
                        stats.Strength.ToString().PadRight(8) +
                        dps.ToString("F1").PadRight(8) +
                        sustain.ToString("F0")
                    );
                }
                UIManager.WriteSystemLine(""); // Blank line between archetypes
            }
            
            UIManager.WriteSystemLine("=== ARCHETYPE ALLOCATION TEST ===");
            UIManager.WriteSystemLine("");
            
            var config = GameConfiguration.Instance.EnemyBalance;
            int testLevel = 3;
            int totalPoints = config.BaseTotalPointsAtLevel1 + ((testLevel - 1) * config.TotalPointsPerLevel);
            
            UIManager.WriteSystemLine($"Level {testLevel} Total Points: {totalPoints}");
            UIManager.WriteSystemLine("");
            
            // Header for detailed breakdown
            UIManager.WriteSystemLine("Archetype".PadRight(12) + "DPS Pool".PadRight(10) + "Attack".PadRight(8) + "Speed".PadRight(8) + "SUSTAIN Pool".PadRight(12) + "Health".PadRight(8) + "Armor");
            UIManager.WriteSystemLine(new string('-', 80));
            
            foreach (var archetype in testArchetypes)
            {
                var allocation = GetArchetypeAllocation(archetype, config);
                int dpsPoints = (int)Math.Round(totalPoints * allocation.DPSPercentage);
                int sustainPoints = totalPoints - dpsPoints;
                
                // Get archetype-specific ratios
                var archetypeConfig = GetArchetypeConfig(archetype, config);
                
                // Calculate DPS breakdown (Attack vs Attack Speed)
                int attackPoints = (int)Math.Round(dpsPoints * archetypeConfig.DPSAttackRatio);
                int attackSpeedPoints = dpsPoints - attackPoints;
                
                // Calculate SUSTAIN breakdown (Health vs Armor)
                int healthPoints = (int)Math.Round(sustainPoints * archetypeConfig.SUSTAINHealthRatio);
                int armorPoints = sustainPoints - healthPoints;
                
                UIManager.WriteSystemLine(
                    archetype.ToString().PadRight(12) +
                    $"{dpsPoints}".PadRight(10) +
                    $"{attackPoints}".PadRight(8) +
                    $"{attackSpeedPoints}".PadRight(8) +
                    $"{sustainPoints}".PadRight(12) +
                    $"{healthPoints}".PadRight(8) +
                    $"{armorPoints}"
                );
            }
            
            UIManager.WriteSystemLine("");
            UIManager.WriteSystemLine("=== ARCHETYPE RATIOS SUMMARY ===");
            UIManager.WriteSystemLine("");
            
            foreach (var archetype in testArchetypes)
            {
                var allocation = GetArchetypeAllocation(archetype, config);
                var archetypeConfig = GetArchetypeConfig(archetype, config);
                
                UIManager.WriteSystemLine($"{archetype.ToString().PadRight(12)}: DPS={allocation.DPSPercentage:P0} (Attack={archetypeConfig.DPSAttackRatio:P0}, Speed={archetypeConfig.DPSAttackSpeedRatio:P0}), SUSTAIN={allocation.SUSTAINPercentage:P0} (Health={archetypeConfig.SUSTAINHealthRatio:P0}, Armor={archetypeConfig.SUSTAINArmorRatio:P0})");
            }
            
            UIManager.WriteSystemLine("");
            UIManager.WriteSystemLine("=== REAL ENEMY TEST ===");
            UIManager.WriteSystemLine("");
            
            // Test with actual enemy data
            var testEnemies = new[] { "Bat", "Goblin", "Orc", "Troll" };
            foreach (var enemyName in testEnemies)
            {
                var enemy = EnemyLoader.CreateEnemy(enemyName, 3);
                if (enemy != null)
                {
                    UIManager.WriteSystemLine($"{enemyName}: Level {enemy.Level}, {enemy.Archetype}, HP={enemy.MaxHealth}, STR={enemy.Strength}, ARMOR={enemy.Armor}");
                }
            }
        }
        
        private static (double DPSPercentage, double SUSTAINPercentage) GetArchetypeAllocation(EnemyArchetype archetype, EnemyBalanceConfig config)
        {
            return archetype switch
            {
                EnemyArchetype.Berserker => (config.DPSAllocation.MaxPercentage, config.SUSTAINAllocation.MinPercentage),
                EnemyArchetype.Assassin => (config.DPSAllocation.MaxPercentage, config.SUSTAINAllocation.MinPercentage),
                EnemyArchetype.Juggernaut => (config.DPSAllocation.MinPercentage, config.SUSTAINAllocation.MaxPercentage),
                EnemyArchetype.Guardian => (config.DPSAllocation.MinPercentage, config.SUSTAINAllocation.MaxPercentage),
                EnemyArchetype.Warrior => (config.DPSAllocation.DefaultPercentage, config.SUSTAINAllocation.DefaultPercentage),
                EnemyArchetype.Brute => (config.DPSAllocation.DefaultPercentage, config.SUSTAINAllocation.DefaultPercentage),
                _ => (config.DPSAllocation.DefaultPercentage, config.SUSTAINAllocation.DefaultPercentage)
            };
        }
        
        private static ArchetypeConfig GetArchetypeConfig(EnemyArchetype archetype, EnemyBalanceConfig config)
        {
            // Try to get from ArchetypeConfigs dictionary first
            if (config.ArchetypeConfigs.TryGetValue(archetype.ToString(), out var archetypeConfig))
            {
                return archetypeConfig;
            }
            
            // Fallback to default ratios based on archetype type
            return archetype switch
            {
                EnemyArchetype.Berserker => new ArchetypeConfig 
                { 
                    DPSAttackRatio = 0.7, DPSAttackSpeedRatio = 0.3, 
                    SUSTAINHealthRatio = 0.6, SUSTAINArmorRatio = 0.4 
                },
                EnemyArchetype.Assassin => new ArchetypeConfig 
                { 
                    DPSAttackRatio = 0.4, DPSAttackSpeedRatio = 0.6, 
                    SUSTAINHealthRatio = 0.8, SUSTAINArmorRatio = 0.2 
                },
                EnemyArchetype.Juggernaut => new ArchetypeConfig 
                { 
                    DPSAttackRatio = 0.5, DPSAttackSpeedRatio = 0.5, 
                    SUSTAINHealthRatio = 0.3, SUSTAINArmorRatio = 0.7 
                },
                EnemyArchetype.Guardian => new ArchetypeConfig 
                { 
                    DPSAttackRatio = 0.6, DPSAttackSpeedRatio = 0.4, 
                    SUSTAINHealthRatio = 0.4, SUSTAINArmorRatio = 0.6 
                },
                EnemyArchetype.Warrior => new ArchetypeConfig 
                { 
                    DPSAttackRatio = 0.5, DPSAttackSpeedRatio = 0.5, 
                    SUSTAINHealthRatio = 0.5, SUSTAINArmorRatio = 0.5 
                },
                EnemyArchetype.Brute => new ArchetypeConfig 
                { 
                    DPSAttackRatio = 0.6, DPSAttackSpeedRatio = 0.4, 
                    SUSTAINHealthRatio = 0.6, SUSTAINArmorRatio = 0.4 
                },
                _ => new ArchetypeConfig 
                { 
                    DPSAttackRatio = 0.5, DPSAttackSpeedRatio = 0.5, 
                    SUSTAINHealthRatio = 0.5, SUSTAINArmorRatio = 0.5 
                }
            };
        }
    }
}
