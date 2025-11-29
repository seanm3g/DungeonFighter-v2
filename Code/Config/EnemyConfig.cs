using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{

    /// <summary>
    /// Pool configuration for enemy attributes
    /// </summary>
    public class PoolConfig
    {
        public int BasePointsAtLevel1 { get; set; }
        public int PointsPerLevel { get; set; }
    }

    /// <summary>
    /// Archetype configuration
    /// </summary>
    public class ArchetypeConfig
    {
        public double AttributePoolRatio { get; set; }
        public double SUSTAINPoolRatio { get; set; }
        public double StrengthRatio { get; set; }
        public double AgilityRatio { get; set; }
        public double TechniqueRatio { get; set; }
        public double IntelligenceRatio { get; set; }
        public double SUSTAINHealthRatio { get; set; }
        public double SUSTAINArmorRatio { get; set; }
        public Level1Modifiers? Level1Modifiers { get; set; }
        public ArchetypeBonuses? ArchetypeBonuses { get; set; }
    }

    /// <summary>
    /// Archetype bonuses configuration
    /// </summary>
    public class ArchetypeBonuses
    {
        public double StrengthMultiplier { get; set; } = 1.0;
        public double AgilityMultiplier { get; set; } = 1.0;
        public double HealthMultiplier { get; set; } = 1.0;
        public double ArmorMultiplier { get; set; } = 1.0;
        public double AttackSpeedMultiplier { get; set; } = 1.0;
    }

    /// <summary>
    /// Base enemy configuration
    /// </summary>
    public class BaseEnemyConfig
    {
        public int BaseLevel { get; set; }
        public double HealthRatio { get; set; } = 1.0;
        public BaseEnemyStats BaseStats { get; set; } = new();
        public int BaseArmor { get; set; }
        public string PrimaryAttribute { get; set; } = "Strength";
        public bool IsLiving { get; set; } = true;
        public List<string> Actions { get; set; } = new();
    }

    /// <summary>
    /// Base enemy stats configuration
    /// </summary>
    public class BaseEnemyStats
    {
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Technique { get; set; }
        public int Intelligence { get; set; }
    }

    /// <summary>
    /// Level 1 modifiers configuration
    /// </summary>
    public class Level1Modifiers
    {
        public int HealthBonus { get; set; }
        public int StrengthBonus { get; set; }
        public int AgilityBonus { get; set; }
        public int TechniqueBonus { get; set; }
        public int IntelligenceBonus { get; set; }
        public int ArmorBonus { get; set; }
    }

    /// <summary>
    /// Stat conversion rates configuration
    /// </summary>
    public class StatConversionRatesConfig
    {
        public double StrengthPerPoint { get; set; }
        public double AgilityPerPoint { get; set; }
        public double TechniquePerPoint { get; set; }
        public double IntelligencePerPoint { get; set; }
        public double HealthPerPoint { get; set; }
        public double ArmorPerPoint { get; set; }
    }

    /// <summary>
    /// DPS balance validation configuration
    /// </summary>
    public class DPSBalanceValidationConfig
    {
        public double TolerancePercentage { get; set; }
        public double MinimumDPS { get; set; }
        public double MaximumDPSMultiplier { get; set; }
    }

    /// <summary>
    /// Sustain balance configuration
    /// </summary>
    public class SustainBalanceConfig
    {
        public string Description { get; set; } = "";
        public TargetActionsToKillConfig TargetActionsToKill { get; set; } = new();
        public DPSToSustainRatioConfig DPSToSustainRatio { get; set; } = new();
        public SpeedToDamageRatioConfig SpeedToDamageRatio { get; set; } = new();
        public HealthToArmorRatioConfig HealthToArmorRatio { get; set; } = new();
        public AttributeGainRatioConfig AttributeGainRatio { get; set; } = new();
        public SustainScalingConfig SustainScaling { get; set; } = new();
        public SustainBalanceValidationConfig BalanceValidation { get; set; } = new();
    }

    /// <summary>
    /// Target actions to kill configuration
    /// </summary>
    public class TargetActionsToKillConfig
    {
        public int Level1 { get; set; }
        public int Level10 { get; set; }
        public int Level20 { get; set; }
        public int Level30 { get; set; }
        public string Formula { get; set; } = "";
    }

    /// <summary>
    /// DPS to sustain ratio configuration
    /// </summary>
    public class DPSToSustainRatioConfig
    {
        public double BaseRatio { get; set; }
        public string Description { get; set; } = "";
        public Dictionary<string, double> ArchetypeModifiers { get; set; } = new();
    }

    /// <summary>
    /// Speed to damage ratio configuration
    /// </summary>
    public class SpeedToDamageRatioConfig
    {
        public double BaseRatio { get; set; }
        public string Description { get; set; } = "";
        public Dictionary<string, double> ArchetypeModifiers { get; set; } = new();
    }

    /// <summary>
    /// Health to armor ratio configuration
    /// </summary>
    public class HealthToArmorRatioConfig
    {
        public double BaseRatio { get; set; }
        public string Description { get; set; } = "";
        public Dictionary<string, double> ArchetypeModifiers { get; set; } = new();
    }

    /// <summary>
    /// Attribute gain ratio configuration
    /// </summary>
    public class AttributeGainRatioConfig
    {
        public double BaseRatio { get; set; }
        public string Description { get; set; } = "";
        public double PrimaryAttributeBonus { get; set; }
        public double SecondaryAttributeBonus { get; set; }
        public Dictionary<string, double> ArchetypeModifiers { get; set; } = new();
    }

    /// <summary>
    /// Sustain scaling configuration
    /// </summary>
    public class SustainScalingConfig
    {
        public double HealthPerLevel { get; set; }
        public double ArmorPerLevel { get; set; }
        public double RegenerationPerLevel { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Sustain balance validation configuration
    /// </summary>
    public class SustainBalanceValidationConfig
    {
        public int MinActionsToKill { get; set; }
        public int MaxActionsToKill { get; set; }
        public double DPSRatioTolerance { get; set; }
        public double SpeedRatioTolerance { get; set; }
        public double HealthArmorRatioTolerance { get; set; }
        public double AttributeRatioTolerance { get; set; }
    }

    /// <summary>
    /// Base stats configuration
    /// </summary>
    public class BaseStatsConfig
    {
        public int Health { get; set; }
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Technique { get; set; }
        public int Intelligence { get; set; }
        public int Armor { get; set; }
    }

    /// <summary>
    /// Scaling per level configuration
    /// </summary>
    public class ScalingPerLevelConfig
    {
        public int Health { get; set; }
        public int Attributes { get; set; }
        public double Armor { get; set; }
    }

    /// <summary>
    /// Enemy archetype configuration
    /// </summary>
    public class EnemyArchetypeConfig
    {
        public StatMultipliersConfig StatMultipliers { get; set; } = new();
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Stat multipliers configuration
    /// </summary>
    public class StatMultipliersConfig
    {
        public double Health { get; set; } = 1.0;
        public double Strength { get; set; } = 1.0;
        public double Agility { get; set; } = 1.0;
        public double Technique { get; set; } = 1.0;
        public double Intelligence { get; set; } = 1.0;
        public double Armor { get; set; } = 1.0;
    }


    /// <summary>
    /// Unified enemy system configuration that consolidates all enemy-related tuning
    /// </summary>
    public class EnemySystemConfig
    {
        public GlobalMultipliersConfig GlobalMultipliers { get; set; } = new();
        public BaselineStatsConfig BaselineStats { get; set; } = new();
        public ScalingPerLevelConfig ScalingPerLevel { get; set; } = new();
        public Dictionary<string, ArchetypeMultipliersConfig> Archetypes { get; set; } = new();
        public int LevelVariance { get; set; } = 1;
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Global multipliers applied to all enemies
    /// </summary>
    public class GlobalMultipliersConfig
    {
        public double HealthMultiplier { get; set; } = 1.0;
        public double DamageMultiplier { get; set; } = 1.0;
        public double ArmorMultiplier { get; set; } = 1.0;
        public double SpeedMultiplier { get; set; } = 1.0;
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Baseline stats that all enemies start with
    /// </summary>
    public class BaselineStatsConfig
    {
        public int Health { get; set; } = 50;
        public int Strength { get; set; } = 3;
        public int Agility { get; set; } = 3;
        public int Technique { get; set; } = 3;
        public int Intelligence { get; set; } = 3;
        public int Armor { get; set; } = 2;
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Alias for BaselineStatsConfig (legacy compatibility)
    /// </summary>
    public class EnemyBaselineConfig : BaselineStatsConfig
    {
    }

    /// <summary>
    /// Stat multipliers for each archetype
    /// </summary>
    public class ArchetypeMultipliersConfig
    {
        public double Health { get; set; } = 1.0;
        public double Strength { get; set; } = 1.0;
        public double Agility { get; set; } = 1.0;
        public double Technique { get; set; } = 1.0;
        public double Intelligence { get; set; } = 1.0;
        public double Armor { get; set; } = 1.0;
        public string Description { get; set; } = "";
    }


}
