using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RPGGame.Data
{
    /// <summary>Maps legacy absolute enemy HP keys to percentage fields and parses optional % suffixes.</summary>
    internal static class EnemyJsonHealthNormalizer
    {
        internal const string HealthPercentKey = "healthPercent";
        internal const string HealthGrowthPercentKey = "healthGrowthPercent";
        internal const string LegacyBaseHealthKey = "baseHealth";
        internal const string LegacyHealthGrowthKey = "healthGrowthPerLevel";

        internal static string RenameLegacyKeysInJsonArrayText(string jsonContent)
        {
            using var doc = JsonDocument.Parse(jsonContent);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return jsonContent;

            var arr = new JsonArray();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                {
                    arr.Add(JsonNode.Parse(el.GetRawText()));
                    continue;
                }

                var obj = JsonNode.Parse(el.GetRawText()) as JsonObject;
                if (obj != null)
                {
                    NormalizeHealthPercentFields(obj);
                    arr.Add(obj);
                }
            }

            return arr.ToJsonString();
        }

        internal static void NormalizeHealthPercentFields(JsonObject obj)
        {
            RenameLegacyKey(obj, LegacyBaseHealthKey, HealthPercentKey);
            RenameLegacyKey(obj, LegacyHealthGrowthKey, HealthGrowthPercentKey);
            CoercePercentNode(obj, HealthPercentKey);
            CoercePercentNode(obj, HealthGrowthPercentKey);
        }

        private static void RenameLegacyKey(JsonObject obj, string legacyKey, string newKey)
        {
            if (!obj.TryGetPropertyValue(legacyKey, out var val) || val is null)
                return;
            if (!obj.ContainsKey(newKey))
                obj[newKey] = val.DeepClone();
            obj.Remove(legacyKey);
        }

        private static void CoercePercentNode(JsonObject obj, string key)
        {
            if (!obj.TryGetPropertyValue(key, out var node) || node is null)
                return;

            if (node is JsonValue jv && jv.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s))
            {
                if (TryParsePercentString(s, out double parsed))
                    obj[key] = parsed;
                return;
            }

            if (node is JsonValue num && num.TryGetValue<double>(out _))
                return;
        }

        internal static bool TryParsePercentString(string raw, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            raw = raw.Trim();
            if (raw.EndsWith('%'))
                raw = raw[..^1].Trim();

            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
