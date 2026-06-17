using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RPGGame.Data
{
    /// <summary>Sheet cell formatting helpers for JSON array push (JSON → spreadsheet cells).</summary>
    internal static class SheetCellFormatters
    {
        internal static string JsonStringToCell(JsonElement el) =>
            SheetsPushUtilities.NormalizeSheetString(el.GetString());

        internal static string JsonElementToCellString(JsonElement el) =>
            el.ValueKind switch
            {
                JsonValueKind.Null => "",
                JsonValueKind.String => JsonStringToCell(el),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => el.GetRawText(),
                JsonValueKind.Object or JsonValueKind.Array => el.GetRawText(),
                _ => el.GetRawText()
            };

        internal static string FormatPipeDelimitedStringArrayForSheetCell(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.String)
                return JsonStringToCell(el);

            if (el.ValueKind != JsonValueKind.Array)
                return JsonElementToCellString(el);

            var parts = new List<string>();
            foreach (var item in el.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                    continue;
                var s = item.GetString()?.Trim();
                if (!string.IsNullOrEmpty(s))
                    parts.Add(s);
            }

            return parts.Count == 0 ? "" : string.Join("|", parts);
        }

        internal static string FormatEnvironmentWeightedListForSheetCell(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.String)
                return JsonStringToCell(el);

            if (el.ValueKind != JsonValueKind.Array)
                return JsonElementToCellString(el);

            var entries = new List<(string name, double weight)>();
            foreach (var item in el.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var s = item.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(s))
                        entries.Add((s, 1.0));
                    continue;
                }

                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                if (!SheetJsonPropertyHelper.TryGetJsonPropertyCaseInsensitive(item, "name", out var nameEl))
                    continue;

                var name = nameEl.GetString()?.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;

                double weight = 1.0;
                if (SheetJsonPropertyHelper.TryGetJsonPropertyCaseInsensitive(item, "weight", out var wEl) && wEl.ValueKind == JsonValueKind.Number)
                    weight = wEl.GetDouble();

                entries.Add((name, weight));
            }

            if (entries.Count == 0)
                return "";

            bool useWeights = entries.Any(e => Math.Abs(e.weight - 1.0) > 0.0001);
            if (!useWeights)
                return string.Join("|", entries.Select(e => e.name));

            return string.Join("|", entries.Select(e =>
                $"{e.name}:{e.weight.ToString(CultureInfo.InvariantCulture)}"));
        }
    }
}
