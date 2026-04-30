using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>
    /// CLASS ACTIONS tab: compact rows (Class Level / Class / Action / optional Min pts) or sheet style
    /// TIER / CLASS / ACTIONS (tier name or number; ACTIONS = JSON array or comma-separated names).
    /// Tier column may use the fixed ladder <see cref="ClassActionsSheetTierWords"/> (Lesser…Abyssal) = class tiers 1–4.
    /// </summary>
    public static class ClassActionsSheetConverter
    {
        /// <summary>Official CLASS ACTIONS sheet tier labels: slot 1 = first named class tier through slot 4 = fourth.</summary>
        public static readonly string[] ClassActionsSheetTierWords = { "Lesser", "Blooded", "Dread", "Abyssal" };

        private static readonly JsonSerializerOptions JsonWrite = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly JsonSerializerOptions JsonReadRelaxed = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// <summary>Alternate labels for the same four class-action tiers (after <see cref="ClassActionsSheetTierWords"/>).</summary>
        private static readonly (string Word, int TierSlot)[] TierWordAliases =
        {
            ("fledgling", 1), ("recruit", 1), ("novice", 1),
            ("veteran", 2), ("adept", 2), ("expert", 2), ("scarred", 1),
            ("champion", 3), ("master", 3),
            ("paragon", 4), ("legend", 4),
            ("prophet", 1), ("godkiller", 2), ("worldbreaker", 3), ("eternal", 4)
        };

        public static ClassPresentationConfig LoadClassPresentationForImport()
        {
            try
            {
                string? path = GameConfiguration.TryGetExistingTuningConfigFilePath();
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return new ClassPresentationConfig().EnsureNormalized();

                string json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("classPresentation", out var cp))
                {
                    var pres = JsonSerializer.Deserialize<ClassPresentationConfig>(cp.GetRawText(), JsonReadRelaxed);
                    return (pres ?? new ClassPresentationConfig()).EnsureNormalized();
                }
            }
            catch
            {
                // ignore
            }

            return new ClassPresentationConfig().EnsureNormalized();
        }

        public static string CsvToClassActionsJsonText(string csvContent, ClassPresentationConfig? presentation = null)
        {
            var cfg = ParseCsvToConfig(csvContent, presentation ?? LoadClassPresentationForImport());
            return JsonSerializer.Serialize(cfg, JsonWrite);
        }

        public static ClassActionsUnlockConfig ParseCsvToConfig(string csvContent, ClassPresentationConfig? presentation = null)
        {
            presentation ??= LoadClassPresentationForImport();
            presentation = presentation.EnsureNormalized();

            var rows = SimpleGameDataCsvParser.ParseToRows(csvContent);
            var rules = new List<ClassActionUnlockRule>();
            if (rows.Count == 0)
                return new ClassActionsUnlockConfig { Rules = rules }.Normalize();

            if (!TryFindHeaderRow(rows, out int headerRow, out int iTier, out int iClass, out int iAction, out int iMin))
                return new ClassActionsUnlockConfig { Rules = rules }.Normalize();

            for (int r = headerRow + 1; r < rows.Count; r++)
            {
                var cells = rows[r];
                if (cells.Length <= Math.Max(Math.Max(iTier, iClass), iAction))
                    continue;

                string tierCell = cells[iTier]?.Trim() ?? "";
                string classCell = cells[iClass]?.Trim() ?? "";
                string actionCell = cells[iAction]?.Trim() ?? "";
                if (string.IsNullOrEmpty(classCell))
                    continue;

                int? tierSlot = MapTierCellToSlot(tierCell, presentation);
                if (!tierSlot.HasValue)
                    continue;

                int? minPts = null;
                if (iMin >= 0 && iMin < cells.Length)
                {
                    string ms = cells[iMin]?.Trim() ?? "";
                    if (int.TryParse(ms, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mp) && mp > 0)
                        minPts = mp;
                }

                var actionNames = ParseActionNamesFromCell(actionCell);
                if (actionNames.Count == 0)
                    continue;

                foreach (string classKey in ExpandClassColumnToClassKeys(classCell, presentation))
                {
                    foreach (string actionName in actionNames)
                    {
                        if (string.IsNullOrWhiteSpace(actionName))
                            continue;
                        rules.Add(new ClassActionUnlockRule
                        {
                            Tier = tierSlot.Value,
                            ClassKey = classKey,
                            ActionName = actionName.Trim(),
                            MinClassPoints = minPts
                        });
                    }
                }
            }

            return new ClassActionsUnlockConfig { Rules = rules }.Normalize();
        }

        private static bool TryFindHeaderRow(
            List<string[]> rows,
            out int headerRow,
            out int iTier,
            out int iClass,
            out int iAction,
            out int iMin)
        {
            headerRow = -1;
            iTier = iClass = iAction = iMin = -1;

            for (int r = 0; r < Math.Min(rows.Count, 20); r++)
            {
                var cells = rows[r];
                if (cells.Length < 3)
                    continue;

                var lowered = cells.Select(c => (c?.Trim() ?? "").ToLowerInvariant()).ToArray();
                int t = -1, c = -1, a = -1, m = -1;

                for (int col = 0; col < lowered.Length; col++)
                {
                    string h = lowered[col];
                    if (h.Contains("class level", StringComparison.Ordinal)
                        || string.Equals(h, "tier", StringComparison.Ordinal)
                        || (h.Contains("tier", StringComparison.Ordinal) && !h.Contains("action", StringComparison.Ordinal)))
                        t = col;
                }

                for (int col = 0; col < lowered.Length; col++)
                {
                    string h = lowered[col];
                    if (h.Contains("action", StringComparison.Ordinal))
                        a = col;
                    else if (h.Contains("min", StringComparison.Ordinal) && (h.Contains("point", StringComparison.Ordinal) || h.Contains("pts", StringComparison.Ordinal)))
                        m = col;
                    else if (h.Contains("class", StringComparison.Ordinal) && !h.Contains("level", StringComparison.Ordinal))
                        c = col;
                }

                if (t >= 0 && c >= 0 && a >= 0)
                {
                    headerRow = r;
                    iTier = t;
                    iClass = c;
                    iAction = a;
                    iMin = m;
                    return true;
                }
            }

            return false;
        }

        private static int? MapTierCellToSlot(string tierCell, ClassPresentationConfig presentation)
        {
            if (string.IsNullOrWhiteSpace(tierCell))
                return 0;

            string s = tierCell.Trim();
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                return Math.Clamp(n, 0, ClassPresentationConfig.TierSlotCount);

            // CLASS ACTIONS sheet standard ladder (always 1–4) — must win over TuningConfig words
            // so e.g. "Blooded" is tier 2 here even if solo/duo prefixes use a different order in JSON.
            for (int i = 0; i < ClassActionsSheetTierWords.Length; i++)
            {
                if (s.Equals(ClassActionsSheetTierWords[i], StringComparison.OrdinalIgnoreCase))
                    return i + 1;
            }

            foreach (var (word, tier) in TierWordAliases)
            {
                if (s.Equals(word, StringComparison.OrdinalIgnoreCase))
                    return tier;
            }

            var pres = presentation.EnsureNormalized();
            for (int i = 0; i < ClassPresentationConfig.TierSlotCount; i++)
            {
                string solo = pres.AttributeSoloTrioTierPrefixes[i]?.Trim() ?? "";
                if (solo.Length > 0 && s.Equals(solo, StringComparison.OrdinalIgnoreCase))
                    return i + 1;
                string rank = ClassPresentationConfig.DefaultWeaponPathRankTierWords[i];
                if (s.Equals(rank, StringComparison.OrdinalIgnoreCase))
                    return i + 1;
                string quad = pres.AttributeQuadTierNames[i]?.Trim() ?? "";
                if (quad.Length > 0 && s.Equals(quad, StringComparison.OrdinalIgnoreCase))
                    return i + 1;
            }

            return null;
        }

        private static List<string> ParseActionNamesFromCell(string cell)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(cell))
                return list;

            string t = cell.Trim();
            if (t.StartsWith("[", StringComparison.Ordinal))
            {
                try
                {
                    var arr = JsonSerializer.Deserialize<List<string>>(t, JsonReadRelaxed);
                    if (arr != null)
                    {
                        foreach (var x in arr)
                        {
                            if (!string.IsNullOrWhiteSpace(x))
                                list.Add(x.Trim());
                        }
                    }
                }
                catch
                {
                    // fall through to split
                }

                if (list.Count > 0)
                    return list;
            }

            foreach (var part in t.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string p = part.Trim();
                if (p.Length > 0)
                    list.Add(p);
            }

            return list;
        }

        private static IEnumerable<string> ExpandClassColumnToClassKeys(string classCell, ClassPresentationConfig p)
        {
            string c = classCell.Trim();
            if (string.IsNullOrEmpty(c))
                yield break;

            p = p.EnsureNormalized();
            if (!string.IsNullOrWhiteSpace(p.DefaultNoPointsClassName)
                && c.Equals(p.DefaultNoPointsClassName.Trim(), StringComparison.OrdinalIgnoreCase))
                yield break;

            if (TryDuoKeys(c, p.AttributeDuoMaceSword)) { yield return "Mace"; yield return "Sword"; yield break; }
            if (TryDuoKeys(c, p.AttributeDuoMaceDagger)) { yield return "Mace"; yield return "Dagger"; yield break; }
            if (TryDuoKeys(c, p.AttributeDuoMaceWand)) { yield return "Mace"; yield return "Wand"; yield break; }
            if (TryDuoKeys(c, p.AttributeDuoSwordDagger)) { yield return "Sword"; yield return "Dagger"; yield break; }
            if (TryDuoKeys(c, p.AttributeDuoSwordWand)) { yield return "Sword"; yield return "Wand"; yield break; }
            if (TryDuoKeys(c, p.AttributeDuoDaggerWand)) { yield return "Dagger"; yield return "Wand"; yield break; }

            yield return c;
        }

        private static bool TryDuoKeys(string cell, string? duoLabel) =>
            !string.IsNullOrWhiteSpace(duoLabel) && cell.Equals(duoLabel.Trim(), StringComparison.OrdinalIgnoreCase);

        public static List<IList<object>> BuildPushValueRows(ClassActionsUnlockConfig? cfg)
        {
            cfg ??= new ClassActionsUnlockConfig();
            cfg.Normalize();
            var rows = new List<IList<object>>
            {
                new List<object> { "Class Level", "Class", "Action", "Min class pts" }
            };
            foreach (var r in cfg.Rules)
            {
                rows.Add(new List<object>
                {
                    r.Tier.ToString(CultureInfo.InvariantCulture),
                    r.ClassKey,
                    r.ActionName,
                    r.MinClassPoints.HasValue
                        ? r.MinClassPoints.Value.ToString(CultureInfo.InvariantCulture)
                        : ""
                });
            }
            return rows;
        }
    }
}
