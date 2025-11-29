using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Enemy DPS configuration
    /// </summary>
    public class EnemyDPSConfig
    {
        public double BaseDPSAtLevel1 { get; set; }
        public double DPSPerLevel { get; set; }
        public string DPSScalingFormula { get; set; } = "";
        public Dictionary<string, EnemyArchetypeConfig> Archetypes { get; set; } = new();
        public DPSBalanceValidationConfig BalanceValidation { get; set; } = new();
        public SustainBalanceConfig SustainBalance { get; set; } = new();
    }
}

