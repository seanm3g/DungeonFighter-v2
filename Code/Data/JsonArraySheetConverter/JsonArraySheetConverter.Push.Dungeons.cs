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
        private static List<IList<object>> BuildDungeonsPushValueRows(string jsonFileText)
        {
            using var doc = JsonDocument.Parse(jsonFileText);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Expected a JSON array at the root.");

            var headers = DungeonsCanonicalHeaders.ToList();
            var rows = new List<IList<object>>();
            rows.Add(headers.Select(h => (object)h).ToList());

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                var row = new List<object>();
                foreach (var h in headers)
                    row.Add(GetDungeonsPushCell(el, h));
                rows.Add(row);
            }

            return rows;
        }

        private static string GetDungeonsPushCell(JsonElement el, string header) =>
            header switch
            {
                "name" => GetEnvironmentStringProperty(el, "name"),
                "theme" => GetEnvironmentStringProperty(el, "theme"),
                "minLevel" => TryGetJsonPropertyCaseInsensitive(el, "minLevel", out var min)
                    ? JsonElementToCellString(min)
                    : "",
                "maxLevel" => TryGetJsonPropertyCaseInsensitive(el, "maxLevel", out var max)
                    ? JsonElementToCellString(max)
                    : "",
                "possibleEnemies" => TryGetJsonPropertyCaseInsensitive(el, "possibleEnemies", out var pe)
                    ? FormatPipeDelimitedStringArrayForSheetCell(pe)
                    : "",
                "colorOverride" => TryGetJsonPropertyCaseInsensitive(el, "colorOverride", out var color)
                    ? JsonElementToCellString(color)
                    : "",
                _ => ""
            };
    }
}
