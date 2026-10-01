using System.Collections.Generic;
using RPGGame.Data;

namespace RPGGame.UI
{
    /// <summary>Player-facing cadence card lines. A keyword prefix means the effect lingers; bare lines are this swing.</summary>
    public static class CadenceCardLineFormatter
    {
        public static string FormatCadenceHeader(string? cadence, int duration)
        {
            string c = CadenceKeywords.NormalizeCadenceType(cadence ?? "");
            if (string.IsNullOrEmpty(c))
                c = CadenceKeywords.Turn;
            if (duration <= 0)
                duration = 1;
            return $"{c} ({duration}x)";
        }

        public static string FormatCadenceHeader(ActionAttackBonusGroup group, int displayCount) =>
            FormatCadenceHeader(ResolveGroupCadence(group), displayCount);

        /// <summary>Prefix for a lingering effect. Duration 1 is <c>turn:</c>; longer is <c>turn x3:</c>.</summary>
        public static string FormatLingeringPrefix(string? cadence, int duration)
        {
            string c = CadenceKeywords.NormalizeCadenceType(cadence ?? "");
            if (string.IsNullOrEmpty(c))
                c = CadenceKeywords.Turn;
            int count = duration <= 0 ? 1 : duration;
            return count <= 1
                ? $"{c.ToLowerInvariant()}:"
                : $"{c.ToLowerInvariant()} x{count}:";
        }

        public static string FormatMechanicLine(string mechanicId, double quantity, string? statSubType = null)
        {
            string label = ActionMechanicsRegistry.GetDisplayLabel(mechanicId, statSubType);
            if (string.IsNullOrEmpty(label))
                return "";
            return FormatQuantityLine(label, quantity, ActionMechanicsRegistry.IsPercentQuantityMechanic(mechanicId));
        }

        public static string FormatMechanicLineFromBonusItem(ActionAttackBonusItem item)
        {
            if (item == null)
                return "";
            if (ActionMechanicsRegistry.TryGetMechanicIdFromBonusType(item.Type, out string mechanicId, out string? statSubType))
                return FormatMechanicLine(mechanicId, item.Value, statSubType);
            string label = item.Type switch
            {
                "ACCURACY" => "ACC",
                "HIT" => "HIT",
                "COMBO" => "COMBO",
                "CRIT" => "CRIT",
                "CRIT_MISS" => "CRIT MISS",
                "DAMAGE_MOD" => "DAMAGE",
                "SPEED_MOD" => "SPEED",
                "MULTIHIT_MOD" => "MULTIHIT",
                "AMP_MOD" => "AMP",
                "WEAPON_SPEED" => "WPN SPD",
                "WEAPON_DAMAGE" => "WPN DMG",
                "STR" or "STRENGTH" => "STR",
                "AGI" or "AGILITY" => "AGI",
                "TECH" or "TECHNIQUE" => "TECH",
                "INT" or "INTELLIGENCE" => "INT",
                _ => item.Type
            };
            bool isPercent = item.Type is "DAMAGE_MOD" or "SPEED_MOD" or "AMP_MOD";
            return FormatQuantityLine(label, item.Value, isPercent);
        }

        public static List<string> FormatBlockLines(string? cadence, int duration, IEnumerable<ActionAttackBonusItem>? bonuses)
        {
            var lines = new List<string>();
            if (bonuses == null)
                return lines;
            foreach (var b in bonuses)
                AddLingering(lines, cadence, duration, FormatMechanicLineFromBonusItem(b));
            return lines;
        }

        public static List<string> FormatBlockLinesFromEditor(CadenceEditorBlock block)
        {
            var lines = new List<string>();
            if (block?.Mechanics == null || block.Mechanics.Count == 0)
                return lines;
            foreach (var row in block.Mechanics)
            {
                if (string.IsNullOrWhiteSpace(row.MechanicId) || row.Quantity == 0)
                    continue;
                AddLingering(lines, block.Cadence, block.Duration, FormatMechanicLine(row.MechanicId, row.Quantity, row.StatSubType));
            }
            return lines;
        }

        public static List<string> FormatAllBlocks(IEnumerable<CadenceEditorBlock>? blocks)
        {
            var lines = new List<string>();
            if (blocks == null)
                return lines;
            bool first = true;
            foreach (var block in blocks)
            {
                var blockLines = FormatBlockLinesFromEditor(block);
                if (blockLines.Count == 0)
                    continue;
                if (!first)
                    lines.Add("");
                lines.AddRange(blockLines);
                first = false;
            }
            return lines;
        }

        public static List<string> FormatGroupLines(ActionAttackBonusGroup group, int displayCount)
        {
            var lines = new List<string>();
            if (group?.Bonuses == null || group.Bonuses.Count == 0)
                return lines;
            string cadence = ResolveGroupCadence(group);
            foreach (var b in group.Bonuses)
                AddLingering(lines, cadence, displayCount, FormatMechanicLineFromBonusItem(b));
            return lines;
        }

        private static string ResolveGroupCadence(ActionAttackBonusGroup group)
        {
            string cad = CadenceKeywords.NormalizeCadenceType(
                string.IsNullOrWhiteSpace(group.CadenceType)
                    ? (string.IsNullOrWhiteSpace(group.Keyword) ? CadenceKeywords.Turn : group.Keyword)
                    : group.CadenceType);
            if (!string.IsNullOrWhiteSpace(group.DurationType)
                && !CadenceKeywords.IsKeywordCadence(group.DurationType))
            {
                cad = CadenceKeywords.NormalizeCadenceType(group.DurationType);
            }
            return string.IsNullOrEmpty(cad) ? CadenceKeywords.Turn : cad;
        }

        private static void AddLingering(List<string> lines, string? cadence, int duration, string mechanicLine)
        {
            if (string.IsNullOrEmpty(mechanicLine))
                return;
            lines.Add($"{FormatLingeringPrefix(cadence, duration)} {mechanicLine}");
        }

        private static string FormatQuantityLine(string label, double quantity, bool isPercent)
        {
            string sign = quantity >= 0 ? "+" : "";
            if (isPercent)
            {
                if (quantity >= 100 && quantity % 100 == 0)
                    return $"{label} {(quantity / 100.0):0.#}x";
                return $"{label} {sign}{quantity:0}%";
            }
            return $"{label} {sign}{quantity:0}";
        }
    }
}
