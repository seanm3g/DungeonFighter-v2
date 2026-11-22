using System;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Action roles (Documentation/CORE_DESIGN_PASS_2026-11-17.md, D5.1): every card is a <b>Setup</b> (makes later
    /// slots stronger), a <b>Sequence</b> (acts on other actions: trigger, repeat, jump) or a <b>Payoff</b> (cashes in
    /// what came before; a plain attack is a payoff that cashes in only the combo multiplier and its class status).
    /// The <c>role</c> column in Actions.json is authored; an empty one is inferred here.
    /// </summary>
    public static class ActionRoles
    {
        public const string Setup = "setup";
        public const string Sequence = "sequence";
        public const string Payoff = "payoff";

        public static readonly string[] All = { Setup, Sequence, Payoff };

        public static bool IsRole(string? role) => All.Contains(Normalize(role));

        public static string Normalize(string? role) => (role ?? "").Trim().ToLowerInvariant();

        /// <summary>The action's role: its authored one, else inferred from its verb, name, tags and text.</summary>
        public static string Of(Action? action)
        {
            if (action == null)
                return Payoff;
            string authored = Normalize(action.Role);
            if (IsRole(authored))
                return authored;
            if (!string.IsNullOrWhiteSpace(action.Name) && ActionLoader.GetActionData(action.Name) is { } data
                && IsRole(data.Role))
                return Normalize(data.Role);
            return Infer(action.Verb, action.Name, action.Tags?.ToArray(), action.Description);
        }

        /// <summary>Feature rules (also used to seed the data column): payoff verbs, then strip / retrigger cards, then
        /// anything that buffs the next action or applies a status, else a payoff.</summary>
        public static string Infer(string? verb, string? name, string[]? tags, string? description)
        {
            string v = (verb ?? "").Trim().ToUpperInvariant();
            if (v.StartsWith("CASH", StringComparison.Ordinal) || v.StartsWith("DETONATE", StringComparison.Ordinal)
                || v.StartsWith("RELEASE", StringComparison.Ordinal))
                return Payoff;
            string n = (name ?? "").ToUpperInvariant();
            if (n.StartsWith("STRIP ", StringComparison.Ordinal) || n.StartsWith("RETRIGGER", StringComparison.Ordinal)
                || (tags ?? Array.Empty<string>()).Any(t => t is "routing" or "sequence"))
                return Sequence;
            string d = (description ?? "").Trim();
            if (v.StartsWith("APPLY", StringComparison.Ordinal) || d.StartsWith("Next ", StringComparison.OrdinalIgnoreCase))
                return Setup;
            return Payoff;
        }

        /// <summary>
        /// The card's one line (D5.1): role word, then the authored text, or for a card with none its verb
        /// ("Payoff · applies 1 Bleed").
        /// </summary>
        public static string CardLine(Action? action, string? description)
        {
            string label = Label(Of(action));
            // Fragile N (D5.4): the card says how many swings it has before it burns out.
            if (action is { Fragile: > 0 })
                label = $"{label} · Fragile {action.Fragile}";
            string body = (description ?? "").Trim().TrimStart('|').Trim();
            // The verb's status leads when the authored text doesn't already say it.
            if (VerbLine(action) is { } verb && body.IndexOf(verb, StringComparison.OrdinalIgnoreCase) < 0)
                body = body.Length == 0 ? verb : $"{verb} · {body}";
            return body.Length == 0 ? label : $"{label} · {body}";
        }

        /// <summary>"applies 1 Bleed" / "gains 1 Guard" for an <c>APPLY status n</c> verb; null otherwise.</summary>
        public static string? VerbLine(Action? action)
        {
            if (VerbPresets.GetSpec(action) is not { Kind: VerbKind.Apply } spec || !Stores.TryGet(spec.Store, out var def))
                return null;
            int n = Math.Max(1, (int)Math.Round(spec.Rate));
            // Guard is today's Harden (Phase 11, D11.12) until the statuses step renames it.
            string name = def.Id == "harden" ? "Guard" : def.Label;
            return def.Kind == StoreKind.Status ? $"applies {n} {name}" : $"gains {n} {name}";
        }

        /// <summary>Card word: "Setup", "Sequence", "Payoff".</summary>
        public static string Label(string? role)
        {
            string r = Normalize(role);
            return r.Length == 0 ? "Payoff" : char.ToUpperInvariant(r[0]) + r[1..];
        }
    }
}
