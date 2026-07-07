using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RPGGame.Data
{
    /// <summary>
    /// Represents a single bonus within an ABILITY/ACTION keyword bonus group
    /// </summary>
    public class ActionAttackBonusItem
    {
        private string _type = "";

        /// <summary>ACCURACY, HIT, COMBO, CRIT, CRIT_MISS, STR, AGI, TECH, INT. Legacy <c>ROLL</c> is normalized to ACCURACY.</summary>
        [JsonPropertyName("type")]
        public string Type
        {
            get => _type;
            set => _type = NormalizeBonusType(value);
        }

        /// <summary>Maps legacy roll-modifier keyword <c>ROLL</c> to ACCURACY for FIFO/slot/ATTACK bonuses.</summary>
        public static string NormalizeBonusType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type)) return "";
            var t = type.Trim();
            if (string.Equals(t, "ROLL", StringComparison.OrdinalIgnoreCase))
                return "ACCURACY";
            return t;
        }

        [JsonPropertyName("value")]
        public double Value { get; set; } = 0.0;
    }
    
    /// <summary>
    /// Represents a group of bonuses applied via TURN or ACTION cadence.
    /// CadenceType: "ACTION" = combo FIFO layers; "TURN" = roll-based (per turn).
    /// </summary>
    public class ActionAttackBonusGroup
    {
        [JsonPropertyName("keyword")]
        public string Keyword { get; set; } = "";
        
        /// <summary>Authoritative cadence from spreadsheet. "ACTION" = combo FIFO; "TURN" = per roll.</summary>
        [JsonPropertyName("cadenceType")]
        public string CadenceType { get; set; } = "";
        
        [JsonPropertyName("count")]
        public int Count { get; set; } = 1; // X in "for next X TURNs/ACTIONs"
        
        [JsonPropertyName("bonuses")]
        public List<ActionAttackBonusItem> Bonuses { get; set; } = new List<ActionAttackBonusItem>();
        
        [JsonPropertyName("durationType")]
        public string DurationType { get; set; } = ""; // "ABILITY", "ACTION", "FIGHT", "DUNGEON", "CHAIN", "COMBO", or empty for default
    }
    
    /// <summary>
    /// Container for all TURN/ACTION cadence keyword bonuses for an action
    /// </summary>
    public class ActionAttackBonuses
    {
        [JsonPropertyName("bonusGroups")]
        public List<ActionAttackBonusGroup> BonusGroups { get; set; } = new List<ActionAttackBonusGroup>();
    }
}
