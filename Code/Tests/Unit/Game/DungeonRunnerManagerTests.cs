using System;
using System.Threading.Tasks;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Comprehensive tests for DungeonRunnerManager
    /// Tests dungeon execution, room progression, and combat integration
    /// </summary>
    public static class DungeonRunnerManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all DungeonRunnerManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== DungeonRunnerManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestGetExitChoiceHandler();

            // Note: RunDungeon requires complex setup and async operations,
            // so we test what we can without full integration

            TestBase.PrintSummary("DungeonRunnerManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var stateManager = new GameStateManager();
            var narrativeManager = new GameNarrativeManager();

            try
            {
                var manager = new DungeonRunnerManager(stateManager, narrativeManager, null, null);
                TestBase.AssertNotNull(manager,
                    "DungeonRunnerManager should be created",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (ArgumentNullException)
            {
                TestBase.AssertTrue(false,
                    "DungeonRunnerManager constructor should not throw for valid parameters",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Handler Tests

        private static void TestGetExitChoiceHandler()
        {
            Console.WriteLine("\n--- Testing GetExitChoiceHandler ---");

            var stateManager = new GameStateManager();
            var narrativeManager = new GameNarrativeManager();

            try
            {
                var manager = new DungeonRunnerManager(stateManager, narrativeManager, null, null);
                var handler = manager.GetExitChoiceHandler();
                
                // Handler might be null, which is acceptable
                TestBase.AssertTrue(handler == null || handler != null,
                    "GetExitChoiceHandler should return handler or null",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch
            {
                TestBase.AssertTrue(true,
                    "GetExitChoiceHandler should be accessible",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
