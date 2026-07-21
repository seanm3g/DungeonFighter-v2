using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Actions.Conditional;
using RPGGame.Combat.Events;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    public static class RollProbabilityContentTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Roll Probability Content Tests ===\n");
            _run = _passed = _failed = 0;
            CombatTriggerContext.ResetForBattle();

            TestNaturalRollGateVsAttackTotal();
            TestMissSalvageCharges();
            TestReplaceNextRollFace();
            TestCritFaceMin();
            TestReplaceNextRollNotDisadvantageDetect();

            CombatTriggerContext.ResetForBattle();
            TestBase.PrintSummary("Roll Probability Content Tests", _run, _passed, _failed);
        }

        private static void TestNaturalRollGateVsAttackTotal()
        {
            TestBase.SetCurrentTestName(nameof(TestNaturalRollGateVsAttackTotal));
            var hero = new Character("ProbHero", 5);
            var action = new Action
            {
                Name = "Lucky",
                Triggers = new ConditionalTriggerProperties
                {
                    ExactRollTriggerValue = 7,
                    TriggerConditions = new List<string> { "ONNATURALROLL:7" }
                }
            };
            var evt = new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Action = action,
                RollValue = 12, // attack total
                NaturalRollValue = 7
            };
            TestBase.AssertTrue(
                ActionTriggerGate.MatchesConditionToken("ONNATURALROLL:7", action, evt),
                "ONNATURALROLL matches natural face", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                !ActionTriggerGate.MatchesConditionToken("ONROLLVALUE:7", action, evt),
                "ONROLLVALUE uses attack total (12 ≠ 7)", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                ActionTriggerGate.MatchesConditionToken("ONROLLVALUE:12", action, evt),
                "ONROLLVALUE matches attack total", ref _run, ref _passed, ref _failed);
        }

        private static void TestMissSalvageCharges()
        {
            TestBase.SetCurrentTestName(nameof(TestMissSalvageCharges));
            CombatTriggerContext.ResetForBattle();
            var hero = new Character("SalvageHero", 5);
            CombatTriggerContext.AddMissSalvageCharges(hero, 1);
            TestBase.AssertEqual(1, CombatTriggerContext.GetMissSalvageCharges(hero), "one charge", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(CombatTriggerContext.TryConsumeMissSalvage(hero), "consume once", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!CombatTriggerContext.TryConsumeMissSalvage(hero), "no second consume", ref _run, ref _passed, ref _failed);
        }

        private static void TestReplaceNextRollFace()
        {
            TestBase.SetCurrentTestName(nameof(TestReplaceNextRollFace));
            CombatTriggerContext.ResetForBattle();
            var hero = new Character("ReplaceHero", 5);
            CombatTriggerContext.SetPendingReplaceRollFace(hero, 20);
            TestBase.AssertTrue(CombatTriggerContext.TryConsumePendingReplaceRollFace(hero, out int face) && face == 20,
                "replace face 20", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!CombatTriggerContext.TryConsumePendingReplaceRollFace(hero, out _),
                "one-shot replace", ref _run, ref _passed, ref _failed);
        }

        private static void TestCritFaceMin()
        {
            TestBase.SetCurrentTestName(nameof(TestCritFaceMin));
            CombatTriggerContext.ResetForBattle();
            var hero = new Character("CritFaceHero", 5);
            CombatTriggerContext.SetCritFaceMin(hero, 19);
            TestBase.AssertTrue(CombatTriggerContext.TryGetCritFaceMin(hero, out int min) && min == 19,
                "crit face min 19", ref _run, ref _passed, ref _failed);
        }

        private static void TestReplaceNextRollNotDisadvantageDetect()
        {
            TestBase.SetCurrentTestName(nameof(TestReplaceNextRollNotDisadvantageDetect));
            var row = new SpreadsheetActionData
            {
                Action = "TEST",
                ReplaceNextRoll = "20",
                HighestLowestRoll = ""
            };
            var ids = ActionMechanicsRegistry.DetectFromSpreadsheetRow(row);
            TestBase.AssertTrue(ids.Contains("replace_next_roll"), "detects replace_next_roll", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!ids.Contains("disadvantage"), "replaceNextRoll is not disadvantage", ref _run, ref _passed, ref _failed);
        }
    }
}
