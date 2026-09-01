using System;

namespace RPGGame.Data
{
    /// <summary>
    /// Authoring helpers for the triggers sheet: derive <c>effectTarget</c> / colon args from
    /// authoritative <c>when</c> + <c>mechanics</c> cells. Combat still resolves from mechanic ids.
    /// </summary>
    public static class TriggerIdentitySheetMeta
    {
        public static string ParseColonArg(string? cell)
        {
            if (string.IsNullOrWhiteSpace(cell))
                return "";
            int colon = cell.IndexOf(':');
            if (colon < 0 || colon >= cell.Length - 1)
                return "";
            return cell.Substring(colon + 1).Trim();
        }

        public static string ParseBaseId(string? cell)
        {
            if (string.IsNullOrWhiteSpace(cell))
                return "";
            string first = cell;
            int comma = cell.IndexOfAny(new[] { ',', ';' });
            if (comma > 0)
                first = cell.Substring(0, comma);
            first = first.Trim();
            int colon = first.IndexOf(':');
            if (colon > 0)
                first = first.Substring(0, colon);
            return first.Trim();
        }

        /// <summary>
        /// Who the effect applies to for sheet authoring:
        /// <c>hero</c> / <c>enemy</c> / <c>self</c> / <c>foe</c> / <c>strip</c> / <c>system</c>.
        /// </summary>
        public static string DeriveEffectTarget(string? mechanicsCell)
        {
            string id = ParseBaseId(mechanicsCell);
            if (string.IsNullOrEmpty(id))
                return "system";

            string norm = id.Replace('-', '_').ToLowerInvariant();

            if (norm.StartsWith("enemy_", StringComparison.Ordinal))
                return "enemy";
            if (norm.StartsWith("hero_", StringComparison.Ordinal))
                return "hero";
            if (norm.StartsWith("strip_", StringComparison.Ordinal)
                || norm.StartsWith("retrigger_", StringComparison.Ordinal))
                return "strip";

            return norm switch
            {
                "harden" or "focus" or "fortify" or "heal" or "armor" or "max_health"
                    or "grant_action" or "grant_action_tag" => "self",
                "expose" or "weaken" or "pierce" or "vulnerability" or "slow"
                    or "poison" or "burn" or "bleed" or "acid"
                    or "confuse" or "confusion" or "silence" or "disrupt"
                    or "stat_drain" or "statdrain" => "foe",
                "salvage_miss" or "replace_next_roll" or "crit_face_min" => "hero",
                _ => "system"
            };
        }

        /// <summary>Fills blank authoring meta from <c>when</c> / <c>mechanics</c>.</summary>
        public static void EnsureAuthoringMeta(TriggerIdentityData row)
        {
            if (row == null)
                return;

            if (string.IsNullOrWhiteSpace(row.EffectTarget))
                row.EffectTarget = DeriveEffectTarget(row.Mechanics);

            if (string.IsNullOrWhiteSpace(row.WhenArg))
                row.WhenArg = ParseColonArg(row.When);

            if (string.IsNullOrWhiteSpace(row.MechanicArg))
                row.MechanicArg = ParseColonArg(row.Mechanics);
        }
    }
}
