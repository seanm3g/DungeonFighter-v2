using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>Merge and split Modifications + PrefixMaterialQuality JSON arrays for sheet push/pull.</summary>
    internal static class ModificationsJsonArrayMerger
    {
        internal static string MergeJsonRootArrays(string? firstJson, string? secondJson)
        {
            static JsonArray ParseArrayOrEmpty(string? text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return new JsonArray();
                try
                {
                    var n = JsonNode.Parse(text.Trim());
                    return n is JsonArray arr ? arr : new JsonArray();
                }
                catch (JsonException)
                {
                    return new JsonArray();
                }
            }

            var a = ParseArrayOrEmpty(firstJson);
            var b = ParseArrayOrEmpty(secondJson);
            var merged = new JsonArray();
            foreach (var x in a)
                merged.Add(x?.DeepClone());
            foreach (var x in b)
                merged.Add(x?.DeepClone());
            return merged.ToJsonString();
        }

        internal static (string coreModificationsJson, string prefixMaterialQualityJson) SplitModificationsMergedJson(
            string mergedArrayJson)
        {
            JsonNode? root = JsonNode.Parse(string.IsNullOrWhiteSpace(mergedArrayJson) ? "[]" : mergedArrayJson.Trim());
            if (root is not JsonArray arr)
                throw new InvalidOperationException("Expected a JSON array at the root.");

            var core = new JsonArray();
            var pmq = new JsonArray();
            var opts = JsonArraySheetSerializerOptions.GetSerializerOptions(GameDataTabularSheetKind.Modifications);

            foreach (var el in arr)
            {
                if (el is not JsonObject o)
                    continue;

                var slot = ItemPrefixHelper.ParsePrefixCategory(ReadModificationPrefixCategoryRaw(o));
                if (slot == ModificationPrefixCategory.Material || slot == ModificationPrefixCategory.Quality)
                    pmq.Add(el.DeepClone());
                else
                    core.Add(el.DeepClone());
            }

            return (core.ToJsonString(opts), pmq.ToJsonString(opts));
        }

        private static string? ReadModificationPrefixCategoryRaw(JsonObject o)
        {
            foreach (string key in new[] { "prefixCategory", "PrefixCategory" })
            {
                if (!o.TryGetPropertyValue(key, out JsonNode? node) || node is null)
                    continue;
                if (node is JsonValue jv)
                {
                    if (jv.TryGetValue(out string? s))
                        return string.IsNullOrEmpty(s) ? s : s.Trim();
                    try
                    {
                        var v = jv.GetValue<string?>();
                        return string.IsNullOrEmpty(v) ? v : v.Trim();
                    }
                    catch (InvalidOperationException)
                    {
                        return jv.ToString()?.Trim();
                    }
                }
            }

            return null;
        }
    }
}
