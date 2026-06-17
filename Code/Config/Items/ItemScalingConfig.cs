using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Item scaling configuration
    /// </summary>
    public class ItemScalingConfig
    {
        public Dictionary<string, double> StartingWeaponDamage { get; set; } = new();
        public Dictionary<string, double> StartingWeaponSpeed { get; set; } = new();
        public Dictionary<string, TierRange> TierDamageRanges { get; set; } = new();
        public double GlobalDamageMultiplier { get; set; }
        public int WeaponDamagePerTier { get; set; }
        public int ArmorValuePerTier { get; set; }
        public double SpeedBonusPerTier { get; set; }
        public int MaxTier { get; set; }
        public double EnchantmentChance { get; set; }

        public void EnsureSanitizedWeaponScalingDefaults()
        {
            if (GlobalDamageMultiplier <= 0)
                GlobalDamageMultiplier = 1.0;
            if (WeaponDamagePerTier <= 0)
                WeaponDamagePerTier = 3;
            if (ArmorValuePerTier <= 0)
                ArmorValuePerTier = 2;
            if (MaxTier <= 0)
                MaxTier = 10;
            if (SpeedBonusPerTier <= 0)
                SpeedBonusPerTier = 0.05;
            if (EnchantmentChance < 0)
                EnchantmentChance = 0;
        }
    }

    /// <summary>
    /// Weapon scaling configuration
    /// </summary>
    public class WeaponScalingConfig
    {
        public StartingWeaponDamageConfig StartingWeaponDamage { get; set; } = new();
        public TierDamageRangesConfig TierDamageRanges { get; set; } = new();
        public double GlobalDamageMultiplier { get; set; }
        public string Description { get; set; } = "";

        public void EnsureSanitizedDefaults()
        {
            if (GlobalDamageMultiplier <= 0)
                GlobalDamageMultiplier = 1.0;
        }
    }

    /// <summary>
    /// Equipment scaling configuration
    /// </summary>
    public class EquipmentScalingConfig
    {
        public int WeaponDamagePerTier { get; set; }
        public int ArmorValuePerTier { get; set; }
        public double SpeedBonusPerTier { get; set; }
        public int MaxTier { get; set; }
        public double EnchantmentChance { get; set; }
        public string Description { get; set; } = "";

        public void EnsureSensibleDefaults()
        {
            if (WeaponDamagePerTier <= 0)
                WeaponDamagePerTier = 3;
            if (ArmorValuePerTier <= 0)
                ArmorValuePerTier = 2;
            if (SpeedBonusPerTier <= 0)
                SpeedBonusPerTier = 0.05;
            if (MaxTier <= 0)
                MaxTier = 10;
            if (EnchantmentChance < 0)
                EnchantmentChance = 0;
        }
    }

    /// <summary>
    /// Rarity scaling configuration
    /// </summary>
    public class RarityScalingConfig
    {
        public RarityMultipliers StatBonusMultipliers { get; set; } = new();
        public RollChanceFormulas RollChanceFormulas { get; set; } = new();
        public MagicFindScalingConfig MagicFindScaling { get; set; } = new();
        public LevelBasedRarityScalingConfig LevelBasedRarityScaling { get; set; } = new();

        /// <summary>
        /// Global strength for magic find on the <b>initial</b> rarity weight roll:
        /// adjustedWeight = baseWeight * Exp(alpha * t * k_r) with t = clamp(MF,0,100)/100 and k_r from <see cref="MagicFindScaling"/> (or built-in defaults when all per-point values are zero).
        /// Values &lt;= 0 disable MF tilt (weights unchanged).
        /// </summary>
        public double MagicFindDistributionAlpha { get; set; } = 0.5;
    }
}
