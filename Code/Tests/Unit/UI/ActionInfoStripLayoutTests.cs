using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for <see cref="ActionInfoStripLayout"/> (action strip panel geometry and hit-testing).
    /// </summary>
    public static class ActionInfoStripLayoutTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionInfoStripLayout Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestCenterOfEachPanelHitsCorrectIndex(ref run, ref passed, ref failed);
            TestOutsideStripMisses(ref run, ref passed, ref failed);
            TestZeroPanels(ref run, ref passed, ref failed);
            TestGetPanelRectConsistency(ref run, ref passed, ref failed);
            TestGapBetweenPanelsMisses(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ActionInfoStripLayout Tests", run, passed, failed);
        }

        private static void TestCenterOfEachPanelHitsCorrectIndex(ref int run, ref int passed, ref int failed)
        {
            const int count = 4;
            for (int i = 0; i < count; i++)
            {
                ActionInfoStripLayout.GetPanelRect(i, count, out int px, out int py, out int pw, out int ph);
                int cx = px + pw / 2;
                int cy = py + ph / 2;
                TestBase.AssertTrue(
                    ActionInfoStripLayout.TryGetPanelIndex(cx, cy, count, out int idx) && idx == i,
                    $"Center of panel {i} should hit index {i}",
                    ref run, ref passed, ref failed);
            }
        }

        private static void TestOutsideStripMisses(ref int run, ref int passed, ref int failed)
        {
            ActionInfoStripLayout.GetStripLayout(4, out int stripX, out _, out int stripW, out int stripH,
                out _, out _, out _, out _, out int panelRowY, out int panelH);
            TestBase.AssertTrue(
                !ActionInfoStripLayout.TryGetPanelIndex(stripX - 1, panelRowY + 1, 4, out _),
                "Left of strip should miss",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                !ActionInfoStripLayout.TryGetPanelIndex(stripX + stripW, panelRowY + 1, 4, out _),
                "Right of strip should miss",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                !ActionInfoStripLayout.TryGetPanelIndex(stripX + 1, panelRowY - 1, 4, out _),
                "Above panels should miss",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                !ActionInfoStripLayout.TryGetPanelIndex(stripX + 1, panelRowY + panelH, 4, out _),
                "Below panel row should miss",
                ref run, ref passed, ref failed);
        }

        private static void TestZeroPanels(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertTrue(
                !ActionInfoStripLayout.TryGetPanelIndex(0, 0, 0, out _),
                "Zero panels returns false",
                ref run, ref passed, ref failed);
        }

        private static void TestGetPanelRectConsistency(ref int run, ref int passed, ref int failed)
        {
            const int count = 3;
            ActionInfoStripLayout.GetStripLayout(count, out int stripX, out _, out int stripW, out _,
                out int gap, out _, out _, out _, out int panelRowY, out int panelH);
            int totalW = 0;
            for (int i = 0; i < count; i++)
            {
                ActionInfoStripLayout.GetPanelRect(i, count, out int px, out int py, out int pw, out int ph);
                TestBase.AssertEqual(panelRowY, py, "Panel Y matches strip row", ref run, ref passed, ref failed);
                TestBase.AssertEqual(panelH, ph, "Panel height matches layout", ref run, ref passed, ref failed);
                if (i == 0)
                    TestBase.AssertEqual(stripX, px, "First panel X matches strip", ref run, ref passed, ref failed);
                totalW += pw;
                if (i < count - 1)
                    totalW += gap;
            }
            TestBase.AssertEqual(stripW, totalW, "Sum of panel widths + gaps equals strip width", ref run, ref passed, ref failed);
        }

        private static void TestGapBetweenPanelsMisses(ref int run, ref int passed, ref int failed)
        {
            const int count = 4;
            ActionInfoStripLayout.GetStripLayout(count, out _, out _, out _, out _,
                out int gap, out _, out _, out _, out int panelRowY, out int panelH);
            int cy = panelRowY + panelH / 2;
            for (int i = 0; i < count - 1; i++)
            {
                ActionInfoStripLayout.GetPanelRect(i, count, out int px, out _, out int pw, out _);
                int gapX = px + pw;
                TestBase.AssertTrue(
                    !ActionInfoStripLayout.TryGetPanelIndex(gapX, cy, count, out _),
                    $"Column at start of gap after panel {i} should not hit any panel",
                    ref run, ref passed, ref failed);
                if (gap > 1)
                {
                    TestBase.AssertTrue(
                        !ActionInfoStripLayout.TryGetPanelIndex(gapX + gap / 2, cy, count, out _),
                        $"Interior of gap after panel {i} should not hit any panel",
                        ref run, ref passed, ref failed);
                }
            }
        }
    }
}
