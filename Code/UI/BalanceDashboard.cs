using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RPGGame.Utils;

namespace RPGGame.UI
{
    /// <summary>
    /// Visual dashboard for balance status and metrics
    /// </summary>
    public static class BalanceDashboard
    {
        /// <summary>
        /// Display balance dashboard
        /// </summary>
        public static void Display(MatchupAnalyzer.AnalysisReport? report = null)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine("BALANCE DASHBOARD");
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine();

            if (report != null)
            {
                // Overall status
                var overallWinRate = report.MatchupResults.Average(m => m.WinRate);
                var statusColor = GetStatusColor(overallWinRate);
                sb.AppendLine($"Overall Win Rate: {statusColor}{overallWinRate:F1}%{GetResetColor()}");
                sb.AppendLine($"Status: {GetOverallStatus(overallWinRate)}");
                sb.AppendLine();

                // Matchup matrix (simplified)
                sb.AppendLine("Matchup Status:");
                var goodCount = report.MatchupResults.Count(m => m.Status == "GOOD");
                var warnCount = report.MatchupResults.Count(m => m.Status == "WARNING");
                var critCount = report.MatchupResults.Count(m => m.Status == "CRITICAL");
                
                sb.AppendLine($"  ✓ Good: {goodCount}");
                sb.AppendLine($"  ⚠ Warning: {warnCount}");
                sb.AppendLine($"  ✗ Critical: {critCount}");
                sb.AppendLine();

                // Key metrics
                var avgTurns = report.MatchupResults.Average(m => m.AverageTurns);
                sb.AppendLine($"Average Combat Duration: {avgTurns:F1} turns");
                sb.AppendLine($"Target Range: 8-15 turns");
                sb.AppendLine();

                // Top issues
                if (report.Issues.Count > 0)
                {
                    sb.AppendLine("Top Issues:");
                    foreach (var issue in report.Issues.Take(5))
                    {
                        sb.AppendLine($"  • {issue}");
                    }
                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("No analysis data available.");
                sb.AppendLine("Run matchup analysis to see balance status.");
                sb.AppendLine();
            }

            // Current configuration summary
            var config = GameConfiguration.Instance;
            sb.AppendLine("Current Configuration:");
            sb.AppendLine($"  Enemy Health Multiplier: {config.EnemySystem.GlobalMultipliers.HealthMultiplier:F2}x");
            sb.AppendLine($"  Enemy Damage Multiplier: {config.EnemySystem.GlobalMultipliers.DamageMultiplier:F2}x");
            sb.AppendLine($"  Enemy Armor Multiplier: {config.EnemySystem.GlobalMultipliers.ArmorMultiplier:F2}x");
            sb.AppendLine();

            TextDisplayIntegration.DisplaySystem(sb.ToString());
        }

        /// <summary>
        /// Get status color (simplified - returns empty string for now)
        /// </summary>
        private static string GetStatusColor(double winRate)
        {
            if (winRate >= 85 && winRate <= 98)
                return ""; // Green
            else if (winRate >= 80 && winRate < 85)
                return ""; // Yellow
            else
                return ""; // Red
        }

        /// <summary>
        /// Get reset color
        /// </summary>
        private static string GetResetColor()
        {
            return "";
        }

        /// <summary>
        /// Get overall status text
        /// </summary>
        private static string GetOverallStatus(double winRate)
        {
            if (winRate >= 85 && winRate <= 98)
                return "✓ Balanced";
            else if (winRate >= 80 && winRate < 85)
                return "⚠ Needs Adjustment";
            else if (winRate > 98)
                return "⚠ Too Easy";
            else
                return "✗ Too Hard";
        }

        /// <summary>
        /// Display tuning impact preview
        /// </summary>
        public static void DisplayTuningPreview(string parameter, double currentValue, double newValue, 
            double? expectedWinRateChange = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Tuning Impact Preview:");
            sb.AppendLine($"  Parameter: {parameter}");
            sb.AppendLine($"  Current: {currentValue:F2}");
            sb.AppendLine($"  New: {newValue:F2}");
            sb.AppendLine($"  Change: {((newValue - currentValue) / currentValue * 100):+#.0;-#.0;+0.0}%");
            
            if (expectedWinRateChange.HasValue)
            {
                sb.AppendLine($"  Expected Win Rate Change: {expectedWinRateChange.Value:+#.0;-#.0;+0.0}%");
            }
            
            sb.AppendLine();
            sb.AppendLine("Note: This is an estimate. Run tests to verify actual impact.");
            
            TextDisplayIntegration.DisplaySystem(sb.ToString());
        }

        /// <summary>
        /// Display patch browser
        /// </summary>
        public static void DisplayPatchBrowser(List<Config.BalancePatchManager.BalancePatch> patches)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine("BALANCE PATCHES");
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine();

            if (patches.Count == 0)
            {
                sb.AppendLine("No patches available.");
                sb.AppendLine();
            }
            else
            {
                for (int i = 0; i < patches.Count; i++)
                {
                    var patch = patches[i];
                    sb.AppendLine($"{i + 1}. {patch.PatchMetadata.Name} v{patch.PatchMetadata.Version}");
                    sb.AppendLine($"   Author: {patch.PatchMetadata.Author}");
                    sb.AppendLine($"   Description: {patch.PatchMetadata.Description}");
                    if (patch.PatchMetadata.Tags.Count > 0)
                    {
                        sb.AppendLine($"   Tags: {string.Join(", ", patch.PatchMetadata.Tags)}");
                    }
                    sb.AppendLine();
                }
            }

            TextDisplayIntegration.DisplaySystem(sb.ToString());
        }
    }
}

