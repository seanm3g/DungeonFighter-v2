using System;
using System.Collections.Generic;

namespace RPGGame.UI.TitleScreen
{
    /// <summary>
    /// Represents the phase of the title animation
    /// </summary>
    public enum AnimationPhase
    {
        BlackScreen,        // Initial black screen
        WhiteFlash,         // Brief flash of warm white
        WhiteHold,          // Hold white light
        ColorTransition,    // Transition to final colors
        FinalHold          // Hold on final colors
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
    /// Follows the Strategy pattern for generating different animation sequences
    /// </summary>
    public class TitleAnimation
    {
        private readonly TitleAnimationConfig _config;
        private readonly TitleFrameBuilder _frameBuilder;

        public TitleAnimation(TitleAnimationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _frameBuilder = new TitleFrameBuilder(config);
        }

        /// <summary>
        /// Generates the complete animation sequence as a series of steps
        /// This method is pure logic and can be unit tested
        /// </summary>
        /// <returns>Enumerable of animation steps to be rendered</returns>
        public IEnumerable<AnimationStep> GenerateAnimationSequence()
        {
            var scheme = _config.ColorScheme;
            int frameDuration = _config.FrameDurationMs;

            // Phase 1: Black screen (1 frame)
            yield return CreateSolidColorStep(
                AnimationPhase.BlackScreen,
                scheme.InitialColor,
                scheme.InitialColor,
                frameDuration
            );

            // Phase 2: Flash of warm white (1 frame)
            yield return CreateSolidColorStep(
                AnimationPhase.WhiteFlash,
                scheme.FlashColor1,
                scheme.FlashColor2,
                frameDuration
            );

            // Phase 3: Hold white light (configurable frames)
            for (int i = 0; i < _config.WhiteLightHoldFrames; i++)
            {
                yield return CreateSolidColorStep(
                    AnimationPhase.WhiteHold,
                    scheme.HoldColor,
                    scheme.HoldColor,
                    frameDuration
                );
            }

            // Phase 4: Transition to final colors (configurable frames)
            for (int frame = 0; frame <= _config.FinalTransitionFrames; frame++)
            {
                float progress = (float)frame / _config.FinalTransitionFrames;
                yield return CreateTransitionStep(
                    AnimationPhase.ColorTransition,
                    progress,
                    frameDuration
                );
            }

            // Phase 5: Final hold (render one more time with full progress)
            yield return CreateTransitionStep(
                AnimationPhase.FinalHold,
                1.0f,
                _config.FinalHoldDuration
            );
        }

        /// <summary>
        /// Creates an animation step with solid colors
        /// </summary>
        private AnimationStep CreateSolidColorStep(
            AnimationPhase phase,
            string dungeonColor,
            string fighterColor,
            int durationMs)
        {
            var frame = _frameBuilder.BuildSolidColorFrame(dungeonColor, fighterColor);
            return new AnimationStep(phase, frame, durationMs);
        }

        /// <summary>
        /// Creates an animation step with transition colors
        /// </summary>
        private AnimationStep CreateTransitionStep(
            AnimationPhase phase,
            float progress,
            int durationMs)
        {
            var frame = _frameBuilder.BuildTransitionFrame(progress);
            return new AnimationStep(phase, frame, durationMs);
        }

        /// <summary>
        /// Gets the total animation duration in milliseconds
        /// Useful for testing and progress indicators
        /// </summary>
        public int GetTotalDurationMs()
        {
            int frameDuration = _config.FrameDurationMs;
            int frameCount = 1 + // Black screen
                            1 + // White flash
                            _config.WhiteLightHoldFrames + // White hold
                            (_config.FinalTransitionFrames + 1); // Transition frames

            return (frameCount * frameDuration) + _config.FinalHoldDuration;
        }

        /// <summary>
        /// Gets the number of frames in the animation
        /// </summary>
        public int GetFrameCount()
        {
            return 2 + // Black + flash
                   _config.WhiteLightHoldFrames +
                   (_config.FinalTransitionFrames + 1) +
                   1; // Final hold
        }
    }
}

