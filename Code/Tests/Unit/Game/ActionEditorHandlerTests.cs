using System;
using RPGGame.Tests;
using RPGGame;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Tests for ActionEditorHandler
    /// Tests action editing, form processing, and state management
    /// </summary>
    public static class ActionEditorHandlerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ActionEditorHandler tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionEditorHandler Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestConstructorWithNullStateManager();

            TestBase.PrintSummary("ActionEditorHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            try
            {
                var stateManager = new GameStateManager();
                var handler = new ActionEditorHandler(stateManager, null);
                
                TestBase.AssertTrue(handler != null,
                    "ActionEditorHandler should be created with valid GameStateManager",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ActionEditorHandler constructor failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestConstructorWithNullStateManager()
        {
            Console.WriteLine("\n--- Testing Constructor with null state manager ---");

            try
            {
                var handler = new ActionEditorHandler(null!, null);
                TestBase.AssertTrue(false,
                    "ActionEditorHandler should throw ArgumentNullException for null state manager",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (ArgumentNullException)
            {
                TestBase.AssertTrue(true,
                    "ActionEditorHandler should throw ArgumentNullException for null state manager",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ActionEditorHandler threw unexpected exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
