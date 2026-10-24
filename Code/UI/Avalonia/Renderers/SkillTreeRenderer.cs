using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Help;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// ASCII skill-tree screen: a depth ruler (tier point gates) on the left, one lane per branch,
    /// an optional shared/hybrid rail lane, and a details panel (right when wide, below when narrow).
    /// Progression grows upward (tier 0 / roots at the bottom). Each node is two text rows:
    /// state glyph + name, then rank pips / cost / why-locked. Same branch+tier siblings stack.
    /// Every glyph is placed with its own AddText at a fixed cell so font fallbacks can't drift columns.
    /// </summary>
    public class SkillTreeRenderer
    {
        public const int NodeHeight = 2;
        public const int TierGap = 0;
        public const int StackGap = 0;
        public const int TierStride = NodeHeight + TierGap;
        public const int ScrollStep = NodeHeight;
        /// <summary>Empty content rows above the highest tier.</summary>
        public const int TopHeadroom = 0;

        private const int HeaderRows = 3;          // title, depth/status line, blank
        private const int FooterRows = 3;          // legend, keys, back
        private const int BottomDetailRows = 9;    // details block when the panel can't sit on the right
        private const int RulerWidth = 7;
        private const int MinLaneWidth = 12;
        private const int MaxLaneWidth = 22;
        private const int MinRightDetailWidth = 30;
        private const int MaxRightDetailWidth = 42;
        private const int MinTreeWidthForRightDetail = 64;

        private const string LearnedGlyph = "◆";
        private const string AvailableGlyph = "◇";
        private const string NeedSpGlyph = "◈";
        private const string LockedGlyph = "·";
        private const string PipFull = "█";
        private const string PipEmpty = "░";

        private static readonly Color Backdrop = Color.FromRgb(12, 12, 16);
        private static readonly Color LockedBandFill = Color.FromRgb(17, 15, 21);
        private static readonly Color SelectedFill = Color.FromRgb(56, 48, 18);
        private static readonly Color Frame = Color.FromRgb(110, 110, 120);
        private static readonly Color Dim = Color.FromRgb(78, 78, 88);
        private static readonly Color LitConnector = Color.FromRgb(96, 150, 96);

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

        public static int GetMaxTier(IReadOnlyList<SkillTreeNodeDefinition> nodes) =>
            nodes == null || nodes.Count == 0 ? 0 : nodes.Max(n => n.Tier);

        /// <summary>Rows at the bottom of the rail lane for its base block (what opens the hybrid tree).</summary>
        public const int RailRootRows = 4;

        public static int GetRailContentHeight(IReadOnlyList<SkillTreeNodeDefinition> railNodes) =>
            railNodes == null || railNodes.Count == 0 ? 0 : TopHeadroom + RailRootRows + railNodes.Count * NodeHeight;

        /// <summary>Rail nodes stack bottom-up above the base block (lowest tier nearest the base).</summary>
        public static int GetRailNodeTopCell(int railIndex, int contentHeight) =>
            Math.Max(0, contentHeight - RailRootRows - (Math.Max(0, railIndex) + 1) * NodeHeight);

        public static int GetDisplayContentHeight(SkillTreeDisplayModel model)
        {
            int primaryH = model.PrimaryNodes.Count > 0 ? GetTreeContentHeight(model.PrimaryNodes) : 0;
            int railH = GetRailContentHeight(model.RailNodes);
            return Math.Max(primaryH, railH);
        }

        public static int GetNodeTopForDisplay(SkillTreeDisplayModel model, int allIndex)
        {
            if (model.IsRailIndex(allIndex))
                return GetRailNodeTopCell(model.RailIndexOf(allIndex), GetDisplayContentHeight(model));
            var node = model.AllNodes[allIndex];
            return GetNodeTopCell(node, model.PrimaryNodes);
        }

        public static int GetNodeBottomForDisplay(SkillTreeDisplayModel model, int allIndex) =>
            GetNodeTopForDisplay(model, allIndex) + NodeHeight;

        public static int GetColumnForDisplay(SkillTreeDisplayModel model, int allIndex)
        {
            var branches = BuildBranchOrder(model.PrimaryNodes);
            if (model.IsRailIndex(allIndex))
                return Math.Max(1, branches.Count); // one past last tree column
            int centerCol = Math.Max(0, branches.Count / 2);
            return ResolveColumn(model.AllNodes[allIndex], branches, centerCol);
        }

        /// <summary>
        /// Content-space layout: highest tiers toward y=0, roots toward larger Y.
        /// Same branch+tier siblings stack downward within the tier band (alphabetical by name).
        /// <see cref="SkillTreePrerequisites"/> reads this order to sequence each branch, so keep it stable.
        /// </summary>
        public static ContentLayout ComputeContentLayout(IReadOnlyList<SkillTreeNodeDefinition> nodes)
        {
            var empty = new ContentLayout(0, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<int, (int top, int height)>());
            if (nodes == null || nodes.Count == 0)
                return empty;

            var branchOrder = BuildBranchOrder(nodes);
            int centerCol = Math.Max(0, branchOrder.Count / 2);
            int maxTier = GetMaxTier(nodes);

            var groups = GroupByCell(nodes, branchOrder, centerCol);

            int[] stackDepth = new int[maxTier + 1];
            for (int t = 0; t <= maxTier; t++)
                stackDepth[t] = 0;
            foreach (var kv in groups)
                stackDepth[kv.Key.tier] = Math.Max(stackDepth[kv.Key.tier], kv.Value.Count);

            static int BandHeight(int depth) =>
                depth * NodeHeight + Math.Max(0, depth - 1) * StackGap;

            var bands = new Dictionary<int, (int top, int height)>();
            int y = TopHeadroom;
            for (int t = maxTier; t >= 0; t--)
            {
                // Empty tiers still get a band so the depth ruler reads continuously.
                int h = BandHeight(Math.Max(1, stackDepth[t]));
                bands[t] = (y, h);
                y += h;
                if (t > 0)
                    y += TierGap;
            }

            var tops = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in groups)
            {
                int bandTop = bands[kv.Key.tier].top;
                for (int i = 0; i < kv.Value.Count; i++)
                    tops[kv.Value[i].Id] = bandTop + i * (NodeHeight + StackGap);
            }

            return new ContentLayout(y, tops, bands);
        }

        private static Dictionary<(int col, int tier), List<SkillTreeNodeDefinition>> GroupByCell(
            IReadOnlyList<SkillTreeNodeDefinition> nodes,
            IReadOnlyList<string> branchOrder,
            int centerCol)
        {
            var groups = new Dictionary<(int col, int tier), List<SkillTreeNodeDefinition>>();
            foreach (var node in nodes)
            {
                int col = ResolveColumn(node, branchOrder, centerCol);
                var key = (col, node.Tier);
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<SkillTreeNodeDefinition>();
                    groups[key] = list;
                }
                list.Add(node);
            }
            foreach (var list in groups.Values)
                list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return groups;
        }

        public static int GetTreeContentHeight(IReadOnlyList<SkillTreeNodeDefinition> nodes) =>
            ComputeContentLayout(nodes).ContentHeight;

        public static int ClampScrollOffset(int scrollOffset, int contentHeight, int viewportHeight)
        {
            int max = Math.Max(0, contentHeight - viewportHeight);
            return Math.Clamp(scrollOffset, 0, max);
        }

        /// <summary>Snap scroll to node-row boundaries so nodes never straddle the viewport edge.</summary>
        public static int AlignScrollToTiers(int scrollOffset) =>
            Math.Max(0, (scrollOffset / ScrollStep) * ScrollStep);

        /// <summary>
        /// Clamp into range, then row-align — but keep absolute top (0) and bottom (max) so dense trees
        /// remain reachable when max scroll is not a multiple of <see cref="ScrollStep"/>.
        /// </summary>
        public static int NormalizeScrollOffset(int scrollOffset, int contentHeight, int viewportHeight)
        {
            int max = Math.Max(0, contentHeight - viewportHeight);
            if (max == 0)
                return 0;
            int clamped = Math.Clamp(scrollOffset, 0, max);
            if (clamped >= max)
                return max;
            if (clamped <= 0)
                return 0;
            return Math.Min(AlignScrollToTiers(clamped), max);
        }

        public static int EnsureNodeVisible(int scrollOffset, int nodeTop, int nodeBottom, int viewportHeight)
        {
            if (nodeTop < scrollOffset)
                return AlignScrollToTiers(nodeTop);
            if (nodeBottom > scrollOffset + viewportHeight)
                return AlignScrollToTiers(Math.Max(0, nodeBottom - viewportHeight));
            // Already fully on-screen — leave scroll alone (don't re-align the bottom pin).
            return scrollOffset;
        }

        /// <summary>Content-space top row for a node (roots at bottom, higher tiers toward y=0).</summary>
        public static int GetNodeTopCell(SkillTreeNodeDefinition node, IReadOnlyList<SkillTreeNodeDefinition> nodes)
        {
            var layout = ComputeContentLayout(nodes);
            if (layout.Tops.TryGetValue(node.Id, out int top))
                return top;
            return 0;
        }

        public static int GetNodeBottomCell(SkillTreeNodeDefinition node, IReadOnlyList<SkillTreeNodeDefinition> nodes) =>
            GetNodeTopCell(node, nodes) + NodeHeight;

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
            int contentWidth = Math.Max(40, width - 2);
            int bottomLimit = y + height;

            var model = SkillTreeService.BuildDisplayModel(player.Progression);
            var tree = model.PrimaryTree;
            var primary = model.PrimaryPath;

            if (tree == null || primary == null || model.PrimaryNodes.Count == 0)
            {
                int ey = y + 1;
                DrawTitledRule(left, ey, Math.Min(contentWidth, 40), "SKILL TREE", Frame, AsciiArtAssets.Colors.Gold);
                ey += 2;
                foreach (var line in TextWrapper.WrapText(
                             "Equip a weapon and earn Skill Points to open your primary path tree.",
                             contentWidth))
                {
                    canvas.AddText(left, ey++, line, AsciiArtAssets.Colors.Gray);
                }
                ey++;
                AddBackOption(left, ey, contentWidth);
                LastViewportHeight = 0;
                LastContentHeight = 0;
                LastAppliedScrollOffset = 0;
                return ey - y + 1;
            }

            var nodes = model.AllNodes;
            selectedIndex = Math.Clamp(selectedIndex, 0, nodes.Count - 1);
            var depth = DepthInfo.For(player.Progression, tree, primary.Value);

            // ── Header ───────────────────────────────────────────────
            int headerY = y + 1;
            DrawHeader(player, model, left, headerY, contentWidth, depth);

            // ── Geometry ─────────────────────────────────────────────
            var primaryNodes = model.PrimaryNodes;
            var branchOrder = BuildBranchOrder(primaryNodes);
            bool showRail = model.HasRail;
            int laneCount = branchOrder.Count + (showRail ? 1 : 0);

            int rightDetailWidth = Math.Clamp(contentWidth * 3 / 10, MinRightDetailWidth, MaxRightDetailWidth);
            bool detailOnRight = contentWidth - rightDetailWidth - 2 >= MinTreeWidthForRightDetail;
            int treeAreaWidth = detailOnRight ? contentWidth - rightDetailWidth - 2 : contentWidth;

            // Frame: ║ ruler ║ lane ║ lane ║ … ║
            int laneWidth = Math.Clamp((treeAreaWidth - RulerWidth - 2) / Math.Max(1, laneCount) - 1, MinLaneWidth, MaxLaneWidth);
            int frameWidth = RulerWidth + 2 + laneCount * (laneWidth + 1);
            int frameLeft = left;

            int frameTop = headerY + HeaderRows;
            int footerTop = bottomLimit - FooterRows;
            int belowFrameReserve = detailOnRight ? 0 : BottomDetailRows;
            int viewportHeight = Math.Max(NodeHeight, footerTop - belowFrameReserve - frameTop - 2);
            int contentHeight = GetDisplayContentHeight(model);
            viewportHeight = Math.Min(viewportHeight, Math.Max(NodeHeight, contentHeight));
            scrollOffset = NormalizeScrollOffset(scrollOffset, contentHeight, viewportHeight);
            LastViewportHeight = viewportHeight;
            LastContentHeight = contentHeight;
            LastAppliedScrollOffset = scrollOffset;

            int treeTop = frameTop + 1;
            int viewportBottom = treeTop + viewportHeight;
            int frameBottom = viewportBottom;

            canvas.AddBox(frameLeft, frameTop, frameWidth, viewportHeight + 2, Backdrop, Backdrop);

            var lanes = new List<Lane>();
            int laneX = frameLeft + RulerWidth + 2;
            foreach (var branch in branchOrder)
            {
                lanes.Add(new Lane(branch.ToUpperInvariant(), laneX, laneWidth, isRail: false));
                laneX += laneWidth + 1;
            }
            Lane? railLane = null;
            if (showRail)
            {
                railLane = new Lane((model.RailTitle ?? model.DuoName ?? "SHARED").ToUpperInvariant(), laneX, laneWidth, isRail: true);
                lanes.Add(railLane);
            }

            var layout = ComputeContentLayout(primaryNodes);
            var view = new Viewport(treeTop, viewportBottom, scrollOffset);

            DrawLockedBands(layout, depth, frameLeft + 1, frameWidth - 2, view);
            bool moreAbove = scrollOffset > 0;
            bool moreBelow = scrollOffset + viewportHeight < contentHeight;
            DrawFrame(frameLeft, frameTop, frameBottom, lanes, view, moreAbove, moreBelow);
            DrawRuler(layout, depth, frameLeft + 1, view, contentHeight);

            var placements = BuildPlacements(primaryNodes, branchOrder, lanes, layout);
            DrawConnectors(player, placements, lanes, layout, contentHeight, view);
            DrawNodes(player, placements, selectedIndex, view, depth);

            if (railLane != null)
                DrawRail(player, model, railLane, contentHeight, view, selectedIndex);

            if (moreAbove || moreBelow)
                MenuMouseScrollHint.DrawRightAligned(canvas, left, contentWidth, footerTop + 2);

            // ── Details ──────────────────────────────────────────────
            if (detailOnRight)
            {
                int detailX = frameLeft + frameWidth + 2;
                int detailW = Math.Min(rightDetailWidth, left + contentWidth - detailX);
                DrawDetailPanel(player, model, depth, selectedIndex, detailX, frameTop, detailW, frameBottom - frameTop + 1, statusMessage);
            }
            else
            {
                DrawDetailPanel(player, model, depth, selectedIndex, left, frameBottom + 1, Math.Min(contentWidth, frameWidth), BottomDetailRows, statusMessage);
            }

            // ── Footer ───────────────────────────────────────────────
            DrawFooter(left, footerTop, contentWidth);
            return height;
        }

        // ─────────────────────────────────────────────────────────────
        // Header / footer
        // ─────────────────────────────────────────────────────────────

        private void DrawHeader(Character player, SkillTreeDisplayModel model, int left, int y, int width, DepthInfo depth)
        {
            var tree = model.PrimaryTree!;
            var primary = model.PrimaryPath!.Value;
            int lifetime = player.Progression.GetClassPoints(primary);
            int spent = player.Progression.GetSpentSkillPoints(primary);
            int available = player.Progression.GetAvailableSkillPoints(primary);

            int cx = left;
            canvas.AddText(cx, y, "SKILL TREE", AsciiArtAssets.Colors.Gold);
            cx += 11;
            canvas.AddText(cx, y, "──", Frame);
            cx += 3;
            canvas.AddText(cx, y, tree.Title, AsciiArtAssets.Colors.White);
            cx += tree.Title.Length + 3;

            string sp = $"{primary} SP: {available} free · {spent} spent · {lifetime} earned";
            var viewable = SkillTreeService.GetViewablePaths(player.Progression);
            string? switchText = null;
            if (viewable.Count > 1)
            {
                string others = string.Join("/", viewable.Where(p => p != primary));
                switchText = $"[T] {others} {AsciiArtAssets.UIElements.ArrowRight}";
            }

            int rightEdge = left + width;
            if (switchText != null && rightEdge - switchText.Length > cx + sp.Length + 2)
            {
                int sx = rightEdge - switchText.Length;
                clickableElements.Add(new ClickableElement
                {
                    X = sx,
                    Y = y,
                    Width = switchText.Length,
                    Height = 1,
                    Type = ElementType.Button,
                    Value = "t",
                    DisplayText = switchText
                });
                canvas.AddText(sx, y, switchText, AsciiArtAssets.Colors.Gold);
                rightEdge = sx - 3;
            }
            if (cx + sp.Length <= rightEdge)
                canvas.AddText(Math.Max(cx, rightEdge - sp.Length), y, sp, AsciiArtAssets.Colors.Cyan);

            // Depth line: what opens next, plus the secondary path's SP when a rail is shown.
            string depthText = depth.NextTier is int nt
                ? $"{AsciiArtAssets.UIElements.ArrowUp} Tier {nt} opens at {depth.Gate(nt)} {primary} pts · {depth.Gate(nt) - depth.Points} to go"
                : $"{LearnedGlyph} Every tier open ({depth.Points} {primary} pts)";
            canvas.AddText(left, y + 1, Truncate(depthText, width), depth.NextTier != null ? AsciiArtAssets.Colors.Yellow : AsciiArtAssets.Colors.Green);

            if (model.HasRail && model.SecondaryPath != null)
            {
                var sec = model.SecondaryPath.Value;
                string duo = string.IsNullOrWhiteSpace(model.DuoName) ? "Shared" : model.DuoName!;
                var hybrid = ResolveRailHybrid(player.Progression)?.info;
                string railText = hybrid == null
                    ? $"{duo} rail · {sec} SP: {player.Progression.GetAvailableSkillPoints(sec)} free"
                    : HybridTrees.IsUnlocked(player.Progression, hybrid)
                        ? $"{hybrid.Key} hybrid · spends {hybrid.ClassA} + {hybrid.ClassB} SP"
                        : $"{hybrid.Key} hybrid · opens at {HybridTrees.UnlockPointsPerParent} {hybrid.ClassA} + {HybridTrees.UnlockPointsPerParent} {hybrid.ClassB} pts";
                int rx = left + depthText.Length + 4;
                if (rx + railText.Length <= left + width)
                    canvas.AddText(rx, y + 1, railText, AsciiArtAssets.Colors.Magenta);
            }
        }

        private void DrawFooter(int left, int y, int width)
        {
            int cx = left + 1;
            void Key(string glyph, Color color, string label)
            {
                canvas.AddText(cx, y, glyph, color);
                canvas.AddText(cx + 2, y, label, AsciiArtAssets.Colors.Gray);
                cx += 2 + label.Length + 3;
            }
            Key(LearnedGlyph, AsciiArtAssets.Colors.Green, "learned");
            Key(AvailableGlyph, AsciiArtAssets.Colors.Cyan, "can learn");
            Key(NeedSpGlyph, AsciiArtAssets.Colors.Yellow, "need SP");
            Key(LockedGlyph, Dim, "locked");
            canvas.AddText(cx, y, PipFull + PipEmpty, AsciiArtAssets.Colors.Green);
            canvas.AddText(cx + 3, y, "rank", AsciiArtAssets.Colors.Gray);

            canvas.AddText(left + 1, y + 1,
                Truncate("Arrows move · L / Enter learn · T switch tree · Wheel / PgUp PgDn scroll · Click select", width - 1),
                Dim);
            AddBackOption(left, y + 2, width);
        }

        // ─────────────────────────────────────────────────────────────
        // Frame, ruler, bands
        // ─────────────────────────────────────────────────────────────

        private void DrawFrame(int frameLeft, int frameTop, int frameBottom, List<Lane> lanes, Viewport view, bool moreAbove, bool moreBelow)
        {
            // Top rule with titles: ╔═ PTS ═╦═══ BONE ═══╦ … ╗ (▲ / ▼ in the ruler segment when scrolled)
            canvas.AddText(frameLeft, frameTop, "╔", Frame);
            if (moreAbove)
                DrawTitledRule(frameLeft + 1, frameTop, RulerWidth, AsciiArtAssets.UIElements.ArrowUp, Frame, AsciiArtAssets.Colors.Yellow, '═');
            else
                DrawTitledRule(frameLeft + 1, frameTop, RulerWidth, "PTS", Frame, AsciiArtAssets.Colors.Gray, '═');
            int sepX = frameLeft + 1 + RulerWidth;
            foreach (var lane in lanes)
            {
                Color sepColor = lane.IsRail ? AsciiArtAssets.Colors.Magenta : Frame;
                canvas.AddText(sepX, frameTop, "╦", sepColor);
                DrawTitledRule(lane.X, frameTop, lane.Width, lane.Title, sepColor,
                    lane.IsRail ? AsciiArtAssets.Colors.Magenta : AsciiArtAssets.Colors.White, '═');
                sepX = lane.X + lane.Width;
            }
            canvas.AddText(sepX, frameTop, "╗", Frame);

            // Side walls and lane separators.
            for (int row = view.Top; row < view.Bottom; row++)
            {
                canvas.AddText(frameLeft, row, "║", Frame);
                canvas.AddText(frameLeft + 1 + RulerWidth, row, "║", Frame);
                for (int i = 0; i < lanes.Count; i++)
                {
                    bool railEdge = lanes[i].IsRail || (i + 1 < lanes.Count && lanes[i + 1].IsRail);
                    canvas.AddText(lanes[i].X + lanes[i].Width, row, "║", railEdge ? AsciiArtAssets.Colors.Magenta : Frame);
                }
            }

            // Bottom rule: ╚═══════╩═══ … ╝
            canvas.AddText(frameLeft, frameBottom, "╚", Frame);
            if (moreBelow)
                DrawTitledRule(frameLeft + 1, frameBottom, RulerWidth, AsciiArtAssets.UIElements.ArrowDown, Frame, AsciiArtAssets.Colors.Yellow, '═');
            else
                canvas.AddText(frameLeft + 1, frameBottom, new string('═', RulerWidth), Frame);
            sepX = frameLeft + 1 + RulerWidth;
            foreach (var lane in lanes)
            {
                Color c = lane.IsRail ? AsciiArtAssets.Colors.Magenta : Frame;
                canvas.AddText(sepX, frameBottom, "╩", c);
                canvas.AddText(lane.X, frameBottom, new string('═', lane.Width), c);
                sepX = lane.X + lane.Width;
            }
            canvas.AddText(sepX, frameBottom, "╝", Frame);
        }

        private void DrawRuler(ContentLayout layout, DepthInfo depth, int x, Viewport view, int contentHeight)
        {
            foreach (var kv in layout.Bands)
            {
                int tier = kv.Key;
                int row = view.ToScreen(kv.Value.top);
                if (!view.Contains(row))
                    continue;
                bool open = depth.IsOpen(tier);
                bool next = depth.NextTier == tier;
                string marker = next ? AsciiArtAssets.UIElements.ArrowUp : " ";
                Color color = next ? AsciiArtAssets.Colors.Yellow : open ? AsciiArtAssets.Colors.Gray : Dim;
                canvas.AddText(x, row, marker, color);
                canvas.AddText(x + 1, row, $"{depth.Gate(tier),3} T{tier}", color);
            }
        }

        private void DrawLockedBands(ContentLayout layout, DepthInfo depth, int x, int width, Viewport view)
        {
            foreach (var kv in layout.Bands)
            {
                if (depth.IsOpen(kv.Key))
                    continue;
                int top = Math.Max(view.Top, view.ToScreen(kv.Value.top));
                int bottom = Math.Min(view.Bottom, view.ToScreen(kv.Value.top + kv.Value.height));
                if (bottom > top)
                    canvas.AddBox(x, top, width, bottom - top, LockedBandFill, LockedBandFill);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Nodes and connectors
        // ─────────────────────────────────────────────────────────────

        private List<NodePlacement> BuildPlacements(
            IReadOnlyList<SkillTreeNodeDefinition> nodes,
            IReadOnlyList<string> branchOrder,
            List<Lane> lanes,
            ContentLayout layout)
        {
            int centerCol = Math.Max(0, branchOrder.Count / 2);
            var treeLanes = lanes.Where(l => !l.IsRail).ToList();
            var groups = GroupByCell(nodes, branchOrder, centerCol);
            int spanLeft = treeLanes[0].X;
            int spanWidth = treeLanes[^1].X + treeLanes[^1].Width - spanLeft;

            var list = new List<NodePlacement>(nodes.Count);
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                int col = ResolveColumn(node, branchOrder, centerCol);
                int contentTop = layout.Tops.TryGetValue(node.Id, out int top) ? top : 0;

                // Core / Confluence span every lane when they sit alone in their tier.
                bool span = IsCenterBranch(node.Branch)
                    && !groups.Any(g => g.Key.tier == node.Tier && g.Value.Any(n => !IsCenterBranch(n.Branch)));
                if (span)
                {
                    list.Add(new NodePlacement(node, i, col, spanLeft, contentTop, spanWidth, spans: true));
                }
                else
                {
                    var lane = treeLanes[Math.Clamp(col, 0, treeLanes.Count - 1)];
                    list.Add(new NodePlacement(node, i, col, lane.X, contentTop, lane.Width, spans: false));
                }
            }
            return list;
        }

        /// <summary>
        /// Fill each lane's empty rows below its tip with a vertical connector. A segment is lit when
        /// the node beneath it (its prerequisite on the branch, or the root) is learned.
        /// </summary>
        private void DrawConnectors(
            Character player,
            List<NodePlacement> placements,
            List<Lane> lanes,
            ContentLayout layout,
            int contentHeight,
            Viewport view)
        {
            var spanRows = new HashSet<int>();
            bool rootLearned = true;
            foreach (var p in placements.Where(p => p.Spans))
            {
                for (int r = 0; r < NodeHeight; r++)
                    spanRows.Add(p.ContentTop + r);
                if (p.Node.Tier == 0)
                    rootLearned = player.Progression.GetSkillRank(p.Node.Id) > 0;
            }

            for (int col = 0; col < lanes.Count; col++)
            {
                var lane = lanes[col];
                if (lane.IsRail)
                    continue;
                var laneNodes = placements.Where(p => !p.Spans && p.Col == col).OrderByDescending(p => p.ContentTop).ToList();
                if (laneNodes.Count == 0)
                    continue;

                var occupied = new HashSet<int>();
                foreach (var p in laneNodes)
                    for (int r = 0; r < NodeHeight; r++)
                        occupied.Add(p.ContentTop + r);

                int tipTop = laneNodes[^1].ContentTop;
                int cx = lane.X + lane.Width / 2;
                for (int row = tipTop; row < contentHeight; row++)
                {
                    if (occupied.Contains(row) || spanRows.Contains(row))
                        continue;
                    int sy = view.ToScreen(row);
                    if (!view.Contains(sy))
                        continue;
                    // Nearest lane node below this row is the segment's prerequisite; none → the root.
                    var below = laneNodes.LastOrDefault(p => p.ContentTop > row);
                    bool lit = below.Node != null
                        ? player.Progression.GetSkillRank(below.Node.Id) > 0
                        : rootLearned;
                    canvas.AddText(cx, sy, AsciiArtAssets.UIElements.BorderVertical, lit ? LitConnector : Dim);
                }
            }
        }

        private void DrawNodes(Character player, List<NodePlacement> placements, int selectedIndex, Viewport view, DepthInfo depth)
        {
            foreach (var p in placements)
            {
                int sy = view.ToScreen(p.ContentTop);
                if (sy < view.Top || sy + NodeHeight > view.Bottom)
                    continue; // no partial nodes while scrolling
                DrawNodeCell(player, p.Node, p.Index, p.X, sy, p.W, p.Index == selectedIndex, p.Spans, depth, isRail: false);
            }
        }

        private void DrawRail(Character player, SkillTreeDisplayModel model, Lane lane, int contentHeight, Viewport view, int selectedIndex)
        {
            var railNodes = model.RailNodes;
            var hybrid = ResolveRailHybrid(player.Progression);
            DepthInfo? railDepth = hybrid?.tree != null
                ? DepthInfo.For(player.Progression, hybrid.Value.tree, hybrid.Value.info.ParentA)
                : null;

            // Base block at the bottom of the lane (where the tree grows from): what opens the hybrid tree.
            if (hybrid != null)
            {
                var h = hybrid.Value.info;
                int need = HybridTrees.UnlockPointsPerParent;
                int a = player.Progression.GetClassPoints(h.ParentA);
                int b = player.Progression.GetClassPoints(h.ParentB);
                bool open = HybridTrees.IsUnlocked(player.Progression, h);
                int baseTop = view.ToScreen(contentHeight - RailRootRows);
                var lines = new (string glyph, string text, Color color)[]
                {
                    (open ? LearnedGlyph : LockedGlyph, $"{h.Key} base", open ? AsciiArtAssets.Colors.Green : AsciiArtAssets.Colors.Magenta),
                    ("", open ? "open" : $"needs {need} + {need}", open ? AsciiArtAssets.Colors.Green : Dim),
                    (a >= need ? LearnedGlyph : "x", $"{h.ClassA} {a}/{need}", a >= need ? AsciiArtAssets.Colors.Green : AsciiArtAssets.Colors.Yellow),
                    (b >= need ? LearnedGlyph : "x", $"{h.ClassB} {b}/{need}", b >= need ? AsciiArtAssets.Colors.Green : AsciiArtAssets.Colors.Yellow),
                };
                for (int i = 0; i < lines.Length; i++)
                {
                    int sy = baseTop + i;
                    if (!view.Contains(sy))
                        continue;
                    if (lines[i].glyph.Length > 0)
                        canvas.AddText(lane.X + 1, sy, lines[i].glyph, lines[i].color);
                    canvas.AddText(lane.X + 3, sy, Truncate(lines[i].text, lane.Width - 3), lines[i].color);
                }
                if (railNodes.Count > 0 && baseTop >= view.Top && baseTop + RailRootRows <= view.Bottom)
                {
                    // Clicking the base selects the first hybrid skill so the details panel explains the rules.
                    clickableElements.Add(new ClickableElement
                    {
                        X = lane.X,
                        Y = baseTop,
                        Width = lane.Width,
                        Height = RailRootRows,
                        Type = ElementType.Button,
                        Value = (model.PrimaryCount + 1).ToString(),
                        DisplayText = $"{h.Key} base"
                    });
                }
            }

            for (int i = 0; i < railNodes.Count; i++)
            {
                int allIndex = model.PrimaryCount + i;
                int sy = view.ToScreen(GetRailNodeTopCell(i, contentHeight));
                if (sy < view.Top || sy + NodeHeight > view.Bottom)
                    continue;
                DrawNodeCell(player, railNodes[i], allIndex, lane.X, sy, lane.Width, allIndex == selectedIndex, spans: false, depth: railDepth, isRail: true);
            }
        }

        /// <summary>The hybrid tree shown on the rail (primary + secondary pair), if the rail is a hybrid tree.</summary>
        private static (HybridInfo info, SkillTreeDefinition tree)? ResolveRailHybrid(CharacterProgression progression)
        {
            var pa = progression.GetPrimaryClassWeaponType();
            var pb = progression.GetSecondaryClassWeaponType();
            if (pa == null || pb == null || HybridTrees.ForPair(pa.Value, pb.Value) is not HybridInfo h)
                return null;
            var tree = SkillTreeService.Trees.Trees.FirstOrDefault(t => string.Equals(t.ClassKey, h.Key, StringComparison.OrdinalIgnoreCase));
            return tree == null ? null : (h, tree);
        }

        private void DrawNodeCell(
            Character player,
            SkillTreeNodeDefinition node,
            int allIndex,
            int x,
            int y,
            int w,
            bool selected,
            bool spans,
            DepthInfo? depth,
            bool isRail)
        {
            var state = SkillTreeService.GetNodeState(player.Progression, node);
            int rank = player.Progression.GetSkillRank(node.Id);
            int maxRank = Math.Max(1, node.MaxRank);
            var (glyph, color) = GlyphFor(state);
            Color nameColor = selected ? AsciiArtAssets.Colors.Yellow
                : state == SkillTreeService.NodeViewState.Locked || state == SkillTreeService.NodeViewState.WrongPath ? Dim
                : isRail ? AsciiArtAssets.Colors.Magenta
                : AsciiArtAssets.Colors.White;

            string name = node.Name;
            string status = StatusText(node, state, rank, maxRank, depth);

            int cellX = x;
            int cellW = w;
            if (spans)
            {
                // Centered token across all lanes: "◆ Level 1 - Barbarian"
                int tokenW = Math.Min(w, Math.Max(name.Length, PipsLength(maxRank, rank, status)) + 4);
                cellX = x + Math.Max(0, (w - tokenW) / 2);
                cellW = tokenW;
            }

            if (selected)
                canvas.AddBox(cellX, y, cellW, NodeHeight, SelectedFill, SelectedFill);

            canvas.AddText(cellX, y, selected ? AsciiArtAssets.UIElements.ArrowRight : " ", AsciiArtAssets.Colors.Yellow);
            canvas.AddText(cellX + 1, y, glyph, color);
            canvas.AddText(cellX + 3, y, Truncate(name, cellW - 3), nameColor);

            int sx = cellX + 3;
            int room = cellW - 3;
            // Tier-gated tree nodes stay a dim name: the shaded band and ruler already say when they open.
            // The rail has no ruler, so its nodes name their gate (points needed in both classes).
            bool tierClosed = depth != null && !depth.IsOpen(node.Tier);
            if (tierClosed)
                status = isRail ? $"needs {depth!.Gate(node.Tier)}+{depth.Gate(node.Tier)}" : "";
            else if (isRail && state == SkillTreeService.NodeViewState.Locked)
            {
                // Rail chains interleave in one column, so name the prerequisite instead of pointing down.
                var tree = SkillTreePrerequisites.FindOwnerTree(node);
                var pred = tree != null ? SkillTreePrerequisites.GetImmediatePredecessor(tree, node) : null;
                status = pred != null ? $"after {pred.Name}" : "after base";
            }
            bool showPips = maxRank > 1 && !tierClosed && (rank > 0 || state != SkillTreeService.NodeViewState.Locked);
            if (showPips && room > 0)
            {
                int pips = Math.Min(maxRank, Math.Max(1, room - 5));
                int filled = (int)Math.Round(rank * (double)pips / maxRank);
                canvas.AddText(sx, y + 1, new string(PipFull[0], filled), AsciiArtAssets.Colors.Green);
                if (pips - filled > 0)
                    canvas.AddText(sx + filled, y + 1, new string(PipEmpty[0], pips - filled), Dim);
                sx += pips + 1;
                room -= pips + 1;
            }
            if (room > 0 && status.Length > 0)
                canvas.AddText(sx, y + 1, Truncate(status, room), color);

            clickableElements.Add(new ClickableElement
            {
                X = cellX,
                Y = y,
                Width = cellW,
                Height = NodeHeight,
                Type = ElementType.Button,
                Value = (allIndex + 1).ToString(),
                DisplayText = node.Name
            });
        }

        private static int PipsLength(int maxRank, int rank, string status) =>
            (maxRank > 1 ? maxRank + 1 : 0) + status.Length;

        private static (string glyph, Color color) GlyphFor(SkillTreeService.NodeViewState state) => state switch
        {
            SkillTreeService.NodeViewState.Maxed => (LearnedGlyph, AsciiArtAssets.Colors.Green),
            SkillTreeService.NodeViewState.Available => (AvailableGlyph, AsciiArtAssets.Colors.Cyan),
            SkillTreeService.NodeViewState.Unaffordable => (NeedSpGlyph, AsciiArtAssets.Colors.Yellow),
            _ => (LockedGlyph, Dim)
        };

        /// <summary>Second-row text: why a node is in its state, in as few characters as possible.</summary>
        private static string StatusText(SkillTreeNodeDefinition node, SkillTreeService.NodeViewState state, int rank, int maxRank, DepthInfo? depth)
        {
            string ranks = maxRank > 1 ? $"{rank}/{maxRank}" : "";
            return state switch
            {
                SkillTreeService.NodeViewState.Maxed => maxRank > 1 ? ranks : "learned",
                SkillTreeService.NodeViewState.Available => maxRank > 1 ? $"{ranks} +{node.Cost}" : $"{node.Cost} SP",
                SkillTreeService.NodeViewState.Unaffordable => maxRank > 1 ? $"{ranks} {node.Cost}SP" : $"need {node.Cost} SP",
                SkillTreeService.NodeViewState.Locked when depth != null && !depth.IsOpen(node.Tier) => $"@{depth.Gate(node.Tier)} pts",
                SkillTreeService.NodeViewState.Locked => $"needs {AsciiArtAssets.UIElements.ArrowDown}",
                _ => "other path"
            };
        }

        // ─────────────────────────────────────────────────────────────
        // Details panel
        // ─────────────────────────────────────────────────────────────

        private void DrawDetailPanel(
            Character player,
            SkillTreeDisplayModel model,
            DepthInfo depth,
            int selectedIndex,
            int x,
            int y,
            int width,
            int height,
            string? statusMessage)
        {
            if (width < 16 || height < 4)
                return;

            var node = model.AllNodes[Math.Clamp(selectedIndex, 0, model.AllNodes.Count - 1)];
            bool onRail = model.IsRailIndex(selectedIndex);
            var state = SkillTreeService.GetNodeState(player.Progression, node);
            var ownerPath = SkillTreeService.FindOwnerPath(node);
            int rank = player.Progression.GetSkillRank(node.Id);
            int maxRank = Math.Max(1, node.MaxRank);
            Color border = onRail ? AsciiArtAssets.Colors.Magenta : AsciiArtAssets.Colors.Gold;
            int inner = width - 4;
            int bottom = y + height - 1;

            // ┌─ NAME ─────┐
            canvas.AddText(x, y, "┌", border);
            DrawTitledRule(x + 1, y, width - 2, node.Name.ToUpperInvariant(), border, AsciiArtAssets.Colors.White, '─', leftAlign: true);
            canvas.AddText(x + width - 1, y, "┐", border);
            for (int row = y + 1; row < bottom; row++)
            {
                canvas.AddText(x, row, "│", border);
                canvas.AddText(x + width - 1, row, "│", border);
            }
            canvas.AddText(x, bottom, "└", border);
            canvas.AddText(x + 1, bottom, new string('─', width - 2), border);
            canvas.AddText(x + width - 1, bottom, "┘", border);

            // Action row sits on a divider just above the bottom border.
            int actionRow = bottom - 1;
            int dividerRow = bottom - 2;
            canvas.AddText(x, dividerRow, "├", border);
            canvas.AddText(x + 1, dividerRow, new string('─', width - 2), border);
            canvas.AddText(x + width - 1, dividerRow, "┤", border);
            var statusLines = string.IsNullOrWhiteSpace(statusMessage)
                ? new List<string>()
                : TextWrapper.WrapText(statusMessage, inner).Take(2).ToList();
            int maxRow = dividerRow - statusLines.Count;

            int ty = y + 1;
            void Line(string text, Color color)
            {
                if (ty >= maxRow) return;
                canvas.AddText(x + 2, ty++, Truncate(text, inner), color);
            }
            void Wrapped(string text, Color color, int maxLines = 99)
            {
                if (string.IsNullOrWhiteSpace(text)) return;
                foreach (var line in TextWrapper.WrapText(text, inner).Take(maxLines))
                    Line(line, color);
            }

            string branch = onRail ? (model.DuoName ?? "Shared") : node.Branch;
            Line($"{branch} · Tier {node.Tier} · {node.Type}", AsciiArtAssets.Colors.Cyan);

            if (ty < maxRow)
            {
                var (glyph, color) = GlyphFor(state);
                canvas.AddText(x + 2, ty, glyph, color);
                string rankText = maxRank > 1 ? $"Rank {rank}/{maxRank}" : rank > 0 ? "Learned" : "Not learned";
                canvas.AddText(x + 4, ty, rankText, AsciiArtAssets.Colors.White);
                int px = x + 4 + rankText.Length + 1;
                if (maxRank > 1 && px + maxRank <= x + width - 2)
                {
                    canvas.AddText(px, ty, new string(PipFull[0], rank), AsciiArtAssets.Colors.Green);
                    canvas.AddText(px + rank, ty, new string(PipEmpty[0], maxRank - rank), Dim);
                }
                ty++;
            }
            var ownerTree = SkillTreePrerequisites.FindOwnerTree(node);
            var hybrid = HybridTrees.ForTree(ownerTree);
            string spName = hybrid != null ? $"{hybrid.ClassA} + {hybrid.ClassB}" : ownerPath?.ToString() ?? "";
            Line($"{node.Cost} {spName} SP per rank", AsciiArtAssets.Colors.Gray);
            if (hybrid == null && ownerPath.HasValue)
            {
                var prog = player.Progression;
                int pts = prog.GetClassPoints(ownerPath.Value);
                Wrapped($"Free SP {prog.GetAvailableSkillPoints(ownerPath.Value)} = {pts} class pts − {prog.GetSpentSkillPoints(ownerPath.Value)} spent. +1 per level-up with a {ownerPath.Value} equipped.", Dim, 2);
            }
            ty++;

            Wrapped(node.DisplayEffect, AsciiArtAssets.Colors.White, 5);
            if (!string.IsNullOrWhiteSpace(node.Payoff))
                Wrapped($"» {node.Payoff}", AsciiArtAssets.Colors.Gray, 2);
            ty++;

            if (hybrid != null)
            {
                // Hybrid rules as a checklist: each line is something the player can go and fix.
                var prog = player.Progression;
                int need = HybridTrees.UnlockPointsPerParent;
                int a = prog.GetClassPoints(hybrid.ParentA);
                int b = prog.GetClassPoints(hybrid.ParentB);
                int gate = HybridTrees.TierGate(node.Tier);
                int lower = Math.Min(a, b);
                void Check(bool ok, string text)
                {
                    if (ty >= maxRow) return;
                    canvas.AddText(x + 2, ty, ok ? LearnedGlyph : "x", ok ? AsciiArtAssets.Colors.Green : AsciiArtAssets.Colors.Yellow);
                    foreach (var line in TextWrapper.WrapText(text, inner - 2).Take(2))
                    {
                        if (ty >= maxRow) return;
                        canvas.AddText(x + 4, ty++, line, ok ? AsciiArtAssets.Colors.Gray : AsciiArtAssets.Colors.White);
                    }
                }
                Line($"HYBRID · {hybrid.Key.ToUpperInvariant()}", AsciiArtAssets.Colors.Magenta);
                Check(a >= need && b >= need,
                    $"Opens at {need} points in both {hybrid.ClassA} ({hybrid.ParentA}) and {hybrid.ClassB} ({hybrid.ParentB}) · you have {a} and {b}");
                if (node.Tier > 0)
                    Check(lower >= gate, $"Tier {node.Tier} needs {gate} in both · your lower is {lower}");
                Check(prog.GetAvailableSkillPoints(hybrid.ParentA) + prog.GetAvailableSkillPoints(hybrid.ParentB) >= node.Cost,
                    $"Each rank spends half from each class's free SP · free: {prog.GetAvailableSkillPoints(hybrid.ParentA)} + {prog.GetAvailableSkillPoints(hybrid.ParentB)}");
                var pred = ownerTree != null ? SkillTreePrerequisites.GetImmediatePredecessor(ownerTree, node) : null;
                if (node.Tier > 0)
                    Check(pred == null || prog.GetSkillRank(pred.Id) > 0,
                        pred != null ? $"Learn {pred.Name} first" : "First skill on its branch");
                ty++;
            }
            else
            {
                // Why it's locked, in order of what to fix first.
                if (!onRail && !depth.IsOpen(node.Tier))
                    Wrapped($"{AsciiArtAssets.UIElements.ArrowUp} Opens at {depth.Gate(node.Tier)} {model.PrimaryPath} pts (have {depth.Points})", AsciiArtAssets.Colors.Yellow, 2);
                string req = SkillTreePrerequisites.FormatRequirementSummary(node);
                if (!string.IsNullOrEmpty(req))
                    Wrapped(req, state == SkillTreeService.NodeViewState.Locked ? AsciiArtAssets.Colors.Yellow : Dim, 2);
            }

            if (ownerTree != null && node.Tier > 0 && !IsCenterBranch(node.Branch))
            {
                var chain = SkillTreePrerequisites.GetBranchSequenceRootToTip(ownerTree, node.Branch);
                if (chain.Count > 1)
                {
                    string arrow = $" {AsciiArtAssets.UIElements.ArrowRight} ";
                    string text = "Chain: " + string.Join(arrow, chain.Select(n =>
                        string.Equals(n.Id, node.Id, StringComparison.OrdinalIgnoreCase) ? $"[{n.Name}]" : n.Name));
                    Wrapped(text, Dim, 3);
                }
            }

            // Status (last learn result etc.) always gets the rows just above the divider.
            int sy = Math.Max(y + 1, maxRow);
            foreach (var line in statusLines)
                canvas.AddText(x + 2, sy++, Truncate(line, inner), AsciiArtAssets.Colors.Yellow);

            // ├──────┤ then the action
            string action = state switch
            {
                SkillTreeService.NodeViewState.Maxed => "Fully ranked",
                SkillTreeService.NodeViewState.Available => rank > 0
                    ? $"[L] Raise rank  (-{node.Cost} {spName} SP)"
                    : $"[L] Learn  (-{node.Cost} {spName} SP)",
                SkillTreeService.NodeViewState.Unaffordable => $"Need {node.Cost} {spName} SP",
                SkillTreeService.NodeViewState.Locked => "Locked",
                _ => "Earn points on this path first"
            };
            action = Truncate(action, inner);
            Color actionColor = state == SkillTreeService.NodeViewState.Available ? AsciiArtAssets.Colors.Gold
                : state == SkillTreeService.NodeViewState.Maxed ? AsciiArtAssets.Colors.Green
                : Dim;
            canvas.AddText(x + 2, actionRow, action, actionColor);
            if (state == SkillTreeService.NodeViewState.Available)
            {
                clickableElements.Add(new ClickableElement
                {
                    X = x + 2,
                    Y = actionRow,
                    Width = action.Length,
                    Height = 1,
                    Type = ElementType.Button,
                    Value = "l",
                    DisplayText = action
                });
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────

        /// <summary>Draws "═══ TITLE ═══" (or left-aligned "─ TITLE ─────") as separate runs so nothing overlaps.</summary>
        private void DrawTitledRule(int x, int y, int width, string title, Color ruleColor, Color titleColor, char rule = '─', bool leftAlign = false)
        {
            if (width <= 0) return;
            string t = Truncate(title, Math.Max(0, width - 4));
            if (t.Length == 0)
            {
                canvas.AddText(x, y, new string(rule, width), ruleColor);
                return;
            }
            int labelW = t.Length + 2;
            int leftW = leftAlign ? 1 : Math.Max(1, (width - labelW) / 2);
            int rightW = Math.Max(0, width - leftW - labelW);
            canvas.AddText(x, y, new string(rule, leftW), ruleColor);
            canvas.AddText(x + leftW + 1, y, t, titleColor);
            if (rightW > 0)
                canvas.AddText(x + leftW + labelW, y, new string(rule, rightW), ruleColor);
        }

        public static IReadOnlyList<SkillTreeNodeDefinition> OrderNodes(IEnumerable<SkillTreeNodeDefinition> source) =>
            SkillTreeService.OrderNodes(source);

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

        public static int ResolveColumn(SkillTreeNodeDefinition node, IReadOnlyList<string> branchOrder, int centerCol)
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

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || max <= 0) return "";
            return text.Length <= max ? text : text.Substring(0, Math.Max(1, max - 1)) + "…";
        }

        private sealed class Lane
        {
            public string Title { get; }
            public int X { get; }
            public int Width { get; }
            public bool IsRail { get; }

            public Lane(string title, int x, int width, bool isRail)
            {
                Title = title;
                X = x;
                Width = width;
                IsRail = isRail;
            }
        }

        private readonly struct Viewport
        {
            public int Top { get; }
            public int Bottom { get; }
            public int Scroll { get; }

            public Viewport(int top, int bottom, int scroll)
            {
                Top = top;
                Bottom = bottom;
                Scroll = scroll;
            }

            public int ToScreen(int contentRow) => Top + contentRow - Scroll;
            public bool Contains(int screenRow) => screenRow >= Top && screenRow < Bottom;
        }

        /// <summary>Tier point gates for the viewed tree and how many points the player has toward them.</summary>
        private sealed class DepthInfo
        {
            private readonly bool hybrid;
            public int Points { get; }
            public int? NextTier { get; }

            private DepthInfo(bool hybrid, int points, int maxTier)
            {
                this.hybrid = hybrid;
                Points = points;
                for (int t = 1; t <= maxTier; t++)
                {
                    if (Gate(t) > points)
                    {
                        NextTier = t;
                        break;
                    }
                }
            }

            public int Gate(int tier) => hybrid ? HybridTrees.TierGate(tier) : SkillTreesConfig.TierPointGate(tier);
            public bool IsOpen(int tier) => tier <= 0 || Points >= Gate(tier);

            public static DepthInfo For(CharacterProgression progression, SkillTreeDefinition tree, WeaponType path)
            {
                int maxTier = tree.Nodes?.Count > 0 ? tree.Nodes.Max(n => n.Tier) : 0;
                if (HybridTrees.ForTree(tree) is HybridInfo h)
                    return new DepthInfo(true, HybridTrees.LowerParentPoints(progression, h), maxTier);
                return new DepthInfo(false, progression.GetClassPoints(path), maxTier);
            }
        }

        private readonly struct NodePlacement
        {
            public SkillTreeNodeDefinition Node { get; }
            public int Index { get; }
            public int Col { get; }
            public int X { get; }
            public int ContentTop { get; }
            public int W { get; }
            public bool Spans { get; }

            public NodePlacement(SkillTreeNodeDefinition node, int index, int col, int x, int contentTop, int w, bool spans)
            {
                Node = node;
                Index = index;
                Col = col;
                X = x;
                ContentTop = contentTop;
                W = w;
                Spans = spans;
            }
        }

        /// <summary>Precomputed content-space tops for each node id, plus each tier's band.</summary>
        public sealed class ContentLayout
        {
            public int ContentHeight { get; }
            public IReadOnlyDictionary<string, int> Tops { get; }
            public IReadOnlyDictionary<int, (int top, int height)> Bands { get; }

            public ContentLayout(int contentHeight, IReadOnlyDictionary<string, int> tops, IReadOnlyDictionary<int, (int top, int height)> bands)
            {
                ContentHeight = contentHeight;
                Tops = tops;
                Bands = bands;
            }
        }
    }
}
