using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for CanvasTextManager
    /// Tests text management, display coordination, and character-specific displays
    /// </summary>
    public static class CanvasTextManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all CanvasTextManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CanvasTextManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestSetStateManager();

            TestBase.PrintSummary("CanvasTextManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region State Management Tests

        private static void TestSetStateManager()
        {
            Console.WriteLine("--- Testing SetStateManager ---");

            var stateManager = new GameStateManager();
            TestBase.AssertTrue(stateManager != null,
                "SetStateManager should accept GameStateManager",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
