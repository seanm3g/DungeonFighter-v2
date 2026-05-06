using System;
using System.Linq;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.Tests.Unit.UI
{
    public static class AnimalEnemyNameColoredTextTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== AnimalEnemyNameColoredText Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            var wolfSegs = AnimalEnemyNameColoredText.TryBuildSegments("Wolf");
            TestBase.AssertTrue(wolfSegs != null && wolfSegs.Count == "Wolf".Length,
                "wolf keyword -> one segment per glyph",
                ref run, ref passed, ref failed);
            int wolfDistinct = wolfSegs!.Select(s => (s.Color.R, s.Color.G, s.Color.B)).Distinct().Count();
            TestBase.AssertTrue(wolfDistinct >= 4, "wolf palette uses several distinct shades",
                ref run, ref passed, ref failed);

            var frostSegs = AnimalEnemyNameColoredText.TryBuildSegments("Frost Wolf");
            TestBase.AssertTrue(frostSegs != null && frostSegs.Count == "Frost Wolf".Length,
                "compound name with wolf uses full-string shading",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(AnimalEnemyNameColoredText.TryBuildSegments("Goblin") == null,
                "non-creature catalog name -> no shaded template",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(AnimalEnemyNameColoredText.TryBuildSegments("combat") == null,
                "substring bat inside combat must not match whole-word bat",
                ref run, ref passed, ref failed);

            var bearSegs = AnimalEnemyNameColoredText.TryBuildSegments("Bear");
            TestBase.AssertTrue(bearSegs != null && bearSegs.Count >= 4, "bear uses multi-shade palette",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("AnimalEnemyNameColoredText Tests", run, passed, failed);
        }
    }
}
