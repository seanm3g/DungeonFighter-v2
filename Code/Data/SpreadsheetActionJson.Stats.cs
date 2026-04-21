using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>
    /// Hero/enemy bonuses and stat columns (spreadsheet columns N–AG).
    /// </summary>
    public partial class SpreadsheetActionJson
    {
        [JsonPropertyName("heroAccuracy")]
        public string HeroAccuracy { get; set; } = "";

        [JsonPropertyName("heroHit")]
        public string HeroHit { get; set; } = "";

        [JsonPropertyName("heroCombo")]
        public string HeroCombo { get; set; } = "";

        [JsonPropertyName("heroCrit")]
        public string HeroCrit { get; set; } = "";

        /// <summary>Crit miss threshold adjustment (Roll bonus: Crit Miss); not in legacy sheet columns N–Q.</summary>
        [JsonPropertyName("heroCritMiss")]
        public string HeroCritMiss { get; set; } = "";

        [JsonPropertyName("enemyAccuracy")]
        public string EnemyAccuracy { get; set; } = "";

        [JsonPropertyName("enemyHit")]
        public string EnemyHit { get; set; } = "";

        [JsonPropertyName("enemyCombo")]
        public string EnemyCombo { get; set; } = "";

        [JsonPropertyName("enemyCrit")]
        public string EnemyCrit { get; set; } = "";

        /// <summary>Crit miss threshold adjustment for enemy attacker (ENEMY DICE MODIFICATIONS / CRIT MISS).</summary>
        [JsonPropertyName("enemyCritMiss")]
        public string EnemyCritMiss { get; set; } = "";

        [JsonPropertyName("heroSTR")]
        public string HeroSTR { get; set; } = "";

        [JsonPropertyName("heroAGI")]
        public string HeroAGI { get; set; } = "";

        [JsonPropertyName("heroTECH")]
        public string HeroTECH { get; set; } = "";

        [JsonPropertyName("heroINT")]
        public string HeroINT { get; set; } = "";

        [JsonPropertyName("enemySTR")]
        public string EnemySTR { get; set; } = "";

        [JsonPropertyName("enemyAGI")]
        public string EnemyAGI { get; set; } = "";

        [JsonPropertyName("enemyTECH")]
        public string EnemyTECH { get; set; } = "";

        [JsonPropertyName("enemyINT")]
        public string EnemyINT { get; set; } = "";

        [JsonPropertyName("speedMod")]
        public string SpeedMod { get; set; } = "";

        [JsonPropertyName("damageMod")]
        public string DamageMod { get; set; } = "";

        [JsonPropertyName("multiHitMod")]
        public string MultiHitMod { get; set; } = "";

        [JsonPropertyName("ampMod")]
        public string AmpMod { get; set; } = "";

        [JsonPropertyName("enemySpeedMod")]
        public string EnemySpeedMod { get; set; } = "";

        [JsonPropertyName("enemyDamageMod")]
        public string EnemyDamageMod { get; set; } = "";

        [JsonPropertyName("enemyMultiHitMod")]
        public string EnemyMultiHitMod { get; set; } = "";

        [JsonPropertyName("enemyAmpMod")]
        public string EnemyAmpMod { get; set; } = "";
    }
}
