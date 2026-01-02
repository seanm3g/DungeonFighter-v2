using System;
using RPGGame.Tests;
using RPGGame;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Tests for DungeonDisplayManager
    /// Tests dungeon display, combat log, and display buffer management
    /// </summary>
    public static class DungeonDisplayManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all DungeonDisplayManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== DungeonDisplayManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestConstructorWithNullNarrativeManager();
            TestCombatLogProperty();
            TestCompleteDisplayLogProperty();

            TestBase.PrintSummary("DungeonDisplayManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            try
            {
                var narrativeManager = new GameNarrativeManager();
                var manager = new DungeonDisplayManager(narrativeManager);
                
                TestBase.AssertTrue(manager != null,
                    "DungeonDisplayManager should be created with valid GameNarrativeManager",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"DungeonDisplayManager constructor failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestConstructorWithNullNarrativeManager()
        {
            Console.WriteLine("\n--- Testing Constructor with null narrative manager ---");

            try
            {
                var manager = new DungeonDisplayManager(null!);
                TestBase.AssertTrue(false,
                    "DungeonDisplayManager should throw ArgumentNullException for null narrative manager",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (ArgumentNullException)
            {
                TestBase.AssertTrue(true,
                    "DungeonDisplayManager should throw ArgumentNullException for null narrative manager",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"DungeonDisplayManager threw unexpected exception: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestCombatLogProperty()
        {
            Console.WriteLine("\n--- Testing CombatLog Property ---");

            var narrativeManager = new GameNarrativeManager();
            var manager = new DungeonDisplayManager(narrativeManager);
            
            TestBase.AssertTrue(manager.CombatLog != null,
                "CombatLog should return non-null list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCompleteDisplayLogProperty()
        {
            Console.WriteLine("\n--- Testing CompleteDisplayLog Property ---");

            var narrativeManager = new GameNarrativeManager();
            var manager = new DungeonDisplayManager(narrativeManager);
            
            TestBase.AssertTrue(manager.CompleteDisplayLog != null,
                "CompleteDisplayLog should return non-null list",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
