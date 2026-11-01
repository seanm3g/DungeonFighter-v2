using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia.Feedback
{
    /// <summary>
    /// Veil over the center column when a fight starts or ends, so the swap between the dungeon log and the combat
    /// board reads as a transition instead of a cut. Muted on purpose: near-black, no hot colors. Entering and leaving
    /// combat alike, it lifts off the top with a soft lower edge and thins as it goes.
    /// Driven by <see cref="GameStateManager.StateChanged"/>; the canvas paints it after everything else. The first
    /// turn callout of a fight waits for it (<see cref="RemainingMs"/>).
    /// </summary>
    public static class CombatTransitionWipe
    {
        public enum Kind { None, Enter, Exit }

        /// <summary>Time the column stays dark before the veil moves, so the cut registers.</summary>
        internal const int HoldMs = 60;
        internal const int OpenMs = 640;

        private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(16);
        private static DispatcherTimer? _timer;
        private static System.Action? _refresh;

        private static readonly Color VeilColor = Color.FromRgb(8, 8, 10);

        private static Kind _kind;
        private static DateTimeOffset _startedUtc;

        public static Kind Current => _kind;

        /// <summary>Milliseconds until the veil is gone; 0 when none is playing.</summary>
        public static int RemainingMs
        {
            get
            {
                if (_kind == Kind.None)
                    return 0;
                return (int)Math.Ceiling(Math.Max(0, HoldMs + OpenMs - ElapsedMs));
            }
        }

        /// <summary>Repaints the canvas; called on the UI thread.</summary>
        public static void SetRefresh(System.Action? refresh) => _refresh = refresh;

        /// <summary>Starts the veil when the state crosses into or out of <see cref="GameState.Combat"/>.</summary>
        public static void OnStateChanged(GameState previous, GameState next)
        {
            Kind kind = KindFor(previous, next);
            if (kind == Kind.None)
                return;
            _kind = kind;
            _startedUtc = DateTimeOffset.UtcNow;
            if (Dispatcher.UIThread.CheckAccess())
                EnsureTimer();
            else
                Dispatcher.UIThread.Post(EnsureTimer, DispatcherPriority.Render);
        }

        internal static Kind KindFor(GameState previous, GameState next)
        {
            if (previous == next)
                return Kind.None;
            if (next == GameState.Combat)
                return Kind.Enter;
            return previous == GameState.Combat ? Kind.Exit : Kind.None;
        }

        /// <summary>0 while the column is fully covered, rising to 1 when the veil is gone.</summary>
        internal static double Openness(Kind kind, double elapsedMs)
        {
            if (kind == Kind.None)
                return 1.0;
            return EaseInOut((elapsedMs - HoldMs) / OpenMs);
        }

        /// <summary>Smoothstep: gentle start and landing, no snap.</summary>
        internal static double EaseInOut(double t)
        {
            t = Math.Clamp(t, 0.0, 1.0);
            return t * t * (3.0 - 2.0 * t);
        }

        private static double ElapsedMs => (DateTimeOffset.UtcNow - _startedUtc).TotalMilliseconds;

        private static void EnsureTimer()
        {
            if (_refresh == null)
                return;
            if (_timer == null)
            {
                _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = FrameInterval };
                _timer.Tick += (_, _) =>
                {
                    // One frame past the end so the last paint clears the veil.
                    if (_kind == Kind.None || Openness(_kind, ElapsedMs) >= 1.0)
                    {
                        _kind = Kind.None;
                        _timer!.Stop();
                    }
                    _refresh?.Invoke();
                };
            }
            if (!_timer.IsEnabled)
                _timer.Start();
        }

        /// <summary>Paints the veil over the center column (full content height).</summary>
        public static void Paint(DrawingContext context, double contentHeight, double charWidth)
        {
            Kind kind = _kind;
            if (kind == Kind.None || charWidth <= 0 || contentHeight <= 0)
                return;
            double open = Openness(kind, ElapsedMs);
            if (open >= 1.0)
                return;

            double x = Math.Round(LayoutConstants.CENTER_PANEL_X * charWidth);
            double w = Math.Round(LayoutConstants.CENTER_PANEL_WIDTH * charWidth);
            double h = contentHeight;
            // The veil thins as it moves so its edge never reads as a hard flash line.
            byte alpha = (byte)Math.Round(255 * (1.0 - 0.45 * open));
            var veil = new SolidColorBrush(Color.FromArgb(alpha, VeilColor.R, VeilColor.G, VeilColor.B));

            // Lifting: the veil rises off the top, its lower edge a soft gradient rather than a line.
            double feather = Math.Max(8, h * 0.2);
            double bottom = (h + feather) * (1.0 - open);
            double solid = Math.Max(0, bottom - feather);
            if (solid > 0)
                context.FillRectangle(veil, new Rect(x, 0, w, Math.Min(h, solid)));
            if (bottom > 0 && solid < h)
            {
                var fade = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.FromArgb(alpha, VeilColor.R, VeilColor.G, VeilColor.B), 0),
                        new GradientStop(Color.FromArgb(0, VeilColor.R, VeilColor.G, VeilColor.B), 1),
                    }
                };
                context.FillRectangle(fade, new Rect(x, solid, w, bottom - solid));
            }
        }
    }
}
