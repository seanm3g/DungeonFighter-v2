using System;
using RPGGame;
using RPGGame.Actions.Execution; // For Dice
using RPGGame.Tests; // For TestBase

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Tests for Dice Roll Mechanics
    /// </summary>
    public static class ComboDiceRollTestsDiceMechanics
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            Console.WriteLine("--- Dice Roll Mechanics Tests ---");
            TestDiceRollMechanics();
            TestDiceRollComboAction();
            TestDiceRollComboContinue();
            TestDiceRollRanges();
            TestBase.PrintSummary("Dice Roll Mechanics", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestDiceRollMechanics()
        {
            Console.WriteLine("Testing Dice Roll Mechanics...");

            // Test basic dice roll
            int roll = Dice.Roll(20);
            TestBase.AssertTrue(roll >= 1 && roll <= 20, $"Dice roll should be between 1 and 20, got: {roll}", ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test multiple dice rolls
            int roll2 = Dice.Roll(2, 20);
            TestBase.AssertTrue(roll2 >= 2 && roll2 <= 40, $"2d20 roll should be between 2 and 40, got: {roll2}", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDiceRollComboAction()
        {
            Console.WriteLine("Testing Dice Roll Combo Action...");

            // Test multiple rolls to verify ranges
            int failCount = 0;
            int normalCount = 0;
            int comboCount = 0;

            for (int i = 0; i < 1000; i++)
            {
                var result = Dice.RollComboAction(0);

                if (result.Roll <= 5)
                {
                    failCount++;
                    TestBase.AssertTrue(!result.Success, $"Roll {result.Roll} should fail", ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(!result.ComboTriggered, $"Roll {result.Roll} should not trigger combo", ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
                else if (result.Roll <= 13)
                {
                    normalCount++;
                    TestBase.AssertTrue(result.Success, $"Roll {result.Roll} should succeed", ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(!result.ComboTriggered, $"Roll {result.Roll} should not trigger combo", ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
                else
                {
                    comboCount++;
                    TestBase.AssertTrue(result.Success, $"Roll {result.Roll} should succeed", ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(result.ComboTriggered, $"Roll {result.Roll} should trigger combo", ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }

            TestBase.AssertTrue(failCount > 0, "Should have some fail rolls (1-5)", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(normalCount > 0, "Should have some normal rolls (6-13)", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(comboCount > 0, "Should have some combo rolls (14-20)", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDiceRollComboContinue()
        {
            Console.WriteLine("Testing Dice Roll Combo Continue...");

            int successCount = 0;
            int failCount = 0;

            for (int i = 0; i < 1000; i++)
            {
                var result = Dice.RollComboContinue(0);

                if (result.Roll >= 14)
                {
                    successCount++;
                    TestBase.AssertTrue(result.Success, $"Roll {result.Roll} should succeed for combo continue", ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
                else
                {
                    failCount++;
                    TestBase.AssertTrue(!result.Success, $"Roll {result.Roll} should fail for combo continue", ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }

            TestBase.AssertTrue(successCount > 0, "Should have some successful combo continues", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(failCount > 0, "Should have some failed combo continues", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDiceRollRanges()
        {
            Console.WriteLine("Testing Dice Roll Ranges...");

            // Test with bonus
            var resultWithBonus = Dice.RollComboAction(5);
            TestBase.AssertTrue(resultWithBonus.Roll >= 6, $"Roll with +5 bonus should be at least 6, got: {resultWithBonus.Roll}", ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test edge cases
            var resultMin = Dice.RollComboAction(-10); // Can go below 1, but that's handled
            TestBase.AssertTrue(resultMin.Roll >= 1, $"Roll should never be below 1, got: {resultMin.Roll}", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

