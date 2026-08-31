using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using RPGGame.Combat.UI;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Feedback;
using RPGGame.UI.BlockDisplay;

namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// Plays recorded combat-sequence steps on the live canvas HUD between the combat-log setup
    /// telegraph and punchline.
    /// The HUD shows every calculation step in a two-row horizontal strip (titles over results);
    /// the active column is highlighted. Skipped for console, mute, and instant combat log.
    /// </summary>
    public static class CombatSequencePresenter
    {
        private static List<CombatSequenceStep>? _pending;
        private static bool _playedThisBlock;
        private static System.Action? _requestInvalidate;

        internal static List<CombatSequenceCue> CuesFiredForTests { get; } = new();
        internal static bool RecordCuesForTests { get; set; }
        internal static bool BypassCanvasCheckForTests { get; set; }
        internal static bool SkipDelaysForTests { get; set; }

        public static void SetRequestInvalidate(System.Action? invalidate) =>
            _requestInvalidate = invalidate;

        /// <summary>
        /// True when the canvas HUD should play (live GUI, delays on, not muted/instant).
        /// </summary>
        public static bool ShouldPlay()
        {
            if (CombatManager.DisableCombatUIOutput)
                return false;
            if (DeveloperModeState.IsCombatLogInstant)
                return false;
            if (!BypassCanvasCheckForTests && UIManager.GetCustomUIManager() == null)
                return false;
            return true;
        }

        /// <summary>True after <see cref="PlayPendingAsync"/> actually played HUD steps for this action block.</summary>
        public static bool PlayedThisBlock => _playedThisBlock;

        public static void SetPending(List<CombatSequenceStep>? steps, IReadOnlyList<(string EntityId, int Health)>? healthHolds = null)
        {
            _pending = steps;
            if (steps != null && steps.Count > 0 && ShouldPlay())
                HealthBarDisplayHold.SetFrom(healthHolds);
        }

        public static void ClearPending()
        {
            _pending = null;
            _playedThisBlock = false;
        }

        /// <summary>
        /// After the log setup telegraph: play the HUD if a swing is pending, otherwise wait
        /// <paramref name="halfDelayMs"/> so punchline timing matches the no-HUD path.
        /// </summary>
        public static async Task PlayPendingOrWaitAsync(int halfDelayMs)
        {
            await PlayPendingAsync();
            if (PlayedThisBlock)
                return;
            if (SkipDelaysForTests || halfDelayMs <= 0)
                return;
            await Task.Delay(halfDelayMs);
        }

        public static async Task PlayPendingAsync()
        {
            _playedThisBlock = false;
            var steps = _pending;
            _pending = null;

            if (steps == null || steps.Count == 0 || !ShouldPlay())
                return;

            _playedThisBlock = true;
            CombatSequenceHudState.Begin(steps);
            try
            {
                for (int i = 0; i < steps.Count; i++)
                {
                    var step = steps[i];
                    CombatSequenceHudState.SetActive(i, resultRevealed: false);
                    Invalidate();
                    if (!SkipDelaysForTests)
                        await CombatDelayManager.DelayAfterSequenceHudBeatAsync();

                    var beats = step.MathBeats;
                    for (int b = 0; b < beats.Count; b++)
                    {
                        CombatSequenceHudState.SetActive(i, resultRevealed: true, beats[b]);
                        if (b == beats.Count - 1)
                            FireCue(step.Cue);
                        Invalidate();
                        if (!SkipDelaysForTests)
                            await CombatDelayManager.DelayAfterSequenceHudBeatAsync();
                    }
                }
            }
            finally
            {
                CombatSequenceHudState.FinishSequence();
                HealthBarDisplayHold.ReleaseAll();
                Invalidate();
            }
        }

        public static void CancelPlaybackHolds()
        {
            HealthBarDisplayHold.ReleaseAll();
            CombatSequenceHudState.ClearStep();
            ClearPending();
        }

        private static void FireCue(CombatSequenceCue cue)
        {
            if (RecordCuesForTests)
                CuesFiredForTests.Add(cue);

            switch (cue)
            {
                case CombatSequenceCue.StripFlashAndSfx:
                    PunchlineRevealFeedback.CommitQueued();
                    break;
                case CombatSequenceCue.HealthBar:
                    HealthBarDisplayHold.ReleaseAll();
                    break;
            }
        }

        /// <summary>
        /// Encounter combat runs on a threadpool task. Never invoke the paint callback inline:
        /// <c>InvalidateVisual</c> / <c>ForceRender</c> from that thread freezes the canvas while
        /// punchline SFX still play. Always post, including when already on the UI thread, so a
        /// paint callback cannot nest another ForceRender (Training Ground deadlock).
        /// </summary>
        private static void Invalidate()
        {
            var invalidate = _requestInvalidate;
            if (invalidate == null)
                return;

            Dispatcher.UIThread.Post(invalidate, DispatcherPriority.Render);
        }

        internal static void ResetForTests()
        {
            _pending = null;
            _playedThisBlock = false;
            RecordCuesForTests = false;
            BypassCanvasCheckForTests = false;
            SkipDelaysForTests = false;
            CuesFiredForTests.Clear();
            CombatSequenceHudState.ResetForTests();
            HealthBarDisplayHold.ResetForTests();
        }
    }
}
