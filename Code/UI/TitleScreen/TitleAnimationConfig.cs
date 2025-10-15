using System;

namespace RPGGame.UI.TitleScreen
{
    /// <summary>
    /// Configuration for title screen animation timing and colors
    /// Can be loaded from JSON for easy customization
    /// </summary>
    public class TitleAnimationConfig
    {
        /// <summary>
        /// Animation frame rate (default: 30 FPS)
        /// </summary>
        public int FramesPerSecond { get; set; } = 30;

        /// <summary>
        /// Number of frames to hold the white light flash (default: 8 frames = 0.25 seconds at 30 FPS)
        /// </summary>
        public int WhiteLightHoldFrames { get; set; } = 8;

        /// <summary>
        /// Number of frames for the final color transition (default: 45 frames = 1.5 seconds at 30 FPS)
        /// </summary>
        public int FinalTransitionFrames { get; set; } = 45;

        /// <summary>
        /// Duration to hold final frame before showing "press key" message (milliseconds)
        /// </summary>
        public int FinalHoldDuration { get; set; } = 1000;

        /// <summary>
        /// Color scheme for the animation
        /// </summary>
        public TitleColorScheme ColorScheme { get; set; } = new TitleColorScheme();

        /// <summary>
        /// Calculates frame duration in milliseconds based on frames per second
        /// </summary>
        public int FrameDurationMs => 1000 / FramesPerSecond;
    }

    /// <summary>
    /// Color scheme configuration for title animation
    /// Uses single-letter color codes matching the game's color system
    /// </summary>
    public class TitleColorScheme
    {
        /// <summary>
        /// Initial color (default: "k" = very dark/black)
        /// </summary>
        public string InitialColor { get; set; } = "k";

        /// <summary>
        /// First flash color (default: "W" = warm white/gold)
        /// </summary>
        public string FlashColor1 { get; set; } = "W";

        /// <summary>
        /// Second flash color (default: "Y" = bright white)
        /// </summary>
        public string FlashColor2 { get; set; } = "Y";

        /// <summary>
        /// Hold phase color (default: "Y" = bright white)
        /// </summary>
        public string HoldColor { get; set; } = "Y";

        /// <summary>
        /// Final color for DUNGEON text (default: "W" = gold/yellow)
        /// </summary>
        public string DungeonFinalColor { get; set; } = "W";

        /// <summary>
        /// Final color for FIGHTER text (default: "R" = bright red)
        /// </summary>
        public string FighterFinalColor { get; set; } = "R";

        /// <summary>
        /// Transition source color (default: "Y" = white, what we transition from)
        /// </summary>
        public string TransitionFromColor { get; set; } = "Y";
    }
}

