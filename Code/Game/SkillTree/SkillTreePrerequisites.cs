using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;
using RPGGame.UI.Avalonia.Renderers;

namespace RPGGame
{
    /// <summary>
    /// Prerequisites from tree layout position: on each branch, skills unlock in root→tip order
    /// (content Y toward the roots first). Each skill needs rank ≥ 1 on the skill immediately before it;
    /// the first skill on a branch needs the tip of the Core trunk. The Core trunk forks instead: skills sharing a
    /// Core tier are side-by-side choices, each opened by any skill on the Core tier below.
    /// Confluence / multi-req nodes still AND their named parents.
    /// </summary>
    public static class SkillTreePrerequisites
    {
        private static bool IsConfluenceBranch(string? branch) =>
            string.Equals(branch, "Confluence", StringComparison.OrdinalIgnoreCase);

        public static bool UsesExplicitRequires(SkillTreeNodeDefinition node)
        {
            if (node == null) return false;
            if (IsConfluenceBranch(node.Branch))
                return true;
            return node.Requires != null && node.Requires.Count > 1;
        }

        public static SkillTreeDefinition? FindOwnerTree(SkillTreeNodeDefinition node)
        {
            if (node == null) return null;
            var trees = GameConfiguration.Instance.SkillTrees;
            if (trees?.Trees == null) return null;
            foreach (var tree in trees.Trees)
            {
                if (tree.Nodes != null &&
                    tree.Nodes.Any(n => string.Equals(n.Id, node.Id, StringComparison.OrdinalIgnoreCase)))
                    return tree;
            }
            return null;
        }

        /// <summary>
        /// Same-branch skills ordered from the root outward (must unlock in this order).
        /// Uses layout content Y so stacked same-tier cards follow on-screen sequence.
        /// </summary>
        public static IReadOnlyList<SkillTreeNodeDefinition> GetBranchSequenceRootToTip(
            SkillTreeDefinition tree,
            string? branch)
        {
            if (tree?.Nodes == null || tree.Nodes.Count == 0)
                return Array.Empty<SkillTreeNodeDefinition>();

            var layout = SkillTreeRenderer.ComputeContentLayout(tree.Nodes);
            return tree.Nodes
                .Where(n => string.Equals(n.Branch, branch, StringComparison.OrdinalIgnoreCase))
                .Where(n => !IsConfluenceBranch(n.Branch) || n.Tier > 0) // confluence handled separately
                .OrderBy(n => n.Tier)
                .ThenByDescending(n => layout.Tops.TryGetValue(n.Id, out int top) ? top : 0)
                .ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Immediate predecessor on the branch toward the roots, or null if this is first on the branch.</summary>
        public static SkillTreeNodeDefinition? GetImmediatePredecessor(
            SkillTreeDefinition tree,
            SkillTreeNodeDefinition node)
        {
            if (tree == null || node == null) return null;
            var seq = GetBranchSequenceRootToTip(tree, node.Branch);
            for (int i = 0; i < seq.Count; i++)
            {
                if (!string.Equals(seq[i].Id, node.Id, StringComparison.OrdinalIgnoreCase))
                    continue;
                return i > 0 ? seq[i - 1] : null;
            }
            return null;
        }

        /// <summary>Depth gate: the node's tier needs enough class points earned in the owning class.</summary>
        public static bool IsTierOpen(CharacterProgression progression, SkillTreeDefinition tree, SkillTreeNodeDefinition node)
        {
            if (HybridTrees.ForTree(tree) is HybridInfo hybrid)
                return HybridTrees.LowerParentPoints(progression, hybrid) >= HybridTrees.TierGate(node.Tier);

            int gate = SkillTreesConfig.TierPointGate(node.Tier);
            if (gate <= 0)
                return true;
            var path = tree.ResolveWeaponType();
            return path != null && progression.GetClassPoints(path.Value) >= gate;
        }

        public static bool AreMet(CharacterProgression progression, SkillTreeNodeDefinition node)
        {
            if (progression == null || node == null)
                return false;
            if (node.Tier <= 0)
                return true;

            var tree = FindOwnerTree(node);
            if (tree?.Nodes == null || tree.Nodes.Count == 0)
                return false;

            if (!IsTierOpen(progression, tree, node))
                return false;

            if (ExcludedBy(progression, node) != null)
                return false;

            if (UsesExplicitRequires(node))
            {
                foreach (string reqId in node.Requires ?? (IReadOnlyList<string>)Array.Empty<string>())
                {
                    if (string.IsNullOrWhiteSpace(reqId))
                        continue;
                    if (!progression.HasLearnedSkill(reqId))
                        return false;
                }
                return true;
            }

            if (IsCoreBranch(node.Branch))
            {
                // Trunk forks: any skill on the Core tier below opens every skill on this tier.
                var options = GetCorePredecessorOptions(tree, node);
                return options.Count == 0
                    ? HasRootLearned(progression, tree)
                    : options.Any(o => progression.GetSkillRank(o.Id) >= 1);
            }

            if (UsesBands(tree))
            {
                // Class material / Rite branches are tier bands too (2026-11-16): skills sharing a tier are parallel
                // options, and each needs any one skill from the nearest lower tier of its branch.
                var band = GetBandPredecessorOptions(tree, node);
                if (band.Count == 0)
                {
                    var tip = GetTrunkTip(tree);
                    return tip != null && progression.HasLearnedSkill(tip.Id);
                }
                return band.Any(o => progression.GetSkillRank(o.Id) >= 1);
            }

            var pred = GetImmediatePredecessor(tree, node);
            if (pred == null)
            {
                // First skill on a branch — needs the trunk tip (the root when the tree has no trunk).
                var baseNode = GetTrunkTip(tree);
                return baseNode != null && progression.HasLearnedSkill(baseNode.Id);
            }

            return progression.GetSkillRank(pred.Id) >= 1;
        }

        private static bool IsCoreBranch(string? branch) =>
            string.Equals(branch, "Core", StringComparison.OrdinalIgnoreCase);

        /// <summary>Class trees use tier bands on every branch; hybrid trees keep their single-file branches.</summary>
        private static bool UsesBands(SkillTreeDefinition tree) => HybridTrees.ForTree(tree) == null;

        /// <summary>Same-branch skills on the nearest lower tier (any one opens this skill). Empty for the first band.</summary>
        public static IReadOnlyList<SkillTreeNodeDefinition> GetBandPredecessorOptions(
            SkillTreeDefinition tree,
            SkillTreeNodeDefinition node)
        {
            if (tree?.Nodes == null || node == null || node.Tier <= 0)
                return Array.Empty<SkillTreeNodeDefinition>();
            var below = tree.Nodes
                .Where(n => string.Equals(n.Branch, node.Branch, StringComparison.OrdinalIgnoreCase) && n.Tier < node.Tier)
                .ToList();
            if (below.Count == 0)
                return Array.Empty<SkillTreeNodeDefinition>();
            int tier = below.Max(n => n.Tier);
            return below.Where(n => n.Tier == tier).ToList();
        }

        /// <summary>The learned skill that shuts <paramref name="node"/> out (either side's <c>excludes</c>), or null.</summary>
        public static SkillTreeNodeDefinition? ExcludedBy(CharacterProgression progression, SkillTreeNodeDefinition node)
        {
            if (progression == null || node == null)
                return null;
            var trees = SkillTreeService.Trees;
            foreach (string id in node.Excludes ?? new List<string>())
                if (!string.IsNullOrWhiteSpace(id) && progression.HasLearnedSkill(id))
                    return trees?.GetNode(id);
            foreach (var kv in progression.LearnedSkillRanks)
            {
                if (kv.Value <= 0)
                    continue;
                var other = trees?.GetNode(kv.Key);
                if (other?.Excludes != null && other.Excludes.Any(x => string.Equals(x, node.Id, StringComparison.OrdinalIgnoreCase)))
                    return other;
            }
            return null;
        }

        /// <summary>Every skill that shuts this one out or that this one shuts out (for card text).</summary>
        public static IReadOnlyList<SkillTreeNodeDefinition> ExclusionPartners(SkillTreeNodeDefinition node)
        {
            var trees = SkillTreeService.Trees;
            if (node == null || trees?.Trees == null)
                return Array.Empty<SkillTreeNodeDefinition>();
            var ids = new HashSet<string>(node.Excludes ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (var other in trees.Trees.SelectMany(t => t.Nodes))
                if (other.Excludes != null && other.Excludes.Any(x => string.Equals(x, node.Id, StringComparison.OrdinalIgnoreCase)))
                    ids.Add(other.Id);
            return ids.Select(id => trees.GetNode(id)).Where(n => n != null).Cast<SkillTreeNodeDefinition>().ToList();
        }

        /// <summary>
        /// The Core trunk is a chain of tier bands: skills sharing a Core tier sit side by side as a choice, and each
        /// needs any one skill from the nearest Core tier below it (so a band of two or three is an either-or fork that
        /// merges again on the next single skill). Empty for the root and for non-Core skills.
        /// </summary>
        public static IReadOnlyList<SkillTreeNodeDefinition> GetCorePredecessorOptions(
            SkillTreeDefinition tree,
            SkillTreeNodeDefinition node)
        {
            if (tree?.Nodes == null || node == null || !IsCoreBranch(node.Branch) || node.Tier <= 0)
                return Array.Empty<SkillTreeNodeDefinition>();
            var below = tree.Nodes.Where(n => IsCoreBranch(n.Branch) && n.Tier < node.Tier).ToList();
            if (below.Count == 0)
                return Array.Empty<SkillTreeNodeDefinition>();
            int tier = below.Max(n => n.Tier);
            return below.Where(n => n.Tier == tier).ToList();
        }

        /// <summary>What a skill waits on, for status text: its Core options, its branch predecessor, or the trunk tip.</summary>
        public static IReadOnlyList<SkillTreeNodeDefinition> GetPredecessorOptions(
            SkillTreeDefinition tree,
            SkillTreeNodeDefinition node)
        {
            if (IsCoreBranch(node?.Branch))
                return GetCorePredecessorOptions(tree, node!);
            if (UsesBands(tree))
            {
                var band = GetBandPredecessorOptions(tree, node!);
                if (band.Count > 0)
                    return band;
                var trunk = GetTrunkTip(tree);
                return trunk != null && node!.Tier > 0 ? new[] { trunk } : Array.Empty<SkillTreeNodeDefinition>();
            }
            var pred = GetImmediatePredecessor(tree, node!);
            if (pred != null)
                return new[] { pred };
            var tip = GetTrunkTip(tree);
            return tip != null && node!.Tier > 0 ? new[] { tip } : Array.Empty<SkillTreeNodeDefinition>();
        }

        /// <summary>
        /// Top of the Core trunk (the single-file opening of a class tree), or the root when the tree has no trunk.
        /// Every other branch grows from here.
        /// </summary>
        public static SkillTreeNodeDefinition? GetTrunkTip(SkillTreeDefinition tree) =>
            tree?.Nodes?
                .Where(n => string.Equals(n.Branch, "Core", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(n => n.Tier)
                .FirstOrDefault()
            ?? tree?.Nodes?.FirstOrDefault(n => n.Tier == 0);

        public static bool HasRootLearned(CharacterProgression progression, SkillTreeDefinition tree)
        {
            var root = tree.Nodes?.FirstOrDefault(n => n.Tier == 0);
            return root != null && progression.HasLearnedSkill(root.Id);
        }

        public static string FormatRequirementSummary(SkillTreeNodeDefinition node)
        {
            if (node == null || node.Tier <= 0)
                return "";

            if (UsesExplicitRequires(node))
            {
                var names = (node.Requires ?? new List<string>())
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => SkillTreeService.Trees.GetNode(id)?.Name ?? id);
                return "Requires: " + string.Join(" and ", names);
            }

            var tree = FindOwnerTree(node);
            if (tree == null)
                return "Requires: earlier skill on this branch";

            string excl = ExclusionPartners(node) is { Count: > 0 } partners
                ? " · Either-or with " + string.Join(", ", partners.Select(p => p.Name))
                : "";

            if (IsCoreBranch(node.Branch))
            {
                var options = GetCorePredecessorOptions(tree, node);
                return (options.Count == 0 ? "Requires: path root" : "Requires: " + string.Join(" or ", options.Select(o => o.Name))) + excl;
            }

            if (UsesBands(tree))
            {
                var band = GetBandPredecessorOptions(tree, node);
                if (band.Count > 0)
                    return "Requires: " + string.Join(" or ", band.Select(o => o.Name)) + excl;
                return (GetTrunkTip(tree) is { Tier: > 0 } trunk ? $"Requires: {trunk.Name}" : "Requires: path root") + excl;
            }

            var pred = GetImmediatePredecessor(tree, node);
            if (pred == null)
                return (GetTrunkTip(tree) is { Tier: > 0 } tip ? $"Requires: {tip.Name}" : "Requires: path root") + excl;

            return $"Requires: {pred.Name}" + excl;
        }

    }
}
