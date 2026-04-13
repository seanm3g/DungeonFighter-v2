using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>
    /// CLASSES tab: vertical <c>property</c> + <c>value</c> rows (one property per row).
    /// Pull also accepts legacy <c>field</c> + <c>jsonPayload</c> blob, or a single wide header row + data row (horizontal).
    /// </summary>
    public static class ClassPresentationSheetConverter
    {
        public const string FieldHeader = "field";
        public const string PayloadHeader = "jsonPayload";
        public const string LegacyPayloadHeader = "value";

        /// <summary>Column A header for vertical layout (push).</summary>
        public const string VerticalPropertyHeader = "property";

        /// <summary>Column B header for vertical layout (push).</summary>
        public const string VerticalValueHeader = "value";

        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>Ordered property keys for vertical push (and horizontal wide row, if used).</summary>
        public static IReadOnlyList<string> FlatSheetColumnHeaders { get; } = BuildFlatColumnHeaders();

        private static List<string> BuildFlatColumnHeaders()
        {
            var h = new List<string>
            {
                "defaultNoPointsClassName",
                "maceClassDisplayName",
                "swordClassDisplayName",
                "daggerClassDisplayName",
                "wandClassDisplayName"
            };
            for (int i = 0; i < ClassPresentationConfig.TierSlotCount; i++)
                h.Add("tierThresholds_" + i);
            for (int i = 0; i < ClassPresentationConfig.TierSlotCount; i++)
                h.Add("attributeSoloTrioTierPrefixes_" + i);
            for (int i = 0; i < ClassPresentationConfig.TierSlotCount; i++)
                h.Add("attributeQuadTierNames_" + i);

            h.AddRange(new[]
            {
                "attributeModifierMace",
                "attributeModifierSword",
                "attributeModifierDagger",
                "attributeModifierWand",
                "attributeDuoMaceSword",
                "attributeDuoMaceDagger",
                "attributeDuoMaceWand",
                "attributeDuoSwordDagger",
                "attributeDuoSwordWand",
                "attributeDuoDaggerWand"
            });

            return h;
        }

        public static List<IList<object>> BuildPushValueRows(ClassPresentationConfig presentation)
        {
            presentation = presentation.EnsureNormalized();
            var rows = new List<IList<object>>
            {
                new List<object> { VerticalPropertyHeader, VerticalValueHeader }
            };
            foreach (string key in FlatSheetColumnHeaders)
                rows.Add(new List<object> { key, GetFlatCellValue(presentation, key) });

            return rows;
        }

        private static string GetFlatCellValue(ClassPresentationConfig c, string key)
        {
            if (key.StartsWith("tierThresholds_", StringComparison.Ordinal))
            {
                int i = ParseSuffixIndex(key, "tierThresholds_");
                return i >= 0 && i < c.TierThresholds.Length ? c.TierThresholds[i].ToString(CultureInfo.InvariantCulture) : "";
            }

            if (key.StartsWith("attributeSoloTrioTierPrefixes_", StringComparison.Ordinal))
            {
                int i = ParseSuffixIndex(key, "attributeSoloTrioTierPrefixes_");
                return i >= 0 && i < c.AttributeSoloTrioTierPrefixes.Length ? c.AttributeSoloTrioTierPrefixes[i] ?? "" : "";
            }

            if (key.StartsWith("attributeQuadTierNames_", StringComparison.Ordinal))
            {
                int i = ParseSuffixIndex(key, "attributeQuadTierNames_");
                return i >= 0 && i < c.AttributeQuadTierNames.Length ? c.AttributeQuadTierNames[i] ?? "" : "";
            }

            return key switch
            {
                "defaultNoPointsClassName" => c.DefaultNoPointsClassName,
                "maceClassDisplayName" => c.MaceClassDisplayName,
                "swordClassDisplayName" => c.SwordClassDisplayName,
                "daggerClassDisplayName" => c.DaggerClassDisplayName,
                "wandClassDisplayName" => c.WandClassDisplayName,
                "attributeModifierMace" => c.AttributeModifierMace,
                "attributeModifierSword" => c.AttributeModifierSword,
                "attributeModifierDagger" => c.AttributeModifierDagger,
                "attributeModifierWand" => c.AttributeModifierWand,
                "attributeDuoMaceSword" => c.AttributeDuoMaceSword,
                "attributeDuoMaceDagger" => c.AttributeDuoMaceDagger,
                "attributeDuoMaceWand" => c.AttributeDuoMaceWand,
                "attributeDuoSwordDagger" => c.AttributeDuoSwordDagger,
                "attributeDuoSwordWand" => c.AttributeDuoSwordWand,
                "attributeDuoDaggerWand" => c.AttributeDuoDaggerWand,
                _ => ""
            };
        }

        private static int ParseSuffixIndex(string key, string prefix)
        {
            if (!key.StartsWith(prefix, StringComparison.Ordinal) || key.Length <= prefix.Length)
                return -1;
            return int.TryParse(key.AsSpan(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx)
                ? idx
                : -1;
        }

        public static void MergeClassPresentationFromCsvIntoTuningFile(string csvContent, string tuningConfigPath)
        {
            var rows = SimpleGameDataCsvParser.ParseToRows(csvContent);
            if (rows.Count < 2)
                throw new InvalidOperationException("CLASSES CSV must have a header row and at least one data row.");

            ClassPresentationConfig parsed;
            if (IsLegacyJsonPayloadFormat(rows[0], rows))
                parsed = ParseLegacyJsonPayloadRow(rows);
            else if (IsVerticalPropertyValueFormat(rows[0], rows))
                parsed = ParseFromPropertyDictionary(BuildVerticalPropertyMap(rows));
            else if (IsHorizontalFlatFormat(rows[0]))
                parsed = ParseFlatHorizontal(rows);
            else
                throw new InvalidOperationException(
                    "CLASSES CSV: expected vertical columns 'property' + 'value', legacy 'field' + 'jsonPayload', " +
                    "or a wide row with 'defaultNoPointsClassName'.");

            parsed = parsed.EnsureNormalized();
            JsonNode? presentationNode = JsonSerializer.SerializeToNode(parsed, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (!File.Exists(tuningConfigPath))
                throw new FileNotFoundException("TuningConfig.json not found.", tuningConfigPath);

            string fullText = File.ReadAllText(tuningConfigPath);
            JsonNode? root = JsonNode.Parse(fullText);
            if (root is not JsonObject rootObj)
                throw new InvalidOperationException("TuningConfig root must be a JSON object.");

            rootObj["classPresentation"] = presentationNode;

            var writeOpts = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            string outText = root.ToJsonString(writeOpts);
            File.WriteAllText(tuningConfigPath, outText);
        }

        private static int FindColumnIndex(string[] header, params string[] names)
        {
            for (int i = 0; i < header.Length; i++)
            {
                string h = header[i]?.Trim() ?? "";
                foreach (string n in names)
                {
                    if (h.Equals(n, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }

            return -1;
        }

        /// <summary>Legacy: jsonPayload column, or field + value with a single classPresentation JSON blob row.</summary>
        private static bool IsLegacyJsonPayloadFormat(string[] header, IReadOnlyList<string[]> rows)
        {
            bool hasFlatKey = false;
            foreach (string? raw in header)
            {
                string h = raw?.Trim() ?? "";
                if (h.Equals("defaultNoPointsClassName", StringComparison.OrdinalIgnoreCase))
                    hasFlatKey = true;
            }

            if (hasFlatKey)
                return false;

            if (FindColumnIndex(header, PayloadHeader) >= 0)
                return true;

            int valueIdx = FindColumnIndex(header, LegacyPayloadHeader);
            int fieldIdx = FindColumnIndex(header, FieldHeader);
            if (valueIdx >= 0 && fieldIdx >= 0 && rows.Count == 2)
            {
                string row0 = rows[1].Length > fieldIdx ? rows[1][fieldIdx]?.Trim() ?? "" : "";
                string blob = rows[1].Length > valueIdx ? rows[1][valueIdx]?.Trim() ?? "" : "";
                if (row0.Equals("classPresentation", StringComparison.OrdinalIgnoreCase) && blob.StartsWith("{", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static bool IsVerticalPropertyValueFormat(string[] header, IReadOnlyList<string[]> rows)
        {
            if (IsLegacyJsonPayloadFormat(header, rows))
                return false;

            int keyCol = FindColumnIndex(header, VerticalPropertyHeader, "key", FieldHeader);
            int valCol = FindColumnIndex(header, VerticalValueHeader);
            if (keyCol < 0 || valCol < 0 || keyCol == valCol)
                return false;

            // field+jsonPayload legacy uses FieldHeader too; already excluded via IsLegacy
            return rows.Count >= 2;
        }

        private static bool IsHorizontalFlatFormat(string[] header) =>
            FindColumnIndex(header, "defaultNoPointsClassName") >= 0;

        private static Dictionary<string, string> BuildVerticalPropertyMap(IReadOnlyList<string[]> rows)
        {
            string[] header = rows[0];
            int keyCol = FindColumnIndex(header, VerticalPropertyHeader, "key", FieldHeader);
            int valCol = FindColumnIndex(header, VerticalValueHeader);
            if (keyCol < 0 || valCol < 0)
                throw new InvalidOperationException("CLASSES vertical CSV must have property (or key/field) and value columns.");

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                string k = keyCol < row.Length ? row[keyCol]?.Trim() ?? "" : "";
                if (k.Length == 0)
                    continue;
                string v = valCol < row.Length ? row[valCol] ?? "" : "";
                map[k] = v;
            }

            return map;
        }

        private static ClassPresentationConfig ParseFlatHorizontal(IReadOnlyList<string[]> rows)
        {
            string[] header = rows[0];
            string[] data = rows[1];
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Length; i++)
            {
                string k = header[i]?.Trim() ?? "";
                if (k.Length == 0)
                    continue;
                string v = i < data.Length ? data[i] ?? "" : "";
                map[k] = v;
            }

            return ParseFromPropertyDictionary(map);
        }

        private static ClassPresentationConfig ParseLegacyJsonPayloadRow(IReadOnlyList<string[]> rows)
        {
            var header = rows[0];
            int fieldIdx = FindColumnIndex(header, FieldHeader);
            int payloadIdx = FindColumnIndex(header, PayloadHeader);
            if (payloadIdx < 0)
                payloadIdx = FindColumnIndex(header, LegacyPayloadHeader);

            if (payloadIdx < 0)
                throw new InvalidOperationException($"CLASSES CSV must include a '{PayloadHeader}' column (or legacy '{LegacyPayloadHeader}').");

            string payload = "";
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (fieldIdx >= 0 && fieldIdx < row.Length)
                {
                    string f = row[fieldIdx]?.Trim() ?? "";
                    if (f.Length > 0 && !f.Equals("classPresentation", StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                if (payloadIdx < row.Length)
                {
                    string cell = row[payloadIdx]?.Trim() ?? "";
                    if (cell.Length > 0)
                    {
                        payload = cell;
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(payload))
                throw new InvalidOperationException("CLASSES sheet: jsonPayload / value cell is empty.");

            var parsed = JsonSerializer.Deserialize<ClassPresentationConfig>(payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (parsed == null)
                throw new InvalidOperationException("CLASSES sheet: could not deserialize classPresentation JSON.");
            return parsed;
        }

        private static ClassPresentationConfig ParseFromPropertyDictionary(Dictionary<string, string> map)
        {
            var c = new ClassPresentationConfig();

            c.DefaultNoPointsClassName = Get(map, "defaultNoPointsClassName");
            c.MaceClassDisplayName = Get(map, "maceClassDisplayName");
            c.SwordClassDisplayName = Get(map, "swordClassDisplayName");
            c.DaggerClassDisplayName = Get(map, "daggerClassDisplayName");
            c.WandClassDisplayName = Get(map, "wandClassDisplayName");

            c.TierThresholds = ReadIntBand(map, "tierThresholds_", ClassPresentationConfig.TierSlotCount);

            c.AttributeSoloTrioTierPrefixes = ReadStringBand(map, "attributeSoloTrioTierPrefixes_", ClassPresentationConfig.TierSlotCount);
            c.AttributeQuadTierNames = ReadStringBand(map, "attributeQuadTierNames_", ClassPresentationConfig.TierSlotCount);

            c.AttributeModifierMace = Get(map, "attributeModifierMace");
            c.AttributeModifierSword = Get(map, "attributeModifierSword");
            c.AttributeModifierDagger = Get(map, "attributeModifierDagger");
            c.AttributeModifierWand = Get(map, "attributeModifierWand");

            c.AttributeDuoMaceSword = Get(map, "attributeDuoMaceSword");
            c.AttributeDuoMaceDagger = Get(map, "attributeDuoMaceDagger");
            c.AttributeDuoMaceWand = Get(map, "attributeDuoMaceWand");
            c.AttributeDuoSwordDagger = Get(map, "attributeDuoSwordDagger");
            c.AttributeDuoSwordWand = Get(map, "attributeDuoSwordWand");
            c.AttributeDuoDaggerWand = Get(map, "attributeDuoDaggerWand");

            return c;
        }

        private static string Get(Dictionary<string, string> map, string key) =>
            map.TryGetValue(key, out string? v) ? v.Trim() : "";

        private static bool TryGetInt(Dictionary<string, string> map, string key, out int value)
        {
            value = 0;
            if (!map.TryGetValue(key, out string? raw))
                return false;
            return int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static string[] ReadStringBand(Dictionary<string, string> map, string prefix, int count)
        {
            var arr = new string[count];
            for (int i = 0; i < count; i++)
                arr[i] = Get(map, prefix + i);
            return arr;
        }

        private static int[] ReadIntBand(Dictionary<string, string> map, string prefix, int count)
        {
            var arr = new int[count];
            for (int i = 0; i < count; i++)
            {
                if (!TryGetInt(map, prefix + i, out int v))
                    arr[i] = 0;
                else
                    arr[i] = v;
            }
            return arr;
        }
    }
}
