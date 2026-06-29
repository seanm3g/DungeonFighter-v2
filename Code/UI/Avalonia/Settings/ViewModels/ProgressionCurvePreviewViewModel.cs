using System;
using System.Collections.ObjectModel;
using System.Linq;
using RPGGame.Tuning;
using RPGGame.Tuning.Profiles;

namespace RPGGame.UI.Avalonia.Settings.ViewModels
{
    public sealed class ProgressionCurveAnchorRowViewModel
    {
        public int Level { get; init; }
        public string GrowthWeightText { get; init; } = "";
        public string EnemyHpText { get; init; } = "";
        public string PlayerHpText { get; init; } = "";
        public string SimCombinedText { get; init; } = "—";
        public string SimWinRateText { get; init; } = "—";
    }

    public sealed class ProgressionCurvePreviewViewModel
    {
        public ObservableCollection<ProgressionCurveAnchorRowViewModel> DecadeRows { get; } = new();

        public void Refresh()
        {
            CombatTuningParameterRegistry.EnsureSanitizedDefaults();
            DecadeRows.Clear();

            var session = LevelTuningSessionStore.Load();
            var fundamentals = session.Fundamentals != null
                ? ComprehensiveSimulationMapper.ToFundamentalsResult(session.Fundamentals)
                : null;

            foreach (int level in EnemyProgressionCurveEvaluator.GetDecadeAnchorLevels())
            {
                var sample = EnemyProgressionCurveEvaluator.GetDerivedSample(level);
                var snap = fundamentals?.GetLevelSnapshot(level);

                DecadeRows.Add(new ProgressionCurveAnchorRowViewModel
                {
                    Level = level,
                    GrowthWeightText = sample.GrowthWeight.ToString("F2"),
                    EnemyHpText = sample.EnemyHealth.ToString(),
                    PlayerHpText = sample.PlayerMaxHealth.ToString(),
                    SimCombinedText = snap != null ? $"{snap.MedianCombinedActions:F0}" : "—",
                    SimWinRateText = snap != null ? $"{snap.WinRate * 100:F0}%" : "—"
                });
            }
        }

        public IReadOnlyList<(int level, int enemyHp, int playerHp)> GetChartPoints()
        {
            return EnemyProgressionCurveEvaluator.GetPreviewSampleLevels()
                .Select(level =>
                {
                    var s = EnemyProgressionCurveEvaluator.GetDerivedSample(level);
                    return (level, s.EnemyHealth, s.PlayerMaxHealth);
                })
                .ToList();
        }
    }
}
