using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using RPGGame.Utils;
using RPGGame.UI.ColorSystem;
using RPGGame.Tests.Unit;
using RPGGame.Tests;

namespace RPGGame
{
    /// <summary>
    /// Manages test execution and analysis for the game
    /// </summary>
    public static class TestManager
    {
        /// <summary>
        /// Helper method to create template syntax strings without quadruple braces
        /// Uses string.Format to avoid escaping issues in string interpolation
        /// </summary>
        private static string ApplyTemplate(string templateName, string text)
        {
            return string.Format("{{{{0}|{1}}}}", templateName, text);
        }
        /// <summary>
        /// Runs Test 1: Item generation analysis across levels 1-20
        /// Delegates to ItemGenerationTestRunner
        /// </summary>
        public static void RunItemGenerationTest()
        {
            ItemGenerationTestRunner.RunTest();
        }
        
        // NOTE: GenerateItemsForLevel, DisplayTestResults, and SaveTestResults have been moved to ItemGenerationTestRunner
        // Keeping these methods for backwards compatibility but they delegate to the new runner
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
            TextDisplayIntegration.DisplaySystem($"\n{GameConstants.StandardSeparator}");
            TextDisplayIntegration.DisplaySystem("ITEM GENERATION TEST RESULTS");
            TextDisplayIntegration.DisplaySystem(GameConstants.StandardSeparator);
            
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
            
            foreach (var kvp in overallRarity.OrderBy(x => TestHarnessBase.GetRarityOrder(x.Key)))
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
                    writer.WriteLine(GameConstants.StandardSeparator);
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
            catch (System.IO.IOException ex)
            {
                TextDisplayIntegration.DisplaySystem($"Error saving test results: I/O error - {ex.Message}");
            }
            catch (System.UnauthorizedAccessException ex)
            {
                TextDisplayIntegration.DisplaySystem($"Error saving test results: Access denied - {ex.Message}");
            }
            catch (Exception ex)
            {
                TextDisplayIntegration.DisplaySystem($"Error saving test results: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Tests Common item bonus chance to verify 10% chance for stat bonuses only (no modifications)
        /// Delegates to CommonItemModificationTestRunner
        /// </summary>
        public static void RunCommonItemModificationTest()
        {
            CommonItemModificationTestRunner.RunTest();
        }

        /// <summary>
        /// Tests item naming to ensure proper order
        /// Delegates to ItemNamingTestRunner
        /// </summary>
        public static void RunItemNamingTest()
        {
            ItemNamingTestRunner.RunTest();
        }

        /// <summary>
        /// Runs comprehensive ColorParser tests
        /// Delegates to ColorParserTestRunner
        /// </summary>
        public static void RunColorParserTest()
        {
            ColorParserTestRunner.RunTest();
        }
        
        /// <summary>
        /// Runs a quick smoke test of ColorParser
        /// Delegates to ColorParserTestRunner
        /// </summary>
        public static void RunColorParserQuickTest()
        {
            ColorParserTestRunner.RunQuickTest();
        }
        
        /// <summary>
        /// Runs color debugging tools to diagnose spacing issues
        /// Delegates to ColorDebugTestRunner
        /// </summary>
        public static void RunColorDebugTest()
        {
            ColorDebugTestRunner.RunTest();
        }
        
        /// <summary>
        /// Runs Test 7: Combat Log Spacing Test
        /// Delegates to CombatLogSpacingTestRunner
        /// </summary>
        public static void RunCombatLogSpacingTest()
        {
            RPGGame.Tests.Runners.CombatLogSpacingTestRunner.RunTest();
        }
        
        /// <summary>
        /// Test 8: Text System Accuracy Tests
        /// Delegates to TextSystemAccuracyTestRunner
        /// </summary>
        public static void RunTextSystemAccuracyTests()
        {
            RPGGame.Tests.Runners.TextSystemAccuracyTestRunner.RunTest();
        }
        
        /// <summary>
        /// Runs all available tests in sequence
        /// This is the main test runner that ensures all tests are completed
        /// </summary>
        /// <param name="skipUserPrompts">If true, skips interactive prompts (useful for UI mode)</param>
        public static void RunAllTests(bool skipUserPrompts = false)
        {
            TextDisplayIntegration.DisplaySystem(GameConstants.StandardSeparator);
            TextDisplayIntegration.DisplaySystem("    DUNGEON FIGHTER v2 - TEST SUITE");
            TextDisplayIntegration.DisplaySystem(GameConstants.StandardSeparator);
            TextDisplayIntegration.DisplaySystem("This will run all available tests to verify system functionality.");
            
            if (!skipUserPrompts)
            {
                TextDisplayIntegration.DisplaySystem("Press any key to continue or 'q' to quit...");
                var key = Console.ReadKey();
                if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                {
                    TextDisplayIntegration.DisplaySystem("Test suite cancelled.");
                    return;
                }
            }
            
            Console.WriteLine();
            Console.WriteLine();
            
            var testResults = new List<(string testName, bool success, string message)>();
            
            try
            {
                // Test 1: Item Generation Test
                TextDisplayIntegration.DisplaySystem("Running Test 1: Item Generation Test...");
                try
                {
                    RunItemGenerationTest();
                    testResults.Add(("Item Generation Test", true, "Completed successfully"));
                }
                catch (InvalidOperationException ex)
                {
                    testResults.Add(("Item Generation Test", false, $"Test setup failed: {ex.Message}"));
                }
                catch (Exception ex)
                {
                    testResults.Add(("Item Generation Test", false, $"Failed: {ex.Message}"));
                }
                
                // Test 2: Common Item Modification Test
                TextDisplayIntegration.DisplaySystem("\nRunning Test 2: Common Item Modification Test...");
                try
                {
                    RunCommonItemModificationTest();
                    testResults.Add(("Common Item Modification Test", true, "Completed successfully"));
                }
                catch (Exception ex)
                {
                    testResults.Add(("Common Item Modification Test", false, $"Failed: {ex.Message}"));
                }
                
                // Test 3: Item Naming Test
                TextDisplayIntegration.DisplaySystem("\nRunning Test 3: Item Naming Test...");
                try
                {
                    RunItemNamingTest();
                    testResults.Add(("Item Naming Test", true, "Completed successfully"));
                }
                catch (Exception ex)
                {
                    testResults.Add(("Item Naming Test", false, $"Failed: {ex.Message}"));
                }
                
                // Test 4: ColorParser Test
                TextDisplayIntegration.DisplaySystem("\nRunning Test 4: ColorParser Test...");
                try
                {
                    RunColorParserTest();
                    testResults.Add(("ColorParser Test", true, "Completed successfully"));
                }
                catch (Exception ex)
                {
                    testResults.Add(("ColorParser Test", false, $"Failed: {ex.Message}"));
                }
                
                // Test 5: ColorParser Quick Test
                TextDisplayIntegration.DisplaySystem("\nRunning Test 5: ColorParser Quick Test...");
                try
                {
                    RunColorParserQuickTest();
                    testResults.Add(("ColorParser Quick Test", true, "Completed successfully"));
                }
                catch (Exception ex)
                {
                    testResults.Add(("ColorParser Quick Test", false, $"Failed: {ex.Message}"));
                }
                
                // Test 6: Color Debug Test
                TextDisplayIntegration.DisplaySystem("\nRunning Test 6: Color Debug Test...");
                try
                {
                    RunColorDebugTest();
                    testResults.Add(("Color Debug Test", true, "Completed successfully"));
                }
                catch (Exception ex)
                {
                    testResults.Add(("Color Debug Test", false, $"Failed: {ex.Message}"));
                }
                
                // Test 7: Combat Log Spacing Test
                TextDisplayIntegration.DisplaySystem("\nRunning Test 7: Combat Log Spacing Test...");
                try
                {
                    RunCombatLogSpacingTest();
                    testResults.Add(("Combat Log Spacing Test", true, "Completed successfully"));
                }
                catch (Exception ex)
                {
                    testResults.Add(("Combat Log Spacing Test", false, $"Failed: {ex.Message}"));
                }
                
                // Test 8: Text System Accuracy Test
                TextDisplayIntegration.DisplaySystem("\nRunning Test 8: Text System Accuracy Test...");
                try
                {
                    RunTextSystemAccuracyTests();
                    testResults.Add(("Text System Accuracy Test", true, "Completed successfully"));
                }
                catch (Exception ex)
                {
                    testResults.Add(("Text System Accuracy Test", false, $"Failed: {ex.Message}"));
                }
                
                // Test 9: Advanced Action Mechanics Test
                TextDisplayIntegration.DisplaySystem("\nRunning Test 9: Advanced Action Mechanics Test...");
                try
                {
                    RunAdvancedMechanicsTest();
                    testResults.Add(("Advanced Action Mechanics Test", true, "Completed successfully"));
                }
                catch (Exception ex)
                {
                    testResults.Add(("Advanced Action Mechanics Test", false, $"Failed: {ex.Message}"));
                }
                
                // Test 10: Text Delay Configuration Test
                TextDisplayIntegration.DisplaySystem("\nRunning Test 10: Text Delay Configuration Test...");
                try
                {
                    RunTextDelayConfigurationTest();
                    testResults.Add(("Text Delay Configuration Test", true, "Completed successfully"));
                }
                catch (Exception ex)
                {
                    testResults.Add(("Text Delay Configuration Test", false, $"Failed: {ex.Message}"));
                }
                
                // Test 11: Action Editor Test
                TextDisplayIntegration.DisplaySystem("\nRunning Test 11: Action Editor Test...");
                try
                {
                    RunActionEditorTest();
                    testResults.Add(("Action Editor Test", true, "Completed successfully"));
                }
                catch (Exception ex)
                {
                    testResults.Add(("Action Editor Test", false, $"Failed: {ex.Message}"));
                }
                
            }
            catch (InvalidOperationException ex)
            {
                TextDisplayIntegration.DisplaySystem($"\nCritical error during test execution: Test setup failed - {ex.Message}");
            }
            catch (Exception ex)
            {
                TextDisplayIntegration.DisplaySystem($"\nCritical error during test execution: {ex.Message}");
            }
            
            // Display test results summary
            DisplayTestResultsSummary(testResults);
            
            TextDisplayIntegration.DisplaySystem($"\n{GameConstants.StandardSeparator}");
            TextDisplayIntegration.DisplaySystem("    TEST SUITE COMPLETED");
            TextDisplayIntegration.DisplaySystem(GameConstants.StandardSeparator);
            
            if (!skipUserPrompts)
            {
                TextDisplayIntegration.DisplaySystem("Press any key to continue...");
                Console.ReadKey();
            }
        }
        
        /// <summary>
        /// Displays a summary of all test results
        /// </summary>
        /// <param name="results">List of test results</param>
        private static void DisplayTestResultsSummary(List<(string testName, bool success, string message)> results)
        {
            TextDisplayIntegration.DisplaySystem($"\n{GameConstants.StandardSeparator}");
            TextDisplayIntegration.DisplaySystem("TEST RESULTS SUMMARY");
            TextDisplayIntegration.DisplaySystem(GameConstants.StandardSeparator);
            
            int passedTests = 0;
            int failedTests = 0;
            
            foreach (var (testName, success, message) in results)
            {
                string status = success ? "✓ PASS" : "✗ FAIL";
                // Use template syntax for colored status
                string statusText = success 
                    ? $"{TestHarnessBase.ApplyTemplate("success", status)} {testName}"
                    : $"{TestHarnessBase.ApplyTemplate("damage", status)} {testName}";
                TextDisplayIntegration.DisplaySystem(statusText);
                TextDisplayIntegration.DisplaySystem($"    {message}");
                
                if (success)
                    passedTests++;
                else
                    failedTests++;
            }
            
            TextDisplayIntegration.DisplaySystem(new string('-', 60));
            TextDisplayIntegration.DisplaySystem($"Total Tests: {results.Count}");
            TextDisplayIntegration.DisplaySystem(TestHarnessBase.ApplyTemplate("success", $"Passed: {passedTests}"));
            TextDisplayIntegration.DisplaySystem(TestHarnessBase.ApplyTemplate("damage", $"Failed: {failedTests}"));
            
            if (failedTests == 0)
            {
                TextDisplayIntegration.DisplaySystem($"\n{TestHarnessBase.ApplyTemplate("success", "🎉 ALL TESTS PASSED! 🎉")}");
            }
            else
            {
                TextDisplayIntegration.DisplaySystem($"\n{TestHarnessBase.ApplyTemplate("damage", $"⚠️  {failedTests} test(s) failed. Please review the errors above.")}");
            }
        }

        /// <summary>
        /// Runs Test 9: Advanced Action Mechanics Test
        /// Delegates to AdvancedMechanicsTestRunner
        /// </summary>
        public static void RunAdvancedMechanicsTest()
        {
            RPGGame.Tests.Runners.AdvancedMechanicsTestRunner.RunTest();
        }
        
        /// <summary>
        /// Runs Test 10: Text Delay Configuration Test
        /// Tests TextDelayConfiguration loading and all delay settings
        /// </summary>
        public static void RunTextDelayConfigurationTest()
        {
            TextDelayConfigurationTest.RunAllTests();
        }
        
        /// <summary>
        /// Runs Test 11: Action Editor Test
        /// Tests ActionEditor create, update, delete, and validation functionality
        /// </summary>
        public static void RunActionEditorTest()
        {
            ActionEditorTest.RunAllTests();
        }

        // NOTE: GetRarityOrder, TestBasicParsing, TestTemplateExpansion, TestLengthCalculations, and TestEdgeCases
        // have been moved to TestHarnessBase and ColorParserTestRunner respectively
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
