using System.Collections.Generic;
using RPGGame;
using RPGGame.Combat;

namespace RPGGame.Tests.Unit.Combat
{
    public static class EnvironmentRollModifierTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAll(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            _run = _pass = _fail = 0;
            TestElegantBoostsRoll(ref _run, ref _pass, ref _fail);
            TestDilapidatedReducesRoll(ref _run, ref _pass, ref _fail);
            testsRun += _run;
            testsPassed += _pass;
            testsFailed += _fail;
        }

        private static Environment MakeRoom(params string[] tags)
        {
            var room = new Environment("Test", "desc", true, "Forest");
            room.SetTags(tags);
            return room;
        }

        private static void TestElegantBoostsRoll(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TestElegantBoostsRoll));
            var room = MakeRoom("elegant");
            var hero = new Character("Hero", 5);
            int result = EnvironmentRollModifier.ApplyStructureRollShift(room, hero, 10);
            TestBase.AssertEqual(15, result, "elegant adds dungeon level", ref run, ref pass, ref fail);
        }

        private static void TestDilapidatedReducesRoll(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TestDilapidatedReducesRoll));
            var room = MakeRoom("dilapidated");
            var hero = new Character("Hero", 5);
            int result = EnvironmentRollModifier.ApplyStructureRollShift(room, hero, 10);
            TestBase.AssertEqual(5, result, "dilapidated subtracts dungeon level", ref run, ref pass, ref fail);
        }
    }
}
