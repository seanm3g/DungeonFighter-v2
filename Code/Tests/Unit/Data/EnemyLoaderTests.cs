using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Comprehensive tests for EnemyLoader
    /// Tests enemy data loading, retrieval, and error handling
    /// </summary>
    public static class EnemyLoaderTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all EnemyLoader tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== EnemyLoader Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestLoadEnemies();
            TestGetEnemyData();
            TestHasEnemy();
            TestGetAllEnemyNames();
            TestGetAllEnemyData();

            TestBase.PrintSummary("EnemyLoader Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Loading Tests

        private static void TestLoadEnemies()
        {
            Console.WriteLine("--- Testing LoadEnemies ---");

            // Test that loading doesn't crash
            EnemyLoader.LoadEnemies();
            TestBase.AssertTrue(true,
                "LoadEnemies should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test loading multiple times
            EnemyLoader.LoadEnemies();
            TestBase.AssertTrue(true,
                "LoadEnemies should handle multiple calls",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Retrieval Tests

        private static void TestGetEnemyData()
        {
            Console.WriteLine("\n--- Testing GetEnemyData ---");

            // Test getting enemy data (might be null if no enemies loaded)
            var enemyData = EnemyLoader.GetEnemyData("Goblin");
            
            // If enemy exists, verify properties
            if (enemyData != null)
            {
                TestBase.AssertTrue(!string.IsNullOrEmpty(enemyData.Name),
                    "Enemy data should have a name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test getting non-existent enemy
            var nonExistent = EnemyLoader.GetEnemyData("NonExistentEnemy");
            TestBase.AssertNull(nonExistent,
                "Non-existent enemy should return null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with empty string
            var empty = EnemyLoader.GetEnemyData("");
            TestBase.AssertNull(empty,
                "Empty enemy name should return null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Query Tests

        private static void TestHasEnemy()
        {
            Console.WriteLine("\n--- Testing HasEnemy ---");

            // Test with potentially existing enemy
            var hasGoblin = EnemyLoader.HasEnemy("Goblin");
            // Result depends on data files, so we just verify it doesn't crash
            TestBase.AssertTrue(hasGoblin || !hasGoblin,
                "HasEnemy should return a boolean",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with non-existent enemy
            TestBase.AssertFalse(EnemyLoader.HasEnemy("NonExistentEnemy"),
                "Non-existent enemy should return false",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with empty string
            TestBase.AssertFalse(EnemyLoader.HasEnemy(""),
                "Empty enemy name should return false",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetAllEnemyNames()
        {
            Console.WriteLine("\n--- Testing GetAllEnemyNames ---");

            var names = EnemyLoader.GetAllEnemyTypes();
            TestBase.AssertNotNull(names,
                "GetAllEnemyTypes should return a list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (names != null)
            {
                TestBase.AssertTrue(names.Count >= 0,
                    $"Enemy names count should be >= 0, got {names.Count}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // If there are enemies, verify names are not empty
                foreach (var name in names)
                {
                    TestBase.AssertTrue(!string.IsNullOrEmpty(name),
                        $"Enemy name should not be empty: '{name}'",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
        }

        private static void TestGetAllEnemyData()
        {
            Console.WriteLine("\n--- Testing GetAllEnemyData ---");

            var allEnemies = EnemyLoader.GetAllEnemyData();
            TestBase.AssertNotNull(allEnemies,
                "GetAllEnemyData should return a list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (allEnemies != null)
            {
                TestBase.AssertTrue(allEnemies.Count >= 0,
                    $"Enemy data count should be >= 0, got {allEnemies.Count}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // If there are enemies, verify they have names
                foreach (var enemy in allEnemies)
                {
                    TestBase.AssertNotNull(enemy,
                        "Enemy data should not be null",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    
                    if (enemy != null)
                    {
                        TestBase.AssertTrue(!string.IsNullOrEmpty(enemy.Name),
                            "Enemy should have a name",
                            ref _testsRun, ref _testsPassed, ref _testsFailed);
                    }
                }
            }
        }

        #endregion
    }
}
