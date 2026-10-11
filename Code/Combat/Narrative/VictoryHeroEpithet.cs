using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RPGGame.Utils;

namespace RPGGame.Combat.Narrative
{
    /// <summary>
    /// Builds a short noun-phrase epithet from a hero name for victory limericks,
    /// so verse can allude to the character without printing their literal name.
    /// </summary>
    public static class VictoryHeroEpithet
    {
        private static readonly string[] CompoundSuffixes =
        {
            // Longest-first so "bringer" wins over "er", etc.
            "bringer", "whisper", "thunder", "lightning", "shadow", "crystal", "diamond", "emerald",
            "phoenix", "griffin", "serpent", "thunder", "caller", "strike", "weaver", "walker",
            "reaper", "thirst", "rider", "heart", "blade", "storm", "shade", "arrow", "thorn",
            "forge", "fang", "claw", "wing", "moon", "wind", "fire", "born", "bane", "mark",
            "fist", "bolt", "fall", "wood", "brand"
        };

        private static readonly string[] ImageryTemplates =
        {
            "the {a}-weaving wanderer",
            "that {a}-tongued blade",
            "the {a}-scented champion",
            "that {b}-born traveler",
            "the {a}-{b} wanderer",
            "that ember of {a}",
            "the {a}-kissed traveler"
        };

        private static readonly string[] FallbackTemplates =
        {
            "the quiet-storm wanderer",
            "that steel-hearted blade",
            "the dusk-tempered traveler",
            "that shadow-sung champion",
            "the iron-willed wanderer"
        };

        /// <summary>
        /// Returns a short epithet phrase derived from <paramref name="heroName"/>.
        /// Never equals the exact full name and never contains forbidden name tokens as whole words.
        /// </summary>
        public static string FromName(string? heroName)
        {
            var parts = SplitNameParts(heroName);
            if (parts.Count == 0)
                return PickFallback(heroName);

            string last = parts.Count > 1 ? parts[^1] : parts[0];
            var imagery = BuildImageryTokens(parts, last);
            if (imagery.Count == 0)
                return PickFallback(heroName);

            string a = imagery[0];
            string b = imagery.Count > 1 ? imagery[1] : imagery[0];

            // Stable-ish rotation: pick from templates using a name-seeded index, then random nudge.
            int seed = Math.Abs((heroName ?? string.Empty).GetHashCode());
            string template = ImageryTemplates[(seed + RandomUtility.Next(ImageryTemplates.Length)) % ImageryTemplates.Length];
            string epithet = template
                .Replace("{a}", a, StringComparison.Ordinal)
                .Replace("{b}", b, StringComparison.Ordinal);

            if (string.Equals(epithet.Trim(), heroName?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                ContainsForbiddenName(epithet, heroName))
            {
                return PickFallback(heroName);
            }

            return epithet;
        }

        /// <summary>
        /// Whole-word name tokens (length ≥ 3) that must not appear in victory verse.
        /// Space-split first/last (and full name) only — CamelCase fragments are for epithet imagery, not bans.
        /// </summary>
        public static IReadOnlyList<string> ForbiddenNameTokens(string? heroName)
        {
            var tokens = new List<string>();
            foreach (string part in SplitNameParts(heroName))
            {
                if (part.Length >= 3 && !tokens.Contains(part, StringComparer.OrdinalIgnoreCase))
                    tokens.Add(part);
            }

            string? full = heroName?.Trim();
            if (!string.IsNullOrWhiteSpace(full) &&
                full.Length >= 3 &&
                !tokens.Contains(full, StringComparer.OrdinalIgnoreCase))
            {
                tokens.Add(full);
            }

            return tokens;
        }

        /// <summary>
        /// Replaces forbidden hero name tokens in <paramref name="text"/> with <paramref name="epithet"/>.
        /// </summary>
        public static string ScrubHeroName(string? text, string? heroName, string epithet)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            string result = text;
            string replacement = string.IsNullOrWhiteSpace(epithet) ? "the hero" : epithet.Trim();

            foreach (string token in ForbiddenNameTokens(heroName).OrderByDescending(t => t.Length))
            {
                result = ReplaceWholeWord(result, token, replacement);
            }

            return result;
        }

        /// <summary>True when any forbidden hero name token appears as a whole word in <paramref name="text"/>.</summary>
        public static bool ContainsForbiddenName(string? text, string? heroName)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            foreach (string token in ForbiddenNameTokens(heroName))
            {
                if (ContainsWholeWord(text, token))
                    return true;
            }
            return false;
        }

        internal static List<string> SplitNameParts(string? heroName)
        {
            if (string.IsNullOrWhiteSpace(heroName))
                return new List<string>();

            return heroName
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();
        }

        internal static List<string> SplitCamelCase(string word)
        {
            if (string.IsNullOrEmpty(word))
                return new List<string>();

            // True CamelCase / Pascal mid-caps (FlameWeaver).
            var bits = new List<string>();
            var current = new StringBuilder();
            for (int i = 0; i < word.Length; i++)
            {
                char ch = word[i];
                if (i > 0 && char.IsUpper(ch) && current.Length > 0)
                {
                    bits.Add(current.ToString());
                    current.Clear();
                }
                current.Append(ch);
            }

            if (current.Length > 0)
                bits.Add(current.ToString());

            if (bits.Count > 1)
                return bits.Where(b => b.Length > 0).ToList();

            // TitleCase compounds (Flameweaver) via known fantasy suffixes.
            return SplitCompoundSurname(word);
        }

        internal static List<string> SplitCompoundSurname(string word)
        {
            if (string.IsNullOrEmpty(word) || word.Length < 6)
                return new List<string> { word };

            string lower = word.ToLowerInvariant();
            foreach (string suffix in CompoundSuffixes.OrderByDescending(s => s.Length))
            {
                if (lower.Length <= suffix.Length + 2)
                    continue;
                if (!lower.EndsWith(suffix, StringComparison.Ordinal))
                    continue;

                int split = word.Length - suffix.Length;
                string head = word[..split];
                string tail = word[split..];
                if (head.Length < 3)
                    continue;
                return new List<string> { head, tail };
            }

            return new List<string> { word };
        }

        private static List<string> BuildImageryTokens(List<string> parts, string last)
        {
            var imagery = new List<string>();
            var forbidden = ForbiddenNameTokens(string.Join(' ', parts));

            void TryAdd(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    return;
                string token = raw.Trim().ToLowerInvariant();
                if (token.Length < 3)
                    return;
                if (forbidden.Any(f => string.Equals(f, token, StringComparison.OrdinalIgnoreCase)))
                    return;
                if (imagery.Contains(token, StringComparer.OrdinalIgnoreCase))
                    return;
                imagery.Add(token);
            }

            // CamelCase surname fragments first (Flameweaver → flame, weaver).
            foreach (string bit in SplitCamelCase(last))
                TryAdd(bit);

            // If surname did not split, derive a shortened stem that is not the full last name.
            if (imagery.Count == 0 && last.Length > 3)
                TryAdd(last[..^1]);

            // First-name stem as secondary imagery when still thin (Basil → basi).
            if (imagery.Count < 2 && parts[0].Length > 3)
                TryAdd(parts[0][..^1]);

            return imagery;
        }

        private static string PickFallback(string? heroName)
        {
            int seed = Math.Abs((heroName ?? "hero").GetHashCode());
            return FallbackTemplates[seed % FallbackTemplates.Length];
        }

        private static bool ContainsWholeWord(string input, string token)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(token))
                return false;

            for (int i = 0; i < input.Length; i++)
            {
                if (MatchesAt(input, i, token) && IsWordBoundary(input, i, token.Length))
                    return true;
            }
            return false;
        }

        private static string ReplaceWholeWord(string input, string token, string replacement)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(token))
                return input;

            var sb = new StringBuilder(input.Length);
            int i = 0;
            while (i < input.Length)
            {
                if (MatchesAt(input, i, token) && IsWordBoundary(input, i, token.Length))
                {
                    sb.Append(replacement);
                    i += token.Length;
                }
                else
                {
                    sb.Append(input[i]);
                    i++;
                }
            }

            return sb.ToString();
        }

        private static bool MatchesAt(string input, int index, string token)
        {
            if (index + token.Length > input.Length)
                return false;
            return string.Compare(input, index, token, 0, token.Length, StringComparison.OrdinalIgnoreCase) == 0;
        }

        private static bool IsWordBoundary(string input, int index, int length)
        {
            bool leftOk = index == 0 || !char.IsLetterOrDigit(input[index - 1]);
            int end = index + length;
            bool rightOk = end >= input.Length || !char.IsLetterOrDigit(input[end]);
            return leftOk && rightOk;
        }
    }
}
