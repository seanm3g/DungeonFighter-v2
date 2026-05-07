using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;

namespace RPGGame.UI.Avalonia.Feedback
{
    /// <summary>How to flash a combo strip panel after the hero resolves a swing.</summary>
    public enum HeroActionStripFlashKind
    {
        Miss,
        /// <summary>Connected hit that is not the combo-chain completion.</summary>
        Hit,
        /// <summary>Combo chain completion (last strip slot on a multi-step sequence, or a tagged finisher there).</summary>
        ComboComplete
    }

    /// <summary>
    /// Border feedback on the combo action strip after the hero resolves an action:
    /// miss / hit / combo-complete each use the same on/off pulse; total time and half-period come from
    /// <see cref="GameSettings.ActionStripMissFlashDurationMs"/> (miss) or success flash settings (hit and combo).
    /// Colors: red, green, gold respectively.
    /// </summary>
    public static class HeroActionStripFeedback
    {
        /// <summary>Strip border stroke width (device pixels) while a hit/miss/combo flash is active on that panel.</summary>
        public const int FlashBorderThicknessPixels = 3;

        private static readonly TimeSpan TimerInterval = TimeSpan.FromMilliseconds(16);

        private static System.Action? _requestInvalidate;
        private static DispatcherTimer? _timer;

        private static int _flashPanelIndex = -1;
        private static DateTimeOffset _pulseSequenceEndAt;
        private static DateTimeOffset _pulseSequenceStartAt;
        private static int _pulseHalfPeriodMs = 150;
        private static bool _pulseActive;
        private static Color _pulseOnColor;

        /// <summary>For unit tests: fixed clock; when null, <see cref="DateTimeOffset.UtcNow"/> is used.</summary>
        internal static Func<DateTimeOffset>? UtcNowProviderForTests;

        private static DateTimeOffset Now() => UtcNowProviderForTests?.Invoke() ?? DateTimeOffset.UtcNow;

        /// <summary>
        /// Registers a callback that forces the main game canvas to repaint (e.g. <see cref="GameCanvasControl.Refresh"/>).
        /// </summary>
        public static void SetRequestInvalidate(System.Action? invalidate) => _requestInvalidate = invalidate;

        /// <summary>
        /// Starts or refreshes feedback on the given strip panel index (0-based).
        /// </summary>
        public static void Trigger(int panelIndex, HeroActionStripFlashKind kind)
        {
            if (panelIndex < 0)
                return;

            void Arm()
            {
                var gs = GameSettings.Instance;
                gs.ValidateAndFix();

                _flashPanelIndex = panelIndex;
                var t = Now();
                _pulseActive = true;
                _pulseSequenceStartAt = t;
                _pulseHalfPeriodMs = Math.Max(50, gs.ActionStripSuccessFlashPulseHalfPeriodMs);

                switch (kind)
                {
                    case HeroActionStripFlashKind.Miss:
                        _pulseOnColor = AsciiArtAssets.Colors.Red;
                        _pulseSequenceEndAt = t.AddMilliseconds(gs.ActionStripMissFlashDurationMs);
                        break;
                    case HeroActionStripFlashKind.Hit:
                        _pulseOnColor = AsciiArtAssets.Colors.Green;
                        _pulseSequenceEndAt = t.AddMilliseconds(gs.ActionStripSuccessFlashDurationMs);
                        break;
                    case HeroActionStripFlashKind.ComboComplete:
                        _pulseOnColor = AsciiArtAssets.Colors.Gold;
                        _pulseSequenceEndAt = t.AddMilliseconds(gs.ActionStripSuccessFlashDurationMs);
                        break;
                    default:
                        ClearFlashState();
                        return;
                }

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
        /// When a swing result flash is playing on <paramref name="panelIndex"/>, the panel uses a thicker border for the full sequence (on and off pulse phases).
        /// </summary>
        public static bool IsFlashEmphasisActive(int panelIndex)
        {
            if (_flashPanelIndex < 0 || panelIndex != _flashPanelIndex)
                return false;

            var t = Now();

            if (!_pulseActive)
            {
                ClearFlashState();
                return false;
            }

            if (t >= _pulseSequenceEndAt)
            {
                ClearFlashState();
                return false;
            }

            return true;
        }

        /// <summary>
        /// When active, overrides the strip panel border color for <paramref name="panelIndex"/>.
        /// Returns <c>false</c> during pulse “off” phases so the normal strip border shows through.
        /// </summary>
        public static bool TryGetBorderOverride(int panelIndex, out Color color)
        {
            color = default;
            if (_flashPanelIndex < 0 || panelIndex != _flashPanelIndex)
                return false;

            var t = Now();

            if (!_pulseActive)
            {
                ClearFlashState();
                return false;
            }

            if (t >= _pulseSequenceEndAt)
            {
                ClearFlashState();
                return false;
            }

            double elapsedMs = (t - _pulseSequenceStartAt).TotalMilliseconds;
            int half = Math.Max(1, _pulseHalfPeriodMs);
            int phase = (int)(elapsedMs / half) % 2;
            if (phase != 0)
                return false;

            color = _pulseOnColor;
            return true;
        }

        /// <summary>Clears flash state (e.g. between tests).</summary>
        internal static void ResetForTests()
        {
            ClearFlashState();
            UtcNowProviderForTests = null;
            _timer?.Stop();
        }

        private static void ClearFlashState()
        {
            _flashPanelIndex = -1;
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
            if (_flashPanelIndex < 0)
            {
                _timer?.Stop();
                return;
            }

            var t = Now();
            if (_pulseActive && t >= _pulseSequenceEndAt)
                ClearFlashState();

            if (_flashPanelIndex < 0)
                _timer?.Stop();

            _requestInvalidate?.Invoke();
        }
    }
}
