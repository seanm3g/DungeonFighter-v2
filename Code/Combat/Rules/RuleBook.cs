using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RPGGame.Combat.Rules
{
    /// <summary>Who a rule belongs to (D6.12 owner order: action card, its item, other items, sets, skills; D6.15 enemy, room).</summary>
    public enum RuleOwnerKind
    {
        Action,
        Item,
        Set,
        Skill,
        Enemy,
        Room
    }

    /// <summary>A rule's owner: kind, display name and its place in the D6.12 owner order (lower resolves first).</summary>
    public sealed class RuleOwner
    {
        public RuleOwner(RuleOwnerKind kind, string name, int order, Item? item = null, SkillTreeNodeDefinition? node = null)
        {
            Kind = kind;
            Name = name;
            Order = order;
            Item = item;
            Node = node;
        }

        public RuleOwnerKind Kind { get; }
        public string Name { get; }
        public int Order { get; }
        /// <summary>The worn piece, for item-owned rules.</summary>
        public Item? Item { get; }
        /// <summary>The skill node, for skill-owned rules.</summary>
        public SkillTreeNodeDefinition? Node { get; }
    }

    /// <summary>One active rule with its owner and its position on the owner's card (written order).</summary>
    public sealed class RuleEntry
    {
        public RuleEntry(CrossWireRule rule, RuleOwner owner, int index)
        {
            Rule = rule;
            Owner = owner;
            Index = index;
        }

        public CrossWireRule Rule { get; }
        public RuleOwner Owner { get; }
        /// <summary>Position among the owner's rules, as written.</summary>
        public int Index { get; }

        /// <summary>D6.12 phase: 0 = gains (MINT, ADD, APPLY) resolve first; 1 = everything else.</summary>
        public int Phase => Rule.Coded != null ? Rule.CodedPhase : Rule.Verb is "ADD" or "APPLY" ? 0 : 1;
    }

    /// <summary>
    /// The hero's active rules in owner order (Documentation/CORE_DESIGN_PASS_2026-11-17.md D6.12), cached per hero and
    /// rebuilt only when worn items, learned ranks or the skill trees change. Every rule reader (triggered dispatch,
    /// passives, aliases, exemptions, tag aliases) goes through it.
    /// <list type="number">
    /// <item>worn items: Weapon, Head, Body, Legs, Feet (the item that granted the swing's card moves to the front at
    /// dispatch time, <see cref="RuleEngine"/>)</item>
    /// <item>material sets' earn rules, in MaterialBuilds.json order</item>
    /// <item>skill nodes in tree order: Core, then the material branches, then Rite; by tier, then position in the tree;
    /// never learn order (a node's coded rules, <see cref="SkillRules"/>, after its written ones)</item>
    /// </list>
    /// Material sets sit between items and skills (their earn rules, <see cref="CrossWireRule.SetBuild"/>), in
    /// MaterialBuilds.json order.
    /// </summary>
    public static class RuleBook
    {
        /// <summary>Owner order bases: action card &lt; items &lt; sets &lt; skills &lt; enemy &lt; room.</summary>
        public const int ActionOrder = 0, ItemOrder = 100, SetOrder = 500, SkillOrder = 1000, EnemyOrder = 100_000, RoomOrder = 200_000;

        private sealed class Book
        {
            public long Signature = long.MinValue;
            public bool Building;
            public IReadOnlyList<RuleEntry> Entries = Array.Empty<RuleEntry>();
        }

        private static readonly ConditionalWeakTable<Character, Book> _books = new();

        /// <summary>Equipped slots in owner order (Weapon first, D6.12).</summary>
        public static IEnumerable<(Item? Item, int Slot)> SlotsInOrder(Character hero)
        {
            var eq = hero.Equipment;
            if (eq == null)
                yield break;
            yield return (eq.Weapon, 0);
            yield return (eq.Head, 1);
            yield return (eq.Body, 2);
            yield return (eq.Legs, 3);
            yield return (eq.Feet, 4);
        }

        /// <summary>The hero's active rules, sorted by owner order then written order.</summary>
        public static IReadOnlyList<RuleEntry> For(Character hero)
        {
            var book = _books.GetValue(hero, _ => new Book());
            long sig = Signature(hero);
            if (sig == book.Signature || book.Building)
                return book.Entries;
            book.Building = true;
            try
            {
                book.Entries = Build(hero);
                book.Signature = sig;
            }
            finally
            {
                book.Building = false;
            }
            return book.Entries;
        }

        /// <summary>Drops every cached book (tests, data reloads).</summary>
        public static void Invalidate(Character? hero = null)
        {
            if (hero != null)
                _books.Remove(hero);
            else
                _books.Clear();
            _treeOrder = null;
        }

        private static long Signature(Character hero)
        {
            unchecked
            {
                long h = CrossWires.ModifierSignatureOf(hero);
                h = h * 31 + RuntimeHelpers.GetHashCode(SkillTreeService.Trees);
                h = h * 31 + RuntimeHelpers.GetHashCode(RPGGame.Data.MaterialBuildsLoader.GetAll());
                return h;
            }
        }

        private static List<RuleEntry> Build(Character hero)
        {
            var list = new List<RuleEntry>();
            foreach (var (item, slot) in SlotsInOrder(hero))
            {
                if (item == null)
                    continue;
                var owner = new RuleOwner(RuleOwnerKind.Item, item.Name ?? "item", ItemOrder + slot, item: item);
                int i = 0;
                foreach (var rule in CrossWires.ItemRules(item))
                    list.Add(new RuleEntry(rule, owner, i++));
            }
            // Material sets in their fixed MaterialBuilds.json order. Every set's earn rule is listed; the 2-piece gate is
            // checked when it fires (set counts read rules, so they can't be read while the book is built).
            int setIndex = 0;
            foreach (var build in RPGGame.Data.MaterialBuildsLoader.GetAll())
            {
                int order = SetOrder + setIndex++;
                if (string.IsNullOrWhiteSpace(build.WhenToken) || string.IsNullOrWhiteSpace(build.Keyword))
                    continue;
                var owner = new RuleOwner(RuleOwnerKind.Set, build.Material + " set", order);
                var rule = new CrossWireRule
                {
                    When = build.WhenToken,
                    Verb = "ADD",
                    Store = build.Keyword,
                    Amount = 1,
                    Source = build.Material + " set",
                    Text = build.Synthesis ?? "",
                    SetBuild = build
                };
                list.Add(new RuleEntry(rule, owner, 0));
            }
            foreach (var (node, rules) in CrossWires.LearnedNodeRuleGroups(hero))
            {
                var owner = new RuleOwner(RuleOwnerKind.Skill, node.Name ?? node.Id, SkillOrder + TreePosition(node), node: node);
                int i = 0;
                foreach (var rule in rules)
                    list.Add(new RuleEntry(rule, owner, i++));
            }
            // Coded skill rules (conversion rows, hybrid trees, per-rank pack mods) after the node's written rules.
            foreach (var (node, rule) in SkillRules.Coded(hero))
            {
                var owner = new RuleOwner(RuleOwnerKind.Skill, node.Name ?? node.Id, SkillOrder + TreePosition(node), node: node);
                list.Add(new RuleEntry(rule, owner, 100));
            }
            // Stable: owner order, then written order.
            return list.OrderBy(e => e.Owner.Order).ThenBy(e => e.Index).ToList();
        }

        // ------------------------------------------------------------------ tree order

        private static Dictionary<string, int>? _treeOrder;
        private static object? _treeOrderFor;

        /// <summary>A node's place in tree order: tree, then Core / material branches / Rite, then tier, then position.</summary>
        public static int TreePosition(SkillTreeNodeDefinition node)
        {
            var trees = SkillTreeService.Trees;
            if (_treeOrder == null || !ReferenceEquals(_treeOrderFor, trees))
            {
                var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                int p = 0;
                foreach (var tree in trees?.Trees ?? new List<SkillTreeDefinition>())
                {
                    var ordered = tree.Nodes.Select((n, i) => (n, i))
                        .OrderBy(x => BranchRank(x.n.Branch)).ThenBy(x => x.n.Tier).ThenBy(x => x.i);
                    foreach (var (n, _) in ordered)
                        map.TryAdd(n.Id, p++);
                }
                _treeOrder = map;
                _treeOrderFor = trees;
            }
            return _treeOrder.TryGetValue(node.Id, out int pos) ? pos : _treeOrder.Count;
        }

        private static int BranchRank(string? branch) => branch?.Trim().ToLowerInvariant() switch
        {
            "core" => 0,
            "rite" => 2,
            "confluence" => 3,
            _ => 1
        };
    }
}
