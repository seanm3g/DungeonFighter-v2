using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia;
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
            TestGetDisplayPanelCount(ref run, ref passed, ref failed);
            TestFiveDisplaySlotsAllHitWhenFilledZero(ref run, ref passed, ref failed);
            TestPanelWidthsEqualWithinOnePixel(ref run, ref passed, ref failed);
            TestStripHitTestUsesDisplaySlotCountNotComboLength(ref run, ref passed, ref failed);
            TestNarrowCenterColumnAllFiveStripSlotsHittable(ref run, ref passed, ref failed);
            TestGetPanelBorderColorBeyondEffectiveMaxIsBlack(ref run, ref passed, ref failed);
            TestGetPanelBorderColorWithinMaxUnchanged(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ActionInfoStripLayout Tests", run, passed, failed);
        }

        private static void TestGetDisplayPanelCount(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertEqual(
                LayoutConstants.ACTION_INFO_STRIP_FIXED_SLOT_COUNT,
                ActionInfoStripLayout.GetDisplayPanelCount(0),
                "GetDisplayPanelCount(0) equals fixed slot count",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(
                LayoutConstants.ACTION_INFO_STRIP_FIXED_SLOT_COUNT,
                ActionInfoStripLayout.GetDisplayPanelCount(4),
                "GetDisplayPanelCount(4) pads to fixed slot count",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(
                7,
                ActionInfoStripLayout.GetDisplayPanelCount(7),
                "GetDisplayPanelCount(7) keeps longer combo",
                ref run, ref passed, ref failed);
        }

        private static void TestFiveDisplaySlotsAllHitWhenFilledZero(ref int run, ref int passed, ref int failed)
        {
            const int displayCount = 5;
            for (int i = 0; i < displayCount; i++)
            {
                ActionInfoStripLayout.GetPanelRect(i, displayCount, out int px, out int py, out int pw, out int ph);
                int cx = px + pw / 2;
                int cy = py + ph / 2;
                TestBase.AssertTrue(
                    ActionInfoStripLayout.TryGetPanelIndex(cx, cy, displayCount, out int idx) && idx == i,
                    $"Empty-strip layout: center of panel {i} hits index {i}",
                    ref run, ref passed, ref failed);
            }
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
            int edge = LayoutConstants.ACTION_INFO_PANEL_EDGE_MARGIN;
            ActionInfoStripLayout.GetStripLayout(count, out int stripX, out _, out int stripW, out _,
                out int gap, out _, out _, out _, out int panelRowY, out int panelH);
            int totalW = 0;
            for (int i = 0; i < count; i++)
            {
                ActionInfoStripLayout.GetPanelRect(i, count, out int px, out int py, out int pw, out int ph);
                TestBase.AssertEqual(panelRowY, py, "Panel Y matches strip row", ref run, ref passed, ref failed);
                TestBase.AssertEqual(panelH, ph, "Panel height matches layout", ref run, ref passed, ref failed);
                if (i == 0)
                    TestBase.AssertEqual(stripX + edge, px, "First panel X matches strip + edge margin", ref run, ref passed, ref failed);
                totalW += pw;
                if (i < count - 1)
                    totalW += gap;
            }
            TestBase.AssertEqual(stripW - edge * 2, totalW, "Sum of panel widths + gaps equals strip width minus side margins", ref run, ref passed, ref failed);
        }

        /// <summary>
        /// Remainder from integer division must not pile onto a single panel; widths may differ by at most 1 cell.
        /// </summary>
        private static void TestPanelWidthsEqualWithinOnePixel(ref int run, ref int passed, ref int failed)
        {
            for (int count = 1; count <= 8; count++)
            {
                ActionInfoStripLayout.GetStripLayout(count, out _, out _, out _, out _,
                    out _, out _, out int panelWidth, out int remainder, out _, out _);
                int minW = int.MaxValue, maxW = 0;
                for (int i = 0; i < count; i++)
                {
                    ActionInfoStripLayout.GetPanelRect(i, count, out _, out _, out int pw, out _);
                    if (pw < minW) minW = pw;
                    if (pw > maxW) maxW = pw;
                }
                TestBase.AssertTrue(maxW - minW <= 1,
                    $"Panel widths for count={count} differ by at most 1 (min={minW}, max={maxW})",
                    ref run, ref passed, ref failed);
                int expectedSpread = remainder > 0 ? 1 : 0;
                TestBase.AssertEqual(expectedSpread, maxW - minW,
                    $"Width spread is 1 iff remainder>0 for count={count}",
                    ref run, ref passed, ref failed);
            }
        }

        /// <summary>
        /// Drag begin/end must use <see cref="ActionInfoStripLayout.GetDisplayPanelCount"/> (same as rendering).
        /// Using <c>combo.Count</c> for hit-test mis-maps panel indices when the strip is padded with empty slots,
        /// which breaks reorder to a lower index (release maps correctly; press did not).
        /// </summary>
        private static void TestStripHitTestUsesDisplaySlotCountNotComboLength(ref int run, ref int passed, ref int failed)
        {
            const int filled = 3;
            int displayCount = ActionInfoStripLayout.GetDisplayPanelCount(filled);
            TestBase.AssertTrue(displayCount > filled,
                "Regression setup: short combo should pad to fixed strip slot count",
                ref run, ref passed, ref failed);

            ActionInfoStripLayout.GetPanelRect(1, displayCount, out int px, out int py, out int pw, out int ph);
            int cx = px + pw / 2;
            int cy = py + ph / 2;

            TestBase.AssertTrue(
                ActionInfoStripLayout.TryGetPanelIndex(cx, cy, displayCount, out int displayIdx) && displayIdx == 1,
                "Center of visual panel 1 resolves to index 1 when using GetDisplayPanelCount(filled)",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                ActionInfoStripLayout.TryGetPanelIndex(cx, cy, filled, out int wrongIdx) && wrongIdx != 1,
                "Same cell must not resolve to panel 1 when panelCount is only combo length (misaligned strip math)",
                ref run, ref passed, ref failed);
        }

        /// <summary>
        /// When the center column is only ~10 cells wide, fixed gap (1) used to leave avail &lt; 5 so the last two
        /// panels got zero width and strip drag/hover missed slots 4–5. Gap must shrink so every slot stays hittable.
        /// </summary>
        private static void TestNarrowCenterColumnAllFiveStripSlotsHittable(ref int run, ref int passed, ref int failed)
        {
            const double charW = 10.0;
            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(75 * charW, charW);

            try
            {
                TestBase.AssertEqual(10, LayoutConstants.ACTION_INFO_WIDTH,
                    "Regression setup: effective width 75 yields ACTION_INFO_WIDTH 10",
                    ref run, ref passed, ref failed);

                const int displayCount = 5;
                for (int i = 0; i < displayCount; i++)
                {
                    ActionInfoStripLayout.GetPanelRect(i, displayCount, out int px, out int py, out int pw, out int ph);
                    TestBase.AssertTrue(pw >= 1,
                        $"Narrow strip: panel {i} width must be >= 1 for hit-test (got {pw})",
                        ref run, ref passed, ref failed);
                    int cx = px + pw / 2;
                    int cy = py + ph / 2;
                    TestBase.AssertTrue(
                        ActionInfoStripLayout.TryGetPanelIndex(cx, cy, displayCount, out int idx) && idx == i,
                        $"Narrow strip: center of panel {i} hits index {i}",
                        ref run, ref passed, ref failed);
                }
            }
            finally
            {
                LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);
            }
        }

        private static void TestGetPanelBorderColorBeyondEffectiveMaxIsBlack(ref int run, ref int passed, ref int failed)
        {
            var black = ActionInfoStripLayout.GetPanelBorderColor(2, 2, 0, effectiveMaxComboSlots: 2);
            TestBase.AssertTrue(
                black == AsciiArtAssets.Colors.Black,
                "Panel index at effective max uses black border",
                ref run, ref passed, ref failed);
            var alsoBlack = ActionInfoStripLayout.GetPanelBorderColor(4, 0, -1, effectiveMaxComboSlots: 2);
            TestBase.AssertTrue(
                alsoBlack == AsciiArtAssets.Colors.Black,
                "Empty strip: indices beyond max stay black",
                ref run, ref passed, ref failed);
        }

        private static void TestGetPanelBorderColorWithinMaxUnchanged(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertTrue(
                ActionInfoStripLayout.GetPanelBorderColor(0, 2, 0, effectiveMaxComboSlots: 5) == AsciiArtAssets.Colors.Gold,
                "Selected filled slot: gold",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                ActionInfoStripLayout.GetPanelBorderColor(1, 2, 0, effectiveMaxComboSlots: 5) == AsciiArtAssets.Colors.Cyan,
                "Non-selected filled slot: cyan",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                ActionInfoStripLayout.GetPanelBorderColor(2, 2, 0, effectiveMaxComboSlots: 5) == AsciiArtAssets.Colors.DarkGray,
                "Empty slot within max: dark gray",
                ref run, ref passed, ref failed);
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
