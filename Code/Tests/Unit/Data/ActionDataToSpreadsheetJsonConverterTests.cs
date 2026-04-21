using System.Collections.Generic;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Tests for ActionDataToSpreadsheetJsonConverter (ActionData → SpreadsheetActionJson merge/save).
    /// </summary>
    public static class ActionDataToSpreadsheetJsonConverterTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            System.Console.WriteLine("=== ActionDataToSpreadsheetJsonConverter Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestMergeWithNoBaseRow();
            TestMergeWithBaseRow();
            TestConvertListPreservesOrderAndMerges();
            TestMergeEmptyTagsOverridesBaseRow();
            TestMergeOpenerFinisherRoundTrip();
            TestMergeHeroRollZeroClearsBaseRow();
            TestMergeCritMissRoundTrip();
            TestMergeEnemyRollBonusesRoundTrip();
            TestMergeJumpRelativeRoundTrip();
            TestSpreadsheetEnvironmentRowClearsWeaponsAndSetsDebuffAoe();
            TestSpreadsheetEnemyRowClearsWeaponTypes();

            TestBase.PrintSummary("ActionDataToSpreadsheetJsonConverter Tests", _testsRun, _testsPassed, _testsFailed);
        }

        /// <summary>
        /// When user clears tags, merged row must have empty Tags (not baseRow.Tags) so the change persists.
        /// </summary>
        private static void TestMergeEmptyTagsOverridesBaseRow()
        {
            System.Console.WriteLine("--- Merge empty Tags overrides base row ---");
            var baseRow = new SpreadsheetActionJson
            {
                Action = "JAB",
                Tags = "weapon",
                Rarity = "Common",
                Category = "100%",
                Cadence = "ABILITY"
            };
            var data = new ActionData
            {
                Name = "JAB",
                Tags = new List<string>(),
                Rarity = "Common",
                Category = "100%",
                Cadence = "ABILITY",
                Type = "Attack",
                TargetType = "SingleTarget",
                DamageMultiplier = 0.33,
                Length = 0.5,
                MultiHitCount = 1
            };
            var row = ActionDataToSpreadsheetJsonConverter.Merge(data, baseRow);
            TestBase.AssertEqual("", row.Tags, "Cleared tags must persist (not baseRow.Tags)", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMergeWithNoBaseRow()
        {
            System.Console.WriteLine("--- Merge with no base row ---");
            var data = new ActionData
            {
                Name = "TEST ACTION",
                Description = "A test",
                Rarity = "WEAPON",
                Category = "100%",
                Cadence = "ABILITY",
                DamageMultiplier = 1.0,
                Length = 1.0,
                MultiHitCount = 1
            };
            var row = ActionDataToSpreadsheetJsonConverter.Merge(data, null);
            TestBase.AssertEqual("TEST ACTION", row.Action, "Action name", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("WEAPON", row.Rarity, "Rarity", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("100%", row.Category, "Category", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("ABILITY", row.Cadence, "Cadence", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("100%", row.Damage, "Damage", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("1.00", row.Speed, "Speed", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("1", row.NumberOfHits, "NumberOfHits", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMergeWithBaseRow()
        {
            System.Console.WriteLine("--- Merge with base row ---");
            var baseRow = new SpreadsheetActionJson
            {
                Action = "ORIGINAL",
                Description = "Original desc",
                Rarity = "LEGENDARY",
                Category = "150%",
                Cadence = "ABILITIES",
                HeroAccuracy = "5",
                EnemyCrit = "1"
            };
            var data = new ActionData
            {
                Name = "UPDATED",
                Description = "Updated desc",
                Rarity = "WEAPON",
                Category = "66%",
                Cadence = "ACTION",
                DamageMultiplier = 0.33,
                Length = 0.5,
                MultiHitCount = 1
            };
            var row = ActionDataToSpreadsheetJsonConverter.Merge(data, baseRow);
            TestBase.AssertEqual("UPDATED", row.Action, "Action name overwritten", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("WEAPON", row.Rarity, "Rarity overwritten", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("", row.HeroAccuracy, "HeroAccuracy from ActionData when RollBonus is 0 (not base row)", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("", row.EnemyCrit, "EnemyCrit cleared when ActionData enemy crit adjustment is 0", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestConvertListPreservesOrderAndMerges()
        {
            System.Console.WriteLine("--- ConvertList order and merge ---");
            var originalRows = new List<SpreadsheetActionJson>
            {
                new SpreadsheetActionJson { Action = "FIRST", Rarity = "WEAPON" },
                new SpreadsheetActionJson { Action = "SECOND", Rarity = "LEGENDARY" }
            };
            var actions = new List<ActionData>
            {
                new ActionData { Name = "SECOND", Rarity = "LEGENDARY", Category = "100%", Cadence = "ABILITY", Type = "Attack", TargetType = "SingleTarget" },
                new ActionData { Name = "FIRST", Rarity = "WEAPON", Category = "66%", Cadence = "ACTION", Type = "Attack", TargetType = "SingleTarget" }
            };
            var result = ActionDataToSpreadsheetJsonConverter.ConvertList(actions, originalRows);
            TestBase.AssertEqual(2, result.Count, "Count", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("SECOND", result[0].Action, "First item is SECOND (editor order)", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("FIRST", result[1].Action, "Second item is FIRST", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMergeOpenerFinisherRoundTrip()
        {
            System.Console.WriteLine("--- Merge Opener/Finisher round-trip ---");
            var data = new ActionData
            {
                Name = "COMBO_SLOT",
                Type = "Attack",
                TargetType = "SingleTarget",
                DamageMultiplier = 1.0,
                Length = 1.0,
                IsOpener = true,
                IsFinisher = false
            };
            var row = ActionDataToSpreadsheetJsonConverter.Merge(data, null);
            TestBase.AssertEqual("true", row.Opener, "Opener true should write as true", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("", row.Finisher, "Finisher false should write empty", ref _testsRun, ref _testsPassed, ref _testsFailed);

            data.IsOpener = false;
            data.IsFinisher = true;
            row = ActionDataToSpreadsheetJsonConverter.Merge(data, null);
            TestBase.AssertEqual("", row.Opener, "Opener false should write empty", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("true", row.Finisher, "Finisher true should write as true", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Zero roll bonuses must clear spreadsheet cells, not fall back to baseRow (fixes Settings Save revert).
        /// </summary>
        private static void TestMergeHeroRollZeroClearsBaseRow()
        {
            System.Console.WriteLine("--- Merge hero roll zero clears base row ---");
            var baseRow = new SpreadsheetActionJson
            {
                Action = "JAB",
                HeroHit = "5",
                HeroCombo = "5",
                HeroAccuracy = "20",
                HeroCrit = "3"
            };
            var data = new ActionData
            {
                Name = "JAB",
                Type = "Attack",
                TargetType = "SingleTarget",
                DamageMultiplier = 1.0,
                Length = 1.0,
                MultiHitCount = 1,
                RollBonus = 0,
                HitThresholdAdjustment = 0,
                ComboThresholdAdjustment = 0,
                CriticalHitThresholdAdjustment = 0
            };
            var row = ActionDataToSpreadsheetJsonConverter.Merge(data, baseRow);
            TestBase.AssertEqual("", row.HeroHit, "Hit 0 must not keep base 5", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("", row.HeroCombo, "Combo 0 must not keep base 5", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("", row.HeroAccuracy, "Accuracy 0 must not keep base 20", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("", row.HeroCrit, "Crit 0 must not keep base 3", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMergeCritMissRoundTrip()
        {
            System.Console.WriteLine("--- Merge crit miss round-trip ---");
            var data = new ActionData
            {
                Name = "TEST",
                Type = "Attack",
                TargetType = "SingleTarget",
                DamageMultiplier = 1.0,
                Length = 1.0,
                MultiHitCount = 1,
                CriticalMissThresholdAdjustment = -2
            };
            var row = ActionDataToSpreadsheetJsonConverter.Merge(data, null);
            TestBase.AssertEqual("-2", row.HeroCritMiss, "Crit miss written to row", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var sheet = row.ToSpreadsheetActionData();
            var actionData = SpreadsheetToActionDataConverter.Convert(sheet);
            TestBase.AssertEqual(-2, actionData.CriticalMissThresholdAdjustment, "Crit miss loads back from sheet", ref _testsRun, ref _testsPassed, ref _testsFailed);

            data.CriticalMissThresholdAdjustment = 0;
            row = ActionDataToSpreadsheetJsonConverter.Merge(data, new SpreadsheetActionJson { Action = "TEST", HeroCritMiss = "-2" });
            TestBase.AssertEqual("", row.HeroCritMiss, "Crit miss 0 clears value", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMergeEnemyRollBonusesRoundTrip()
        {
            System.Console.WriteLine("--- Merge enemy roll bonuses round-trip ---");
            var data = new ActionData
            {
                Name = "ENEMY_STRIKE",
                Type = "Attack",
                TargetType = "SingleTarget",
                DamageMultiplier = 1.0,
                Length = 1.0,
                MultiHitCount = 1,
                EnemyRollBonus = -3,
                EnemyCriticalMissThresholdAdjustment = -1,
                EnemyHitThresholdAdjustment = 1,
                EnemyComboThresholdAdjustment = 2,
                EnemyCriticalHitThresholdAdjustment = -1
            };
            var row = ActionDataToSpreadsheetJsonConverter.Merge(data, null);
            TestBase.AssertEqual("-3", row.EnemyAccuracy, "Enemy accuracy", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("-1", row.EnemyCritMiss, "Enemy crit miss", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("1", row.EnemyHit, "Enemy hit", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("2", row.EnemyCombo, "Enemy combo", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("-1", row.EnemyCrit, "Enemy crit", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var sheet = row.ToSpreadsheetActionData();
            var back = SpreadsheetToActionDataConverter.Convert(sheet);
            TestBase.AssertEqual(-3, back.EnemyRollBonus, "EnemyRollBonus loads back", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(-1, back.EnemyCriticalMissThresholdAdjustment, "Enemy crit miss loads back", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMergeJumpRelativeRoundTrip()
        {
            System.Console.WriteLine("--- Merge jump relative round-trip ---");
            var data = new ActionData
            {
                Name = "TEST",
                Type = "Attack",
                TargetType = "SingleTarget",
                DamageMultiplier = 1.0,
                Length = 1.0,
                MultiHitCount = 1,
                Jump = "",
                JumpRelative = "1"
            };
            var row = ActionDataToSpreadsheetJsonConverter.Merge(data, null);
            TestBase.AssertEqual("1", row.JumpRelative, "JumpRelative written to row", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var sheet = row.ToSpreadsheetActionData();
            var back = SpreadsheetToActionDataConverter.Convert(sheet);
            TestBase.AssertEqual("1", back.JumpRelative, "JumpRelative loads back from sheet", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var action = ActionDataToActionMapper.CreateAction(back);
            TestBase.AssertTrue(action.ComboRouting.JumpRelativeSlots == 1, "Mapper sets JumpRelativeSlots", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(action.ComboRouting.JumpToSlot == 0, "Absolute jump stays off when Jump empty", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSpreadsheetEnvironmentRowClearsWeaponsAndSetsDebuffAoe()
        {
            System.Console.WriteLine("--- Spreadsheet environment row: strip weapons, Debuff + AoE ---");
            var sheet = new SpreadsheetActionData
            {
                Action = "TEST_ENV_ROW",
                Description = "Test hazard",
                Damage = "120%",
                Speed = "2.5",
                Target = "AOE",
                Category = "ENVIRONMENT",
                Tags = "environment, crypt",
                WeaponTypes = "Sword, Mace"
            };
            var data = SpreadsheetToActionDataConverter.Convert(sheet);
            TestBase.AssertTrue(data.WeaponTypes == null || data.WeaponTypes.Count == 0, "WeaponTypes cleared for environment row", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Debuff", data.Type, "Type becomes Debuff", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("AreaOfEffect", data.TargetType, "Target is AreaOfEffect", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!data.IsComboAction, "Not a combo action", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(-1, data.ComboOrder, "ComboOrder -1 for hazards", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSpreadsheetEnemyRowClearsWeaponTypes()
        {
            System.Console.WriteLine("--- Spreadsheet enemy row: strip weapon types ---");
            var sheet = new SpreadsheetActionData
            {
                Action = "TEST_ENEMY_ROW",
                Damage = "90%",
                Speed = "1.00",
                Target = "ENEMY",
                Category = "ENEMY",
                Tags = "enemy",
                WeaponTypes = "Dagger"
            };
            var data = SpreadsheetToActionDataConverter.Convert(sheet);
            TestBase.AssertTrue(data.WeaponTypes == null || data.WeaponTypes.Count == 0, "WeaponTypes cleared for enemy row", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Attack", data.Type, "Enemy strike remains Attack by default", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
