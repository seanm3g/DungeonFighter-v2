using System;
using System.Collections.Generic;

namespace RPGGame.Data
{
    /// <summary>
    /// Full (uncompacted) TURN / ACTION / FIGHT / DUNGEON cadence explanations for spreadsheet authoring.
    /// </summary>
    public static class CadenceScopeDescriptions
    {
        public const string ListTabName = "CADENCE_LIST";

        public static readonly (string Cadence, string Summary, string Detail)[] All =
        {
            (
                "TURN",
                "Next roll / turn-scoped bonus",
                "Applies to the hero's (or foe's) upcoming attack rolls or turn-scoped dice/stat shifts. "
                + "DURATION = how many turn applications (e.g. TURN ×2). "
                + "Typical for accuracy / HIT / COMBO / CRIT threshold mods and most status effects that tick with turns."
            ),
            (
                "ACTION",
                "Next combo-strip action (hit+combo bank)",
                "Deposits into the next-action bank for the following strip swing. "
                + "Redeemed on hit+combo; survives miss/non-combo until redeemed. "
                + "DURATION stacks grants additively (ACTION ×N). "
                + "Typical for ACTION SPEED / ACTION DAMAGE / MULTIHIT / AMP next-action mods."
            ),
            (
                "FIGHT",
                "While fighting this enemy",
                "Lasts for the current engagement with this foe. "
                + "Benefits can re-apply while that fight continues; clears when the enemy dies or fight ends. "
                + "Use for kill-granted or mid-fight lasting buffs (e.g. on kill → +damage for the fight)."
            ),
            (
                "DUNGEON",
                "Across the current dungeon run",
                "Long-lived for the run — intended to persist across fights until you leave the dungeon "
                + "or room-boundary temp-effect clears remove it. Heaviest / rarest scope."
            ),
        };

        public static string GetDetail(string? cadence)
        {
            string n = CadenceKeywords.Normalize(cadence ?? "");
            foreach (var row in All)
            {
                if (string.Equals(row.Cadence, n, StringComparison.OrdinalIgnoreCase))
                    return row.Detail;
            }

            return "";
        }

        public static string GetSummary(string? cadence)
        {
            string n = CadenceKeywords.Normalize(cadence ?? "");
            foreach (var row in All)
            {
                if (string.Equals(row.Cadence, n, StringComparison.OrdinalIgnoreCase))
                    return row.Summary;
            }

            return "";
        }

        /// <summary>Hover note for CADENCE / SCOPE cells listing all four scopes.</summary>
        public static string CombinedAuthoringNote()
        {
            var lines = new List<string>
            {
                "Cadence scope (pick ONE). Blank on a TRIGGERS SCOPE cell = instant one-shot.",
                ""
            };
            foreach (var row in All)
                lines.Add($"{row.Cadence} — {row.Summary}. {row.Detail}");
            return string.Join("\n\n", lines);
        }
    }
}
