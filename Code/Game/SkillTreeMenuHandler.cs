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
    /// Concept A: primary tree plus optional shared/hybrid side rail from the secondary path.
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
                // Second paint: LastViewportHeight is known so selected root stays on-screen.
                Render(ensureSelectionVisible: true);
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

            var model = SkillTreeService.BuildDisplayModel(player.Progression);
            var nodes = model.AllNodes;
            if (nodes.Count == 0)
            {
                statusMessage = "Equip a weapon and earn Skill Points to open a skill tree.";
                Render(ensureSelectionVisible: false);
                return;
            }

            int beforeSelection = selectedIndex;

            if (TryScroll(trimmed, model))
            {
                statusMessage = null;
                Render(ensureSelectionVisible: false);
                return;
            }

            if (TryMoveSelection(trimmed, model))
            {
                statusMessage = null;
                bool selectionChanged = selectedIndex != beforeSelection;
                Render(ensureSelectionVisible: selectionChanged);
                return;
            }

            if (string.Equals(trimmed, "l", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "enter", StringComparison.OrdinalIgnoreCase))
            {
                TryLearnSelected(player, model);
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

        private bool TryScroll(string input, SkillTreeDisplayModel model)
        {
            string key = input.ToLowerInvariant();
            int step = SkillTreeRenderer.ScrollStep;
            int viewport = Math.Max(SkillTreeRenderer.NodeHeight, lastViewportHeight);
            int delta = key switch
            {
                "pageup" => -Math.Max(step, (viewport / step) * step),
                "pagedown" => Math.Max(step, (viewport / step) * step),
                _ => 0
            };
            if (delta == 0)
                return false;
            return TryScrollBy(delta, model);
        }

        private bool TryScrollBy(int delta, SkillTreeDisplayModel model)
        {
            int contentHeight = SkillTreeRenderer.GetDisplayContentHeight(model);
            int viewport = Math.Max(SkillTreeRenderer.NodeHeight, lastViewportHeight);
            int before = scrollOffset;
            scrollOffset = SkillTreeRenderer.NormalizeScrollOffset(scrollOffset + delta, contentHeight, viewport);
            return scrollOffset != before;
        }

        private bool TryMoveSelection(string input, SkillTreeDisplayModel model)
        {
            string key = input.ToLowerInvariant();
            var nodes = model.AllNodes;
            if (nodes.Count == 0) return false;

            int ColOf(int index) => SkillTreeRenderer.GetColumnForDisplay(model, index);
            int TopOf(int index) => SkillTreeRenderer.GetNodeTopForDisplay(model, index);

            selectedIndex = Math.Clamp(selectedIndex, 0, nodes.Count - 1);
            int curCol = ColOf(selectedIndex);
            int curTop = TopOf(selectedIndex);
            int curTier = nodes[selectedIndex].Tier;

            if (key is "up" or "w")
            {
                var candidates = Enumerable.Range(0, nodes.Count)
                    .Where(i => TopOf(i) < curTop)
                    .OrderByDescending(TopOf)
                    .ThenBy(i => Math.Abs(ColOf(i) - curCol))
                    .ToList();
                if (candidates.Count > 0)
                {
                    selectedIndex = candidates[0];
                    return true;
                }
                return TryScrollBy(-SkillTreeRenderer.ScrollStep, model);
            }

            if (key is "down" or "s")
            {
                var candidates = Enumerable.Range(0, nodes.Count)
                    .Where(i => TopOf(i) > curTop)
                    .OrderBy(TopOf)
                    .ThenBy(i => Math.Abs(ColOf(i) - curCol))
                    .ToList();
                if (candidates.Count > 0)
                {
                    selectedIndex = candidates[0];
                    return true;
                }
                return TryScrollBy(SkillTreeRenderer.ScrollStep, model);
            }

            if (key is "left" or "a")
            {
                var candidates = Enumerable.Range(0, nodes.Count)
                    .Where(i => ColOf(i) < curCol)
                    .OrderByDescending(ColOf)
                    .ThenBy(i => Math.Abs(TopOf(i) - curTop))
                    .ThenBy(i => Math.Abs(nodes[i].Tier - curTier))
                    .ToList();
                if (candidates.Count == 0)
                    return true;
                selectedIndex = candidates[0];
                return true;
            }

            if (key is "right" or "d")
            {
                var candidates = Enumerable.Range(0, nodes.Count)
                    .Where(i => ColOf(i) > curCol)
                    .OrderBy(ColOf)
                    .ThenBy(i => Math.Abs(TopOf(i) - curTop))
                    .ThenBy(i => Math.Abs(nodes[i].Tier - curTier))
                    .ToList();
                if (candidates.Count == 0)
                    return true;
                selectedIndex = candidates[0];
                return true;
            }

            return false;
        }

        private void EnsureSelectedVisible(SkillTreeDisplayModel model)
        {
            if (model.AllNodes.Count == 0) return;
            selectedIndex = Math.Clamp(selectedIndex, 0, model.AllNodes.Count - 1);
            int top = SkillTreeRenderer.GetNodeTopForDisplay(model, selectedIndex);
            int bottom = SkillTreeRenderer.GetNodeBottomForDisplay(model, selectedIndex);
            int viewport = Math.Max(SkillTreeRenderer.NodeHeight, lastViewportHeight);
            scrollOffset = SkillTreeRenderer.EnsureNodeVisible(scrollOffset, top, bottom, viewport);
            int contentHeight = SkillTreeRenderer.GetDisplayContentHeight(model);
            scrollOffset = SkillTreeRenderer.NormalizeScrollOffset(scrollOffset, contentHeight, viewport);
        }

        private void TryLearnSelected(Character player, SkillTreeDisplayModel model)
        {
            var nodes = model.AllNodes;
            selectedIndex = Math.Clamp(selectedIndex, 0, Math.Max(0, nodes.Count - 1));
            var node = nodes[selectedIndex];
            var state = SkillTreeService.GetNodeState(player.Progression, node);
            if (state == SkillTreeService.NodeViewState.Maxed)
            {
                statusMessage = $"{node.Name} is fully ranked ({node.MaxRank}/{node.MaxRank}).";
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
                var owner = SkillTreeService.FindOwnerPath(node);
                statusMessage = owner != null
                    ? $"Need {node.Cost} {owner} Skill Points for {node.Name}."
                    : $"Need {node.Cost} Skill Points for {node.Name}.";
                Render(ensureSelectionVisible: false);
                return;
            }
            if (state == SkillTreeService.NodeViewState.WrongPath)
            {
                statusMessage = model.IsRailIndex(selectedIndex)
                    ? "This shared skill is not spendable for your current paths."
                    : "Only your primary path tree (or shared secondary rail) can be spent into.";
                Render(ensureSelectionVisible: false);
                return;
            }

            var result = SkillTreeService.TryLearn(player, node.Id);
            if (result == CharacterProgression.LearnSkillResult.Success)
            {
                int afterRank = player.Progression.GetSkillRank(node.Id);
                var owner = SkillTreeService.FindOwnerPath(node);
                string pathBit = owner != null ? $" {owner}" : "";
                statusMessage = $"{node.Name} rank {afterRank}/{Math.Max(1, node.MaxRank)} (−{node.Cost}{pathBit} SP).";
                AudioCues.Trigger(AudioCue.Menu_Confirm);
            }
            else if (result == CharacterProgression.LearnSkillResult.MaxRankReached)
            {
                statusMessage = $"{node.Name} is fully ranked.";
            }
            else
            {
                statusMessage = $"Could not learn {node.Name} ({result}).";
            }
            Render(ensureSelectionVisible: false);
        }

        private void Render(bool ensureSelectionVisible)
        {
            var player = stateManager.CurrentPlayer;
            if (player == null) return;

            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                var model = SkillTreeService.BuildDisplayModel(player.Progression);
                if (model.AllNodes.Count > 0)
                {
                    selectedIndex = Math.Clamp(selectedIndex, 0, model.AllNodes.Count - 1);
                    lastViewportHeight = Math.Max(SkillTreeRenderer.NodeHeight, SkillTreeRenderer.LastViewportHeight);
                    if (ensureSelectionVisible)
                        EnsureSelectedVisible(model);
                }
                canvasUI.RenderSkillTree(player, selectedIndex, scrollOffset, statusMessage);
                lastViewportHeight = Math.Max(SkillTreeRenderer.NodeHeight, SkillTreeRenderer.LastViewportHeight);
                scrollOffset = SkillTreeRenderer.LastAppliedScrollOffset;
            }
        }
    }
}
