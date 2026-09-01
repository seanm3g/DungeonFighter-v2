using System;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;

namespace RPGGame.Data
{
    public static partial class JsonArraySheetConverter
    {
        /// <summary>Maps common PREFIX tab column titles to <c>Modifications.json</c> property names.</summary>
        internal static void RenameModificationSheetImportHeaders(JsonObject obj)
        {
            MoveModificationJsonKeyIfPresent(obj, "DiceResult", "Dice Result", "dice result");
            MoveModificationJsonKeyIfPresent(obj, "ItemRank", "Item Rank", "item rank", "Rarity");
            MoveModificationJsonKeyIfPresent(obj, "prefixCategory", "Prefix Category", "prefix category", "Category", "category", "PREFIX CATEGORY");
            MoveModificationJsonKeyIfPresent(obj, "Name", "name", "Prefix", "prefix");
            MoveModificationJsonKeyIfPresent(obj, "Description", "description");
            MoveModificationJsonKeyIfPresent(obj, "Effect", "effect");
            MoveModificationJsonKeyIfPresent(obj, "MinValue", "Min Value", "min value", "minValue");
            MoveModificationJsonKeyIfPresent(obj, "MaxValue", "Max Value", "max value", "maxValue");
            MoveModificationJsonKeyIfPresent(obj, "RolledValue", "Rolled Value", "rolled value", "rolledValue");
            MoveModificationJsonKeyIfPresent(obj, "tags", "Tags", "TAGS");
        }

        private static void MoveModificationJsonKeyIfPresent(JsonObject obj, string canonicalKey, params string[] sourceAliases)
        {
            foreach (var key in obj.Select(kvp => kvp.Key).ToList())
            {
                if (string.Equals(key, canonicalKey, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            foreach (var alias in sourceAliases)
            {
                foreach (var kvp in obj.ToList())
                {
                    if (!string.Equals(kvp.Key, alias, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!obj.TryGetPropertyValue(kvp.Key, out var val))
                        continue;
                    obj.Remove(kvp.Key);
                    obj[canonicalKey] = val?.DeepClone() ?? JsonValue.Create("");
                    return;
                }
            }
        }
    }
}
