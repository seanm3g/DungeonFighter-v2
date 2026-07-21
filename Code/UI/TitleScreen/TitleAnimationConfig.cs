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
        /// Animation frame rate (default: 10 FPS — slower intro + idle wave)
        /// </summary>
        public int FramesPerSecond { get; set; } = 10;

        /// <summary>
        /// Number of frames for the brief black screen (default: 8)
        /// </summary>
        public int BlackScreenFrames { get; set; } = 8;

        /// <summary>
        /// Number of frames for the fade-in toward bright hold (default: 15)
        /// </summary>
        public int FadeInFrames { get; set; } = 15;

        /// <summary>
        /// Number of frames for the POP overshoot flash (default: 3)
        /// </summary>
        public int PopFrames { get; set; } = 3;

        /// <summary>
        /// Number of frames to hold the white light flash (default: 4)
        /// Kept for backward compatibility with older configs / tests.
        /// </summary>
        public int WhiteLightHoldFrames { get; set; } = 4;

        /// <summary>
        /// Number of frames for settling into the chosen idle palette (default: 20)
        /// </summary>
        public int SettleFrames { get; set; } = 20;

        /// <summary>
        /// Number of frames for the final color transition (default: 20).
        /// Alias of SettleFrames for older configs; when both are set, SettleFrames wins after load.
        /// </summary>
        public int FinalTransitionFrames { get; set; } = 20;

        /// <summary>
        /// Non-whitespace glyphs per color band for static phased palette frames (settle preview).
        /// Idle undulation wave size is controlled by UIConfiguration.DungeonSelectionAnimation.UndulationWaveLength.
        /// </summary>
        public int IdleWaveLength { get; set; } = 12;

        /// <summary>
        /// HSV saturation multiplier applied to DEMON / FIGHTER palette colors (default 1.5 = +50%).
        /// Decorator, tagline, and press-key are not affected.
        /// </summary>
        public double IdleSaturationScale { get; set; } = 1.5;

        /// <summary>
        /// HSV Value multiplier for accent piping glyphs (box-drawing edges) on DEMON / FIGHTER.
        /// Full-block <c>█</c> bodies stay at 1.0; piping defaults to 0.275 so faces read brighter than outlines.
        /// </summary>
        public double IdlePipingBrightnessScale { get; set; } = 0.275;

        /// <summary>
        /// How long a palette holds before crossfading to the next, in milliseconds (default 1000).
        /// </summary>
        public int PaletteShiftIntervalMs { get; set; } = 1000;

        /// <summary>
        /// Duration of the RGB crossfade between idle color sequences, in milliseconds (default 1000).
        /// </summary>
        public int PaletteTransitionMs { get; set; } = 1000;

        /// <summary>
        /// Duration to hold final frame before showing "press key" message (milliseconds).
        /// Default 0: the title screen waits for any key, so no timed hold is needed.
        /// </summary>
        public int FinalHoldDuration { get; set; } = 0;

        /// <summary>
        /// Color scheme for the animation
        /// </summary>
        public TitleColorScheme ColorScheme { get; set; } = new TitleColorScheme();

        /// <summary>
        /// Calculates frame duration in milliseconds based on frames per second
        /// </summary>
        public int FrameDurationMs => Math.Max(1, 1000 / Math.Max(1, FramesPerSecond));

        /// <summary>
        /// Effective settle frame count (prefers SettleFrames when positive).
        /// </summary>
        public int EffectiveSettleFrames =>
            SettleFrames > 0 ? SettleFrames : Math.Max(1, FinalTransitionFrames);
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
        /// POP overshoot color (default: "Y" = bright white)
        /// </summary>
        public string PopColor { get; set; } = "Y";

        /// <summary>
        /// Final color for DUNGEON text (default: "W" = gold/yellow)
        /// </summary>
        public string DungeonFinalColor { get; set; } = "W";

        /// <summary>
        /// Final color for FIGHTERS text (default: "O" = orange)
        /// </summary>
        public string FighterFinalColor { get; set; } = "O";

        /// <summary>
        /// Transition source color (default: "Y" = white, what we transition from)
        /// </summary>
        public string TransitionFromColor { get; set; } = "Y";
    }
}
