using System;
using Avalonia.Threading;
using RPGGame.Combat.Sequence;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Frame clock for the combat board's middle section. Each sequence column (<see cref="CombatSequenceHudState"/>)
    /// plays a short motion — die tumble, branch pick, strike line, card slide — measured from the column's start time.
    /// While a motion is unfinished the board calls <see cref="RequestFrame"/>, which schedules one more layout render;
    /// the clock stops on its own once every motion has settled.
    /// </summary>
    public static class CombatBoardAnimation
    {
        private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(33);

        /// <summary>Card slide (next → now → prev) after a swing finishes.</summary>
        public const int SlideMs = 420;

        private static System.Action? _requestRender;
        private static DispatcherTimer? _timer;

        public static void SetRequestRender(System.Action? render) => _requestRender = render;

        /// <summary>
        /// Length of one sequence beat on screen; motions finish inside it so the next column starts from rest.
        /// Clamped so a very fast or very slow combat speed still reads as motion.
        /// </summary>
        public static int BeatMs => Math.Clamp(CombatDelayManager.GetSequenceHudBeatDelayMs(), 120, 900);

        /// <summary>0..1 progress of the active column's motion (1 when there is no live swing).</summary>
        public static double PhaseProgress(int durationMs) =>
            Progress(CombatSequenceHudState.PhaseStartedUtc, durationMs);

        /// <summary>0..1 progress of the post-swing card slide; 1 when no swing has finished recently.</summary>
        public static double SlideProgress() =>
            CombatSequenceHudState.IsFinished ? Progress(CombatSequenceHudState.FinishedUtc, SlideMs) : 1.0;

        public static double Progress(DateTimeOffset start, int durationMs)
        {
            if (start == default || durationMs <= 0)
                return 1.0;
            double t = (DateTimeOffset.UtcNow - start).TotalMilliseconds / durationMs;
            return Math.Clamp(t, 0.0, 1.0);
        }

        /// <summary>Ease-out cubic: fast start, soft landing.</summary>
        public static double EaseOut(double t)
        {
            t = Math.Clamp(t, 0.0, 1.0);
            double u = 1.0 - t;
            return 1.0 - u * u * u;
        }

        public static int Lerp(int a, int b, double t) => (int)Math.Round(a + (b - a) * t);

        public static double Lerp(double a, double b, double t) => a + (b - a) * t;

        /// <summary>Asks for another board render in about one frame. Safe to call every paint.</summary>
        public static void RequestFrame()
        {
            if (_requestRender == null)
                return;
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(RequestFrame, DispatcherPriority.Background);
                return;
            }

            if (_timer == null)
            {
                _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = FrameInterval };
                _timer.Tick += (_, _) =>
                {
                    // One-shot: the render that follows re-arms it if motion is still running.
                    _timer!.Stop();
                    _requestRender?.Invoke();
                };
            }

            if (!_timer.IsEnabled)
                _timer.Start();
        }
    }
}
