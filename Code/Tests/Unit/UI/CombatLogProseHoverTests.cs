using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for F7 narrative combat-log prose hover hit map and hover state
    /// (mechanical combat-log info tip on paragraph hover).
    /// </summary>
    public static class CombatLogProseHoverTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatLogProseHover Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            CombatLogProseHoverMap.Clear();
            CombatLogActionHoverState.Clear();

            Console.WriteLine("--- FromMessageGroups builds tip lines ---");
            var groups = new List<(List<ColoredText> segments, UIMessageType messageType)>
            {
                (new List<ColoredText> { new ColoredText("Hero Attacks Goblin and hits with SLAM", Colors.White) }, UIMessageType.Combat),
                (new List<ColoredText> { new ColoredText("(roll: 18 | speed: 1.0)", Colors.Gray) }, UIMessageType.RollInfo)
            };
            var tip = CombatLogProseHoverInfo.FromMessageGroups(groups);
            TestBase.AssertEqual(2, tip.Count, "Tip has headline + roll", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                ColoredTextRenderer.RenderAsPlainText(tip[0]).Contains("SLAM", StringComparison.Ordinal),
                "Tip headline keeps action text",
                ref run, ref passed, ref failed);

            var appended = CombatLogProseHoverInfo.AppendBlocks(tip, tip);
            TestBase.AssertEqual(5, appended.Count, "Append inserts blank spacer between blocks", ref run, ref passed, ref failed);

            Console.WriteLine("--- Map hit returns combat-log info lines ---");
            CombatLogProseHoverMap.BeginFrame();
            CombatLogProseHoverMap.Add(10, 20, 40, 2, tip);
            TestBase.AssertTrue(
                CombatLogProseHoverMap.TryHit(15, 21, out var hitLines, out int hitX, out int y, out int hitW, out int h),
                "Pointer over prose rect hits",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, hitLines.Count, "Hit returns tip lines", ref run, ref passed, ref failed);
            TestBase.AssertEqual(10, hitX, "Hit target X", ref run, ref passed, ref failed);
            TestBase.AssertEqual(40, hitW, "Hit target width", ref run, ref passed, ref failed);
            TestBase.AssertEqual(20, y, "Hit target Y", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, h, "Hit target height", ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !CombatLogProseHoverMap.TryHit(5, 21, out _, out _, out _, out _, out _),
                "Pointer left of rect misses",
                ref run, ref passed, ref failed);

            Console.WriteLine("--- Hover state waits ShowDelayMs before showing ---");
            var t0 = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            CombatLogActionHoverState.UtcNowProviderForTests = () => t0;

            bool entered = CombatLogActionHoverState.UpdateFromPointer(15, 21);
            TestBase.AssertTrue(!entered, "Enter does not show tip immediately", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!CombatLogActionHoverState.IsActive, "Inactive before delay", ref run, ref passed, ref failed);
            TestBase.AssertTrue(CombatLogActionHoverState.IsPending, "Pending after enter", ref run, ref passed, ref failed);

            CombatLogActionHoverState.UtcNowProviderForTests = () => t0.AddMilliseconds(CombatLogActionHoverState.ShowDelayMs - 1);
            bool stillWaiting = CombatLogActionHoverState.UpdateFromPointer(16, 20);
            TestBase.AssertTrue(!stillWaiting, "Still waiting just under delay", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!CombatLogActionHoverState.IsActive, "Still inactive just under delay", ref run, ref passed, ref failed);

            CombatLogActionHoverState.UtcNowProviderForTests = () => t0.AddMilliseconds(CombatLogActionHoverState.ShowDelayMs);
            bool revealed = CombatLogActionHoverState.UpdateFromPointer(16, 20);
            TestBase.AssertTrue(revealed, "Tip reveals after delay", ref run, ref passed, ref failed);
            TestBase.AssertTrue(CombatLogActionHoverState.IsActive, "Hover is active after delay", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!CombatLogActionHoverState.IsPending, "Not pending once shown", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                CombatLogActionHoverState.InfoLines != null && CombatLogActionHoverState.InfoLines.Count == 2,
                "Info lines stored",
                ref run, ref passed, ref failed);

            bool same = CombatLogActionHoverState.UpdateFromPointer(16, 20);
            TestBase.AssertTrue(!same, "Same tip does not change", ref run, ref passed, ref failed);

            Console.WriteLine("--- Click while hovered keeps tip hidden ---");
            bool dismissed = CombatLogActionHoverState.TryDismissFromClick(15, 21);
            TestBase.AssertTrue(dismissed, "Click dismisses visible tip", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!CombatLogActionHoverState.IsActive, "Inactive after dismiss", ref run, ref passed, ref failed);

            CombatLogActionHoverState.UtcNowProviderForTests = () => t0.AddMilliseconds(CombatLogActionHoverState.ShowDelayMs + 10_000);
            bool staysHidden = CombatLogActionHoverState.UpdateFromPointer(15, 21);
            TestBase.AssertTrue(!staysHidden, "Dismissed tip does not reappear on same hover", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!CombatLogActionHoverState.IsActive, "Still inactive while dismissed", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!CombatLogActionHoverState.IsPending, "Not pending while dismissed", ref run, ref passed, ref failed);

            bool leaveAfterDismiss = CombatLogActionHoverState.UpdateFromPointer(0, 0);
            TestBase.AssertTrue(!leaveAfterDismiss, "Leave after dismiss has no visible change", ref run, ref passed, ref failed);

            Console.WriteLine("--- Re-hover after leave waits delay again ---");
            var t1 = t0.AddMinutes(1);
            CombatLogActionHoverState.UtcNowProviderForTests = () => t1;
            bool reenter = CombatLogActionHoverState.UpdateFromPointer(15, 21);
            TestBase.AssertTrue(!reenter, "Re-enter starts pending again", ref run, ref passed, ref failed);
            TestBase.AssertTrue(CombatLogActionHoverState.IsPending, "Pending on re-enter", ref run, ref passed, ref failed);

            CombatLogActionHoverState.UtcNowProviderForTests = () => t1.AddMilliseconds(CombatLogActionHoverState.ShowDelayMs);
            bool rereveal = CombatLogActionHoverState.UpdateFromPointer(15, 21);
            TestBase.AssertTrue(rereveal, "Re-hover reveals after delay", ref run, ref passed, ref failed);
            TestBase.AssertTrue(CombatLogActionHoverState.IsActive, "Active after re-hover delay", ref run, ref passed, ref failed);

            Console.WriteLine("--- Click during pending prevents reveal ---");
            CombatLogActionHoverState.Clear();
            CombatLogProseHoverMap.BeginFrame();
            CombatLogProseHoverMap.Add(10, 20, 40, 2, tip);
            var t2 = t0.AddMinutes(2);
            CombatLogActionHoverState.UtcNowProviderForTests = () => t2;
            CombatLogActionHoverState.UpdateFromPointer(15, 21);
            TestBase.AssertTrue(CombatLogActionHoverState.IsPending, "Pending before early click", ref run, ref passed, ref failed);
            bool earlyDismiss = CombatLogActionHoverState.TryDismissFromClick(15, 21);
            TestBase.AssertTrue(!earlyDismiss, "Early click has no visible tip to clear", ref run, ref passed, ref failed);
            CombatLogActionHoverState.UtcNowProviderForTests = () => t2.AddMilliseconds(CombatLogActionHoverState.ShowDelayMs + 1);
            bool blocked = CombatLogActionHoverState.UpdateFromPointer(15, 21);
            TestBase.AssertTrue(!blocked, "Early dismiss blocks delayed reveal", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!CombatLogActionHoverState.IsActive, "Never became active after early dismiss", ref run, ref passed, ref failed);

            bool cleared = CombatLogActionHoverState.UpdateFromPointer(0, 0);
            TestBase.AssertTrue(!cleared, "Leave while inactive", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!CombatLogActionHoverState.IsActive, "Inactive after leave", ref run, ref passed, ref failed);

            CombatLogProseHoverMap.Clear();
            CombatLogActionHoverState.Clear();
            TestBase.PrintSummary("CombatLogProseHover Tests", run, passed, failed);
        }
    }
}
