using System;
using RPGGame.Actions.RollModification;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    public static class IntelligenceMilestoneThresholdBonusesTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== IntelligenceMilestoneThresholdBonuses Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestGetSteps(runRef: ref run, passedRef: ref passed, failedRef: ref failed);
            TestApplyAdjustsThresholds(runRef: ref run, passedRef: ref passed, failedRef: ref failed);

            TestBase.PrintSummary("IntelligenceMilestoneThresholdBonuses Tests", run, passed, failed);
        }

        private static void TestGetSteps(ref int runRef, ref int passedRef, ref int failedRef)
        {
            var s9 = IntelligenceMilestoneThresholdBonuses.GetSteps(9);
            TestBase.AssertEqual(0, s9.HitSteps, "INT 9: no HIT steps", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(0, s9.ComboSteps, "INT 9: no COMBO steps", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(0, s9.CritSteps, "INT 9: no CRIT steps", ref runRef, ref passedRef, ref failedRef);

            var s10 = IntelligenceMilestoneThresholdBonuses.GetSteps(10);
            TestBase.AssertEqual(1, s10.HitSteps, "INT 10: +1 HIT", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(0, s10.ComboSteps, "INT 10: +0 COMBO", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(0, s10.CritSteps, "INT 10: +0 CRIT", ref runRef, ref passedRef, ref failedRef);

            var s35 = IntelligenceMilestoneThresholdBonuses.GetSteps(35);
            TestBase.AssertEqual(5, s35.HitSteps, "INT 35: HIT steps (10,15,20,25,35)", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(1, s35.ComboSteps, "INT 35: +1 COMBO (35)", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(1, s35.CritSteps, "INT 35: +1 CRIT (30)", ref runRef, ref passedRef, ref failedRef);

            var s100 = IntelligenceMilestoneThresholdBonuses.GetSteps(100);
            TestBase.AssertEqual(5, s100.HitSteps, "INT 100: HIT steps (10,15,20,25,35)", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(9, s100.ComboSteps, "INT 100: COMBO steps (35,40,45,55,60,65,70,80,85)", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(7, s100.CritSteps, "INT 100: CRIT steps (30,50,75,90,95,100 +1 each)", ref runRef, ref passedRef, ref failedRef);
        }

        private static void TestApplyAdjustsThresholds(ref int runRef, ref int passedRef, ref int failedRef)
        {
            var tm = RollModificationManager.GetThresholdManager();
            tm.Clear();

            var c = TestDataBuilders.Character().WithName("IntHero").WithStats(10, 10, 10, 35).Build();
            tm.ResetThresholds(c);
            int baseHit = tm.GetHitThreshold(c);
            int baseCombo = tm.GetComboThreshold(c);
            int baseCrit = tm.GetCriticalHitThreshold(c);

            IntelligenceMilestoneThresholdBonuses.Apply(tm, c);
            TestBase.AssertEqual(baseHit - 5, tm.GetHitThreshold(c), "Apply(INT 35) lowers hit threshold by 5", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(baseCombo - 1, tm.GetComboThreshold(c), "Apply(INT 35) lowers combo threshold by 1", ref runRef, ref passedRef, ref failedRef);
            TestBase.AssertEqual(baseCrit - 1, tm.GetCriticalHitThreshold(c), "Apply(INT 35) lowers crit threshold by 1", ref runRef, ref passedRef, ref failedRef);
        }
    }
}

