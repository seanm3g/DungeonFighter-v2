using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>
    /// Core and identity columns (spreadsheet columns A–M).
    /// </summary>
    public partial class SpreadsheetActionJson
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("columnC")]
        public string ColumnC { get; set; } = "";

        [JsonPropertyName("rarity")]
        public string Rarity { get; set; } = "";

        [JsonPropertyName("category")]
        public string Category { get; set; } = "";

        [JsonPropertyName("dps")]
        public string DPS { get; set; } = "";

        [JsonPropertyName("numberOfHits")]
        public string NumberOfHits { get; set; } = "";

        [JsonPropertyName("damage")]
        public string Damage { get; set; } = "";

        [JsonPropertyName("speed")]
        public string Speed { get; set; } = "";

        [JsonPropertyName("duration")]
        public string Duration { get; set; } = "";

        [JsonPropertyName("cadence")]
        public string Cadence { get; set; } = "";

        [JsonPropertyName("opener")]
        public string Opener { get; set; } = "";

        [JsonPropertyName("finisher")]
        public string Finisher { get; set; } = "";
    }
}
