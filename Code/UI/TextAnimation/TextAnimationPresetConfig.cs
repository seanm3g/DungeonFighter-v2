using System.Collections.Generic;

namespace RPGGame.UI.TextAnimation
{
    /// <summary>
    /// JSON-driven preset: ordered layer stack for per-character text animation compositing.
    /// </summary>
    public class TextAnimationPresetConfig
    {
        public bool InheritBaseFromSegments { get; set; }

        public TextAnimationClampConfig? ClampBrightness { get; set; }

        public List<TextAnimationLayerConfig> Layers { get; set; } = new();
    }

    public class TextAnimationClampConfig
    {
        public double Min { get; set; }
        public double Max { get; set; } = 255;
    }

    public class TextAnimationLayerConfig
    {
        /// <summary>baseColor, colorOverlay, or hsvAdjust.</summary>
        public string Type { get; set; } = "";

        public TextAnimationColorSourceConfig? Source { get; set; }

        /// <summary>Hex (#RRGGBB) or single-letter color codes for overlay gradient endpoints.</summary>
        public List<string>? Gradient { get; set; }

        public TextAnimationMaskConfig? Mask { get; set; }

        public TextAnimationHsvAdjustConfig? HsvAdjust { get; set; }

        /// <summary>When true, output lerp(gradient[0], gradient[1], mask) directly instead of blending onto accumulated color.</summary>
        public bool DirectGradient { get; set; }

        /// <summary>When true, skip undulation for characters whose template has undulate disabled (brightness mask still applies when combined).</summary>
        public bool SkipWhenTemplateUndulateDisabled { get; set; }
    }

    public class TextAnimationColorSourceConfig
    {
        public bool Inherit { get; set; }
        public string? Solid { get; set; }
        public string? Template { get; set; }
    }

    public class TextAnimationMaskConfig
    {
        /// <summary>sineWave, undulation, brightnessMask, or constant.</summary>
        public string Type { get; set; } = "constant";

        /// <summary>dungeonSelection or crit — which BaseAnimationState singleton to read.</summary>
        public string? AnimationState { get; set; }

        public double PhaseDivisorMs { get; set; } = 320;
        public double CharacterPhaseOffset { get; set; } = 0.36;
        public double Amplitude { get; set; } = 3.0;
        public double ConstantValue { get; set; } = 1.0;
    }

    public class TextAnimationHsvAdjustConfig
    {
        public TextAnimationBrightnessScaleConfig? BrightnessScale { get; set; }
        public double? HueShift { get; set; }
        public double? SaturationScale { get; set; }
    }

    public class TextAnimationBrightnessScaleConfig
    {
        public double Min { get; set; } = 0.3;
        public double Max { get; set; } = 2.0;
        public bool FromMaskIntensity { get; set; }
        public double Amplitude { get; set; } = 3.0;

        /// <summary>
        /// When true, undulation and brightness-mask contributions are summed into one brightness factor
        /// (matches legacy UndulatingTextHelper / dungeon selection behavior).
        /// </summary>
        public bool CombineUndulationAndMask { get; set; }
    }
}
