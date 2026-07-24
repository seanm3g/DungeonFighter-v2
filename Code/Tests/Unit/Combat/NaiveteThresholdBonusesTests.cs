using System;
using System.Collections.Generic;
using RPGGame.Actions.Conditional;
using RPGGame.Actions.Execution;
using RPGGame.Actions.RollModification;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    public static class NaiveteThresholdBonusesTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Naivete Advantage (miss→advantage) Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestComputeMaxByLevel(ref run, ref passed, ref failed);
            TestEnemyHasNoNaivete(ref run, ref passed, ref failed);
            TestNoHitThresholdSteps(ref run, ref passed, ref failed);
            TestDisabledConfig(ref run, ref passed, ref failed);
            TestMissConsumesChargeAndAdvantageCanHit(ref run, ref passed, ref failed);
            TestChainedMissesDrainCharges(ref run, ref passed, ref failed);
            TestZeroChargesNoAdvantage(ref run, ref passed, ref failed);

            TestBase.PrintSummary("Naivete Advantage Tests", run, passed, failed);
        }

        private static void TestComputeMaxByLevel(ref int runRef, ref int passedRef, ref int failedRef)
        {
            var cfg = GameConfiguration.Instance.EarlyGame.Naivete;
            int savedStart = cfg.StartingNaivete;
            try
            {
                cfg.StartingNaivete = 5;

                var l1 = TestDataBuilders.Character().WithName("NaiveHero").WithLevel(1).WithStats(3, 3, 3, 3).Build();
                TestBase.AssertEqual(5, NaiveteBalanceHelper.ComputeNaivete(l1), "L1 hero: naivete max = 5", ref runRef, ref passedRef, ref failedRef);

                var l2 = TestDataBuilders.Character().WithName("L2Hero").WithLevel(2).WithStats(3, 3, 3, 3).Build();
                TestBase.AssertEqual(4, NaiveteBalanceHelper.ComputeNaivete(l2), "L2 hero: naivete max = 4", ref runRef, ref passedRef, ref failedRef);

                var l5 = TestDataBuilders.Character().WithName("L5Hero").WithLevel(5).WithStats(3, 3, 3, 3).Build();
                TestBase.AssertEqual(1, NaiveteBalanceHelper.ComputeNaivete(l5), "L5 hero: naivete max = 1", ref runRef, ref passedRef, ref failedRef);

                var l6 = TestDataBuilders.Character().WithName("L6Hero").WithLevel(6).WithStats(16, 17, 16, 17).Build();
                TestBase.AssertEqual(0, NaiveteBalanceHelper.ComputeNaivete(l6), "L6+ hero: naivete max = 0", ref runRef, ref passedRef, ref failedRef);
            }
            finally
            {
                cfg.StartingNaivete = savedStart;
            }
        }

        private static void TestEnemyHasNoNaivete(ref int runRef, ref int passedRef, ref int failedRef)
        {
            var enemy = TestDataBuilders.Enemy().WithName("Goblin").WithStats(3, 3, 3, 3).Build();
            TestBase.AssertEqual(0, NaiveteBalanceHelper.ComputeNaivete(enemy), "Enemy: naivete = 0", ref runRef, ref passedRef, ref failedRef);
            CombatTriggerContext.ResetForBattle();
            TestBase.AssertTrue(!CombatTriggerContext.TryConsumeNaiveteCharge(enemy), "Enemy: cannot consume naivete", ref runRef, ref passedRef, ref failedRef);
        }

        private static void TestNoHitThresholdSteps(ref int runRef, ref int passedRef, ref int failedRef)
        {
            var tm = RollModificationManager.GetThresholdManager();
            tm.Clear();

            var c = TestDataBuilders.Character().WithName("NaiveFighter").WithLevel(1).WithStats(3, 3, 3, 3).Build();
            tm.ResetThresholds(c);
            int baseHit = tm.GetHitThreshold(c);

            NaiveteThresholdBonuses.Apply(tm, c);
            TestBase.AssertEqual(baseHit, tm.GetHitThreshold(c), "Apply no longer shifts HIT threshold", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(0, NaiveteBalanceHelper.GetHitSteps(c), "GetHitSteps always 0", ref runRef, ref passedRef, ref failedRef);
        }

        private static void TestDisabledConfig(ref int runRef, ref int passedRef, ref int failedRef)
        {
            var cfg = GameConfiguration.Instance;
            bool savedEnabled = cfg.EarlyGame.Naivete.Enabled;
            try
            {
                cfg.EarlyGame.Naivete.Enabled = false;
                var c = TestDataBuilders.Character().WithName("DisabledNaivete").WithLevel(1).WithStats(3, 3, 3, 3).Build();
                TestBase.AssertEqual(0, NaiveteBalanceHelper.ComputeNaivete(c), "Disabled: naivete = 0", ref runRef, ref passedRef, ref failedRef);
            }
            finally
            {
                cfg.EarlyGame.Naivete.Enabled = savedEnabled;
            }
        }

        private static void TestMissConsumesChargeAndAdvantageCanHit(ref int runRef, ref int passedRef, ref int failedRef)
        {
            CombatTriggerContext.ResetForBattle();
            var cfg = GameConfiguration.Instance.EarlyGame.Naivete;
            int savedStart = cfg.StartingNaivete;
            cfg.StartingNaivete = 5;
            try
            {
                var hero = TestDataBuilders.Character().WithName("AdvHero").WithLevel(1).WithStats(3, 3, 3, 3).Build();
                var enemy = TestDataBuilders.Enemy().WithName("Target").WithLevel(1).Build();
                var action = TestDataBuilders.CreateMockAction("Swing", ActionType.Attack);
                action.IsComboAction = false;

                var tm = RollModificationManager.GetThresholdManager();
                tm.Clear();
                tm.ResetThresholds(hero);
                TechniqueMilestoneThresholdBonuses.Apply(tm, hero);

                int rollBonus = ActionUtilities.CalculateRollBonus(hero, action, consumeTempBonus: false);
                int hitTh = tm.GetHitThreshold(hero);
                int missFace = Math.Max(2, hitTh - rollBonus - 1);

                Dice.SetTestRoll(missFace);
                Dice.QueueUnforcedTestRolls(20);
                ActionSelector.SetStoredActionRoll(hero, missFace);
                var lastUsed = new Dictionary<Actor, Action>();
                var lastCrit = new Dictionary<Actor, bool>();
                var result = ActionExecutionFlow.Execute(hero, enemy, null, null, action, null, lastUsed, lastCrit);
                Dice.ClearTestRoll();
                Dice.ClearUnforcedTestRolls();

                TestBase.AssertTrue(result.Hit, "Miss + naiveté advantage (20) should hit", ref runRef, ref passedRef, ref failedRef);
                TestBase.AssertEqual(1, result.NaiveteAdvantageUses, "spent one naiveté charge", ref runRef, ref passedRef, ref failedRef);
                TestBase.AssertEqual(4, CombatTriggerContext.GetNaiveteCharges(hero), "4 charges remain of 5", ref runRef, ref passedRef, ref failedRef);
                TestBase.AssertTrue(result.MultiDiceRollDetail.Mode == MultiDiceLuckMode.Advantage,
                    "roll detail shows advantage", ref runRef, ref passedRef, ref failedRef);
            }
            finally
            {
                cfg.StartingNaivete = savedStart;
            }
        }

        private static void TestChainedMissesDrainCharges(ref int runRef, ref int passedRef, ref int failedRef)
        {
            CombatTriggerContext.ResetForBattle();
            var cfg = GameConfiguration.Instance.EarlyGame.Naivete;
            int savedStart = cfg.StartingNaivete;
            cfg.StartingNaivete = 5;
            try
            {
                var hero = TestDataBuilders.Character().WithName("ChainHero").WithLevel(1).WithStats(3, 3, 3, 3).Build();
                var enemy = TestDataBuilders.Enemy().WithName("Target").WithLevel(1).Build();
                var action = TestDataBuilders.CreateMockAction("Swing", ActionType.Attack);
                action.IsComboAction = false;

                var tm = RollModificationManager.GetThresholdManager();
                tm.Clear();
                tm.ResetThresholds(hero);
                TechniqueMilestoneThresholdBonuses.Apply(tm, hero);

                int rollBonus = ActionUtilities.CalculateRollBonus(hero, action, consumeTempBonus: false);
                int hitTh = tm.GetHitThreshold(hero);
                int missFace = Math.Max(2, hitTh - rollBonus - 1);

                Dice.SetTestRoll(missFace);
                // Two more misses, then a hit
                Dice.QueueUnforcedTestRolls(missFace, missFace, 20);
                ActionSelector.SetStoredActionRoll(hero, missFace);
                var lastUsed = new Dictionary<Actor, Action>();
                var lastCrit = new Dictionary<Actor, bool>();
                var result = ActionExecutionFlow.Execute(hero, enemy, null, null, action, null, lastUsed, lastCrit);
                Dice.ClearTestRoll();
                Dice.ClearUnforcedTestRolls();

                TestBase.AssertTrue(result.Hit, "third advantage die hits", ref runRef, ref passedRef, ref failedRef);
                TestBase.AssertEqual(3, result.NaiveteAdvantageUses, "three charges spent on chained misses", ref runRef, ref passedRef, ref failedRef);
                TestBase.AssertEqual(2, CombatTriggerContext.GetNaiveteCharges(hero), "2 of 5 remain", ref runRef, ref passedRef, ref failedRef);
            }
            finally
            {
                cfg.StartingNaivete = savedStart;
            }
        }

        private static void TestZeroChargesNoAdvantage(ref int runRef, ref int passedRef, ref int failedRef)
        {
            CombatTriggerContext.ResetForBattle();
            var hero = TestDataBuilders.Character().WithName("EmptyHero").WithLevel(6).WithStats(3, 3, 3, 3).Build();
            TestBase.AssertEqual(0, NaiveteBalanceHelper.ComputeNaivete(hero), "L6 max is 0", ref runRef, ref passedRef, ref failedRef);

            var enemy = TestDataBuilders.Enemy().WithName("Target").WithLevel(1).Build();
            var action = TestDataBuilders.CreateMockAction("Swing", ActionType.Attack);
            action.IsComboAction = false;

            var tm = RollModificationManager.GetThresholdManager();
            tm.Clear();
            tm.ResetThresholds(hero);

            int rollBonus = ActionUtilities.CalculateRollBonus(hero, action, consumeTempBonus: false);
            int hitTh = tm.GetHitThreshold(hero);
            int missFace = Math.Max(2, hitTh - rollBonus - 1);

            Dice.SetTestRoll(missFace);
            Dice.QueueUnforcedTestRolls(20);
            ActionSelector.SetStoredActionRoll(hero, missFace);
            var lastUsed = new Dictionary<Actor, Action>();
            var lastCrit = new Dictionary<Actor, bool>();
            var result = ActionExecutionFlow.Execute(hero, enemy, null, null, action, null, lastUsed, lastCrit);
            Dice.ClearTestRoll();
            Dice.ClearUnforcedTestRolls();

            TestBase.AssertFalse(result.Hit, "L6 with 0 naiveté stays a miss", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(0, result.NaiveteAdvantageUses, "no naiveté spends", ref runRef, ref passedRef, ref failedRef);
        }
    }
}
