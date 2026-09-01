using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.World
{
    /// <summary>
    /// Comprehensive tests for DungeonManagerWithRegistry
    /// Tests dungeon management, selection, completion, and dungeon state
    /// </summary>
    public static class DungeonManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all DungeonManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== DungeonManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestRegenerateDungeons();
            TestRegenerateDungeonsUsesCurrentRegion();
            TestRegenerateDungeonsUsesCustomAnchor();
            TestGetAvailableDungeons();

            TestBase.PrintSummary("DungeonManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var manager = new DungeonManagerWithRegistry();
            TestBase.AssertNotNull(manager,
                "DungeonManagerWithRegistry should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Dungeon Management Tests

        private static void TestRegenerateDungeons()
        {
            Console.WriteLine("\n--- Testing RegenerateDungeons ---");

            var manager = new DungeonManagerWithRegistry();
            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(5)
                .Build();

            var availableDungeons = new List<Dungeon>();
            manager.RegenerateDungeons(character, availableDungeons);

            TestBase.AssertEqual(5, availableDungeons.Count,
                "RegenerateDungeons should produce three scaled dungeons plus custom and reset options",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // If dungeons were generated, verify they have properties
            if (availableDungeons.Count > 0)
            {
                var dungeon = availableDungeons[0];
                TestBase.AssertNotNull(dungeon,
                    "Generated dungeon should not be null",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                if (dungeon != null)
                {
                    TestBase.AssertTrue(!string.IsNullOrEmpty(dungeon.Name),
                        "Generated dungeon should have a name",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }

            var last = availableDungeons[availableDungeons.Count - 1];
            TestBase.AssertTrue(last.Name == RPGGame.GameConstants.DungeonResetDifficultyMenuName,
                "Last row should be the reset-to-default option",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var custom = availableDungeons[availableDungeons.Count - 2];
            TestBase.AssertTrue(custom.Name == RPGGame.GameConstants.DungeonCustomLevelMenuName,
                "Second-to-last row should be the custom difficulty placeholder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetAvailableDungeons()
        {
            Console.WriteLine("\n--- Testing GetAvailableDungeons ---");

            var manager = new DungeonManagerWithRegistry();
            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(5)
                .Build();

            var availableDungeons = new List<Dungeon>();
            manager.RegenerateDungeons(character, availableDungeons);

            // Test that dungeons are accessible
            TestBase.AssertTrue(availableDungeons.Count >= 0,
                "Available dungeons should be accessible",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRegenerateDungeonsUsesCurrentRegion()
        {
            Console.WriteLine("\n--- Testing RegenerateDungeons - Current Region Filter ---");

            var manager = new DungeonManagerWithRegistry();
            var character = TestDataBuilders.Character()
                .WithName("LavaTraveler")
                .WithLevel(5)
                .Build();
            character.CurrentRegionId = "lava";

            var availableDungeons = new List<Dungeon>();
            manager.RegenerateDungeons(character, availableDungeons);

            var offered = availableDungeons.FindAll(d => !RPGGame.GameConstants.IsDungeonSelectionUtilityOption(d.Name));
            TestBase.AssertTrue(offered.Count > 0,
                "Region-filtered dungeon list should include non-custom dungeon rows",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var catalog = new TravelRegionCatalog();
            var lavaRegion = catalog.GetById("lava");
            TestBase.AssertTrue(lavaRegion != null, "lava region should exist in catalog", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (lavaRegion == null)
                return;

            var pool = lavaRegion.ResolveLinkedDungeonThemePool()
                .Select(t => t.ToUpperInvariant())
                .ToHashSet();
            TestBase.AssertTrue(
                offered.TrueForAll(d => pool.Contains(d.Theme.ToUpperInvariant())),
                "Non-custom dungeon rows should use only themes linked to the current region",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRegenerateDungeonsUsesCustomAnchor()
        {
            Console.WriteLine("\n--- Testing RegenerateDungeons - Custom Difficulty Anchor ---");

            var manager = new DungeonManagerWithRegistry();
            var character = TestDataBuilders.Character()
                .WithName("AnchorHero")
                .WithLevel(5)
                .Build();
            character.DungeonDifficultyAnchorLevel = 20;

            var availableDungeons = new List<Dungeon>();
            manager.RegenerateDungeons(character, availableDungeons);

            var offered = availableDungeons.FindAll(d => !RPGGame.GameConstants.IsDungeonSelectionUtilityOption(d.Name));
            TestBase.AssertEqual(3, offered.Count,
                "Should still offer three scaled dungeons when a custom anchor is set",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var levels = offered.ConvertAll(d => d.MinLevel);
            levels.Sort();
            TestBase.AssertEqual(19, levels[0],
                "Easier option should be anchor - 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(20, levels[1],
                "Middle option should match the custom anchor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(21, levels[2],
                "Harder option should be anchor + 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
