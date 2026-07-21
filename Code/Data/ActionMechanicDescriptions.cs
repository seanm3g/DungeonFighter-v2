using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPGGame.Data
{
    /// <summary>
    /// Author-facing descriptions for MECHANIC_LIST / header hover notes.
    /// Flags near-duplicates so designers know which column is authoritative.
    /// </summary>
    public static class ActionMechanicDescriptions
    {
        public const string ListTabName = "MECHANIC_LIST";

        public static string GetDescription(string? mechanicId)
        {
            string id = ActionMechanicsRegistry.NormalizeMechanicId(mechanicId ?? "");
            if (string.IsNullOrEmpty(id))
                return "";

            if (Descriptions.TryGetValue(id, out string? text) && !string.IsNullOrEmpty(text))
                return text;

            return $"Mechanic '{id}'. Value lives in the matching ACTIONS detail column when present.";
        }

        /// <summary>Full cadence words allowed for this mechanic (TURN, ACTION, FIGHT, DUNGEON) — never compacted.</summary>
        public static string FormatAllowedCadences(string? mechanicId)
        {
            var allowed = ActionMechanicsRegistry.GetAllowedCadencesForMechanic(mechanicId ?? "");
            if (allowed.Count == 0)
                return "(timing-agnostic — cadence optional)";

            var order = new[] { "TURN", "ACTION", "FIGHT", "DUNGEON" };
            return string.Join(", ", order.Where(c => allowed.Contains(c)));
        }

        /// <summary>Note attached to MECHANIC_LIST id cells (hover in Sheets).</summary>
        public static string BuildHoverNote(string? mechanicId)
        {
            string id = ActionMechanicsRegistry.NormalizeMechanicId(mechanicId ?? "");
            var sb = new StringBuilder();
            sb.AppendLine(GetDescription(id));
            sb.AppendLine();
            sb.Append("Allowed cadences: ");
            sb.Append(FormatAllowedCadences(id));
            if (RedundancyNotes.TryGetValue(id, out string? redun) && !string.IsNullOrEmpty(redun))
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.Append("Note: ");
                sb.Append(redun);
            }

            return sb.ToString().TrimEnd();
        }

        private static readonly Dictionary<string, string> Descriptions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["hero_accuracy"] = "Hero ACCURACY — flat d20 roll bonus on this action (HERO DICE → ACCURACY column).",
            ["hero_hit_threshold"] = "Hero HIT threshold shift — easier/harder to land a normal hit (HERO DICE → HIT).",
            ["hero_combo_threshold"] = "Hero COMBO threshold shift — easier/harder to trigger combo-band rolls (HERO DICE → COMBO).",
            ["hero_crit_threshold"] = "Hero CRIT threshold shift (HERO DICE → CRIT).",
            ["hero_crit_miss_threshold"] = "Hero CRIT MISS threshold shift (HERO DICE → CRIT MISS).",
            ["enemy_accuracy"] = "Enemy ACCURACY when they use this action (ENEMY DICE → ACCURACY).",
            ["enemy_hit_threshold"] = "Enemy HIT threshold shift (ENEMY DICE → HIT).",
            ["enemy_combo_threshold"] = "Enemy COMBO threshold shift (ENEMY DICE → COMBO).",
            ["enemy_crit_threshold"] = "Enemy CRIT threshold shift (ENEMY DICE → CRIT).",
            ["enemy_crit_miss_threshold"] = "Enemy CRIT MISS threshold shift (ENEMY DICE → CRIT MISS).",
            ["hero_stat_bonus"] = "Temporary hero STR/AGI/TECH/INT bonus (HERO ATTRIBUTE columns). Pick the stat subtype in Settings.",
            ["enemy_stat_bonus"] = "Temporary enemy attribute bonus (ENEMY ATTRIBUTE columns).",
            ["hero_next_action_speed"] = "Hero ACTION SPEED mod % for the next strip action (HERO BASE STATS → ACTION SPEED). ACTION cadence.",
            ["hero_next_action_damage"] = "Hero ACTION DAMAGE mod % for the next strip action (HERO BASE STATS → DAMAGE MOD / ACTION DAMAGE). ACTION cadence.",
            ["hero_next_action_multihit"] = "Extra multihit layers on the next strip action (HERO BASE STATS → MULTIHIT MOD). ACTION cadence.",
            ["hero_next_action_amp"] = "Combo AMP % for the next strip action (HERO BASE STATS → AMP_MOD). ACTION cadence.",
            ["enemy_next_action_speed"] = "Enemy next-action speed % (ENEMY BASE STATS). ACTION cadence.",
            ["enemy_next_action_damage"] = "Enemy next-action damage % (ENEMY BASE STATS). ACTION cadence.",
            ["enemy_next_action_multihit"] = "Enemy next-action multihit (ENEMY BASE STATS). ACTION cadence.",
            ["enemy_next_action_amp"] = "Enemy next-action AMP % (ENEMY BASE STATS). ACTION cadence.",
            ["hero_weapon_speed"] = "Flat hero weapon speed points (HERO BASE → WEAPON SPEED). Each point ≈ −0.1 weapon time mult. TURN/ACTION/FIGHT/DUNGEON.",
            ["hero_weapon_damage"] = "Flat hero weapon damage (HERO BASE → WEAPON DAMAGE). Added to weapon damage. TURN/ACTION/FIGHT/DUNGEON.",
            ["enemy_weapon_speed"] = "Flat enemy weapon speed (ENEMY BASE → WEAPON SPEED).",
            ["enemy_weapon_damage"] = "Flat enemy weapon damage (ENEMY BASE → WEAPON DAMAGE).",
            ["weaken"] = "Apply WEAKEN status (ENEMY TARGET → WEAKEN). Strength of application is the cell value.",
            ["slow"] = "Apply SLOW status (ENEMY TARGET → SLOW).",
            ["vulnerability"] = "Apply VULNERABILITY (ENEMY TARGET → VULNERABILITY).",
            ["harden"] = "Apply HARDEN defensive buff (often self/target harden column).",
            ["focus"] = "Apply FOCUS buff.",
            ["confuse"] = "Apply CONFUSE.",
            ["stat_drain"] = "Apply STAT DRAIN.",
            ["fortify"] = "Apply FORTIFY (armor stacks). Prefer HARDEN/FOCUS for many self-buffs — FORTIFY may be display-only on some pulls.",
            ["disrupt"] = "DISRUPT combo/routing interrupt (DISRUPT column). Timing-agnostic.",
            ["heal"] = "Flat heal amount (HERO HEAL → HEAL). Timing-agnostic unless a trigger gates it.",
            ["max_health"] = "Permanent max HP gain (HERO HEAL → MAX HEALTH).",
            ["advantage"] = "Roll advantage (HIGHEST/LOWEST ROLL / multi-dice helpers).",
            ["disadvantage"] = "Roll disadvantage.",
            ["pierce"] = "PIERCE — ignore flat armor on this swing (PIERCE column).",
            ["self_damage"] = "Legacy self-damage concept — prefer target=self; may be needs-improvement.",
            ["strip_jump"] = "Fight-scoped: next combo advance jumps to slot N (Count or strip_jump:N).",
            ["strip_skip"] = "Fight-scoped: skip the next combo strip slot.",
            ["strip_repeat"] = "Fight-scoped: next advance returns to the previous strip slot.",
            ["strip_loop"] = "Fight-scoped: next advance loops to the opener.",
            ["strip_stop"] = "Fight-scoped: stop the combo early (reset to opener).",
            ["strip_random"] = "Fight-scoped: next combo slot is chosen at random among enabled slots.",
            ["strip_disable"] = "Fight-scoped: disable a strip slot for the rest of the fight.",
            ["strip_shuffle"] = "Fight-scoped: shuffle strip play order for the rest of the fight.",
            ["strip_replace_next"] = "One-shot: next swing resolves a named strip/pool action (strip_replace_next:NAME).",
            ["combo_jump"] = "Alias of strip_jump.",
            ["loop_chain"] = "Alias of strip_loop.",
            ["shuffle"] = "Alias of strip_shuffle.",
            ["replace_action"] = "Alias of strip_replace_next.",
            ["skip"] = "Alias of strip_skip.",
            ["retrigger_next"] = "Schedule a nested full resolve of the next strip slot (not Multihit).",
            ["retrigger_opener"] = "Schedule a nested full resolve of the opener / first strip slot.",
            ["retrigger_finisher"] = "Schedule a nested full resolve of the finisher / last strip slot.",
            ["retrigger_slot"] = "Schedule a nested full resolve of strip slot N (retrigger_slot:N).",
            ["salvage_miss"] = "Grant fight-scoped miss→hit salvage charges (once per charge).",
            ["crit_face_min"] = "Treat natural die faces ≥ N as crit for this fight (default 19).",
            ["replace_next_roll"] = "Force the next attack die face to N (replace_next_roll:N or Count).",
            ["exploding_dice"] = "When the die meets the exploding threshold, roll again and add (sheet EXPLODING DICE THRESHOLD).",
        };

        private static readonly Dictionary<string, string> RedundancyNotes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["hero_accuracy"] = "Same data as the ACCURACY column — do not also invent a second accuracy source.",
            ["hero_hit_threshold"] = "Same as HIT column under HERO DICE — MECHANICS id is a checklist pointer, not a second value.",
            ["hero_combo_threshold"] = "Same as COMBO column under HERO DICE.",
            ["hero_crit_threshold"] = "Same as CRIT column under HERO DICE.",
            ["hero_crit_miss_threshold"] = "Same as CRIT MISS column under HERO DICE.",
            ["hero_next_action_damage"] = "Same magnitude as DAMAGE MOD / ACTION DAMAGE under HERO BASE STATS — not the main DAMAGE(%) swing column.",
            ["hero_next_action_speed"] = "Same as ACTION SPEED under HERO BASE STATS — not SPEED(x) swing length.",
            ["hero_next_action_multihit"] = "Same as MULTIHIT MOD — not # OF HITS on the swing itself.",
            ["hero_next_action_amp"] = "Same as AMP_MOD column.",
            ["enemy_accuracy"] = "Enemy mirror of hero ACCURACY — use ENEMY DICE columns for the value.",
            ["enemy_next_action_damage"] = "Enemy mirror of hero next-action damage — ENEMY BASE STATS magnitude.",
            ["weaken"] = "Status cell value is authority; MECHANICS listing only documents it.",
            ["heal"] = "HEAL column is authority; listing heal in MECHANICS / ON MISS → only wires when it applies.",
            ["fortify"] = "Often redundant with HARDEN for self defense — check pull docs before relying on FORTIFY combat apply.",
        };
    }
}
