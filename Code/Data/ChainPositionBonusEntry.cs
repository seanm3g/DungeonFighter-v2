using System.Text.Json.Serialization;

namespace RPGGame
{
    /// <summary>
    /// Bonus that scales with combo chain position (slot index or amp tier). Applied when
    /// <see cref="ActionData.ModifyBasedOnChainPosition"/> / <see cref="ComboRoutingProperties.ModifyBasedOnChainPosition"/> is enabled (non-empty, not "false").
    /// </summary>
    public class ChainPositionBonusEntry
    {
        /// <summary>Accuracy, EnemyAccuracy, Damage, or MultiHit (legacy: RollBonus / EnemyRollBonus; <c>ROLL</c> is treated as accuracy).</summary>
        [JsonPropertyName("modifiesParam")]
        public string ModifiesParam { get; set; } = "";

        /// <summary>Bonus per unit of position (see <see cref="PositionBasis"/>).</summary>
        [JsonPropertyName("value")]
        public double Value { get; set; }

        /// <summary>
        /// "#" = flat scaling with position for Roll/MultiHit/Damage (Damage: adds <c>value * positionFactor</c> to combo multiplier).
        /// "%" = Damage only: multiply combo multiplier by <c>1 + (value/100) * positionFactor</c> per matching row.
        /// </summary>
        [JsonPropertyName("valueKind")]
        public string ValueKind { get; set; } = "#";

        /// <summary>
        /// ComboSlotIndex0 (0-based coefficient: opener 0), ComboSlotIndex1 (1-based slot), AmpTier, or empty for
        /// 1-based slot scaling (same coefficient as ComboSlotIndex1).
        /// </summary>
        [JsonPropertyName("positionBasis")]
        public string PositionBasis { get; set; } = "";
    }
}
