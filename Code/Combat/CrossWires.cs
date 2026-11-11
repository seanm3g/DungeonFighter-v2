using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPGGame.Combat.Events;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Cross-wires (Documentation/BUILD_SYSTEM_CONDENSED.md §6, skill family 8): a rule that turns one store's event
    /// into another store's gain. Written as data on skill nodes (<c>crossWire</c>, in-class) and rare item prefixes
    /// (<c>CROSSWIRE</c> effect, cross-class; decision 4):
    /// <code>WHEN[:arg] [IF cond] -> VERB store n[%]</code>
    /// <list type="bullet">
    /// <item>WHEN: any event token, hub (ONCASH:RAGE, ONBEAT, ONSTATUSAPPLY:bleed…) or combat (ONKILL, ONCONNECT…).</item>
    /// <item>IF: <c>STORE&gt;=n</c> (hero store, e.g. <c>CRISIS&gt;=1</c>) or <c>TARGET status</c> (target holds it).</item>
    /// <item>VERB: <c>MINT</c> (hero mint), <c>ADD</c> (hero resource: harden, fortify, focus, temphp),
    /// <c>APPLY</c> (target status: mark, expose, slow, bleed, burn).</item>
    /// <item>n%: percent of the event's amount (units spent for ONCASH); plain n is flat.</item>
    /// </list>
    /// </summary>
    public sealed class CrossWireRule
    {
        public string When { get; init; } = "";
        public string? IfStore { get; init; }
        public int IfAtLeast { get; init; }
        public string? IfTargetStatus { get; init; }
        public string Verb { get; init; } = "";
        public string Store { get; init; } = "";
        public double Amount { get; init; }
        public bool Percent { get; init; }
        public string Source { get; init; } = "";

        public static CrossWireRule? Parse(string? spec, string source = "")
        {
            if (string.IsNullOrWhiteSpace(spec))
                return null;
            int arrow = spec.IndexOf("->", StringComparison.Ordinal);
            if (arrow < 0)
                return null;
            var left = spec[..arrow].Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            var right = spec[(arrow + 2)..].Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (left.Length == 0 || right.Length < 3 || !TriggerEvents.IsEvent(left[0]))
                return null;

            string? ifStore = null, ifTarget = null;
            int atLeast = 0;
            if (left.Length >= 3 && left[1].Equals("IF", StringComparison.OrdinalIgnoreCase))
            {
                if (left[2].Equals("TARGET", StringComparison.OrdinalIgnoreCase) && left.Length >= 4)
                    ifTarget = left[3].ToLowerInvariant();
                else
                {
                    var cond = left[2].Split(">=");
                    if (cond.Length != 2 || !int.TryParse(cond[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out atLeast))
                        return null;
                    ifStore = cond[0];
                }
            }

            string verb = right[0].ToUpperInvariant();
            if (verb is not ("MINT" or "ADD" or "APPLY"))
                return null;
            string amountRaw = right[2];
            bool pct = amountRaw.EndsWith('%');
            if (!double.TryParse(amountRaw.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out double amount) || amount <= 0)
                return null;
            return new CrossWireRule
            {
                When = left[0], IfStore = ifStore, IfAtLeast = atLeast, IfTargetStatus = ifTarget,
                Verb = verb, Store = right[1], Amount = amount, Percent = pct, Source = source
            };
        }
    }

    public static class CrossWires
    {
        public const string AffixEffect = "CROSSWIRE";

        private static readonly Dictionary<string, CrossWireRule?> _parsed = new(StringComparer.Ordinal);
        private static readonly object _lock = new();

        private static CrossWireRule? Cached(string spec, string source)
        {
            lock (_lock)
            {
                string key = source + "|" + spec;
                if (!_parsed.TryGetValue(key, out var rule))
                {
                    rule = CrossWireRule.Parse(spec, source);
                    _parsed[key] = rule;
                }
                return rule;
            }
        }

        /// <summary>Active rules: learned skill nodes with a cross-wire, then equipped CROSSWIRE prefixes.</summary>
        public static List<CrossWireRule> Active(Character hero)
        {
            var list = new List<CrossWireRule>();
            var trees = SkillTreeService.Trees;
            foreach (var kv in hero.Progression.LearnedSkillRanks)
            {
                if (kv.Value <= 0)
                    continue;
                var node = trees?.GetNode(kv.Key);
                if (node == null || string.IsNullOrWhiteSpace(node.CrossWire))
                    continue;
                var rule = Cached(node.CrossWire, node.Name);
                if (rule != null)
                    list.Add(rule);
            }
            foreach (var item in MaterialSetController.EnumerateEquipped(hero))
            {
                if (item?.Modifications == null)
                    continue;
                foreach (var mod in item.Modifications)
                {
                    if (!AffixTemplates.Is(mod, AffixEffect) || string.IsNullOrWhiteSpace(mod.CrossWire))
                        continue;
                    var rule = Cached(mod.CrossWire, item.Name);
                    if (rule != null)
                        list.Add(rule);
                }
            }
            return list;
        }

        /// <summary>Hub events (ONCASH, ONBEAT, ONSTATUSAPPLY, ONSTOREFULL, ONSELFCOST, ONENEMYACT).</summary>
        public static void OnHub(Character hero, string token, string? arg, int amount)
        {
            foreach (var rule in Active(hero))
                if (HeroEventHub.Matches(rule.When, token, arg))
                    Fire(hero, rule, amount);
        }

        /// <summary>Combat-pipeline events, matched with the minting path's WHEN check.</summary>
        internal static void OnCombatEvent(Character hero, Func<string, bool> whenMatches, bool takeHitOnly)
        {
            foreach (var rule in Active(hero))
            {
                if (!TriggerEvents.TryGet(rule.When, out var def) || !def.Emitted)
                    continue;
                if (takeHitOnly != TriggerEvents.IsTakeHit(rule.When))
                    continue;
                if (whenMatches(rule.When))
                    Fire(hero, rule, 1);
            }
        }

        private static void Fire(Character hero, CrossWireRule rule, int eventAmount)
        {
            var target = HeroEventHub.CurrentTarget(hero);
            if (rule.IfStore != null && Stores.Get(hero, rule.IfStore) < rule.IfAtLeast)
                return;
            if (rule.IfTargetStatus != null && Stores.Get(target, rule.IfTargetStatus) <= 0)
                return;
            int n = rule.Percent
                ? (int)Math.Floor(Math.Max(0, eventAmount) * rule.Amount / 100.0 + 1e-9)
                : (int)Math.Round(rule.Amount);
            if (n <= 0)
                return;
            switch (rule.Verb)
            {
                case "MINT":
                {
                    string keyword = rule.Store.ToUpperInvariant();
                    if (MaterialBuildsLoader.FindByKeyword(keyword) != null)
                        MaterialSetController.MintKeyword(hero, keyword, n, $"{rule.Source} · cross-wire");
                    break;
                }
                case "ADD":
                    if (rule.Store.Equals("temphp", StringComparison.OrdinalIgnoreCase))
                        n = Math.Max(1, (int)Math.Round(hero.GetEffectiveMaxHealth() * n / 100.0)); // temp HP is % max HP
                    Stores.Add(hero, rule.Store, n);
                    break;
                case "APPLY":
                    ApplyToTarget(target, rule.Store.ToLowerInvariant(), n);
                    break;
            }
        }

        /// <summary>Statuses cross-wires can put on the target. DoTs add to their intensity (their handlers tick it).</summary>
        private static void ApplyToTarget(Actor? target, string status, int n)
        {
            if (target == null)
                return;
            switch (status)
            {
                case "bleed":
                    target.BleedIntensity += n;
                    break;
                case "burn":
                    target.BurnIntensity += n;
                    break;
                case "slow":
                    if (target is Character c)
                    {
                        // Same values as the Slow effect handler (Freeze config, fallback 0.5 / 3).
                        var freeze = GameConfiguration.Instance.StatusEffects.Freeze;
                        double reduction = freeze.SpeedReduction > 0 ? freeze.SpeedReduction : 0.5;
                        int turns = freeze.Duration > 0 ? (int)freeze.Duration : 3;
                        c.ApplySlow(reduction, turns);
                    }
                    break;
                default:
                    Stores.Add(target, status, n);
                    break;
            }
        }

        /// <summary>Card line, e.g. <c>On RAGE spent: +50% of it as CRISIS</c>.</summary>
        public static string Describe(CrossWireRule rule)
        {
            string when = TriggerEvents.Label(rule.When) ?? rule.When;
            string cond = rule.IfStore != null ? $" with {rule.IfStore} {rule.IfAtLeast}+"
                : rule.IfTargetStatus != null ? $" vs a target with {rule.IfTargetStatus}" : "";
            string amount = rule.Percent ? $"{rule.Amount:0.#}% of it" : $"{rule.Amount:0.#}";
            string what = rule.Verb switch
            {
                "MINT" => $"+{amount} {rule.Store.ToUpperInvariant()}",
                "ADD" => $"+{amount} {rule.Store}",
                _ => $"{amount} {rule.Store} on the target"
            };
            return $"{when}{cond}: {what}";
        }
    }
}
