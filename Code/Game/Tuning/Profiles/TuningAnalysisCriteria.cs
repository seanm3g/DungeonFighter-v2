using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Tuning.Profiles
{
    /// <summary>
    /// Filters validators and suggesters when win-rate optimization is disabled.
    /// </summary>
    public static class TuningAnalysisCriteria
    {
        public static bool IsWinRateValidator(string id) =>
            id.Equals(TuningValidatorIds.LevelCurve, StringComparison.OrdinalIgnoreCase) ||
            id.Equals(TuningValidatorIds.WinRate, StringComparison.OrdinalIgnoreCase) ||
            id.Equals(TuningValidatorIds.Comprehensive, StringComparison.OrdinalIgnoreCase);

        public static bool IsWinRateSuggester(string id) =>
            id.Equals(TuningSuggesterIds.LevelCurve, StringComparison.OrdinalIgnoreCase) ||
            id.Equals(TuningSuggesterIds.Global, StringComparison.OrdinalIgnoreCase) ||
            id.Equals(TuningSuggesterIds.Player, StringComparison.OrdinalIgnoreCase) ||
            id.Equals(TuningSuggesterIds.EnemyBaseline, StringComparison.OrdinalIgnoreCase) ||
            id.Equals(TuningSuggesterIds.Enemy, StringComparison.OrdinalIgnoreCase);

        public static IReadOnlyList<string> GetEffectiveValidators(AnalysisProfileConfig analysis)
        {
            var effective = new List<string>();

            foreach (string id in analysis.Validators.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!analysis.OptimizeWinRate && IsWinRateValidator(id))
                {
                    if (id.Equals(TuningValidatorIds.Comprehensive, StringComparison.OrdinalIgnoreCase))
                    {
                        AddIfMissing(effective, TuningValidatorIds.CombatDuration);
                        AddIfMissing(effective, TuningValidatorIds.WeaponVariance);
                        AddIfMissing(effective, TuningValidatorIds.EnemyDifferentiation);
                    }

                    continue;
                }

                AddIfMissing(effective, id);
            }

            return effective;
        }

        public static IReadOnlyList<string> GetEffectiveSuggesters(AnalysisProfileConfig analysis)
        {
            if (analysis.OptimizeWinRate)
                return analysis.Suggesters.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            return analysis.Suggesters
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(id => !IsWinRateSuggester(id))
                .ToList();
        }

        private static void AddIfMissing(List<string> list, string id)
        {
            if (!list.Any(existing => existing.Equals(id, StringComparison.OrdinalIgnoreCase)))
                list.Add(id);
        }
    }
}
