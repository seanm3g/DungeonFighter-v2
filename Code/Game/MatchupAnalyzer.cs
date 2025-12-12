using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Enhanced matchup analysis tool with visualization and reporting
    /// </summary>
    public static class MatchupAnalyzer
    {
        /// <summary>
        /// Analysis result for a single matchup
        /// </summary>
        public class MatchupResult
        {
            public string WeaponType { get; set; } = "";
            public string EnemyType { get; set; } = "";
            public double WinRate { get; set; }
            public double AverageTurns { get; set; }
            public string Status { get; set; } = ""; // GOOD, WARNING, CRITICAL
            public List<string> Issues { get; set; } = new();
        }

        /// <summary>
        /// Complete analysis report
        /// </summary>
        public class AnalysisReport
        {
            public DateTime GeneratedDate { get; set; }
            public int BattlesPerMatchup { get; set; }
            public int PlayerLevel { get; set; }
            public int EnemyLevel { get; set; }
            public List<MatchupResult> MatchupResults { get; set; } = new();
            public List<string> Issues { get; set; } = new();
            public List<string> Recommendations { get; set; } = new();
            public Dictionary<string, double> WeaponAverages { get; set; } = new();
            public Dictionary<string, double> EnemyAverages { get; set; } = new();
        }

        /// <summary>
        /// Analyze comprehensive test results
        /// </summary>
        public static AnalysisReport Analyze(BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult testResult)
        {
            var report = new AnalysisReport
            {
                GeneratedDate = DateTime.Now,
                BattlesPerMatchup = testResult.CombinationResults.FirstOrDefault()?.TotalBattles ?? 0,
                PlayerLevel = 1, // Default, can be set from test configuration
                EnemyLevel = 1,  // Default, can be set from test configuration
                MatchupResults = new List<MatchupResult>()
            };

            // Analyze each matchup
            foreach (var combination in testResult.CombinationResults)
            {
                var matchup = new MatchupResult
                {
                    WeaponType = combination.WeaponType.ToString(),
                    EnemyType = combination.EnemyType,
                    WinRate = combination.WinRate,
                    AverageTurns = combination.AverageTurns
                };

                // Determine status
                if (combination.WinRate >= 85 && combination.WinRate <= 98)
                {
                    matchup.Status = "GOOD";
                }
                else if (combination.WinRate >= 80 && combination.WinRate < 85)
                {
                    matchup.Status = "WARNING";
                    matchup.Issues.Add($"Win rate {combination.WinRate:F1}% is below target (85-98%)");
                }
                else if (combination.WinRate > 98)
                {
                    matchup.Status = "WARNING";
                    matchup.Issues.Add($"Win rate {combination.WinRate:F1}% is above target (85-98%) - enemy too easy");
                }
                else
                {
                    matchup.Status = "CRITICAL";
                    matchup.Issues.Add($"Win rate {combination.WinRate:F1}% is critically low (<80%)");
                }

                // Check combat duration
                if (combination.AverageTurns < 8)
                {
                    matchup.Issues.Add($"Combat too short: {combination.AverageTurns:F1} turns (target: 8-15)");
                }
                else if (combination.AverageTurns > 15)
                {
                    matchup.Issues.Add($"Combat too long: {combination.AverageTurns:F1} turns (target: 8-15)");
                }

                report.MatchupResults.Add(matchup);
            }

            // Calculate weapon averages
            foreach (var weaponStat in testResult.WeaponStatistics)
            {
                report.WeaponAverages[weaponStat.Key.ToString()] = weaponStat.Value.WinRate;
            }

            // Calculate enemy averages
            foreach (var enemyStat in testResult.EnemyStatistics)
            {
                report.EnemyAverages[enemyStat.Key] = enemyStat.Value.WinRate;
            }

            // Generate issues and recommendations
            GenerateIssuesAndRecommendations(report, testResult);

            return report;
        }

        /// <summary>
        /// Generate issues and recommendations from analysis
        /// </summary>
        private static void GenerateIssuesAndRecommendations(AnalysisReport report, 
            BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult testResult)
        {
            // Find problematic matchups
            var problematicMatchups = report.MatchupResults
                .Where(m => m.Status != "GOOD")
                .ToList();

            foreach (var matchup in problematicMatchups)
            {
                if (matchup.WinRate < 85)
                {
                    report.Issues.Add($"{matchup.WeaponType} vs {matchup.EnemyType}: {matchup.WinRate:F1}% win rate (below 85% target)");
                    report.Recommendations.Add($"Consider: Increase {matchup.WeaponType} base damage or reduce {matchup.EnemyType} health/armor");
                }
                else if (matchup.WinRate > 98)
                {
                    report.Issues.Add($"{matchup.WeaponType} vs {matchup.EnemyType}: {matchup.WinRate:F1}% win rate (above 98% target)");
                    report.Recommendations.Add($"Consider: Increase {matchup.EnemyType} health/damage or reduce {matchup.WeaponType} effectiveness");
                }
            }

            // Check weapon balance
            if (report.WeaponAverages.Count > 0)
            {
                var minWinRate = report.WeaponAverages.Values.Min();
                var maxWinRate = report.WeaponAverages.Values.Max();
                var variance = maxWinRate - minWinRate;

                if (variance > 10)
                {
                    report.Issues.Add($"Weapon balance variance too high: {variance:F1}% difference between best and worst weapon");
                    var worstWeapon = report.WeaponAverages.OrderBy(kvp => kvp.Value).First();
                    var bestWeapon = report.WeaponAverages.OrderByDescending(kvp => kvp.Value).First();
                    report.Recommendations.Add($"Consider: Buff {worstWeapon.Key} (currently {worstWeapon.Value:F1}% win rate) or nerf {bestWeapon.Key} (currently {bestWeapon.Value:F1}% win rate)");
                }
            }

            // Check enemy differentiation
            if (report.EnemyAverages.Count > 0)
            {
                var minWinRate = report.EnemyAverages.Values.Min();
                var maxWinRate = report.EnemyAverages.Values.Max();
                var variance = maxWinRate - minWinRate;

                if (variance < 5)
                {
                    report.Issues.Add($"Enemies feel too similar: Only {variance:F1}% win rate variance");
                    report.Recommendations.Add("Consider: Adjust enemy archetypes to create more distinct playstyles");
                }
            }
        }

        /// <summary>
        /// Generate text report
        /// </summary>
        public static string GenerateTextReport(AnalysisReport report)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine("MATCHUP ANALYSIS REPORT");
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine($"Generated: {report.GeneratedDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Test Configuration: {report.BattlesPerMatchup} battles per matchup");
            sb.AppendLine($"Player Level: {report.PlayerLevel}, Enemy Level: {report.EnemyLevel}");
            sb.AppendLine();

            // Matchup matrix
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine("WEAPON vs ENEMY WIN RATES:");
            sb.AppendLine("-".PadRight(80, '-'));

            var weapons = report.MatchupResults.Select(m => m.WeaponType).Distinct().OrderBy(w => w).ToList();
            var enemies = report.MatchupResults.Select(m => m.EnemyType).Distinct().OrderBy(e => e).ToList();

            // Header
            sb.Append("         ");
            foreach (var enemy in enemies)
            {
                sb.Append($"{enemy,-12}");
            }
            sb.AppendLine();

            // Rows
            foreach (var weapon in weapons)
            {
                sb.Append($"{weapon,-8}");
                foreach (var enemy in enemies)
                {
                    var matchup = report.MatchupResults.FirstOrDefault(m => m.WeaponType == weapon && m.EnemyType == enemy);
                    if (matchup != null)
                    {
                        string status = matchup.Status == "GOOD" ? "[GOOD]" : 
                                       matchup.Status == "WARNING" ? "[WARN]" : "[CRIT]";
                        sb.Append($"{matchup.WinRate,6:F1}% {status,-4}");
                    }
                    else
                    {
                        sb.Append("N/A      ");
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine();

            // Issues
            if (report.Issues.Count > 0)
            {
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine("ISSUES DETECTED:");
                sb.AppendLine("-".PadRight(80, '-'));
                foreach (var issue in report.Issues)
                {
                    sb.AppendLine($"- {issue}");
                }
                sb.AppendLine();
            }

            // Recommendations
            if (report.Recommendations.Count > 0)
            {
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine("RECOMMENDATIONS:");
                sb.AppendLine("-".PadRight(80, '-'));
                for (int i = 0; i < report.Recommendations.Count; i++)
                {
                    sb.AppendLine($"{i + 1}. {report.Recommendations[i]}");
                }
                sb.AppendLine();
            }

            // Weapon averages
            if (report.WeaponAverages.Count > 0)
            {
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine("WEAPON AVERAGE WIN RATES:");
                sb.AppendLine("-".PadRight(80, '-'));
                foreach (var kvp in report.WeaponAverages.OrderByDescending(k => k.Value))
                {
                    sb.AppendLine($"{kvp.Key,-12}: {kvp.Value,6:F1}%");
                }
                sb.AppendLine();
            }

            // Enemy averages
            if (report.EnemyAverages.Count > 0)
            {
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine("ENEMY AVERAGE WIN RATES:");
                sb.AppendLine("-".PadRight(80, '-'));
                foreach (var kvp in report.EnemyAverages.OrderBy(k => k.Value))
                {
                    sb.AppendLine($"{kvp.Key,-12}: {kvp.Value,6:F1}%");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Export report to file
        /// </summary>
        public static bool ExportReport(AnalysisReport report, string filePath)
        {
            try
            {
                string textReport = GenerateTextReport(report);
                File.WriteAllText(filePath, textReport);
                ScrollDebugLogger.Log($"MatchupAnalyzer: Exported report to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"MatchupAnalyzer: Error exporting report: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Compare two analysis reports
        /// </summary>
        public static string CompareReports(AnalysisReport baseline, AnalysisReport current)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine("COMPARATIVE ANALYSIS");
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine();

            // Overall win rate comparison
            var baselineOverall = baseline.MatchupResults.Average(m => m.WinRate);
            var currentOverall = current.MatchupResults.Average(m => m.WinRate);
            var change = currentOverall - baselineOverall;

            sb.AppendLine($"Overall Win Rate: {baselineOverall:F1}% → {currentOverall:F1}% ({change:+#.0;-#.0;+0.0}%)");
            sb.AppendLine();

            // Matchup changes
            sb.AppendLine("Matchup Changes:");
            foreach (var currentMatchup in current.MatchupResults)
            {
                var baselineMatchup = baseline.MatchupResults.FirstOrDefault(m => 
                    m.WeaponType == currentMatchup.WeaponType && 
                    m.EnemyType == currentMatchup.EnemyType);

                if (baselineMatchup != null)
                {
                    var winRateChange = currentMatchup.WinRate - baselineMatchup.WinRate;
                    if (Math.Abs(winRateChange) > 1.0) // Only show significant changes
                    {
                        sb.AppendLine($"  {currentMatchup.WeaponType} vs {currentMatchup.EnemyType}: " +
                            $"{baselineMatchup.WinRate:F1}% → {currentMatchup.WinRate:F1}% ({winRateChange:+#.0;-#.0;+0.0}%)");
                    }
                }
            }

            return sb.ToString();
        }
    }
}

