using System.Collections.Generic;
using RPGGame.Combat;
using RPGGame.Entity;

namespace RPGGame.BattleStatistics
{
    /// <summary>
    /// Data models for battle statistics results
    /// </summary>
    public class BattleResult
    {
        public bool PlayerWon { get; set; }
        public int Turns { get; set; }
        public int PlayerDamageDealt { get; set; }
        public int EnemyDamageDealt { get; set; }
        public int PlayerFinalHealth { get; set; }
        public int EnemyFinalHealth { get; set; }
        public string? ErrorMessage { get; set; }
        public List<CombatTurnLog>? TurnLogs { get; set; }
        public Dictionary<string, int>? ActionUsageCount { get; set; }
        public FunMomentDataStructures.FunMomentSummary? FunMomentSummary { get; set; }
    }

    public class BattleConfiguration
    {
        public int PlayerDamage { get; set; }
        public double PlayerAttackSpeed { get; set; }
        public int PlayerArmor { get; set; }
        public int PlayerHealth { get; set; }
        
        public int EnemyDamage { get; set; }
        public double EnemyAttackSpeed { get; set; }
        public int EnemyArmor { get; set; }
        public int EnemyHealth { get; set; }
    }

    public class StatisticsResult
    {
        public BattleConfiguration Config { get; set; } = new();
        public int TotalBattles { get; set; }
        public int PlayerWins { get; set; }
        public int EnemyWins { get; set; }
        public double WinRate { get; set; }
        public double AverageTurns { get; set; }
        public double AveragePlayerDamageDealt { get; set; }
        public double AverageEnemyDamageDealt { get; set; }
        public int MinTurns { get; set; }
        public int MaxTurns { get; set; }
        public List<BattleResult> BattleResults { get; set; } = new();
    }

    public class WeaponTestResult
    {
        public WeaponType WeaponType { get; set; }
        public int TotalBattles { get; set; }
        public int PlayerWins { get; set; }
        public int EnemyWins { get; set; }
        public double WinRate { get; set; }
        public double AverageTurns { get; set; }
        public double AveragePlayerDamageDealt { get; set; }
        public double AverageEnemyDamageDealt { get; set; }
        public int MinTurns { get; set; }
        public int MaxTurns { get; set; }
        public List<BattleResult> BattleResults { get; set; } = new();
    }

    public class WeaponEnemyCombinationResult
    {
        public WeaponType WeaponType { get; set; }
        public string EnemyType { get; set; } = "";
        public int TotalBattles { get; set; }
        public int PlayerWins { get; set; }
        public int EnemyWins { get; set; }
        public double WinRate { get; set; }
        public double AverageTurns { get; set; }
        public double AveragePlayerDamageDealt { get; set; }
        public double AverageEnemyDamageDealt { get; set; }
        public int MinTurns { get; set; }
        public int MaxTurns { get; set; }
        public List<BattleResult> BattleResults { get; set; } = new();
    }

    public class WeaponOverallStats
    {
        public int TotalBattles { get; set; }
        public int Wins { get; set; }
        public double WinRate { get; set; }
        public double AverageTurns { get; set; }
        public double AverageDamage { get; set; }
        public double AverageFunScore { get; set; }
        public double AverageActionVariety { get; set; }
        public double AverageHealthLeadChanges { get; set; }
    }

    public class EnemyOverallStats
    {
        public int TotalBattles { get; set; }
        public int Wins { get; set; }
        public double WinRate { get; set; }
        public double AverageTurns { get; set; }
        public double AverageDamageReceived { get; set; }
        public double AverageFunScore { get; set; }
        public double AverageActionVariety { get; set; }
        public double AverageHealthLeadChanges { get; set; }
    }

    public class ComprehensiveWeaponEnemyTestResult
    {
        public List<WeaponType> WeaponTypes { get; set; } = new();
        public List<string> EnemyTypes { get; set; } = new();
        public List<WeaponEnemyCombinationResult> CombinationResults { get; set; } = new();
        
        public int TotalBattles { get; set; }
        public int TotalPlayerWins { get; set; }
        public int TotalEnemyWins { get; set; }
        public double OverallWinRate { get; set; }
        public double OverallAverageTurns { get; set; }
        public double OverallAveragePlayerDamage { get; set; }
        public double OverallAverageEnemyDamage { get; set; }
        
        public double OverallAverageFunScore { get; set; }
        public double OverallAverageActionVariety { get; set; }
        public double OverallAverageTurnVariance { get; set; }
        public int TotalHealthLeadChanges { get; set; }
        public int TotalComebacks { get; set; }
        public int TotalCloseCalls { get; set; }
        
        public Dictionary<WeaponType, WeaponOverallStats> WeaponStatistics { get; set; } = new();
        public Dictionary<string, EnemyOverallStats> EnemyStatistics { get; set; } = new();
    }
}

