using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using RPGGame;
using RPGGame.World.Tags;

namespace RPGGame.Data
{
    public static partial class JsonArraySheetConverter
    {
        private static void NormalizeEnvironmentJsonArrayRow(JsonObject obj)
        {
            CanonicalizeEnvironmentImportRowKeys(obj);
            NormalizeTagsFromSheet(obj);
            NormalizeEnvironmentWeightedListFromSheet(obj, "actions");
            NormalizeEnvironmentWeightedListFromSheet(obj, "enemies");
        }

        private static void CanonicalizeEnvironmentImportRowKeys(JsonObject obj)
        {
            var renames = new List<(string oldKey, string newKey)>();
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (!EnvironmentRootHeaderCanonicalNames.TryGetValue(key.Trim(), out var canon))
                    continue;
                if (!string.Equals(key, canon, StringComparison.Ordinal))
                    renames.Add((key, canon));
            }

            foreach (var (oldKey, newKey) in renames)
            {
                if (!obj.TryGetPropertyValue(oldKey, out var val))
                    continue;
                if (obj.ContainsKey(newKey))
                    obj.Remove(oldKey);
                else
                {
                    obj.Remove(oldKey);
                    if (val is not null)
                        obj[newKey] = val;
                }
            }

            // Legacy Rooms.json used "Location" PascalCase without other fields.
            if (obj.TryGetPropertyValue("Location", out var locNode) && locNode is not null
                && !obj.ContainsKey("location"))
            {
                obj["location"] = locNode.DeepClone();
                obj.Remove("Location");
            }

            if (obj.TryGetPropertyValue("name", out var nameNode) && nameNode is not null
                && !obj.ContainsKey("location"))
            {
                obj["location"] = nameNode.DeepClone();
            }
        }

        private static readonly Dictionary<string, string> EnvironmentRootHeaderCanonicalNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["region"] = "region",
                ["biome"] = "biome",
                ["location"] = "location",
                ["tags"] = "tags",
                ["description"] = "description",
                ["actions"] = "actions",
                ["enemies"] = "enemies",
                ["name"] = "location",
                ["theme"] = "biome"
            };

        /// <summary>
        /// Coerces sheet <c>actions</c> / <c>enemies</c> cells into <c>{ name, weight }</c> JSON arrays.
        /// </summary>
        private static void NormalizeEnvironmentWeightedListFromSheet(JsonObject obj, string property)
        {
            if (!obj.TryGetPropertyValue(property, out var node) || node is null || IsJsonNodeNullOrMissing(node))
                return;

            if (node is JsonArray arr)
            {
                var normalized = new JsonArray();
                foreach (var item in arr)
                {
                    if (item is JsonObject o && o.TryGetPropertyValue("name", out _))
                    {
                        normalized.Add(item.DeepClone());
                        continue;
                    }

                    if (item is JsonValue jv && jv.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s))
                        normalized.Add(BuildWeightedEntry(s.Trim(), 1.0));
                }

                if (normalized.Count > 0)
                    obj[property] = normalized;
                return;
            }

            if (node is not JsonValue strVal || !strVal.TryGetValue<string>(out var cell) || string.IsNullOrWhiteSpace(cell))
                return;

            var parts = SplitEnvironmentSheetList(cell);
            if (parts.Length == 0)
                return;

            var outArr = new JsonArray();
            foreach (var part in parts)
            {
                var (name, weight) = ParseEnvironmentWeightedToken(part);
                if (!string.IsNullOrWhiteSpace(name))
                    outArr.Add(BuildWeightedEntry(name, weight));
            }

            obj[property] = outArr;
        }

        private static string[] SplitEnvironmentSheetList(string cell)
        {
            var s = cell.Trim();
            char[] splitChars = s.Contains('|', StringComparison.Ordinal)
                ? new[] { '|' }
                : new[] { ',', '\n', '\r', ';' };
            return s.Split(splitChars, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => p.Trim().Trim('"'))
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();
        }

        private static (string name, double weight) ParseEnvironmentWeightedToken(string token)
        {
            int colon = token.IndexOf(':');
            if (colon > 0 && colon < token.Length - 1
                && double.TryParse(token[(colon + 1)..].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var w))
            {
                return (token[..colon].Trim(), w);
            }

            return (token, 1.0);
        }

        private static JsonObject BuildWeightedEntry(string name, double weight) =>
            new JsonObject { ["name"] = name, ["weight"] = weight };
    }
}
