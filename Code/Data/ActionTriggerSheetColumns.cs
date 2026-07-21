using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace RPGGame.Data
{
    /// <summary>
    /// ACTIONS sheet TRIGGERS band: each family is a 3-column triple
    /// (count label, <c>… SCOPE</c>, <c>… →</c> mechanic pointers).
    /// </summary>
    public static class ActionTriggerSheetColumns
    {
        public const string ContextBand = "TRIGGERS";

        public readonly record struct Family(
            string WhenToken,
            string CountLabel,
            string ScopeLabel,
            string MechanicsLabel);

        /// <summary>Families pushed/ensured as column triples under <see cref="ContextBand"/>.</summary>
        public static readonly Family[] Families =
        {
            new("ONHIT", "ON HIT", "ON HIT SCOPE", "ON HIT →"),
            new("ONMISS", "ON MISS", "ON MISS SCOPE", "ON MISS →"),
            new("ONCRITICAL", "ON CRIT", "ON CRIT SCOPE", "ON CRIT →"),
            new("ONKILL", "ON KILL", "ON KILL SCOPE", "ON KILL →"),
            new("ONCONNECT", "ON CONNECT", "ON CONNECT SCOPE", "ON CONNECT →"),
            new("ONCOMBO", "ON COMBO", "ON COMBO SCOPE", "ON COMBO →"),
            new("ONCOMBOEND", "ON COMBO END", "ON COMBO END SCOPE", "ON COMBO END →"),
            new("ONCRITICALMISS", "ON CRIT MISS", "ON CRIT MISS SCOPE", "ON CRIT MISS →"),
            new("ONFIRSTHIT", "ON FIRST HIT", "ON FIRST HIT SCOPE", "ON FIRST HIT →"),
            new("ONAFTERMISS", "ON AFTER MISS", "ON AFTER MISS SCOPE", "ON AFTER MISS →"),
            new("ONROOMSCLEARED", "ON ROOMS CLEARED", "ON ROOMS CLEARED SCOPE", "ON ROOMS CLEARED →"),
            new("ONROLLVALUE", "ON ROLL VALUE", "ON ROLL VALUE SCOPE", "ON ROLL VALUE →"),
            new("ONNATURALROLL", "ON NATURAL ROLL", "ON NATURAL ROLL SCOPE", "ON NATURAL ROLL →"),
        };

        /// <summary>
        /// Appends any missing trigger triple columns (context = TRIGGERS). Returns a new header and whether columns were added.
        /// </summary>
        public static (SpreadsheetHeader Header, bool ColumnsAdded) EnsureHeader(SpreadsheetHeader header)
        {
            if (header == null)
                throw new ArgumentNullException(nameof(header));

            var contexts = header.ContextByIndex.ToList();
            var labels = header.LabelByIndex.ToList();
            bool added = false;

            while (contexts.Count < labels.Count)
                contexts.Add("");
            while (labels.Count < contexts.Count)
                labels.Add("");

            foreach (var family in Families)
            {
                if (FindColumn(contexts, labels, family.CountLabel) < 0)
                {
                    Append(contexts, labels, family.CountLabel);
                    added = true;
                }

                if (FindColumn(contexts, labels, family.ScopeLabel) < 0)
                {
                    Append(contexts, labels, family.ScopeLabel);
                    added = true;
                }

                if (FindColumn(contexts, labels, family.MechanicsLabel) < 0)
                {
                    Append(contexts, labels, family.MechanicsLabel);
                    added = true;
                }
            }

            if (!added)
                return (header, false);

            var newHeader = new SpreadsheetHeader(
                contexts,
                labels,
                header.LabelRowIndex,
                header.DataStartRowIndex);
            return (newHeader, true);
        }

        /// <summary>Reads all trigger triples from a CSV/API row into <see cref="SpreadsheetActionData.TriggerBundlesJson"/> and legacy count cells.</summary>
        public static void ReadFromRow(SpreadsheetActionData data, string[] columns, SpreadsheetHeader header)
        {
            if (data == null || columns == null || header == null)
                return;

            var bundles = new List<ActionTriggerBundle>();

            foreach (var family in Families)
            {
                string count = FirstNonEmpty(
                    header.GetValue(columns, ContextBand, family.CountLabel, allowUnscopedLabelFallback: false),
                    header.GetValue(columns, null, family.CountLabel));
                string scope = FirstNonEmpty(
                    header.GetValue(columns, ContextBand, family.ScopeLabel, allowUnscopedLabelFallback: false),
                    header.GetValue(columns, null, family.ScopeLabel));
                string mechanics = FirstNonEmpty(
                    header.GetValue(columns, ContextBand, family.MechanicsLabel, allowUnscopedLabelFallback: false),
                    header.GetValue(columns, null, family.MechanicsLabel));

                ApplyLegacyCountFallback(data, family.WhenToken, ref count);

                if (string.IsNullOrWhiteSpace(count)
                    && string.IsNullOrWhiteSpace(scope)
                    && string.IsNullOrWhiteSpace(mechanics))
                    continue;

                if (string.IsNullOrWhiteSpace(count) && !string.IsNullOrWhiteSpace(mechanics))
                    count = "1";

                var bundle = new ActionTriggerBundle
                {
                    When = family.WhenToken,
                    Count = count?.Trim() ?? "",
                    Scope = NormalizeScope(scope),
                    Mechanics = mechanics?.Trim() ?? ""
                };

                if (!bundle.IsEnabled && string.IsNullOrWhiteSpace(bundle.Mechanics))
                    continue;

                bundles.Add(bundle);
                SyncLegacyCountField(data, family.WhenToken, bundle.Count);
            }

            if (bundles.Count > 0)
                data.TriggerBundlesJson = JsonSerializer.Serialize(bundles);
        }

        /// <summary>Writes trigger triples into a push row (requires header columns from <see cref="EnsureHeader"/>).</summary>
        public static void WriteToRow(SpreadsheetHeader header, string[] row, SpreadsheetActionData data)
        {
            if (header == null || row == null || data == null)
                return;

            var byWhen = LoadBundles(data)
                .ToDictionary(b => SpreadsheetHeader.NormalizeLabel(b.When), StringComparer.OrdinalIgnoreCase);

            foreach (var family in Families)
            {
                string count = "";
                string scope = "";
                string mechanics = "";

                if (byWhen.TryGetValue(SpreadsheetHeader.NormalizeLabel(family.WhenToken), out var bundle))
                {
                    count = bundle.Count ?? "";
                    scope = bundle.Scope ?? "";
                    mechanics = bundle.Mechanics ?? "";
                }
                else
                {
                    count = GetLegacyCount(data, family.WhenToken);
                }

                header.SetCell(row, ContextBand, family.CountLabel, count, allowUnscopedLabelFallback: true);
                header.SetCell(row, ContextBand, family.ScopeLabel, scope, allowUnscopedLabelFallback: true);
                header.SetCell(row, ContextBand, family.MechanicsLabel, mechanics, allowUnscopedLabelFallback: true);
            }
        }

        public static List<ActionTriggerBundle> LoadBundles(SpreadsheetActionData data)
        {
            var list = new List<ActionTriggerBundle>();
            if (data == null)
                return list;

            if (!string.IsNullOrWhiteSpace(data.TriggerBundlesJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<List<ActionTriggerBundle>>(data.TriggerBundlesJson);
                    if (parsed != null)
                    {
                        foreach (var b in parsed)
                        {
                            if (b == null || string.IsNullOrWhiteSpace(b.When))
                                continue;
                            b.When = CanonicalWhen(b.When);
                            b.Scope = NormalizeScope(b.Scope);
                            list.Add(b);
                        }
                    }
                }
                catch (JsonException)
                {
                    // ignore corrupt round-trip; fall through to legacy
                }
            }

            if (list.Count == 0)
                list.AddRange(BuildBundlesFromLegacyCountFields(data));

            return list;
        }

        /// <summary>
        /// Converts sheet bundles into runtime bundles and merges WHEN tokens into <paramref name="triggerConditions"/>.
        /// </summary>
        public static List<ActionTriggerBundle> ApplyToActionData(
            SpreadsheetActionData spreadsheet,
            List<string> triggerConditions)
        {
            var bundles = LoadBundles(spreadsheet);
            triggerConditions ??= new List<string>();

            foreach (var bundle in bundles)
            {
                if (!bundle.IsEnabled)
                    continue;

                // gate_only_listed: WHEN with mechanic pointers is owned by the combat applicator.
                // Empty → still merges WHEN into whole-row triggerConditions (legacy enable-only).
                if (bundle.ParseMechanicIds().Count > 0)
                    continue;

                string when = CanonicalWhen(bundle.When);
                if (string.Equals(when, "ONROLLVALUE", StringComparison.OrdinalIgnoreCase))
                {
                    if (!triggerConditions.Exists(c =>
                            c.StartsWith("ONROLLVALUE", StringComparison.OrdinalIgnoreCase)))
                        triggerConditions.Add("ONROLLVALUE");
                    continue;
                }

                if (string.Equals(when, "ONROOMSCLEARED", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(bundle.Count.Trim(), out int n) && n > 0)
                    {
                        string token = $"ONROOMSCLEARED:{n}";
                        if (!triggerConditions.Exists(c =>
                                c.StartsWith("ONROOMSCLEARED", StringComparison.OrdinalIgnoreCase)))
                            triggerConditions.Add(token);
                    }
                    else if (!triggerConditions.Exists(c =>
                                 c.StartsWith("ONROOMSCLEARED", StringComparison.OrdinalIgnoreCase)))
                    {
                        triggerConditions.Add("ONROOMSCLEARED");
                    }

                    continue;
                }

                if (!triggerConditions.Exists(c =>
                        string.Equals(CanonicalWhen(c), when, StringComparison.OrdinalIgnoreCase)))
                    triggerConditions.Add(when);
            }

            return bundles;
        }

        public static void ApplyBundlesToSpreadsheetRow(SpreadsheetActionData data, IEnumerable<ActionTriggerBundle>? bundles)
        {
            if (data == null)
                return;

            var list = bundles?.Where(b => b != null && !string.IsNullOrWhiteSpace(b.When)).ToList()
                       ?? new List<ActionTriggerBundle>();
            foreach (var b in list)
            {
                b.When = CanonicalWhen(b.When);
                b.Scope = NormalizeScope(b.Scope);
                SyncLegacyCountField(data, b.When, b.Count);
            }

            data.TriggerBundlesJson = list.Count > 0 ? JsonSerializer.Serialize(list) : "";
        }

        public static IReadOnlyList<object> BuildHeaderContextRow(SpreadsheetHeader header)
        {
            return header.ContextByIndex.Select(c => (object)(c ?? "")).ToList();
        }

        public static IReadOnlyList<object> BuildHeaderLabelRow(SpreadsheetHeader header)
        {
            return header.LabelByIndex.Select(l => (object)(l ?? "")).ToList();
        }

        private static List<ActionTriggerBundle> BuildBundlesFromLegacyCountFields(SpreadsheetActionData data)
        {
            var list = new List<ActionTriggerBundle>();
            void Add(string when, string count)
            {
                if (string.IsNullOrWhiteSpace(count))
                    return;
                list.Add(new ActionTriggerBundle { When = when, Count = count.Trim() });
            }

            Add("ONHIT", data.OnHit);
            Add("ONMISS", data.OnMiss);
            Add("ONCRITICAL", data.OnCrit);
            Add("ONKILL", data.OnKill);
            Add("ONROOMSCLEARED", data.OnRoomsCleared);
            Add("ONROLLVALUE", data.OnRollValue);
            return list;
        }

        private static void ApplyLegacyCountFallback(SpreadsheetActionData data, string when, ref string count)
        {
            if (!string.IsNullOrWhiteSpace(count))
                return;
            count = GetLegacyCount(data, when);
        }

        private static string GetLegacyCount(SpreadsheetActionData data, string when)
        {
            return CanonicalWhen(when) switch
            {
                "ONHIT" => data.OnHit ?? "",
                "ONMISS" => data.OnMiss ?? "",
                "ONCRITICAL" => data.OnCrit ?? "",
                "ONKILL" => data.OnKill ?? "",
                "ONROOMSCLEARED" => data.OnRoomsCleared ?? "",
                "ONROLLVALUE" => data.OnRollValue ?? "",
                _ => ""
            };
        }

        private static void SyncLegacyCountField(SpreadsheetActionData data, string when, string count)
        {
            switch (CanonicalWhen(when))
            {
                case "ONHIT": data.OnHit = count; break;
                case "ONMISS": data.OnMiss = count; break;
                case "ONCRITICAL": data.OnCrit = count; break;
                case "ONKILL": data.OnKill = count; break;
                case "ONROOMSCLEARED": data.OnRoomsCleared = count; break;
                case "ONROLLVALUE": data.OnRollValue = count; break;
            }
        }

        public static string CanonicalWhen(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";
            string u = SpreadsheetHeader.NormalizeLabel(raw);
            return u switch
            {
                "ONCRIT" => "ONCRITICAL",
                "ONCRITICALHIT" => "ONCRITICAL",
                "ONCRITMISS" => "ONCRITICALMISS",
                "ONCOMBOHIT" => "ONCOMBO",
                "ONCOMBOENDED" => "ONCOMBOEND",
                "ONANYHIT" => "ONCONNECT",
                "ONFIRSTBLOOD" => "ONFIRSTHIT",
                "ONROOMCLEARED" => "ONROOMSCLEARED",
                _ => u
            };
        }

        public static string NormalizeScope(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";
            return CadenceKeywords.Normalize(raw.Trim());
        }

        private static void Append(List<string> contexts, List<string> labels, string label)
        {
            contexts.Add(ContextBand);
            labels.Add(label);
        }

        private static int FindColumn(List<string> contexts, List<string> labels, string label)
        {
            string want = SpreadsheetHeader.NormalizeLabel(label);
            string ctx = SpreadsheetHeader.NormalizeLabel(ContextBand);
            for (int i = 0; i < labels.Count; i++)
            {
                if (SpreadsheetHeader.NormalizeLabel(labels[i]) != want)
                    continue;
                string c = i < contexts.Count ? SpreadsheetHeader.NormalizeLabel(contexts[i]) : "";
                if (c == ctx || string.IsNullOrEmpty(c))
                    return i;
            }

            for (int i = 0; i < labels.Count; i++)
            {
                if (SpreadsheetHeader.NormalizeLabel(labels[i]) == want)
                    return i;
            }

            return -1;
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v))
                    return v;
            }

            return "";
        }
    }
}
