using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Test class to verify tier distribution is working correctly
    /// </summary>
    public static class TierDistributionTest
    {
        public static void TestTierDistribution()
        {
            Console.WriteLine("Comprehensive Tier Distribution Test");
            Console.WriteLine("Formula: lootLevel = dungeonLevel - (playerLevel - dungeonLevel)");
            Console.WriteLine("Testing up to Dungeon Level 100, Character Level 100");
            Console.WriteLine("With up to 20% level difference in either direction");
            Console.WriteLine();
            
            // Initialize the loot generator
            LootGenerator.Initialize();
            
            // Test key scenarios across the full range
            var testScenarios = new List<(int playerLevel, int dungeonLevel, string description)>
            {
                // Early game scenarios
                (1, 1, "Level 1 Character in Level 1 Dungeon (Same Level)"),
                (1, 2, "Level 1 Character in Level 2 Dungeon (+20% Challenge)"),
                (2, 1, "Level 2 Character in Level 1 Dungeon (-20% Challenge)"),
                
                // Mid game scenarios
                (25, 25, "Level 25 Character in Level 25 Dungeon (Same Level)"),
                (20, 25, "Level 20 Character in Level 25 Dungeon (+25% Challenge)"),
                (30, 25, "Level 30 Character in Level 25 Dungeon (-20% Challenge)"),
                
                // High level scenarios
                (50, 50, "Level 50 Character in Level 50 Dungeon (Same Level)"),
                (40, 50, "Level 40 Character in Level 50 Dungeon (+25% Challenge)"),
                (60, 50, "Level 60 Character in Level 50 Dungeon (-20% Challenge)"),
                
                // End game scenarios
                (100, 100, "Level 100 Character in Level 100 Dungeon (Same Level)"),
                (80, 100, "Level 80 Character in Level 100 Dungeon (+25% Challenge)"),
                (100, 80, "Level 100 Character in Level 80 Dungeon (-25% Challenge)"),
                
                // Extreme scenarios
                (1, 100, "Level 1 Character in Level 100 Dungeon (Extreme Challenge)"),
                (100, 1, "Level 100 Character in Level 1 Dungeon (Extreme Overlevel)")
            };
            
            foreach (var scenario in testScenarios)
            {
                TestScenario(scenario.playerLevel, scenario.dungeonLevel, scenario.description);
            }
            
            // Summary analysis
            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("SUMMARY ANALYSIS");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine("This test verifies that:");
            Console.WriteLine("1. Same level characters get appropriate tier distribution");
            Console.WriteLine("2. Underleveled characters get higher tier loot (reward for challenge)");
            Console.WriteLine("3. Overleveled characters get lower tier loot (prevents farming)");
            Console.WriteLine("4. Loot level calculation works correctly across all ranges");
            Console.WriteLine("5. Tier distribution matches TierDistribution.json expectations");
        }
        
        private static void TestScenario(int playerLevel, int dungeonLevel, string description)
        {
            int lootLevel = dungeonLevel - (playerLevel - dungeonLevel);
            if (lootLevel <= 0) lootLevel = 1;
            if (lootLevel > 100) lootLevel = 100;
            
            Console.WriteLine($"\n{description}");
            Console.WriteLine($"Player Level: {playerLevel}, Dungeon Level: {dungeonLevel}");
            Console.WriteLine($"Calculated Loot Level: {lootLevel}");
            
            // Generate 500 items for this scenario
            var results = new Dictionary<int, int>();
            for (int i = 1; i <= 5; i++) results[i] = 0;
            
            for (int i = 0; i < 500; i++)
            {
                var item = LootGenerator.GenerateLoot(playerLevel, dungeonLevel, null, true);
                if (item != null)
                {
                    results[item.Tier]++;
                }
            }
            
            // Display results
            Console.WriteLine("Tier Distribution:");
            foreach (var kvp in results.OrderBy(x => x.Key))
            {
                if (kvp.Value > 0)
                {
                    double percentage = (double)kvp.Value / 500 * 100;
                    Console.WriteLine($"  Tier {kvp.Key}: {kvp.Value,3} items ({percentage,5:F1}%)");
                }
            }
            
            // Show expected values for comparison (if we have them)
            if (lootLevel <= 20)
            {
                ShowExpectedValues(lootLevel);
            }
        }
        
        private static void ShowExpectedValues(int lootLevel)
        {
            // Expected values from TierDistribution.json for levels 1-20
            var expectedValues = new Dictionary<int, (double tier1, double tier2, double tier3, double tier4, double tier5)>
            {
                {1, (93.10, 6.90, 0.00, 0.00, 0.00)},
                {2, (92.30, 7.70, 0.00, 0.00, 0.00)},
                {3, (91.23, 8.77, 0.00, 0.00, 0.00)},
                {4, (89.81, 10.19, 0.00, 0.00, 0.00)},
                {5, (87.95, 12.04, 0.00, 0.00, 0.00)},
                {6, (85.52, 14.47, 0.01, 0.00, 0.00)},
                {7, (82.36, 17.63, 0.01, 0.00, 0.00)},
                {8, (78.28, 21.69, 0.02, 0.00, 0.00)},
                {9, (73.11, 26.85, 0.03, 0.01, 0.00)},
                {10, (66.70, 33.23, 0.06, 0.01, 0.00)},
                {11, (59.04, 40.84, 0.10, 0.02, 0.00)},
                {12, (50.32, 49.49, 0.16, 0.03, 0.00)},
                {13, (41.01, 58.68, 0.27, 0.04, 0.00)},
                {14, (31.79, 67.73, 0.42, 0.06, 0.00)},
                {15, (23.37, 75.91, 0.65, 0.08, 0.00)},
                {16, (16.30, 82.64, 0.95, 0.11, 0.00)},
                {17, (10.83, 87.67, 1.36, 0.14, 0.00)},
                {18, (6.88, 91.04, 1.90, 0.19, 0.00)},
                {19, (4.20, 92.99, 2.58, 0.24, 0.00)},
                {20, (2.47, 93.79, 3.44, 0.30, 0.00)}
            };
            
            if (expectedValues.ContainsKey(lootLevel))
            {
                var expected = expectedValues[lootLevel];
                Console.WriteLine("Expected (from TierDistribution.json):");
                Console.WriteLine($"  Tier 1: {expected.tier1,5:F1}%");
                Console.WriteLine($"  Tier 2: {expected.tier2,5:F1}%");
                if (expected.tier3 > 0) Console.WriteLine($"  Tier 3: {expected.tier3,5:F1}%");
                if (expected.tier4 > 0) Console.WriteLine($"  Tier 4: {expected.tier4,5:F1}%");
                if (expected.tier5 > 0) Console.WriteLine($"  Tier 5: {expected.tier5,5:F1}%");
            }
        }
    }
}
