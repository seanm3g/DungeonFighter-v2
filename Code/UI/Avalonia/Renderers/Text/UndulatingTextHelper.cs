using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Renderers.Text
{
    /// <summary>
    /// Character-wise brightness / undulation used by dungeon selection, display log, and completion UI.
    /// </summary>
    public static class UndulatingTextHelper
    {
        public static int GetElementRandomOffset(int lineOffset)
        {
            int hash = (lineOffset * 7919 + 12345) % 101;
            return hash - 50;
        }

        public static Color AdjustColorBrightness(Color color, double factor)
        {
            factor = System.Math.Max(0.0, System.Math.Min(2.0, factor));
            byte r = (byte)System.Math.Min(255, (int)(color.R * factor));
            byte g = (byte)System.Math.Min(255, (int)(color.G * factor));
            byte b = (byte)System.Math.Min(255, (int)(color.B * factor));
            return Color.FromRgb(r, g, b);
        }

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
            foreach (var segment in segments)
            {
                string t = segment.Text ?? "";
                AppendAnimatedChars(t, segment.Color, animated, ref charPos, lineY, segment.SourceTemplate);
            }
            return animated;
        }

        public static void AppendAnimatedChars(
            string text,
            Color baseColor,
            List<ColoredText> result,
            ref int startCharPosition,
            int lineOffset,
            string? sourceTemplate,
            DungeonSelectionAnimationState? animationState = null)
        {
            var state = animationState ?? DungeonSelectionAnimationState.Instance;
            bool shouldUndulate;
            if (string.IsNullOrEmpty(sourceTemplate))
                shouldUndulate = true;
            else
            {
                var templateData = ColorTemplateLoader.GetTemplate(sourceTemplate);
                shouldUndulate = templateData != null && templateData.Undulate;
            }

            int elementOffset = GetElementRandomOffset(lineOffset);
            foreach (char c in text ?? "")
            {
                int adjustedPosition = startCharPosition + elementOffset;
                float brightnessAdjustment = state.GetBrightnessAt(adjustedPosition, lineOffset);
                double brightnessFactor = 1.0 + (brightnessAdjustment / 100.0) * 2.0;
                brightnessFactor = System.Math.Max(0.3, System.Math.Min(2.0, brightnessFactor));
                if (shouldUndulate)
                {
                    double undulationBrightness = state.GetUndulationBrightnessAt(adjustedPosition, lineOffset);
                    brightnessFactor += undulationBrightness * 3.0;
                    brightnessFactor = System.Math.Max(0.3, System.Math.Min(2.0, brightnessFactor));
                }

                Color adjustedColor = AdjustColorBrightness(baseColor, brightnessFactor);
                result.Add(new ColoredText(c.ToString(), adjustedColor, sourceTemplate));
                startCharPosition++;
            }
        }
    }
}
