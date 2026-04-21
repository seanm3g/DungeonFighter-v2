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
            TestDamageColumn_ResolvedWithoutPercentInHeader(ref testsRun, ref testsPassed, ref testsFailed);
            TestSingleHeaderRow_CoreColumns(ref testsRun, ref testsPassed, ref testsFailed);
            TestEmptyOptionalFields(ref testsRun, ref testsPassed, ref testsFailed);
            TestBuildHeaderFromSheetRows_FirstDataRow(ref testsRun, ref testsPassed, ref testsFailed);
            TestParseCsvContent_RejectsNonActionHeader(ref testsRun, ref testsPassed, ref testsFailed);
            TestShiftColumn_MapsJumpRelativeRoundTrip(ref testsRun, ref testsPassed, ref testsFailed);
            TestJumpRelativeFallback_WhenNoShiftColumn(ref testsRun, ref testsPassed, ref testsFailed);
            TestHeroBaseStats_ParsesNextActionMods(ref testsRun, ref testsPassed, ref testsFailed);
            TestHeroBaseStats_ActionSpeedActionDamageHeaders(ref testsRun, ref testsPassed, ref testsFailed);
            TestHeroBaseStats_PrefixedHeroModColumnLabels(ref testsRun, ref testsPassed, ref testsFailed);
            TestEnemyAndHeroBaseStats_SeparateNextActionMods(ref testsRun, ref testsPassed, ref testsFailed);

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

        /// <summary>
        /// Published CSV often labels the column DAMAGE without a % in the header; ingestion must not read # OF HITS (index 6) as damage.
        /// </summary>
        private static void TestDamageColumn_ResolvedWithoutPercentInHeader(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestDamageColumn_ResolvedWithoutPercentInHeader));
            var labelRow = new[]
            {
                "ACTION", "DESCRIPTION", "", "RARITY", "CATEGORY", "DPS(%)", "# OF HITS", "DAMAGE", "SPEED(x)"
            };
            var (header, _) = SpreadsheetActionParser.BuildHeaderFromSheetRows(new List<string[]> { labelRow });
            TestBase.AssertTrue(header != null, "header parsed", ref testsRun, ref testsPassed, ref testsFailed);
            if (header == null) return;

            var dataRow = new[]
            {
                "Lucky Charm", "test", "", "C", "General", "0", "1", "10%", "1"
            };
            var parsed = SpreadsheetActionData.FromCsvRow(dataRow, header);

            TestBase.AssertEqual("10%", parsed.Damage, "DAMAGE column (not # OF HITS)", ref testsRun, ref testsPassed, ref testsFailed);

            var actionData = SpreadsheetToActionDataConverter.Convert(parsed);
            TestBase.AssertTrue(Math.Abs(actionData.DamageMultiplier - 0.1) < 0.0001, "10% -> DamageMultiplier 0.1", ref testsRun, ref testsPassed, ref testsFailed);
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

        private static void TestParseCsvContent_RejectsNonActionHeader(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestParseCsvContent_RejectsNonActionHeader));
            string fakeHtml = "<!DOCTYPE html>\n<html><head></head><body>not a csv</body></html>";
            var r = SpreadsheetActionParser.ParseCsvContent(fakeHtml);
            TestBase.AssertTrue(r.Header == null, "non-CSV must not yield ACTION header", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestBuildHeaderFromSheetRows_FirstDataRow(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestBuildHeaderFromSheetRows_FirstDataRow));
            var rows = new List<string[]>();
            TestBase.AssertTrue(SpreadsheetActionParser.BuildHeaderFromSheetRows(rows).Header == null, "empty rows -> no header", ref testsRun, ref testsPassed, ref testsFailed);
        }

        /// <summary>
        /// ACTIONS sheet: <c>SHIFT</c> between <c>JUMP</c> and <c>DISRUPT</c> maps to <see cref="SpreadsheetActionData.JumpRelative"/> (Jump +slots).
        /// </summary>
        private static void TestShiftColumn_MapsJumpRelativeRoundTrip(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestShiftColumn_MapsJumpRelativeRoundTrip));
            var labelRow = new[] { "ACTION", "JUMP", "SHIFT", "DISRUPT" };
            var (header, _) = SpreadsheetActionParser.BuildHeaderFromSheetRows(new List<string[]> { labelRow });
            TestBase.AssertTrue(header != null, "header parsed", ref testsRun, ref testsPassed, ref testsFailed);
            if (header == null) return;

            var dataRow = new[] { "ChainTest", "2", "1", "" };
            var parsed = SpreadsheetActionData.FromCsvRow(dataRow, header);
            TestBase.AssertEqual("2", parsed.Jump, "JUMP", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("1", parsed.JumpRelative, "SHIFT -> JumpRelative", ref testsRun, ref testsPassed, ref testsFailed);

            var outRow = SpreadsheetActionDataSheetRowSerializer.ToRow(parsed, header);
            TestBase.AssertEqual(4, outRow.Length, "row width", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("1", outRow[2], "SHIFT column preserved", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestJumpRelativeFallback_WhenNoShiftColumn(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestJumpRelativeFallback_WhenNoShiftColumn));
            var labelRow = new[] { "ACTION", "JUMP", "JUMP RELATIVE", "DISRUPT" };
            var (header, _) = SpreadsheetActionParser.BuildHeaderFromSheetRows(new List<string[]> { labelRow });
            TestBase.AssertTrue(header != null, "header parsed", ref testsRun, ref testsPassed, ref testsFailed);
            if (header == null) return;

            var dataRow = new[] { "Legacy", "", "3", "" };
            var parsed = SpreadsheetActionData.FromCsvRow(dataRow, header);
            TestBase.AssertEqual("3", parsed.JumpRelative, "JUMP RELATIVE fallback", ref testsRun, ref testsPassed, ref testsFailed);

            var outRow = SpreadsheetActionDataSheetRowSerializer.ToRow(parsed, header);
            TestBase.AssertEqual("3", outRow[2], "JUMP RELATIVE column when no SHIFT", ref testsRun, ref testsPassed, ref testsFailed);
        }

        /// <summary>
        /// Row-1 block "HERO BASE STATS" must populate hero SpeedMod/DamageMod/MultiHitMod/AmpMod (sheet AJ–AM) for CSV import and push.
        /// </summary>
        private static void TestHeroBaseStats_ParsesNextActionMods(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestHeroBaseStats_ParsesNextActionMods));
            var contextRow = new[] { "", "HERO BASE STATS", "HERO BASE STATS", "HERO BASE STATS", "HERO BASE STATS" };
            var labelRow = new[] { "ACTION", "SPEED MOD", "DAMAGE MOD", "MULTIHIT MOD", "AMP_MOD" };
            var (header, _) = SpreadsheetActionParser.BuildHeaderFromSheetRows(new List<string[]> { contextRow, labelRow });
            TestBase.AssertTrue(header != null, "header parsed", ref testsRun, ref testsPassed, ref testsFailed);
            if (header == null) return;

            var dataRow = new[] { "BULK DAY", "20", "50", "2", "10" };
            var parsed = SpreadsheetActionData.FromCsvRow(dataRow, header);
            TestBase.AssertEqual("20", parsed.SpeedMod, "SPEED MOD under HERO BASE STATS", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("50", parsed.DamageMod, "DAMAGE MOD (e.g. BULK DAY)", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("2", parsed.MultiHitMod, "MULTIHIT MOD", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("10", parsed.AmpMod, "AMP_MOD", ref testsRun, ref testsPassed, ref testsFailed);

            var outRow = SpreadsheetActionDataSheetRowSerializer.ToRow(parsed, header);
            TestBase.AssertEqual("50", outRow[2], "push DAMAGE MOD column", ref testsRun, ref testsPassed, ref testsFailed);

            var actionData = SpreadsheetToActionDataConverter.Convert(parsed);
            TestBase.AssertEqual("50", actionData.DamageMod, "ActionData.DamageMod for UI", ref testsRun, ref testsPassed, ref testsFailed);
        }

        /// <summary>
        /// Google Sheet templates may use ACTION SPEED / ACTION DAMAGE (vs SPEED MOD / DAMAGE MOD); wrapped headers use line breaks.
        /// </summary>
        private static void TestHeroBaseStats_ActionSpeedActionDamageHeaders(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestHeroBaseStats_ActionSpeedActionDamageHeaders));
            var contextRow = new[] { "", "HERO BASE STATS", "HERO BASE STATS", "HERO BASE STATS", "HERO BASE STATS" };
            var labelRow = new[] { "ACTION", "ACTION\nSPEED", "ACTION DAMAGE", "MULTIHIT MOD", "AMP_MOD" };
            var (header, _) = SpreadsheetActionParser.BuildHeaderFromSheetRows(new List<string[]> { contextRow, labelRow });
            TestBase.AssertTrue(header != null, "header parsed", ref testsRun, ref testsPassed, ref testsFailed);
            if (header == null) return;

            var dataRow = new[] { "BULK DAY", "20", "50", "2", "10" };
            var parsed = SpreadsheetActionData.FromCsvRow(dataRow, header);
            TestBase.AssertEqual("20", parsed.SpeedMod, "ACTION SPEED (newline) under HERO BASE STATS", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("50", parsed.DamageMod, "ACTION DAMAGE under HERO BASE STATS", ref testsRun, ref testsPassed, ref testsFailed);

            var outRow = SpreadsheetActionDataSheetRowSerializer.ToRow(parsed, header);
            TestBase.AssertEqual("20", outRow[1], "push ACTION SPEED column", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("50", outRow[2], "push ACTION DAMAGE column", ref testsRun, ref testsPassed, ref testsFailed);
        }

        /// <summary>Some ACTION tabs use row-2 labels like "HERO ACTION SPEED MOD" / "HERO MULTIHIT MOD" instead of "SPEED MOD" / "MULTIHIT MOD".</summary>
        private static void TestHeroBaseStats_PrefixedHeroModColumnLabels(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestHeroBaseStats_PrefixedHeroModColumnLabels));
            var contextRow = new[] { "", "HERO BASE STATS", "HERO BASE STATS", "HERO BASE STATS", "HERO BASE STATS" };
            var labelRow = new[] { "ACTION", "HERO ACTION SPEED MOD", "DAMAGE MOD", "HERO MULTIHIT MOD", "AMP_MOD" };
            var (header, _) = SpreadsheetActionParser.BuildHeaderFromSheetRows(new List<string[]> { contextRow, labelRow });
            TestBase.AssertTrue(header != null, "header parsed", ref testsRun, ref testsPassed, ref testsFailed);
            if (header == null) return;

            var dataRow = new[] { "WIDE SWING", "-15", "-40%", "2", "0" };
            var parsed = SpreadsheetActionData.FromCsvRow(dataRow, header);
            TestBase.AssertEqual("-15", parsed.SpeedMod, "HERO ACTION SPEED MOD under HERO BASE STATS", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("2", parsed.MultiHitMod, "HERO MULTIHIT MOD under HERO BASE STATS", ref testsRun, ref testsPassed, ref testsFailed);

            var outRow = SpreadsheetActionDataSheetRowSerializer.ToRow(parsed, header);
            TestBase.AssertEqual("-15", outRow[1], "push HERO ACTION SPEED MOD column", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("2", outRow[3], "push HERO MULTIHIT MOD column", ref testsRun, ref testsPassed, ref testsFailed);
        }

        /// <summary>ENEMY BASE STATS (AD–AG) and HERO BASE STATS (AJ–AM) must map to separate fields and round-trip.</summary>
        private static void TestEnemyAndHeroBaseStats_SeparateNextActionMods(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestEnemyAndHeroBaseStats_SeparateNextActionMods));
            var contextRow = new[]
            {
                "", "ENEMY BASE STATS", "ENEMY BASE STATS", "ENEMY BASE STATS", "ENEMY BASE STATS",
                "HERO BASE STATS", "HERO BASE STATS", "HERO BASE STATS", "HERO BASE STATS"
            };
            var labelRow = new[]
            {
                "ACTION", "SPEED MOD", "DAMAGE MOD", "MULTIHIT MOD", "AMP_MOD",
                "SPEED MOD", "DAMAGE MOD", "MULTIHIT MOD", "AMP_MOD"
            };
            var (header, _) = SpreadsheetActionParser.BuildHeaderFromSheetRows(new List<string[]> { contextRow, labelRow });
            TestBase.AssertTrue(header != null, "header parsed", ref testsRun, ref testsPassed, ref testsFailed);
            if (header == null) return;

            var dataRow = new[] { "DUAL", "1", "2", "3", "4", "10", "20", "30", "40" };
            var parsed = SpreadsheetActionData.FromCsvRow(dataRow, header);
            TestBase.AssertEqual("1", parsed.EnemySpeedMod, "enemy SPEED MOD", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("2", parsed.EnemyDamageMod, "enemy DAMAGE MOD", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("3", parsed.EnemyMultiHitMod, "enemy MULTIHIT", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("4", parsed.EnemyAmpMod, "enemy AMP", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("10", parsed.SpeedMod, "hero SPEED MOD", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("20", parsed.DamageMod, "hero DAMAGE MOD", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("30", parsed.MultiHitMod, "hero MULTIHIT", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("40", parsed.AmpMod, "hero AMP", ref testsRun, ref testsPassed, ref testsFailed);

            var outRow = SpreadsheetActionDataSheetRowSerializer.ToRow(parsed, header);
            TestBase.AssertEqual("1", outRow[1], "push enemy speed", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("10", outRow[5], "push hero speed", ref testsRun, ref testsPassed, ref testsFailed);
        }
    }
}
