using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RPGGame.Data
{
    /// <summary>Push/pull for JSON array game files on a single-header-row sheet tab.</summary>
    public enum GameDataTabularSheetKind
    {
        Weapons,
        Modifications,
        Armor
    }

    public static class JsonArraySheetConverter
    {
        public static readonly string[] WeaponsCanonicalHeaders =
            { "type", "name", "baseDamage", "attackSpeed", "tier", "attributeRequirements" };

        public static readonly string[] ModificationsCanonicalHeaders =
            { "DiceResult", "ItemRank", "Name", "Description", "Effect", "MinValue", "MaxValue" };

        public static readonly string[] ArmorCanonicalHeaders =
            { "slot", "name", "armor", "tier", "attributeRequirements" };

        public static IReadOnlyList<string> GetCanonicalHeaders(GameDataTabularSheetKind kind) =>
            kind switch
            {
                GameDataTabularSheetKind.Weapons => WeaponsCanonicalHeaders,
                GameDataTabularSheetKind.Modifications => ModificationsCanonicalHeaders,
                GameDataTabularSheetKind.Armor => ArmorCanonicalHeaders,
                _ => Array.Empty<string>()
            };

        /// <summary>Builds sheet rows (row 0 = headers, row 1+ = data) from a JSON array file body.</summary>
        public static List<IList<object>> BuildPushValueRows(string jsonFileText, GameDataTabularSheetKind kind)
        {
            using var doc = JsonDocument.Parse(jsonFileText);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Expected a JSON array at the root.");

            var canonical = GetCanonicalHeaders(kind).ToList();
            var extraKeys = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                foreach (var p in el.EnumerateObject())
                {
                    if (!canonical.Any(c => string.Equals(c, p.Name, StringComparison.Ordinal)))
                        extraKeys.Add(p.Name);
                }
            }

            var headers = new List<string>(canonical);
            headers.AddRange(extraKeys);

            var rows = new List<IList<object>>();
            rows.Add(headers.Select(h => (object)h).ToList());

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                var row = new List<object>();
                foreach (var h in headers)
                {
                    if (!el.TryGetProperty(h, out var prop))
                    {
                        row.Add("");
                        continue;
                    }
                    row.Add(JsonElementToCellString(prop));
                }
                rows.Add(row);
            }

            return rows;
        }

        /// <summary>Converts CSV (header row + data) into pretty-printed JSON array file text.</summary>
        public static string CsvToJsonArrayText(string csvContent, GameDataTabularSheetKind kind)
        {
            var table = SimpleGameDataCsvParser.ParseToRows(csvContent);
            if (table.Count == 0)
                throw new InvalidOperationException("CSV has no rows.");

            var headerRow = table[0];

            var arr = new JsonArray();
            for (int r = 1; r < table.Count; r++)
            {
                var cells = table[r];
                if (cells.Length == 0 || cells.All(string.IsNullOrWhiteSpace))
                    continue;

                var obj = new JsonObject();
                for (int i = 0; i < headerRow.Length; i++)
                {
                    string header = headerRow[i]?.Trim() ?? "";
                    if (header.Length == 0)
                        continue;
                    string cell = i < cells.Length ? cells[i] ?? "" : "";
                    obj[header] = CellToJsonNode(cell);
                }
                arr.Add(obj);
            }

            return arr.ToJsonString(GetSerializerOptions(kind));
        }

        private static JsonSerializerOptions GetSerializerOptions(GameDataTabularSheetKind kind)
        {
            var o = new JsonSerializerOptions { WriteIndented = true };
            if (kind == GameDataTabularSheetKind.Weapons || kind == GameDataTabularSheetKind.Armor)
                o.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            else
                o.PropertyNamingPolicy = null;
            return o;
        }

        private static string JsonElementToCellString(JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.Null => "",
                JsonValueKind.String => el.GetString() ?? "",
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => el.GetRawText(),
                JsonValueKind.Object or JsonValueKind.Array => el.GetRawText(),
                _ => el.GetRawText()
            };
        }

        internal static JsonNode? CellToJsonNode(string cell)
        {
            if (string.IsNullOrWhiteSpace(cell))
                return null;

            cell = cell.Trim();
            if (cell.Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;

            if (cell.Length > 0 && (cell[0] == '{' || cell[0] == '['))
            {
                try
                {
                    return JsonNode.Parse(cell);
                }
                catch
                {
                    return JsonValue.Create(cell);
                }
            }

            if (bool.TryParse(cell, out bool b))
                return JsonValue.Create(b);

            if (double.TryParse(cell, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double d))
            {
                if (d >= long.MinValue && d <= long.MaxValue && Math.Abs(d - (long)d) < 1e-9)
                    return JsonValue.Create((long)d);
                return JsonValue.Create(d);
            }

            return JsonValue.Create(cell);
        }
    }
}
