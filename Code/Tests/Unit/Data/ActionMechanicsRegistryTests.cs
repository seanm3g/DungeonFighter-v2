using System;
using System.Linq;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class ActionMechanicsRegistryTests
    {
        public static void RunAll()
        {
            Console.WriteLine("\n=== ActionMechanicsRegistry Tests ===");
            TestMeasuredAdvanceDetectsComboThresholdAndDefaultsAttackOne();
            TestAngryEnragePreservesExplicitAttackDuration();
            TestSeparateCadenceDurationColumnsUnchanged();
            TestMechanicsValidationPassAndWarn();
            TestSyncRowBackfillsMechanicsOnPush();
            TestEnemyDiceAndModsDetectedSeparately();
            TestLegacyAliasNormalization();
            TestAllMechanicIdsTrimmedToGoodOnActions();
            TestEditorMechanicOrderHeroBeforeEnemy();
            TestCadenceAgnosticMechanicsAppearInDropdown();
            TestEditorDropdownLabelsActionSetStyle();
            TestItemOnlyStatusNotAppliedOnConvert();
            TestFortifyAndMaxHealthAppliedOnConvert();
        }

        private static void TestMeasuredAdvanceDetectsComboThresholdAndDefaultsAttackOne()
        {
            var row = new SpreadsheetActionData { Action = "MEASURED ADVANCE", HeroCombo = "1" };
            var detected = ActionMechanicsRegistry.DetectFromSpreadsheetRow(row);
            TestHarnessBase.AssertTrue(detected.Contains("hero_combo_threshold"),
                "heroCombo -> hero_combo_threshold");

            ActionMechanicsSheetSync.ApplyCadenceDefaultsForMechanics(row);
            TestHarnessBase.AssertEqual("TURN", row.Cadence, "Implicit dice row defaults ATTACK (turn bonus)");
            TestHarnessBase.AssertEqual("1", row.Duration, "Implicit cadence-gated row defaults duration 1");

            var data = SpreadsheetToActionDataConverter.Convert(row);
            TestHarnessBase.AssertEqual(1, data.ComboBonusDuration, "MEASURED ADVANCE ATTACK x1");
            TestHarnessBase.AssertEqual("TURN", data.Cadence, "MEASURED ADVANCE cadence ATTACK");
            TestHarnessBase.AssertTrue(data.Mechanics.Contains("hero_combo_threshold"), "Mechanics list includes hero_combo_threshold");
        }

        private static void TestAngryEnragePreservesExplicitAttackDuration()
        {
            var row = new SpreadsheetActionData
            {
                Action = "ANGRY ENRAGE",
                Cadence = "TURN",
                Duration = "3",
                HeroCombo = "5",
            };

            ActionMechanicsSheetSync.ApplyCadenceDefaultsForMechanics(row);
            TestHarnessBase.AssertEqual("TURN", row.Cadence, "Explicit ATTACK cadence preserved");
            TestHarnessBase.AssertEqual("3", row.Duration, "Explicit duration 3 preserved");

            var data = SpreadsheetToActionDataConverter.Convert(row);
            TestHarnessBase.AssertEqual(3, data.ComboBonusDuration, "ANGRY ENRAGE ATTACK x3");
        }

        private static void TestSeparateCadenceDurationColumnsUnchanged()
        {
            var row = new SpreadsheetActionData { Cadence = "TURN", Duration = "3", DamageMod = "10" };
            SpreadsheetDurationSemantics.NormalizeDurationAndCadence(row);
            TestHarnessBase.AssertEqual("TURN", row.Cadence, "Separate CADENCE column unchanged");
            TestHarnessBase.AssertEqual("3", row.Duration, "Separate DURATION count unchanged");
            TestHarnessBase.AssertEqual(3, SpreadsheetDurationSemantics.ResolveCadenceDuration(row),
                "Runtime resolves ATTACK x3 from separate columns");
        }

        private static void TestMechanicsValidationPassAndWarn()
        {
            var pass = new SpreadsheetActionData
            {
                Action = "STUN COMBO",
                Mechanics = "hero_combo_threshold",
                HeroCombo = "2",
            };
            TestHarnessBase.AssertEqual(0, ActionMechanicsRegistry.FindUndetectedDeclaredMechanics(pass).Count,
                "Declared mechanics match columns");

            var warn = new SpreadsheetActionData
            {
                Action = "MISSING COLUMN",
                Mechanics = "hero_combo_threshold",
            };
            var missing = ActionMechanicsRegistry.FindUndetectedDeclaredMechanics(warn);
            TestHarnessBase.AssertTrue(missing.Contains("hero_combo_threshold"), "Undeclared column flagged");
        }

        private static void TestSyncRowBackfillsMechanicsOnPush()
        {
            var row = new SpreadsheetActionData { Action = "PUSH SYNC", HeroHit = "2", Stun = "1" };
            ActionMechanicsSheetSync.SyncRow(row);
            TestHarnessBase.AssertTrue(row.Mechanics.Contains("hero_hit_threshold"), "Push sync detects hero_hit_threshold");
            TestHarnessBase.AssertFalse(row.Mechanics.Contains("weaken"), "weaken excluded when column empty on push sync");
            TestHarnessBase.AssertEqual("TURN", row.Cadence, "Push sync writes ATTACK default for dice mod");
            TestHarnessBase.AssertEqual("1", row.Duration, "Push sync writes duration 1");
        }

        private static void TestEnemyDiceAndModsDetectedSeparately()
        {
            var row = new SpreadsheetActionData
            {
                Action = "DEBUFF ENEMY",
                EnemyCombo = "2",
                EnemyDamageMod = "15",
                EnemySTR = "1",
            };
            var detected = ActionMechanicsRegistry.DetectFromSpreadsheetRow(row);
            TestHarnessBase.AssertTrue(detected.Contains("enemy_combo_threshold"), "enemy COMBO detected");
            TestHarnessBase.AssertTrue(detected.Contains("enemy_next_action_damage"), "enemy damage mod detected");
            TestHarnessBase.AssertTrue(detected.Contains("enemy_stat_bonus"), "enemy stat bonus detected");
            TestHarnessBase.AssertFalse(detected.Contains("hero_combo_threshold"), "hero combo not inferred from enemy columns");
        }

        private static void TestLegacyAliasNormalization()
        {
            var parsed = ActionMechanicsRegistry.ParseMechanicsCell("combo_threshold");
            TestHarnessBase.AssertTrue(parsed.Contains("hero_combo_threshold"), "legacy combo_threshold -> hero_combo_threshold");

            var row = new SpreadsheetActionData
            {
                Action = "LEGACY CELL",
                Mechanics = "combo_threshold",
                HeroCombo = "1",
            };
            TestHarnessBase.AssertEqual(0, ActionMechanicsRegistry.FindUndetectedDeclaredMechanics(row).Count,
                "legacy declared ID matches hero column detection");
        }

        private static void TestAllMechanicIdsTrimmedToGoodOnActions()
        {
            TestHarnessBase.AssertEqual(35, ActionMechanicsRegistry.AllMechanicIds.Count, "MECHANIC_LIST has 35 GOOD+ON ACTIONS IDs");
            TestHarnessBase.AssertFalse(ActionMechanicsRegistry.AllMechanicIds.Contains("damage"), "REDUNDANT damage excluded");
            TestHarnessBase.AssertFalse(ActionMechanicsRegistry.AllMechanicIds.Contains("silence"), "CUT silence excluded");
            TestHarnessBase.AssertFalse(ActionMechanicsRegistry.AllMechanicIds.Contains("stun"), "item-only stun excluded from MECHANIC_LIST");
            TestHarnessBase.AssertTrue(ActionMechanicsRegistry.ItemAppliedStatusEffectIds.Contains("poison"), "poison listed as item-applied");
            TestHarnessBase.AssertTrue(ActionMechanicsRegistry.AllMechanicIds.Contains("hero_combo_threshold"), "hero_combo_threshold included");
        }

        private static void TestEditorMechanicOrderHeroBeforeEnemy()
        {
            var turnIds = ActionMechanicsRegistry.GetMechanicIdsForCadence("Turn");
            int firstEnemy = turnIds.ToList().FindIndex(id => id.StartsWith("enemy_", StringComparison.OrdinalIgnoreCase));
            int lastHero = turnIds.ToList().FindLastIndex(id => id.StartsWith("hero_", StringComparison.OrdinalIgnoreCase));
            TestHarnessBase.AssertTrue(firstEnemy < 0 || lastHero < firstEnemy,
                "hero mechanics appear before enemy mechanics in editor dropdown");

            TestHarnessBase.AssertTrue(
                ActionMechanicsRegistry.CompareMechanicIdsForEditor("hero_accuracy", "enemy_accuracy") < 0,
                "hero_accuracy sorts before enemy_accuracy");
            TestHarnessBase.AssertTrue(
                ActionMechanicsRegistry.CompareMechanicIdsForEditor("weaken", "enemy_accuracy") < 0,
                "neutral mechanics sort between hero and enemy groups");
        }

        private static void TestCadenceAgnosticMechanicsAppearInDropdown()
        {
            Console.WriteLine("--- Timing-agnostic mechanics stay in every cadence dropdown ---");
            foreach (string cadence in ActionMechanicsRegistry.EditorCadenceOptions)
            {
                var ids = ActionMechanicsRegistry.GetMechanicIdsForCadence(cadence);
                TestHarnessBase.AssertTrue(ids.Contains("heal"), $"heal listed for {cadence}");
                TestHarnessBase.AssertTrue(ids.Contains("disrupt"), $"disrupt listed for {cadence}");
            }

            var turnIds = ActionMechanicsRegistry.GetMechanicIdsForCadence("Turn");
            TestHarnessBase.AssertTrue(turnIds.Contains("hero_accuracy"), "TURN lists hero_accuracy");

            var actionIds = ActionMechanicsRegistry.GetMechanicIdsForCadence("Action");
            TestHarnessBase.AssertTrue(actionIds.Contains("hero_next_action_damage"), "ACTION lists hero_next_action_damage");
            TestHarnessBase.AssertFalse(actionIds.Contains("hero_accuracy"), "ACTION does not list turn-only hero_accuracy");
        }

        private static void TestEditorDropdownLabelsActionSetStyle()
        {
            Console.WriteLine("--- Editor dropdown labels match Action-set short style ---");
            TestHarnessBase.AssertEqual("Hero ACC", ActionMechanicsRegistry.GetEditorDropdownLabel("hero_accuracy"),
                "hero accuracy short label");
            TestHarnessBase.AssertEqual("Enemy DAMAGE", ActionMechanicsRegistry.GetEditorDropdownLabel("enemy_next_action_damage"),
                "enemy damage short label");
            TestHarnessBase.AssertEqual("WEAKEN", ActionMechanicsRegistry.GetEditorDropdownLabel("weaken"),
                "neutral short label");
            TestHarnessBase.AssertEqual("Hero STR", ActionMechanicsRegistry.GetEditorDropdownLabel("hero_stat_bonus", "STR"),
                "stat subtype in short label");
        }

        private static void TestItemOnlyStatusNotAppliedOnConvert()
        {
            var row = new SpreadsheetActionData
            {
                Action = "ITEM ONLY STATUS",
                Stun = "1",
                Poison = "5",
                Burn = "3",
                Bleed = "2",
                Weaken = "1",
            };
            var data = SpreadsheetToActionDataConverter.Convert(row);
            TestHarnessBase.AssertFalse(data.CausesStun, "stun column not applied on pull");
            TestHarnessBase.AssertFalse(data.CausesPoison, "poison column not applied on pull");
            TestHarnessBase.AssertFalse(data.CausesBurn, "burn column not applied on pull");
            TestHarnessBase.AssertFalse(data.CausesBleed, "bleed column not applied on pull");
            TestHarnessBase.AssertTrue(data.CausesWeaken, "weaken still applied from action sheet");
        }

        private static void TestFortifyAndMaxHealthAppliedOnConvert()
        {
            var fortifyRow = new SpreadsheetActionData { Action = "IRON SKIN", Fortify = "2", Target = "self" };
            var fortifyData = SpreadsheetToActionDataConverter.Convert(fortifyRow);
            TestHarnessBase.AssertTrue(fortifyData.CausesFortify, "FORTIFY column maps to CausesFortify");
            TestHarnessBase.AssertEqual(2, fortifyData.FortifyArmorPerStack, "FORTIFY magnitude preserved");

            var maxHpRow = new SpreadsheetActionData { Action = "VITALITY", HeroHealMaxHealth = "15" };
            var maxHpData = SpreadsheetToActionDataConverter.Convert(maxHpRow);
            TestHarnessBase.AssertEqual(15, maxHpData.MaxHealthIncrease, "MAX HEALTH column maps to MaxHealthIncrease");
            TestHarnessBase.AssertEqual("Heal", maxHpData.Type, "max-health-only row is Heal type");
        }
    }
}
