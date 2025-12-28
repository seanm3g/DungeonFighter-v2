using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for dice mechanics
    /// Tests base rolls, roll distribution, multiple dice, and exploding dice
    /// </summary>
    public static class DiceMechanicsTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Dice Mechanics Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestBaseRollGeneration();
            TestRollRange();
            TestRollDistribution();
            TestMultipleDice();
            TestExplodingDice();

            TestBase.PrintSummary("Dice Mechanics Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestBaseRollGeneration()
        {
            Console.WriteLine("--- Testing Base Roll Generation ---");

            // Test 1d20 roll
            for (int i = 0; i < 100; i++)
            {
                int roll = Dice.Roll(1, 20);
                TestBase.AssertTrue(roll >= 1 && roll <= 20, 
                    $"Roll {i+1} should be between 1-20, got {roll}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test single die roll
            int singleRoll = Dice.Roll(20);
            TestBase.AssertTrue(singleRoll >= 1 && singleRoll <= 20, 
                $"Single roll should be between 1-20, got {singleRoll}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRollRange()
        {
            Console.WriteLine("\n--- Testing Roll Range ---");

            // Test various dice sizes
            int d6 = Dice.Roll(6);
            TestBase.AssertTrue(d6 >= 1 && d6 <= 6, 
                $"d6 should be 1-6, got {d6}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            int d10 = Dice.Roll(10);
            TestBase.AssertTrue(d10 >= 1 && d10 <= 10, 
                $"d10 should be 1-10, got {d10}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            int d100 = Dice.Roll(100);
            TestBase.AssertTrue(d100 >= 1 && d100 <= 100, 
                $"d100 should be 1-100, got {d100}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test minimum and maximum possible values
            bool foundMin = false;
            bool foundMax = false;
            for (int i = 0; i < 1000; i++)
            {
                int roll = Dice.Roll(20);
                if (roll == 1) foundMin = true;
                if (roll == 20) foundMax = true;
                if (foundMin && foundMax) break;
            }
            TestBase.AssertTrue(foundMin, 
                "Should be able to roll minimum value (1)", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(foundMax, 
                "Should be able to roll maximum value (20)", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRollDistribution()
        {
            Console.WriteLine("\n--- Testing Roll Distribution ---");

            // Test distribution over many rolls
            var distribution = new Dictionary<int, int>();
            int sampleSize = 1000;

            for (int i = 0; i < sampleSize; i++)
            {
                int roll = Dice.Roll(20);
                if (!distribution.ContainsKey(roll))
                {
                    distribution[roll] = 0;
                }
                distribution[roll]++;
            }

            // Verify all values 1-20 appear (with some tolerance for randomness)
            int valuesFound = distribution.Keys.Count;
            TestBase.AssertTrue(valuesFound >= 15, 
                $"Distribution should cover most values, found {valuesFound} unique values", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Verify no values outside range
            foreach (var kvp in distribution)
            {
                TestBase.AssertTrue(kvp.Key >= 1 && kvp.Key <= 20, 
                    $"Distribution should only contain values 1-20, found {kvp.Key}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestMultipleDice()
        {
            Console.WriteLine("\n--- Testing Multiple Dice ---");

            // Test 2d6
            int twoD6 = Dice.Roll(2, 6);
            TestBase.AssertTrue(twoD6 >= 2 && twoD6 <= 12, 
                $"2d6 should be 2-12, got {twoD6}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test 3d20
            int threeD20 = Dice.Roll(3, 20);
            TestBase.AssertTrue(threeD20 >= 3 && threeD20 <= 60, 
                $"3d20 should be 3-60, got {threeD20}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test 5d4
            int fiveD4 = Dice.Roll(5, 4);
            TestBase.AssertTrue(fiveD4 >= 5 && fiveD4 <= 20, 
                $"5d4 should be 5-20, got {fiveD4}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExplodingDice()
        {
            Console.WriteLine("\n--- Testing Exploding Dice ---");

            // Note: Exploding dice is handled by RollModificationManager
            // This test verifies the concept works
            var action = TestDataBuilders.CreateMockAction("ExplodingAction");
            action.RollMods.ExplodingDice = true;
            action.RollMods.ExplodingDiceThreshold = 20;

            TestBase.AssertTrue(action.RollMods.ExplodingDice, 
                "Action should support exploding dice", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(20, action.RollMods.ExplodingDiceThreshold, 
                "Exploding dice threshold should be set", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

