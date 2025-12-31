using System;
using System.Collections.Generic;
using RPGGame.Tests;
using RPGGame.Actions.RollModification;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for dice roll mechanics
    /// Tests all dice roll functions, combo rolls, modification rolls, and distribution
    /// </summary>
    public static class DiceRollMechanicsTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Dice Roll Mechanics Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestBasicRolls();
            TestComboActionRolls();
            TestComboContinueRolls();
            TestModificationRolls();
            TestMultipleModificationRolls();
            TestLootDropRolls();
            TestRollDistribution();
            TestRollBounds();
            TestMultiDiceRoller();
            TestDiceRollResultMatching();

            TestBase.PrintSummary("Dice Roll Mechanics Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestBasicRolls()
        {
            Console.WriteLine("--- Testing Basic Rolls ---");

            // Test 1d20
            for (int i = 0; i < 50; i++)
            {
                int roll = Dice.Roll(20);
                TestBase.AssertTrue(roll >= 1 && roll <= 20, 
                    $"Roll {i+1} should be 1-20, got {roll}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test multiple dice
            for (int i = 0; i < 20; i++)
            {
                int roll = Dice.Roll(3, 6); // 3d6
                TestBase.AssertTrue(roll >= 3 && roll <= 18, 
                    $"3d6 roll {i+1} should be 3-18, got {roll}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestComboActionRolls()
        {
            Console.WriteLine("\n--- Testing Combo Action Rolls ---");

            int failCount = 0;
            int normalCount = 0;
            int comboCount = 0;

            for (int i = 0; i < 100; i++)
            {
                var result = Dice.RollComboAction();
                TestBase.AssertTrue(result.Roll >= 1, 
                    $"Combo roll {i+1} should be >= 1, got {result.Roll}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                if (result.Roll <= 5) failCount++;
                else if (result.Roll <= 13) normalCount++;
                else comboCount++;
            }

            // Verify all three categories occur
            TestBase.AssertTrue(failCount > 0, 
                "Should have some fail rolls (1-5)", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(normalCount > 0, 
                "Should have some normal rolls (6-13)", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(comboCount > 0, 
                "Should have some combo rolls (14-20)", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestComboContinueRolls()
        {
            Console.WriteLine("\n--- Testing Combo Continue Rolls ---");

            int successCount = 0;
            int failCount = 0;

            for (int i = 0; i < 100; i++)
            {
                var result = Dice.RollComboContinue();
                TestBase.AssertTrue(result.Roll >= 1, 
                    $"Combo continue roll {i+1} should be >= 1, got {result.Roll}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                if (result.Success) successCount++;
                else failCount++;
            }

            // Both success and failure should occur
            TestBase.AssertTrue(successCount > 0, 
                "Should have some successful combo continues (14+)", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(failCount > 0, 
                "Should have some failed combo continues (<14)", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestModificationRolls()
        {
            Console.WriteLine("\n--- Testing Modification Rolls ---");

            // Test modification rolls for different tiers
            for (int tier = 1; tier <= 5; tier++)
            {
                int modRoll = Dice.RollModification(tier);
                TestBase.AssertTrue(modRoll >= 1 && modRoll <= 31, 
                    $"Modification roll for tier {tier} should be 1-31, got {modRoll}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestMultipleModificationRolls()
        {
            Console.WriteLine("\n--- Testing Multiple Modification Rolls ---");

            var rarities = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };

            foreach (var rarity in rarities)
            {
                var mods = Dice.RollMultipleModifications(5, rarity);
                TestBase.AssertTrue(mods.Count > 0, 
                    $"{rarity} items should have at least one modification", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                foreach (var mod in mods)
                {
                    TestBase.AssertTrue(mod >= 1 && mod <= 31, 
                        $"Modification should be 1-31, got {mod}", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
        }

        private static void TestLootDropRolls()
        {
            Console.WriteLine("\n--- Testing Loot Drop Rolls ---");

            int dropCount = 0;
            int noDropCount = 0;

            for (int i = 0; i < 100; i++)
            {
                bool dropped = Dice.RollLootDrop(5, 3); // Enemy level 5, player level 3
                if (dropped) dropCount++;
                else noDropCount++;
            }

            // Both outcomes should occur
            TestBase.AssertTrue(dropCount > 0, 
                "Should have some loot drops", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(noDropCount > 0, 
                "Should have some no-drop results", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRollDistribution()
        {
            Console.WriteLine("\n--- Testing Roll Distribution ---");

            var distribution = new Dictionary<int, int>();
            int sampleSize = 1000;

            for (int i = 0; i < sampleSize; i++)
            {
                int roll = Dice.Roll(20);
                if (!distribution.ContainsKey(roll))
                    distribution[roll] = 0;
                distribution[roll]++;
            }

            // Verify all values 1-20 appear
            for (int i = 1; i <= 20; i++)
            {
                TestBase.AssertTrue(distribution.ContainsKey(i), 
                    $"Distribution should include value {i}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestRollBounds()
        {
            Console.WriteLine("\n--- Testing Roll Bounds ---");

            // Test minimum possible roll
            bool foundMin = false;
            for (int i = 0; i < 100; i++)
            {
                if (Dice.Roll(20) == 1)
                {
                    foundMin = true;
                    break;
                }
            }
            TestBase.AssertTrue(foundMin, 
                "Should be able to roll minimum (1)", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test maximum possible roll
            bool foundMax = false;
            for (int i = 0; i < 100; i++)
            {
                if (Dice.Roll(20) == 20)
                {
                    foundMax = true;
                    break;
                }
            }
            TestBase.AssertTrue(foundMax, 
                "Should be able to roll maximum (20)", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultiDiceRoller()
        {
            Console.WriteLine("\n--- Testing Multi-Dice Roller ---");

            // Test TakeHighest
            int highest = MultiDiceRoller.RollMultipleDice(3, 20, MultiDiceRoller.DiceSelectionMode.TakeHighest);
            TestBase.AssertTrue(highest >= 1 && highest <= 20, 
                $"TakeHighest should be 1-20, got {highest}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test TakeLowest
            int lowest = MultiDiceRoller.RollMultipleDice(3, 20, MultiDiceRoller.DiceSelectionMode.TakeLowest);
            TestBase.AssertTrue(lowest >= 1 && lowest <= 20, 
                $"TakeLowest should be 1-20, got {lowest}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test TakeAverage
            int average = MultiDiceRoller.RollMultipleDice(3, 20, MultiDiceRoller.DiceSelectionMode.TakeAverage);
            TestBase.AssertTrue(average >= 1 && average <= 20, 
                $"TakeAverage should be 1-20, got {average}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test Sum
            int sum = MultiDiceRoller.RollMultipleDice(3, 6, MultiDiceRoller.DiceSelectionMode.Sum);
            TestBase.AssertTrue(sum >= 3 && sum <= 18, 
                $"Sum of 3d6 should be 3-18, got {sum}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Tests that dice rolls match to the appropriate result categorization
        /// Verifies that DiceResult properties (Success, ComboTriggered, Description) 
        /// correctly match the roll value according to the game rules
        /// </summary>
        private static void TestDiceRollResultMatching()
        {
            Console.WriteLine("\n--- Testing Dice Roll Result Matching ---");

            // Test RollComboAction results match expected categorization
            int failMatches = 0;
            int normalMatches = 0;
            int comboMatches = 0;
            int totalRolls = 0;

            for (int i = 0; i < 500; i++)
            {
                var result = Dice.RollComboAction();
                totalRolls++;

                // Verify result matches expected categorization based on roll value
                if (result.Roll <= 5)
                {
                    // Fail range: 1-5
                    TestBase.AssertTrue(!result.Success, 
                        $"Roll {result.Roll} should have Success=false (Fail range), got Success={result.Success}", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(!result.ComboTriggered, 
                        $"Roll {result.Roll} should have ComboTriggered=false (Fail range), got ComboTriggered={result.ComboTriggered}", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertEqual("Fail", result.Description, 
                        $"Roll {result.Roll} should have Description='Fail', got '{result.Description}'", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    failMatches++;
                }
                else if (result.Roll <= 13)
                {
                    // Normal attack range: 6-13
                    TestBase.AssertTrue(result.Success, 
                        $"Roll {result.Roll} should have Success=true (Normal Attack range), got Success={result.Success}", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(!result.ComboTriggered, 
                        $"Roll {result.Roll} should have ComboTriggered=false (Normal Attack range), got ComboTriggered={result.ComboTriggered}", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertEqual("Normal Attack", result.Description, 
                        $"Roll {result.Roll} should have Description='Normal Attack', got '{result.Description}'", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    normalMatches++;
                }
                else // result.Roll >= 14
                {
                    // Combo attack range: 14-20
                    TestBase.AssertTrue(result.Success, 
                        $"Roll {result.Roll} should have Success=true (Combo Attack range), got Success={result.Success}", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(result.ComboTriggered, 
                        $"Roll {result.Roll} should have ComboTriggered=true (Combo Attack range), got ComboTriggered={result.ComboTriggered}", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertEqual("Combo Attack", result.Description, 
                        $"Roll {result.Roll} should have Description='Combo Attack', got '{result.Description}'", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    comboMatches++;
                }
            }

            // Verify we got rolls in all three categories
            TestBase.AssertTrue(failMatches > 0, 
                $"Should have some fail rolls (1-5), got {failMatches} out of {totalRolls}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(normalMatches > 0, 
                $"Should have some normal attack rolls (6-13), got {normalMatches} out of {totalRolls}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(comboMatches > 0, 
                $"Should have some combo attack rolls (14-20), got {comboMatches} out of {totalRolls}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test RollComboAction with bonuses
            Console.WriteLine("\n--- Testing RollComboAction with Bonuses ---");
            for (int bonus = -5; bonus <= 10; bonus += 5)
            {
                var result = Dice.RollComboAction(bonus);
                int adjustedRoll = result.Roll - bonus; // Reverse the bonus to get base roll
                
                // Verify the result matches the adjusted roll categorization
                if (result.Roll <= 5)
                {
                    TestBase.AssertTrue(!result.Success, 
                        $"Roll {result.Roll} (base {adjustedRoll} + bonus {bonus}) should have Success=false", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(!result.ComboTriggered, 
                        $"Roll {result.Roll} (base {adjustedRoll} + bonus {bonus}) should have ComboTriggered=false", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
                else if (result.Roll <= 13)
                {
                    TestBase.AssertTrue(result.Success, 
                        $"Roll {result.Roll} (base {adjustedRoll} + bonus {bonus}) should have Success=true", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(!result.ComboTriggered, 
                        $"Roll {result.Roll} (base {adjustedRoll} + bonus {bonus}) should have ComboTriggered=false", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
                else
                {
                    TestBase.AssertTrue(result.Success, 
                        $"Roll {result.Roll} (base {adjustedRoll} + bonus {bonus}) should have Success=true", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(result.ComboTriggered, 
                        $"Roll {result.Roll} (base {adjustedRoll} + bonus {bonus}) should have ComboTriggered=true", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }

            // Test RollComboContinue results match expected categorization
            Console.WriteLine("\n--- Testing RollComboContinue Result Matching ---");
            int continueSuccessMatches = 0;
            int continueFailMatches = 0;
            totalRolls = 0;

            for (int i = 0; i < 500; i++)
            {
                var result = Dice.RollComboContinue();
                totalRolls++;

                // Verify result matches expected categorization based on roll value
                if (result.Roll >= 14)
                {
                    // Success range: 14+
                    TestBase.AssertTrue(result.Success, 
                        $"Combo continue roll {result.Roll} should have Success=true (14+), got Success={result.Success}", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(!result.ComboTriggered, 
                        $"Combo continue roll {result.Roll} should have ComboTriggered=false, got ComboTriggered={result.ComboTriggered}", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertEqual("Combo Continue", result.Description, 
                        $"Combo continue roll {result.Roll} should have Description='Combo Continue', got '{result.Description}'", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    continueSuccessMatches++;
                }
                else // result.Roll < 14
                {
                    // Fail range: < 14
                    TestBase.AssertTrue(!result.Success, 
                        $"Combo continue roll {result.Roll} should have Success=false (<14), got Success={result.Success}", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(!result.ComboTriggered, 
                        $"Combo continue roll {result.Roll} should have ComboTriggered=false, got ComboTriggered={result.ComboTriggered}", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertEqual("Combo Fail", result.Description, 
                        $"Combo continue roll {result.Roll} should have Description='Combo Fail', got '{result.Description}'", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    continueFailMatches++;
                }
            }

            // Verify we got both success and failure results
            TestBase.AssertTrue(continueSuccessMatches > 0, 
                $"Should have some successful combo continues (14+), got {continueSuccessMatches} out of {totalRolls}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(continueFailMatches > 0, 
                $"Should have some failed combo continues (<14), got {continueFailMatches} out of {totalRolls}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test RollComboContinue with bonuses
            Console.WriteLine("\n--- Testing RollComboContinue with Bonuses ---");
            for (int bonus = -5; bonus <= 10; bonus += 5)
            {
                var result = Dice.RollComboContinue(bonus);
                
                // Verify the result matches the roll categorization
                if (result.Roll >= 14)
                {
                    TestBase.AssertTrue(result.Success, 
                        $"Combo continue roll {result.Roll} (with bonus {bonus}) should have Success=true", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertEqual("Combo Continue", result.Description, 
                        $"Combo continue roll {result.Roll} (with bonus {bonus}) should have Description='Combo Continue'", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
                else
                {
                    TestBase.AssertTrue(!result.Success, 
                        $"Combo continue roll {result.Roll} (with bonus {bonus}) should have Success=false", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertEqual("Combo Fail", result.Description, 
                        $"Combo continue roll {result.Roll} (with bonus {bonus}) should have Description='Combo Fail'", 
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
        }
    }
}

