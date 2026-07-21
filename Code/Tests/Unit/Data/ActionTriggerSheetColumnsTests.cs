using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RPGGame;
using RPGGame.Data;

namespace RPGGame.Tests.Unit.Data
{
    public static class ActionTriggerSheetColumnsTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionTriggerSheetColumns Tests ===\n");

            int testsRun = 0, testsPassed = 0, testsFailed = 0;

            TestEnsureHeader_AppendsScopeAndMechanicsColumns(ref testsRun, ref testsPassed, ref testsFailed);
            TestParseTriple_OnKillFightDamageMod_Converts(ref testsRun, ref testsPassed, ref testsFailed);
            TestRoundTrip_WriteThenRead_PreservesBundle(ref testsRun, ref testsPassed, ref testsFailed);
            TestLegacyOnKillOnly_StillBuildsBundle(ref testsRun, ref testsPassed, ref testsFailed);
            TestBlankScope_MeansInstant(ref testsRun, ref testsPassed, ref testsFailed);
            TestActionsJson_HasDemoActionPerTriggerFamily(ref testsRun, ref testsPassed, ref testsFailed);

            TestBase.PrintSummary("ActionTriggerSheetColumns Tests", testsRun, testsPassed, testsFailed);
        }

        private static void TestEnsureHeader_AppendsScopeAndMechanicsColumns(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestEnsureHeader_AppendsScopeAndMechanicsColumns));
            var context = new[] { "", "", "" };
            var labels = new[] { "ACTION", "DAMAGE(%)", "ON KILL" };
            var header = new SpreadsheetHeader(context, labels, labelRowIndex: 1, dataStartRowIndex: 2);

            var (ensured, added) = ActionTriggerSheetColumns.EnsureHeader(header);
            TestBase.AssertTrue(added, "columns were appended", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                ensured.GetColumnIndex(ActionTriggerSheetColumns.ContextBand, "ON KILL SCOPE") >= 0,
                "ON KILL SCOPE present",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                ensured.GetColumnIndex(ActionTriggerSheetColumns.ContextBand, "ON KILL →") >= 0,
                "ON KILL → present",
                ref testsRun, ref testsPassed, ref testsFailed);

            var (again, addedAgain) = ActionTriggerSheetColumns.EnsureHeader(ensured);
            TestBase.AssertTrue(!addedAgain, "second ensure is idempotent", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(again.LabelByIndex.Count, ensured.LabelByIndex.Count, "width stable", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestParseTriple_OnKillFightDamageMod_Converts(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestParseTriple_OnKillFightDamageMod_Converts));
            var context = new[]
            {
                "", "", "TRIGGERS", "TRIGGERS", "TRIGGERS", "HERO BASE STATS"
            };
            var labels = new[]
            {
                "ACTION", "DAMAGE(%)", "ON KILL", "ON KILL SCOPE", "ON KILL →", "DAMAGE MOD"
            };
            var (header, _) = SpreadsheetActionParser.BuildHeaderFromSheetRows(new List<string[]> { context, labels });
            TestBase.AssertTrue(header != null, "header parsed", ref testsRun, ref testsPassed, ref testsFailed);
            if (header == null) return;

            var columns = new[] { "FINISHER", "100%", "1", "FIGHT", "hero_action_damage", "5" };
            var data = SpreadsheetActionData.FromCsvRow(columns, header);

            TestBase.AssertEqual("1", data.OnKill, "legacy OnKill count", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(!string.IsNullOrWhiteSpace(data.TriggerBundlesJson), "bundles json set", ref testsRun, ref testsPassed, ref testsFailed);

            var action = SpreadsheetToActionDataConverter.Convert(data);
            TestBase.AssertTrue(
                !action.TriggerConditions.Exists(c => string.Equals(c, "ONKILL", StringComparison.OrdinalIgnoreCase)),
                "ONKILL with → pointers does not merge into whole-row triggerConditions (gate_only_listed)",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(action.TriggerBundles.Count >= 1, "has bundle", ref testsRun, ref testsPassed, ref testsFailed);
            var kill = action.TriggerBundles.FirstOrDefault(b =>
                string.Equals(ActionTriggerSheetColumns.CanonicalWhen(b.When), "ONKILL", StringComparison.OrdinalIgnoreCase));
            TestBase.AssertTrue(kill != null, "ONKILL bundle", ref testsRun, ref testsPassed, ref testsFailed);
            if (kill == null) return;
            TestBase.AssertEqual("FIGHT", kill.Scope.ToUpperInvariant(), "scope FIGHT", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                kill.ParseMechanicIds().Any(m => m.Equals("hero_action_damage", StringComparison.OrdinalIgnoreCase)),
                "points at hero_action_damage",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("5", data.DamageMod, "magnitude stays in DAMAGE MOD", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestRoundTrip_WriteThenRead_PreservesBundle(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestRoundTrip_WriteThenRead_PreservesBundle));
            var bare = new SpreadsheetHeader(
                new[] { "", "" },
                new[] { "ACTION", "DAMAGE(%)" },
                labelRowIndex: 1,
                dataStartRowIndex: 2);
            var (header, _) = ActionTriggerSheetColumns.EnsureHeader(bare);

            var data = new SpreadsheetActionData
            {
                Action = "EXECUTE",
                Damage = "100%",
                DamageMod = "5"
            };
            ActionTriggerSheetColumns.ApplyBundlesToSpreadsheetRow(data, new[]
            {
                new ActionTriggerBundle
                {
                    When = "ONKILL",
                    Count = "1",
                    Scope = "FIGHT",
                    Mechanics = "hero_action_damage"
                }
            });

            var row = SpreadsheetActionDataSheetRowSerializer.ToRow(data, header);
            var roundTrip = SpreadsheetActionData.FromCsvRow(row, header);
            var bundles = ActionTriggerSheetColumns.LoadBundles(roundTrip);
            var kill = bundles.FirstOrDefault(b =>
                ActionTriggerSheetColumns.CanonicalWhen(b.When) == "ONKILL");
            TestBase.AssertTrue(kill != null, "bundle survived round-trip", ref testsRun, ref testsPassed, ref testsFailed);
            if (kill == null) return;
            TestBase.AssertEqual("1", kill.Count, "count", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("FIGHT", kill.Scope.ToUpperInvariant(), "scope", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                kill.Mechanics.IndexOf("hero_action_damage", StringComparison.OrdinalIgnoreCase) >= 0,
                "mechanics pointer",
                ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestLegacyOnKillOnly_StillBuildsBundle(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestLegacyOnKillOnly_StillBuildsBundle));
            var labels = new[] { "ACTION", "ON KILL" };
            var (header, _) = SpreadsheetActionParser.BuildHeaderFromSheetRows(new List<string[]> { labels });
            TestBase.AssertTrue(header != null, "header", ref testsRun, ref testsPassed, ref testsFailed);
            if (header == null) return;

            var data = SpreadsheetActionData.FromCsvRow(new[] { "SLASH", "1" }, header);
            var action = SpreadsheetToActionDataConverter.Convert(data);
            TestBase.AssertTrue(
                action.TriggerConditions.Exists(c => string.Equals(c, "ONKILL", StringComparison.OrdinalIgnoreCase)),
                "legacy OnKill → ONKILL token",
                ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestBlankScope_MeansInstant(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestBlankScope_MeansInstant));
            var data = new SpreadsheetActionData();
            ActionTriggerSheetColumns.ApplyBundlesToSpreadsheetRow(data, new[]
            {
                new ActionTriggerBundle
                {
                    When = "ONMISS",
                    Count = "1",
                    Scope = "",
                    Mechanics = "heal"
                }
            });
            var bundles = ActionTriggerSheetColumns.LoadBundles(data);
            TestBase.AssertEqual(1, bundles.Count, "one bundle", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("", bundles[0].Scope, "blank scope = instant", ref testsRun, ref testsPassed, ref testsFailed);
        }

        /// <summary>
        /// Every TRIGGERS sheet WHEN family should have at least one Actions.json demo row
        /// with that WHEN in triggerBundlesJson (Tier 3 samples for designers / Action Lab).
        /// </summary>
        private static void TestActionsJson_HasDemoActionPerTriggerFamily(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestActionsJson_HasDemoActionPerTriggerFamily));

            string? path = JsonLoader.FindGameDataFile("Actions.json");
            TestBase.AssertTrue(!string.IsNullOrWhiteSpace(path), "Actions.json found", ref testsRun, ref testsPassed, ref testsFailed);
            if (string.IsNullOrWhiteSpace(path))
                return;

            string json = System.IO.File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (!el.TryGetProperty("triggerBundlesJson", out var bundlesProp))
                    continue;
                string? raw = bundlesProp.GetString();
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                try
                {
                    var bundles = JsonSerializer.Deserialize<List<ActionTriggerBundle>>(raw);
                    if (bundles == null)
                        continue;
                    foreach (var b in bundles)
                    {
                        if (b == null || string.IsNullOrWhiteSpace(b.When))
                            continue;
                        covered.Add(ActionTriggerSheetColumns.CanonicalWhen(b.When));
                    }
                }
                catch (JsonException)
                {
                    // skip corrupt row; coverage assert below will fail if needed
                }
            }

            foreach (var family in ActionTriggerSheetColumns.Families)
            {
                TestBase.AssertTrue(
                    covered.Contains(family.WhenToken),
                    $"Actions.json demo covers TRIGGERS family {family.WhenToken}",
                    ref testsRun, ref testsPassed, ref testsFailed);
            }
        }
    }
}
