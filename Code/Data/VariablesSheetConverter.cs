using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RPGGame.Data
{
    /// <summary>
    /// VARIABLES tab: vertical <c>property</c> + <c>value</c> rows for every scalar leaf in the active balance patch,
    /// excluding <c>classPresentation</c> (owned by the CLASSES tab).
    /// </summary>
    public static class VariablesSheetConverter
    {
        public const string SkippedRootProperty = "classPresentation";

        public const string VerticalPropertyHeader = "property";
        public const string VerticalValueHeader = "value";

        /// <summary>Flatten a balance-patch JSON object into push rows (header + sorted property/value).</summary>
        public static List<IList<object>> BuildPushValueRows(JsonObject root)
        {
            var leaves = new List<(string Path, string Value)>();
            CollectLeaves(root, prefix: "", leaves);

            leaves.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));

            var rows = new List<IList<object>>
            {
                new List<object> { VerticalPropertyHeader, VerticalValueHeader }
            };
            foreach (var (path, value) in leaves)
                rows.Add(new List<object> { path, value });
            return rows;
        }

        /// <summary>Flatten raw JSON text (object root) into push rows.</summary>
        public static List<IList<object>> BuildPushValueRowsFromJsonText(string jsonText)
        {
            JsonNode? root = JsonNode.Parse(string.IsNullOrWhiteSpace(jsonText) ? "{}" : jsonText);
            if (root is not JsonObject obj)
                throw new InvalidOperationException("VARIABLES push: balance patch root must be a JSON object.");
            return BuildPushValueRows(obj);
        }

        /// <summary>
        /// Merge VARIABLES CSV into the balance patch file by dotted path.
        /// Does not wipe siblings; never writes under <see cref="SkippedRootProperty"/>.
        /// </summary>
        public static void MergeVariablesFromCsvIntoTuningFile(string csvContent, string tuningConfigPath)
        {
            var rows = SimpleGameDataCsvParser.ParseToRows(csvContent);
            if (rows.Count < 1)
                throw new InvalidOperationException("VARIABLES CSV must have a header row.");

            string[] header = rows[0];
            int keyCol = FindColumnIndex(header, VerticalPropertyHeader, "key", "field");
            int valCol = FindColumnIndex(header, VerticalValueHeader);
            if (keyCol < 0 || valCol < 0 || keyCol == valCol)
                throw new InvalidOperationException(
                    "VARIABLES CSV: expected columns 'property' (or key/field) and 'value'.");

            if (!File.Exists(tuningConfigPath))
                throw new FileNotFoundException("Balance patch / TuningConfig.json not found.", tuningConfigPath);

            string fullText = File.ReadAllText(tuningConfigPath);
            JsonNode? root = JsonNode.Parse(string.IsNullOrWhiteSpace(fullText) ? "{}" : fullText);
            if (root is not JsonObject rootObj)
                throw new InvalidOperationException("Balance patch root must be a JSON object.");

            int applied = 0;
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                string path = keyCol < row.Length ? row[keyCol]?.Trim() ?? "" : "";
                if (path.Length == 0)
                    continue;
                if (IsSkippedPath(path))
                    continue;

                string cell = valCol < row.Length ? row[valCol] ?? "" : "";
                SetLeafByPath(rootObj, path, cell);
                applied++;
            }

            if (applied == 0 && rows.Count > 1)
                Console.WriteLine("VARIABLES pull: no property rows applied (empty paths or only skipped classPresentation).");

            var writeOpts = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            File.WriteAllText(tuningConfigPath, root.ToJsonString(writeOpts));
        }

        /// <summary>True when the path is or starts under classPresentation.</summary>
        public static bool IsSkippedPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;
            string p = path.Trim();
            if (p.Equals(SkippedRootProperty, StringComparison.OrdinalIgnoreCase))
                return true;
            return p.StartsWith(SkippedRootProperty + ".", StringComparison.OrdinalIgnoreCase);
        }

        private static void CollectLeaves(JsonNode? node, string prefix, List<(string Path, string Value)> leaves)
        {
            if (node == null)
                return;

            if (node is JsonObject obj)
            {
                foreach (var kv in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    if (string.IsNullOrEmpty(prefix)
                        && kv.Key.Equals(SkippedRootProperty, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string next = string.IsNullOrEmpty(prefix) ? kv.Key : prefix + "." + kv.Key;
                    if (kv.Value == null)
                    {
                        leaves.Add((next, ""));
                        continue;
                    }

                    if (kv.Value is JsonObject childObj)
                    {
                        if (childObj.Count == 0)
                            continue;
                        CollectLeaves(childObj, next, leaves);
                        continue;
                    }

                    if (kv.Value is JsonArray arr)
                    {
                        CollectArrayLeaves(arr, next, leaves);
                        continue;
                    }

                    leaves.Add((next, FormatScalar(kv.Value)));
                }

                return;
            }

            if (node is JsonArray rootArr)
                CollectArrayLeaves(rootArr, prefix, leaves);
            else if (!string.IsNullOrEmpty(prefix))
                leaves.Add((prefix, FormatScalar(node)));
        }

        private static void CollectArrayLeaves(JsonArray arr, string prefix, List<(string Path, string Value)> leaves)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                string next = prefix + "." + i.ToString(CultureInfo.InvariantCulture);
                JsonNode? el = arr[i];
                if (el == null)
                {
                    leaves.Add((next, ""));
                    continue;
                }

                if (el is JsonObject o)
                {
                    if (o.Count == 0)
                        continue;
                    CollectLeaves(o, next, leaves);
                    continue;
                }

                if (el is JsonArray nested)
                {
                    CollectArrayLeaves(nested, next, leaves);
                    continue;
                }

                leaves.Add((next, FormatScalar(el)));
            }
        }

        private static string FormatScalar(JsonNode node)
        {
            if (node is JsonValue jv)
            {
                if (jv.TryGetValue(out bool b))
                    return b ? "true" : "false";
                if (jv.TryGetValue(out long l))
                    return l.ToString(CultureInfo.InvariantCulture);
                if (jv.TryGetValue(out int i))
                    return i.ToString(CultureInfo.InvariantCulture);
                if (jv.TryGetValue(out double d))
                    return d.ToString("G17", CultureInfo.InvariantCulture);
                if (jv.TryGetValue(out decimal m))
                    return m.ToString(CultureInfo.InvariantCulture);
                if (jv.TryGetValue(out string? s))
                    return s ?? "";
            }

            return node.ToJsonString();
        }

        /// <summary>
        /// Set a leaf by dotted path. Prefer preserving the existing JSON value kind when present;
        /// otherwise infer bool/number/string from the cell text.
        /// </summary>
        internal static void SetLeafByPath(JsonObject root, string path, string cellText)
        {
            string[] parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                return;
            if (parts[0].Equals(SkippedRootProperty, StringComparison.OrdinalIgnoreCase))
                return;

            JsonNode current = root;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                string seg = parts[i];
                JsonNode? next = NavigateOrCreateChild(current, seg, parts[i + 1]);
                if (next == null)
                    throw new InvalidOperationException($"VARIABLES: cannot navigate path '{path}' at '{seg}'.");
                current = next;
            }

            string leafKey = parts[^1];
            JsonNode newValue = CoerceCellToJsonNode(current, leafKey, cellText);
            AssignChild(current, leafKey, newValue);
        }

        private static JsonNode? NavigateOrCreateChild(JsonNode parent, string seg, string nextSeg)
        {
            bool nextIsIndex = int.TryParse(nextSeg, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

            if (parent is JsonObject obj)
            {
                if (obj.TryGetPropertyValue(seg, out JsonNode? existing) && existing != null)
                    return existing;

                JsonNode created = nextIsIndex ? new JsonArray() : new JsonObject();
                obj[seg] = created;
                return created;
            }

            if (parent is JsonArray arr)
            {
                if (!int.TryParse(seg, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx) || idx < 0)
                    return null;
                EnsureArrayLength(arr, idx + 1);
                if (arr[idx] == null)
                    arr[idx] = nextIsIndex ? new JsonArray() : new JsonObject();
                return arr[idx];
            }

            return null;
        }

        private static void AssignChild(JsonNode parent, string key, JsonNode value)
        {
            if (parent is JsonObject obj)
            {
                obj[key] = value;
                return;
            }

            if (parent is JsonArray arr)
            {
                if (!int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx) || idx < 0)
                    throw new InvalidOperationException($"VARIABLES: array index expected, got '{key}'.");
                EnsureArrayLength(arr, idx + 1);
                arr[idx] = value;
                return;
            }

            throw new InvalidOperationException("VARIABLES: cannot assign leaf under a scalar parent.");
        }

        private static void EnsureArrayLength(JsonArray arr, int length)
        {
            while (arr.Count < length)
                arr.Add(null);
        }

        private static JsonNode CoerceCellToJsonNode(JsonNode parent, string leafKey, string cellText)
        {
            JsonNode? existing = null;
            if (parent is JsonObject obj && obj.TryGetPropertyValue(leafKey, out JsonNode? n))
                existing = n;
            else if (parent is JsonArray arr
                     && int.TryParse(leafKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx)
                     && idx >= 0
                     && idx < arr.Count)
                existing = arr[idx];

            string raw = cellText ?? "";
            if (existing is JsonValue)
                return CoercePreservingKind(existing, raw);

            return InferScalarNode(raw);
        }

        private static JsonNode CoercePreservingKind(JsonNode existing, string raw)
        {
            if (existing is JsonValue jv)
            {
                if (jv.TryGetValue(out bool _))
                {
                    if (bool.TryParse(raw.Trim(), out bool b))
                        return JsonValue.Create(b);
                    if (raw.Trim() == "1")
                        return JsonValue.Create(true);
                    if (raw.Trim() == "0")
                        return JsonValue.Create(false);
                    return JsonValue.Create(false);
                }

                if (jv.TryGetValue(out int _)
                    || jv.TryGetValue(out long _))
                {
                    if (long.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
                    {
                        if (l >= int.MinValue && l <= int.MaxValue)
                            return JsonValue.Create((int)l);
                        return JsonValue.Create(l);
                    }

                    if (double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double dIntFallback))
                        return JsonValue.Create(dIntFallback);
                }

                if (jv.TryGetValue(out double _)
                    || jv.TryGetValue(out float _)
                    || jv.TryGetValue(out decimal _))
                {
                    if (double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                        return JsonValue.Create(d);
                }

                if (jv.TryGetValue(out string? _))
                    return JsonValue.Create(raw);
            }

            return InferScalarNode(raw);
        }

        private static JsonNode InferScalarNode(string raw)
        {
            string t = raw?.Trim() ?? "";
            if (t.Equals("true", StringComparison.OrdinalIgnoreCase))
                return JsonValue.Create(true);
            if (t.Equals("false", StringComparison.OrdinalIgnoreCase))
                return JsonValue.Create(false);
            if (long.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
            {
                if (l >= int.MinValue && l <= int.MaxValue)
                    return JsonValue.Create((int)l);
                return JsonValue.Create(l);
            }

            if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                return JsonValue.Create(d);

            return JsonValue.Create(raw ?? "");
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
    }
}
