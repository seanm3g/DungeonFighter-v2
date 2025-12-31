using System;
using System.Collections.Generic;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests
{
    /// <summary>
    /// Test runner for Common item bonus chance verification
    /// </summary>
    public static class CommonItemModificationTestRunner
    {
        /// <summary>
        /// Tests Common item bonus chance to verify 10% chance for stat bonuses only (no modifications)
        /// </summary>
        public static void RunTest()
        {
            TextDisplayIntegration.DisplaySystem("Starting Common Item Bonus Test...");
            TextDisplayIntegration.DisplaySystem("This will generate 1000 Common items and verify the 10% chance for stat bonuses (no modifications).");
            
            if (!TestHarnessBase.PromptContinue())
                return;
            
            TextDisplayIntegration.DisplaySystem("\nGenerating 1000 Common items... This may take a moment.");
            
            int totalCommonItems = 0;
            int commonItemsWithMods = 0;
            int commonItemsWithStatBonuses = 0;
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
                    
                    // Collect sample items for display
                    if (sampleItems.Count < 20)
                    {
                        string bonusInfo = "";
                        if (hasMods)
                            bonusInfo = " (⚠ HAS MODIFICATIONS - ERROR!)";
                        else if (hasStatBonuses)
                            bonusInfo = " (Stat Bonuses)";
                        else
                            bonusInfo = " (No Bonuses)";
                        
                        sampleItems.Add($"{item.Name}{bonusInfo}");
                    }
                }
            }
            
            // Display results
            TestHarnessBase.DisplayTestHeader("COMMON ITEM BONUS TEST RESULTS");
            
            TextDisplayIntegration.DisplaySystem($"Total items generated: {itemsGenerated:N0}");
            TextDisplayIntegration.DisplaySystem($"Common items found: {totalCommonItems:N0}");
            TextDisplayIntegration.DisplaySystem($"Common items with modifications: {commonItemsWithMods:N0} (Expected: 0)");
            TextDisplayIntegration.DisplaySystem($"Common items with stat bonuses: {commonItemsWithStatBonuses:N0}");
            
            if (totalCommonItems > 0)
            {
                double modChance = (double)commonItemsWithMods / totalCommonItems * 100;
                double statBonusChance = (double)commonItemsWithStatBonuses / totalCommonItems * 100;
                
                TextDisplayIntegration.DisplaySystem($"\nModification chance: {modChance:F1}% (Expected: 0.0% - Common items should NEVER have modifications)");
                TextDisplayIntegration.DisplaySystem($"Stat bonus chance: {statBonusChance:F1}% (Expected: ~10.0%)");
                
                // Check if results are within acceptable range
                bool modChanceValid = modChance == 0.0; // Should be exactly 0%
                bool statBonusChanceValid = statBonusChance >= 5.0 && statBonusChance <= 15.0; // 10% ± 5% tolerance
                
                TextDisplayIntegration.DisplaySystem($"\nValidation Results:");
                TextDisplayIntegration.DisplaySystem($"  No modifications: {(modChanceValid ? "✓ PASS" : "✗ FAIL")} (Expected: 0%)");
                TextDisplayIntegration.DisplaySystem($"  Stat bonus chance: {(statBonusChanceValid ? "✓ PASS" : "✗ FAIL")} (Expected: ~10%, tolerance: 5-15%)");
                
                if (modChanceValid && statBonusChanceValid)
                {
                    TextDisplayIntegration.DisplaySystem("\n🎉 TEST PASSED! Common items have 10% chance for stat bonuses and never have modifications.");
                }
                else
                {
                    if (!modChanceValid)
                        TextDisplayIntegration.DisplaySystem("\n❌ TEST FAILED! Common items should NEVER have modifications.");
                    if (!statBonusChanceValid)
                        TextDisplayIntegration.DisplaySystem("\n❌ TEST FAILED! Common item stat bonus chance is not within expected range (5-15%).");
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

