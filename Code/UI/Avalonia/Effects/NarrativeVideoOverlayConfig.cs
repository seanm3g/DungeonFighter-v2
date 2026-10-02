namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Per-cell opacity video overlay over the center combat-log band.
    /// Config: <c>UIConfiguration.json</c> → <c>narrativeVideoOverlay</c>.
    /// </summary>
    public class NarrativeVideoOverlayConfig
    {
        /// <summary>Master enable. Default: true when a video file is present.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Path under GameData (forward or back slashes). Default: <c>Video/videoplayback.mp4</c>.
        /// </summary>
        public string FileName { get; set; } = "Video/videoplayback.mp4";

        /// <summary>
        /// Peak cell opacity for full-bright white text (0–1). Default: 0.45 so prose stays readable underneath.
        /// </summary>
        public double MaxOpacity { get; set; } = 0.45;

        /// <summary>
        /// Halo cells around occupied glyphs for volume. 0 = occupied cells only. Default: 1.
        /// </summary>
        public int HaloCells { get; set; } = 1;

        /// <summary>
        /// Halo opacity as a fraction of the source cell opacity. Default: 0.35.
        /// </summary>
        public double HaloOpacityScale { get; set; } = 0.35;

        /// <summary>
        /// When true, only show while F7 narrative combat log is on. Default: true.
        /// </summary>
        public bool OnlyWhenNarrativeLog { get; set; } = true;

        /// <summary>
        /// When true, only show while a dungeon run is active
        /// (<see cref="RPGGame.GameStateManager.HasCurrentDungeon"/>). Default: true.
        /// </summary>
        public bool OnlyWhenInDungeon { get; set; } = true;

        /// <summary>Mute video audio so it does not fight combat SFX. Default: true.</summary>
        public bool MuteAudio { get; set; } = true;

        /// <summary>Loop the clip. Default: true.</summary>
        public bool Loop { get; set; } = true;

        /// <summary>
        /// Clamps opacity / blurry-opacity / video-level (halo cells) knobs to safe ranges.
        /// Returns a new instance; does not mutate <paramref name="source"/>.
        /// </summary>
        public static NarrativeVideoOverlayConfig Normalize(NarrativeVideoOverlayConfig? source)
        {
            var src = source ?? new NarrativeVideoOverlayConfig();
            return new NarrativeVideoOverlayConfig
            {
                Enabled = src.Enabled,
                FileName = string.IsNullOrWhiteSpace(src.FileName) ? "Video/videoplayback.mp4" : src.FileName,
                MaxOpacity = System.Math.Clamp(src.MaxOpacity, 0.0, 1.0),
                HaloCells = System.Math.Clamp(src.HaloCells, 0, 8),
                HaloOpacityScale = System.Math.Clamp(src.HaloOpacityScale, 0.0, 1.0),
                OnlyWhenNarrativeLog = src.OnlyWhenNarrativeLog,
                OnlyWhenInDungeon = src.OnlyWhenInDungeon,
                MuteAudio = src.MuteAudio,
                Loop = src.Loop
            };
        }
    }
}
