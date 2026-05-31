using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RPGGame.UI.TextAnimation;

namespace RPGGame.UI.Avalonia.Settings.Helpers
{
    /// <summary>
    /// Reads and writes tunable fields on <see cref="TextAnimationPresetConfig"/> for the settings panel.
    /// </summary>
    public static class TextAnimationPresetUiHelper
    {
        public static TextAnimationPresetConfig ClonePreset(TextAnimationPresetConfig source)
        {
            string json = JsonSerializer.Serialize(source);
            var clone = JsonSerializer.Deserialize<TextAnimationPresetConfig>(json) ?? new TextAnimationPresetConfig();
            clone.Layers ??= new List<TextAnimationLayerConfig>();
            return clone;
        }

        public static Dictionary<string, TextAnimationPresetConfig> ClonePresets(
            Dictionary<string, TextAnimationPresetConfig> source)
        {
            var copy = new Dictionary<string, TextAnimationPresetConfig>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in source)
                copy[kv.Key] = ClonePreset(kv.Value);
            return copy;
        }

        public static bool IsPathIntroPreset(string presetName)
            => presetName.Equals("pathIntro", StringComparison.OrdinalIgnoreCase);

        public static TextAnimationLayerConfig? FindFirstHsvLayer(TextAnimationPresetConfig preset)
            => preset.Layers?.FirstOrDefault(l =>
                l.Type?.Equals("hsvAdjust", StringComparison.OrdinalIgnoreCase) == true);

        public static TextAnimationLayerConfig? FindColorOverlayLayer(TextAnimationPresetConfig preset)
            => preset.Layers?.FirstOrDefault(l =>
                l.Type?.Equals("colorOverlay", StringComparison.OrdinalIgnoreCase) == true);

        public static TextAnimationLayerConfig? FindBaseColorLayer(TextAnimationPresetConfig preset)
            => preset.Layers?.FirstOrDefault(l =>
                l.Type?.Equals("baseColor", StringComparison.OrdinalIgnoreCase) == true);

        public static (double min, double max) GetClamp(TextAnimationPresetConfig preset)
        {
            if (preset.ClampBrightness == null)
                return (0, 255);
            return (preset.ClampBrightness.Min, preset.ClampBrightness.Max);
        }

        public static void SetClamp(TextAnimationPresetConfig preset, double min, double max)
        {
            preset.ClampBrightness ??= new TextAnimationClampConfig();
            preset.ClampBrightness.Min = min;
            preset.ClampBrightness.Max = max;
        }

        public static double GetHsvAmplitude(TextAnimationPresetConfig preset)
        {
            var layer = FindFirstHsvLayer(preset);
            return layer?.HsvAdjust?.BrightnessScale?.Amplitude ?? 3.0;
        }

        public static void SetHsvAmplitude(TextAnimationPresetConfig preset, double amplitude)
        {
            var layer = FindFirstHsvLayer(preset);
            if (layer?.HsvAdjust?.BrightnessScale == null)
                return;
            layer.HsvAdjust.BrightnessScale.Amplitude = amplitude;
            if (layer.Mask != null)
                layer.Mask.Amplitude = amplitude;
        }

        public static (string start, string end) GetGradientColors(TextAnimationPresetConfig preset)
        {
            var overlay = FindColorOverlayLayer(preset);
            if (overlay?.Gradient == null || overlay.Gradient.Count == 0)
                return ("#FFF4DC", "#E2F1FF");
            string start = overlay.Gradient[0];
            string end = overlay.Gradient.Count > 1 ? overlay.Gradient[1] : overlay.Gradient[0];
            return (start, end);
        }

        public static void SetGradientColors(TextAnimationPresetConfig preset, string start, string end)
        {
            var overlay = FindColorOverlayLayer(preset);
            if (overlay == null)
                return;
            overlay.Gradient = new List<string> { start, end };

            var baseLayer = FindBaseColorLayer(preset);
            if (baseLayer?.Source != null)
                baseLayer.Source.Solid = start;
        }

        public static TextAnimationLayerConfig? FindAccentHsvLayer(TextAnimationPresetConfig preset)
        {
            if (preset.Layers == null)
                return null;

            for (int i = preset.Layers.Count - 1; i >= 0; i--)
            {
                var layer = preset.Layers[i];
                if (layer.Type?.Equals("hsvAdjust", StringComparison.OrdinalIgnoreCase) != true)
                    continue;
                if (layer.HsvAdjust?.BrightnessScale != null)
                    continue;
                return layer;
            }

            return null;
        }

        public static (double hue, double sat, double phaseMs, double charOffset) GetAccentHsv(TextAnimationPresetConfig preset)
        {
            var layer = FindAccentHsvLayer(preset);
            if (layer?.HsvAdjust == null)
                return (0, 1.0, 500, 0.2);

            return (
                layer.HsvAdjust.HueShift ?? 0,
                layer.HsvAdjust.SaturationScale ?? 1.0,
                layer.Mask?.PhaseDivisorMs ?? 500,
                layer.Mask?.CharacterPhaseOffset ?? 0.2);
        }

        public static void SetAccentHsv(TextAnimationPresetConfig preset, double hue, double sat, double phaseMs, double charOffset)
        {
            preset.Layers ??= new List<TextAnimationLayerConfig>();

            if (Math.Abs(hue) < 1e-6 && Math.Abs(sat - 1.0) < 1e-6)
            {
                var existing = FindAccentHsvLayer(preset);
                if (existing != null)
                    preset.Layers.Remove(existing);
                return;
            }

            var layer = FindAccentHsvLayer(preset);
            if (layer == null)
            {
                layer = new TextAnimationLayerConfig
                {
                    Type = "hsvAdjust",
                    HsvAdjust = new TextAnimationHsvAdjustConfig(),
                    Mask = new TextAnimationMaskConfig { Type = "sineWave" }
                };
                preset.Layers.Add(layer);
            }

            layer.HsvAdjust ??= new TextAnimationHsvAdjustConfig();
            layer.HsvAdjust.HueShift = hue;
            layer.HsvAdjust.SaturationScale = sat;
            layer.Mask ??= new TextAnimationMaskConfig { Type = "sineWave" };
            layer.Mask.Type = "sineWave";
            layer.Mask.PhaseDivisorMs = Math.Max(1, phaseMs);
            layer.Mask.CharacterPhaseOffset = charOffset;
        }

        public static readonly string[] PreviewTemplateNames =
        {
            "fiery", "icy", "ethereal", "crystalline", "golden", "shadow"
        };

        public static (double phaseDivisorMs, double charOffset) GetSineMask(TextAnimationPresetConfig preset)
        {
            var overlay = FindColorOverlayLayer(preset);
            var mask = overlay?.Mask;
            return (mask?.PhaseDivisorMs ?? 320, mask?.CharacterPhaseOffset ?? 0.36);
        }

        public static void SetSineMask(TextAnimationPresetConfig preset, double phaseDivisorMs, double charOffset)
        {
            var overlay = FindColorOverlayLayer(preset);
            if (overlay?.Mask == null)
                return;
            overlay.Mask.PhaseDivisorMs = Math.Max(1, phaseDivisorMs);
            overlay.Mask.CharacterPhaseOffset = charOffset;
        }

        public static string DescribeLayers(TextAnimationPresetConfig preset)
        {
            if (preset.Layers == null || preset.Layers.Count == 0)
                return "(no layers)";

            var lines = new List<string>();
            for (int i = 0; i < preset.Layers.Count; i++)
            {
                var layer = preset.Layers[i];
                string mask = layer.Mask?.Type ?? "none";
                lines.Add($"{i}: {layer.Type} (mask: {mask})");
            }
            return string.Join('\n', lines);
        }
    }
}
