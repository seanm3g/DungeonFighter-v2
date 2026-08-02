using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Spatial skill-tree layout: tier rows × branch columns, boxed nodes, ASCII connectors, vertical scroll.
    /// Progression grows upward (tier 0 / roots at the bottom, higher tiers above).
    /// </summary>
    public class SkillTreeRenderer
    {
        public const int NodeHeight = 4;
        public const int TierGap = 2;
        public const int TierStride = NodeHeight + TierGap;

        private const int DetailReserve = 10;
        private const int FooterReserve = 3;
        private const int MinNodeWidth = 12;
        private const int MaxNodeWidth = 18;

        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;

        public SkillTreeRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
        }

        public static int LastViewportHeight { get; private set; } = 16;
        public static int LastContentHeight { get; private set; }
        public static int LastAppliedScrollOffset { get; private set; }

        /// <summary>Viewport height from remaining center-panel space after the header already drawn.</summary>
        public static int GetTreeViewportHeight(int panelHeight, int headerLinesUsed)
        {
            int usable = panelHeight - Math.Max(0, headerLinesUsed) - DetailReserve - FooterReserve;
            return Math.Max(NodeHeight, usable);
        }

        public static int GetMaxTier(IReadOnlyList<SkillTreeNodeDefinition> nodes) =>
            nodes == null || nodes.Count == 0 ? 0 : nodes.Max(n => n.Tier);

        public static int GetTreeContentHeight(IReadOnlyList<SkillTreeNodeDefinition> nodes)
        {
            if (nodes == null || nodes.Count == 0)
                return 0;
            int maxTier = GetMaxTier(nodes);
            return (maxTier + 1) * NodeHeight + maxTier * TierGap;
        }

        public static int ClampScrollOffset(int scrollOffset, int contentHeight, int viewportHeight)
        {
            int max = Math.Max(0, contentHeight - viewportHeight);
            return Math.Clamp(scrollOffset, 0, max);
        }

        /// <summary>Snap scroll to tier row boundaries so cards never straddle the viewport edge.</summary>
        public static int AlignScrollToTier(int scrollOffset) =>
            Math.Max(0, (scrollOffset / TierStride) * TierStride);

        public static int EnsureNodeVisible(int scrollOffset, int nodeTop, int nodeBottom, int viewportHeight)
        {
            if (nodeTop < scrollOffset)
                return AlignScrollToTier(nodeTop);
            if (nodeBottom > scrollOffset + viewportHeight)
                return AlignScrollToTier(Math.Max(0, nodeBottom - viewportHeight));
            return AlignScrollToTier(scrollOffset);
        }

        /// <summary>Content-space top row for a node (roots at bottom, higher tiers toward y=0).</summary>
        public static int GetNodeTopCell(SkillTreeNodeDefinition node, int maxTier) =>
            (maxTier - node.Tier) * TierStride;

        public static int GetNodeTopCell(SkillTreeNodeDefinition node, IReadOnlyList<SkillTreeNodeDefinition> nodes) =>
            GetNodeTopCell(node, GetMaxTier(nodes));

        public static int GetNodeBottomCell(SkillTreeNodeDefinition node, int maxTier) =>
            GetNodeTopCell(node, maxTier) + NodeHeight;

        public static int GetNodeBottomCell(SkillTreeNodeDefinition node, IReadOnlyList<SkillTreeNodeDefinition> nodes) =>
            GetNodeBottomCell(node, GetMaxTier(nodes));

        public int RenderSkillTree(
            int x,
            int y,
            int width,
            int height,
            Character player,
            int selectedIndex,
            int scrollOffset,
            string? statusMessage)
        {
            // Wipe prior center content so scroll/selection redraws never stack glyphs/boxes.
            clickableElements.Clear();
            canvas.ClearTextInArea(x, y, width, height);
            canvas.ClearBoxesInArea(x, y, width, height);

            int left = x + 1;
            int contentWidth = Math.Max(28, width - 2);
            int currentY = y + 1;

            player.Progression.EnsureSkillTreeRootsGranted();
            var tree = SkillTreeService.GetPrimaryTree(player.Progression);
            var primary = player.Progression.GetPrimaryClassWeaponType();

            canvas.AddText(left, currentY, "SKILL TREE", AsciiArtAssets.Colors.Gold);
            currentY += 2;

            if (tree == null || primary == null)
            {
                foreach (var line in TextWrapper.WrapText(
                             "Equip a weapon and earn Path Points to open your primary path tree.",
                             contentWidth))
                {
                    canvas.AddText(left, currentY++, line, AsciiArtAssets.Colors.Gray);
                }
                currentY++;
                AddBackOption(left, currentY, contentWidth);
                LastViewportHeight = 0;
                LastContentHeight = 0;
                LastAppliedScrollOffset = 0;
                return currentY - y + 1;
            }

            int lifetime = player.Progression.GetClassPoints(primary.Value);
            int spent = player.Progression.GetSpentPathPoints(primary.Value);
            int available = player.Progression.GetAvailablePathPoints(primary.Value);

            canvas.AddText(left, currentY++, tree.Title, AsciiArtAssets.Colors.White);
            canvas.AddText(left, currentY++,
                $"PP  avail {available}  /  invested {spent}  /  lifetime {lifetime}",
                AsciiArtAssets.Colors.Cyan);

            var nodes = OrderNodes(tree.Nodes);
            if (nodes.Count == 0)
            {
                canvas.AddText(left, currentY++, "No nodes defined.", AsciiArtAssets.Colors.Gray);
                AddBackOption(left, currentY + 1, contentWidth);
                return currentY - y + 2;
            }

            selectedIndex = Math.Clamp(selectedIndex, 0, nodes.Count - 1);
            var branchOrder = BuildBranchOrder(nodes);
            int colCount = Math.Max(1, branchOrder.Count);

            int treeTop = currentY + 1;
            int headerUsed = treeTop - y;
            int viewportHeight = GetTreeViewportHeight(height, headerUsed);
            int contentHeight = GetTreeContentHeight(nodes);
            scrollOffset = AlignScrollToTier(ClampScrollOffset(scrollOffset, contentHeight, viewportHeight));
            LastViewportHeight = viewportHeight;
            LastContentHeight = contentHeight;
            LastAppliedScrollOffset = scrollOffset;

            int nodeWidth = Math.Clamp((contentWidth - (colCount - 1)) / colCount, MinNodeWidth, MaxNodeWidth);
            int treeWidth = colCount * nodeWidth + Math.Max(0, colCount - 1);
            int treeLeft = left + Math.Max(0, (contentWidth - treeWidth) / 2);
            int viewportBottom = treeTop + viewportHeight;

            // Opaque tree backdrop so scrolled frames do not show leftover glyphs through gaps.
            canvas.AddBox(left, treeTop, contentWidth, viewportHeight,
                AsciiArtAssets.Colors.Gray, Color.FromRgb(12, 12, 16));

            int maxTier = GetMaxTier(nodes);
            var placements = BuildPlacements(nodes, branchOrder, treeLeft, treeTop, nodeWidth, scrollOffset, maxTier);
            DrawConnectors(placements, treeTop, viewportBottom);
            DrawNodes(player, placements, selectedIndex, treeTop, viewportBottom);

            if (contentHeight > viewportHeight)
            {
                canvas.AddText(left, Math.Min(viewportBottom, y + height - DetailReserve - FooterReserve),
                    $"{AsciiArtAssets.UIElements.ArrowUpDown} PgUp/PgDn scroll",
                    AsciiArtAssets.Colors.Gray);
            }

            int detailY = viewportBottom + 1;
            int detailMax = y + height - FooterReserve;
            detailY = DrawDetail(player, nodes[selectedIndex], left, detailY, contentWidth, statusMessage, detailMax);
            if (detailY < detailMax)
            {
                canvas.AddText(left, detailY, "Arrows=move  L=Learn  Click=select  0=Back", AsciiArtAssets.Colors.Gray);
                detailY++;
            }
            if (detailY < y + height)
            {
                AddBackOption(left, detailY, contentWidth);
                AddLearnOption(left + 22, detailY, player, nodes[selectedIndex]);
            }
            return Math.Min(height, detailY - y + 2);
        }

        public static IReadOnlyList<SkillTreeNodeDefinition> OrderNodes(IEnumerable<SkillTreeNodeDefinition> source) =>
            source
                .OrderBy(n => n.Tier)
                .ThenBy(n => n.Branch, StringComparer.OrdinalIgnoreCase)
                .ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

        public static List<string> BuildBranchOrder(IReadOnlyList<SkillTreeNodeDefinition> nodes)
        {
            var order = new List<string>();
            foreach (var n in nodes.OrderBy(n => n.Tier))
            {
                if (IsCenterBranch(n.Branch))
                    continue;
                if (!order.Any(b => string.Equals(b, n.Branch, StringComparison.OrdinalIgnoreCase)))
                    order.Add(n.Branch);
            }
            if (order.Count == 0)
                order.Add("Core");
            return order;
        }

        private static bool IsCenterBranch(string? branch) =>
            string.Equals(branch, "Core", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(branch, "Confluence", StringComparison.OrdinalIgnoreCase);

        private List<NodePlacement> BuildPlacements(
            IReadOnlyList<SkillTreeNodeDefinition> nodes,
            IReadOnlyList<string> branchOrder,
            int treeLeft,
            int treeTop,
            int nodeWidth,
            int scrollOffset,
            int maxTier)
        {
            int centerCol = Math.Max(0, branchOrder.Count / 2);
            var list = new List<NodePlacement>(nodes.Count);
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                int col = ResolveColumn(node, branchOrder, centerCol);
                int absY = treeTop + GetNodeTopCell(node, maxTier) - scrollOffset;
                int absX = treeLeft + col * (nodeWidth + 1);
                list.Add(new NodePlacement(node, i, col, absX, absY, nodeWidth, NodeHeight));
            }
            return list;
        }

        private static int ResolveColumn(SkillTreeNodeDefinition node, IReadOnlyList<string> branchOrder, int centerCol)
        {
            if (IsCenterBranch(node.Branch))
                return centerCol;
            for (int i = 0; i < branchOrder.Count; i++)
            {
                if (string.Equals(branchOrder[i], node.Branch, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return centerCol;
        }

        private static bool IsFullyVisible(NodePlacement p, int clipTop, int clipBottom) =>
            p.Y >= clipTop && p.Y + p.H <= clipBottom;

        private void DrawConnectors(List<NodePlacement> placements, int clipTop, int clipBottom)
        {
            var byId = placements.ToDictionary(p => p.Node.Id, p => p, StringComparer.OrdinalIgnoreCase);
            foreach (var child in placements)
            {
                if (!IsFullyVisible(child, clipTop, clipBottom))
                    continue;
                foreach (string req in child.Node.Requires)
                {
                    if (!byId.TryGetValue(req, out var parent))
                        continue;
                    if (!IsFullyVisible(parent, clipTop, clipBottom))
                        continue;

                    int parentCx = parent.X + parent.W / 2;
                    // Upward tree: child (higher tier) is above parent (prerequisite).
                    var upper = child.Y <= parent.Y ? child : parent;
                    var lower = child.Y <= parent.Y ? parent : child;
                    int upperCx = upper.X + upper.W / 2;
                    int lowerCx = lower.X + lower.W / 2;
                    int gapStart = upper.Y + upper.H;
                    int gapEnd = lower.Y - 1;
                    if (gapEnd < gapStart)
                        continue;

                    if (parent.Col == child.Col)
                    {
                        for (int gy = gapStart; gy <= gapEnd; gy++)
                        {
                            if (gy < clipTop || gy >= clipBottom) continue;
                            canvas.AddText(parentCx, gy, AsciiArtAssets.UIElements.BorderVertical, AsciiArtAssets.Colors.Gray);
                        }
                    }
                    else
                    {
                        // Vertical from upper node, then horizontal bridge just above lower, then into lower.
                        for (int gy = gapStart; gy < gapEnd; gy++)
                        {
                            if (gy < clipTop || gy >= clipBottom) continue;
                            canvas.AddText(upperCx, gy, AsciiArtAssets.UIElements.BorderVertical, AsciiArtAssets.Colors.Gray);
                        }
                        if (gapEnd >= clipTop && gapEnd < clipBottom)
                        {
                            int x0 = Math.Min(upperCx, lowerCx);
                            int x1 = Math.Max(upperCx, lowerCx);
                            for (int cx = x0; cx <= x1; cx++)
                            {
                                string ch = AsciiArtAssets.UIElements.BorderHorizontal;
                                if (cx == upperCx || cx == lowerCx)
                                    ch = AsciiArtAssets.UIElements.BorderVertical;
                                canvas.AddText(cx, gapEnd, ch, AsciiArtAssets.Colors.Gray);
                            }
                        }
                    }
                }
            }
        }

        private void DrawNodes(
            Character player,
            List<NodePlacement> placements,
            int selectedIndex,
            int clipTop,
            int clipBottom)
        {
            for (int i = 0; i < placements.Count; i++)
            {
                var p = placements[i];
                // No partial cards — avoids stacked fragments while scrolling.
                if (!IsFullyVisible(p, clipTop, clipBottom))
                    continue;

                var state = SkillTreeService.GetNodeState(player.Progression, p.Node);
                bool selected = i == selectedIndex;
                Color border = selected
                    ? AsciiArtAssets.Colors.White
                    : state switch
                    {
                        SkillTreeService.NodeViewState.Learned => AsciiArtAssets.Colors.Green,
                        SkillTreeService.NodeViewState.Available => AsciiArtAssets.Colors.Cyan,
                        SkillTreeService.NodeViewState.Unaffordable => AsciiArtAssets.Colors.Yellow,
                        _ => AsciiArtAssets.Colors.Gray
                    };
                Color fill = selected
                    ? Color.FromRgb(36, 36, 40)
                    : Color.FromRgb(18, 18, 22);

                canvas.AddBox(p.X, p.Y, p.W, p.H, border, fill);

                string name = Truncate(p.Node.Name, p.W - 2);
                canvas.AddText(p.X + 1, p.Y + 1, name, AsciiArtAssets.Colors.White);

                string stateTag = state switch
                {
                    SkillTreeService.NodeViewState.Learned => "OWN",
                    SkillTreeService.NodeViewState.Available => "BUY",
                    SkillTreeService.NodeViewState.Unaffordable => "$$$",
                    SkillTreeService.NodeViewState.Locked => "LCK",
                    _ => "---"
                };
                string meta = Truncate($"T{p.Node.Tier} {ShortType(p.Node.Type)} {p.Node.Cost}PP {stateTag}", p.W - 2);
                canvas.AddText(p.X + 1, p.Y + 2, meta, border);

                clickableElements.Add(new ClickableElement
                {
                    X = p.X,
                    Y = p.Y,
                    Width = p.W,
                    Height = p.H,
                    Type = ElementType.Button,
                    Value = (i + 1).ToString(),
                    DisplayText = p.Node.Name
                });
            }
        }

        private int DrawDetail(
            Character player,
            SkillTreeNodeDefinition selected,
            int left,
            int y,
            int width,
            string? statusMessage,
            int maxY)
        {
            if (y >= maxY) return y;
            var state = SkillTreeService.GetNodeState(player.Progression, selected);
            canvas.AddText(left, y++, "DETAIL", AsciiArtAssets.Colors.Gold);
            if (y >= maxY) return y;
            canvas.AddText(left, y++,
                Truncate($"{selected.Name}  ·  {selected.Type}  ·  Tier {selected.Tier}  ·  {selected.Cost} PP", width),
                AsciiArtAssets.Colors.Cyan);

            foreach (var line in TextWrapper.WrapText(selected.Effect ?? "", width).Take(2))
            {
                if (y >= maxY) return y;
                canvas.AddText(left, y++, line, AsciiArtAssets.Colors.White);
            }
            foreach (var line in TextWrapper.WrapText(selected.Payoff ?? "", width).Take(1))
            {
                if (y >= maxY) return y;
                canvas.AddText(left, y++, line, AsciiArtAssets.Colors.Gray);
            }

            if (selected.Requires.Count > 0 && y < maxY)
            {
                string req = "Requires: " + string.Join(", ",
                    selected.Requires.Select(id => SkillTreeService.Trees.GetNode(id)?.Name ?? id));
                foreach (var line in TextWrapper.WrapText(req, width).Take(1))
                {
                    if (y >= maxY) return y;
                    canvas.AddText(left, y++, line, AsciiArtAssets.Colors.Yellow);
                }
            }

            if (y < maxY)
            {
                string cta = state switch
                {
                    SkillTreeService.NodeViewState.Learned => "Already learned (permanent).",
                    SkillTreeService.NodeViewState.Available => "Press L or click Learn to unlock.",
                    SkillTreeService.NodeViewState.Unaffordable => "Not enough Path Points.",
                    SkillTreeService.NodeViewState.Locked => "Prerequisites not met.",
                    _ => "Cannot spend on this path."
                };
                canvas.AddText(left, y++, cta, AsciiArtAssets.Colors.White);
            }

            if (!string.IsNullOrWhiteSpace(statusMessage) && y < maxY)
            {
                foreach (var line in TextWrapper.WrapText(statusMessage, width).Take(1))
                {
                    if (y >= maxY) return y;
                    canvas.AddText(left, y++, line, AsciiArtAssets.Colors.Yellow);
                }
            }
            return y;
        }

        private void AddBackOption(int x, int y, int width)
        {
            var back = new ClickableElement
            {
                X = x,
                Y = y,
                Width = Math.Min(22, width),
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = MenuOptionFormatter.Format(0, "Back")
            };
            clickableElements.Add(back);
            canvas.AddMenuOption(x, y, 0, "Back", AsciiArtAssets.Colors.White, back.IsHovered);
        }

        private void AddLearnOption(int x, int y, Character player, SkillTreeNodeDefinition node)
        {
            var state = SkillTreeService.GetNodeState(player.Progression, node);
            if (state != SkillTreeService.NodeViewState.Available)
                return;
            var learn = new ClickableElement
            {
                X = x,
                Y = y,
                Width = 14,
                Height = 1,
                Type = ElementType.Button,
                Value = "l",
                DisplayText = "[L] Learn"
            };
            clickableElements.Add(learn);
            canvas.AddText(x, y, "[L] Learn", AsciiArtAssets.Colors.Gold);
        }

        private static string ShortType(string type) =>
            type?.Length > 0 == true ? type.Substring(0, Math.Min(3, type.Length)).ToUpperInvariant() : "???";

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || max <= 0) return "";
            return text.Length <= max ? text : text.Substring(0, Math.Max(1, max - 1)) + "…";
        }

        private readonly struct NodePlacement
        {
            public SkillTreeNodeDefinition Node { get; }
            public int Index { get; }
            public int Col { get; }
            public int X { get; }
            public int Y { get; }
            public int W { get; }
            public int H { get; }

            public NodePlacement(SkillTreeNodeDefinition node, int index, int col, int x, int y, int w, int h)
            {
                Node = node;
                Index = index;
                Col = col;
                X = x;
                Y = y;
                W = w;
                H = h;
            }
        }
    }
}
