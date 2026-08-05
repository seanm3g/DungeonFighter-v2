using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Orchestrates skill-tree learning, action unlocks, and rebuild hooks.
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
            foreach (string id in progression.LearnedSkillNodeIds)
            {
                var node = Trees.GetNode(id);
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

        public enum NodeViewState
        {
            Learned,
            Available,
            Locked,
            Unaffordable,
            WrongPath
        }

        public static NodeViewState GetNodeState(CharacterProgression progression, SkillTreeNodeDefinition node)
        {
            progression.EnsureSkillTreeRootsGranted();
            if (progression.HasLearnedSkill(node.Id))
                return NodeViewState.Learned;

            WeaponType? ownerPath = null;
            foreach (var tree in Trees.Trees)
            {
                if (tree.Nodes.Any(n => string.Equals(n.Id, node.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    ownerPath = tree.ResolveWeaponType();
                    break;
                }
            }

            var primary = progression.GetPrimaryClassWeaponType();
            if (ownerPath == null || primary == null || primary.Value != ownerPath.Value)
                return NodeViewState.WrongPath;

            foreach (string req in node.Requires)
            {
                if (!progression.HasLearnedSkill(req))
                    return NodeViewState.Locked;
            }

            return progression.GetAvailableSkillPoints(ownerPath.Value) >= node.Cost
                ? NodeViewState.Available
                : NodeViewState.Unaffordable;
        }
    }
}
