using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPGGame.Simulation
{
    /// <summary>
    /// Analyzes simulation results to understand balance and suggest tuning
    /// </summary>
    public class SimulationAnalyzer
    {
        public class AnalysisReport
        {
            public string Summary { get; set; } = string.Empty;
            public List<string> Issues { get; set; } = new();
            public List<string> Suggestions { get; set; } = new();
            public Dictionary<string, double> HealthMetrics { get; set; } = new();
            public Dictionary<string, double> PhaseMetrics { get; set; } = new();
        }

    }
}
