using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame
{
    /// <summary>
    /// Deserializes JSON <c>null</c> (common for empty spreadsheet cells) as <c>0</c> for <see cref="int"/> properties.
    /// </summary>
    public sealed class JsonInt32NullAsZeroConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Null => 0,
                JsonTokenType.String => int.TryParse(reader.GetString(), out int parsed)
                    ? parsed
                    : throw new JsonException("Cannot convert string to Int32 for armor/loot numeric field."),
                _ => reader.GetInt32(),
            };
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value);
    }
}
