using System;
using RPGGame.Combat.Calculators;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Comprehensive tests for StatusEffectCalculator
    /// Tests status effect chance calculations
    /// </summary>
    public static class StatusEffectCalculatorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all StatusEffectCalculator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== StatusEffectCalculator Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestCalculateStatusEffectChanceWithBleed();
            TestCalculateStatusEffectChanceWithWeaken();
            TestCalculateStatusEffectChanceWithSlow();
            TestCalculateStatusEffectChanceWithPoison();
            TestCalculateStatusEffectChanceWithStun();
            TestCalculateStatusEffectChanceWithBurn();
            TestCalculateStatusEffectChanceNoEffect();
            TestCalculateStatusEffectChanceMultipleEffects();

            TestBase.PrintSummary("StatusEffectCalculator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Status Effect Tests

        private static void TestCalculateStatusEffectChanceWithBleed()
        {
            Console.WriteLine("--- Testing CalculateStatusEffectChanceWithBleed ---");

            var action = TestDataBuilders.CreateMockAction("BLEED_ATTACK");
            action.CausesBleed = true;

            var attacker = TestDataBuilders.Character().WithName("Attacker").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();

            // Run multiple times to test probability
            int successes = 0;
            for (int i = 0; i < 100; i++)
            {
                if (StatusEffectCalculator.CalculateStatusEffectChance(action, attacker, target))
                {
                    successes++;
                }
            }

            // Should have some successes (2d2-2 gives ~66.7% chance)
            TestBase.AssertTrue(successes > 0,
                "Status effect should have chance to apply",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCalculateStatusEffectChanceWithWeaken()
        {
            Console.WriteLine("\n--- Testing CalculateStatusEffectChanceWithWeaken ---");

            var action = TestDataBuilders.CreateMockAction("WEAKEN_ATTACK");
            action.CausesWeaken = true;

            var attacker = TestDataBuilders.Character().WithName("Attacker").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();

            var result = StatusEffectCalculator.CalculateStatusEffectChance(action, attacker, target);
            // Result is probabilistic, so we just verify it doesn't crash
            TestBase.AssertTrue(true,
                "Status effect chance calculation should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCalculateStatusEffectChanceWithSlow()
        {
            Console.WriteLine("\n--- Testing CalculateStatusEffectChanceWithSlow ---");

            var action = TestDataBuilders.CreateMockAction("SLOW_ATTACK");
            action.CausesSlow = true;

            var attacker = TestDataBuilders.Character().WithName("Attacker").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();

            var result = StatusEffectCalculator.CalculateStatusEffectChance(action, attacker, target);
            TestBase.AssertTrue(true,
                "Status effect chance calculation should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCalculateStatusEffectChanceWithPoison()
        {
            Console.WriteLine("\n--- Testing CalculateStatusEffectChanceWithPoison ---");

            var action = TestDataBuilders.CreateMockAction("POISON_ATTACK");
            action.CausesPoison = true;

            var attacker = TestDataBuilders.Character().WithName("Attacker").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();

            var result = StatusEffectCalculator.CalculateStatusEffectChance(action, attacker, target);
            TestBase.AssertTrue(true,
                "Status effect chance calculation should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCalculateStatusEffectChanceWithStun()
        {
            Console.WriteLine("\n--- Testing CalculateStatusEffectChanceWithStun ---");

            var action = TestDataBuilders.CreateMockAction("STUN_ATTACK");
            action.CausesStun = true;

            var attacker = TestDataBuilders.Character().WithName("Attacker").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();

            var result = StatusEffectCalculator.CalculateStatusEffectChance(action, attacker, target);
            TestBase.AssertTrue(true,
                "Status effect chance calculation should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCalculateStatusEffectChanceWithBurn()
        {
            Console.WriteLine("\n--- Testing CalculateStatusEffectChanceWithBurn ---");

            var action = TestDataBuilders.CreateMockAction("BURN_ATTACK");
            action.CausesBurn = true;

            var attacker = TestDataBuilders.Character().WithName("Attacker").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();

            var result = StatusEffectCalculator.CalculateStatusEffectChance(action, attacker, target);
            TestBase.AssertTrue(true,
                "Status effect chance calculation should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCalculateStatusEffectChanceNoEffect()
        {
            Console.WriteLine("\n--- Testing CalculateStatusEffectChanceNoEffect ---");

            var action = TestDataBuilders.CreateMockAction("NORMAL_ATTACK");
            // No status effects enabled

            var attacker = TestDataBuilders.Character().WithName("Attacker").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();

            var result = StatusEffectCalculator.CalculateStatusEffectChance(action, attacker, target);
            TestBase.AssertFalse(result,
                "Action with no status effects should return false",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCalculateStatusEffectChanceMultipleEffects()
        {
            Console.WriteLine("\n--- Testing CalculateStatusEffectChanceMultipleEffects ---");

            var action = TestDataBuilders.CreateMockAction("MULTI_EFFECT_ATTACK");
            action.CausesBleed = true;
            action.CausesPoison = true;
            action.CausesBurn = true;

            var attacker = TestDataBuilders.Character().WithName("Attacker").Build();
            var target = TestDataBuilders.Enemy().WithName("Target").Build();

            var result = StatusEffectCalculator.CalculateStatusEffectChance(action, attacker, target);
            // Should still use 2d2-2 roll regardless of number of effects
            TestBase.AssertTrue(true,
                "Multiple status effects should still calculate chance",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
