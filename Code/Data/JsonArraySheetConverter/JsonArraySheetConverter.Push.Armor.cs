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
        private static List<IList<object>> BuildArmorPushValueRows(string jsonFileText)
        {
            using var doc = JsonDocument.Parse(jsonFileText);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Expected a JSON array at the root.");

            var canonical = ArmorCanonicalHeaders.ToList();
            var handledJson = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "slot", "name", "armor", "tags", "strength", "agility", "technique", "intelligence",
                "hit", "combo", "crit", "extraActionSlots",
                "minActionBonuses", "tier", "attributeRequirements"
            };

            var extraKeys = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                foreach (var p in el.EnumerateObject())
                {
                    if (!handledJson.Contains(p.Name))
                        extraKeys.Add(p.Name);
                }
            }

            var headers = new List<string>(canonical);
            headers.AddRange(extraKeys);

            var rows = new List<IList<object>>();
            rows.Add(headers.Select(h => (object)h).ToList());

            foreach (var el in SortArmorElementsForPush(doc.RootElement.EnumerateArray()))
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                var row = new List<object>();
                foreach (var h in headers)
                    row.Add(GetArmorPushCellValue(el, h));
                rows.Add(row);
            }

            return rows;
        }

        private static object GetArmorPushCellValue(JsonElement el, string header)
        {
            if (string.Equals(header, "attributeRequirements", StringComparison.Ordinal))
            {
                GetArmorAttributeRequirementCells(el, out string attrCol, out _);
                return attrCol;
            }

            if (string.Equals(header, "requirement value", StringComparison.Ordinal))
            {
                GetArmorAttributeRequirementCells(el, out _, out string reqVal);
                return reqVal;
            }

            string? jsonName = MapArmorSheetHeaderToJsonProperty(header);
            if (jsonName != null && TryGetJsonPropertyCaseInsensitive(el, jsonName, out var mapped))
            {
                if (string.Equals(jsonName, "tags", StringComparison.OrdinalIgnoreCase) && mapped.ValueKind == JsonValueKind.Array)
                    return FormatTagsArrayForSheetCell(mapped);
                return JsonElementToCellString(mapped);
            }

            if (TryGetJsonPropertyCaseInsensitive(el, header, out var direct))
            {
                if (string.Equals(header, "tags", StringComparison.OrdinalIgnoreCase) && direct.ValueKind == JsonValueKind.Array)
                    return FormatTagsArrayForSheetCell(direct);
                return JsonElementToCellString(direct);
            }

            return "";
        }

        private static string? MapArmorSheetHeaderToJsonProperty(string header) =>
            header switch
            {
                "STRENGTH" => "strength",
                "AGILITY" => "agility",
                "TECHNIQUE" => "technique",
                "INTELLIGENCE" => "intelligence",
                "HIT" => "hit",
                "COMBO" => "combo",
                "CRIT" => "crit",
                "# OF ACTION SLOTS" => "extraActionSlots",
                "# OF BONUS ACTIONS" => "minActionBonuses",
                "slot" => "slot",
                "name" => "name",
                "armor" => "armor",
                "tags" => "tags",
                "tier" => "tier",
                _ => null
            };

        private static void GetArmorAttributeRequirementCells(JsonElement el, out string attrCol, out string reqValCol)
        {
            attrCol = "";
            reqValCol = "";
            if (!TryGetJsonPropertyCaseInsensitive(el, "attributeRequirements", out var ar) || ar.ValueKind == JsonValueKind.Null)
                return;

            if (ar.ValueKind == JsonValueKind.Object)
            {
                var props = ar.EnumerateObject().ToList();
                if (props.Count == 1)
                {
                    var p = props[0];
                    attrCol = ArmorRequirementKeyToSheetStatColumn(p.Name);
                    reqValCol = p.Value.ValueKind == JsonValueKind.Number
                        ? JsonElementToCellString(p.Value)
                        : "";
                }
                else
                    attrCol = ar.GetRawText();
            }
            else if (ar.ValueKind == JsonValueKind.String)
            {
                attrCol = ar.GetString() ?? "";
                if (TryGetJsonPropertyCaseInsensitive(el, "requirement value", out var rv) &&
                    rv.ValueKind is JsonValueKind.Number or JsonValueKind.String)
                    reqValCol = JsonElementToCellString(rv);
            }
        }

        private static string ArmorRequirementKeyToSheetStatColumn(string requirementPropertyName)
        {
            string canon = Item.CanonicalizeAttributeRequirementKey(requirementPropertyName);
            return canon switch
            {
                "strength" => "STRENGTH",
                "agility" => "AGILITY",
                "technique" => "TECHNIQUE",
                "intelligence" => "INTELLIGENCE",
                "primary" => "PRIMARY",
                "secondary" => "SECONDARY",
                "neglected" => "NEGLECTED",
                "weakness" => "WEAKNESS",
                _ => string.IsNullOrEmpty(canon) ? "" : canon.ToUpperInvariant()
            };
        }
    }
}
