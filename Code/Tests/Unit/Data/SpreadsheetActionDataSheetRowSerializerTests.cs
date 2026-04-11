using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Data;

namespace RPGGame.Tests.Unit.Data
{
    public static class SpreadsheetActionDataSheetRowSerializerTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== SpreadsheetActionDataSheetRowSerializer Tests ===\n");

            int testsRun = 0, testsPassed = 0, testsFailed = 0;

            TestHeaderTwoRows_MapsStunAndAction(ref testsRun, ref testsPassed, ref testsFailed);
            TestSingleHeaderRow_CoreColumns(ref testsRun, ref testsPassed, ref testsFailed);
            TestEmptyOptionalFields(ref testsRun, ref testsPassed, ref testsFailed);
            TestBuildHeaderFromSheetRows_FirstDataRow(ref testsRun, ref testsPassed, ref testsFailed);

            TestBase.PrintSummary("SpreadsheetActionDataSheetRowSerializer Tests", testsRun, testsPassed, testsFailed);
        }

        private static void TestHeaderTwoRows_MapsStunAndAction(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestHeaderTwoRows_MapsStunAndAction));
            var contextRow = new[] { "", "", "ENEMY TARGET", "" };
            var labelRow = new[] { "ACTION", "DESCRIPTION", "STUN", "TARGET" };
            var (header, firstData) = SpreadsheetActionParser.BuildHeaderFromSheetRows(new List<string[]> { contextRow, labelRow });

            TestBase.AssertTrue(header != null, "header parsed", ref testsRun, ref testsPassed, ref testsFailed);
            if (header == null) return;

            var data = new SpreadsheetActionData
            {
                Action = "STAB",
                Description = "test",
                Stun = "2",
                Target = "ENEMY"
            };
            var row = SpreadsheetActionDataSheetRowSerializer.ToRow(data, header);

            TestBase.AssertEqual(4, row.Length, "row width matches header", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("STAB", row[0], "ACTION column", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("test", row[1], "DESCRIPTION column", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("2", row[2], "STUN under ENEMY TARGET", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("ENEMY", row[3], "TARGET column", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(3, firstData, "first data row is 3 when two header rows", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestSingleHeaderRow_CoreColumns(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestSingleHeaderRow_CoreColumns));
            var labelRow = new[] { "ACTION", "DESCRIPTION", "RARITY", "DPS(%)" };
            var (header, firstData) = SpreadsheetActionParser.BuildHeaderFromSheetRows(new List<string[]> { labelRow });

            TestBase.AssertTrue(header != null, "single-row header parsed", ref testsRun, ref testsPassed, ref testsFailed);
            if (header == null) return;

            var data = new SpreadsheetActionData
            {
                Action = "JAB",
                Description = "d",
                Rarity = "C",
                DPS = "50%"
            };
            var row = SpreadsheetActionDataSheetRowSerializer.ToRow(data, header);

            TestBase.AssertEqual("JAB", row[0], "ACTION", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("d", row[1], "DESCRIPTION", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("C", row[2], "RARITY", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("50%", row[3], "DPS(%)", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(2, firstData, "first data row is 2 for single header row", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestEmptyOptionalFields(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestEmptyOptionalFields));
            var labelRow = new[] { "ACTION", "DESCRIPTION", "STUN" };
            var (header, _) = SpreadsheetActionParser.BuildHeaderFromSheetRows(new List<string[]> { labelRow });
            if (header == null) return;

            var data = new SpreadsheetActionData { Action = "ONLY", Description = "", Stun = "" };
            var row = SpreadsheetActionDataSheetRowSerializer.ToRow(data, header);

            TestBase.AssertEqual("", row[1], "empty description", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("", row[2], "empty stun with label-only STUN column", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestBuildHeaderFromSheetRows_FirstDataRow(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestBuildHeaderFromSheetRows_FirstDataRow));
            var rows = new List<string[]>();
            TestBase.AssertTrue(SpreadsheetActionParser.BuildHeaderFromSheetRows(rows).Header == null, "empty rows -> no header", ref testsRun, ref testsPassed, ref testsFailed);
        }
    }
}
