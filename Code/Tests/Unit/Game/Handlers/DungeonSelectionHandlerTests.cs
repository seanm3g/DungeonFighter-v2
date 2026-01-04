using System;
using System.Threading.Tasks;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game.Handlers
{
    /// <summary>
    /// Comprehensive tests for DungeonSelectionHandler
    /// Tests dungeon selection and display
    /// </summary>
    public static class DungeonSelectionHandlerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all DungeonSelectionHandler tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== DungeonSelectionHandler Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestShowDungeonSelection();
            TestHandleMenuInput_ValidDungeon();
            TestHandleMenuInput_ReturnToGameLoop();
            TestHandleMenuInput_InvalidChoice();
            TestHandleMenuInput_NoCharacter();

            TestBase.PrintSummary("DungeonSelectionHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var stateManager = new GameStateManager();
            var dungeonManager = new DungeonManagerWithRegistry();
            
            var handler = new DungeonSelectionHandler(stateManager, dungeonManager, null);
            TestBase.AssertNotNull(handler,
                "DungeonSelectionHandler should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Display Tests

        private static void TestShowDungeonSelection()
        {
            Console.WriteLine("\n--- Testing ShowDungeonSelection ---");

            var stateManager = new GameStateManager();
            var dungeonManager = new DungeonManagerWithRegistry();
            var handler = new DungeonSelectionHandler(stateManager, dungeonManager, null);
            
            // Test that ShowDungeonSelection doesn't crash
            Task.Run(async () => await handler.ShowDungeonSelection()).Wait();
            TestBase.AssertTrue(true,
                "ShowDungeonSelection should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Input Handling Tests

        private static void TestHandleMenuInput_ValidDungeon()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Valid Dungeon ---");

            var stateManager = new GameStateManager();
            var dungeonManager = new DungeonManagerWithRegistry();
            var character = new Character("TestHero", 1);
            stateManager.SetCurrentPlayer(character);
            
            // Add a dungeon to available dungeons
            var dungeon = new Dungeon("Test Dungeon", 1, 1, "Test");
            stateManager.AvailableDungeons.Add(dungeon);
            
            var handler = new DungeonSelectionHandler(stateManager, dungeonManager, null);
            handler.StartDungeonEvent += async () => { await Task.CompletedTask; };
            
            // Test valid dungeon selection
            Task.Run(async () => await handler.HandleMenuInput("1")).Wait();
            
            TestBase.AssertTrue(true,
                "HandleMenuInput for valid dungeon should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_ReturnToGameLoop()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Return to Game Loop ---");

            var stateManager = new GameStateManager();
            var dungeonManager = new DungeonManagerWithRegistry();
            var character = new Character("TestHero", 1);
            stateManager.SetCurrentPlayer(character);
            
            var handler = new DungeonSelectionHandler(stateManager, dungeonManager, null);
            handler.ShowGameLoopEvent += () => { };
            
            // Test return to game loop
            Task.Run(async () => await handler.HandleMenuInput("0")).Wait();
            
            TestBase.AssertEqualEnum(GameState.GameLoop, stateManager.CurrentState,
                "State should transition to GameLoop",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_InvalidChoice()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Invalid Choice ---");

            var stateManager = new GameStateManager();
            var dungeonManager = new DungeonManagerWithRegistry();
            var character = new Character("TestHero", 1);
            stateManager.SetCurrentPlayer(character);
            
            string? messageReceived = null;
            var handler = new DungeonSelectionHandler(stateManager, dungeonManager, null);
            handler.ShowMessageEvent += (msg) => { messageReceived = msg; };
            
            // Test invalid choice
            Task.Run(async () => await handler.HandleMenuInput("99")).Wait();
            
            TestBase.AssertTrue(messageReceived != null && messageReceived.Contains("Invalid"),
                "HandleMenuInput should show error for invalid choice",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_NoCharacter()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - No Character ---");

            var stateManager = new GameStateManager();
            var dungeonManager = new DungeonManagerWithRegistry();
            stateManager.SetCurrentPlayer(null);
            
            var handler = new DungeonSelectionHandler(stateManager, dungeonManager, null);
            
            // Test input with no character (should return early)
            Task.Run(async () => await handler.HandleMenuInput("1")).Wait();
            
            TestBase.AssertTrue(true,
                "HandleMenuInput should complete even with no character",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
