using System;
using System.Threading.Tasks;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game.Handlers
{
    /// <summary>
    /// Tests for GameLoopInputHandler navigation, especially save-and-return-to-main-menu.
    /// </summary>
    public static class GameLoopInputHandlerTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== GameLoopInputHandler Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestReturnToMainMenu_FiresShowMainMenuAfterSave();
            TestReturnToMainMenu_ContinuesWhenSaveThrows();

            TestBase.PrintSummary("GameLoopInputHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestReturnToMainMenu_FiresShowMainMenuAfterSave()
        {
            Console.WriteLine("--- Return to Main Menu fires ShowMainMenu after async save ---");

            var stateManager = new GameStateManager();
            stateManager.TransitionToState(GameState.GameLoop);
            var character = new Character("LoopHero", 1);
            stateManager.SetCurrentPlayer(character);

            bool mainMenuShown = false;
            var handler = new GameLoopInputHandler(stateManager);
            handler.ShowMainMenuEvent += () => mainMenuShown = true;

            Task.Run(async () => await handler.HandleMenuInput("0")).GetAwaiter().GetResult();

            TestBase.AssertEqualEnum(GameState.MainMenu, stateManager.CurrentState,
                "Option 0 should transition to MainMenu",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(mainMenuShown,
                "Option 0 must invoke ShowMainMenuEvent after save (UI thread continuation)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestReturnToMainMenu_ContinuesWhenSaveThrows()
        {
            Console.WriteLine("--- Return to Main Menu continues when character id is missing ---");

            var stateManager = new GameStateManager();
            stateManager.TransitionToState(GameState.GameLoop);
            // Legacy fallback player may have no registered character id; save can fail.
            var character = new Character("OrphanHero", 1);
            stateManager.SetCurrentPlayer(character);

            bool mainMenuShown = false;
            var handler = new GameLoopInputHandler(stateManager);
            handler.ShowMainMenuEvent += () => mainMenuShown = true;

            Task.Run(async () => await handler.HandleMenuInput("0")).GetAwaiter().GetResult();

            TestBase.AssertEqualEnum(GameState.MainMenu, stateManager.CurrentState,
                "Option 0 should still transition to MainMenu if save fails",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(mainMenuShown,
                "ShowMainMenuEvent should still fire when save throws",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
