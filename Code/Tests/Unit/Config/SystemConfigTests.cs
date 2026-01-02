using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Config
{
    /// <summary>
    /// Comprehensive tests for SystemConfig
    /// Tests configuration structure and default values
    /// </summary>
    public static class SystemConfigTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all SystemConfig tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== SystemConfig Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestGameSpeedConfig();
            TestGameDataConfig();
            TestDebugConfig();
            TestBalanceAnalysisConfig();

            TestBase.PrintSummary("SystemConfig Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Configuration Tests

        private static void TestGameSpeedConfig()
        {
            Console.WriteLine("--- Testing GameSpeedConfig ---");

            var config = new GameSpeedConfig
            {
                GameTickerInterval = 0.1,
                GameSpeedMultiplier = 1.0
            };

            TestBase.AssertEqual(0.1, config.GameTickerInterval,
                "Game ticker interval should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.0, config.GameSpeedMultiplier,
                "Game speed multiplier should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGameDataConfig()
        {
            Console.WriteLine("\n--- Testing GameDataConfig ---");

            var config = new GameDataConfig
            {
                AutoGenerateOnLaunch = true,
                ShowGenerationMessages = false,
                CreateBackupsOnAutoGenerate = true,
                ForceOverwriteOnAutoGenerate = false
            };

            TestBase.AssertTrue(config.AutoGenerateOnLaunch,
                "Auto generate flag should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(config.ShowGenerationMessages,
                "Show generation messages flag should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(config.CreateBackupsOnAutoGenerate,
                "Create backups flag should default to true",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDebugConfig()
        {
            Console.WriteLine("\n--- Testing DebugConfig ---");

            var config = new DebugConfig
            {
                EnableDebugOutput = true,
                ShowCombatSimulationDebug = false,
                MaxDetailedBattles = 10
            };

            TestBase.AssertTrue(config.EnableDebugOutput,
                "Enable debug output flag should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(10, config.MaxDetailedBattles,
                "Max detailed battles should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBalanceAnalysisConfig()
        {
            Console.WriteLine("\n--- Testing BalanceAnalysisConfig ---");

            var config = new BalanceAnalysisConfig
            {
                SimulationsPerMatchup = 100,
                TargetWinRateMin = 85,
                TargetWinRateMax = 98
            };

            TestBase.AssertEqual(100, config.SimulationsPerMatchup,
                "Simulations per matchup should be settable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(config.TargetWinRateMin < config.TargetWinRateMax,
                "Min win rate should be less than max win rate",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
