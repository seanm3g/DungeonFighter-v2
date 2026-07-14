using System.Collections.Generic;
using System.Linq;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>
    /// Immutable center-panel combat-log lines captured after an interactive lab Step for instant undo restore.
    /// </summary>
    public sealed class LabCombatLogSnapshot
    {
        public LabCombatLogSnapshot(IReadOnlyList<(List<ColoredText> Segments, UIMessageType MessageType)> lines)
        {
            Lines = lines ?? System.Array.Empty<(List<ColoredText>, UIMessageType)>();
        }

        public IReadOnlyList<(List<ColoredText> Segments, UIMessageType MessageType)> Lines { get; }

        /// <summary>Deep-clones structured buffer rows (segments + per-line message type).</summary>
        public static LabCombatLogSnapshot CloneFrom(
            IReadOnlyList<(List<ColoredText> Segments, UIMessageType MessageType)> source)
        {
            if (source == null || source.Count == 0)
                return new LabCombatLogSnapshot(System.Array.Empty<(List<ColoredText>, UIMessageType)>());

            var lines = new List<(List<ColoredText>, UIMessageType)>(source.Count);
            foreach (var (segments, messageType) in source)
            {
                var cloned = segments == null || segments.Count == 0
                    ? new List<ColoredText>()
                    : segments.Select(s => new ColoredText(s.Text, s.Color, s.SourceTemplate, s.ColorReadyForCanvas)).ToList();
                lines.Add((cloned, messageType));
            }

            return new LabCombatLogSnapshot(lines);
        }
    }
}
