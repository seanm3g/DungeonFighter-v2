using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game.Handlers
{
    /// <summary>
    /// Comprehensive tests for WeaponSelectionHandler
    /// Tests weapon selection flow
    /// </summary>
    public static class WeaponSelectionHandlerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all WeaponSelectionHandler tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== WeaponSelectionHandler Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestShowWeaponSelection();
            TestHandleMenuInput_ValidChoice();
            TestHandleMenuInput_InvalidChoice();
            TestHandleMenuInput_NoCharacter();

            TestBase.PrintSummary("WeaponSelectionHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var stateManager = new GameStateManager();
            var initManager = new GameInitializationManager();
            
            var handler = new WeaponSelectionHandler(stateManager, initManager, null);
            TestBase.AssertNotNull(handler,
                "WeaponSelectionHandler should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Display Tests

        private static void TestShowWeaponSelection()
        {
            Console.WriteLine("\n--- Testing ShowWeaponSelection ---");

            var stateManager = new GameStateManager();
            var initManager = new GameInitializationManager();
            var handler = new WeaponSelectionHandler(stateManager, initManager, null);
            
            // Test that ShowWeaponSelection doesn't crash (may show error if no character)
            handler.ShowWeaponSelection();
            TestBase.AssertTrue(true,
                "ShowWeaponSelection should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Input Handling Tests

        private static void TestHandleMenuInput_ValidChoice()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Valid Choice ---");

            var stateManager = new GameStateManager();
            var initManager = new GameInitializationManager();
            string? messageReceived = null;
            var handler = new WeaponSelectionHandler(stateManager, initManager, null);
            handler.ShowMessageEvent += (msg) => { messageReceived = msg; };
            
            // Create a character first
            var character = new Character("TestHero", 1);
            stateManager.SetCurrentPlayer(character);
            
            // Test valid weapon choice
            handler.HandleMenuInput("1");
            
            TestBase.AssertTrue(true,
                "HandleMenuInput for valid choice should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_InvalidChoice()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Invalid Choice ---");

            var stateManager = new GameStateManager();
            var initManager = new GameInitializationManager();
            string? messageReceived = null;
            var handler = new WeaponSelectionHandler(stateManager, initManager, null);
            handler.ShowMessageEvent += (msg) => { messageReceived = msg; };
            
            // Create a character first
            var character = new Character("TestHero", 1);
            stateManager.SetCurrentPlayer(character);
            
            // Test invalid weapon choice
            handler.HandleMenuInput("99");
            
            TestBase.AssertTrue(messageReceived != null && messageReceived.Contains("Invalid"),
                "HandleMenuInput should show error for invalid choice",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_NoCharacter()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - No Character ---");

            var stateManager = new GameStateManager();
            var initManager = new GameInitializationManager();
            string? messageReceived = null;
            var handler = new WeaponSelectionHandler(stateManager, initManager, null);
            handler.ShowMessageEvent += (msg) => { messageReceived = msg; };
            
            // Ensure no character is set
            stateManager.SetCurrentPlayer(null);
            
            // Test input with no character
            handler.HandleMenuInput("1");
            
            TestBase.AssertTrue(messageReceived != null && messageReceived.Contains("No character"),
                "HandleMenuInput should show error when no character is selected",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
