using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Hover on dungeon-selection rows must not change glyph layout — only color.
    /// </summary>
    public static class DungeonSelectionHoverLayoutTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;
            Console.WriteLine("=== DungeonSelectionHoverLayout Tests ===\n");

            TestPerCharSegmentsOneGlyphEach(ref run, ref passed, ref failed);
            TestRecolorPreservesTextAndCount(ref run, ref passed, ref failed);
            TestHoverIdleSameDisplayTextAndSegmentCount(ref run, ref passed, ref failed);
            TestHoverMatchesMenuOptionFormatter(ref run, ref passed, ref failed);
            TestRegionTerrainReusedAcrossSiteSeeds(ref run, ref passed, ref failed);

            TestBase.PrintSummary("DungeonSelectionHoverLayout Tests", run, passed, failed);
        }

        private static void TestPerCharSegmentsOneGlyphEach(ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("--- PerCharSegments is one glyph per segment ---");
            var segs = DungeonSelectionRenderer.PerCharSegments("[1] ", Colors.Gray);
            TestBase.AssertEqual(4, segs.Count, "prefix length 4 → 4 segments", ref run, ref passed, ref failed);
            TestBase.AssertTrue(segs.All(s => s.Text.Length == 1),
                "every segment is a single character",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual("[1] ", DungeonSelectionRenderer.JoinSegmentText(segs),
                "joined text matches source",
                ref run, ref passed, ref failed);
        }

        private static void TestRecolorPreservesTextAndCount(ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("\n--- RecolorSegments keeps text and count ---");
            var segs = DungeonSelectionRenderer.PerCharSegments("Haunted", Colors.Cyan);
            string before = DungeonSelectionRenderer.JoinSegmentText(segs);
            int count = segs.Count;
            DungeonSelectionRenderer.RecolorSegments(segs, ColorPalette.Yellow.GetColor());
            TestBase.AssertEqual(count, segs.Count, "recolor does not add/remove segments", ref run, ref passed, ref failed);
            TestBase.AssertEqual(before, DungeonSelectionRenderer.JoinSegmentText(segs),
                "recolor does not change characters",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(segs.All(s => s.Color == ColorPalette.Yellow.GetColor()),
                "all segments become yellow",
                ref run, ref passed, ref failed);
        }

        private static void TestHoverIdleSameDisplayTextAndSegmentCount(
            ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("\n--- Idle and hover share layout (text + segment count) ---");
            var dungeon = new Dungeon("Haunted Crypt", 1, 1, "crypt");
            var template = new List<ColoredText> { new ColoredText("Haunted Crypt", Colors.White) };

            var idle = DungeonSelectionRenderer.BuildDungeonOptionSegments(
                1, dungeon, template, lineY: 10,
                DungeonSelectionAnimationState.Instance);
            string idleText = DungeonSelectionRenderer.JoinSegmentText(idle);
            int idleCount = idle.Count;

            var hovered = DungeonSelectionRenderer.BuildDungeonOptionSegments(
                1, dungeon, template, lineY: 10,
                DungeonSelectionAnimationState.Instance);
            DungeonSelectionRenderer.RecolorSegments(hovered, ColorPalette.Yellow.GetColor());

            TestBase.AssertEqual(idleCount, hovered.Count,
                "hover keeps the same segment count as idle",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(idleText, DungeonSelectionRenderer.JoinSegmentText(hovered),
                "hover keeps the same characters as idle",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(hovered.All(s => s.Text.Length == 1),
                "hover row stays one glyph per segment (grid layout)",
                ref run, ref passed, ref failed);
        }

        private static void TestHoverMatchesMenuOptionFormatter(
            ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("\n--- Segment join matches MenuOptionFormatter.FormatDungeon ---");
            var dungeon = new Dungeon("Ancient Forest", 2, 2, "forest");
            var template = new List<ColoredText> { new ColoredText(dungeon.Name, Colors.Green) };
            var segs = DungeonSelectionRenderer.BuildDungeonOptionSegments(
                2, dungeon, template, lineY: 12,
                DungeonSelectionAnimationState.Instance);
            string joined = DungeonSelectionRenderer.JoinSegmentText(segs);
            string expected = MenuOptionFormatter.FormatDungeon(2, dungeon.Name, dungeon.MinLevel);
            TestBase.AssertEqual(expected, joined,
                "built segments match formatter display text",
                ref run, ref passed, ref failed);
        }

        /// <summary>
        /// A new dungeon list only re-places the sites: terrain is shared, sites sit on dry ground of the map,
        /// and placing sites is far cheaper than building terrain (the dungeon-selection open lag).
        /// </summary>
        private static void TestRegionTerrainReusedAcrossSiteSeeds(ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("\n--- Region map terrain is reused when sites move ---");
            var regions = RegionTravelRenderer.OrderRegions(new TravelRegionCatalog().GetAllRegions());
            if (regions.Count == 0)
            {
                TestBase.AssertTrue(false, "travel regions load", ref run, ref passed, ref failed);
                return;
            }
            string regionId = regions[0].Id;
            const int w = 130, h = 76;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var terrain = TravelMapGeography.BuildRegionTerrain(w, h, regions, regionId);
            double terrainMs = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            var a = terrain.WithSites(3, 11);
            var b = terrain.WithSites(3, 4242);
            double sitesMs = sw.Elapsed.TotalMilliseconds / 2;
            Console.WriteLine($"  terrain build {terrainMs:F1} ms, site placement {sitesMs:F2} ms");

            TestBase.AssertTrue(ReferenceEquals(a.Terrain, terrain.Terrain) && ReferenceEquals(b.Terrain, terrain.Terrain),
                "site sets share the cached terrain grid", ref run, ref passed, ref failed);
            TestBase.AssertEqual(3, a.Sites.Count, "three sites placed", ref run, ref passed, ref failed);
            TestBase.AssertTrue(a.Sites.All(s => terrain.Land[s.FineX, s.FineY]
                    && terrain.Terrain[s.FineX, s.FineY].Ground != TravelMapInk.LakeGround),
                "sites sit on dry land, not lakes or rivers", ref run, ref passed, ref failed);
            var again = TravelMapGeography.BuildRegionView(w, h, regions, regionId, 3, 11);
            TestBase.AssertTrue(again.Sites.SequenceEqual(a.Sites),
                "same seed places the same sites", ref run, ref passed, ref failed);
            TestBase.AssertTrue(sitesMs * 5 < terrainMs,
                "placing sites costs a fraction of a terrain build", ref run, ref passed, ref failed);
        }
    }
}
