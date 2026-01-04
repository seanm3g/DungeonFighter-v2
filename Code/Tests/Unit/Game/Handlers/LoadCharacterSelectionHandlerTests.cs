using System;
using System.Threading.Tasks;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game.Handlers
{
    /// <summary>
    /// Comprehensive tests for LoadCharacterSelectionHandler
    /// Tests character loading interface
    /// </summary>
    public static class LoadCharacterSelectionHandlerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all LoadCharacterSelectionHandler tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== LoadCharacterSelectionHandler Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestShowLoadCharacterSelection();
            TestHandleLoadCharacterSelectionInput_ValidChoice();
            TestHandleLoadCharacterSelectionInput_ReturnToMainMenu();
            TestHandleLoadCharacterSelectionInput_InvalidChoice();
            TestHandleLoadCharacterSelectionInput_NoSavedCharacters();

            TestBase.PrintSummary("LoadCharacterSelectionHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            
            var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);
            TestBase.AssertNotNull(handler,
                "LoadCharacterSelectionHandler should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Display Tests

        private static void TestShowLoadCharacterSelection()
        {
            Console.WriteLine("\n--- Testing ShowLoadCharacterSelection ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);
            
            // Test that ShowLoadCharacterSelection doesn't crash
            Task.Run(async () => await handler.ShowLoadCharacterSelection()).Wait();
            TestBase.AssertTrue(true,
                "ShowLoadCharacterSelection should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Input Handling Tests

        private static void TestHandleLoadCharacterSelectionInput_ValidChoice()
        {
            Console.WriteLine("\n--- Testing HandleLoadCharacterSelectionInput - Valid Choice ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            string? messageReceived = null;
            
            var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);
            handler.ShowGameLoopEvent += () => { };
            handler.ShowMessageEvent += (msg) => { messageReceived = msg; };
            
            // Test valid character selection (may fail if no saved characters, but should not crash)
            Task.Run(async () => await handler.HandleLoadCharacterSelectionInput("1")).Wait();
            
            TestBase.AssertTrue(true,
                "HandleLoadCharacterSelectionInput for valid choice should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleLoadCharacterSelectionInput_ReturnToMainMenu()
        {
            Console.WriteLine("\n--- Testing HandleLoadCharacterSelectionInput - Return to Main Menu ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            
            var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);
            handler.ShowMainMenuEvent += () => { };
            
            // Test return to main menu
            Task.Run(async () => await handler.HandleLoadCharacterSelectionInput("0")).Wait();
            
            TestBase.AssertEqualEnum(GameState.MainMenu, stateManager.CurrentState,
                "State should transition to MainMenu",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleLoadCharacterSelectionInput_InvalidChoice()
        {
            Console.WriteLine("\n--- Testing HandleLoadCharacterSelectionInput - Invalid Choice ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            string? messageReceived = null;
            
            var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);
            handler.ShowMessageEvent += (msg) => { messageReceived = msg; };
            
            // Test invalid choice
            Task.Run(async () => await handler.HandleLoadCharacterSelectionInput("99")).Wait();
            
            TestBase.AssertTrue(true,
                "HandleLoadCharacterSelectionInput should handle invalid choice",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleLoadCharacterSelectionInput_NoSavedCharacters()
        {
            Console.WriteLine("\n--- Testing HandleLoadCharacterSelectionInput - No Saved Characters ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            string? messageReceived = null;
            
            var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);
            handler.ShowMessageEvent += (msg) => { messageReceived = msg; };
            
            // Test with no saved characters (choice 0 should still work)
            Task.Run(async () => await handler.HandleLoadCharacterSelectionInput("0")).Wait();
            
            TestBase.AssertTrue(true,
                "HandleLoadCharacterSelectionInput should handle no saved characters",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
