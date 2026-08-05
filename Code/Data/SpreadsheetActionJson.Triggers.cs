using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>
    /// Trigger columns and JSON-serialized form data (stat bonuses, thresholds, accumulations).
    /// </summary>
    public partial class SpreadsheetActionJson
    {
        [JsonPropertyName("onHit")]
        public string OnHit { get; set; } = "";

        [JsonPropertyName("onMiss")]
        public string OnMiss { get; set; } = "";

        [JsonPropertyName("onCrit")]
        public string OnCrit { get; set; } = "";

        [JsonPropertyName("onKill")]
        public string OnKill { get; set; } = "";

        [JsonPropertyName("onRoomsCleared")]
        public string OnRoomsCleared { get; set; } = "";

        [JsonPropertyName("onRollValue")]
        public string OnRollValue { get; set; } = "";

        [JsonPropertyName("triggerConditions")]
        public string TriggerConditions { get; set; } = "";

        /// <summary>JSON list of trigger triples (WHEN / SCOPE / mechanic pointers).</summary>
        [JsonPropertyName("triggerBundlesJson")]
        public string TriggerBundlesJson { get; set; } = "";

        [JsonPropertyName("statBonusesJson")]
        public string StatBonusesJson { get; set; } = "";

        [JsonPropertyName("thresholdsJson")]
        public string ThresholdsJson { get; set; } = "";

        [JsonPropertyName("accumulationsJson")]
        public string AccumulationsJson { get; set; } = "";

        [JsonPropertyName("chainPositionBonusesJson")]
        public string ChainPositionBonusesJson { get; set; } = "";

        [JsonPropertyName("actionAttackBonusesJson")]
        public string ActionAttackBonusesJson { get; set; } = "";
    }
}
