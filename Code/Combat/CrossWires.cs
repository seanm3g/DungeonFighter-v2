using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using RPGGame.Combat.Events;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>One <c>IF</c> clause of a rule (see <see cref="CrossWireRule"/>).</summary>
    public sealed class RuleCondition
    {
        /// <summary><c>self</c> (hero value or store), <c>target</c> (target status or HP), <c>prefix</c> (enemy prefix tag),
        /// or the swing's action: <c>action</c> (name), <c>tag</c> (action tag), <c>class</c> (class-tagged action).</summary>
        public string Subject { get; init; } = "self";
        /// <summary>HP, STEP, SPENT, LENGTH, ROLL, DISTINCT, TURN, or a store id (RAGE, focus, bleed…).</summary>
        public string Key { get; init; } = "";
        /// <summary>&gt;=, &lt;=, &gt;, &lt;, =; empty = "is up" (value &gt; 0) / prefix present.</summary>
        public string Op { get; init; } = "";
        public double Value { get; init; }

        /// <summary>True for ACTION / TAG / CLASS: the condition reads the hero's swing, so it only holds when one is known.</summary>
        public bool ReadsAction => Subject is "action" or "tag" or "class";
    }

    /// <summary>
    /// Rules: the item / skill effect language (Documentation/BUILD_SYSTEM_CONDENSED.md §5-6; cross-wires are its first
    /// family). Written as data on skill nodes (<c>crossWire</c>) and item prefixes (Effect <c>CROSSWIRE</c> or
    /// <c>RULE</c>, field <c>crossWire</c>; several rules separated by <c>;</c>; <c>{n}</c> = the prefix's rolled value).
    /// <code>WHEN[:arg] [EVERY n] [IF cond [AND cond]…] -> VERB what n[%] [FOR fight|dungeon]
    /// WHILE [IF cond [AND cond]…] -> STAT ±n[%] [PER TARGET status]
    /// WHILE [IF cond …] -> POTENCY status ±n%</code>
    /// Swing conditions (base item modifiers): <c>IF ACTION Name_With_Underscores</c>, <c>IF TAG tag</c>,
    /// <c>IF CLASS warrior</c>. POTENCY scales the damage of the hero's bleed / burn / poison / acid ticks on foes.
    /// <list type="bullet">
    /// <item>WHEN: any event token, hub (ONCASH:RAGE, ONBEAT, ONSTATUSAPPLY:bleed…) or combat (ONKILL, ONCONNECT,
    /// ONTAKEHIT…). EVERY n fires on every n-th match this fight.</item>
    /// <item>IF: <c>RAGE&gt;=3</c> (hero store), <c>HP&lt;50</c> (hero HP %), <c>STEP&gt;=2</c> (combo step),
    /// <c>SPENT&gt;=10</c> (event amount), <c>LENGTH&lt;0.7</c> (action length), <c>ROLL&gt;=15</c> (natural roll),
    /// <c>DISTINCT&gt;=4</c> (worn materials), <c>TURN&gt;=3</c>, <c>SLOT&gt;=3</c> (the swing's 1-based strip slot),
    /// <c>SLOTS&gt;=5</c> (strip length); <c>TARGET bleed</c> / <c>TARGET bleed&gt;=3</c> /
    /// <c>TARGET HP&lt;30</c>; <c>PREFIX undead</c> (enemy prefix tag).</item>
    /// <item>Triggered verbs: <c>MINT</c> (hero mint), <c>ADD</c> (hero resource: harden, fortify, focus, temphp % max
    /// HP), <c>APPLY</c> (target status), <c>HEAL n%</c> (of the event's damage; plain n = % max HP), <c>REFLECT n%</c>
    /// (of damage taken, to the attacker), <c>BUFF stat n FOR fight|dungeon</c> (STR, AGI, TECH, INT, HIT, COMBO,
    /// CRIT, ACCURACY).</item>
    /// <item>Passive (WHILE) stats: <c>DAMAGE ±n%</c> (outgoing), <c>ARMOR ±n%</c>, <c>SPEED ±n%</c> (+ = faster),
    /// <c>HIT</c> / <c>COMBO</c> / <c>CRIT</c> ±n (roll bonuses). <c>PER TARGET status</c> multiplies by its stacks.</item>
    /// <item>Alias (skill family): <c>ONMISS -> COUNTAS ONTAKEHIT</c>: a combat event also counts as another for
    /// material mints, Listeners and rules (combat events only, one hop).</item>
    /// <item>Exemption (skill family): <c>WHILE [IF …] -> EXEMPT STREAK|COMBO</c>: misses don't reset the hit streak /
    /// don't end the combo.</item>
    /// <item>Threshold (skill family) needs no verb: <c>ONSTOREFULL:RAGE -> …</c>.</item>
    /// </list>
    /// </summary>
    public sealed class CrossWireRule
    {
        public string When { get; init; } = "";
        public int Every { get; init; }
        public List<RuleCondition> Conditions { get; init; } = new();
        /// <summary>First hero-store <c>&gt;=</c> condition (kept for callers that read it directly).</summary>
        public string? IfStore { get; init; }
        public int IfAtLeast { get; init; }
        /// <summary>First <c>TARGET status</c> condition.</summary>
        public string? IfTargetStatus { get; init; }
        public string Verb { get; init; } = "";
        /// <summary>Store, status or stat the verb acts on (empty for HEAL / REFLECT).</summary>
        public string Store { get; init; } = "";
        public double Amount { get; init; }
        public bool Percent { get; init; }
        /// <summary>BUFF scope: fight (default) or dungeon.</summary>
        public string Scope { get; init; } = "fight";
        /// <summary>Passive PER TARGET status: the value is multiplied by the target's stacks.</summary>
        public string? PerTargetStatus { get; init; }
        public string Source { get; init; } = "";
        /// <summary>The rule text it was parsed from (EVERY counters key on it).</summary>
        public string Text { get; init; } = "";

        public bool IsPassive => When.Equals("WHILE", StringComparison.OrdinalIgnoreCase);

        public static readonly string[] TriggeredVerbs = { "MINT", "ADD", "APPLY", "HEAL", "REFLECT", "BUFF" };
        public static readonly string[] PassiveStats = { "DAMAGE", "ARMOR", "SPEED", "HIT", "COMBO", "CRIT", "POTENCY" };
        /// <summary>Statuses POTENCY can scale (the hero's damage-over-time on foes).</summary>
        public static readonly string[] PotencyStatuses = { "bleed", "burn", "poison", "acid" };
        public static readonly string[] BuffStats = { "STR", "AGI", "TECH", "TEC", "INT", "HIT", "COMBO", "CRIT", "ACCURACY" };
        public static readonly string[] Exemptions = { "STREAK", "COMBO" };
        private static readonly string[] SelfKeys = { "HP", "STEP", "SLOT", "SLOTS", "SPENT", "LENGTH", "ROLL", "DISTINCT", "TURN" };
        private static readonly Regex Compare = new(@"^([A-Za-z_]+)(>=|<=|>|<|=)(-?[0-9]+(?:\.[0-9]+)?)$", RegexOptions.Compiled);

        public static CrossWireRule? Parse(string? spec, string source = "")
        {
            if (string.IsNullOrWhiteSpace(spec))
                return null;
            int arrow = spec.IndexOf("->", StringComparison.Ordinal);
            if (arrow < 0)
                return null;
            var left = spec[..arrow].Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            var right = spec[(arrow + 2)..].Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (left.Length == 0 || right.Length < 2)
                return null;
            bool passive = left[0].Equals("WHILE", StringComparison.OrdinalIgnoreCase);
            if (!passive && !TriggerEvents.IsEvent(left[0]))
                return null;

            int i = 1, every = 0;
            if (i + 1 < left.Length && left[i].Equals("EVERY", StringComparison.OrdinalIgnoreCase))
            {
                if (passive || !int.TryParse(left[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out every) || every < 1)
                    return null;
                i += 2;
            }
            var conds = new List<RuleCondition>();
            if (i < left.Length)
            {
                if (!left[i].Equals("IF", StringComparison.OrdinalIgnoreCase))
                    return null;
                i++;
                while (i < left.Length)
                {
                    var cond = ParseCondition(left, ref i);
                    if (cond == null)
                        return null;
                    conds.Add(cond);
                    if (i < left.Length)
                    {
                        if (!left[i].Equals("AND", StringComparison.OrdinalIgnoreCase))
                            return null;
                        i++;
                    }
                }
            }

            string verb = right[0].ToUpperInvariant();
            if (verb is "COUNTAS" or "EXEMPT")
            {
                // COUNTAS <event> (triggered) / EXEMPT <rule> (passive): no amount.
                if (right.Length != 2 || (verb == "COUNTAS") == passive)
                    return null;
                string what = right[1].ToUpperInvariant();
                if (verb == "COUNTAS" ? !TriggerEvents.IsEvent(what) : !Exemptions.Contains(what))
                    return null;
                return new CrossWireRule
                {
                    When = passive ? "WHILE" : left[0], Every = every, Conditions = conds,
                    Verb = verb, Store = what, Amount = 1, Source = source, Text = spec.Trim()
                };
            }
            if (passive != PassiveStats.Contains(verb) || (!passive && !TriggeredVerbs.Contains(verb)))
                return null;

            // Right side shapes: VERB store n | HEAL n | REFLECT n% | BUFF stat n [FOR scope] | STAT n [PER TARGET status]
            string store = "";
            int amountAt = 1;
            if (verb is "MINT" or "ADD" or "APPLY" or "BUFF" or "POTENCY")
            {
                if (right.Length < 3)
                    return null;
                store = right[1];
                amountAt = 2;
                if (verb == "BUFF" && !BuffStats.Contains(store.ToUpperInvariant()))
                    return null;
                if (verb == "POTENCY")
                {
                    store = store.ToLowerInvariant();
                    if (!PotencyStatuses.Contains(store))
                        return null;
                }
            }
            else if (passive)
                store = verb;
            if (!TryAmount(right[amountAt], out double amount, out bool pct))
                return null;
            if (!passive && amount <= 0)
                return null;
            if (verb == "REFLECT" && !pct)
                return null;

            string scope = "fight";
            string? per = null;
            int k = amountAt + 1;
            if (k + 1 < right.Length + 1 && k < right.Length)
            {
                string word = right[k].ToUpperInvariant();
                if (word == "FOR" && verb == "BUFF" && k + 1 < right.Length)
                {
                    scope = right[k + 1].ToLowerInvariant();
                    if (scope is not ("fight" or "dungeon"))
                        return null;
                }
                else if (word == "PER" && passive && k + 2 < right.Length && right[k + 1].Equals("TARGET", StringComparison.OrdinalIgnoreCase))
                    per = right[k + 2].ToLowerInvariant();
                else
                    return null;
            }

            var firstStore = conds.FirstOrDefault(c => c.Subject == "self" && c.Op == ">=" && !SelfKeys.Contains(c.Key.ToUpperInvariant()));
            var firstTarget = conds.FirstOrDefault(c => c.Subject == "target" && !c.Key.Equals("HP", StringComparison.OrdinalIgnoreCase));
            return new CrossWireRule
            {
                When = passive ? "WHILE" : left[0], Every = every, Conditions = conds,
                IfStore = firstStore?.Key, IfAtLeast = firstStore == null ? 0 : (int)firstStore.Value,
                IfTargetStatus = firstTarget?.Key,
                Verb = verb, Store = store, Amount = amount, Percent = pct, Scope = scope, PerTargetStatus = per,
                Source = source, Text = spec.Trim()
            };
        }

        private static bool TryAmount(string raw, out double amount, out bool pct)
        {
            pct = raw.EndsWith('%');
            return double.TryParse(raw.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out amount);
        }

        private static RuleCondition? ParseCondition(string[] t, ref int i)
        {
            string head = t[i];
            if ((head.Equals("ACTION", StringComparison.OrdinalIgnoreCase) || head.Equals("TAG", StringComparison.OrdinalIgnoreCase)
                    || head.Equals("CLASS", StringComparison.OrdinalIgnoreCase)) && i + 1 < t.Length)
            {
                string subject = head.ToLowerInvariant();
                string body = t[i + 1];
                i += 2;
                // Action names are written with underscores for spaces (one token).
                return new RuleCondition { Subject = subject, Key = subject == "action" ? body.Replace('_', ' ') : body.ToLowerInvariant() };
            }
            if ((head.Equals("TARGET", StringComparison.OrdinalIgnoreCase) || head.Equals("PREFIX", StringComparison.OrdinalIgnoreCase))
                && i + 1 < t.Length)
            {
                string subject = head.ToLowerInvariant();
                string body = t[i + 1];
                i += 2;
                if (subject == "prefix")
                    return new RuleCondition { Subject = "prefix", Key = body.ToLowerInvariant() };
                var m = Compare.Match(body);
                if (m.Success)
                    return new RuleCondition { Subject = "target", Key = KeyOf(m.Groups[1].Value), Op = m.Groups[2].Value, Value = Num(m.Groups[3].Value) };
                return new RuleCondition { Subject = "target", Key = body.ToLowerInvariant() };
            }
            var c = Compare.Match(head);
            if (!c.Success)
                return null;
            i++;
            return new RuleCondition { Subject = "self", Key = KeyOf(c.Groups[1].Value), Op = c.Groups[2].Value, Value = Num(c.Groups[3].Value) };
        }

        /// <summary>Self keys and HP upper-case; status ids lower-case; mints keep their spelling (RAGE).</summary>
        private static string KeyOf(string raw) =>
            SelfKeys.Contains(raw.ToUpperInvariant()) ? raw.ToUpperInvariant() : raw;

        private static double Num(string s) => double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    public static class CrossWires
    {
        /// <summary>Cross-class cross-wire prefixes (rare).</summary>
        public const string AffixEffect = "CROSSWIRE";
        /// <summary>General item rule prefixes (lifesteal, reflect, conditional damage, tilt…).</summary>
        public const string RuleEffect = "RULE";

        private static readonly Dictionary<string, CrossWireRule?> _parsed = new(StringComparer.Ordinal);
        private static readonly object _lock = new();
        private static readonly ConditionalWeakTable<Character, Dictionary<string, int>> _everyCounters = new();
        private static bool? _treesHavePassive;

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

        /// <summary>Rules carried by an item prefix (<c>;</c>-separated, <c>{n}</c> = rolled value).</summary>
        public static IEnumerable<CrossWireRule> RulesOf(Modification mod, string source)
        {
            if (string.IsNullOrWhiteSpace(mod.CrossWire)
                || !(AffixTemplates.Is(mod, AffixEffect) || AffixTemplates.Is(mod, RuleEffect)))
                yield break;
            string text = mod.CrossWire.Replace("{n}", RolledText(mod), StringComparison.Ordinal);
            foreach (string part in text.Split(';'))
            {
                var rule = Cached(part.Trim(), source);
                if (rule != null)
                    yield return rule;
            }
        }

        private static string RolledText(Modification mod)
        {
            double v = mod.RolledValue > 0 ? mod.RolledValue : mod.MinValue;
            return ((int)Math.Round(v, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Card text for a RULE / CROSSWIRE prefix with its rolled value: its rules, described.</summary>
        public static string? DescribeMod(Modification mod)
        {
            var rules = RulesOf(mod, mod.Name ?? "").ToList();
            return rules.Count == 0 ? null : string.Join("; ", rules.Select(Describe));
        }

        /// <summary>Active rules: learned skill nodes with a rule, then equipped CROSSWIRE / RULE prefixes.</summary>
        public static List<CrossWireRule> Active(Character hero) => Collect(hero, includeNodes: true);

        private static List<CrossWireRule> Collect(Character hero, bool includeNodes)
        {
            var list = new List<CrossWireRule>();
            if (includeNodes)
            {
                var trees = SkillTreeService.Trees;
                foreach (var kv in hero.Progression.LearnedSkillRanks)
                {
                    if (kv.Value <= 0)
                        continue;
                    var node = trees?.GetNode(kv.Key);
                    if (node == null || string.IsNullOrWhiteSpace(node.CrossWire))
                        continue;
                    foreach (string part in node.CrossWire.Split(';'))
                    {
                        var rule = Cached(part.Trim(), node.Name);
                        if (rule != null)
                            list.Add(rule);
                    }
                }
            }
            foreach (var item in MaterialSetController.EnumerateEquipped(hero))
            {
                if (item == null)
                    continue;
                // The base item's modifier (Item.ImplicitRule, e.g. "+12% damage with Cleave"); high tiers hold two rules.
                if (!string.IsNullOrWhiteSpace(item.ImplicitRule))
                {
                    foreach (string part in item.ImplicitRule.Split(';'))
                    {
                        if (string.IsNullOrWhiteSpace(part))
                            continue;
                        var implicitRule = Cached(part.Trim(), item.Name);
                        if (implicitRule != null)
                            list.Add(implicitRule);
                    }
                }
                if (item.Modifications == null)
                    continue;
                foreach (var mod in item.Modifications)
                    if (mod != null)
                        list.AddRange(RulesOf(mod, item.Name));
            }
            return list;
        }

        // ------------------------------------------------------------------ triggered rules

        /// <summary>Hub events (ONCASH, ONBEAT, ONSTATUSAPPLY, ONSTOREFULL, ONSELFCOST, ONENEMYACT).</summary>
        public static void OnHub(Character hero, string token, string? arg, int amount)
        {
            foreach (var rule in Active(hero))
                if (!rule.IsPassive && HeroEventHub.Matches(rule.When, token, arg))
                    Fire(hero, rule, amount, null);
        }

        /// <summary>Combat-pipeline events, matched with the minting path's WHEN check.</summary>
        /// <param name="whenMatches">Already gated for take-hit vs own events, aliases included (MaterialSetController).</param>
        internal static void OnCombatEvent(Character hero, Func<string, bool> whenMatches, bool takeHitOnly, CombatEvent? evt = null)
        {
            foreach (var rule in Active(hero))
            {
                if (rule.IsPassive || rule.Verb == "COUNTAS" || !TriggerEvents.TryGet(rule.When, out var def) || !def.Emitted)
                    continue;
                if (whenMatches(rule.When))
                    Fire(hero, rule, evt?.Damage ?? 0, evt, takeHitOnly);
            }
        }

        /// <summary>New fight: EVERY counters restart.</summary>
        public static void ResetFight(Character? hero)
        {
            if (hero != null)
                _everyCounters.Remove(hero);
        }

        private static void Fire(Character hero, CrossWireRule rule, int eventAmount, CombatEvent? evt, bool takeHit = false)
        {
            if (rule.Verb == "COUNTAS")
                return;
            var target = takeHit ? evt?.Source ?? HeroEventHub.CurrentTarget(hero) : evt?.Target ?? HeroEventHub.CurrentTarget(hero);
            if (ReferenceEquals(target, hero))
                target = HeroEventHub.CurrentTarget(hero);
            if (!ConditionsHold(hero, target, rule, eventAmount, evt))
                return;
            if (rule.Every > 1)
            {
                var counters = _everyCounters.GetOrCreateValue(hero);
                int seen = counters.GetValueOrDefault(rule.Source + "|" + rule.Text) + 1;
                counters[rule.Source + "|" + rule.Text] = seen;
                if (seen % rule.Every != 0)
                    return;
            }

            switch (rule.Verb)
            {
                case "HEAL":
                {
                    int hp = rule.Percent
                        ? (int)Math.Floor(Math.Max(0, eventAmount) * rule.Amount / 100.0 + 1e-9)
                        : (int)Math.Round(hero.GetEffectiveMaxHealth() * rule.Amount / 100.0);
                    if (hp > 0 && hero.CurrentHealth > 0)
                        hero.Heal(hp);
                    return;
                }
                case "REFLECT":
                {
                    int dmg = (int)Math.Floor(Math.Max(0, eventAmount) * rule.Amount / 100.0 + 1e-9);
                    if (dmg > 0 && target != null && !ReferenceEquals(target, hero))
                        ActionUtilities.ApplyDamage(target, dmg);
                    return;
                }
                case "BUFF":
                {
                    int n = (int)Math.Round(rule.Amount);
                    if (n == 0)
                        return;
                    string stat = rule.Store.ToUpperInvariant() == "TEC" ? "TECH" : rule.Store.ToUpperInvariant();
                    var bonus = new[] { new ActionAttackBonusItem { Type = stat, Value = n } };
                    if (rule.Scope == "dungeon")
                        hero.DungeonCadenceBuffs.MergeAdditively(bonus);
                    else
                        hero.FightCadenceBuffs.MergeAdditively(bonus);
                    return;
                }
            }

            int units = rule.Percent
                ? (int)Math.Floor(Math.Max(0, eventAmount) * rule.Amount / 100.0 + 1e-9)
                : (int)Math.Round(rule.Amount);
            if (units <= 0)
                return;
            switch (rule.Verb)
            {
                case "MINT":
                {
                    string keyword = rule.Store.ToUpperInvariant();
                    if (MaterialBuildsLoader.FindByKeyword(keyword) != null)
                        MaterialSetController.MintKeyword(hero, keyword, units, $"{rule.Source} · rule");
                    break;
                }
                case "ADD":
                    if (rule.Store.Equals("temphp", StringComparison.OrdinalIgnoreCase))
                        units = Math.Max(1, (int)Math.Round(hero.GetEffectiveMaxHealth() * units / 100.0)); // temp HP is % max HP
                    Stores.Add(hero, rule.Store, units);
                    break;
                case "APPLY":
                    ApplyToTarget(target, rule.Store.ToLowerInvariant(), units);
                    break;
            }
        }

        // ------------------------------------------------------------------ conditions

        public static bool ConditionsHold(Character hero, Actor? target, CrossWireRule rule, int eventAmount = 0, CombatEvent? evt = null,
            Action? action = null)
        {
            foreach (var c in rule.Conditions)
                if (!Holds(hero, target, c, eventAmount, evt, action ?? evt?.Action))
                    return false;
            return true;
        }

        private static bool Holds(Character hero, Actor? target, RuleCondition c, int eventAmount, CombatEvent? evt, Action? action)
        {
            switch (c.Subject)
            {
                case "action":
                    return action != null && string.Equals(action.Name?.Trim(), c.Key, StringComparison.OrdinalIgnoreCase);
                case "tag":
                    return action?.Tags != null && action.Tags.Any(t => string.Equals(t?.Trim(), c.Key, StringComparison.OrdinalIgnoreCase));
                case "class":
                    return action != null && GearActionPools.IsClassActionFor(action.Tags, new[] { c.Key });
                case "prefix":
                    return target is Enemy e && e.PrefixTags.Any(t => string.Equals(t, c.Key, StringComparison.OrdinalIgnoreCase));
                case "target":
                {
                    if (target == null)
                        return false;
                    double v = c.Key == "HP"
                        ? HealthPercent(target)
                        : Stores.Get(target, c.Key.ToLowerInvariant());
                    return c.Op.Length == 0 ? v > 0 : Compare(v, c.Op, c.Value);
                }
                default:
                {
                    double v = c.Key switch
                    {
                        "HP" => HealthPercent(hero),
                        "STEP" => hero.ComboStep,
                        "SLOT" => StripSlot(hero, action),
                        "SLOTS" => ActionUtilities.GetComboActions(hero).Count,
                        "SPENT" => eventAmount,
                        "LENGTH" => evt?.Action?.Length ?? 0,
                        "ROLL" => evt?.NaturalRollValue ?? 0,
                        "DISTINCT" => Attunement.CountDistinctMaterials(hero),
                        "TURN" => HeroEventHub.FightTurns(hero),
                        _ => Stores.Get(hero, c.Key)
                    };
                    return c.Op.Length == 0 ? v > 0 : Compare(v, c.Op, c.Value);
                }
            }
        }

        /// <summary>1-based strip slot of <paramref name="action"/> (else the hero's current combo slot); 0 with no strip.</summary>
        private static double StripSlot(Character hero, Action? action)
        {
            var combo = ActionUtilities.GetComboActions(hero);
            if (combo.Count == 0)
                return 0;
            int idx = action != null ? ActionUtilities.TryGetComboActionSlotIndex(action, combo) : -1;
            if (idx < 0)
                idx = ((hero.ComboStep % combo.Count) + combo.Count) % combo.Count;
            return idx + 1;
        }

        private static double HealthPercent(Actor actor) =>
            actor is Character ch ? ch.GetHealthPercentage() * 100.0 : 100.0;

        private static bool Compare(double v, string op, double x) => op switch
        {
            ">=" => v >= x,
            "<=" => v <= x,
            ">" => v > x,
            "<" => v < x,
            "=" => Math.Abs(v - x) < 1e-9,
            _ => false
        };

        // ------------------------------------------------------------------ alias / exemption

        /// <summary>Active COUNTAS rules (From = rule.When, To = rule.Store). Empty for most heroes.</summary>
        public static List<CrossWireRule> Aliases(Character hero) =>
            Active(hero).Where(r => r.Verb == "COUNTAS").ToList();

        /// <summary>True while an active <c>EXEMPT what</c> rule's conditions hold (e.g. misses don't reset STREAK).</summary>
        public static bool Exempt(Character? hero, string what, Actor? target = null)
        {
            if (hero == null || hero is Enemy)
                return false;
            foreach (var rule in Collect(hero, includeNodes: true))
            {
                if (rule.Verb != "EXEMPT" || !rule.Store.Equals(what, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (ConditionsHold(hero, target ?? HeroEventHub.CurrentTarget(hero), rule))
                    return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ passive (WHILE) rules

        private static bool TreesHavePassive()
        {
            if (_treesHavePassive is bool known)
                return known;
            bool any = SkillTreeService.Trees?.Trees.SelectMany(t => t.Nodes)
                .Any(n => n.CrossWire?.TrimStart().StartsWith("WHILE", StringComparison.OrdinalIgnoreCase) == true) ?? false;
            _treesHavePassive = any;
            return any;
        }

        /// <summary>
        /// Sum of the hero's passive rules for <paramref name="stat"/> (DAMAGE / ARMOR / SPEED in %, HIT / COMBO /
        /// CRIT in roll points) whose conditions hold against <paramref name="target"/> (default: current target).
        /// </summary>
        /// <param name="action">The swing, for ACTION / TAG / CLASS conditions; rules with those conditions are skipped
        /// when it is null (attack speed and roll bonuses are computed without a swing).</param>
        /// <param name="actionOnly">Only rules with a swing condition (for callers that already counted the others).</param>
        public static double Passive(Character? hero, string stat, Actor? target = null, Action? action = null, bool actionOnly = false)
        {
            if (hero == null || hero is Enemy)
                return 0;
            double sum = 0;
            bool resolved = false;
            foreach (var rule in Collect(hero, TreesHavePassive()))
            {
                if (!rule.IsPassive || !rule.Verb.Equals(stat, StringComparison.OrdinalIgnoreCase))
                    continue;
                bool readsAction = rule.Conditions.Any(c => c.ReadsAction);
                if ((actionOnly && !readsAction) || (action == null && readsAction))
                    continue;
                if (!resolved)
                {
                    target ??= HeroEventHub.CurrentTarget(hero);
                    resolved = true;
                }
                if (!ConditionsHold(hero, target, rule, action: action))
                    continue;
                double v = rule.Amount;
                if (rule.PerTargetStatus != null)
                    v *= target == null ? 0 : Stores.Get(target, rule.PerTargetStatus);
                sum += v;
            }
            return sum;
        }

        /// <summary>Swing-scoped SPEED rules (IF ACTION / TAG / CLASS) for <paramref name="action"/>, in % (+ = faster), floored at -50.
        /// The unscoped SPEED rules are already in the hero's attack speed.</summary>
        public static double ActionSpeedPercent(Character? hero, Action? action) =>
            action == null || hero == null || hero is Enemy ? 0 : Math.Max(-50, Passive(hero, "SPEED", null, action, actionOnly: true));

        /// <summary>POTENCY rules for <paramref name="status"/> (bleed / burn / poison / acid), in %: how much harder the hero's
        /// ticks of it hit a foe. <paramref name="action"/> is the swing that applied it (for swing conditions).</summary>
        public static double Potency(Character? hero, string status, Action? action = null, Actor? target = null)
        {
            if (hero == null || hero is Enemy || string.IsNullOrWhiteSpace(status))
                return 0;
            double sum = 0;
            foreach (var rule in Collect(hero, TreesHavePassive()))
            {
                if (!rule.IsPassive || rule.Verb != "POTENCY" || !rule.Store.Equals(status, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (action == null && rule.Conditions.Any(c => c.ReadsAction))
                    continue;
                if (ConditionsHold(hero, target ?? HeroEventHub.CurrentTarget(hero), rule, action: action))
                    sum += rule.Amount;
            }
            return sum;
        }

        /// <summary>Every POTENCY status for the hero in one pass (null when none apply).</summary>
        public static Dictionary<string, double>? PotencyAll(Character? hero, Action? action, Actor? target)
        {
            if (hero == null || hero is Enemy)
                return null;
            Dictionary<string, double>? map = null;
            foreach (var rule in Collect(hero, TreesHavePassive()))
            {
                if (!rule.IsPassive || rule.Verb != "POTENCY")
                    continue;
                if (action == null && rule.Conditions.Any(c => c.ReadsAction))
                    continue;
                if (!ConditionsHold(hero, target, rule, action: action))
                    continue;
                map ??= new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                map[rule.Store] = (map.TryGetValue(rule.Store, out double v) ? v : 0) + rule.Amount;
            }
            return map;
        }

        /// <summary>Statuses rules can put on the target. DoTs add to their intensity (their handlers tick it).</summary>
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
                case "stun":
                    target.IsStunned = true;
                    target.StunTurnsRemaining = Math.Max(target.StunTurnsRemaining, n);
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

        // ------------------------------------------------------------------ text

        /// <summary>Card line, e.g. <c>On RAGE spent: +50% of it as CRISIS</c>.</summary>
        public static string Describe(CrossWireRule rule)
        {
            string cond = rule.Conditions.Count == 0 ? "" : " " + string.Join(" and ", rule.Conditions.Select(DescribeCondition));
            string sign = rule.Amount >= 0 ? "+" : "";
            if (rule.Verb == "EXEMPT")
            {
                string lead = cond.Length > 0 ? $"While{cond}: " : "";
                return lead + (rule.Store == "STREAK" ? "misses don't reset your hit streak" : "misses don't end your combo");
            }
            if (rule.Verb == "COUNTAS")
                return $"{TriggerEvents.Label(rule.When) ?? rule.When}{cond} also counts as {(TriggerEvents.Label(rule.Store) ?? rule.Store).ToLowerInvariant()}";
            if (rule.IsPassive && rule.Verb == "POTENCY")
            {
                string lead = cond.Length > 0 ? $"While{cond}: " : "";
                return $"{lead}{sign}{rule.Amount:0.#}% {rule.Store} damage";
            }
            if (rule.IsPassive && rule.Conditions.Count > 0 && rule.Conditions.All(c => c.ReadsAction))
            {
                // Base item modifiers read as "+12% damage with Cleave".
                string unit = rule.Percent ? "%" : "";
                string statName = rule.Verb switch
                {
                    "DAMAGE" => "damage", "ARMOR" => "armor", "SPEED" => "speed", "HIT" => "to hit", "COMBO" => "to combo", _ => "to crit"
                };
                return $"{sign}{rule.Amount:0.#}{unit} {statName} {string.Join(" and ", rule.Conditions.Select(DescribeCondition))}";
            }
            if (rule.IsPassive)
            {
                string unit = rule.Percent ? "%" : "";
                string stat = rule.Verb switch
                {
                    "DAMAGE" => "damage",
                    "ARMOR" => "armor",
                    "SPEED" => "speed",
                    "HIT" => "to hit",
                    "COMBO" => "to combo",
                    _ => "to crit"
                };
                string per = rule.PerTargetStatus != null ? $" per {rule.PerTargetStatus} on the target" : "";
                string lead = cond.Length > 0 ? $"While{cond}: " : "";
                return $"{lead}{sign}{rule.Amount:0.#}{unit} {stat}{per}";
            }
            string when = TriggerEvents.Label(rule.When) ?? rule.When;
            string every = rule.Every > 1 ? $" (every {rule.Every})" : "";
            string amount = rule.Percent ? $"{rule.Amount:0.#}% of it" : $"{rule.Amount:0.#}";
            string what = rule.Verb switch
            {
                "MINT" => $"+{amount} {rule.Store.ToUpperInvariant()}",
                "ADD" => $"+{amount} {rule.Store}",
                "HEAL" => rule.Percent ? $"heal {rule.Amount:0.#}% of the damage" : $"heal {rule.Amount:0.#}% max HP",
                "REFLECT" => $"deal {rule.Amount:0.#}% of it back",
                "BUFF" => $"+{rule.Amount:0.#} {rule.Store.ToUpperInvariant()} for the {rule.Scope}",
                _ => $"{amount} {rule.Store} on the target"
            };
            return $"{when}{every}{cond}: {what}";
        }

        private static string DescribeCondition(RuleCondition c)
        {
            string cmp = c.Op.Length == 0 ? "" : $" {c.Op.Replace(">=", "≥").Replace("<=", "≤")} {c.Value:0.##}";
            return c.Subject switch
            {
                "prefix" => $"vs {c.Key}",
                "action" => $"with {c.Key}",
                "tag" => $"with {c.Key} actions",
                "class" => $"with {CultureInfo.InvariantCulture.TextInfo.ToTitleCase(c.Key)} actions",
                "target" => c.Key == "HP" ? $"target HP{cmp}%" : c.Op.Length == 0 ? $"vs a target with {c.Key}" : $"target {c.Key}{cmp}",
                _ => c.Key switch
                {
                    "HP" => $"your HP{cmp}%",
                    "STEP" => $"combo step{cmp}",
                    "SLOT" => $"in strip slot{cmp}",
                    "SLOTS" => $"strip length{cmp}",
                    "SPENT" => $"spending{cmp}",
                    "LENGTH" => $"action length{cmp}",
                    "ROLL" => $"roll{cmp}",
                    "DISTINCT" => $"materials worn{cmp}",
                    "TURN" => $"turn{cmp}",
                    _ => c.Op == ">=" ? $"with {c.Key} {c.Value:0.##}+" : $"{c.Key}{cmp}"
                }
            };
        }
    }
}
