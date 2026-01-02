using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Actions
{
    /// <summary>
    /// Comprehensive tests for Action class
    /// Tests action creation, properties, and cooldown management
    /// </summary>
    public static class ActionTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all Action tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Action Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestActionCreation();
            TestActionProperties();
            TestCooldownManagement();
            TestStatusEffects();
            TestComboProperties();

            TestBase.PrintSummary("Action Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Creation Tests

        private static void TestActionCreation()
        {
            Console.WriteLine("--- Testing Action Creation ---");

            var action = new Action
            {
                Name = "TestAction",
                Type = ActionType.Attack,
                Target = TargetType.SingleTarget,
                Description = "Test description"
            };

            TestBase.AssertEqual("TestAction", action.Name,
                "Action name should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(ActionType.Attack, action.Type,
                "Action type should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(TargetType.SingleTarget, action.Target,
                "Target type should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Property Tests

        private static void TestActionProperties()
        {
            Console.WriteLine("\n--- Testing Action Properties ---");

            var action = new Action
            {
                DamageMultiplier = 2.5,
                Length = 1.5,
                Cooldown = 3
            };

            TestBase.AssertEqual(2.5, action.DamageMultiplier,
                "Damage multiplier should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.5, action.Length,
                "Length should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(3, action.Cooldown,
                "Cooldown should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCooldownManagement()
        {
            Console.WriteLine("\n--- Testing Cooldown Management ---");

            var action = new Action { Cooldown = 5 };

            // Test setting cooldown
            action.CurrentCooldown = 3;
            TestBase.AssertEqual(3, action.CurrentCooldown,
                "Current cooldown should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test negative cooldown clamping
            action.CurrentCooldown = -5;
            TestBase.AssertEqual(0, action.CurrentCooldown,
                "Negative cooldown should be clamped to 0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStatusEffects()
        {
            Console.WriteLine("\n--- Testing Status Effects ---");

            var action = new Action
            {
                CausesBleed = true,
                CausesWeaken = true,
                CausesSlow = false,
                CausesPoison = true,
                CausesStun = true
            };

            TestBase.AssertTrue(action.CausesBleed,
                "Bleed status should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(action.CausesWeaken,
                "Weaken status should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(action.CausesSlow,
                "Slow status should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestComboProperties()
        {
            Console.WriteLine("\n--- Testing Combo Properties ---");

            var action = new Action
            {
                IsComboAction = true,
                ComboOrder = 2,
                ComboBonusAmount = 5,
                ComboBonusDuration = 3
            };

            TestBase.AssertTrue(action.IsComboAction,
                "Is combo action should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, action.ComboOrder,
                "Combo order should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(5, action.ComboBonusAmount,
                "Combo bonus amount should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
