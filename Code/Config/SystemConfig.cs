using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Game speed configuration
    /// </summary>
    public class GameSpeedConfig
    {
        public double GameTickerInterval { get; set; }
        public double GameSpeedMultiplier { get; set; }
    }

    /// <summary>
    /// Game data configuration
    /// </summary>
    public class GameDataConfig
    {
        public bool AutoGenerateOnLaunch { get; set; }
        public bool ShowGenerationMessages { get; set; }
        public bool CreateBackupsOnAutoGenerate { get; set; } = true;
        public bool ForceOverwriteOnAutoGenerate { get; set; } = false;
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Debug configuration
    /// </summary>
    public class DebugConfig
    {
        public bool EnableDebugOutput { get; set; }
        public bool ShowCombatSimulationDebug { get; set; }
        public int MaxDetailedBattles { get; set; } = 10;
        public bool LogCombatActions { get; set; } = false;
        public bool LogOnlyOnErrors { get; set; } = true;
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Balance analysis configuration
    /// </summary>
    public class BalanceAnalysisConfig
    {
        public int SimulationsPerMatchup { get; set; } = 100;
        public int TargetWinRateMin { get; set; } = 85;
        public int TargetWinRateMax { get; set; } = 98;
        public bool EnableDetailedLogging { get; set; } = false;
        public int MaxDetailedBattles { get; set; } = 5;
        public bool HideCombatLogs { get; set; } = true;
        public Dictionary<string, PlayerStatsConfig> PlayerStats { get; set; } = new Dictionary<string, PlayerStatsConfig>();
        public DamageCalculationConfig DamageCalculation { get; set; } = new DamageCalculationConfig();
        public DifficultyThresholdsConfig DifficultyThresholds { get; set; } = new DifficultyThresholdsConfig();
        public DPSCalculationConfig DPSCalculation { get; set; } = new DPSCalculationConfig();
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Balance tuning goals configuration
    /// Defines targets and thresholds for automated tuning
    /// </summary>
    public class BalanceTuningGoalsConfig
    {
        /// <summary>
        /// Win rate targets
        /// </summary>
        public WinRateGoalsConfig WinRate { get; set; } = new WinRateGoalsConfig();

        /// <summary>
        /// Combat duration targets
        /// </summary>
        public CombatDurationGoalsConfig CombatDuration { get; set; } = new CombatDurationGoalsConfig();

        /// <summary>
        /// Weapon balance targets
        /// </summary>
        public WeaponBalanceGoalsConfig WeaponBalance { get; set; } = new WeaponBalanceGoalsConfig();

        /// <summary>
        /// Enemy differentiation targets
        /// </summary>
        public EnemyDifferentiationGoalsConfig EnemyDifferentiation { get; set; } = new EnemyDifferentiationGoalsConfig();

        /// <summary>
        /// Quality score weights (must sum to 1.0)
        /// </summary>
        public QualityWeightsConfig QualityWeights { get; set; } = new QualityWeightsConfig();
    }

    /// <summary>
    /// Win rate goals configuration
    /// </summary>
    public class WinRateGoalsConfig
    {
        public double MinTarget { get; set; } = 85.0;
        public double MaxTarget { get; set; } = 98.0;
        public double OptimalMin { get; set; } = 88.0;
        public double OptimalMax { get; set; } = 95.0;
        public double CriticalLow { get; set; } = 80.0;
        public double WarningLow { get; set; } = 85.0;
        public double WarningHigh { get; set; } = 98.0;
        public double CriticalHigh { get; set; } = 99.0;
    }

    /// <summary>
    /// Combat duration goals configuration
    /// </summary>
    public class CombatDurationGoalsConfig
    {
        public double MinTarget { get; set; } = 8.0;
        public double MaxTarget { get; set; } = 15.0;
        public double OptimalMin { get; set; } = 9.0;
        public double OptimalMax { get; set; } = 13.0;
        public double CriticalShort { get; set; } = 6.0;
        public double WarningShort { get; set; } = 8.0;
        public double WarningLong { get; set; } = 15.0;
        public double CriticalLong { get; set; } = 18.0;
    }

    /// <summary>
    /// Weapon balance goals configuration
    /// </summary>
    public class WeaponBalanceGoalsConfig
    {
        public double MaxVariance { get; set; } = 10.0;
        public double OptimalVariance { get; set; } = 5.0;
        public double CriticalVariance { get; set; } = 15.0;
    }

    /// <summary>
    /// Enemy differentiation goals configuration
    /// </summary>
    public class EnemyDifferentiationGoalsConfig
    {
        public double MinVariance { get; set; } = 3.0;
        public double OptimalVariance { get; set; } = 5.0;
        public double CriticalVariance { get; set; } = 1.0;
    }

    /// <summary>
    /// Quality score weights configuration
    /// </summary>
    public class QualityWeightsConfig
    {
        public double WinRateWeight { get; set; } = 0.40;
        public double DurationWeight { get; set; } = 0.25;
        public double WeaponBalanceWeight { get; set; } = 0.20;
        public double EnemyDiffWeight { get; set; } = 0.15;
    }

    /// <summary>
    /// Balance validation configuration
    /// </summary>
    public class BalanceValidationConfig
    {
        public bool EnableBalanceChecks { get; set; }
        public double MaxDamageVariance { get; set; }
        public double MinCombatDuration { get; set; }
        public double MaxCombatDuration { get; set; }
        public List<double> TargetDPSRange { get; set; } = new();
        public bool ValidateEnemyScaling { get; set; }
        public bool ValidateLootRates { get; set; }
        public bool ValidateXPProgression { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Difficulty settings configuration
    /// </summary>
    public class DifficultySettingsConfig
    {
        public DifficultyLevel Easy { get; set; } = new();
        public DifficultyLevel Normal { get; set; } = new();
        public DifficultyLevel Hard { get; set; } = new();
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Difficulty level configuration
    /// </summary>
    public class DifficultyLevel
    {
        public double EnemyHealthMultiplier { get; set; }
        public double EnemyDamageMultiplier { get; set; }
        public double XPMultiplier { get; set; }
        public double LootMultiplier { get; set; }
    }

    /// <summary>
    /// Player stats configuration
    /// </summary>
    public class PlayerStatsConfig
    {
        public int Strength { get; set; } = 1;
        public int Agility { get; set; } = 1;
        public int Technique { get; set; } = 1;
        public int Intelligence { get; set; } = 1;
    }

    /// <summary>
    /// Damage calculation configuration
    /// </summary>
    public class DamageCalculationConfig
    {
        public double AverageDamageMultiplier { get; set; } = 0.8;
        public int MinimumDamage { get; set; } = 1;
    }

    /// <summary>
    /// Difficulty thresholds configuration
    /// </summary>
    public class DifficultyThresholdsConfig
    {
        public double TooEasy { get; set; } = 0.98;
        public double Moderate { get; set; } = 0.6;
        public double Hard { get; set; } = 0.4;
    }

    /// <summary>
    /// DPS calculation configuration
    /// </summary>
    public class DPSCalculationConfig
    {
        public double BaseDamageMultiplier { get; set; } = 0.5;
        public double AttackSpeedMultiplier { get; set; } = 0.1;
    }

    /// <summary>
    /// Progression curves configuration
    /// </summary>
    public class ProgressionCurvesConfig
    {
        public string ExperienceFormula { get; set; } = "";
        public string AttributeGrowth { get; set; } = "";
        public double ExponentFactor { get; set; }
        public double LinearGrowth { get; set; }
        public double QuadraticGrowth { get; set; }
    }
}
