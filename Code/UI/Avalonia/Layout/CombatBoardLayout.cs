using RPGGame;

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

        /// <summary>Middle view: false = previous / current / next combo cards, true = framed combat log. Click toggles.</summary>
        public static bool ShowLog { get; private set; }

        /// <summary>Clickable middle region between the health bars, recorded by the last board paint.</summary>
        public static (int X, int Y, int Width, int Height) MiddleRect { get; set; }

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
            if (IsActive && !Enabled)
                _pendingTeardown = true;
            IsActive = Enabled && IsActive;
            return Enabled;
        }

        public static bool ShouldBeActive(GameState? state) => Enabled && state == GameState.Combat;

        /// <summary>Aligns <see cref="IsActive"/> with <paramref name="state"/>; no-op when state is unknown.</summary>
        public static void Sync(GameState? state)
        {
            if (state == null)
                return;
            bool next = ShouldBeActive(state);
            if (IsActive && !next)
                _pendingTeardown = true;
            IsActive = next;
        }

        private static bool _pendingTeardown;

        /// <summary>
        /// After the board switches off (fight over, F9), removes what only the board paints in the center column:
        /// its health / d20 bars, connector lines, card boxes and text. Partial layout renders do not clear the
        /// canvas, so without this they linger under the dungeon log between fights.
        /// </summary>
        public static void TeardownIfNeeded(GameCanvasControl canvas)
        {
            if (!_pendingTeardown || IsActive || canvas == null)
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
