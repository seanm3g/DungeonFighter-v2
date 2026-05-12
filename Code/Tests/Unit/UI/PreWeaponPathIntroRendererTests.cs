using System;
using System.Linq;
using Avalonia.Media;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Renderers.Menu;

namespace RPGGame.Tests.Unit.UI
{
    public static class PreWeaponPathIntroRendererTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            TestQuestLineMatchesRequestedCopy(ref run, ref passed, ref failed);
            TestShimmerColorInterpolatesWarmToCoolWhite(ref run, ref passed, ref failed);
            TestBuildQuestLineSegmentsPreservesTextAndShimmers(ref run, ref passed, ref failed);

            TestBase.PrintSummary(nameof(PreWeaponPathIntroRendererTests), run, passed, failed);
        }

        private static void TestQuestLineMatchesRequestedCopy(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestQuestLineMatchesRequestedCopy));

            TestBase.AssertEqual(
                "The quest to find yourself begins when you choose a path...",
                PreWeaponPathIntroRenderer.QuestLine,
                "Path intro copy should match requested text",
                ref run,
                ref passed,
                ref failed);

            TestBase.ClearCurrentTestName();
        }

        private static void TestShimmerColorInterpolatesWarmToCoolWhite(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestShimmerColorInterpolatesWarmToCoolWhite));

            Color warm = PreWeaponPathIntroRenderer.GetShimmerColorForCharacter(0, -Math.PI / 2.0);
            TestBase.AssertEqual(
                PreWeaponPathIntroRenderer.WarmWhite,
                warm,
                "Low shimmer phase should be warm white",
                ref run,
                ref passed,
                ref failed);

            Color cool = PreWeaponPathIntroRenderer.GetShimmerColorForCharacter(0, Math.PI / 2.0);
            TestBase.AssertEqual(
                PreWeaponPathIntroRenderer.CoolWhite,
                cool,
                "High shimmer phase should be cool white",
                ref run,
                ref passed,
                ref failed);

            TestBase.ClearCurrentTestName();
        }

        private static void TestBuildQuestLineSegmentsPreservesTextAndShimmers(ref int run, ref int passed, ref int failed)
        {
            TestBase.SetCurrentTestName(nameof(TestBuildQuestLineSegmentsPreservesTextAndShimmers));

            var segments = PreWeaponPathIntroRenderer.BuildQuestLineSegments(0.0);
            string text = string.Concat(segments.Select(s => s.Text));

            TestBase.AssertEqual(
                PreWeaponPathIntroRenderer.QuestLine,
                text,
                "Shimmer segments should preserve the full quote",
                ref run,
                ref passed,
                ref failed);

            bool hasMultipleColors = segments.Select(s => s.Color).Distinct().Count() > 1;
            TestBase.AssertTrue(
                hasMultipleColors,
                "Shimmer segments should vary color across the line",
                ref run,
                ref passed,
                ref failed);

            TestBase.ClearCurrentTestName();
        }
    }
}
