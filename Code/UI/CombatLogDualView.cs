using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI
{
    /// <summary>
    /// F7 dual-view combat log: each swing can store both the narrative prose form and the
    /// mechanical combat-log block. Toggling F7 swaps which form is displayed without
    /// regenerating combat (uses the alternate already bound on the buffer line).
    /// </summary>
    public static class CombatLogDualView
    {
        public readonly struct Line
        {
            public Line(
                List<ColoredText> segments,
                UIMessageType messageType,
                List<List<ColoredText>>? alternateLines,
                int dualSpan)
            {
                Segments = segments ?? new List<ColoredText>();
                MessageType = messageType;
                AlternateLines = alternateLines;
                DualSpan = dualSpan;
            }

            public List<ColoredText> Segments { get; }
            public UIMessageType MessageType { get; }
            /// <summary>Other view for this dual entry (mechanical tip when showing prose, or prose when showing mechanical).</summary>
            public List<List<ColoredText>>? AlternateLines { get; }
            /// <summary>
            /// When showing mechanical: number of consecutive display lines in this block (on the first line only).
            /// When showing narrative: always 1 for a prose anchor. 0 = not a dual-view anchor.
            /// </summary>
            public int DualSpan { get; }
        }

        /// <summary>
        /// Swaps dual-view entries between narrative (1 prose line + mechanical alternate)
        /// and mechanical (N lines + prose alternate on the first). Lines without an alternate pass through.
        /// </summary>
        /// <param name="source">Current buffer lines.</param>
        /// <param name="currentlyShowingNarrative">True when the buffer currently shows prose (F7 was on).</param>
        public static List<Line> Swap(IReadOnlyList<Line> source, bool currentlyShowingNarrative)
        {
            var result = new List<Line>();
            if (source == null || source.Count == 0)
                return result;

            int i = 0;
            while (i < source.Count)
            {
                var line = source[i];
                bool hasAlternate = line.AlternateLines != null && line.AlternateLines.Count > 0;

                if (currentlyShowingNarrative && hasAlternate)
                {
                    // Prose → mechanical: expand 1 line into the stored tip block.
                    var prose = CloneSegments(line.Segments);
                    var mechanical = CombatLogProseHoverInfo.CloneLines(line.AlternateLines)!;
                    for (int j = 0; j < mechanical.Count; j++)
                    {
                        bool first = j == 0;
                        result.Add(new Line(
                            mechanical[j],
                            first ? line.MessageType : InferMechanicalLineType(mechanical[j]),
                            first ? new List<List<ColoredText>> { prose } : null,
                            first ? mechanical.Count : 0));
                    }

                    i++;
                    continue;
                }

                if (!currentlyShowingNarrative && hasAlternate && line.DualSpan > 0)
                {
                    // Mechanical → prose: collapse DualSpan lines into the stored prose.
                    int span = Math.Min(line.DualSpan, source.Count - i);
                    var mechanical = new List<List<ColoredText>>(span);
                    for (int j = 0; j < span; j++)
                        mechanical.Add(CloneSegments(source[i + j].Segments));

                    var prose = CloneSegments(line.AlternateLines![0]);
                    // If alternate stored extra prose rows, keep the first (paragraph is one buffer entry).
                    result.Add(new Line(
                        prose,
                        UIMessageType.Combat,
                        mechanical,
                        dualSpan: 1));

                    i += span;
                    continue;
                }

                // Pass-through (no dual binding, or mechanical mid-block line).
                result.Add(new Line(
                    CloneSegments(line.Segments),
                    line.MessageType,
                    CombatLogProseHoverInfo.CloneLines(line.AlternateLines),
                    line.DualSpan));
                i++;
            }

            return result;
        }

        private static UIMessageType InferMechanicalLineType(List<ColoredText> segments)
        {
            if (segments == null || segments.Count == 0)
                return UIMessageType.System;

            string plain = ColoredTextRenderer.RenderAsPlainText(segments).TrimStart();
            if (plain.StartsWith("(roll:", StringComparison.OrdinalIgnoreCase)
                || plain.StartsWith("(speed:", StringComparison.OrdinalIgnoreCase))
                return UIMessageType.RollInfo;

            return UIMessageType.Combat;
        }

        private static List<ColoredText> CloneSegments(List<ColoredText> segments)
        {
            var copy = new List<ColoredText>();
            if (segments == null)
                return copy;

            foreach (var seg in segments)
            {
                if (seg == null)
                    continue;
                copy.Add(new ColoredText(
                    seg.Text ?? string.Empty,
                    seg.Color,
                    seg.SourceTemplate,
                    colorReadyForCanvas: seg.ColorReadyForCanvas));
            }

            return copy;
        }
    }
}
