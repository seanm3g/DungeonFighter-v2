using Avalonia.Media;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Canvas;

namespace RPGGame.Tests.Unit.UI
{
    public static class CanvasElementManagerSnapshotTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            var m = new CanvasElementManager();
            m.AddText(new CanvasText { X = 1, Y = 0, Content = "Hi", Color = Colors.White });
            m.AddText(new CanvasText { X = 3, Y = 0, Content = "!", Color = Colors.White });
            string snap = m.BuildPlainTextSnapshotInRect(0, 0, 8, 2, excludeOverlay: true);
            string[] lines = snap.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            TestBase.AssertTrue(lines.Length >= 1, "snapshot has at least one line", ref run, ref passed, ref failed);
            TestBase.AssertTrue(lines[0].TrimEnd().Contains("Hi") && lines[0].Contains("!"), "snapshot row contains adjacent segments", ref run, ref passed, ref failed);

            m.AddText(new CanvasText { X = 1, Y = 1, Content = "OV", Color = Colors.Red, IsOverlay = true });
            string noOverlay = m.BuildPlainTextSnapshotInRect(0, 0, 8, 2, excludeOverlay: true);
            TestBase.AssertTrue(!noOverlay.Contains("OV"), "excludeOverlay omits overlay text", ref run, ref passed, ref failed);
            string withOverlay = m.BuildPlainTextSnapshotInRect(0, 0, 8, 2, excludeOverlay: false);
            TestBase.AssertTrue(withOverlay.Contains("OV"), "overlay included when requested", ref run, ref passed, ref failed);

            TestBase.PrintSummary("CanvasElementManagerSnapshotTests", run, passed, failed);
        }
    }
}
