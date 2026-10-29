using RPGGame;
using System;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Board-style combat layout: enemy combo strip across the top, enemy / hero health in the
    /// center column with the hero's previous → current → next combo cards between them, and the
    /// hero strip moved to the bottom of the center column. Replaces the framed combat log while
    /// <see cref="IsActive"/>; F9 toggles back to the log view.
    /// </summary>
    public static class CombatBoardLayout
    {
        /// <summary>User preference (F9). When false, Combat keeps the classic strip + log layout.</summary>
        public static bool Enabled { get; private set; } = true;

        /// <summary>True while the board owns the center column (Enabled and in Combat).</summary>
        public static bool IsActive { get; private set; }

        /// <summary>
        /// Between fights of a dungeon run (room text, next-room choice) and on the completion / death screens while
        /// <see cref="Enabled"/>: the hero strip stays at the bottom and the log sits in a framed box like the board's
        /// log view (<see cref="InterludeBoxRect"/>) instead of the classic strip + full-width log.
        /// </summary>
        public static bool IsInterlude { get; private set; }

        /// <summary>
        /// <see cref="IsInterlude"/> mid-run (room text, next-room choice) and on the completion screen, not death: the log
        /// box takes the whole column over the last room's backdrop, with no hero health bar or hero strip below it.
        /// </summary>
        public static bool IsBetweenFights { get; private set; }

        /// <summary>Board geometry owns the center column: hero strip at the bottom, no sequence HUD band.</summary>
        public static bool OwnsCenterColumn => IsActive || IsInterlude;

        /// <summary>Middle view: false = previous / current / next combo cards, true = framed combat log. Click toggles.</summary>
        public static bool ShowLog { get; private set; }

        /// <summary>Clickable middle region between the health bars, recorded by the last board paint.</summary>
        public static (int X, int Y, int Width, int Height) MiddleRect { get; set; }

        /// <summary>The NOW card on the grid from the last board paint, or null when the cards aren't showing.</summary>
        public static (int X, int Y, int W, int H)? NowRect { get; set; }

        /// <summary>
        /// The LAST, NOW and NEXT cards together (with the label row above them) from the last board paint, or null when
        /// the cards aren't showing. The turn callout opens out of NOW to fill it.
        /// </summary>
        public static (int X, int Y, int W, int H)? FlowCardsRect { get; set; }

        /// <summary>True while the board itself paints the log box, letting the display renderer through.</summary>
        public static bool IsPaintingBoardLog { get; set; }

        public static bool ContainsMiddle(int gridX, int gridY)
        {
            var r = MiddleRect;
            return IsActive && r.Width > 0 && r.Height > 0
                && gridX >= r.X && gridX < r.X + r.Width
                && gridY >= r.Y && gridY < r.Y + r.Height;
        }

        public static bool ToggleMiddleView()
        {
            ShowLog = !ShowLog;
            return ShowLog;
        }

        public static bool Toggle()
        {
            Enabled = !Enabled;
            if (OwnsCenterColumn && !Enabled)
                _pendingTeardown = true;
            IsActive = Enabled && IsActive;
            IsInterlude = Enabled && IsInterlude;
            IsBetweenFights = IsInterlude && IsBetweenFights;
            return Enabled;
        }

        public static bool ShouldBeActive(GameState? state) => Enabled && state == GameState.Combat;

        public static bool ShouldBeInterlude(GameState? state, bool inDungeonRun) =>
            Enabled && ((state == GameState.Dungeon && inDungeonRun)
                || state == GameState.DungeonCompletion
                || state == GameState.Death);

        /// <summary>Aligns <see cref="IsActive"/> / <see cref="IsInterlude"/> with <paramref name="state"/>; no-op when state is unknown.</summary>
        public static void Sync(GameState? state, bool inDungeonRun)
        {
            if (state == null)
                return;
            bool next = ShouldBeActive(state);
            bool nextInterlude = !next && ShouldBeInterlude(state, inDungeonRun);
            if ((IsActive && !next) || (IsInterlude && !nextInterlude))
                _pendingTeardown = true;
            IsActive = next;
            IsInterlude = nextInterlude;
            IsBetweenFights = nextInterlude && (state == GameState.Dungeon || state == GameState.DungeonCompletion);
        }

        /// <summary>Width of the board's health bars and log box, centered in a <paramref name="columnWidth"/> column.</summary>
        public static int BarWidth(int columnWidth) =>
            Math.Max(1, Math.Clamp(columnWidth * 11 / 20, Math.Min(20, columnWidth - 12), columnWidth - 12));

        /// <summary>Hero health bar row on the board (name sits one row below), counted up from the hero strip.</summary>
        public static int HeroBarY => LayoutConstants.ACTION_INFO_Y - 3;

        /// <summary>
        /// Framed log box for <see cref="IsInterlude"/>: same width as the board's log view, from the top of the column
        /// down to just above the hero health bar (to the bottom of the column <see cref="IsBetweenFights"/>).
        /// </summary>
        public static (int X, int Y, int W, int H) InterludeBoxRect()
        {
            int colW = LayoutConstants.CENTER_PANEL_WIDTH;
            int w = BarWidth(colW);
            int x = LayoutConstants.CENTER_PANEL_X + (colW - w) / 2;
            const int y = 1;
            int bottom = IsBetweenFights ? LayoutConstants.SCREEN_HEIGHT - 1 : HeroBarY - 1;
            return (x, y, w, Math.Max(4, bottom - y));
        }

        private static bool _pendingTeardown;

        /// <summary>
        /// After the board switches off or between fight and interlude (fight over, F9), removes what only the board paints in the center column:
        /// its health / d20 bars, connector lines, card boxes and text. Partial layout renders do not clear the
        /// canvas, so without this they linger under the dungeon log between fights.
        /// </summary>
        public static void TeardownIfNeeded(GameCanvasControl canvas)
        {
            if (!_pendingTeardown || canvas == null)
                return;
            _pendingTeardown = false;
            int x = LayoutConstants.CENTER_PANEL_X;
            int w = LayoutConstants.CENTER_PANEL_WIDTH;
            int h = LayoutConstants.SCREEN_HEIGHT + 1;
            canvas.ClearTextInArea(x, 0, w, h);
            canvas.ClearBoxesInArea(x, 0, w, h);
            canvas.ClearProgressBarsInArea(x, 0, w, h);
            canvas.ClearSegmentedBarsInArea(x, 0, w, h);
            canvas.ClearLinesInArea(x, 0, w, h);
        }
    }
}
