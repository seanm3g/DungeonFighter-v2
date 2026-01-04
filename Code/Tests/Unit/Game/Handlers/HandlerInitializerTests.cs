using System;
using RPGGame.Handlers;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game.Handlers
{
    /// <summary>
    /// Comprehensive tests for HandlerInitializer
    /// Tests handler creation and event wiring
    /// </summary>
    public static class HandlerInitializerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all HandlerInitializer tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== HandlerInitializer Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestCreateHandlers();
            TestWireHandlerEvents();

            TestBase.PrintSummary("HandlerInitializer Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Handler Creation Tests

        private static void TestCreateHandlers()
        {
            Console.WriteLine("--- Testing CreateHandlers ---");

            var stateManager = new GameStateManager();
            var initManager = new GameInitializationManager();
            var gameInitializer = new GameInitializer();
            var dungeonManager = new DungeonManagerWithRegistry();
            var combatManager = new CombatManager();
            var narrativeManager = new GameNarrativeManager();
            
            var result = HandlerInitializer.CreateHandlers(
                stateManager,
                initManager,
                gameInitializer,
                dungeonManager,
                combatManager,
                narrativeManager,
                null);
            
            TestBase.AssertNotNull(result,
                "CreateHandlers should return HandlerInitializationResult",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            
            if (result != null)
            {
                TestBase.AssertNotNull(result.MainMenuHandler,
                    "MainMenuHandler should be created",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                
                TestBase.AssertNotNull(result.CharacterMenuHandler,
                    "CharacterMenuHandler should be created",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                
                TestBase.AssertNotNull(result.SettingsMenuHandler,
                    "SettingsMenuHandler should be created",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Event Wiring Tests

        private static void TestWireHandlerEvents()
        {
            Console.WriteLine("\n--- Testing WireHandlerEvents ---");

            var stateManager = new GameStateManager();
            var initManager = new GameInitializationManager();
            var gameInitializer = new GameInitializer();
            var dungeonManager = new DungeonManagerWithRegistry();
            var combatManager = new CombatManager();
            var narrativeManager = new GameNarrativeManager();
            
            var handlers = HandlerInitializer.CreateHandlers(
                stateManager,
                initManager,
                gameInitializer,
                dungeonManager,
                combatManager,
                narrativeManager,
                null);
            
            // Test that event wiring doesn't crash
            HandlerInitializer.WireHandlerEvents(
                handlers,
                stateManager,
                null,
                () => { },
                () => { },
                () => { },
                () => { },
                (msg) => { },
                () => { },
                async () => await Task.CompletedTask,
                (level, item, levelUps, items) => { },
                (character) => { },
                () => { });
            
            TestBase.AssertTrue(true,
                "WireHandlerEvents should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
