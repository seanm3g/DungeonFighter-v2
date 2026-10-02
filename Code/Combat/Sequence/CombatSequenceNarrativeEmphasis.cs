using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// Colors F7 narrative prose sparsely: identity tokens (names, action) stand out as spans;
    /// only high-signal outcome/effect keywords (wound, critical, poison, …) are painted via
    /// <see cref="KeywordColorSystem"/>. Atmospheric prose (fortune, stance, steel, …) stays white.
    /// Canvas combat log emphasizes via color (no bold/italic typeface path yet).
    /// </summary>
    public static class CombatSequenceNarrativeEmphasis
    {
        public static List<ColoredText> ColorizeSentence(string sentence, CombatSequenceFlavorTokens tokens)
        {
            if (string.IsNullOrWhiteSpace(sentence))
                return new List<ColoredText>();

            var phrases = BuildEmphasisPhrases(tokens);
            if (phrases.Count == 0)
                return KeywordColorSystem.Colorize(sentence);

            var matches = FindNonOverlappingMatches(sentence, phrases);
            if (matches.Count == 0)
                return KeywordColorSystem.Colorize(sentence);

            var result = new List<ColoredText>();
            int cursor = 0;
            foreach (var match in matches)
            {
                if (match.Start > cursor)
                {
                    string gap = sentence.Substring(cursor, match.Start - cursor);
                    result.AddRange(KeywordColorSystem.Colorize(gap));
                }

                result.Add(new ColoredText(sentence.Substring(match.Start, match.Length), match.Color));
                cursor = match.Start + match.Length;
            }

            if (cursor < sentence.Length)
                result.AddRange(KeywordColorSystem.Colorize(sentence.Substring(cursor)));

            return MergeAdjacentSameColor(result);
        }

        public static List<ColoredText> AppendSentence(
            List<ColoredText> paragraph,
            string sentence,
            CombatSequenceFlavorTokens tokens)
        {
            paragraph ??= new List<ColoredText>();
            if (string.IsNullOrWhiteSpace(sentence))
                return paragraph;

            if (paragraph.Count > 0
                && paragraph.Any(s => !string.IsNullOrWhiteSpace(s.Text)))
                paragraph.Add(new ColoredText(" ", Colors.White));

            paragraph.AddRange(ColorizeSentence(sentence.Trim(), tokens));
            return paragraph;
        }

        /// <summary>
        /// Identity tokens only — attacker/target/tempo names and the action name.
        /// Remaining prose falls through to sparse per-word keyword coloring (severity/status only).
        /// </summary>
        private static List<(string Phrase, Color Color)> BuildEmphasisPhrases(CombatSequenceFlavorTokens tokens)
        {
            var list = new List<(string Phrase, Color Color)>();

            void Add(string? phrase, Color color)
            {
                if (string.IsNullOrWhiteSpace(phrase))
                    return;
                // Articles stay white; only the name span is emphasized ("the Orc" → "Orc").
                string trimmed = CombatSequenceFlavorTokens.StripLeadingArticle(phrase.Trim());
                if (trimmed.Length < 2)
                    return;
                list.Add((trimmed, color));
            }

            Add(tokens.Attacker, ColorPalette.Gold.GetColor());
            Add(tokens.Target, ColorPalette.Enemy.GetColor());
            if (tokens.AdditionalNames != null)
            {
                foreach (var name in tokens.AdditionalNames)
                    Add(name, ColorPalette.Gold.GetColor());
            }
            // Skip synthetic HUD labels so "miss"/"hit" are not painted success-green across the paragraph.
            if (!tokens.IsMiss
                && !string.Equals(tokens.Action, "miss", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(tokens.Action, "hit", StringComparison.OrdinalIgnoreCase))
            {
                Add(tokens.Action, ColorPalette.Success.GetColor());
            }
            Add(tokens.Tempo, ColorPalette.Gold.GetColor());

            // Prefer longer phrases when scanning (e.g. two-word action names).
            return list
                .OrderByDescending(p => p.Phrase.Length)
                .ThenBy(p => p.Phrase, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<(int Start, int Length, Color Color)> FindNonOverlappingMatches(
            string sentence,
            List<(string Phrase, Color Color)> phrases)
        {
            var candidates = new List<(int Start, int Length, Color Color)>();
            foreach (var (phrase, color) in phrases)
            {
                int index = 0;
                while (index < sentence.Length)
                {
                    int found = sentence.IndexOf(phrase, index, StringComparison.OrdinalIgnoreCase);
                    if (found < 0)
                        break;
                    candidates.Add((found, phrase.Length, color));
                    index = found + phrase.Length;
                }
            }

            candidates.Sort((a, b) =>
            {
                int byStart = a.Start.CompareTo(b.Start);
                if (byStart != 0)
                    return byStart;
                return b.Length.CompareTo(a.Length);
            });

            var chosen = new List<(int Start, int Length, Color Color)>();
            int occupiedThrough = -1;
            foreach (var c in candidates)
            {
                if (c.Start < occupiedThrough)
                    continue;
                chosen.Add(c);
                occupiedThrough = c.Start + c.Length;
            }

            return chosen;
        }

        private static List<ColoredText> MergeAdjacentSameColor(List<ColoredText> segments)
        {
            if (segments.Count <= 1)
                return segments;

            var merged = new List<ColoredText>();
            var current = new ColoredText(segments[0].Text, segments[0].Color);
            for (int i = 1; i < segments.Count; i++)
            {
                var next = segments[i];
                if (next.Color == current.Color)
                {
                    current.Text += next.Text;
                }
                else
                {
                    merged.Add(current);
                    current = new ColoredText(next.Text, next.Color);
                }
            }

            merged.Add(current);
            return merged;
        }
    }
}
