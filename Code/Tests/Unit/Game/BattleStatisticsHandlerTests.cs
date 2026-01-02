using System;
using RPGGame.Tests;
using RPGGame;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Tests for BattleStatisticsHandler
    /// Tests battle statistics, test execution, and result handling
    /// </summary>
    public static class BattleStatisticsHandlerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all BattleStatisticsHandler tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== BattleStatisticsHandler Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestConstructorWithNullStateManager();

            TestBase.PrintSummary("BattleStatisticsHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            try
            {
                var stateManager = new GameStateManager();
                var handler = new BattleStatisticsHandler(stateManager, null);
                
                TestBase.AssertTrue(handler != null,
                    "BattleStatisticsHandler should be created with valid GameStateManager",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"BattleStatisticsHandler constructor failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestConstructorWithNullStateManager()
        {
            Console.WriteLine("\n--- Testing Constructor with null state manager ---");

            try
            {
                var handler = new BattleStatisticsHandler(null!, null);
                TestBase.AssertTrue(false,
                    "BattleStatisticsHandler should throw ArgumentNullException for null state manager",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (ArgumentNullException)
            {
                TestBase.AssertTrue(true,
                    "BattleStatisticsHandler should throw ArgumentNullException for null state manager",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"BattleStatisticsHandler threw unexpected exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
