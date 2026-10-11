using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Combat.Narrative
{
    /// <summary>Validates and normalizes model output into a 5-line limerick.</summary>
    public static class VictoryLimerickValidator
    {
        public const int RequiredLines = 5;

        public static bool TryNormalize(string? raw, out string limerick)
            => TryNormalize(raw, heroName: null, out limerick);

        /// <summary>
        /// Normalizes model/template output into a 5-line limerick.
        /// When <paramref name="heroName"/> is provided, rejects verse that still contains the hero's name tokens.
        /// </summary>
        public static bool TryNormalize(string? raw, string? heroName, out string limerick)
        {
            limerick = string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var lines = SplitLines(raw)
                .Select(CleanLine)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Where(l => !IsMetaLine(l))
                .Take(RequiredLines)
                .ToList();

            if (lines.Count != RequiredLines)
                return false;

            // Reject if any line is mostly digits / mechanical junk.
            if (lines.Any(l => CountDigits(l) > 2))
                return false;

            limerick = string.Join(System.Environment.NewLine, lines);

            if (!string.IsNullOrWhiteSpace(heroName) &&
                VictoryHeroEpithet.ContainsForbiddenName(limerick, heroName))
            {
                limerick = string.Empty;
                return false;
            }

            return true;
        }

        public static IReadOnlyList<string> SplitToDisplayLines(string limerick)
        {
            if (string.IsNullOrWhiteSpace(limerick))
                return Array.Empty<string>();
            return SplitLines(limerick).Select(CleanLine).Where(l => l.Length > 0).ToList();
        }

        private static IEnumerable<string> SplitLines(string raw)
        {
            return raw
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n', StringSplitOptions.None);
        }

        private static string CleanLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return string.Empty;

            string trimmed = line.Trim();
            // Strip common list / quote wrappers
            if (trimmed.Length >= 2 &&
                ((trimmed[0] == '"' && trimmed[^1] == '"') ||
                 (trimmed[0] == '\'' && trimmed[^1] == '\'')))
            {
                trimmed = trimmed[1..^1].Trim();
            }

            if (trimmed.Length > 2 && char.IsDigit(trimmed[0]) &&
                (trimmed[1] == '.' || trimmed[1] == ')' || trimmed[1] == ':'))
            {
                trimmed = trimmed[2..].Trim();
            }
            else if (trimmed.StartsWith("- ", StringComparison.Ordinal) ||
                     trimmed.StartsWith("* ", StringComparison.Ordinal))
            {
                trimmed = trimmed[2..].Trim();
            }

            return trimmed;
        }

        private static bool IsMetaLine(string line)
        {
            string lower = line.ToLowerInvariant();
            return lower.StartsWith("here", StringComparison.Ordinal) && lower.Contains("limerick")
                || lower.StartsWith("limerick:", StringComparison.Ordinal)
                || lower == "limerick"
                || lower.StartsWith("victory verse", StringComparison.Ordinal);
        }

        private static int CountDigits(string line) => line.Count(char.IsDigit);
    }
}
