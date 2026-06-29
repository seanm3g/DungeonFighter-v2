using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;

namespace RPGGame.Tuning.Suggesters
{
    public static class MultiLevelDurationAdjustmentSuggester
    {
        public static List<TuningSuggestion> Suggest(MultiLevelSimulationResult multiResult)
        {
            var suggestions = new List<TuningSuggestion>();
            if (multiResult.LevelSnapshots.Count == 0)
                return suggestions;

            double avgTurns = multiResult.LevelSnapshots.Average(s => s.AverageTurns);
            var analysis = new TuningAnalysis { AverageCombatDuration = avgTurns };
            var dummy = new BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult();
            return DurationAdjustmentSuggester.Suggest(dummy, analysis);
        }
    }
}
