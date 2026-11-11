using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Combat.Events
{
    /// <summary>
    /// Which gear slot's event family a trigger event belongs to (Documentation/BUILD_SYSTEM_CONDENSED.md §5):
    /// weapon = hit, head = dice, body = defense, legs = combo, feet = time. <see cref="Equip"/> is the passive channel.
    /// </summary>
    public enum TriggerEventFamily
    {
        Hit,
        Dice,
        Defense,
        Combo,
        Time,
        Equip
    }

    /// <summary>One trigger event: its canonical token, accepted aliases and player-facing text.</summary>
    public sealed class TriggerEventDef
    {
        public TriggerEventDef(string token, TriggerEventFamily family, string label, string description,
            string[]? aliases = null, string? argLabel = null, bool emitted = true)
        {
            Token = token;
            Family = family;
            Label = label;
            Description = description;
            Aliases = aliases ?? Array.Empty<string>();
            ArgLabel = argLabel;
            Emitted = emitted;
        }

        /// <summary>Canonical token, e.g. <c>ONTAKEHIT</c>.</summary>
        public string Token { get; }

        public TriggerEventFamily Family { get; }

        /// <summary>Short label for item / trigger cards, e.g. <c>On take hit</c>.</summary>
        public string Label { get; }

        /// <summary>Plain-English "what makes this happen", e.g. <c>each time an enemy hits you</c>.</summary>
        public string Description { get; }

        /// <summary>Other spellings that mean the same event (normalized form, no spaces / underscores).</summary>
        public IReadOnlyList<string> Aliases { get; }

        /// <summary>Label format when the token carries a <c>:arg</c>, with <c>{0}</c> for the argument.</summary>
        public string? ArgLabel { get; }

        /// <summary>True for events on the CombatEvent pipeline (action trigger bundles see them); false for hub events fired by <see cref="HeroEventHub"/> (materials, Listener affixes, cross-wires only).</summary>
        public bool Emitted { get; }
    }

    /// <summary>
    /// The single trigger event vocabulary. Trigger WHEN tokens (<c>ONCONNECT</c>), material synthesis tokens
    /// (<c>ON_RECEIVE_HIT</c>) and sheet labels (<c>ON CRIT</c>) all resolve here, so every system agrees on what an
    /// event is called and what it means. Older spellings stay valid as aliases.
    /// </summary>
    public static class TriggerEvents
    {
        public static IReadOnlyList<TriggerEventDef> All { get; } = new List<TriggerEventDef>
        {
            // Hit (weapon)
            new("ONCONNECT", TriggerEventFamily.Hit, "On connect", "each of your attacks that connects (normal, action or crit)", new[] { "ONANYHIT" }),
            new("ONHIT", TriggerEventFamily.Hit, "On normal hit", "each normal hit (not an action roll or crit)", new[] { "ONNORMALHIT" }),
            new("ONATTACK", TriggerEventFamily.Hit, "On attack", "each attack you make, hit or miss"),
            new("ONFIRSTHIT", TriggerEventFamily.Hit, "On first hit", "your first hit that lands each fight", new[] { "ONFIRSTBLOOD" }),
            new("ONKILL", TriggerEventFamily.Hit, "On kill", "each enemy you kill"),
            new("ONHEALTHTHRESHOLD", TriggerEventFamily.Hit, "On health threshold", "each time your hit pushes an enemy past a health milestone", new[] { "ONENEMYHEALTHTHRESHOLD" }),

            // Dice (head)
            new("ONCRITICAL", TriggerEventFamily.Dice, "On crit", "each critical hit", new[] { "ONCRIT", "ONCRITICALHIT" }),
            new("ONMISS", TriggerEventFamily.Dice, "On miss", "each attack you miss"),
            new("ONCRITICALMISS", TriggerEventFamily.Dice, "On crit miss", "each critical miss", new[] { "ONCRITMISS" }),
            new("ONAFTERMISS", TriggerEventFamily.Dice, "After miss", "each hit that lands right after a miss"),
            new("ONEVEN", TriggerEventFamily.Dice, "On even roll", "each even roll"),
            new("ONODD", TriggerEventFamily.Dice, "On odd roll", "each odd roll"),
            new("ONNATURALROLL", TriggerEventFamily.Dice, "On natural roll", "each natural roll of the given face", argLabel: "On natural {0}"),
            new("ONROLLVALUE", TriggerEventFamily.Dice, "On exact roll", "each roll totalling the given value", argLabel: "On roll total {0}"),
            new("ONFOCUS", TriggerEventFamily.Dice, "On focus", "each hit that gains you Focus"),

            // Defense (body)
            new("ONTAKEHIT", TriggerEventFamily.Defense, "On take hit", "each time an enemy hits you", new[] { "ONHEROHURT", "ONRECEIVEHIT" }),

            // Combo (legs)
            new("ONCOMBO", TriggerEventFamily.Combo, "On combo", "each action roll that lands", new[] { "ONCOMBOHIT" }),
            new("ONCOMBOEND", TriggerEventFamily.Combo, "On combo end", "each time your combo sequence ends", new[] { "ONCOMBOENDED" }),
            new("ONCONSECUTIVEATTACK", TriggerEventFamily.Combo, "On hit streak", "each hit that lands while you have 2+ hits in a row (a miss resets the run)"),

            // Time (feet)
            new("ONTURN", TriggerEventFamily.Time, "On turn", "at the start of each of your turns"),
            new("ONSLOW", TriggerEventFamily.Time, "On slow", "each hit that slows the target"),
            new("ONROOMSCLEARED", TriggerEventFamily.Time, "On room clear", "each room you clear", new[] { "ONROOMCLEARED" }, argLabel: "On room clear #{0}"),

            // Passive channel
            new("WHILEEQUIPPED", TriggerEventFamily.Equip, "While equipped", "while the item is worn", new[] { "ONEQUIP" }),

            // Hub events (HeroEventHub): materials, Listener affixes and skill cross-wires read them; action trigger bundles do not.
            new("ONSTATUSAPPLY", TriggerEventFamily.Hit, "On status applied", "each time you apply a status", argLabel: "On {0} applied", emitted: false),
            new("ONCASH", TriggerEventFamily.Combo, "On spend", "each time you spend a stored resource", argLabel: "On {0} spent", emitted: false),
            new("ONSTOREFULL", TriggerEventFamily.Combo, "On store full", "each time a stored resource reaches its cap", argLabel: "On {0} full", emitted: false),
            new("ONBEAT", TriggerEventFamily.Time, "On beat", "every n-th turn", argLabel: "Every {0}th turn", emitted: false),
            new("ONSELFCOST", TriggerEventFamily.Defense, "On self cost", "each time you pay HP, item condition or a status on yourself", emitted: false),
            new("ONENEMYACT", TriggerEventFamily.Time, "On enemy act", "each time the enemy takes an action", emitted: false),
        };

        private static readonly Dictionary<string, TriggerEventDef> ByToken = BuildIndex();

        private static Dictionary<string, TriggerEventDef> BuildIndex()
        {
            var map = new Dictionary<string, TriggerEventDef>(StringComparer.OrdinalIgnoreCase);
            foreach (var def in All)
            {
                map.Add(def.Token, def);
                foreach (var alias in def.Aliases)
                    map.Add(alias, def);
            }
            return map;
        }

        /// <summary>Uppercases and strips spaces and underscores (<c>ON_RECEIVE_HIT</c> → <c>ONRECEIVEHIT</c>).</summary>
        public static string Strip(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";
            return raw.Trim().ToUpperInvariant().Replace(" ", "").Replace("_", "");
        }

        /// <summary>
        /// Canonical event token for a raw spelling (<c>ON_RECEIVE_HIT</c> → <c>ONTAKEHIT</c>). Unknown tokens come back
        /// stripped but otherwise unchanged, so filter tokens (<c>IFCLUTCH</c>) pass through. Does not split <c>:arg</c>.
        /// </summary>
        public static string NormalizeToken(string? raw)
        {
            string t = Strip(raw);
            return ByToken.TryGetValue(t, out var def) ? def.Token : t;
        }

        /// <summary>Resolves a raw token (alias or canonical, optional <c>:arg</c>) to its event.</summary>
        public static bool TryGet(string? raw, out TriggerEventDef def, out string? arg)
        {
            def = null!;
            arg = null;
            string t = Strip(raw);
            if (t.Length == 0)
                return false;
            int colon = t.IndexOf(':');
            if (colon >= 0)
            {
                arg = colon < t.Length - 1 ? t.Substring(colon + 1) : null;
                t = t.Substring(0, colon);
            }
            if (!ByToken.TryGetValue(t, out var found))
                return false;
            def = found;
            return true;
        }

        public static bool TryGet(string? raw, out TriggerEventDef def) => TryGet(raw, out def, out _);

        /// <summary>True when the token names an event (any spelling), whether or not combat fires it yet.</summary>
        public static bool IsEvent(string? raw) => TryGet(raw, out _);

        /// <summary>True for the hero-takes-a-hit event in any spelling.</summary>
        public static bool IsTakeHit(string? raw) => TryGet(raw, out var def) && def.Token == "ONTAKEHIT";

        /// <summary>Card label, e.g. <c>On natural 7</c>; null when the token is not an event.</summary>
        public static string? Label(string? raw)
        {
            if (!TryGet(raw, out var def, out string? arg))
                return null;
            return arg != null && def.ArgLabel != null ? string.Format(def.ArgLabel, arg) : def.Label;
        }

        /// <summary>Plain-English trigger description; null when the token is not an event.</summary>
        public static string? Describe(string? raw) => TryGet(raw, out var def) ? def.Description : null;

        public static IEnumerable<TriggerEventDef> InFamily(TriggerEventFamily family) =>
            All.Where(d => d.Family == family);
    }
}
