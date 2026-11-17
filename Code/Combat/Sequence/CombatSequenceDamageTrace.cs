using System;

namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// Snapshot of one swing's damage formula for the sequence HUD (filled while damage is calculated).
    /// </summary>
    public sealed class CombatSequenceDamageTrace
    {
        public int BaseDamage { get; set; }
        /// <summary>Action multiplier including <see cref="NextActionDamageModPercent"/> (the product the damage used).</summary>
        public double ActionMultiplier { get; set; } = 1.0;

        /// <summary>Banked "next action" DAMAGE_MOD % (e.g. WIND UP's +25%) folded into <see cref="ActionMultiplier"/>.</summary>
        public double NextActionDamageModPercent { get; set; }

        /// <summary><see cref="ActionMultiplier"/> without the next-action bonus.</summary>
        public double ActionMultiplierWithoutNextActionMod =>
            Math.Abs(NextActionDamageModPercent) > 0.0001 && 1.0 + NextActionDamageModPercent / 100.0 > 0
                ? ActionMultiplier / (1.0 + NextActionDamageModPercent / 100.0)
                : ActionMultiplier;
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
