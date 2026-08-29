namespace RPGGame.UI.Avalonia.Handlers
{
    /// <summary>
    /// Pure helpers for title-screen → main-menu boot pacing.
    /// GameCoordinator is warmed while the title idle runs so a keypress only shows the menu.
    /// The main window stays hidden (opacity 0) until the first idle frame is painted.
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

        /// <summary>
        /// Window opacity while the first title frame is still loading vs after it is painted.
        /// </summary>
        public static double GetStartupWindowOpacity(bool titleFirstFrameReady) => titleFirstFrameReady ? 1.0 : 0.0;

        /// <summary>
        /// Hide the taskbar button until the title frame is ready so a blank window does not flash.
        /// </summary>
        public static bool GetStartupShowInTaskbar(bool titleFirstFrameReady) => titleFirstFrameReady;
    }
}
