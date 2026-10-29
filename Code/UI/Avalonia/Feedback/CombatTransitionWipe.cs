using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia.Feedback
{
    /// <summary>
    /// Shutter over the center column when a fight starts or ends, so the swap between the dungeon log and the combat
    /// board reads as a transition instead of a cut. Entering combat the column snaps dark with a red slash across its
    /// middle, then two bars part up and down; leaving combat two bars part left and right along gold edges.
    /// Driven by <see cref="GameStateManager.StateChanged"/>; the canvas paints it after everything else.
    /// </summary>
    public static class CombatTransitionWipe
    {
        public enum Kind { None, Enter, Exit }

        /// <summary>Time the column stays dark before the bars move, so the cut registers.</summary>
        internal const int HoldMs = 90;
        internal const int EnterOpenMs = 460;
        internal const int ExitOpenMs = 380;

        private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(16);
        private static DispatcherTimer? _timer;
        private static System.Action? _refresh;

        private static readonly Color ShutterColor = Color.FromRgb(6, 4, 8);
        private static readonly Color EnterEdge = Color.FromRgb(255, 50, 40);
        private static readonly Color ExitEdge = Color.FromRgb(255, 200, 40);

        private static Kind _kind;
        private static DateTimeOffset _startedUtc;

        public static Kind Current => _kind;

        /// <summary>Repaints the canvas; called on the UI thread.</summary>
        public static void SetRefresh(System.Action? refresh) => _refresh = refresh;

        /// <summary>Starts the shutter when the state crosses into or out of <see cref="GameState.Combat"/>.</summary>
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

        /// <summary>0 while the column is fully covered, rising to 1 when the bars are gone.</summary>
        internal static double Openness(Kind kind, double elapsedMs)
        {
            if (kind == Kind.None)
                return 1.0;
            int openMs = kind == Kind.Enter ? EnterOpenMs : ExitOpenMs;
            return CombatBoardAnimation.EaseOut((elapsedMs - HoldMs) / openMs);
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
                    // One frame past the end so the last paint clears the bars.
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

        /// <summary>Paints the shutter over the center column (full content height).</summary>
        public static void Paint(DrawingContext context, double contentHeight, double charWidth)
        {
            Kind kind = _kind;
            if (kind == Kind.None || charWidth <= 0 || contentHeight <= 0)
                return;
            double elapsed = ElapsedMs;
            double open = Openness(kind, elapsed);
            if (open >= 1.0)
                return;

            double x = Math.Round(LayoutConstants.CENTER_PANEL_X * charWidth);
            double w = Math.Round(LayoutConstants.CENTER_PANEL_WIDTH * charWidth);
            double h = contentHeight;
            var shutter = new SolidColorBrush(ShutterColor);
            // Edges glow brightest at the cut and fade as the bars leave.
            byte edgeAlpha = (byte)Math.Round(255 * (1.0 - open * 0.6));
            const double edgeThickness = 3;

            if (kind == Kind.Enter)
            {
                double half = h / 2;
                double gap = half * open;
                var edge = new SolidColorBrush(Color.FromArgb(edgeAlpha, EnterEdge.R, EnterEdge.G, EnterEdge.B));
                context.FillRectangle(shutter, new Rect(x, 0, w, half - gap));
                context.FillRectangle(shutter, new Rect(x, half + gap, w, h - half - gap));
                if (open <= 0)
                {
                    // The slash: a thin line across the middle that widens from the center during the hold.
                    double grow = CombatBoardAnimation.EaseOut(elapsed / HoldMs);
                    double sw = w * grow;
                    context.FillRectangle(edge, new Rect(x + (w - sw) / 2, half - edgeThickness / 2, sw, edgeThickness));
                }
                else
                {
                    context.FillRectangle(edge, new Rect(x, half - gap - edgeThickness, w, edgeThickness));
                    context.FillRectangle(edge, new Rect(x, half + gap, w, edgeThickness));
                    // Red wash over the revealed board, gone by the time the bars are.
                    byte wash = (byte)Math.Round(70 * (1.0 - open) * (1.0 - open));
                    if (wash > 0)
                        context.FillRectangle(new SolidColorBrush(Color.FromArgb(wash, EnterEdge.R, 0, 0)),
                            new Rect(x, half - gap, w, gap * 2));
                }
                return;
            }

            double halfW = w / 2;
            double gapW = halfW * open;
            var goldEdge = new SolidColorBrush(Color.FromArgb(edgeAlpha, ExitEdge.R, ExitEdge.G, ExitEdge.B));
            context.FillRectangle(shutter, new Rect(x, 0, halfW - gapW, h));
            context.FillRectangle(shutter, new Rect(x + halfW + gapW, 0, w - halfW - gapW, h));
            if (open > 0)
            {
                context.FillRectangle(goldEdge, new Rect(x + halfW - gapW - edgeThickness, 0, edgeThickness, h));
                context.FillRectangle(goldEdge, new Rect(x + halfW + gapW, 0, edgeThickness, h));
            }
        }
    }
}
