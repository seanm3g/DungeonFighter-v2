using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>Plain-text formatting for <see cref="ActionLabEncounterSimulationReport"/>.</summary>
    public static class ActionLabEncounterReportFormatter
    {
        public const int ComboHistogramMaxBucket = 10;
        public const int MaxComboStreakHistogramBucket = 15;

        private static readonly string[] TurnHistogramBucketOrder =
        {
            "1-5", "6-10", "11-20", "21-40", "41+",
        };

        /// <summary>Formats <paramref name="report"/> as plain text for the report dialog.</summary>
        public static string FormatReportText(ActionLabEncounterSimulationReport report, LabCombatSnapshot? snapshot = null)
        {
            var lines = new List<string>();
            if (snapshot != null)
            {
                lines.Add("Setup");
                lines.Add($"  Forced catalog action: {snapshot.SelectedCatalogActionName}");
                lines.Add($"  Hero combo strip: {snapshot.ComboStripActionNames.Count} action(s)");
                lines.Add(string.IsNullOrEmpty(snapshot.SessionEnemyLoaderType)
                    ? "  Enemy: default test dummy (lab baseline stats)"
                    : $"  Enemy: loader '{snapshot.SessionEnemyLoaderType}' level {snapshot.EnemyLevel}");
                lines.Add("");
            }

            lines.Add("Summary");
            lines.Add($"  Encounters (total): {report.EncounterCount}");
            lines.Add($"  Successful (no sim error): {report.EncounterCount - report.ErroredEncounters}");
            lines.Add($"  Errored / aborted: {report.ErroredEncounters}");
            lines.Add($"  Player wins: {report.PlayerWins} ({report.WinRate:P1} of successful)");
            lines.Add("");

            lines.Add("Tempo (turns)");
            lines.Add($"  Mean: {report.AverageTurns:F2}  Median: {report.MedianTurns:F2}  Std dev: {report.StdDevTurns:F2}");
            lines.Add($"  Min: {report.MinTurns}  Max: {report.MaxTurns}");
            lines.Add($"  Mean turns on win: {FmtMaybeNaN(report.AverageTurnsOnWin, "F2")}  on loss: {FmtMaybeNaN(report.AverageTurnsOnLoss, "F2")}");
            lines.Add("");

            lines.Add("Turn count histogram (successful encounters)");
            foreach (var key in TurnHistogramBucketOrder)
            {
                if (!report.TurnCountHistogram.TryGetValue(key, out int n) || n == 0)
                    continue;
                int ok = report.EncounterCount - report.ErroredEncounters;
                double pct = ok > 0 ? n / (double)ok : 0;
                lines.Add($"  {key,-7}: {n} ({pct:P1})");
            }

            lines.Add("");
            lines.Add("Damage and DPS (player)");
            lines.Add($"  Damage — mean: {report.AveragePlayerDamage:F1}  median: {report.MedianPlayerDamage:F1}  std dev: {report.StdDevPlayerDamage:F1}");
            lines.Add($"  DPS — mean: {report.AveragePlayerDps:F2}  median: {report.MedianPlayerDps:F2}  std dev: {report.StdDevPlayerDps:F2}");
            lines.Add($"  Mean damage on win: {FmtMaybeNaN(report.AveragePlayerDamageOnWin, "F1")}  on loss: {FmtMaybeNaN(report.AveragePlayerDamageOnLoss, "F1")}");
            lines.Add($"  Mean DPS on win: {FmtMaybeNaN(report.AveragePlayerDpsOnWin, "F2")}  on loss: {FmtMaybeNaN(report.AveragePlayerDpsOnLoss, "F2")}");
            lines.Add($"  Combat game time — median: {report.MedianCombatGameTime:F3}  std dev: {report.StdDevCombatGameTime:F3}");
            lines.Add("");

            lines.Add("HP remaining (end of fight)");
            lines.Add($"  Mean player HP on win: {FmtMaybeNaN(report.AveragePlayerHpRemainingOnWin, "F1")}  on loss: {FmtMaybeNaN(report.AveragePlayerHpRemainingOnLoss, "F1")}");
            lines.Add("");

            lines.Add("Enemy (from narrative)");
            lines.Add($"  Mean damage to player: {report.AverageEnemyDamage:F1}");
            lines.Add($"  Mean enemy combo events: {report.AverageEnemyComboCount:F2}");
            lines.Add("");

            lines.Add("Crits");
            lines.Add($"  Mean crits per encounter: {report.AverageCritsPerEncounter:F2}");
            lines.Add($"  Crit rate (all player damage events): {report.CritRatePerDamageEvent:P1}");
            lines.Add("");

            lines.Add("Player combo events per encounter (successful player actions with IsCombo)");
            for (int b = 0; b <= ComboHistogramMaxBucket; b++)
            {
                if (!report.PlayerComboCountHistogram.TryGetValue(b, out int n) || n == 0)
                    continue;
                string label = b == ComboHistogramMaxBucket ? $"{ComboHistogramMaxBucket}+" : b.ToString();
                int ok = report.EncounterCount - report.ErroredEncounters;
                double pct = ok > 0 ? n / (double)ok : 0;
                lines.Add($"  {label,3}: {n} ({pct:P1})");
            }

            int okEnc = report.EncounterCount - report.ErroredEncounters;
            var streakSamples = report.Encounters.Where(e => string.IsNullOrEmpty(e.ErrorMessage)).ToList();
            lines.Add("");
            lines.Add("Player combo chains (consecutive successful combo-flagged actions)");
            if (okEnc > 0 && streakSamples.Count > 0)
            {
                lines.Add("  P(encounter had longest chain ≥ N) — among successful encounters:");
                for (int n = 2; n <= 10; n++)
                {
                    int c = streakSamples.Count(e => e.PlayerMaxComboStreak >= n);
                    lines.Add($"    ≥{n}: {c} ({c / (double)okEnc:P1})");
                }

                lines.Add($"  Mean longest chain per encounter: {report.AveragePlayerMaxComboStreak:F2}");

                lines.Add("  Longest chain in encounter (histogram, successful encounters)");
                for (int b = 0; b <= MaxComboStreakHistogramBucket; b++)
                {
                    if (!report.PlayerMaxComboStreakHistogram.TryGetValue(b, out int hn) || hn == 0)
                        continue;
                    string label = b == MaxComboStreakHistogramBucket ? $"{MaxComboStreakHistogramBucket}+" : b.ToString();
                    lines.Add($"    {label,3}: {hn} ({hn / (double)okEnc:P1})");
                }

                int totalRuns = report.PlayerComboStreakRunTotals.Values.Sum();
                lines.Add("  Pooled combo runs (each chain of length ≥ 2 counts once; share of all such runs):");
                if (totalRuns > 0)
                {
                    foreach (var len in report.PlayerComboStreakRunTotals.Keys.OrderBy(k => k))
                    {
                        int rc = report.PlayerComboStreakRunTotals[len];
                        lines.Add($"    length {len}: {rc} ({rc / (double)totalRuns:P1} of runs)");
                    }
                }
                else
                    lines.Add("    (no chains of length ≥ 2 in sample)");
            }
            else
                lines.Add("  (no successful encounters to summarize)");

            if (report.TerminalReasonCounts.Count > 0)
            {
                lines.Add("");
                lines.Add("Last AdvanceSingleTurnAsync result (all encounters)");
                foreach (var kv in report.TerminalReasonCounts.OrderBy(k => k.Key.ToString()))
                    lines.Add($"  {kv.Key}: {kv.Value}");
            }

            var errors = report.Encounters.Select((e, i) => (i, e.ErrorMessage)).Where(x => !string.IsNullOrEmpty(x.ErrorMessage)).Take(12).ToList();
            if (errors.Count > 0)
            {
                lines.Add("");
                lines.Add("Sample errors:");
                foreach (var (idx, msg) in errors)
                    lines.Add($"  #{idx + 1}: {msg}");
            }

            if (report.SimulationWallElapsed > TimeSpan.Zero)
            {
                lines.Add("");
                lines.Add($"Simulation wall time: {FormatSimulationWallTime(report.SimulationWallElapsed)}");
            }

            return string.Join(global::System.Environment.NewLine, lines);
        }

        public static string TurnBucketLabel(int turns)
        {
            if (turns <= 5) return "1-5";
            if (turns <= 10) return "6-10";
            if (turns <= 20) return "11-20";
            if (turns <= 40) return "21-40";
            return "41+";
        }

        private static string FormatSimulationWallTime(TimeSpan elapsed)
        {
            if (elapsed.TotalHours >= 1.0)
                return $"{(int)elapsed.TotalHours} h {elapsed.Minutes} min {elapsed.Seconds}.{elapsed.Milliseconds:D3} s";
            if (elapsed.TotalMinutes >= 1.0)
                return $"{(int)elapsed.TotalMinutes} min {elapsed.Seconds}.{elapsed.Milliseconds:D3} s";
            return $"{elapsed.TotalSeconds:F2} s";
        }

        private static string FmtMaybeNaN(double value, string format)
        {
            if (double.IsNaN(value))
                return "n/a";
            return value.ToString(format);
        }
    }
}
