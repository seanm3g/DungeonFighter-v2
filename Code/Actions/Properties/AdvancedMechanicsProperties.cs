namespace RPGGame
{
    /// <summary>
    /// Advanced mechanics properties for actions
    /// </summary>
    public class AdvancedMechanicsProperties
    {
        public int MultiHitCount { get; set; } = 1;
        public int SelfDamagePercent { get; set; } = 0;
        public int RollBonus { get; set; } = 0;
        public int StatBonus { get; set; } = 0;
        public string StatBonusType { get; set; } = "";
        public int StatBonusDuration { get; set; } = 0;
        public bool SkipNextTurn { get; set; } = false;
        public bool GuaranteeNextSuccess { get; set; } = false;
        public int HealAmount { get; set; } = 0;
        public double HealthThreshold { get; set; } = 0.0;
        public double StatThreshold { get; set; } = 0.0;
        public string StatThresholdType { get; set; } = "";
        public double ConditionalDamageMultiplier { get; set; } = 1.0;
        public bool RepeatLastAction { get; set; } = false;
        public int ExtraAttacks { get; set; } = 0;
        public double ComboAmplifierMultiplier { get; set; } = 1.0;
        public int EnemyRollPenalty { get; set; } = 0;
        public int ExtraDamage { get; set; } = 0;
        public int ExtraDamageDecay { get; set; } = 0;
        public double DamageReduction { get; set; } = 0.0;
        public int DamageReductionDecay { get; set; } = 0;
        public double SelfAttackChance { get; set; } = 0.0;
        public bool ResetEnemyCombo { get; set; } = false;
        public bool StunEnemy { get; set; } = false;
        public int StunDuration { get; set; } = 0;
        public bool ReduceLengthNextActions { get; set; } = false;
        public double LengthReduction { get; set; } = 0.0;
        public int LengthReductionDuration { get; set; } = 0;
    }
}

