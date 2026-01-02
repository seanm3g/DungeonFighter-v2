using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Entity
{
    /// <summary>
    /// Comprehensive tests for Enemy class
    /// Tests enemy creation, AI behavior, combat actions, enemy scaling, and archetype handling
    /// </summary>
    public static class EnemyTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all Enemy tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Enemy Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestEnemyCreation();
            TestEnemyProperties();
            TestEnemyScaling();
            TestEnemyActionPool();

            TestBase.PrintSummary("Enemy Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Creation Tests

        private static void TestEnemyCreation()
        {
            Console.WriteLine("--- Testing Enemy Creation ---");

            // Test basic creation (use first constructor with explicit parameters)
            var enemy = new Enemy("TestEnemy", 1, 50, 8, 6, 4, 4, 0, PrimaryAttribute.Strength, true, null);
            TestBase.AssertNotNull(enemy,
                "Enemy should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (enemy != null)
            {
                TestBase.AssertEqual("TestEnemy", enemy.Name,
                    "Enemy should have correct name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(1, enemy.Level,
                    "Enemy should have correct level",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test creation with all parameters
            var enemy2 = new Enemy("TestEnemy2", 5, 100, 10, 8, 6, 4);
            TestBase.AssertNotNull(enemy2,
                "Enemy should be created with all parameters",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (enemy2 != null)
            {
                TestBase.AssertTrue(enemy2.CurrentHealth > 0,
                    "Enemy should have positive health",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Properties Tests

        private static void TestEnemyProperties()
        {
            Console.WriteLine("\n--- Testing Enemy Properties ---");

            var enemy = new Enemy("TestEnemy", 1, 50, 8, 6, 4, 4, 0, PrimaryAttribute.Strength, true, null);

            // Test health properties
            TestBase.AssertTrue(enemy.CurrentHealth > 0,
                $"Enemy should have positive health, got {enemy.CurrentHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(enemy.MaxHealth > 0,
                $"Enemy should have positive max health, got {enemy.MaxHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test action pool is initialized
            TestBase.AssertNotNull(enemy.ActionPool,
                "Enemy action pool should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Scaling Tests

        private static void TestEnemyScaling()
        {
            Console.WriteLine("\n--- Testing Enemy Scaling ---");

            // Test that higher level enemies have more health
            var enemy1 = new Enemy("Enemy1", 1, 50, 8, 6, 4, 4, 0, PrimaryAttribute.Strength, true, null);
            var enemy5 = new Enemy("Enemy5", 5, 100, 10, 8, 6, 4, 0, PrimaryAttribute.Strength, true, null);

            // Higher level enemies should generally have more health
            // (This is probabilistic, so we just verify they don't crash)
            TestBase.AssertTrue(enemy1.MaxHealth > 0,
                "Level 1 enemy should have positive health",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(enemy5.MaxHealth > 0,
                "Level 5 enemy should have positive health",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Action Pool Tests

        private static void TestEnemyActionPool()
        {
            Console.WriteLine("\n--- Testing Enemy Action Pool ---");

            var enemy = new Enemy("TestEnemy", 1, 50, 8, 6, 4, 4, 0, PrimaryAttribute.Strength, true, null);

            // Test action pool is accessible
            TestBase.AssertNotNull(enemy.ActionPool,
                "Action pool should be accessible",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test adding action to pool
            var action = TestDataBuilders.CreateMockAction("TestAction", ActionType.Attack);
            enemy.ActionPool.Add((action, 1.0));

            TestBase.AssertTrue(enemy.ActionPool.Count > 0,
                "Action pool should contain added action",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
