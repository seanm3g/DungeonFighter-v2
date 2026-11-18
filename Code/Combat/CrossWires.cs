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
        /// <c>weapon</c> (equipped weapon type), or the swing's action: <c>action</c> (name), <c>tag</c> (action tag),
        /// <c>class</c> (class-tagged action), <c>prevtag</c> / <c>nexttag</c> (the strip neighbour before / after the
        /// swing's slot has the tag; the strip wraps).</summary>
        public string Subject { get; init; } = "self";
        /// <summary>HP, STEP, SLOT, SLOTS, EXTRASLOTS, SPENT, LENGTH, ROLL, DISTINCT, TURN, HITSTREAK, STR, AGI, TEC, INT,
        /// or a store id (RAGE, focus, bleed…).</summary>
        public string Key { get; init; } = "";
        /// <summary>&gt;=, &lt;=, &gt;, &lt;, =; empty = "is up" (value &gt; 0) / prefix present.</summary>
        public string Op { get; init; } = "";
        public double Value { get; init; }

        /// <summary>True for ACTION / TAG / CLASS: the condition reads the hero's swing, so it only holds when one is known.</summary>
        public bool ReadsAction => Subject is "action" or "tag" or "class" or "prevtag" or "nexttag";
    }

    /// <summary>
    /// <c>PER</c> clause of a rule: the amount is multiplied by a count (divided by <see cref="Div"/>, whole steps when
    /// Div &gt; 1). Sources: <c>TARGET status</c> (stacks on the target), <c>SELF key</c> or a bare self key (<c>PER SLOTS</c>,
    /// <c>PER SPENT</c>, <c>PER RAGE</c>), <c>STAT STR</c> (effective attribute), <c>WORN tag</c> (equipped pieces of a class's
    /// materials / a material / an item tag), <c>TAGS tag</c> (strip actions with the tag), <c>QUALITY a|b</c> (equipped pieces
    /// with one of those quality prefixes).
    /// </summary>
    public sealed class RulePer
    {
        public string Kind { get; init; } = "self";
        public string Arg { get; init; } = "";
        public int Div { get; init; } = 1;
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
        /// <summary>Any PER clause (TARGET status is mirrored in <see cref="PerTargetStatus"/>).</summary>
        public RulePer? Per { get; init; }
        /// <summary>CHANCE n%: the rule fires on that share of matches (0 = always).</summary>
        public double Chance { get; init; }
        /// <summary>ONCE: only the first match each fight fires.</summary>
        public bool Once { get; init; }
        /// <summary>NEXT: how many of your next rolls get the bonus (FOR n).</summary>
        public int Turns { get; init; } = 1;
        /// <summary>NEXT: RAMP / DECAY shape over <see cref="Turns"/>.</summary>
        public string? Shape { get; init; }
        /// <summary>NEXT: the bonus waits for (and only counts down on) an action with this tag (IFTAG tag).</summary>
        public string? IfTag { get; init; }
        /// <summary>Skill rank × node power the amount was scaled by (1 for items).</summary>
        public double Scale { get; init; } = 1;
        public string Source { get; init; } = "";
        /// <summary>The rule text it was parsed from (EVERY counters key on it).</summary>
        public string Text { get; init; } = "";

        public bool IsPassive => When.Equals("WHILE", StringComparison.OrdinalIgnoreCase);

        public static readonly string[] TriggeredVerbs = { "MINT", "ADD", "APPLY", "HEAL", "REFLECT", "BUFF", "NEXT" };
        public static readonly string[] PassiveStats =
            { "DAMAGE", "ARMOR", "SPEED", "HIT", "COMBO", "CRIT", "POTENCY", "AMP", "MULTIHIT", "STR", "AGI", "TEC", "INT" };
        /// <summary>Passive attribute stats (folded into the effective attribute getters).</summary>
        public static readonly string[] AttributeStats = { "STR", "AGI", "TEC", "INT" };
        /// <summary>What NEXT can grant on your next roll(s).</summary>
        public static readonly string[] NextStats =
            { "DAMAGE", "SPEED", "AMP", "MULTIHIT", "HIT", "COMBO", "CRIT", "ACCURACY", "STR", "AGI", "TEC", "TECH", "INT" };
        /// <summary>Statuses POTENCY can scale (the hero's damage-over-time on foes).</summary>
        public static readonly string[] PotencyStatuses = { "bleed", "burn", "poison", "acid" };
        public static readonly string[] BuffStats = { "STR", "AGI", "TECH", "TEC", "INT", "HIT", "COMBO", "CRIT", "ACCURACY" };
        public static readonly string[] Exemptions = { "STREAK", "COMBO" };
        internal static readonly string[] SelfKeys =
            { "HP", "STEP", "SLOT", "SLOTS", "EXTRASLOTS", "SPENT", "LENGTH", "ROLL", "DISTINCT", "TURN", "HITSTREAK", "STR", "AGI", "TEC", "INT" };
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

            // Left: WHEN [EVERY n] [ONCE] [CHANCE n%] [IF cond [AND cond]...]
            int i = 1, every = 0;
            bool once = false;
            double chance = 0;
            while (i < left.Length && !left[i].Equals("IF", StringComparison.OrdinalIgnoreCase))
            {
                string word = left[i].ToUpperInvariant();
                if (passive)
                    return null;
                if (word == "EVERY" && i + 1 < left.Length)
                {
                    if (!int.TryParse(left[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out every) || every < 1)
                        return null;
                    i += 2;
                }
                else if (word == "ONCE")
                {
                    once = true;
                    i++;
                }
                else if (word == "CHANCE" && i + 1 < left.Length)
                {
                    if (!TryAmount(left[i + 1], out chance, out _) || chance <= 0)
                        return null;
                    chance = Math.Min(1.0, chance / 100.0);
                    i += 2;
                }
                else
                    return null;
            }
            var conds = new List<RuleCondition>();
            if (i < left.Length)
            {
                i++; // IF
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
                    When = passive ? "WHILE" : left[0], Every = every, Once = once, Chance = chance, Conditions = conds,
                    Verb = verb, Store = what, Amount = 1, Source = source, Text = spec.Trim()
                };
            }
            if (verb is "TAGAS" or "RANK" or "SETCOUNT")
            {
                // Modifier verbs (2026-11-17), passive only: TAGAS tag (the swing's action also counts as that tag),
                // RANK node_id n (+n effective rank on a learned skill node), SETCOUNT n (this piece counts n more toward its
                // material set; item modifiers only).
                if (!passive)
                    return null;
                string what = verb == "SETCOUNT" ? "" : right[1].ToLowerInvariant();
                int amountIdx = verb switch { "TAGAS" => -1, "RANK" => 2, _ => 1 };
                if (right.Length != (verb == "TAGAS" ? 2 : amountIdx + 1))
                    return null;
                double n = 1;
                if (amountIdx > 0 && (!TryAmount(right[amountIdx], out n, out bool isPct) || isPct || n < 1))
                    return null;
                return new CrossWireRule
                {
                    When = "WHILE", Conditions = conds, Verb = verb, Store = what, Amount = n, Source = source, Text = spec.Trim()
                };
            }
            if (passive != PassiveStats.Contains(verb) || (!passive && !TriggeredVerbs.Contains(verb)))
                return null;

            // Right side shapes: VERB store n | HEAL n | REFLECT n% | BUFF stat n [FOR scope] | NEXT stat n [FOR k] [RAMP|DECAY]
            // [IFTAG tag] | STAT n, each optionally followed by PER <source> [n].
            string store = "";
            int amountAt = 1;
            if (verb is "MINT" or "ADD" or "APPLY" or "BUFF" or "POTENCY" or "NEXT")
            {
                if (right.Length < 3)
                    return null;
                store = right[1];
                amountAt = 2;
                if (verb == "BUFF" && !BuffStats.Contains(store.ToUpperInvariant()))
                    return null;
                if (verb == "NEXT")
                {
                    store = store.ToUpperInvariant();
                    if (!NextStats.Contains(store))
                        return null;
                }
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
            RulePer? per = null;
            int turns = 1;
            string? shape = null, ifTag = null;
            int k = amountAt + 1;
            while (k < right.Length)
            {
                string word = right[k].ToUpperInvariant();
                if (word == "FOR" && k + 1 < right.Length && verb == "BUFF")
                {
                    scope = right[k + 1].ToLowerInvariant();
                    if (scope is not ("fight" or "dungeon"))
                        return null;
                    k += 2;
                }
                else if (word == "FOR" && k + 1 < right.Length && verb == "NEXT")
                {
                    if (!int.TryParse(right[k + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out turns) || turns < 1)
                        return null;
                    k += 2;
                }
                else if (word is "RAMP" or "DECAY" && verb == "NEXT")
                {
                    shape = word.ToLowerInvariant();
                    k++;
                }
                else if (word == "IFTAG" && k + 1 < right.Length && verb == "NEXT")
                {
                    ifTag = right[k + 1].ToLowerInvariant();
                    k += 2;
                }
                else if (word == "PER" && per == null)
                {
                    per = ParsePer(right, ref k);
                    if (per == null)
                        return null;
                }
                else
                    return null;
            }

            var firstStore = conds.FirstOrDefault(c => c.Subject == "self" && c.Op == ">=" && !SelfKeys.Contains(c.Key.ToUpperInvariant()));
            var firstTarget = conds.FirstOrDefault(c => c.Subject == "target" && !c.Key.Equals("HP", StringComparison.OrdinalIgnoreCase));
            return new CrossWireRule
            {
                When = passive ? "WHILE" : left[0], Every = every, Once = once, Chance = chance, Conditions = conds,
                IfStore = firstStore?.Key, IfAtLeast = firstStore == null ? 0 : (int)firstStore.Value,
                IfTargetStatus = firstTarget?.Key,
                Verb = verb, Store = store, Amount = amount, Percent = pct, Scope = scope,
                PerTargetStatus = per?.Kind == "target" ? per.Arg : null, Per = per,
                Turns = turns, Shape = shape, IfTag = ifTag,
                Source = source, Text = spec.Trim()
            };
        }

        /// <summary>A copy with the amount multiplied by <paramref name="scale"/> (skill rank x node power).</summary>
        public CrossWireRule WithScale(double scale) => new()
        {
            When = When, Every = Every, Once = Once, Chance = Chance, Conditions = Conditions,
            IfStore = IfStore, IfAtLeast = IfAtLeast, IfTargetStatus = IfTargetStatus,
            Verb = Verb, Store = Store, Amount = Amount * scale, Percent = Percent, Scope = Scope,
            PerTargetStatus = PerTargetStatus, Per = Per, Turns = Turns, Shape = Shape, IfTag = IfTag,
            Source = Source, Text = Text, Scale = scale
        };

        private static RulePer? ParsePer(string[] t, ref int k)
        {
            // PER <source> [n]
            k++;
            if (k >= t.Length)
                return null;
            string head = t[k].ToUpperInvariant();
            string kind, arg;
            if (head is "TARGET" or "SELF" or "STAT" or "WORN" or "TAGS" or "QUALITY")
            {
                if (k + 1 >= t.Length)
                    return null;
                kind = head.ToLowerInvariant();
                arg = t[k + 1];
                if (kind == "stat")
                {
                    arg = arg.ToUpperInvariant() == "TECH" ? "TEC" : arg.ToUpperInvariant();
                    if (!AttributeStats.Contains(arg))
                        return null;
                }
                else if (kind == "self")
                    arg = KeyOf(arg);
                else if (kind != "quality")
                    arg = arg.ToLowerInvariant();
                k += 2;
            }
            else
            {
                // Bare self key or hero store: PER SLOTS, PER SPENT, PER RAGE.
                kind = "self";
                arg = KeyOf(t[k]);
                k++;
            }
            int div = 1;
            if (k < t.Length && int.TryParse(t[k], NumberStyles.Integer, CultureInfo.InvariantCulture, out int d) && d >= 1)
            {
                div = d;
                k++;
            }
            return new RulePer { Kind = kind, Arg = arg, Div = div };
        }

        private static bool TryAmount(string raw, out double amount, out bool pct)
        {
            pct = raw.EndsWith('%');
            return double.TryParse(raw.TrimEnd('%').TrimStart('+'), NumberStyles.Float, CultureInfo.InvariantCulture, out amount);
        }

        private static RuleCondition? ParseCondition(string[] t, ref int i)
        {
            string head = t[i].ToUpperInvariant();
            if (head is "ACTION" or "TAG" or "CLASS" or "WEAPON" or "PREVTAG" or "NEXTTAG" && i + 1 < t.Length)
            {
                string subject = head.ToLowerInvariant();
                string body = t[i + 1];
                i += 2;
                // Action names are written with underscores for spaces (one token).
                return new RuleCondition { Subject = subject, Key = subject == "action" ? body.Replace('_', ' ') : body.ToLowerInvariant() };
            }
            if (head is "TARGET" or "PREFIX" && i + 1 < t.Length)
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
            var c = Compare.Match(t[i]);
            if (!c.Success)
                return null;
            i++;
            return new RuleCondition { Subject = "self", Key = KeyOf(c.Groups[1].Value), Op = c.Groups[2].Value, Value = Num(c.Groups[3].Value) };
        }

        /// <summary>Self keys upper-case (TECH = TEC); status ids lower-case; mints keep their spelling (RAGE).</summary>
        private static string KeyOf(string raw)
        {
            string up = raw.ToUpperInvariant();
            if (up == "TECH")
                return "TEC";
            return SelfKeys.Contains(up) ? up : raw;
        }

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

        private static readonly Dictionary<string, CrossWireRule?> _scaled = new(StringComparer.Ordinal);

        private static CrossWireRule? Scaled(string spec, string source, double scale)
        {
            if (Math.Abs(scale - 1.0) < 1e-9)
                return Cached(spec, source);
            lock (_lock)
            {
                string key = source + "|" + spec + "|" + scale.ToString("R", CultureInfo.InvariantCulture);
                if (!_scaled.TryGetValue(key, out var rule))
                {
                    rule = CrossWireRule.Parse(spec, source)?.WithScale(scale);
                    _scaled[key] = rule;
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
            return rules.Count == 0 ? null : string.Join("; ", rules.Select(RPGGame.UI.RuleText.Line));
        }

        /// <summary>Attribute points ONSTATUSAPPLY BUFF rules have granted (never reset; sim fight logs diff it).</summary>
        public static long StatusBuffPointsGranted { get; private set; }

        /// <summary>Learned skill nodes' rules (scaled by rank x power), with their node.</summary>
        public static IEnumerable<(SkillTreeNodeDefinition Node, CrossWireRule Rule)> LearnedNodeRules(Character hero)
        {
            var trees = SkillTreeService.Trees;
            if (hero?.Progression == null || trees == null)
                yield break;
            var bonus = hero is Enemy ? null : View(hero).RankBonus;
            foreach (var kv in hero.Progression.LearnedSkillRanks)
            {
                if (kv.Value <= 0)
                    continue;
                var node = trees.GetNode(kv.Key);
                if (node == null || string.IsNullOrWhiteSpace(node.CrossWire))
                    continue;
                int rank = kv.Value + (bonus?.GetValueOrDefault(kv.Key) ?? 0);
                double scale = Math.Max(1, rank) * (node.Power > 0 ? node.Power : 1.0);
                foreach (string part in node.CrossWire.Split(';'))
                {
                    var rule = Scaled(part.Trim(), node.Name, scale);
                    if (rule != null)
                        yield return (node, rule);
                }
            }
        }

        /// <summary>
        /// Action-card preview: learned nodes' passive DAMAGE (dmg) / SPEED (spd) / AMP (amp) / MULTIHIT (multihit) rules that
        /// hold for <paramref name="action"/> right now, one entry per node and stat.
        /// </summary>
        public static List<(string Source, string Stat, double Value)> NodeCardBonuses(Character hero, Action action)
        {
            var list = new List<(string, string, double)>();
            var target = HeroEventHub.CurrentTarget(hero);
            foreach (var (node, rule) in LearnedNodeRules(hero))
            {
                if (!rule.IsPassive)
                    continue;
                string? stat = rule.Verb switch
                {
                    "DAMAGE" => "dmg", "SPEED" => "spd", "AMP" => "amp", "MULTIHIT" => "multihit", _ => null
                };
                if (stat == null || !ConditionsHold(hero, target, rule, action: action))
                    continue;
                double v = rule.Amount * (rule.Per == null ? 1 : PerValue(hero, target, rule.Per, action: action));
                if (v > 0)
                    list.Add((node.Name, stat, v));
            }
            return list;
        }

        /// <summary>Active rules: learned skill nodes with a rule, then equipped CROSSWIRE / RULE prefixes.</summary>
        public static List<CrossWireRule> Active(Character hero) => Collect(hero, includeNodes: true);

        private static List<CrossWireRule> Collect(Character hero, bool includeNodes)
        {
            var list = new List<CrossWireRule>();
            if (includeNodes)
            {
                var trees = SkillTreeService.Trees;
                var bonus = hero is Enemy ? null : View(hero).RankBonus;
                foreach (var kv in hero.Progression.LearnedSkillRanks)
                {
                    if (kv.Value <= 0)
                        continue;
                    var node = trees?.GetNode(kv.Key);
                    if (node == null || string.IsNullOrWhiteSpace(node.CrossWire))
                        continue;
                    // Ranked rule nodes: amounts scale by rank x node power (rank-1 nodes at power 1 stay as written);
                    // worn RANK modifiers add to the rank.
                    int rank = kv.Value + (bonus?.GetValueOrDefault(kv.Key) ?? 0);
                    double scale = Math.Max(1, rank) * (node.Power > 0 ? node.Power : 1.0);
                    foreach (string part in node.CrossWire.Split(';'))
                    {
                        var rule = Scaled(part.Trim(), node.Name, scale);
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
            if (rule.Once)
            {
                var counters = _everyCounters.GetOrCreateValue(hero);
                string key = "once|" + rule.Source + "|" + rule.Text;
                if (counters.GetValueOrDefault(key) > 0)
                    return;
                counters[key] = 1;
            }
            if (rule.Chance > 0 && rule.Chance < 1 && RandomUtility.NextDouble() >= rule.Chance)
                return;

            double perFactor = rule.Per == null ? 1.0 : PerValue(hero, target, rule.Per, eventAmount, evt, evt?.Action);
            if (perFactor <= 0)
                return;
            double amount = rule.Amount * perFactor;
            // Q7 (D2.5): inside the hero's crit, rule amounts are amplified, except those already read off the crit's
            // damage (HEAL n% of the hit, REFLECT).
            if (!(rule.Verb == "REFLECT" || (rule.Verb == "HEAL" && rule.Percent)))
                amount *= CritAmplifier.For(hero);

            switch (rule.Verb)
            {
                case "HEAL":
                {
                    int hp = rule.Percent
                        ? (int)Math.Floor(Math.Max(0, eventAmount) * amount / 100.0 + 1e-9)
                        : Math.Max(1, (int)Math.Round(hero.GetEffectiveMaxHealth() * amount / 100.0));
                    if (hp > 0 && hero.CurrentHealth > 0)
                        hero.Heal(hp);
                    return;
                }
                case "REFLECT":
                {
                    int dmg = (int)Math.Floor(Math.Max(0, eventAmount) * amount / 100.0 + 1e-9);
                    if (dmg > 0 && target != null && !ReferenceEquals(target, hero))
                        ActionUtilities.ApplyDamage(target, dmg);
                    return;
                }
                case "NEXT":
                    QueueNext(hero, rule, amount);
                    return;
                case "BUFF":
                {
                    int n = (int)Math.Round(amount);
                    if (n == 0)
                        return;
                    string stat = rule.Store.ToUpperInvariant() == "TEC" ? "TECH" : rule.Store.ToUpperInvariant();
                    if (rule.When.StartsWith("ONSTATUSAPPLY", StringComparison.OrdinalIgnoreCase)
                        && stat is "STR" or "AGI" or "TECH" or "INT")
                        StatusBuffPointsGranted += n;
                    var bonus = new[] { new ActionAttackBonusItem { Type = stat, Value = n } };
                    if (rule.Scope == "dungeon")
                        hero.DungeonCadenceBuffs.MergeAdditively(bonus);
                    else
                        hero.FightCadenceBuffs.MergeAdditively(bonus);
                    return;
                }
            }

            int units = rule.Percent
                ? (int)Math.Floor(Math.Max(0, eventAmount) * amount / 100.0 + 1e-9)
                : RoundUnits(amount);
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

        /// <summary>Whole units from a scaled amount: the fraction is a chance for one more (0.5 = half the time).</summary>
        private static int RoundUnits(double amount)
        {
            if (amount <= 0)
                return 0;
            int whole = (int)Math.Floor(amount + 1e-9);
            double frac = amount - whole;
            if (frac > 1e-6 && RandomUtility.NextDouble() < frac)
                whole++;
            return whole;
        }

        /// <summary>Card / cadence bonus type for a NEXT stat.</summary>
        internal static string NextBonusType(string stat) => stat.ToUpperInvariant() switch
        {
            "DAMAGE" => "DAMAGE_MOD",
            "SPEED" => "SPEED_MOD",
            "AMP" => "AMP_MOD",
            "MULTIHIT" => "MULTIHIT_MOD",
            "TEC" or "TECH" => "TECH",
            var s => s
        };

        /// <summary>NEXT: a TURN grant on the hero's next roll(s), with optional RAMP / DECAY shape and IFTAG wait.</summary>
        private static void QueueNext(Character hero, CrossWireRule rule, double amount)
        {
            if (amount == 0)
                return;
            int turns = Math.Max(1, rule.Turns);
            CadenceTiming? timing = null;
            if (rule.Shape != null || rule.IfTag != null)
                timing = new CadenceTiming { Shape = rule.Shape, IfTag = rule.IfTag };
            hero.Effects.TurnBonuses.Add(new ActionAttackBonusGroup
            {
                Keyword = CadenceKeywords.Turn,
                CadenceType = CadenceKeywords.Turn,
                Count = turns,
                Total = turns,
                Timing = timing,
                Bonuses = new List<ActionAttackBonusItem> { new() { Type = NextBonusType(rule.Store), Value = amount } }
            });
        }

        // ------------------------------------------------------------------ PER sources

        [ThreadStatic] private static int _attributeDepth;

        /// <summary>The count a PER clause multiplies by (divided by Div, whole steps when Div &gt; 1).</summary>
        internal static double PerValue(Character hero, Actor? target, RulePer per, int eventAmount = 0, CombatEvent? evt = null,
            Action? action = null)
        {
            double raw = per.Kind switch
            {
                "target" => target == null ? 0 : Stores.Get(target, per.Arg),
                "stat" => AttributeValue(hero, per.Arg),
                "worn" => WornCount(hero, per.Arg),
                "tags" => ActionUtilities.GetComboActions(hero).Count(a => HasTag(hero, a, per.Arg)),
                "quality" => QualityCount(hero, per.Arg.Split('|', StringSplitOptions.RemoveEmptyEntries)),
                _ => SelfValue(hero, per.Arg, eventAmount, evt, action)
            };
            if (raw <= 0)
                return 0;
            return per.Div > 1 ? Math.Floor(raw / per.Div) : raw;
        }

        private static double AttributeValue(Character hero, string stat) => stat switch
        {
            "STR" => hero.GetEffectiveStrength(),
            "AGI" => hero.GetEffectiveAgility(),
            "TEC" => hero.GetEffectiveTechnique(),
            "INT" => hero.GetEffectiveIntelligence(),
            _ => 0
        };

        /// <summary>
        /// Equipped pieces that count for <paramref name="tag"/>: a class tag counts pieces of the class's materials or with
        /// the class tag (hybrid material nodes merge in the partner class); anything else counts pieces of that material or
        /// with that item tag.
        /// </summary>
        public static int WornCount(Character hero, string tag)
        {
            var tags = new List<string> { tag };
            if (ClassTags.All.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                tags.AddRange(ClassTags.MaterialsOf(tag).Select(m => m.ToLowerInvariant()));
                foreach (string partner in HybridTrees.MergedPartners(hero, tag))
                {
                    tags.Add(partner);
                    tags.AddRange(ClassTags.MaterialsOf(partner).Select(m => m.ToLowerInvariant()));
                }
            }
            int count = 0;
            foreach (var item in MaterialSetController.EnumerateEquipped(hero))
            {
                if (item == null)
                    continue;
                if (!string.IsNullOrWhiteSpace(item.Material)
                    && tags.Any(t => string.Equals(ItemMaterialRules.RemapLegacyMaterial(item.Material), t, StringComparison.OrdinalIgnoreCase)))
                {
                    count++;
                    continue;
                }
                if (item.Tags != null && item.Tags.Any(it => tags.Any(t => string.Equals(it, t, StringComparison.OrdinalIgnoreCase))))
                    count++;
            }
            return count;
        }

        private static int QualityCount(Character hero, string[] qualities)
        {
            int count = 0;
            foreach (var item in MaterialSetController.EnumerateEquipped(hero))
            {
                if (item?.Modifications == null)
                    continue;
                if (item.Modifications.Any(m => string.Equals(m.PrefixCategory, "Quality", StringComparison.OrdinalIgnoreCase)
                        && qualities.Any(q => string.Equals(m.Name, q.Replace('_', ' '), StringComparison.OrdinalIgnoreCase))))
                    count++;
            }
            return count;
        }

        private static double SelfValue(Character hero, string key, int eventAmount, CombatEvent? evt, Action? action) => key switch
        {
            "HP" => HealthPercent(hero),
            "STEP" => hero.ComboStep,
            "SLOT" => StripSlot(hero, action),
            "SLOTS" => ActionUtilities.GetComboActions(hero).Count,
            "EXTRASLOTS" => Math.Max(0, ActionUtilities.GetComboActions(hero).Count - 1),
            "SPENT" => eventAmount,
            "LENGTH" => (evt?.Action ?? action)?.Length ?? 0,
            "ROLL" => evt?.NaturalRollValue ?? 0,
            "DISTINCT" => Attunement.CountDistinctMaterials(hero),
            "TURN" => HeroEventHub.FightTurns(hero),
            "HITSTREAK" => hero.Effects.MaterialConsecutiveConnects,
            "STR" or "AGI" or "TEC" or "INT" => AttributeValue(hero, key),
            _ => Stores.Get(hero, key)
        };

        /// <summary>The strip neighbour of <paramref name="action"/>'s slot (offset −1 = before, +1 = after; the strip wraps).</summary>
        private static Action? StripNeighbour(Character hero, Action? action, int offset)
        {
            var combo = ActionUtilities.GetComboActions(hero);
            if (combo.Count < 2)
                return null;
            int slot = (int)StripSlot(hero, action) - 1;
            if (slot < 0)
                return null;
            int idx = ((slot + offset) % combo.Count + combo.Count) % combo.Count;
            return combo[idx];
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
                    return HasTag(hero, action, c.Key);
                case "class":
                    return action != null && (GearActionPools.IsClassActionFor(action.Tags, new[] { c.Key }) || HasTag(hero, action, c.Key));
                case "weapon":
                    return hero.Equipment.Weapon is WeaponItem w
                        && string.Equals(w.WeaponType.ToString(), c.Key, StringComparison.OrdinalIgnoreCase);
                case "prevtag":
                case "nexttag":
                {
                    var n = StripNeighbour(hero, action, c.Subject == "prevtag" ? -1 : 1);
                    return HasTag(hero, n, c.Key);
                }
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
                    double v = SelfValue(hero, c.Key, eventAmount, evt, action);
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

        // ------------------------------------------------------------------ modifier verbs: TAGAS / RANK / SETCOUNT

        /// <summary>Per-hero view of the modifier verbs, rebuilt when gear or learned ranks change.</summary>
        private sealed class ModifierView
        {
            public long Signature = long.MinValue;
            public bool Building;
            public List<CrossWireRule> TagAliases = new();
            public Dictionary<string, int> RankBonus = new(StringComparer.OrdinalIgnoreCase);
        }

        private static readonly ConditionalWeakTable<Character, ModifierView> _modifierViews = new();

        private static long ModifierSignature(Character hero)
        {
            unchecked
            {
                long h = 23;
                foreach (var item in MaterialSetController.EnumerateEquipped(hero))
                {
                    if (item == null)
                        continue;
                    h = h * 31 + RuntimeHelpers.GetHashCode(item);
                    h = h * 31 + StringComparer.Ordinal.GetHashCode(item.ImplicitRule ?? "");
                    h = h * 31 + (item.Modifications?.Count ?? 0);
                }
                var ranks = hero.Progression?.LearnedSkillRanks;
                if (ranks != null)
                {
                    h = h * 31 + ranks.Count;
                    foreach (var kv in ranks)
                        h += StringComparer.OrdinalIgnoreCase.GetHashCode(kv.Key) * 397L + kv.Value;
                }
                return h;
            }
        }

        private static ModifierView View(Character hero)
        {
            var view = _modifierViews.GetValue(hero, _ => new ModifierView());
            long sig = ModifierSignature(hero);
            if (sig == view.Signature || view.Building)
                return view;
            view.Building = true;
            try
            {
                // Rank bonuses first (items only), so node rules collected below already scale by them.
                var ranks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in MaterialSetController.EnumerateEquipped(hero))
                    foreach (var rule in ItemRules(item).Where(r => r.Verb == "RANK"))
                        ranks[rule.Store] = ranks.GetValueOrDefault(rule.Store) + (int)Math.Round(rule.Amount);
                view.RankBonus = ranks;
                view.TagAliases = Collect(hero, includeNodes: true).Where(r => r.Verb == "TAGAS").ToList();
                view.Signature = sig;
            }
            finally
            {
                view.Building = false;
            }
            return view;
        }

        /// <summary>An item's own rules: its base modifier and its RULE / CROSSWIRE prefixes.</summary>
        public static IEnumerable<CrossWireRule> ItemRules(Item? item)
        {
            if (item == null)
                yield break;
            foreach (string part in BaseModifierParts(item))
            {
                var rule = Cached(part, item.Name);
                if (rule != null)
                    yield return rule;
            }
            foreach (var mod in item.Modifications ?? new List<Modification>())
                if (mod != null)
                    foreach (var rule in RulesOf(mod, item.Name))
                        yield return rule;
        }

        private static IEnumerable<string> BaseModifierParts(Item item) =>
            (item.ImplicitRule ?? "").Split(';').Select(p => p.Trim()).Where(p => p.Length > 0);

        /// <summary>
        /// Effective-rank bonus worn items give a learned skill node (<c>WHILE -> RANK node_id n</c>). Zero while the node is
        /// unlearned: gear deepens a skill you took, it does not grant one. Prerequisites and the skill screen read learned ranks.
        /// </summary>
        public static int SkillRankBonus(Character? hero, string? nodeId)
        {
            if (hero?.Progression == null || hero is Enemy || string.IsNullOrWhiteSpace(nodeId)
                || hero.Progression.GetSkillRank(nodeId) < 1)
                return 0;
            return View(hero).RankBonus.GetValueOrDefault(nodeId);
        }

        /// <summary>Worn RANK bonuses by node id (learned or not; callers check). Empty for most heroes.</summary>
        public static IReadOnlyDictionary<string, int> SkillRankBonuses(Character? hero) =>
            hero == null || hero is Enemy ? new Dictionary<string, int>() : View(hero).RankBonus;

        /// <summary>Learned rank + worn RANK bonus (0 when unlearned).</summary>
        public static int EffectiveSkillRank(Character? hero, string nodeId)
        {
            int learned = hero?.Progression?.GetSkillRank(nodeId) ?? 0;
            return learned < 1 ? 0 : learned + SkillRankBonus(hero, nodeId);
        }

        /// <summary>
        /// True when <paramref name="action"/> has <paramref name="tag"/>, or counts as it through an active
        /// <c>WHILE [IF ACTION|TAG|CLASS …] -> TAGAS tag</c> rule (worn modifiers, skill nodes). Rule conditions, PER TAGS,
        /// cadence ifTag / perTag, strip adjacency and class-tag counts read tags through here. Alias conditions see the
        /// action's own tags only (no chains).
        /// </summary>
        public static bool HasTag(Character? hero, Action? action, string? tag)
        {
            if (action == null || string.IsNullOrWhiteSpace(tag))
                return false;
            string t = tag.Trim();
            if (action.Tags != null && action.Tags.Any(x => string.Equals(x?.Trim(), t, StringComparison.OrdinalIgnoreCase)))
                return true;
            if (hero == null || hero is Enemy)
                return false;
            foreach (var alias in View(hero).TagAliases)
            {
                if (!alias.Store.Equals(t, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (alias.Conditions.All(c => c.Subject switch
                    {
                        "action" => string.Equals(action.Name?.Trim(), c.Key, StringComparison.OrdinalIgnoreCase),
                        "tag" => action.Tags?.Any(x => string.Equals(x?.Trim(), c.Key, StringComparison.OrdinalIgnoreCase)) == true,
                        "class" => GearActionPools.IsClassActionFor(action.Tags, new[] { c.Key }),
                        "weapon" => hero.Equipment.Weapon is WeaponItem w
                            && string.Equals(w.WeaponType.ToString(), c.Key, StringComparison.OrdinalIgnoreCase),
                        _ => false
                    }))
                    return true;
            }
            return false;
        }

        /// <summary>SETCOUNT on worn pieces of <paramref name="material"/>: extra set count (base modifier "counts twice").</summary>
        public static int ExtraSetCount(Character hero, string material)
        {
            int n = 0;
            foreach (var item in MaterialSetController.EnumerateEquipped(hero))
            {
                if (item == null || !string.Equals(ItemMaterialRules.RemapLegacyMaterial(item.Material), material, StringComparison.OrdinalIgnoreCase))
                    continue;
                foreach (string part in BaseModifierParts(item))
                    if (Cached(part, item.Name) is { Verb: "SETCOUNT" } r)
                        n += (int)Math.Round(r.Amount);
            }
            return n;
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
            // Attribute rules can read attributes (PER STAT INT, IF STR>=…): nested reads leave rule attributes out.
            bool attribute = CrossWireRule.AttributeStats.Contains(stat.ToUpperInvariant());
            if (attribute && _attributeDepth > 0)
                return 0;
            if (attribute)
                _attributeDepth++;
            try
            {
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
                    if (rule.Per != null)
                        v *= PerValue(hero, target, rule.Per, action: action);
                    sum += v;
                }
                return sum;
            }
            finally
            {
                if (attribute)
                    _attributeDepth--;
            }
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
            string per = rule.Per == null ? "" : " " + DescribePer(rule.Per);
            if (rule.Verb == "EXEMPT")
            {
                string lead = cond.Length > 0 ? $"While{cond}: " : "";
                return lead + (rule.Store == "STREAK" ? "misses don't reset your hit streak" : "misses don't end your combo");
            }
            if (rule.Verb == "COUNTAS")
                return $"{TriggerEvents.Label(rule.When) ?? rule.When}{cond} also counts as {(TriggerEvents.Label(rule.Store) ?? rule.Store).ToLowerInvariant()}";
            if (rule.Verb == "TAGAS")
            {
                // "Wizard actions also count as focus actions" / "CLEAVE also counts as a bludgeon action"
                var named = rule.Conditions.FirstOrDefault(c => c.Subject == "action");
                string who = named != null ? named.Key.ToUpperInvariant()
                    : rule.Conditions.Count > 0
                        ? CultureInfo.InvariantCulture.TextInfo.ToTitleCase(string.Join(" ", rule.Conditions.Select(c => c.Key))) + " actions"
                        : "Your actions";
                bool one = named != null;
                return $"{who} also count{(one ? "s" : "")} as {(one ? "a " : "")}{rule.Store} action{(one ? "" : "s")}";
            }
            if (rule.Verb == "RANK")
            {
                var node = SkillTreeService.Trees?.GetNode(rule.Store);
                string name = string.IsNullOrWhiteSpace(node?.Name) ? rule.Store : node!.Name;
                return $"+{rule.Amount:0} rank to {name} (once learned)";
            }
            if (rule.Verb == "SETCOUNT")
                return rule.Amount <= 1 ? "Counts twice toward its material set" : $"Counts {rule.Amount + 1:0} times toward its material set";
            if (rule.IsPassive && rule.Verb == "POTENCY")
            {
                string lead = cond.Length > 0 ? $"While{cond}: " : "";
                return $"{lead}{sign}{rule.Amount:0.##}% {rule.Store} damage{per}";
            }
            if (rule.IsPassive)
            {
                string unit = rule.Percent ? "%" : "";
                string stat = StatName(rule.Verb);
                if (rule.Conditions.Count > 0 && rule.Conditions.All(c => c.ReadsAction))
                    return $"{sign}{rule.Amount:0.##}{unit} {stat}{per} {string.Join(" and ", rule.Conditions.Select(DescribeCondition))}";
                string lead = cond.Length > 0 ? $"While{cond}: " : "";
                return $"{lead}{sign}{rule.Amount:0.##}{unit} {stat}{per}";
            }
            string when = TriggerEvents.Label(rule.When) ?? rule.When;
            var gates = new List<string>();
            if (rule.Every > 1) gates.Add($"every {rule.Every}");
            if (rule.Once) gates.Add("first each fight");
            if (rule.Chance > 0 && rule.Chance < 1) gates.Add($"{rule.Chance * 100:0.#}% chance");
            string gate = gates.Count > 0 ? $" ({string.Join(", ", gates)})" : "";
            string amount = rule.Percent ? $"{rule.Amount:0.##}% of it" : $"{rule.Amount:0.##}";
            string what = rule.Verb switch
            {
                "MINT" => $"+{amount} {rule.Store.ToUpperInvariant()}{per}",
                "ADD" => $"+{amount} {rule.Store}{per}",
                "HEAL" => (rule.Percent ? $"heal {rule.Amount:0.##}% of the damage" : $"heal {rule.Amount:0.##}% max HP") + per,
                "REFLECT" => $"deal {rule.Amount:0.##}% of it back",
                "BUFF" => $"+{rule.Amount:0.##} {rule.Store.ToUpperInvariant()}{per} for the {rule.Scope}",
                "NEXT" => DescribeNext(rule, per),
                _ => $"{amount} {rule.Store}{per} on the target"
            };
            return $"{when}{gate}{cond}: {what}";
        }

        private static string StatName(string verb) => verb switch
        {
            "DAMAGE" => "damage",
            "ARMOR" => "armor",
            "SPEED" => "speed",
            "HIT" => "to hit",
            "COMBO" => "to combo",
            "AMP" => "AMP",
            "MULTIHIT" => "hits",
            "STR" or "AGI" or "TEC" or "INT" => verb,
            _ => "to crit"
        };

        private static string DescribeNext(CrossWireRule rule, string per)
        {
            string unit = rule.Percent ? "%" : "";
            string stat = rule.Store switch
            {
                "DAMAGE" => "damage",
                "SPEED" => "speed",
                "AMP" => "AMP",
                "MULTIHIT" => "hits",
                "HIT" => "to hit",
                "COMBO" => "to combo",
                "CRIT" => "to crit",
                "ACCURACY" => "accuracy",
                "TECH" => "TEC",
                var s => s
            };
            string target = rule.IfTag != null ? $"your next {rule.IfTag} action" : "your next action";
            if (rule.Turns > 1)
                target = rule.IfTag != null ? $"your next {rule.Turns} {rule.IfTag} actions" : $"your next {rule.Turns} actions";
            string shape = rule.Shape == "ramp" ? " (ramping)" : rule.Shape == "decay" ? " (fading)" : "";
            return $"+{rule.Amount:0.##}{unit} {stat}{per} on {target}{shape}";
        }

        private static string DescribePer(RulePer p)
        {
            string step = p.Div > 1 ? $"{p.Div} " : "";
            return p.Kind switch
            {
                "target" => $"per {step}{p.Arg} on the target",
                "stat" => $"per {step}{p.Arg}",
                "worn" => $"per {(p.Div > 1 ? step : "")}{p.Arg} piece worn",
                "tags" => $"per {(p.Div > 1 ? step : "")}{p.Arg} action in your strip",
                "quality" => $"per {p.Arg.Replace('|', '/').Replace('_', ' ')} piece worn",
                _ => p.Arg switch
                {
                    "SLOTS" => $"per {step}strip slot",
                    "EXTRASLOTS" => $"per {step}strip slot beyond the first",
                    "SLOT" => "x strip slot",
                    "SPENT" => $"per {step}spent",
                    "ROLL" => "x the roll",
                    "HITSTREAK" => $"per {step}hit in your streak",
                    "DISTINCT" => $"per {step}distinct material",
                    _ => $"per {step}{p.Arg}"
                }
            };
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
                "weapon" => $"with a {c.Key}",
                "prevtag" => $"right after a {c.Key} action in your strip",
                "nexttag" => $"right before a {c.Key} action in your strip",
                "target" => c.Key == "HP" ? $"target HP{cmp}%" : c.Op.Length == 0 ? $"vs a target with {c.Key}" : $"target {c.Key}{cmp}",
                _ => c.Key switch
                {
                    "HP" => $"your HP{cmp}%",
                    "STEP" => $"combo step{cmp}",
                    "SLOT" => $"in strip slot{cmp}",
                    "SLOTS" => $"strip length{cmp}",
                    "EXTRASLOTS" => $"extra strip slots{cmp}",
                    "SPENT" => $"spending{cmp}",
                    "LENGTH" => $"action length{cmp}",
                    "ROLL" => $"roll{cmp}",
                    "DISTINCT" => $"materials worn{cmp}",
                    "TURN" => $"turn{cmp}",
                    "HITSTREAK" => $"hit streak{cmp}",
                    "STR" or "AGI" or "TEC" or "INT" => $"{c.Key}{cmp}",
                    _ => c.Op.Length == 0 ? $"with {c.Key} up" : c.Op == ">=" ? $"with {c.Key} {c.Value:0.##}+" : $"{c.Key}{cmp}"
                }
            };
        }
    }
}
