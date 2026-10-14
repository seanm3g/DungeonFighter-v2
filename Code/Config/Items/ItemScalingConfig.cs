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
            StartingWeaponDamage ??= new StartingWeaponDamageConfig();
            StartingWeaponDamage.EnsureValidDefaults();
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

        /// <summary>
        /// Armor pieces: catalog armor × this before affixes (0 or less = 1.0, legacy).
        /// </summary>
        public double ArmorMultiplier { get; set; }

        /// <summary>Flat armor added to every armor piece before affixes, times its slot weight (0 = legacy).</summary>
        public double ArmorFlatPerPiece { get; set; }

        /// <summary>Armor per item level on every armor piece before affixes, times its slot weight (0 = legacy).</summary>
        public double ArmorPerItemLevel { get; set; }

        /// <summary>Armor per item tier above 1 on every armor piece before affixes, times its slot weight (0 = legacy).</summary>
        public double ArmorPerItemTier { get; set; }

        /// <summary>Slot weights for the flat and per-level armor (head / chest / legs / feet; missing = 1.0).</summary>
        public Dictionary<string, double> ArmorSlotWeights { get; set; } = new();

        /// <summary>
        /// Base armor for a piece: catalog × <see cref="ArmorMultiplier"/> + slot weight × (flat + per level × item level + per tier × (tier − 1)).
        /// Read at armor-total time, so saved items follow the active patch.
        /// </summary>
        public int ScalePieceArmor(int catalogArmor, ItemType slot, int itemLevel, int itemTier = 1)
        {
            double mult = ArmorMultiplier > 0 ? ArmorMultiplier : 1.0;
            double extra = ArmorFlatPerPiece + ArmorPerItemLevel * System.Math.Max(1, itemLevel)
                + ArmorPerItemTier * System.Math.Max(0, itemTier - 1);
            if (extra == 0 && mult == 1.0)
                return catalogArmor;
            string key = slot switch
            {
                ItemType.Head => "head",
                ItemType.Chest => "chest",
                ItemType.Legs => "legs",
                ItemType.Feet => "feet",
                _ => ""
            };
            double weight = 1.0;
            if (ArmorSlotWeights != null)
                foreach (var kv in ArmorSlotWeights)
                    if (string.Equals(kv.Key, key, System.StringComparison.OrdinalIgnoreCase))
                        weight = kv.Value;
            return (int)System.Math.Round(catalogArmor * mult + weight * extra);
        }

        public void EnsureSensibleDefaults()
        {
            ArmorSlotWeights ??= new Dictionary<string, double>();
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
