using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RPGGame.Config
{
    /// <summary>
    /// Balance patches as overlays: a patch JSON may name <c>"base": "&lt;patch name&gt;"</c> and hold only the values it
    /// changes. Objects merge key by key (case-insensitive); arrays and scalars replace. Bases can chain (an overlay of an
    /// overlay). Saving an overlay writes only its differences from the resolved base, so it stays an overlay.
    /// </summary>
    public static class BalancePatchOverlay
    {
        public const string BaseKey = "base";
        private const int MaxDepth = 16;

        /// <summary>The patch JSON with every base merged in (the "base" key removed).</summary>
        public static string ResolveJson(string patchPath) => Resolve(patchPath, 0).ToJsonString();

        /// <summary>The base patch name a patch file names, or null for a full patch.</summary>
        public static string? GetBaseName(string patchPath)
        {
            if (!File.Exists(patchPath)) return null;
            var node = JsonNode.Parse(File.ReadAllText(patchPath)) as JsonObject;
            return node != null && TryGet(node, BaseKey, out var b) && b is JsonValue v && v.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s) ? s : null;
        }

        /// <summary>Path of a base named in a patch: a bare name is a sibling <c>&lt;name&gt;.json</c>.</summary>
        public static string BasePath(string patchPath, string baseName)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(patchPath)) ?? ".";
            string file = baseName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? baseName : baseName + ".json";
            return Path.IsPathRooted(file) ? file : Path.GetFullPath(Path.Combine(dir, file));
        }

        /// <summary>
        /// JSON to write for <paramref name="fullJson"/> into <paramref name="patchPath"/>: the whole thing for a full patch,
        /// or <c>{"base": …, &lt;differences&gt;}</c> when the existing file is an overlay.
        /// </summary>
        public static string ToStoredJson(string patchPath, string fullJson, JsonSerializerOptions? writeOptions = null)
        {
            string? baseName = GetBaseName(patchPath);
            if (baseName == null) return fullJson;
            var full = JsonNode.Parse(fullJson) as JsonObject ?? new JsonObject();
            var baseNode = Resolve(BasePath(patchPath, baseName), 0);
            var diff = Diff(full, baseNode) as JsonObject ?? new JsonObject();
            var stored = new JsonObject { [BaseKey] = baseName };
            foreach (var kv in diff.ToList()) { diff.Remove(kv.Key); stored[kv.Key] = kv.Value; }
            return stored.ToJsonString(new JsonSerializerOptions { WriteIndented = writeOptions?.WriteIndented ?? true });
        }

        private static JsonObject Resolve(string path, int depth)
        {
            if (depth > MaxDepth) throw new InvalidOperationException($"Balance patch base chain is deeper than {MaxDepth} (cycle?) at {path}");
            var node = JsonNode.Parse(File.ReadAllText(path)) as JsonObject
                       ?? throw new InvalidOperationException($"Balance patch is not a JSON object: {path}");
            if (!TryGet(node, BaseKey, out var b) || b is not JsonValue v || !v.TryGetValue<string>(out var baseName) || string.IsNullOrWhiteSpace(baseName))
                return node;
            var merged = Resolve(BasePath(path, baseName), depth + 1);
            RemoveKey(node, BaseKey);
            Merge(merged, node);
            return merged;
        }

        /// <summary>Deep-merges <paramref name="src"/> into <paramref name="target"/>.</summary>
        public static void Merge(JsonObject target, JsonObject src)
        {
            foreach (var kv in src.ToList())
            {
                string key = FindKey(target, kv.Key) ?? kv.Key;
                if (kv.Value is JsonObject so && target[key] is JsonObject to)
                {
                    Merge(to, so);
                    continue;
                }
                src.Remove(kv.Key);
                target[key] = kv.Value;
            }
        }

        /// <summary>The parts of <paramref name="full"/> that differ from <paramref name="baseNode"/> (null when equal).</summary>
        public static JsonNode? Diff(JsonNode? full, JsonNode? baseNode)
        {
            if (full is JsonObject fo && baseNode is JsonObject bo)
            {
                var result = new JsonObject();
                foreach (var kv in fo.ToList())
                {
                    string? bk = FindKey(bo, kv.Key);
                    var d = Diff(kv.Value, bk != null ? bo[bk] : null);
                    if (d != null || (bk == null && kv.Value == null)) result[kv.Key] = d?.DeepClone();
                }
                return result.Count == 0 ? null : result;
            }
            string a = full?.ToJsonString() ?? "null", b = baseNode?.ToJsonString() ?? "null";
            if (a == b) return null;
            if (full != null && baseNode != null && double.TryParse(a, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x)
                && double.TryParse(b, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y) && x == y)
                return null;
            return full?.DeepClone() ?? JsonValue.Create((string?)null);
        }

        private static bool TryGet(JsonObject o, string key, out JsonNode? value)
        {
            string? k = FindKey(o, key);
            value = k != null ? o[k] : null;
            return k != null;
        }

        private static void RemoveKey(JsonObject o, string key)
        {
            string? k = FindKey(o, key);
            if (k != null) o.Remove(k);
        }

        private static string? FindKey(JsonObject o, string key) =>
            o.Select(kv => kv.Key).FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
    }
}
