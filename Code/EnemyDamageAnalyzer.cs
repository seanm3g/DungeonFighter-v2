using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Analyzes and fixes enemy damage scaling issues
    /// </summary>
    public static class EnemyDamageAnalyzer
    {
        /// <summary>
        /// Analyzes why enemies are doing such low damage
        /// </summary>
        public static void AnalyzeEnemyDamageIssues()
        {
            Console.WriteLine("=== ENEMY DAMAGE ANALYSIS ===");
            Console.WriteLine();
            
            // Load enemy data
            EnemyLoader.LoadEnemies();
            var enemyDataList = EnemyLoader.GetAllEnemyData();
            
            if (!enemyDataList.Any())
            {
                Console.WriteLine("No enemy data found!");
                return;
            }
            
            // Test damage calculation for various enemies at different levels
            var testLevels = new[] { 1, 5, 10, 15, 20 };
            
            Console.WriteLine("Enemy\t\tLevel\tSTR\tHighest\tBase\tArchetype\tFinal\tMinDmg\tResult");
            Console.WriteLine("".PadRight(90, '='));
            
            foreach (var enemyData in enemyDataList.Take(8)) // Test first 8 enemies
            {
                foreach (int level in testLevels)
                {
                    var enemy = EnemyLoader.CreateEnemy(enemyData.Name, level);
                    if (enemy == null) continue;
                    
                    AnalyzeEnemyDamage(enemy);
                }
                Console.WriteLine(); // Blank line between enemies
            }
            
            Console.WriteLine();
            Console.WriteLine("=== DAMAGE CALCULATION BREAKDOWN ===");
            
            // Detailed breakdown for a specific enemy
            var testEnemy = EnemyLoader.CreateEnemy("Goblin", 5);
            if (testEnemy != null)
            {
                Console.WriteLine($"Detailed analysis for Level 5 Goblin:");
                Console.WriteLine($"  Base Stats: STR {testEnemy.Strength}, AGI {testEnemy.Agility}, TEC {testEnemy.Technique}, INT {testEnemy.Intelligence}");
                Console.WriteLine($"  Archetype: {testEnemy.Archetype} (Damage Multiplier: {testEnemy.AttackProfile.DamageMultiplier:F1}x)");
                
                // Step by step damage calculation
                int strength = testEnemy.Strength;
                int highestAttribute = strength; // For enemies, use strength as highest
                int baseDamage = strength + highestAttribute;
                
                Console.WriteLine($"  Step 1 - Base Damage: STR({strength}) + Highest({highestAttribute}) = {baseDamage}");
                
                double archetypeDamage = baseDamage * testEnemy.AttackProfile.DamageMultiplier;
                Console.WriteLine($"  Step 2 - Archetype Multiplier: {baseDamage} * {testEnemy.AttackProfile.DamageMultiplier:F1} = {archetypeDamage:F1}");
                
                var tuning = TuningConfig.Instance;
                int finalDamage = Math.Max(tuning.Combat.MinimumDamage, (int)archetypeDamage);
                Console.WriteLine($"  Step 3 - Minimum Damage Cap: Max({tuning.Combat.MinimumDamage}, {archetypeDamage:F0}) = {finalDamage}");
                
                Console.WriteLine($"  Final Result: {finalDamage} damage");
                
                // Suggest fixes
                Console.WriteLine();
                Console.WriteLine("SUGGESTED FIXES:");
                if (baseDamage < 5)
                {
                    Console.WriteLine($"  - Enemy base stats too low! STR {strength} results in only {baseDamage} base damage");
                }
                if (finalDamage <= tuning.Combat.MinimumDamage)
                {
                    Console.WriteLine($"  - Damage calculation hitting minimum cap of {tuning.Combat.MinimumDamage}");
                }
            }
        }
        
        private static void AnalyzeEnemyDamage(Enemy enemy)
        {
            // Calculate damage step by step
            int strength = enemy.Strength;
            int highestAttribute = strength; // For enemies, use strength as highest
            int baseDamage = strength + highestAttribute;
            
            double archetypeMultiplier = enemy.AttackProfile.DamageMultiplier;
            double archetypeDamage = baseDamage * archetypeMultiplier;
            
            var tuning = TuningConfig.Instance;
            int finalDamage = Math.Max(tuning.Combat.MinimumDamage, (int)archetypeDamage);
            
            string result = finalDamage <= tuning.Combat.MinimumDamage ? "MIN CAP!" : "OK";
            
            Console.WriteLine($"{enemy.Name?.PadRight(12)}\tL{enemy.Level}\t{strength}\t{highestAttribute}\t{baseDamage}\t{enemy.Archetype.ToString().PadRight(8)}\t{archetypeDamage:F0}\t{tuning.Combat.MinimumDamage}\t{result}");
        }
        
        /// <summary>
        /// Suggests and applies fixes for enemy damage scaling
        /// </summary>
        public static void SuggestDamageFixes()
        {
            Console.WriteLine("=== ENEMY DAMAGE FIX SUGGESTIONS ===");
            Console.WriteLine();
            
            var tuning = TuningConfig.Instance;
            
            Console.WriteLine("Current Settings:");
            Console.WriteLine($"  Minimum Damage: {tuning.Combat.MinimumDamage}");
            Console.WriteLine($"  Enemy Attributes Per Level: {tuning.Attributes.EnemyAttributesPerLevel}");
            Console.WriteLine($"  Enemy Primary Attribute Bonus: {tuning.Attributes.EnemyPrimaryAttributeBonus}");
            Console.WriteLine();
            
            Console.WriteLine("RECOMMENDED FIXES:");
            Console.WriteLine();
            
            Console.WriteLine("1. INCREASE ENEMY BASE STATS:");
            Console.WriteLine("   - Current enemy stats are too low for meaningful damage");
            Console.WriteLine("   - Goblin at level 5: STR 3 + (5 * 2) + (5 * 1) = 18 STR");
            Console.WriteLine("   - Damage: 18 + 18 = 36 base * archetype multiplier");
            Console.WriteLine("   - Suggested: Increase EnemyAttributesPerLevel to 3-4");
            Console.WriteLine();
            
            Console.WriteLine("2. ADD LEVEL-BASED DAMAGE SCALING:");
            Console.WriteLine("   - Enemies should get stronger with level");
            Console.WriteLine("   - Suggested: Add level * 2 to base damage calculation");
            Console.WriteLine();
            
            Console.WriteLine("3. ADJUST ARCHETYPE MULTIPLIERS:");
            Console.WriteLine("   - Current multipliers may be too conservative");
            Console.WriteLine("   - Berserker: 0.7x damage might be too low");
            Console.WriteLine("   - Juggernaut: 1.8x damage might be appropriate");
            Console.WriteLine();
            
            Console.WriteLine("4. REDUCE MINIMUM DAMAGE IMPACT:");
            Console.WriteLine("   - Consider setting MinimumDamage to 0 or very low");
            Console.WriteLine("   - Let natural scaling determine damage floors");
            Console.WriteLine();
            
            Console.WriteLine("Would you like to apply automatic fixes? (y/n)");
            string? response = Console.ReadLine();
            
            if (response?.ToLower() == "y")
            {
                ApplyDamageFixes();
            }
        }
        
        private static void ApplyDamageFixes()
        {
            Console.WriteLine("Applying enemy damage fixes...");
            
            var tuning = TuningConfig.Instance;
            
            // Fix 1: Increase enemy attribute scaling
            int oldAttributesPerLevel = tuning.Attributes.EnemyAttributesPerLevel;
            tuning.Attributes.EnemyAttributesPerLevel = 3; // Increased from 2
            Console.WriteLine($"  ✓ EnemyAttributesPerLevel: {oldAttributesPerLevel} → {tuning.Attributes.EnemyAttributesPerLevel}");
            
            // Fix 2: Increase primary attribute bonus
            int oldPrimaryBonus = tuning.Attributes.EnemyPrimaryAttributeBonus;
            tuning.Attributes.EnemyPrimaryAttributeBonus = 2; // Increased from 1
            Console.WriteLine($"  ✓ EnemyPrimaryAttributeBonus: {oldPrimaryBonus} → {tuning.Attributes.EnemyPrimaryAttributeBonus}");
            
            // Fix 3: Set minimum damage to 0 (let natural scaling work)
            int oldMinDamage = tuning.Combat.MinimumDamage;
            tuning.Combat.MinimumDamage = 0;
            Console.WriteLine($"  ✓ MinimumDamage: {oldMinDamage} → {tuning.Combat.MinimumDamage}");
            
            Console.WriteLine();
            Console.WriteLine("Fixes applied! Enemy damage should now scale properly.");
            Console.WriteLine("Note: These changes affect the in-memory config only.");
            Console.WriteLine("Use 'Export Configuration' in Tuning Console to save permanently.");
        }
        
        /// <summary>
        /// Tests damage after applying fixes
        /// </summary>
        public static void TestDamageAfterFixes()
        {
            Console.WriteLine("=== TESTING DAMAGE AFTER FIXES ===");
            Console.WriteLine();
            
            // Test a few enemies at different levels
            var testEnemies = new[] { "Goblin", "Spider", "Skeleton", "Orc" };
            var testLevels = new[] { 1, 5, 10, 15 };
            
            Console.WriteLine("Enemy\t\tLevel\tExpected Damage\tActual DPS\tStatus");
            Console.WriteLine("".PadRight(70, '='));
            
            foreach (string enemyName in testEnemies)
            {
                foreach (int level in testLevels)
                {
                    var enemy = EnemyLoader.CreateEnemy(enemyName, level);
                    if (enemy == null) continue;
                    
                    // Calculate expected damage range for this level
                    int expectedMinDamage = level * 3; // Rough expectation
                    int expectedMaxDamage = level * 8;
                    
                    // Calculate actual DPS
                    double actualDPS = EnemyDPSCalculator.CalculateEnemyDPS(enemy, enemy.Archetype);
                    
                    string status = actualDPS >= expectedMinDamage ? "✓ Good" : "⚠ Low";
                    
                    Console.WriteLine($"{enemyName.PadRight(12)}\tL{level}\t{expectedMinDamage}-{expectedMaxDamage}\t\t{actualDPS:F1}\t\t{status}");
                }
            }
        }
    }
}
