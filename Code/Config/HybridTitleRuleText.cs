using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPGGame
{
    /// <summary>
    /// Parses / formats multiline hybrid title rules for the Classes settings panel.
    /// Duo: PrimaryPath,SecondaryPath,primaryBand,secondaryBand:title1|title2 (bands 0–4: 0 = pre–first threshold with ≥1 pt, 4 = top tier)
    /// Trio: Mace,Sword,Dagger:title1|title2  (paths sorted canonically on parse)
    /// Quad: QUAD:title1|title2  (or ALL:…)
    /// </summary>
    public static class HybridTitleRuleText
    {
        public static string FormatDuoRules(IReadOnlyList<HybridDuoTierRule> rules)
        {
            if (rules == null || rules.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var r in rules)
            {
                if (r.Titles == null || r.Titles.Length == 0) continue;
                string t = string.Join("|", r.Titles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
                if (t.Length == 0) continue;
                sb.Append(r.PrimaryPath).Append(',').Append(r.SecondaryPath).Append(',')
                    .Append(r.PrimaryTierBand).Append(',').Append(r.SecondaryTierBand).Append(':').Append(t)
                    .AppendLine();
            }
            return sb.ToString().TrimEnd();
        }

        public static List<HybridDuoTierRule> ParseDuoRules(string? text)
        {
            var list = new List<HybridDuoTierRule>();
            if (string.IsNullOrWhiteSpace(text)) return list;
            foreach (var rawLine in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                int colon = line.IndexOf(':');
                if (colon < 0) continue;
                string left = line[..colon].Trim();
                string right = line[(colon + 1)..].Trim();
                var parts = left.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 4) continue;
                if (!TryParseWeapon(parts[0], out var p) || !TryParseWeapon(parts[1], out var s)) continue;
                if (!int.TryParse(parts[2], out int bp) || !int.TryParse(parts[3], out int bs)) continue;
                var titles = right.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (titles.Length == 0) continue;
                list.Add(new HybridDuoTierRule
                {
                    PrimaryPath = p.ToString(),
                    SecondaryPath = s.ToString(),
                    PrimaryTierBand = bp,
                    SecondaryTierBand = bs,
                    Titles = titles
                });
            }
            return list;
        }

        public static string FormatTrioRules(IReadOnlyList<HybridPathComboRule> rules)
        {
            if (rules == null || rules.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var r in rules)
            {
                if (r.Paths == null || r.Paths.Length != 3 || r.Titles == null || r.Titles.Length == 0) continue;
                string key = string.Join(",", r.Paths.Select(x => x.Trim()).OrderBy(x => x, StringComparer.Ordinal));
                string t = string.Join("|", r.Titles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
                if (t.Length == 0) continue;
                sb.Append(key).Append(':').Append(t).AppendLine();
            }
            return sb.ToString().TrimEnd();
        }

        public static List<HybridPathComboRule> ParseTrioRules(string? text)
        {
            var list = new List<HybridPathComboRule>();
            if (string.IsNullOrWhiteSpace(text)) return list;
            foreach (var rawLine in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                int colon = line.IndexOf(':');
                if (colon < 0) continue;
                string left = line[..colon].Trim();
                string right = line[(colon + 1)..].Trim();
                var pathParts = left.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (pathParts.Length != 3) continue;
                var wts = new List<WeaponType>();
                foreach (var pp in pathParts)
                {
                    if (!TryParseWeapon(pp, out var w)) { wts.Clear(); break; }
                    wts.Add(w);
                }
                if (wts.Count != 3) continue;
                var titles = right.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (titles.Length == 0) continue;
                list.Add(new HybridPathComboRule
                {
                    Paths = wts.Select(x => x.ToString()).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                    Titles = titles
                });
            }
            return list;
        }

        public static string FormatQuadTitles(string[]? titles)
        {
            if (titles == null || titles.Length == 0) return "";
            string t = string.Join("|", titles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
            return t.Length == 0 ? "" : "QUAD:" + t;
        }

        public static string[] ParseQuadTitles(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
            string line = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "";
            if (line.Length == 0) return Array.Empty<string>();
            int colon = line.IndexOf(':');
            if (colon < 0) return Array.Empty<string>();
            string prefix = line[..colon].Trim();
            if (!prefix.Equals("QUAD", StringComparison.OrdinalIgnoreCase) && !prefix.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                return Array.Empty<string>();
            string right = line[(colon + 1)..].Trim();
            return right.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }

        public static bool TryParseWeapon(string name, out WeaponType wt)
        {
            wt = default;
            if (string.IsNullOrWhiteSpace(name)) return false;
            return Enum.TryParse(name.Trim(), true, out wt);
        }
    }

    public sealed class HybridDuoTierRule
    {
        public string PrimaryPath { get; set; } = "";
        public string SecondaryPath { get; set; } = "";
        public int PrimaryTierBand { get; set; }
        public int SecondaryTierBand { get; set; }
        public string[] Titles { get; set; } = Array.Empty<string>();
    }

    public sealed class HybridPathComboRule
    {
        /// <summary>Sorted weapon names, e.g. Dagger,Mace,Sword</summary>
        public string[] Paths { get; set; } = Array.Empty<string>();
        public string[] Titles { get; set; } = Array.Empty<string>();
    }
}
