using System;
using RPGGame.Combat.Calculators;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Comprehensive tests for HitCalculator
    /// Tests hit/miss determination with various roll values and thresholds
    /// </summary>
    public static class HitCalculatorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all HitCalculator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== HitCalculator Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestCalculateHit();
            TestHitThresholds();
            TestRollBonuses();
            TestEdgeCases();
            TestCriticalHits();

            TestBase.PrintSummary("HitCalculator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Basic Hit Tests

        private static void TestCalculateHit()
        {
            Console.WriteLine("--- Testing CalculateHit ---");

            var attacker = TestDataBuilders.Character()
                .WithName("Attacker")
                .WithStats(10, 10, 10, 10)
                .Build();

            var target = TestDataBuilders.Enemy()
                .WithName("Target")
                .WithHealth(100)
                .Build();

            // Test with roll that should hit (default threshold is 5, so 6+ hits)
            var hitResult = HitCalculator.CalculateHit(attacker, target, 0, 10);
            TestBase.AssertTrue(hitResult,
                "Roll of 10 should hit (threshold is 5)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with roll that should miss
            var missResult = HitCalculator.CalculateHit(attacker, target, 0, 3);
            TestBase.AssertFalse(missResult,
                "Roll of 3 should miss (threshold is 5)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with roll exactly at threshold (should miss, need threshold + 1)
            var thresholdResult = HitCalculator.CalculateHit(attacker, target, 0, 5);
            TestBase.AssertFalse(thresholdResult,
                "Roll of 5 should miss (need 6+ to hit)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Threshold Tests

        private static void TestHitThresholds()
        {
            Console.WriteLine("\n--- Testing HitThresholds ---");

            var attacker = TestDataBuilders.Character()
                .WithName("Attacker")
                .WithStats(10, 10, 10, 10)
                .Build();

            var target = TestDataBuilders.Enemy()
                .WithName("Target")
                .WithHealth(100)
                .Build();

            // Test various roll values
            for (int roll = 1; roll <= 20; roll++)
            {
                var result = HitCalculator.CalculateHit(attacker, target, 0, roll);
                
                // Default threshold is 5, so 6+ should hit
                if (roll > 5)
                {
                    TestBase.AssertTrue(result,
                        $"Roll of {roll} should hit",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
                else
                {
                    TestBase.AssertFalse(result,
                        $"Roll of {roll} should miss",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
        }

        #endregion

        #region Roll Bonus Tests

        private static void TestRollBonuses()
        {
            Console.WriteLine("\n--- Testing RollBonuses ---");

            var attacker = TestDataBuilders.Character()
                .WithName("Attacker")
                .WithStats(10, 10, 10, 10)
                .Build();

            var target = TestDataBuilders.Enemy()
                .WithName("Target")
                .WithHealth(100)
                .Build();

            // Note: HitCalculator uses roll value only, not rollBonus
            // Roll bonus is applied before calling CalculateHit
            // So we test with adjusted roll values

            // Test with base roll that would miss
            var baseRoll = 4; // Would miss without bonus
            var missResult = HitCalculator.CalculateHit(attacker, target, 0, baseRoll);
            TestBase.AssertFalse(missResult,
                "Roll of 4 should miss",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with effective roll that would hit (simulating bonus applied)
            var effectiveRoll = baseRoll + 3; // 7, should hit
            var hitResult = HitCalculator.CalculateHit(attacker, target, 0, effectiveRoll);
            TestBase.AssertTrue(hitResult,
                "Effective roll of 7 should hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Edge Cases

        private static void TestEdgeCases()
        {
            Console.WriteLine("\n--- Testing EdgeCases ---");

            var attacker = TestDataBuilders.Character()
                .WithName("Attacker")
                .WithStats(10, 10, 10, 10)
                .Build();

            var target = TestDataBuilders.Enemy()
                .WithName("Target")
                .WithHealth(100)
                .Build();

            // Test with minimum roll
            var minRoll = HitCalculator.CalculateHit(attacker, target, 0, 1);
            TestBase.AssertFalse(minRoll,
                "Roll of 1 should miss",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with maximum roll
            var maxRoll = HitCalculator.CalculateHit(attacker, target, 0, 20);
            TestBase.AssertTrue(maxRoll,
                "Roll of 20 should hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with roll exactly at threshold boundary
            var boundaryRoll = HitCalculator.CalculateHit(attacker, target, 0, 6);
            TestBase.AssertTrue(boundaryRoll,
                "Roll of 6 should hit (threshold + 1)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Critical Hit Tests

        private static void TestCriticalHits()
        {
            Console.WriteLine("\n--- Testing CriticalHits ---");

            var attacker = TestDataBuilders.Character()
                .WithName("Attacker")
                .WithStats(10, 10, 10, 10)
                .Build();

            var target = TestDataBuilders.Enemy()
                .WithName("Target")
                .WithHealth(100)
                .Build();

            // Roll of 20 should hit (and typically be critical)
            var criticalRoll = HitCalculator.CalculateHit(attacker, target, 0, 20);
            TestBase.AssertTrue(criticalRoll,
                "Roll of 20 should hit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // High rolls should hit
            for (int roll = 15; roll <= 20; roll++)
            {
                var result = HitCalculator.CalculateHit(attacker, target, 0, roll);
                TestBase.AssertTrue(result,
                    $"High roll of {roll} should hit",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
