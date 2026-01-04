using System;
using System.Threading.Tasks;
using RPGGame.GameCore.Input;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game.Input
{
    /// <summary>
    /// Comprehensive tests for EscapeKeyHandler
    /// Tests escape key handling and navigation
    /// </summary>
    public static class EscapeKeyHandlerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all EscapeKeyHandler tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== EscapeKeyHandler Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestHandleEscapeKey_Inventory();
            TestHandleEscapeKey_CharacterInfo();
            TestHandleEscapeKey_DungeonSelection();
            TestHandleEscapeKey_Default();

            TestBase.PrintSummary("EscapeKeyHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var stateManager = new GameStateManager();
            var handlers = new EscapeKeyHandlers();
            System.Action showGameLoop = () => { };
            System.Action showMainMenu = () => { };
            System.Action showSettings = () => { };
            System.Action showDeveloperMenu = () => { };
            System.Action showActionEditor = () => { };
            
            var handler = new EscapeKeyHandler(stateManager, handlers, showGameLoop, showMainMenu, showSettings, showDeveloperMenu, showActionEditor);
            TestBase.AssertNotNull(handler,
                "EscapeKeyHandler should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Escape Key Handling Tests

        private static void TestHandleEscapeKey_Inventory()
        {
            Console.WriteLine("\n--- Testing HandleEscapeKey - Inventory State ---");

            var stateManager = new GameStateManager();
            stateManager.TransitionToState(GameState.Inventory);
            
            var handlers = new EscapeKeyHandlers();
            System.Action showGameLoop = () => { };
            System.Action showMainMenu = () => { };
            System.Action showSettings = () => { };
            System.Action showDeveloperMenu = () => { };
            System.Action showActionEditor = () => { };
            
            var handler = new EscapeKeyHandler(stateManager, handlers, showGameLoop, showMainMenu, showSettings, showDeveloperMenu, showActionEditor);
            
            Task.Run(async () => await handler.HandleEscapeKey()).Wait();
            
            TestBase.AssertEqualEnum(GameState.MainMenu, stateManager.CurrentState,
                "State should transition to MainMenu from Inventory",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleEscapeKey_CharacterInfo()
        {
            Console.WriteLine("\n--- Testing HandleEscapeKey - CharacterInfo State ---");

            var stateManager = new GameStateManager();
            stateManager.TransitionToState(GameState.CharacterInfo);
            
            var handlers = new EscapeKeyHandlers();
            System.Action showGameLoop = () => { };
            System.Action showMainMenu = () => { };
            System.Action showSettings = () => { };
            System.Action showDeveloperMenu = () => { };
            System.Action showActionEditor = () => { };
            
            var handler = new EscapeKeyHandler(stateManager, handlers, showGameLoop, showMainMenu, showSettings, showDeveloperMenu, showActionEditor);
            
            Task.Run(async () => await handler.HandleEscapeKey()).Wait();
            
            TestBase.AssertEqualEnum(GameState.MainMenu, stateManager.CurrentState,
                "State should transition to MainMenu from CharacterInfo",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleEscapeKey_DungeonSelection()
        {
            Console.WriteLine("\n--- Testing HandleEscapeKey - DungeonSelection State ---");

            var stateManager = new GameStateManager();
            stateManager.TransitionToState(GameState.DungeonSelection);
            
            var handlers = new EscapeKeyHandlers();
            System.Action showGameLoop = () => { };
            System.Action showMainMenu = () => { };
            System.Action showSettings = () => { };
            System.Action showDeveloperMenu = () => { };
            System.Action showActionEditor = () => { };
            
            var handler = new EscapeKeyHandler(stateManager, handlers, showGameLoop, showMainMenu, showSettings, showDeveloperMenu, showActionEditor);
            
            Task.Run(async () => await handler.HandleEscapeKey()).Wait();
            
            TestBase.AssertEqualEnum(GameState.GameLoop, stateManager.CurrentState,
                "State should transition to GameLoop from DungeonSelection",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleEscapeKey_Default()
        {
            Console.WriteLine("\n--- Testing HandleEscapeKey - Default State ---");

            var stateManager = new GameStateManager();
            stateManager.TransitionToState(GameState.GameLoop);
            
            var handlers = new EscapeKeyHandlers();
            System.Action showGameLoop = () => { };
            System.Action showMainMenu = () => { };
            System.Action showSettings = () => { };
            System.Action showDeveloperMenu = () => { };
            System.Action showActionEditor = () => { };
            
            var handler = new EscapeKeyHandler(stateManager, handlers, showGameLoop, showMainMenu, showSettings, showDeveloperMenu, showActionEditor);
            
            Task.Run(async () => await handler.HandleEscapeKey()).Wait();
            
            TestBase.AssertEqualEnum(GameState.MainMenu, stateManager.CurrentState,
                "State should transition to MainMenu for default case",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
