using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>
    /// Threshold and attribute columns, tags, and flags.
    /// </summary>
    public partial class SpreadsheetActionJson
    {
        [JsonPropertyName("target")]
        public string Target { get; set; } = "";

        [JsonPropertyName("thresholdCategory")]
        public string ThresholdCategory { get; set; } = "";

        [JsonPropertyName("thresholdAmount")]
        public string ThresholdAmount { get; set; } = "";

        [JsonPropertyName("bonus")]
        public string Bonus { get; set; } = "";

        [JsonPropertyName("bonusAttribute")]
        public string BonusAttribute { get; set; } = "";

        [JsonPropertyName("value")]
        public string Value { get; set; } = "";

        [JsonPropertyName("attribute")]
        public string Attribute { get; set; } = "";

        [JsonPropertyName("reset")]
        public string Reset { get; set; } = "";

        [JsonPropertyName("resetBlockerBuffer")]
        public string ResetBlockerBuffer { get; set; } = "";

        [JsonPropertyName("modifyRoom")]
        public string ModifyRoom { get; set; } = "";

        [JsonPropertyName("tags")]
        public string Tags { get; set; } = "";

        [JsonPropertyName("isDefaultAction")]
        public string IsDefaultAction { get; set; } = "";

        /// <summary>Comma-separated weapon types (e.g. "Sword, Dagger") for round-trip from Actions settings.</summary>
        [JsonPropertyName("weaponTypes")]
        public string WeaponTypes { get; set; } = "";
    }
}
