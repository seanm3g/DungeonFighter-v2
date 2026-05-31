using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.TextAnimation
{
    /// <summary>
    /// Composites ordered text animation layers into per-character <see cref="ColoredText"/> segments.
    /// </summary>
    public static class TextAnimationCompositor
    {
        public static List<ColoredText> Compose(
            string presetName,
            IReadOnlyList<ColoredText>? baseSegments = null,
            string? plainText = null,
            int charStartIndex = 0,
            int lineOffset = 0,
            BaseAnimationState? animationState = null,
            double? phaseOverride = null,
            int elementOffset = 0)
        {
            var preset = TextAnimationPresetLoader.GetPreset(presetName);
            return Compose(preset, baseSegments, plainText, charStartIndex, lineOffset, animationState, phaseOverride, elementOffset);
        }

        public static List<ColoredText> Compose(
            TextAnimationPresetConfig preset,
            IReadOnlyList<ColoredText>? baseSegments = null,
            string? plainText = null,
            int charStartIndex = 0,
            int lineOffset = 0,
            BaseAnimationState? animationState = null,
            double? phaseOverride = null,
            int elementOffset = 0)
        {
            var expanded = ExpandCharacters(baseSegments, plainText);
            if (expanded.Count == 0)
                return new List<ColoredText>();

            var layers = preset.Layers ?? new List<TextAnimationLayerConfig>();
            var result = new List<ColoredText>(expanded.Count);
            double phase = phaseOverride ?? ResolvePhase(preset);

            for (int i = 0; i < expanded.Count; i++)
            {
                var (character, inheritedColor, sourceTemplate) = expanded[i];
                var context = new TextAnimationContext
                {
                    AnimationState = animationState,
                    Phase = phase,
                    CharacterIndex = charStartIndex + i,
                    LineOffset = lineOffset,
                    ElementOffset = elementOffset
                };

                Color accumulated = ResolveBaseColor(preset, inheritedColor, character, i, plainText, baseSegments, layers);
                string? template = sourceTemplate;

                foreach (var layer in layers)
                {
                    if (ShouldSkipLayer(layer, template))
                        continue;

                    accumulated = ApplyLayer(layer, accumulated, context, template);
                }

                if (preset.ClampBrightness != null)
                {
                    accumulated = ColorValidator.ClampAnimatedTextBrightness(
                        accumulated,
                        preset.ClampBrightness.Min,
                        preset.ClampBrightness.Max);
                }

                result.Add(new ColoredText(character.ToString(), accumulated, template, colorReadyForCanvas: true));
            }

            return result;
        }

        public static void AppendComposed(
            string presetName,
            string text,
            Color baseColor,
            List<ColoredText> result,
            ref int startCharPosition,
            int lineOffset,
            string? sourceTemplate,
            BaseAnimationState? animationState = null,
            int elementOffset = 0)
        {
            var segment = new ColoredText(text ?? "", baseColor, sourceTemplate);
            var composed = Compose(
                presetName,
                baseSegments: new List<ColoredText> { segment },
                charStartIndex: startCharPosition,
                lineOffset: lineOffset,
                animationState: animationState,
                elementOffset: elementOffset);

            foreach (var c in composed)
            {
                result.Add(c);
                startCharPosition++;
            }
        }

        public static void AppendComposedFromSegments(
            string presetName,
            IReadOnlyList<ColoredText> segments,
            List<ColoredText> result,
            ref int startCharPosition,
            int lineOffset,
            BaseAnimationState? animationState = null,
            int elementOffset = 0)
        {
            if (segments == null || segments.Count == 0)
                return;

            var composed = Compose(
                presetName,
                baseSegments: segments,
                charStartIndex: startCharPosition,
                lineOffset: lineOffset,
                animationState: animationState,
                elementOffset: elementOffset);

            foreach (var c in composed)
            {
                result.Add(c);
                startCharPosition++;
            }
        }

        private static double ResolvePhase(TextAnimationPresetConfig preset)
        {
            if (preset.Layers == null)
                return 0.0;

            foreach (var layer in preset.Layers)
            {
                if (layer.Mask?.Type?.Equals("sineWave", StringComparison.OrdinalIgnoreCase) == true)
                    return TextAnimationMaskEvaluator.GetCurrentPhase(layer.Mask.PhaseDivisorMs);
            }
            return 0.0;
        }

        private static List<(char Character, Color Color, string? SourceTemplate)> ExpandCharacters(
            IReadOnlyList<ColoredText>? baseSegments,
            string? plainText)
        {
            var list = new List<(char, Color, string?)>();
            if (baseSegments != null)
            {
                foreach (var segment in baseSegments)
                {
                    string t = segment.Text ?? "";
                    foreach (char c in t)
                        list.Add((c, segment.Color, segment.SourceTemplate));
                }
                return list;
            }

            if (!string.IsNullOrEmpty(plainText))
            {
                foreach (char c in plainText)
                    list.Add((c, Colors.White, null));
            }

            return list;
        }

        private static Color ResolveBaseColor(
            TextAnimationPresetConfig preset,
            Color inheritedColor,
            char character,
            int charIndex,
            string? plainText,
            IReadOnlyList<ColoredText>? baseSegments,
            IReadOnlyList<TextAnimationLayerConfig> layers)
        {
            foreach (var layer in layers)
            {
                if (!IsBaseColorLayer(layer))
                    continue;

                var source = layer.Source;
                if (source == null)
                    return inheritedColor;

                if (source.Inherit || preset.InheritBaseFromSegments)
                    return inheritedColor;

                if (!string.IsNullOrEmpty(source.Solid))
                    return TextAnimationPresetLoader.ResolveColor(source.Solid);

                if (!string.IsNullOrEmpty(source.Template))
                {
                    string text = plainText ?? BuildTextFromSegments(baseSegments);
                    var templateSegments = ColorTemplateLibrary.GetTemplate(source.Template, text);
                    if (charIndex < templateSegments.Count)
                        return templateSegments[charIndex].Color;
                    if (templateSegments.Count > 0)
                        return templateSegments[charIndex % templateSegments.Count].Color;
                }
            }

            return inheritedColor;
        }

        private static string BuildTextFromSegments(IReadOnlyList<ColoredText>? segments)
        {
            if (segments == null)
                return "";
            var sb = new System.Text.StringBuilder();
            foreach (var s in segments)
                sb.Append(s.Text);
            return sb.ToString();
        }

        private static bool IsBaseColorLayer(TextAnimationLayerConfig layer)
            => layer.Type?.Equals("baseColor", StringComparison.OrdinalIgnoreCase) == true;

        private static bool ShouldSkipLayer(TextAnimationLayerConfig layer, string? sourceTemplate)
        {
            if (layer.HsvAdjust?.BrightnessScale?.CombineUndulationAndMask == true)
                return false;

            return ShouldSkipUndulationForTemplate(layer, sourceTemplate);
        }

        private static bool ShouldSkipUndulationForTemplate(TextAnimationLayerConfig layer, string? sourceTemplate)
        {
            if (!layer.SkipWhenTemplateUndulateDisabled)
                return false;

            if (string.IsNullOrEmpty(sourceTemplate))
                return false;

            var templateData = ColorTemplateLoader.GetTemplate(sourceTemplate);
            return templateData != null && !templateData.Undulate;
        }

        private static Color ApplyLayer(TextAnimationLayerConfig layer, Color accumulated, TextAnimationContext context, string? sourceTemplate)
        {
            return layer.Type?.ToLowerInvariant() switch
            {
                "basecolor" => accumulated,
                "coloroverlay" => ApplyColorOverlay(layer, accumulated, context),
                "hsvadjust" => ApplyHsvAdjust(layer, accumulated, context, sourceTemplate),
                _ => accumulated
            };
        }

        private static Color ApplyColorOverlay(TextAnimationLayerConfig layer, Color accumulated, TextAnimationContext context)
        {
            double maskAlpha = TextAnimationMaskEvaluator.EvaluateAlpha(layer.Mask, context);
            if (maskAlpha <= 0 && !layer.DirectGradient)
                return accumulated;

            if (layer.DirectGradient && layer.Gradient != null && layer.Gradient.Count >= 2)
            {
                Color start = TextAnimationPresetLoader.ResolveColor(layer.Gradient[0]);
                Color end = TextAnimationPresetLoader.ResolveColor(layer.Gradient[1]);
                return ColorValidator.LerpRgb(start, end, maskAlpha);
            }

            Color overlayColor = ResolveOverlayColor(layer, context, maskAlpha);
            return ColorValidator.LerpRgb(accumulated, overlayColor, maskAlpha);
        }

        private static Color ResolveOverlayColor(TextAnimationLayerConfig layer, TextAnimationContext context, double maskAlpha)
        {
            if (layer.Gradient == null || layer.Gradient.Count == 0)
                return Colors.White;

            Color start = TextAnimationPresetLoader.ResolveColor(layer.Gradient[0]);
            if (layer.Gradient.Count == 1)
                return start;

            Color end = TextAnimationPresetLoader.ResolveColor(layer.Gradient[1]);
            double t = TextAnimationMaskEvaluator.EvaluateAlpha(layer.Mask, context);
            return ColorValidator.LerpRgb(start, end, t);
        }

        private static Color ApplyHsvAdjust(TextAnimationLayerConfig layer, Color accumulated, TextAnimationContext context, string? sourceTemplate)
        {
            var adjust = layer.HsvAdjust;
            if (adjust == null)
                return accumulated;

            double maskAlpha = TextAnimationMaskEvaluator.EvaluateAlpha(layer.Mask, context);
            if (maskAlpha <= 0 && adjust.BrightnessScale == null)
                return accumulated;

            Color color = accumulated;

            if (adjust.BrightnessScale != null)
            {
                double factor = adjust.BrightnessScale.CombineUndulationAndMask
                    ? ComputeLegacyCombinedBrightnessFactor(
                        adjust.BrightnessScale,
                        layer.Mask,
                        context,
                        ShouldSkipUndulationForTemplate(layer, sourceTemplate))
                    : ComputeBrightnessFactor(adjust.BrightnessScale, layer.Mask, context);
                color = ColorValidator.ScaleBrightnessHsv(color, factor);
            }

            if (adjust.HueShift.HasValue && adjust.HueShift.Value != 0)
            {
                double effectiveShift = adjust.HueShift.Value * maskAlpha;
                color = ColorValidator.AdjustAccentHueHsv(color, effectiveShift, maskAlpha);
            }

            if (adjust.SaturationScale.HasValue && Math.Abs(adjust.SaturationScale.Value - 1.0) > 1e-6)
            {
                double effectiveScale = 1.0 + (adjust.SaturationScale.Value - 1.0) * maskAlpha;
                color = ColorValidator.ScaleSaturationHsv(color, effectiveScale);
            }

            return color;
        }

        private static double ComputeBrightnessFactor(
            TextAnimationBrightnessScaleConfig scale,
            TextAnimationMaskConfig? mask,
            TextAnimationContext context)
        {
            double raw = TextAnimationMaskEvaluator.EvaluateRawWave(mask, context);

            double factor;
            if (scale.FromMaskIntensity)
            {
                // Legacy brightness mask: adjustment is percent, factor = 1 + (pct/100)*2
                factor = 1.0 + raw * 2.0;
            }
            else
            {
                // Legacy undulation: factor = 1 + raw * amplitude (raw is -0.3..0.3)
                factor = 1.0 + raw * scale.Amplitude;
            }

            return Math.Clamp(factor, scale.Min, scale.Max);
        }

        private static double ComputeLegacyCombinedBrightnessFactor(
            TextAnimationBrightnessScaleConfig scale,
            TextAnimationMaskConfig? mask,
            TextAnimationContext context,
            bool skipUndulation)
        {
            var state = context.AnimationState;
            if (state == null && mask != null)
            {
                state = mask.AnimationState?.ToLowerInvariant() switch
                {
                    "crit" => CritAnimationState.Instance,
                    _ => DungeonSelectionAnimationState.Instance
                };
            }

            if (state == null)
                return 1.0;

            float brightnessAdjustment = state.GetBrightnessAt(context.AdjustedPosition, context.LineOffset);
            double factor = 1.0 + (brightnessAdjustment / 100.0) * 2.0;

            if (!skipUndulation)
            {
                double undulationBrightness = state.GetUndulationBrightnessAt(context.AdjustedPosition, context.LineOffset);
                factor += undulationBrightness * scale.Amplitude;
            }

            return Math.Clamp(factor, scale.Min, scale.Max);
        }
    }
}
