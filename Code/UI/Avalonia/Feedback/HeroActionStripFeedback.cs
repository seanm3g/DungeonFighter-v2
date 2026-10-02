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
        /// <summary>Connected hit that is not a combo-action hit. The strip border stays its steady white (or gray) color.</summary>
        Hit,
        /// <summary>Successful combo-action hit.</summary>
        ComboComplete
    }

    /// <summary>
    /// Border feedback on the combo action strip after the hero resolves an action.
    /// A miss pulses red. A normal hit does not pulse: the card keeps its solid white (selected) or gray border.
    /// A combo-action hit pulses gold. Durations come from
    /// <see cref="GameSettings.ActionStripMissFlashDurationMs"/> (miss) or the success flash settings (combo).
    /// Successful combos also hold the white next-border on the firing panel until
    /// <see cref="ReleaseSelectedHold"/> when the action block finishes.
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

        private static int _queuedPanelIndex = -1;
        private static HeroActionStripFlashKind _queuedKind;

        /// <summary>
        /// While set, the strip keeps this panel as the white "next" selection even if
        /// <see cref="Character.ComboStep"/> already advanced (successful combo during an open action block).
        /// </summary>
        private static int _heldSelectedPanelIndex = -1;

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
                        // Normal hits keep the steady strip border (selected slot is solid white).
                        ClearFlashState();
                        _requestInvalidate?.Invoke();
                        return;
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
        /// Hold strip flash until the combat-log punchline so miss/hit color does not leak during setup.
        /// A successful combo also holds the white "next" selection on the firing panel until
        /// <see cref="ReleaseSelectedHold"/> (end of the action block).
        /// </summary>
        public static void QueueForPunchline(int panelIndex, HeroActionStripFlashKind kind)
        {
            if (panelIndex < 0)
                return;
            _queuedPanelIndex = panelIndex;
            _queuedKind = kind;
            // ComboStep already advanced in execution; keep the white next-border on the swing
            // that is still playing until the action block finishes.
            if (kind == HeroActionStripFlashKind.ComboComplete)
                _heldSelectedPanelIndex = panelIndex;
        }

        /// <summary>Start a previously queued strip flash, if any.</summary>
        public static void CommitQueued()
        {
            if (_queuedPanelIndex < 0)
                return;
            int index = _queuedPanelIndex;
            var kind = _queuedKind;
            _queuedPanelIndex = -1;
            Trigger(index, kind);
        }

        /// <summary>Drop a queued flash when the action block is not shown.</summary>
        public static void ClearQueued()
        {
            _queuedPanelIndex = -1;
            ReleaseSelectedHold();
        }

        /// <summary>
        /// When a successful combo is still presenting, returns the panel that should keep the white next-border.
        /// </summary>
        public static bool TryGetHeldSelectedIndex(out int panelIndex)
        {
            if (_heldSelectedPanelIndex < 0)
            {
                panelIndex = -1;
                return false;
            }

            panelIndex = _heldSelectedPanelIndex;
            return true;
        }

        /// <summary>
        /// Clears the deferred next-border hold so the strip follows live <see cref="Character.ComboStep"/>.
        /// Called when the combat action block finishes (or is skipped).
        /// </summary>
        public static void ReleaseSelectedHold()
        {
            if (_heldSelectedPanelIndex < 0)
                return;
            _heldSelectedPanelIndex = -1;
            _requestInvalidate?.Invoke();
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
            _queuedPanelIndex = -1;
            _heldSelectedPanelIndex = -1;
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
