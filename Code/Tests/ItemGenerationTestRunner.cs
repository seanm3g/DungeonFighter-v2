using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests
{
    /// <summary>
    /// Test runner for item generation analysis across levels 1-20
    /// </summary>
    public static class ItemGenerationTestRunner
    {
        /// <summary>
        /// Runs the item generation test
        /// </summary>
        public static void RunTest()
        {
            TextDisplayIntegration.DisplaySystem("Starting Item Generation Test...");
            TextDisplayIntegration.DisplaySystem("This will generate 100 items at each level from 1-20 and analyze the results.");
            
            if (!TestHarnessBase.PromptContinue())
                return;
            
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
            TestHarnessBase.WaitForContinue();
        }
        
        /// <summary>
        /// Generates the specified number of items for a given level
        /// </summary>
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
                var item = LootGenerator.GenerateLoot(level, 1, null, true);
                
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
        private static void DisplayTestResults(List<LevelTestResult> results)
        {
            TestHarnessBase.DisplayTestHeader("ITEM GENERATION TEST RESULTS");
            
            // Summary statistics
            var totalItems = results.Sum(r => r.TotalItems);
            TextDisplayIntegration.DisplaySystem($"\nTotal Items Generated: {totalItems:N0}");
            TextDisplayIntegration.DisplaySystem($"Levels Tested: {results.Count}");
            TextDisplayIntegration.DisplaySystem($"Items per Level: {results.FirstOrDefault()?.TotalItems ?? 0}");
            
            // Overall rarity distribution
            TestHarnessBase.DisplaySectionHeader("OVERALL RARITY DISTRIBUTION");
            
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
            
            foreach (var kvp in overallRarity.OrderBy(x => TestHarnessBase.GetRarityOrder(x.Key)))
            {
                double percentage = (double)kvp.Value / totalItems * 100;
                TextDisplayIntegration.DisplaySystem($"{kvp.Key,-12}: {kvp.Value,4} items ({percentage,5:F1}%)");
            }
            
            // Overall tier distribution
            TestHarnessBase.DisplaySectionHeader("OVERALL TIER DISTRIBUTION");
            
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
            TestHarnessBase.DisplaySectionHeader("LEVEL-BY-LEVEL BREAKDOWN (Levels 1-5)");
            
            for (int i = 0; i < Math.Min(5, results.Count); i++)
            {
                var result = results[i];
                TextDisplayIntegration.DisplaySystem($"\nLevel {result.Level}:");
                
                foreach (var kvp in result.RarityDistribution.OrderBy(x => TestHarnessBase.GetRarityOrder(x.Key)))
                {
                    if (kvp.Value > 0)
                    {
                        double percentage = (double)kvp.Value / result.TotalItems * 100;
                        TextDisplayIntegration.DisplaySystem($"  {kvp.Key,-12}: {kvp.Value,2} items ({percentage,4:F1}%)");
                    }
                }
            }
            
            // Top modifications
            TestHarnessBase.DisplaySectionHeader("TOP 10 MODIFICATIONS");
            
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
                    
                    foreach (var kvp in overallRarity.OrderBy(x => TestHarnessBase.GetRarityOrder(x.Key)))
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
                        foreach (var kvp in result.RarityDistribution.OrderBy(x => TestHarnessBase.GetRarityOrder(x.Key)))
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

