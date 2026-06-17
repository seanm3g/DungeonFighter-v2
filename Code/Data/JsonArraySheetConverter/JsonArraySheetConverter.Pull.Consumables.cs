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
        /// <summary>Maps human CONSUMABLES sheet headers to camelCase JSON keys and drops extra columns.</summary>
        private static void NormalizeConsumablesJsonArrayRow(JsonObject obj)
        {
            MoveConsumableJsonKeyIfPresent(obj, "displayName", "Display name", "displayName", "name", "Name");
            MoveConsumableJsonKeyIfPresent(obj, "internalKind", "Internal kind", "internalKind", "kind", "Kind");
            MoveConsumableJsonKeyIfPresent(obj, "effect", "Effect (dungeon-scoped until run ends)", "Effect", "effect");
            MoveConsumableJsonKeyIfPresent(obj, "potency", "Typical potency*", "Typical potency", "potency", "Typical Potency");

            if (obj.TryGetPropertyValue("potency", out JsonNode? potNode) && potNode is JsonValue jv)
            {
                string formatted = FormatConsumablePotencyCell(jv);
                if (formatted.Length > 0)
                    obj["potency"] = JsonValue.Create(formatted);
            }

            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (!JsonArraySheetSchemas.ConsumablesAuthorizedJsonKeys.Contains(key))
                    obj.Remove(key);
            }
        }

        private static void MoveConsumableJsonKeyIfPresent(JsonObject obj, string canonicalKey, params string[] sourceAliases)
        {
            if (obj.TryGetPropertyValue(canonicalKey, out JsonNode? existing) && existing != null)
                return;

            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                bool matchesAlias = false;
                foreach (string alias in sourceAliases)
                {
                    if (string.Equals(key, alias, StringComparison.OrdinalIgnoreCase))
                    {
                        matchesAlias = true;
                        break;
                    }
                }

                if (!matchesAlias)
                    continue;
                if (!obj.TryGetPropertyValue(key, out JsonNode? val) || val is null)
                    continue;
                obj.Remove(key);
                obj[canonicalKey] = val;
                return;
            }
        }

        private static string FormatConsumablePotencyCell(JsonValue jv)
        {
            if (jv.TryGetValue<string>(out string? s) && !string.IsNullOrWhiteSpace(s))
                return s.Trim();
            if (jv.TryGetValue<int>(out int i))
                return i.ToString(CultureInfo.InvariantCulture);
            if (jv.TryGetValue<long>(out long l))
                return l.ToString(CultureInfo.InvariantCulture);
            if (jv.TryGetValue<double>(out double d))
            {
                double r = Math.Round(d);
                if (Math.Abs(d - r) < 1e-9)
                    return ((long)r).ToString(CultureInfo.InvariantCulture);
                return d.ToString("0.###", CultureInfo.InvariantCulture);
            }

            return "";
        }
    }
}
