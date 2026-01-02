using System;
using RPGGame.Tests;
using RPGGame.MCP;

namespace RPGGame.Tests.Unit.MCP
{
    /// <summary>
    /// Tests for GameWrapper
    /// Tests game initialization, state management, and input handling
    /// </summary>
    public static class GameWrapperTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all GameWrapper tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== GameWrapper Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestIsGameInitialized();
            TestInitializeGame();
            TestInitializeGameTwice();

            TestBase.PrintSummary("GameWrapper Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            try
            {
                var wrapper = new GameWrapper();
                
                TestBase.AssertTrue(wrapper != null,
                    "GameWrapper should be created successfully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"GameWrapper constructor failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestIsGameInitialized()
        {
            Console.WriteLine("\n--- Testing IsGameInitialized ---");

            var wrapper = new GameWrapper();
            
            TestBase.AssertTrue(wrapper.IsGameInitialized == false,
                "IsGameInitialized should be false initially",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestInitializeGame()
        {
            Console.WriteLine("\n--- Testing InitializeGame ---");

            try
            {
                var wrapper = new GameWrapper();
                wrapper.InitializeGame();
                
                TestBase.AssertTrue(wrapper.IsGameInitialized == true,
                    "IsGameInitialized should be true after InitializeGame",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                
                wrapper.DisposeGame();
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"InitializeGame failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestInitializeGameTwice()
        {
            Console.WriteLine("\n--- Testing InitializeGame twice (should throw) ---");

            try
            {
                var wrapper = new GameWrapper();
                wrapper.InitializeGame();
                
                try
                {
                    wrapper.InitializeGame();
                    TestBase.AssertTrue(false,
                        "InitializeGame should throw InvalidOperationException when called twice",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
                catch (InvalidOperationException)
                {
                    TestBase.AssertTrue(true,
                        "InitializeGame should throw InvalidOperationException when called twice",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
                
                wrapper.DisposeGame();
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"TestInitializeGameTwice failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
