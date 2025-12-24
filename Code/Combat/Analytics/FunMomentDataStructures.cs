using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Data structures for fun moment tracking
    /// Extracted from FunMomentTracker to separate data structures
    /// </summary>
    public class FunMomentDataStructures
    {
        /// <summary>
        /// Represents a single "fun" moment that occurred during combat
        /// </summary>
        public class FunMoment
        {
            public FunMomentType Type { get; set; }
            public int Turn { get; set; }
            public string Actor { get; set; } = string.Empty;
            public string Target { get; set; } = string.Empty;
            public double Intensity { get; set; } // 0.0-1.0 scale of how "fun" this moment was
            public string Description { get; set; } = string.Empty;
            public Dictionary<string, object> Metadata { get; set; } = new();
        }

        /// <summary>
        /// Types of fun moments to track
        /// </summary>
        public enum FunMomentType
        {
            BigDamageSpike,          // Damage significantly above average
            HealthLeadChange,        // Momentum shift - health advantage changes
            CloseCall,               // Near-death recovery
            Comeback,                // Recovery from low health to win
            DominantStreak,          // Multiple consecutive successful actions
            ActionVariety,           // Different actions used in sequence
            TurnVariance,            // High variance in damage per turn
            CriticalSequence,        // Multiple critical hits in a row
            ComboChain,              // Extended combo sequence
            HealthSwing              // Large health percentage change in one turn
        }

        /// <summary>
        /// Summary of fun moments for a combat session
        /// </summary>
        public class FunMomentSummary
        {
            public int TotalFunMoments { get; set; }
            public double AverageIntensity { get; set; }
            public double FunScore { get; set; } // Overall "fun" score 0-100
            public Dictionary<FunMomentType, int> MomentsByType { get; set; } = new();
            public List<FunMoment> TopMoments { get; set; } = new(); // Top 5 most intense moments
            public double TurnVariance { get; set; } // Variance in damage per turn
            public double ActionVarietyScore { get; set; } // 0-1, higher = more variety
            public int HealthLeadChanges { get; set; }
            public int Comebacks { get; set; }
            public int CloseCalls { get; set; }
        }
    }
}

