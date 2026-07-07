using System;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class SpreadsheetDurationSemanticsTests
    {
        public static void RunAll()
        {
            Console.WriteLine("\n=== SpreadsheetDurationSemantics Tests ===");
            TestDurationColumnIsCadenceCount();
            TestStunRowDurationStillCadenceCount();
            TestLegacyCadenceDurationJsonFallback();
            TestActionBonusProcessorUsesDurationColumn();
            TestCombinedDurationCellInfersCadence();
            TestAttackCombinedDurationCell();
            TestHeroComboWithoutCadenceInfersActionDuration();
            TestSeparateCadenceDurationColumns();
        }

        private static void TestSeparateCadenceDurationColumns()
        {
            var row = new SpreadsheetActionData { Cadence = "TURN", Duration = "3", HeroCombo = "1" };
            var data = SpreadsheetToActionDataConverter.Convert(row);
            TestHarnessBase.AssertEqual("TURN", data.Cadence, "Separate CADENCE=ATTACK");
            TestHarnessBase.AssertEqual(3, data.ComboBonusDuration, "Separate DURATION=3");
        }

        private static void TestDurationColumnIsCadenceCount()
        {
            var row = new SpreadsheetActionData { Duration = "2", Cadence = "ACTION", DamageMod = "25" };
            TestHarnessBase.AssertEqual(2, SpreadsheetDurationSemantics.ResolveCadenceDuration(row),
                "Column K DURATION is cadence duration (ACTION x2)");
        }

        private static void TestStunRowDurationStillCadenceCount()
        {
            var row = new SpreadsheetActionData { Duration = "3", Cadence = "TURN", Stun = "1" };
            TestHarnessBase.AssertEqual(3, SpreadsheetDurationSemantics.ResolveCadenceDuration(row),
                "DURATION is cadence count even when row has status effects");
        }

        private static void TestLegacyCadenceDurationJsonFallback()
        {
            var row = new SpreadsheetActionData { Duration = "", CadenceApplicationCount = "2", Cadence = "ACTION" };
            TestHarnessBase.AssertEqual(2, SpreadsheetDurationSemantics.ResolveCadenceDuration(row),
                "Legacy cadenceDuration JSON field fallback");
        }

        private static void TestActionBonusProcessorUsesDurationColumn()
        {
            var row = new SpreadsheetActionData { Duration = "2", Cadence = "ACTION", DamageMod = "25" };
            var bonuses = ActionAttackKeywordProcessor.ProcessBonuses(row);
            TestHarnessBase.AssertEqual(1, bonuses.BonusGroups.Count, "One bonus group");
            TestHarnessBase.AssertEqual(2, bonuses.BonusGroups[0].Count, "ACTION x2 from DURATION column");
        }

        private static void TestCombinedDurationCellInfersCadence()
        {
            var row = new SpreadsheetActionData { Duration = "3 ACTION", HeroCombo = "5" };
            SpreadsheetDurationSemantics.NormalizeDurationAndCadence(row);
            TestHarnessBase.AssertEqual("3", row.Duration, "3 ACTION -> numeric duration");
            TestHarnessBase.AssertEqual("ACTION", row.Cadence, "3 ACTION -> cadence keyword");

            var bonuses = ActionAttackKeywordProcessor.ProcessBonuses(row);
            TestHarnessBase.AssertEqual(1, bonuses.BonusGroups.Count, "Combined cell creates bonus group");
            TestHarnessBase.AssertEqual(3, bonuses.BonusGroups[0].Count, "3 ACTION layers");
            TestHarnessBase.AssertTrue(bonuses.BonusGroups[0].Bonuses.Exists(b => b.Type == "COMBO" && b.Value == 5),
                "Hero COMBO feeds keyword bonus group");
        }

        private static void TestAttackCombinedDurationCell()
        {
            var row = new SpreadsheetActionData { Duration = "ACTION x2", HeroHit = "2" };
            SpreadsheetDurationSemantics.NormalizeDurationAndCadence(row);
            TestHarnessBase.AssertEqual("2", row.Duration, "ACTION x2 -> count 2");
            TestHarnessBase.AssertEqual("ACTION", row.Cadence, "ACTION x2 -> ACTION cadence");
        }

        private static void TestHeroComboWithoutCadenceInfersActionDuration()
        {
            var row = new SpreadsheetActionData { HeroCombo = "5" };
            var data = SpreadsheetToActionDataConverter.Convert(row);
            TestHarnessBase.AssertEqual(1, data.ComboBonusDuration, "Inferred ATTACK x1 duration");
            TestHarnessBase.AssertEqual("TURN", data.Cadence, "Inferred ATTACK cadence for hero dice");
            TestHarnessBase.AssertTrue(data.ActionAttackBonuses?.BonusGroups?.Count == 1, "Bonus group created");
            TestHarnessBase.AssertEqual("TURN", data.ActionAttackBonuses!.BonusGroups[0].CadenceType, "COMBO bonus uses ATTACK cadence");
        }
    }
}
