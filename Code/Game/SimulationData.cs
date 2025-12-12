using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Enhanced data structures for simulation analysis
    /// </summary>
    
    /// <summary>
    /// Turn-by-turn combat log for detailed analysis
    /// </summary>
    public class CombatTurnLog
    {
        public int TurnNumber { get; set; }
        public string Actor { get; set; } = ""; // "player" or "enemy"
        public string Action { get; set; } = "";
        public int DamageDealt { get; set; }
        public int DamageReceived { get; set; }
        public int PlayerHealthAfter { get; set; }
        public int EnemyHealthAfter { get; set; }
        public bool WasCritical { get; set; }
        public bool WasMiss { get; set; }
        public List<string> StatusEffectsApplied { get; set; } = new();
        public int? RollValue { get; set; }
        public double? TimeToNextAction { get; set; }
        public string? ActionType { get; set; } // "basic", "combo", "special"
    }

    /// <summary>
    /// Enhanced battle result with turn-by-turn logs
    /// </summary>
    public class EnhancedBattleResult : BattleStatisticsRunner.BattleResult
    {
        public new List<CombatTurnLog> TurnLogs { get; set; } = new();
        public new Dictionary<string, int> ActionUsageCount { get; set; } = new();
        public List<int> DamagePerTurn { get; set; } = new();
        public List<int> HealthPercentagePerTurn { get; set; } = new();
        public int CriticalHits { get; set; }
        public int Misses { get; set; }
        public double AverageDamagePerTurn { get; set; }
        public int MaxDamageInSingleTurn { get; set; }
        public int MinDamageInSingleTurn { get; set; }
    }

    /// <summary>
    /// Parameter sensitivity analysis result
    /// </summary>
    public class ParameterSensitivityResult
    {
        public string ParameterName { get; set; } = "";
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public int TestPoints { get; set; }
        public List<ParameterTestPoint> TestPointsData { get; set; } = new();
        public double OptimalValue { get; set; }
        public double OptimalQualityScore { get; set; }
        public double SensitivityScore { get; set; } // 0-1, higher = more sensitive
        public string Recommendation { get; set; } = "";
    }

    /// <summary>
    /// Single test point in parameter sensitivity analysis
    /// </summary>
    public class ParameterTestPoint
    {
        public double ParameterValue { get; set; }
        public double WinRate { get; set; }
        public double AverageCombatDuration { get; set; }
        public double QualityScore { get; set; }
        public int BattlesTested { get; set; }
    }

    /// <summary>
    /// What-if scenario test result
    /// </summary>
    public class WhatIfTestResult
    {
        public string ParameterName { get; set; } = "";
        public double CurrentValue { get; set; }
        public double TestValue { get; set; }
        public double WinRateChange { get; set; }
        public double DurationChange { get; set; }
        public double QualityScoreChange { get; set; }
        public double QualityScoreBefore { get; set; }
        public double QualityScoreAfter { get; set; }
        public string RiskAssessment { get; set; } = ""; // "low", "medium", "high"
        public string Recommendation { get; set; } = "";
        public Dictionary<string, object> DetailedMetrics { get; set; } = new();
    }
}

