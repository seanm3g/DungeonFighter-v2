namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Mild auto-reforming neighbor-push for F7 DistortionPulse action emphasis.
    /// Nested under <c>UIConfiguration.json</c> → <c>dungeonSelectionAnimation.windSway.emphasisPulse</c>.
    /// Gated by <see cref="Enabled"/> and F6 like wind sway / click-burst.
    /// </summary>
    public class TextEmphasisPulseConfig
    {
        /// <summary>Master enable. Default: true.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Disturbance radius in character cells. Default: 8.</summary>
        public double ImpulseRadiusCells { get; set; } = 8.0;

        /// <summary>
        /// Peak glyph offset as a fraction of one character cell.
        /// Default: 0.85 (noticeable nudge, not a full explode).
        /// </summary>
        public double MaxOffsetFraction { get; set; } = 0.85;

        /// <summary>Seconds to push outward. Default: 0.18.</summary>
        public double OutSeconds { get; set; } = 0.18;

        /// <summary>Seconds to ease back to rest. Default: 0.35.</summary>
        public double ReformSeconds { get; set; } = 0.35;

        /// <summary>Vertical offset scale relative to horizontal. Default: 0.85.</summary>
        public double VerticalScale { get; set; } = 0.85;

        /// <summary>
        /// Optional chromatic fringe scale from pulse velocity (seconds). Default: 0.08.
        /// </summary>
        public double ChromaticVelocitySeconds { get; set; } = 0.08;

        /// <summary>
        /// When true, play <see cref="ExplosionGifFileName"/> centered on the action word as a
        /// letter-masked overlay (visible only through occupied combat-log glyphs).
        /// Default: true.
        /// </summary>
        public bool ExplosionGifEnabled { get; set; } = true;

        /// <summary>
        /// Path under GameData for the DistortionPulse explosion GIF.
        /// Default: <c>Video/explosion.gif</c>.
        /// </summary>
        public string ExplosionGifFileName { get; set; } = "Video/explosion.gif";

        /// <summary>
        /// Drawn diameter of the explosion as a multiple of character cell width.
        /// Default: 60.
        /// </summary>
        public double ExplosionDiameterCells { get; set; } = 60.0;

        /// <summary>
        /// Seconds from cut-in (full opacity) until the explosion is fully gone.
        /// Caps playback even if the GIF is longer. Default: 0.5.
        /// </summary>
        public double ExplosionFadeOutSeconds { get; set; } = 0.5;
    }
}
