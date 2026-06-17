using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using RPGGame;

namespace RPGGame.Data
{
    /// <summary>Sort order helpers for weapons and armor catalog JSON arrays.</summary>
    internal static class ItemCatalogSortHelper
    {
        internal static int GetWeaponTypeSortRank(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return int.MaxValue;

            if (Enum.TryParse(type.Trim(), ignoreCase: true, out WeaponType weaponType))
            {
                int orderIndex = Array.IndexOf(ClassPresentationConfig.ClassWeaponOrder, weaponType);
                return orderIndex >= 0 ? orderIndex : int.MaxValue - 1;
            }

            return int.MaxValue;
        }

        internal static int GetArmorSlotSortRank(string? slot) =>
            (slot ?? "").Trim().ToLowerInvariant() switch
            {
                "head" => 0,
                "chest" => 1,
                "legs" => 2,
                "feet" => 3,
                _ => 99
            };

        internal static string SortItemCatalogJsonArrayText(string jsonFileText, GameDataTabularSheetKind kind)
        {
            if (kind != GameDataTabularSheetKind.Weapons && kind != GameDataTabularSheetKind.Armor)
                return jsonFileText;

            JsonNode? root = JsonNode.Parse(string.IsNullOrWhiteSpace(jsonFileText) ? "[]" : jsonFileText.Trim());
            if (root is not JsonArray arr)
                throw new InvalidOperationException("Expected a JSON array at the root.");

            SortJsonArrayForItemCatalog(arr, kind);
            return arr.ToJsonString(JsonArraySheetSerializerOptions.GetSerializerOptions(kind));
        }

        internal static void SortJsonArrayForItemCatalog(JsonArray arr, GameDataTabularSheetKind kind)
        {
            var objects = arr.Where(n => n is JsonObject).Cast<JsonObject>().ToList();
            if (objects.Count <= 1)
                return;

            IEnumerable<JsonObject> sorted = kind switch
            {
                GameDataTabularSheetKind.Weapons => objects
                    .OrderBy(GetWeaponTypeSortRankFromJsonObject)
                    .ThenBy(GetTierFromJsonObject)
                    .ThenBy(GetNameFromJsonObject, StringComparer.OrdinalIgnoreCase),
                GameDataTabularSheetKind.Armor => objects
                    .OrderBy(GetArmorSlotSortRankFromJsonObject)
                    .ThenBy(GetTierFromJsonObject)
                    .ThenBy(GetNameFromJsonObject, StringComparer.OrdinalIgnoreCase),
                _ => objects
            };

            arr.Clear();
            foreach (var obj in sorted)
                arr.Add(obj.DeepClone());
        }

        internal static IEnumerable<JsonElement> SortWeaponElementsForPush(IEnumerable<JsonElement> elements) =>
            elements
                .Where(el => el.ValueKind == JsonValueKind.Object)
                .OrderBy(GetWeaponTypeSortRankFromElement)
                .ThenBy(GetTierFromElement)
                .ThenBy(GetNameFromElement, StringComparer.OrdinalIgnoreCase);

        internal static IEnumerable<JsonElement> SortArmorElementsForPush(IEnumerable<JsonElement> elements) =>
            elements
                .Where(el => el.ValueKind == JsonValueKind.Object)
                .OrderBy(GetArmorSlotSortRankFromElement)
                .ThenBy(GetTierFromElement)
                .ThenBy(GetNameFromElement, StringComparer.OrdinalIgnoreCase);

        private static int GetWeaponTypeSortRankFromElement(JsonElement el) =>
            GetWeaponTypeSortRank(SheetJsonPropertyHelper.GetJsonStringProperty(el, "type"));

        private static int GetArmorSlotSortRankFromElement(JsonElement el) =>
            GetArmorSlotSortRank(SheetJsonPropertyHelper.GetJsonStringProperty(el, "slot"));

        private static int GetWeaponTypeSortRankFromJsonObject(JsonObject obj) =>
            GetWeaponTypeSortRank(SheetJsonPropertyHelper.GetJsonObjectStringProperty(obj, "type"));

        private static int GetArmorSlotSortRankFromJsonObject(JsonObject obj) =>
            GetArmorSlotSortRank(SheetJsonPropertyHelper.GetJsonObjectStringProperty(obj, "slot"));

        private static int GetTierFromJsonObject(JsonObject obj) => SheetJsonPropertyHelper.GetJsonObjectIntProperty(obj, "tier");

        private static string GetNameFromJsonObject(JsonObject obj) => SheetJsonPropertyHelper.GetJsonObjectStringProperty(obj, "name");

        private static int GetTierFromElement(JsonElement el) => SheetJsonPropertyHelper.GetJsonIntProperty(el, "tier");

        private static string GetNameFromElement(JsonElement el) => SheetJsonPropertyHelper.GetJsonStringProperty(el, "name");
    }
}
