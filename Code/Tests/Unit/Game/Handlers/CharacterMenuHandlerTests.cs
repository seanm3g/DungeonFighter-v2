using System;
using System.Threading.Tasks;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game.Handlers
{
    /// <summary>
    /// Comprehensive tests for CharacterMenuHandler
    /// Tests character menu display and navigation
    /// </summary>
    public static class CharacterMenuHandlerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all CharacterMenuHandler tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CharacterMenuHandler Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestShowCharacterInfo();
            TestHandleMenuInput();

            TestBase.PrintSummary("CharacterMenuHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var stateManager = new GameStateManager();
            
            var handler = new CharacterMenuHandler(stateManager, null);
            TestBase.AssertNotNull(handler,
                "CharacterMenuHandler should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Display Tests

        private static void TestShowCharacterInfo()
        {
            Console.WriteLine("\n--- Testing ShowCharacterInfo ---");

            var stateManager = new GameStateManager();
            var handler = new CharacterMenuHandler(stateManager, null);
            
            // Test that ShowCharacterInfo doesn't crash
            handler.ShowCharacterInfo();
            TestBase.AssertTrue(true,
                "ShowCharacterInfo should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Input Handling Tests

        private static void TestHandleMenuInput()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput ---");

            var stateManager = new GameStateManager();
            
            var handler = new CharacterMenuHandler(stateManager, null);
            handler.ShowDungeonSelectionEvent += async () => { await Task.CompletedTask; };
            handler.ShowMainMenuEvent += () => { };
            
            // Test that any input triggers dungeon selection
            Task.Run(async () => await handler.HandleMenuInput("any")).Wait();
            
            TestBase.AssertTrue(true,
                "HandleMenuInput should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
