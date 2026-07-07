using System;
using System.Linq;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class ActionMechanicsCadenceMatrixTests
    {
        public static void RunAll()
        {
            Console.WriteLine("\n=== ActionMechanicsCadenceMatrix Tests ===");
            TestHeroDiceDefaultsAttack();
            TestHeroNextActionModDefaultsAction();
            TestHeroNextActionDamageAllowsFightAndDungeon();
            TestEnemyNextActionModDefaultsAction();
            TestInstantStatusDoesNotForceCadence();
            TestResolveDefaultCadenceMixedRowPrefersAttack();
            TestCadenceValidationRejectsInvalidKeyword();
            TestRedundantMechanicsNotDetected();
            TestItemOnlyStatusEffectsNotDetectedFromActionRow();
            TestCutMechanicsStillDetectedInternally();
        }

        private static void TestHeroDiceDefaultsAttack()
        {
            var row = new SpreadsheetActionData { HeroCombo = "2" };
            ActionMechanicsSheetSync.ApplyCadenceDefaultsForMechanics(row);
            TestHarnessBase.AssertEqual("TURN", row.Cadence, "hero dice blank cadence -> ATTACK");
            TestHarnessBase.AssertEqual("1", row.Duration, "duration 1");
        }

        private static void TestHeroNextActionModDefaultsAction()
        {
            var row = new SpreadsheetActionData { SpeedMod = "10" };
            ActionMechanicsSheetSync.ApplyCadenceDefaultsForMechanics(row);
            TestHarnessBase.AssertEqual("ACTION", row.Cadence, "hero next-action mod blank cadence -> ACTION");
        }

        private static void TestHeroNextActionDamageAllowsFightAndDungeon()
        {
            var allowed = ActionMechanicsRegistry.GetAllowedCadencesForMechanic("hero_next_action_damage");
            TestHarnessBase.AssertTrue(allowed.Contains("ACTION"), "damage mod allows ACTION");
            TestHarnessBase.AssertTrue(allowed.Contains("DUNGEON"), "damage mod allows DUNGEON");
            TestHarnessBase.AssertFalse(allowed.Contains("FIGHT"), "damage mod does not allow FIGHT");
            TestHarnessBase.AssertFalse(allowed.Contains("TURN"), "damage mod does not allow ATTACK");
        }

        private static void TestEnemyNextActionModDefaultsAction()
        {
            var row = new SpreadsheetActionData { EnemyDamageMod = "15" };
            ActionMechanicsSheetSync.ApplyCadenceDefaultsForMechanics(row);
            TestHarnessBase.AssertEqual("ACTION", row.Cadence, "enemy next-action mod -> ACTION");

            var allowed = ActionMechanicsRegistry.GetAllowedCadencesForMechanic("enemy_next_action_damage");
            TestHarnessBase.AssertFalse(allowed.Contains("FIGHT"), "enemy damage mod no FIGHT");
            TestHarnessBase.AssertFalse(allowed.Contains("DUNGEON"), "enemy damage mod no DUNGEON");
        }

        private static void TestItemOnlyStatusEffectsNotDetectedFromActionRow()
        {
            var row = new SpreadsheetActionData { Stun = "1", Poison = "1", Burn = "1", Bleed = "1" };
            var detected = ActionMechanicsRegistry.DetectFromSpreadsheetRow(row);
            TestHarnessBase.AssertFalse(detected.Contains("stun"), "stun not an action-sheet mechanic");
            TestHarnessBase.AssertFalse(detected.Contains("poison"), "poison not an action-sheet mechanic");
            TestHarnessBase.AssertFalse(detected.Contains("burn"), "burn not an action-sheet mechanic");
            TestHarnessBase.AssertFalse(detected.Contains("bleed"), "bleed not an action-sheet mechanic");
        }

        private static void TestInstantStatusDoesNotForceCadence()
        {
            var row = new SpreadsheetActionData { Weaken = "1" };
            ActionMechanicsSheetSync.ApplyCadenceDefaultsForMechanics(row);
            TestHarnessBase.AssertTrue(string.IsNullOrWhiteSpace(row.Cadence), "weaken alone does not set CADENCE column");
        }

        private static void TestResolveDefaultCadenceMixedRowPrefersAttack()
        {
            var ids = new[] { "hero_hit_threshold", "hero_next_action_speed" };
            TestHarnessBase.AssertEqual("TURN", ActionMechanicsRegistry.ResolveDefaultCadence(ids),
                "mixed turn-scoped + next-action -> ATTACK");
        }

        private static void TestCadenceValidationRejectsInvalidKeyword()
        {
            var row = new SpreadsheetActionData
            {
                Action = "BAD CADENCE",
                HeroCombo = "1",
                Cadence = "ACTION",
            };
            string? warning = ActionMechanicsRegistry.ValidateCadenceForRow(row);
            TestHarnessBase.AssertTrue(warning != null && warning.Contains("ACTION"), "hero_combo + ACTION cadence warns");
        }

        private static void TestRedundantMechanicsNotDetected()
        {
            var row = new SpreadsheetActionData
            {
                Damage = "150%",
                NumberOfHits = "3",
                AccumulationsJson = "[{\"type\":\"HitsLanded\"}]",
            };
            var detected = ActionMechanicsRegistry.DetectFromSpreadsheetRow(row);
            TestHarnessBase.AssertFalse(detected.Contains("damage"), "damage not a mechanic ID");
            TestHarnessBase.AssertFalse(detected.Contains("multi_hit"), "multi_hit not a mechanic ID");
            TestHarnessBase.AssertFalse(detected.Contains("accumulations"), "accumulations not a mechanic ID");
        }

        private static void TestCutMechanicsStillDetectedInternally()
        {
            var row = new SpreadsheetActionData { Silence = "1", Lifesteal = "10%", DiceRolls = "2" };
            var detected = ActionMechanicsRegistry.DetectFromSpreadsheetRow(row);
            TestHarnessBase.AssertTrue(detected.Contains("silence"), "CUT silence still detected internally");
            var filtered = ActionMechanicsRegistry.FilterForMechanicsColumn(detected);
            TestHarnessBase.AssertFalse(filtered.Contains("silence"), "CUT silence not in MECHANICS column");
        }
    }
}
