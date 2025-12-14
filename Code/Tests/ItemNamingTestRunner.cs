using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests
{
    /// <summary>
    /// Test runner for item naming verification
    /// </summary>
    public static class ItemNamingTestRunner
    {
        /// <summary>
        /// Tests item naming to ensure proper order (e.g., "Leather Armor of the Wind" not "of the Wind Leather Armor")
        /// </summary>
        public static void RunTest()
        {
            TextDisplayIntegration.DisplaySystem("Starting Item Naming Test...");
            TextDisplayIntegration.DisplaySystem("This will generate items and verify that names are properly formatted.");
            
            if (!TestHarnessBase.PromptContinue())
                return;
            
            TextDisplayIntegration.DisplaySystem("\nGenerating test items...");
            
            var testResults = new List<string>();
            int itemsGenerated = 0;
            int itemsWithStatBonuses = 0;
            int itemsWithModifications = 0;
            
            // Generate items until we get some with stat bonuses and modifications
            while (itemsWithStatBonuses < 5 || itemsWithModifications < 5)
            {
                var item = LootGenerator.GenerateLoot(1, 1, null, true);
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
            TestHarnessBase.DisplayTestHeader("ITEM NAMING TEST RESULTS");
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
            TestHarnessBase.WaitForContinue();
        }
    }
}

