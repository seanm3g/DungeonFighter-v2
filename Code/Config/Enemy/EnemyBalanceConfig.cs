using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Enemy balance configuration
    /// </summary>
    public class EnemyBalanceConfig
    {
        public PoolConfig AttributePool { get; set; } = new();
        public PoolConfig SustainPool { get; set; } = new();
        public StatConversionRatesConfig StatConversionRates { get; set; } = new();
        public Dictionary<string, BaseEnemyConfig> BaseEnemyConfigs { get; set; } = new();
        public Dictionary<string, ArchetypeConfig> ArchetypeConfigs { get; set; } = new();
    }
}

