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

        /// <summary>Workshop set stamped from ACTIONS sheet "TIER N ACTIONS" markers (0 before any marker).</summary>
        [JsonPropertyName("tier")]
        public int Tier { get; set; }

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

        /// <summary>Declarative comma-separated mechanic IDs (MECHANICS column).</summary>
        [JsonPropertyName("mechanics")]
        public string Mechanics { get; set; } = "";

        /// <summary>STATUS EFFECT / CADENCE DURATION — modifier application count (ACTION x2).</summary>
        [JsonPropertyName("cadenceDuration")]
        public string CadenceDuration { get; set; } = "";

        /// <summary>CADENCES triples JSON (enable / duration / mechanic pointers).</summary>
        [JsonPropertyName("cadenceBundlesJson")]
        public string CadenceBundlesJson { get; set; } = "";

        [JsonPropertyName("opener")]
        public string Opener { get; set; } = "";

        [JsonPropertyName("finisher")]
        public string Finisher { get; set; } = "";

        /// <summary>RESERVE POOL column — truthy / empty; maps to <c>reserve_pool</c> tag.</summary>
        [JsonPropertyName("reservePool")]
        public string ReservePool { get; set; } = "";
    }
}
