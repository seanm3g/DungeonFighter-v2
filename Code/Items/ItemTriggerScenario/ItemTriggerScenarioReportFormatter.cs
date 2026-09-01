using System;
using System.Collections.Generic;
using System.Text;

namespace RPGGame.Items.ItemTriggerScenario
{
    /// <summary>Plain-text formatting for item trigger scenario reports (CLI + Lab dialog).</summary>
    public static class ItemTriggerScenarioReportFormatter
    {
        public static string Format(ItemTriggerScenarioReport report, bool verbose = true)
        {
            ArgumentNullException.ThrowIfNull(report);
            var sb = new StringBuilder();
            string mark = report.Passed ? "PASS" : "FAIL";
            sb.AppendLine($"[{mark}] #{report.IdentityIndex} {report.IdentityName}");
            sb.AppendLine($"  WHEN={report.When}  SCOPE={(string.IsNullOrEmpty(report.Scope) ? "(instant)" : report.Scope)}  DO={report.Mechanics}");
            if (report.Value.HasValue)
                sb.AppendLine($"  value={report.Value}  scaleFrom={report.ScaleFrom ?? "-"}");
            if (report.Filters.Count > 0)
                sb.AppendLine($"  filters: {string.Join(", ", report.Filters)}");
            if (!string.IsNullOrWhiteSpace(report.Description))
                sb.AppendLine($"  {report.Description}");
            sb.AppendLine($"  path={report.Path}  channel={report.Channel}  d20={report.ForcedD20}");
            if (!string.IsNullOrWhiteSpace(report.OutcomeDetail))
                sb.AppendLine($"  outcome: {(report.OutcomeMatched ? "ok" : "MISMATCH")} — {report.OutcomeDetail}");
            sb.AppendLine($"  finding: {report.Finding}");

            if (verbose)
            {
                if (report.SetupNotes.Count > 0)
                {
                    sb.AppendLine("  setup:");
                    foreach (var n in report.SetupNotes)
                        sb.AppendLine($"    - {n}");
                }

                if (report.BuffDeltaLines.Count > 0)
                {
                    sb.AppendLine("  buffs:");
                    foreach (var d in report.BuffDeltaLines)
                        sb.AppendLine($"    - {d}");
                }
                else
                {
                    sb.AppendLine("  buffs: (no bank/status deltas)");
                }

                if (report.Hit || report.IsCritical || report.IsCombo || report.DamageDealt > 0)
                {
                    sb.AppendLine(
                        $"  swing: hit={report.Hit} crit={report.IsCritical} combo={report.IsCombo} " +
                        $"critMiss={report.IsCriticalMiss} dmg={report.DamageDealt} nested={report.NestedRetriggerCount}");
                }

                if (report.StatusMessages.Count > 0)
                {
                    sb.AppendLine("  messages:");
                    foreach (var m in report.StatusMessages)
                    {
                        string line = m.Length > 90 ? m.Substring(0, 87) + "…" : m;
                        sb.AppendLine($"    - {line}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(report.Error))
                    sb.AppendLine($"  error: {report.Error}");
            }

            return sb.ToString().TrimEnd();
        }

        public static string FormatBatch(ItemTriggerScenarioBatchResult batch, bool verbosePerReport = false)
        {
            ArgumentNullException.ThrowIfNull(batch);
            var sb = new StringBuilder();
            sb.AppendLine("=== Item Trigger Scenario Report ===");
            sb.AppendLine($"Total: {batch.Total}  Passed: {batch.Passed}  Failed: {batch.Failed}");
            sb.AppendLine();

            foreach (var r in batch.Reports)
            {
                sb.AppendLine(Format(r, verbose: verbosePerReport || !r.Passed));
                sb.AppendLine();
            }

            sb.AppendLine($"Summary: {batch.Passed}/{batch.Total} passed");
            return sb.ToString().TrimEnd();
        }

        /// <summary>Short multi-line blurb for the Action Lab tools panel.</summary>
        public static IReadOnlyList<string> FormatCompactLines(ItemTriggerScenarioReport report, int maxLines = 8)
        {
            ArgumentNullException.ThrowIfNull(report);
            var lines = new List<string>
            {
                $"{(report.Passed ? "PASS" : "FAIL")} #{report.IdentityIndex} {Truncate(report.IdentityName, 22)}",
                $"WHEN {Truncate(report.When, 12)} → {Truncate(report.Mechanics, 16)}",
                Truncate(report.Finding, 32)
            };
            foreach (var d in report.BuffDeltaLines)
            {
                if (lines.Count >= maxLines) break;
                lines.Add(Truncate(d, 32));
            }

            return lines;
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? "";
            return s.Substring(0, Math.Max(0, max - 1)) + "…";
        }
    }
}
