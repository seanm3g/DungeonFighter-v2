using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>
    /// Represents a single bonus within an ABILITY/ACTION keyword bonus group
    /// </summary>
    public class ActionAttackBonusItem
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = ""; // ACCURACY, HIT, COMBO, CRIT, CRIT_MISS, STR, AGI, TECH, INT
        
        [JsonPropertyName("value")]
        public double Value { get; set; } = 0.0;
    }
    
    /// <summary>
    /// Represents a group of bonuses applied via ABILITY or ACTION keyword
    /// </summary>
    public class ActionAttackBonusGroup
    {
        [JsonPropertyName("keyword")]
        public string Keyword { get; set; } = ""; // "ABILITY" or "ACTION"
        
        [JsonPropertyName("count")]
        public int Count { get; set; } = 1; // Number of applications
        
        [JsonPropertyName("bonuses")]
        public List<ActionAttackBonusItem> Bonuses { get; set; } = new List<ActionAttackBonusItem>();
        
        [JsonPropertyName("durationType")]
        public string DurationType { get; set; } = ""; // "ABILITY", "ACTION", "FIGHT", "DUNGEON", "CHAIN", "COMBO", or empty for default
    }
    
    /// <summary>
    /// Container for all ABILITY/ACTION keyword bonuses for an action
    /// </summary>
    public class ActionAttackBonuses
    {
        [JsonPropertyName("bonusGroups")]
        public List<ActionAttackBonusGroup> BonusGroups { get; set; } = new List<ActionAttackBonusGroup>();
    }
}
