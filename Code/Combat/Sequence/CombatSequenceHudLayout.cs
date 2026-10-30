using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// Horizontal packing for the two-row combat sequence HUD: titles on row 1, results on row 2.
    /// </summary>
    public static class CombatSequenceHudLayout
    {
        public const int SeparatorWidth = 1;

        /// <summary>
        /// Fixed dungeon HUD columns. DEFENSE, TARGET and EFFECTS stay visible even when a swing has no value there.
        /// Status effects land on the target before the damage number, matching the combat board's beat order.
        /// TARGET names who the next EFFECTS lands on; a swing with several effects plays several TARGET/EFFECTS pairs.
        /// </summary>
        public static readonly string[] ColumnTitles =
        {
            "ATTACKER", "ROLL", "OUTCOME", "ACTION", "DEFENSE", "TARGET", "EFFECTS", "DAMAGE"
        };

        public static int ColumnIndexFor(CombatSequenceStepKind kind) =>
            kind switch
            {
                CombatSequenceStepKind.Attacker => 0,
                CombatSequenceStepKind.Roll => 1,
                CombatSequenceStepKind.Outcome => 2,
                CombatSequenceStepKind.Action => 3,
                CombatSequenceStepKind.Defense => 4,
                CombatSequenceStepKind.Target => 5,
                CombatSequenceStepKind.Effect => 6,
                CombatSequenceStepKind.Damage => 7,
                CombatSequenceStepKind.Heal => 7,
                _ => -1
            };

        /// <summary>
        /// Step a column shows: the latest one of that column that has played (so repeated TARGET/EFFECTS pairs
        /// cycle through the cell), else the first one still pending.
        /// </summary>
        public static int FindDisplayStepIndex(IReadOnlyList<CombatSequenceStep> steps, int columnIndex, Func<int, Phase> phaseOf)
        {
            if (steps == null)
                return -1;
            int first = -1, latest = -1;
            for (int i = 0; i < steps.Count; i++)
            {
                if (ColumnIndexFor(steps[i].Kind) != columnIndex)
                    continue;
                if (first < 0)
                    first = i;
                if (phaseOf(i) != Phase.Pending)
                    latest = i;
            }
            return latest >= 0 ? latest : first;
        }

        public static int FindStepIndex(IReadOnlyList<CombatSequenceStep> steps, int columnIndex)
        {
            if (steps == null)
                return -1;
            for (int i = 0; i < steps.Count; i++)
            {
                if (ColumnIndexFor(steps[i].Kind) == columnIndex)
                    return i;
            }
            return -1;
        }

        public enum Phase
        {
            Pending,
            Active,
            Complete
        }

        public readonly struct ColumnRect
        {
            public ColumnRect(int x, int width)
            {
                X = x;
                Width = width;
            }

            public int X { get; }
            public int Width { get; }
        }

        /// <summary>
        /// Active while <paramref name="index"/> equals <paramref name="currentIndex"/>.
        /// Indices below that are complete; a current index equal to the step count means every column is complete.
        /// </summary>
        public static Phase GetPhase(int index, int currentIndex)
        {
            if (currentIndex < 0 || index > currentIndex)
                return Phase.Pending;
            if (index < currentIndex)
                return Phase.Complete;
            return Phase.Active;
        }

        public static ColorPalette TitlePalette(Phase phase)
        {
            return phase switch
            {
                Phase.Active => ColorPalette.Gold,
                Phase.Complete => ColorPalette.White,
                _ => ColorPalette.DarkGray
            };
        }

        public static ColumnRect[] MeasureColumns(int originX, int innerWidth, int count)
        {
            if (count <= 0 || innerWidth <= 0)
                return Array.Empty<ColumnRect>();

            int separators = Math.Max(0, count - 1) * SeparatorWidth;
            int usable = Math.Max(count, innerWidth - separators);
            int baseWidth = Math.Max(1, usable / count);
            int remainder = Math.Max(0, usable - baseWidth * count);

            var columns = new ColumnRect[count];
            int x = originX;
            int maxX = originX + innerWidth;
            for (int i = 0; i < count; i++)
            {
                int width = baseWidth + (i < remainder ? 1 : 0);
                if (x + width > maxX)
                    width = Math.Max(0, maxX - x);
                columns[i] = new ColumnRect(x, width);
                x += width + (i < count - 1 ? SeparatorWidth : 0);
            }

            return columns;
        }

        public static string FormatTitle(string title, Phase phase, int width)
        {
            if (width <= 0)
                return string.Empty;

            string text = title ?? string.Empty;
            if (phase == Phase.Active && text.Length + 2 <= width)
                text = "[" + text + "]";

            if (text.Length <= width)
                return text;
            return text.Substring(0, width);
        }

        public static bool ShowsResult(Phase phase, bool resultRevealed)
        {
            if (phase == Phase.Complete)
                return true;
            if (phase == Phase.Active && resultRevealed)
                return true;
            return false;
        }

        public static List<ColoredText> FormatResult(
            IReadOnlyList<ColoredText>? result,
            Phase phase,
            bool resultRevealed,
            int width)
        {
            if (width <= 0 || !ShowsResult(phase, resultRevealed) || result == null || result.Count == 0)
                return new List<ColoredText>();
            return ColoredTextRenderer.Truncate(result, width);
        }
    }
}
