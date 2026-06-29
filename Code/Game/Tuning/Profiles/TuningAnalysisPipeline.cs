using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;
using RPGGame.Tuning.Suggesters;

namespace RPGGame.Tuning.Profiles
{
    public sealed class TuningAnalysisOutcome
    {
        public TuningAnalysis Analysis { get; init; } = new();
        public BalanceValidator.ValidationResult Validation { get; init; } = new();
        public bool AllChecksPass { get; init; }
    }

    public static class TuningAnalysisPipeline
    {
        public static TuningAnalysisOutcome Analyze(BalanceTuningProfile profile, TuningSimulationOutcome simulation)
        {
            var validatorIds = TuningAnalysisCriteria.GetEffectiveValidators(profile.Analysis);
            var suggesterIds = TuningAnalysisCriteria.GetEffectiveSuggesters(profile.Analysis);

            var validation = RunValidators(profile, validatorIds, simulation);
            var analysis = BuildAnalysis(profile, simulation);
            var classification = BalanceDialClassifier.Classify(simulation, analysis);
            analysis.PrimaryDial = classification.PrimaryDial.ToString();
            analysis.DialDiagnosis = classification.Diagnosis;

            if (profile.Analysis.FundamentalsTargets?.RequireL1AnchorBeforeScaling == true
                && simulation.Fundamentals != null)
            {
                suggesterIds = new List<string> { TuningSuggesterIds.Duration };
            }
            else if (profile.Analysis.EnableDialRouting || suggesterIds.Contains(TuningSuggesterIds.DialRouted))
            {
                suggesterIds = BalanceDialClassifier.SuggestersForDial(classification.PrimaryDial).ToList();
            }

            var suggestions = RunSuggesters(suggesterIds, simulation, analysis, profile);

            suggestions = suggestions
                .OrderBy(s => (int)s.Priority)
                .ThenByDescending(s => Math.Abs(s.AdjustmentMagnitude))
                .Take(profile.Analysis.MaxSuggestions)
                .ToList();

            analysis.Suggestions = suggestions;
            analysis.SuggestionCounts = suggestions
                .GroupBy(s => s.Priority)
                .ToDictionary(g => g.Key, g => g.Count());

            if (string.IsNullOrWhiteSpace(analysis.Summary))
                analysis.Summary = BuildSummary(profile, simulation, suggestions, analysis);

            bool allPass = validation.IsValid &&
                           (profile.Analysis.OptimizeWinRate == false ||
                            simulation.Mode != TuningSimulationModes.MultiLevelWeaponEnemy ||
                            simulation.MultiLevel?.AllAnchorsWithinTolerance != false);

            return new TuningAnalysisOutcome
            {
                Analysis = analysis,
                Validation = validation,
                AllChecksPass = allPass && suggestions.Count == 0
            };
        }

        private static BalanceValidator.ValidationResult RunValidators(
            BalanceTuningProfile profile,
            IReadOnlyList<string> validatorIds,
            TuningSimulationOutcome simulation)
        {
            var merged = new BalanceValidator.ValidationResult();

            foreach (string id in validatorIds.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var partial = RunValidator(profile, id, simulation);
                MergeValidation(merged, partial);
            }

            return merged;
        }

        private static BalanceValidator.ValidationResult RunValidator(
            BalanceTuningProfile profile,
            string id,
            TuningSimulationOutcome simulation)
        {
            switch (id.ToLowerInvariant())
            {
                case TuningValidatorIds.LevelCurve:
                    if (simulation.MultiLevel == null)
                        return WarningOnly("level_curve validator requires multi-level simulation data");
                    return BalanceValidator.ValidateMultiLevel(simulation.MultiLevel);

                case TuningValidatorIds.Comprehensive:
                    if (simulation.Comprehensive == null)
                        return WarningOnly("comprehensive validator requires comprehensive simulation data");
                    return BalanceValidator.Validate(simulation.Comprehensive);

                case TuningValidatorIds.WinRate:
                    return ValidateWinRate(simulation);

                case TuningValidatorIds.CombatDuration:
                    return ValidateCombatDuration(simulation);

                case TuningValidatorIds.WeaponVariance:
                    return ValidateWeaponVariance(simulation);

                case TuningValidatorIds.EnemyDifferentiation:
                    return ValidateEnemyDifferentiation(simulation);

                case TuningValidatorIds.FundamentalsTempo:
                    return ValidateFundamentalsTempo(simulation, profile);

                case TuningValidatorIds.FundamentalsAnchors:
                    return ValidateFundamentalsAnchors(simulation, profile);

                case TuningValidatorIds.FundamentalsComboStreaks:
                    return ValidateFundamentalsComboStreaks(simulation, profile);

                case TuningValidatorIds.DialVariance:
                    return ValidateDialVariance(simulation);

                case TuningValidatorIds.DialAgency:
                    return ValidateDialAgency(simulation);

                default:
                    return WarningOnly($"Unknown validator '{id}'");
            }
        }

        private static BalanceValidator.ValidationResult ValidateWinRate(TuningSimulationOutcome simulation)
        {
            var result = new BalanceValidator.ValidationResult { TotalChecks = 1 };
            double winRate = simulation.Mode switch
            {
                TuningSimulationModes.MultiLevelWeaponEnemy => simulation.MultiLevel?.LevelSnapshots.Average(s => s.ActualWinRate) ?? 0,
                _ => simulation.Comprehensive?.OverallWinRate ?? 0
            };

            var targets = BalanceTuningGoals.WinRateTargets;
            if (winRate < targets.MinTarget || winRate > targets.MaxTarget)
            {
                result.Warnings.Add($"Overall win rate {winRate:F1}% outside target {targets.MinTarget}-{targets.MaxTarget}%");
                result.IsValid = false;
            }
            else
            {
                result.PassedChecks = 1;
            }

            return result;
        }

        private static BalanceValidator.ValidationResult ValidateCombatDuration(TuningSimulationOutcome simulation)
        {
            var result = new BalanceValidator.ValidationResult { TotalChecks = 1 };
            var durationTargets = BalanceTuningGoals.CombatDurationTargets;

            if (simulation.MultiLevel != null)
            {
                var failures = simulation.MultiLevel.LevelSnapshots
                    .Where(s => s.AverageTurns < durationTargets.MinTarget || s.AverageTurns > durationTargets.MaxTarget)
                    .ToList();
                if (failures.Count > 0)
                {
                    result.Warnings.Add($"{failures.Count} level(s) have combat duration outside {durationTargets.MinTarget}-{durationTargets.MaxTarget} turns");
                    result.IsValid = false;
                }
                else
                    result.PassedChecks = 1;
                return result;
            }

            double turns = simulation.Comprehensive?.OverallAverageTurns ?? 0;
            if (turns < durationTargets.MinTarget || turns > durationTargets.MaxTarget)
            {
                result.Warnings.Add($"Average combat duration {turns:F1} turns outside {durationTargets.MinTarget}-{durationTargets.MaxTarget}");
                result.IsValid = false;
            }
            else
                result.PassedChecks = 1;

            return result;
        }

        private static BalanceValidator.ValidationResult ValidateWeaponVariance(TuningSimulationOutcome simulation)
        {
            var result = new BalanceValidator.ValidationResult { TotalChecks = 1 };
            var comprehensive = simulation.Comprehensive;
            if (comprehensive == null || comprehensive.WeaponStatistics.Count == 0)
            {
                result.Warnings.Add("weapon_variance validator requires comprehensive weapon statistics");
                return result;
            }

            var rates = comprehensive.WeaponStatistics.Values.Select(w => w.WinRate).ToList();
            double variance = rates.Max() - rates.Min();
            double max = BalanceTuningGoals.WeaponBalanceTargets.MaxVariance;

            if (variance > max)
            {
                result.Errors.Add($"Weapon balance variance {variance:F1}% exceeds max {max:F1}%");
                result.IsValid = false;
            }
            else if (variance > max / 2)
            {
                result.Warnings.Add($"Weapon balance variance {variance:F1}% (target ≤{max:F1}%)");
            }
            else
            {
                result.PassedChecks = 1;
            }

            return result;
        }

        private static BalanceValidator.ValidationResult ValidateEnemyDifferentiation(TuningSimulationOutcome simulation)
        {
            var result = new BalanceValidator.ValidationResult { TotalChecks = 1 };
            var comprehensive = simulation.Comprehensive;
            if (comprehensive == null || comprehensive.EnemyStatistics.Count == 0)
            {
                result.Warnings.Add("enemy_differentiation validator requires comprehensive enemy statistics");
                return result;
            }

            var rates = comprehensive.EnemyStatistics.Values.Select(e => e.WinRate).ToList();
            double variance = rates.Max() - rates.Min();
            double min = BalanceTuningGoals.EnemyDifferentiationTargets.MinVariance;

            if (variance < min)
            {
                result.Warnings.Add($"Enemies feel too similar: {variance:F1}% win-rate spread (target ≥{min:F1}%)");
            }
            else
            {
                result.PassedChecks = 1;
            }

            return result;
        }

        private static TuningAnalysis BuildAnalysis(BalanceTuningProfile profile, TuningSimulationOutcome simulation)
        {
            if (simulation.Fundamentals != null)
            {
                var f = simulation.Fundamentals;
                double tempoScore = ScoreInBand(f.AverageActionsPerEncounter, profile.Analysis.FundamentalsTargets!.MinAverageActions, profile.Analysis.FundamentalsTargets.MaxAverageActions);
                double streakScore = Math.Min(100, f.AverageComboStreakRuns2PlusPerEncounter * 40 + f.AverageMaxComboStreak * 15);
                return new TuningAnalysis
                {
                    OverallWinRate = f.WinRate * 100.0,
                    AverageCombatDuration = f.AveragePlayerTurnsPerEncounter,
                    TurnDurationStdDev = f.TurnDurationStdDev,
                    AverageMissRate = f.AverageMissRate,
                    AverageCritRate = f.AverageCritRate,
                    AverageLossSeverity = f.AverageLossSeverity,
                    AverageComboStreak = f.AverageMaxComboStreak,
                    QualityScore = (tempoScore + streakScore) / 2.0,
                    Summary = $"Fundamentals: hero median {f.MedianPlayerTurnsPerEncounter:F0}, enemy median {f.MedianEnemyTurnsPerEncounter:F0}, combined median {f.MedianActionsPerEncounter:F0} — {f.AverageComboStreakRuns2PlusPerEncounter:F2} combo+ chains (≥2)/encounter, max chain {f.AverageMaxComboStreak:F2}"
                };
            }

            if (simulation.MultiLevel != null)
            {
                return new TuningAnalysis
                {
                    OverallWinRate = simulation.MultiLevel.LevelSnapshots.Count > 0
                        ? simulation.MultiLevel.LevelSnapshots.Average(s => s.ActualWinRate)
                        : 0,
                    AverageCombatDuration = simulation.MultiLevel.LevelSnapshots.Count > 0
                        ? simulation.MultiLevel.LevelSnapshots.Average(s => s.AverageTurns)
                        : 0,
                    QualityScore = profile.Analysis.OptimizeWinRate
                        ? BalanceTuningGoals.CalculateLevelCurveQualityScore(simulation.MultiLevel)
                        : BalanceTuningGoals.CalculateMultiLevelDurationQualityScore(simulation.MultiLevel)
                };
            }

            var testResult = simulation.Comprehensive ?? new BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult();
            var analysis = new TuningAnalysis
            {
                OverallWinRate = testResult.OverallWinRate,
                AverageCombatDuration = testResult.OverallAverageTurns
            };

            if (testResult.WeaponStatistics.Count > 0)
            {
                var weaponWinRates = testResult.WeaponStatistics.Values.Select(w => w.WinRate).ToList();
                analysis.WeaponVariance = weaponWinRates.Max() - weaponWinRates.Min();
            }

            if (testResult.EnemyStatistics.Count > 0)
            {
                var enemyWinRates = testResult.EnemyStatistics.Values.Select(e => e.WinRate).ToList();
                analysis.EnemyVariance = enemyWinRates.Max() - enemyWinRates.Min();
            }

            analysis.QualityScore = profile.Analysis.OptimizeWinRate
                ? BalanceTuningGoals.CalculateQualityScore(
                    analysis.OverallWinRate,
                    analysis.AverageCombatDuration,
                    analysis.WeaponVariance,
                    analysis.EnemyVariance)
                : BalanceTuningGoals.CalculateQualityScoreWithoutWinRate(
                    analysis.AverageCombatDuration,
                    analysis.WeaponVariance,
                    analysis.EnemyVariance);

            return analysis;
        }

        private static List<TuningSuggestion> RunSuggesters(
            IReadOnlyList<string> suggesterIds,
            TuningSimulationOutcome simulation,
            TuningAnalysis analysis,
            BalanceTuningProfile profile)
        {
            var suggestions = new List<TuningSuggestion>();

            foreach (string id in suggesterIds.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                switch (id.ToLowerInvariant())
                {
                    case TuningSuggesterIds.LevelCurve:
                        if (simulation.MultiLevel != null)
                            suggestions.AddRange(LevelCurveAdjustmentSuggester.Suggest(simulation.MultiLevel));
                        break;

                    case TuningSuggesterIds.Variance:
                        suggestions.AddRange(VarianceAdjustmentSuggester.Suggest(analysis));
                        break;

                    case TuningSuggesterIds.Agency:
                        suggestions.AddRange(AgencyAdjustmentSuggester.Suggest(analysis));
                        break;

                    case TuningSuggesterIds.DungeonScaling:
                        suggestions.AddRange(DungeonScalingAdjustmentSuggester.Suggest(analysis));
                        break;

                    case TuningSuggesterIds.Global:
                    case TuningSuggesterIds.Player:
                    case TuningSuggesterIds.EnemyBaseline:
                    case TuningSuggesterIds.Weapon:
                    case TuningSuggesterIds.Enemy:
                    case TuningSuggesterIds.Duration:
                        if (simulation.Comprehensive != null)
                            suggestions.AddRange(RunComprehensiveSuggester(id, simulation.Comprehensive, analysis));
                        else if (simulation.MultiLevel != null && id == TuningSuggesterIds.Duration)
                            suggestions.AddRange(MultiLevelDurationAdjustmentSuggester.Suggest(simulation.MultiLevel));
                        else if (simulation.Fundamentals != null && id == TuningSuggesterIds.Duration)
                            suggestions.AddRange(RunFundamentalsDurationSuggester(simulation.Fundamentals, profile));
                        break;
                }
            }

            return suggestions;
        }

        private static IEnumerable<TuningSuggestion> RunFundamentalsDurationSuggester(
            FundamentalsSimulationResult fundamentals,
            BalanceTuningProfile profile) =>
            FundamentalsDurationAdjustmentSuggester.Suggest(fundamentals, profile.Analysis.FundamentalsTargets);

        private static IEnumerable<TuningSuggestion> RunComprehensiveSuggester(
            string id,
            BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult testResult,
            TuningAnalysis analysis) =>
            id.ToLowerInvariant() switch
            {
                TuningSuggesterIds.Global => GlobalAdjustmentSuggester.Suggest(testResult, analysis),
                TuningSuggesterIds.Player => PlayerAdjustmentSuggester.Suggest(testResult, analysis),
                TuningSuggesterIds.EnemyBaseline => EnemyBaselineAdjustmentSuggester.Suggest(testResult, analysis),
                TuningSuggesterIds.Weapon => WeaponBalanceSuggester.Suggest(testResult, analysis),
                TuningSuggesterIds.Enemy => EnemyAdjustmentSuggester.Suggest(testResult, analysis),
                TuningSuggesterIds.Duration => DurationAdjustmentSuggester.Suggest(testResult, analysis),
                _ => Array.Empty<TuningSuggestion>()
            };

        private static string BuildSummary(
            BalanceTuningProfile profile,
            TuningSimulationOutcome simulation,
            IReadOnlyList<TuningSuggestion> suggestions,
            TuningAnalysis analysis)
        {
            if (simulation.Fundamentals != null)
            {
                var f = simulation.Fundamentals;
                string dial = string.IsNullOrEmpty(analysis.PrimaryDial) ? "" : $" [{analysis.PrimaryDial} dial]";
                return $"{profile.Name}{dial}: {f.AverageActionsPerEncounter:F1} combined actions/encounter (hero {f.MedianPlayerTurnsPerEncounter:F0}, enemy {f.MedianEnemyTurnsPerEncounter:F0}), {f.AverageComboStreakRuns2PlusPerEncounter:F2} combo+ chains (≥2)/encounter";
            }

            if (simulation.MultiLevel != null)
            {
                if (profile.Analysis.OptimizeWinRate)
                {
                    return suggestions.Count > 0
                        ? $"Level curve ({profile.Id}): worst level {simulation.MultiLevel.WorstLevel}, delta {simulation.MultiLevel.WorstDeltaMagnitude:F1}%"
                        : "All anchor levels within tolerance";
                }

                double avgTurns = simulation.MultiLevel.LevelSnapshots.Count > 0
                    ? simulation.MultiLevel.LevelSnapshots.Average(s => s.AverageTurns)
                    : 0;
                return suggestions.Count > 0
                    ? $"{profile.Name}: avg {avgTurns:F1} turns/level — {suggestions.Count} suggestion(s)"
                    : $"{profile.Name}: combat duration within target at all levels (avg {avgTurns:F1} turns)";
            }

            return suggestions.Count > 0
                ? $"{profile.Name}: quality {simulation.Comprehensive?.OverallWinRate:F1}% WR — {suggestions.Count} suggestion(s)"
                : $"{profile.Name}: no automated adjustments recommended";
        }

        private static void MergeValidation(
            BalanceValidator.ValidationResult target,
            BalanceValidator.ValidationResult source)
        {
            target.TotalChecks += source.TotalChecks;
            target.PassedChecks += source.PassedChecks;
            target.Errors.AddRange(source.Errors);
            target.Warnings.AddRange(source.Warnings);
            if (!source.IsValid)
                target.IsValid = false;
        }

        private static BalanceValidator.ValidationResult WarningOnly(string message) =>
            new() { Warnings = { message } };

        private static BalanceValidator.ValidationResult ValidateFundamentalsTempo(
            TuningSimulationOutcome simulation,
            BalanceTuningProfile profile)
        {
            var result = new BalanceValidator.ValidationResult { TotalChecks = 3 };
            var f = simulation.Fundamentals;
            if (f == null)
            {
                result.Warnings.Add("fundamentals_tempo requires fundamentals simulation data");
                return result;
            }

            var targets = profile.Analysis.FundamentalsTargets ?? new FundamentalsAnalysisTargets();
            double tolerance = targets.TempoTolerance > 0 ? targets.TempoTolerance : 1.5;
            result.TotalChecks = 3;

            if (Math.Abs(f.MedianPlayerTurnsPerEncounter - targets.TargetMedianPlayerTurns) > tolerance)
            {
                result.Warnings.Add(
                    $"Median hero turns {f.MedianPlayerTurnsPerEncounter:F1} outside target {targets.TargetMedianPlayerTurns:F0}±{tolerance:F0}");
                result.IsValid = false;
            }
            else
                result.PassedChecks++;

            if (Math.Abs(f.MedianEnemyTurnsPerEncounter - targets.TargetMedianEnemyTurns) > tolerance)
            {
                result.Warnings.Add(
                    $"Median enemy turns {f.MedianEnemyTurnsPerEncounter:F1} outside target {targets.TargetMedianEnemyTurns:F0}±{tolerance:F0}");
                result.IsValid = false;
            }
            else
                result.PassedChecks++;

            if (Math.Abs(f.MedianActionsPerEncounter - targets.TargetMedianCombinedActions) > tolerance * 2)
            {
                result.Warnings.Add(
                    $"Median combined actions {f.MedianActionsPerEncounter:F1} outside target {targets.TargetMedianCombinedActions:F0}±{tolerance * 2:F0}");
                result.IsValid = false;
            }
            else
                result.PassedChecks++;

            if (f.AverageActionsPerEncounter < targets.MinAverageActions || f.AverageActionsPerEncounter > targets.MaxAverageActions)
            {
                result.Warnings.Add(
                    $"Mean combined actions {f.AverageActionsPerEncounter:F1} outside band {targets.MinAverageActions}-{targets.MaxAverageActions}");
                result.IsValid = false;
            }

            return result;
        }

        private static BalanceValidator.ValidationResult ValidateFundamentalsAnchors(
            TuningSimulationOutcome simulation,
            BalanceTuningProfile profile)
        {
            var result = new BalanceValidator.ValidationResult();
            var f = simulation.Fundamentals;
            if (f == null)
            {
                result.Warnings.Add("fundamentals_anchors requires fundamentals simulation data");
                return result;
            }

            var targets = profile.Analysis.FundamentalsTargets ?? new FundamentalsAnalysisTargets();
            double targetCombined = FundamentalsDurationAdjustmentSuggester.ResolveTargetCombinedActions(targets);
            const double tolerance = 1.5;

            var snapshots = f.LevelSnapshots.Count > 0
                ? f.LevelSnapshots
                : new[] { new FundamentalsLevelSnapshot
                {
                    Level = f.PlayerLevel,
                    MedianCombinedActions = f.MedianActionsPerEncounter,
                    WinRate = f.WinRate
                }};

            result.TotalChecks = snapshots.Count * 2;
            foreach (var snap in snapshots.OrderBy(s => s.Level))
            {
                if (Math.Abs(snap.MedianCombinedActions - targetCombined) <= tolerance)
                    result.PassedChecks++;
                else
                {
                    result.Warnings.Add(
                        $"L{snap.Level} median combined {snap.MedianCombinedActions:F0} outside target {targetCombined:F0}±{tolerance:F0}");
                    result.IsValid = false;
                }

                double targetWr = LevelWinRateCurve.GetTargetWinRate(snap.Level);
                double wrTol = LevelWinRateCurve.GetTolerance(snap.Level);
                if (LevelWinRateCurve.IsWinRateInBand(snap.WinRate, snap.Level))
                    result.PassedChecks++;
                else
                {
                    double actualWr = LevelWinRateCurve.NormalizeToPercent(snap.WinRate);
                    result.Warnings.Add(
                        $"L{snap.Level} win rate {actualWr:F0}% outside level curve target {targetWr:F0}%±{wrTol:F0}");
                    result.IsValid = false;
                }
            }

            return result;
        }

        private static BalanceValidator.ValidationResult ValidateFundamentalsComboStreaks(
            TuningSimulationOutcome simulation,
            BalanceTuningProfile profile)
        {
            var result = new BalanceValidator.ValidationResult { TotalChecks = 2 };
            var f = simulation.Fundamentals;
            if (f == null)
            {
                result.Warnings.Add("fundamentals_combo_streaks requires fundamentals simulation data");
                return result;
            }

            var targets = profile.Analysis.FundamentalsTargets ?? new FundamentalsAnalysisTargets();

            result.TotalChecks = 2;
            if (f.AverageComboStreakRuns2PlusPerEncounter < targets.MinAverageComboStreakRuns2Plus)
            {
                result.Warnings.Add(
                    $"Mean completed 2+ combo+ chains per encounter {f.AverageComboStreakRuns2PlusPerEncounter:F2} below target {targets.MinAverageComboStreakRuns2Plus:F2}");
                result.IsValid = false;
            }
            else
                result.PassedChecks++;

            if (f.AverageMaxComboStreak < targets.MinAverageMaxComboStreak)
            {
                result.Warnings.Add(
                    $"Mean longest combo+ chain {f.AverageMaxComboStreak:F2} below target {targets.MinAverageMaxComboStreak:F2}");
                result.IsValid = false;
            }
            else
                result.PassedChecks++;

            return result;
        }

        private static BalanceValidator.ValidationResult ValidateDialVariance(TuningSimulationOutcome simulation)
        {
            var result = new BalanceValidator.ValidationResult { TotalChecks = 2 };
            var f = simulation.Fundamentals;
            if (f == null && simulation.Comprehensive == null)
            {
                result.Warnings.Add("dial_variance requires fundamentals or comprehensive simulation data");
                return result;
            }

            double stdDev = f?.TurnDurationStdDev ?? 0;
            double missRate = f?.AverageMissRate ?? 0;
            result.TotalChecks = 2;

            if (stdDev > 5.0)
            {
                result.Warnings.Add($"Turn duration std dev {stdDev:F1} indicates high variance");
                result.IsValid = false;
            }
            else
                result.PassedChecks++;

            if (missRate > 0.4)
            {
                result.Warnings.Add($"Miss rate {missRate:P0} exceeds 40%");
                result.IsValid = false;
            }
            else
                result.PassedChecks++;

            return result;
        }

        private static BalanceValidator.ValidationResult ValidateDialAgency(TuningSimulationOutcome simulation)
        {
            var result = new BalanceValidator.ValidationResult { TotalChecks = 1 };
            var f = simulation.Fundamentals;
            if (f == null)
            {
                result.Warnings.Add("dial_agency requires fundamentals simulation data");
                return result;
            }

            result.TotalChecks = 1;
            if (f.ContinuePastZeroHp && f.AverageLossSeverity > 100)
            {
                result.Warnings.Add($"High loss severity {f.AverageLossSeverity:F0} despite decent win rate");
                result.IsValid = false;
            }
            else
                result.PassedChecks++;

            return result;
        }

        private static double ScoreInBand(double value, double min, double max)
        {
            if (value >= min && value <= max)
                return 100;
            if (value < min)
                return Math.Max(0, 100 - (min - value) * 10);
            return Math.Max(0, 100 - (value - max) * 10);
        }
    }
}
