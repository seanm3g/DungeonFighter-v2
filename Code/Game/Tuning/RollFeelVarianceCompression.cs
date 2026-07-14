using System;
using RPGGame.Config;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Master slider (0 = chaotic, 1 = regular) that lerps impact-variance knobs used alongside dice/thresholds.
    /// Moving the slider writes linked sub-parameters; those sub-parameters remain editable afterward.
    /// </summary>
    public static class RollFeelVarianceCompression
    {
        public const string MasterParameterId = "rollFeelVarianceCompression";
        public const string SubGroupName = "Variance Compression";

        /// <summary>Registry ids updated when the master slider moves (UI order).</summary>
        public static readonly string[] DrivenParameterIds =
        {
            "criticalHitDamageMult",
            "comboRollDamageMult",
            "basicRollDamageMult",
            "playerArmorBaseline",
            "globalEnemyArmorMult",
            "maximumDamageCap",
            "minimumDamage",
            "armorReductionFactor"
        };

        // Chaotic (0) endpoints
        private const double ChaoticCritMult = 2.5;
        private const double RegularCritMult = 1.2;
        // Combo-band raw multiplier stays 1.0 at both ends; combo power is AMP/actions/crit.
        private const double ChaoticComboMult = 1.0;
        private const double RegularComboMult = 1.0;
        private const double BasicRollMult = 1.0;
        private const int ChaoticPlayerArmor = 0;
        private const int RegularPlayerArmor = 12;
        private const double ChaoticEnemyArmorMult = 0.5;
        private const double RegularEnemyArmorMult = 1.5;
        private const int ChaoticDamageCap = 1500;
        private const int RegularDamageCap = 400;
        private const int ChaoticMinDamage = 1;
        private const int RegularMinDamage = 4;
        private const double ChaoticArmorReductionFactor = 500.0;
        private const double RegularArmorReductionFactor = 50.0;

        public static double Lerp(double chaotic, double regular, double compression) =>
            chaotic + Math.Clamp(compression, 0, 1) * (regular - chaotic);

        /// <summary>
        /// Applies variance compression to all linked combat config fields and stores the slider value.
        /// </summary>
        public static void Apply(double compression, GameConfiguration? config = null)
        {
            config ??= GameConfiguration.Instance;
            compression = Math.Clamp(compression, 0, 1);

            config.CombatBalance.RollFeelVarianceCompression = compression;

            config.CombatBalance.CriticalHitDamageMultiplier =
                Lerp(ChaoticCritMult, RegularCritMult, compression);

            config.CombatBalance.RollDamageMultipliers ??= new RollDamageMultipliersConfig();
            config.CombatBalance.RollDamageMultipliers.ComboRollDamageMultiplier =
                Lerp(ChaoticComboMult, RegularComboMult, compression);
            config.CombatBalance.RollDamageMultipliers.BasicRollDamageMultiplier = BasicRollMult;

            config.Combat.PlayerBaseArmor = (int)Math.Round(Lerp(ChaoticPlayerArmor, RegularPlayerArmor, compression));

            config.EnemySystem ??= new EnemySystemConfig();
            config.EnemySystem.GlobalMultipliers ??= new GlobalMultipliersConfig();
            config.EnemySystem.GlobalMultipliers.ArmorMultiplier =
                Lerp(ChaoticEnemyArmorMult, RegularEnemyArmorMult, compression);

            config.Combat.MaximumDamageCap = (int)Math.Round(Lerp(ChaoticDamageCap, RegularDamageCap, compression));
            config.Combat.MinimumDamage = (int)Math.Round(Lerp(ChaoticMinDamage, RegularMinDamage, compression));
            config.Combat.ArmorReductionFactor =
                Lerp(ChaoticArmorReductionFactor, RegularArmorReductionFactor, compression);
        }
    }
}
