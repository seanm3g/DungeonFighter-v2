using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Config
{
    /// <summary>
    /// Comprehensive tests for CombatConfig
    /// Tests combat configuration, balance settings, and status effect configs
    /// </summary>
    public static class CombatConfigTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all CombatConfig tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatConfig Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestCombatConfig();
            TestCombatBalanceConfig();
            TestRollDamageMultipliersConfig();
            TestStatusEffectScalingConfig();
            TestEnvironmentalEffectsConfig();
            TestRollSystemConfig();

            TestBase.PrintSummary("CombatConfig Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region CombatConfig Tests

        private static void TestCombatConfig()
        {
            Console.WriteLine("--- Testing CombatConfig ---");

            var config = new CombatConfig
            {
                CriticalHitThreshold = 20,
                CriticalHitMultiplier = 2.0,
                MinimumDamage = 1,
                BaseAttackTime = 1.0
            };

            TestBase.AssertEqual(20, config.CriticalHitThreshold,
                "CriticalHitThreshold should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(2.0, config.CriticalHitMultiplier,
                "CriticalHitMultiplier should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region CombatBalanceConfig Tests

        private static void TestCombatBalanceConfig()
        {
            Console.WriteLine("\n--- Testing CombatBalanceConfig ---");

            var config = new CombatBalanceConfig
            {
                CriticalHitChance = 0.05,
                CriticalHitDamageMultiplier = 2.0
            };

            TestBase.AssertNotNull(config.RollDamageMultipliers,
                "RollDamageMultipliers should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(config.StatusEffectScaling,
                "StatusEffectScaling should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(config.EnvironmentalEffects,
                "EnvironmentalEffects should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region RollDamageMultipliersConfig Tests

        private static void TestRollDamageMultipliersConfig()
        {
            Console.WriteLine("\n--- Testing RollDamageMultipliersConfig ---");

            var config = new RollDamageMultipliersConfig
            {
                ComboRollDamageMultiplier = 1.5,
                BasicRollDamageMultiplier = 1.0,
                ComboAmplificationScalingMultiplier = 1.2
            };

            TestBase.AssertEqual(1.5, config.ComboRollDamageMultiplier,
                "ComboRollDamageMultiplier should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(1.0, config.BasicRollDamageMultiplier,
                "BasicRollDamageMultiplier should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region StatusEffectScalingConfig Tests

        private static void TestStatusEffectScalingConfig()
        {
            Console.WriteLine("\n--- Testing StatusEffectScalingConfig ---");

            var config = new StatusEffectScalingConfig
            {
                BleedDuration = 3.0,
                PoisonDuration = 5.0,
                StunDuration = 1.0,
                BurnDuration = 4.0
            };

            TestBase.AssertEqual(3.0, config.BleedDuration,
                "BleedDuration should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(5.0, config.PoisonDuration,
                "PoisonDuration should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region EnvironmentalEffectsConfig Tests

        private static void TestEnvironmentalEffectsConfig()
        {
            Console.WriteLine("\n--- Testing EnvironmentalEffectsConfig ---");

            var config = new EnvironmentalEffectsConfig
            {
                EnableEnvironmentalEffects = true,
                EnvironmentalDamageMultiplier = 1.2,
                EnvironmentalDebuffChance = 0.1
            };

            TestBase.AssertTrue(config.EnableEnvironmentalEffects,
                "EnableEnvironmentalEffects should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(1.2, config.EnvironmentalDamageMultiplier,
                "EnvironmentalDamageMultiplier should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region RollSystemConfig Tests

        private static void TestRollSystemConfig()
        {
            Console.WriteLine("\n--- Testing RollSystemConfig ---");

            var config = new RollSystemConfig
            {
                CriticalThreshold = 20
            };

            TestBase.AssertNotNull(config.MissThreshold,
                "MissThreshold should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(config.BasicAttackThreshold,
                "BasicAttackThreshold should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(config.ComboThreshold,
                "ComboThreshold should be initialized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
