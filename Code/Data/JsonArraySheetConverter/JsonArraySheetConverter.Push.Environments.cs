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
        private static List<IList<object>> BuildEnvironmentPushValueRows(string jsonFileText)
        {
            using var doc = JsonDocument.Parse(jsonFileText);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Expected a JSON array at the root.");

            var headers = EnvironmentsCanonicalHeaders.ToList();
            var rows = new List<IList<object>>();
            rows.Add(headers.Select(h => (object)h).ToList());

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                var row = new List<object>();
                foreach (var h in headers)
                    row.Add(GetEnvironmentPushCell(el, h));
                rows.Add(row);
            }

            return rows;
        }

        private static string GetEnvironmentPushCell(JsonElement el, string header)
        {
            return header switch
            {
                "region" => GetEnvironmentStringProperty(el, "region"),
                "biome" => GetEnvironmentBiomeCell(el),
                "location" => GetEnvironmentLocationCell(el),
                "tags" => TryGetJsonPropertyCaseInsensitive(el, "tags", out var tags)
                    ? FormatTagsArrayForSheetCell(tags)
                    : "",
                "description" => GetEnvironmentStringProperty(el, "description"),
                "actions" => TryGetJsonPropertyCaseInsensitive(el, "actions", out var actions)
                    ? FormatEnvironmentWeightedListForSheetCell(actions)
                    : "",
                "enemies" => TryGetJsonPropertyCaseInsensitive(el, "enemies", out var enemies)
                    ? FormatEnvironmentWeightedListForSheetCell(enemies)
                    : "",
                _ => ""
            };
        }

        private static string GetEnvironmentLocationCell(JsonElement el)
        {
            if (TryGetJsonPropertyCaseInsensitive(el, "location", out var loc))
                return JsonStringToCell(loc);
            if (TryGetJsonPropertyCaseInsensitive(el, "name", out var name))
                return JsonStringToCell(name);
            return "";
        }

        private static string GetEnvironmentBiomeCell(JsonElement el)
        {
            if (TryGetJsonPropertyCaseInsensitive(el, "biome", out var biome))
                return JsonStringToCell(biome);
            if (TryGetJsonPropertyCaseInsensitive(el, "theme", out var theme))
                return JsonStringToCell(theme);
            return "";
        }

        private static string GetEnvironmentStringProperty(JsonElement el, string name) =>
            TryGetJsonPropertyCaseInsensitive(el, name, out var prop) ? JsonStringToCell(prop) : "";
    }
}
