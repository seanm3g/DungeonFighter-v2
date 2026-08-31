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
    }
}
