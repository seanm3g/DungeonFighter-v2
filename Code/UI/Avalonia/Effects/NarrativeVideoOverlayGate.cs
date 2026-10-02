namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Pure playback/visibility gates for the narrative video overlay (unit-testable).
    /// </summary>
    public static class NarrativeVideoOverlayGate
    {
        /// <summary>
        /// Whether dungeon membership should allow the overlay. Victory/death keep
        /// <see cref="GameStateManager.HasCurrentDungeon"/> until the player leaves, but the video must stop there.
        /// </summary>
        public static bool CountsAsActiveDungeonRunForOverlay(bool hasCurrentDungeon, GameState? state)
        {
            if (!hasCurrentDungeon)
                return false;
            if (state == GameState.DungeonCompletion || state == GameState.Death)
                return false;
            return true;
        }

        /// <summary>
        /// Config/mode gate before glyph sampling. Requires dungeon run when
        /// <paramref name="onlyWhenInDungeon"/> is true.
        /// </summary>
        public static bool ShouldAllowOverlayMode(
            bool enabled,
            bool disposed,
            bool onlyWhenNarrativeLog,
            bool isNarrativeCombatLog,
            bool onlyWhenInDungeon,
            bool inDungeonRun)
        {
            if (!enabled || disposed)
                return false;
            if (onlyWhenNarrativeLog && !isNarrativeCombatLog)
                return false;
            if (onlyWhenInDungeon && !inDungeonRun)
                return false;
            return true;
        }

        /// <summary>Full show/play gate: mode allowed and combat-log band has glyphs.</summary>
        public static bool ShouldShowOverlay(bool allowOverlayMode, bool logHasContent) =>
            allowOverlayMode && logHasContent;
    }
}
