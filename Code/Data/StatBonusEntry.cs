using System.Text.Json.Serialization;

namespace RPGGame
{
    /// <summary>
    /// A single stat bonus entry for an action (value + type). Duration is from Cadence.
    /// </summary>
    public class StatBonusEntry
    {
        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";
    }
}
