using RPGGame;
using RPGGame.Actions.RollModification;
using RPGGame.Combat;

namespace RPGGame.Tests.Unit.Combat
{
    public static class ActionRollTagProcessorTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAll(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            _run = _pass = _fail = 0;
            TestFootworkLowersHitThreshold(ref _run, ref _pass, ref _fail);
            testsRun += _run;
            testsPassed += _pass;
            testsFailed += _fail;
        }

        private static void TestFootworkLowersHitThreshold(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TestFootworkLowersHitThreshold));
            var hero = new Character("Hero", 1);
            var tm = RollModificationManager.GetThresholdManager();
            tm.ResetThresholds(hero);
            int baseHit = tm.GetHitThreshold(hero);
            var action = new Action("Sidestep") { Tags = new System.Collections.Generic.List<string> { "footwork" } };
            ActionRollTagProcessor.ApplyRollTags(action, hero);
            TestBase.AssertTrue(tm.GetHitThreshold(hero) < baseHit,
                "footwork lowers hit threshold", ref run, ref pass, ref fail);
        }
    }
}
