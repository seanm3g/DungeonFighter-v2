using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using RPGGame.World.Tags;

namespace RPGGame.Data
{
    /// <summary>Tag normalization for sheet import and push formatting.</summary>
    internal static class SheetTagsNormalizer
    {
        internal static void NormalizeTagsFromSheet(JsonObject obj)
        {
            string? tagsKey = null;
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (string.Equals(key, "tags", StringComparison.OrdinalIgnoreCase))
                {
                    tagsKey = key;
                    break;
                }
            }

            if (tagsKey is null)
                return;

            if (!obj.TryGetPropertyValue(tagsKey, out var node) || node is null || SheetJsonNodeHelper.IsJsonNodeNullOrMissing(node))
            {
                obj.Remove(tagsKey);
                return;
            }

            List<string> normalized;
            if (node is JsonArray arr)
            {
                var raw = new List<string>();
                foreach (var item in arr)
                {
                    if (item is JsonValue jv && jv.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s))
                        raw.Add(s);
                }

                normalized = GameDataTagHelper.NormalizeDistinct(raw);
            }
            else if (node is JsonValue jv && jv.TryGetValue<string>(out var cell) && !string.IsNullOrWhiteSpace(cell))
                normalized = GameDataTagHelper.ParseCommaSeparatedTags(cell);
            else
            {
                obj.Remove(tagsKey);
                return;
            }

            if (!string.Equals(tagsKey, "tags", StringComparison.Ordinal))
                obj.Remove(tagsKey);

            if (normalized.Count == 0)
                return;

            var outArr = new JsonArray();
            foreach (var t in normalized)
                outArr.Add(t);
            obj["tags"] = outArr;
        }

        internal static string FormatTagsArrayForSheetCell(JsonElement el)
        {
            if (el.ValueKind != JsonValueKind.Array)
                return SheetCellFormatters.JsonElementToCellString(el);

            var raw = new List<string>();
            foreach (var item in el.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var s = item.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(s))
                        raw.Add(s);
                }
            }

            var normalized = GameDataTagHelper.NormalizeDistinct(raw);
            if (normalized.Count == 0)
                return "";
            return string.Join(", ", normalized.Select(t => t.ToLowerInvariant()));
        }
    }
}
