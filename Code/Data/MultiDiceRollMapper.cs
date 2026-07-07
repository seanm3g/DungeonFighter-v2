using System;

namespace RPGGame.Data
{
    /// <summary>
    /// Maps spreadsheet LUCK/UNLUCK and dice-roll columns to ACTION-cadence pending bonus types
    /// (ADVANTAGE / DISADVANTAGE) for next-roll 2d20 take-higher/lower.
    /// </summary>
    public static class MultiDiceRollMapper
    {
        public const string AdvantageBonusType = "ADVANTAGE";
        public const string DisadvantageBonusType = "DISADVANTAGE";

        /// <summary>
        /// Ensures <paramref name="actionData"/> has an ACTION-cadence bonus group when advantage/disadvantage is detected.
        /// </summary>
        public static void ApplyRollAdvantageBonuses(ActionData actionData, SpreadsheetActionData spreadsheet)
        {
            if (actionData == null || spreadsheet == null)
                return;

            string? bonusType = ResolveBonusType(spreadsheet);
            if (bonusType == null)
                return;

            int diceCount = ResolveDiceCount(spreadsheet.DiceRolls);
            int duration = SpreadsheetActionData.ParseIntValue(spreadsheet.Duration);
            if (duration <= 0)
                duration = 1;

            string cadence = string.IsNullOrWhiteSpace(spreadsheet.Cadence)
                ? CadenceKeywords.Action
                : CadenceKeywords.Normalize(spreadsheet.Cadence);
            if (!CadenceKeywords.IsKeywordCadence(cadence))
                cadence = CadenceKeywords.Action;

            string cadenceType = CadenceKeywords.IsTurn(cadence) ? CadenceKeywords.Turn : CadenceKeywords.Action;

            if (string.IsNullOrWhiteSpace(actionData.Cadence))
                actionData.Cadence = cadenceType;

            actionData.ActionAttackBonuses ??= new ActionAttackBonuses();
            if (HasBonusType(actionData.ActionAttackBonuses, bonusType))
                return;

            actionData.ActionAttackBonuses.BonusGroups.Add(new ActionAttackBonusGroup
            {
                Keyword = cadenceType,
                CadenceType = cadenceType,
                Count = duration,
                DurationType = cadenceType,
                Bonuses = new System.Collections.Generic.List<ActionAttackBonusItem>
                {
                    new ActionAttackBonusItem { Type = bonusType, Value = diceCount }
                }
            });
        }

        private static bool HasBonusType(ActionAttackBonuses bonuses, string bonusType)
        {
            foreach (var group in bonuses.BonusGroups)
            {
                if (group.Bonuses == null)
                    continue;
                foreach (var item in group.Bonuses)
                {
                    if (string.Equals(item.Type, bonusType, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private static string? ResolveBonusType(SpreadsheetActionData spreadsheet)
        {
            string? fromColumns = ParseHighestLowestRoll(spreadsheet.HighestLowestRoll);
            if (fromColumns != null)
                return fromColumns;

            string actionName = spreadsheet.Action?.Trim() ?? "";
            if (string.Equals(actionName, "LUCK", StringComparison.OrdinalIgnoreCase))
                return AdvantageBonusType;
            if (string.Equals(actionName, "UNLUCK", StringComparison.OrdinalIgnoreCase))
                return DisadvantageBonusType;

            string desc = spreadsheet.Description?.Trim() ?? "";
            if (desc.Contains("ROLL FROM ADVANTAGE", StringComparison.OrdinalIgnoreCase)
                || desc.Contains("ROLL WITH ADVANTAGE", StringComparison.OrdinalIgnoreCase))
                return AdvantageBonusType;
            if (desc.Contains("ROLL WITH DISADVANTAGE", StringComparison.OrdinalIgnoreCase)
                || desc.Contains("ROLL FROM DISADVANTAGE", StringComparison.OrdinalIgnoreCase))
                return DisadvantageBonusType;

            return null;
        }

        internal static string? ParseHighestLowestRoll(string? cell)
        {
            if (string.IsNullOrWhiteSpace(cell))
                return null;

            string norm = cell.Trim().ToUpperInvariant();
            if (norm is "HIGHEST" or "HIGH" or "ADVANTAGE" or "MAX" or "TAKEHIGHEST" or "TAKE HIGHEST")
                return AdvantageBonusType;
            if (norm is "LOWEST" or "LOW" or "DISADVANTAGE" or "MIN" or "TAKELOWEST" or "TAKE LOWEST")
                return DisadvantageBonusType;

            return null;
        }

        internal static int ResolveDiceCount(string? diceRollsCell)
        {
            int parsed = SpreadsheetActionData.ParseIntValue(diceRollsCell ?? "");
            return parsed >= 2 ? parsed : 2;
        }
    }
}
