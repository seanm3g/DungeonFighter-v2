using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Audio;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Renderers;

namespace RPGGame
{
    /// <summary>
    /// GameLoop hub handler for viewing and permanently learning skill-tree nodes (no respec).
    /// </summary>
    public class SkillTreeMenuHandler
    {
        private readonly GameStateManager stateManager;
        private readonly IUIManager? customUIManager;
        private int selectedIndex;
        private int scrollOffset;
        private string? statusMessage;
        private int lastViewportHeight = 16;

        public delegate void OnShowGameLoop();
        public delegate void OnShowMessage(string message);

        public event OnShowGameLoop? ShowGameLoopEvent;
        public event OnShowMessage? ShowMessageEvent;

        public SkillTreeMenuHandler(GameStateManager stateManager, IUIManager? customUIManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
        }

        /// <summary>Reset selection/scroll before opening via <see cref="GameScreenCoordinator"/>.</summary>
        public void PrepareForShow()
        {
            selectedIndex = 0;
            // Pin toward bottom of content so roots are visible; ClampScrollOffset applies once height is known.
            scrollOffset = int.MaxValue;
            statusMessage = null;
        }

        public void ShowSkillTree()
        {
            if (stateManager.CurrentPlayer == null)
            {
                ShowMessageEvent?.Invoke("No character loaded.");
                return;
            }

            PrepareForShow();

            // Prefer the same clear+transition path Inventory uses so the GameLoop menu does not ghost.
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                var player = stateManager.CurrentPlayer;
                RPGGame.UI.Avalonia.Transitions.ScreenTransitionProtocol.TransitionToMenuScreen(
                    stateManager,
                    canvasUI,
                    GameState.SkillTree,
                    (ui) => ui.RenderSkillTree(player, selectedIndex, scrollOffset, statusMessage),
                    character: player,
                    clearEnemyContext: true,
                    clearDungeonContext: true);
                return;
            }

            stateManager.TransitionToState(GameState.SkillTree);
            Render(ensureSelectionVisible: false);
        }

        public void Refresh()
        {
            if (stateManager.CurrentState != GameState.SkillTree)
                return;
            Render(ensureSelectionVisible: false);
        }

        public void HandleMenuInput(string input)
        {
            var player = stateManager.CurrentPlayer;
            if (player == null) return;

            string trimmed = (input ?? "").Trim();
            if (string.Equals(trimmed, "0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "esc", StringComparison.OrdinalIgnoreCase))
            {
                statusMessage = null;
                stateManager.TransitionToState(GameState.GameLoop);
                ShowGameLoopEvent?.Invoke();
                return;
            }

            var nodes = GetDisplayNodes(player);
            if (nodes.Count == 0)
            {
                statusMessage = "Equip a weapon and earn Path Points to open a skill tree.";
                Render(ensureSelectionVisible: false);
                return;
            }

            if (TryScroll(trimmed, nodes))
            {
                statusMessage = null;
                Render(ensureSelectionVisible: false);
                return;
            }

            if (TryMoveSelection(trimmed, nodes))
            {
                statusMessage = null;
                Render(ensureSelectionVisible: true);
                return;
            }

            if (string.Equals(trimmed, "l", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "enter", StringComparison.OrdinalIgnoreCase))
            {
                TryLearnSelected(player, nodes);
                return;
            }

            if (int.TryParse(trimmed, out int choice) && choice >= 1 && choice <= nodes.Count)
            {
                selectedIndex = choice - 1;
                statusMessage = null;
                Render(ensureSelectionVisible: true);
                return;
            }

            statusMessage = "Click a node or use arrows. L to learn, PgUp/PgDn to scroll, 0 to return.";
            Render(ensureSelectionVisible: false);
        }

        private bool TryScroll(string input, IReadOnlyList<SkillTreeNodeDefinition> nodes)
        {
            int contentHeight = SkillTreeRenderer.GetTreeContentHeight(nodes);
            int viewport = Math.Max(SkillTreeRenderer.NodeHeight, lastViewportHeight);
            string key = input.ToLowerInvariant();
            int step = SkillTreeRenderer.TierStride;
            int delta = key switch
            {
                "pageup" => -Math.Max(step, (viewport / step) * step),
                "pagedown" => Math.Max(step, (viewport / step) * step),
                _ => 0
            };
            if (delta == 0)
                return false;

            scrollOffset = SkillTreeRenderer.AlignScrollToTier(
                SkillTreeRenderer.ClampScrollOffset(scrollOffset + delta, contentHeight, viewport));
            return true;
        }

        private bool TryMoveSelection(string input, IReadOnlyList<SkillTreeNodeDefinition> nodes)
        {
            string key = input.ToLowerInvariant();
            var branches = SkillTreeRenderer.BuildBranchOrder(nodes);
            int centerCol = Math.Max(0, branches.Count / 2);

            int ColOf(SkillTreeNodeDefinition n)
            {
                if (string.Equals(n.Branch, "Core", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(n.Branch, "Confluence", StringComparison.OrdinalIgnoreCase))
                    return centerCol;
                for (int i = 0; i < branches.Count; i++)
                {
                    if (string.Equals(branches[i], n.Branch, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
                return centerCol;
            }

            var current = nodes[Math.Clamp(selectedIndex, 0, nodes.Count - 1)];
            int curCol = ColOf(current);
            int curTier = current.Tier;

            if (key is "up" or "w")
            {
                // Visually up = higher tier (tree grows upward).
                var candidates = nodes
                    .Select((n, i) => (n, i))
                    .Where(t => t.n.Tier > curTier)
                    .OrderBy(t => t.n.Tier)
                    .ThenBy(t => Math.Abs(ColOf(t.n) - curCol))
                    .ToList();
                if (candidates.Count == 0)
                    return true; // consumed; already at top
                selectedIndex = candidates[0].i;
                return true;
            }

            if (key is "down" or "s")
            {
                // Visually down = lower tier (toward roots).
                var candidates = nodes
                    .Select((n, i) => (n, i))
                    .Where(t => t.n.Tier < curTier)
                    .OrderByDescending(t => t.n.Tier)
                    .ThenBy(t => Math.Abs(ColOf(t.n) - curCol))
                    .ToList();
                if (candidates.Count == 0)
                    return true;
                selectedIndex = candidates[0].i;
                return true;
            }

            if (key is "left" or "a")
            {
                var sameTier = nodes
                    .Select((n, i) => (n, i))
                    .Where(t => t.n.Tier == curTier && ColOf(t.n) < curCol)
                    .OrderByDescending(t => ColOf(t.n))
                    .ToList();
                if (sameTier.Count == 0)
                    return true;
                selectedIndex = sameTier[0].i;
                return true;
            }

            if (key is "right" or "d")
            {
                var sameTier = nodes
                    .Select((n, i) => (n, i))
                    .Where(t => t.n.Tier == curTier && ColOf(t.n) > curCol)
                    .OrderBy(t => ColOf(t.n))
                    .ToList();
                if (sameTier.Count == 0)
                    return true;
                selectedIndex = sameTier[0].i;
                return true;
            }

            return false;
        }

        private void EnsureSelectedVisible(IReadOnlyList<SkillTreeNodeDefinition> nodes)
        {
            if (nodes.Count == 0) return;
            selectedIndex = Math.Clamp(selectedIndex, 0, nodes.Count - 1);
            var node = nodes[selectedIndex];
            int top = SkillTreeRenderer.GetNodeTopCell(node, nodes);
            int bottom = SkillTreeRenderer.GetNodeBottomCell(node, nodes);
            int viewport = Math.Max(SkillTreeRenderer.NodeHeight, lastViewportHeight);
            scrollOffset = SkillTreeRenderer.EnsureNodeVisible(scrollOffset, top, bottom, viewport);
            int contentHeight = SkillTreeRenderer.GetTreeContentHeight(nodes);
            scrollOffset = SkillTreeRenderer.AlignScrollToTier(
                SkillTreeRenderer.ClampScrollOffset(scrollOffset, contentHeight, viewport));
        }

        private void TryLearnSelected(Character player, IReadOnlyList<SkillTreeNodeDefinition> nodes)
        {
            selectedIndex = Math.Clamp(selectedIndex, 0, Math.Max(0, nodes.Count - 1));
            var node = nodes[selectedIndex];
            var state = SkillTreeService.GetNodeState(player.Progression, node);
            if (state == SkillTreeService.NodeViewState.Learned)
            {
                statusMessage = $"{node.Name} is already learned.";
                Render(ensureSelectionVisible: false);
                return;
            }
            if (state == SkillTreeService.NodeViewState.Locked)
            {
                statusMessage = $"{node.Name} requires earlier nodes.";
                Render(ensureSelectionVisible: false);
                return;
            }
            if (state == SkillTreeService.NodeViewState.Unaffordable)
            {
                statusMessage = $"Need {node.Cost} Path Points for {node.Name}.";
                Render(ensureSelectionVisible: false);
                return;
            }
            if (state == SkillTreeService.NodeViewState.WrongPath)
            {
                statusMessage = "Only your primary path tree can be spent into.";
                Render(ensureSelectionVisible: false);
                return;
            }

            var result = SkillTreeService.TryLearn(player, node.Id);
            if (result == CharacterProgression.LearnSkillResult.Success)
            {
                statusMessage = $"Learned {node.Name} (−{node.Cost} Path Points).";
                AudioCues.Trigger(AudioCue.Menu_Confirm);
            }
            else
            {
                statusMessage = $"Could not learn {node.Name} ({result}).";
            }
            Render(ensureSelectionVisible: false);
        }

        private static IReadOnlyList<SkillTreeNodeDefinition> GetDisplayNodes(Character player)
        {
            var tree = SkillTreeService.GetPrimaryTree(player.Progression);
            if (tree == null) return Array.Empty<SkillTreeNodeDefinition>();
            return SkillTreeRenderer.OrderNodes(tree.Nodes);
        }

        private void Render(bool ensureSelectionVisible)
        {
            var player = stateManager.CurrentPlayer;
            if (player == null) return;

            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                var nodes = GetDisplayNodes(player);
                if (nodes.Count > 0)
                {
                    selectedIndex = Math.Clamp(selectedIndex, 0, nodes.Count - 1);
                    lastViewportHeight = Math.Max(SkillTreeRenderer.NodeHeight, SkillTreeRenderer.LastViewportHeight);
                    if (ensureSelectionVisible)
                        EnsureSelectedVisible(nodes);
                }
                canvasUI.RenderSkillTree(player, selectedIndex, scrollOffset, statusMessage);
                lastViewportHeight = Math.Max(SkillTreeRenderer.NodeHeight, SkillTreeRenderer.LastViewportHeight);
                scrollOffset = SkillTreeRenderer.LastAppliedScrollOffset;
            }
        }
    }
}
