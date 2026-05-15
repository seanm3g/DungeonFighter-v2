using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Center-panel combat log: center-justify environment, menu, title, leveling, encounter headlines,
    /// enemy-defeated summary, remaining-health lines, and post-dungeon outcome summaries within the content column.
    /// </summary>
    public static class CombatCenterPanelContentAlignment
    {
        /// <summary>
        /// Per buffer row, whether the line should be center-aligned (takes precedence over enemy right-align).
        /// </summary>
        public static bool[] ResolveCenterAlignFlags(
            IReadOnlyList<List<ColoredText>> lines,
            IReadOnlyList<UIMessageType> messageTypes)
        {
            var result = new bool[lines.Count];
            if (lines.Count == 0)
                return result;

            if (messageTypes == null || messageTypes.Count != lines.Count)
            {
                for (int i = 0; i < lines.Count; i++)
                    result[i] = ShouldCenterLine(lines[i], UIMessageType.System);
                return result;
            }

            for (int i = 0; i < lines.Count; i++)
                result[i] = ShouldCenterLine(lines[i], messageTypes[i]);

            return result;
        }

        /// <summary>
        /// True when this row is a menu/title/environment message, post-dungeon outcome summary, or matches level-up / dungeon header heuristics.
        /// </summary>
        public static bool ShouldCenterLine(IReadOnlyList<ColoredText>? segments, UIMessageType messageType)
        {
            if (messageType == UIMessageType.OutcomeSummary)
            {
                if (segments == null || segments.Count == 0)
                    return false;
                string outcomePlain = ColoredTextRenderer.RenderAsPlainText(segments);
                return !string.IsNullOrWhiteSpace(outcomePlain);
            }

            if (messageType == UIMessageType.Menu ||
                messageType == UIMessageType.Title ||
                messageType == UIMessageType.MainTitle ||
                messageType == UIMessageType.Environmental)
                return true;

            if (segments == null || segments.Count == 0)
                return false;

            string plain = ColoredTextRenderer.RenderAsPlainText(segments);
            return ShouldCenterByPlainContent(plain);
        }

        internal static bool ShouldCenterByPlainContent(string plain)
        {
            if (string.IsNullOrWhiteSpace(plain))
                return false;

            string t = plain.Trim();

            if (t.Contains("LEVEL UP!", StringComparison.Ordinal))
                return true;
            if (t.Contains("*** LEVEL UP", StringComparison.Ordinal))
                return true;
            if (t.StartsWith("You reached level ", StringComparison.OrdinalIgnoreCase))
                return true;
            if (t.StartsWith("Gained ", StringComparison.OrdinalIgnoreCase) && t.Contains("class point", StringComparison.OrdinalIgnoreCase))
                return true;
            if (t.StartsWith("Gained ", StringComparison.OrdinalIgnoreCase) && t.Contains("action slot", StringComparison.OrdinalIgnoreCase))
                return true;
            if (t.StartsWith("Stats increased:", StringComparison.OrdinalIgnoreCase))
                return true;
            if (t.StartsWith("Current class:", StringComparison.OrdinalIgnoreCase))
                return true;
            if (t.StartsWith("You are now known as:", StringComparison.OrdinalIgnoreCase))
                return true;
            if (t.StartsWith("Class Points:", StringComparison.OrdinalIgnoreCase))
                return true;
            if (t.StartsWith("Next Upgrades:", StringComparison.OrdinalIgnoreCase))
                return true;
            if (t.Contains("No weapon equipped - equal stat increases", StringComparison.OrdinalIgnoreCase))
                return true;

            if (t.Contains("ENTERING DUNGEON", StringComparison.OrdinalIgnoreCase))
                return true;
            if (t.Contains("ENTERING ROOM", StringComparison.OrdinalIgnoreCase))
                return true;
            if (t.StartsWith("Dungeon:", StringComparison.OrdinalIgnoreCase))
                return true;
            if (t.StartsWith("Room:", StringComparison.OrdinalIgnoreCase))
                return true;
            if (t.StartsWith("===", StringComparison.Ordinal))
                return true;

            if (t.StartsWith("It appears you are safe", StringComparison.OrdinalIgnoreCase))
                return true;

            // Enemy encounter headline from EnemyInfoBuilder: "A {name} appears." / "A {name} with {weapon} appears."
            if (t.StartsWith("A ", StringComparison.Ordinal) && t.EndsWith(" appears.", StringComparison.Ordinal))
                return true;

            // Post-combat summary (EnemyEncounterHandler): "{Enemy} has been defeated!" — must center so it is not
            // treated as an enemy primary combat line (which would right-align within the column).
            if (t.Contains(" has been defeated!", StringComparison.Ordinal))
                return true;

            if (t.StartsWith("Remaining Health:", StringComparison.OrdinalIgnoreCase))
                return true;

            if (t.Contains("🌍", StringComparison.Ordinal))
                return true;

            return false;
        }
    }
}
