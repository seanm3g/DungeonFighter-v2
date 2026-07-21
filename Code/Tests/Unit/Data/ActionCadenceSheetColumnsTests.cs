using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Data;

namespace RPGGame.Tests.Unit.Data
{
    public static class ActionCadenceSheetColumnsTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionCadenceSheetColumns Tests ===\n");

            int testsRun = 0, testsPassed = 0, testsFailed = 0;

            TestEnsureHeader_AppendsFourFamilies(ref testsRun, ref testsPassed, ref testsFailed);
            TestParseTriple_ActionAndTurn_Independent(ref testsRun, ref testsPassed, ref testsFailed);
            TestLegacyCompact_StillConverts(ref testsRun, ref testsPassed, ref testsFailed);
            TestPush_ClearsLegacyCompactColumns(ref testsRun, ref testsPassed, ref testsFailed);
            TestCollectLegacyColumnIndices_MatchesKyBand(ref testsRun, ref testsPassed, ref testsFailed);
            TestRemoveColumns_PreservesCadencesBand(ref testsRun, ref testsPassed, ref testsFailed);
            TestBuildDescendingDeleteRanges_Contiguous(ref testsRun, ref testsPassed, ref testsFailed);

            TestBase.PrintSummary("ActionCadenceSheetColumns Tests", testsRun, testsPassed, testsFailed);
        }

        private static void TestEnsureHeader_AppendsFourFamilies(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestEnsureHeader_AppendsFourFamilies));
            var header = new SpreadsheetHeader(
                new[] { "", "" },
                new[] { "ACTION", "DAMAGE(%)" },
                labelRowIndex: 1,
                dataStartRowIndex: 2);
            var (ensured, added) = ActionCadenceSheetColumns.EnsureHeader(header);
            TestBase.AssertTrue(added, "columns added", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                ensured.GetColumnIndex(ActionCadenceSheetColumns.ContextBand, "TURN →") >= 0,
                "TURN →",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                ensured.GetColumnIndex(ActionCadenceSheetColumns.ContextBand, "ACTION DURATION") >= 0,
                "ACTION DURATION",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                ensured.GetColumnIndex(ActionCadenceSheetColumns.ContextBand, "FIGHT") >= 0,
                "FIGHT enable",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                ensured.GetColumnIndex(ActionCadenceSheetColumns.ContextBand, "DUNGEON →") >= 0,
                "DUNGEON →",
                ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestParseTriple_ActionAndTurn_Independent(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestParseTriple_ActionAndTurn_Independent));
            var context = new[]
            {
                "", "",
                "CADENCES", "CADENCES", "CADENCES",
                "CADENCES", "CADENCES", "CADENCES",
                "HERO DICE ROLL MODIFICATIONS", "HERO BASE STATS"
            };
            var labels = new[]
            {
                "ACTION", "DAMAGE(%)",
                "TURN", "TURN DURATION", "TURN →",
                "ACTION", "ACTION DURATION", "ACTION →",
                "COMBO", "DAMAGE MOD"
            };
            var (header, _) = SpreadsheetActionParser.BuildHeaderFromSheetRows(new List<string[]> { context, labels });
            TestBase.AssertTrue(header != null, "header", ref testsRun, ref testsPassed, ref testsFailed);
            if (header == null) return;

            var cols = new[]
            {
                "SETUP", "100%",
                "1", "1", "hero_combo_threshold",
                "1", "1", "hero_next_action_damage",
                "3", "10"
            };
            var data = SpreadsheetActionData.FromCsvRow(cols, header);
            TestBase.AssertTrue(!string.IsNullOrWhiteSpace(data.CadenceBundlesJson), "bundles json", ref testsRun, ref testsPassed, ref testsFailed);

            var action = SpreadsheetToActionDataConverter.Convert(data);
            var blocks = ActionCadenceEditorSync.LoadBlocks(action);
            TestBase.AssertTrue(blocks.Count >= 2, "two cadence blocks", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                blocks.Any(b => b.Cadence.Equals("Turn", StringComparison.OrdinalIgnoreCase)
                                && b.Mechanics.Any(m => m.MechanicId.Contains("combo", StringComparison.OrdinalIgnoreCase))),
                "TURN has combo",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                blocks.Any(b => b.Cadence.Equals("Action", StringComparison.OrdinalIgnoreCase)
                                && b.Mechanics.Any(m => m.MechanicId.Contains("damage", StringComparison.OrdinalIgnoreCase))),
                "ACTION has damage mod",
                ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestLegacyCompact_StillConverts(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestLegacyCompact_StillConverts));
            var labels = new[] { "ACTION", "DURATION", "CADENCE", "COMBO" };
            var context = new[] { "", "STATUS EFFECT", "STATUS EFFECT", "HERO DICE ROLL MODIFICATIONS" };
            var (header, _) = SpreadsheetActionParser.BuildHeaderFromSheetRows(new List<string[]> { context, labels });
            if (header == null)
            {
                TestBase.AssertTrue(false, "header", ref testsRun, ref testsPassed, ref testsFailed);
                return;
            }

            var data = SpreadsheetActionData.FromCsvRow(new[] { "LEGACY", "2", "TURN", "1" }, header);
            data.Mechanics = "hero_combo_threshold";
            // Force legacy path (no CADENCES columns → empty CadenceBundlesJson until LoadBundles fallback)
            data.CadenceBundlesJson = "";
            var action = SpreadsheetToActionDataConverter.Convert(data);
            TestBase.AssertTrue(
                action.ActionAttackBonuses?.BonusGroups != null && action.ActionAttackBonuses.BonusGroups.Count > 0
                || action.ComboThresholdAdjustment != 0
                || ActionCadenceEditorSync.LoadBlocks(action).Count > 0,
                "legacy cadence still produces bonuses or blocks",
                ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestPush_ClearsLegacyCompactColumns(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestPush_ClearsLegacyCompactColumns));
            var bare = new SpreadsheetHeader(
                new[] { "", "", "", "" },
                new[] { "ACTION", "DURATION", "CADENCE", "MECHANICS" },
                1, 2);
            var (header, _) = ActionCadenceSheetColumns.EnsureHeader(bare);
            var data = new SpreadsheetActionData { Action = "X", Duration = "9", Cadence = "TURN", Mechanics = "weaken" };
            ActionCadenceSheetColumns.ApplyBundlesToSpreadsheetRow(data, new[]
            {
                new CadenceEditorBlock
                {
                    Cadence = "Turn",
                    Duration = 1,
                    Mechanics = { new CadenceMechanicRow { MechanicId = "hero_accuracy", Quantity = 2 } }
                }
            });
            var row = SpreadsheetActionDataSheetRowSerializer.ToRow(data, header);
            int durIdx = header.GetColumnIndex(null, "DURATION");
            int cadIdx = header.GetColumnIndex(null, "CADENCE");
            int mechIdx = header.GetColumnIndex(null, "MECHANICS");
            TestBase.AssertTrue(durIdx < 0 || string.IsNullOrEmpty(row[durIdx]), "DURATION cleared", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(cadIdx < 0 || string.IsNullOrEmpty(row[cadIdx]), "CADENCE cleared", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(mechIdx < 0 || string.IsNullOrEmpty(row[mechIdx]), "MECHANICS cleared", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                header.GetColumnIndex(ActionCadenceSheetColumns.ContextBand, "TURN →") >= 0,
                "TURN → exists for push",
                ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestCollectLegacyColumnIndices_MatchesKyBand(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestCollectLegacyColumnIndices_MatchesKyBand));
            // Mirrors live ACTIONS layout: A–J core, K–V old * CADENCE triples, W–Y compact, Z TARGET, then CADENCES.
            var contexts = new[]
            {
                "", "", "", "", "", "", "", "", "", "",
                "TURN CADENCE", "", "",
                "ACTION CADENCE", "", "",
                "FIGHT CADENCE", "", "",
                "DUNGEON CADENCE", "", "",
                "STATUS EFFECT", "",
                "MECHANICS",
                "MECHANICS",
                "CADENCES", "CADENCES", "CADENCES"
            };
            var labels = new[]
            {
                "ACTION", "DESCRIPTION", "RARITY", "CATEGORY", "TAGS", "e(V)", "DPS(%)", "# OF HITS", "DAMAGE(%)", "SPEED(x)",
                "TURN", "TURN DURATION", "TURN →",
                "ACTION", "ACTION DURATION", "ACTION →",
                "FIGHT", "FIGHT DURATION", "FIGHT →",
                "DUNGEON", "DUNGEON DURATION", "DUNGEON →",
                "DURATION", "CADENCE",
                "MECHANICS",
                "TARGET",
                "TURN", "TURN DURATION", "TURN →"
            };
            var (header, _) = SpreadsheetActionParser.BuildHeaderFromSheetRows(
                new List<string[]> { contexts, labels });
            TestBase.AssertTrue(header != null, "header", ref testsRun, ref testsPassed, ref testsFailed);
            if (header == null) return;

            var legacy = ActionCadenceSheetColumns.CollectLegacyColumnIndicesToRemove(header);
            TestBase.AssertTrue(legacy.Count == 15, $"expected 15 legacy cols, got {legacy.Count}", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(legacy[0] == 10 && legacy[^1] == 24, "legacy is K–Y (10–24)", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(!legacy.Contains(25), "TARGET kept", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(!legacy.Contains(26), "CADENCES TURN kept", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestRemoveColumns_PreservesCadencesBand(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestRemoveColumns_PreservesCadencesBand));
            var contexts = new[]
            {
                "", "TURN CADENCE", "STATUS EFFECT", "MECHANICS", "CADENCES"
            };
            var labels = new[] { "ACTION", "TURN", "DURATION", "MECHANICS", "TURN →" };
            var header = new SpreadsheetHeader(contexts, labels, 1, 2);
            var legacy = ActionCadenceSheetColumns.CollectLegacyColumnIndicesToRemove(header);
            var trimmed = ActionCadenceSheetColumns.RemoveColumns(header, legacy);
            TestBase.AssertTrue(
                trimmed.GetColumnIndex(null, "ACTION") == 0,
                "ACTION remains first",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                trimmed.GetColumnIndex(ActionCadenceSheetColumns.ContextBand, "TURN →") >= 0,
                "CADENCES TURN → remains",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                trimmed.GetColumnIndex(null, "DURATION") < 0,
                "legacy DURATION removed",
                ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(
                trimmed.LabelByIndex.Count == 2,
                $"expected 2 cols left, got {trimmed.LabelByIndex.Count}",
                ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestBuildDescendingDeleteRanges_Contiguous(
            ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestBuildDescendingDeleteRanges_Contiguous));
            var ranges = ActionCadenceSheetColumns.BuildDescendingDeleteRanges(
                new[] { 10, 11, 12, 22, 23, 24 });
            TestBase.AssertTrue(ranges.Count == 2, $"two ranges, got {ranges.Count}", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(ranges[0].StartInclusive == 22 && ranges[0].EndExclusive == 25, "first delete W–Y", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(ranges[1].StartInclusive == 10 && ranges[1].EndExclusive == 13, "then K–M", ref testsRun, ref testsPassed, ref testsFailed);
        }
    }
}
