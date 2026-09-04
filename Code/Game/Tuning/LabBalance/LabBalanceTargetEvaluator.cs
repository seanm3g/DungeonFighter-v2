using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RPGGame.ActionInteractionLab;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tuning.LabBalance
{
    public sealed class LabBalanceTargetCheck
    {
        public string Name { get; init; } = "";
        public string ActualText { get; init; } = "";
        public string TargetText { get; init; } = "";
        public bool Passed { get; init; }
    }

    public sealed class LabBalanceTargetEvaluation
    {
        public IReadOnlyList<LabBalanceTargetCheck> Checks { get; init; } = Array.Empty<LabBalanceTargetCheck>();
        public bool AllPassed => Checks.Count > 0 && Checks.All(c => c.Passed);
        public int PassedCount => Checks.Count(c => c.Passed);

        public string FormatSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Targets: {PassedCount}/{Checks.Count} passed");
            foreach (var c in Checks)
                sb.AppendLine($"  {(c.Passed ? "PASS" : "FAIL")} {c.Name}: {c.ActualText} (want {c.TargetText})");
            return sb.ToString().TrimEnd();
        }
    }

    public sealed class LabBalanceReportDelta
    {
        public double WinRateDelta { get; init; }
        public double MedianTurnsDelta { get; init; }
        public double MedianPlayerTurnsDelta { get; init; }
        public double AverageMaxComboStreakDelta { get; init; }
        public double AverageHpRemainingOnWinDelta { get; init; }

        public string FormatSummary()
        {
            return
                $"Δ vs previous — WR {WinRateDelta * 100:+0.0;-0.0;0}% | " +
                $"median turns {MedianTurnsDelta:+0.0;-0.0;0} | " +
                $"hero turns {MedianPlayerTurnsDelta:+0.0;-0.0;0} | " +
                $"max combo streak {AverageMaxComboStreakDelta:+0.00;-0.00;0} | " +
                $"HP left (win) {AverageHpRemainingOnWinDelta:+0.0;-0.0;0}";
        }
    }

    /// <summary>Compares encounter / dungeon / matrix runs to per-layer goals.</summary>
    public static class LabBalanceTargetEvaluator
    {
        public static string FormatRecipeForLayer(LabBalanceLayerDefinition def)
        {
            if (def.StatsMode == LabBalanceStatsMode.DeepLinkOnly)
            {
                return $"Goal: {def.GoalIntent} — open Workbench profile '{def.WorkbenchProfileId}'.";
            }

            var parts = new List<string> { $"Goal: {def.GoalIntent}", "OK = every checklist line PASS after Run Stats" };

            if (def.HasFlag(LabBalanceCheckFlags.Duration))
            {
                var t = def.Targets;
                double tol = t.TempoTolerance > 0 ? t.TempoTolerance : 1.5;
                parts.Add(
                    $"tempo median combined {t.TargetMedianCombinedActions:F0} ± {tol:F1}, " +
                    $"hero/enemy {t.TargetMedianPlayerTurns:F0}/{t.TargetMedianEnemyTurns:F0} ± {tol:F1}, " +
                    $"mean {t.MinAverageActions:F0}–{t.MaxAverageActions:F0}");
            }

            if (def.HasFlag(LabBalanceCheckFlags.Combo))
            {
                parts.Add(
                    $"combo runs(2+) ≥ {def.Targets.MinAverageComboStreakRuns2Plus:F1}, " +
                    $"max streak ≥ {def.Targets.MinAverageMaxComboStreak:F1}");
            }

            if (def.HasFlag(LabBalanceCheckFlags.FeelVariance))
                parts.Add($"turn std-dev ≤ {def.MaxTurnStdDev:F0}");

            if (def.HasFlag(LabBalanceCheckFlags.WinRateFixed))
                parts.Add($"win rate {def.WinRateMin * 100:F0}–{def.WinRateMax * 100:F0}%");

            if (def.HasFlag(LabBalanceCheckFlags.WinRateCurve))
                parts.Add("win rate on LevelWinRateCurve (± tolerance) at scenario/anchor level");

            if (def.HasFlag(LabBalanceCheckFlags.WeaponSpread))
                parts.Add($"weapon WR spread ≤ {def.MaxWeaponWinRateSpread * 100:F0}pp");

            if (def.HasFlag(LabBalanceCheckFlags.AllAnchors))
                parts.Add("checked on every level anchor");

            if (def.HasFlag(LabBalanceCheckFlags.DungeonClear))
                parts.Add($"clear rate {def.MinDungeonClearRate * 100:F0}–{def.MaxDungeonClearRate * 100:F0}%");

            return string.Join(" · ", parts);
        }

        /// <summary>Single-encounter evaluation using the layer's check flags.</summary>
        public static LabBalanceTargetEvaluation EvaluateForLayer(
            LabBalanceLayerDefinition def,
            ActionLabEncounterSimulationReport report,
            int? levelOverride = null)
        {
            int level = levelOverride
                        ?? Math.Clamp(def.DefaultPlayerLevel, 1, 99);
            var checks = new List<LabBalanceTargetCheck>();

            if (def.HasFlag(LabBalanceCheckFlags.Duration))
                AddDurationChecks(checks, report, DurationTargetsForLevel(def, level));

            if (def.HasFlag(LabBalanceCheckFlags.Combo))
                AddComboChecks(checks, report, def.Targets);

            if (def.HasFlag(LabBalanceCheckFlags.FeelVariance))
            {
                checks.Add(new LabBalanceTargetCheck
                {
                    Name = "Turn std-dev",
                    ActualText = $"{report.StdDevTurns:F1}",
                    TargetText = $"≤ {def.MaxTurnStdDev:F0}",
                    Passed = report.StdDevTurns <= def.MaxTurnStdDev
                });
            }

            if (def.HasFlag(LabBalanceCheckFlags.WinRateFixed))
            {
                checks.Add(new LabBalanceTargetCheck
                {
                    Name = "Win rate",
                    ActualText = $"{report.WinRate * 100:F1}%",
                    TargetText = $"{def.WinRateMin * 100:F0}–{def.WinRateMax * 100:F0}%",
                    Passed = report.WinRate >= def.WinRateMin && report.WinRate <= def.WinRateMax
                });
            }

            if (def.HasFlag(LabBalanceCheckFlags.WinRateCurve)
                && !def.HasFlag(LabBalanceCheckFlags.AllAnchors)
                && !def.HasFlag(LabBalanceCheckFlags.WeaponSpread))
            {
                AddWinRateCurveCheck(checks, report.WinRate, level);
            }

            return new LabBalanceTargetEvaluation { Checks = checks };
        }

        public static LabBalanceTargetEvaluation EvaluateAnchors(
            LabBalanceLayerDefinition def,
            IReadOnlyList<FundamentalsLevelSnapshot> anchors)
        {
            var checks = new List<LabBalanceTargetCheck>();
            if (anchors.Count == 0)
                return new LabBalanceTargetEvaluation { Checks = checks };

            foreach (var snap in anchors)
            {
                int level = snap.Level;
                var duration = DurationTargetsForLevel(def, level);

                if (def.HasFlag(LabBalanceCheckFlags.Duration))
                {
                    double tol = duration.TempoTolerance;
                    double combined = snap.MedianCombinedActions;
                    checks.Add(MakeBand(
                        $"L{level} median combined",
                        combined,
                        duration.TargetMedianCombinedActions,
                        tol,
                        "F1"));
                }

                if (def.HasFlag(LabBalanceCheckFlags.WinRateCurve))
                    AddWinRateCurveCheck(checks, snap.WinRate, level, namePrefix: $"L{level} ");
            }

            return new LabBalanceTargetEvaluation { Checks = checks };
        }

        public static LabBalanceTargetEvaluation EvaluateWeaponMatrix(
            LabBalanceLayerDefinition def,
            IReadOnlyList<(string Weapon, double WinRate, double MedianTurns)> rows)
        {
            var checks = new List<LabBalanceTargetCheck>();
            int level = Math.Clamp(def.DefaultPlayerLevel, 1, 99);
            var duration = DurationTargetsForLevel(def, level);

            if (rows.Count == 0)
                return new LabBalanceTargetEvaluation { Checks = checks };

            if (def.HasFlag(LabBalanceCheckFlags.WeaponSpread))
            {
                double spread = rows.Max(r => r.WinRate) - rows.Min(r => r.WinRate);
                checks.Add(new LabBalanceTargetCheck
                {
                    Name = "Weapon WR spread",
                    ActualText = $"{spread * 100:F1}pp",
                    TargetText = $"≤ {def.MaxWeaponWinRateSpread * 100:F0}pp",
                    Passed = spread <= def.MaxWeaponWinRateSpread + 1e-9
                });
            }

            foreach (var row in rows)
            {
                if (def.HasFlag(LabBalanceCheckFlags.WinRateCurve))
                    AddWinRateCurveCheck(checks, row.WinRate, level, namePrefix: $"{row.Weapon} ");

                if (def.HasFlag(LabBalanceCheckFlags.Duration))
                {
                    checks.Add(MakeBand(
                        $"{row.Weapon} median combined",
                        row.MedianTurns,
                        duration.TargetMedianCombinedActions,
                        duration.TempoTolerance,
                        "F1"));
                }
            }

            return new LabBalanceTargetEvaluation { Checks = checks };
        }

        public static LabBalanceTargetEvaluation EvaluateDungeon(
            LabBalanceLayerDefinition def,
            ActionLabDungeonSimulationReport report)
        {
            var checks = new List<LabBalanceTargetCheck>();
            if (!def.HasFlag(LabBalanceCheckFlags.DungeonClear))
                return new LabBalanceTargetEvaluation { Checks = checks };

            checks.Add(new LabBalanceTargetCheck
            {
                Name = "Dungeon clear rate",
                ActualText = $"{report.ClearRate * 100:F1}%",
                TargetText = $"{def.MinDungeonClearRate * 100:F0}–{def.MaxDungeonClearRate * 100:F0}%",
                Passed = report.ClearRate >= def.MinDungeonClearRate
                         && report.ClearRate <= def.MaxDungeonClearRate
            });

            return new LabBalanceTargetEvaluation { Checks = checks };
        }

        /// <summary>Legacy helper used by unit tests — duration (+ optional combo/WR fixed).</summary>
        public static LabBalanceTargetEvaluation EvaluateEncounter(
            ActionLabEncounterSimulationReport report,
            FundamentalsAnalysisTargets targets,
            bool includeComboChecks = true,
            bool includeWinRateBand = false)
        {
            var checks = new List<LabBalanceTargetCheck>();
            AddDurationChecks(checks, report, targets);
            if (includeComboChecks)
                AddComboChecks(checks, report, targets);
            if (includeWinRateBand)
            {
                checks.Add(new LabBalanceTargetCheck
                {
                    Name = "Win rate",
                    ActualText = $"{report.WinRate * 100:F1}%",
                    TargetText = "85–98%",
                    Passed = report.WinRate >= 0.85 && report.WinRate <= 0.98
                });
            }

            return new LabBalanceTargetEvaluation { Checks = checks };
        }

        public static LabBalanceReportDelta ComputeDelta(
            ActionLabEncounterSimulationReport? previous,
            ActionLabEncounterSimulationReport current)
        {
            if (previous == null)
                return new LabBalanceReportDelta();

            return new LabBalanceReportDelta
            {
                WinRateDelta = current.WinRate - previous.WinRate,
                MedianTurnsDelta = current.MedianTurns - previous.MedianTurns,
                MedianPlayerTurnsDelta = EstimateMedianPlayerTurns(current) - EstimateMedianPlayerTurns(previous),
                AverageMaxComboStreakDelta = current.AveragePlayerMaxComboStreak - previous.AveragePlayerMaxComboStreak,
                AverageHpRemainingOnWinDelta =
                    current.AveragePlayerHpRemainingOnWin - previous.AveragePlayerHpRemainingOnWin
            };
        }

        public static double EstimateMedianCombined(ActionLabEncounterSimulationReport report) =>
            report.MedianTurns > 0 ? report.MedianTurns : report.AverageTurns;

        public static double EstimateMedianPlayerTurns(ActionLabEncounterSimulationReport report)
        {
            var samples = report.Encounters.Where(e => string.IsNullOrEmpty(e.ErrorMessage)).Select(e => (double)e.PlayerTurns).OrderBy(v => v).ToList();
            if (samples.Count == 0)
                return report.AverageTurnsOnWin > 0 ? report.AverageTurnsOnWin * 0.5 : report.AverageTurns * 0.5;
            int mid = samples.Count / 2;
            return (samples.Count & 1) == 1
                ? samples[mid]
                : 0.5 * (samples[mid - 1] + samples[mid]);
        }

        public static FundamentalsAnalysisTargets DurationTargetsForLevel(
            LabBalanceLayerDefinition def, int level)
        {
            var baseT = def.Targets;
            if (def.Layer == LabBalanceProcessLayer.GearInjection)
                return LabBalanceLayerDefinition.CloneTargets(baseT);

            // Prefer the layer's editable TempoTolerance at the scenario default level;
            // wider anchors still scale tolerance by level so high-level bands do not stay L1-tight.
            double tol;
            if (def.HasFlag(LabBalanceCheckFlags.AllAnchors) && level != def.DefaultPlayerLevel)
                tol = LabBalanceLayerDefinition.TempoToleranceForLevel(level);
            else if (baseT.TempoTolerance > 0)
                tol = baseT.TempoTolerance;
            else
                tol = LabBalanceLayerDefinition.TempoToleranceForLevel(level);

            return new FundamentalsAnalysisTargets
            {
                TargetMedianPlayerTurns = baseT.TargetMedianPlayerTurns,
                TargetMedianEnemyTurns = baseT.TargetMedianEnemyTurns,
                TargetMedianCombinedActions = baseT.TargetMedianCombinedActions,
                MinAverageActions = baseT.MinAverageActions,
                MaxAverageActions = baseT.MaxAverageActions,
                MinAverageComboStreakRuns2Plus = baseT.MinAverageComboStreakRuns2Plus,
                MinAverageMaxComboStreak = baseT.MinAverageMaxComboStreak,
                TempoTolerance = tol,
                RequireL1AnchorBeforeScaling = baseT.RequireL1AnchorBeforeScaling
            };
        }

        private static void AddDurationChecks(
            List<LabBalanceTargetCheck> checks,
            ActionLabEncounterSimulationReport report,
            FundamentalsAnalysisTargets targets)
        {
            double tol = targets.TempoTolerance > 0 ? targets.TempoTolerance : 1.5;
            double medianCombined = EstimateMedianCombined(report);
            double medianPlayer = EstimateMedianPlayerTurns(report);
            double medianEnemy = Math.Max(0, medianCombined - medianPlayer);

            checks.Add(MakeBand("Median combined actions", medianCombined,
                targets.TargetMedianCombinedActions, tol, "F1"));
            checks.Add(MakeBand("Median hero turns", medianPlayer,
                targets.TargetMedianPlayerTurns, tol, "F1"));
            checks.Add(MakeBand("Median enemy turns", medianEnemy,
                targets.TargetMedianEnemyTurns, tol, "F1"));
            checks.Add(new LabBalanceTargetCheck
            {
                Name = "Mean combined actions band",
                ActualText = $"{report.AverageTurns:F1}",
                TargetText = $"{targets.MinAverageActions:F0}–{targets.MaxAverageActions:F0}",
                Passed = report.AverageTurns >= targets.MinAverageActions
                         && report.AverageTurns <= targets.MaxAverageActions
            });
        }

        private static void AddComboChecks(
            List<LabBalanceTargetCheck> checks,
            ActionLabEncounterSimulationReport report,
            FundamentalsAnalysisTargets targets)
        {
            double streakRuns = report.Encounters.Count > 0
                ? report.Encounters
                    .Where(e => string.IsNullOrEmpty(e.ErrorMessage))
                    .DefaultIfEmpty()
                    .Average(e => e == null ? 0 : e.PlayerComboStreakRunCounts.Values.Sum())
                : 0;

            checks.Add(new LabBalanceTargetCheck
            {
                Name = "Avg combo streak runs (2+)",
                ActualText = $"{streakRuns:F2}",
                TargetText = $">= {targets.MinAverageComboStreakRuns2Plus:F1}",
                Passed = streakRuns >= targets.MinAverageComboStreakRuns2Plus
            });

            checks.Add(new LabBalanceTargetCheck
            {
                Name = "Avg max combo streak",
                ActualText = $"{report.AveragePlayerMaxComboStreak:F2}",
                TargetText = $">= {targets.MinAverageMaxComboStreak:F1}",
                Passed = report.AveragePlayerMaxComboStreak >= targets.MinAverageMaxComboStreak
            });
        }

        private static void AddWinRateCurveCheck(
            List<LabBalanceTargetCheck> checks,
            double winRateFraction,
            int level,
            string namePrefix = "")
        {
            double actualPct = LevelWinRateCurve.NormalizeToPercent(winRateFraction);
            double target = LevelWinRateCurve.GetTargetWinRate(level);
            double tol = LevelWinRateCurve.GetTolerance(level);
            bool passed = LevelWinRateCurve.IsWinRateInBand(winRateFraction, level);
            checks.Add(new LabBalanceTargetCheck
            {
                Name = $"{namePrefix}Win rate (curve L{level})",
                ActualText = $"{actualPct:F1}%",
                TargetText = $"{target:F0}% ± {tol:F0}pp",
                Passed = passed
            });
        }

        private static LabBalanceTargetCheck MakeBand(
            string name, double actual, double target, double tolerance, string format)
        {
            return new LabBalanceTargetCheck
            {
                Name = name,
                ActualText = actual.ToString(format),
                TargetText = $"{target.ToString(format)} ± {tolerance.ToString(format)}",
                Passed = Math.Abs(actual - target) <= tolerance
            };
        }
    }
}
