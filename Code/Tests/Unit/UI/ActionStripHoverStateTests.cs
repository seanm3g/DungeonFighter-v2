using System;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for <see cref="ActionStripHoverState"/> (action strip tooltip hover index).
    /// </summary>
    public static class ActionStripHoverStateTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionStripHoverState Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            ActionStripHoverState.Clear();
            TestBase.AssertEqual(-1, ActionStripHoverState.HoveredPanelIndex, "Clear leaves index -1", ref run, ref passed, ref failed);

            bool first = ActionStripHoverState.SetHoveredPanelIndex(2);
            TestBase.AssertTrue(first, "First Set returns true", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, ActionStripHoverState.HoveredPanelIndex, "Index is stored", ref run, ref passed, ref failed);

            bool second = ActionStripHoverState.SetHoveredPanelIndex(2);
            TestBase.AssertTrue(!second, "Same index returns false", ref run, ref passed, ref failed);

            bool third = ActionStripHoverState.SetHoveredPanelIndex(0);
            TestBase.AssertTrue(third, "Change returns true", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, ActionStripHoverState.HoveredPanelIndex, "Index updated", ref run, ref passed, ref failed);

            ActionStripHoverState.Clear();
            TestBase.AssertEqual(-1, ActionStripHoverState.HoveredPanelIndex, "Clear after set", ref run, ref passed, ref failed);

            TestBase.PrintSummary("ActionStripHoverState Tests", run, passed, failed);
        }
    }
}
