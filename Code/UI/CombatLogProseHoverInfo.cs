using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI
{
    /// <summary>
    /// Helpers for F7 narrative prose hover: the mechanical combat-log block
    /// (action headline, roll info, status lines) shown when hovering a prose paragraph.
    /// </summary>
    public static class CombatLogProseHoverInfo
    {
        /// <summary>
        /// Builds tip lines from <see cref="BlockDisplay.BlockMessageCollector"/> groups
        /// (empty spacer rows kept so the tip matches the combat-log info layout).
        /// </summary>
        public static List<List<ColoredText>> FromMessageGroups(
            IReadOnlyList<(List<ColoredText> segments, RPGGame.UIMessageType messageType)>? groups)
        {
            var lines = new List<List<ColoredText>>();
            if (groups == null || groups.Count == 0)
                return lines;

            foreach (var (segments, _) in groups)
            {
                if (segments == null || segments.Count == 0)
                {
                    lines.Add(new List<ColoredText>());
                    continue;
                }

                lines.Add(CloneSegments(segments));
            }

            // Drop leading/trailing blank rows so the tip is tight.
            while (lines.Count > 0 && lines[0].Count == 0)
                lines.RemoveAt(0);
            while (lines.Count > 0 && lines[lines.Count - 1].Count == 0)
                lines.RemoveAt(lines.Count - 1);

            return lines;
        }

        public static List<List<ColoredText>>? CloneLines(IReadOnlyList<List<ColoredText>>? lines)
        {
            if (lines == null || lines.Count == 0)
                return null;

            var copy = new List<List<ColoredText>>(lines.Count);
            foreach (var line in lines)
                copy.Add(line == null || line.Count == 0 ? new List<ColoredText>() : CloneSegments(line));
            return copy;
        }

        /// <summary>
        /// Joins two combat-log tip blocks with a blank spacer (same-attacker paragraph continuation).
        /// </summary>
        public static List<List<ColoredText>> AppendBlocks(
            IReadOnlyList<List<ColoredText>>? existing,
            IReadOnlyList<List<ColoredText>>? next)
        {
            var result = new List<List<ColoredText>>();
            if (existing != null)
            {
                foreach (var line in existing)
                    result.Add(line == null || line.Count == 0 ? new List<ColoredText>() : CloneSegments(line));
            }

            if (next == null || next.Count == 0)
                return result;

            if (result.Count > 0)
                result.Add(new List<ColoredText>());

            foreach (var line in next)
                result.Add(line == null || line.Count == 0 ? new List<ColoredText>() : CloneSegments(line));

            return result;
        }

        /// <summary>Stable fingerprint for hover-state change detection.</summary>
        public static string Fingerprint(IReadOnlyList<List<ColoredText>>? lines)
        {
            if (lines == null || lines.Count == 0)
                return string.Empty;

            var parts = new List<string>(lines.Count);
            foreach (var line in lines)
            {
                if (line == null || line.Count == 0)
                    parts.Add(string.Empty);
                else
                    parts.Add(ColoredTextRenderer.RenderAsPlainText(line));
            }

            return string.Join("\n", parts);
        }

        private static List<ColoredText> CloneSegments(List<ColoredText> segments)
        {
            var copy = new List<ColoredText>(segments.Count);
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
