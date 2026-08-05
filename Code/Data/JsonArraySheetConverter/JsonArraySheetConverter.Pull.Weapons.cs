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
        private static void NormalizeWeaponsJsonArrayRow(JsonObject obj)
        {
            RemoveWeaponJsonKeyIgnoreCase(obj, "dps");
            RemoveWeaponJsonKeyIgnoreCase(obj, "balance");

            // Google Sheets often use spaced headers (e.g. "Base Damage", "Attack Speed") before canonical camelCase.
            RenameWeaponJsonKeyIfPresent(obj, "baseDamage", "base damage", "basedamage");
            RenameWeaponJsonKeyIfPresent(obj, "attackSpeed", "attack speed", "attackspeed");

            CanonicalizeWeaponField(obj, "baseDamage", preferWholeNumber: true);
            CanonicalizeWeaponField(obj, "attackSpeed", preferWholeNumber: false);
            NormalizeWeaponDamageBonusRange(obj);
            NormalizeTagsFromSheet(obj);
            RenameWeaponJsonKeyIfPresent(obj, "triggerName", "trigger name", "Trigger Name", "trigger");
            obj.Remove("triggerBundles");
            obj.Remove("equipEffects");
            // Case-insensitive cleanup for legacy nested blobs
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (string.Equals(key, "triggerBundles", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(key, "equipEffects", StringComparison.OrdinalIgnoreCase))
                    obj.Remove(key);
            }
        }

        /// <summary>When a row uses a human-readable header instead of JSON camelCase, move its value onto <paramref name="canonicalKey"/>.</summary>
        private static void RenameWeaponJsonKeyIfPresent(JsonObject obj, string canonicalKey, params string[] alternateHeaders)
        {
            if (obj.ContainsKey(canonicalKey))
                return;

            foreach (var alt in alternateHeaders)
            {
                foreach (var key in obj.Select(kvp => kvp.Key).ToList())
                {
                    if (!string.Equals(key, alt, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!obj.TryGetPropertyValue(key, out var node))
                    {
                        obj.Remove(key);
                        return;
                    }

                    obj.Remove(key);
                    if (node is not null && !IsJsonNodeNullOrMissing(node))
                        obj[canonicalKey] = node;
                    return;
                }
            }
        }

        /// <summary>Google Sheet / CSV headers for min damage bonus (case-insensitive); includes common typo <c>Min BOnus</c>.</summary>
        private static readonly string[] WeaponDamageBonusMinHeaderAliases =
        {
            "damageBonusMin", "minDamageBonus", "minBonus", "min bonus",
            "Min BOnus", "Min Bonus"
        };

        private static readonly string[] WeaponDamageBonusMaxHeaderAliases =
        {
            "damageBonusMax", "maxDamageBonus", "maxBonus", "max bonus", "Max Bonus"
        };

        /// <summary>Consumes known sheet/legacy keys and writes canonical <c>damageBonusMin</c> / <c>damageBonusMax</c> (non-negative integers, min â‰¤ max).</summary>
        private static void NormalizeWeaponDamageBonusRange(JsonObject obj)
        {
            int minV = ConsumeWeaponIntByHeaderAliases(obj, WeaponDamageBonusMinHeaderAliases);
            int maxV = ConsumeWeaponIntByHeaderAliases(obj, WeaponDamageBonusMaxHeaderAliases);
            minV = Math.Max(0, minV);
            maxV = Math.Max(0, maxV);
            if (maxV < minV)
                (minV, maxV) = (maxV, minV);

            obj["damageBonusMin"] = JsonValue.Create(minV);
            obj["damageBonusMax"] = JsonValue.Create(maxV);
        }

        /// <summary>Finds the first object key matching any alias (ordinal case-insensitive), removes it, and returns its integer value.</summary>
        private static int ConsumeWeaponIntByHeaderAliases(JsonObject obj, string[] aliases)
        {
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                bool matches = false;
                foreach (var a in aliases)
                {
                    if (string.Equals(key, a, StringComparison.OrdinalIgnoreCase))
                    {
                        matches = true;
                        break;
                    }
                }

                if (!matches)
                    continue;

                int v = 0;
                if (obj.TryGetPropertyValue(key, out var node) && node is not null && !IsJsonNodeNullOrMissing(node)
                    && TryGetWeaponNumericFromJsonNode(node, out double d))
                    v = (int)Math.Round(d, MidpointRounding.AwayFromZero);
                obj.Remove(key);
                return v;
            }

            return 0;
        }

        private static void RemoveWeaponJsonKeyIgnoreCase(JsonObject obj, string logicalName)
        {
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (string.Equals(key, logicalName, StringComparison.OrdinalIgnoreCase))
                    obj.Remove(key);
            }
        }

        /// <summary>Renames matching keys to <paramref name="canonicalKey"/> (camelCase) and coerces values for runtime.</summary>
        private static void CanonicalizeWeaponField(JsonObject obj, string canonicalKey, bool preferWholeNumber)
        {
            string? foundKey = null;
            foreach (var key in obj.Select(kvp => kvp.Key))
            {
                if (string.Equals(key, canonicalKey, StringComparison.OrdinalIgnoreCase))
                {
                    foundKey = key;
                    break;
                }
            }

            if (foundKey is null)
                return;

            if (!obj.TryGetPropertyValue(foundKey, out var node) || node is null || IsJsonNodeNullOrMissing(node))
            {
                if (!string.Equals(foundKey, canonicalKey, StringComparison.Ordinal))
                    obj.Remove(foundKey);
                return;
            }

            JsonNode? coerced = preferWholeNumber ? CoerceWeaponBaseDamageJson(node) : CoerceWeaponAttackSpeedJson(node);
            if (!string.Equals(foundKey, canonicalKey, StringComparison.Ordinal))
                obj.Remove(foundKey);

            if (coerced is null || IsJsonNodeNullOrMissing(coerced))
                obj.Remove(canonicalKey);
            else
                obj[canonicalKey] = coerced;
        }

        private static JsonNode? CoerceWeaponBaseDamageJson(JsonNode node)
        {
            if (!TryGetWeaponNumericFromJsonNode(node, out double d))
                return null;
            int rounded = (int)Math.Max(1, Math.Round(d, MidpointRounding.AwayFromZero));
            return JsonValue.Create(rounded);
        }

        private static JsonNode? CoerceWeaponAttackSpeedJson(JsonNode node)
        {
            if (!TryGetWeaponNumericFromJsonNode(node, out double d))
                return null;
            if (double.IsNaN(d) || double.IsInfinity(d) || d <= 0)
                return null;
            return JsonValue.Create(d);
        }

        private static bool TryGetWeaponNumericFromJsonNode(JsonNode node, out double value)
        {
            value = 0;
            if (node is not JsonValue jv)
                return false;

            if (jv.TryGetValue<double>(out value))
                return true;
            if (jv.TryGetValue<long>(out var l))
            {
                value = l;
                return true;
            }

            if (jv.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s))
            {
                s = s.Trim().TrimEnd('%');
                return double.TryParse(s, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out value);
            }

            return false;
        }
    }
}
