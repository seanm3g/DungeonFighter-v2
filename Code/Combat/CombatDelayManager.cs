using System;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Config;
using RPGGame.Config.TextDelay;

namespace RPGGame
{
    /// <summary>
    /// Centralized delay management for combat actions
    /// Provides a single point of control for all combat timing
    /// Now loads configuration from TextDelayConfig.json via TextDelayConfiguration
    /// </summary>
    public static class CombatDelayManager
    {
        /// <summary>
        /// Configuration for combat delays
        /// Now loads from TextDelayConfiguration
        /// </summary>
        public static class Config
        {
            /// <summary>
            /// Delay between complete actions (in milliseconds)
            /// </summary>
            public static int ActionDelayMs => TextDelayConfiguration.GetActionDelayMs();
            
            /// <summary>
            /// Delay between individual messages within an action (in milliseconds)
            /// </summary>
            public static int MessageDelayMs => TextDelayConfiguration.GetMessageDelayMs();
            
            /// <summary>
            /// Whether delays are enabled for GUI
            /// </summary>
            public static bool EnableGuiDelays => TextDelayConfiguration.GetEnableGuiDelays();
            
            /// <summary>
            /// Whether delays are enabled for console
            /// </summary>
            public static bool EnableConsoleDelays => TextDelayConfiguration.GetEnableConsoleDelays();
        }
        
        /// <summary>
        /// Applies delay after a complete action is processed and displayed.
        /// Console: blocks here with ActionDelayMs.
        /// GUI: no-op — end-of-action pacing is applied by the batch display path via delayAfterBatchMs
        /// so the combat loop does not double-wait after DisplayActionBlockAsync returns.
        /// </summary>
        public static async Task DelayAfterActionAsync()
        {
            if (!ShouldApplyDelay()) return;

            // GUI action gaps are owned by BatchOperationCoordinator's delayAfterBatchMs.
            if (UIManager.GetCustomUIManager() != null)
                return;

            int delayMs = DeveloperModeState.ScaleDelayMs(Config.ActionDelayMs);
            if (delayMs > 0)
                await Task.Delay(delayMs);
        }
        
        /// <summary>
        /// Applies delay between individual messages within an action block.
        /// Applies for both GUI and console when the matching Enable*Delays flag is on,
        /// so Avalonia combat log gets real line-by-line reveal (not an instant buffer dump).
        /// </summary>
        public static async Task DelayAfterMessageAsync()
        {
            await DelayAsync(DeveloperModeState.ScaleDelayMs(Config.MessageDelayMs));
        }

        /// <summary>
        /// Delay between sequence HUD column/math beats. 50% slower than the combat-log
        /// inter-line gap (<see cref="TextDelayConfiguration.GetSequenceHudDelayMultiplier"/>).
        /// </summary>
        public static int GetSequenceHudBeatDelayMs()
        {
            int messageMs = DeveloperModeState.ScaleDelayMs(Config.MessageDelayMs);
            double multiplier = TextDelayConfiguration.GetSequenceHudDelayMultiplier();
            if (multiplier <= 0)
                multiplier = GameConstants.SequenceHudDelayMultiplier;
            return (int)Math.Ceiling(messageMs * multiplier);
        }

        public static async Task DelayAfterSequenceHudBeatAsync()
        {
            await DelayAsync(GetSequenceHudBeatDelayMs());
        }

        /// <summary>
        /// Per-character delay for F7 narrative combat-log typewriter reveal.
        /// When paragraph budget mode is on (<c>ParagraphTargetMs &gt; 0</c>), callers should use
        /// <see cref="BuildNarrativeParagraphSchedule"/> / <see cref="DelayAfterNarrativeCharAsync(int)"/>.
        /// Otherwise delay scales by sentence/word length and emphasis curve, plus battle ramp.
        /// </summary>
        public static int GetNarrativeCharRevealMs(int charsTyped = 0)
        {
            return GetNarrativeCharRevealMs(charsTyped, CharacterRevealContext.Neutral);
        }

        /// <summary>
        /// Per-character delay with sentence/word/position context for expressive rhythm (legacy path).
        /// </summary>
        public static int GetNarrativeCharRevealMs(int charsTyped, in CharacterRevealContext ctx)
        {
            var rhythm = TextDelayConfiguration.GetCharacterRevealRhythm();
            int delayMs = CharacterRevealRhythmCalculator.ComputeCharDelayMs(rhythm, in ctx, charsTyped);
            return DeveloperModeState.ScaleDelayMs(delayMs);
        }

        /// <summary>
        /// Builds a paragraph typewriter schedule when budget mode is enabled; otherwise empty
        /// (caller falls back to <see cref="GetNarrativeCharRevealMs(int, in CharacterRevealContext)"/>).
        /// </summary>
        public static int[] BuildNarrativeParagraphSchedule(string plainWrapped)
        {
            var rhythm = TextDelayConfiguration.GetCharacterRevealRhythm();
            if (!CharacterRevealRhythmCalculator.UsesParagraphBudget(rhythm))
                return Array.Empty<int>();
            return CharacterRevealRhythmCalculator.BuildParagraphSchedule(plainWrapped, rhythm.ParagraphTargetMs);
        }

        public static async Task DelayAfterNarrativeCharAsync(int charsTyped = 0)
        {
            await DelayAfterNarrativeCharAsync(charsTyped, CharacterRevealContext.Neutral);
        }

        public static async Task DelayAfterNarrativeCharAsync(int charsTyped, CharacterRevealContext ctx)
        {
            await DelayAsync(GetNarrativeCharRevealMs(charsTyped, in ctx));
        }

        /// <summary>
        /// Awaits a precomputed per-character delay (paragraph-budget schedule), with combat-speed scaling.
        /// </summary>
        public static async Task DelayAfterNarrativeScheduledMsAsync(int scheduledMs)
        {
            int delayMs = DeveloperModeState.ScaleDelayMs(Math.Max(0, scheduledMs));
            await DelayAsync(delayMs);
        }

        /// <summary>
        /// Pause between F7 narrative sentences.
        /// </summary>
        public static int GetNarrativeSentencePauseMs()
        {
            int pauseMs = TextDelayConfiguration.GetNarrativeSentencePauseMs();
            if (pauseMs <= 0)
                pauseMs = GameConstants.NarrativeSentencePauseMs;
            return DeveloperModeState.ScaleDelayMs(pauseMs);
        }

        public static async Task DelayAfterNarrativeSentenceAsync()
        {
            await DelayAsync(GetNarrativeSentencePauseMs());
        }

        private static async Task DelayAsync(int delayMs)
        {
            if (!ShouldApplyDelay()) return;
            if (delayMs > 0)
                await Task.Delay(delayMs);
        }
        
        /// <summary>
        /// Determines if delays should be applied based on UI type and configuration
        /// </summary>
        private static bool ShouldApplyDelay()
        {
            if (DeveloperModeState.IsCombatLogInstant)
                return false;

            // Skip all delays if combat UI output is disabled (e.g., during statistics runs)
            if (CombatManager.DisableCombatUIOutput)
            {
                return false;
            }
            
            // Check if we have a custom UI manager (GUI)
            if (UIManager.GetCustomUIManager() != null)
            {
                return Config.EnableGuiDelays;
            }
            else
            {
                return Config.EnableConsoleDelays;
            }
        }
        
    }
}
