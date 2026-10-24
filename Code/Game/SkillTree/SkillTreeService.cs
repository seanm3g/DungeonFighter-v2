using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Orchestrates skill-tree learning, action unlocks, hybrid side-rail nodes, and rebuild hooks.
    /// </summary>
    public static class SkillTreeService
    {
        public static SkillTreesConfig Trees =>
            GameConfiguration.Instance.SkillTrees ?? SkillTreesConfig.CreateEmpty();

        public static bool IsTreeActionName(string? actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName)) return false;
            return Trees.AllUnlockActionNames()
                .Any(n => string.Equals(n, actionName, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<string> GetLearnedUnlockActionNames(CharacterProgression progression)
        {
            progression.EnsureSkillTreeRootsGranted();
            foreach (var kv in progression.LearnedSkillRanks)
            {
                if (kv.Value < 1) continue;
                var node = Trees.GetNode(kv.Key);
                if (node != null && !string.IsNullOrWhiteSpace(node.UnlockActionName))
                    yield return node.UnlockActionName!;
            }
        }

        public static CharacterProgression.LearnSkillResult TryLearn(
            Character character,
            string nodeId,
            bool rebuildActions = true)
        {
            if (character?.Progression == null)
                return CharacterProgression.LearnSkillResult.UnknownNode;

            var result = character.Progression.TryLearnSkillNode(nodeId, requirePrimaryPath: true);
            if (result != CharacterProgression.LearnSkillResult.Success)
                return result;

            if (rebuildActions)
            {
                WeaponType? weaponType = character.Equipment.Weapon is WeaponItem w
                    ? w.WeaponType
                    : (WeaponType?)null;
                character.Actions.AddClassActions(character, character.Progression, weaponType);
            }

            SkillEffectRouter.Instance.RefreshForCharacter(character);
            return result;
        }

        public static void ApplyLearnedSkillActions(Actor entity, CharacterProgression progression)
        {
            progression.EnsureSkillTreeRootsGranted();
            foreach (string actionName in GetLearnedUnlockActionNames(progression).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var action = ActionLoader.GetAction(actionName);
                    if (action == null)
                        continue;
                    if (entity.ActionPool.Any(e =>
                            string.Equals(e.action.Name, action.Name, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    action.IsComboAction = true;
                    entity.AddAction(action, 1.0);
                }
                catch (Exception ex)
                {
                    DebugLogger.LogFormat("SkillTreeService",
                        "Error adding skill action {0}: {1}", actionName, ex.Message);
                }
            }
        }

        public static SkillTreeDefinition? GetPrimaryTree(CharacterProgression progression)
        {
            var primary = progression.GetPrimaryClassWeaponType();
            if (primary == null) return null;
            return Trees.GetTreeForWeapon(primary.Value);
        }

        public static SkillTreeDefinition? GetSecondaryTree(CharacterProgression progression)
        {
            var secondary = progression.GetSecondaryClassWeaponType();
            if (secondary == null) return null;
            return Trees.GetTreeForWeapon(secondary.Value);
        }

        public static WeaponType? FindOwnerPath(SkillTreeNodeDefinition node)
        {
            if (node == null) return null;
            foreach (var tree in Trees.Trees)
            {
                if (tree.Nodes.Any(n => string.Equals(n.Id, node.Id, StringComparison.OrdinalIgnoreCase)))
                    return tree.ResolveWeaponType();
            }
            return null;
        }

        /// <summary>
        /// Any class tree the character has earned points on; each tree spends only its own path's SP.
        /// </summary>
        public static bool CanSpendIntoNode(
            CharacterProgression progression,
            SkillTreeNodeDefinition node,
            WeaponType ownerPath) =>
            progression.GetClassPoints(ownerPath) > 0;

        /// <summary>Class paths with earned points, highest first (the trees the skill screen can switch between).</summary>
        public static IReadOnlyList<WeaponType> GetViewablePaths(CharacterProgression progression) =>
            progression.GetClassPathsSortedByPoints()
                .Where(p => p.Points > 0)
                .Select(p => p.Path)
                .ToList();

        /// <summary>Tree currently shown: <see cref="CharacterProgression.SkillTreeViewPath"/> when it has points, else the primary path.</summary>
        public static WeaponType? GetViewedPath(CharacterProgression progression)
        {
            var view = progression.SkillTreeViewPath;
            if (view != null && progression.GetClassPoints(view.Value) > 0)
                return view;
            return progression.GetPrimaryClassWeaponType();
        }

        /// <summary>Advance the viewed tree to the next path with points. Returns false when only one tree exists.</summary>
        public static bool CycleViewedPath(CharacterProgression progression)
        {
            var paths = GetViewablePaths(progression);
            if (paths.Count < 2)
                return false;
            var current = GetViewedPath(progression);
            int idx = current == null ? -1 : paths.ToList().IndexOf(current.Value);
            progression.SkillTreeViewPath = paths[(idx + 1) % paths.Count];
            return true;
        }

        /// <summary>
        /// Secondary-path nodes tagged for the current primary (Concept A hybrid side rail).
        /// Excludes free roots (tier 0 / cost 0) so the rail stays a short shared list.
        /// </summary>
        public static IReadOnlyList<SkillTreeNodeDefinition> GetSharedRailNodes(CharacterProgression progression)
        {
            var primary = progression.GetPrimaryClassWeaponType();
            var secondaryTree = GetSecondaryTree(progression);
            if (primary == null || secondaryTree == null)
                return Array.Empty<SkillTreeNodeDefinition>();

            // Hybrid tree for the primary/secondary pair replaces the shared-node rail.
            var secondaryPath = progression.GetSecondaryClassWeaponType();
            if (secondaryPath != null && HybridTrees.ForPair(primary.Value, secondaryPath.Value) is HybridInfo hybrid)
            {
                var hybridTree = Trees.Trees.FirstOrDefault(t => string.Equals(t.ClassKey, hybrid.Key, StringComparison.OrdinalIgnoreCase));
                if (hybridTree != null)
                    return OrderNodes(hybridTree.Nodes.Where(n => n.Tier > 0 || n.Cost > 0));
            }

            return secondaryTree.Nodes
                .Where(n => n.IsSharedWith(primary.Value)
                            && !(n.Tier <= 0 && n.Cost <= 0))
                .OrderBy(n => n.Tier)
                .ThenBy(n => n.Branch, StringComparer.OrdinalIgnoreCase)
                .ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static SkillTreeDisplayModel BuildDisplayModel(CharacterProgression progression)
        {
            progression.EnsureSkillTreeRootsGranted();
            var primary = progression.GetPrimaryClassWeaponType();
            var secondary = progression.GetSecondaryClassWeaponType();
            var viewed = GetViewedPath(progression);
            var primaryTree = viewed != null ? Trees.GetTreeForWeapon(viewed.Value) : null;
            // Header pairs the viewed tree with the other half of the top-two duo.
            var otherPath = viewed == primary ? secondary : primary;
            var primaryNodes = primaryTree != null
                ? OrderNodes(primaryTree.Nodes)
                : (IReadOnlyList<SkillTreeNodeDefinition>)Array.Empty<SkillTreeNodeDefinition>();
            var railNodes = GetSharedRailNodes(progression);

            string? duoName = null;
            if (primary != null && secondary != null)
            {
                var pres = GameConfiguration.Instance.ClassPresentation.EnsureNormalized();
                duoName = pres.GetAttributeDuoCoreName(primary.Value, secondary.Value);
            }

            string? railTitle = null;
            if (railNodes.Count > 0 && secondary != null && primary != null)
            {
                var hybrid = HybridTrees.ForPair(primary.Value, secondary.Value);
                railTitle = hybrid != null
                    ? $"{hybrid.Key.ToUpperInvariant()} ({hybrid.Attribute})"
                    : $"SHARED · {secondary.Value}";
            }

            var all = new List<SkillTreeNodeDefinition>(primaryNodes.Count + railNodes.Count);
            all.AddRange(primaryNodes);
            all.AddRange(railNodes);

            return new SkillTreeDisplayModel(
                primaryTree,
                viewed,
                otherPath,
                primaryNodes,
                railNodes,
                all,
                duoName,
                railTitle);
        }

        public static IReadOnlyList<SkillTreeNodeDefinition> OrderNodes(IEnumerable<SkillTreeNodeDefinition> source) =>
            source
                .OrderBy(n => n.Tier)
                .ThenBy(n => n.Branch, StringComparer.OrdinalIgnoreCase)
                .ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

        public enum NodeViewState
        {
            /// <summary>At max rank.</summary>
            Maxed,
            Available,
            Locked,
            Unaffordable,
            WrongPath
        }

        public static NodeViewState GetNodeState(CharacterProgression progression, SkillTreeNodeDefinition node)
        {
            progression.EnsureSkillTreeRootsGranted();
            int rank = progression.GetSkillRank(node.Id);
            int maxRank = Math.Max(1, node.MaxRank);
            if (rank >= maxRank)
                return NodeViewState.Maxed;

            var ownerTree = SkillTreePrerequisites.FindOwnerTree(node);
            if (HybridTrees.ForTree(ownerTree) is HybridInfo hybrid)
            {
                if (!HybridTrees.IsUnlocked(progression, hybrid) || !SkillTreePrerequisites.AreMet(progression, node))
                    return NodeViewState.Locked;
                bool affordable = progression.GetAvailableSkillPoints(hybrid.ParentA) + progression.GetAvailableSkillPoints(hybrid.ParentB) >= node.Cost;
                return affordable ? NodeViewState.Available : NodeViewState.Unaffordable;
            }

            WeaponType? ownerPath = FindOwnerPath(node);
            if (ownerPath == null || !CanSpendIntoNode(progression, node, ownerPath.Value))
                return NodeViewState.WrongPath;

            if (!SkillTreePrerequisites.AreMet(progression, node))
                return NodeViewState.Locked;

            return progression.GetAvailableSkillPoints(ownerPath.Value) >= node.Cost
                ? NodeViewState.Available
                : NodeViewState.Unaffordable;
        }
    }

    /// <summary>Primary tree nodes plus optional Concept A shared side-rail nodes (contiguous selection indices).</summary>
    public sealed class SkillTreeDisplayModel
    {
        public SkillTreeDefinition? PrimaryTree { get; }
        public WeaponType? PrimaryPath { get; }
        public WeaponType? SecondaryPath { get; }
        public IReadOnlyList<SkillTreeNodeDefinition> PrimaryNodes { get; }
        public IReadOnlyList<SkillTreeNodeDefinition> RailNodes { get; }
        public IReadOnlyList<SkillTreeNodeDefinition> AllNodes { get; }
        public string? DuoName { get; }
        public string? RailTitle { get; }
        public int PrimaryCount => PrimaryNodes.Count;
        public bool HasRail => RailNodes.Count > 0;

        public SkillTreeDisplayModel(
            SkillTreeDefinition? primaryTree,
            WeaponType? primaryPath,
            WeaponType? secondaryPath,
            IReadOnlyList<SkillTreeNodeDefinition> primaryNodes,
            IReadOnlyList<SkillTreeNodeDefinition> railNodes,
            IReadOnlyList<SkillTreeNodeDefinition> allNodes,
            string? duoName,
            string? railTitle)
        {
            PrimaryTree = primaryTree;
            PrimaryPath = primaryPath;
            SecondaryPath = secondaryPath;
            PrimaryNodes = primaryNodes;
            RailNodes = railNodes;
            AllNodes = allNodes;
            DuoName = duoName;
            RailTitle = railTitle;
        }

        public bool IsRailIndex(int index) => index >= PrimaryCount && index < AllNodes.Count;

        public int RailIndexOf(int allIndex) => allIndex - PrimaryCount;
    }
}
