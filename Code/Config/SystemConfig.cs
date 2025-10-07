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
