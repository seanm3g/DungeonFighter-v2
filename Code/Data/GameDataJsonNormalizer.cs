using System;
using System.Globalization;
using System.IO;
using RPGGame;
using System.Linq;
using System.Text.Json.Nodes;

namespace RPGGame.Data
{
    /// <summary>
    /// Repairs Google Sheets–exported JSON so it matches runtime deserialization (duplicate keys, string cells vs typed models).
    /// Applied in <see cref="JsonLoader"/> before <see cref="System.Text.Json.JsonSerializer.Deserialize"/>.
    /// </summary>
    public static class GameDataJsonNormalizer
    {
        /// <summary>Rewrites JSON text for known game data files when needed; otherwise returns <paramref name="json"/> unchanged.</summary>
        public static string NormalizeForGameDataFile(string fileName, string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return json;

            string name = Path.GetFileName(fileName);

            if (string.Equals(name, GameConstants.WeaponsJson, StringComparison.OrdinalIgnoreCase))
                return NormalizeWeaponsJson(json);
            if (string.Equals(name, GameConstants.StatBonusesJson, StringComparison.OrdinalIgnoreCase))
                return NormalizeStatBonusesJson(json);
            if (string.Equals(name, GameConstants.ModificationsJson, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, GameConstants.PrefixMaterialQualityJson, StringComparison.OrdinalIgnoreCase))
                return NormalizeModificationsJson(json);

            return json;
        }

        private static string NormalizeWeaponsJson(string json)
        {
            try
            {
                JsonNode? root = JsonNode.Parse(json);
                if (root is not JsonArray arr)
                    return json;

                foreach (var el in arr)
                {
                    if (el is JsonObject o)
                        NormalizeWeaponRow(o);
                }

                return root.ToJsonString();
            }
            catch
            {
                return json;
            }
        }

        /// <summary>Merges sheet columns <c>attributeRequirements</c> (abbrev string) + <c>requirement value</c> into a dictionary.</summary>
        private static void NormalizeWeaponRow(JsonObject o)
        {
            JsonNode? attrNode = FindPropertyIgnoreCase(o, "attributeRequirements");
            if (attrNode is not JsonValue strVal || !strVal.TryGetValue<string>(out var abbrev) ||
                string.IsNullOrWhiteSpace(abbrev))
                return;

            int reqVal = CoerceInt(FindPropertyIgnoreCase(o, "requirement value"));
            string statKey = MapSheetStatAbbrevToRequirementKey(abbrev.Trim());

            RemovePropertyIgnoreCase(o, "attributeRequirements");
            var dict = new JsonObject { [statKey] = JsonValue.Create(reqVal) };
            o["attributeRequirements"] = dict;
        }

        private static string NormalizeStatBonusesJson(string json)
        {
            try
            {
                JsonNode? root = JsonNode.Parse(json);
                if (root is not JsonArray arr)
                    return json;

                foreach (var el in arr)
                {
                    if (el is JsonObject o)
                        NormalizeStatBonusRow(o);
                }

                return root.ToJsonString();
            }
            catch
            {
                return json;
            }
        }

        /// <summary>
        /// Sheet exports may include both a raw bracket string column (<c>mechanics</c>) and a parsed <c>Mechanics</c> array.
        /// Case-insensitive deserialization maps both to <see cref="Items.StatBonus.Mechanics"/> and breaks on string first.
        /// </summary>
        private static void NormalizeStatBonusRow(JsonObject o)
        {
            try
            {
                // Lowercase `mechanics` is the raw bracket column; `Mechanics` is the parsed array — both may exist as distinct JSON keys.
                if (!o.TryGetPropertyValue("mechanics", out var lowMechanics) || lowMechanics is null)
                    return;

                if (o.TryGetPropertyValue("Mechanics", out var pascalMechanics) && pascalMechanics is JsonArray arr && arr.Count > 0)
                {
                    o.Remove("mechanics");
                    return;
                }

                if (lowMechanics is JsonValue mv && mv.TryGetValue<string>(out var bracket) && !string.IsNullOrWhiteSpace(bracket))
                {
                    var parsed = JsonArraySheetConverter.ParseStatBonusBracketMechanics(bracket.Trim());
                    if (parsed != null && parsed.Count > 0)
                        o["Mechanics"] = parsed;
                    o.Remove("mechanics");
                }
            }
            finally
            {
                JsonArraySheetConverter.RemoveStatBonusUnknownJsonKeys(o);
            }
        }

        private static string NormalizeModificationsJson(string json)
        {
            try
            {
                JsonNode? root = JsonNode.Parse(json);
                if (root is not JsonArray arr)
                    return json;

                foreach (var el in arr)
                {
                    if (el is JsonObject o)
                        NormalizeModificationRow(o);
                }

                return root.ToJsonString();
            }
            catch
            {
                return json;
            }
        }

        private static void NormalizeModificationRow(JsonObject o)
        {
            CoercePropertyToInt(o, "DiceResult");
            CoercePropertyToDouble(o, "MinValue");
            CoercePropertyToDouble(o, "MaxValue");
            CoercePropertyToDouble(o, "RolledValue");
        }

        private static void CoercePropertyToInt(JsonObject o, string propertyName)
        {
            JsonNode? n = FindPropertyIgnoreCase(o, propertyName);
            if (n == null || IsJsonNull(n))
            {
                SetPropertyIgnoreCase(o, propertyName, JsonValue.Create(0));
                return;
            }

            if (n is JsonValue jv)
            {
                if (jv.TryGetValue(out int i))
                {
                    SetPropertyIgnoreCase(o, propertyName, JsonValue.Create(i));
                    return;
                }

                if (jv.TryGetValue(out string? sInt) && !string.IsNullOrEmpty(sInt) &&
                    int.TryParse(sInt.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int p))
                {
                    SetPropertyIgnoreCase(o, propertyName, JsonValue.Create(p));
                    return;
                }

                if (jv.TryGetValue(out double d))
                {
                    SetPropertyIgnoreCase(o, propertyName, JsonValue.Create((int)d));
                    return;
                }
            }

            SetPropertyIgnoreCase(o, propertyName, JsonValue.Create(0));
        }

        private static void CoercePropertyToDouble(JsonObject o, string propertyName)
        {
            JsonNode? n = FindPropertyIgnoreCase(o, propertyName);
            if (n == null || IsJsonNull(n))
            {
                SetPropertyIgnoreCase(o, propertyName, JsonValue.Create(0.0));
                return;
            }

            if (n is JsonValue jv)
            {
                if (jv.TryGetValue(out double d))
                {
                    SetPropertyIgnoreCase(o, propertyName, JsonValue.Create(d));
                    return;
                }

                if (jv.TryGetValue(out string? s) && !string.IsNullOrEmpty(s) &&
                    double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double p))
                {
                    SetPropertyIgnoreCase(o, propertyName, JsonValue.Create(p));
                    return;
                }

                if (jv.TryGetValue(out int i))
                {
                    SetPropertyIgnoreCase(o, propertyName, JsonValue.Create((double)i));
                    return;
                }
            }

            SetPropertyIgnoreCase(o, propertyName, JsonValue.Create(0.0));
        }

        private static int CoerceInt(JsonNode? n)
        {
            if (n == null || IsJsonNull(n))
                return 0;
            if (n is JsonValue jv)
            {
                if (jv.TryGetValue(out int i))
                    return i;
                if (jv.TryGetValue(out double d))
                    return (int)d;
                if (jv.TryGetValue(out string? s) && !string.IsNullOrEmpty(s) &&
                    int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int p))
                    return p;
            }

            return 0;
        }

        private static bool IsJsonNull(JsonNode n) =>
            n is JsonValue v && v.TryGetValue<object?>(out var o) && o is null;

        private static JsonNode? FindPropertyIgnoreCase(JsonObject o, string name)
        {
            foreach (var kvp in o)
            {
                if (string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }

            return null;
        }

        private static void RemovePropertyIgnoreCase(JsonObject o, string name)
        {
            string? found = o.Select(kvp => kvp.Key).FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
            if (found != null)
                o.Remove(found);
        }

        private static void SetPropertyIgnoreCase(JsonObject o, string name, JsonNode value)
        {
            RemovePropertyIgnoreCase(o, name);
            o[name] = value;
        }

        /// <summary>Maps weapon sheet stat abbreviations to <see cref="Items.Item.MeetsRequirements"/> dictionary keys.</summary>
        private static string MapSheetStatAbbrevToRequirementKey(string raw)
        {
            string u = raw.Trim().ToUpperInvariant().Replace(" ", "").Replace("_", "");
            return u switch
            {
                "STR" or "STRENGTH" => "strength",
                "AGI" or "AGILITY" => "agility",
                "TEC" or "TECH" or "TECHNIQUE" => "technique",
                "INT" or "INTELLIGENCE" => "intelligence",
                _ => raw.Trim().ToLowerInvariant()
            };
        }
    }
}
