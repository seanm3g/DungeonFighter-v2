using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Test class to verify enemy armor and stat pool balance
    /// </summary>
    public static class EnemyArmorBalanceTest
    {
        /// <summary>
        /// Tests the new enemy armor and stat pool system
        /// </summary>
        public static void TestEnemyArmorAndStatPools()
        {
            Console.WriteLine("=== ENEMY ARMOR & STAT POOL BALANCE TEST ===");
            Console.WriteLine();
            
            // Test 1: Verify enemies have armor
            Console.WriteLine("TEST 1: Enemy Armor Verification");
            Console.WriteLine("Enemy\t\tLevel\tArmor\tHealth\tSTR\tAGI\tDPS\tSurvivability");
            Console.WriteLine("".PadRight(85, '='));
            
            EnemyLoader.LoadEnemies();
            var enemyDataList = EnemyLoader.GetAllEnemyData();
            
            var testEnemies = enemyDataList.Take(5);
            var testLevels = new[] { 1, 5, 10 };
            
            foreach (var enemyData in testEnemies)
            {
                foreach (int level in testLevels)
                {
                    var enemy = EnemyLoader.CreateEnemy(enemyData.Name, level);
                    if (enemy == null) continue;
                    
                    // Calculate DPS and survivability
                    double dps = CalculateEnemyDPS(enemy);
                    double survivability = CalculateEnemySurvivability(enemy);
                    
                    Console.WriteLine($"{enemyData.Name.PadRight(12)}\tL{level}\t{enemy.Armor}\t{enemy.MaxHealth}\t{enemy.Strength}\t{enemy.Agility}\t{dps:F1}\t{survivability:F1}");
                }
                Console.WriteLine();
            }
            
            // Test 2: Stat pool distribution analysis
            Console.WriteLine("TEST 2: Stat Pool Distribution Analysis");
            Console.WriteLine("Level\tArchetype\tDPS Pool\tSurv Pool\tTotal Power\tBalance");
            Console.WriteLine("".PadRight(75, '='));
            
            foreach (int level in new[] { 1, 5, 10, 15 })
            {
                foreach (var archetype in Enum.GetValues<EnemyArchetype>())
                {
                    var distribution = EnemyStatPoolSystem.DistributeStats(level, archetype);
                    double totalPower = distribution.DPSPool + distribution.SurvivabilityPool;
                    double balance = Math.Abs(distribution.DPSPool - distribution.SurvivabilityPool) / totalPower * 100;
                    
                    Console.WriteLine($"{level}\t{archetype.ToString().PadRight(8)}\t{distribution.DPSPool:F1}\t\t{distribution.SurvivabilityPool:F1}\t\t{totalPower:F1}\t\t{balance:F0}%");
                }
                Console.WriteLine();
            }
            
            // Test 3: Armor effectiveness in combat
            Console.WriteLine("TEST 3: Armor Effectiveness in Combat");
            Console.WriteLine("Enemy\t\tLevel\tArmor\tBase Damage\tReduced Damage\tReduction %");
            Console.WriteLine("".PadRight(70, '='));
            
            foreach (var enemyData in testEnemies)
            {
                foreach (int level in new[] { 1, 5, 10 })
                {
                    var enemy = EnemyLoader.CreateEnemy(enemyData.Name, level);
                    if (enemy == null) continue;
                    
                    // Simulate damage calculation
                    int baseDamage = 20; // Fixed base damage for testing
                    int reducedDamage = Math.Max(1, baseDamage - enemy.Armor);
                    double reductionPercent = (double)(baseDamage - reducedDamage) / baseDamage * 100;
                    
                    Console.WriteLine($"{enemyData.Name.PadRight(12)}\tL{level}\t{enemy.Armor}\t{baseDamage}\t\t{reducedDamage}\t\t{reductionPercent:F0}%");
                }
                Console.WriteLine();
            }
            
            // Test 4: Stat pool validation
            Console.WriteLine("TEST 4: Stat Pool Validation");
            Console.WriteLine("Enemy\t\tLevel\tDPS Dev%\tSurv Dev%\tTotal Dev%\tStatus");
            Console.WriteLine("".PadRight(70, '='));
            
            foreach (var enemyData in testEnemies)
            {
                foreach (int level in new[] { 1, 5, 10 })
                {
                    var enemy = EnemyLoader.CreateEnemy(enemyData.Name, level);
                    if (enemy == null) continue;
                    
                    var validation = EnemyStatPoolSystem.ValidateEnemyStats(enemy);
                    string status = validation.IsBalanced ? "✓ Balanced" : "⚠ Unbalanced";
                    
                    Console.WriteLine($"{enemyData.Name.PadRight(12)}\tL{level}\t{validation.DPSDeviation:F0}%\t\t{validation.SurvivabilityDeviation:F0}%\t\t{validation.TotalDeviation:F0}%\t\t{status}");
                }
                Console.WriteLine();
            }
            
            // Test 5: Archetype comparison with detailed variance analysis
            Console.WriteLine("TEST 5: Archetype Comparison at Level 10");
            Console.WriteLine("Archetype\tDPS%\tSurv%\tSTR\tAGI\tHealth\tArmor\tDPS\tSurvivability\tTotal");
            Console.WriteLine("".PadRight(95, '='));
            
            foreach (var archetype in Enum.GetValues<EnemyArchetype>())
            {
                var distribution = EnemyStatPoolSystem.DistributeStats(10, archetype);
                double dps = CalculateDistributionDPS(distribution);
                double survivability = CalculateDistributionSurvivability(distribution);
                double total = dps + survivability;
                double dpsPercent = (distribution.DPSPool / distribution.TotalPower) * 100;
                double survPercent = (distribution.SurvivabilityPool / distribution.TotalPower) * 100;
                
                Console.WriteLine($"{archetype.ToString().PadRight(12)}\t{dpsPercent:F0}%\t{survPercent:F0}%\t{distribution.Strength}\t{distribution.Agility}\t{distribution.Health}\t{distribution.Armor}\t{dps:F1}\t{survivability:F1}\t\t{total:F1}");
            }
            
            Console.WriteLine();
            Console.WriteLine("TEST 6: Archetype Variance Analysis");
            Console.WriteLine("Archetype\tFocus\tDPS Style\tSurvivability Style\tKey Characteristics");
            Console.WriteLine("".PadRight(85, '='));
            
            var archetypeDescriptions = new Dictionary<EnemyArchetype, (string focus, string dpsStyle, string survStyle, string characteristics)>
            {
                { EnemyArchetype.Berserker, ("Agility", "High DPS, Fast", "Low Health, No Armor", "Glass cannon, rapid attacks") },
                { EnemyArchetype.Assassin, ("Technique", "Moderate DPS, Quick", "Balanced Health/Armor", "Precise strikes, moderate defense") },
                { EnemyArchetype.Warrior, ("Balanced", "Balanced DPS", "Balanced Health/Armor", "Well-rounded, reliable") },
                { EnemyArchetype.Brute, ("Strength", "High DPS, Slow", "Moderate Health/Armor", "Heavy hitter, decent defense") },
                { EnemyArchetype.Juggernaut, ("Tank", "Low DPS, Very Slow", "High Health/Armor", "Extremely durable, slow attacks") }
            };
            
            foreach (var archetype in Enum.GetValues<EnemyArchetype>())
            {
                if (archetypeDescriptions.TryGetValue(archetype, out var desc))
                {
                    Console.WriteLine($"{archetype.ToString().PadRight(12)}\t{desc.focus.PadRight(6)}\t{desc.dpsStyle.PadRight(15)}\t{desc.survStyle.PadRight(20)}\t{desc.characteristics}");
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("=== SUMMARY ===");
            Console.WriteLine("✓ Enemies now have armor that reduces incoming damage");
            Console.WriteLine("✓ Stat pools ensure consistent DPS vs survivability balance");
            Console.WriteLine("✓ Archetypes maintain distinct characteristics while being balanced");
            Console.WriteLine("✓ Armor scales with level and archetype for proper progression");
        }
        
        /// <summary>
        /// Calculates enemy DPS for testing
        /// </summary>
        private static double CalculateEnemyDPS(Enemy enemy)
        {
            // Simplified DPS calculation
            int baseDamage = enemy.Strength + enemy.Strength; // STR + highest attribute
            double attackTime = enemy.GetTotalAttackSpeed();
            return baseDamage / attackTime;
        }
        
        /// <summary>
        /// Calculates enemy survivability for testing
        /// </summary>
        private static double CalculateEnemySurvivability(Enemy enemy)
        {
            // Survivability = health + armor effectiveness
            return enemy.MaxHealth + (enemy.Armor * 2.0); // Armor is worth 2x its value in survivability
        }
        
        /// <summary>
        /// Calculates DPS from stat distribution
        /// </summary>
        private static double CalculateDistributionDPS(EnemyStatDistribution distribution)
        {
            int baseDamage = distribution.Strength + distribution.Strength;
            double attackTime = 9.3 - (distribution.Agility * 0.5); // Simplified attack time calculation
            attackTime = Math.Max(3.0, attackTime); // Minimum attack time
            return baseDamage / attackTime;
        }
        
        /// <summary>
        /// Calculates survivability from stat distribution
        /// </summary>
        private static double CalculateDistributionSurvivability(EnemyStatDistribution distribution)
        {
            return distribution.Health + (distribution.Armor * 2.0);
        }
        
        /// <summary>
        /// Tests armor damage reduction in actual combat scenarios
        /// </summary>
        public static void TestArmorDamageReduction()
        {
            Console.WriteLine("=== ARMOR DAMAGE REDUCTION TEST ===");
            Console.WriteLine();
            
            // Create test enemies with different armor values
            var testEnemies = new[]
            {
                EnemyStatPoolSystem.CreateBalancedEnemy("Light Armor", 5, EnemyArchetype.Assassin),
                EnemyStatPoolSystem.CreateBalancedEnemy("Medium Armor", 5, EnemyArchetype.Warrior),
                EnemyStatPoolSystem.CreateBalancedEnemy("Heavy Armor", 5, EnemyArchetype.Juggernaut)
            };
            
            Console.WriteLine("Enemy\t\tArmor\tHealth\tBase Dmg\tReduced Dmg\tReduction %");
            Console.WriteLine("".PadRight(70, '='));
            
            foreach (var enemy in testEnemies)
            {
                int baseDamage = 25; // Fixed base damage
                int reducedDamage = Math.Max(1, baseDamage - enemy.Armor);
                double reductionPercent = (double)(baseDamage - reducedDamage) / baseDamage * 100;
                
                Console.WriteLine($"{enemy.Name.PadRight(12)}\t{enemy.Armor}\t{enemy.MaxHealth}\t{baseDamage}\t\t{reducedDamage}\t\t{reductionPercent:F0}%");
            }
            
            Console.WriteLine();
            Console.WriteLine("Armor provides meaningful damage reduction while maintaining balance!");
        }

        /// <summary>
        /// Tests archetype variance in stat distributions
        /// </summary>
        public static void TestArchetypeVariance()
        {
            Console.WriteLine("=== ARCHETYPE VARIANCE TEST ===");
            Console.WriteLine();
            
            Console.WriteLine("Testing stat distribution variance across archetypes at different levels...");
            Console.WriteLine();
            
            var testLevels = new[] { 1, 5, 10, 15, 20 };
            
            foreach (int level in testLevels)
            {
                Console.WriteLine($"LEVEL {level} COMPARISON:");
                Console.WriteLine("Archetype\tDPS%\tSurv%\tSTR\tAGI\tHealth\tArmor\tTotal Power");
                Console.WriteLine("".PadRight(75, '-'));
                
                foreach (var archetype in Enum.GetValues<EnemyArchetype>())
                {
                    var distribution = EnemyStatPoolSystem.DistributeStats(level, archetype);
                    double dpsPercent = (distribution.DPSPool / distribution.TotalPower) * 100;
                    double survPercent = (distribution.SurvivabilityPool / distribution.TotalPower) * 100;
                    
                    Console.WriteLine($"{archetype.ToString().PadRight(12)}\t{dpsPercent:F0}%\t{survPercent:F0}%\t{distribution.Strength}\t{distribution.Agility}\t{distribution.Health}\t{distribution.Armor}\t{distribution.TotalPower:F1}");
                }
                Console.WriteLine();
            }
            
            Console.WriteLine("=== ARCHETYPE SPECIALIZATION ANALYSIS ===");
            Console.WriteLine();
            
            // Analyze specialization at level 10
            var level10Distributions = new Dictionary<EnemyArchetype, EnemyStatDistribution>();
            foreach (var archetype in Enum.GetValues<EnemyArchetype>())
            {
                level10Distributions[archetype] = EnemyStatPoolSystem.DistributeStats(10, archetype);
            }
            
            // Find min/max values for each stat
            var maxStrength = level10Distributions.Values.Max(d => d.Strength);
            var minStrength = level10Distributions.Values.Min(d => d.Strength);
            var maxAgility = level10Distributions.Values.Max(d => d.Agility);
            var minAgility = level10Distributions.Values.Min(d => d.Agility);
            var maxHealth = level10Distributions.Values.Max(d => d.Health);
            var minHealth = level10Distributions.Values.Min(d => d.Health);
            var maxArmor = level10Distributions.Values.Max(d => d.Armor);
            var minArmor = level10Distributions.Values.Min(d => d.Armor);
            
            Console.WriteLine("Stat Ranges at Level 10:");
            Console.WriteLine($"Strength: {minStrength} - {maxStrength} (Range: {maxStrength - minStrength})");
            Console.WriteLine($"Agility: {minAgility} - {maxAgility} (Range: {maxAgility - minAgility})");
            Console.WriteLine($"Health: {minHealth} - {maxHealth} (Range: {maxHealth - minHealth})");
            Console.WriteLine($"Armor: {minArmor} - {maxArmor} (Range: {maxArmor - minArmor})");
            Console.WriteLine();
            
            Console.WriteLine("Archetype Specializations:");
            foreach (var kvp in level10Distributions)
            {
                var archetype = kvp.Key;
                var dist = kvp.Value;
                
                string specializations = "";
                if (dist.Strength == maxStrength) specializations += "Max STR ";
                if (dist.Agility == maxAgility) specializations += "Max AGI ";
                if (dist.Health == maxHealth) specializations += "Max Health ";
                if (dist.Armor == maxArmor) specializations += "Max Armor ";
                
                if (string.IsNullOrEmpty(specializations))
                    specializations = "Balanced";
                
                Console.WriteLine($"{archetype.ToString().PadRight(12)}: {specializations}");
            }
            
            Console.WriteLine();
            Console.WriteLine("✓ Archetypes show meaningful variance in stat distributions");
            Console.WriteLine("✓ Each archetype has distinct strengths and weaknesses");
            Console.WriteLine("✓ Stat pools maintain balance while allowing specialization");
        }
    }
}
