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
            var l1 = TestDataBuilders.Character().WithName("NaiveHero").WithStats(3, 3, 3, 3).Build();
            TestBase.AssertEqual(54, NaiveteBalanceHelper.ComputeNaivete(l1), "L1 hero: naivete = 54", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(4, NaiveteBalanceHelper.GetHitSteps(l1), "L1 hero: +4 hit steps (capped)", ref runRef, ref passedRef, ref failedRef);

            var l9ish = TestDataBuilders.Character().WithName("GrowingHero").WithStats(15, 15, 15, 15).Build();
            TestBase.AssertEqual(6, NaiveteBalanceHelper.ComputeNaivete(l9ish), "60 total attrs: naivete = 6", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(3, NaiveteBalanceHelper.GetHitSteps(l9ish), "60 total attrs: +3 hit steps", ref runRef, ref passedRef, ref failedRef);

            var mature = TestDataBuilders.Character().WithName("Veteran").WithStats(16, 17, 16, 17).Build();
            TestBase.AssertEqual(0, NaiveteBalanceHelper.ComputeNaivete(mature), "66 total attrs: naivete = 0", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(0, NaiveteBalanceHelper.GetHitSteps(mature), "66 total attrs: no hit steps", ref runRef, ref passedRef, ref failedRef);
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

            var c = TestDataBuilders.Character().WithName("NaiveFighter").WithStats(3, 3, 3, 3).Build();
            tm.ResetThresholds(c);
            int baseHit = tm.GetHitThreshold(c);

            NaiveteThresholdBonuses.Apply(tm, c);
            TestBase.AssertEqual(baseHit - 4, tm.GetHitThreshold(c), "Apply(L1) lowers hit threshold by 4", ref runRef, ref passedRef, ref failedRef);
        }

        private static void TestDisabledConfig(ref int runRef, ref int passedRef, ref int failedRef)
        {
            var cfg = GameConfiguration.Instance;
            bool savedEnabled = cfg.EarlyGame.Naivete.Enabled;
            try
            {
                cfg.EarlyGame.Naivete.Enabled = false;
                var c = TestDataBuilders.Character().WithName("DisabledNaivete").WithStats(3, 3, 3, 3).Build();
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
