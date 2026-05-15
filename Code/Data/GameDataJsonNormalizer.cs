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
            if (string.Equals(name, GameConstants.ArmorJson, StringComparison.OrdinalIgnoreCase))
                return NormalizeArmorJson(json);

            return json;
        }

        private static string NormalizeArmorJson(string json)
        {
            try
            {
                JsonNode? root = JsonNode.Parse(json);
                if (root is not JsonArray arr)
                    return json;

                foreach (var el in arr)
                {
                    if (el is JsonObject o)
                        NormalizeArmorRow(o);
                }

                return root.ToJsonString();
            }
            catch
            {
                return json;
            }
        }

        /// <summary>
        /// Applies the same per-row rules as <see cref="NormalizeArmorJson"/> for a single armor catalog object
        /// (null stat coercion, sheet <c>attributeRequirements</c> abbrev + <c>requirement value</c> merge, <c>attackSpeed</c> coercion).
        /// Used by <see cref="ArmorDataJsonConverter"/> so armor loads even when JSON is deserialized outside <see cref="JsonLoader"/>.
        /// </summary>
        public static void NormalizeArmorDataRow(JsonObject o)
        {
            if (o == null)
                throw new ArgumentNullException(nameof(o));
            NormalizeArmorRow(o);
        }

        /// <summary>Sheet exports use <c>null</c> for blank stat columns; coerce to <c>0</c> so <see cref="LootGenerator.ArmorData"/> integers deserialize.</summary>
        private static void NormalizeArmorRow(JsonObject o)
        {
            RenameArmorSheetColumnKeysToCanonical(o);

            CoercePropertyToInt(o, "armor");
            CoercePropertyToInt(o, "tier");
            CoercePropertyToInt(o, "strength");
            CoercePropertyToInt(o, "agility");
            CoercePropertyToInt(o, "technique");
            CoercePropertyToInt(o, "intelligence");
            CoercePropertyToInt(o, "hit");
            CoercePropertyToInt(o, "combo");
            CoercePropertyToInt(o, "crit");
            CoercePropertyToInt(o, "extraActionSlots");
            CoercePropertyToInt(o, "extraActionSlotsMin");
            CoercePropertyToInt(o, "extraActionSlotsMax");
            CoercePropertyToInt(o, "minActionBonuses");
            CoercePropertyToInt(o, "requirement value");
            CoercePropertyToDouble(o, "attackSpeed");
            MergeSheetAbbrevAttributeRequirementsIfPresent(o);
        }

        /// <summary>
        /// ARMOR tab authors often use ALL CAPS stat columns and labels like <c># OF ACTION SLOTS</c>; runtime JSON uses camelCase keys.
        /// </summary>
        private static void RenameArmorSheetColumnKeysToCanonical(JsonObject obj)
        {
            EnsureCanonicalArmorKey(obj, "slot", "SLOT");
            EnsureCanonicalArmorKey(obj, "name", "NAME");
            EnsureCanonicalArmorKey(obj, "armor", "ARMOR");
            EnsureCanonicalArmorKey(obj, "tags", "TAGS");
            EnsureCanonicalArmorKey(obj, "tier", "TIER");

            EnsureCanonicalArmorKey(obj, "strength", "STRENGTH", "STR");
            EnsureCanonicalArmorKey(obj, "agility", "AGILITY", "AGI");
            EnsureCanonicalArmorKey(obj, "technique", "TECHNIQUE", "TEC", "TECH");
            EnsureCanonicalArmorKey(obj, "intelligence", "INTELLIGENCE", "INT");
            EnsureCanonicalArmorKey(obj, "hit", "HIT");
            EnsureCanonicalArmorKey(obj, "combo", "COMBO");
            EnsureCanonicalArmorKey(obj, "crit", "CRIT");

            EnsureCanonicalArmorKey(obj, "extraActionSlots", "# OF ACTION SLOTS");
            EnsureCanonicalArmorKey(obj, "minActionBonuses", "# OF BONUS ACTIONS");

            EnsureCanonicalArmorKey(obj, "extraActionSlotsMin", "EXTRA ACTION SLOTS MIN");
            EnsureCanonicalArmorKey(obj, "extraActionSlotsMax", "EXTRA ACTION SLOTS MAX");
            EnsureCanonicalArmorKey(obj, "attackSpeed", "ATTACK SPEED", "ATTACKSPEED");
            EnsureCanonicalArmorKey(obj, "attributeRequirements", "ATTRIBUTE REQUIREMENTS", "ATTRIBUTEREQUIREMENTS");
        }

        private static string CollapseInteriorSpaces(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return "";
            return string.Join(" ", s.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        /// <summary>Moves the first matching alternate header (collapsed whitespace, case-insensitive) onto <paramref name="canonicalKey"/>.</summary>
        private static void EnsureCanonicalArmorKey(JsonObject obj, string canonicalKey, params string[] collapsedAlternateHeaders)
        {
            if (obj.TryGetPropertyValue(canonicalKey, out _))
            {
                foreach (var key in obj.Select(kvp => kvp.Key).ToList())
                {
                    if (string.Equals(key, canonicalKey, StringComparison.Ordinal))
                        continue;
                    string ck = CollapseInteriorSpaces(key);
                    if (collapsedAlternateHeaders.Any(a => string.Equals(ck, a, StringComparison.OrdinalIgnoreCase)))
                        obj.Remove(key);
                }

                return;
            }

            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                string ck = CollapseInteriorSpaces(key);
                if (!collapsedAlternateHeaders.Any(a => string.Equals(ck, a, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (!obj.TryGetPropertyValue(key, out var node))
                    return;

                obj.Remove(key);
                if (node is not null)
                    obj[canonicalKey] = node;
                return;
            }
        }

        /// <summary>
        /// Sheet rows often use <c>attributeRequirements</c> as a stat abbrev string plus <c>requirement value</c>;
        /// runtime models expect a <c>Dictionary&lt;string,int&gt;</c> JSON object (same as weapons).
        /// When <c>attributeRequirements</c> is already a dictionary, dictionary keys are still canonicalized
        /// (abbreviations like <c>STR</c> and known typos like <c>techinque</c> map to <c>strength</c> /
        /// <c>technique</c>) so <see cref="Items.Item.GetEffectiveValueForRequirementKey"/> recognizes them.
        /// </summary>
        private static void MergeSheetAbbrevAttributeRequirementsIfPresent(JsonObject o)
        {
            JsonNode? attrNode = FindPropertyIgnoreCase(o, "attributeRequirements");
            if (attrNode is JsonObject obj)
            {
                CanonicalizeAttributeRequirementKeys(obj);
                return;
            }
            if (attrNode is not JsonValue strVal || !strVal.TryGetValue<string>(out var abbrev) ||
                string.IsNullOrWhiteSpace(abbrev))
                return;

            int reqVal = CoerceInt(FindPropertyIgnoreCase(o, "requirement value"));
            string statKey = MapSheetStatAbbrevToRequirementKey(abbrev.Trim());

            RemovePropertyIgnoreCase(o, "attributeRequirements");
            var dict = new JsonObject { [statKey] = JsonValue.Create(reqVal) };
            o["attributeRequirements"] = dict;
        }

        /// <summary>
        /// Rewrites each key of an <c>attributeRequirements</c> object to the canonical
        /// <c>strength</c>/<c>agility</c>/<c>technique</c>/<c>intelligence</c> form used by
        /// <see cref="Items.Item.GetEffectiveValueForRequirementKey"/>. Unrecognized keys are passed through
        /// lowercased so JSON authors can still see and fix bad data, but known abbreviations and the
        /// historical <c>techinque</c> typo round-trip to the canonical key.
        /// </summary>
        private static void CanonicalizeAttributeRequirementKeys(JsonObject reqs)
        {
            if (reqs == null || reqs.Count == 0)
                return;

            var pairs = reqs.Select(kvp => (kvp.Key, kvp.Value)).ToList();
            bool changed = false;
            var normalized = new List<(string Key, JsonNode? Value)>(pairs.Count);
            foreach (var (key, value) in pairs)
            {
                string mapped = MapSheetStatAbbrevToRequirementKey(key ?? string.Empty);
                if (!string.Equals(mapped, key, StringComparison.Ordinal))
                    changed = true;
                normalized.Add((mapped, value));
            }

            if (!changed)
                return;

            foreach (var key in pairs.Select(p => p.Key).ToList())
                reqs.Remove(key);

            foreach (var (key, value) in normalized)
            {
                if (reqs.ContainsKey(key))
                    continue;
                reqs[key] = value?.DeepClone();
            }
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
            MergeSheetAbbrevAttributeRequirementsIfPresent(o);
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
        /// Also parses a bracket cell for the new <c>Requirements</c> column into a canonical lowercase dictionary.
        /// </summary>
        private static void NormalizeStatBonusRow(JsonObject o)
        {
            try
            {
                NormalizeStatBonusMechanicsField(o);
                NormalizeStatBonusRequirementsField(o);
            }
            finally
            {
                JsonArraySheetConverter.RemoveStatBonusUnknownJsonKeys(o);
            }
        }

        private static void NormalizeStatBonusMechanicsField(JsonObject o)
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

        /// <summary>
        /// If <c>Requirements</c> is authored as a string (sheet bracket cell), parse it into the canonical object form
        /// expected by <see cref="Items.StatBonus.Requirements"/>; case-insensitive aliases are not needed because
        /// JSON files use the canonical key directly.
        /// </summary>
        private static void NormalizeStatBonusRequirementsField(JsonObject o)
        {
            if (!o.TryGetPropertyValue("Requirements", out var node) || node is null)
                return;

            if (node is JsonValue jv && jv.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s))
            {
                var parsed = JsonArraySheetConverter.ParseStatBonusBracketRequirements(s.Trim());
                if (parsed != null && parsed.Count > 0)
                    o["Requirements"] = parsed;
                else
                    o.Remove("Requirements");
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

        /// <summary>Normalizes a single PREFIX / modifications CSV row object (sheet columns A–I and legacy Min/Max).</summary>
        public static void NormalizeModificationsImportRow(JsonObject row)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));
            NormalizeModificationRow(row);
        }

        private static void NormalizeModificationRow(JsonObject o)
        {
            MergeModificationSheetColumnsAttributeRequirement(o);
            MergeSheetAbbrevAttributeRequirementsIfPresent(o);
            if (FindPropertyIgnoreCase(o, "attributeRequirements") is JsonObject reqObj)
                CanonicalizeAttributeRequirementKeys(reqObj);

            ApplyModificationValueColumnToMinMax(o);
            CoercePropertyToInt(o, "DiceResult");
            CoercePropertyToDouble(o, "MinValue");
            CoercePropertyToDouble(o, "MaxValue");
            CoercePropertyToDouble(o, "RolledValue");
        }

        /// <summary>
        /// PREFIX tab columns <c>ATTRIBUTE REQUIREMENT</c> + <c>REQUIREMENT VALUE</c> (case-insensitive) merge into
        /// <c>attributeRequirements</c> object, same canonical keys as weapons/armor.
        /// </summary>
        private static void MergeModificationSheetColumnsAttributeRequirement(JsonObject o)
        {
            JsonNode? abbrevNode = FindPropertyIgnoreCase(o, "ATTRIBUTE REQUIREMENT");
            if (abbrevNode is not JsonValue jv || !jv.TryGetValue<string>(out var abbrevRaw) ||
                string.IsNullOrWhiteSpace(abbrevRaw))
                return;

            JsonNode? reqNode = FindPropertyIgnoreCase(o, "REQUIREMENT VALUE");
            if (reqNode == null || IsJsonNull(reqNode))
                return;

            int reqVal = CoerceInt(reqNode);
            string statKey = MapSheetStatAbbrevToRequirementKey(abbrevRaw.Trim());
            RemovePropertyIgnoreCase(o, "ATTRIBUTE REQUIREMENT");
            RemovePropertyIgnoreCase(o, "REQUIREMENT VALUE");

            JsonNode? existing = FindPropertyIgnoreCase(o, "attributeRequirements");
            if (existing is JsonObject eo)
            {
                int prior = CoerceInt(FindPropertyIgnoreCase(eo, statKey));
                eo[statKey] = JsonValue.Create(Math.Max(prior, reqVal));
                CanonicalizeAttributeRequirementKeys(eo);
            }
            else
            {
                RemovePropertyIgnoreCase(o, "attributeRequirements");
                o["attributeRequirements"] = new JsonObject { [statKey] = JsonValue.Create(reqVal) };
            }
        }

        /// <summary>Sheet column <c>value</c> (case-insensitive) sets both <c>MinValue</c> and <c>MaxValue</c>, then is removed.</summary>
        private static void ApplyModificationValueColumnToMinMax(JsonObject o)
        {
            JsonNode? vn = FindPropertyIgnoreCase(o, "value");
            if (vn == null || IsJsonNull(vn))
                return;

            double v = CoerceDouble(vn);
            SetPropertyIgnoreCase(o, "MinValue", JsonValue.Create(v));
            SetPropertyIgnoreCase(o, "MaxValue", JsonValue.Create(v));
            RemovePropertyIgnoreCase(o, "value");
        }

        private static double CoerceDouble(JsonNode? n)
        {
            if (n == null || IsJsonNull(n))
                return 0;
            if (n is JsonValue jv)
            {
                if (jv.TryGetValue(out double d))
                    return d;
                if (jv.TryGetValue(out long lng))
                    return lng;
                if (jv.TryGetValue(out int i))
                    return i;
                if (jv.TryGetValue(out string? s) && !string.IsNullOrEmpty(s) &&
                    double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double p))
                    return p;
            }

            return 0;
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

                if (jv.TryGetValue(out long lng))
                {
                    SetPropertyIgnoreCase(o, propertyName, JsonValue.Create((int)lng));
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

                if (jv.TryGetValue(out long lng))
                {
                    SetPropertyIgnoreCase(o, propertyName, JsonValue.Create((double)lng));
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
                if (jv.TryGetValue(out long lng))
                    return (int)lng;
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

        /// <summary>
        /// Maps weapon / armor sheet stat abbreviations and known JSON typos to canonical
        /// <see cref="Items.Item.MeetsRequirements"/> dictionary keys. Unknown values fall through as
        /// the lowercased input so authors can still see bad data in tooltips.
        /// </summary>
        private static string MapSheetStatAbbrevToRequirementKey(string raw) =>
            Item.CanonicalizeAttributeRequirementKey(raw);
    }
}
