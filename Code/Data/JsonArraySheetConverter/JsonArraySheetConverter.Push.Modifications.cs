using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RPGGame;

namespace RPGGame.Data
{
    public static partial class JsonArraySheetConverter
    {
        private static List<IList<object>> BuildModificationsPushValueRows(string jsonFileText)
        {
            using var doc = JsonDocument.Parse(jsonFileText);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Expected a JSON array at the root.");

            var handledJson = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "DiceResult", "ItemRank", "Name", "Description", "Effect", "prefixCategory",
                "MinValue", "MaxValue", "RolledValue", "tags", "attributeRequirements"
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

            var headers = new List<string>(ModificationsCanonicalHeaders);
            headers.AddRange(extraKeys);

            var rows = new List<IList<object>>();
            rows.Add(headers.Select(h => (object)h).ToList());

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                var row = new List<object>();
                foreach (var h in headers)
                    row.Add(GetModificationPushCellValue(el, h));
                rows.Add(row);
            }

            return rows;
        }

        private static object GetModificationPushCellValue(JsonElement el, string header)
        {
            if (string.Equals(header, "ATTRIBUTE REQUIREMENT", StringComparison.OrdinalIgnoreCase)
                || string.Equals(header, "ATTRIBUTE REQUREMENT", StringComparison.OrdinalIgnoreCase))
            {
                GetModificationAttributeRequirementCells(el, out string statCol, out _);
                return statCol;
            }

            if (string.Equals(header, "REQUIREMENT VALUE", StringComparison.OrdinalIgnoreCase))
            {
                GetModificationAttributeRequirementCells(el, out _, out string reqVal);
                return reqVal;
            }

            if (TryGetJsonPropertyCaseInsensitive(el, header, out var prop))
            {
                if (string.Equals(header, "tags", StringComparison.OrdinalIgnoreCase)
                    && prop.ValueKind == JsonValueKind.Array)
                    return FormatTagsArrayForSheetCell(prop);
                return JsonElementToCellString(prop);
            }

            return "";
        }

        private static void GetModificationAttributeRequirementCells(
            JsonElement el, out string statCol, out string reqValCol)
        {
            statCol = "";
            reqValCol = "";
            if (!TryGetJsonPropertyCaseInsensitive(el, "attributeRequirements", out var ar)
                || ar.ValueKind == JsonValueKind.Null)
                return;

            if (ar.ValueKind != JsonValueKind.Object)
                return;

            var props = ar.EnumerateObject().ToList();
            if (props.Count != 1)
                return;

            var p = props[0];
            statCol = ModificationRequirementKeyToSheetStatCell(p.Name);
            if (p.Value.ValueKind is JsonValueKind.Number or JsonValueKind.String)
                reqValCol = JsonElementToCellString(p.Value);
        }

        /// <summary>Sheet uses lowercase stat names / categories (e.g. <c>strength</c>, <c>primary</c>).</summary>
        private static string ModificationRequirementKeyToSheetStatCell(string requirementPropertyName)
        {
            string canon = Item.CanonicalizeAttributeRequirementKey(requirementPropertyName);
            return string.IsNullOrEmpty(canon) ? "" : canon;
        }
    }
}
