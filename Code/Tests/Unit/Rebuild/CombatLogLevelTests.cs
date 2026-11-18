using System;
using System.Linq;
using RPGGame.Combat.Formatting;

namespace RPGGame.Tests.Unit.Rebuild
{
    /// <summary>Rebuild 1.7 (D0.5): the simple log (default) shows the roll and its nudge; the detailed log the full math.</summary>
    public static class CombatLogLevelTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatLogLevel Tests ===\n");
            _run = _passed = _failed = 0;
            bool old = GameSettings.Instance.DetailedCombatLog;
            try
            {
                TestBase.AssertFalse(new GameSettings().DetailedCombatLog, "The simple log is the default", ref _run, ref _passed, ref _failed);

                GameSettings.Instance.DetailedCombatLog = false;
                string simple = Text(RollInfoFormatter.FormatRollInfoColored(16, -2, 40, 6, 1.3, 1.5));
                TestBase.AssertTrue(simple.Contains("rolled 16") && simple.Contains("= 14"), $"Simple shows the roll and nudge: {simple.Trim()}", ref _run, ref _passed, ref _failed);
                TestBase.AssertFalse(simple.Contains("amp", StringComparison.OrdinalIgnoreCase) || simple.Contains("speed", StringComparison.OrdinalIgnoreCase),
                    "Simple hides AMP and speed", ref _run, ref _passed, ref _failed);

                GameSettings.Instance.DetailedCombatLog = true;
                string detailed = Text(RollInfoFormatter.FormatRollInfoColored(16, -2, 40, 6, 1.3, 1.5));
                TestBase.AssertTrue(detailed.Length > simple.Length && detailed.Contains("roll:"), $"Detailed shows the full math: {detailed.Trim()}", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                GameSettings.Instance.DetailedCombatLog = old;
            }
            TestBase.PrintSummary("CombatLogLevel Tests", _run, _passed, _failed);
        }

        private static string Text(System.Collections.Generic.List<RPGGame.UI.ColorSystem.ColoredText> segs) => string.Concat(segs.Select(s => s.Text));
    }
}
