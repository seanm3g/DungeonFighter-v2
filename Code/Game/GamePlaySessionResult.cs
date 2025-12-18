using System;
using System.Collections.Generic;

namespace RPGGame.Game
{
    /// <summary>
    /// Represents the result of a game play session
    /// Tracks statistics and outcomes from a complete game session
    /// </summary>
    public class GamePlaySessionResult
    {
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int TurnsPlayed { get; set; }
        public bool Success { get; set; }
        public int? FinalLevel { get; set; }
        public int? FinalGold { get; set; }
        public int? FinalHealth { get; set; }
        public int? MaxHealth { get; set; }
        public string? EndReason { get; set; }
        public List<string> ActionSequence { get; set; } = new();

        public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;

        public override string ToString()
        {
            return $"GamePlaySessionResult: " +
                   $"Success={Success}, " +
                   $"Turns={TurnsPlayed}, " +
                   $"Duration={Duration.TotalSeconds:F1}s, " +
                   $"Level={FinalLevel}, " +
                   $"Gold={FinalGold}, " +
                   $"Health={FinalHealth}/{MaxHealth}";
        }
    }
}
