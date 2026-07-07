using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data
{
    /// <summary>
    /// Push/pull sync for the declarative MECHANICS column and cadence timing defaults.
    /// </summary>
    public static class ActionMechanicsSheetSync
    {
        /// <summary>
        /// On push: auto-fill MECHANICS from detected columns and write effective CADENCE/DURATION defaults.
        /// </summary>
        public static void SyncRow(SpreadsheetActionData row)
        {
            if (row == null)
                return;

            ApplyCadenceDefaultsForMechanics(row);
            var detected = ActionMechanicsRegistry.DetectFromSpreadsheetRow(row);
            var forColumn = ActionMechanicsRegistry.FilterForMechanicsColumn(detected);
            row.Mechanics = ActionMechanicsRegistry.JoinMechanics(forColumn);
        }

        /// <summary>
        /// When a cadence-gated mechanic is present and timing cells are empty, default CADENCE/DURATION from the mechanic matrix.
        /// Partial timing fills the missing half (cadence without duration → 1; duration without cadence → resolved default).
        /// </summary>
        public static void ApplyCadenceDefaultsForMechanics(SpreadsheetActionData row)
        {
            if (row == null)
                return;

            SpreadsheetDurationSemantics.NormalizeDurationAndCadence(row);

            if (!ActionMechanicsRegistry.RowHasCadenceGatedMechanic(row))
                return;

            var detected = ActionMechanicsRegistry.DetectFromSpreadsheetRow(row);
            string? defaultCadence = ActionMechanicsRegistry.ResolveDefaultCadence(detected) ?? CadenceKeywords.Turn;

            bool cadenceBlank = string.IsNullOrWhiteSpace(row.Cadence);
            bool durationBlank = string.IsNullOrWhiteSpace(row.Duration)
                && string.IsNullOrWhiteSpace(row.CadenceApplicationCount);

            if (cadenceBlank && durationBlank)
            {
                row.Cadence = defaultCadence;
                row.Duration = "1";
            }
            else if (cadenceBlank)
            {
                row.Cadence = defaultCadence;
            }
            else if (durationBlank)
            {
                row.Duration = "1";
            }
        }

        /// <summary>Non-fatal pull validation: declared MECHANICS and CADENCE vs matrix.</summary>
        public static void ValidateAndWarn(SpreadsheetActionData row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.Action))
                return;

            var missing = ActionMechanicsRegistry.FindUndetectedDeclaredMechanics(row);
            if (missing.Count > 0)
            {
                Console.WriteLine(
                    $"Mechanics validation [{row.Action}]: declared but not detected from columns: {string.Join(", ", missing)}");
            }

            string? cadenceWarning = ActionMechanicsRegistry.ValidateCadenceForRow(row);
            if (!string.IsNullOrEmpty(cadenceWarning))
            {
                Console.WriteLine($"Mechanics validation [{row.Action}]: {cadenceWarning}");
            }

            var detected = ActionMechanicsRegistry.DetectFromSpreadsheetRow(row);
            bool hasNextAction = detected.Any(id => id.Contains("next_action", StringComparison.OrdinalIgnoreCase));
            bool hasTurnScoped = detected.Any(id =>
                id.Contains("threshold", StringComparison.OrdinalIgnoreCase)
                || id.EndsWith("_accuracy", StringComparison.OrdinalIgnoreCase)
                || id.EndsWith("_stat_bonus", StringComparison.OrdinalIgnoreCase));
            if (hasNextAction && hasTurnScoped && string.IsNullOrWhiteSpace(row.Cadence))
            {
                Console.WriteLine(
                    $"Mechanics validation [{row.Action}]: mixed turn-scoped and next-action mods without explicit CADENCE — defaulted to ATTACK");
            }
        }

        /// <summary>Warn for each row during pull.</summary>
        public static void ValidateAllAndWarn(IEnumerable<SpreadsheetActionData> rows)
        {
            if (rows == null)
                return;
            foreach (var row in rows)
                ValidateAndWarn(row);
        }
    }
}
