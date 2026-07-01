using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.World
{
    public static class EnemyGenerationManagerTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== EnemyGenerationManager Tests ===\n");
            int run = 0, pass = 0, fail = 0;
            ResolvePoolIncludesPlacementMatchedRegionalEnemies(ref run, ref pass, ref fail);
            ResolvePoolUsesCaseInsensitiveDungeonList(ref run, ref pass, ref fail);
            TestBase.PrintSummary("EnemyGenerationManager Tests", run, pass, fail);
        }

        private static EnemyData Make(string name, string? region = null, string? biome = null, string? location = null) =>
            new()
            {
                Name = name,
                Region = region,
                Biome = biome,
                Location = location,
                Archetype = "Berserker",
                Actions = new List<string> { "JAB" }
            };

        private static void ResolvePoolIncludesPlacementMatchedRegionalEnemies(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ResolvePoolIncludesPlacementMatchedRegionalEnemies));
            var all = new List<EnemyData>
            {
                Make("Goblin", region: "n/a", biome: "n/a", location: "General"),
                Make("Chronicle Watcher", region: "Water", biome: "Spring", location: "Chronicle Basin"),
                Make("Forest Only", region: "Forest", biome: "Forest", location: "Canopy")
            };
            var ctx = new EnemySpawnContext("Water", "Spring", "Chronicle Basin");
            var pool = EnemyGenerationManager.ResolveAvailableEnemyPool(
                all,
                possibleEnemies: new List<string> { "Goblin", "Spider" },
                ctx,
                resolvedRegion: null,
                dungeonTheme: "Forest");

            TestBase.AssertTrue(pool.Exists(e => string.Equals(e.Name, "Chronicle Watcher", StringComparison.Ordinal)),
                "regional enemy from sheet placement", ref run, ref pass, ref fail);
            TestBase.AssertTrue(pool.Exists(e => string.Equals(e.Name, "Goblin", StringComparison.Ordinal)),
                "legacy dungeon list enemy", ref run, ref pass, ref fail);
            TestBase.AssertTrue(!pool.Exists(e => string.Equals(e.Name, "Forest Only", StringComparison.Ordinal)),
                "non-matching regional enemy excluded", ref run, ref pass, ref fail);
        }

        private static void ResolvePoolUsesCaseInsensitiveDungeonList(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ResolvePoolUsesCaseInsensitiveDungeonList));
            var all = new List<EnemyData> { Make("Goblin", region: "n/a", biome: "n/a", location: "General") };
            var ctx = new EnemySpawnContext(null, "Forest", "Entrance");
            var pool = EnemyGenerationManager.ResolveAvailableEnemyPool(
                all,
                possibleEnemies: new List<string> { "goblin" },
                ctx,
                resolvedRegion: null,
                dungeonTheme: "Forest");

            TestBase.AssertEqual(1, pool.Count, "case-insensitive dungeon name", ref run, ref pass, ref fail);
        }
    }
}
