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
        }

        private static void TestDurationColumnIsCadenceCount()
        {
            var row = new SpreadsheetActionData { Duration = "2", Cadence = "ACTION", DamageMod = "25%" };
            TestHarnessBase.AssertEqual(2, SpreadsheetDurationSemantics.ResolveCadenceDuration(row),
                "Column K DURATION is cadence duration (ACTION x2)");
        }

        private static void TestStunRowDurationStillCadenceCount()
        {
            var row = new SpreadsheetActionData { Duration = "3", Cadence = "ATTACK", Stun = "1" };
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
            var row = new SpreadsheetActionData { Duration = "2", Cadence = "ACTION", DamageMod = "25%" };
            var bonuses = ActionAttackKeywordProcessor.ProcessBonuses(row);
            TestHarnessBase.AssertEqual(1, bonuses.BonusGroups.Count, "One bonus group");
            TestHarnessBase.AssertEqual(2, bonuses.BonusGroups[0].Count, "ACTION x2 from DURATION column");
        }
    }
}
