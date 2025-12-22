using System;
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
    }

    /// <summary>
    /// Loot system configuration
    /// </summary>
    public class LootSystemConfig
    {
        public double BaseDropChance { get; set; }
        public double DropChancePerLevel { get; set; }
        public double MaxDropChance { get; set; }
        public double GuaranteedLootChance { get; set; }
        public double MagicFindEffectiveness { get; set; }
        public double GoldDropMultiplier { get; set; }
        public double ItemValueMultiplier { get; set; }
        public RarityUpgradeConfig RarityUpgrade { get; set; } = new();
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Rarity upgrade configuration - cascading rarity progression
    /// </summary>
    public class RarityUpgradeConfig
    {
        public bool Enabled { get; set; } = false;
        public double BaseUpgradeChance { get; set; } = 0.05;
        public double UpgradeChanceDecayPerTier { get; set; } = 0.5;
        public int MaxUpgradeTiers { get; set; } = 6;
        public double MagicFindBonus { get; set; } = 0.0001;
        public string Description { get; set; } = "Cascading rarity upgrade system - items can upgrade to next tier with exponentially decreasing probability";
    }

    /// <summary>
    /// Modification rarity configuration
    /// </summary>
    public class ModificationRarityConfig
    {
        public double Common { get; set; } = 35.0;
        public double Uncommon { get; set; } = 25.0;
        public double Rare { get; set; } = 20.0;
        public double Epic { get; set; } = 12.0;
        public double Legendary { get; set; } = 6.0;
        public double Mythic { get; set; } = 1.8;
        public double Transcendent { get; set; } = 0.2;
        public double TierBonusPerLevel { get; set; } = 1.5;
        public double BonusPointEffectiveness { get; set; } = 1.0;
        public string Description { get; set; } = "Modification rarity distribution percentages. Total should equal 100.0";
    }

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
    }

    /// <summary>
    /// Starting gear configuration (weapons and armor)
    /// </summary>
    public class StartingGearConfig
    {
        public List<StartingWeaponConfig> Weapons { get; set; } = new();
        public List<StartingArmorConfig> Armor { get; set; } = new();
        public string Description { get; set; } = "Initial equipment for new characters";
    }

    /// <summary>
    /// Starting weapon configuration
    /// </summary>
    public class StartingWeaponConfig
    {
        public string Name { get; set; } = "";
        public double Damage { get; set; }
        public double AttackSpeed { get; set; }
        public double Weight { get; set; } = 0.0;
    }

    /// <summary>
    /// Starting armor configuration
    /// </summary>
    public class StartingArmorConfig
    {
        public string Slot { get; set; } = "";
        public string Name { get; set; } = "";
        public int Armor { get; set; }
        public double Weight { get; set; } = 0.0;
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

    /// <summary>
    /// Magic find scaling configuration
    /// </summary>
    public class MagicFindScalingConfig
    {
        public RarityMagicFindConfig Common { get; set; } = new();
        public RarityMagicFindConfig Uncommon { get; set; } = new();
        public RarityMagicFindConfig Rare { get; set; } = new();
        public RarityMagicFindConfig Epic { get; set; } = new();
        public RarityMagicFindConfig Legendary { get; set; } = new();
    }

    /// <summary>
    /// Rarity magic find configuration
    /// </summary>
    public class RarityMagicFindConfig
    {
        public double PerPointMultiplier { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Level-based rarity scaling configuration
    /// </summary>
    public class LevelBasedRarityScalingConfig
    {
        public CommonRarityScalingConfig Common { get; set; } = new();
        public UncommonRarityScalingConfig Uncommon { get; set; } = new();
        public RareRarityScalingConfig Rare { get; set; } = new();
        public EpicRarityScalingConfig Epic { get; set; } = new();
        public LegendaryRarityScalingConfig Legendary { get; set; } = new();
    }

    /// <summary>
    /// Common rarity scaling configuration
    /// </summary>
    public class CommonRarityScalingConfig
    {
        public double BaseMultiplier { get; set; }
        public double LevelReduction { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Uncommon rarity scaling configuration
    /// </summary>
    public class UncommonRarityScalingConfig
    {
        public double BaseMultiplier { get; set; }
        public double LevelBonus { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Rare rarity scaling configuration
    /// </summary>
    public class RareRarityScalingConfig
    {
        public double BaseMultiplier { get; set; }
        public double LevelBonus { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Epic rarity scaling configuration
    /// </summary>
    public class EpicRarityScalingConfig
    {
        public int MinLevel { get; set; }
        public double EarlyMultiplier { get; set; }
        public double BaseMultiplier { get; set; }
        public double LevelBonus { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Legendary rarity scaling configuration
    /// </summary>
    public class LegendaryRarityScalingConfig
    {
        public int MinLevel { get; set; }
        public int EarlyThreshold { get; set; }
        public double EarlyMultiplier { get; set; }
        public double MidMultiplier { get; set; }
        public double BaseMultiplier { get; set; }
        public double LevelBonus { get; set; }
        public string Description { get; set; } = "";
    }
}
