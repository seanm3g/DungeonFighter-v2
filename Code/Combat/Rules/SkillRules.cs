using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RPGGame.Combat.Events;
using RPGGame.Data;

namespace RPGGame.Combat.Rules
{
    /// <summary>
    /// Skill effects that need real code, as coded rules on the engine (one rule engine, step 7, 2026-11-20). Each is a
    /// skill-owned <c>ONCONNECT</c> rule (<see cref="CrossWireRule.Coded"/>) in the hero's <see cref="RuleBook"/>, so it
    /// resolves in tree order with the other rules and shows in the trace; nothing subscribes to the combat bus on its own.
    /// <list type="bullet">
    /// <item>conversion table rows (<c>GameData/Conversions.json</c>; Venom Mace, Envenomed Edge, Hemorrhage, Thermal
    /// Shock): owned by the node whose customEffectId is the row's owner; phase early = gain, late = other</item>
    /// <item>hybrid accumulation (+1 secondary attribute / hybrid tag per connect this fight × power): gain</item>
    /// <item>hybrid unique actions (BODY BLOW, WARBRUTE SMASH, …: +25% of the bonus per 10 of the scaling source × power)</item>
    /// <item>per-rank pack mods (<c>damageModPerRank</c> / <c>healOnHitPerRank</c>) of nodes in the primary class tree,
    /// with that class's weapon in hand</item>
    /// </list>
    /// </summary>
    public static class SkillRules
    {
        public const string Verb = "CODE";
        private const string When = "ONCONNECT";

        private enum HybridBonus { Damage, Speed, Amp, Accuracy, Combo, Status }

        /// <summary>Percent of the hybrid bonus per 10 units of the scaling source (the design board's +25%).</summary>
        public const double HybridBonusPerTen = 25.0;

        private static readonly (string Action, string Hybrid, bool FromTags, HybridBonus Bonus)[] HybridActions =
        {
            ("BODY BLOW", "Warbrute", false, HybridBonus.Damage),
            ("WARBRUTE SMASH", "Warbrute", true, HybridBonus.Damage),
            ("PROWESS RUSH", "Ravager", false, HybridBonus.Speed),
            ("RAVAGER FLURRY", "Ravager", true, HybridBonus.Speed),
            ("WILLFIRE", "Ragecaller", false, HybridBonus.Amp),
            ("RAGECALLER HOWL", "Ragecaller", true, HybridBonus.Amp),
            ("FINESSE THRUST", "Duelist", false, HybridBonus.Accuracy),
            ("DUELIST FEINT", "Duelist", true, HybridBonus.Accuracy),
            ("WITS ARC", "Spellblade", false, HybridBonus.Combo),
            ("SPELLBLADE WEAVE", "Spellblade", true, HybridBonus.Combo),
            ("MIND HEX", "Hexblade", false, HybridBonus.Status),
            ("HEXBLADE CURSE", "Hexblade", true, HybridBonus.Status)
        };

        public static string HybridActionEffect(HybridInfo h, bool fromTags) =>
            $"{h.Tag}_action_{(fromTags ? "tag" : "attribute")}";

        // ------------------------------------------------------------------ the rules

        /// <summary>The hero's coded skill rules, each with its owning node (one rule per customEffectId).</summary>
        public static IEnumerable<(SkillTreeNodeDefinition Node, CrossWireRule Rule)> Coded(Character hero)
        {
            var trees = SkillTreeService.Trees;
            if (hero?.Progression == null || hero is Enemy || trees == null)
                yield break;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in hero.Progression.LearnedSkillRanks)
            {
                if (kv.Value <= 0)
                    continue;
                var node = trees.GetNode(kv.Key);
                if (node == null)
                    continue;
                string id = node.CustomEffectId ?? node.Id;
                if (seen.Add(id))
                {
                    foreach (var row in ConversionTable.Rows)
                        if (string.Equals(row.Owner, id, StringComparison.OrdinalIgnoreCase))
                            yield return (node, Conversion(node, row));
                    foreach (var h in HybridTrees.All)
                    {
                        if (string.Equals(id, h.AccumulateAttributeEffect, StringComparison.OrdinalIgnoreCase))
                            yield return (node, Accumulate(node, h, tags: false));
                        else if (string.Equals(id, h.AccumulateTagEffect, StringComparison.OrdinalIgnoreCase))
                            yield return (node, Accumulate(node, h, tags: true));
                    }
                    foreach (var entry in HybridActions)
                    {
                        var h = HybridTrees.ByKey(entry.Hybrid);
                        if (h != null && string.Equals(id, HybridActionEffect(h, entry.FromTags), StringComparison.OrdinalIgnoreCase))
                            yield return (node, HybridAction(node, h, entry.Action, entry.FromTags, entry.Bonus));
                    }
                }
                if (node.DamageModPerRank > 0 || node.HealOnHitPerRank > 0)
                    yield return (node, PackMod(node));
            }
        }

        private static CrossWireRule Make(SkillTreeNodeDefinition node, string text, int phase, Func<Character, CombatEvent?, bool> fire) =>
            new()
            {
                When = When, Verb = Verb, Source = node.Name ?? node.Id, Text = $"{When} -> {text}",
                Coded = fire, CodedPhase = phase
            };

        private static Actor? Foe(Character hero, CombatEvent? evt) =>
            evt?.Target is { } t && !ReferenceEquals(t, hero) ? t : null;

        private static CrossWireRule Conversion(SkillTreeNodeDefinition node, ConversionRow row)
        {
            bool early = !string.Equals(row.Phase, "late", StringComparison.OrdinalIgnoreCase);
            return Make(node, $"CONVERT {row.Label}", early ? 0 : 1, (hero, evt) =>
                Foe(hero, evt) is { } target && ConversionTable.ApplyRow(hero, target, row, evt!.IsCritical) > 0);
        }

        private static CrossWireRule Accumulate(SkillTreeNodeDefinition node, HybridInfo h, bool tags) =>
            Make(node, $"ACCUMULATE {h.Key} {(tags ? "tag" : h.Attribute)}", 0, (hero, _) =>
            {
                string effect = tags ? h.AccumulateTagEffect : h.AccumulateAttributeEffect;
                double power = SkillRanks.GetPower(hero, effect);
                if (power <= 0)
                    return false;
                var state = State(hero);
                var map = tags ? state.Tags : state.Attributes;
                map[h.Key] = map.GetValueOrDefault(h.Key) + power;
                return true;
            });

        private static CrossWireRule HybridAction(SkillTreeNodeDefinition node, HybridInfo h, string action, bool fromTags, HybridBonus bonus) =>
            Make(node, $"{action}: {bonus.ToString().ToUpperInvariant()} +{HybridBonusPerTen:0}% per 10 {(fromTags ? h.Key + " tags" : h.Attribute)}", 1,
                (hero, evt) =>
                {
                    if (!string.Equals(evt?.Action?.Name, action, StringComparison.OrdinalIgnoreCase))
                        return false;
                    double power = SkillRanks.GetPower(hero, HybridActionEffect(h, fromTags));
                    if (power <= 0)
                        return false;
                    int source = fromTags ? HybridTrees.TagCount(hero, h) : HybridTrees.SecondaryAttribute(hero, h);
                    double pct = HybridBonusPerTen * Math.Floor(source / 10.0) * power;
                    if (pct <= 0)
                        return false;
                    int steps = Math.Max(1, (int)Math.Round(pct / HybridBonusPerTen));
                    switch (bonus)
                    {
                        case HybridBonus.Damage: AddMod(hero, "DAMAGE_MOD", pct); break;
                        case HybridBonus.Speed: AddMod(hero, "SPEED_MOD", pct); break;
                        case HybridBonus.Amp: hero.Effects.ConsumedSkillAmpPercent += pct; break;
                        case HybridBonus.Accuracy:
                            hero.Effects.SetTempRollBonus(Math.Max(hero.Effects.GetTempRollBonus(), steps), 1);
                            break;
                        case HybridBonus.Combo:
                            State(hero).PendingComboSteps += steps;
                            break;
                        case HybridBonus.Status:
                            if (Foe(hero, evt) is not { } target)
                                return false;
                            ExposeStatus.Apply(target, steps, 2);
                            if (target is Character weakened)
                                weakened.ApplyWeaken(2);
                            break;
                    }
                    return true;
                });

        /// <summary>A node's per-rank pack mods: only from the primary class tree, with that class's weapon in hand.</summary>
        private static CrossWireRule PackMod(SkillTreeNodeDefinition node)
        {
            string text = string.Join(", ", new[]
            {
                node.DamageModPerRank > 0 ? $"DAMAGE +{node.DamageModPerRank}% per rank" : null,
                node.HealOnHitPerRank > 0 ? $"HEAL {node.HealOnHitPerRank} HP per rank" : null
            }.Where(x => x != null));
            return Make(node, text, 1, (hero, _) =>
            {
                WeaponType? path = hero.Progression?.GetPrimaryClassWeaponType();
                if (path == null || hero.Equipment.Weapon is not WeaponItem w || w.WeaponType != path.Value)
                    return false;
                var tree = SkillTreeService.Trees?.GetTreeForWeapon(path.Value);
                if (tree == null || !tree.Nodes.Any(n => string.Equals(n.Id, node.Id, StringComparison.OrdinalIgnoreCase)))
                    return false;
                int learned = hero.Progression!.LearnedSkillRanks.GetValueOrDefault(node.Id);
                if (learned <= 0)
                    return false;
                int rank = learned + CrossWires.SkillRankBonus(hero, node.Id);
                if (node.DamageModPerRank > 0)
                    AddMod(hero, "DAMAGE_MOD", node.DamageModPerRank * rank);
                if (node.HealOnHitPerRank > 0)
                    hero.Heal(node.HealOnHitPerRank * rank);
                return true;
            });
        }

        private static void AddMod(Character hero, string type, double value)
        {
            if (value <= 0)
                return;
            hero.Effects.AccumulateConsumedModifierBonuses(new List<ActionAttackBonusItem> { new() { Type = type, Value = value } });
        }

        // ------------------------------------------------------------------ hybrid fight state

        private sealed class HybridState
        {
            public readonly Dictionary<string, double> Attributes = new(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, double> Tags = new(StringComparer.OrdinalIgnoreCase);
            public int PendingComboSteps;
        }

        private static readonly ConditionalWeakTable<Character, HybridState> _state = new();

        private static HybridState State(Character hero) => _state.GetOrCreateValue(hero);

        /// <summary>Secondary attribute this fight's connects added (Warbrute Surge, …).</summary>
        public static int AttributeAccumulation(Character c, HybridInfo h) =>
            _state.TryGetValue(c, out var s) ? (int)Math.Floor(s.Attributes.GetValueOrDefault(h.Key)) : 0;

        /// <summary>Hybrid tags this fight's connects added (Warbrute Momentum, …).</summary>
        public static int TagAccumulation(Character c, HybridInfo h) =>
            _state.TryGetValue(c, out var s) ? (int)Math.Floor(s.Tags.GetValueOrDefault(h.Key)) : 0;

        /// <summary>Combo-threshold steps banked by a Spellblade action, spent on the hero's next attack.</summary>
        public static int ConsumePendingComboSteps(Character hero)
        {
            if (!_state.TryGetValue(hero, out var s) || s.PendingComboSteps <= 0)
                return 0;
            int steps = s.PendingComboSteps;
            s.PendingComboSteps = 0;
            return steps;
        }

        /// <summary>New fight: every hero's accumulation and banked combo steps restart.</summary>
        public static void ResetFight() => _state.Clear();
    }
}
