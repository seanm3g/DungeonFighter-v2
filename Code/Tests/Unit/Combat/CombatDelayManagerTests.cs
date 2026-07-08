using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Verifies GUI/console combat delay contract: inter-line delays apply for both UI modes;
    /// GUI skips DelayAfterActionAsync (batch owns end-of-action pacing).
    /// </summary>
    public static class CombatDelayManagerTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatDelayManager Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestDelayAfterMessageAsync_AppliesWhenDelaysEnabled().GetAwaiter().GetResult();
            TestDelayAfterMessageAsync_SkippedWhenMuted().GetAwaiter().GetResult();
            TestDelayAfterMessageAsync_SkippedWhenCombatLogInstant().GetAwaiter().GetResult();
            TestDelayAfterActionAsync_NoOpWithCustomUiManager().GetAwaiter().GetResult();

            TestBase.PrintSummary("CombatDelayManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static async Task TestDelayAfterMessageAsync_AppliesWhenDelaysEnabled()
        {
            Console.WriteLine("--- DelayAfterMessageAsync applies when delays enabled ---");
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            bool prevDelays = UIManager.EnableDelays;
            bool prevMute = CombatManager.DisableCombatUIOutput;
            var prevUi = UIManager.GetCustomUIManager();
            try
            {
                DeveloperModeState.SetCombatLogInstant(false);
                UIManager.EnableDelays = true;
                CombatManager.DisableCombatUIOutput = false;
                UIManager.SetCustomUIManager(null);

                var sw = Stopwatch.StartNew();
                await CombatDelayManager.DelayAfterMessageAsync();
                sw.Stop();

                int expected = DeveloperModeState.ScaleDelayMs(CombatDelayManager.Config.MessageDelayMs);
                if (expected > 0)
                {
                    TestBase.AssertTrue(sw.ElapsedMilliseconds >= expected * 0.5,
                        $"DelayAfterMessageAsync should wait (~{expected}ms), observed {sw.ElapsedMilliseconds}ms",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
                else
                {
                    TestBase.AssertTrue(true, "MessageDelayMs is 0 in this environment", ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                UIManager.EnableDelays = prevDelays;
                CombatManager.DisableCombatUIOutput = prevMute;
                UIManager.SetCustomUIManager(prevUi);
            }
        }

        private static async Task TestDelayAfterMessageAsync_SkippedWhenMuted()
        {
            Console.WriteLine("\n--- DelayAfterMessageAsync skipped when muted ---");
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            bool prevDelays = UIManager.EnableDelays;
            bool prevMute = CombatManager.DisableCombatUIOutput;
            try
            {
                DeveloperModeState.SetCombatLogInstant(false);
                UIManager.EnableDelays = true;
                CombatManager.DisableCombatUIOutput = true;

                var sw = Stopwatch.StartNew();
                await CombatDelayManager.DelayAfterMessageAsync();
                sw.Stop();

                TestBase.AssertTrue(sw.ElapsedMilliseconds < 50,
                    $"Muted DelayAfterMessageAsync should be near-instant, observed {sw.ElapsedMilliseconds}ms",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                UIManager.EnableDelays = prevDelays;
                CombatManager.DisableCombatUIOutput = prevMute;
            }
        }

        private static async Task TestDelayAfterMessageAsync_SkippedWhenCombatLogInstant()
        {
            Console.WriteLine("\n--- DelayAfterMessageAsync skipped when combat log instant ---");
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            bool prevDelays = UIManager.EnableDelays;
            bool prevMute = CombatManager.DisableCombatUIOutput;
            try
            {
                DeveloperModeState.SetCombatLogInstant(true);
                UIManager.EnableDelays = true;
                CombatManager.DisableCombatUIOutput = false;

                var sw = Stopwatch.StartNew();
                await CombatDelayManager.DelayAfterMessageAsync();
                sw.Stop();

                TestBase.AssertTrue(sw.ElapsedMilliseconds < 50,
                    $"Instant-mode DelayAfterMessageAsync should be near-instant, observed {sw.ElapsedMilliseconds}ms",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                UIManager.EnableDelays = prevDelays;
                CombatManager.DisableCombatUIOutput = prevMute;
            }
        }

        private static async Task TestDelayAfterActionAsync_NoOpWithCustomUiManager()
        {
            Console.WriteLine("\n--- DelayAfterActionAsync is no-op when custom UI manager is set ---");
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            bool prevDelays = UIManager.EnableDelays;
            bool prevMute = CombatManager.DisableCombatUIOutput;
            var prevUi = UIManager.GetCustomUIManager();
            try
            {
                DeveloperModeState.SetCombatLogInstant(false);
                UIManager.EnableDelays = true;
                CombatManager.DisableCombatUIOutput = false;
                UIManager.SetCustomUIManager(new StubUiManager());

                var sw = Stopwatch.StartNew();
                await CombatDelayManager.DelayAfterActionAsync();
                sw.Stop();

                TestBase.AssertTrue(sw.ElapsedMilliseconds < 50,
                    $"GUI DelayAfterActionAsync should be no-op (batch owns pacing), observed {sw.ElapsedMilliseconds}ms",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                UIManager.EnableDelays = prevDelays;
                CombatManager.DisableCombatUIOutput = prevMute;
                UIManager.SetCustomUIManager(prevUi);
            }
        }

        private sealed class StubUiManager : IUIManager
        {
            public void WriteLine(string message, UIMessageType messageType = UIMessageType.System) { }
            public void Write(string message) { }
            public void WriteSystemLine(string message) { }
            public void WriteMenuLine(string message) { }
            public void WriteTitleLine(string message) { }
            public void WriteDungeonLine(string message) { }
            public void WriteRoomLine(string message) { }
            public void WriteEnemyLine(string message) { }
            public void WriteRoomClearedLine(string message) { }
            public void WriteEffectLine(string message) { }
            public void WriteBlankLine() { }
            public void ResetForNewBattle() { }
            public void ResetMenuDelayCounter() { }
            public int GetConsecutiveMenuLineCount() => 0;
            public int GetBaseMenuDelay() => 0;
            public void WriteChunked(string message, ChunkedTextReveal.RevealConfig? config = null) { }
            public void WriteColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System) { }
            public void WriteLineColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System) { }
            public void WriteColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System) { }
            public void WriteLineColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System) { }
            public void WriteColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System) { }
            public void WriteLineColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System) { }
        }
    }
}
