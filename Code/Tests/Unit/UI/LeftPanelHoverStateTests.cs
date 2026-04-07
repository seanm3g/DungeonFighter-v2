using System.Collections.Generic;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.Avalonia;

namespace RPGGame.Tests.Unit.UI
{
    public static class LeftPanelHoverStateTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== LeftPanelHoverState Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            LeftPanelHoverState.Clear();
            TestBase.AssertEqual("", LeftPanelHoverState.Value, "Clear leaves empty", ref run, ref passed, ref failed);

            var lp1 = new ClickableElement
            {
                X = 0, Y = 0, Width = 10, Height = 1,
                Type = ElementType.Text,
                Value = LeftPanelHoverState.Prefix + "stat:damage",
                IsHovered = true
            };
            TestBase.AssertTrue(LeftPanelHoverState.UpdateFromClickables(new List<ClickableElement> { lp1 }),
                "First lphover registers",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(LeftPanelHoverState.Prefix + "stat:damage", LeftPanelHoverState.Value, "Value is full lphover id", ref run, ref passed, ref failed);

            var plain = new ClickableElement
            {
                X = 0, Y = 0, Width = 5, Height = 1,
                Type = ElementType.Text,
                Value = "non_lphover_control",
                IsHovered = true
            };
            TestBase.AssertTrue(LeftPanelHoverState.UpdateFromClickables(new List<ClickableElement> { plain }),
                "Update clears when hovered element is not lphover",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual("", LeftPanelHoverState.Value, "Non-lphover hovered clears state", ref run, ref passed, ref failed);

            TestBase.AssertTrue(LeftPanelHoverState.UpdateFromClickables(new List<ClickableElement> { lp1 }),
                "Re-activate lphover after clear",
                ref run, ref passed, ref failed);

            var lp2 = new ClickableElement
            {
                X = 0, Y = 0, Width = 10, Height = 1,
                Type = ElementType.Text,
                Value = LeftPanelHoverState.Prefix + "stat:armor",
                IsHovered = true
            };
            // Later element in list wins (topmost registration)
            TestBase.AssertTrue(LeftPanelHoverState.UpdateFromClickables(new List<ClickableElement> { lp1, lp2 }),
                "Change when second lphover is last in list",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(LeftPanelHoverState.Prefix + "stat:armor", LeftPanelHoverState.Value, "Last-listed lphover wins", ref run, ref passed, ref failed);

            lp1.IsHovered = true;
            lp2.IsHovered = true;
            TestBase.AssertTrue(LeftPanelHoverState.UpdateFromClickables(new List<ClickableElement> { lp2, lp1 }),
                "Change when first in list should win scan-from-end",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(LeftPanelHoverState.Prefix + "stat:damage", LeftPanelHoverState.Value, "Earlier in list = lower index = loses; end of list wins", ref run, ref passed, ref failed);

            var tooltipOnly = new ClickableElement
            {
                X = 0, Y = 0, Width = 10, Height = 2,
                Type = ElementType.Button,
                Value = "1",
                TooltipHoverValue = LeftPanelHoverState.Prefix + "inv:0",
                IsHovered = true
            };
            TestBase.AssertTrue(LeftPanelHoverState.UpdateFromClickables(new List<ClickableElement> { tooltipOnly }),
                "TooltipHoverValue used when set",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(LeftPanelHoverState.Prefix + "inv:0", LeftPanelHoverState.Value, "inv list id from TooltipHoverValue", ref run, ref passed, ref failed);

            TestBase.PrintSummary("LeftPanelHoverState Tests", run, passed, failed);
        }
    }
}
