using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using RPGGame.Combat.UI;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Feedback;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.BlockDisplay;

namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// Plays recorded combat-sequence steps on the live canvas HUD between the combat-log setup
    /// telegraph and punchline.
    /// The HUD shows every calculation step in a two-row horizontal strip (titles over results);
    /// the active column is highlighted. Skipped for console, mute, and instant combat log.
    /// Action Lab Piece mode waits for <see cref="TryAdvanceManualBeat"/> between formula pieces
    /// instead of using beat delays.
    /// </summary>
    public static class CombatSequencePresenter
    {
        private static List<CombatSequenceStep>? _pending;
        private static bool _playedThisBlock;
        private static System.Action? _requestInvalidate;
        private static TaskCompletionSource<bool>? _manualAdvance;
        private static bool _flushRemaining;
        private static bool _isManualPlaybackWaiting;
        private static bool _manualPlaybackActive;

        internal static List<CombatSequenceCue> CuesFiredForTests { get; } = new();
        internal static bool RecordCuesForTests { get; set; }
        internal static bool BypassCanvasCheckForTests { get; set; }
        internal static bool SkipDelaysForTests { get; set; }

        /// <summary>
        /// When true, HUD playback waits for <see cref="TryAdvanceManualBeat"/> (or
        /// <see cref="FlushRemainingManualBeats"/>) instead of timed delays.
        /// </summary>
        public static bool UseManualPlayback { get; set; }

        /// <summary>True while Piece-mode playback is blocked on the next Step click.</summary>
        public static bool IsManualPlaybackWaiting => _isManualPlaybackWaiting;

        /// <summary>True from the start of a manual HUD play until it finishes or is cancelled.</summary>
        public static bool IsManualPlaybackActive => _manualPlaybackActive;

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
        /// Completes the current Piece-mode wait so the next formula piece (or finish) can run.
        /// </summary>
        /// <returns><c>true</c> when a waiter was waiting.</returns>
        public static bool TryAdvanceManualBeat()
        {
            var tcs = _manualAdvance;
            if (tcs == null)
                return false;
            return tcs.TrySetResult(true);
        }

        /// <summary>
        /// Reveals remaining HUD pieces without further clicks (undo/reset/toggle back to Swing).
        /// </summary>
        public static void FlushRemainingManualBeats()
        {
            _flushRemaining = true;
            _manualAdvance?.TrySetResult(true);
        }

        /// <summary>
        /// Aborts a Piece-mode wait so <see cref="PlayPendingAsync"/> can return (lab exit).
        /// </summary>
        public static void CancelManualPlayback()
        {
            _flushRemaining = false;
            var tcs = _manualAdvance;
            _manualAdvance = null;
            _isManualPlaybackWaiting = false;
            _manualPlaybackActive = false;
            tcs?.TrySetCanceled();
            CancelPlaybackHolds();
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
            _flushRemaining = false;

            if (steps == null || steps.Count == 0 || !ShouldPlay())
            {
                if (!ShouldPlay()) CombatVisualPlayback.Clear();
                return;
            }

            _playedThisBlock = true;
            _manualPlaybackActive = UseManualPlayback;
            CombatSequenceHudState.Begin(steps);
            try
            {
                for (int i = 0; i < steps.Count; i++)
                {
                    var step = steps[i];
                    if (!UseManualPlayback)
                    {
                        CombatSequenceHudState.SetActive(i, resultRevealed: false);
                        if (step.Kind == CombatSequenceStepKind.Action)
                            FighterResolveActionStackState.RevealCurrent();
                        Invalidate();
                        if (!await WaitForBeatAsync())
                            return;
                    }

                    var beats = step.MathBeats;
                    for (int b = 0; b < beats.Count; b++)
                    {
                        CombatSequenceHudState.SetActive(i, resultRevealed: true, beats[b]);
                        if (step.Kind == CombatSequenceStepKind.Action)
                            FighterResolveActionStackState.RevealCurrent();
                        if (b == beats.Count - 1)
                        {
                            FireCue(step.Cue, GameConfiguration.Instance.UICustomization.IllustratedCombat
                                && step.VisualAction is { Hit: true } visualSound && (visualSound.Damage > 0 || visualSound.Heal > 0));
                            FireVisualCue(step);
                        }
                        Invalidate();
                        if (!await WaitForBeatAsync())
                            return;
                    }
                }
            }
            finally
            {
                _flushRemaining = false;
                _manualPlaybackActive = false;
                _isManualPlaybackWaiting = false;
                _manualAdvance = null;
                CombatSequenceHudState.FinishSequence();
                HealthBarDisplayHold.ReleaseAll();
                Invalidate();
            }
        }

        public static void CancelPlaybackHolds()
        {
            CombatVisualPlayback.Clear();
            HealthBarDisplayHold.ReleaseAll();
            CombatSequenceHudState.ClearStep();
            ClearPending();
        }

        /// <summary>
        /// Timed delay in Swing/live play; Piece mode waits for <see cref="TryAdvanceManualBeat"/>.
        /// </summary>
        /// <returns><c>false</c> when playback was cancelled.</returns>
        private static async Task<bool> WaitForBeatAsync()
        {
            if (UseManualPlayback)
            {
                if (_flushRemaining)
                    return true;

                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _manualAdvance = tcs;
                _isManualPlaybackWaiting = true;
                try
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    return false;
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
                finally
                {
                    _isManualPlaybackWaiting = false;
                    if (ReferenceEquals(_manualAdvance, tcs))
                        _manualAdvance = null;
                }
            }

            if (SkipDelaysForTests)
                return true;
            await CombatPlaybackControls.WaitAsync();
            await CombatDelayManager.DelayAfterSequenceHudBeatAsync();
            await CombatPlaybackControls.WaitAsync();
            return true;
        }

        internal static void FireVisualCue(CombatSequenceStep step)
        {
            if (step.VisualAction is not { } visual) return;
            string? phase = step.Kind switch
            {
                CombatSequenceStepKind.Action => visual.Hit ? visual.Style : "miss",
                CombatSequenceStepKind.Defense when visual.Damage == 0 => "guard",
                CombatSequenceStepKind.Damage => "impact",
                CombatSequenceStepKind.Heal => "heal",
                CombatSequenceStepKind.Effect => "effect",
                _ => null
            };
            if (phase != null) FighterResolveActionStackState.ApplyVisualResult(visual, phase);
            if (phase != null)
                CombatVisualPlayback.Publish(visual, phase, CombatDelayManager.GetSequenceHudBeatDelayMs());
            if (GameConfiguration.Instance.UICustomization.IllustratedCombat && phase is "impact" or "heal")
                RPGGame.Audio.AudioCues.CommitQueued();
        }

        private static void FireCue(CombatSequenceCue cue, bool deferAudio = false)
        {
            if (RecordCuesForTests)
                CuesFiredForTests.Add(cue);

            switch (cue)
            {
                case CombatSequenceCue.StripFlashAndSfx:
                    PunchlineRevealFeedback.CommitQueued(!deferAudio);
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
            CombatVisualPlayback.Clear();
            _pending = null;
            _playedThisBlock = false;
            RecordCuesForTests = false;
            BypassCanvasCheckForTests = false;
            SkipDelaysForTests = false;
            UseManualPlayback = false;
            _flushRemaining = false;
            _isManualPlaybackWaiting = false;
            _manualPlaybackActive = false;
            var tcs = _manualAdvance;
            _manualAdvance = null;
            tcs?.TrySetCanceled();
            CuesFiredForTests.Clear();
            CombatSequenceHudState.ResetForTests();
            HealthBarDisplayHold.ResetForTests();
            FighterResolveActionStackState.ResetForTests();
        }
    }
}


