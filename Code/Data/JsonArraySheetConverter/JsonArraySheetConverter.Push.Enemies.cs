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
        private static List<IList<object>> BuildEnemyPushValueRows(string jsonFileText)
        {
            using var doc = JsonDocument.Parse(jsonFileText);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Expected a JSON array at the root.");

            var canonical = EnemiesCanonicalHeaders.ToList();
            var flatCanon = new HashSet<string>(canonical, StringComparer.OrdinalIgnoreCase);
            var nestedParents = new HashSet<string>(JsonArraySheetSchemas.EnemyNestedObjectNames, StringComparer.OrdinalIgnoreCase);
            var extraKeys = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                foreach (var p in el.EnumerateObject())
                {
                    if (flatCanon.Contains(p.Name))
                        continue;
                    if (nestedParents.Contains(p.Name))
                        continue;
                    if (EnemyLegacyRootStatPropertyNames.Contains(p.Name))
                        continue;
                    extraKeys.Add(p.Name);
                }
            }

            var headers = new List<string>(canonical);
            headers.AddRange(extraKeys);

            var rows = new List<IList<object>>();
            rows.Add(headers.Select(EnemyCategoryLabelForCanonicalHeader).Select(s => (object)s).ToList());
            rows.Add(headers.Select(EnemyShortHeaderForCanonicalHeader).Select(s => (object)s).ToList());

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                var row = new List<object>();
                foreach (var h in headers)
                    row.Add(GetEnemyPushCell(el, h));
                rows.Add(row);
            }

            return rows;
        }

        private static string EnemyCategoryLabelForCanonicalHeader(string canonicalHeader)
        {
            if (canonicalHeader.StartsWith("baseAttributes.", StringComparison.Ordinal))
                return EnemySheetCategoryBaseAttributes;
            if (canonicalHeader.StartsWith("growthPerLevel.", StringComparison.Ordinal))
                return EnemySheetCategoryGrowth;
            if (string.Equals(canonicalHeader, "baseHealth", StringComparison.OrdinalIgnoreCase)
                || string.Equals(canonicalHeader, "healthGrowthPerLevel", StringComparison.OrdinalIgnoreCase))
                return EnemySheetCategoryHealth;
            return "";
        }

        /// <summary>Second header row: dotted keys with group prefix removed for overrides / baseAttributes / growthPerLevel.</summary>
        private static string EnemyShortHeaderForCanonicalHeader(string canonicalHeader)
        {
            if (canonicalHeader.StartsWith("baseAttributes.", StringComparison.Ordinal))
                return canonicalHeader["baseAttributes.".Length..];
            if (canonicalHeader.StartsWith("growthPerLevel.", StringComparison.Ordinal))
                return canonicalHeader["growthPerLevel.".Length..];
            return canonicalHeader;
        }

        private static readonly HashSet<string> EnemyLegacyRootStatPropertyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "strength", "agility", "technique", "intelligence"
        };

        private static string GetEnemyPushCell(JsonElement el, string header)
        {
            if (header.StartsWith("baseAttributes.", StringComparison.Ordinal))
            {
                if (TryGetPropertyPathCaseInsensitive(el, header, out var nested))
                    return JsonElementToCellString(nested);
                string leaf = header["baseAttributes.".Length..];
                if (TryGetJsonPropertyCaseInsensitive(el, leaf, out var rootStat))
                    return JsonElementToCellString(rootStat);
                return "";
            }

            if (header.StartsWith("growthPerLevel.", StringComparison.Ordinal))
            {
                if (TryGetPropertyPathCaseInsensitive(el, header, out var nestedG))
                    return JsonElementToCellString(nestedG);
                string leafG = header["growthPerLevel.".Length..];
                if (TryGetJsonPropertyCaseInsensitive(el, leafG, out var rootG))
                    return JsonElementToCellString(rootG);
                return "";
            }

            if (header.Contains('.', StringComparison.Ordinal))
            {
                if (TryGetPropertyPathCaseInsensitive(el, header, out var at))
                    return JsonElementToCellString(at);
                return "";
            }

            if (!TryGetJsonPropertyCaseInsensitive(el, header, out var prop))
                return "";
            if (string.Equals(header, "tags", StringComparison.OrdinalIgnoreCase))
                return FormatTagsArrayForSheetCell(prop);
            if (string.Equals(header, "actions", StringComparison.OrdinalIgnoreCase))
                return FormatPipeDelimitedStringArrayForSheetCell(prop);
            return JsonElementToCellString(prop);
        }
    }
}
