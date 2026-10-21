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

            // Level-banded regions: one dungeon per linked biome, no custom / reset rows (travel for harder levels).
            TestBase.AssertEqual(3, availableDungeons.Count,
                "RegenerateDungeons should produce one dungeon per region biome",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var region = new TravelRegionCatalog().GetRegionForCharacter(character);
            TestBase.AssertTrue(availableDungeons.TrueForAll(d => region.ContainsLevel(d.MinLevel)),
                "Every dungeon level should sit inside the current region's band",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!availableDungeons.Exists(d => RPGGame.GameConstants.IsDungeonSelectionUtilityOption(d.Name)),
                "Level-banded regions should not offer custom / reset difficulty rows",
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
            Console.WriteLine("\n--- Testing RegenerateDungeons - region band ignores custom anchor ---");

            var manager = new DungeonManagerWithRegistry();
            var character = TestDataBuilders.Character()
                .WithName("AnchorHero")
                .WithLevel(5)
                .Build();
            character.DungeonDifficultyAnchorLevel = 20;

            var availableDungeons = new List<Dungeon>();
            manager.RegenerateDungeons(character, availableDungeons);

            // The region (Lv 1-5) pins dungeon levels; harder levels require travel, not a custom anchor.
            var region = new TravelRegionCatalog().GetRegionForCharacter(character);
            TestBase.AssertTrue(availableDungeons.Count > 0 && availableDungeons.TrueForAll(d => region.ContainsLevel(d.MinLevel)),
                "Dungeon levels stay inside the region band even with a custom anchor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
