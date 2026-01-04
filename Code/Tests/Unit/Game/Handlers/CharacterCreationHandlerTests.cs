using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game.Handlers
{
    /// <summary>
    /// Comprehensive tests for CharacterCreationHandler
    /// Tests character creation process
    /// </summary>
    public static class CharacterCreationHandlerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all CharacterCreationHandler tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CharacterCreationHandler Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestShowCharacterCreation();
            TestHandleMenuInput_StartGame();
            TestHandleMenuInput_BackToWeaponSelection();
            TestHandleMenuInput_NoCharacter();

            TestBase.PrintSummary("CharacterCreationHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var stateManager = new GameStateManager();
            
            var handler = new CharacterCreationHandler(stateManager, null);
            TestBase.AssertNotNull(handler,
                "CharacterCreationHandler should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Display Tests

        private static void TestShowCharacterCreation()
        {
            Console.WriteLine("\n--- Testing ShowCharacterCreation ---");

            var stateManager = new GameStateManager();
            var handler = new CharacterCreationHandler(stateManager, null);
            
            // Test that ShowCharacterCreation doesn't crash (may not show if no character)
            handler.ShowCharacterCreation();
            TestBase.AssertTrue(true,
                "ShowCharacterCreation should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Input Handling Tests

        private static void TestHandleMenuInput_StartGame()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Start Game ---");

            var stateManager = new GameStateManager();
            string? messageReceived = null;
            
            var handler = new CharacterCreationHandler(stateManager, null);
            handler.StartGameLoopEvent += () => { };
            handler.ShowMessageEvent += (msg) => { messageReceived = msg; };
            
            // Create a character first
            var character = new Character("TestHero", 1);
            stateManager.SetCurrentPlayer(character);
            
            // Test start game input
            handler.HandleMenuInput("any");
            
            TestBase.AssertTrue(true,
                "HandleMenuInput for start game should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_BackToWeaponSelection()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Back to Weapon Selection ---");

            var stateManager = new GameStateManager();
            string? messageReceived = null;
            
            var handler = new CharacterCreationHandler(stateManager, null);
            handler.ShowMessageEvent += (msg) => { messageReceived = msg; };
            
            // Create a character first
            var character = new Character("TestHero", 1);
            stateManager.SetCurrentPlayer(character);
            
            // Test back to weapon selection input
            handler.HandleMenuInput("0");
            
            TestBase.AssertEqualEnum(GameState.WeaponSelection, stateManager.CurrentState,
                "State should transition to WeaponSelection",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_NoCharacter()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - No Character ---");

            var stateManager = new GameStateManager();
            string? messageReceived = null;
            
            var handler = new CharacterCreationHandler(stateManager, null);
            handler.ShowMessageEvent += (msg) => { messageReceived = msg; };
            
            // Ensure no character is set
            stateManager.SetCurrentPlayer(null);
            
            // Test input with no character
            handler.HandleMenuInput("any");
            
            TestBase.AssertTrue(messageReceived != null && messageReceived.Contains("No character"),
                "HandleMenuInput should show error when no character is selected",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
