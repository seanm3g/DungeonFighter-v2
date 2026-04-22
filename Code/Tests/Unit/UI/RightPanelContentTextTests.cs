using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for <see cref="RightPanelContentText"/> (right-panel LOCATION / enemy name line width).
    /// </summary>
    public static class RightPanelContentTextTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            LayoutConstants.UpdateGridDimensions(210, 52);
            LayoutConstants.UpdateEffectiveVisibleWidth(2100, 10);

            const string room = "Tomb of the forgotten";
            TestBase.AssertEqual(21, room.Length, "fixture room name length", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                RightPanelContentText.MaxCharsPerLine >= room.Length,
                "inner panel max chars fits Tomb of the forgotten",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(
                room,
                RightPanelContentText.EllipsizeToPanelWidth(room),
                "room name not ellipsized when it fits",
                ref run, ref passed, ref failed);

            var longName = new string('x', 50);
            var truncated = RightPanelContentText.EllipsizeToPanelWidth(longName);
            TestBase.AssertEqual(
                RightPanelContentText.MaxCharsPerLine,
                truncated.Length,
                "truncated name length equals max",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                truncated.EndsWith("...", StringComparison.Ordinal),
                "truncated name ends with ellipsis",
                ref run, ref passed, ref failed);

            TestBase.AssertEqual(
                "",
                RightPanelContentText.EllipsizeToPanelWidth(""),
                "empty string unchanged",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("RightPanelContentTextTests", run, passed, failed);
        }
    }
}
