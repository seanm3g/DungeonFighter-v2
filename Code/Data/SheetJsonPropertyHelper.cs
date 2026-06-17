using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RPGGame.Data
{
    /// <summary>Case-insensitive JSON property access shared by sheet conversion helpers.</summary>
    internal static class SheetJsonPropertyHelper
    {
        internal static bool TryGetJsonPropertyCaseInsensitive(JsonElement el, string name, out JsonElement prop)
        {
            prop = default;
            if (el.ValueKind != JsonValueKind.Object)
                return false;
            foreach (var p in el.EnumerateObject())
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    prop = p.Value;
                    return true;
                }
            }

            return false;
        }

        internal static bool TryGetJsonObjectPropertyCaseInsensitive(JsonObject obj, string name, out JsonNode? node)
        {
            node = null;
            foreach (var kvp in obj)
            {
                if (!string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase))
                    continue;
                node = kvp.Value;
                return true;
            }

            return false;
        }

        internal static bool TryGetPropertyPathCaseInsensitive(JsonElement el, string dottedPath, out JsonElement found)
        {
            found = default;
            var parts = dottedPath.Split('.');
            if (parts.Length < 2)
                return false;
            JsonElement cur = el;
            foreach (var part in parts)
            {
                if (cur.ValueKind != JsonValueKind.Object || !TryGetJsonPropertyCaseInsensitive(cur, part, out cur))
                    return false;
            }

            found = cur;
            return true;
        }

        internal static string GetJsonStringProperty(JsonElement el, string name)
        {
            if (!TryGetJsonPropertyCaseInsensitive(el, name, out var prop) || prop.ValueKind != JsonValueKind.String)
                return "";

            return prop.GetString() ?? "";
        }

        internal static int GetJsonIntProperty(JsonElement el, string name, int defaultValue = 0)
        {
            if (!TryGetJsonPropertyCaseInsensitive(el, name, out var prop) || prop.ValueKind != JsonValueKind.Number)
                return defaultValue;

            if (prop.TryGetInt32(out int iv))
                return iv;
            if (prop.TryGetDouble(out double dv))
                return (int)Math.Round(dv);

            return defaultValue;
        }

        internal static string GetJsonObjectStringProperty(JsonObject obj, string name)
        {
            if (!TryGetJsonObjectPropertyCaseInsensitive(obj, name, out JsonNode? node) || node is not JsonValue jv)
                return "";

            if (jv.TryGetValue<string>(out string? s))
                return s ?? "";

            return jv.ToString();
        }

        internal static int GetJsonObjectIntProperty(JsonObject obj, string name, int defaultValue = 0)
        {
            if (!TryGetJsonObjectPropertyCaseInsensitive(obj, name, out JsonNode? node) || node is not JsonValue jv)
                return defaultValue;

            if (jv.TryGetValue<int>(out int iv))
                return iv;
            if (jv.TryGetValue<long>(out long lv))
                return (int)lv;
            if (jv.TryGetValue<double>(out double dv))
                return (int)Math.Round(dv);

            return defaultValue;
        }
    }
}
