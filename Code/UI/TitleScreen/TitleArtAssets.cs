namespace RPGGame.UI.TitleScreen
{
    /// <summary>
    /// Centralized storage for title screen ASCII art
    /// Separates presentation data from logic
    /// </summary>
    public static class TitleArtAssets
    {
        /// <summary>
        /// ASCII art lines for "DUNGEON" word
        /// </summary>
        public static readonly string[] DungeonLines = new[]
        {
            "       ██████╗  ██╗   ██╗███╗   ██╗ ██████╗ ███████╗ ██████╗ ███╗   ██╗",
            "       ██╔═══██╗██║   ██║████╗  ██║██╔════╝ ██╔════╝██╔═══██╗████╗  ██║",
            "       ██║   ██║██║   ██║██╔██╗ ██║██║  ███╗█████╗  ██║   ██║██╔██╗ ██║",
            "       ██║   ██║██║   ██║██║╚██╗██║██║   ██║██╔══╝  ██║   ██║██║╚██╗██║",
            "       ╚██████╔╝╚██████╔╝██║ ╚████║╚██████╔╝███████╗╚██████╔╝██║ ╚████║",
            "        ╚═════╝  ╚═════╝ ╚═╝  ╚═══╝ ╚═════╝ ╚══════╝ ╚═════╝ ╚═╝  ╚═══╝"
        };

        /// <summary>
        /// ASCII art lines for "FIGHTER" word
        /// </summary>
        public static readonly string[] FighterLines = new[]
        {
            "      ███████╗██╗ ██████╗ ██╗  ██╗████████╗███████╗██████╗ ",
            "      ██╔════╝██║██╔════╝ ██║  ██║╚══██╔══╝██╔════╝██╔══██╗",
            "      █████╗  ██║██║  ███╗███████║   ██║   █████╗  ██████╔╝",
            "      ██╔══╝  ██║██║   ██║██╔══██║   ██║   ██╔══╝  ██╔══██╗",
            "      ██║     ██║╚██████╔╝██║  ██║   ██║   ███████╗██║  ██║",
            "      ╚═╝     ╚═╝ ╚═════╝ ╚═╝  ╚═╝   ╚═╝   ╚══════╝╚═╝  ╚═╝"
        };

        /// <summary>
        /// Decorative separator line between title words
        /// </summary>
        public static readonly string DecoratorLine = "      ◈━━━━━━━━━━━━━━━◈    ";

        /// <summary>
        /// Tagline displayed at bottom of title screen
        /// </summary>
        public static readonly string Tagline = "      ◈ Enter the depths. Face the darkness. Claim your glory. ◈";

        /// <summary>
        /// Number of blank lines at top of screen
        /// </summary>
        public const int TopPadding = 15;

        /// <summary>
        /// Horizontal offset for title positioning
        /// </summary>
        public const int TitleStartX = 0;

        /// <summary>
        /// Vertical offset for title positioning (after top padding)
        /// </summary>
        public const int TitleStartY = 2;

        /// <summary>
        /// Background color for padding areas (default: black)
        /// </summary>
        public const string BackgroundColor = "k";
    }
}

