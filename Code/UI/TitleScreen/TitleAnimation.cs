using System;
using System.Collections.Generic;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.TitleScreen
{
    /// <summary>
    /// Represents the phase of the title animation
    /// </summary>
    public enum AnimationPhase
    {
        BlackScreen,
        FadeIn,
        WhiteFlash,
        WhiteHold,
        Pop,
        ColorTransition,
        Settle,
        FinalHold,
        IdleCycle
    }

    /// <summary>
    /// Represents a single animation step with timing and frame data
    /// </summary>
    public class AnimationStep
    {
        public AnimationPhase Phase { get; set; }
        public TitleFrame Frame { get; set; }
        public int DurationMs { get; set; }

        public AnimationStep(AnimationPhase phase, TitleFrame frame, int durationMs)
        {
            Phase = phase;
            Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            DurationMs = durationMs;
        }
    }

    /// <summary>
    /// Generates the title screen animation sequence
    /// Testable class that contains only animation logic, no rendering
    /// </summary>
    public class TitleAnimation
    {
        private readonly TitleAnimationConfig _config;
        private readonly TitleFrameBuilder _frameBuilder;
        private TitleIdlePalette _palette;

        public TitleAnimation(TitleAnimationConfig config, TitleIdlePalette? palette = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _frameBuilder = new TitleFrameBuilder(config);
            _palette = palette ?? TitleIdlePalettePicker.CreateFallback();
        }

        public TitleIdlePalette Palette => _palette;

        /// <summary>
        /// Swaps the idle color pattern mid-loop (e.g. every few seconds).
        /// </summary>
        public void SetPalette(TitleIdlePalette palette)
        {
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
        }

        /// <summary>
        /// Generates the intro animation sequence (fade → pop → settle). Idle is separate.
        /// </summary>
        public IEnumerable<AnimationStep> GenerateAnimationSequence()
        {
            var scheme = _config.ColorScheme;
            int frameDuration = _config.FrameDurationMs;

            int blackFrames = Math.Max(1, _config.BlackScreenFrames);
            for (int i = 0; i < blackFrames; i++)
            {
                yield return CreateSolidColorStep(
                    AnimationPhase.BlackScreen,
                    scheme.InitialColor,
                    scheme.InitialColor,
                    frameDuration);
            }

            int fadeFrames = Math.Max(1, _config.FadeInFrames);
            for (int frame = 0; frame < fadeFrames; frame++)
            {
                float progress = fadeFrames <= 1 ? 1f : (float)(frame + 1) / fadeFrames;
                string color = progress < 0.5f ? scheme.FlashColor1 : scheme.HoldColor;
                yield return CreateSolidColorStep(
                    AnimationPhase.FadeIn,
                    color,
                    color,
                    frameDuration);
            }

            // Brief legacy white flash/hold for compatibility with older timing knobs
            yield return CreateSolidColorStep(
                AnimationPhase.WhiteFlash,
                scheme.FlashColor1,
                scheme.FlashColor2,
                frameDuration);

            int holdFrames = Math.Max(0, _config.WhiteLightHoldFrames);
            for (int i = 0; i < holdFrames; i++)
            {
                yield return CreateSolidColorStep(
                    AnimationPhase.WhiteHold,
                    scheme.HoldColor,
                    scheme.HoldColor,
                    frameDuration);
            }

            int popFrames = Math.Max(1, _config.PopFrames);
            string popColor = string.IsNullOrWhiteSpace(scheme.PopColor) ? "Y" : scheme.PopColor;
            for (int i = 0; i < popFrames; i++)
            {
                yield return CreateSolidColorStep(
                    AnimationPhase.Pop,
                    popColor,
                    popColor,
                    frameDuration);
            }

            int settleFrames = Math.Max(1, _config.EffectiveSettleFrames);
            for (int frame = 0; frame <= settleFrames; frame++)
            {
                float progress = (float)frame / settleFrames;
                yield return new AnimationStep(
                    AnimationPhase.Settle,
                    _frameBuilder.BuildSettleFrame(_palette, progress),
                    frameDuration);
            }

            yield return new AnimationStep(
                AnimationPhase.FinalHold,
                _frameBuilder.BuildPhasedPaletteFrame(_palette, 0),
                _config.FinalHoldDuration);
        }

        /// <summary>
        /// Builds a single idle-cycle frame using the dungeon-selection undulation compositor.
        /// Optionally crossfades glyph colors from <paramref name="blendFrom"/> into the current
        /// (or <paramref name="blendTo"/>) palette.
        /// </summary>
        public TitleFrame BuildIdleFrame(
            BaseAnimationState? animationState = null,
            TitleIdlePalette? blendFrom = null,
            TitleIdlePalette? blendTo = null,
            float blendProgress = 1f)
        {
            var target = blendTo ?? _palette;
            return _frameBuilder.BuildComposedIdleFrame(target, animationState, blendFrom, blendProgress);
        }

        private AnimationStep CreateSolidColorStep(
            AnimationPhase phase,
            string dungeonColor,
            string fighterColor,
            int durationMs)
        {
            var frame = _frameBuilder.BuildSolidColorFrame(dungeonColor, fighterColor);
            return new AnimationStep(phase, frame, durationMs);
        }

        public int GetTotalDurationMs()
        {
            int frameDuration = _config.FrameDurationMs;
            int frameCount =
                Math.Max(1, _config.BlackScreenFrames) +
                Math.Max(1, _config.FadeInFrames) +
                1 + // White flash
                Math.Max(0, _config.WhiteLightHoldFrames) +
                Math.Max(1, _config.PopFrames) +
                (Math.Max(1, _config.EffectiveSettleFrames) + 1); // settle including progress 1.0

            return (frameCount * frameDuration) + _config.FinalHoldDuration;
        }

        public int GetFrameCount()
        {
            return Math.Max(1, _config.BlackScreenFrames) +
                   Math.Max(1, _config.FadeInFrames) +
                   1 +
                   Math.Max(0, _config.WhiteLightHoldFrames) +
                   Math.Max(1, _config.PopFrames) +
                   (Math.Max(1, _config.EffectiveSettleFrames) + 1) +
                   1; // Final hold
        }
    }
}
