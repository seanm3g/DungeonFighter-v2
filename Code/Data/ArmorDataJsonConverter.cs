using System;
using System.Text.Json;
using RPGGame;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>
    /// Ensures <see cref="ArmorData"/> can deserialize from Google Sheets–style rows where
    /// <c>attributeRequirements</c> is a stat abbrev string paired with <c>requirement value</c>,
    /// not only from a <c>Dictionary&lt;string,int&gt;</c> JSON object.
    /// Bulk repair still runs in <see cref="GameDataJsonNormalizer.NormalizeArmorJson"/>; this converter is a safety net.
    /// </summary>
    public sealed class ArmorDataJsonConverter : JsonConverter<ArmorData>
    {
        private static readonly JsonSerializerOptions InnerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public override ArmorData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                throw new JsonException("ArmorData must be a JSON object.");

            JsonNode? node = JsonNode.Parse(doc.RootElement.GetRawText());
            if (node is not JsonObject jo)
                throw new JsonException("ArmorData must be a JSON object.");

            GameDataJsonNormalizer.NormalizeArmorDataRow(jo);
            return JsonSerializer.Deserialize<ArmorData>(jo, InnerOptions);
        }

        public override void Write(Utf8JsonWriter writer, ArmorData value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value, InnerOptions);
    }
}
