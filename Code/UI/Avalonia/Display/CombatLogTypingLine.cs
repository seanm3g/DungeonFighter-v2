using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// The combat-log line the typewriter is currently revealing. Lets the renderer align a partial row
    /// as the finished line will be (center decision and width), so a centered headline types in place
    /// instead of starting left and snapping to center on its last character.
    /// </summary>
    public static class CombatLogTypingLine
    {
        private sealed class Entry
        {
            public Entry(List<ColoredText> line, string plain, UIMessageType messageType)
            {
                Line = line;
                Plain = plain;
                MessageType = messageType;
            }

            public List<ColoredText> Line { get; }
            public string Plain { get; }
            public UIMessageType MessageType { get; }
        }

        private static volatile Entry? current;

        public static void Begin(List<ColoredText> fullLine, UIMessageType messageType)
        {
            current = new Entry(fullLine, ColoredTextRenderer.RenderAsPlainText(fullLine), messageType);
        }

        public static void End(List<ColoredText> fullLine)
        {
            if (ReferenceEquals(current?.Line, fullLine))
                current = null;
        }

        /// <summary>
        /// When <paramref name="segments"/> is a strict, non-empty prefix of the line being typed, returns the
        /// finished line's plain length and whether the finished line centers.
        /// </summary>
        public static bool TryGetFinalLayout(IReadOnlyList<ColoredText>? segments, out int fullLength, out bool center)
        {
            fullLength = 0;
            center = false;
            var entry = current;
            if (entry == null || segments == null || segments.Count == 0)
                return false;

            string plain = ColoredTextRenderer.RenderAsPlainText(segments);
            if (plain.Length == 0 || plain.Length >= entry.Plain.Length
                || !entry.Plain.StartsWith(plain, StringComparison.Ordinal))
                return false;

            fullLength = entry.Plain.Length;
            center = CombatCenterPanelContentAlignment.ShouldCenterLine(entry.Line, entry.MessageType);
            return true;
        }
    }
}
