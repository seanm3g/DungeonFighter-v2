using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RPGGame.BattleStatistics;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Runs quick balance simulations for the Combat tuning settings panel.
    /// </summary>
    public static class CombatTuningSimulatorService
    {
        public sealed class SimulationRequest
        {
            public int BattlesPerCombination { get; set; } = 50;
            public int PlayerLevel { get; set; } = 1;
            public int EnemyLevel { get; set; } = 1;
        }

        public sealed class SimulationSummary
        {
            public double OverallWinRate { get; set; }
            public double AverageTurns { get; set; }
            public int MinTurns { get; set; }
            public int MaxTurns { get; set; }
            public double TurnCoefficientOfVariation { get; set; }
            public double QualityScore { get; set; }
            public double WeaponWinRateVariance { get; set; }
            public double EnemyWinRateVariance { get; set; }
            public int ValidationPassedChecks { get; set; }
            public int ValidationTotalChecks { get; set; }
            public List<string> ValidationWarnings { get; set; } = new();
            public List<string> TopOffenderMatchups { get; set; } = new();
            public string FormattedReport { get; set; } = "";
        }

        public static Task<SimulationSummary> RunAsync(
            SimulationRequest request,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            // Headless combat is CPU-heavy; never run on the UI synchronization context.
            return Task.Run(async () =>
            {
            var result = await BattleStatisticsRunner.RunComprehensiveWeaponEnemyTests(
                request.BattlesPerCombination,
                request.PlayerLevel,
                request.EnemyLevel,
                progress).ConfigureAwait(false);

            var allTurns = result.CombinationResults
                .SelectMany(c => c.BattleResults)
                .Where(r => r.ErrorMessage == null)
                .Select(r => r.Turns)
                .ToList();

            double avgTurns = allTurns.Count > 0 ? allTurns.Average() : 0;
            int minTurns = allTurns.Count > 0 ? allTurns.Min() : 0;
            int maxTurns = allTurns.Count > 0 ? allTurns.Max() : 0;
            double turnCv = 0;
            if (allTurns.Count > 1 && avgTurns > 0)
            {
                double variance = allTurns.Average(t => Math.Pow(t - avgTurns, 2));
                turnCv = Math.Sqrt(variance) / avgTurns;
            }

            double weaponVariance = 0;
            if (result.WeaponStatistics.Count > 0)
            {
                var rates = result.WeaponStatistics.Values.Select(w => w.WinRate).ToList();
                weaponVariance = rates.Max() - rates.Min();
            }

            double enemyVariance = 0;
            if (result.EnemyStatistics.Count > 0)
            {
                var rates = result.EnemyStatistics.Values.Select(e => e.WinRate).ToList();
                enemyVariance = rates.Max() - rates.Min();
            }

            double qualityScore = BalanceTuningGoals.CalculateQualityScore(
                result.OverallWinRate,
                result.OverallAverageTurns,
                weaponVariance,
                enemyVariance);

            var validation = BalanceValidator.Validate(result);

            var offenders = result.CombinationResults
                .Where(c => c.WinRate < BalanceTuningGoals.WinRateTargets.MinTarget
                            || c.WinRate > BalanceTuningGoals.WinRateTargets.MaxTarget
                            || c.AverageTurns < BalanceTuningGoals.CombatDurationTargets.MinTarget
                            || c.AverageTurns > BalanceTuningGoals.CombatDurationTargets.MaxTarget)
                .OrderBy(c => Math.Abs(c.WinRate - 91.5))
                .Take(5)
                .Select(c => $"{c.WeaponType} vs {c.EnemyType}: {c.WinRate:F1}% win, {c.AverageTurns:F1} turns")
                .ToList();

            var summary = new SimulationSummary
            {
                OverallWinRate = result.OverallWinRate,
                AverageTurns = result.OverallAverageTurns,
                MinTurns = minTurns,
                MaxTurns = maxTurns,
                TurnCoefficientOfVariation = turnCv,
                QualityScore = qualityScore,
                WeaponWinRateVariance = weaponVariance,
                EnemyWinRateVariance = enemyVariance,
                ValidationPassedChecks = validation.PassedChecks,
                ValidationTotalChecks = validation.TotalChecks,
                ValidationWarnings = validation.Warnings.Concat(validation.Errors).ToList(),
                TopOffenderMatchups = offenders
            };

            summary.FormattedReport = FormatReport(summary, request);
            return summary;
            });
        }

        private static string FormatReport(SimulationSummary summary, SimulationRequest request)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Combat Tuning Simulation ===");
            sb.AppendLine($"Battles/matchup: {request.BattlesPerCombination}  |  Player L{request.PlayerLevel}  |  Enemy L{request.EnemyLevel}");
            sb.AppendLine();
            sb.AppendLine($"Overall win rate: {summary.OverallWinRate:F1}%");
            sb.AppendLine($"Average turns: {summary.AverageTurns:F1}  (min {summary.MinTurns}, max {summary.MaxTurns})");
            sb.AppendLine($"Turn CV (σ/μ): {summary.TurnCoefficientOfVariation:F3}");
            sb.AppendLine($"Quality score: {summary.QualityScore:F1} / 100");
            sb.AppendLine($"Weapon win-rate spread: {summary.WeaponWinRateVariance:F1}%");
            sb.AppendLine($"Enemy win-rate spread: {summary.EnemyWinRateVariance:F1}%");
            sb.AppendLine();
            sb.AppendLine($"Validation: {summary.ValidationPassedChecks}/{summary.ValidationTotalChecks} checks passed");
            foreach (var w in summary.ValidationWarnings.Take(8))
                sb.AppendLine($"  - {w}");
            if (summary.TopOffenderMatchups.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Top off-target matchups:");
                foreach (var m in summary.TopOffenderMatchups)
                    sb.AppendLine($"  - {m}");
            }
            return sb.ToString();
        }
    }
}
