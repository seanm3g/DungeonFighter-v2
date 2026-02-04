using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>
    /// Status effect and heal columns.
    /// </summary>
    public partial class SpreadsheetActionJson
    {
        [JsonPropertyName("stun")]
        public string Stun { get; set; } = "";

        [JsonPropertyName("poison")]
        public string Poison { get; set; } = "";

        [JsonPropertyName("burn")]
        public string Burn { get; set; } = "";

        [JsonPropertyName("bleed")]
        public string Bleed { get; set; } = "";

        [JsonPropertyName("weaken")]
        public string Weaken { get; set; } = "";

        [JsonPropertyName("expose")]
        public string Expose { get; set; } = "";

        [JsonPropertyName("slow")]
        public string Slow { get; set; } = "";

        [JsonPropertyName("vulnerability")]
        public string Vulnerability { get; set; } = "";

        [JsonPropertyName("harden")]
        public string Harden { get; set; } = "";

        [JsonPropertyName("silence")]
        public string Silence { get; set; } = "";

        [JsonPropertyName("pierce")]
        public string Pierce { get; set; } = "";

        [JsonPropertyName("statDrain")]
        public string StatDrain { get; set; } = "";

        [JsonPropertyName("fortify")]
        public string Fortify { get; set; } = "";

        [JsonPropertyName("consume")]
        public string Consume { get; set; } = "";

        [JsonPropertyName("focus")]
        public string Focus { get; set; } = "";

        [JsonPropertyName("cleanse")]
        public string Cleanse { get; set; } = "";

        [JsonPropertyName("lifesteal")]
        public string Lifesteal { get; set; } = "";

        [JsonPropertyName("reflect")]
        public string Reflect { get; set; } = "";

        [JsonPropertyName("selfDamage")]
        public string SelfDamage { get; set; } = "";

        [JsonPropertyName("heroHeal")]
        public string HeroHeal { get; set; } = "";

        [JsonPropertyName("heroHealMaxHealth")]
        public string HeroHealMaxHealth { get; set; } = "";
    }
}
