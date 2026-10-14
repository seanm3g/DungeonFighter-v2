using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers.Helpers
{
    /// <summary>Whether a comparison row favors the new item, the worn one, or neither.</summary>
    public enum ItemComparisonVerdict
    {
        Same,
        Better,
        Worse,
        Unknown
    }

    /// <summary>One stat compared across the selected item and the worn one, totals summed across all sources.</summary>
    public readonly struct ItemComparisonRow
    {
        public ItemComparisonRow(string label, string newText, string wornText, string changeText, ItemComparisonVerdict verdict)
        {
            Label = label;
            NewText = newText;
            WornText = wornText;
            ChangeText = changeText;
            Verdict = verdict;
        }

        public string Label { get; }
        public string NewText { get; }
        public string WornText { get; }
        public string ChangeText { get; }
        public ItemComparisonVerdict Verdict { get; }
    }

    /// <summary>
    /// Side-by-side stat totals for "selected item vs. item worn in that slot": every contribution with the same
    /// label and unit (base stat, affix, suffix) is summed so each stat appears once with a single change value.
    /// </summary>
    public static class ItemComparisonTable
    {
        public const string Missing = "—";

        public static List<ItemComparisonRow> Build(
            IReadOnlyList<ItemStatContribution> selected,
            IReadOnlyList<ItemStatContribution> worn)
        {
            var newTotals = Aggregate(selected);
            var wornTotals = Aggregate(worn);

            var keys = newTotals.Keys.ToList();
            foreach (var k in wornTotals.Keys)
            {
                if (!keys.Contains(k))
                    keys.Add(k);
            }

            var rows = new List<ItemComparisonRow>();
            foreach (var key in keys)
            {
                newTotals.TryGetValue(key, out var a);
                wornTotals.TryGetValue(key, out var b);
                rows.Add(BuildRow(key, a, b));
            }

            return rows;
        }

        /// <summary>Actions the selected item adds and the worn item's actions that would be lost by swapping.</summary>
        public static (List<string> Gained, List<string> Lost) DiffActions(IEnumerable<string>? selected, IEnumerable<string>? worn)
        {
            var a = (selected ?? Enumerable.Empty<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var b = (worn ?? Enumerable.Empty<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var gained = a.Where(x => !b.Contains(x, StringComparer.OrdinalIgnoreCase)).ToList();
            var lost = b.Where(x => !a.Contains(x, StringComparer.OrdinalIgnoreCase)).ToList();
            return (gained, lost);
        }

        private sealed class Total
        {
            public double? Number;
            public string? Text;
            public bool AnyDebuff;
        }

        private readonly record struct RowKey(string Label, string Unit);

        private static Dictionary<RowKey, Total> Aggregate(IReadOnlyList<ItemStatContribution> contributions)
        {
            var map = new Dictionary<RowKey, Total>();
            if (contributions == null)
                return map;

            foreach (var c in contributions)
            {
                if (TryParseValue(c.ValueText, out double n, out string unit))
                {
                    var key = new RowKey(c.Label, unit);
                    if (!map.TryGetValue(key, out var t))
                        map[key] = t = new Total();
                    t.Number = (t.Number ?? 0) + n;
                    t.AnyDebuff |= c.IsDebuff;
                }
                else
                {
                    var key = new RowKey(c.Label, "\u0000" + c.ValueText);
                    if (!map.ContainsKey(key))
                        map[key] = new Total { Text = c.ValueText, AnyDebuff = c.IsDebuff };
                }
            }

            return map;
        }

        private static ItemComparisonRow BuildRow(RowKey key, Total? a, Total? b)
        {
            bool textRow = key.Unit.StartsWith("\u0000", StringComparison.Ordinal);
            if (textRow)
            {
                string text = key.Unit.Substring(1);
                string label = $"{key.Label} ({text})";
                bool inNew = a != null;
                bool inWorn = b != null;
                var verdict = inNew == inWorn ? ItemComparisonVerdict.Same
                    : inNew ? ((a!.AnyDebuff) ? ItemComparisonVerdict.Worse : ItemComparisonVerdict.Better)
                    : ((b!.AnyDebuff) ? ItemComparisonVerdict.Better : ItemComparisonVerdict.Worse);
                return new ItemComparisonRow(label, inNew ? "yes" : Missing, inWorn ? "yes" : Missing,
                    verdict == ItemComparisonVerdict.Same ? "=" : (inNew ? "gain" : "lose"), verdict);
            }

            string unitSuffix = IsShortUnit(key.Unit) ? key.Unit : (key.Unit.StartsWith("%", StringComparison.Ordinal) ? "%" : "");
            string rowLabel = IsShortUnit(key.Unit) ? key.Label : $"{key.Label} ({key.Unit.TrimStart('%').Trim()})";

            double newVal = a?.Number ?? 0;
            double wornVal = b?.Number ?? 0;
            string newText = a == null ? Missing : FormatNumber(newVal, unitSuffix, signed: IsSignedStat(key));
            string wornText = b == null ? Missing : FormatNumber(wornVal, unitSuffix, signed: IsSignedStat(key));

            double delta = newVal - wornVal;
            if (Math.Abs(delta) < 1e-9)
                return new ItemComparisonRow(rowLabel, newText, wornText, "=", ItemComparisonVerdict.Same);

            bool lowerIsBetter = IsLowerBetter(key);
            bool better = lowerIsBetter ? delta < 0 : delta > 0;
            return new ItemComparisonRow(rowLabel, newText, wornText, FormatNumber(delta, unitSuffix, signed: true),
                better ? ItemComparisonVerdict.Better : ItemComparisonVerdict.Worse);
        }

        private static bool IsShortUnit(string unit) => unit is "" or "%" or "s" or "×";

        /// <summary>Weapon damage and attack speed/time are absolute values, not bonuses.</summary>
        private static bool IsSignedStat(RowKey key) =>
            !(key.Label is "Damage" && key.Unit == "") && key.Unit != "×" && key.Unit != "s";

        private static bool IsLowerBetter(RowKey key)
        {
            if (key.Unit.Contains("slower", StringComparison.OrdinalIgnoreCase))
                return true;
            return (key.Label is "Attack speed" or "Attack time") && (key.Unit is "×" or "s");
        }

        private static string FormatNumber(double value, string unit, bool signed)
        {
            string body = Math.Abs(value).ToString("0.##", CultureInfo.InvariantCulture);
            string sign = value < 0 ? "-" : (signed && value > 0 ? "+" : "");
            return sign + body + unit;
        }

        /// <summary>Leading signed number ("+4", "−10%", "0.81×", "1.20s", "5% faster") and whatever follows it.</summary>
        internal static bool TryParseValue(string? text, out double value, out string unit)
        {
            value = 0;
            unit = "";
            string s = (text ?? "").Trim();
            if (s.Length == 0)
                return false;

            int i = 0;
            int sign = 1;
            if (s[0] == '+')
                i++;
            else if (s[0] == '-' || s[0] == '−')
            {
                sign = -1;
                i++;
            }

            int start = i;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.'))
                i++;
            if (i == start || !double.TryParse(s.AsSpan(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double n))
                return false;

            value = sign * n;
            unit = s.Substring(i).Trim();
            return true;
        }
    }
}
