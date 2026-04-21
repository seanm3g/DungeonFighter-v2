using System;
using RPGGame;
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
            TestEnsureValidPlayerHealthDefaults();
            TestEnsureValidPlayerBaseStatDefaults();
            TestEnsureValidIntelligenceRollBonusDefaults();
            TestAttributeSet();
            TestProgressionConfig();
            TestEnsureValidEnemyXpAndGoldDefaults();
            TestClassBalanceSanitizer();
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

        private static void TestEnsureValidPlayerHealthDefaults()
        {
            Console.WriteLine("\n--- Testing EnsureValidPlayerHealthDefaults ---");

            var zeroed = new CharacterConfig { PlayerBaseHealth = 0, HealthPerLevel = 0 };
            zeroed.EnsureValidPlayerHealthDefaults();
            TestBase.AssertEqual(60, zeroed.PlayerBaseHealth,
                "Non-positive PlayerBaseHealth should default to 60",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(3, zeroed.HealthPerLevel,
                "Non-positive HealthPerLevel should default to 3",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var custom = new CharacterConfig { PlayerBaseHealth = 55, HealthPerLevel = 4 };
            custom.EnsureValidPlayerHealthDefaults();
            TestBase.AssertEqual(55, custom.PlayerBaseHealth,
                "Positive PlayerBaseHealth should be preserved",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(4, custom.HealthPerLevel,
                "Positive HealthPerLevel should be preserved",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEnsureValidPlayerBaseStatDefaults()
        {
            Console.WriteLine("\n--- Testing EnsureValidPlayerBaseStatDefaults ---");

            var zeroed = new AttributesConfig
            {
                PlayerBaseAttributes = new AttributeSet(),
                PlayerAttributesPerLevel = 0
            };
            zeroed.EnsureValidPlayerBaseStatDefaults();
            TestBase.AssertEqual(3, zeroed.PlayerBaseAttributes.Strength,
                "All-zero base stats should default Strength to 3",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, zeroed.PlayerAttributesPerLevel,
                "Non-positive PlayerAttributesPerLevel should default to 2",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var partial = new AttributesConfig
            {
                PlayerBaseAttributes = new AttributeSet { Strength = 5, Agility = 0, Technique = 0, Intelligence = 0 },
                PlayerAttributesPerLevel = 1
            };
            partial.EnsureValidPlayerBaseStatDefaults();
            TestBase.AssertEqual(5, partial.PlayerBaseAttributes.Strength,
                "Partial non-zero base stats should not be overwritten",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, partial.PlayerAttributesPerLevel,
                "Positive PlayerAttributesPerLevel should be preserved",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEnsureValidIntelligenceRollBonusDefaults()
        {
            Console.WriteLine("\n--- Testing EnsureValidIntelligenceRollBonusDefaults ---");

            var zeroed = new AttributesConfig { IntelligenceRollBonusPer = 0 };
            zeroed.EnsureValidIntelligenceRollBonusDefaults();
            TestBase.AssertEqual(10, zeroed.IntelligenceRollBonusPer,
                "Zero IntelligenceRollBonusPer should default to 10",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var custom = new AttributesConfig { IntelligenceRollBonusPer = 7 };
            custom.EnsureValidIntelligenceRollBonusDefaults();
            TestBase.AssertEqual(7, custom.IntelligenceRollBonusPer,
                "Positive IntelligenceRollBonusPer should be preserved",
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

        private static void TestEnsureValidEnemyXpAndGoldDefaults()
        {
            Console.WriteLine("\n--- Testing ProgressionConfig.EnsureValidEnemyXpAndGoldDefaults ---");

            var p = new ProgressionConfig
            {
                EnemyXPBase = 0,
                EnemyXPPerLevel = 0,
                EnemyGoldBase = 0,
                EnemyGoldPerLevel = 0
            };
            p.EnsureValidEnemyXpAndGoldDefaults();
            TestBase.AssertEqual(25, p.EnemyXPBase,
                "EnemyXPBase should match runtime fallback",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(5, p.EnemyXPPerLevel,
                "EnemyXPPerLevel should match runtime fallback",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(10, p.EnemyGoldBase,
                "EnemyGoldBase should use positive default",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(3, p.EnemyGoldPerLevel,
                "EnemyGoldPerLevel should use positive default",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestClassBalanceSanitizer()
        {
            Console.WriteLine("\n--- Testing ClassBalanceConfig.EnsureNonDegenerateClassMultipliers ---");

            var cb = new ClassBalanceConfig
            {
                Barbarian = new ClassMultipliers { HealthMultiplier = 0, DamageMultiplier = 0, SpeedMultiplier = 0 }
            };
            cb.EnsureNonDegenerateClassMultipliers();
            TestBase.AssertEqual(1.0, cb.Barbarian.HealthMultiplier,
                "Degenerate class multipliers should become 1.0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

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
