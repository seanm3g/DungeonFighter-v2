using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;

namespace RPGGame.Data
{
    public static partial class JsonArraySheetConverter
    {
        /// <summary>Maps triggers sheet headers to canonical JSON keys and drops extra columns.</summary>
        private static void NormalizeTriggersJsonArrayRow(JsonObject obj)
        {
            MoveTriggerJsonKeyIfPresent(obj, "id", "Id", "index", "Index");
            MoveTriggerJsonKeyIfPresent(obj, "name", "Name", "identity", "Identity");
            MoveTriggerJsonKeyIfPresent(obj, "when", "When", "WHEN");
            MoveTriggerJsonKeyIfPresent(obj, "count", "Count", "COUNT");
            MoveTriggerJsonKeyIfPresent(obj, "scope", "Scope", "SCOPE");
            MoveTriggerJsonKeyIfPresent(obj, "mechanics", "Mechanics", "MECHANICS", "mechanic");
            MoveTriggerJsonKeyIfPresent(obj, "value", "Value", "VALUE", "magnitude");
            MoveTriggerJsonKeyIfPresent(obj, "filters", "Filters", "FILTERS", "filter");
            MoveTriggerJsonKeyIfPresent(obj, "channel", "Channel", "CHANNEL");

            if (obj.TryGetPropertyValue("filters", out JsonNode? filtersNode) && filtersNode is JsonArray fa)
            {
                var parts = new List<string>();
                foreach (var n in fa)
                {
                    if (n is JsonValue jv && jv.TryGetValue<string>(out string? s) && !string.IsNullOrWhiteSpace(s))
                        parts.Add(s.Trim());
                }

                obj["filters"] = string.Join(",", parts);
            }
            else if (filtersNode is JsonValue fjv && fjv.TryGetValue<string>(out string? fs))
            {
                obj["filters"] = (fs ?? "").Trim();
            }

            if (!obj.TryGetPropertyValue("channel", out JsonNode? chNode)
                || chNode is null
                || (chNode is JsonValue cv && cv.TryGetValue<string>(out string? chs) && string.IsNullOrWhiteSpace(chs)))
            {
                obj["channel"] = "combat";
            }
            else if (chNode is JsonValue chv && chv.TryGetValue<string>(out string? channelRaw))
            {
                string c = (channelRaw ?? "").Trim();
                if (c.Equals("equip", StringComparison.OrdinalIgnoreCase)
                    || c.Equals("equipped", StringComparison.OrdinalIgnoreCase)
                    || c.Equals("while_equipped", StringComparison.OrdinalIgnoreCase))
                    obj["channel"] = "equip";
                else
                    obj["channel"] = "combat";
            }

            if (!obj.TryGetPropertyValue("count", out JsonNode? countNode)
                || countNode is null
                || (countNode is JsonValue countVal && countVal.TryGetValue<string>(out string? cs) && string.IsNullOrWhiteSpace(cs)))
            {
                obj["count"] = "1";
            }
            else if (countNode is JsonValue cjv)
            {
                if (cjv.TryGetValue<int>(out int ci))
                    obj["count"] = ci.ToString(CultureInfo.InvariantCulture);
                else if (cjv.TryGetValue<long>(out long cl))
                    obj["count"] = cl.ToString(CultureInfo.InvariantCulture);
                else if (cjv.TryGetValue<double>(out double cd))
                    obj["count"] = ((long)Math.Round(cd)).ToString(CultureInfo.InvariantCulture);
            }

            if (obj.TryGetPropertyValue("id", out JsonNode? idNode) && idNode is JsonValue idv)
            {
                if (idv.TryGetValue<double>(out double idd))
                    obj["id"] = (int)Math.Round(idd);
                else if (idv.TryGetValue<string>(out string? ids)
                         && int.TryParse(ids, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idi))
                    obj["id"] = idi;
            }

            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (!JsonArraySheetSchemas.TriggersAuthorizedJsonKeys.Contains(key))
                    obj.Remove(key);
            }
        }

        private static void MoveTriggerJsonKeyIfPresent(JsonObject obj, string canonicalKey, params string[] sourceAliases)
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
    }
}
