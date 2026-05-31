using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.TextAnimation;

namespace RPGGame.UI.Avalonia.Renderers.Text
{
    /// <summary>
    /// Character-wise layered text animation used by dungeon selection, display log, and completion UI.
    /// </summary>
    public static class UndulatingTextHelper
    {
        public const string DisplayLogShimmerPreset = "displayLogShimmer";

        public static int GetElementRandomOffset(int lineOffset)
        {
            int hash = (lineOffset * 7919 + 12345) % 101;
            return hash - 50;
        }

        public static Color AdjustColorBrightness(Color color, double factor)
            => ColorValidator.ScaleBrightnessHsv(color, factor);

        /// <summary>
        /// Expands plain text into per-character segments with undulation (no template = always undulate).
        /// </summary>
        public static List<ColoredText> ApplyUndulationToPlainText(string text, Color baseColor, int lineY)
        {
            var list = new List<ColoredText>();
            int pos = 0;
            AppendAnimatedChars(text ?? "", baseColor, list, ref pos, lineY, sourceTemplate: null);
            return list;
        }

        /// <summary>
        /// Applies the same animation as the level-up banner in <see cref="DisplayRenderer"/> to a full segment line.
        /// </summary>
        public static List<ColoredText> ApplyUndulationToSegmentLine(List<ColoredText> segments, int lineY)
        {
            var animated = new List<ColoredText>();
            int charPos = 0;
            if (segments == null)
                return animated;

            AppendComposedFromSegments(DisplayLogShimmerPreset, segments, animated, ref charPos, lineY);
            return animated;
        }

        public static void AppendAnimatedChars(
            string text,
            Color baseColor,
            List<ColoredText> result,
            ref int startCharPosition,
            int lineOffset,
            string? sourceTemplate,
            BaseAnimationState? animationState = null)
        {
            var state = animationState ?? DungeonSelectionAnimationState.Instance;
            int elementOffset = GetElementRandomOffset(lineOffset);

            TextAnimationCompositor.AppendComposed(
                DisplayLogShimmerPreset,
                text,
                baseColor,
                result,
                ref startCharPosition,
                lineOffset,
                sourceTemplate,
                state,
                elementOffset);
        }

        public static void AppendComposedFromSegments(
            string presetName,
            IReadOnlyList<ColoredText> segments,
            List<ColoredText> result,
            ref int startCharPosition,
            int lineOffset,
            BaseAnimationState? animationState = null)
        {
            var state = animationState ?? DungeonSelectionAnimationState.Instance;
            int elementOffset = GetElementRandomOffset(lineOffset);

            TextAnimationCompositor.AppendComposedFromSegments(
                presetName,
                segments,
                result,
                ref startCharPosition,
                lineOffset,
                state,
                elementOffset);
        }
    }
}
