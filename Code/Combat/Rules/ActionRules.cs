using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using RPGGame.Actions.Conditional;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Events;
using RPGGame.Data;

namespace RPGGame.Combat.Rules
{
    /// <summary>
    /// Action-owned rules (one rule engine, step 4, 2026-11-19): an action card's trigger bundles
    /// (<see cref="ActionTriggerBundle"/>, still the data / sheet format) converted to rules once per action, and their
    /// dispatch. A card's rules fire only on its own swing: they match with the card's own event check
    /// (<see cref="ActionTriggerGate"/>, card trigger filters included), resolve through <see cref="RuleEngine"/> (gains
    /// first, written order, logged with the card as owner) and work for heroes and enemies alike.
    /// <list type="bullet">
    /// <item><c>SWING DAMAGE|SPEED|AMP n% [PER SELF MISSINGHP]</c>: this swing (resolves before damage)</item>
    /// <item><c>STRIP op[:arg] [n]</c>, <c>RETRIGGER next|opener|finisher|slot:n</c>: the strip vocabulary</item>
    /// <item><c>NEXTROLL n</c>, <c>CRITFACE n</c> (crit on faces n+ this fight), <c>ADD salvage n</c></item>
    /// <item><c>HEAL n HP</c> (the card's heal amount), <c>APPLY status [n]</c> (the card's status settings; n overrides a
    /// DoT's size)</item>
    /// </list>
    /// </summary>
    public static class ActionRules
    {
        private sealed class Holder
        {
            public object? For;
            public IReadOnlyList<CrossWireRule> Rules = Array.Empty<CrossWireRule>();
        }

        private static readonly ConditionalWeakTable<Action, Holder> _cache = new();
        private static readonly EffectHandlerRegistry EffectRegistry = new();

        private static readonly HashSet<string> StatusIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "weaken", "slow", "vulnerability", "harden", "focus", "confuse", "confusion",
            "stat_drain", "statdrain", "fortify", "pierce", "disrupt", "expose", "silence",
            "bleed", "poison", "burn", "acid", "stun", "mark", "hpregen", "armorbreak",
            "absorb", "temporaryhp"
        };

        /// <summary>The card's rules, converted from its trigger bundles (cached until the bundle list changes).</summary>
        public static IReadOnlyList<CrossWireRule> For(Action? action)
        {
            var bundles = action?.Triggers?.Bundles;
            if (action == null || bundles == null || bundles.Count == 0)
                return Array.Empty<CrossWireRule>();
            var holder = _cache.GetOrCreateValue(action);
            if (!ReferenceEquals(holder.For, bundles))
            {
                var list = new List<CrossWireRule>();
                foreach (var bundle in bundles)
                    if (bundle != null && bundle.IsEnabled)
                        list.AddRange(Convert(action, bundle));
                holder.Rules = list;
                holder.For = bundles;
                foreach (string raw in Unsupported(action))
                    RPGGame.DebugLogger.WriteCombatDebug("ActionRules",
                        $"{action.Name}: trigger mechanic '{raw}' has no rule form and is ignored");
            }
            return holder.Rules;
        }

        /// <summary>Converts one bundle; mechanics with no rule form yield nothing (<see cref="Unsupported"/> names them).</summary>
        public static IEnumerable<CrossWireRule> Convert(Action action, ActionTriggerBundle bundle)
        {
            string when = WhenOf(bundle);
            string source = action.Name ?? "action";
            int count = ParsePositive(bundle.Count, 1);
            bool fight = CadenceKeywords.IsFight(CadenceKeywords.Normalize(bundle.Scope));
            foreach (string raw in bundle.ParseMechanicIds())
            {
                string id = ActionMechanicsRegistry.NormalizeMechanicId(raw);
                string? arg = null;
                int colon = (raw ?? "").IndexOf(':');
                if (colon > 0)
                {
                    id = ActionMechanicsRegistry.NormalizeMechanicId(raw!.Substring(0, colon));
                    arg = raw.Substring(colon + 1).Trim();
                }
                CrossWireRule Make(string verb, string store, double amount, string text, bool pct = false, RulePer? per = null,
                    bool flat = false) => new()
                {
                    When = when, Verb = verb, Store = store, Amount = amount, Percent = pct, Per = per, Flat = flat,
                    Scope = fight ? "fight" : "", Source = source, Text = $"{when} -> {text}"
                };

                if (id is "hero_action_damage" or "hero_action_speed" or "hero_action_amp")
                {
                    string stat = id.Substring("hero_action_".Length).ToUpperInvariant();
                    double v = bundle.Value ?? 0;
                    if (v == 0)
                        continue;
                    string scale = ScaleKey(bundle.ScaleFrom);
                    RulePer? per = scale.Length > 0 ? new RulePer { Kind = "self", Arg = scale } : null;
                    yield return Make("SWING", stat, v, $"SWING {stat} {v:0.##}%" + (per != null ? $" PER SELF {scale}" : ""), pct: true, per: per);
                }
                else if (StripMutationApplier.IsStripMechanic(id))
                {
                    string op = arg != null ? $"{id}:{arg}" : id;
                    yield return Make("STRIP", op, count, $"STRIP {op} {count}");
                }
                else if (RetriggerScheduler.IsRetriggerMechanic(id))
                {
                    string op = arg != null ? $"{id}:{arg}" : id;
                    yield return Make("RETRIGGER", op, count, $"RETRIGGER {op}");
                }
                else if (id == "replace_next_roll")
                {
                    int face = ParseFace(arg, bundle.Value, 1, 20) ?? 20;
                    yield return Make("NEXTROLL", "", face, $"NEXTROLL {face}");
                }
                else if (id == "crit_face_min")
                {
                    int face = ParseFace(arg, bundle.Value, 2, 20) ?? (action.RollMods.CriticalHitThresholdOverride > 0
                        ? action.RollMods.CriticalHitThresholdOverride
                        : 19);
                    yield return Make("CRITFACE", "", face, $"CRITFACE {face}");
                }
                else if (id == "salvage_miss")
                {
                    int charges = int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out int a) && a > 0
                        ? a
                        : bundle.Value is > 0 ? Math.Max(1, (int)Math.Round(bundle.Value.Value)) : count;
                    yield return Make("ADD", "salvage", charges, $"ADD salvage {charges}");
                }
                else if (id == "heal")
                {
                    yield return Make("HEAL", "", bundle.Value ?? 0, "HEAL (card) HP", flat: true);
                }
                else if (StatusIds.Contains(id))
                {
                    yield return Make("APPLY", id, bundle.Value ?? 0,
                        bundle.Value is > 0 ? $"APPLY {id} {bundle.Value.Value:0.##}" : $"APPLY {id} (card)");
                }
            }
        }

        /// <summary>Mechanic ids on enabled bundles that <see cref="Convert"/> has no rule form for (tests keep this empty
        /// for live data).</summary>
        public static IEnumerable<string> Unsupported(Action action)
        {
            foreach (var bundle in action.Triggers?.Bundles ?? new List<ActionTriggerBundle>())
            {
                if (bundle == null || !bundle.IsEnabled)
                    continue;
                foreach (string raw in bundle.ParseMechanicIds())
                {
                    var one = new ActionTriggerBundle
                    {
                        When = bundle.When, Count = bundle.Count, Scope = bundle.Scope, Mechanics = raw, Value = bundle.Value,
                        ScaleFrom = bundle.ScaleFrom
                    };
                    if (!Convert(action, one).Any())
                        yield return raw;
                }
            }
        }

        /// <summary>
        /// Fires the card's matching rules for <paramref name="evt"/>. <paramref name="swingOnly"/> = the pre-damage pass
        /// (SWING rules only); otherwise every other rule. Returns true when one took effect.
        /// </summary>
        public static bool Dispatch(Action action, CombatEvent? evt, Actor source, Actor target, List<string> messages,
            bool swingOnly = false)
        {
            var rules = For(action);
            if (rules.Count == 0 || !ActionTriggerGate.PassesNonOutcomeFilters(action, evt))
                return false;
            var owner = new RuleOwner(RuleOwnerKind.Action, action.Name ?? "action", RuleBook.ActionOrder);
            var matched = new List<RuleEntry>();
            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if ((rule.Verb == "SWING") != swingOnly)
                    continue;
                if (ActionTriggerGate.MatchesConditionToken(rule.When, action, evt))
                    matched.Add(new RuleEntry(rule, owner, i));
            }
            if (matched.Count == 0)
                return false;

            using var scope = RuleEngine.Scope();
            bool any = false;
            var hero = source as Character;
            foreach (var entry in RuleEngine.InOrder(matched))
            {
                if (!Fire(entry.Rule, action, evt, source, target, messages))
                    continue;
                any = true;
                if (hero != null)
                    RuleEngine.Record(hero, entry, evt?.Type.ToString() ?? "event");
            }
            return any;
        }

        private static bool Fire(CrossWireRule rule, Action action, CombatEvent? evt, Actor source, Actor target, List<string> messages)
        {
            switch (rule.Verb)
            {
                case "SWING":
                    return Swing(source, target, rule, evt, messages);
                case "STRIP":
                    return Strip(source, action, rule, messages);
                case "RETRIGGER":
                    return Retrigger(source, action, rule, messages);
                case "NEXTROLL":
                {
                    int face = (int)Math.Round(rule.Amount);
                    CombatTriggerContext.SetPendingReplaceRollFace(source, face);
                    messages.Add($"{source.Name}'s next roll becomes {face}.");
                    return true;
                }
                case "CRITFACE":
                    return CritFace(source, (int)Math.Round(rule.Amount), rule.Scope == "fight", messages);
                case "ADD" when rule.Store.Equals("salvage", StringComparison.OrdinalIgnoreCase):
                {
                    int charges = Math.Max(1, (int)Math.Round(rule.Amount));
                    CombatTriggerContext.AddMissSalvageCharges(source, charges);
                    messages.Add($"{source.Name} gains miss salvage ({charges}).");
                    return true;
                }
                case "HEAL":
                {
                    int amount = action.Advanced?.HealAmount ?? 0;
                    if (amount <= 0 && rule.Amount > 0)
                        amount = Math.Max(1, (int)Math.Round(rule.Amount));
                    return HealFlat(source, amount, messages);
                }
                case "APPLY":
                    return ApplyCardStatus(rule.Store, rule.Amount > 0 ? rule.Amount : null, action, source, target, messages);
            }
            return false;
        }

        // ------------------------------------------------------------------ verb bodies (shared with item / skill rules)

        /// <summary>SWING stat n%: this swing's damage / speed / amp (PER scales it; heroes only).</summary>
        private static bool Swing(Actor source, Actor target, CrossWireRule rule, CombatEvent? evt, List<string> messages)
        {
            if (source is not Character hero || hero is Enemy)
                return false;
            double v = rule.Amount;
            if (rule.Per != null)
                v *= CrossWires.PerValue(hero, target, rule.Per, 0, evt, evt?.Action);
            if (v == 0)
                return false;
            switch (rule.Store)
            {
                case "DAMAGE":
                    hero.Effects.ConsumedDamageModPercent += v;
                    messages.Add($"{source.Name} gains {v:+0;-0}% damage this swing.");
                    return true;
                case "SPEED":
                    hero.Effects.ConsumedSpeedModPercent += v;
                    messages.Add($"{source.Name} gains {v:+0;-0}% speed this swing.");
                    return true;
                case "AMP":
                    hero.Effects.ConsumedAmpModPercent += v;
                    messages.Add($"{source.Name} gains {v:+0;-0}% amp this swing.");
                    return true;
            }
            return false;
        }

        /// <summary>STRIP op[:arg] n.</summary>
        public static bool Strip(Actor source, Action? action, CrossWireRule rule, List<string> messages)
        {
            var (id, arg) = SplitOp(rule.Store);
            string count = ((int)Math.Max(1, Math.Round(rule.Amount))).ToString(CultureInfo.InvariantCulture);
            return StripMutationApplier.TryApply(id, arg, count, action ?? new Action { Name = rule.Source }, source, messages);
        }

        /// <summary>RETRIGGER next|opener|finisher|slot:n.</summary>
        public static bool Retrigger(Actor source, Action? action, CrossWireRule rule, List<string> messages)
        {
            var (id, arg) = SplitOp(rule.Store);
            string count = ((int)Math.Max(1, Math.Round(rule.Amount))).ToString(CultureInfo.InvariantCulture);
            return RetriggerScheduler.TrySchedule(id, arg, count, action ?? new Action { Name = rule.Source }, source, messages);
        }

        /// <summary>CRITFACE n: crits on natural faces n+ (this fight for a hero with <paramref name="fight"/>).</summary>
        public static bool CritFace(Actor source, int minFace, bool fight, List<string> messages)
        {
            if (minFace < 2 || minFace > 20)
                return false;
            CombatTriggerContext.SetCritFaceMin(source, minFace);
            if (fight && source is Character hero && hero is not Enemy)
            {
                var items = new List<ActionAttackBonusItem>
                {
                    new() { Type = RollModificationManager.SetCriticalHitThresholdType, Value = minFace }
                };
                CadenceScopedBuffApplicator.DepositToScope(hero, "FIGHT", items, 1);
            }
            messages.Add($"{source.Name}: crit on natural faces ≥ {minFace}.");
            return true;
        }

        /// <summary>HEAL n HP: a flat heal (a crit amplifies it).</summary>
        public static bool HealFlat(Actor source, int amount, List<string> messages)
        {
            if (amount <= 0)
                return false;
            amount *= CritAmplifier.For(source);
            ActionUtilities.ApplyHealing(source, amount);
            messages.Add($"{source.Name} heals {amount}.");
            return true;
        }

        /// <summary>The card's status, with the card's settings (a crit lands it extra times).</summary>
        private static bool ApplyCardStatus(string id, double? magnitude, Action action, Actor source, Actor target, List<string> messages)
        {
            bool landed = ApplyStatusOnce(id, action, source, target, messages, magnitude);
            int critApps = landed ? CritAmplifier.For(source) : 1;
            int extra = 0;
            for (int i = 1; i < critApps; i++)
                if (ApplyStatusOnce(id, action, source, target, new List<string>(), magnitude))
                    extra++;
            if (extra > 0)
                messages.Add($"    Critical: {id} lands {extra + 1} times");
            return landed;
        }

        private static bool ApplyStatusOnce(string mechanicId, Action action, Actor source, Actor target, List<string> messages,
            double? magnitude)
        {
            string effectType = ActionMechanicsRegistry.NormalizeMechanicId(mechanicId) switch
            {
                "confuse" => "confusion",
                "stat_drain" => "statdrain",
                var x => x
            };
            var recipient = RPGGame.Actions.Execution.ActionEffectTargetResolver.ResolveStatusEffectRecipient(action, effectType, source, target);
            var effectAction = StatusEffectActionResolver.ResolveActionForEffectApplication(action, effectType);
            // DoT rules use their value: poison = % of max HP per tick, burn / bleed = stacks.
            if (magnitude is > 0 && effectAction != null && effectType is "poison" or "burn" or "bleed")
            {
                effectAction = StatusEffectActionResolver.CreateActionWithStatusEffect(effectType) ?? effectAction;
                if (effectType == "poison")
                    effectAction.PoisonPercentToAdd = magnitude.Value;
                else if (effectType == "burn")
                    effectAction.BurnAmountToAdd = Math.Max(1, (int)Math.Round(magnitude.Value));
                else
                    effectAction.BleedAmountToAdd = Math.Max(1, (int)Math.Round(magnitude.Value));
            }
            return EffectRegistry.ApplyEffect(effectType, recipient, effectAction, messages);
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>The bundle's WHEN as a rule event. Roll-face events take the bundle count as their face; ONROOMSCLEARED
        /// takes it as "the Nth clear" only above 1 (a count of 1 used to mean "the first clear only").</summary>
        private static string WhenOf(ActionTriggerBundle bundle)
        {
            string when = ActionTriggerGate.NormalizeToken(bundle.When ?? "");
            if (when == "ONHEROHURT")
                when = "ONTAKEHIT";
            int n = ParsePositive(bundle.Count, 0);
            return when switch
            {
                "ONROLLVALUE" or "ONNATURALROLL" when n > 0 => $"{when}:{n}",
                "ONROOMSCLEARED" when n > 1 => $"{when}:{n}",
                _ => when
            };
        }

        /// <summary>A bundle's scaleFrom as a rule self key (MISSINGHEALTH → MISSINGHP, STRENGTH → STR, …).</summary>
        private static string ScaleKey(string? scaleFrom)
        {
            string key = (scaleFrom ?? "").Trim().ToUpperInvariant().Replace(" ", "").Replace("_", "");
            return key switch
            {
                "MISSINGHEALTH" => "MISSINGHP",
                "STRENGTH" => "STR",
                "AGILITY" => "AGI",
                "TECH" or "TECHNIQUE" => "TEC",
                "INTELLIGENCE" => "INT",
                _ => key
            };
        }

        private static (string Id, string? Arg) SplitOp(string op)
        {
            int colon = op.IndexOf(':');
            return colon > 0 ? (op[..colon], op[(colon + 1)..]) : (op, null);
        }

        private static int? ParseFace(string? arg, double? value, int min, int max)
        {
            if (int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out int a) && a >= min && a <= max)
                return a;
            if (value is { } v && v >= min && v <= max)
                return (int)Math.Round(v);
            return null;
        }

        private static int ParsePositive(string? cell, int fallback) =>
            int.TryParse((cell ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > 0 ? n : fallback;
    }
}
