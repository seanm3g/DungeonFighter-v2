using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions;
using RPGGame.Actions.Execution;
using RPGGame.Actions.RollModification;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.Utils;

namespace RPGGame.Tests.Unit.Data.ActionBonusMechanics
{
    public static class ActionBonusScopedCadenceTests
    {
        public static void RunAll(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            TestFightCadence_PersistsAcrossRolls();
            TestFightCadence_ClearsOnEncounterEnd();
            TestDungeonCadence_PersistsAcrossEncounters();
            TestDungeonCadence_StatBonusSurvivesEncounterClear();
            TestDungeonCadence_ClearsOnDungeonRunEnd();

            testsRun += _testsRun;
            testsPassed += _testsPassed;
            testsFailed += _testsFailed;
        }

        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;



        private static void TestFightCadence_PersistsAcrossRolls()
        {
            Console.WriteLine("\n--- FIGHT cadence: bonus persists across rolls in combat ---\n");
            var character = new Character("FightHero", 1);
            character.Effects.AddActionAttackBonuses(new ActionAttackBonuses
            {
                BonusGroups = new List<ActionAttackBonusGroup>
                {
                    new ActionAttackBonusGroup
                    {
                        CadenceType = "FIGHT",
                        DurationType = "FIGHT",
                        Count = 1,
                        Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "ACCURACY", Value = 1 } }
                    }
                }
            }, null, character);

            TestBase.AssertTrue(character.FightCadenceBuffs.HasAny,
                "FIGHT grant deposits into FightCadenceBuffs",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(character.Effects.PeekTurnBonuses().Count == 0,
                "FIGHT grant does not use TURN queue",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            int accBefore = ActionSelector.PeekQueuedAccuracyBonus(character);
            character.Effects.GetAndConsumeTurnBonuses();
            int accAfterConsume = ActionSelector.PeekQueuedAccuracyBonus(character);
            character.Effects.GetAndConsumeTurnBonuses();
            int accSecondRoll = ActionSelector.PeekQueuedAccuracyBonus(character);

            TestBase.AssertEqual(1, accBefore, "FIGHT +1 ACC visible before rolls", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, accAfterConsume, "FIGHT +1 ACC persists after first roll consume", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, accSecondRoll, "FIGHT +1 ACC persists after second roll consume", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }


        private static void TestFightCadence_ClearsOnEncounterEnd()
        {
            Console.WriteLine("\n--- FIGHT cadence: clears on encounter end ---\n");
            var character = new Character("FightHero", 1);
            CadenceScopedBuffApplicator.DepositToScope(character, "FIGHT",
                new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "ACCURACY", Value = 2 } });
            TestBase.AssertTrue(character.FightCadenceBuffs.HasAny, "Fight buff present", ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.ClearEncounterTempEffects();
            TestBase.AssertTrue(!character.FightCadenceBuffs.HasAny,
                "ClearEncounterTempEffects clears fight-scoped bonuses",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, ActionSelector.PeekQueuedAccuracyBonus(character),
                "No fight ACC after encounter clear", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }


        private static void TestDungeonCadence_PersistsAcrossEncounters()
        {
            Console.WriteLine("\n--- DUNGEON cadence: bonus survives encounter clear ---\n");
            var character = new Character("DungeonHero", 1);
            character.Effects.AddActionAttackBonuses(new ActionAttackBonuses
            {
                BonusGroups = new List<ActionAttackBonusGroup>
                {
                    new ActionAttackBonusGroup
                    {
                        CadenceType = "DUNGEON",
                        DurationType = "DUNGEON",
                        Count = 1,
                        Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "ACCURACY", Value = 1 } }
                    }
                }
            }, null, character);

            character.ClearEncounterTempEffects();
            TestBase.AssertTrue(character.DungeonCadenceBuffs.HasAny,
                "Dungeon cadence survives encounter clear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, ActionSelector.PeekQueuedAccuracyBonus(character),
                "Dungeon +1 ACC still peeked after encounter clear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }


        private static void TestDungeonCadence_StatBonusSurvivesEncounterClear()
        {
            Console.WriteLine("\n--- DUNGEON cadence: stat bonus survives encounter clear ---\n");
            var character = new Character("DungeonHero", 1);
            int baseStr = character.GetEffectiveStrength();
            CadenceScopedBuffApplicator.DepositToScope(character, "DUNGEON",
                new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "STR", Value = 1 } });

            TestBase.AssertEqual(baseStr + 1, character.GetEffectiveStrength(),
                "Dungeon +1 STR applied", ref _testsRun, ref _testsPassed, ref _testsFailed);
            character.ClearEncounterTempEffects();
            TestBase.AssertEqual(baseStr + 1, character.GetEffectiveStrength(),
                "Dungeon +1 STR survives encounter clear", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }


        private static void TestDungeonCadence_ClearsOnDungeonRunEnd()
        {
            Console.WriteLine("\n--- DUNGEON cadence: clears on dungeon run end ---\n");
            var character = new Character("DungeonHero", 1);
            CadenceScopedBuffApplicator.DepositToScope(character, "DUNGEON",
                new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = "ACCURACY", Value = 3 } });
            TestBase.AssertTrue(character.DungeonCadenceBuffs.HasAny, "Dungeon buff present", ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.ClearDungeonRunTempEffects();
            TestBase.AssertTrue(!character.DungeonCadenceBuffs.HasAny,
                "ClearDungeonRunTempEffects clears dungeon-scoped bonuses",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
