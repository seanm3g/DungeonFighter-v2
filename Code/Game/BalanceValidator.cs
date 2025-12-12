using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Automated balance validation checks
    /// </summary>
    public static class BalanceValidator
    {
        /// <summary>
        /// Validation result
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; } = true;
            public List<string> Errors { get; set; } = new();
            public List<string> Warnings { get; set; } = new();
            public int TotalChecks { get; set; }
            public int PassedChecks { get; set; }
        }

        /// <summary>
        /// Validate balance based on test results
        /// </summary>
        public static ValidationResult Validate(BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult testResult)
        {
            var result = new ValidationResult();
            result.TotalChecks = 0;
            result.PassedChecks = 0;

            // Check 1: Target win rate range (85-98%)
            result.TotalChecks++;
            var winRates = testResult.CombinationResults.Select(c => c.WinRate).ToList();
            var outOfRange = winRates.Where(w => w < 85 || w > 98).ToList();
            if (outOfRange.Count > 0)
            {
                var count = outOfRange.Count;
                var total = winRates.Count;
                result.Warnings.Add($"{count}/{total} matchups have win rates outside target range (85-98%)");
                result.Warnings.Add($"Out of range: {string.Join(", ", outOfRange.Select(w => $"{w:F1}%"))}");
            }
            else
            {
                result.PassedChecks++;
            }

            // Check 2: Combat duration (8-15 turns)
            result.TotalChecks++;
            var durations = testResult.CombinationResults.Select(c => c.AverageTurns).ToList();
            var outOfRangeDurations = durations.Where(d => d < 8 || d > 15).ToList();
            if (outOfRangeDurations.Count > 0)
            {
                var count = outOfRangeDurations.Count;
                var total = durations.Count;
                result.Warnings.Add($"{count}/{total} matchups have combat duration outside target range (8-15 turns)");
            }
            else
            {
                result.PassedChecks++;
            }

            // Check 3: Weapon balance
            result.TotalChecks++;
            if (testResult.WeaponStatistics.Count > 0)
            {
                var weaponWinRates = testResult.WeaponStatistics.Values.Select(w => w.WinRate).ToList();
                var minWinRate = weaponWinRates.Min();
                var maxWinRate = weaponWinRates.Max();
                var variance = maxWinRate - minWinRate;

                if (variance > 10)
                {
                    result.Errors.Add($"Weapon balance variance too high: {variance:F1}% difference between best and worst weapon");
                    result.IsValid = false;
                }
                else if (variance > 5)
                {
                    result.Warnings.Add($"Weapon balance variance: {variance:F1}% (target: <5%)");
                }
                else
                {
                    result.PassedChecks++;
                }
            }

            // Check 4: Enemy differentiation
            result.TotalChecks++;
            if (testResult.EnemyStatistics.Count > 0)
            {
                var enemyWinRates = testResult.EnemyStatistics.Values.Select(e => e.WinRate).ToList();
                var minWinRate = enemyWinRates.Min();
                var maxWinRate = enemyWinRates.Max();
                var variance = maxWinRate - minWinRate;

                if (variance < 3)
                {
                    result.Warnings.Add($"Enemies feel too similar: Only {variance:F1}% win rate variance (target: >5%)");
                }
                else
                {
                    result.PassedChecks++;
                }
            }

            // Check 5: Overall win rate
            result.TotalChecks++;
            if (testResult.OverallWinRate < 85 || testResult.OverallWinRate > 98)
            {
                result.Errors.Add($"Overall win rate {testResult.OverallWinRate:F1}% is outside target range (85-98%)");
                result.IsValid = false;
            }
            else
            {
                result.PassedChecks++;
            }

            // Check 6: Critical matchups (<80% or >98%)
            result.TotalChecks++;
            var criticalMatchups = testResult.CombinationResults
                .Where(c => c.WinRate < 80 || c.WinRate > 98)
                .ToList();

            if (criticalMatchups.Count > 0)
            {
                result.Errors.Add($"{criticalMatchups.Count} critical matchup(s) detected:");
                foreach (var matchup in criticalMatchups)
                {
                    result.Errors.Add($"  {matchup.WeaponType} vs {matchup.EnemyType}: {matchup.WinRate:F1}% win rate");
                }
                result.IsValid = false;
            }
            else
            {
                result.PassedChecks++;
            }

            return result;
        }

        /// <summary>
        /// Validate scaling consistency across levels
        /// </summary>
        public static ValidationResult ValidateScaling(List<BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult> levelResults)
        {
            var result = new ValidationResult();
            result.TotalChecks = 0;
            result.PassedChecks = 0;

            if (levelResults.Count < 2)
            {
                result.Warnings.Add("Need at least 2 level results to validate scaling");
                return result;
            }

            // Check that win rates remain consistent across levels
            result.TotalChecks++;
            var winRateVariances = new List<double>();
            for (int i = 1; i < levelResults.Count; i++)
            {
                var prevWinRate = levelResults[i - 1].OverallWinRate;
                var currWinRate = levelResults[i].OverallWinRate;
                var variance = Math.Abs(currWinRate - prevWinRate);
                winRateVariances.Add(variance);
            }

            var maxVariance = winRateVariances.Max();
            if (maxVariance > 10)
            {
                result.Warnings.Add($"Win rate variance across levels: {maxVariance:F1}% (target: <10%)");
            }
            else
            {
                result.PassedChecks++;
            }

            // Check that combat duration scales appropriately
            result.TotalChecks++;
            var durationVariances = new List<double>();
            for (int i = 1; i < levelResults.Count; i++)
            {
                var prevDuration = levelResults[i - 1].OverallAverageTurns;
                var currDuration = levelResults[i].OverallAverageTurns;
                var variance = Math.Abs(currDuration - prevDuration);
                durationVariances.Add(variance);
            }

            var maxDurationVariance = durationVariances.Max();
            if (maxDurationVariance > 5)
            {
                result.Warnings.Add($"Combat duration variance across levels: {maxDurationVariance:F1} turns (target: <5)");
            }
            else
            {
                result.PassedChecks++;
            }

            return result;
        }

        /// <summary>
        /// Generate validation report
        /// </summary>
        public static string GenerateReport(ValidationResult validation)
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=".PadRight(80, '='));
            report.AppendLine("BALANCE VALIDATION REPORT");
            report.AppendLine("=".PadRight(80, '='));
            report.AppendLine();

            report.AppendLine($"Status: {(validation.IsValid ? "PASS" : "FAIL")}");
            report.AppendLine($"Checks Passed: {validation.PassedChecks}/{validation.TotalChecks}");
            report.AppendLine();

            if (validation.Errors.Count > 0)
            {
                report.AppendLine("ERRORS:");
                report.AppendLine("-".PadRight(80, '-'));
                foreach (var error in validation.Errors)
                {
                    report.AppendLine($"  ✗ {error}");
                }
                report.AppendLine();
            }

            if (validation.Warnings.Count > 0)
            {
                report.AppendLine("WARNINGS:");
                report.AppendLine("-".PadRight(80, '-'));
                foreach (var warning in validation.Warnings)
                {
                    report.AppendLine($"  ⚠ {warning}");
                }
                report.AppendLine();
            }

            if (validation.Errors.Count == 0 && validation.Warnings.Count == 0)
            {
                report.AppendLine("✓ All validation checks passed!");
            }

            return report.ToString();
        }
    }
}

