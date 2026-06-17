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
        private static void NormalizeStatBonusesJsonArrayRow(JsonObject obj)
        {
            RenameStatBonusSheetImportHeaders(obj);
            RemoveStatBonusSheetDerivedColumns(obj);
            TryApplyStatBonusBracketMechanics(obj);
            TryApplyStatBonusBracketRequirements(obj);

            string? rarityText = null;
            var hadWeight = false;
            foreach (var kvp in obj.ToList())
            {
                if (string.Equals(kvp.Key, "Weight", StringComparison.OrdinalIgnoreCase))
                {
                    hadWeight = true;
                    obj.Remove(kvp.Key);
                    continue;
                }

                if (string.Equals(kvp.Key, "rarity", StringComparison.OrdinalIgnoreCase))
                {
                    rarityText ??= StatBonusRarityCellToString(kvp.Value);
                    if (!string.Equals(kvp.Key, "Rarity", StringComparison.Ordinal))
                        obj.Remove(kvp.Key);
                }
            }

            if (rarityText == null && obj.TryGetPropertyValue("Rarity", out var rn) && rn is not null)
                rarityText = StatBonusRarityCellToString(rn);

            if (string.IsNullOrWhiteSpace(rarityText) && hadWeight)
                rarityText = "Common";

            var final = string.IsNullOrWhiteSpace(rarityText) ? "Common" : rarityText.Trim();
            obj["Rarity"] = JsonValue.Create(final);
            RemoveStatBonusUnknownJsonKeys(obj);
        }

        internal static void RemoveStatBonusUnknownJsonKeys(JsonObject obj) =>
            StatBonusSheetBracketParser.RemoveStatBonusUnknownJsonKeys(obj);

        /// <summary>Maps common Google Sheets column titles to <c>StatBonuses.json</c> property names.</summary>
        private static void RenameStatBonusSheetImportHeaders(JsonObject obj)
        {
            MoveStatBonusJsonKeyIfPresent(obj, "Name", "Suffix tags", "Suffix Tags", "suffix tags", "Suffix", "suffix name", "Suffix name", "Affix name");
            MoveStatBonusJsonKeyIfPresent(obj, "Requirements", "stat requirement", "Stat requirement", "Stat Requirement", "requirement", "Requirement", "requirements");
            MoveStatBonusJsonKeyIfPresent(obj, "Mechanics", "Mechanics (bracket)", "mechanics bracket", "Bracket");
            MoveStatBonusJsonKeyIfPresent(obj, "Description", "description");
            MoveStatBonusJsonKeyIfPresent(obj, "StatType", "stat type", "Stat type");
            MoveStatBonusJsonKeyIfPresent(obj, "Value", "value");
            MoveStatBonusJsonKeyIfPresent(obj, "Rarity", "rarity");
        }

        /// <summary>When <paramref name="canonicalKey"/> is missing, copies the first matching alias key onto it and removes the alias.</summary>
        private static void MoveStatBonusJsonKeyIfPresent(JsonObject obj, string canonicalKey, params string[] sourceAliases)
        {
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (string.Equals(key, canonicalKey, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            foreach (var alias in sourceAliases)
            {
                foreach (var kvp in obj.ToList())
                {
                    if (!string.Equals(kvp.Key, alias, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!obj.TryGetPropertyValue(kvp.Key, out var val))
                        continue;
                    obj.Remove(kvp.Key);
                    obj[canonicalKey] = val?.DeepClone() ?? JsonValue.Create("");
                    return;
                }
            }
        }

        /// <summary>Removes split mechanic columns from the sheet export (keeps <c>Mechanics</c>).</summary>
        private static void RemoveStatBonusSheetDerivedColumns(JsonObject obj)
        {
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (string.Equals(key, "Mechanics", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (key.StartsWith("Mechanic", StringComparison.OrdinalIgnoreCase) ||
                    key.StartsWith("Mechanc", StringComparison.OrdinalIgnoreCase) || // sheet typo "Mechanc i2"
                    key.StartsWith("Mechaniv", StringComparison.OrdinalIgnoreCase)) // sheet typo "Mechaniv 1 value"
                    obj.Remove(key);
            }
        }

        /// <summary>
        /// Fills <c>Mechanics</c> from a bracket cell <c>[ARMOR:5,MAX HEALTH:15]</c> or from any column whose string value matches that pattern.
        /// When mechanics are present, mirrors the first pair onto legacy <c>StatType</c>/<c>Value</c> for older tooling.
        /// </summary>
        private static void TryApplyStatBonusBracketMechanics(JsonObject obj)
        {
            string? bracketSource = null;

            if (obj.TryGetPropertyValue("Mechanics", out var mechNode) && mechNode is not null && !IsJsonNodeNullOrMissing(mechNode))
            {
                if (mechNode is JsonValue jv && jv.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s))
                    bracketSource = s.Trim();
                else if (mechNode is JsonArray existingArr && existingArr.Count > 0)
                {
                    NormalizeStatBonusMechanicsArrayStatTypes(existingArr);
                    return;
                }
            }

            if (string.IsNullOrEmpty(bracketSource))
            {
                foreach (var kvp in obj)
                {
                    // The Requirements column is also bracket-formatted but parsed separately.
                    if (string.Equals(kvp.Key, "Requirements", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (kvp.Value is JsonValue jv2 && jv2.TryGetValue<string>(out var cell) && cell != null)
                    {
                        string t = cell.Trim();
                        if (t.Length >= 2 && t[0] == '[' && t[^1] == ']')
                        {
                            bracketSource = t;
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(bracketSource))
                return;

            var parsed = ParseStatBonusBracketMechanics(bracketSource);
            if (parsed == null || parsed.Count == 0)
                return;

            obj["Mechanics"] = parsed;
            if (parsed.Count > 0 && parsed[0] is JsonObject first)
            {
                if (first.TryGetPropertyValue("StatType", out var st) && st is JsonValue stv && stv.TryGetValue<string>(out var statType))
                    obj["StatType"] = JsonValue.Create(statType);
                if (first.TryGetPropertyValue("Value", out var vn) && vn is JsonValue vv)
                    obj["Value"] = vv.DeepClone();
            }
        }

        private static void NormalizeStatBonusMechanicsArrayStatTypes(JsonArray arr) =>
            StatBonusSheetBracketParser.NormalizeStatBonusMechanicsArrayStatTypes(arr);

        /// <summary>
        /// Fills <c>Requirements</c> from a bracket cell <c>[strength:5,primary:15]</c> on the SUFFIXES sheet
        /// (or whatever cell currently holds the value after header rename / pre-existing JSON). Duplicate keys
        /// keep the maximum value. Already-parsed object form is canonicalized in place. Unknown keys are dropped.
        /// </summary>
        private static void TryApplyStatBonusBracketRequirements(JsonObject obj)
        {
            string? bracketSource = null;

            if (obj.TryGetPropertyValue("Requirements", out var reqNode) && reqNode is not null && !IsJsonNodeNullOrMissing(reqNode))
            {
                if (reqNode is JsonValue jv && jv.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s))
                {
                    bracketSource = s.Trim();
                }
                else if (reqNode is JsonObject existingObj && existingObj.Count > 0)
                {
                    obj["Requirements"] = StatBonusSheetBracketParser.NormalizeStatBonusRequirementsObject(existingObj);
                    return;
                }
                else if (reqNode is JsonArray existingArr && existingArr.Count > 0)
                {
                    var fromArr = StatBonusSheetBracketParser.StatBonusRequirementsArrayToObject(existingArr);
                    if (fromArr != null)
                    {
                        obj["Requirements"] = fromArr;
                        return;
                    }
                }
            }

            if (string.IsNullOrEmpty(bracketSource))
                return;

            var parsed = ParseStatBonusBracketRequirements(bracketSource);
            if (parsed == null || parsed.Count == 0)
            {
                obj.Remove("Requirements");
                return;
            }

            obj["Requirements"] = parsed;
        }

        internal static JsonObject? ParseStatBonusBracketRequirements(string bracketCell) =>
            StatBonusSheetBracketParser.ParseStatBonusBracketRequirements(bracketCell);

        internal static JsonArray? ParseStatBonusBracketMechanics(string bracketCell) =>
            StatBonusSheetBracketParser.ParseStatBonusBracketMechanics(bracketCell);

        internal static string NormalizeStatBonusSheetStatType(string rawKey) =>
            StatBonusSheetBracketParser.NormalizeStatBonusSheetStatType(rawKey);

        private static string? StatBonusRarityCellToString(JsonNode? n)
        {
            if (n is null || IsJsonNodeNullOrMissing(n))
                return null;
            if (n is JsonValue jv)
            {
                if (jv.TryGetValue<string>(out var s))
                    return s;
                if (jv.TryGetValue<double>(out var d))
                    return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            return n.ToJsonString().Trim('"');
        }
    }
}
