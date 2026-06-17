using System;
using System.Collections.Generic;
using RPGGame.Combat;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>Aggregated results from a batch of lab encounter simulations.</summary>
    public sealed class ActionLabEncounterSimulationReport
    {
        public List<EncounterMetrics> Encounters { get; } = new();
        public int EncounterCount => Encounters.Count;
        public int PlayerWins { get; set; }
        public double WinRate { get; set; }
        public double AverageTurns { get; set; }
        public int MinTurns { get; set; }
        public int MaxTurns { get; set; }
        public double MedianTurns { get; set; }
        public double StdDevTurns { get; set; }
        public double MedianPlayerDamage { get; set; }
        public double StdDevPlayerDamage { get; set; }
        public double MedianPlayerDps { get; set; }
        public double StdDevPlayerDps { get; set; }
        public double MedianCombatGameTime { get; set; }
        public double StdDevCombatGameTime { get; set; }
        public double AverageTurnsOnWin { get; set; }
        public double AverageTurnsOnLoss { get; set; }
        public double AveragePlayerDamageOnWin { get; set; }
        public double AveragePlayerDamageOnLoss { get; set; }
        public double AveragePlayerDpsOnWin { get; set; }
        public double AveragePlayerDpsOnLoss { get; set; }
        public double AveragePlayerHpRemainingOnWin { get; set; }
        public double AveragePlayerHpRemainingOnLoss { get; set; }
        public double AverageEnemyDamage { get; set; }
        public double AverageEnemyComboCount { get; set; }
        public double AveragePlayerDamage { get; set; }
        public double AveragePlayerDps { get; set; }
        public double AverageCritsPerEncounter { get; set; }
        public double CritRatePerDamageEvent { get; set; }
        public Dictionary<int, int> PlayerComboCountHistogram { get; } = new();
        /// <summary>Pooled counts of combo chains (length ≥ 2) across successful encounters.</summary>
        public Dictionary<int, int> PlayerComboStreakRunTotals { get; } = new();
        /// <summary>Bucket = min(<see cref="ActionLabEncounterReportFormatter.MaxComboStreakHistogramBucket"/>, longest chain in encounter); last bucket is <c>N+</c>.</summary>
        public Dictionary<int, int> PlayerMaxComboStreakHistogram { get; } = new();
        /// <summary>Mean of <see cref="EncounterMetrics.PlayerMaxComboStreak"/> over successful (non-errored) encounters.</summary>
        public double AveragePlayerMaxComboStreak { get; set; }
        public Dictionary<string, int> TurnCountHistogram { get; } = new();
        public Dictionary<CombatSingleTurnResult, int> TerminalReasonCounts { get; } = new();
        public int ErroredEncounters { get; set; }

        /// <summary>Wall-clock time spent in <see cref="ActionLabEncounterSimulator.RunBatchAsync"/> (reload, encounters, aggregate).</summary>
        public TimeSpan SimulationWallElapsed { get; set; }
    }
}
