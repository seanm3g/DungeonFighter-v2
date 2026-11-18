using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// The item card's Modifier block (2026-11-17): each base modifier rule as <c>trigger → effect</c> in one keyword
    /// notation, plus dim "what this means for you" lines. Notation: ACTIONS in caps (white), <c>[tags]</c> in brackets
    /// (cyan), STORES in caps (orange), statuses lowercase (pink), skills (gold), numbers (green), prose in gray.
    /// </summary>
    public static class ModifierTooltipFormatter
    {
        public static Color Prose => ColorPalette.LightGray.GetColor();
        public static Color Dim => ColorPalette.Gray.GetColor();
        public static Color Arrow => ColorPalette.DarkGray.GetColor();
        public static Color ActionColor => Colors.White;
        public static Color TagColor => ColorPalette.Cyan.GetColor();
        public static Color StoreColor => ColorPalette.Orange.GetColor();
        public static Color StatusColor => ColorPalette.Pink.GetColor();
        public static Color SkillColor => ColorPalette.Gold.GetColor();
        public static Color NumberColor => ColorPalette.Success.GetColor();
        public static Color WarnColor => ColorPalette.Warning.GetColor();

        /// <summary>The parsed rules of <paramref name="item"/>'s base modifier (empty when it has none).</summary>
        public static List<CrossWireRule> Rules(Item? item)
        {
            var list = new List<CrossWireRule>();
            if (item == null || string.IsNullOrWhiteSpace(item.ImplicitRule))
                return list;
            foreach (string part in item.ImplicitRule.Split(';'))
            {
                if (string.IsNullOrWhiteSpace(part))
                    continue;
                var rule = CrossWireRule.Parse(part.Trim(), item.Name ?? "");
                if (rule != null)
                    list.Add(rule);
            }
            return list;
        }

        /// <summary>Card lines for the modifier: one styled line per rule, each followed by its payoff lines for
        /// <paramref name="hero"/> (none without a hero).</summary>
        public static List<List<ColoredText>> BuildLines(Item? item, Character? hero)
        {
            var lines = new List<List<ColoredText>>();
            foreach (var rule in Rules(item))
            {
                var line = new List<ColoredText> { new("  ", Prose) };
                line.AddRange(StyleRule(rule));
                lines.Add(line);
                foreach (var payoff in PayoffLines(rule, item, hero))
                {
                    var p = new List<ColoredText> { new("    ", Dim) };
                    p.AddRange(payoff);
                    lines.Add(p);
                }
            }
            return lines;
        }

        /// <summary>One rule as <c>lead → effect</c> (no arrow when the rule has no lead), keywords styled.</summary>
        public static List<ColoredText> StyleRule(CrossWireRule rule)
        {
            string text = RPGGame.UI.RuleText.Line(rule);
            var (lead, effect) = Split(rule, text);
            var tokens = Tokens(rule);
            var segs = new List<ColoredText>();
            if (lead != null)
            {
                segs.AddRange(Style(lead, tokens, Prose));
                segs.Add(new ColoredText("  →  ", Arrow));
            }
            segs.AddRange(Style(effect, tokens, Prose));
            return segs;
        }

        /// <summary>Splits the card text into the trigger / subject and the effect.</summary>
        internal static (string? Lead, string Effect) Split(CrossWireRule rule, string text)
        {
            if (rule.Verb is "TAGAS" or "COUNTAS")
            {
                int i = text.IndexOf(" also count", StringComparison.Ordinal);
                if (i > 0)
                    return (text.Substring(0, i), text.Substring(i + 1));
            }
            int colon = text.IndexOf(": ", StringComparison.Ordinal);
            if (colon > 0)
            {
                string effect = text.Substring(colon + 2);
                if (effect.Length > 0)
                    effect = char.ToUpperInvariant(effect[0]) + effect.Substring(1);
                return (text.Substring(0, colon), effect);
            }
            return (null, text);
        }

        internal enum Kind { Action, Tag, Store, Status, Skill }

        /// <summary>The keywords this rule names, by kind.</summary>
        private static List<(string Word, Kind Kind)> Tokens(CrossWireRule rule)
        {
            var t = new List<(string, Kind)>();
            void Add(string? w, Kind k)
            {
                if (!string.IsNullOrWhiteSpace(w))
                    t.Add((w.Trim(), k));
            }
            foreach (var c in rule.Conditions)
            {
                switch (c.Subject)
                {
                    case "action": Add(c.Key, Kind.Action); break;
                    case "tag" or "class" or "prevtag" or "nexttag": Add(c.Key, Kind.Tag); break;
                    case "target" when c.Key != "HP": Add(c.Key, Kind.Status); break;
                    case "self" when IsStoreKey(c.Key): Add(c.Key, Kind.Store); break;
                }
            }
            int colon = rule.When.IndexOf(':');
            if (colon > 0)
            {
                string arg = rule.When.Substring(colon + 1);
                Add(arg, rule.When.StartsWith("ONSTATUSAPPLY", StringComparison.OrdinalIgnoreCase) ? Kind.Status : Kind.Store);
            }
            switch (rule.Verb)
            {
                case "TAGAS": Add(rule.Store, Kind.Tag); break;
                case "MINT": Add(rule.Store, Kind.Store); break;
                case "APPLY" or "ADD" or "POTENCY": Add(rule.Store, Kind.Status); break;
                case "RANK":
                    var node = SkillTreeService.Trees?.GetNode(rule.Store);
                    Add(string.IsNullOrWhiteSpace(node?.Name) ? rule.Store : node!.Name, Kind.Skill);
                    break;
            }
            Add(rule.IfTag, Kind.Tag);
            if (rule.Per?.Kind == "tags") Add(rule.Per.Arg, Kind.Tag);
            if (rule.Per?.Kind == "target") Add(rule.Per.Arg, Kind.Status);
            Add(rule.PerTargetStatus, Kind.Status);
            return t;
        }

        private static readonly HashSet<string> SelfKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "HP", "STEP", "SLOT", "SLOTS", "EXTRASLOTS", "SPENT", "LENGTH", "ROLL", "DISTINCT", "TURN", "HITSTREAK",
            "STR", "AGI", "TEC", "INT"
        };

        private static bool IsStoreKey(string key) => !SelfKeys.Contains(key);

        private static readonly Regex Number = new(@"(?<![\w\[])[+\-−]?\d+(?:\.\d+)?%?", RegexOptions.Compiled);

        /// <summary>Colors <paramref name="text"/>: rule keywords by kind (tags shown as <c>[tag]</c>), numbers green, the
        /// rest in <paramref name="prose"/>.</summary>
        internal static List<ColoredText> Style(string text, List<(string Word, Kind Kind)> tokens, Color prose)
        {
            var spans = new List<(int Start, int Length, string Display, Color Color)>();
            // Longest words first so "CRIT HEAL" wins over "HEAL".
            foreach (var (word, kind) in tokens.OrderByDescending(x => x.Word.Length))
            {
                string pattern = kind == Kind.Tag
                    ? $@"(?<![\w\[]){Regex.Escape(word)}(?=\]?\s+actions?\b)"
                    : $@"(?<!\w){Regex.Escape(word)}(?!\w)";
                foreach (Match m in Regex.Matches(text, pattern, RegexOptions.IgnoreCase))
                {
                    if (spans.Any(s => m.Index < s.Start + s.Length && s.Start < m.Index + m.Length))
                        continue;
                    string display = kind switch
                    {
                        Kind.Tag => $"[{word.ToLowerInvariant()}]",
                        Kind.Action or Kind.Store => m.Value.ToUpperInvariant(),
                        Kind.Status => m.Value.ToLowerInvariant(),
                        _ => m.Value
                    };
                    spans.Add((m.Index, m.Length, display, ColorFor(kind)));
                }
            }
            foreach (Match m in Number.Matches(text))
            {
                if (!spans.Any(s => m.Index < s.Start + s.Length && s.Start < m.Index + m.Length))
                    spans.Add((m.Index, m.Length, m.Value, NumberColor));
            }
            var result = new List<ColoredText>();
            int pos = 0;
            foreach (var s in spans.OrderBy(s => s.Start))
            {
                if (s.Start > pos)
                    result.Add(new ColoredText(text.Substring(pos, s.Start - pos), prose));
                result.Add(new ColoredText(s.Display, s.Color));
                pos = s.Start + s.Length;
            }
            if (pos < text.Length)
                result.Add(new ColoredText(text.Substring(pos), prose));
            return result;
        }

        private static Color ColorFor(Kind kind) => kind switch
        {
            Kind.Action => ActionColor,
            Kind.Tag => TagColor,
            Kind.Store => StoreColor,
            Kind.Status => StatusColor,
            _ => SkillColor
        };

        // ------------------------------------------------------------------ payoff

        /// <summary>
        /// What the rule means for <paramref name="hero"/> right now: for a tag it grants, which worn pieces and learned
        /// skills read that tag; for a named action, whether it is in the strip; for a skill rank, whether the skill is
        /// learned. Empty without a hero.
        /// </summary>
        public static List<List<ColoredText>> PayoffLines(CrossWireRule rule, Item? item, Character? hero)
        {
            var lines = new List<List<ColoredText>>();
            if (hero == null || hero is Enemy)
                return lines;

            if (rule.Verb == "TAGAS" && !string.IsNullOrWhiteSpace(rule.Store))
            {
                string tag = rule.Store.ToLowerInvariant();
                var readers = TagReaders(hero, tag, item);
                var b = new ColoredTextBuilder();
                if (readers.Count == 0)
                {
                    b.Add("Nothing you wear or have learned reads ", Dim).Add($"[{tag}]", TagColor).Add(" yet", Dim);
                }
                else
                {
                    b.Add($"[{tag}]", TagColor).Add(" is read by ", Dim);
                    for (int i = 0; i < readers.Count; i++)
                    {
                        if (i > 0)
                            b.Add(", ", Dim);
                        b.Add(readers[i], Colors.White);
                    }
                }
                lines.Add(b.Build());
            }

            foreach (var c in rule.Conditions.Where(c => c.Subject == "action"))
            {
                string name = c.Key.Trim();
                bool inStrip = (hero.GetComboActions() ?? new List<Action>())
                    .Any(a => string.Equals(a?.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase));
                var b = new ColoredTextBuilder().Add(name.ToUpperInvariant(), ActionColor);
                if (inStrip)
                    b.Add(" is in your strip", Dim);
                else
                    b.Add(" is not in your strip", WarnColor);
                lines.Add(b.Build());
            }

            if (rule.Verb == "RANK" && !string.IsNullOrWhiteSpace(rule.Store))
            {
                var node = SkillTreeService.Trees?.GetNode(rule.Store);
                string name = string.IsNullOrWhiteSpace(node?.Name) ? rule.Store : node!.Name;
                int rank = hero.Progression.LearnedSkillRanks.TryGetValue(rule.Store, out int r) ? Math.Max(0, r) : 0;
                var b = new ColoredTextBuilder().Add(name, SkillColor);
                if (rank <= 0)
                    b.Add(" not learned: no effect until you learn it", WarnColor);
                else
                    b.Add($" rank {rank} → {rank + (int)Math.Round(rule.Amount)}", Dim);
                lines.Add(b.Build());
            }
            return lines;
        }

        /// <summary>Distinct sources (worn pieces, learned skills) with an active rule that reads <paramref name="tag"/>,
        /// leaving out <paramref name="self"/>'s own modifier.</summary>
        public static List<string> TagReaders(Character hero, string tag, Item? self)
        {
            var own = new HashSet<string>(Rules(self).Select(r => r.Text), StringComparer.OrdinalIgnoreCase);
            string selfName = self?.Name ?? "";
            var names = new List<string>();
            foreach (var rule in CrossWires.Active(hero))
            {
                if (string.Equals(rule.Source, selfName, StringComparison.Ordinal) && own.Contains(rule.Text))
                    continue;
                if (!ReadsTag(rule, tag))
                    continue;
                if (!string.IsNullOrWhiteSpace(rule.Source) && !names.Contains(rule.Source))
                    names.Add(rule.Source);
            }
            return names;
        }

        /// <summary>True when the rule's condition, PER count or NEXT filter looks at <paramref name="tag"/>.</summary>
        public static bool ReadsTag(CrossWireRule rule, string tag)
        {
            bool Eq(string? s) => string.Equals(s?.Trim(), tag, StringComparison.OrdinalIgnoreCase);
            return rule.Conditions.Any(c => c.Subject is "tag" or "class" or "prevtag" or "nexttag" && Eq(c.Key))
                || (rule.Per?.Kind == "tags" && Eq(rule.Per.Arg))
                || Eq(rule.IfTag);
        }
    }
}
