using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Renderers.Helpers;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>Tests for <see cref="ItemComparisonTable"/> (inventory "vs Equipped" table).</summary>
    public static class ItemComparisonTableTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ItemComparisonTable Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestSameStatSummedAcrossSources();
            TestStatOnlyOnOneSide();
            TestEqualStatShowsEquals();
            TestAttackTimeLowerIsBetter();
            TestActionDiff();
            TestQualityRowOmitted();

            TestBase.PrintSummary("ItemComparisonTable Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static ItemComparisonRow Row(List<ItemComparisonRow> rows, string label) =>
            rows.First(r => r.Label == label);

        private static void Check(bool ok, string message) =>
            TestBase.AssertTrue(ok, message, ref _testsRun, ref _testsPassed, ref _testsFailed);

        private static void TestSameStatSummedAcrossSources()
        {
            var selected = new List<ItemStatContribution>
            {
                new("Agility", "+1", "Item base"),
                new("Agility", "+4", "Mithril"),
            };
            var worn = new List<ItemStatContribution> { new("Agility", "+2", "Bone") };
            var row = Row(ItemComparisonTable.Build(selected, worn), "Agility");
            Check(row.NewText == "+5" && row.WornText == "+2" && row.ChangeText == "+3" && row.Verdict == ItemComparisonVerdict.Better,
                $"Agility summed: got {row.NewText}/{row.WornText}/{row.ChangeText}");
        }

        private static void TestStatOnlyOnOneSide()
        {
            var selected = new List<ItemStatContribution> { new("Crit threshold", "+1", "Long") };
            var worn = new List<ItemStatContribution> { new("Strength", "+2", "Bone") };
            var rows = ItemComparisonTable.Build(selected, worn);
            var str = Row(rows, "Strength");
            var crit = Row(rows, "Crit threshold");
            Check(str.NewText == ItemComparisonTable.Missing && str.ChangeText == "-2" && str.Verdict == ItemComparisonVerdict.Worse,
                $"Strength lost: got {str.NewText}/{str.ChangeText}");
            Check(crit.WornText == ItemComparisonTable.Missing && crit.ChangeText == "+1" && crit.Verdict == ItemComparisonVerdict.Better,
                $"Crit gained: got {crit.WornText}/{crit.ChangeText}");
            Check(rows[0].Label == "Crit threshold", "selected item's stats listed first");
        }

        private static void TestEqualStatShowsEquals()
        {
            var a = new List<ItemStatContribution> { new("Defense", "+4") };
            var b = new List<ItemStatContribution> { new("Defense", "+4") };
            var row = Row(ItemComparisonTable.Build(a, b), "Defense");
            Check(row.ChangeText == "=" && row.Verdict == ItemComparisonVerdict.Same, $"Defense equal: got {row.ChangeText}");
        }

        private static void TestAttackTimeLowerIsBetter()
        {
            var a = new List<ItemStatContribution> { new("Attack time", "1.20s") };
            var b = new List<ItemStatContribution> { new("Attack time", "1.50s") };
            var row = Row(ItemComparisonTable.Build(a, b), "Attack time");
            Check(row.NewText == "1.2s" && row.ChangeText == "-0.3s" && row.Verdict == ItemComparisonVerdict.Better,
                $"Attack time: got {row.NewText}/{row.ChangeText}/{row.Verdict}");
        }

        private static void TestActionDiff()
        {
            var (gained, lost) = ItemComparisonTable.DiffActions(
                new[] { "REPLACE ROLL", "JAB" },
                new[] { "RETRIGGER NEXT", "AFTER MISS HEAL", "jab" });
            Check(gained.SequenceEqual(new[] { "REPLACE ROLL" }) && lost.SequenceEqual(new[] { "RETRIGGER NEXT", "AFTER MISS HEAL" }),
                $"Action diff: +[{string.Join(",", gained)}] -[{string.Join(",", lost)}]");
        }

        private static void TestQualityRowOmitted()
        {
            var a = new List<ItemStatContribution> { new("Damage", "7") };
            var b = new List<ItemStatContribution> { new("Damage", "10"), new("Quality", "−5% armor, damage, speed", null, isDebuff: true) };
            var rows = ItemComparisonTable.Build(a, b);
            Check(rows.Count == 1 && rows[0].Label == "Damage", $"Quality omitted: got [{string.Join(",", rows.Select(r => r.Label))}]");
        }
    }
}
