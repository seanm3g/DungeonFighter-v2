using System;
using RPGGame.Actions.RollModification;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    public static class NaiveteThresholdBonusesTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== NaiveteThresholdBonuses Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestComputeNaiveteAndHitSteps(ref run, ref passed, ref failed);
            TestEnemyHasNoNaivete(ref run, ref passed, ref failed);
            TestApplyAdjustsHitThreshold(ref run, ref passed, ref failed);
            TestDisabledConfig(ref run, ref passed, ref failed);

            TestBase.PrintSummary("NaiveteThresholdBonuses Tests", run, passed, failed);
        }

        private static void TestComputeNaiveteAndHitSteps(ref int runRef, ref int passedRef, ref int failedRef)
        {
            var l1 = TestDataBuilders.Character().WithName("NaiveHero").WithLevel(1).WithStats(3, 3, 3, 3).Build();
            TestBase.AssertEqual(3, NaiveteBalanceHelper.ComputeNaivete(l1), "L1 hero: naivete = 3", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(3, NaiveteBalanceHelper.GetHitSteps(l1), "L1 hero: +3 hit steps", ref runRef, ref passedRef, ref failedRef);

            var l2 = TestDataBuilders.Character().WithName("L2Hero").WithLevel(2).WithStats(3, 3, 3, 3).Build();
            TestBase.AssertEqual(2, NaiveteBalanceHelper.ComputeNaivete(l2), "L2 hero: naivete = 2", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(2, NaiveteBalanceHelper.GetHitSteps(l2), "L2 hero: +2 hit steps", ref runRef, ref passedRef, ref failedRef);

            var l3 = TestDataBuilders.Character().WithName("L3Hero").WithLevel(3).WithStats(15, 15, 15, 15).Build();
            TestBase.AssertEqual(1, NaiveteBalanceHelper.ComputeNaivete(l3), "L3 hero: naivete = 1", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(1, NaiveteBalanceHelper.GetHitSteps(l3), "L3 hero: +1 hit step", ref runRef, ref passedRef, ref failedRef);

            var l4 = TestDataBuilders.Character().WithName("L4Hero").WithLevel(4).WithStats(16, 17, 16, 17).Build();
            TestBase.AssertEqual(0, NaiveteBalanceHelper.ComputeNaivete(l4), "L4+ hero: naivete = 0", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(0, NaiveteBalanceHelper.GetHitSteps(l4), "L4+ hero: no hit steps", ref runRef, ref passedRef, ref failedRef);
        }

        private static void TestEnemyHasNoNaivete(ref int runRef, ref int passedRef, ref int failedRef)
        {
            var enemy = TestDataBuilders.Enemy().WithName("Goblin").WithStats(3, 3, 3, 3).Build();
            TestBase.AssertEqual(0, NaiveteBalanceHelper.ComputeNaivete(enemy), "Enemy: naivete = 0", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(0, NaiveteThresholdBonuses.GetHitSteps(enemy), "Enemy: no hit steps", ref runRef, ref passedRef, ref failedRef);
        }

        private static void TestApplyAdjustsHitThreshold(ref int runRef, ref int passedRef, ref int failedRef)
        {
            var tm = RollModificationManager.GetThresholdManager();
            tm.Clear();

            var c = TestDataBuilders.Character().WithName("NaiveFighter").WithLevel(1).WithStats(3, 3, 3, 3).Build();
            tm.ResetThresholds(c);
            int baseHit = tm.GetHitThreshold(c);

            NaiveteThresholdBonuses.Apply(tm, c);
            TestBase.AssertEqual(baseHit - 3, tm.GetHitThreshold(c), "Apply(L1) lowers hit threshold by 3", ref runRef, ref passedRef, ref failedRef);
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
                TestBase.AssertEqual(0, NaiveteBalanceHelper.GetHitSteps(c), "Disabled: no hit steps", ref runRef, ref passedRef, ref failedRef);
            }
            finally
            {
                cfg.EarlyGame.Naivete.Enabled = savedEnabled;
            }
        }
    }
}
