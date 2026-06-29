using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Config;

namespace RPGGame.Tuning.Profiles
{
    public sealed class BalanceDialClassification
    {
        public BalanceDial PrimaryDial { get; init; }
        public double Confidence { get; init; }
        public string Diagnosis { get; init; } = "";
        public Dictionary<BalanceDial, double> Scores { get; init; } = new();
    }

    public static class BalanceDialClassifier
    {
        public static BalanceDialClassification Classify(TuningSimulationOutcome simulation, TuningAnalysis analysis)
        {
            var scores = new Dictionary<BalanceDial, double>
            {
                [BalanceDial.Power] = ScorePower(simulation, analysis),
                [BalanceDial.Variance] = ScoreVariance(simulation, analysis),
                [BalanceDial.Agency] = ScoreAgency(simulation, analysis),
                [BalanceDial.Scaling] = ScoreScaling(simulation, analysis)
            };

            BalanceDial primary = BalanceDial.Power;
            double best = -1;
            foreach (var kv in scores)
            {
                if (kv.Value > best)
                {
                    best = kv.Value;
                    primary = kv.Key;
                }
            }

            return new BalanceDialClassification
            {
                PrimaryDial = primary,
                Confidence = Math.Clamp(best, 0, 100),
                Diagnosis = BuildDiagnosis(primary, simulation, analysis),
                Scores = scores
            };
        }

        private static double ScorePower(TuningSimulationOutcome simulation, TuningAnalysis analysis)
        {
            double score = 0;
            var duration = BalanceTuningGoals.CombatDurationTargets;
            var winRate = BalanceTuningGoals.WinRateTargets;

            if (analysis.AverageCombatDuration > 0)
            {
                if (analysis.AverageCombatDuration < duration.OptimalMin || analysis.AverageCombatDuration > duration.OptimalMax)
                    score += 40;
                if (analysis.AverageCombatDuration < duration.MinTarget || analysis.AverageCombatDuration > duration.MaxTarget)
                    score += 30;
            }

            if (simulation.Fundamentals != null)
            {
                double avgTurns = simulation.Fundamentals.AveragePlayerTurnsPerEncounter;
                if (avgTurns < duration.OptimalMin || avgTurns > duration.OptimalMax)
                    score += 35;
            }

            if (analysis.OverallWinRate > 0)
            {
                if (analysis.OverallWinRate < winRate.MinTarget || analysis.OverallWinRate > winRate.MaxTarget)
                    score += 35;
            }

            if (simulation.Fundamentals?.WinRate > 0)
            {
                var snapshots = simulation.Fundamentals.LevelSnapshots;
                var snap = snapshots.Count > 0
                    ? snapshots.OrderByDescending(s => Math.Abs(LevelWinRateCurve.GetWinRateDelta(s.WinRate, s.Level))).First()
                    : null;
                int level = snap?.Level ?? simulation.Fundamentals.PlayerLevel;
                double observed = snap?.WinRate ?? simulation.Fundamentals.WinRate;
                if (!LevelWinRateCurve.IsWinRateInBand(observed, level))
                    score += 25;
            }

            return score;
        }

        private static double ScoreVariance(TuningSimulationOutcome simulation, TuningAnalysis analysis)
        {
            double score = 0;

            if (analysis.WeaponVariance > BalanceTuningGoals.WeaponBalanceTargets.MaxVariance)
                score += 30;

            if (simulation.Fundamentals != null)
            {
                var f = simulation.Fundamentals;
                if (f.TurnDurationStdDev > 4.0)
                    score += 25;
                if (f.AverageMaxComboStreak < 2.0)
                    score += 20;
                if (f.AverageMissRate > 0.35 || f.AverageCritRate > 0.25)
                    score += 20;
                if (f.AverageComboStreakRuns2PlusPerEncounter < 0.5)
                    score += 15;
            }

            return score;
        }

        private static double ScoreAgency(TuningSimulationOutcome simulation, TuningAnalysis analysis)
        {
            double score = 0;

            if (simulation.Fundamentals != null)
            {
                var f = simulation.Fundamentals;
                if (f.AverageLossSeverity > 50 && f.WinRate > 0.5)
                    score += 35;
                if (f.AverageTurnsBelowZero > 2)
                    score += 25;
                if (f.AverageMissRate > 0.4 && f.WinRate < 0.7)
                    score += 20;
            }

            if (analysis.OverallWinRate >= BalanceTuningGoals.WinRateTargets.MinTarget
                && analysis.AverageCombatDuration > BalanceTuningGoals.CombatDurationTargets.MaxTarget)
                score += 15;

            return score;
        }

        private static double ScoreScaling(TuningSimulationOutcome simulation, TuningAnalysis analysis)
        {
            double score = 0;

            if (simulation.MultiLevel != null)
            {
                if (!simulation.MultiLevel.AllAnchorsWithinTolerance)
                    score += 40;
                if (simulation.MultiLevel.WorstDeltaMagnitude > 5)
                    score += 30;
            }

            if (analysis.EnemyVariance < BalanceTuningGoals.EnemyDifferentiationTargets.MinVariance)
                score += 20;

            return score;
        }

        private static string BuildDiagnosis(BalanceDial dial, TuningSimulationOutcome simulation, TuningAnalysis analysis)
        {
            return dial switch
            {
                BalanceDial.Power =>
                    $"Power dial: avg turns {analysis.AverageCombatDuration:F1}, win rate {analysis.OverallWinRate:F1}% off target.",
                BalanceDial.Variance =>
                    "Variance dial: turn spread/combo streaks/miss-crit rates indicate excessive randomness.",
                BalanceDial.Agency =>
                    $"Agency dial: high loss severity ({simulation.Fundamentals?.AverageLossSeverity:F0}) or low player control.",
                BalanceDial.Scaling =>
                    "Scaling dial: level curve or enemy differentiation drift detected.",
                _ => "No dominant dial."
            };
        }

        public static IReadOnlyList<string> SuggestersForDial(BalanceDial dial) => dial switch
        {
            BalanceDial.Power => new[]
            {
                TuningSuggesterIds.Global,
                TuningSuggesterIds.Duration,
                TuningSuggesterIds.Player,
                TuningSuggesterIds.EnemyBaseline
            },
            BalanceDial.Variance => new[]
            {
                TuningSuggesterIds.Variance,
                TuningSuggesterIds.Weapon
            },
            BalanceDial.Agency => new[]
            {
                TuningSuggesterIds.Agency,
                TuningSuggesterIds.Player
            },
            BalanceDial.Scaling => new[]
            {
                TuningSuggesterIds.LevelCurve,
                TuningSuggesterIds.Enemy,
                TuningSuggesterIds.DungeonScaling
            },
            _ => Array.Empty<string>()
        };
    }
}
