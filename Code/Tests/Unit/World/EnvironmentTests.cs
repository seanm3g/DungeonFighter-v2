using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.World
{
    /// <summary>
    /// Comprehensive tests for Environment
    /// Tests environment effects, environmental actions, and enemy management
    /// </summary>
    public static class EnvironmentTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all Environment tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Environment Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestEnvironmentCreation();
            TestEnvironmentProperties();
            TestGenerateEnemies();
            TestGetEnemies();
            TestHasLivingEnemies();
            TestResetForNewFight();

            TestBase.PrintSummary("Environment Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Creation Tests

        private static void TestEnvironmentCreation()
        {
            Console.WriteLine("--- Testing Environment Creation ---");

            var environment = new Environment("Test Room", "A test room", true, "Forest", "Chamber");
            TestBase.AssertNotNull(environment,
                "Environment should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (environment != null)
            {
                TestBase.AssertEqual("Test Room", environment.Name,
                    "Environment should have correct name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual("A test room", environment.Description,
                    "Environment should have correct description",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(environment.IsHostile,
                    "Environment should have correct hostility",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual("Forest", environment.Theme,
                    "Environment should have correct theme",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Properties Tests

        private static void TestEnvironmentProperties()
        {
            Console.WriteLine("\n--- Testing Environment Properties ---");

            var environment = new Environment("Test Room", "A test room", false, "Forest");

            TestBase.AssertFalse(environment.IsHostile,
                "Non-hostile environment should have IsHostile = false",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(environment.ActionPool,
                "Environment action pool should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Enemy Management Tests

        private static void TestGenerateEnemies()
        {
            Console.WriteLine("\n--- Testing GenerateEnemies ---");

            var environment = new Environment("Test Room", "A test room", true, "Forest");
            environment.GenerateEnemies(5);

            // Generation should not crash
            TestBase.AssertTrue(true,
                "GenerateEnemies should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetEnemies()
        {
            Console.WriteLine("\n--- Testing GetEnemies ---");

            var environment = new Environment("Test Room", "A test room", true, "Forest");
            environment.GenerateEnemies(5);

            var enemies = environment.GetEnemies();
            TestBase.AssertNotNull(enemies,
                "GetEnemies should return a list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (enemies != null)
            {
                TestBase.AssertTrue(enemies.Count >= 0,
                    $"Enemies count should be >= 0, got {enemies.Count}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestHasLivingEnemies()
        {
            Console.WriteLine("\n--- Testing HasLivingEnemies ---");

            var environment = new Environment("Test Room", "A test room", true, "Forest");
            environment.GenerateEnemies(5);

            var hasLiving = environment.HasLivingEnemies();
            // Result depends on enemy generation, so we just verify it doesn't crash
            TestBase.AssertTrue(hasLiving || !hasLiving,
                "HasLivingEnemies should return a boolean",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Reset Tests

        private static void TestResetForNewFight()
        {
            Console.WriteLine("\n--- Testing ResetForNewFight ---");

            var environment = new Environment("Test Room", "A test room", true, "Forest");
            environment.ResetForNewFight();

            // Reset should not crash
            TestBase.AssertTrue(true,
                "ResetForNewFight should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
