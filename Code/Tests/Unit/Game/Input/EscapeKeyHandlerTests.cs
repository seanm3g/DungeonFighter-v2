using System;
using System.Threading.Tasks;
using RPGGame;
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
            TestHandleEscapeKey_DungeonSelection_EscapeClearsCustomLevelPrompt();
            TestHandleEscapeKey_ActionInteractionLab_InvokesExitDelegate();
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
            
            System.Action exitLab = () => { };
            var handler = new EscapeKeyHandler(stateManager, handlers, showGameLoop, showMainMenu, showSettings, showDeveloperMenu, showActionEditor, exitLab);
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
            
            System.Action exitLab = () => { };
            var handler = new EscapeKeyHandler(stateManager, handlers, showGameLoop, showMainMenu, showSettings, showDeveloperMenu, showActionEditor, exitLab);
            
            Task.Run(async () => await handler.HandleEscapeKey()).Wait();
            
            TestBase.AssertEqualEnum(GameState.GameLoop, stateManager.CurrentState,
                "State should transition to GameLoop from Inventory",
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
            
            System.Action exitLab = () => { };
            var handler = new EscapeKeyHandler(stateManager, handlers, showGameLoop, showMainMenu, showSettings, showDeveloperMenu, showActionEditor, exitLab);
            
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
            
            System.Action exitLab = () => { };
            var handler = new EscapeKeyHandler(stateManager, handlers, showGameLoop, showMainMenu, showSettings, showDeveloperMenu, showActionEditor, exitLab);
            
            Task.Run(async () => await handler.HandleEscapeKey()).Wait();
            
            TestBase.AssertEqualEnum(GameState.GameLoop, stateManager.CurrentState,
                "State should transition to GameLoop from DungeonSelection",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleEscapeKey_DungeonSelection_EscapeClearsCustomLevelPrompt()
        {
            Console.WriteLine("\n--- Testing HandleEscapeKey - DungeonSelection clears custom level prompt ---");

            var stateManager = new GameStateManager();
            stateManager.TransitionToState(GameState.DungeonSelection);
            var character = new Character("Hero", 2);
            stateManager.SetCurrentPlayer(character);

            stateManager.AvailableDungeons.Clear();
            stateManager.AvailableDungeons.Add(new Dungeon("A", 1, 1, "Forest"));
            stateManager.AvailableDungeons.Add(new Dungeon("B", 2, 2, "Forest"));
            stateManager.AvailableDungeons.Add(new Dungeon("C", 3, 3, "Forest"));
            stateManager.AvailableDungeons.Add(new Dungeon(RPGGame.GameConstants.DungeonCustomLevelMenuName, 1, 1, "Crypt"));

            var dungeonManager = new DungeonManagerWithRegistry();
            var dungeonSelectionHandler = new DungeonSelectionHandler(stateManager, dungeonManager, null);

            Task.Run(async () => await dungeonSelectionHandler.HandleMenuInput("4")).Wait();

            var handlers = new EscapeKeyHandlers { DungeonSelectionHandler = dungeonSelectionHandler };
            System.Action showGameLoop = () => { };
            System.Action showMainMenu = () => { };
            System.Action showSettings = () => { };
            System.Action showDeveloperMenu = () => { };
            System.Action showActionEditor = () => { };
            System.Action exitLab = () => { };
            var escapeHandler = new EscapeKeyHandler(stateManager, handlers, showGameLoop, showMainMenu, showSettings, showDeveloperMenu, showActionEditor, exitLab);

            Task.Run(async () => await escapeHandler.HandleEscapeKey()).Wait();

            TestBase.AssertEqualEnum(GameState.DungeonSelection, stateManager.CurrentState,
                "Escape while entering a custom level should stay on dungeon selection",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleEscapeKey_ActionInteractionLab_InvokesExitDelegate()
        {
            Console.WriteLine("\n--- Testing HandleEscapeKey - ActionInteractionLab invokes exit delegate ---");

            var stateManager = new GameStateManager();
            stateManager.TransitionToState(GameState.ActionInteractionLab);

            var handlers = new EscapeKeyHandlers();
            int exitCalls = 0;
            System.Action showGameLoop = () => { };
            System.Action showMainMenu = () => { };
            System.Action showSettings = () => { };
            System.Action showDeveloperMenu = () => { };
            System.Action showActionEditor = () => { };
            System.Action exitLab = () => exitCalls++;

            var handler = new EscapeKeyHandler(stateManager, handlers, showGameLoop, showMainMenu, showSettings, showDeveloperMenu, showActionEditor, exitLab);

            Task.Run(async () => await handler.HandleEscapeKey()).Wait();

            TestBase.AssertEqual(1, exitCalls,
                "ActionInteractionLab escape should delegate to exitActionInteractionLab once",
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
            
            System.Action exitLab = () => { };
            var handler = new EscapeKeyHandler(stateManager, handlers, showGameLoop, showMainMenu, showSettings, showDeveloperMenu, showActionEditor, exitLab);
            
            Task.Run(async () => await handler.HandleEscapeKey()).Wait();
            
            TestBase.AssertEqualEnum(GameState.MainMenu, stateManager.CurrentState,
                "State should transition to MainMenu for default case",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
