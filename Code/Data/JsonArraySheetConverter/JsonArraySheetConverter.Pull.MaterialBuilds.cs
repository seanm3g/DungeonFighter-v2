using System;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;

namespace RPGGame.Data
{
    public static partial class JsonArraySheetConverter
    {
        /// <summary>Maps MATERIAL BUILDS sheet headers to camelCase JSON keys and drops extra columns.</summary>
        private static void NormalizeMaterialBuildsJsonArrayRow(JsonObject obj)
        {
            MoveMaterialBuildJsonKeyIfPresent(obj, "class", "CLASS", "AssociatedClass", "Class");
            MoveMaterialBuildJsonKeyIfPresent(obj, "material", "MATERIAL", "Material");
            MoveMaterialBuildJsonKeyIfPresent(obj, "synthesis", "SYNTHESIS", "Synthesis");
            MoveMaterialBuildJsonKeyIfPresent(obj, "convertAction",
                "CONVERT", "CONVERT(action)", "CONVERT (action)", "CONVERT(ACTION)", "Convert", "convertAction");
            MoveMaterialBuildJsonKeyIfPresent(obj, "feed", "FEED", "Feed");
            MoveMaterialBuildJsonKeyIfPresent(obj, "stack2", "2 STACK", "2STACK", "2-STACK", "stack2", "F");
            MoveMaterialBuildJsonKeyIfPresent(obj, "stack3", "3 STACK", "3STACK", "3-STACK", "stack3", "G");
            MoveMaterialBuildJsonKeyIfPresent(obj, "stack5", "5 STACK", "5STACK", "5-STACK", "stack5", "H");

            CoerceMaterialBuildString(obj, "class");
            CoerceMaterialBuildString(obj, "material");
            CoerceMaterialBuildString(obj, "synthesis");
            CoerceMaterialBuildString(obj, "convertAction");
            CoerceMaterialBuildString(obj, "feed");
            CoerceMaterialBuildString(obj, "stack2");
            CoerceMaterialBuildString(obj, "stack3");
            CoerceMaterialBuildString(obj, "stack5");

            if (obj.TryGetPropertyValue("material", out JsonNode? matNode)
                && matNode is JsonValue mjv
                && mjv.TryGetValue<string>(out string? mat)
                && !string.IsNullOrWhiteSpace(mat))
            {
                obj["material"] = MaterialBuildData.CanonicalMaterialName(mat);
            }

            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (!JsonArraySheetSchemas.MaterialBuildsAuthorizedJsonKeys.Contains(key))
                    obj.Remove(key);
            }
        }

        private static void CoerceMaterialBuildString(JsonObject obj, string key)
        {
            if (!obj.TryGetPropertyValue(key, out JsonNode? node) || node is not JsonValue jv)
                return;
            if (jv.TryGetValue<string>(out string? s))
            {
                obj[key] = (s ?? "").Trim();
                return;
            }
            if (jv.TryGetValue<int>(out int i))
            {
                obj[key] = i.ToString(CultureInfo.InvariantCulture);
                return;
            }
            if (jv.TryGetValue<double>(out double d))
            {
                obj[key] = d.ToString("0.###", CultureInfo.InvariantCulture);
            }
        }

        private static void MoveMaterialBuildJsonKeyIfPresent(JsonObject obj, string canonicalKey, params string[] sourceAliases)
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
