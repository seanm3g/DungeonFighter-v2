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
        public static readonly string[] WeaponsCanonicalHeaders = JsonArraySheetSchemas.WeaponsCanonicalHeaders;
        public static readonly string[] ModificationsCanonicalHeaders = JsonArraySheetSchemas.ModificationsCanonicalHeaders;
        public static readonly string[] ArmorCanonicalHeaders = JsonArraySheetSchemas.ArmorCanonicalHeaders;
        public static readonly string[] EnemiesCanonicalHeaders = JsonArraySheetSchemas.EnemiesCanonicalHeaders;
        public static readonly string[] EnvironmentsCanonicalHeaders = JsonArraySheetSchemas.EnvironmentsCanonicalHeaders;
        public static readonly string[] DungeonsCanonicalHeaders = JsonArraySheetSchemas.DungeonsCanonicalHeaders;
        public static readonly string[] StatBonusesCanonicalHeaders = JsonArraySheetSchemas.StatBonusesCanonicalHeaders;
        public static readonly string[] ConsumablesCanonicalHeaders = JsonArraySheetSchemas.ConsumablesCanonicalHeaders;
        public static readonly string[] TriggersCanonicalHeaders = JsonArraySheetSchemas.TriggersCanonicalHeaders;

        public static int GetTabularSheetHeaderRowCount(GameDataTabularSheetKind kind) =>
            JsonArraySheetSchemas.GetTabularSheetHeaderRowCount(kind);

        public const string EnemySheetCategoryOverrides = JsonArraySheetSchemas.EnemySheetCategoryOverrides;
        public const string EnemySheetCategoryBaseAttributes = JsonArraySheetSchemas.EnemySheetCategoryBaseAttributes;
        public const string EnemySheetCategoryGrowth = JsonArraySheetSchemas.EnemySheetCategoryGrowth;
        public const string EnemySheetCategoryHealth = JsonArraySheetSchemas.EnemySheetCategoryHealth;

        public static IReadOnlyList<string> GetCanonicalHeaders(GameDataTabularSheetKind kind) =>
            JsonArraySheetSchemas.GetCanonicalHeaders(kind);

        public static string MergeJsonRootArrays(string? firstJson, string? secondJson) =>
            ModificationsJsonArrayMerger.MergeJsonRootArrays(firstJson, secondJson);

        public static (string coreModificationsJson, string prefixMaterialQualityJson) SplitModificationsMergedJson(
            string mergedArrayJson) =>
            ModificationsJsonArrayMerger.SplitModificationsMergedJson(mergedArrayJson);
        public static List<IList<object>> BuildPushValueRows(string jsonFileText, GameDataTabularSheetKind kind)
        {
            if (kind == GameDataTabularSheetKind.Armor)
                return BuildArmorPushValueRows(jsonFileText);

            if (kind == GameDataTabularSheetKind.Enemies)
                return BuildEnemyPushValueRows(jsonFileText);

            if (kind == GameDataTabularSheetKind.Environments)
                return BuildEnvironmentPushValueRows(jsonFileText);

            if (kind == GameDataTabularSheetKind.Dungeons)
                return BuildDungeonsPushValueRows(jsonFileText);

            using var doc = JsonDocument.Parse(jsonFileText);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Expected a JSON array at the root.");

            var canonical = GetCanonicalHeaders(kind).ToList();
            var extraKeys = new SortedSet<string>(StringComparer.Ordinal);
            // SUFFIXES tab is fixed A–G; do not emit helper/junk JSON keys as extra columns.
            // CONSUMABLES tab is fixed A–D.
            // TRIGGERS tab is fixed A–I.
            if (kind != GameDataTabularSheetKind.StatBonuses
                && kind != GameDataTabularSheetKind.Consumables
                && kind != GameDataTabularSheetKind.Triggers)
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.ValueKind != JsonValueKind.Object)
                        continue;
                    foreach (var p in el.EnumerateObject())
                    {
                        if (!canonical.Any(c => string.Equals(c, p.Name, StringComparison.Ordinal)))
                        {
                            // Nested trigger blobs are authored via triggers sheet + triggerName, not as gear columns.
                            if (string.Equals(p.Name, "triggerBundles", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(p.Name, "equipEffects", StringComparison.OrdinalIgnoreCase))
                                continue;
                            extraKeys.Add(p.Name);
                        }
                    }
                }
            }

            var headers = new List<string>(canonical);
            headers.AddRange(extraKeys);

            var rows = new List<IList<object>>();
            rows.Add(headers.Select(h => (object)h).ToList());

            IEnumerable<JsonElement> dataElements = doc.RootElement.EnumerateArray();
            if (kind == GameDataTabularSheetKind.Weapons)
                dataElements = SortWeaponElementsForPush(dataElements);

            foreach (var el in dataElements)
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                var row = new List<object>();
                foreach (var h in headers)
                {
                    if (!TryGetJsonPropertyCaseInsensitive(el, h, out var prop))
                    {
                        row.Add("");
                        continue;
                    }

                    if (kind == GameDataTabularSheetKind.StatBonuses
                        && string.Equals(h, "Requirements", StringComparison.OrdinalIgnoreCase)
                        && prop.ValueKind == JsonValueKind.Object)
                    {
                        row.Add(FormatStatBonusRequirementsForSheetCell(prop));
                        continue;
                    }

                    if (string.Equals(h, "tags", StringComparison.OrdinalIgnoreCase)
                        && prop.ValueKind == JsonValueKind.Array)
                    {
                        row.Add(FormatTagsArrayForSheetCell(prop));
                        continue;
                    }

                    row.Add(JsonElementToCellString(prop));
                }
                rows.Add(row);
            }

            return rows;
        }
        internal static int GetWeaponTypeSortRank(string? type) =>
            ItemCatalogSortHelper.GetWeaponTypeSortRank(type);

        internal static int GetArmorSlotSortRank(string? slot) =>
            ItemCatalogSortHelper.GetArmorSlotSortRank(slot);

        public static string SortItemCatalogJsonArrayText(string jsonFileText, GameDataTabularSheetKind kind) =>
            ItemCatalogSortHelper.SortItemCatalogJsonArrayText(jsonFileText, kind);

        private static IEnumerable<JsonElement> SortWeaponElementsForPush(IEnumerable<JsonElement> elements) =>
            ItemCatalogSortHelper.SortWeaponElementsForPush(elements);

        private static IEnumerable<JsonElement> SortArmorElementsForPush(IEnumerable<JsonElement> elements) =>
            ItemCatalogSortHelper.SortArmorElementsForPush(elements);

        private static void SortJsonArrayForItemCatalog(JsonArray arr, GameDataTabularSheetKind kind) =>
            ItemCatalogSortHelper.SortJsonArrayForItemCatalog(arr, kind);

        private static string JsonStringToCell(JsonElement el) =>
            SheetCellFormatters.JsonStringToCell(el);

        private static string JsonElementToCellString(JsonElement el) =>
            SheetCellFormatters.JsonElementToCellString(el);

        internal static string FormatEnvironmentWeightedListForSheetCell(JsonElement el) =>
            SheetCellFormatters.FormatEnvironmentWeightedListForSheetCell(el);

        internal static string FormatPipeDelimitedStringArrayForSheetCell(JsonElement el) =>
            SheetCellFormatters.FormatPipeDelimitedStringArrayForSheetCell(el);

        internal static string FormatTagsArrayForSheetCell(JsonElement el) =>
            SheetTagsNormalizer.FormatTagsArrayForSheetCell(el);

        private static bool TryGetJsonPropertyCaseInsensitive(JsonElement el, string name, out JsonElement prop) =>
            SheetJsonPropertyHelper.TryGetJsonPropertyCaseInsensitive(el, name, out prop);
        internal static string FormatStatBonusRequirementsForSheetCell(JsonElement obj) =>
            StatBonusSheetBracketParser.FormatStatBonusRequirementsForSheetCell(obj);
        private static bool TryGetPropertyPath(JsonElement el, string dottedPath, out JsonElement found) =>
            TryGetPropertyPathCaseInsensitive(el, dottedPath, out found);

        private static bool TryGetPropertyPathCaseInsensitive(JsonElement el, string dottedPath, out JsonElement found) =>
            SheetJsonPropertyHelper.TryGetPropertyPathCaseInsensitive(el, dottedPath, out found);
        public static string CsvToJsonArrayText(string csvContent, GameDataTabularSheetKind kind)
        {
            var table = SimpleGameDataCsvParser.ParseToRows(csvContent);
            if (table.Count == 0)
                throw new InvalidOperationException("CSV has no rows.");

            TryGetEnemyImportHeaderRowAndFirstDataIndex(table, kind, out var headerRow, out int firstDataRow);

            var arr = new JsonArray();
            for (int r = firstDataRow; r < table.Count; r++)
            {
                var cells = table[r];
                if (cells.Length == 0 || cells.All(string.IsNullOrWhiteSpace))
                    continue;

                var obj = new JsonObject();
                int headerCount = headerRow.Length;
                if (kind == GameDataTabularSheetKind.Consumables)
                    headerCount = Math.Min(headerCount, ConsumablesCanonicalHeaders.Length);
                if (kind == GameDataTabularSheetKind.Triggers)
                    headerCount = Math.Min(headerCount, TriggersCanonicalHeaders.Length);
                for (int i = 0; i < headerCount; i++)
                {
                    // Google / Excel CSV exports may prefix the file with U+FEFF, which lands on the first header cell.
                    string header = (headerRow[i] ?? "").Trim().TrimStart('\uFEFF');
                    // Live SUFFIXES tab: column A holds affix names ("of Protection") but often has a blank header cell.
                    if (kind == GameDataTabularSheetKind.StatBonuses && header.Length == 0 && i == 0)
                        header = "Name";
                    if (header.Length == 0)
                        continue;
                    string cell = i < cells.Length ? cells[i] ?? "" : "";
                    JsonNode? parsed = CellToJsonNode(cell);
                    if (parsed != null)
                        obj[header] = parsed;
                }

                if (kind == GameDataTabularSheetKind.Enemies)
                {
                    MergeEnemyFlatColumnsToNested(obj);
                    NormalizeEnemyJsonArrayRow(obj);
                    if (!obj.TryGetPropertyValue("name", out var nameNode)
                        || nameNode is null
                        || string.IsNullOrWhiteSpace(nameNode.GetValue<string>()))
                        continue;
                }
                else if (kind == GameDataTabularSheetKind.Dungeons)
                {
                    NormalizeDungeonsJsonArrayRow(obj);
                }
                else if (kind == GameDataTabularSheetKind.Weapons)
                {
                    NormalizeWeaponsJsonArrayRow(obj);
                }
                else if (kind == GameDataTabularSheetKind.StatBonuses)
                {
                    NormalizeStatBonusesJsonArrayRow(obj);
                }
                else if (kind == GameDataTabularSheetKind.Modifications)
                {
                    GameDataJsonNormalizer.NormalizeModificationsImportRow(obj);
                }
                else if (kind == GameDataTabularSheetKind.Armor)
                {
                    GameDataJsonNormalizer.NormalizeArmorDataRow(obj);
                    NormalizeTagsFromSheet(obj);
                }
                else if (kind == GameDataTabularSheetKind.Consumables)
                {
                    NormalizeConsumablesJsonArrayRow(obj);
                }
                else if (kind == GameDataTabularSheetKind.Triggers)
                {
                    NormalizeTriggersJsonArrayRow(obj);
                }
                else if (kind == GameDataTabularSheetKind.Environments)
                {
                    NormalizeEnvironmentJsonArrayRow(obj);
                }

                arr.Add(obj);
            }

            if (kind == GameDataTabularSheetKind.Weapons || kind == GameDataTabularSheetKind.Armor)
                SortJsonArrayForItemCatalog(arr, kind);

            return arr.ToJsonString(GetSerializerOptions(kind));
        }
        private static JsonSerializerOptions GetSerializerOptions(GameDataTabularSheetKind kind) =>
            JsonArraySheetSerializerOptions.GetSerializerOptions(kind);

        internal static JsonNode? CellToJsonNode(string cell) =>
            SheetCellParsers.CellToJsonNode(cell);

        private static bool IsJsonNodeNullOrMissing(JsonNode n) =>
            SheetJsonNodeHelper.IsJsonNodeNullOrMissing(n);
        internal static void NormalizeTagsFromSheet(JsonObject obj) =>
            SheetTagsNormalizer.NormalizeTagsFromSheet(obj);
    }
}
