using System;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for <see cref="InventoryRightPanelLayout"/> (pool row Y and tooltip X helpers).
    /// </summary>
    public static class InventoryRightPanelLayoutTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== InventoryRightPanelLayout Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);

            var emptyCombo = TestDataBuilders.Character().WithName("LayoutPool").Build();
            var pa = TestDataBuilders.CreateMockAction("POOL_A");
            var pb = TestDataBuilders.CreateMockAction("POOL_B");
            emptyCombo.AddAction(pa, 1.0);
            emptyCombo.AddAction(pb, 1.0);

            TestBase.AssertTrue(InventoryRightPanelLayout.TryGetActionPoolRowY(emptyCombo, 0, out int y0),
                "pool row 0", ref run, ref passed, ref failed);
            TestBase.AssertEqual(14, y0, "empty combo pool index 0 row", ref run, ref passed, ref failed);
            TestBase.AssertTrue(InventoryRightPanelLayout.TryGetActionPoolRowY(emptyCombo, 1, out int y1),
                "pool row 1", ref run, ref passed, ref failed);
            TestBase.AssertEqual(15, y1, "empty combo pool index 1 row", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!InventoryRightPanelLayout.TryGetActionPoolRowY(emptyCombo, 2, out _),
                "pool index OOB", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!InventoryRightPanelLayout.TryGetActionPoolRowY(emptyCombo, -1, out _),
                "negative index", ref run, ref passed, ref failed);

            var withCombo = TestDataBuilders.Character().WithName("LayoutCombo").Build();
            var comboAction = TestDataBuilders.CreateMockAction("IN_COMBO");
            comboAction.IsComboAction = true;
            withCombo.AddAction(comboAction, 1.0);
            withCombo.AddToCombo(comboAction);
            var px = TestDataBuilders.CreateMockAction("POOL_ONLY");
            withCombo.AddAction(px, 1.0);

            TestBase.AssertTrue(InventoryRightPanelLayout.TryGetActionPoolRowY(withCombo, 0, out int yCombo0),
                "combo pool row 0", ref run, ref passed, ref failed);
            TestBase.AssertEqual(15, yCombo0, "one combo line shifts pool row down", ref run, ref passed, ref failed);

            int anchor = LayoutConstants.RIGHT_PANEL_X;
            int boxW = 52;
            int ideal = InventoryRightPanelLayout.GetPoolTooltipIdealBoxLeft(boxW);
            TestBase.AssertEqual(anchor - boxW / 2, ideal, "straddle ideal left", ref run, ref passed, ref failed);
            int clamped = InventoryRightPanelLayout.ClampPoolTooltipBoxLeft(ideal, boxW);
            TestBase.AssertEqual(ideal, clamped, "clamp unchanged when in grid", ref run, ref passed, ref failed);
            int clampedLeft = InventoryRightPanelLayout.ClampPoolTooltipBoxLeft(-100, boxW);
            TestBase.AssertEqual(1, clampedLeft, "clamp min to 1", ref run, ref passed, ref failed);

            TestBase.PrintSummary("InventoryRightPanelLayout Tests", run, passed, failed);
        }
    }
}
