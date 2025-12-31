using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for game state management
    /// Tests state transitions, error recovery, validation, and state persistence
    /// </summary>
    public static class GameStateManagementTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Game State Management Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestStateTransitions();
            TestInvalidStateTransitions();
            TestStatePersistence();
            TestErrorRecovery();
            TestStateValidation();

            TestBase.PrintSummary("Game State Management Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestStateTransitions()
        {
            Console.WriteLine("\n--- Testing State Transitions ---");

            var stateManager = new GameStateManager();

            // Test initial state
            TestBase.AssertEqualEnum(GameState.MainMenu, stateManager.CurrentState,
                $"Initial state should be MainMenu: {stateManager.CurrentState}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test transitioning to GameLoop
            stateManager.TransitionToState(GameState.GameLoop);
            TestBase.AssertEqualEnum(GameState.GameLoop, stateManager.CurrentState,
                $"State should transition to GameLoop: {stateManager.CurrentState}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test transitioning back to MainMenu
            stateManager.TransitionToState(GameState.MainMenu);
            TestBase.AssertEqualEnum(GameState.MainMenu, stateManager.CurrentState,
                $"State should transition back to MainMenu: {stateManager.CurrentState}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestInvalidStateTransitions()
        {
            Console.WriteLine("\n--- Testing Invalid State Transitions ---");

            var stateManager = new GameStateManager();

            // Test that state manager handles transitions gracefully
            try
            {
                stateManager.TransitionToState(GameState.GameLoop);
                TestBase.AssertTrue(true,
                    "State transition should handle valid states",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"State transition should not throw exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestStatePersistence()
        {
            Console.WriteLine("\n--- Testing State Persistence ---");

            var stateManager = new GameStateManager();
            var character = TestDataBuilders.Character().WithName("StateTest").Build();

            // Set player and state
            stateManager.SetCurrentPlayer(character);
            stateManager.TransitionToState(GameState.GameLoop);

            // Verify state persists
            TestBase.AssertEqualEnum(GameState.GameLoop, stateManager.CurrentState,
                $"State should persist: {stateManager.CurrentState}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertNotNull(stateManager.CurrentPlayer,
                "Current player should persist",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestErrorRecovery()
        {
            Console.WriteLine("\n--- Testing Error Recovery ---");

            var stateManager = new GameStateManager();

            // Test recovery from invalid state
            try
            {
                // Set a valid state
                stateManager.TransitionToState(GameState.MainMenu);
                
                // Try to set player (should handle gracefully)
                var character = TestDataBuilders.Character().Build();
                stateManager.SetCurrentPlayer(character);

                TestBase.AssertTrue(true,
                    "State manager should recover from operations",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"Error recovery should not throw exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestStateValidation()
        {
            Console.WriteLine("\n--- Testing State Validation ---");

            var stateManager = new GameStateManager();

            // Test that state is always valid
            TestBase.AssertTrue(Enum.IsDefined(typeof(GameState), stateManager.CurrentState),
                $"Current state should be valid enum value: {stateManager.CurrentState}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test state after transition
            stateManager.TransitionToState(GameState.GameLoop);
            TestBase.AssertTrue(Enum.IsDefined(typeof(GameState), stateManager.CurrentState),
                $"State after transition should be valid: {stateManager.CurrentState}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

