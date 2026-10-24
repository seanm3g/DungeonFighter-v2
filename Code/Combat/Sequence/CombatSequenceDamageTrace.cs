namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// Snapshot of one swing's damage formula for the sequence HUD (filled while damage is calculated).
    /// </summary>
    public sealed class CombatSequenceDamageTrace
    {
        public int BaseDamage { get; set; }
        public double ActionMultiplier { get; set; } = 1.0;
        public double Amp { get; set; } = 1.0;
        public int ConvertFlat { get; set; }
        public int Raw { get; set; }
        public int Block { get; set; }
        public int Final { get; set; }
        public bool CritDamage { get; set; }

        /// <summary>Hero STR damage multiplier (1.0 for enemies / legacy flat mode).</summary>
        public double StrengthMultiplier { get; set; } = 1.0;

        /// <summary>Roll band that scaled damage: crit, action roll or normal hit (empty when no roll).</summary>
        public string RollBand { get; set; } = "";
        public double RollBandMultiplier { get; set; } = 1.0;

        /// <summary>Weapon class balance multiplier for heroes.</summary>
        public double ClassMultiplier { get; set; } = 1.0;

        /// <summary>Damage before the final floor, after every multiplier in <see cref="Calculators.DamageCalculator.CalculateRawDamage"/>.</summary>
        public double PreRoundTotal { get; set; }

        /// <summary>Raw damage after status/set outgoing and tag modifiers, before the target's armor.</summary>
        public int BeforeArmor { get; set; }
    }
}
