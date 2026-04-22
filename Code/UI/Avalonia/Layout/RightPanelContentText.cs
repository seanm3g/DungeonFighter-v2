namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Text width helpers for the fixed-width right combat panel (LOCATION / enemy header lines).
    /// </summary>
    public static class RightPanelContentText
    {
        /// <summary>
        /// Monospace character budget for one full row inside the right panel content inset
        /// (matches <c>RIGHT_PANEL_WIDTH - 4</c> used when clearing/drawing that area).
        /// </summary>
        public static int MaxCharsPerLine => System.Math.Max(8, LayoutConstants.RIGHT_PANEL_WIDTH - 4);

        /// <summary>
        /// Truncates with ellipsis when longer than <see cref="MaxCharsPerLine"/>.
        /// </summary>
        public static string EllipsizeToPanelWidth(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            int max = MaxCharsPerLine;
            if (text.Length <= max)
                return text;

            return text.Substring(0, max - 3) + "...";
        }
    }
}
