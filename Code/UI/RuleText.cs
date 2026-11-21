using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace RPGGame.UI
{
    /// <summary>
    /// The text pipeline (Rebuild 0.4, D6.5): one place that turns any rule or rule template into one plain line of
    /// player text. Item modifiers, rule prefixes, skill nodes and the MCP screens all go through it, so template
    /// tokens like <c>+{8}%</c> or <c>{10-20}</c> can never leak onto a card.
    /// <list type="bullet">
    /// <item><c>{n}</c> → n × scale (a skill node's power × rank, or 1)</item>
    /// <item><c>{lo-hi}</c> → <c>lo–hi</c> (a catalog range that hasn't rolled yet)</item>
    /// <item>any other <c>{token}</c> → the token without braces</item>
    /// <item>line breaks and repeated spaces collapse to one line</item>
    /// </list>
    /// </summary>
    public static class RuleText
    {
        private static readonly Regex Number = new(@"\{(-?\d+(?:\.\d+)?)\}", RegexOptions.Compiled);
        private static readonly Regex Range = new(@"\{(-?\d+(?:\.\d+)?)\s*-\s*(-?\d+(?:\.\d+)?)\}", RegexOptions.Compiled);
        private static readonly Regex AnyBraces = new(@"\{([^{}]*)\}", RegexOptions.Compiled);
        private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);

        /// <summary>One plain line from authored text that may hold template tokens. Numbers scale by <paramref name="scale"/>.</summary>
        public static string Plain(string? text, double scale = 1.0)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            string s = Range.Replace(text, m =>
                $"{FormatNumber(Parse(m.Groups[1].Value) * scale)}–{FormatNumber(Parse(m.Groups[2].Value) * scale)}");
            s = Number.Replace(s, m => FormatNumber(Parse(m.Groups[1].Value) * scale));
            s = AnyBraces.Replace(s, m => m.Groups[1].Value.Trim());
            s = s.Replace("{", "").Replace("}", "");
            return Spaces.Replace(s, " ").Trim();
        }

        /// <summary>One plain line for a parsed rule.</summary>
        public static string Line(CrossWireRule? rule) => rule == null ? "" : Plain(CrossWires.Describe(rule));

        /// <summary>
        /// One line per rule in a rule source (<c>;</c>-separated CrossWires rules, as on a base modifier or a node).
        /// <c>{n}</c> becomes <paramref name="rolledValue"/> when given. A part that doesn't parse is shown as plain text.
        /// </summary>
        public static List<string> Lines(string? ruleSource, string sourceName = "", double? rolledValue = null)
        {
            var lines = new List<string>();
            if (string.IsNullOrWhiteSpace(ruleSource))
                return lines;
            string src = rolledValue.HasValue
                ? ruleSource.Replace("{n}", FormatNumber(rolledValue.Value), StringComparison.Ordinal)
                : ruleSource;
            foreach (string part in src.Split(';'))
            {
                string p = part.Trim();
                if (p.Length == 0)
                    continue;
                var rule = p.Contains('{') ? null : CrossWireRule.Parse(p, sourceName);
                lines.Add(rule != null ? Line(rule) : Plain(p));
            }
            return lines.Where(l => l.Length > 0).ToList();
        }

        /// <summary>A skill node's effect as one line: its authored text with tokens scaled by power, else its rules, else its payoff.</summary>
        public static string SkillNode(SkillTreeNodeDefinition? node)
        {
            if (node == null)
                return "";
            double power = node.Power > 0 ? node.Power : 1.0;
            if (!string.IsNullOrWhiteSpace(node.Effect))
                return Plain(node.Effect, power);
            if (!string.IsNullOrWhiteSpace(node.CrossWire))
                return string.Join("; ", Lines(node.CrossWire, node.Name));
            return Plain(node.Payoff);
        }

        private static readonly Regex CapsWord = new(@"\b[A-Z][A-Z'\-]{2,}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Item mod text in proper word case: an all-caps store name (a material mint such as <c>CRISIS</c> / <c>DRAG</c>,
        /// a status or a hero resource) reads as a word (<c>Crisis</c>, <c>Drag</c>). Ids in the data stay as they are;
        /// action names (an all-caps run of two or more words, e.g. <c>BARBARIAN RAGE</c>) and stat abbreviations
        /// (STR, HP, AMP) are left alone.
        /// </summary>
        public static string WordCase(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return text ?? "";
            return CapsWord.Replace(text, m =>
            {
                if (!Stores.TryGet(m.Value, out _)) // ids are case-insensitive
                    return m.Value;
                if (NextToCapsWord(text, m.Index, m.Length))
                    return m.Value;
                return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(m.Value.ToLowerInvariant());
            });
        }

        private static bool NextToCapsWord(string text, int index, int length)
        {
            static bool CapsAt(string s, int start, int step)
            {
                int i = start;
                int letters = 0;
                while (i >= 0 && i < s.Length && char.IsLetter(s[i]))
                {
                    if (!char.IsUpper(s[i]))
                        return false;
                    letters++;
                    i += step;
                }
                return letters >= 2;
            }
            int before = index - 1;
            if (before >= 1 && text[before] == ' ' && CapsAt(text, before - 1, -1))
                return true;
            int after = index + length;
            return after + 1 < text.Length && text[after] == ' ' && CapsAt(text, after + 1, 1);
        }

        /// <summary>One plain item-mod line: <see cref="Plain"/> then <see cref="WordCase"/>.</summary>
        public static string ItemLine(string? text) => WordCase(Plain(text));

        /// <summary>True when <paramref name="text"/> still holds a template brace (a leak).</summary>
        public static bool HasTemplateBraces(string? text) => !string.IsNullOrEmpty(text) && (text.Contains('{') || text.Contains('}'));

        internal static string FormatNumber(double value) =>
            Math.Round(value, Math.Abs(value) >= 10 ? 0 : Math.Abs(value) >= 1 ? 1 : 2)
                .ToString("0.##", CultureInfo.InvariantCulture);

        private static double Parse(string s) => double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
