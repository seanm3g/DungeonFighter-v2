namespace RPGGame.UI.Avalonia.Handlers
{
    /// <summary>
    /// Pure helpers for title-screen → main-menu boot pacing.
    /// GameCoordinator is warmed while the title idle runs so a keypress only shows the menu.
    /// </summary>
    public static class TitleToMenuBootstrap
    {
        /// <summary>
        /// When warmup finished before the keypress, the menu path should not wait on construction.
        /// </summary>
        public static bool CanShowMenuImmediately(bool warmupCompleted) => warmupCompleted;

        /// <summary>
        /// Key input is accepted only after the first idle frame paints "Press any key…".
        /// </summary>
        public static bool ShouldAcceptTitleKey(bool waitingForKeyAfterAnimation) => waitingForKeyAfterAnimation;
    }
}
