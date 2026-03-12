using System.Collections.Generic;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Tests for CombatActionInfoState (action-info strip below center panel during combat).
    /// </summary>
    public static class CombatActionInfoStateTests
    {
        /// <summary>
        /// Runs all CombatActionInfoState tests.
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatActionInfoState Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            CombatActionInfoState.Clear();
            var empty = CombatActionInfoState.GetLines();
            TestBase.AssertTrue(empty != null && empty.Count == 0,
                "GetLines after Clear returns empty",
                ref run, ref passed, ref failed);

            CombatActionInfoState.SetSummary(new List<string> { "Line 1", "Line 2" });
            var lines = CombatActionInfoState.GetLines();
            TestBase.AssertTrue(lines != null && lines.Count == 2 && lines[0] == "Line 1" && lines[1] == "Line 2",
                "SetSummary then GetLines returns same lines",
                ref run, ref passed, ref failed);

            CombatActionInfoState.Clear();
            var afterClear = CombatActionInfoState.GetLines();
            TestBase.AssertTrue(afterClear != null && afterClear.Count == 0,
                "Clear clears state",
                ref run, ref passed, ref failed);

            CombatActionInfoState.SetSummary(null);
            TestBase.AssertTrue(CombatActionInfoState.GetLines().Count == 0,
                "SetSummary(null) yields empty",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("CombatActionInfoState Tests", run, passed, failed);
        }
    }
}
