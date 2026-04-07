using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.Tests.Unit.UI
{
    public static class CanvasInteractionManagerHitTestTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== CanvasInteractionManager HitTest Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            var m = new CanvasInteractionManager();
            m.AddClickableElement(new ClickableElement
            {
                X = 0, Y = 0, Width = 10, Height = 5,
                Type = ElementType.Text,
                Value = "under"
            });
            m.AddClickableElement(new ClickableElement
            {
                X = 2, Y = 2, Width = 3, Height = 1,
                Type = ElementType.Text,
                Value = "over"
            });

            var hit = m.GetElementAt(2, 2);
            TestBase.AssertTrue(hit != null && hit.Value == "over",
                "GetElementAt: last-registered element wins on overlap",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("CanvasInteractionManager HitTest Tests", run, passed, failed);
        }
    }
}
