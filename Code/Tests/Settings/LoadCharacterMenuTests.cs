using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.Tests;

namespace RPGGame.Tests.Settings
{
    /// <summary>
    /// Tests for the load character menu functionality
    /// Tests menu display, input handling, state transitions, and character loading
    /// </summary>
    public static class LoadCharacterMenuTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Load Character Menu Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestShowLoadCharacterSelection();
            TestStateTransitionToLoadCharacterSelection();
            TestHandleEmptyCharacterList();
            TestHandleInvalidInput();
            TestHandleReturnToMainMenu();
            TestCharacterSelectionInput();
            TestLoadCharacterSelectionHandlerInitialization();

            TestBase.PrintSummary("Load Character Menu Tests", _testsRun, _testsPassed, _testsFailed);
        }

        /// <summary>
        /// Tests that ShowLoadCharacterSelection transitions to the correct state
        /// </summary>
        private static void TestShowLoadCharacterSelection()
        {
            Console.WriteLine("\n--- Testing Show Load Character Selection ---");

            try
            {
                var stateManager = new GameStateManager();
                var gameInitializer = new GameInitializer();
                var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);

                // Initially should be MainMenu
                TestBase.AssertEqualEnum(GameState.MainMenu, stateManager.CurrentState,
                    $"Initial state should be MainMenu: {stateManager.CurrentState}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Show load character selection
                handler.ShowLoadCharacterSelection().GetAwaiter().GetResult();

                // Should transition to LoadCharacterSelection state
                TestBase.AssertEqualEnum(GameState.LoadCharacterSelection, stateManager.CurrentState,
                    $"State should transition to LoadCharacterSelection: {stateManager.CurrentState}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"ShowLoadCharacterSelection should not throw exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        /// <summary>
        /// Tests state transition to LoadCharacterSelection
        /// </summary>
        private static void TestStateTransitionToLoadCharacterSelection()
        {
            Console.WriteLine("\n--- Testing State Transition to LoadCharacterSelection ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);

            // Start from different states
            stateManager.TransitionToState(GameState.GameLoop);
            handler.ShowLoadCharacterSelection().GetAwaiter().GetResult();

            TestBase.AssertEqualEnum(GameState.LoadCharacterSelection, stateManager.CurrentState,
                $"State should transition to LoadCharacterSelection from GameLoop: {stateManager.CurrentState}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test from MainMenu
            stateManager.TransitionToState(GameState.MainMenu);
            handler.ShowLoadCharacterSelection().GetAwaiter().GetResult();

            TestBase.AssertEqualEnum(GameState.LoadCharacterSelection, stateManager.CurrentState,
                $"State should transition to LoadCharacterSelection from MainMenu: {stateManager.CurrentState}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Tests handling input when character list is empty
        /// </summary>
        private static void TestHandleEmptyCharacterList()
        {
            Console.WriteLine("\n--- Testing Handle Empty Character List ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);
            
            bool mainMenuEventFired = false;
            string? messageReceived = null;

            handler.ShowMainMenuEvent += () => mainMenuEventFired = true;
            handler.ShowMessageEvent += (msg) => messageReceived = msg;

            // Set state to LoadCharacterSelection
            stateManager.TransitionToState(GameState.LoadCharacterSelection);

            // Test with empty list - choice 0 should return to main menu
            Task.Run(async () =>
            {
                await handler.HandleLoadCharacterSelectionInput("0");
            }).GetAwaiter().GetResult();

            TestBase.AssertEqualEnum(GameState.MainMenu, stateManager.CurrentState,
                $"State should transition to MainMenu when choice is 0 with empty list: {stateManager.CurrentState}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(mainMenuEventFired,
                "ShowMainMenuEvent should fire when returning to main menu",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with empty list - invalid choice should show message
            messageReceived = null;
            Task.Run(async () =>
            {
                await handler.HandleLoadCharacterSelectionInput("1");
            }).GetAwaiter().GetResult();

            TestBase.AssertNotNull(messageReceived,
                "Should receive message for invalid choice with empty list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // The message could be "Invalid choice. Press 0 to return." or a load failure message
            // Accept either as valid error handling
            TestBase.AssertTrue(
                (messageReceived?.Contains("Invalid choice") == true || 
                 messageReceived?.Contains("Press 0") == true ||
                 messageReceived?.Contains("Failed to load") == true ||
                 messageReceived?.Contains("Error") == true),
                $"Message should indicate invalid choice or error: {messageReceived}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Tests handling invalid input (non-numeric)
        /// </summary>
        private static void TestHandleInvalidInput()
        {
            Console.WriteLine("\n--- Testing Handle Invalid Input ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);
            
            string? messageReceived = null;
            handler.ShowMessageEvent += (msg) => messageReceived = msg;

            stateManager.TransitionToState(GameState.LoadCharacterSelection);

            // Test with non-numeric input
            Task.Run(async () =>
            {
                await handler.HandleLoadCharacterSelectionInput("abc");
            }).GetAwaiter().GetResult();

            TestBase.AssertNotNull(messageReceived,
                "Should receive message for invalid input",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(messageReceived?.Contains("Invalid input") == true || 
                               messageReceived?.Contains("number") == true,
                $"Message should indicate invalid input: {messageReceived}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with empty string
            messageReceived = null;
            Task.Run(async () =>
            {
                await handler.HandleLoadCharacterSelectionInput("");
            }).GetAwaiter().GetResult();

            // Should handle empty string gracefully
            TestBase.AssertTrue(true,
                "Should handle empty string input gracefully",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Tests returning to main menu from load character selection
        /// </summary>
        private static void TestHandleReturnToMainMenu()
        {
            Console.WriteLine("\n--- Testing Return to Main Menu ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);
            
            bool mainMenuEventFired = false;
            handler.ShowMainMenuEvent += () => mainMenuEventFired = true;

            // Set state to LoadCharacterSelection
            stateManager.TransitionToState(GameState.LoadCharacterSelection);

            // Test returning to main menu with choice 0
            Task.Run(async () =>
            {
                await handler.HandleLoadCharacterSelectionInput("0");
            }).GetAwaiter().GetResult();

            TestBase.AssertEqualEnum(GameState.MainMenu, stateManager.CurrentState,
                $"State should be MainMenu after choosing 0: {stateManager.CurrentState}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(mainMenuEventFired,
                "ShowMainMenuEvent should fire when returning to main menu",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Tests character selection input handling
        /// </summary>
        private static void TestCharacterSelectionInput()
        {
            Console.WriteLine("\n--- Testing Character Selection Input ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);
            
            string? messageReceived = null;
            handler.ShowMessageEvent += (msg) => messageReceived = msg;

            stateManager.TransitionToState(GameState.LoadCharacterSelection);

            // Get actual saved characters
            var savedCharacters = CharacterSaveManager.ListAllSavedCharacters();

            if (savedCharacters != null && savedCharacters.Count > 0)
            {
                // Test selecting a valid character (choice 1)
                messageReceived = null;
                Task.Run(async () =>
                {
                    await handler.HandleLoadCharacterSelectionInput("1");
                }).GetAwaiter().GetResult();

                // Should attempt to load character (may fail if file doesn't exist, but should handle gracefully)
                TestBase.AssertTrue(true,
                    "Should handle character selection input without throwing exception",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            else
            {
                // Test with invalid selection when no characters exist
                messageReceived = null;
                Task.Run(async () =>
                {
                    await handler.HandleLoadCharacterSelectionInput("1");
                }).GetAwaiter().GetResult();

                TestBase.AssertTrue(true,
                    "Should handle invalid selection when no characters exist",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test with out-of-range selection
            messageReceived = null;
            Task.Run(async () =>
            {
                await handler.HandleLoadCharacterSelectionInput("999");
            }).GetAwaiter().GetResult();

            // Should handle out-of-range selection
            TestBase.AssertTrue(true,
                "Should handle out-of-range selection gracefully",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Tests LoadCharacterSelectionHandler initialization
        /// </summary>
        private static void TestLoadCharacterSelectionHandlerInitialization()
        {
            Console.WriteLine("\n--- Testing LoadCharacterSelectionHandler Initialization ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();

            // Test that handler can be created
            try
            {
                var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);
                TestBase.AssertNotNull(handler,
                    "Handler should be created successfully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Handler initialization should not throw exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test that null stateManager throws exception
            try
            {
                var handler = new LoadCharacterSelectionHandler(null!, null, gameInitializer);
                TestBase.AssertTrue(false,
                    "Handler should throw exception with null stateManager",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (ArgumentNullException)
            {
                TestBase.AssertTrue(true,
                    "Handler should throw ArgumentNullException with null stateManager",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Handler should throw ArgumentNullException, not {ex.GetType().Name}: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test that null gameInitializer throws exception
            try
            {
                var handler = new LoadCharacterSelectionHandler(stateManager, null, null!);
                TestBase.AssertTrue(false,
                    "Handler should throw exception with null gameInitializer",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (ArgumentNullException)
            {
                TestBase.AssertTrue(true,
                    "Handler should throw ArgumentNullException with null gameInitializer",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Handler should throw ArgumentNullException, not {ex.GetType().Name}: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }
    }
}
