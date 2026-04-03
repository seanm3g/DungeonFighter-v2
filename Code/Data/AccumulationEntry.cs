using System.Text.Json.Serialization;

namespace RPGGame
{
    /// <summary>
    /// A single accumulation entry: pair a source (e.g. Hits landed) with a parameter it modifies (e.g. Damage) and value per unit (e.g. +5).
    /// Works like Thresholds: select accumulation type, then what it modifies and the bonus amount.
    /// </summary>
    public class AccumulationEntry
    {
        /// <summary>Accumulation source: CadenceAction, CadenceAbility, CadenceChain, CadenceFight, CadenceDungeon, SelfDamage, HealthRestored, HitsLanded, Blocks, Dodges, Kills, DamageTaken, TurnsTaken, CombosUsed.</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "HitsLanded";

        /// <summary>Parameter this accumulation modifies (e.g. Damage, Max Health, Heal). Per-unit bonus is applied to this.</summary>
        [JsonPropertyName("modifiesParam")]
        public string ModifiesParam { get; set; } = "Damage";

        /// <summary>Bonus per unit of accumulation (e.g. +5 damage per hit when ModifiesParam is Damage).</summary>
        [JsonPropertyName("value")]
        public double Value { get; set; }

        /// <summary>How value is applied: "#" = flat amount per unit, "%" = percent per unit.</summary>
        [JsonPropertyName("valueKind")]
        public string ValueKind { get; set; } = "#";

        /// <summary>Legacy: comparison operator. Kept for JSON round-trip.</summary>
        [JsonPropertyName("operator")]
        public string Operator { get; set; } = "";

        /// <summary>Legacy: param (e.g. cadence name). Migrated to Type; kept for JSON round-trip.</summary>
        [JsonPropertyName("param")]
        public string Param { get; set; } = "";
    }
}
