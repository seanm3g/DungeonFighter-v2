using System;
using RPGGame.Tests;
using RPGGame.UI;
using Avalonia.Input;

namespace RPGGame.Tests.Unit.UI
{
    public static class HoverTooltipDetailStateTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== HoverTooltipDetailState Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            HoverTooltipDetailState.Clear();
            TestBase.AssertTrue(!HoverTooltipDetailState.IsAltDetailActive,
                "starts cleared",
                ref run, ref passed, ref failed);

            bool changed = HoverTooltipDetailState.SetFromModifiers(KeyModifiers.Alt);
            TestBase.AssertTrue(changed && HoverTooltipDetailState.IsAltDetailActive,
                "Alt modifier activates detail mode",
                ref run, ref passed, ref failed);

            changed = HoverTooltipDetailState.SetFromModifiers(KeyModifiers.Alt);
            TestBase.AssertTrue(!changed && HoverTooltipDetailState.IsAltDetailActive,
                "same Alt state is idempotent",
                ref run, ref passed, ref failed);

            changed = HoverTooltipDetailState.SetFromModifiers(KeyModifiers.None);
            TestBase.AssertTrue(changed && !HoverTooltipDetailState.IsAltDetailActive,
                "releasing Alt clears detail mode",
                ref run, ref passed, ref failed);

            changed = HoverTooltipDetailState.SetFromAltKey(Key.LeftAlt, isDown: true);
            TestBase.AssertTrue(changed && HoverTooltipDetailState.IsAltDetailActive,
                "LeftAlt key down activates",
                ref run, ref passed, ref failed);
            changed = HoverTooltipDetailState.SetFromAltKey(Key.A, isDown: true);
            TestBase.AssertTrue(!changed && HoverTooltipDetailState.IsAltDetailActive,
                "non-Alt key ignored",
                ref run, ref passed, ref failed);
            changed = HoverTooltipDetailState.SetFromAltKey(Key.LeftAlt, isDown: false);
            TestBase.AssertTrue(changed && !HoverTooltipDetailState.IsAltDetailActive,
                "LeftAlt key up clears",
                ref run, ref passed, ref failed);

            HoverTooltipDetailState.SetFromModifiers(KeyModifiers.Alt);
            HoverTooltipDetailState.Clear();
            TestBase.AssertTrue(!HoverTooltipDetailState.IsAltDetailActive,
                "Clear resets",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("HoverTooltipDetailState Tests", run, passed, failed);
        }
    }
}
