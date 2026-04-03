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
    /// Represents a group of bonuses applied via ABILITY, ACTION, or ATTACK keyword.
    /// CadenceType: "ACTION" = slot-based (next action in combo); "ATTACK" = roll-based (next roll); "ABILITY" = consumed on hit.
    /// </summary>
    public class ActionAttackBonusGroup
    {
        [JsonPropertyName("keyword")]
        public string Keyword { get; set; } = ""; // "ABILITY", "ACTION", or "ATTACK"
        
        /// <summary>Authoritative cadence from spreadsheet. "ACTION" = slot-based; "ATTACK" = roll-based; "ABILITY" = on hit.</summary>
        [JsonPropertyName("cadenceType")]
        public string CadenceType { get; set; } = "";
        
        [JsonPropertyName("count")]
        public int Count { get; set; } = 1; // X in "for next X ACTIONs/ATTACKs"
        
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
