using System.Text.Json.Serialization;

namespace RPGGame
{
    /// <summary>
    /// A single threshold entry for an action: qualifier (Enemy/Hero/Environment), attribute type, operator, and value.
    /// e.g. Enemy Health &gt; 99, Hero Strength &lt;= 0.5.
    /// </summary>
    public class ThresholdEntry
    {
        /// <summary>Whose attribute: Enemy, Hero, Environment. Empty = context-dependent.</summary>
        [JsonPropertyName("qualifier")]
        public string Qualifier { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "Health";

        /// <summary>Comparison operator: &lt;, &gt;, =, &lt;=, &gt;=, !=. Empty = legacy (value as fraction 0.0-1.0).</summary>
        [JsonPropertyName("operator")]
        public string Operator { get; set; } = "";

        /// <summary>How value is interpreted: "#" = absolute number, "%" = percent of max (e.g. for Health, % of max health).</summary>
        [JsonPropertyName("valueKind")]
        public string ValueKind { get; set; } = "#";

        [JsonPropertyName("value")]
        public double Value { get; set; }
    }
}
