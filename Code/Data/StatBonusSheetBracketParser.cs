using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>Stat bonus bracket parsing and formatting for SUFFIXES sheet round-trip.</summary>
    internal static class StatBonusSheetBracketParser
    {
        internal static string FormatStatBonusRequirementsForSheetCell(JsonElement obj)
        {
            if (obj.ValueKind != JsonValueKind.Object)
                return "";

            var parts = new List<string>();
            foreach (var prop in obj.EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(prop.Name))
                    continue;
                int value;
                if (prop.Value.ValueKind == JsonValueKind.Number)
                {
                    if (prop.Value.TryGetInt32(out int iv))
                        value = iv;
                    else if (prop.Value.TryGetDouble(out double dv))
                        value = (int)Math.Round(dv);
                    else
                        continue;
                }
                else
                {
                    continue;
                }
                parts.Add($"{prop.Name}:{value.ToString(CultureInfo.InvariantCulture)}");
            }

            return parts.Count == 0 ? "" : "[" + string.Join(",", parts) + "]";
        }

        internal static void RemoveStatBonusUnknownJsonKeys(JsonObject obj)
        {
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (!JsonArraySheetSchemas.StatBonusAuthorizedJsonKeys.Contains(key))
                    obj.Remove(key);
            }
        }

        internal static JsonObject? ParseStatBonusBracketRequirements(string bracketCell)
        {
            string t = bracketCell.Trim();
            if (t.Length < 3 || t[0] != '[' || t[^1] != ']')
                return null;

            string inner = t[1..^1].Trim();
            if (inner.Length == 0)
                return null;

            var segments = inner.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var aggregated = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var segment in segments)
            {
                int colon = segment.LastIndexOf(':');
                if (colon <= 0 || colon >= segment.Length - 1)
                    continue;

                string rawKey = segment[..colon].Trim();
                string rawVal = segment[(colon + 1)..].Trim();
                if (rawKey.Length == 0)
                    continue;

                if (!double.TryParse(rawVal, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    continue;

                string canonical = Item.CanonicalizeAttributeRequirementKey(rawKey);
                if (string.IsNullOrEmpty(canonical))
                    continue;

                int intValue = (int)Math.Round(value);
                if (aggregated.TryGetValue(canonical, out int cur))
                    aggregated[canonical] = Math.Max(cur, intValue);
                else
                    aggregated[canonical] = intValue;
            }

            if (aggregated.Count == 0)
                return null;

            var result = new JsonObject();
            foreach (var kv in aggregated)
                result[kv.Key] = JsonValue.Create(kv.Value);
            return result;
        }

        internal static JsonArray? ParseStatBonusBracketMechanics(string bracketCell)
        {
            string t = bracketCell.Trim();
            if (t.Length < 3 || t[0] != '[' || t[^1] != ']')
                return null;

            string inner = t[1..^1].Trim();
            if (inner.Length == 0)
                return null;

            var segments = inner.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var arr = new JsonArray();
            foreach (var segment in segments)
            {
                int colon = segment.LastIndexOf(':');
                if (colon <= 0 || colon >= segment.Length - 1)
                    continue;

                string rawKey = segment[..colon].Trim();
                string rawVal = segment[(colon + 1)..].Trim();
                if (rawKey.Length == 0)
                    continue;

                if (!double.TryParse(rawVal, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    continue;

                var pair = new JsonObject
                {
                    ["StatType"] = JsonValue.Create(NormalizeStatBonusSheetStatType(rawKey)),
                    ["Value"] = JsonValue.Create(value)
                };
                arr.Add(pair);
            }

            return arr.Count > 0 ? arr : null;
        }

        internal static string NormalizeStatBonusSheetStatType(string rawKey)
        {
            if (string.IsNullOrWhiteSpace(rawKey))
                return "";

            string collapsed = string.Join(" ", rawKey.Trim()
                .Replace('_', ' ')
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            string upper = collapsed.ToUpperInvariant();

            return upper switch
            {
                "ARMOR" => "Armor",
                "HEALTH" => "Health",
                "MAX HEALTH" => "Health",
                "MAXHEALTH" => "Health",
                "MAX ATTACK SPEED" => "AttackSpeed",
                "MAXATTACKSPEED" => "AttackSpeed",
                "ATTACK SPEED" => "AttackSpeed",
                "ATTACKSPEED" => "AttackSpeed",
                "SPEED" => "AttackSpeed",
                "DAMAGE" => "Damage",
                "BASE DAMAGE" => "Damage",
                "BASEDAMAGE" => "Damage",
                "WEAPON DAMAGE" => "Damage",
                "WEAPONDAMAGE" => "Damage",
                "HEALTH REGEN" => "HealthRegen",
                "HEALTHREGEN" => "HealthRegen",
                "ROLL" => "RollBonus",
                "ROLLBONUS" => "RollBonus",
                "ROLL BONUS" => "RollBonus",
                "MAGICFIND" => "MagicFind",
                "MAGIC FIND" => "MagicFind",
                "HIT" => "HIT",
                "COMBO" => "COMBO",
                "CRIT" => "CRIT",
                "ACCURACY" => "ACCURACY",
                "STR" or "STRENGTH" => "STR",
                "AGI" or "AGILITY" => "AGI",
                "TEC" or "TECHNIQUE" => "TEC",
                "INT" or "INTELLIGENCE" => "INT",
                "ALL" => "ALL",
                _ => collapsed
            };
        }

        internal static JsonObject NormalizeStatBonusRequirementsObject(JsonObject existing)
        {
            var aggregated = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in existing)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || kvp.Value is not JsonValue jv)
                    continue;
                int value;
                if (jv.TryGetValue<int>(out int iv))
                    value = iv;
                else if (jv.TryGetValue<double>(out double dv))
                    value = (int)Math.Round(dv);
                else
                    continue;

                string canonical = Item.CanonicalizeAttributeRequirementKey(kvp.Key);
                if (string.IsNullOrEmpty(canonical))
                    continue;

                if (aggregated.TryGetValue(canonical, out int cur))
                    aggregated[canonical] = Math.Max(cur, value);
                else
                    aggregated[canonical] = value;
            }

            var result = new JsonObject();
            foreach (var kv in aggregated)
                result[kv.Key] = JsonValue.Create(kv.Value);
            return result;
        }

        internal static JsonObject? StatBonusRequirementsArrayToObject(JsonArray arr)
        {
            var aggregated = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var el in arr)
            {
                if (el is not JsonObject o)
                    continue;
                string? key = null;
                int? value = null;
                foreach (var kvp in o)
                {
                    if (kvp.Value is not JsonValue jv)
                        continue;
                    if (key == null && (string.Equals(kvp.Key, "Key", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(kvp.Key, "StatType", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(kvp.Key, "Attribute", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (jv.TryGetValue<string>(out var s))
                            key = s;
                    }
                    else if (value == null && (string.Equals(kvp.Key, "Value", StringComparison.OrdinalIgnoreCase) ||
                                                string.Equals(kvp.Key, "Required", StringComparison.OrdinalIgnoreCase) ||
                                                string.Equals(kvp.Key, "Threshold", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (jv.TryGetValue<int>(out int iv))
                            value = iv;
                        else if (jv.TryGetValue<double>(out double dv))
                            value = (int)Math.Round(dv);
                    }
                }

                if (string.IsNullOrWhiteSpace(key) || value == null)
                    continue;
                string canonical = Item.CanonicalizeAttributeRequirementKey(key);
                if (string.IsNullOrEmpty(canonical))
                    continue;

                if (aggregated.TryGetValue(canonical, out int cur))
                    aggregated[canonical] = Math.Max(cur, value.Value);
                else
                    aggregated[canonical] = value.Value;
            }

            if (aggregated.Count == 0)
                return null;

            var result = new JsonObject();
            foreach (var kv in aggregated)
                result[kv.Key] = JsonValue.Create(kv.Value);
            return result;
        }

        internal static void NormalizeStatBonusMechanicsArrayStatTypes(JsonArray arr)
        {
            foreach (var el in arr)
            {
                if (el is not JsonObject o)
                    continue;
                if (!o.TryGetPropertyValue("StatType", out var stNode) || stNode is not JsonValue stv || !stv.TryGetValue<string>(out var raw) || raw == null)
                    continue;
                string canon = NormalizeStatBonusSheetStatType(raw);
                o["StatType"] = JsonValue.Create(canon);
            }
        }
    }
}
