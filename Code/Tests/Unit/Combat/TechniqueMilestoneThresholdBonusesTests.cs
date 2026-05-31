using System;
using RPGGame.Actions.RollModification;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    public static class TechniqueMilestoneThresholdBonusesTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== TechniqueMilestoneThresholdBonuses Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestGetSteps(runRef: ref run, passedRef: ref passed, failedRef: ref failed);
            TestApplyAdjustsThresholds(runRef: ref run, passedRef: ref passed, failedRef: ref failed);

            TestBase.PrintSummary("TechniqueMilestoneThresholdBonuses Tests", run, passed, failed);
        }

        private static void TestGetSteps(ref int runRef, ref int passedRef, ref int failedRef)
        {
            var s9 = TechniqueMilestoneThresholdBonuses.GetSteps(9);
            TestBase.AssertEqual(0, s9.HitSteps, "TECH 9: no HIT steps", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(0, s9.ComboSteps, "TECH 9: no COMBO steps", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(0, s9.CritSteps, "TECH 9: no CRIT steps", ref runRef, ref passedRef, ref failedRef);

            var s10 = TechniqueMilestoneThresholdBonuses.GetSteps(10);
            TestBase.AssertEqual(1, s10.HitSteps, "TECH 10: +1 HIT", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(0, s10.ComboSteps, "TECH 10: +0 COMBO", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(0, s10.CritSteps, "TECH 10: +0 CRIT", ref runRef, ref passedRef, ref failedRef);

            var s35 = TechniqueMilestoneThresholdBonuses.GetSteps(35);
            TestBase.AssertEqual(5, s35.HitSteps, "TECH 35: HIT steps (10,15,20,25,35)", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(1, s35.ComboSteps, "TECH 35: +1 COMBO (35)", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(1, s35.CritSteps, "TECH 35: +1 CRIT (30)", ref runRef, ref passedRef, ref failedRef);

            var s100 = TechniqueMilestoneThresholdBonuses.GetSteps(100);
            TestBase.AssertEqual(5, s100.HitSteps, "TECH 100: HIT steps (10,15,20,25,35)", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(9, s100.ComboSteps, "TECH 100: COMBO steps (35,40,45,55,60,65,70,80,85)", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(7, s100.CritSteps, "TECH 100: CRIT steps (30,50,75,90,95,100 +1 each)", ref runRef, ref passedRef, ref failedRef);
        }

        private static void TestApplyAdjustsThresholds(ref int runRef, ref int passedRef, ref int failedRef)
        {
            var tm = RollModificationManager.GetThresholdManager();
            tm.Clear();

            var c = TestDataBuilders.Character().WithName("TecHero").WithStats(10, 10, 35, 10).Build();
            tm.ResetThresholds(c);
            int baseHit = tm.GetHitThreshold(c);
            int baseCombo = tm.GetComboThreshold(c);
            int baseCrit = tm.GetCriticalHitThreshold(c);

            TechniqueMilestoneThresholdBonuses.Apply(tm, c);
            TestBase.AssertEqual(baseHit - 5, tm.GetHitThreshold(c), "Apply(TECH 35) lowers hit threshold by 5", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(baseCombo - 1, tm.GetComboThreshold(c), "Apply(TECH 35) lowers combo threshold by 1", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(baseCrit - 1, tm.GetCriticalHitThreshold(c), "Apply(TECH 35) lowers crit threshold by 1", ref runRef, ref passedRef, ref failedRef);
        }
    }
}
