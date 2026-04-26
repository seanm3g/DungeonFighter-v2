using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class DungeonLevelMathTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAllTests()
        {
            System.Console.WriteLine("=== DungeonLevelMath Tests ===\n");
            _run = _pass = _fail = 0;

            TestResolveEffectiveDungeonLevel_Basic();
            TestResolveEffectiveDungeonLevel_Clamps();
            TestClampDungeonDelta_RespectsHeroBounds();

            TestBase.PrintSummary("DungeonLevelMath Tests", _run, _pass, _fail);
            TestBase.ClearCurrentTestName();
        }

        private static void TestResolveEffectiveDungeonLevel_Basic()
        {
            TestBase.SetCurrentTestName(nameof(TestResolveEffectiveDungeonLevel_Basic));

            int d = DungeonLevelMath.ResolveEffectiveDungeonLevel(heroLevel: 10, dungeonDelta: 5);
            TestBase.AssertEqual(15, d, "hero 10 +5 => dungeon 15", ref _run, ref _pass, ref _fail);

            d = DungeonLevelMath.ResolveEffectiveDungeonLevel(heroLevel: 10, dungeonDelta: -3);
            TestBase.AssertEqual(7, d, "hero 10 -3 => dungeon 7", ref _run, ref _pass, ref _fail);
        }

        private static void TestResolveEffectiveDungeonLevel_Clamps()
        {
            TestBase.SetCurrentTestName(nameof(TestResolveEffectiveDungeonLevel_Clamps));

            int d = DungeonLevelMath.ResolveEffectiveDungeonLevel(heroLevel: 1, dungeonDelta: -999);
            TestBase.AssertEqual(1, d, "clamps dungeon to 1", ref _run, ref _pass, ref _fail);

            d = DungeonLevelMath.ResolveEffectiveDungeonLevel(heroLevel: 99, dungeonDelta: 999);
            TestBase.AssertEqual(99, d, "clamps dungeon to 99", ref _run, ref _pass, ref _fail);
        }

        private static void TestClampDungeonDelta_RespectsHeroBounds()
        {
            TestBase.SetCurrentTestName(nameof(TestClampDungeonDelta_RespectsHeroBounds));

            int delta = DungeonLevelMath.ClampDungeonDelta(heroLevel: 10, dungeonDelta: 999);
            TestBase.AssertEqual(89, delta, "hero 10 max delta is +89 (dungeon 99)", ref _run, ref _pass, ref _fail);

            delta = DungeonLevelMath.ClampDungeonDelta(heroLevel: 10, dungeonDelta: -999);
            TestBase.AssertEqual(-9, delta, "hero 10 min delta is -9 (dungeon 1)", ref _run, ref _pass, ref _fail);
        }
    }
}

