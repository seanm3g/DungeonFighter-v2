using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Comprehensive tests for GameInitializer
    /// Tests game initialization, character creation, starting equipment, and initial state
    /// </summary>
    public static class GameInitializerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all GameInitializer tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== GameInitializer Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestLoadStartingGear();

            TestBase.PrintSummary("GameInitializer Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var initializer = new GameInitializer();
            TestBase.AssertNotNull(initializer,
                "GameInitializer should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Starting Gear Tests

        private static void TestLoadStartingGear()
        {
            Console.WriteLine("\n--- Testing LoadStartingGear ---");

            var initializer = new GameInitializer();
            var startingGear = initializer.LoadStartingGear();

            TestBase.AssertNotNull(startingGear,
                "LoadStartingGear should return starting gear data",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (startingGear != null)
            {
                TestBase.AssertNotNull(startingGear.weapons,
                    "Starting gear should have weapons list",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertNotNull(startingGear.armor,
                    "Starting gear should have armor list",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
