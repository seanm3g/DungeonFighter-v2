using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace RPGGame
{
    /// <summary>
    /// Manages test execution and analysis for the game
    /// </summary>
    public static class TestManager
    {
        /// <summary>
        /// Runs Test 1: Item generation analysis across levels 1-20
        /// Generates 100 items at each level and analyzes rarity, mod, and tier distribution
        /// </summary>
        public static void RunItemGenerationTest()
        {
            TextDisplayIntegration.DisplaySystem("Starting Item Generation Test...");
            TextDisplayIntegration.DisplaySystem("This will generate 100 items at each level from 1-20 and analyze the results.");
            TextDisplayIntegration.DisplaySystem("Press any key to continue or 'q' to quit...");
            
            var key = Console.ReadKey();
            if (key.KeyChar == 'q' || key.KeyChar == 'Q')
            {
                TextDisplayIntegration.DisplaySystem("Test cancelled.");
                return;
            }
            
            TextDisplayIntegration.DisplaySystem("\nGenerating items... This may take a moment.");
            
            var results = new List<LevelTestResult>();
            
            // Generate items for each level
            for (int level = 1; level <= 20; level++)
            {
                TextDisplayIntegration.DisplaySystem($"Generating items for level {level}...");
                var levelResult = GenerateItemsForLevel(level, 100);
                results.Add(levelResult);
            }
            
            // Display results
            DisplayTestResults(results);
            
            // Save results to file
            SaveTestResults(results);
            
            TextDisplayIntegration.DisplaySystem("\nTest completed! Results saved to 'item_generation_test_results.txt'");
            TextDisplayIntegration.DisplaySystem("Press any key to continue...");
            Console.ReadKey();
        }
        
        /// <summary>
        /// Generates the specified number of items for a given level
        /// </summary>
        /// <param name="level">The level to generate items for</param>
        /// <param name="count">Number of items to generate</param>
        /// <returns>Test results for this level</returns>
        private static LevelTestResult GenerateItemsForLevel(int level, int count)
        {
            var result = new LevelTestResult
            {
                Level = level,
                TotalItems = count,
                RarityDistribution = new Dictionary<string, int>(),
                TierDistribution = new Dictionary<int, int>(),
                ModificationDistribution = new Dictionary<string, int>(),
                StatBonusDistribution = new Dictionary<string, int>(),
                ActionBonusDistribution = new Dictionary<string, int>(),
                ItemTypeDistribution = new Dictionary<string, int>()
            };
            
            // Initialize distributions
            foreach (var rarity in new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary" })
            {
                result.RarityDistribution[rarity] = 0;
            }
            
            for (int tier = 1; tier <= 5; tier++)
            {
                result.TierDistribution[tier] = 0;
            }
            
            // Generate items
            for (int i = 0; i < count; i++)
            {
                // Test loot generation for a character of this level in a level 1 dungeon
                // This tests: lootLevel = 1 - (level - 1) = 2 - level
                // Level 1: lootLevel = 1 (normal)
                // Level 2: lootLevel = 0 -> clamped to 1 (slightly lower tier)
                // Level 3: lootLevel = -1 -> clamped to 1 (lower tier)
                var item = LootGenerator.GenerateLoot(level, 1, null, true); // guaranteedLoot = true
                
                if (item != null)
                {
                    // Track rarity
                    if (result.RarityDistribution.ContainsKey(item.Rarity))
                    {
                        result.RarityDistribution[item.Rarity]++;
                    }
                    else
                    {
                        result.RarityDistribution[item.Rarity] = 1;
                    }
                    
                    // Track tier
                    if (result.TierDistribution.ContainsKey(item.Tier))
                    {
                        result.TierDistribution[item.Tier]++;
                    }
                    else
                    {
                        result.TierDistribution[item.Tier] = 1;
                    }
                    
                    // Track item type
                    string itemType = item switch
                    {
                        WeaponItem => "Weapon",
                        HeadItem => "Head Armor",
                        ChestItem => "Chest Armor",
                        FeetItem => "Feet Armor",
                        _ => "Unknown"
                    };
                    
                    if (result.ItemTypeDistribution.ContainsKey(itemType))
                    {
                        result.ItemTypeDistribution[itemType]++;
                    }
                    else
                    {
                        result.ItemTypeDistribution[itemType] = 1;
                    }
                    
                    // Track modifications
                    foreach (var mod in item.Modifications)
                    {
                        if (result.ModificationDistribution.ContainsKey(mod.Name))
                        {
                            result.ModificationDistribution[mod.Name]++;
                        }
                        else
                        {
                            result.ModificationDistribution[mod.Name] = 1;
                        }
                    }
                    
                    // Track stat bonuses
                    foreach (var statBonus in item.StatBonuses)
                    {
                        if (result.StatBonusDistribution.ContainsKey(statBonus.Name))
                        {
                            result.StatBonusDistribution[statBonus.Name]++;
                        }
                        else
                        {
                            result.StatBonusDistribution[statBonus.Name] = 1;
                        }
                    }
                    
                    // Track action bonuses
                    foreach (var actionBonus in item.ActionBonuses)
                    {
                        if (result.ActionBonusDistribution.ContainsKey(actionBonus.Name))
                        {
                            result.ActionBonusDistribution[actionBonus.Name]++;
                        }
                        else
                        {
                            result.ActionBonusDistribution[actionBonus.Name] = 1;
                        }
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Displays the test results in a formatted manner
        /// </summary>
        /// <param name="results">The test results to display</param>
        private static void DisplayTestResults(List<LevelTestResult> results)
        {
            TextDisplayIntegration.DisplaySystem("\n" + new string('=', 80));
            TextDisplayIntegration.DisplaySystem("ITEM GENERATION TEST RESULTS");
            TextDisplayIntegration.DisplaySystem(new string('=', 80));
            
            // Summary statistics
            var totalItems = results.Sum(r => r.TotalItems);
            TextDisplayIntegration.DisplaySystem($"\nTotal Items Generated: {totalItems:N0}");
            TextDisplayIntegration.DisplaySystem($"Levels Tested: {results.Count}");
            TextDisplayIntegration.DisplaySystem($"Items per Level: {results.FirstOrDefault()?.TotalItems ?? 0}");
            
            // Overall rarity distribution
            TextDisplayIntegration.DisplaySystem("\n" + new string('-', 50));
            TextDisplayIntegration.DisplaySystem("OVERALL RARITY DISTRIBUTION");
            TextDisplayIntegration.DisplaySystem(new string('-', 50));
            
            var overallRarity = new Dictionary<string, int>();
            foreach (var result in results)
            {
                foreach (var kvp in result.RarityDistribution)
                {
                    if (overallRarity.ContainsKey(kvp.Key))
                        overallRarity[kvp.Key] += kvp.Value;
                    else
                        overallRarity[kvp.Key] = kvp.Value;
                }
            }
            
            foreach (var kvp in overallRarity.OrderBy(x => GetRarityOrder(x.Key)))
            {
                double percentage = (double)kvp.Value / totalItems * 100;
                TextDisplayIntegration.DisplaySystem($"{kvp.Key,-12}: {kvp.Value,4} items ({percentage,5:F1}%)");
            }
            
            // Overall tier distribution
            TextDisplayIntegration.DisplaySystem("\n" + new string('-', 50));
            TextDisplayIntegration.DisplaySystem("OVERALL TIER DISTRIBUTION");
            TextDisplayIntegration.DisplaySystem(new string('-', 50));
            
            var overallTier = new Dictionary<int, int>();
            foreach (var result in results)
            {
                foreach (var kvp in result.TierDistribution)
                {
                    if (overallTier.ContainsKey(kvp.Key))
                        overallTier[kvp.Key] += kvp.Value;
                    else
                        overallTier[kvp.Key] = kvp.Value;
                }
            }
            
            foreach (var kvp in overallTier.OrderBy(x => x.Key))
            {
                double percentage = (double)kvp.Value / totalItems * 100;
                TextDisplayIntegration.DisplaySystem($"Tier {kvp.Key,-2}: {kvp.Value,4} items ({percentage,5:F1}%)");
            }
            
            // Level-by-level breakdown (first 5 levels)
            TextDisplayIntegration.DisplaySystem("\n" + new string('-', 50));
            TextDisplayIntegration.DisplaySystem("LEVEL-BY-LEVEL BREAKDOWN (Levels 1-5)");
            TextDisplayIntegration.DisplaySystem(new string('-', 50));
            
            for (int i = 0; i < Math.Min(5, results.Count); i++)
            {
                var result = results[i];
                TextDisplayIntegration.DisplaySystem($"\nLevel {result.Level}:");
                
                foreach (var kvp in result.RarityDistribution.OrderBy(x => GetRarityOrder(x.Key)))
                {
                    if (kvp.Value > 0)
                    {
                        double percentage = (double)kvp.Value / result.TotalItems * 100;
                        TextDisplayIntegration.DisplaySystem($"  {kvp.Key,-12}: {kvp.Value,2} items ({percentage,4:F1}%)");
                    }
                }
            }
            
            // Top modifications
            TextDisplayIntegration.DisplaySystem("\n" + new string('-', 50));
            TextDisplayIntegration.DisplaySystem("TOP 10 MODIFICATIONS");
            TextDisplayIntegration.DisplaySystem(new string('-', 50));
            
            var overallMods = new Dictionary<string, int>();
            foreach (var result in results)
            {
                foreach (var kvp in result.ModificationDistribution)
                {
                    if (overallMods.ContainsKey(kvp.Key))
                        overallMods[kvp.Key] += kvp.Value;
                    else
                        overallMods[kvp.Key] = kvp.Value;
                }
            }
            
            var topMods = overallMods.OrderByDescending(x => x.Value).Take(10);
            foreach (var kvp in topMods)
            {
                double percentage = (double)kvp.Value / totalItems * 100;
                TextDisplayIntegration.DisplaySystem($"{kvp.Key,-20}: {kvp.Value,3} items ({percentage,4:F1}%)");
            }
        }
        
        /// <summary>
        /// Saves the test results to a file
        /// </summary>
        /// <param name="results">The test results to save</param>
        private static void SaveTestResults(List<LevelTestResult> results)
        {
            try
            {
                string fileName = "item_generation_test_results.txt";
                using (var writer = new StreamWriter(fileName))
                {
                    writer.WriteLine("ITEM GENERATION TEST RESULTS");
                    writer.WriteLine(new string('=', 80));
                    writer.WriteLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"Total Items: {results.Sum(r => r.TotalItems):N0}");
                    writer.WriteLine($"Levels Tested: {results.Count}");
                    writer.WriteLine();
                    
                    // Overall statistics
                    var totalItems = results.Sum(r => r.TotalItems);
                    
                    // Overall rarity distribution
                    writer.WriteLine("OVERALL RARITY DISTRIBUTION");
                    writer.WriteLine(new string('-', 50));
                    var overallRarity = new Dictionary<string, int>();
                    foreach (var result in results)
                    {
                        foreach (var kvp in result.RarityDistribution)
                        {
                            if (overallRarity.ContainsKey(kvp.Key))
                                overallRarity[kvp.Key] += kvp.Value;
                            else
                                overallRarity[kvp.Key] = kvp.Value;
                        }
                    }
                    
                    foreach (var kvp in overallRarity.OrderBy(x => GetRarityOrder(x.Key)))
                    {
                        double percentage = (double)kvp.Value / totalItems * 100;
                        writer.WriteLine($"{kvp.Key,-12}: {kvp.Value,4} items ({percentage,5:F1}%)");
                    }
                    
                    // Overall tier distribution
                    writer.WriteLine("\nOVERALL TIER DISTRIBUTION");
                    writer.WriteLine(new string('-', 50));
                    var overallTier = new Dictionary<int, int>();
                    foreach (var result in results)
                    {
                        foreach (var kvp in result.TierDistribution)
                        {
                            if (overallTier.ContainsKey(kvp.Key))
                                overallTier[kvp.Key] += kvp.Value;
                            else
                                overallTier[kvp.Key] = kvp.Value;
                        }
                    }
                    
                    foreach (var kvp in overallTier.OrderBy(x => x.Key))
                    {
                        double percentage = (double)kvp.Value / totalItems * 100;
                        writer.WriteLine($"Tier {kvp.Key,-2}: {kvp.Value,4} items ({percentage,5:F1}%)");
                    }
                    
                    // Level-by-level breakdown
                    writer.WriteLine("\nLEVEL-BY-LEVEL BREAKDOWN");
                    writer.WriteLine(new string('-', 50));
                    
                    foreach (var result in results)
                    {
                        writer.WriteLine($"\nLevel {result.Level}:");
                        writer.WriteLine($"  Total Items: {result.TotalItems}");
                        
                        writer.WriteLine("  Rarity Distribution:");
                        foreach (var kvp in result.RarityDistribution.OrderBy(x => GetRarityOrder(x.Key)))
                        {
                            if (kvp.Value > 0)
                            {
                                double percentage = (double)kvp.Value / result.TotalItems * 100;
                                writer.WriteLine($"    {kvp.Key,-12}: {kvp.Value,2} items ({percentage,4:F1}%)");
                            }
                        }
                        
                        writer.WriteLine("  Tier Distribution:");
                        foreach (var kvp in result.TierDistribution.OrderBy(x => x.Key))
                        {
                            if (kvp.Value > 0)
                            {
                                double percentage = (double)kvp.Value / result.TotalItems * 100;
                                writer.WriteLine($"    Tier {kvp.Key,-2}: {kvp.Value,2} items ({percentage,4:F1}%)");
                            }
                        }
                        
                        writer.WriteLine("  Item Type Distribution:");
                        foreach (var kvp in result.ItemTypeDistribution.OrderBy(x => x.Key))
                        {
                            double percentage = (double)kvp.Value / result.TotalItems * 100;
                            writer.WriteLine($"    {kvp.Key,-12}: {kvp.Value,2} items ({percentage,4:F1}%)");
                        }
                    }
                    
                    // Top modifications
                    writer.WriteLine("\nTOP MODIFICATIONS");
                    writer.WriteLine(new string('-', 50));
                    var overallMods = new Dictionary<string, int>();
                    foreach (var result in results)
                    {
                        foreach (var kvp in result.ModificationDistribution)
                        {
                            if (overallMods.ContainsKey(kvp.Key))
                                overallMods[kvp.Key] += kvp.Value;
                            else
                                overallMods[kvp.Key] = kvp.Value;
                        }
                    }
                    
                    var topMods = overallMods.OrderByDescending(x => x.Value).Take(20);
                    foreach (var kvp in topMods)
                    {
                        double percentage = (double)kvp.Value / totalItems * 100;
                        writer.WriteLine($"{kvp.Key,-20}: {kvp.Value,3} items ({percentage,4:F1}%)");
                    }
                    
                    // Top stat bonuses
                    writer.WriteLine("\nTOP STAT BONUSES");
                    writer.WriteLine(new string('-', 50));
                    var overallStatBonuses = new Dictionary<string, int>();
                    foreach (var result in results)
                    {
                        foreach (var kvp in result.StatBonusDistribution)
                        {
                            if (overallStatBonuses.ContainsKey(kvp.Key))
                                overallStatBonuses[kvp.Key] += kvp.Value;
                            else
                                overallStatBonuses[kvp.Key] = kvp.Value;
                        }
                    }
                    
                    var topStatBonuses = overallStatBonuses.OrderByDescending(x => x.Value).Take(20);
                    foreach (var kvp in topStatBonuses)
                    {
                        double percentage = (double)kvp.Value / totalItems * 100;
                        writer.WriteLine($"{kvp.Key,-20}: {kvp.Value,3} items ({percentage,4:F1}%)");
                    }
                    
                    // Top action bonuses
                    writer.WriteLine("\nTOP ACTION BONUSES");
                    writer.WriteLine(new string('-', 50));
                    var overallActionBonuses = new Dictionary<string, int>();
                    foreach (var result in results)
                    {
                        foreach (var kvp in result.ActionBonusDistribution)
                        {
                            if (overallActionBonuses.ContainsKey(kvp.Key))
                                overallActionBonuses[kvp.Key] += kvp.Value;
                            else
                                overallActionBonuses[kvp.Key] = kvp.Value;
                        }
                    }
                    
                    var topActionBonuses = overallActionBonuses.OrderByDescending(x => x.Value).Take(20);
                    foreach (var kvp in topActionBonuses)
                    {
                        double percentage = (double)kvp.Value / totalItems * 100;
                        writer.WriteLine($"{kvp.Key,-20}: {kvp.Value,3} items ({percentage,4:F1}%)");
                    }
                }
            }
            catch (Exception ex)
            {
                TextDisplayIntegration.DisplaySystem($"Error saving test results: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Tests Common item modification chance to verify 25% chance for mods/stat bonuses
        /// </summary>
        public static void RunCommonItemModificationTest()
        {
            TextDisplayIntegration.DisplaySystem("Starting Common Item Modification Test...");
            TextDisplayIntegration.DisplaySystem("This will generate 1000 Common items and verify the 25% chance for modifications.");
            TextDisplayIntegration.DisplaySystem("Press any key to continue or 'q' to quit...");
            
            var key = Console.ReadKey();
            if (key.KeyChar == 'q' || key.KeyChar == 'Q')
            {
                TextDisplayIntegration.DisplaySystem("Test cancelled.");
                return;
            }
            
            TextDisplayIntegration.DisplaySystem("\nGenerating 1000 Common items... This may take a moment.");
            
            int totalCommonItems = 0;
            int commonItemsWithMods = 0;
            int commonItemsWithStatBonuses = 0;
            int commonItemsWithBoth = 0;
            var sampleItems = new List<string>();
            
            // Generate items until we have 1000 Common items
            int itemsGenerated = 0;
            while (totalCommonItems < 1000 && itemsGenerated < 10000) // Safety limit
            {
                var item = LootGenerator.GenerateLoot(1, 1, null, true); // guaranteedLoot = true
                itemsGenerated++;
                
                if (item != null && item.Rarity.Equals("Common", StringComparison.OrdinalIgnoreCase))
                {
                    totalCommonItems++;
                    
                    bool hasMods = item.Modifications.Count > 0;
                    bool hasStatBonuses = item.StatBonuses.Count > 0;
                    
                    if (hasMods) commonItemsWithMods++;
                    if (hasStatBonuses) commonItemsWithStatBonuses++;
                    if (hasMods && hasStatBonuses) commonItemsWithBoth++;
                    
                    // Collect sample items for display
                    if (sampleItems.Count < 20)
                    {
                        string bonusInfo = "";
                        if (hasMods && hasStatBonuses)
                            bonusInfo = " (Mods + Stat Bonuses)";
                        else if (hasMods)
                            bonusInfo = " (Mods)";
                        else if (hasStatBonuses)
                            bonusInfo = " (Stat Bonuses)";
                        else
                            bonusInfo = " (No Bonuses)";
                        
                        sampleItems.Add($"{item.Name}{bonusInfo}");
                    }
                }
            }
            
            // Display results
            TextDisplayIntegration.DisplaySystem("\n" + new string('=', 80));
            TextDisplayIntegration.DisplaySystem("COMMON ITEM MODIFICATION TEST RESULTS");
            TextDisplayIntegration.DisplaySystem(new string('=', 80));
            
            TextDisplayIntegration.DisplaySystem($"Total items generated: {itemsGenerated:N0}");
            TextDisplayIntegration.DisplaySystem($"Common items found: {totalCommonItems:N0}");
            TextDisplayIntegration.DisplaySystem($"Common items with modifications: {commonItemsWithMods:N0}");
            TextDisplayIntegration.DisplaySystem($"Common items with stat bonuses: {commonItemsWithStatBonuses:N0}");
            TextDisplayIntegration.DisplaySystem($"Common items with both: {commonItemsWithBoth:N0}");
            
            if (totalCommonItems > 0)
            {
                double modChance = (double)commonItemsWithMods / totalCommonItems * 100;
                double statBonusChance = (double)commonItemsWithStatBonuses / totalCommonItems * 100;
                double bothChance = (double)commonItemsWithBoth / totalCommonItems * 100;
                
                TextDisplayIntegration.DisplaySystem($"\nModification chance: {modChance:F1}% (Expected: 25.0%)");
                TextDisplayIntegration.DisplaySystem($"Stat bonus chance: {statBonusChance:F1}% (Expected: 25.0%)");
                TextDisplayIntegration.DisplaySystem($"Both bonuses chance: {bothChance:F1}% (Expected: 25.0%)");
                
                // Check if results are within acceptable range (20-30%)
                bool modChanceValid = modChance >= 20.0 && modChance <= 30.0;
                bool statBonusChanceValid = statBonusChance >= 20.0 && statBonusChance <= 30.0;
                
                TextDisplayIntegration.DisplaySystem($"\nValidation Results:");
                TextDisplayIntegration.DisplaySystem($"  Modification chance: {(modChanceValid ? "✓ PASS" : "✗ FAIL")} (20-30% range)");
                TextDisplayIntegration.DisplaySystem($"  Stat bonus chance: {(statBonusChanceValid ? "✓ PASS" : "✗ FAIL")} (20-30% range)");
                
                if (modChanceValid && statBonusChanceValid)
                {
                    TextDisplayIntegration.DisplaySystem("\n🎉 TEST PASSED! Common items have approximately 25% chance for modifications.");
                }
                else
                {
                    TextDisplayIntegration.DisplaySystem("\n❌ TEST FAILED! Common item modification chance is not within expected range.");
                }
            }
            
            TextDisplayIntegration.DisplaySystem("\nSample Common items:");
            foreach (var sample in sampleItems)
            {
                TextDisplayIntegration.DisplaySystem($"  {sample}");
            }
            
            TextDisplayIntegration.DisplaySystem("\nTest completed!");
            TextDisplayIntegration.DisplaySystem("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Tests item naming to ensure proper order (e.g., "Leather Armor of the Wind" not "of the Wind Leather Armor")
        /// </summary>
        public static void RunItemNamingTest()
        {
            TextDisplayIntegration.DisplaySystem("Starting Item Naming Test...");
            TextDisplayIntegration.DisplaySystem("This will generate items and verify that names are properly formatted.");
            TextDisplayIntegration.DisplaySystem("Press any key to continue or 'q' to quit...");
            
            var key = Console.ReadKey();
            if (key.KeyChar == 'q' || key.KeyChar == 'Q')
            {
                TextDisplayIntegration.DisplaySystem("Test cancelled.");
                return;
            }
            
            TextDisplayIntegration.DisplaySystem("\nGenerating test items...");
            
            var testResults = new List<string>();
            int itemsGenerated = 0;
            int itemsWithStatBonuses = 0;
            int itemsWithModifications = 0;
            
            // Generate items until we get some with stat bonuses and modifications
            while (itemsWithStatBonuses < 5 || itemsWithModifications < 5)
            {
                var item = LootGenerator.GenerateLoot(1, 1, null, true); // guaranteedLoot = true
                if (item != null)
                {
                    itemsGenerated++;
                    testResults.Add($"Item {itemsGenerated}: {item.Name}");
                    
                    if (item.StatBonuses.Count > 0)
                    {
                        itemsWithStatBonuses++;
                        TextDisplayIntegration.DisplaySystem($"✓ Found item with stat bonus: {item.Name}");
                    }
                    
                    if (item.Modifications.Count > 0)
                    {
                        itemsWithModifications++;
                        TextDisplayIntegration.DisplaySystem($"✓ Found item with modification: {item.Name}");
                    }
                    
                    // Stop after generating 100 items to avoid infinite loop
                    if (itemsGenerated >= 100)
                    {
                        break;
                    }
                }
            }
            
            // Display results
            TextDisplayIntegration.DisplaySystem("\n" + new string('=', 80));
            TextDisplayIntegration.DisplaySystem("ITEM NAMING TEST RESULTS");
            TextDisplayIntegration.DisplaySystem(new string('=', 80));
            TextDisplayIntegration.DisplaySystem($"Total items generated: {itemsGenerated}");
            TextDisplayIntegration.DisplaySystem($"Items with stat bonuses: {itemsWithStatBonuses}");
            TextDisplayIntegration.DisplaySystem($"Items with modifications: {itemsWithModifications}");
            
            TextDisplayIntegration.DisplaySystem("\nSample item names:");
            foreach (var result in testResults.Take(20))
            {
                TextDisplayIntegration.DisplaySystem($"  {result}");
            }
            
            // Check for specific naming patterns
            TextDisplayIntegration.DisplaySystem("\nChecking for proper naming patterns...");
            bool foundWindItem = false;
            foreach (var result in testResults)
            {
                if (result.Contains("of the Wind"))
                {
                    foundWindItem = true;
                    TextDisplayIntegration.DisplaySystem($"✓ Found 'of the Wind' item: {result}");
                    
                    // Check if it's properly formatted (base name should come before "of the Wind")
                    if (result.Contains("Armor of the Wind") || result.Contains("Weapon of the Wind"))
                    {
                        TextDisplayIntegration.DisplaySystem("  ✓ Properly formatted!");
                    }
                    else
                    {
                        TextDisplayIntegration.DisplaySystem("  ✗ Incorrectly formatted!");
                    }
                    break;
                }
            }
            
            if (!foundWindItem)
            {
                TextDisplayIntegration.DisplaySystem("No 'of the Wind' items found in this test run.");
            }
            
            TextDisplayIntegration.DisplaySystem("\nTest completed!");
            TextDisplayIntegration.DisplaySystem("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Gets the order for rarity sorting
        /// </summary>
        /// <param name="rarity">The rarity name</param>
        /// <returns>Sort order value</returns>
        private static int GetRarityOrder(string rarity)
        {
            return rarity.ToLower() switch
            {
                "common" => 1,
                "uncommon" => 2,
                "rare" => 3,
                "epic" => 4,
                "legendary" => 5,
                _ => 6
            };
        }
    }
    
    /// <summary>
    /// Represents test results for a single level
    /// </summary>
    public class LevelTestResult
    {
        public int Level { get; set; }
        public int TotalItems { get; set; }
        public Dictionary<string, int> RarityDistribution { get; set; } = new Dictionary<string, int>();
        public Dictionary<int, int> TierDistribution { get; set; } = new Dictionary<int, int>();
        public Dictionary<string, int> ModificationDistribution { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> StatBonusDistribution { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> ActionBonusDistribution { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> ItemTypeDistribution { get; set; } = new Dictionary<string, int>();
    }
}
