using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.TextAnimation
{
    /// <summary>
    /// Loads and resolves text animation presets from UIConfiguration with built-in fallbacks.
    /// </summary>
    public static class TextAnimationPresetLoader
    {
        private static Dictionary<string, TextAnimationPresetConfig>? _cachedPresets;

        public static void Reload()
        {
            _cachedPresets = null;
        }

        public static TextAnimationPresetConfig GetPreset(string presetName)
        {
            EnsureLoaded();
            if (_cachedPresets != null && _cachedPresets.TryGetValue(presetName, out var preset))
                return preset;
            if (BuiltInDefaults.TryGetValue(presetName, out var fallback))
                return fallback;
            throw new InvalidOperationException($"Text animation preset '{presetName}' was not found.");
        }

        public static bool TryGetPreset(string presetName, out TextAnimationPresetConfig preset)
        {
            EnsureLoaded();
            if (_cachedPresets != null && _cachedPresets.TryGetValue(presetName, out preset!))
                return true;
            return BuiltInDefaults.TryGetValue(presetName, out preset!);
        }

        private static void EnsureLoaded()
        {
            if (_cachedPresets != null)
                return;

            var uiConfig = UIConfiguration.LoadFromFile();
            _cachedPresets = uiConfig.TextAnimationPresets != null && uiConfig.TextAnimationPresets.Count > 0
                ? new Dictionary<string, TextAnimationPresetConfig>(uiConfig.TextAnimationPresets, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, TextAnimationPresetConfig>(BuiltInDefaults, StringComparer.OrdinalIgnoreCase);
        }

        internal static readonly Dictionary<string, TextAnimationPresetConfig> BuiltInDefaults =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["pathIntro"] = CreatePathIntroPreset(),
                ["dungeonSelection"] = CreateDungeonSelectionPreset(),
                ["critLine"] = CreateCritLinePreset(),
                ["displayLogShimmer"] = CreateDisplayLogShimmerPreset(),
            };

        private static TextAnimationPresetConfig CreatePathIntroPreset()
        {
            return new TextAnimationPresetConfig
            {
                Layers = new List<TextAnimationLayerConfig>
                {
                    new()
                    {
                        Type = "baseColor",
                        Source = new TextAnimationColorSourceConfig { Solid = "#FFF4DC" }
                    },
                    new()
                    {
                        Type = "colorOverlay",
                        DirectGradient = true,
                        Gradient = new List<string> { "#FFF4DC", "#E2F1FF" },
                        Mask = new TextAnimationMaskConfig
                        {
                            Type = "sineWave",
                            PhaseDivisorMs = 320,
                            CharacterPhaseOffset = 0.36
                        }
                    }
                }
            };
        }

        private static TextAnimationPresetConfig CreateDungeonSelectionPreset()
        {
            return new TextAnimationPresetConfig
            {
                InheritBaseFromSegments = true,
                ClampBrightness = new TextAnimationClampConfig { Min = 150, Max = 255 },
                Layers = new List<TextAnimationLayerConfig>
                {
                    new()
                    {
                        Type = "baseColor",
                        Source = new TextAnimationColorSourceConfig { Inherit = true }
                    },
                    new()
                    {
                        Type = "hsvAdjust",
                        HsvAdjust = new TextAnimationHsvAdjustConfig
                        {
                            BrightnessScale = new TextAnimationBrightnessScaleConfig
                            {
                                Min = 0.3,
                                Max = 2.0,
                                Amplitude = 3.0,
                                CombineUndulationAndMask = true
                            }
                        },
                        Mask = new TextAnimationMaskConfig
                        {
                            Type = "undulation",
                            AnimationState = "dungeonSelection",
                            Amplitude = 3.0
                        }
                    }
                }
            };
        }

        private static TextAnimationPresetConfig CreateCritLinePreset()
        {
            return new TextAnimationPresetConfig
            {
                InheritBaseFromSegments = true,
                ClampBrightness = new TextAnimationClampConfig { Min = 150, Max = 255 },
                Layers = new List<TextAnimationLayerConfig>
                {
                    new()
                    {
                        Type = "baseColor",
                        Source = new TextAnimationColorSourceConfig { Inherit = true }
                    },
                    new()
                    {
                        Type = "hsvAdjust",
                        HsvAdjust = new TextAnimationHsvAdjustConfig
                        {
                            BrightnessScale = new TextAnimationBrightnessScaleConfig
                            {
                                Min = 0.3,
                                Max = 2.0,
                                Amplitude = 5.0
                            }
                        },
                        Mask = new TextAnimationMaskConfig
                        {
                            Type = "undulation",
                            AnimationState = "crit",
                            Amplitude = 5.0
                        }
                    }
                }
            };
        }

        private static TextAnimationPresetConfig CreateDisplayLogShimmerPreset()
        {
            return new TextAnimationPresetConfig
            {
                InheritBaseFromSegments = true,
                ClampBrightness = new TextAnimationClampConfig { Min = 150, Max = 255 },
                Layers = new List<TextAnimationLayerConfig>
                {
                    new()
                    {
                        Type = "baseColor",
                        Source = new TextAnimationColorSourceConfig { Inherit = true }
                    },
                    new()
                    {
                        Type = "hsvAdjust",
                        SkipWhenTemplateUndulateDisabled = true,
                        HsvAdjust = new TextAnimationHsvAdjustConfig
                        {
                            BrightnessScale = new TextAnimationBrightnessScaleConfig
                            {
                                Min = 0.3,
                                Max = 2.0,
                                Amplitude = 3.0,
                                CombineUndulationAndMask = true
                            }
                        },
                        Mask = new TextAnimationMaskConfig
                        {
                            Type = "undulation",
                            AnimationState = "dungeonSelection",
                            Amplitude = 3.0
                        }
                    }
                }
            };
        }

        /// <summary>Resolves a color string: #RRGGBB hex or single-letter color code.</summary>
        public static Color ResolveColor(string colorSpec)
        {
            if (string.IsNullOrWhiteSpace(colorSpec))
                return Colors.White;

            colorSpec = colorSpec.Trim();
            if (colorSpec.StartsWith("#", StringComparison.Ordinal))
            {
                var hex = colorSpec.Substring(1);
                if (hex.Length == 6 &&
                    byte.TryParse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out byte r) &&
                    byte.TryParse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out byte g) &&
                    byte.TryParse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out byte b))
                {
                    return Color.FromRgb(r, g, b);
                }
            }

            return ColorCodeLoader.GetColor(colorSpec);
        }
    }
}
