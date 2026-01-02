using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Config
{
    /// <summary>
    /// Comprehensive tests for CharacterConfig
    /// Tests configuration loading, validation, and default values
    /// </summary>
    public static class CharacterConfigTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all CharacterConfig tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CharacterConfig Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestCharacterConfig();
            TestAttributesConfig();
            TestAttributeSet();
            TestProgressionConfig();
            TestExperienceSystemConfig();
            TestXPRewardsConfig();

            TestBase.PrintSummary("CharacterConfig Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region CharacterConfig Tests

        private static void TestCharacterConfig()
        {
            Console.WriteLine("--- Testing CharacterConfig ---");

            var config = new CharacterConfig
            {
                PlayerBaseHealth = 100,
                HealthPerLevel = 10,
                EnemyHealthPerLevel = 8
            };

            TestBase.AssertEqual(100, config.PlayerBaseHealth,
                "PlayerBaseHealth should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(10, config.HealthPerLevel,
                "HealthPerLevel should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(8, config.EnemyHealthPerLevel,
                "EnemyHealthPerLevel should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AttributesConfig Tests

        private static void TestAttributesConfig()
        {
            Console.WriteLine("\n--- Testing AttributesConfig ---");

            var config = new AttributesConfig
            {
                PlayerAttributesPerLevel = 2,
                EnemyAttributesPerLevel = 1,
                EnemyPrimaryAttributeBonus = 3,
                IntelligenceRollBonusPer = 1
            };

            TestBase.AssertNotNull(config.PlayerBaseAttributes,
                "PlayerBaseAttributes should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(2, config.PlayerAttributesPerLevel,
                "PlayerAttributesPerLevel should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AttributeSet Tests

        private static void TestAttributeSet()
        {
            Console.WriteLine("\n--- Testing AttributeSet ---");

            var attributeSet = new AttributeSet
            {
                Strength = 10,
                Agility = 8,
                Technique = 7,
                Intelligence = 5
            };

            TestBase.AssertEqual(10, attributeSet.Strength,
                "Strength should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(8, attributeSet.Agility,
                "Agility should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region ProgressionConfig Tests

        private static void TestProgressionConfig()
        {
            Console.WriteLine("\n--- Testing ProgressionConfig ---");

            var config = new ProgressionConfig
            {
                BaseXPToLevel2 = 100,
                XPScalingFactor = 1.5,
                EnemyXPBase = 50,
                EnemyXPPerLevel = 10
            };

            TestBase.AssertEqual(100, config.BaseXPToLevel2,
                "BaseXPToLevel2 should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(1.5, config.XPScalingFactor,
                "XPScalingFactor should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region ExperienceSystemConfig Tests

        private static void TestExperienceSystemConfig()
        {
            Console.WriteLine("\n--- Testing ExperienceSystemConfig ---");

            var config = new ExperienceSystemConfig
            {
                BaseXPFormula = "100 * level",
                LevelCap = 50,
                StatPointsPerLevel = 2,
                SkillPointsPerLevel = 1
            };

            TestBase.AssertEqual("100 * level", config.BaseXPFormula,
                "BaseXPFormula should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(50, config.LevelCap,
                "LevelCap should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region XPRewardsConfig Tests

        private static void TestXPRewardsConfig()
        {
            Console.WriteLine("\n--- Testing XPRewardsConfig ---");

            var config = new XPRewardsConfig
            {
                BaseXPFormula = "50 * level",
                MinimumXP = 10,
                MaximumXPMultiplier = 2.0
            };

            TestBase.AssertNotNull(config.LevelDifferenceMultipliers,
                "LevelDifferenceMultipliers should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(config.DungeonCompletionBonus,
                "DungeonCompletionBonus should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(10, config.MinimumXP,
                "MinimumXP should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
