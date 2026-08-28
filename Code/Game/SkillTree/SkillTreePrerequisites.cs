using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;
using RPGGame.UI.Avalonia.Renderers;

namespace RPGGame
{
    /// <summary>
    /// Prerequisites from tree layout position: on each branch, skills unlock in root→tip order
    /// (content Y toward the roots first). Each skill needs rank ≥ 1 on the skill immediately before it.
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

        public static bool AreMet(CharacterProgression progression, SkillTreeNodeDefinition node)
        {
            if (progression == null || node == null)
                return false;
            if (node.Tier <= 0)
                return true;

            var tree = FindOwnerTree(node);
            if (tree?.Nodes == null || tree.Nodes.Count == 0)
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

            var pred = GetImmediatePredecessor(tree, node);
            if (pred == null)
            {
                // First skill on this branch (usually tier 1) — needs path root.
                return HasRootLearned(progression, tree);
            }

            return progression.GetSkillRank(pred.Id) >= 1;
        }

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

            var pred = GetImmediatePredecessor(tree, node);
            if (pred == null)
                return "Requires: path root";

            return $"Requires: {pred.Name}";
        }

        public static IReadOnlyList<string> GetConnectorParentIds(SkillTreeNodeDefinition node)
        {
            if (node == null || node.Tier <= 0)
                return Array.Empty<string>();

            if (UsesExplicitRequires(node))
                return (node.Requires ?? new List<string>())
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .ToList();

            var tree = FindOwnerTree(node);
            if (tree?.Nodes == null)
                return Array.Empty<string>();

            var pred = GetImmediatePredecessor(tree, node);
            if (pred != null)
                return new[] { pred.Id };

            var root = tree.Nodes.FirstOrDefault(n => n.Tier == 0);
            return root != null ? new[] { root.Id } : Array.Empty<string>();
        }
    }
}
