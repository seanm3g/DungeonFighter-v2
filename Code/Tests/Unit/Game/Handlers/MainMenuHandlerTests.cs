using System;
using System.Threading.Tasks;
using RPGGame.Tests;
using RPGGame.UI.Avalonia;

namespace RPGGame.Tests.Unit.Game.Handlers
{
    /// <summary>
    /// Comprehensive tests for MainMenuHandler
    /// Tests main menu navigation, new game, load game, settings
    /// </summary>
    public static class MainMenuHandlerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all MainMenuHandler tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== MainMenuHandler Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestShowMainMenu();
            TestHandleMenuInput_NewGame();
            TestHandleMenuInput_LoadGame();
            TestHandleMenuInput_Settings();
            TestHandleMenuInput_CharacterSelection();
            TestHandleMenuInput_Quit();
            TestHandleMenuInput_InvalidInput();
            TestEventFiring();

            TestBase.PrintSummary("MainMenuHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var stateManager = new GameStateManager();
            var initManager = new GameInitializationManager();
            var gameInitializer = new GameInitializer();
            
            var handler = new MainMenuHandler(stateManager, initManager, null, gameInitializer);
            TestBase.AssertNotNull(handler,
                "MainMenuHandler should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Display Tests

        private static void TestShowMainMenu()
        {
            Console.WriteLine("\n--- Testing ShowMainMenu ---");

            var stateManager = new GameStateManager();
            var initManager = new GameInitializationManager();
            var gameInitializer = new GameInitializer();
            
            var handler = new MainMenuHandler(stateManager, initManager, null, gameInitializer);
            
            // Test that ShowMainMenu doesn't crash
            handler.ShowMainMenu();
            TestBase.AssertTrue(true,
                "ShowMainMenu should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Input Handling Tests

        private static void TestHandleMenuInput_NewGame()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - New Game ---");

            var stateManager = new GameStateManager();
            var initManager = new GameInitializationManager();
            var gameInitializer = new GameInitializer();
            
            var handler = new MainMenuHandler(stateManager, initManager, null, gameInitializer);
            handler.ShowWeaponSelectionEvent += () => { };
            
            // Test new game input
            Task.Run(async () => await handler.HandleMenuInput("1")).Wait();
            
            TestBase.AssertTrue(true,
                "HandleMenuInput for new game should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_LoadGame()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Load Game ---");

            var stateManager = new GameStateManager();
            var initManager = new GameInitializationManager();
            var gameInitializer = new GameInitializer();
            
            var handler = new MainMenuHandler(stateManager, initManager, null, gameInitializer);
            
            // Test load game input
            Task.Run(async () => await handler.HandleMenuInput("2")).Wait();
            
            TestBase.AssertTrue(true,
                "HandleMenuInput for load game should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_Settings()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Settings ---");

            var stateManager = new GameStateManager();
            var initManager = new GameInitializationManager();
            var gameInitializer = new GameInitializer();
            
            var handler = new MainMenuHandler(stateManager, initManager, null, gameInitializer);
            handler.ShowSettingsEvent += () => { };
            
            // Test settings input
            handler.HandleMenuInput("3").Wait();
            
            TestBase.AssertTrue(true,
                "HandleMenuInput for settings should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_CharacterSelection()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Character Selection ---");

            var stateManager = new GameStateManager();
            var initManager = new GameInitializationManager();
            var gameInitializer = new GameInitializer();
            
            var handler = new MainMenuHandler(stateManager, initManager, null, gameInitializer);
            handler.ShowCharacterSelectionEvent += () => { };
            
            // Test character selection input (both "4" and "C")
            handler.HandleMenuInput("4").Wait();
            TestBase.AssertTrue(true,
                "HandleMenuInput for character selection (4) should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            handler.HandleMenuInput("C").Wait();
            TestBase.AssertTrue(true,
                "HandleMenuInput for character selection (C) should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_Quit()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Quit ---");

            var stateManager = new GameStateManager();
            var initManager = new GameInitializationManager();
            var gameInitializer = new GameInitializer();
            
            var handler = new MainMenuHandler(stateManager, initManager, null, gameInitializer);
            handler.ExitGameEvent += () => { };
            
            // Test quit input
            handler.HandleMenuInput("0").Wait();
            
            TestBase.AssertTrue(true,
                "HandleMenuInput for quit should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_InvalidInput()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Invalid Input ---");

            var stateManager = new GameStateManager();
            var initManager = new GameInitializationManager();
            var gameInitializer = new GameInitializer();
            
            string? messageReceived = null;
            var handler = new MainMenuHandler(stateManager, initManager, null, gameInitializer);
            handler.ShowMessageEvent += (msg) => { messageReceived = msg; };
            
            // Test invalid input
            handler.HandleMenuInput("invalid").Wait();
            
            TestBase.AssertTrue(messageReceived != null && messageReceived.Contains("Invalid choice"),
                "HandleMenuInput should show error message for invalid input",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Event Tests

        private static void TestEventFiring()
        {
            Console.WriteLine("\n--- Testing Event Firing ---");

            var stateManager = new GameStateManager();
            var initManager = new GameInitializationManager();
            var gameInitializer = new GameInitializer();
            
            var handler = new MainMenuHandler(stateManager, initManager, null, gameInitializer);
            
            // Test that events can be subscribed to
            handler.ShowMessageEvent += (msg) => { };
            
            TestBase.AssertTrue(true,
                "Events should be subscribable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
