namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Click-charge glyph burst: spam-click builds charge near the cursor, then glyphs
    /// explode outward with per-letter distance/rotation variance and stay scattered on that
    /// screen region. Clicks outside start another independent mess; clicks inside a mess
    /// rebuild one letter per click. After <see cref="AutoRebuildIdleSeconds"/> idle, letters
    /// auto-fill every <see cref="AutoRebuildIntervalSeconds"/>. Nested under
    /// <c>UIConfiguration.json</c> → <c>dungeonSelectionAnimation.windSway.clickBurst</c>.
    /// Gated by <see cref="Enabled"/> and F6 distortion like wind sway.
    /// </summary>
    public class TextClickBurstConfig
    {
        /// <summary>Master enable. Default: true.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Clicks needed to fill charge and trigger explode. Default: 5.</summary>
        public int ClicksToExplode { get; set; } = 5;

        /// <summary>
        /// Charge lost per second while idle (not exploding).
        /// Default: 0.55 (~empty in ~2s from full).
        /// </summary>
        public double ChargeDecayPerSecond { get; set; } = 0.55;

        /// <summary>
        /// How fast the post-click ripple amplitude decays (per second).
        /// Default: 2.5.
        /// </summary>
        public double ImpulseDecayPerSecond { get; set; } = 2.5;

        /// <summary>Disturbance / explode radius in character cells. Default: 12.</summary>
        public double ImpulseRadiusCells { get; set; } = 12.0;

        /// <summary>
        /// Peak pre-explode glyph offset as a fraction of one character cell.
        /// Default: 0.35.
        /// </summary>
        public double MaxImpulseOffsetFraction { get; set; } = 0.35;

        /// <summary>
        /// Peak explode glyph offset as a fraction of one character cell (before distance variance).
        /// Default: 3 (per-glyph variance multiplies this).
        /// </summary>
        public double ExplodeMaxOffsetFraction { get; set; } = 3.0;

        /// <summary>
        /// Minimum per-glyph distance multiplier during explode (hash-mapped with
        /// <see cref="DistanceVarianceMax"/>). Default: 0.2.
        /// </summary>
        public double DistanceVarianceMin { get; set; } = 0.2;

        /// <summary>
        /// Maximum per-glyph distance multiplier during explode. Default: 1.45.
        /// </summary>
        public double DistanceVarianceMax { get; set; } = 1.45;

        /// <summary>
        /// Peak random glyph spin in radians (± this value, plus a secondary hash twist).
        /// Default: ~2.4π (~1.2 turns).
        /// </summary>
        public double MaxRotationRadians { get; set; } = 7.5;

        /// <summary>Seconds for glyphs to push outward. Default: 0.28.</summary>
        public double ExplodeOutSeconds { get; set; } = 0.28;

        /// <summary>
        /// Legacy hold beat (unused for auto-reform; scatter stays until click-rebuild).
        /// Kept for JSON compatibility. Default: 0.
        /// </summary>
        public double ExplodeHoldSeconds { get; set; } = 0.0;

        /// <summary>
        /// Seconds for a single letter to ease home when rebuilt by a click. Default: 0.22.
        /// </summary>
        public double ReformSeconds { get; set; } = 0.22;

        /// <summary>
        /// Approximate letters restored per full rebuild (each click advances 1/N).
        /// Default: 14.
        /// </summary>
        public int LettersToRebuild { get; set; } = 14;

        /// <summary>
        /// Seconds of idle (no hole click) after scatter lands before auto-fill starts.
        /// Manual hole clicks reset this wait. Default: 10.
        /// </summary>
        public double AutoRebuildIdleSeconds { get; set; } = 10.0;

        /// <summary>
        /// Seconds between auto-filled letters once idle autofill is running. Default: 0.5.
        /// </summary>
        public double AutoRebuildIntervalSeconds { get; set; } = 0.5;

        /// <summary>
        /// Converts burst velocity (px/s) into chromatic fringe space (seconds).
        /// Only velocity feeds CA for burst glyphs — landed letters stay clean. Default: 0.12.
        /// </summary>
        public double ChromaticVelocitySeconds { get; set; } = 0.12;

        /// <summary>Vertical offset scale relative to horizontal. Default: 0.85.</summary>
        public double VerticalScale { get; set; } = 0.85;
    }
}
