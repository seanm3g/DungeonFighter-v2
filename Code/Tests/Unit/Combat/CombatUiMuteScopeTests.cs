using System;
using RPGGame;
using RPGGame.Combat;
using RPGGame.Combat.UI;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Verifies AsyncLocal mute/room scoping so nested lab/sim mute cannot leak into outer combat UI.
    /// </summary>
    public static class CombatUiMuteScopeTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatUiMuteScope / EnvironmentContext Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestMuteScope_OverridesGlobalWithoutPermanentChange();
            TestMuteScope_NestedRestore();
            TestTurnManager_AliasesCombatManagerMute();
            TestEnvironmentContext_BeginScopeRestores();
            TestEnvironmentContext_DoesNotLeakAcrossScopes();
            TestIsolatedTicker_ResetDoesNotZeroGlobalClock();
            TestMuteScope_ClearsHealthBarHintsOnEnter();
            TestMutedScope_SkipsHealthBarHintWrites();

            TestBase.PrintSummary("CombatUiMuteScope Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestMuteScope_OverridesGlobalWithoutPermanentChange()
        {
            Console.WriteLine("--- Mute scope overrides global without permanent change ---");
            bool prev = CombatUiMuteScope.GlobalMute;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                TestBase.AssertTrue(!CombatManager.DisableCombatUIOutput,
                    "Global unmute should clear DisableCombatUIOutput",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                using (CombatUiMuteScope.Begin(muted: true))
                {
                    TestBase.AssertTrue(CombatManager.DisableCombatUIOutput,
                        "Begin(muted:true) should mute DisableCombatUIOutput",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    TestBase.AssertTrue(!CombatUiMuteScope.GlobalMute,
                        "AsyncLocal mute must not flip GlobalMute",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }

                TestBase.AssertTrue(!CombatManager.DisableCombatUIOutput,
                    "After dispose, mute should restore to global false",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                CombatUiMuteScope.GlobalMute = prev;
            }
        }

        private static void TestMuteScope_NestedRestore()
        {
            Console.WriteLine("\n--- Nested mute scopes restore correctly ---");
            bool prev = CombatUiMuteScope.GlobalMute;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                using (CombatUiMuteScope.Begin(muted: true))
                {
                    TestBase.AssertTrue(CombatManager.DisableCombatUIOutput, "outer mute",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    using (CombatUiMuteScope.Begin(muted: false))
                    {
                        TestBase.AssertTrue(!CombatManager.DisableCombatUIOutput, "inner unmute",
                            ref _testsRun, ref _testsPassed, ref _testsFailed);
                    }
                    TestBase.AssertTrue(CombatManager.DisableCombatUIOutput, "restore to outer mute",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
                TestBase.AssertTrue(!CombatManager.DisableCombatUIOutput, "restore to global",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                CombatUiMuteScope.GlobalMute = prev;
            }
        }

        private static void TestTurnManager_AliasesCombatManagerMute()
        {
            Console.WriteLine("\n--- TurnManager.DisableCombatUIOutput aliases CombatManager ---");
            bool prev = CombatUiMuteScope.GlobalMute;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                using (CombatUiMuteScope.Begin(muted: true))
                {
                    TestBase.AssertTrue(TurnManager.DisableCombatUIOutput,
                        "TurnManager mute should match CombatManager scoped mute",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            finally
            {
                CombatUiMuteScope.GlobalMute = prev;
            }
        }

        private static void TestEnvironmentContext_BeginScopeRestores()
        {
            Console.WriteLine("\n--- Environment context BeginScope restores previous room ---");
            var prev = CombatEnvironmentContext.CurrentRoom;
            try
            {
                CombatEnvironmentContext.CurrentRoom = null;
                var roomA = new Environment("RoomA", "test", true, "Forest");
                var roomB = new Environment("RoomB", "test", true, "Forest");

                using (CombatEnvironmentContext.BeginScope(roomA))
                {
                    TestBase.AssertEqual("RoomA", CombatEnvironmentContext.CurrentRoom?.Name,
                        "Scope A should set CurrentRoom",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);

                    using (CombatEnvironmentContext.BeginScope(roomB))
                    {
                        TestBase.AssertEqual("RoomB", CombatEnvironmentContext.CurrentRoom?.Name,
                            "Scope B should set CurrentRoom",
                            ref _testsRun, ref _testsPassed, ref _testsFailed);
                    }

                    TestBase.AssertEqual("RoomA", CombatEnvironmentContext.CurrentRoom?.Name,
                        "Dispose B should restore RoomA",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }

                TestBase.AssertTrue(CombatEnvironmentContext.CurrentRoom == null,
                    "Dispose A should restore null",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                CombatEnvironmentContext.CurrentRoom = prev;
            }
        }

        private static void TestEnvironmentContext_DoesNotLeakAcrossScopes()
        {
            Console.WriteLine("\n--- Environment context does not leak across independent scopes ---");
            var prev = CombatEnvironmentContext.CurrentRoom;
            try
            {
                CombatEnvironmentContext.CurrentRoom = null;
                var room = new Environment("Isolated", "test", true, "Forest");
                using (CombatEnvironmentContext.BeginScope(room))
                {
                    TestBase.AssertEqual("Isolated", CombatEnvironmentContext.CurrentRoom?.Name,
                        "Active scope exposes room",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
                TestBase.AssertTrue(CombatEnvironmentContext.CurrentRoom == null,
                    "Room cleared after scope end",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                CombatEnvironmentContext.CurrentRoom = prev;
            }
        }

        private static void TestIsolatedTicker_ResetDoesNotZeroGlobalClock()
        {
            Console.WriteLine("\n--- Isolated ticker Reset does not zero global clock ---");
            GameTicker.Instance.AdvanceGameTime(12.5);
            double before = GameTicker.Instance.GameTime;

            using (GameTicker.BeginIsolatedEncounterGameTime())
            {
                GameTicker.Instance.Reset();
                TestBase.AssertTrue(Math.Abs(GameTicker.Instance.GameTime) < 0.0001,
                    "Isolated Reset zeros only the AsyncLocal clock",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                GameTicker.Instance.AdvanceGameTime(3.0);
                TestBase.AssertTrue(Math.Abs(GameTicker.Instance.GameTime - 3.0) < 0.0001,
                    "Isolated Advance updates AsyncLocal clock",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            TestBase.AssertTrue(Math.Abs(GameTicker.Instance.GameTime - before) < 0.0001,
                "Global GameTime preserved after isolated encounter ends",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMuteScope_ClearsHealthBarHintsOnEnter()
        {
            Console.WriteLine("\n--- Mute scope clears pending health bar hints on enter ---");
            HealthBarDeltaDamageHint.ClearAll();
            const string id = "player_MuteClear";
            HealthBarDeltaDamageHint.SetPending(id, 2, 1, 0);
            using (CombatUiMuteScope.Begin(muted: true))
            {
                TestBase.AssertTrue(CombatUiMuteScope.IsMuted, "muted in scope", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            bool ok = HealthBarDeltaDamageHint.TryConsume(id, 3, out _);
            TestBase.AssertFalse(ok, "hint cleared when mute scope entered", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMutedScope_SkipsHealthBarHintWrites()
        {
            Console.WriteLine("\n--- Muted scope skips health bar hint writes ---");
            HealthBarDeltaDamageHint.ClearAll();
            const string id = "enemy_MutedSkip";
            using (CombatUiMuteScope.Begin(muted: true))
            {
                HealthBarDeltaDamageHint.SetPending(id, 4, 0, 0);
                HealthBarDeltaDamageHint.RecordAfterMitigation(id, 4, 0, 0, 4, 4);
            }
            bool ok = HealthBarDeltaDamageHint.TryConsume(id, 4, out _);
            TestBase.AssertFalse(ok, "muted sim must not leave pending hints", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
