using System;
using System.Threading.Tasks;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game.Handlers
{
    /// <summary>
    /// Comprehensive tests for DeathScreenHandler
    /// Tests death screen and character management
    /// </summary>
    public static class DeathScreenHandlerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all DeathScreenHandler tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== DeathScreenHandler Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestShowDeathScreen();
            TestShowDeathScreen_NullCharacter();
            TestHandleMenuInput();
            TestHandleMenuInput_NoCharacter();

            TestBase.PrintSummary("DeathScreenHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var stateManager = new GameStateManager();
            
            var handler = new DeathScreenHandler(stateManager);
            TestBase.AssertNotNull(handler,
                "DeathScreenHandler should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Display Tests

        private static void TestShowDeathScreen()
        {
            Console.WriteLine("\n--- Testing ShowDeathScreen ---");

            var stateManager = new GameStateManager();
            var handler = new DeathScreenHandler(stateManager);
            var character = new Character("TestHero", 1);
            
            // Test that ShowDeathScreen doesn't crash
            handler.ShowDeathScreen(character);
            TestBase.AssertTrue(true,
                "ShowDeathScreen should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestShowDeathScreen_NullCharacter()
        {
            Console.WriteLine("\n--- Testing ShowDeathScreen - Null Character ---");

            var stateManager = new GameStateManager();
            var handler = new DeathScreenHandler(stateManager);
            
            // Test that ShowDeathScreen handles null character gracefully
            handler.ShowDeathScreen(null);
            TestBase.AssertTrue(true,
                "ShowDeathScreen should handle null character gracefully",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Input Handling Tests

        private static void TestHandleMenuInput()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput ---");

            var stateManager = new GameStateManager();
            var character = new Character("TestHero", 1);
            stateManager.SetCurrentPlayer(character);
            
            var handler = new DeathScreenHandler(stateManager);
            handler.ShowMainMenuEvent += () => { };
            
            // Test that any input returns to main menu
            Task.Run(async () => await handler.HandleMenuInput("any")).Wait();
            
            TestBase.AssertEqualEnum(GameState.MainMenu, stateManager.CurrentState,
                "State should transition to MainMenu",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_NoCharacter()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - No Character ---");

            var stateManager = new GameStateManager();
            stateManager.SetCurrentPlayer(null);
            
            var handler = new DeathScreenHandler(stateManager);
            handler.ShowMainMenuEvent += () => { };
            
            // Test input with no character
            Task.Run(async () => await handler.HandleMenuInput("any")).Wait();
            
            TestBase.AssertEqualEnum(GameState.MainMenu, stateManager.CurrentState,
                "State should transition to MainMenu even with no character",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
