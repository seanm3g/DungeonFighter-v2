using System.Collections.Generic;
using RPGGame.Handlers.Inventory;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.Avalonia;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for <see cref="RightPanelActionHoverState"/> (inventory right-panel tooltip targets).
    /// </summary>
    public static class RightPanelActionHoverStateTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== RightPanelActionHoverState Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            var poolBtn = new ClickableElement
            {
                X = 0,
                Y = 0,
                Width = 10,
                Height = 1,
                Type = ElementType.Button,
                Value = $"{ComboPointerInput.Prefix}pool:2",
                IsHovered = true
            };
            var seqBtn = new ClickableElement
            {
                X = 0,
                Y = 1,
                Width = 10,
                Height = 1,
                Type = ElementType.Button,
                Value = $"{ComboPointerInput.Prefix}rm:0",
                IsHovered = true
            };

            TestBase.AssertTrue(RightPanelActionHoverState.UpdateFromClickables(new List<ClickableElement> { poolBtn }, true),
                "Warm pool hover", ref run, ref passed, ref failed);
            TestBase.AssertTrue(RightPanelActionHoverState.UpdateFromClickables(new List<ClickableElement>(), false),
                "Disable inventory clears after hover", ref run, ref passed, ref failed);
            TestBase.AssertEqual(-1, RightPanelActionHoverState.HoveredSequenceIndex, "Sequence cleared", ref run, ref passed, ref failed);
            TestBase.AssertEqual(-1, RightPanelActionHoverState.HoveredPoolIndex, "Pool cleared", ref run, ref passed, ref failed);

            TestBase.AssertTrue(RightPanelActionHoverState.UpdateFromClickables(new List<ClickableElement> { poolBtn }, true),
                "Pool hover applies", ref run, ref passed, ref failed);
            TestBase.AssertEqual(-1, RightPanelActionHoverState.HoveredSequenceIndex, "No sequence", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, RightPanelActionHoverState.HoveredPoolIndex, "Pool index 2", ref run, ref passed, ref failed);

            TestBase.AssertTrue(RightPanelActionHoverState.UpdateFromClickables(new List<ClickableElement> { seqBtn }, true),
                "Sequence hover applies", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, RightPanelActionHoverState.HoveredSequenceIndex, "Sequence index 0", ref run, ref passed, ref failed);
            TestBase.AssertEqual(-1, RightPanelActionHoverState.HoveredPoolIndex, "Pool cleared for seq row", ref run, ref passed, ref failed);

            TestBase.AssertTrue(!RightPanelActionHoverState.UpdateFromClickables(new List<ClickableElement> { seqBtn }, true),
                "Same hover is idempotent", ref run, ref passed, ref failed);

            TestBase.PrintSummary("RightPanelActionHoverState Tests", run, passed, failed);
        }
    }
}
