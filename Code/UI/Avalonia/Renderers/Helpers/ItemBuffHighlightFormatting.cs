using System;
using System.Text.RegularExpressions;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Renderers.Helpers
{
    /// <summary>
    /// Highlight colors for buff values on item tooltips (effective stat modifiers).
    /// </summary>
    public static class ItemBuffHighlightFormatting
    {
        private static readonly Regex NumericChunkRegex = new(
            @"(\+[0-9]+(?:\.[0-9]+)?%?|\-[0-9]+(?:\.[0-9]+)?%?|−[0-9]+(?:\.[0-9]+)?%?|×[0-9]+(?:\.[0-9]+)?|\d+(?:\.[0-9]+)?%)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static void AppendContributionLine(ColoredTextBuilder builder, ItemStatContribution contribution)
        {
            if (!string.IsNullOrEmpty(contribution.SourceTag))
            {
                builder.Add(contribution.SourceTag, Colors.DarkGray);
                builder.Add(" — ", Colors.Gray);
            }

            builder.Add($"{contribution.Label}: ", ColorPalette.Info);

            bool debuff = contribution.IsDebuff;
            AppendHighlightedValue(builder, contribution.ValueText, debuff);
        }

        public static void AppendHighlightedValue(ColoredTextBuilder builder, string valueText, bool isDebuff = false)
        {
            if (string.IsNullOrEmpty(valueText))
                return;

            var color = isDebuff ? ColorPalette.Error.GetColor() : ColorPalette.Highlight.GetColor();
            var segment = new ColoredText(valueText, color);
            if (!isDebuff)
                segment.Undulate(0.07);
            builder.Add(segment);
        }

        /// <summary>
        /// Colors numeric fragments inside affix effect descriptions (e.g. +1, 25%, ×0.75).
        /// </summary>
        public static void AppendHighlightedDescription(ColoredTextBuilder builder, string description, bool defaultDebuff = false)
        {
            if (string.IsNullOrEmpty(description))
                return;

            int last = 0;
            foreach (Match m in NumericChunkRegex.Matches(description))
            {
                if (m.Index > last)
                    builder.Add(description.Substring(last, m.Index - last), Colors.White);

                string chunk = m.Value;
                bool debuff = defaultDebuff || chunk.StartsWith("-", StringComparison.Ordinal) ||
                                chunk.StartsWith("−", StringComparison.Ordinal);
                AppendHighlightedValue(builder, chunk, debuff);
                last = m.Index + m.Length;
            }

            if (last < description.Length)
                builder.Add(description.Substring(last), Colors.White);
        }
    }
}
