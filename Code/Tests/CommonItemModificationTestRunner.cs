using System;
using System.Collections.Generic;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests
{
    /// <summary>
    /// Test runner for Common item modification chance verification
    /// </summary>
    public static class CommonItemModificationTestRunner
    {
        /// <summary>
        /// Tests Common item modification chance to verify 25% chance for mods/stat bonuses
        /// </summary>
        public static void RunTest()
        {
            TextDisplayIntegration.DisplaySystem("Starting Common Item Modification Test...");
            TextDisplayIntegration.DisplaySystem("This will generate 1000 Common items and verify the 25% chance for modifications.");
            
            if (!TestHarnessBase.PromptContinue())
                return;
            
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
                var item = LootGenerator.GenerateLoot(1, 1, null, true);
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
            TestHarnessBase.DisplayTestHeader("COMMON ITEM MODIFICATION TEST RESULTS");
            
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
            TestHarnessBase.WaitForContinue();
        }
    }
}

