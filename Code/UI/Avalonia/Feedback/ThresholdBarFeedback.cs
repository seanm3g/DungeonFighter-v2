using System;
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
    /// Uses the same on/off timing as hero action strip feedback.
    /// </summary>
    public static class ThresholdBarFeedback
    {
        private static readonly TimeSpan TimerInterval = TimeSpan.FromMilliseconds(16);

        private static System.Action? _requestInvalidate;
        private static DispatcherTimer? _timer;

        private static ThresholdBarPanel _panel;
        private static int _segmentIndex = -1;
        private static DateTimeOffset _pulseSequenceEndAt;
        private static DateTimeOffset _pulseSequenceStartAt;
        private static int _pulseHalfPeriodMs = 150;
        private static bool _pulseActive;

        /// <summary>For unit tests: fixed clock; when null, <see cref="DateTimeOffset.UtcNow"/> is used.</summary>
        internal static Func<DateTimeOffset>? UtcNowProviderForTests;

        private static DateTimeOffset Now() => UtcNowProviderForTests?.Invoke() ?? DateTimeOffset.UtcNow;

        public static void SetRequestInvalidate(System.Action? invalidate) => _requestInvalidate = invalidate;

        /// <summary>Starts or refreshes a blink on the given bar segment.</summary>
        public static void Trigger(ThresholdBarPanel panel, int segmentIndex)
        {
            if (segmentIndex < 0)
                return;

            void Arm()
            {
                var gs = GameSettings.Instance;
                gs.ValidateAndFix();

                _panel = panel;
                _segmentIndex = segmentIndex;
                var t = Now();
                _pulseActive = true;
                _pulseSequenceStartAt = t;
                _pulseHalfPeriodMs = Math.Max(50, gs.ActionStripSuccessFlashPulseHalfPeriodMs);
                _pulseSequenceEndAt = t.AddMilliseconds(gs.ActionStripMissFlashDurationMs);

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

        /// <summary>
        /// When active and in the pulse "on" phase, returns a highlight color for the segment.
        /// </summary>
        public static bool TryGetSegmentHighlight(ThresholdBarPanel panel, int segmentIndex, out Color color)
        {
            color = default;
            if (!_pulseActive || _segmentIndex < 0 || panel != _panel || segmentIndex != _segmentIndex)
                return false;

            var t = Now();
            if (t >= _pulseSequenceEndAt)
            {
                ClearFlashState();
                return false;
            }

            double elapsedMs = (t - _pulseSequenceStartAt).TotalMilliseconds;
            int half = Math.Max(1, _pulseHalfPeriodMs);
            if ((int)(elapsedMs / half) % 2 != 0)
                return false;

            color = AsciiArtAssets.Colors.Gold;
            return true;
        }

        internal static void ResetForTests()
        {
            ClearFlashState();
            UtcNowProviderForTests = null;
            _timer?.Stop();
        }

        private static void ClearFlashState()
        {
            _segmentIndex = -1;
            _pulseActive = false;
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
            if (_segmentIndex < 0)
            {
                _timer?.Stop();
                return;
            }

            var t = Now();
            if (_pulseActive && t >= _pulseSequenceEndAt)
                ClearFlashState();

            if (_segmentIndex < 0)
                _timer?.Stop();

            _requestInvalidate?.Invoke();
        }
    }
}
