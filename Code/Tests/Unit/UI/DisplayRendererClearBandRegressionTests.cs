using RPGGame.Combat.Sequence;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Regression: display buffer clear band must not start inside the action-info strip rows
    /// (must match <see cref="RPGGame.UI.Avalonia.Display.DisplayRenderer.Render"/> clear logic),
    /// and combat-log paints must clip soft-wrapped rows inside the framed content height.
    /// </summary>
    public static class DisplayRendererClearBandRegressionTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);

            int firstRowBelowStrip = LayoutConstants.ACTION_INFO_Y + LayoutConstants.ACTION_INFO_HEIGHT;
            int persistentInnerContentY = LayoutConstants.CENTER_PANEL_Y + 1;

            TestBase.AssertEqual(
                firstRowBelowStrip,
                ComputeClearStartY(persistentInnerContentY),
                "persistent center content clear starts at first row below action strip",
                ref run, ref passed, ref failed);

            TestBase.AssertEqual(
                0,
                ComputeClearStartY(0),
                "chromeless content at y=0 clear starts at row 0",
                ref run, ref passed, ref failed);

            TestBase.AssertEqual(
                LayoutConstants.CENTER_PANEL_Y,
                firstRowBelowStrip,
                "first row below strip aligns with framed center panel top",
                ref run, ref passed, ref failed);

            CombatSequenceHudState.IsBandReserved = true;
            try
            {
                int hudY = LayoutConstants.CombatSequenceHudY;
                int logContentY = LayoutConstants.CombatLogContentY;
                TestBase.AssertTrue(
                    ComputeClearStartY(logContentY) >= LayoutConstants.CENTER_PANEL_Y,
                    "combat log clear stays at or below the log frame when the sequence panel is reserved",
                    ref run, ref passed, ref failed);
                TestBase.AssertTrue(
                    ComputeClearStartY(logContentY) > hudY,
                    "combat log clear does not erase the sequence HUD panel",
                    ref run, ref passed, ref failed);

                int contentH = LayoutConstants.CombatLogContentHeight;
                int clearEnd = DisplayRenderer.ComputeClearEndY(logContentY, contentH);
                int framedBottomExclusive = LayoutConstants.CENTER_PANEL_Y + LayoutConstants.CENTER_PANEL_HEIGHT;
                TestBase.AssertTrue(
                    clearEnd > logContentY + contentH,
                    "framed log clear extends past inner content into the border/pad band",
                    ref run, ref passed, ref failed);
                TestBase.AssertEqual(
                    framedBottomExclusive + CanvasGridSizer.OuterPaddingBottom,
                    clearEnd,
                    "framed log clear reaches the outer bottom pad under the cyan border",
                    ref run, ref passed, ref failed);

                int viewportBottom = logContentY + contentH;
                TestBase.AssertEqual(
                    2,
                    DisplayRenderer.CountLinesFittingInViewport(viewportBottom - 2, wrappedLineCount: 5, viewportBottom),
                    "soft-wrapped overflow clips to remaining viewport rows (no paint below border)",
                    ref run, ref passed, ref failed);
                TestBase.AssertEqual(
                    0,
                    DisplayRenderer.CountLinesFittingInViewport(viewportBottom, wrappedLineCount: 3, viewportBottom),
                    "rows at the exclusive viewport bottom are not painted",
                    ref run, ref passed, ref failed);
            }
            finally
            {
                CombatSequenceHudState.IsBandReserved = false;
            }

            TestBase.PrintSummary("DisplayRendererClearBandRegressionTests", run, passed, failed);
        }

        /// <summary>Mirrors DisplayRenderer scroll-area clear start calculation.</summary>
        private static int ComputeClearStartY(int contentY)
        {
            int scrollOverflowPad = System.Math.Max(0, contentY - 2);
            int framedLogTop = LayoutConstants.CENTER_PANEL_Y;
            return contentY >= framedLogTop
                ? System.Math.Max(scrollOverflowPad, framedLogTop)
                : scrollOverflowPad;
        }
    }
}
