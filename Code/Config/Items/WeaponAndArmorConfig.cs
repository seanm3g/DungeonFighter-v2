namespace RPGGame
{
    /// <summary>
    /// Tier range configuration
    /// </summary>
    public class TierRange
    {
        public int Min { get; set; }
        public int Max { get; set; }
    }

    /// <summary>
    /// Weapon type configuration
    /// </summary>
    public class WeaponTypeConfig
    {
        public string DamageFormula { get; set; } = "";
        public string SpeedFormula { get; set; } = "";
        public ScalingFactorsConfig ScalingFactors { get; set; } = new();
    }

    /// <summary>
    /// Armor type configuration
    /// </summary>
    public class ArmorTypeConfig
    {
        public string ArmorFormula { get; set; } = "";
        public string ActionChanceFormula { get; set; } = "";
    }

    /// <summary>
    /// Scaling factors configuration
    /// </summary>
    public class ScalingFactorsConfig
    {
        public double StrengthWeight { get; set; }
        public double AgilityWeight { get; set; }
        public double TechniqueWeight { get; set; }
        public double IntelligenceWeight { get; set; }
    }

    /// <summary>
    /// Rarity modifier configuration
    /// </summary>
    public class RarityModifierConfig
    {
        public double DamageMultiplier { get; set; }
        public double ArmorMultiplier { get; set; }
        public double BonusChanceMultiplier { get; set; }
    }

    /// <summary>
    /// Level scaling caps configuration
    /// </summary>
    public class LevelScalingCapsConfig
    {
        public double MaxDamageScaling { get; set; }
        public double MaxArmorScaling { get; set; }
        public double MaxSpeedScaling { get; set; }
        public MinimumValuesConfig MinimumValues { get; set; } = new();
    }

    /// <summary>
    /// Minimum values configuration
    /// </summary>
    public class MinimumValuesConfig
    {
        public int Damage { get; set; }
        public int Armor { get; set; }
        public double Speed { get; set; }
    }

    /// <summary>
    /// Formula configuration
    /// </summary>
    public class FormulaConfig
    {
        public double BaseMultiplier { get; set; }
        public double TierScaling { get; set; }
        public double LevelScaling { get; set; }
        public string Formula { get; set; } = "";
    }

    /// <summary>
    /// Rarity multipliers configuration
    /// </summary>
    public class RarityMultipliers
    {
        public double Common { get; set; }
        public double Uncommon { get; set; }
        public double Rare { get; set; }
        public double Epic { get; set; }
        public double Legendary { get; set; }
    }

    /// <summary>
    /// Roll chance formulas configuration
    /// </summary>
    public class RollChanceFormulas
    {
        public string ActionBonusChance { get; set; } = "";
        public string StatBonusChance { get; set; } = "";
    }

    /// <summary>
    /// Starting weapon damage configuration
    /// </summary>
    public class StartingWeaponDamageConfig
    {
        public int Mace { get; set; }
        public int Sword { get; set; }
        public int Dagger { get; set; }
        public int Wand { get; set; }

        public void EnsureValidDefaults()
        {
            if (Mace <= 0) Mace = 6;
            if (Sword <= 0) Sword = 5;
            if (Dagger <= 0) Dagger = 3;
            if (Wand <= 0) Wand = 4;
        }
    }

    /// <summary>
    /// Tier damage ranges configuration
    /// </summary>
    public class TierDamageRangesConfig
    {
        public MinMaxConfig Tier1 { get; set; } = new();
        public MinMaxConfig Tier2 { get; set; } = new();
        public MinMaxConfig Tier3 { get; set; } = new();
        public MinMaxConfig Tier4 { get; set; } = new();
        public MinMaxConfig Tier5 { get; set; } = new();
    }
}
