using System;
using System.Collections.Generic;
using RPGGame.UI.BlockDisplay;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Center-panel combat log horizontal alignment: enemy primary lines and their indented
    /// roll/stat follow-ups are right-justified within the content column; hero lines stay left.
    /// </summary>
    public static class CombatCenterPanelEnemyLineAlignment
    {
        /// <summary>
        /// Returns true when <paramref name="segments"/> is a non-indented line whose visible text
        /// begins with the enemy name (primary enemy action line only).
        /// </summary>
        public static bool ShouldRightAlignEnemyPrimaryCombatLine(IReadOnlyList<ColoredText>? segments, string? enemyName)
        {
            if (segments == null || segments.Count == 0 || string.IsNullOrEmpty(enemyName))
                return false;

            string plain = ColoredTextRenderer.RenderAsPlainText(segments);
            if (string.IsNullOrWhiteSpace(plain))
                return false;

            if (plain.StartsWith(BlockMessageCollector.ActionBlockSubsequentIndent, StringComparison.Ordinal))
                return false;

            string trimmed = plain.TrimStart();
            if (trimmed.Length < enemyName.Length)
                return false;

            return trimmed.StartsWith(enemyName, StringComparison.Ordinal);
        }

        /// <summary>
        /// For each buffer row, whether to right-align within the combat column (enemy primary + indented
        /// continuation rows that belong to the enemy action block until the next hero/other primary line).
        /// </summary>
        public static bool[] ResolveRightAlignFlags(
            IReadOnlyList<List<ColoredText>> lines,
            string? enemyName,
            string? heroName)
        {
            var result = new bool[lines.Count];
            bool inEnemyContinuation = false;

            for (int i = 0; i < lines.Count; i++)
            {
                var segments = lines[i];
                if (segments == null || segments.Count == 0)
                {
                    result[i] = false;
                    continue;
                }

                string plain = ColoredTextRenderer.RenderAsPlainText(segments);
                if (string.IsNullOrWhiteSpace(plain))
                {
                    result[i] = false;
                    continue;
                }

                bool isIndented = plain.StartsWith(BlockMessageCollector.ActionBlockSubsequentIndent, StringComparison.Ordinal);
                string trimmed = plain.TrimStart();

                bool enemyPrimary = IsActorPrimaryLine(trimmed, enemyName, isIndented);
                bool heroPrimary = IsActorPrimaryLine(trimmed, heroName, isIndented);

                if (enemyPrimary && heroPrimary && !string.IsNullOrEmpty(enemyName) && !string.IsNullOrEmpty(heroName))
                {
                    if (enemyName.Length > heroName.Length)
                        heroPrimary = false;
                    else if (heroName.Length > enemyName.Length)
                        enemyPrimary = false;
                    else
                        enemyPrimary = false;
                }

                if (enemyPrimary)
                {
                    inEnemyContinuation = true;
                    result[i] = true;
                }
                else if (heroPrimary)
                {
                    inEnemyContinuation = false;
                    result[i] = false;
                }
                else if (isIndented && inEnemyContinuation)
                {
                    result[i] = true;
                }
                else
                {
                    if (!isIndented)
                        inEnemyContinuation = false;
                    result[i] = false;
                }
            }

            return result;
        }

        private static bool IsActorPrimaryLine(string trimmed, string? name, bool isIndented)
        {
            if (isIndented || string.IsNullOrEmpty(name))
                return false;
            return trimmed.Length >= name.Length && trimmed.StartsWith(name, StringComparison.Ordinal);
        }
    }
}
