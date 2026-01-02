using System;
using System.Collections.Generic;
using RPGGame.Tests;
using RPGGame.Simulation;

namespace RPGGame.Tests.Unit.Simulation
{
    /// <summary>
    /// Comprehensive tests for ScenarioConfiguration
    /// Tests scenario configuration creation and serialization
    /// </summary>
    public static class ScenarioConfigurationTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ScenarioConfiguration tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ScenarioConfiguration Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestScenarioConfigurationCreation();
            TestJsonSerialization();
            TestPlayerConfig();
            TestEnemyConfig();

            TestBase.PrintSummary("ScenarioConfiguration Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Configuration Tests

        private static void TestScenarioConfigurationCreation()
        {
            Console.WriteLine("--- Testing ScenarioConfiguration Creation ---");

            var config = new ScenarioConfiguration
            {
                Name = "Test Scenario",
                Description = "Test description",
                Version = "1.0"
            };

            TestBase.AssertEqual("Test Scenario", config.Name,
                "Scenario name should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertNotNull(config.PlayerConfig,
                "Player config should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertNotNull(config.EnemyConfigs,
                "Enemy configs should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestJsonSerialization()
        {
            Console.WriteLine("\n--- Testing JSON Serialization ---");

            var config = new ScenarioConfiguration
            {
                Name = "Test",
                Version = "1.0"
            };

            string json = ScenarioConfiguration.ToJson(config);
            TestBase.AssertTrue(!string.IsNullOrEmpty(json),
                "JSON serialization should produce output",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var deserialized = ScenarioConfiguration.FromJson(json);
            TestBase.AssertNotNull(deserialized,
                "JSON deserialization should work",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (deserialized != null)
            {
                TestBase.AssertEqual("Test", deserialized.Name,
                    "Deserialized name should match",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestPlayerConfig()
        {
            Console.WriteLine("\n--- Testing PlayerConfig ---");

            var playerConfig = new PlayerConfig
            {
                Name = "TestHero",
                Level = 5,
                ClassType = "Warrior"
            };

            TestBase.AssertEqual("TestHero", playerConfig.Name,
                "Player name should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(5, playerConfig.Level,
                "Player level should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertNotNull(playerConfig.BaseStats,
                "Base stats should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEnemyConfig()
        {
            Console.WriteLine("\n--- Testing EnemyConfig ---");

            var enemyConfig = new EnemyConfig
            {
                Name = "TestEnemy",
                Level = 3,
                EnemyType = "Goblin"
            };

            TestBase.AssertEqual("TestEnemy", enemyConfig.Name,
                "Enemy name should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(3, enemyConfig.Level,
                "Enemy level should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
