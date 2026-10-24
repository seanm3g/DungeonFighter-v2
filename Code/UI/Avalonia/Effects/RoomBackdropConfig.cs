using System;
using System.Collections.Generic;
using SkiaSharp;

namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Look of the combat-screen room plate (<see cref="RoomBackdropArt"/>).
    /// Config: <c>UIConfiguration.json</c> → <c>roomBackdrop</c>; tuned live from Settings → Appearance → Room Backdrop.
    /// The plate is filtered (saturation → contrast → brightness, then blur), then drawn onto <see cref="BaseColor"/>
    /// with <see cref="BlendMode"/> at <see cref="Opacity"/>. With <see cref="Style"/> "Ascii" the filtered plate is
    /// averaged per text-grid cell and redrawn as glyphs picked by brightness from <see cref="AsciiRamp"/>.
    /// </summary>
    public class RoomBackdropConfig
    {
        public const double MaxBrightness = 2;
        public const double MaxContrast = 2;
        public const double MaxSaturation = 2;
        public const double MaxBlur = 20;

        /// <summary>Show room plates behind the center panel. Default: true.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Color multiplier: 0 = black, 1 = as painted, 2 = double. Default: 0.4 (keeps combat text readable).</summary>
        public double Brightness { get; set; } = 0.4;

        /// <summary>Contrast around mid-grey: 0 = flat grey, 1 = as painted. Default: 1.</summary>
        public double Contrast { get; set; } = 1;

        /// <summary>0 = greyscale, 1 = as painted, 2 = vivid. Default: 1.</summary>
        public double Saturation { get; set; } = 1;

        /// <summary>Gaussian blur radius in device-independent pixels. Default: 0.</summary>
        public double Blur { get; set; } = 0;

        /// <summary>Plate opacity over <see cref="BaseColor"/> (0–1). Default: 1.</summary>
        public double Opacity { get; set; } = 1;

        /// <summary>How the plate combines with <see cref="BaseColor"/>: one of <see cref="BlendModeNames"/>. Default: Normal.</summary>
        public string BlendMode { get; set; } = "Normal";

        /// <summary>Hex color under the plate; with Multiply / Color / Luminosity etc. it tints the room. Default: #000000.</summary>
        public string BaseColor { get; set; } = "#000000";

        /// <summary>How the plate is drawn: one of <see cref="StyleNames"/>. "Ascii" redraws it as glyphs on the text grid. Default: Image.</summary>
        public string Style { get; set; } = "Image";

        /// <summary>Ascii style: glyphs from darkest to brightest cell. Default: " .:-=+*#%@".</summary>
        public string AsciiRamp { get; set; } = DefaultAsciiRamp;

        /// <summary>Ascii style: how much of each cell's color fills the cell behind its glyph (0 = glyphs only). Default: 0.15.</summary>
        public double AsciiBackground { get; set; } = 0.15;

        /// <summary>Ascii style: glyph color multiplier; glyphs cover a fraction of the cell, so they read darker than the plate. Default: 1.6.</summary>
        public double AsciiGlyphGain { get; set; } = 1.6;

        /// <summary>
        /// Ascii style: glyphs per text-grid cell along each axis (1 = one glyph per cell, on the text grid; 2 = a 2×2 block
        /// of half-size glyphs). Higher = finer picture. Default: 2.
        /// </summary>
        public double AsciiDensity { get; set; } = 2;

        public const string DefaultAsciiRamp = " .:-=+*#%@";
        public const double MaxAsciiGlyphGain = 4;
        public const double MaxAsciiDensity = 4;

        public int ResolveAsciiDensity() => (int)Math.Clamp(Math.Round(AsciiDensity), 1, MaxAsciiDensity);

        public static readonly IReadOnlyList<string> StyleNames = new[] { "Image", "Ascii" };

        public bool IsAscii => string.Equals(Style, "Ascii", StringComparison.OrdinalIgnoreCase);

        public string ResolveAsciiRamp() => string.IsNullOrEmpty(AsciiRamp) ? DefaultAsciiRamp : AsciiRamp;

        /// <summary>Blend modes offered in settings (Skia separable + non-separable modes).</summary>
        public static readonly IReadOnlyList<string> BlendModeNames = new[]
        {
            "Normal", "Multiply", "Screen", "Overlay", "SoftLight", "HardLight", "Darken", "Lighten",
            "ColorDodge", "ColorBurn", "Difference", "Exclusion", "Hue", "Saturation", "Color", "Luminosity", "Plus"
        };

        public SKBlendMode ResolveBlendMode() =>
            !string.Equals(BlendMode, "Normal", StringComparison.OrdinalIgnoreCase)
            && Enum.TryParse(BlendMode, ignoreCase: true, out SKBlendMode mode)
                ? mode
                : SKBlendMode.SrcOver;

        public SKColor ResolveBaseColor() =>
            SKColor.TryParse(BaseColor ?? "", out var color) ? color : SKColors.Black;

        public RoomBackdropConfig Clone() => (RoomBackdropConfig)MemberwiseClone();
    }

    /// <summary>
    /// Cached <see cref="RoomBackdropConfig"/> (rendering reads it every frame); the settings panel updates it live.
    /// </summary>
    public static class RoomBackdropSettings
    {
        private static RoomBackdropConfig? cached;

        public static RoomBackdropConfig Current
        {
            get
            {
                if (cached == null)
                {
                    try
                    {
                        cached = UIConfiguration.LoadFromFile().RoomBackdrop ?? new RoomBackdropConfig();
                    }
                    catch
                    {
                        cached = new RoomBackdropConfig();
                    }
                }
                return cached;
            }
            set => cached = value ?? new RoomBackdropConfig();
        }
    }
}
