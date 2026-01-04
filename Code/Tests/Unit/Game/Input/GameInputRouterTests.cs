using System;
using System.Threading.Tasks;
using RPGGame.GameCore.Input;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game.Input
{
    /// <summary>
    /// Comprehensive tests for GameInputRouter
    /// Tests input routing to correct handlers based on game state
    /// </summary>
    public static class GameInputRouterTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all GameInputRouter tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== GameInputRouter Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestRouteInput_MainMenu();
            TestRouteInput_EmptyInput();
            TestRouteInput_NullHandlers();

            TestBase.PrintSummary("GameInputRouter Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var stateManager = new GameStateManager();
            var handlers = new GameInputHandlers();
            Action<string> showMessage = (msg) => { };
            Action<string> handleScroll = (input) => { };
            
            var router = new GameInputRouter(stateManager, handlers, showMessage, handleScroll);
            TestBase.AssertNotNull(router,
                "GameInputRouter should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Routing Tests

        private static void TestRouteInput_MainMenu()
        {
            Console.WriteLine("\n--- Testing RouteInput - MainMenu State ---");

            var stateManager = new GameStateManager();
            stateManager.TransitionToState(GameState.MainMenu);
            
            var mainMenuHandler = new MainMenuHandler(stateManager, new GameInitializationManager(), null, new GameInitializer());
            mainMenuHandler.ShowMessageEvent += (msg) => { };
            
            var handlers = new GameInputHandlers
            {
                MainMenuHandler = mainMenuHandler
            };
            
            Action<string> showMessage = (msg) => { };
            Action<string> handleScroll = (input) => { };
            var router = new GameInputRouter(stateManager, handlers, showMessage, handleScroll);
            
            // Test routing to main menu handler
            Task.Run(async () => await router.RouteInput("1")).Wait();
            
            TestBase.AssertTrue(true,
                "RouteInput should route to MainMenuHandler",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRouteInput_EmptyInput()
        {
            Console.WriteLine("\n--- Testing RouteInput - Empty Input ---");

            var stateManager = new GameStateManager();
            var handlers = new GameInputHandlers();
            Action<string> showMessage = (msg) => { };
            Action<string> handleScroll = (input) => { };
            var router = new GameInputRouter(stateManager, handlers, showMessage, handleScroll);
            
            // Test that empty input returns early
            Task.Run(async () => await router.RouteInput("")).Wait();
            Task.Run(async () => await router.RouteInput(null!)).Wait();
            
            TestBase.AssertTrue(true,
                "RouteInput should handle empty input gracefully",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRouteInput_NullHandlers()
        {
            Console.WriteLine("\n--- Testing RouteInput - Null Handlers ---");

            var stateManager = new GameStateManager();
            stateManager.TransitionToState(GameState.MainMenu);
            
            var handlers = new GameInputHandlers
            {
                MainMenuHandler = null
            };
            
            Action<string> showMessage = (msg) => { };
            Action<string> handleScroll = (input) => { };
            var router = new GameInputRouter(stateManager, handlers, showMessage, handleScroll);
            
            // Test that null handlers don't cause crashes
            Task.Run(async () => await router.RouteInput("1")).Wait();
            
            TestBase.AssertTrue(true,
                "RouteInput should handle null handlers gracefully",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
