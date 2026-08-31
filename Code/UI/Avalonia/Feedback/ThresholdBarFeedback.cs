using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;

namespace RPGGame.UI.Avalonia.Feedback
{
    /// <summary>Which combat panel owns the d20 threshold bar.</summary>
    public enum ThresholdBarPanel
    {
        Hero,
        Enemy
    }

    /// <summary>
    /// Pulses the selected d20 threshold bar segment after a combat roll resolves.
    /// The yellow roll diamond travels along the bar, then lands on the rolled face.
    /// Hero and enemy panels track feedback independently.
    /// </summary>
    public static class ThresholdBarFeedback
    {
        internal const int FeedbackDurationMs = 3000;
        internal const int TravelDurationMs = 400;

        private static readonly TimeSpan TimerInterval = TimeSpan.FromMilliseconds(16);

        private static System.Action? _requestInvalidate;
        private static DispatcherTimer? _timer;

        private sealed class PanelFeedback
        {
            public int SegmentIndex = -1;
            public int Roll = -1;
            public DateTimeOffset PulseSequenceEndAt;
            public DateTimeOffset PulseSequenceStartAt;
            public int PulseHalfPeriodMs = 150;
            public bool PulseActive;
        }

        private static readonly Dictionary<ThresholdBarPanel, PanelFeedback> States = new()
        {
            [ThresholdBarPanel.Hero] = new PanelFeedback(),
            [ThresholdBarPanel.Enemy] = new PanelFeedback()
        };

        internal static Func<DateTimeOffset>? UtcNowProviderForTests;
        private static DateTimeOffset Now() => UtcNowProviderForTests?.Invoke() ?? DateTimeOffset.UtcNow;

        public static void SetRequestInvalidate(System.Action? invalidate) => _requestInvalidate = invalidate;

        public static void Trigger(ThresholdBarPanel panel, int segmentIndex, int roll)
        {
            if (segmentIndex < 0)
                return;

            void Arm()
            {
                var gs = GameSettings.Instance;
                gs.ValidateAndFix();

                var state = States[panel];
                state.SegmentIndex = segmentIndex;
                state.Roll = roll >= 1 && roll <= 20 ? roll : -1;
                var t = Now();
                state.PulseActive = true;
                state.PulseSequenceStartAt = t;
                state.PulseHalfPeriodMs = Math.Max(50, gs.ActionStripSuccessFlashPulseHalfPeriodMs);
                state.PulseSequenceEndAt = t.AddMilliseconds(FeedbackDurationMs);

                EnsureTimerStarted();
                _requestInvalidate?.Invoke();
            }

            if (Application.Current == null)
            {
                Arm();
                return;
            }

            if (Dispatcher.UIThread.CheckAccess())
                Arm();
            else
                Dispatcher.UIThread.Post(Arm, DispatcherPriority.Normal);
        }

        public static bool TryGetSegmentHighlight(ThresholdBarPanel panel, int segmentIndex, out Color color)
        {
            color = default;
            var state = States[panel];
            if (!state.PulseActive || state.SegmentIndex < 0 || segmentIndex != state.SegmentIndex)
                return false;

            var t = Now();
            if (t >= state.PulseSequenceEndAt)
            {
                ClearPanel(state);
                return false;
            }

            double elapsedMs = (t - state.PulseSequenceStartAt).TotalMilliseconds;
            if (elapsedMs < TravelDurationMs)
                return false;

            int half = Math.Max(1, state.PulseHalfPeriodMs);
            if ((int)((elapsedMs - TravelDurationMs) / half) % 2 != 0)
                return false;

            color = AsciiArtAssets.Colors.Gold;
            return true;
        }

        public static bool TryGetRollMarker(ThresholdBarPanel panel, out int roll)
        {
            roll = -1;
            var state = States[panel];
            if (!state.PulseActive || state.Roll < 1)
                return false;

            var t = Now();
            if (t >= state.PulseSequenceEndAt)
            {
                ClearPanel(state);
                return false;
            }

            double elapsedMs = (t - state.PulseSequenceStartAt).TotalMilliseconds;
            if (elapsedMs < TravelDurationMs)
            {
                double progress = Math.Clamp(elapsedMs / TravelDurationMs, 0.0, 1.0);
                double eased = 1.0 - Math.Pow(1.0 - progress, 2.0);
                roll = Math.Clamp((int)Math.Round(1 + (state.Roll - 1) * eased), 1, 20);
                return true;
            }

            roll = state.Roll;
            return true;
        }

        internal static void ResetForTests()
        {
            foreach (var kv in States)
                ClearPanel(kv.Value);
            UtcNowProviderForTests = null;
            _timer?.Stop();
        }

        private static void ClearPanel(PanelFeedback state)
        {
            state.SegmentIndex = -1;
            state.Roll = -1;
            state.PulseActive = false;
        }

        private static void EnsureTimerStarted()
        {
            if (_timer == null)
            {
                _timer = new DispatcherTimer { Interval = TimerInterval };
                _timer.Tick += OnTimerTick;
            }

            if (!_timer.IsEnabled)
                _timer.Start();
        }

        private static void OnTimerTick(object? sender, EventArgs e)
        {
            bool anyActive = false;
            var t = Now();
            foreach (var kv in States)
            {
                var state = kv.Value;
                if (!state.PulseActive)
                    continue;
                if (t >= state.PulseSequenceEndAt)
                    ClearPanel(state);
                else
                    anyActive = true;
            }

            if (!anyActive)
                _timer?.Stop();

            _requestInvalidate?.Invoke();
        }
    }
}
