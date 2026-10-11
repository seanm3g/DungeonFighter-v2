using System;
using System.Collections.Generic;
using RPGGame.UI.Avalonia.Canvas;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Finds the pixel AABB of an action word in center-panel canvas text for DistortionPulse.
    /// Prefers the lowest (newest) match in the combat-log band.
    /// The rigid exclude rect pads one cell on each side so flanking spaces do not flee
    /// and leave a visible hole before/after the word.
    /// </summary>
    public static class ActionTextEmphasisWordLocator
    {
        /// <summary>
        /// Extra rigid cells on each side of the matched word (keeps separators from vacating).
        /// </summary>
        public const int FlankingExcludeCells = 1;

        public static bool TryLocate(
            IReadOnlyList<CanvasText> textElements,
            string word,
            double charWidth,
            double charHeight,
            out double originX,
            out double originY,
            out double excludeMinX,
            out double excludeMaxX,
            out double excludeMinY,
            out double excludeMaxY)
        {
            originX = originY = 0;
            excludeMinX = excludeMaxX = excludeMinY = excludeMaxY = 0;

            if (textElements == null || textElements.Count == 0
                || string.IsNullOrWhiteSpace(word)
                || charWidth <= 0 || charHeight <= 0)
            {
                return false;
            }

            string needle = word.Trim();
            int bestY = int.MinValue;
            int bestX = int.MinValue;
            int bestIndex = -1;
            int bestLen = 0;
            bool found = false;

            for (int i = 0; i < textElements.Count; i++)
            {
                var text = textElements[i];
                if (text == null || text.IsOverlay)
                    continue;
                if (!LayoutConstants.ContainsCenterPanelContent(text.X, text.Y))
                    continue;

                string content = text.Content ?? string.Empty;
                if (content.Length == 0)
                    continue;

                int searchFrom = 0;
                while (searchFrom <= content.Length - needle.Length)
                {
                    int idx = content.IndexOf(needle, searchFrom, StringComparison.OrdinalIgnoreCase);
                    if (idx < 0)
                        break;

                    int end = idx + needle.Length;
                    if (IsWordBoundary(content, idx, end))
                    {
                        // Prefer lowest Y, then rightmost X (newest combat-log paint).
                        if (!found
                            || text.Y > bestY
                            || (text.Y == bestY && text.X + idx >= bestX + bestIndex))
                        {
                            found = true;
                            bestY = text.Y;
                            bestX = text.X;
                            bestIndex = idx;
                            bestLen = needle.Length;
                        }
                    }

                    searchFrom = idx + 1;
                }
            }

            if (!found || bestLen <= 0)
                return false;

            int gridStartX = bestX + bestIndex;
            // Origin stays on the word center; exclude pads flanking cells so separator
            // spaces (often their own CanvasText run) stay rigid during the neighbor push.
            int excludeStartX = gridStartX - FlankingExcludeCells;
            int excludeEndX = gridStartX + bestLen + FlankingExcludeCells;
            excludeMinX = excludeStartX * charWidth;
            excludeMaxX = excludeEndX * charWidth;
            excludeMinY = bestY * charHeight;
            excludeMaxY = (bestY + 1) * charHeight;
            originX = (gridStartX + bestLen * 0.5) * charWidth;
            originY = (excludeMinY + excludeMaxY) * 0.5;
            return true;
        }

        private static bool IsWordBoundary(string plain, int start, int end)
        {
            if (start > 0 && IsWordChar(plain[start - 1]))
                return false;
            if (end < plain.Length && IsWordChar(plain[end]))
                return false;
            return true;
        }

        private static bool IsWordChar(char c) =>
            char.IsLetterOrDigit(c) || c == '_' || c == '\'';
    }
}
