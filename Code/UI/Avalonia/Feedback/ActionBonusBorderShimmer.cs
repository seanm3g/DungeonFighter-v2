using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Feedback
{
    /// <summary>
    /// Continuous border shimmer for combo-strip cards whose action carries
    /// <see cref="ActionAttackBonuses"/> groups (ACTION/TURN cadence lines on the card).
    /// Hit/miss flash from <see cref="HeroActionStripFeedback"/> still overrides the border.
    /// </summary>
    public static class ActionBonusBorderShimmer
    {
        /// <summary>Timer cadence while any bonus card is visible (~20 fps).</summary>
        private static readonly TimeSpan TimerInterval = TimeSpan.FromMilliseconds(50);

        /// <summary>Keep the timer alive this long after the last strip render that needed shimmer.</summary>
        private const int KeepAliveMs = 200;

        /// <summary>Milliseconds per full sine cycle for the base border color wave.</summary>
        internal const double ColorPhaseMs = 900.0;

        /// <summary>Milliseconds for one full lap of the traveling highlight around the card.</summary>
        internal const double TravelLapMs = 1600.0;

        /// <summary>How many perimeter cells the bright comet covers.</summary>
        internal const int TravelHighlightLength = 3;

        private static System.Action? _requestInvalidate;
        private static DispatcherTimer? _timer;
        private static DateTimeOffset _keepAliveUntil;

        /// <summary>For unit tests: fixed clock; when null, <see cref="DateTimeOffset.UtcNow"/> is used.</summary>
        internal static Func<DateTimeOffset>? UtcNowProviderForTests;

        private static DateTimeOffset Now() => UtcNowProviderForTests?.Invoke() ?? DateTimeOffset.UtcNow;

        /// <summary>Dim cyan used at the low end of the shimmer wave (unselected cards).</summary>
        internal static readonly Color DimCyan = Color.FromRgb(36, 110, 128);

        /// <summary>Bright cyan used at the high end of the shimmer wave.</summary>
        internal static readonly Color BrightCyan = Color.FromRgb(140, 240, 255);

        /// <summary>Cool white used when the next-slot card also has bonuses (keeps the white “next” cue).</summary>
        internal static readonly Color SelectedBright = Color.FromRgb(230, 250, 255);

        public static void SetRequestInvalidate(System.Action? invalidate) => _requestInvalidate = invalidate;

        /// <summary>
        /// True when the action should get a shimmering strip border (any non-empty attack-bonus group).
        /// </summary>
        public static bool ActionHasBonusCue(Action? action) => Action.HasActionAttackBonusGroups(action);

        /// <summary>
        /// Call once per strip render when at least one filled panel needs shimmer so the invalidate timer stays armed.
        /// </summary>
        public static void KeepAlive()
        {
            _keepAliveUntil = Now().AddMilliseconds(KeepAliveMs);
            EnsureTimerStarted();
        }

        /// <summary>Stops the timer (e.g. between tests).</summary>
        internal static void ResetForTests()
        {
            _keepAliveUntil = default;
            UtcNowProviderForTests = null;
            _timer?.Stop();
        }

        /// <summary>
        /// Animated border color for a bonus card. Selected next-slot cards shimmer white↔cool cyan so the next cue remains readable.
        /// </summary>
        public static Color GetBorderColor(bool isSelected, DateTimeOffset? now = null)
        {
            double t = GetWaveT((now ?? Now()).ToUnixTimeMilliseconds() / ColorPhaseMs);
            if (isSelected)
                return ColorValidator.LerpRgb(AsciiArtAssets.Colors.White, SelectedBright, t);
            return ColorValidator.LerpRgb(DimCyan, BrightCyan, t);
        }

        /// <summary>
        /// Dual-sine wave mapped to 0..1 for a less mechanical pulse than a single sine.
        /// </summary>
        internal static double GetWaveT(double phase)
        {
            double raw = Math.Sin(phase) * 0.7 + Math.Sin(phase * 2.37 + 0.4) * 0.3;
            return Math.Clamp((raw + 1.0) * 0.5, 0.0, 1.0);
        }

        /// <summary>Bright comet color that rides the perimeter (slightly ahead of the base wave).</summary>
        public static Color GetTravelHighlightColor(DateTimeOffset? now = null)
        {
            double t = GetWaveT((now ?? Now()).ToUnixTimeMilliseconds() / ColorPhaseMs + 0.6);
            return ColorValidator.LerpRgb(BrightCyan, AsciiArtAssets.Colors.White, t);
        }

        /// <summary>
        /// Character-grid rectangles along the panel perimeter for the traveling highlight comet.
        /// Empty when the panel is too small for a meaningful trail.
        /// </summary>
        public static List<(int X, int Y, int W, int H)> GetTravelHighlightRects(
            int px, int py, int pw, int ph, DateTimeOffset? now = null)
        {
            var result = new List<(int X, int Y, int W, int H)>();
            if (pw < 2 || ph < 2)
                return result;

            var perimeter = BuildPerimeterCells(px, py, pw, ph);
            if (perimeter.Count == 0)
                return result;

            double lap = ((now ?? Now()).ToUnixTimeMilliseconds() / TravelLapMs) % 1.0;
            if (lap < 0) lap += 1.0;
            int head = (int)Math.Floor(lap * perimeter.Count) % perimeter.Count;
            int length = Math.Min(TravelHighlightLength, perimeter.Count);

            for (int i = 0; i < length; i++)
            {
                int idx = (head - i + perimeter.Count) % perimeter.Count;
                var (cx, cy) = perimeter[idx];
                result.Add((cx, cy, 1, 1));
            }

            return result;
        }

        /// <summary>
        /// Ordered perimeter cells (top L→R, right T→B, bottom R→L, left B→T), excluding duplicate corners.
        /// </summary>
        internal static List<(int X, int Y)> BuildPerimeterCells(int px, int py, int pw, int ph)
        {
            var cells = new List<(int X, int Y)>();
            int right = px + pw - 1;
            int bottom = py + ph - 1;

            for (int x = px; x <= right; x++)
                cells.Add((x, py));
            for (int y = py + 1; y <= bottom; y++)
                cells.Add((right, y));
            for (int x = right - 1; x >= px; x--)
                cells.Add((x, bottom));
            for (int y = bottom - 1; y > py; y--)
                cells.Add((px, y));

            return cells;
        }

        private static void EnsureTimerStarted()
        {
            void Arm()
            {
                if (_timer == null)
                {
                    _timer = new DispatcherTimer { Interval = TimerInterval };
                    _timer.Tick += OnTimerTick;
                }

                if (!_timer.IsEnabled)
                    _timer.Start();
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

        private static void OnTimerTick(object? sender, EventArgs e)
        {
            if (Now() >= _keepAliveUntil)
            {
                _timer?.Stop();
                return;
            }

            _requestInvalidate?.Invoke();
        }
    }
}
