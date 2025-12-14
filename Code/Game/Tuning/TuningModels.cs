using System.Collections.Generic;
using RPGGame.Config;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Tuning suggestion for a specific adjustment
    /// </summary>
    public class TuningSuggestion
    {
        public string Id { get; set; } = "";
        public BalanceTuningGoals.TuningPriority Priority { get; set; }
        public string Category { get; set; } = "";
        public string Target { get; set; } = "";
        public string Parameter { get; set; } = "";
        public double CurrentValue { get; set; }
        public double SuggestedValue { get; set; }
        public double AdjustmentMagnitude { get; set; }
        public string Reason { get; set; } = "";
        public string Impact { get; set; } = "";
        public List<string> AffectedMatchups { get; set; } = new();
    }

    /// <summary>
    /// Complete tuning analysis with suggestions
    /// </summary>
    public class TuningAnalysis
    {
        public double QualityScore { get; set; }
        public double OverallWinRate { get; set; }
        public double AverageCombatDuration { get; set; }
        public double WeaponVariance { get; set; }
        public double EnemyVariance { get; set; }
        public List<TuningSuggestion> Suggestions { get; set; } = new();
        public Dictionary<BalanceTuningGoals.TuningPriority, int> SuggestionCounts { get; set; } = new();
        public string Summary { get; set; } = "";
    }
}

