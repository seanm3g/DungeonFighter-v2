using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class SheetsPushUtilitiesTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== SheetsPushUtilities Tests ===\n");

            int testsRun = 0, testsPassed = 0, testsFailed = 0;

            TestColumnIndexToA1Letters_Core(ref testsRun, ref testsPassed, ref testsFailed);
            TestNormalizeCellValueForUpload(ref testsRun, ref testsPassed, ref testsFailed);
            TestSplitActionPushRowPreservingColumnsEF(ref testsRun, ref testsPassed, ref testsFailed);

            TestBase.PrintSummary("SheetsPushUtilities Tests", testsRun, testsPassed, testsFailed);
        }

        private static void TestColumnIndexToA1Letters_Core(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestColumnIndexToA1Letters_Core));
            TestBase.AssertEqual("A", SheetsPushUtilities.ColumnIndexToA1Letters(0), "index 0 → A", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("E", SheetsPushUtilities.ColumnIndexToA1Letters(4), "index 4 → E", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("Z", SheetsPushUtilities.ColumnIndexToA1Letters(25), "index 25 → Z", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("AA", SheetsPushUtilities.ColumnIndexToA1Letters(26), "index 26 → AA", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestNormalizeCellValueForUpload(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestNormalizeCellValueForUpload));
            TestBase.AssertEqual("", (string)(object)SheetsPushUtilities.NormalizeCellValueForUpload(null), "null → empty", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("", (string)(object)SheetsPushUtilities.NormalizeCellValueForUpload(""), "empty string", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("", (string)(object)SheetsPushUtilities.NormalizeCellValueForUpload(" "), "single space → empty", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("", (string)(object)SheetsPushUtilities.NormalizeCellValueForUpload("  \t "), "whitespace → empty", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("", SheetsPushUtilities.NormalizeSheetString("\u200B\uFEFF"), "zero-width only → empty", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("", SheetsPushUtilities.NormalizeSheetString("\u200E"), "LTR mark only (Format) → empty", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("x", (string)(object)SheetsPushUtilities.NormalizeCellValueForUpload("x"), "non-blank unchanged", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(3, (int)(object)SheetsPushUtilities.NormalizeCellValueForUpload(3), "non-string unchanged", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestSplitActionPushRowPreservingColumnsEF(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestSplitActionPushRowPreservingColumnsEF));
            var full = new List<object> { "a", "b", "c", "d", "E_FORMULA", "F_FORMULA", "g", "h" };
            var (ad, gPlus) = SheetsPushUtilities.SplitActionPushRowPreservingColumnsEF(full);
            TestBase.AssertTrue(
                Enumerable.SequenceEqual(new[] { "a", "b", "c", "d" }, ad.Cast<string>()),
                "A–D preserved in order",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                Enumerable.SequenceEqual(new[] { "g", "h" }, gPlus.Cast<string>()),
                "columns E–F omitted; G+ values",
                ref testsRun, ref testsPassed, ref testsFailed);
        }
    }
}
