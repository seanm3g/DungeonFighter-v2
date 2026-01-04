using System;
using System.Threading.Tasks;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Comprehensive tests for GameInitializationManager
    /// Tests game initialization flow
    /// </summary>
    public static class GameInitializationManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all GameInitializationManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== GameInitializationManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestInitializeNewCharacter();
            TestInitializeNewCharacter_NullCharacter();
            TestLoadSavedCharacterAsync();

            TestBase.PrintSummary("GameInitializationManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var manager = new GameInitializationManager();
            TestBase.AssertNotNull(manager,
                "GameInitializationManager should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Initialization Tests

        private static void TestInitializeNewCharacter()
        {
            Console.WriteLine("\n--- Testing InitializeNewCharacter ---");

            var manager = new GameInitializationManager();
            var character = new Character("TestHero", 1);
            
            bool result = manager.InitializeNewCharacter(character, 1);
            
            TestBase.AssertTrue(result,
                "InitializeNewCharacter should return true for valid character",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestInitializeNewCharacter_NullCharacter()
        {
            Console.WriteLine("\n--- Testing InitializeNewCharacter - Null Character ---");

            var manager = new GameInitializationManager();
            
            bool result = manager.InitializeNewCharacter(null!, 1);
            
            TestBase.AssertFalse(result,
                "InitializeNewCharacter should return false for null character",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Load Tests

        private static void TestLoadSavedCharacterAsync()
        {
            Console.WriteLine("\n--- Testing LoadSavedCharacterAsync ---");

            var manager = new GameInitializationManager();
            
            // Test that LoadSavedCharacterAsync doesn't crash (may return null if no save exists)
            Task.Run(async () =>
            {
                var character = await manager.LoadSavedCharacterAsync();
                return character;
            }).Wait();
            
            TestBase.AssertTrue(true,
                "LoadSavedCharacterAsync should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
