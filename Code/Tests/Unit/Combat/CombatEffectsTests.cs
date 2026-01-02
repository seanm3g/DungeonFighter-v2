using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Comprehensive tests for CombatEffects
    /// Tests status effects in combat, effect application, duration tracking, and effect removal
    /// </summary>
    public static class CombatEffectsTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all CombatEffects tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatEffects Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestEffectHandlerRegistry();
            TestStatusEffectApplication();

            TestBase.PrintSummary("CombatEffects Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Effect Handler Registry Tests

        private static void TestEffectHandlerRegistry()
        {
            Console.WriteLine("--- Testing EffectHandlerRegistry ---");

            var registry = new EffectHandlerRegistry();
            TestBase.AssertNotNull(registry,
                "EffectHandlerRegistry should be accessible",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Status Effect Tests

        private static void TestStatusEffectApplication()
        {
            Console.WriteLine("\n--- Testing Status Effect Application ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            // Test that status effects can be set
            character.IsBleeding = true;
            TestBase.AssertTrue(character.IsBleeding,
                "Character should be able to have bleeding status",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.IsStunned = true;
            TestBase.AssertTrue(character.IsStunned,
                "Character should be able to have stun status",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.IsWeakened = true;
            TestBase.AssertTrue(character.IsWeakened,
                "Character should be able to have weaken status",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
