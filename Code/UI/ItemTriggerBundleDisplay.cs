using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RPGGame.Actions.Conditional;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Player-facing labels for catalog <see cref="ActionTriggerBundle"/> lines on item tooltips.
    /// </summary>
    public static class ItemTriggerBundleDisplay
    {
        /// <summary>One-line summary, e.g. <c>Wound Momentum — On connect · ACTION → DAMAGE +10%</c>.</summary>
        public static string FormatSummary(ActionTriggerBundle? bundle)
        {
            if (bundle == null || !bundle.IsEnabled)
                return "";

            var sb = new StringBuilder();
            sb.Append(FormatWhen(bundle.When));

            if (bundle.Filters != null && bundle.Filters.Count > 0)
            {
                sb.Append(" (");
                sb.Append(string.Join(", ", bundle.Filters.Select(FormatFilter)));
                sb.Append(')');
            }

            string scope = (bundle.Scope ?? "").Trim();
            if (!string.IsNullOrEmpty(scope))
            {
                sb.Append(" · ");
                sb.Append(scope.ToUpperInvariant());
            }

            sb.Append(" → ");
            sb.Append(FormatMechanics(bundle.Mechanics, bundle.Value));

            string? identityName = TryMatchIdentityName(bundle);
            if (!string.IsNullOrEmpty(identityName))
                return $"{identityName} — {sb}";

            return sb.ToString();
        }

        public static IEnumerable<string> FormatSummaries(IEnumerable<ActionTriggerBundle>? bundles)
        {
            if (bundles == null)
                yield break;
            foreach (var b in bundles)
            {
                if (b == null || !b.IsEnabled)
                    continue;
                string s = FormatSummary(b);
                if (!string.IsNullOrEmpty(s))
                    yield return s;
            }
        }

        public static string FormatWhen(string? when)
        {
            if (string.IsNullOrWhiteSpace(when))
                return "On trigger";

            string upper = when.Trim().ToUpperInvariant();
            int colon = upper.IndexOf(':');
            string token = colon >= 0 ? upper.Substring(0, colon).Trim() : upper;
            string? arg = colon >= 0 && colon < upper.Length - 1 ? upper.Substring(colon + 1).Trim() : null;
            token = ActionTriggerGate.NormalizeToken(token);

            return token switch
            {
                "ONHIT" => "On normal hit",
                "ONCONNECT" => "On connect",
                "ONMISS" => "On miss",
                "ONCRITICAL" => "On crit",
                "ONCRITICALMISS" => "On crit miss",
                "ONCOMBO" => "On combo",
                "ONCOMBOEND" => "On combo end",
                "ONKILL" => "On kill",
                "ONFIRSTHIT" => "On first hit",
                "ONAFTERMISS" => "After miss",
                "ONROOMSCLEARED" => arg != null ? $"On room clear #{arg}" : "On room clear",
                "ONROLLVALUE" => arg != null ? $"On roll total {arg}" : "On exact roll",
                "ONNATURALROLL" => arg != null ? $"On natural {arg}" : "On natural roll",
                "ONEVEN" => "On even roll",
                "ONODD" => "On odd roll",
                "WHILEEQUIPPED" or "WHILE_EQUIPPED" => "While equipped",
                "ONHEALTHTHRESHOLD" => "On health threshold",
                _ => PrettifyToken(token)
            };
        }

        public static string FormatFilter(string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return "";
            string upper = filter.Trim().ToUpperInvariant();
            int colon = upper.IndexOf(':');
            string token = colon >= 0 ? upper.Substring(0, colon).Trim() : upper;
            string? arg = colon >= 0 && colon < upper.Length - 1 ? upper.Substring(colon + 1).Trim() : null;
            token = ActionTriggerGate.NormalizeToken(token);
            return token switch
            {
                "IFCLUTCH" => "clutch",
                "IFSOURCEUNDERDOT" => "while you have DoT",
                "IFTARGETUNDERDOT" => "vs DoT target",
                "IFSAMESACTION" => "same action",
                "IFDIFFERENTACTION" => "different action",
                "IFLASTENEMY" => "last enemy",
                "IFACTIONHASTAG" => arg != null ? $"action:{arg}" : "action tag",
                "IFGEARHASTAG" => arg != null ? $"gear:{arg}" : "gear tag",
                "IFTARGETHASTAG" => arg != null ? $"foe:{arg}" : "foe tag",
                "IFSLOT" => arg != null ? $"slot {arg}" : "combo slot",
                "IFUNARMED" => "unarmed",
                "IFCLASSTAG" => arg != null ? $"class:{arg}" : "class tag",
                _ => PrettifyToken(token)
            };
        }

        public static string FormatMechanics(string? mechanics, double? value)
        {
            if (string.IsNullOrWhiteSpace(mechanics))
                return "effect";

            var parts = mechanics
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Select(raw => FormatOneMechanic(raw, value));

            return string.Join(", ", parts);
        }

        private static string FormatOneMechanic(string raw, double? value)
        {
            int colon = raw.IndexOf(':');
            string id = colon > 0 ? raw.Substring(0, colon).Trim() : raw.Trim();
            string? arg = colon > 0 ? raw.Substring(colon + 1).Trim() : null;
            string normalized = ActionMechanicsRegistry.NormalizeMechanicId(id);
            string label = FriendlyMechanicLabel(normalized);

            if (!string.IsNullOrEmpty(arg)
                && (normalized.StartsWith("strip_", StringComparison.OrdinalIgnoreCase)
                    || normalized.StartsWith("retrigger_", StringComparison.OrdinalIgnoreCase)
                    || normalized is "crit_face_min" or "replace_next_roll" or "salvage_miss"))
            {
                label = $"{label} {arg}";
            }

            if (value is { } v && v != 0
                && normalized is not ("crit_face_min" or "replace_next_roll" or "salvage_miss")
                && !normalized.StartsWith("strip_", StringComparison.OrdinalIgnoreCase)
                && !normalized.StartsWith("retrigger_", StringComparison.OrdinalIgnoreCase)
                && !IsStatusMechanic(normalized))
            {
                string sign = v > 0 ? "+" : "";
                bool percent = normalized.Contains("next_action_speed", StringComparison.OrdinalIgnoreCase)
                    || normalized.Contains("next_action_damage", StringComparison.OrdinalIgnoreCase)
                    || normalized.Contains("next_action_amp", StringComparison.OrdinalIgnoreCase)
                    || normalized is "hero_action_damage";
                string qty = v.ToString("0.##", CultureInfo.InvariantCulture);
                label = percent ? $"{label} {sign}{qty}%" : $"{label} {sign}{qty}";
            }

            return label;
        }

        private static string FriendlyMechanicLabel(string normalized)
        {
            string fromRegistry = ActionMechanicsRegistry.GetDisplayLabel(normalized);
            if (!string.IsNullOrWhiteSpace(fromRegistry)
                && !string.Equals(fromRegistry, normalized, StringComparison.OrdinalIgnoreCase))
                return fromRegistry;

            return normalized switch
            {
                "crit_face_min" => "Crit face min",
                "salvage_miss" => "Miss salvage",
                "replace_next_roll" => "Replace next roll",
                "strip_shuffle" => "Shuffle strip",
                "strip_repeat" => "Repeat strip slot",
                "strip_skip" => "Skip strip slot",
                "strip_jump" => "Jump strip",
                "strip_loop" => "Loop strip",
                "strip_stop" => "Stop strip",
                "strip_random" => "Random strip",
                "strip_disable" => "Disable strip slot",
                "strip_replace_next" => "Replace next strip action",
                "retrigger_next" => "Retrigger next",
                "retrigger_opener" => "Retrigger opener",
                "retrigger_finisher" => "Retrigger finisher",
                "retrigger_slot" => "Retrigger slot",
                "hero_action_damage" => "This swing damage",
                "armor" or "hero_armor" => "Armor",
                "grant_action_tag" => "Grant action tag",
                "grant_action" => "Grant action",
                "expose" => "Expose",
                "heal" => "Heal",
                _ => PrettifyToken(normalized.Replace("hero_", "").Replace("enemy_", ""))
            };
        }

        private static bool IsStatusMechanic(string id) =>
            id is "expose" or "weaken" or "focus" or "harden" or "fortify" or "pierce"
                or "vulnerability" or "slow" or "heal" or "max_health" or "confuse" or "confusion"
                or "stat_drain" or "disrupt";

        private static string? TryMatchIdentityName(ActionTriggerBundle bundle)
        {
            foreach (var id in ItemTriggerIdentityCatalog.Identities)
            {
                if (!string.Equals(id.When, bundle.When, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.Equals(id.Scope ?? "", bundle.Scope ?? "", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.Equals(id.Mechanics, bundle.Mechanics, StringComparison.OrdinalIgnoreCase))
                    continue;
                return SplitCamel(id.Name);
            }

            return null;
        }

        private static string SplitCamel(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            var sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
                    sb.Append(' ');
                sb.Append(c);
            }

            return sb.ToString();
        }

        private static string PrettifyToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return "Trigger";
            string t = token;
            if (t.StartsWith("ON", StringComparison.OrdinalIgnoreCase))
                t = t.Substring(2);
            if (t.StartsWith("IF", StringComparison.OrdinalIgnoreCase))
                t = t.Substring(2);
            return t.ToLowerInvariant().Replace('_', ' ');
        }
    }
}
