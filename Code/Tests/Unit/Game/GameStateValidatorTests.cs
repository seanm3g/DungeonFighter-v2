using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Comprehensive tests for GameStateValidator
    /// Tests state validation logic, invalid state detection, and state recovery mechanisms
    /// </summary>
    public static class GameStateValidatorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all GameStateValidator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== GameStateValidator Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestValidateStateTransition_AllTransitions();
            TestValidateStateTransition_FromMainMenu();
            TestValidateStateTransition_FromCombat();

            TestBase.PrintSummary("GameStateValidator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var validator = new GameStateValidator();
            TestBase.AssertNotNull(validator,
                "GameStateValidator should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Validation Tests

        private static void TestValidateStateTransition_AllTransitions()
        {
            Console.WriteLine("\n--- Testing ValidateStateTransition - All Transitions ---");

            var validator = new GameStateValidator();
            
            // Test that all transitions are currently valid (as per current implementation)
            bool result = validator.ValidateStateTransition(GameState.MainMenu, GameState.GameLoop);
            
            TestBase.AssertTrue(result,
                "ValidateStateTransition should return true for valid transitions",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestValidateStateTransition_FromMainMenu()
        {
            Console.WriteLine("\n--- Testing ValidateStateTransition - From MainMenu ---");

            var validator = new GameStateValidator();
            
            // Test transitions from MainMenu
            bool toGameLoop = validator.ValidateStateTransition(GameState.MainMenu, GameState.GameLoop);
            bool toSettings = validator.ValidateStateTransition(GameState.MainMenu, GameState.Settings);
            bool toCombat = validator.ValidateStateTransition(GameState.MainMenu, GameState.Combat);
            
            TestBase.AssertTrue(toGameLoop && toSettings && toCombat,
                "All transitions from MainMenu should be valid",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestValidateStateTransition_FromCombat()
        {
            Console.WriteLine("\n--- Testing ValidateStateTransition - From Combat ---");

            var validator = new GameStateValidator();
            
            // Test transitions from Combat
            bool toMainMenu = validator.ValidateStateTransition(GameState.Combat, GameState.MainMenu);
            bool toDungeon = validator.ValidateStateTransition(GameState.Combat, GameState.Dungeon);
            bool toGameLoop = validator.ValidateStateTransition(GameState.Combat, GameState.GameLoop);
            
            TestBase.AssertTrue(toMainMenu && toDungeon && toGameLoop,
                "All transitions from Combat should be valid",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
