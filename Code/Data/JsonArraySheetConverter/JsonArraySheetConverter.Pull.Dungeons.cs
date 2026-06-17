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
        private static void NormalizeDungeonsJsonArrayRow(JsonObject obj)
        {
            if (!obj.TryGetPropertyValue("possibleEnemies", out var node) || node is null)
                return;

            if (node is JsonArray)
                return;

            if (node is not JsonValue jv || !jv.TryGetValue<string>(out var s) || string.IsNullOrWhiteSpace(s))
                return;

            var parts = s.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var arr = new JsonArray();
            foreach (var p in parts)
                arr.Add(p);
            obj["possibleEnemies"] = arr;
        }
    }
}
