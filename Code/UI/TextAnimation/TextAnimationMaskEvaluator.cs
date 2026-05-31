using System;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.TextAnimation
{
    /// <summary>
    /// Context passed to mask evaluation — animation state and optional time phase.
    /// </summary>
    public sealed class TextAnimationContext
    {
        public BaseAnimationState? AnimationState { get; init; }
        public double Phase { get; init; }
        public int CharacterIndex { get; init; }
        public int LineOffset { get; init; }
        public int ElementOffset { get; init; }

        public int AdjustedPosition => CharacterIndex + ElementOffset;
    }

    /// <summary>
    /// Computes per-character mask alpha (0–1) and raw wave values for layer compositing.
    /// </summary>
    public static class TextAnimationMaskEvaluator
    {
        public static double EvaluateAlpha(TextAnimationMaskConfig? mask, TextAnimationContext context)
        {
            if (mask == null)
                return 1.0;

            return mask.Type?.ToLowerInvariant() switch
            {
                "sinewave" => EvaluateSineWaveAlpha(mask, context),
                "undulation" => NormalizeSignedWave(GetUndulationRaw(mask, context)),
                "brightnessmask" => NormalizeSignedWave(GetBrightnessMaskRaw(mask, context) / 100.0),
                "constant" => Math.Clamp(mask.ConstantValue, 0.0, 1.0),
                _ => 1.0
            };
        }

        /// <summary>Raw wave in roughly [-1, 1] for brightness factor mapping.</summary>
        public static double EvaluateRawWave(TextAnimationMaskConfig? mask, TextAnimationContext context)
        {
            if (mask == null)
                return 0.0;

            return mask.Type?.ToLowerInvariant() switch
            {
                "sinewave" => Math.Sin(ResolveSineWavePhase(mask, context) + context.CharacterIndex * mask.CharacterPhaseOffset),
                "undulation" => GetUndulationRaw(mask, context),
                "brightnessmask" => GetBrightnessMaskRaw(mask, context) / 100.0,
                "constant" => mask.ConstantValue,
                _ => 0.0
            };
        }

        public static double EvaluateSineWaveAlpha(TextAnimationMaskConfig mask, TextAnimationContext context)
        {
            double raw = Math.Sin(ResolveSineWavePhase(mask, context) + context.CharacterIndex * mask.CharacterPhaseOffset);
            return (raw + 1.0) / 2.0;
        }

        /// <summary>
        /// Each sine-wave mask uses its own <see cref="TextAnimationMaskConfig.PhaseDivisorMs"/> when set,
        /// so stacked layers (e.g. path intro overlay + accent HSV) can animate at different speeds.
        /// </summary>
        private static double ResolveSineWavePhase(TextAnimationMaskConfig mask, TextAnimationContext context)
        {
            if (mask.PhaseDivisorMs > 0)
                return GetCurrentPhase(mask.PhaseDivisorMs);
            return context.Phase;
        }

        private static double GetUndulationRaw(TextAnimationMaskConfig mask, TextAnimationContext context)
        {
            var state = ResolveAnimationState(mask, context);
            if (state == null)
                return 0.0;
            return state.GetUndulationBrightnessAt(context.AdjustedPosition, context.LineOffset);
        }

        private static float GetBrightnessMaskRaw(TextAnimationMaskConfig mask, TextAnimationContext context)
        {
            var state = ResolveAnimationState(mask, context);
            if (state == null)
                return 0.0f;
            return state.GetBrightnessAt(context.AdjustedPosition, context.LineOffset);
        }

        private static BaseAnimationState? ResolveAnimationState(TextAnimationMaskConfig mask, TextAnimationContext context)
        {
            if (context.AnimationState != null)
                return context.AnimationState;

            return mask.AnimationState?.ToLowerInvariant() switch
            {
                "crit" => CritAnimationState.Instance,
                "dungeonselection" or "dungeon" => DungeonSelectionAnimationState.Instance,
                _ => DungeonSelectionAnimationState.Instance
            };
        }

        private static double NormalizeSignedWave(double raw)
        {
            return Math.Clamp((raw + 1.0) / 2.0, 0.0, 1.0);
        }

        public static double GetCurrentPhase(double phaseDivisorMs)
        {
            if (phaseDivisorMs <= 0)
                return 0.0;
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / phaseDivisorMs;
        }
    }
}
