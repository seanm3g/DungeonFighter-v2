using System;
using System.Globalization;

namespace RPGGame.Data
{
    /// <summary>
    /// Column K <c>STATUS EFFECT / DURATION</c> is the cadence application <b>count</b> only (<c>1</c>, <c>2</c>, <c>3</c>).
    /// Pair with the <c>CADENCE</c> column (<c>TURN</c>, <c>ACTION</c>, …). Legacy combined cells (<c>3 ACTION</c>, <c>3 ATTACK</c>) are split on import.
    /// </summary>
    public static class SpreadsheetDurationSemantics
    {
        public const string StatusEffectContext = "STATUS EFFECT";
        public const string DurationLabel = "DURATION";

        /// <summary>
        /// Splits combined duration cells such as <c>3 ACTION</c> or <c>ACTION x2</c> into a numeric count and optional cadence keyword.
        /// Does not overwrite an explicit <see cref="SpreadsheetActionData.Cadence"/> column when present.
        /// </summary>
        public static void NormalizeDurationAndCadence(SpreadsheetActionData? row)
        {
            if (row == null)
                return;

            string raw = row.Duration?.Trim() ?? "";
            if (string.IsNullOrEmpty(raw))
                return;

            if (!TryParseCombinedDurationCell(raw, out int count, out string? keyword))
                return;

            if (count > 0)
                row.Duration = count.ToString(CultureInfo.InvariantCulture);

            if (string.IsNullOrWhiteSpace(row.Cadence) && !string.IsNullOrWhiteSpace(keyword))
                row.Cadence = CadenceKeywords.NormalizeFromRow(keyword, row);
        }

        /// <summary>
        /// Parses <c>3</c>, <c>3 ACTION</c>, <c>2 TURNS</c>, <c>2 ATTACKS</c> (legacy), <c>ACTION x3</c>, etc.
        /// </summary>
        public static bool TryParseCombinedDurationCell(string? cell, out int count, out string? cadenceKeyword)
        {
            count = 0;
            cadenceKeyword = null;
            if (string.IsNullOrWhiteSpace(cell))
                return false;

            string raw = cell.Trim();
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out count) && count > 0)
                return true;

            string[] parts = raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                if (TryParseCadenceKeyword(parts[0], out cadenceKeyword)
                    && TryParseDurationToken(parts[1], out count))
                    return count > 0;

                if (TryParseDurationToken(parts[0], out count)
                    && TryParseCadenceKeyword(parts[1], out cadenceKeyword))
                    return count > 0;
            }

            return false;
        }

        private static bool TryParseDurationToken(string token, out int count)
        {
            count = 0;
            if (string.IsNullOrWhiteSpace(token))
                return false;

            string t = token.Trim();
            if (t.StartsWith("x", StringComparison.OrdinalIgnoreCase))
                t = t.Substring(1);
            if (t.EndsWith("x", StringComparison.OrdinalIgnoreCase))
                t = t.Substring(0, t.Length - 1);

            return int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out count) && count > 0;
        }

        private static bool TryParseCadenceKeyword(string token, out string? cadenceKeyword)
        {
            cadenceKeyword = null;
            if (string.IsNullOrWhiteSpace(token))
                return false;

            string upper = token.Trim().ToUpperInvariant();
            if (upper is "FIGHT" or "DUNGEON" or "CHAIN" or "COMBO")
            {
                cadenceKeyword = upper;
                return true;
            }
            if (CadenceKeywords.IsTurn(upper) || CadenceKeywords.IsAction(upper)
                || upper is "ATTACK" or "ATTACKS" or "ABILITY" or "ABILITIES")
            {
                cadenceKeyword = upper;
                return true;
            }
            return false;
        }

        public static bool RowHasCadenceKeyword(SpreadsheetActionData? row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.Cadence)) return false;
            var c = row.Cadence.Trim();
            return CadenceKeywords.IsKeywordCadence(c)
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
            NormalizeDurationAndCadence(row);
            int fromDuration = SpreadsheetActionData.ParseIntValue(row.Duration);
            if (fromDuration > 0)
                return fromDuration;

            // Legacy JSON rows may still carry a separate cadenceDuration field.
            return SpreadsheetActionData.ParseIntValue(row.CadenceApplicationCount);
        }
    }
}
