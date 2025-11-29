namespace RPGGame
{
    /// <summary>
    /// Enemy scaling configuration
    /// </summary>
    public class EnemyScalingConfig
    {
        public double EnemyHealthMultiplier { get; set; }
        public double EnemyDamageMultiplier { get; set; }
        public int EnemyLevelVariance { get; set; }
        public double BaseDPSAtLevel1 { get; set; }
        public double DPSPerLevel { get; set; }
        public int EnemyBaseArmorAtLevel1 { get; set; }
        public double EnemyArmorPerLevel { get; set; }
    }
}

