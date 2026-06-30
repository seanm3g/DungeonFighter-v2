using System;

namespace RPGGame.Data
{
    /// <summary>
    /// Column K <c>STATUS EFFECT / DURATION</c> is cadence duration — how many ACTION/ATTACK/ABILITY applications apply (e.g. ACTION x2).
    /// </summary>
    public static class SpreadsheetDurationSemantics
    {
        public const string StatusEffectContext = "STATUS EFFECT";
        public const string DurationLabel = "DURATION";

        public static bool RowHasCadenceKeyword(SpreadsheetActionData? row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.Cadence)) return false;
            var c = row.Cadence.Trim();
            return c.Equals("ACTION", StringComparison.OrdinalIgnoreCase)
                || c.Equals("ACTIONS", StringComparison.OrdinalIgnoreCase)
                || c.Equals("ATTACK", StringComparison.OrdinalIgnoreCase)
                || c.Equals("ATTACKS", StringComparison.OrdinalIgnoreCase)
                || c.Equals("ABILITY", StringComparison.OrdinalIgnoreCase)
                || c.Equals("ABILITIES", StringComparison.OrdinalIgnoreCase)
                || c.Equals("FIGHT", StringComparison.OrdinalIgnoreCase)
                || c.Equals("DUNGEON", StringComparison.OrdinalIgnoreCase)
                || c.Equals("CHAIN", StringComparison.OrdinalIgnoreCase)
                || c.Equals("COMBO", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>STATUS EFFECT / DURATION → cadence application count (<see cref="ActionData.ComboBonusDuration"/>).</summary>
        public static int ResolveCadenceDuration(SpreadsheetActionData? row)
        {
            if (row == null) return 0;
            int fromDuration = SpreadsheetActionData.ParseIntValue(row.Duration);
            if (fromDuration > 0)
                return fromDuration;

            // Legacy JSON rows may still carry a separate cadenceDuration field.
            return SpreadsheetActionData.ParseIntValue(row.CadenceApplicationCount);
        }
    }
}
