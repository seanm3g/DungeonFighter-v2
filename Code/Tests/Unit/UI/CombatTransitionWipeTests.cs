using System;
using RPGGame;
using RPGGame.UI.Avalonia.Feedback;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Verifies which state changes play the combat enter / exit shutter and how far it is open over time.
    /// </summary>
    public static class CombatTransitionWipeTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatTransitionWipe Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestKindForStateChanges();
            TestOpennessOverTime();

            TestBase.PrintSummary("CombatTransitionWipe Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestKindForStateChanges()
        {
            Console.WriteLine("--- Enter / exit only across the Combat boundary ---");
            TestBase.AssertEqualEnum(CombatTransitionWipe.Kind.Enter,
                CombatTransitionWipe.KindFor(GameState.Dungeon, GameState.Combat),
                "Dungeon -> Combat enters", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(CombatTransitionWipe.Kind.Exit,
                CombatTransitionWipe.KindFor(GameState.Combat, GameState.Dungeon),
                "Combat -> Dungeon exits", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(CombatTransitionWipe.Kind.Exit,
                CombatTransitionWipe.KindFor(GameState.Combat, GameState.Death),
                "Combat -> Death exits", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(CombatTransitionWipe.Kind.None,
                CombatTransitionWipe.KindFor(GameState.Dungeon, GameState.DungeonCompletion),
                "non-combat change plays nothing", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(CombatTransitionWipe.Kind.None,
                CombatTransitionWipe.KindFor(GameState.Combat, GameState.Combat),
                "same state plays nothing", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOpennessOverTime()
        {
            Console.WriteLine("--- Shutter holds closed, then opens fully ---");
            var enter = CombatTransitionWipe.Kind.Enter;
            TestBase.AssertTrue(CombatTransitionWipe.Openness(enter, 0) == 0.0,
                "closed at the cut", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(CombatTransitionWipe.Openness(enter, CombatTransitionWipe.HoldMs) == 0.0,
                "still closed at the end of the hold", ref _testsRun, ref _testsPassed, ref _testsFailed);
            double mid = CombatTransitionWipe.Openness(enter, CombatTransitionWipe.HoldMs + CombatTransitionWipe.EnterOpenMs / 2.0);
            TestBase.AssertTrue(mid > 0.5 && mid < 1.0,
                $"part open mid-way (ease-out, got {mid:F2})", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(CombatTransitionWipe.Openness(enter, CombatTransitionWipe.HoldMs + CombatTransitionWipe.EnterOpenMs) == 1.0,
                "fully open at the end", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(CombatTransitionWipe.Openness(CombatTransitionWipe.Kind.Exit, CombatTransitionWipe.HoldMs + CombatTransitionWipe.ExitOpenMs) == 1.0,
                "exit fully open at its end", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
