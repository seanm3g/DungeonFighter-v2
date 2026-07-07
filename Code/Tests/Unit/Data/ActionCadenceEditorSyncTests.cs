using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class ActionCadenceEditorSyncTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionCadenceEditorSync Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            var action = new ActionData
            {
                Name = "SyncTest",
                Cadence = "Turn",
                ComboBonusDuration = 3,
                ComboThresholdAdjustment = 1,
                DamageMod = "25"
            };

            var blocks = ActionCadenceEditorSync.LoadBlocks(action);
            TestBase.AssertTrue(blocks.Count >= 1
                && blocks[0].Mechanics.Any(m => m.MechanicId == "hero_combo_threshold" && m.Quantity == 1),
                "LoadBlocks infers TURN combo threshold from flat fields",
                ref run, ref passed, ref failed);

            var edited = new List<CadenceEditorBlock>
            {
                new CadenceEditorBlock
                {
                    Cadence = "Turn",
                    Duration = 3,
                    Mechanics = new List<CadenceMechanicRow>
                    {
                        new CadenceMechanicRow { MechanicId = "hero_combo_threshold", Quantity = 1 }
                    }
                },
                new CadenceEditorBlock
                {
                    Cadence = "Action",
                    Duration = 3,
                    Mechanics = new List<CadenceMechanicRow>
                    {
                        new CadenceMechanicRow { MechanicId = "hero_next_action_damage", Quantity = 25 }
                    }
                }
            };

            ActionCadenceEditorSync.ApplyBlocks(action, edited);
            TestBase.AssertTrue(action.ComboBonusDuration == 3
                && action.ComboThresholdAdjustment == 1
                && string.IsNullOrEmpty(action.DamageMod)
                && action.ActionAttackBonuses?.BonusGroups?.Count == 2,
                "ApplyBlocks syncs primary block to flat fields and persists multi-block bonuses",
                ref run, ref passed, ref failed);

            var reloaded = ActionCadenceEditorSync.LoadBlocks(action);
            TestBase.AssertTrue(reloaded.Count == 2
                && reloaded[1].Mechanics.Any(m => m.MechanicId == "hero_next_action_damage" && m.Quantity == 25),
                "LoadBlocks round-trips multi-block ActionAttackBonuses",
                ref run, ref passed, ref failed);

            Console.WriteLine($"\nActionCadenceEditorSync: {passed}/{run} passed, {failed} failed\n");
        }
    }
}
