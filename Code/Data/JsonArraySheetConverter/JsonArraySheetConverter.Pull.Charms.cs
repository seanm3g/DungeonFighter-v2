using System;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;

namespace RPGGame.Data
{
    public static partial class JsonArraySheetConverter
    {
        /// <summary>Maps CHARMS sheet headers to camelCase JSON keys and drops extra columns.</summary>
        private static void NormalizeCharmsJsonArrayRow(JsonObject obj)
        {
            MoveCharmJsonKeyIfPresent(obj, "name", "NAME", "Name");
            MoveCharmJsonKeyIfPresent(obj, "description", "DESCRIPTION", "Description");
            MoveCharmJsonKeyIfPresent(obj, "layer", "LAYER", "Layer");
            MoveCharmJsonKeyIfPresent(obj, "amplifyMint", "AMPLIFYMINT", "amplify mint", "AmplifyMint");
            MoveCharmJsonKeyIfPresent(obj, "amplifyConvert", "AMPLIFYCONVERT", "amplify convert", "AmplifyConvert");
            MoveCharmJsonKeyIfPresent(obj, "amplifyClassDefense", "AMPLIFYCLASSDEFENSE", "amplify class defense", "AmplifyClassDefense");
            MoveCharmJsonKeyIfPresent(obj, "amplifyClassTagDamage", "AMPLIFYCLASSTAGDAMAGE", "amplify class tag damage", "AmplifyClassTagDamage");
            MoveCharmJsonKeyIfPresent(obj, "amplifyAnimalLadder", "AMPLIFYANIMALLADDER", "amplify animal ladder", "AmplifyAnimalLadder");
            MoveCharmJsonKeyIfPresent(obj, "unlockAnimalAction", "UNLOCKANIMALACTION", "unlock animal action", "UnlockAnimalAction");
            MoveCharmJsonKeyIfPresent(obj, "rarity", "RARITY", "Rarity");
            MoveCharmJsonKeyIfPresent(obj, "tags", "TAGS", "Tags");

            CoerceCharmString(obj, "name");
            CoerceCharmString(obj, "description");
            CoerceCharmString(obj, "layer");
            CoerceCharmString(obj, "rarity");
            CoerceCharmDouble(obj, "amplifyMint");
            CoerceCharmDouble(obj, "amplifyConvert");
            CoerceCharmDouble(obj, "amplifyClassDefense");
            CoerceCharmDouble(obj, "amplifyClassTagDamage");
            CoerceCharmDouble(obj, "amplifyAnimalLadder");
            CoerceCharmBool(obj, "unlockAnimalAction");

            if (obj.TryGetPropertyValue("layer", out JsonNode? layerNode)
                && layerNode is JsonValue ljv
                && ljv.TryGetValue<string>(out string? layer)
                && !string.IsNullOrWhiteSpace(layer))
            {
                obj["layer"] = layer.Trim().ToLowerInvariant();
            }

            NormalizeTagsFromSheet(obj);

            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (!JsonArraySheetSchemas.CharmsAuthorizedJsonKeys.Contains(key))
                    obj.Remove(key);
            }
        }

        private static void CoerceCharmString(JsonObject obj, string key)
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

        private static void CoerceCharmDouble(JsonObject obj, string key)
        {
            if (!obj.TryGetPropertyValue(key, out JsonNode? node) || node is not JsonValue jv)
                return;
            if (jv.TryGetValue<double>(out double d))
            {
                obj[key] = d;
                return;
            }
            if (jv.TryGetValue<int>(out int i))
            {
                obj[key] = (double)i;
                return;
            }
            if (jv.TryGetValue<string>(out string? s)
                && double.TryParse((s ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            {
                obj[key] = parsed;
            }
        }

        private static void CoerceCharmBool(JsonObject obj, string key)
        {
            if (!obj.TryGetPropertyValue(key, out JsonNode? node) || node is not JsonValue jv)
                return;
            if (jv.TryGetValue<bool>(out bool b))
            {
                obj[key] = b;
                return;
            }
            if (jv.TryGetValue<string>(out string? s))
            {
                string t = (s ?? "").Trim();
                if (bool.TryParse(t, out bool pb))
                    obj[key] = pb;
                else if (t == "1" || t.Equals("yes", StringComparison.OrdinalIgnoreCase)
                         || t.Equals("y", StringComparison.OrdinalIgnoreCase))
                    obj[key] = true;
                else if (t == "0" || t.Equals("no", StringComparison.OrdinalIgnoreCase)
                         || t.Equals("n", StringComparison.OrdinalIgnoreCase))
                    obj[key] = false;
            }
            else if (jv.TryGetValue<int>(out int i))
            {
                obj[key] = i != 0;
            }
        }

        private static void MoveCharmJsonKeyIfPresent(JsonObject obj, string canonicalKey, params string[] sourceAliases)
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
