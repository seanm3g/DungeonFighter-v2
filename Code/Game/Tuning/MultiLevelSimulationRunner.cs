using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame.MCP.Tools;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Runs comprehensive weapon×enemy simulations at multiple same-level anchor points.
    /// </summary>
    public static class MultiLevelSimulationRunner
    {
        private const int PrimaryWeaponCount = 4;

        public static int EstimateBattlesPerLevel(int battlesPerCombination)
        {
            int enemyCount = EnemyLoader.GetAllEnemyTypes().Count;
            if (enemyCount == 0)
                enemyCount = 1;
            return PrimaryWeaponCount * enemyCount * battlesPerCombination;
        }

        public static async Task<MultiLevelSimulationResult> RunAsync(
            IReadOnlyList<int>? levels = null,
            int battlesPerCombination = 25,
            IProgress<(int completed, int total, string status)>? progress = null)
        {
            levels ??= LevelWinRateCurve.GetDefaultAnchorLevels();
            var sortedLevels = levels.Distinct().OrderBy(l => l).ToList();

            var result = new MultiLevelSimulationResult
            {
                BattlesPerCombination = battlesPerCombination,
                Timestamp = DateTime.UtcNow
            };

            int step = 0;
            int totalSteps = sortedLevels.Count;
            int battlesPerLevel = EstimateBattlesPerLevel(battlesPerCombination);
            int globalTotal = totalSteps * battlesPerLevel;

            LevelSimulationSnapshot? worstSnapshot = null;

            foreach (int level in sortedLevels)
            {
                step++;
                int levelBase = (step - 1) * battlesPerLevel;
                progress?.Report((levelBase, globalTotal, $"Level {level}: starting ({battlesPerLevel} battles)"));

                var levelProgress = new Progress<(int completed, int total, string status)>(inner =>
                {
                    progress?.Report((
                        levelBase + inner.completed,
                        globalTotal,
                        $"Level {level}: {inner.status}"));
                });

                var levelResult = await BattleStatisticsRunner.RunComprehensiveWeaponEnemyTests(
                    battlesPerCombination,
                    level,
                    level,
                    levelProgress);

                double target = LevelWinRateCurve.GetTargetWinRate(level);
                double actual = levelResult.OverallWinRate;
                var snapshot = new LevelSimulationSnapshot
                {
                    Level = level,
                    TargetWinRate = target,
                    ActualWinRate = actual,
                    AverageTurns = levelResult.OverallAverageTurns,
                    WithinTolerance = LevelWinRateCurve.IsWithinTolerance(actual, level),
                    ConformanceScore = LevelWinRateCurve.GetConformanceScore(actual, level),
                    TotalBattles = levelResult.TotalBattles
                };

                result.LevelSnapshots.Add(snapshot);

                if (worstSnapshot == null || Math.Abs(snapshot.Delta) > Math.Abs(worstSnapshot.Delta))
                {
                    worstSnapshot = snapshot;
                    McpToolState.LastTestResult = levelResult;
                }

                progress?.Report((step * battlesPerLevel, globalTotal,
                    $"Level {level}: WR {actual:F1}% (target {target:F1}%)"));
            }

            result.OverallCurveScore = BalanceTuningGoals.CalculateLevelCurveQualityScore(result);

            if (worstSnapshot != null)
            {
                result.WorstLevel = worstSnapshot.Level;
                result.WorstDeltaMagnitude = Math.Abs(worstSnapshot.Delta);
            }

            var anchorLevels = new HashSet<int>(LevelWinRateCurve.GetCurveAnchorLevels());
            result.AllAnchorsWithinTolerance = result.LevelSnapshots
                .Where(s => anchorLevels.Contains(s.Level))
                .All(s => s.WithinTolerance);

            McpToolState.LastMultiLevelResult = result;
            return result;
        }

        public static string FormatReport(MultiLevelSimulationResult result)
        {
            var lines = new List<string>
            {
                "Level | Target WR | Actual WR | Delta | Turns | Pass",
                "------|-----------|-----------|-------|-------|-----"
            };

            foreach (var s in result.LevelSnapshots.OrderBy(x => x.Level))
            {
                string pass = s.WithinTolerance ? "OK" : "FAIL";
                lines.Add($"{s.Level,5} | {s.TargetWinRate,9:F1} | {s.ActualWinRate,9:F1} | {s.Delta,5:F1} | {s.AverageTurns,5:F1} | {pass}");
            }

            lines.Add("");
            lines.Add($"Curve score: {result.OverallCurveScore:F1}/100");
            lines.Add($"Worst level: {result.WorstLevel} (delta {result.WorstDeltaMagnitude:F1}%)");
            lines.Add($"All anchors within tolerance: {(result.AllAnchorsWithinTolerance ? "yes" : "no")}");

            return string.Join(System.Environment.NewLine, lines);
        }
    }
}
