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
        /// <summary>Sets <paramref name="headerRow"/> and <paramref name="firstDataRow"/> for ENEMIES two-row headers or single-row fallback.</summary>
        private static void TryGetEnemyImportHeaderRowAndFirstDataIndex(
            List<string[]> table,
            GameDataTabularSheetKind kind,
            out string[] headerRow,
            out int firstDataRow)
        {
            headerRow = table[0];
            firstDataRow = 1;
            if (kind != GameDataTabularSheetKind.Enemies || table.Count < 2)
                return;

            var row0 = table[0];
            var row1 = table[1];
            // Avoid treating legacy single-row CSV (column "overrides" JSON blob) as a category band.
            if (!EnemySheetTwoRowHeaderFormat(row0, row1))
                return;

            int width = Math.Max(row0.Length, row1.Length);
            var combined = new string[width];
            string carryCategory = "";
            var seenBaseStatLeaves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < width; i++)
            {
                string cat = i < row0.Length ? row0[i]?.Trim() ?? "" : "";
                string sub = i < row1.Length ? row1[i]?.Trim() ?? "" : "";
                // Some sheets export "merged" category headers: the first column in a block has the category label
                // and the remaining columns are blank. Carry the last seen category across blanks so the short
                // headers can still be combined into dotted keys.
                if (!string.IsNullOrWhiteSpace(cat))
                    carryCategory = cat;
                else if (!string.IsNullOrWhiteSpace(carryCategory))
                    cat = carryCategory;

                // Merged category bands can end before the growth label column when name/archetype precede stat blocks.
                // When the same stat leaf (e.g. strength) appears again under a carried "base attributes" band, treat
                // the column as growth so growthPerLevel.* keys align with the data row.
                if (string.Equals(cat, EnemySheetCategoryBaseAttributes, StringComparison.OrdinalIgnoreCase)
                    && EnemyImportSubHeaderIsStatLeaf(sub)
                    && seenBaseStatLeaves.Contains(NormalizeEnemyStatLeafHeader(sub)))
                {
                    cat = EnemySheetCategoryGrowth;
                }
                else if (string.Equals(cat, EnemySheetCategoryGrowth, StringComparison.OrdinalIgnoreCase)
                         && EnemyImportSubHeaderIsStatLeaf(sub)
                         && !seenBaseStatLeaves.Contains(NormalizeEnemyStatLeafHeader(sub)))
                {
                    // Excel-merged sheets often place the growth band label on the last base-attribute stat column.
                    cat = EnemySheetCategoryBaseAttributes;
                    seenBaseStatLeaves.Add(NormalizeEnemyStatLeafHeader(sub));
                }
                else if (string.Equals(cat, EnemySheetCategoryBaseAttributes, StringComparison.OrdinalIgnoreCase)
                         && EnemyImportSubHeaderIsStatLeaf(sub))
                {
                    seenBaseStatLeaves.Add(NormalizeEnemyStatLeafHeader(sub));
                }
                else if (string.Equals(cat, EnemySheetCategoryHealth, StringComparison.OrdinalIgnoreCase)
                         && string.Equals(NormalizeEnemyStatLeafHeader(sub), "intelligence", StringComparison.Ordinal))
                {
                    // HEALTH band label can overlap the final growthPerLevel.intelligence column in merged exports.
                    cat = EnemySheetCategoryGrowth;
                }

                combined[i] = CombineEnemyImportHeaderCell(cat, sub);
            }

            headerRow = combined;
            firstDataRow = 2;
        }

        /// <summary>
        /// True when row 0 is the category band (blank over general columns, repeated group labels) and row 1 is short column names
        /// (contains <c>name</c> somewhere), not a data row.
        /// </summary>
        private static bool EnemySheetTwoRowHeaderFormat(string[] row0, string[] row1)
        {
            if (row1.Length == 0)
                return false;
            if (!EnemySheetRowContainsNameHeader(row1))
                return false;
            if (!EnemySheetRowLooksLikeCategoryBand(row0) || EnemySheetRowHasDottedStatKeys(row0))
                return false;
            return true;
        }

        private static bool EnemySheetRowContainsNameHeader(string[] row)
        {
            foreach (var cell in row)
            {
                if (string.Equals(cell?.Trim(), "name", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool EnemySheetRowLooksLikeCategoryBand(string[] row)
        {
            foreach (var cell in row)
            {
                string t = cell?.Trim() ?? "";
                if (t.Length == 0)
                    continue;
                if (string.Equals(t, EnemySheetCategoryOverrides, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t, EnemySheetCategoryBaseAttributes, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t, EnemySheetCategoryGrowth, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t, EnemySheetCategoryHealth, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool EnemySheetRowHasDottedStatKeys(string[] row)
        {
            foreach (var cell in row)
            {
                string t = cell?.Trim() ?? "";
                if (t.Contains("overrides.", StringComparison.Ordinal)
                    || t.Contains("baseAttributes.", StringComparison.Ordinal)
                    || t.Contains("growthPerLevel.", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static bool EnemyImportSubHeaderIsStatLeaf(string subHeader)
        {
            string leaf = NormalizeEnemyStatLeafHeader(subHeader);
            return leaf is "strength" or "agility" or "technique" or "intelligence";
        }

        private static string CombineEnemyImportHeaderCell(string category, string subHeader)
        {
            category = category.Trim().TrimStart('\uFEFF');
            subHeader = subHeader.Trim().TrimStart('\uFEFF');
            if (subHeader.Length == 0)
                return "";

            // Sheet row-0 blanks reuse the last category label (Excel merges). Columns after growthPerLevel.*
            // (actions, isLiving, …) have blank row-0 but must not inherit "growth" or actions land in growthPerLevel.actions.
            if (EnemyImportSubHeaderIsRootEnemyColumn(subHeader))
                return NormalizeEnemyRootHeader(subHeader);

            if (category.Length == 0)
                return subHeader;

            if (string.Equals(category, EnemySheetCategoryOverrides, StringComparison.OrdinalIgnoreCase))
                return "overrides." + subHeader;

            if (string.Equals(category, EnemySheetCategoryBaseAttributes, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(subHeader, "healthPercent", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(subHeader, "baseHealth", StringComparison.OrdinalIgnoreCase))
                    return EnemyJsonHealthNormalizer.HealthPercentKey;
                return "baseAttributes." + NormalizeEnemyStatLeafHeader(subHeader);
            }

            if (string.Equals(category, EnemySheetCategoryGrowth, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(subHeader, "healthGrowthPercent", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(subHeader, "healthGrowthPerLevel", StringComparison.OrdinalIgnoreCase))
                    return EnemyJsonHealthNormalizer.HealthGrowthPercentKey;
                return "growthPerLevel." + NormalizeEnemyStatLeafHeader(subHeader);
            }

            if (string.Equals(category, EnemySheetCategoryHealth, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(subHeader, "healthPercent", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(subHeader, "baseHealth", StringComparison.OrdinalIgnoreCase))
                    return EnemyJsonHealthNormalizer.HealthPercentKey;
                if (string.Equals(subHeader, "healthGrowthPercent", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(subHeader, "healthGrowthPerLevel", StringComparison.OrdinalIgnoreCase))
                    return EnemyJsonHealthNormalizer.HealthGrowthPercentKey;
            }

            return NormalizeEnemyRootHeader(subHeader);
        }

        private static string NormalizeEnemyStatLeafHeader(string subHeader) =>
            subHeader.Trim().ToLowerInvariant();

        private static string NormalizeEnemyRootHeader(string subHeader)
        {
            string t = subHeader.Trim();
            return EnemyRootHeaderCanonicalNames.TryGetValue(t, out var canon) ? canon : t;
        }

        private static readonly Dictionary<string, string> EnemyRootHeaderCanonicalNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["region"] = "region",
                ["biome"] = "biome",
                ["location"] = "location",
                ["rarity"] = "rarity",
                ["name"] = "name",
                ["tags"] = "tags",
                ["archetype"] = "archetype",
                ["actions"] = "actions",
                ["isLiving"] = "isLiving",
                ["description"] = "description",
                ["colorOverride"] = "colorOverride",
                ["healthPercent"] = EnemyJsonHealthNormalizer.HealthPercentKey,
                ["baseHealth"] = EnemyJsonHealthNormalizer.HealthPercentKey,
                ["healthGrowthPercent"] = EnemyJsonHealthNormalizer.HealthGrowthPercentKey,
                ["healthGrowthPerLevel"] = EnemyJsonHealthNormalizer.HealthGrowthPercentKey
            };

        /// <summary>Short headers on ENEMIES row-1 that are top-level JSON fields, not growth/baseAttributes sub-keys.</summary>
        private static bool EnemyImportSubHeaderIsRootEnemyColumn(string subHeader) =>
            EnemyRootHeaderCanonicalNames.ContainsKey(subHeader.Trim());

        /// <summary>
        /// Merges dotted headers (<c>overrides.health</c>, <c>baseAttributes.strength</c>, …) into nested objects and removes those keys.
        /// Flat values override the same keys on an existing legacy <c>overrides</c> / <c>baseAttributes</c> / <c>growthPerLevel</c> JSON cell.
        /// </summary>
        private static void MergeEnemyFlatColumnsToNested(JsonObject obj)
        {
            var buckets = new Dictionary<string, Dictionary<string, JsonNode?>>(StringComparer.Ordinal)
            {
                ["overrides"] = new Dictionary<string, JsonNode?>(StringComparer.Ordinal),
                ["baseAttributes"] = new Dictionary<string, JsonNode?>(StringComparer.Ordinal),
                ["growthPerLevel"] = new Dictionary<string, JsonNode?>(StringComparer.Ordinal)
            };

            var keysToRemove = new List<string>();
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                int dot = key.IndexOf('.', StringComparison.Ordinal);
                if (dot <= 0)
                    continue;
                string prefix = key[..dot];
                string suffix = key[(dot + 1)..];
                if (!buckets.TryGetValue(prefix, out var bucket))
                    continue;

                if (!obj.TryGetPropertyValue(key, out var valNode))
                    continue;

                keysToRemove.Add(key);
                if (valNode is null || IsJsonNodeNullOrMissing(valNode))
                    continue;
                bucket[suffix] = valNode;
            }

            foreach (var key in keysToRemove)
                obj.Remove(key);

            foreach (var (prefix, bucket) in buckets)
            {
                if (bucket.Count == 0)
                    continue;

                var merged = new JsonObject();
                foreach (var (sk, sv) in bucket)
                {
                    if (sv is null || IsJsonNodeNullOrMissing(sv))
                        continue;
                    merged[sk] = sv;
                }

                if (merged.Count == 0)
                    continue;

                if (obj.TryGetPropertyValue(prefix, out var existing) && existing is JsonObject eo)
                {
                    foreach (var (sk, sv) in merged)
                    {
                        if (sv is null || IsJsonNodeNullOrMissing(sv))
                            continue;
                        eo[sk] = sv;
                    }
                }
                else
                {
                    obj[prefix] = merged;
                }
            }
        }

        /// <summary>
        /// When <c>actions</c> is a plain string (not JSON), treat <c>|</c> as delimiter so sheet authors can write
        /// <c>JAB|TAUNT</c> instead of a JSON array.
        /// </summary>
        private static void NormalizeEnemyJsonArrayRow(JsonObject obj)
        {
            CanonicalizeEnemyImportRowKeys(obj);
            PromoteMisplacedEnemyHealthFields(obj);
            StripEnemyLegacyRootStatsWhenNestedPresent(obj);
            NormalizeEnemyActionsFromSheet(obj);
            NormalizeTagsFromSheet(obj);
            NormalizeEnemyArchetypeFromSheet(obj);
            EnemyJsonHealthNormalizer.NormalizeHealthPercentFields(obj);
        }

        private static void NormalizeEnemyArchetypeFromSheet(JsonObject obj)
        {
            if (!obj.TryGetPropertyValue("archetype", out var node) || node == null)
                return;

            var raw = node.GetValue<string>()?.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                obj.Remove("archetype");
                return;
            }

            var canonical = TagDefinitions.CanonicalizeEnemyArchetype(raw);
            if (canonical != null && TagDefinitions.IsValidEnemyArchetype(canonical))
                obj["archetype"] = canonical;
            else
                obj["archetype"] = raw;
        }

        /// <summary>Moves HP keys mistakenly nested under <c>growthPerLevel</c> / <c>baseAttributes</c> to root (legacy sheet bands).</summary>
        private static void PromoteMisplacedEnemyHealthFields(JsonObject obj)
        {
            static void Hoist(JsonObject root, JsonObject nested, string nestedKey, string rootKey)
            {
                if (!nested.TryGetPropertyValue(nestedKey, out var val) || val is null || IsJsonNodeNullOrMissing(val))
                    return;
                if (!root.ContainsKey(rootKey))
                    root[rootKey] = val.DeepClone();
                nested.Remove(nestedKey);
            }

            if (obj.TryGetPropertyValue("growthPerLevel", out var gpNode) && gpNode is JsonObject gp)
            {
                Hoist(obj, gp, "baseHealth", EnemyJsonHealthNormalizer.HealthPercentKey);
                Hoist(obj, gp, "healthGrowthPerLevel", EnemyJsonHealthNormalizer.HealthGrowthPercentKey);
                Hoist(obj, gp, "healthPercent", EnemyJsonHealthNormalizer.HealthPercentKey);
                Hoist(obj, gp, "healthGrowthPercent", EnemyJsonHealthNormalizer.HealthGrowthPercentKey);
            }

            if (obj.TryGetPropertyValue("baseAttributes", out var baNode) && baNode is JsonObject ba)
            {
                Hoist(obj, ba, "baseHealth", EnemyJsonHealthNormalizer.HealthPercentKey);
                Hoist(obj, ba, "healthPercent", EnemyJsonHealthNormalizer.HealthPercentKey);
            }
        }

        /// <summary>Drops mistaken root <c>strength</c>/<c>agility</c>/… when nested stat objects were already merged.</summary>
        private static void StripEnemyLegacyRootStatsWhenNestedPresent(JsonObject obj)
        {
            bool hasBase = obj.ContainsKey("baseAttributes");
            bool hasGrowth = obj.ContainsKey("growthPerLevel");
            if (!hasBase && !hasGrowth)
                return;

            foreach (string stat in EnemyLegacyRootStatPropertyNames)
                obj.Remove(stat);
        }

        /// <summary>Maps mixed-case sheet headers to lowercase camelCase JSON keys before nested merge.</summary>
        private static void CanonicalizeEnemyImportRowKeys(JsonObject obj)
        {
            var renames = new List<(string oldKey, string newKey)>();
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                int dot = key.IndexOf('.', StringComparison.Ordinal);
                if (dot > 0)
                {
                    string prefix = key[..dot];
                    string suffix = key[(dot + 1)..];
                    string canonPrefix = prefix.Equals("baseAttributes", StringComparison.OrdinalIgnoreCase)
                        ? "baseAttributes"
                        : prefix.Equals("growthPerLevel", StringComparison.OrdinalIgnoreCase)
                            ? "growthPerLevel"
                            : prefix.Equals("overrides", StringComparison.OrdinalIgnoreCase)
                                ? "overrides"
                                : prefix;
                    string canonKey = canonPrefix + "." + suffix.Trim().ToLowerInvariant();
                    if (!string.Equals(key, canonKey, StringComparison.Ordinal))
                        renames.Add((key, canonKey));
                    continue;
                }

                string canonRoot = NormalizeEnemyRootHeader(key);
                if (!string.Equals(key, canonRoot, StringComparison.Ordinal))
                    renames.Add((key, canonRoot));
            }

            foreach (var (oldKey, newKey) in renames)
            {
                if (!obj.TryGetPropertyValue(oldKey, out var val))
                    continue;
                if (obj.ContainsKey(newKey))
                    obj.Remove(oldKey);
                else
                {
                    obj.Remove(oldKey);
                    if (val is not null)
                        obj[newKey] = val;
                }
            }
        }

        /// <summary>
        /// When <c>actions</c> is a plain string (not JSON), treat <c>|</c> as delimiter so sheet authors can write
        /// <c>JAB|TAUNT</c> instead of a JSON array.
        /// </summary>
        private static void NormalizeEnemyActionsFromSheet(JsonObject obj)
        {
            if (!obj.TryGetPropertyValue("actions", out var node) || node is null)
                return;

            if (node is JsonArray)
                return;

            if (node is not JsonValue jv || !jv.TryGetValue<string>(out var s) || string.IsNullOrWhiteSpace(s))
                return;

            // ENEMIES sheet often uses one of:
            // - Pipe list: JAB|TAUNT
            // - JSON array: ["JAB","TAUNT"]
            // - Quoted CSV-style list in a single cell:
            //     "JAB",
            //     "TAUNT"
            // Normalize all non-JSON strings into a canonical string array of action names.
            s = s.Trim();

            char[] splitChars = s.Contains('|', StringComparison.Ordinal)
                ? new[] { '|' }
                : new[] { ',', '\n', '\r', ';' };

            var parts = s.Split(splitChars, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => p.Trim().Trim('"'))
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();

            if (parts.Length == 0)
                return;

            var arr = new JsonArray();
            foreach (var p in parts)
                arr.Add(p);
            obj["actions"] = arr;
        }
    }
}
