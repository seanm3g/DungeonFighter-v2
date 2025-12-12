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

        /// <summary>
        /// Analyzes a batch to identify balance issues
        /// </summary>
        public static AnalysisReport AnalyzeBatch(BatchSimulationRunner.SimulationBatch batch)
        {
            var report = new AnalysisReport();
            var sb = new StringBuilder();

            // Win rate analysis
            double winRate = (double)batch.PlayerWins / batch.TotalSimulations;
            sb.AppendLine($"Win Rate: {winRate:P0} ({batch.PlayerWins}/{batch.TotalSimulations})");
            report.HealthMetrics["winRate"] = winRate;

            if (winRate < 0.5)
            {
                report.Issues.Add("Builds are losing too often (win rate < 50%)");
                report.Suggestions.Add("Increase player damage or reduce enemy health/damage");
            }
            else if (winRate > 0.9)
            {
                report.Issues.Add("Builds are winning too reliably (win rate > 90%)");
                report.Suggestions.Add("Increase enemy damage or reduce player damage");
            }

            // Combat duration analysis
            double avgTurns = batch.AverageTurns;
            sb.AppendLine($"Average Combat Duration: {avgTurns:F1} turns");
            report.HealthMetrics["avgTurns"] = avgTurns;

            // Removed unused targetTurns constant
            if (avgTurns < 6)
            {
                report.Issues.Add("Combats are too short (avg < 6 turns)");
                report.Suggestions.Add("Increase enemy health or armor, or reduce player damage");
            }
            else if (avgTurns > 14)
            {
                report.Issues.Add("Combats are too long (avg > 14 turns)");
                report.Suggestions.Add("Increase player damage or reduce enemy health");
            }

            // Phase distribution analysis
            var winningResults = batch.DetailedResults.Where(r => r.PlayerWon).ToList();
            if (winningResults.Count > 0)
            {
                double phase1Avg = winningResults.Average(r => r.Phase1Turns);
                double phase2Avg = winningResults.Average(r => r.Phase2Turns);
                double phase3Avg = winningResults.Average(r => r.Phase3Turns);
                
                sb.AppendLine($"Phase Distribution: {phase1Avg:F1} | {phase2Avg:F1} | {phase3Avg:F1}");
                report.PhaseMetrics["phase1"] = phase1Avg;
                report.PhaseMetrics["phase2"] = phase2Avg;
                report.PhaseMetrics["phase3"] = phase3Avg;

                // Check for unbalanced phases
                var phases = new[] { phase1Avg, phase2Avg, phase3Avg };
                double maxPhase = phases.Max();
                double minPhase = phases.Min();
                
                if (maxPhase / minPhase > 2.0)
                {
                    report.Issues.Add("Phase distribution is unbalanced");
                    report.Suggestions.Add("Adjust enemy scaling or player progression to balance phases");
                }
            }

            // Win distribution analysis
            sb.AppendLine($"Good Builds (≤6 turns): {batch.WinsIn6OrFewer}");
            sb.AppendLine($"Target Range (6-14 turns): {batch.WinsInTargetRange}");
            sb.AppendLine($"Struggling (≥14 turns): {batch.WinsIn14OrMore}");

            // Analyze build diversity
            if (batch.WinsIn6OrFewer == 0)
            {
                report.Issues.Add("No optimized builds found (none winning in ≤6 turns)");
                report.Suggestions.Add("Look for mechanical interactions that could enable faster kills");
            }
            
            if (batch.WinsInTargetRange < batch.PlayerWins * 0.5)
            {
                report.Issues.Add("Not enough wins in target range (6-14 turns)");
                report.Suggestions.Add("Adjust balance to push more wins toward the middle");
            }

            report.Summary = sb.ToString();
            return report;
        }

        /// <summary>
        /// Suggests tuning parameters based on analysis
        /// </summary>
        public static Dictionary<string, double> SuggestTuning(AnalysisReport report, double adjustmentMagnitude = 1.1)
        {
            var tuning = new Dictionary<string, double>();

            // Damage adjustment
            if (report.HealthMetrics.TryGetValue("winRate", out var winRate))
            {
                if (winRate < 0.5)
                {
                    tuning["playerDamageMultiplier"] = adjustmentMagnitude;
                    tuning["enemyDamageMultiplier"] = 1.0 / adjustmentMagnitude;
                }
                else if (winRate > 0.9)
                {
                    tuning["playerDamageMultiplier"] = 1.0 / adjustmentMagnitude;
                    tuning["enemyDamageMultiplier"] = adjustmentMagnitude;
                }
            }

            // Health adjustment
            if (report.HealthMetrics.TryGetValue("avgTurns", out var avgTurns))
            {
                if (avgTurns < 6)
                {
                    tuning["enemyHealthMultiplier"] = adjustmentMagnitude;
                }
                else if (avgTurns > 14)
                {
                    tuning["enemyHealthMultiplier"] = 1.0 / adjustmentMagnitude;
                }
            }

            return tuning;
        }

        /// <summary>
        /// Formats analysis for display
        /// </summary>
        public static string FormatAnalysisReport(AnalysisReport report)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("=== SIMULATION ANALYSIS ===");
            sb.AppendLine();
            sb.AppendLine(report.Summary);
            sb.AppendLine();
            
            if (report.Issues.Count > 0)
            {
                sb.AppendLine("ISSUES:");
                foreach (var issue in report.Issues)
                {
                    sb.AppendLine($"  • {issue}");
                }
                sb.AppendLine();
            }
            
            if (report.Suggestions.Count > 0)
            {
                sb.AppendLine("SUGGESTIONS:");
                foreach (var suggestion in report.Suggestions)
                {
                    sb.AppendLine($"  • {suggestion}");
                }
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Generates a CSV report for external analysis
        /// </summary>
        public static string ExportToCsv(BatchSimulationRunner.SimulationBatch batch)
        {
            var sb = new StringBuilder();
            
            // Header
            sb.AppendLine("Turn,PlayerHealth,EnemyHealth,PlayerDamage,EnemyDamage,Phase");
            
            // One row per turn across all simulations (simplified)
            foreach (var result in batch.DetailedResults.Take(10)) // Limit for manageability
            {
                sb.AppendLine($"{result.TurnsToComplete}," +
                    $"{result.PlayerFinalHealth}," +
                    $"{result.EnemyFinalHealth}," +
                    $"{result.TotalPlayerDamageDealt}," +
                    $"{result.TotalEnemyDamageDealt}," +
                    $"{(result.PlayerWon ? 'W' : 'L')}");
            }
            
            return sb.ToString();
        }
    }
}
