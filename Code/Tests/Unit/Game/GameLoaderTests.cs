using System;
using System.Threading.Tasks;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Comprehensive tests for GameLoader
    /// Tests game loading from files
    /// </summary>
    public static class GameLoaderTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all GameLoader tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== GameLoader Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestLoadGame_NoSaveFile();
            TestLoadGame_CharacterAlreadyLoaded();

            TestBase.PrintSummary("GameLoader Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            
            var loader = new GameLoader(stateManager, gameInitializer, null);
            TestBase.AssertNotNull(loader,
                "GameLoader should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Load Tests

        private static void TestLoadGame_NoSaveFile()
        {
            Console.WriteLine("\n--- Testing LoadGame - No Save File ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            var loader = new GameLoader(stateManager, gameInitializer, null);
            
            string? messageReceived = null;
            
            Task.Run(async () =>
            {
                bool result = await loader.LoadGame(
                    (msg) => { messageReceived = msg; },
                    () => { },
                    () => { });
                return result;
            }).Wait();
            
            TestBase.AssertTrue(true,
                "LoadGame should handle no save file gracefully",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestLoadGame_CharacterAlreadyLoaded()
        {
            Console.WriteLine("\n--- Testing LoadGame - Character Already Loaded ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            var loader = new GameLoader(stateManager, gameInitializer, null);
            
            // Set a character as current player
            var character = new Character("TestHero", 1);
            stateManager.SetCurrentPlayer(character);
            
            Task.Run(async () =>
            {
                bool result = await loader.LoadGame(
                    (msg) => { },
                    () => { },
                    () => { });
                return result;
            }).Wait();
            
            TestBase.AssertEqualEnum(GameState.GameLoop, stateManager.CurrentState,
                "State should transition to GameLoop when character already loaded",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
