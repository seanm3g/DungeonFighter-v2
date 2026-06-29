using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tuning.Suggesters
{
    /// <summary>
    /// Routes broad progression knobs (tempo, shape, parity) and curve inputs using decade anchor snapshots.
    /// </summary>
    public static class FundamentalsDurationAdjustmentSuggester
    {
        public sealed class FundamentalsProgressionDiagnosis
        {
            public int? WorstAnchorLevel { get; init; }
            public string PrimaryAxis { get; init; } = "None";
            public string? SuggestedParameter { get; init; }
            public string? SuggestionSummary { get; init; }
        }

        public const double HealthToActionsExponent = 0.5;
        public const double MaxHealthRatioPerStep = 3.0;
        public const double MinHealthRatioPerStep = 0.5;
        public const double WinRateHealthMaxRatioPerStep = 1.25;
        public const double WinRateHealthMinRatioPerStep = 0.80;
        public const double L1TempoMaxRatioPerStep = 1.35;
        public const double L1TempoMinRatioPerStep = 0.75;
        public const double L1DamageMaxRatioPerStep = 1.25;
        public const double L1DamageMinRatioPerStep = 0.80;
        public const double HighWinRateThreshold = 0.75;

        public static List<TuningSuggestion> Suggest(
            FundamentalsSimulationResult fundamentals,
            FundamentalsAnalysisTargets? targets = null)
        {
            targets ??= new FundamentalsAnalysisTargets();
            double targetCombined = ResolveTargetCombinedActions(targets);
            double tolerance = targets.TempoTolerance > 0 ? targets.TempoTolerance : 1.5;
            var snapshots = ResolveSnapshots(fundamentals);
            if (snapshots.Count == 0)
                return new List<TuningSuggestion>();

            var l1 = snapshots.FirstOrDefault(s => s.Level == 1);

            if (targets.RequireL1AnchorBeforeScaling && l1 != null)
            {
                if (!IsL1AnchorInBand(l1, targets, targetCombined, tolerance))
                {
                    return new List<TuningSuggestion>
                    {
                        BuildL1ExclusiveSuggestion(l1, fundamentals, targets, targetCombined, tolerance)
                    };
                }

                if (NeedsPlayerBaseHealthTune(l1, targetCombined, tolerance))
                {
                    return new List<TuningSuggestion>
                    {
                        BuildPlayerBaseHealthSuggestion(l1, fundamentals, targetCombined, tolerance)
                    };
                }

                return new List<TuningSuggestion>();
            }

            if (l1 != null && l1.Level <= 1 && !LevelWinRateCurve.IsWinRateInBand(l1.WinRate, l1.Level))
            {
                return new List<TuningSuggestion>
                {
                    BuildPlayerBaseHealthForWinRate(
                        l1, fundamentals, increase: LevelWinRateCurve.GetWinRateDelta(l1.WinRate, l1.Level) < 0)
                };
            }

            if (l1 != null && NeedsPlayerBaseHealthTune(l1, targetCombined, tolerance))
            {
                return new List<TuningSuggestion>
                {
                    BuildPlayerBaseHealthSuggestion(l1, fundamentals, targetCombined, tolerance)
                };
            }

            if (TryBuildGlobalTempoSuggestion(snapshots, fundamentals, targets, targetCombined, tolerance, out var tempo))
                return new List<TuningSuggestion> { tempo };

            var worst = FindWorstAnchor(snapshots, targetCombined, tolerance, targets);
            if (worst == null)
                return new List<TuningSuggestion>();

            if (TryBuildAxisSuggestion(worst, fundamentals, targetCombined, tolerance, targets, out var axis))
                return new List<TuningSuggestion> { axis };

            return new List<TuningSuggestion>();
        }

        /// <summary>Surfaces worst decade anchor and broad knob axis for UI (workbench, reports).</summary>
        public static FundamentalsProgressionDiagnosis Diagnose(
            FundamentalsSimulationResult fundamentals,
            FundamentalsAnalysisTargets? targets = null)
        {
            targets ??= new FundamentalsAnalysisTargets();
            double targetCombined = ResolveTargetCombinedActions(targets);
            double tolerance = targets.TempoTolerance > 0 ? targets.TempoTolerance : 1.5;
            var snapshots = ResolveSnapshots(fundamentals);
            if (snapshots.Count == 0)
                return new FundamentalsProgressionDiagnosis();

            var suggestions = Suggest(fundamentals, targets);
            var top = suggestions.Count > 0 ? suggestions[0] : null;
            var worst = FindWorstAnchor(snapshots, targetCombined, tolerance, targets);

            return new FundamentalsProgressionDiagnosis
            {
                WorstAnchorLevel = worst?.Level,
                PrimaryAxis = MapParameterToAxis(top?.Parameter),
                SuggestedParameter = top?.Parameter,
                SuggestionSummary = top != null
                    ? $"{top.Parameter}: {top.CurrentValue:F2} → {top.SuggestedValue:F2}"
                    : null
            };
        }

        internal static string MapParameterToAxis(string? parameter) => parameter switch
        {
            "CombatTempoScale" => "Tempo",
            "ProgressionShape" => "Shape",
            "PlayerEnemyParity" => "Parity",
            "HealthPerLevel" => "Parity",
            null or "" => "None",
            _ => parameter
        };

        private static IReadOnlyList<FundamentalsLevelSnapshot> ResolveSnapshots(FundamentalsSimulationResult fundamentals)
        {
            if (fundamentals.LevelSnapshots.Count > 0)
                return fundamentals.LevelSnapshots;

            return new[]
            {
                new FundamentalsLevelSnapshot
                {
                    Level = fundamentals.PlayerLevel,
                    MedianCombinedActions = fundamentals.MedianActionsPerEncounter,
                    MedianPlayerTurns = fundamentals.MedianPlayerTurnsPerEncounter,
                    MedianEnemyTurns = fundamentals.MedianEnemyTurnsPerEncounter,
                    WinRate = fundamentals.WinRate
                }
            };
        }

        private static double GetMedianPlayerTurns(
            FundamentalsLevelSnapshot snap,
            FundamentalsSimulationResult? fundamentals = null)
        {
            if (snap.MedianPlayerTurns > 0)
                return snap.MedianPlayerTurns;
            if (fundamentals != null && fundamentals.MedianPlayerTurnsPerEncounter > 0)
                return fundamentals.MedianPlayerTurnsPerEncounter;
            return snap.MedianCombinedActions / 2.0;
        }

        private static bool IsL1AnchorInBand(
            FundamentalsLevelSnapshot l1,
            FundamentalsAnalysisTargets targets,
            double targetCombined,
            double tolerance)
        {
            double targetPlayer = ResolveTargetPlayerTurns(targets);
            bool tempoOk = targets.TargetMedianPlayerTurns > 0
                ? Math.Abs(GetMedianPlayerTurns(l1) - targetPlayer) <= tolerance
                : Math.Abs(l1.MedianCombinedActions - targetCombined) <= tolerance;
            bool wrOk = LevelWinRateCurve.IsWinRateInBand(l1.WinRate, l1.Level);
            return tempoOk && wrOk;
        }

        private static TuningSuggestion BuildL1ExclusiveSuggestion(
            FundamentalsLevelSnapshot l1,
            FundamentalsSimulationResult fundamentals,
            FundamentalsAnalysisTargets targets,
            double targetCombined,
            double tolerance)
        {
            _ = targetCombined;
            double targetPlayer = ResolveTargetPlayerTurns(targets);
            double currentPlayer = GetMedianPlayerTurns(l1, fundamentals);
            bool tooShort = currentPlayer < targetPlayer - tolerance;
            bool tooLong = currentPlayer > targetPlayer + tolerance;

            if (!LevelWinRateCurve.IsWinRateInBand(l1.WinRate, l1.Level))
            {
                return BuildPlayerBaseHealthForWinRate(
                    l1, fundamentals, increase: LevelWinRateCurve.GetWinRateDelta(l1.WinRate, l1.Level) < 0);
            }

            if (tooShort)
            {
                return BuildL1EnemyTempoSuggestion(
                    l1, fundamentals, targetPlayer, tolerance, increase: true);
            }

            if (tooLong)
            {
                return BuildL1DamageEquationSuggestion(
                    l1, fundamentals, targetPlayer, tolerance);
            }

            return BuildL1EnemyTempoSuggestion(
                l1, fundamentals, targetPlayer, tolerance, increase: true);
        }

        private static bool NeedsPlayerBaseHealthTune(
            FundamentalsLevelSnapshot l1,
            double targetCombined,
            double tolerance)
        {
            bool tooShort = l1.MedianCombinedActions < targetCombined - tolerance;
            bool tooLong = l1.MedianCombinedActions > targetCombined + tolerance;
            if (!tooShort && !tooLong)
                return false;

            if (tooShort)
                return l1.WinRate < HighWinRateThreshold;
            return l1.WinRate >= HighWinRateThreshold;
        }

        private static bool TryBuildGlobalTempoSuggestion(
            IReadOnlyList<FundamentalsLevelSnapshot> snapshots,
            FundamentalsSimulationResult fundamentals,
            FundamentalsAnalysisTargets targets,
            double targetCombined,
            double tolerance,
            out TuningSuggestion suggestion)
        {
            suggestion = null!;

            var l1 = snapshots.FirstOrDefault(s => s.Level == 1);
            if (targets.RequireL1AnchorBeforeScaling && l1 != null
                && !IsL1AnchorInBand(l1, targets, targetCombined, tolerance))
                return false;

            int tooShort = snapshots.Count(s =>
                s.MedianCombinedActions < targetCombined - tolerance && s.WinRate >= HighWinRateThreshold);
            int tooLong = snapshots.Count(s =>
                s.MedianCombinedActions > targetCombined + tolerance && s.WinRate >= HighWinRateThreshold);

            if (tooShort < snapshots.Count * 0.6 && tooLong < snapshots.Count * 0.6)
                return false;

            bool increase = tooShort >= tooLong;
            double medianCombined = snapshots.Average(s => s.MedianCombinedActions);
            var prog = GameConfiguration.Instance.EnemySystem.ProgressionScales;
            prog.EnsurePositiveScales();
            double ratio = ComputeHealthRatio(medianCombined, targetCombined, increase);
            double current = prog.CombatTempoScale;
            double suggested = Math.Clamp(current * ratio, 0.25, 3.0);
            if (Math.Abs(suggested - current) < 0.001)
                return false;

            suggestion = new TuningSuggestion
            {
                Id = increase ? "fundamentals_increase_combat_tempo" : "fundamentals_decrease_combat_tempo",
                Priority = BalanceTuningGoals.TuningPriority.High,
                Category = "enemy_progression",
                Target = "Global fight tempo",
                Parameter = "CombatTempoScale",
                CurrentValue = current,
                SuggestedValue = suggested,
                AdjustmentMagnitude = Math.Abs(ratio - 1.0) * 100.0,
                Reason =
                    $"Most anchors off tempo (median {medianCombined:F1} vs target {targetCombined:F0}); uniform enemy HP stretch",
                Impact = $"{(increase ? "Increase" : "Reduce")} combat tempo scale {current:F2} → {suggested:F2}",
                AffectedMatchups = new List<string> { fundamentals.WeaponType + " fundamentals sweep" }
            };
            return true;
        }

        private static FundamentalsLevelSnapshot? FindWorstAnchor(
            IReadOnlyList<FundamentalsLevelSnapshot> snapshots,
            double targetCombined,
            double tolerance,
            FundamentalsAnalysisTargets targets)
        {
            _ = targets;

            return snapshots
                .Select(s => new
                {
                    Snap = s,
                    Score = Math.Abs(s.MedianCombinedActions - targetCombined)
                            + Math.Abs(LevelWinRateCurve.GetWinRateDelta(s.WinRate, s.Level)) * 2
                })
                .Where(x => Math.Abs(x.Snap.MedianCombinedActions - targetCombined) > tolerance
                            || !LevelWinRateCurve.IsWinRateInBand(x.Snap.WinRate, x.Snap.Level))
                .OrderByDescending(x => x.Score)
                .Select(x => x.Snap)
                .FirstOrDefault();
        }

        private static bool TryBuildAxisSuggestion(
            FundamentalsLevelSnapshot snap,
            FundamentalsSimulationResult fundamentals,
            double targetCombined,
            double tolerance,
            FundamentalsAnalysisTargets targets,
            out TuningSuggestion suggestion)
        {
            suggestion = null!;
            if (targets.RequireL1AnchorBeforeScaling && snap.Level > 1)
            {
                var l1 = fundamentals.LevelSnapshots.FirstOrDefault(s => s.Level == 1);
                if (l1 != null && !IsL1AnchorInBand(l1, targets, targetCombined, tolerance))
                    return false;
            }

            bool tooShort = snap.MedianCombinedActions < targetCombined - tolerance;
            bool tooLong = snap.MedianCombinedActions > targetCombined + tolerance;
            if (!tooShort && !tooLong)
                return false;

            if (tooShort && snap.WinRate < HighWinRateThreshold)
            {
                if (snap.Level <= 1)
                {
                    suggestion = BuildPlayerBaseHealthForWinRate(
                        snap, fundamentals, increase: LevelWinRateCurve.GetWinRateDelta(snap.WinRate, snap.Level) < 0);
                    return true;
                }

                suggestion = snap.Level < 50
                    ? BuildPlayerHealthPerLevelSuggestion(snap, fundamentals, targetCombined, tolerance, increase: true)
                    : BuildParitySuggestion(snap, fundamentals, targetCombined, tolerance, increase: true);
                return true;
            }

            if (tooShort && snap.WinRate >= HighWinRateThreshold)
            {
                if (snap.Level <= 40)
                {
                    if (snap.Level <= 1)
                        suggestion = BuildEnemyProgressionSuggestion(snap, fundamentals, targetCombined, tolerance, "BaseHealthScale", increase: true);
                    else if (snap.Level <= 30)
                        suggestion = BuildShapeSuggestion(snap, fundamentals, targetCombined, tolerance, towardEarly: true);
                    else
                        suggestion = BuildEnemyProgressionSuggestion(snap, fundamentals, targetCombined, tolerance, "HealthGrowthScale", increase: true);
                }
                else
                {
                    suggestion = BuildEnemyProgressionSuggestion(snap, fundamentals, targetCombined, tolerance, "HealthGrowthScale", increase: true);
                }

                return true;
            }

            if (tooLong && snap.WinRate < HighWinRateThreshold)
            {
                suggestion = BuildEnemyProgressionSuggestion(snap, fundamentals, targetCombined, tolerance, "CombatTempoScale", increase: false);
                return true;
            }

            if (tooLong && snap.WinRate >= HighWinRateThreshold)
            {
                suggestion = snap.Level <= 40
                    ? BuildEnemyProgressionSuggestion(snap, fundamentals, targetCombined, tolerance, "BaseHealthScale", increase: false)
                    : BuildEnemyProgressionSuggestion(snap, fundamentals, targetCombined, tolerance, "HealthGrowthScale", increase: false);
                return true;
            }

            return false;
        }

        private static TuningSuggestion BuildPlayerBaseHealthForWinRate(
            FundamentalsLevelSnapshot snap,
            FundamentalsSimulationResult fundamentals,
            bool increase)
        {
            double targetWr = LevelWinRateCurve.GetTargetWinRate(snap.Level);
            double tolerance = LevelWinRateCurve.GetTolerance(snap.Level);
            double currentWr = Math.Max(0.01, LevelWinRateCurve.NormalizeToPercent(snap.WinRate));

            double wrRatio = increase
                ? Math.Max(1.0, targetWr / currentWr)
                : Math.Min(1.0, targetWr / currentWr);
            if (Math.Abs(wrRatio - 1.0) < 0.001)
                wrRatio = increase ? 1.05 : 0.95;

            double healthRatio = Math.Pow(wrRatio, 0.35);
            healthRatio = Math.Clamp(healthRatio, WinRateHealthMinRatioPerStep, WinRateHealthMaxRatioPerStep);

            int currentBase = GameConfiguration.Instance.Character.PlayerBaseHealth;
            int suggestedBase = Math.Max(1, (int)Math.Round(currentBase * healthRatio));

            return new TuningSuggestion
            {
                Id = increase ? "fundamentals_l1_increase_player_base_health" : "fundamentals_l1_decrease_player_base_health",
                Priority = BalanceTuningGoals.TuningPriority.High,
                Category = "player",
                Target = $"Player (L{snap.Level} base)",
                Parameter = "BaseHealth",
                CurrentValue = currentBase,
                SuggestedValue = suggestedBase,
                AdjustmentMagnitude = Math.Abs(healthRatio - 1.0) * 100.0,
                Reason =
                    $"L{snap.Level} WR {currentWr:F0}% vs level curve target {targetWr:F0}%±{tolerance:F0} — tune hero base HP",
                Impact = $"{(increase ? "Increase" : "Reduce")} player base health {currentBase} → {suggestedBase}",
                AffectedMatchups = BuildMatchupLabel(fundamentals, snap.Level)
            };
        }

        private static TuningSuggestion BuildPlayerBaseHealthSuggestion(
            FundamentalsLevelSnapshot snap,
            FundamentalsSimulationResult fundamentals,
            double targetCombined,
            double tolerance,
            bool forceIncrease = false)
        {
            bool tooShort = snap.MedianCombinedActions < targetCombined - tolerance;
            bool tooLong = snap.MedianCombinedActions > targetCombined + tolerance;
            bool increase = forceIncrease
                            || tooShort
                            || (tooLong && snap.WinRate < HighWinRateThreshold);
            if (!forceIncrease && tooLong && snap.WinRate >= HighWinRateThreshold)
                increase = false;

            double healthRatio = ComputeHealthRatio(snap.MedianCombinedActions, targetCombined, increase);
            int currentBase = GameConfiguration.Instance.Character.PlayerBaseHealth;
            int suggestedBase = Math.Max(1, (int)Math.Round(currentBase * healthRatio));

            return new TuningSuggestion
            {
                Id = increase ? "fundamentals_l1_increase_player_base_health" : "fundamentals_l1_decrease_player_base_health",
                Priority = PriorityForGap(targetCombined, snap.MedianCombinedActions, tolerance),
                Category = "player",
                Target = "Player (L1 base)",
                Parameter = "BaseHealth",
                CurrentValue = currentBase,
                SuggestedValue = suggestedBase,
                AdjustmentMagnitude = Math.Abs(healthRatio - 1.0) * 100.0,
                Reason = $"L{snap.Level} {snap.MedianCombinedActions:F1} combined vs target {targetCombined:F0}; WR {snap.WinRate * 100:F0}%",
                Impact = $"{(increase ? "Increase" : "Reduce")} player base health {currentBase} → {suggestedBase}",
                AffectedMatchups = BuildMatchupLabel(fundamentals, snap.Level)
            };
        }

        private static TuningSuggestion BuildPlayerHealthPerLevelSuggestion(
            FundamentalsLevelSnapshot snap,
            FundamentalsSimulationResult fundamentals,
            double targetCombined,
            double tolerance,
            bool increase)
        {
            double healthRatio = ComputeHealthRatio(snap.MedianCombinedActions, targetCombined, increase);
            int current = GameConfiguration.Instance.Character.HealthPerLevel;
            int suggested = Math.Max(1, (int)Math.Round(current * healthRatio));

            return new TuningSuggestion
            {
                Id = increase ? "fundamentals_increase_player_hp_per_level" : "fundamentals_decrease_player_hp_per_level",
                Priority = PriorityForGap(targetCombined, snap.MedianCombinedActions, tolerance),
                Category = "player",
                Target = $"Player scaling @ L{snap.Level}",
                Parameter = "HealthPerLevel",
                CurrentValue = current,
                SuggestedValue = suggested,
                AdjustmentMagnitude = Math.Abs(healthRatio - 1.0) * 100.0,
                Reason = $"L{snap.Level} short fights, low WR {snap.WinRate * 100:F0}% — buff hero midgame HP",
                Impact = $"{(increase ? "Increase" : "Reduce")} health per level {current} → {suggested}",
                AffectedMatchups = BuildMatchupLabel(fundamentals, snap.Level)
            };
        }

        private static TuningSuggestion BuildParitySuggestion(
            FundamentalsLevelSnapshot snap,
            FundamentalsSimulationResult fundamentals,
            double targetCombined,
            double tolerance,
            bool increase)
        {
            var prog = GameConfiguration.Instance.EnemySystem.ProgressionScales;
            prog.EnsurePositiveScales();
            double step = 0.1;
            double current = prog.PlayerEnemyParity;
            double suggested = Math.Clamp(current + (increase ? step : -step), -1, 1);

            return new TuningSuggestion
            {
                Id = increase ? "fundamentals_increase_parity" : "fundamentals_decrease_parity",
                Priority = PriorityForGap(targetCombined, snap.MedianCombinedActions, tolerance),
                Category = "enemy_progression",
                Target = "Player/enemy parity",
                Parameter = "PlayerEnemyParity",
                CurrentValue = current,
                SuggestedValue = suggested,
                AdjustmentMagnitude = step * 100,
                Reason = $"L{snap.Level} low WR {snap.WinRate * 100:F0}% with short tempo — shift parity toward hero",
                Impact = $"Player/enemy parity {current:F2} → {suggested:F2}",
                AffectedMatchups = BuildMatchupLabel(fundamentals, snap.Level)
            };
        }

        private static TuningSuggestion BuildShapeSuggestion(
            FundamentalsLevelSnapshot snap,
            FundamentalsSimulationResult fundamentals,
            double targetCombined,
            double tolerance,
            bool towardEarly)
        {
            var prog = GameConfiguration.Instance.EnemySystem.ProgressionScales;
            prog.EnsurePositiveScales();
            double step = 0.08;
            double current = prog.ProgressionShape;
            double suggested = Math.Clamp(current + (towardEarly ? -step : step), 0, 1);

            return new TuningSuggestion
            {
                Id = towardEarly ? "fundamentals_shape_earlier" : "fundamentals_shape_later",
                Priority = PriorityForGap(targetCombined, snap.MedianCombinedActions, tolerance),
                Category = "enemy_progression",
                Target = $"Progression shape @ L{snap.Level}",
                Parameter = "ProgressionShape",
                CurrentValue = current,
                SuggestedValue = suggested,
                AdjustmentMagnitude = step * 100,
                Reason = $"L{snap.Level} {snap.MedianCombinedActions:F1} combined vs {targetCombined:F0}; WR {snap.WinRate * 100:F0}%",
                Impact = towardEarly
                    ? "Shift shape toward early/base-dominated enemy HP"
                    : "Shift shape toward late/growth-dominated enemy HP",
                AffectedMatchups = BuildMatchupLabel(fundamentals, snap.Level)
            };
        }

        private static TuningSuggestion BuildL1EnemyTempoSuggestion(
            FundamentalsLevelSnapshot snap,
            FundamentalsSimulationResult fundamentals,
            double targetPlayerTurns,
            double tolerance,
            bool increase)
        {
            double currentPlayer = GetMedianPlayerTurns(snap, fundamentals);
            double healthRatio = ComputeHealthRatio(currentPlayer, targetPlayerTurns, increase);
            healthRatio = Math.Clamp(healthRatio, L1TempoMinRatioPerStep, L1TempoMaxRatioPerStep);
            var prog = GameConfiguration.Instance.EnemySystem.ProgressionScales;
            prog.EnsurePositiveScales();
            double current = prog.BaseHealthScale;
            double suggested = Math.Max(0.1, current * healthRatio);

            return new TuningSuggestion
            {
                Id = $"basehealthscale_fundamentals_l{snap.Level}_{(increase ? "up" : "down")}",
                Priority = PriorityForGap(targetPlayerTurns, currentPlayer, tolerance),
                Category = "enemy_progression",
                Target = $"Enemy L1 tempo (base scale)",
                Parameter = "BaseHealthScale",
                CurrentValue = current,
                SuggestedValue = suggested,
                AdjustmentMagnitude = Math.Abs(healthRatio - 1.0) * 100.0,
                Reason =
                    $"L{snap.Level} {currentPlayer:F1} hero turns vs target {targetPlayerTurns:F0} — tune fight length via enemy base scale",
                Impact = $"{(increase ? "Increase" : "Reduce")} BaseHealthScale ×{healthRatio:F2}",
                AffectedMatchups = BuildMatchupLabel(fundamentals, snap.Level)
            };
        }

        private static TuningSuggestion BuildL1DamageEquationSuggestion(
            FundamentalsLevelSnapshot snap,
            FundamentalsSimulationResult fundamentals,
            double targetPlayerTurns,
            double tolerance)
        {
            double currentPlayer = GetMedianPlayerTurns(snap, fundamentals);
            double rawRatio = currentPlayer / Math.Max(1.0, targetPlayerTurns);
            double damageRatio = Math.Pow(Math.Max(1.0, rawRatio), 0.5);
            damageRatio = Math.Clamp(damageRatio, L1DamageMinRatioPerStep, L1DamageMaxRatioPerStep);
            if (Math.Abs(damageRatio - 1.0) < 0.001)
                damageRatio = 1.05;

            var roll = GameConfiguration.Instance.CombatBalance.RollDamageMultipliers;
            double current = roll.BasicRollDamageMultiplier;
            double suggested = Math.Clamp(current * damageRatio, 0.5, 2.0);

            return new TuningSuggestion
            {
                Id = $"basicrolldamagemult_fundamentals_l{snap.Level}_up",
                Priority = PriorityForGap(targetPlayerTurns, currentPlayer, tolerance),
                Category = "roll_feel",
                Target = "L1 damage equation",
                Parameter = "basicRollDamageMult",
                CurrentValue = current,
                SuggestedValue = suggested,
                AdjustmentMagnitude = Math.Abs(damageRatio - 1.0) * 100.0,
                Reason =
                    $"L{snap.Level} {currentPlayer:F1} hero turns vs target {targetPlayerTurns:F0} — shorten fights via roll damage multiplier",
                Impact = $"Increase basic roll damage multiplier {current:F3} → {suggested:F3}",
                AffectedMatchups = BuildMatchupLabel(fundamentals, snap.Level)
            };
        }

        private static TuningSuggestion BuildEnemyProgressionSuggestion(
            FundamentalsLevelSnapshot snap,
            FundamentalsSimulationResult fundamentals,
            double targetCombined,
            double tolerance,
            string param,
            bool increase)
        {
            double healthRatio = ComputeHealthRatio(snap.MedianCombinedActions, targetCombined, increase);
            var prog = GameConfiguration.Instance.EnemySystem.ProgressionScales;
            prog.EnsurePositiveScales();

            double current = param switch
            {
                "BaseHealthScale" => prog.BaseHealthScale,
                "HealthGrowthScale" => prog.HealthGrowthScale,
                "CombatTempoScale" => prog.CombatTempoScale,
                _ => 1.0
            };
            double suggested = Math.Max(0.1, current * healthRatio);

            return new TuningSuggestion
            {
                Id = $"{param.ToLowerInvariant()}_fundamentals_l{snap.Level}_{(increase ? "up" : "down")}",
                Priority = PriorityForGap(targetCombined, snap.MedianCombinedActions, tolerance),
                Category = "enemy_progression",
                Target = $"Enemy scaling @ L{snap.Level}",
                Parameter = param,
                CurrentValue = current,
                SuggestedValue = suggested,
                AdjustmentMagnitude = Math.Abs(healthRatio - 1.0) * 100.0,
                Reason = $"L{snap.Level} {snap.MedianCombinedActions:F1} combined vs target {targetCombined:F0}; WR {snap.WinRate * 100:F0}%",
                Impact = $"{(increase ? "Increase" : "Reduce")} {param} ×{healthRatio:F2}",
                AffectedMatchups = BuildMatchupLabel(fundamentals, snap.Level)
            };
        }

        private static double ComputeHealthRatio(double currentCombined, double targetCombined, bool increase)
        {
            double rawRatio = targetCombined / Math.Max(1.0, currentCombined);
            double actionRatio = increase
                ? Math.Max(1.0, rawRatio)
                : Math.Min(1.0, rawRatio);
            if (Math.Abs(actionRatio - 1.0) < 0.001)
                actionRatio = increase ? 1.05 : 0.95;

            double healthRatio = Math.Pow(actionRatio, 1.0 / HealthToActionsExponent);
            return Math.Clamp(healthRatio, MinHealthRatioPerStep, MaxHealthRatioPerStep);
        }

        private static BalanceTuningGoals.TuningPriority PriorityForGap(
            double targetCombined,
            double currentCombined,
            double tolerance) =>
            Math.Abs(currentCombined - targetCombined) > tolerance * 4
                ? BalanceTuningGoals.TuningPriority.Critical
                : BalanceTuningGoals.TuningPriority.High;

        private static List<string> BuildMatchupLabel(FundamentalsSimulationResult fundamentals, int level) =>
            new() { $"{fundamentals.WeaponType} L{level} vs {fundamentals.EnemyType ?? "enemy"} L{level}" };

        internal static double ResolveTargetCombinedActions(FundamentalsAnalysisTargets targets)
        {
            if (targets.TargetMedianCombinedActions > 0)
                return targets.TargetMedianCombinedActions;
            if (targets.MinAverageActions > 0 && targets.MaxAverageActions > 0)
                return 0.5 * (targets.MinAverageActions + targets.MaxAverageActions);
            return 24.0;
        }

        internal static double ResolveTargetPlayerTurns(FundamentalsAnalysisTargets targets)
        {
            if (targets.TargetMedianPlayerTurns > 0)
                return targets.TargetMedianPlayerTurns;
            return ResolveTargetCombinedActions(targets) / 2.0;
        }
    }
}
