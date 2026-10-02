namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Mouse-driven per-glyph wind sway: one wake circle with near→far gradient,
    /// rear-biased by motion direction, plus a decaying trail.
    /// Tunable via <c>UIConfiguration.json</c> → <c>dungeonSelectionAnimation.windSway</c>
    /// and Settings → Text &amp; Animation.
    /// </summary>
    public class WindSwayConfig
    {
        /// <summary>Master enable. Default: true.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// When true, only left/right character panels sway (combat log stays locked).
        /// Default: false so the wake is visible wherever the pointer moves.
        /// </summary>
        public bool SidePanelsOnly { get; set; } = false;

        /// <summary>
        /// Peak glyph offset as a fraction of one character cell (width for X, height for Y).
        /// Default: 0.55 (noticeable but soft).
        /// </summary>
        public double MaxOffsetFraction { get; set; } = 0.55;

        /// <summary>
        /// How strongly mouse movement (in character cells) adds to the wind vector.
        /// Default: 0.35.
        /// </summary>
        public double ImpulseGain { get; set; } = 0.35;

        /// <summary>Hard cap on wind vector magnitude. Default: 3.5.</summary>
        public double MaxWind { get; set; } = 3.5;

        /// <summary>
        /// Exponential decay rate per second (higher = settles faster).
        /// Default: 1.35.
        /// </summary>
        public double DecayPerSecond { get; set; } = 1.35;

        /// <summary>
        /// Legacy spatial wave length (kept for config compatibility; ripple sampling was removed for performance).
        /// Default: 3.5.
        /// </summary>
        public double WaveLength { get; set; } = 3.5;

        /// <summary>
        /// Vertical sway scale relative to horizontal.
        /// Default: 0.75.
        /// </summary>
        public double VerticalScale { get; set; } = 0.75;

        /// <summary>
        /// How often the settle timer repaints while wind is active (ms).
        /// Default: 24 (~40fps settle; cheaper than 60fps while still smooth).
        /// </summary>
        public int SettleIntervalMs { get; set; } = 24;

        /// <summary>
        /// Outer wake size in character cells (settings “size”).
        /// Forms a cell-aspect ellipse (<c>cells×charWidth</c> by <c>cells×charHeight</c>) whose major
        /// axis rotates to follow the mouse motion vector.
        /// Default: 36.
        /// </summary>
        public double WakeRadiusCells { get; set; } = 36.0;

        /// <summary>
        /// Influence multiplier at the cursor (relative to <see cref="MaxOffsetFraction"/>).
        /// Default: 1.0.
        /// </summary>
        public double NearInfluence { get; set; } = 1.0;

        /// <summary>
        /// Influence multiplier at the radius edge.
        /// Default: 0.35.
        /// </summary>
        public double FarInfluence { get; set; } = 0.35;

        /// <summary>
        /// How much stronger the wake is behind the motion vector vs in front.
        /// <c>0</c> = isotropic, <c>1</c> = front nearly zero. Default: 0.65.
        /// </summary>
        public double WakeRearBias { get; set; } = 0.65;

        /// <summary>
        /// Curves the near→far radial blend. Values &gt; 1 use cheap quadratic <c>t²</c>;
        /// otherwise linear <c>t</c> (replaces the old <c>Math.Pow</c> path).
        /// Default: 1.35.
        /// </summary>
        public double WakeFalloffPower { get; set; } = 1.35;

        /// <summary>
        /// Mouse speed in cells/sec that maps to full wake strength.
        /// Lower = easier to hit full strength. Default: 12.
        /// </summary>
        public double SpeedReferenceCellsPerSec { get; set; } = 12.0;

        /// <summary>
        /// How long a deposited trail sample stays alive (seconds).
        /// Default: 0.40.
        /// </summary>
        public double TrailLifetimeSeconds { get; set; } = 0.40;

        /// <summary>
        /// Cap on stored trail samples.
        /// Default: 10 (keeps wake feel without per-glyph O(n) blowups).
        /// </summary>
        public int TrailMaxPoints { get; set; } = 10;

        /// <summary>
        /// Minimum cursor travel in character cells before depositing another trail sample.
        /// Default: 1.25.
        /// </summary>
        public double TrailMinSpacingCells { get; set; } = 1.25;

        /// <summary>
        /// When true, swayed glyphs draw red/cyan chromatic fringe ghosts.
        /// Default: true (subtle; still gated by <see cref="Enabled"/> and F6).
        /// </summary>
        public bool ChromaticAberrationEnabled { get; set; } = true;

        /// <summary>
        /// Fringe offset as a fraction of the glyph sway magnitude (pixels already in ox/oy).
        /// Default: 0.10. Settings UI clamps roughly 0–0.4.
        /// </summary>
        public double ChromaticSpreadFraction { get; set; } = 0.10;

        /// <summary>
        /// Alpha for chromatic ghost passes (0–1). Default: 0.35.
        /// </summary>
        public double ChromaticOpacity { get; set; } = 0.35;

        /// <summary>
        /// Click-charge explode/reform burst nested under wind sway settings.
        /// </summary>
        public TextClickBurstConfig ClickBurst { get; set; } = new TextClickBurstConfig();
    }
}
