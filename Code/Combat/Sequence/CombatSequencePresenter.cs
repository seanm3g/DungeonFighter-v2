using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using RPGGame.Combat.UI;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Feedback;
using RPGGame.UI.BlockDisplay;
using RPGGame.UI.ColorSystem;

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
        /// True when the canvas HUD should play timed column beats (live GUI, delays on, not muted/instant).
        /// F7 narrative mode keeps the band visible and reveals columns with matching prose instead.
        /// </summary>
        public static bool ShouldPlay()
        {
            if (CombatManager.DisableCombatUIOutput)
                return false;
            if (DeveloperModeState.IsCombatLogInstant)
                return false;
            if (DeveloperModeState.IsNarrativeCombatLog)
                return false;
            if (!BypassCanvasCheckForTests && UIManager.GetCustomUIManager() == null)
                return false;
            return true;
        }

        /// <summary>
        /// Snap the reserved sequence bar to a finished swing (all columns complete).
        /// Prefer <see cref="BeginNarrativeSyncedReveal"/> + <see cref="RevealNarrativeColumn"/> for F7.
        /// </summary>
        public static void ShowFinishedSnapshot(IReadOnlyList<CombatSequenceStep>? steps)
        {
            if (steps == null || steps.Count == 0)
                return;

            CombatSequenceHudState.Begin(steps);
            CombatSequenceHudState.FinishSequence();
            Invalidate();
        }

        /// <summary>
        /// F7: reserve this swing's columns empty, then reveal each kind when its prose line plays.
        /// </summary>
        public static void BeginNarrativeSyncedReveal(IReadOnlyList<CombatSequenceStep>? steps)
        {
            if (steps == null || steps.Count == 0)
                return;

            CombatSequenceHudState.BeginSelective(steps);
            Invalidate();
        }

        /// <summary>
        /// F7: show the HUD column for <paramref name="kind"/> when that narrative sentence appears.
        /// No-ops when the kind has no HUD column or is absent from this swing.
        /// </summary>
        public static void RevealNarrativeColumn(CombatSequenceStepKind kind)
        {
            if (!CombatSequenceHudState.RevealStepKind(kind))
                return;

            if (RecordCuesForTests)
                NarrativeColumnsRevealedForTests.Add(kind);

            Invalidate();
        }

        /// <summary>F7: leave every column complete after the prose paragraph finishes.</summary>
        public static void FinishNarrativeSyncedReveal()
        {
            if (CombatSequenceHudState.Steps.Count == 0)
                return;

            CombatSequenceHudState.FinishSequence();
            Invalidate();
        }

        /// <summary>Kinds revealed via <see cref="RevealNarrativeColumn"/> while recording tests.</summary>
        internal static List<CombatSequenceStepKind> NarrativeColumnsRevealedForTests { get; } = new();

        /// <summary>True after <see cref="PlayPendingAsync"/> actually played HUD steps for this action block.</summary>
        public static bool PlayedThisBlock => _playedThisBlock;

        public static void SetPending(List<CombatSequenceStep>? steps, IReadOnlyList<(string EntityId, int Health)>? healthHolds = null)
        {
            _pending = steps;
            if (steps != null && steps.Count > 0 && ShouldPlay())
                HealthBarDisplayHold.SetFrom(healthHolds);
        }

        /// <summary>Gives the pending swing its combat-log action block, for the board's box above NOW.</summary>
        public static void AttachLogBlock(List<List<ColoredText>> block) =>
            CombatSequenceSwingInfo.AttachLogBlock(_pending, block);

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
                return;

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
                        Invalidate();
                        if (!await WaitForBeatAsync(TitleBeats(step.Kind)))
                            return;
                        if (step.Kind == CombatSequenceStepKind.Attacker)
                            await WaitForRollGateAsync();
                    }

                    var beats = step.MathBeats;
                    for (int b = 0; b < beats.Count; b++)
                    {
                        CombatSequenceHudState.SetActive(i, resultRevealed: true, beats[b]);
                        if (b == beats.Count - 1)
                            FireCue(step.Cue);
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
            HealthBarDisplayHold.ReleaseAll();
            CombatSequenceHudState.ClearStep();
            ClearPending();
        }

        /// <summary>
        /// Timed delay in Swing/live play; Piece mode waits for <see cref="TryAdvanceManualBeat"/>.
        /// </summary>
        /// <returns><c>false</c> when playback was cancelled.</returns>
        /// <summary>
        /// Beats the d20 tumbles (faces shuffling) before the ROLL column reveals the kept die. Under a beat, but long
        /// enough to watch the faces shuffle.
        /// </summary>
        public const double RollTumbleBeats = 0.75;

        /// <summary>
        /// Beats ATTACKER holds before the die rolls: the turn lights up and the odds pulse, then the blank die appears.
        /// </summary>
        public const int AttackerBeats = 2;

        /// <summary>Beats a column shows its title before its result reveals.</summary>
        public static double TitleBeats(CombatSequenceStepKind kind) => kind switch
        {
            CombatSequenceStepKind.Roll => RollTumbleBeats,
            CombatSequenceStepKind.Attacker => AttackerBeats,
            _ => 1
        };

        /// <summary>Longest ATTACKER is held past its beats waiting on the combat board's die.</summary>
        private const int RollGateMaxWaitMs = 1500;

        private static CombatSequenceSwingInfo? _rollGateSwing;
        private static DateTimeOffset _rollGateReadyUtc;
        private static DateTimeOffset _rollGateSeenUtc;

        /// <summary>
        /// The combat board reports (every frame it paints) when <paramref name="swing"/>'s die is on screen, so ROLL
        /// doesn't tumble a die the board hasn't shown yet. <paramref name="readyUtc"/> is null while the board is
        /// still bringing the turn in.
        /// </summary>
        public static void ReportRollGate(CombatSequenceSwingInfo? swing, DateTimeOffset? readyUtc)
        {
            _rollGateSeenUtc = DateTimeOffset.UtcNow;
            _rollGateSwing = readyUtc != null ? swing : null;
            _rollGateReadyUtc = readyUtc ?? default;
        }

        /// <summary>Holds ATTACKER until the board's die is up (no wait when the board isn't painting).</summary>
        private static async Task WaitForRollGateAsync()
        {
            if (SkipDelaysForTests || UseManualPlayback)
                return;
            var swing = CombatSequenceHudState.ActiveSwing;
            var giveUp = DateTimeOffset.UtcNow.AddMilliseconds(RollGateMaxWaitMs);
            while (swing != null && DateTimeOffset.UtcNow < giveUp)
            {
                var now = DateTimeOffset.UtcNow;
                if ((now - _rollGateSeenUtc).TotalMilliseconds > 250)
                    return; // board not on screen
                if (ReferenceEquals(_rollGateSwing, swing) && now >= _rollGateReadyUtc)
                    return;
                await Task.Delay(30);
            }
        }

        private static async Task<bool> WaitForBeatAsync(double beats = 1)
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
            int whole = (int)beats;
            for (int i = 0; i < whole; i++)
                await CombatDelayManager.DelayAfterSequenceHudBeatAsync();
            if (beats > whole)
                await CombatDelayManager.DelayAfterSequenceHudBeatAsync(beats - whole);
            return true;
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
            UseManualPlayback = false;
            _flushRemaining = false;
            _isManualPlaybackWaiting = false;
            _manualPlaybackActive = false;
            var tcs = _manualAdvance;
            _manualAdvance = null;
            tcs?.TrySetCanceled();
            CuesFiredForTests.Clear();
            NarrativeColumnsRevealedForTests.Clear();
            CombatSequenceHudState.ResetForTests();
            HealthBarDisplayHold.ResetForTests();
        }
    }
}
