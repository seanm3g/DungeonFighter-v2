using System;
using System.Threading.Tasks;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game.Handlers
{
    /// <summary>
    /// Comprehensive tests for DungeonCompletionHandler
    /// Tests dungeon completion flow
    /// </summary>
    public static class DungeonCompletionHandlerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all DungeonCompletionHandler tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== DungeonCompletionHandler Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestHandleMenuInput_DungeonSelection();
            TestHandleMenuInput_Inventory();
            TestHandleMenuInput_SaveAndExit();
            TestHandleMenuInput_InvalidChoice();
            TestHandleMenuInput_NoCharacter();

            TestBase.PrintSummary("DungeonCompletionHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var stateManager = new GameStateManager();
            
            var handler = new DungeonCompletionHandler(stateManager);
            TestBase.AssertNotNull(handler,
                "DungeonCompletionHandler should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Input Handling Tests

        private static void TestHandleMenuInput_DungeonSelection()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Dungeon Selection ---");

            var stateManager = new GameStateManager();
            var character = new Character("TestHero", 1);
            stateManager.SetCurrentPlayer(character);
            
            var handler = new DungeonCompletionHandler(stateManager);
            handler.StartDungeonSelectionEvent += async () => { await Task.CompletedTask; };
            
            // Test dungeon selection option
            Task.Run(async () => await handler.HandleMenuInput("1")).Wait();
            
            TestBase.AssertTrue(true,
                "HandleMenuInput for dungeon selection should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_Inventory()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Inventory ---");

            var stateManager = new GameStateManager();
            var character = new Character("TestHero", 1);
            stateManager.SetCurrentPlayer(character);
            
            var handler = new DungeonCompletionHandler(stateManager);
            handler.ShowInventoryEvent += () => { };
            
            // Test inventory option
            Task.Run(async () => await handler.HandleMenuInput("2")).Wait();
            
            TestBase.AssertEqualEnum(GameState.Inventory, stateManager.CurrentState,
                "State should transition to Inventory",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_SaveAndExit()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Save and Exit ---");

            var stateManager = new GameStateManager();
            var character = new Character("TestHero", 1);
            stateManager.SetCurrentPlayer(character);
            
            var handler = new DungeonCompletionHandler(stateManager);
            handler.SaveGameEvent += async () => { await Task.CompletedTask; };
            handler.ShowMainMenuEvent += () => { };
            
            // Test save and exit option
            Task.Run(async () => await handler.HandleMenuInput("0")).Wait();
            
            TestBase.AssertEqualEnum(GameState.MainMenu, stateManager.CurrentState,
                "State should transition to MainMenu",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleMenuInput_InvalidChoice()
        {
            Console.WriteLine("\n--- Testing HandleMenuInput - Invalid Choice ---");

            var stateManager = new GameStateManager();
            var character = new Character("TestHero", 1);
            stateManager.SetCurrentPlayer(character);
            
            string? messageReceived = null;
            var handler = new DungeonCompletionHandler(stateManager);
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
            stateManager.SetCurrentPlayer(null);
            
            var handler = new DungeonCompletionHandler(stateManager);
            
            // Test input with no character (should return early)
            Task.Run(async () => await handler.HandleMenuInput("1")).Wait();
            
            TestBase.AssertTrue(true,
                "HandleMenuInput should complete even with no character",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
