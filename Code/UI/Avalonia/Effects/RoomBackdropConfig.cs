using System;
using System.Collections.Generic;
using SkiaSharp;

namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Look of the combat-screen room plate (<see cref="RoomBackdropArt"/>).
    /// Config: <c>UIConfiguration.json</c> → <c>roomBackdrop</c>; tuned live from Settings → Appearance → Room Backdrop.
    /// The plate is filtered (saturation → contrast → brightness, then blur), then drawn onto <see cref="BaseColor"/>
    /// with <see cref="BlendMode"/> at <see cref="Opacity"/>.
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
