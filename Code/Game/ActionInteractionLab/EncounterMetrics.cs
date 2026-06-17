using System.Collections.Generic;
using RPGGame.Combat;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>Metrics from one simulated encounter.</summary>
    public sealed class EncounterMetrics
    {
        public bool PlayerWon { get; set; }
        public int Turns { get; set; }
        public int PlayerDamageDealt { get; set; }
        public int PlayerComboCount { get; set; }
        /// <summary>Longest single chain of consecutive successful player actions flagged as combo (timeline order).</summary>
        public int PlayerMaxComboStreak { get; set; }
        /// <summary>Completed combo chains of exact length ≥ 2 within this encounter (each uninterrupted run counts once).</summary>
        public Dictionary<int, int> PlayerComboStreakRunCounts { get; } = new();
        public int PlayerCritCount { get; set; }
        public int PlayerDamageEvents { get; set; }
        public int EnemyDamageDealt { get; set; }
        public int EnemyComboCount { get; set; }
        public int PlayerFinalHealth { get; set; }
        public int EnemyFinalHealth { get; set; }
        public double CombatGameTime { get; set; }
        public double PlayerDpsVersusTime { get; set; }
        public CombatSingleTurnResult? TerminalReason { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
