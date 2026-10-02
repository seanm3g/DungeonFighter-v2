using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using RPGGame;
using RPGGame.Config;
using RPGGame.Config.TextDelay;
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
            TestSequenceHudBeatIsFiftyPercentSlowerThanMessage();
            TestNarrativeCharRevealMsRampsAndSentencePause();

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

        private static void TestSequenceHudBeatIsFiftyPercentSlowerThanMessage()
        {
            Console.WriteLine("\n--- Sequence HUD beat is 50% slower than MessageDelayMs ---");
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            try
            {
                DeveloperModeState.SetCombatLogInstant(false);
                int messageMs = DeveloperModeState.ScaleDelayMs(CombatDelayManager.Config.MessageDelayMs);
                double multiplier = TextDelayConfiguration.GetSequenceHudDelayMultiplier();
                int expected = (int)Math.Ceiling(messageMs * multiplier);
                int hudMs = CombatDelayManager.GetSequenceHudBeatDelayMs();
                TestBase.AssertTrue(Math.Abs(multiplier - GameConstants.SequenceHudDelayMultiplier) < 0.001,
                    $"SequenceHudDelayMultiplier should be 1.5 (got {multiplier})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(messageMs > 0,
                    $"MessageDelayMs should be positive when not instant (got {messageMs}ms)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(hudMs == expected,
                    $"HUD beat should be {expected}ms (MessageDelayMs {messageMs} × {multiplier}), got {hudMs}ms",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(hudMs > messageMs,
                    $"HUD beat ({hudMs}ms) is slower than log line delay ({messageMs}ms)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(prevInstant);
            }
        }

        private static void TestNarrativeCharRevealMsRampsAndSentencePause()
        {
            Console.WriteLine("\n--- Narrative char reveal rhythm + battle ramp (Neutral ctx) ---");
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            CharacterRevealRhythmConfig? previousRhythm = null;
            try
            {
                DeveloperModeState.SetCombatLogInstant(false);
                previousRhythm = TextDelayConfiguration.GetCharacterRevealRhythm();

                // Isolate to Neutral scales (sentence 40 / word 6) with known ramp ceiling.
                TextDelayConfiguration.SetCharacterRevealRhythm(new CharacterRevealRhythmConfig
                {
                    Enabled = true,
                    BaseCharDelayMs = 3,
                    MinCharDelayMs = 1,
                    MaxCharDelayMs = 10,
                    SentencePauseMs = 250,
                    SentenceReferenceChars = 40,
                    SentenceScaleMin = 0.5,
                    SentenceScaleMax = 2.0,
                    WordReferenceChars = 6,
                    WordScaleMin = 0.75,
                    WordScaleMax = 1.5,
                    WordEmphasisPreset = CharacterRevealRhythmCalculator.PresetFlat,
                    WordBeginWeight = 1.0,
                    WordMidWeight = 1.0,
                    WordEndWeight = 1.0,
                    BattleRampChars = 200
                });

                int configured = TextDelayConfiguration.GetNarrativeCharRevealMs();
                int rampChars = TextDelayConfiguration.GetNarrativeCharRevealRampChars();
                int maxMs = TextDelayConfiguration.GetNarrativeCharRevealMaxMs();
                int sentencePause = TextDelayConfiguration.GetNarrativeSentencePauseMs();
                TestBase.AssertTrue(configured == 3,
                    $"NarrativeCharRevealMs config should be 3 (got {configured})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(rampChars == 200,
                    $"Ramp chars should be 200 (got {rampChars})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(maxMs == 10,
                    $"NarrativeCharRevealMaxMs should be 10 for this fixture (got {maxMs})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(sentencePause == 250,
                    $"Sentence pause should be 250 (got {sentencePause})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                int at0 = CombatDelayManager.GetNarrativeCharRevealMs(0);
                int at199 = CombatDelayManager.GetNarrativeCharRevealMs(199);
                int at200 = CombatDelayManager.GetNarrativeCharRevealMs(200);
                int at399 = CombatDelayManager.GetNarrativeCharRevealMs(399);
                int at400 = CombatDelayManager.GetNarrativeCharRevealMs(400);
                int at1400 = CombatDelayManager.GetNarrativeCharRevealMs(1400);
                int at2000 = CombatDelayManager.GetNarrativeCharRevealMs(2000);
                int expectedBase = DeveloperModeState.ScaleDelayMs(configured);
                int expectedPlus1 = DeveloperModeState.ScaleDelayMs(configured + 1);
                int expectedPlus2 = DeveloperModeState.ScaleDelayMs(configured + 2);
                int expectedCap = DeveloperModeState.ScaleDelayMs(maxMs);

                TestBase.AssertTrue(at0 == expectedBase && at199 == expectedBase,
                    $"0/199 chars should be {expectedBase}ms (got {at0}/{at199})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(at200 == expectedPlus1 && at399 == expectedPlus1,
                    $"200/399 chars should be {expectedPlus1}ms (got {at200}/{at399})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(at400 == expectedPlus2,
                    $"400 chars should be {expectedPlus2}ms (got {at400})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(at1400 == expectedCap && at2000 == expectedCap,
                    $"1400/2000 chars should cap at {expectedCap}ms (got {at1400}/{at2000})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Short sentence context should be slower than Neutral at char 0.
                var shortCtx = new CharacterRevealContext(10, 6, 0);
                int shortDelay = CombatDelayManager.GetNarrativeCharRevealMs(0, shortCtx);
                TestBase.AssertTrue(shortDelay > at0,
                    $"Short-sentence context ({shortDelay}) should exceed Neutral ({at0})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                int pauseScaled = CombatDelayManager.GetNarrativeSentencePauseMs();
                int expectedPause = DeveloperModeState.ScaleDelayMs(sentencePause);
                TestBase.AssertTrue(pauseScaled == expectedPause,
                    $"Scaled sentence pause should be {expectedPause}ms (got {pauseScaled}ms)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                if (previousRhythm != null)
                    TextDelayConfiguration.SetCharacterRevealRhythm(previousRhythm);
                DeveloperModeState.SetCombatLogInstant(prevInstant);
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
