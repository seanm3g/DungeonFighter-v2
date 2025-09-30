using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Comprehensive test for loot generation system
    /// </summary>
    public class LootGenerationTest
    {
        public static void RunLootGenerationTests()
        {
            Console.WriteLine("=== LOOT GENERATION SYSTEM TEST ===");
            Console.WriteLine();

            // Test 1: Basic loot generation
            TestBasicLootGeneration();
            
            // Test 2: Guaranteed loot generation
            TestGuaranteedLootGeneration();
            
            // Test 3: Loot scaling and tier distribution
            TestLootScaling();
            
            // Test 4: Rarity distribution
            TestRarityDistribution();
            
            // Test 5: Data integrity check
            TestDataIntegrity();
            
            Console.WriteLine("=== LOOT GENERATION TEST COMPLETE ===");
        }

        private static void TestBasicLootGeneration()
        {
            Console.WriteLine("Test 1: Basic Loot Generation");
            Console.WriteLine("------------------------------");
            
            // Initialize loot generator
            LootGenerator.Initialize();
            
            // Create a test character
            var testCharacter = new Character("TestHero", 5);
            
            // Test multiple loot generations
            int successCount = 0;
            int totalTests = 10;
            
            for (int i = 0; i < totalTests; i++)
            {
                var loot = LootGenerator.GenerateLoot(5, 5, testCharacter, false);
                if (loot != null)
                {
                    successCount++;
                    Console.WriteLine($"  Generated: {loot.Name} (Tier {loot.Tier}, Rarity: {loot.Rarity})");
                }
                else
                {
                    Console.WriteLine($"  No loot generated (roll failed)");
                }
            }
            
            Console.WriteLine($"  Success Rate: {successCount}/{totalTests} ({(double)successCount/totalTests*100:F1}%)");
            Console.WriteLine();
        }

        private static void TestGuaranteedLootGeneration()
        {
            Console.WriteLine("Test 2: Guaranteed Loot Generation");
            Console.WriteLine("----------------------------------");
            
            var testCharacter = new Character("TestHero", 5);
            int successCount = 0;
            int totalTests = 10;
            
            for (int i = 0; i < totalTests; i++)
            {
                var loot = LootGenerator.GenerateLoot(5, 5, testCharacter, true);
                if (loot != null)
                {
                    successCount++;
                    Console.WriteLine($"  Generated: {loot.Name} (Tier {loot.Tier}, Rarity: {loot.Rarity})");
                    
                    // Check if item has reasonable stats
                    if (loot is WeaponItem weapon)
                    {
                        Console.WriteLine($"    Weapon - Damage: {weapon.BaseDamage}, Speed: {weapon.BaseAttackSpeed:F2}");
                        if (weapon.BaseDamage > 1000 || weapon.BaseAttackSpeed > 1000)
                        {
                            Console.WriteLine($"    ⚠️  WARNING: Unusually high weapon stats detected!");
                        }
                    }
                    else if (loot is HeadItem headArmor)
                    {
                        Console.WriteLine($"    Head Armor - Armor: {headArmor.Armor}");
                        if (headArmor.Armor > 1000)
                        {
                            Console.WriteLine($"    ⚠️  WARNING: Unusually high armor value detected!");
                        }
                    }
                    else if (loot is ChestItem chestArmor)
                    {
                        Console.WriteLine($"    Chest Armor - Armor: {chestArmor.Armor}");
                        if (chestArmor.Armor > 1000)
                        {
                            Console.WriteLine($"    ⚠️  WARNING: Unusually high armor value detected!");
                        }
                    }
                    else if (loot is FeetItem feetArmor)
                    {
                        Console.WriteLine($"    Feet Armor - Armor: {feetArmor.Armor}");
                        if (feetArmor.Armor > 1000)
                        {
                            Console.WriteLine($"    ⚠️  WARNING: Unusually high armor value detected!");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"  ❌ FAILED: Guaranteed loot returned null!");
                }
            }
            
            Console.WriteLine($"  Success Rate: {successCount}/{totalTests} ({(double)successCount/totalTests*100:F1}%)");
            if (successCount < totalTests)
            {
                Console.WriteLine($"  ❌ CRITICAL: Guaranteed loot generation is failing!");
            }
            Console.WriteLine();
        }

        private static void TestLootScaling()
        {
            Console.WriteLine("Test 3: Loot Scaling and Tier Distribution");
            Console.WriteLine("------------------------------------------");
            
            var testCharacter = new Character("TestHero", 10);
            
            // Test different level combinations
            var testCases = new[]
            {
                new { PlayerLevel = 1, DungeonLevel = 1, Description = "Level 1 vs Level 1" },
                new { PlayerLevel = 5, DungeonLevel = 3, Description = "Level 5 vs Level 3" },
                new { PlayerLevel = 10, DungeonLevel = 10, Description = "Level 10 vs Level 10" },
                new { PlayerLevel = 15, DungeonLevel = 12, Description = "Level 15 vs Level 12" }
            };
            
            foreach (var testCase in testCases)
            {
                Console.WriteLine($"  Testing: {testCase.Description}");
                
                var loot = LootGenerator.GenerateLoot(testCase.PlayerLevel, testCase.DungeonLevel, testCharacter, true);
                if (loot != null)
                {
                    Console.WriteLine($"    Generated: {loot.Name} (Tier {loot.Tier})");
                    
                    // Check if tier makes sense for the level difference
                    int levelDiff = testCase.PlayerLevel - testCase.DungeonLevel;
                    if (levelDiff <= -3 && loot.Tier > 1)
                    {
                        Console.WriteLine($"    ⚠️  WARNING: High tier loot for low level difference!");
                    }
                    else if (levelDiff >= 3 && loot.Tier < 3)
                    {
                        Console.WriteLine($"    ⚠️  WARNING: Low tier loot for high level difference!");
                    }
                }
                else
                {
                    Console.WriteLine($"    ❌ FAILED: No loot generated");
                }
            }
            Console.WriteLine();
        }

        private static void TestRarityDistribution()
        {
            Console.WriteLine("Test 4: Rarity Distribution");
            Console.WriteLine("---------------------------");
            
            var testCharacter = new Character("TestHero", 10);
            var rarityCounts = new Dictionary<string, int>();
            
            int totalTests = 100;
            for (int i = 0; i < totalTests; i++)
            {
                var loot = LootGenerator.GenerateLoot(10, 10, testCharacter, true);
                if (loot != null)
                {
                    string rarity = loot.Rarity ?? "Unknown";
                    rarityCounts[rarity] = rarityCounts.GetValueOrDefault(rarity, 0) + 1;
                }
            }
            
            Console.WriteLine($"  Rarity Distribution (out of {totalTests} items):");
            foreach (var kvp in rarityCounts.OrderByDescending(x => x.Value))
            {
                double percentage = (double)kvp.Value / totalTests * 100;
                Console.WriteLine($"    {kvp.Key}: {kvp.Value} ({percentage:F1}%)");
            }
            Console.WriteLine();
        }

        private static void TestDataIntegrity()
        {
            Console.WriteLine("Test 5: Data Integrity Check");
            Console.WriteLine("----------------------------");
            
            // Check weapon data
            Console.WriteLine("  Checking Weapon Data:");
            var weaponData = LoadWeaponData();
            if (weaponData != null)
            {
                var problematicWeapons = weaponData.Where(w => w.BaseDamage > 1000 || w.AttackSpeed > 1000).ToList();
                if (problematicWeapons.Any())
                {
                    Console.WriteLine($"    ❌ CRITICAL: Found {problematicWeapons.Count} weapons with inflated stats:");
                    foreach (var weapon in problematicWeapons.Take(5)) // Show first 5
                    {
                        Console.WriteLine($"      {weapon.Name}: Damage={weapon.BaseDamage}, Speed={weapon.AttackSpeed:F2}");
                    }
                    if (problematicWeapons.Count > 5)
                    {
                        Console.WriteLine($"      ... and {problematicWeapons.Count - 5} more");
                    }
                }
                else
                {
                    Console.WriteLine($"    ✅ Weapon data looks reasonable");
                }
            }
            else
            {
                Console.WriteLine($"    ❌ FAILED: Could not load weapon data");
            }
            
            // Check armor data
            Console.WriteLine("  Checking Armor Data:");
            var armorData = LoadArmorData();
            if (armorData != null)
            {
                var problematicArmor = armorData.Where(a => a.Armor > 1000).ToList();
                if (problematicArmor.Any())
                {
                    Console.WriteLine($"    ❌ CRITICAL: Found {problematicArmor.Count} armor pieces with inflated stats:");
                    foreach (var armor in problematicArmor.Take(5)) // Show first 5
                    {
                        Console.WriteLine($"      {armor.Name}: Armor={armor.Armor}");
                    }
                    if (problematicArmor.Count > 5)
                    {
                        Console.WriteLine($"      ... and {problematicArmor.Count - 5} more");
                    }
                }
                else
                {
                    Console.WriteLine($"    ✅ Armor data looks reasonable");
                }
            }
            else
            {
                Console.WriteLine($"    ❌ FAILED: Could not load armor data");
            }
            
            Console.WriteLine();
        }

        private static List<WeaponData>? LoadWeaponData()
        {
            try
            {
                string? filePath = JsonLoader.FindGameDataFile("Weapons.json");
                if (filePath != null)
                {
                    return JsonLoader.LoadJsonList<WeaponData>(filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Error loading weapon data: {ex.Message}");
            }
            return null;
        }

        private static List<ArmorData>? LoadArmorData()
        {
            try
            {
                string? filePath = JsonLoader.FindGameDataFile("Armor.json");
                if (filePath != null)
                {
                    return JsonLoader.LoadJsonList<ArmorData>(filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Error loading armor data: {ex.Message}");
            }
            return null;
        }
    }
}
