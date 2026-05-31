using System;
using System.Linq;
using RPGGame.Tests;
using RPGGame;
using RPGGame.UI.ColorSystem;

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
            TestEnemySurpriseCombatEvent_NoLeadingBlankAndUsesColoredMarkup();

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

        private static void TestEnemySurpriseCombatEvent_NoLeadingBlankAndUsesColoredMarkup()
        {
            Console.WriteLine("\n--- Testing enemy surprise combat event spacing/color ---");

            var narrativeManager = new GameNarrativeManager();
            var manager = new DungeonDisplayManager(narrativeManager);
            var player = new Character("TestHero", 1);

            EnemyEncounterHandler.AddEnemySurpriseCombatEvent(manager, player, new FixedRandom(0));

            var log = manager.CombatLog;
            TestBase.AssertEqual(3, log.Count,
                "Enemy surprise should add a leading blank, the colored warning, and one trailing blank",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("", log[0],
                "Enemy surprise should start with a leading blank line after enemy appears",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("You've been surprised! The enemy will strike first!",
                ColoredTextRenderer.RenderAsPlainText(ColoredTextParser.Parse(log[1])),
                "Enemy surprise plain text should match the selected message",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("", log[2],
                "Enemy surprise should keep one trailing blank before combat actions",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var coloredSegments = ColoredTextParser.Parse(log[1])
                .Where(segment => !string.IsNullOrWhiteSpace(segment.Text))
                .ToList();
            TestBase.AssertTrue(coloredSegments.Count > 0 &&
                coloredSegments.All(segment => ColorValidator.AreColorsEqual(segment.Color, ColorPalette.Red.GetColor())),
                "Enemy surprise text should use the red warning color",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private sealed class FixedRandom : Random
        {
            private readonly int value;

            public FixedRandom(int value)
            {
                this.value = value;
            }

            public override int Next(int maxValue)
            {
                return value;
            }
        }

        #endregion
    }
}
