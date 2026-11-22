using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// The 7-status core (Documentation/CORE_DESIGN_PASS_2026-11-17.md, D3.13-D3.16, Phase 10 step 5): Stagger, Guard,
    /// Bleed, Burn, Focus, Rattle, Expose. Guard is today's Harden store (id <c>harden</c>, shown as Guard). Retired ids
    /// resolve to their replacement (the save migration table) wherever a status is applied or read through
    /// <see cref="Stores"/> or <see cref="EffectHandlerRegistry"/>; dropped ones apply nothing. Hero resources that are
    /// not statuses (Haste, Fortify, Temp HP, material stores, salvage) are untouched. The legacy status set and its
    /// switch were deleted in step 12; the alias table stays so old saves and data load.
    /// </summary>
    public static class CoreStatuses
    {
        public const string Guard = "harden";

        public static readonly string[] Core = { "stagger", Guard, "bleed", "burn", "focus", "rattle", "expose" };

        /// <summary>Kept beyond the core because a system needs it (D3.6): Mark (Glass), Haste (tempo grants).</summary>
        public static readonly string[] Kept = { "mark", "haste" };

        private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["guard"] = Guard, ["fortify"] = Guard, ["temphp"] = Guard, ["temporaryhp"] = Guard, ["absorb"] = Guard,
            ["acid"] = "expose", ["pierce"] = "expose", ["vulnerability"] = "expose", ["armorbreak"] = "expose",
            ["slow"] = "stagger", ["stun"] = "stagger",
            ["rollpenalty"] = "rattle", ["roll_penalty"] = "rattle",
            // Not in the migration table; Weaken (less damage dealt) reads closest to Rattle (worse rolls). REVIEW.
            ["weaken"] = "rattle",
            ["poison"] = "bleed",
            // Mark stays: the Glass material spends into it (a rung that needs it, D3.6); Brittle loads as Mark.
            ["brittle"] = "mark"
        };

        private static readonly HashSet<string> Dropped = new(StringComparer.OrdinalIgnoreCase)
        {
            "silence", "statdrain", "stat_drain", "reflect", "regen", "hpregen", "disrupt", "confusion"
        };

        public static bool IsCore(string? id) => Core.Contains((id ?? "").Trim().ToLowerInvariant());

        public static bool IsDropped(string? id) => Dropped.Contains((id ?? "").Trim());

        /// <summary>The id to use: its replacement for a retired status, null for a dropped one, else itself.</summary>
        public static string? Resolve(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return id;
            string key = id.Trim();
            if (Aliases.TryGetValue(key, out var to))
                return to;
            return Dropped.Contains(key) ? null : key;
        }

        /// <summary>Player-facing name ("Guard" for the harden store).</summary>
        public static string Label(string? id) => (Resolve(id) ?? id ?? "") switch
        {
            Guard => "Guard",
            var s when s.Length > 0 => char.ToUpperInvariant(s[0]) + s[1..],
            _ => ""
        };
    }
}
