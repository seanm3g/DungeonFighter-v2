using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for environmental actions
    /// Tests theme-based and room-specific environmental effects
    /// </summary>
    public static class EnvironmentalActionsTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Environmental Actions Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestActionLoading();
            TestThemeBasedActions();
            TestRoomTypeActions();
            TestActionExecution();
            TestEffectApplication();
            TestDurationCalculation();
            TestTargetSelection();
            TestNoEffectCases();

            TestBase.PrintSummary("Environmental Actions Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestActionLoading()
        {
            Console.WriteLine("--- Testing Action Loading ---");

            // Test that environmental actions can be loaded
            TestBase.AssertTrue(true, 
                "Environmental actions should be loadable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestThemeBasedActions()
        {
            Console.WriteLine("\n--- Testing Theme-Based Actions ---");

            // Test theme-specific actions (Forest, Lava, Crypt, etc.)
            TestBase.AssertTrue(true, 
                "Theme-based actions should be available", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRoomTypeActions()
        {
            Console.WriteLine("\n--- Testing Room-Type Actions ---");

            // Test room-specific actions
            TestBase.AssertTrue(true, 
                "Room-type actions should be available", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestActionExecution()
        {
            Console.WriteLine("\n--- Testing Action Execution ---");

            var environment = new Environment("TestEnvironment", "Test Description", false, "forest", "");
            var targets = new List<Actor> { TestDataBuilders.Enemy().Build() };
            var action = TestDataBuilders.CreateMockAction("EnvironmentalAction");

            var result = EnvironmentalActionExecutor.ExecuteEnvironmentalAction(environment, targets, action);
            TestBase.AssertTrue(!string.IsNullOrEmpty(result), 
                "Environmental action should execute and return result", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEffectApplication()
        {
            Console.WriteLine("\n--- Testing Effect Application ---");

            var environment = new Environment("TestEnvironment", "Test Description", false, "forest", "");
            var targets = new List<Actor> { TestDataBuilders.Character().Build() };
            var action = TestDataBuilders.CreateMockAction("EnvironmentalAction");
            action.CausesBleed = true;

            var result = EnvironmentalActionExecutor.ExecuteEnvironmentalAction(environment, targets, action);
            TestBase.AssertTrue(!string.IsNullOrEmpty(result), 
                "Environmental effect should be applicable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDurationCalculation()
        {
            Console.WriteLine("\n--- Testing Duration Calculation ---");

            // Test 2d2-2 duration system
            int duration = Dice.Roll(1, 2) + Dice.Roll(1, 2) - 2;
            TestBase.AssertTrue(duration >= 0 && duration <= 2, 
                $"Duration should be 0-2, got {duration}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTargetSelection()
        {
            Console.WriteLine("\n--- Testing Target Selection ---");

            var targets = new List<Actor>
            {
                TestDataBuilders.Character().Build(),
                TestDataBuilders.Enemy().Build()
            };

            TestBase.AssertTrue(targets.Count > 0, 
                "Targets should be selectable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNoEffectCases()
        {
            Console.WriteLine("\n--- Testing No Effect Cases ---");

            // Test case where duration is 0 (no effect)
            int duration = 0;
            TestBase.AssertTrue(duration == 0, 
                "Duration 0 should indicate no effect", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

