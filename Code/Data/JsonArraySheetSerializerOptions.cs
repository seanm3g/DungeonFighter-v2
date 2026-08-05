using System;
using System.Text.Json;

namespace RPGGame.Data
{
    /// <summary>JSON serializer options per tabular sheet kind (camelCase vs PascalCase).</summary>
    internal static class JsonArraySheetSerializerOptions
    {
        internal static JsonSerializerOptions GetSerializerOptions(GameDataTabularSheetKind kind)
        {
            var o = new JsonSerializerOptions { WriteIndented = true };
            if (kind == GameDataTabularSheetKind.Weapons || kind == GameDataTabularSheetKind.Armor
                || kind == GameDataTabularSheetKind.Enemies || kind == GameDataTabularSheetKind.Environments
                || kind == GameDataTabularSheetKind.Dungeons || kind == GameDataTabularSheetKind.Consumables
                || kind == GameDataTabularSheetKind.Triggers)
                o.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            else
                o.PropertyNamingPolicy = null;
            return o;
        }
    }
}
