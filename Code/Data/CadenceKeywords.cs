using System;

namespace RPGGame.Data
{
    /// <summary>
    /// Canonical cadence keywords for bonus timing: TURN (per roll) and ACTION (combo FIFO).
    /// Legacy ATTACK/ATTACKS and ABILITY/ABILITIES are normalized on import.
    /// </summary>
    public static class CadenceKeywords
    {
        public const string Turn = "TURN";
        public const string Action = "ACTION";

        /// <summary>Normalize a raw cadence cell to canonical TURN or ACTION, or return uppercased special scopes (FIGHT, DUNGEON, …).</summary>
        public static string Normalize(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";

            string u = raw.Trim().ToUpperInvariant();
            return u switch
            {
                "ATTACK" or "ATTACKS" => Turn,
                "ACTION" or "ACTIONS" => Action,
                "ABILITY" or "ABILITIES" => Turn, // deprecated; default to TURN when no row context
                "FIGHT" => "FIGHT",
                "DUNGEON" => "DUNGEON",
                "CHAIN" => "CHAIN",
                "COMBO" => "COMBO",
                "TURN" or "TURNS" => Turn,
                _ => raw.Trim()
            };
        }

        /// <summary>
        /// Resolves deprecated ABILITY cadence using bonus content: modifier columns → ACTION, dice/stat → TURN.
        /// </summary>
        public static string NormalizeFromRow(string? raw, SpreadsheetActionData? row)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";

            string u = raw.Trim().ToUpperInvariant();
            if (u is not ("ABILITY" or "ABILITIES"))
                return Normalize(raw);

            if (row != null && RowHasModifierBonuses(row))
                return Action;
            return Turn;
        }

        /// <summary>Normalize cadence type on an existing bonus group (legacy ATTACK/ABILITY → TURN).</summary>
        public static string NormalizeCadenceType(string? cadenceType)
        {
            if (string.IsNullOrWhiteSpace(cadenceType))
                return "";
            return Normalize(cadenceType);
        }

        public static bool IsTurn(string? cadence)
        {
            if (string.IsNullOrWhiteSpace(cadence)) return false;
            var n = Normalize(cadence);
            return string.Equals(n, Turn, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAction(string? cadence)
        {
            if (string.IsNullOrWhiteSpace(cadence)) return false;
            var n = Normalize(cadence);
            return string.Equals(n, Action, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsKeywordCadence(string? cadence)
            => IsTurn(cadence) || IsAction(cadence);

        public static bool IsFight(string? cadence)
        {
            if (string.IsNullOrWhiteSpace(cadence)) return false;
            return string.Equals(Normalize(cadence), "FIGHT", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsDungeon(string? cadence)
        {
            if (string.IsNullOrWhiteSpace(cadence)) return false;
            return string.Equals(Normalize(cadence), "DUNGEON", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Resolves scoped cadence from bonus group fields (DurationType wins when set).</summary>
        public static string ResolveScope(string? cadenceType, string? durationType)
        {
            if (!string.IsNullOrWhiteSpace(durationType) && !IsKeywordCadence(durationType))
                return Normalize(durationType);
            return Normalize(cadenceType ?? "");
        }

        /// <summary>Player-facing label, e.g. "TURN x3" or "ACTION x2".</summary>
        public static string GetDisplayLabel(string? cadence, int count)
        {
            string c = NormalizeCadenceType(cadence ?? "");
            if (string.IsNullOrEmpty(c))
                c = Turn;
            if (count > 1)
                return $"{c} x{count}";
            return c;
        }

        /// <summary>Plural duration phrase for keyword strings, e.g. "3 TURNS".</summary>
        public static string GetPluralDurationPhrase(string? cadence, int count)
        {
            string c = NormalizeCadenceType(cadence ?? Turn);
            if (count > 1)
            {
                return c switch
                {
                    Turn => $"{count} TURNS",
                    Action => $"{count} ACTIONS",
                    _ => $"{count} {c}"
                };
            }
            return "the Next " + c;
        }

        private static bool RowHasModifierBonuses(SpreadsheetActionData row)
        {
            return !string.IsNullOrWhiteSpace(row.SpeedMod)
                || !string.IsNullOrWhiteSpace(row.DamageMod)
                || !string.IsNullOrWhiteSpace(row.MultiHitMod)
                || !string.IsNullOrWhiteSpace(row.AmpMod);
        }
    }
}
