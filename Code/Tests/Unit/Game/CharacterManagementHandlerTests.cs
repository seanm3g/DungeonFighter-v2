using System;
using RPGGame.Tests;
using RPGGame;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Tests for CharacterManagementHandler
    /// Tests character management, selection, and switching
    /// </summary>
    public static class CharacterManagementHandlerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all CharacterManagementHandler tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CharacterManagementHandler Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestConstructorWithNullStateManager();

            TestBase.PrintSummary("CharacterManagementHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            try
            {
                var stateManager = new GameStateManager();
                var handler = new CharacterManagementHandler(stateManager, null);
                
                TestBase.AssertTrue(handler != null,
                    "CharacterManagementHandler should be created with valid GameStateManager",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"CharacterManagementHandler constructor failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestConstructorWithNullStateManager()
        {
            Console.WriteLine("\n--- Testing Constructor with null state manager ---");

            try
            {
                var handler = new CharacterManagementHandler(null!, null);
                TestBase.AssertTrue(false,
                    "CharacterManagementHandler should throw ArgumentNullException for null state manager",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (ArgumentNullException)
            {
                TestBase.AssertTrue(true,
                    "CharacterManagementHandler should throw ArgumentNullException for null state manager",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"CharacterManagementHandler threw unexpected exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
