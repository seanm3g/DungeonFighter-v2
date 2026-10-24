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
    /// The roll caret first tumbles across the faces for <see cref="RollTumbleMs"/>, slowing until it lands;
    /// then the segment highlight pulses (same on/off half-period as hero action strip success feedback)
    /// and the caret stays on the roll for the full <see cref="FeedbackDurationMs"/> window.
    /// </summary>
    public static class ThresholdBarFeedback
    {
        /// <summary>Time the caret tumbles across the faces before landing on the roll.</summary>
        internal const int RollTumbleMs = 450;

        /// <summary>Total time segment pulse and roll caret remain visible after the caret lands.</summary>
        internal const int FeedbackDurationMs = 3000;

        /// <summary>Face changes over a full tumble; eased so they come fast at first and slow into the landing.</summary>
        private const int TumbleSteps = 12;

        private static readonly TimeSpan TimerInterval = TimeSpan.FromMilliseconds(16);

        private static System.Action? _requestInvalidate;
        private static DispatcherTimer? _timer;

        private static ThresholdBarPanel _panel;
        private static int _segmentIndex = -1;
        private static int _roll = -1;
        private static DateTimeOffset _tumbleStartAt;
        private static DateTimeOffset _pulseSequenceEndAt;
        private static DateTimeOffset _pulseSequenceStartAt;
        private static int _pulseHalfPeriodMs = 150;
        private static bool _pulseActive;

        /// <summary>For unit tests: fixed clock; when null, <see cref="DateTimeOffset.UtcNow"/> is used.</summary>
        internal static Func<DateTimeOffset>? UtcNowProviderForTests;

        private static DateTimeOffset Now() => UtcNowProviderForTests?.Invoke() ?? DateTimeOffset.UtcNow;

        public static void SetRequestInvalidate(System.Action? invalidate) => _requestInvalidate = invalidate;

        /// <summary>Starts or refreshes a blink on the given bar segment and roll caret.</summary>
        public static void Trigger(ThresholdBarPanel panel, int segmentIndex, int roll)
        {
            if (segmentIndex < 0)
                return;

            void Arm()
            {
                var gs = GameSettings.Instance;
                gs.ValidateAndFix();

                _panel = panel;
                _segmentIndex = segmentIndex;
                _roll = roll >= 1 && roll <= 20 ? roll : -1;
                var t = Now();
                _pulseActive = true;
                _tumbleStartAt = t;
                if (_roll > 0)
                    t = t.AddMilliseconds(RollTumbleMs);
                _pulseSequenceStartAt = t;
                _pulseHalfPeriodMs = Math.Max(50, gs.ActionStripSuccessFlashPulseHalfPeriodMs);
                _pulseSequenceEndAt = t.AddMilliseconds(FeedbackDurationMs);

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

            if (t < _pulseSequenceStartAt)
                return false;

            double elapsedMs = (t - _pulseSequenceStartAt).TotalMilliseconds;
            int half = Math.Max(1, _pulseHalfPeriodMs);
            if ((int)(elapsedMs / half) % 2 != 0)
                return false;

            color = AsciiArtAssets.Colors.Gold;
            return true;
        }

        /// <summary>
        /// When active, returns the face (1–20) for a pixel caret under the threshold bar: a tumbling face while the
        /// roll animates, then the base d20 roll for the full flash window (not only the segment pulse on-phase).
        /// </summary>
        public static bool TryGetRollMarker(ThresholdBarPanel panel, out int roll)
        {
            roll = -1;
            if (!_pulseActive || _roll < 1 || panel != _panel)
                return false;

            var t = Now();
            if (t >= _pulseSequenceEndAt)
            {
                ClearFlashState();
                return false;
            }

            roll = t < _pulseSequenceStartAt
                ? TumbleFace(_roll, (t - _tumbleStartAt).TotalMilliseconds / RollTumbleMs)
                : _roll;
            return true;
        }

        /// <summary>
        /// Face a tumbling d20 caret shows at <paramref name="progress"/> (0..1) of its roll: it hops between faces,
        /// slowing as it goes, and shows <paramref name="roll"/> from 1 on. Never repeats the previous hop's face.
        /// </summary>
        public static int TumbleFace(int roll, double progress)
        {
            roll = Math.Clamp(roll, 1, 20);
            if (progress >= 1.0)
                return roll;

            double u = 1.0 - Math.Clamp(progress, 0.0, 1.0);
            int step = (int)((1.0 - u * u) * TumbleSteps);
            // Stride 7 is coprime with 20, so consecutive hops always land on different faces.
            int face = 1 + (roll * 3 + step * 7 + 5) % 20;
            return face == roll ? 1 + face % 20 : face;
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
            _roll = -1;
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
