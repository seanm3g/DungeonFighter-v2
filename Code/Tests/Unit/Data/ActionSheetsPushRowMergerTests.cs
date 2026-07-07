using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class ActionSheetsPushRowMergerTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionSheetsPushRowMerger Tests ===\n");

            int testsRun = 0, testsPassed = 0, testsFailed = 0;

            TestPreservesSheetOrderWithTierHeaders(ref testsRun, ref testsPassed, ref testsFailed);
            TestPreservesLayerHeaders(ref testsRun, ref testsPassed, ref testsFailed);
            TestAppendsMissingActionsAfterSheetRows(ref testsRun, ref testsPassed, ref testsFailed);
            TestEmptySheetFallsBackToJsonOrder(ref testsRun, ref testsPassed, ref testsFailed);

            TestBase.PrintSummary("ActionSheetsPushRowMerger Tests", testsRun, testsPassed, testsFailed);
        }

        private static SpreadsheetHeader BuildTestHeader()
        {
            var labelRow = new[] { "ACTION", "DESCRIPTION", "RARITY" };
            var (header, _) = SpreadsheetActionParser.BuildHeaderFromSheetRows(new List<string[]> { labelRow });
            if (header == null)
                throw new InvalidOperationException("test header failed to parse");
            return header;
        }

        private static SpreadsheetActionJson MakeAction(string name, string description = "")
        {
            return SpreadsheetActionJson.FromSpreadsheetActionData(new SpreadsheetActionData
            {
                Action = name,
                Description = string.IsNullOrEmpty(description) ? name.ToLowerInvariant() : description
            });
        }

        private static void TestPreservesSheetOrderWithTierHeaders(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestPreservesSheetOrderWithTierHeaders));
            var header = BuildTestHeader();
            var existing = new List<string[]>
            {
                new[] { "JAB", "old jab", "common" },
                new[] { "TIER 2 ACTIONS", "", "" },
                new[] { "TAUNT", "old taunt", "common" }
            };
            var json = new List<SpreadsheetActionJson>
            {
                MakeAction("TAUNT", "new taunt"),
                MakeAction("JAB", "new jab")
            };

            var merge = ActionSheetsPushRowMerger.BuildBodyRowsPreservingSheetOrder(existing, json, header);

            TestBase.AssertEqual(3, merge.BodyRows.Count, "three output rows", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("JAB", merge.BodyRows[0][0]?.ToString(), "first row stays JAB", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("new jab", merge.BodyRows[0][1]?.ToString(), "JAB updated from JSON", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("TIER 2 ACTIONS", merge.BodyRows[1][0]?.ToString(), "tier header preserved", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("", merge.BodyRows[1][1]?.ToString(), "tier header description untouched", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("TAUNT", merge.BodyRows[2][0]?.ToString(), "TAUNT stays after tier header", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("new taunt", merge.BodyRows[2][1]?.ToString(), "TAUNT updated from JSON", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(1, merge.PreservedSectionRowCount, "one preserved section row", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(2, merge.UpdatedActionCount, "two updated actions", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(0, merge.AppendedActionCount, "no appended actions", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestPreservesLayerHeaders(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestPreservesLayerHeaders));
            var header = BuildTestHeader();
            var existing = new List<string[]>
            {
                new[] { "JAB", "jab", "common" },
                new[] { "LAYER 2 ACTIONS", "", "" },
                new[] { "SLASH", "slash", "common" }
            };
            var json = new List<SpreadsheetActionJson> { MakeAction("JAB"), MakeAction("SLASH", "updated slash") };

            var merge = ActionSheetsPushRowMerger.BuildBodyRowsPreservingSheetOrder(existing, json, header);

            TestBase.AssertEqual("LAYER 2 ACTIONS", merge.BodyRows[1][0]?.ToString(), "layer header preserved", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("updated slash", merge.BodyRows[2][1]?.ToString(), "slash updated after layer header", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestAppendsMissingActionsAfterSheetRows(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestAppendsMissingActionsAfterSheetRows));
            var header = BuildTestHeader();
            var existing = new List<string[]> { new[] { "JAB", "jab", "common" } };
            var json = new List<SpreadsheetActionJson> { MakeAction("JAB"), MakeAction("NEW_ACTION", "brand new") };

            var merge = ActionSheetsPushRowMerger.BuildBodyRowsPreservingSheetOrder(existing, json, header);

            TestBase.AssertEqual(2, merge.BodyRows.Count, "appended row added", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("NEW_ACTION", merge.BodyRows[1][0]?.ToString(), "new action appended", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(1, merge.AppendedActionCount, "one appended action", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestEmptySheetFallsBackToJsonOrder(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestEmptySheetFallsBackToJsonOrder));
            var header = BuildTestHeader();
            var json = new List<SpreadsheetActionJson> { MakeAction("B"), MakeAction("A") };

            var merge = ActionSheetsPushRowMerger.BuildBodyRowsPreservingSheetOrder(Array.Empty<string[]>(), json, header);

            TestBase.AssertEqual(2, merge.BodyRows.Count, "both actions written", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                Enumerable.SequenceEqual(new[] { "B", "A" }, merge.BodyRows.Select(r => r[0]?.ToString())),
                "JSON order used when sheet has no rows",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(2, merge.AppendedActionCount, "both counted as appended", ref testsRun, ref testsPassed, ref testsFailed);
        }
    }
}
