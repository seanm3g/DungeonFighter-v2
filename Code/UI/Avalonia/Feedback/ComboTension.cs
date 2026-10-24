using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Combat.Sequence;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Feedback
{
    /// <summary>
    /// Anticipation cue for walking a combo: one "charge" value (0 on the opener, 1 when the finisher is next)
    /// drives the strip card states, an accelerating glow around the next card, and a screen vignette.
    /// Card states are baked when the strip is built; the glow and vignette are painted every frame by
    /// <see cref="GameCanvasControl"/> via <see cref="Paint"/> so they animate without rebuilding the layout.
    /// A miss resets <see cref="Character.ComboStep"/> to 0, which drops charge back to 0 and clears the cue.
    /// </summary>
    public static class ComboTension
    {
        /// <summary>Glow pulse period on the step after the opener (slow breathing).</summary>
        internal const double PulsePeriodSlowMs = 1400.0;

        /// <summary>Glow pulse period when the finisher is next (fast heartbeat).</summary>
        internal const double PulsePeriodFastMs = 420.0;

        /// <summary>Peak alpha of the vignette edge at full charge (left/right/bottom bands).</summary>
        internal const double VignetteMaxAlpha = 0.42;

        /// <summary>Band depth as a fraction of the content width/height.</summary>
        internal const double VignetteBandFraction = 0.16;

        private static readonly TimeSpan TimerInterval = TimeSpan.FromMilliseconds(33);

        /// <summary>Cool white for the glow on early steps.</summary>
        internal static readonly Color GlowCool = Color.FromRgb(170, 225, 255);

        /// <summary>Warm amber the glow shifts toward as charge rises.</summary>
        internal static readonly Color GlowWarm = Color.FromRgb(255, 170, 60);

        /// <summary>Gold glow / border for the finisher card.</summary>
        internal static readonly Color FinisherGold = Color.FromRgb(255, 200, 70);

        /// <summary>Vignette edge color: neutral black early, ember red at the finisher.</summary>
        private static readonly Color VignetteEarly = Color.FromRgb(0, 0, 0);
        private static readonly Color VignetteFinisher = Color.FromRgb(48, 6, 0);

        private static System.Action? _requestInvalidate;
        private static DispatcherTimer? _timer;
        private static WeakReference<Character>? _tracked;

        /// <summary>For unit tests: fixed clock; when null, <see cref="DateTimeOffset.UtcNow"/> is used.</summary>
        internal static Func<DateTimeOffset>? UtcNowProviderForTests;

        private static DateTimeOffset Now() => UtcNowProviderForTests?.Invoke() ?? DateTimeOffset.UtcNow;

        public static void SetRequestInvalidate(System.Action? invalidate) => _requestInvalidate = invalidate;

        /// <summary>Called by the strip renderer with the hero the strip shows (null clears tracking).</summary>
        public static void Track(Character? player)
        {
            _tracked = player == null ? null : new WeakReference<Character>(player);
        }

        /// <summary>
        /// 0 on the opener, rising linearly to 1 when the last filled slot is next. Needs at least two filled slots.
        /// </summary>
        public static double GetCharge(int nextIndex, int filled)
        {
            if (filled < 2 || nextIndex <= 0)
                return 0.0;
            return Math.Clamp(nextIndex / (double)(filled - 1), 0.0, 1.0);
        }

        /// <summary>True when the next slot is the last filled one of a combo with at least two slots.</summary>
        public static bool IsFinisherNext(int nextIndex, int filled) => filled >= 2 && nextIndex == filled - 1;

        /// <summary>Slots before the next one have already fired this run of the combo.</summary>
        public static bool IsSpent(int panelIndex, int nextIndex) => nextIndex > 0 && panelIndex < nextIndex;

        /// <summary>Live next-slot index for <paramref name="player"/>, honoring the held white border after a combo hit.</summary>
        public static int ResolveNextIndex(Character player, int filled)
        {
            if (filled <= 0)
                return -1;
            int next = player.ComboStep % filled;
            if (HeroActionStripFeedback.TryGetHeldSelectedIndex(out int held) && held >= 0 && held < filled)
                next = held;
            return next;
        }

        /// <summary>Pulse period: slow early, fast near the finisher.</summary>
        internal static double GetPulsePeriodMs(double charge) =>
            PulsePeriodSlowMs + (PulsePeriodFastMs - PulsePeriodSlowMs) * Math.Clamp(charge, 0.0, 1.0);

        /// <summary>0..1 pulse wave; the phase is continuous in charge so the speed-up does not jump.</summary>
        internal static double GetPulseT(double charge, DateTimeOffset now)
        {
            double phase = now.ToUnixTimeMilliseconds() / GetPulsePeriodMs(charge) * Math.PI * 2.0;
            return (Math.Sin(phase) + 1.0) * 0.5;
        }

        /// <summary>
        /// Paints the next-card glow and the vignette. Call inside the content-origin transform, after the
        /// grid elements. No-op outside the dungeon/combat chrome or when charge is 0.
        /// </summary>
        public static void Paint(DrawingContext context, double contentWidth, double contentHeight, double charWidth, double charHeight)
        {
            if (!TryGetLiveState(out int filled, out int next, out double charge) || charge <= 0.0)
                return;

            var now = Now();
            double pulse = GetPulseT(charge, now);
            bool finisher = IsFinisherNext(next, filled);

            PaintVignette(context, contentWidth, contentHeight, charge, pulse, finisher);

            // The board layout draws its own cards elsewhere; only the strip has fixed rects to glow around.
            if (!CombatBoardLayout.IsActive)
            {
                int displayCount = ActionInfoStripLayout.GetDisplayPanelCount(filled);
                ActionInfoStripLayout.GetPanelRect(next, displayCount, out int px, out int py, out int pw, out int ph);
                var rect = new Rect(px * charWidth, py * charHeight, pw * charWidth, ph * charHeight);
                PaintGlow(context, rect, charge, pulse, finisher);
            }

            EnsureTimerStarted();
        }

        private static bool TryGetLiveState(out int filled, out int next, out double charge)
        {
            filled = 0;
            next = -1;
            charge = 0.0;
            if (!CombatSequenceHudState.IsBandReserved)
                return false;
            if (_tracked == null || !_tracked.TryGetTarget(out var player))
                return false;
            var actions = player.GetComboActions();
            filled = actions?.Count ?? 0;
            next = ResolveNextIndex(player, filled);
            int effectiveMax = ComboSequenceMaxHelper.GetEffectiveMax(player);
            if (next < 0 || next >= effectiveMax)
                return false;
            charge = GetCharge(next, filled);
            return true;
        }

        private static void PaintGlow(DrawingContext context, Rect rect, double charge, double pulse, bool finisher)
        {
            var baseColor = finisher ? FinisherGold : ColorValidator.LerpRgb(GlowCool, GlowWarm, charge);
            // Stronger and wider as charge rises; the pulse swings between a floor and the peak.
            double peak = 0.35 + 0.5 * charge;
            double alpha = peak * (0.35 + 0.65 * pulse);
            int rings = finisher ? 4 : 2 + (int)Math.Round(charge);
            for (int r = 0; r < rings; r++)
            {
                double inset = -1.5 - r * 2.0;
                double ringAlpha = alpha * (1.0 - r / (double)(rings + 1));
                var pen = new Pen(new SolidColorBrush(baseColor, ringAlpha), 2.0);
                context.DrawRectangle(null, pen, rect.Inflate(-inset));
            }
        }

        private static void PaintVignette(DrawingContext context, double w, double h, double charge, double pulse, bool finisher)
        {
            if (w <= 0 || h <= 0)
                return;
            // Ease-in so early steps stay almost clean and the last steps carry the weight.
            double strength = charge * charge;
            double alpha = VignetteMaxAlpha * strength * (0.88 + 0.12 * pulse);
            var edge = finisher ? VignetteFinisher : VignetteEarly;
            var solid = Color.FromArgb((byte)Math.Round(alpha * 255), edge.R, edge.G, edge.B);
            var clear = Color.FromArgb(0, edge.R, edge.G, edge.B);
            double bandX = w * VignetteBandFraction;
            double bandY = h * VignetteBandFraction;

            // Top is left open so the action strip itself stays bright.
            FillBand(context, new Rect(0, 0, bandX, h), new RelativePoint(0, 0.5, RelativeUnit.Relative), new RelativePoint(1, 0.5, RelativeUnit.Relative), solid, clear);
            FillBand(context, new Rect(w - bandX, 0, bandX, h), new RelativePoint(1, 0.5, RelativeUnit.Relative), new RelativePoint(0, 0.5, RelativeUnit.Relative), solid, clear);
            FillBand(context, new Rect(0, h - bandY, w, bandY), new RelativePoint(0.5, 1, RelativeUnit.Relative), new RelativePoint(0.5, 0, RelativeUnit.Relative), solid, clear);
        }

        private static void FillBand(DrawingContext context, Rect rect, RelativePoint start, RelativePoint end, Color solid, Color clear)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = start,
                EndPoint = end,
                GradientStops = { new GradientStop(solid, 0.0), new GradientStop(clear, 1.0) }
            };
            context.FillRectangle(brush, rect);
        }

        private static void EnsureTimerStarted()
        {
            if (Application.Current == null)
                return;
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
            _requestInvalidate?.Invoke();
            // Stop once the combo drops back to the opener; the next paint with charge re-arms it.
            if (!TryGetLiveState(out _, out _, out double charge) || charge <= 0.0)
                _timer?.Stop();
        }

        /// <summary>Stops the timer and clears tracking (e.g. between tests).</summary>
        internal static void ResetForTests()
        {
            _tracked = null;
            UtcNowProviderForTests = null;
            _timer?.Stop();
        }
    }
}
