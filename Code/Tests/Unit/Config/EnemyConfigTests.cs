using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Config
{
    /// <summary>
    /// Comprehensive tests for EnemyConfig
    /// Tests enemy scaling, archetype configs, and balance settings
    /// </summary>
    public static class EnemyConfigTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all EnemyConfig tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== EnemyConfig Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestPoolConfig();
            TestArchetypeConfig();
            TestArchetypeBonuses();
            TestBaseEnemyConfig();
            TestBaseEnemyStats();
            TestLevel1Modifiers();

            TestBase.PrintSummary("EnemyConfig Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region PoolConfig Tests

        private static void TestPoolConfig()
        {
            Console.WriteLine("--- Testing PoolConfig ---");

            var config = new PoolConfig
            {
                BasePointsAtLevel1 = 20,
                PointsPerLevel = 2
            };

            TestBase.AssertEqual(20, config.BasePointsAtLevel1,
                "BasePointsAtLevel1 should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(2, config.PointsPerLevel,
                "PointsPerLevel should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region ArchetypeConfig Tests

        private static void TestArchetypeConfig()
        {
            Console.WriteLine("\n--- Testing ArchetypeConfig ---");

            var config = new ArchetypeConfig
            {
                AttributePoolRatio = 0.7,
                SUSTAINPoolRatio = 0.3,
                StrengthRatio = 0.4,
                AgilityRatio = 0.3
            };

            TestBase.AssertEqual(0.7, config.AttributePoolRatio,
                "AttributePoolRatio should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(0.3, config.SUSTAINPoolRatio,
                "SUSTAINPoolRatio should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region ArchetypeBonuses Tests

        private static void TestArchetypeBonuses()
        {
            Console.WriteLine("\n--- Testing ArchetypeBonuses ---");

            var bonuses = new ArchetypeBonuses
            {
                StrengthMultiplier = 1.2,
                AgilityMultiplier = 1.1,
                HealthMultiplier = 1.3
            };

            TestBase.AssertEqual(1.2, bonuses.StrengthMultiplier,
                "StrengthMultiplier should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(1.1, bonuses.AgilityMultiplier,
                "AgilityMultiplier should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region BaseEnemyConfig Tests

        private static void TestBaseEnemyConfig()
        {
            Console.WriteLine("\n--- Testing BaseEnemyConfig ---");

            var config = new BaseEnemyConfig
            {
                BaseLevel = 1,
                HealthRatio = 1.0,
                BaseArmor = 5,
                PrimaryAttribute = "Strength",
                IsLiving = true
            };

            TestBase.AssertNotNull(config.BaseStats,
                "BaseStats should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(config.Actions,
                "Actions should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual("Strength", config.PrimaryAttribute,
                "PrimaryAttribute should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region BaseEnemyStats Tests

        private static void TestBaseEnemyStats()
        {
            Console.WriteLine("\n--- Testing BaseEnemyStats ---");

            var stats = new BaseEnemyStats
            {
                Strength = 10,
                Agility = 8,
                Technique = 6,
                Intelligence = 4
            };

            TestBase.AssertEqual(10, stats.Strength,
                "Strength should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(8, stats.Agility,
                "Agility should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Level1Modifiers Tests

        private static void TestLevel1Modifiers()
        {
            Console.WriteLine("\n--- Testing Level1Modifiers ---");

            var modifiers = new Level1Modifiers
            {
                HealthBonus = 10,
                StrengthBonus = 2,
                AgilityBonus = 1
            };

            TestBase.AssertEqual(10, modifiers.HealthBonus,
                "HealthBonus should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(2, modifiers.StrengthBonus,
                "StrengthBonus should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
